using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Research
{
    /// <summary>
    /// The Laboratory's brain: what has been researched, what is being researched right now, and
    /// what that permits.
    ///
    /// The rules, as decided:
    /// - As many researches at once as the Laboratory has SCIENTISTS (the user, 2026-10-28: each
    ///   scientist is a slot -- the player must not hit a wall on upgrades). Coins (and, from level
    ///   3, an upgrade crystal) are paid when one STARTS; cancelling pays most of the coins and the
    ///   whole crystal back, and losing the Laboratory pays everything back.
    /// - Time is real seconds, the topic's own. Taking scientists away stops the researches beyond
    ///   the scientists left -- the newest first -- where they stand, rather than losing them.
    /// - The Laboratory's own level decides WHAT may be researched: level 1 opens things, level 2
    ///   permits second levels, level 3 permits third ones. The Laboratory is deliberately outside
    ///   its own gate -- its sheet row names no level research, so nothing can lock its upgrade
    ///   behind an upgrade of itself.
    /// - Finished research is never lost. Losing the Laboratory only stops NEW research; everything
    ///   already researched stays researched, and buildings can still be upgraded to those levels.
    /// </summary>
    public class ResearchManager : MonoBehaviour
    {
        /// <summary>The building that does the researching. One id, in one place, because three separate systems ask about it.</summary>
        public const string LaboratoryBuildingId = "Laboratory";

        public static ResearchManager Instance { get; private set; }

        private readonly HashSet<string> _completed = new HashSet<string>();

        /// <summary>One research under way: what, how far in, and what was paid for it.</summary>
        public class Running
        {
            public ResearchTopic Topic;
            public float Elapsed;
            public int PaidCoins;
            public int PaidCrystals;
        }

        // In the order they were started: the first LabWorkers of them run, the rest stand still.
        private readonly List<Running> _running = new List<Running>();

        /// <summary>Everything under way, oldest first.</summary>
        public IReadOnlyList<Running> UnderWay => _running;

        /// <summary>The oldest research under way, or null -- for the callers that only ask "is anything being researched".</summary>
        public ResearchTopic Current => _running.Count > 0 ? _running[0].Topic : null;

        // The lab is found once per lab built, not per frame: HasAny is a dictionary lookup, and the
        // scene scan behind it only runs when that says there is a lab and the cached one is gone.
        private BuildingInstance _lab;
        private ProductionBuilding _labWorkplace;

        /// <summary>Raised when anything the Laboratory's window or the hotbar shows changes: a research started, finished, or was cancelled.</summary>
        public event Action OnResearchChanged;

        /// <summary>Level of the best Laboratory standing, or 0 when there is none.</summary>
        public int LabLevel
        {
            get
            {
                var lab = Lab;
                return lab != null ? lab.Level : 0;
            }
        }

        /// <summary>Scientists currently assigned to that Laboratory.</summary>
        public int LabWorkers => _labWorkplace != null && Lab != null ? _labWorkplace.AssignedWorkers : 0;

        /// <summary>How many more researches the scientists could take on right now.</summary>
        public int FreeSlots => Mathf.Max(0, LabWorkers - _running.Count);

        public bool IsRunning(ResearchTopic topic) => Find(topic) != null;

        /// <summary>How long this research takes in all: the topic's own seconds, never under `research_min_seconds`.</summary>
        public static float TotalSeconds(ResearchTopic topic)
        {
            if (topic == null) return 0f;
            var config = BalanceConfig.Instance;
            return Mathf.Max(config != null ? config.ResearchMinSeconds : 0f, topic.BaseSeconds);
        }

        /// <summary>Seconds left of this research, or zero when it is not under way.</summary>
        public float RemainingSeconds(ResearchTopic topic)
        {
            var run = Find(topic);
            return run != null ? Mathf.Max(0f, TotalSeconds(topic) - run.Elapsed) : 0f;
        }

        /// <summary>Under way but standing still: there are fewer scientists than researches, and this is one of the newest.</summary>
        public bool IsPaused(ResearchTopic topic)
        {
            var index = IndexOf(topic);
            return index >= 0 && index >= LabWorkers;
        }

        private Running Find(ResearchTopic topic)
        {
            var index = IndexOf(topic);
            return index >= 0 ? _running[index] : null;
        }

        private int IndexOf(ResearchTopic topic)
        {
            if (topic == null) return -1;
            for (var i = 0; i < _running.Count; i++)
            {
                if (_running[i].Topic.Id == topic.Id) return i;
            }
            return -1;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_running.Count == 0) return;

            if (Lab == null)
            {
                AbandonForLostLab();
                return;
            }

            // One scientist per research: the ones beyond the scientists stand still, not lost.
            var working = Mathf.Min(_running.Count, LabWorkers);
            for (var i = 0; i < working; i++) _running[i].Elapsed += Time.deltaTime;
            for (var i = working - 1; i >= 0; i--)
            {
                if (_running[i].Elapsed >= TotalSeconds(_running[i].Topic)) Complete(i);
            }
        }

        /// <summary>
        /// The Laboratory that does the work: the highest-level one standing, and among equals the
        /// one with the most scientists on it. Two laboratories are not a designed case -- this only
        /// has to be predictable rather than clever.
        /// </summary>
        private BuildingInstance Lab
        {
            get
            {
                if (_lab != null) return _lab;
                if (!BuildingInstance.HasAny(LaboratoryBuildingId)) return null;

                BuildingInstance best = null;
                ProductionBuilding bestWorkplace = null;
                foreach (var instance in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
                {
                    if (instance.Data == null || instance.Data.buildingName != LaboratoryBuildingId) continue;

                    var workplace = instance.GetComponent<ProductionBuilding>();
                    if (best != null)
                    {
                        var betterLevel = instance.Level > best.Level;
                        var sameLevelMoreHands = instance.Level == best.Level
                            && (workplace != null ? workplace.AssignedWorkers : 0) > (bestWorkplace != null ? bestWorkplace.AssignedWorkers : 0);
                        if (!betterLevel && !sameLevelMoreHands) continue;
                    }

                    best = instance;
                    bestWorkplace = workplace;
                }

                _lab = best;
                _labWorkplace = bestWorkplace;
                return _lab;
            }
        }

        /// <summary>What a cancelled research pays back. Rounded down, so cancelling is never free money.</summary>
        public static int RefundCoins(int paidCoins, int refundPercent)
        {
            return Mathf.Max(0, paidCoins * Mathf.Clamp(refundPercent, 0, 100) / 100);
        }

        /// <summary>Coins the player gets back if they cancel this research right now.</summary>
        public int CancelRefund(ResearchTopic topic)
        {
            var run = Find(topic);
            return run != null ? RefundCoins(run.PaidCoins, BalanceConfig.Instance.ResearchCancelRefundPercent) : 0;
        }

        public bool IsCompleted(string topicId)
        {
            return topicId != null && _completed.Contains(topicId);
        }

        /// <summary>
        /// IsBuildingUnlocked for the many call sites that just want a yes/no and should not care
        /// whether research exists in this scene at all -- no manager means nothing is gated, which
        /// is what an EditMode fixture or the main menu needs.
        /// </summary>
        public static bool BuildingUnlocked(string buildingId)
        {
            return Instance == null || Instance.IsBuildingUnlocked(buildingId);
        }

        /// <summary>Whether the settlement may build this at all. A building the sheet never locked is always buildable.</summary>
        public bool IsBuildingUnlocked(string buildingId)
        {
            if (!ResearchCatalog.NeedsUnlock(buildingId)) return true;
            return IsCompleted(ResearchTopic.UnlockBuildingId(buildingId));
        }

        /// <summary>
        /// Whether this building may be upgraded to that level yet. Level 1 is how it is placed, and
        /// a level the sheet gates with no research is not gated at all -- which is how the
        /// Laboratory's own levels stay reachable.
        /// </summary>
        public bool IsBuildingLevelResearched(string buildingId, int level)
        {
            if (level <= 1) return true;

            var topic = ResearchCatalog.ById(ResearchTopic.BuildingLevelId(buildingId, level));
            return topic == null || IsCompleted(topic.Id);
        }

        /// <summary>Whether this soldier type can be recruited. Militia starts open; later tiers will need their unlock.</summary>
        public bool IsUnitUnlocked(SoldierType type)
        {
            var topic = ResearchCatalog.ById(ResearchTopic.UnlockUnitId(SoldierStats.SheetIdOf(type)));
            return topic == null || IsCompleted(topic.Id);
        }

        /// <summary>
        /// The level every soldier of this type fights at: the highest researched one. Applies to
        /// soldiers already recruited, which is why it is asked of this rather than stored per unit.
        /// </summary>
        public int UnitLevel(SoldierType type)
        {
            var sheetId = SoldierStats.SheetIdOf(type);
            var level = 1;
            for (var candidate = 2; candidate <= UnitBalance.MaxLevel; candidate++)
            {
                if (IsCompleted(ResearchTopic.UnitLevelId(sheetId, candidate))) level = candidate;
            }
            return level;
        }

        /// <summary>
        /// Whether this topic belongs in the window's list at all. The list is deliberately compact:
        /// what can be researched now, plus what already has been (greyed out). A topic waiting on a
        /// bigger Laboratory or on the level below it is not shown -- there would be a hundred rows
        /// of them.
        /// </summary>
        public bool IsAvailable(ResearchTopic topic)
        {
            if (topic == null) return false;
            if (IsCompleted(topic.Id)) return false;
            if (topic.PrerequisiteTopicId != null && !IsCompleted(topic.PrerequisiteTopicId)) return false;
            return LabLevel >= topic.RequiredLabLevel;
        }

        /// <summary>
        /// Why this research cannot start right now, or null when it can. A sentence rather than a
        /// bool for the same reason ArmyManager.DescribeRecruitBlocker is: a greyed-out button that
        /// will not say what is wrong is worse than no button.
        /// </summary>
        public string DescribeBlocker(ResearchTopic topic)
        {
            if (topic == null) return Localization.Get("#research_blocked_unknown");
            if (IsCompleted(topic.Id)) return Localization.Get("#research_done");
            if (IsRunning(topic)) return Localization.Get("#research_blocked_busy");

            if (Lab == null) return Localization.Get("#research_blocked_no_lab");
            if (LabLevel < topic.RequiredLabLevel) return Localization.Format("#research_blocked_lab_level", topic.RequiredLabLevel);

            if (topic.PrerequisiteTopicId != null && !IsCompleted(topic.PrerequisiteTopicId))
            {
                var prerequisite = ResearchCatalog.ById(topic.PrerequisiteTopicId);
                return Localization.Format("#research_blocked_prereq", prerequisite != null ? prerequisite.Title : topic.PrerequisiteTopicId);
            }

            if (LabWorkers <= 0) return Localization.Get("#research_blocked_workers");
            if (FreeSlots <= 0) return Localization.Get("#research_blocked_slots");

            var resources = ResourceManager.Instance;
            if (resources != null && !resources.HasEnough(CoinCost(topic.Coins))) return Localization.Get("#research_blocked_coins");
            if (topic.Crystals > 0 && !CrystalsAreFree && Wallet.UpgradeCrystals < topic.Crystals) return Localization.Get("#research_blocked_crystals");

            return null;
        }

        /// <summary>Starts a research, taking its coins. False (and nothing spent) if DescribeBlocker had anything to say.</summary>
        public bool TryStart(ResearchTopic topic)
        {
            if (DescribeBlocker(topic) != null) return false;

            var resources = ResourceManager.Instance;
            if (resources != null && !resources.TrySpend(CoinCost(topic.Coins))) return false;

            // Nothing was actually deducted while the infinite-resources cheat is on, so nothing may
            // be refunded later either -- paying it back would mint coins (the same trap
            // ArmyManager.TryRecruit documents).
            var run = new Running
            {
                Topic = topic,
                PaidCoins = resources != null && resources.InfiniteResources ? 0 : topic.Coins,
                PaidCrystals = CrystalsAreFree ? 0 : topic.Crystals,
            };
            if (run.PaidCrystals > 0) Wallet.UpgradeCrystals -= run.PaidCrystals;
            _running.Add(run);

            EventLogManager.Instance?.Log(Localization.Format("#log_research_started", topic.Title));
            OnResearchChanged?.Invoke();
            return true;
        }

        /// <summary>The player changing their mind: most of the coins come back, the crystal all of it, the progress does not.</summary>
        public void Cancel(ResearchTopic topic)
        {
            var index = IndexOf(topic);
            if (index < 0) return;
            var run = _running[index];
            Refund(RefundCoins(run.PaidCoins, BalanceConfig.Instance.ResearchCancelRefundPercent), run.PaidCrystals);
            EventLogManager.Instance?.Log(Localization.Format("#log_research_cancelled", run.Topic.Title));
            _running.RemoveAt(index);
            OnResearchChanged?.Invoke();
        }

        /// <summary>
        /// The Laboratory is gone mid-research. Everything comes back, unlike a cancellation: the
        /// player did not choose this, an orc did.
        /// </summary>
        private void AbandonForLostLab()
        {
            foreach (var run in _running)
            {
                Refund(run.PaidCoins, run.PaidCrystals);
                EventLogManager.Instance?.Post(GameEvent.ForSettlement(Localization.Format("#log_research_lost", run.Topic.Title), EventSeverity.Warning, EventGlyph.Alert, LaboratoryBuildingId));
            }
            _running.Clear();
            OnResearchChanged?.Invoke();
        }

        private void Complete(int index)
        {
            var finished = _running[index].Topic;
            _running.RemoveAt(index);
            _completed.Add(finished.Id);

            // Every soldier of the type, including the ones already standing: the whole point of a
            // level being researched rather than bought per recruit.
            if (finished.Kind == ResearchKind.UnitLevel) SoldierUnit.RefreshStatsForAll();

            EventLogManager.Instance?.Post(GameEvent.ForSettlement(Localization.Format("#log_research_done", finished.Title), EventSeverity.Good, EventGlyph.Star, LaboratoryBuildingId, LaboratoryBuildingId));
            OnResearchChanged?.Invoke();
        }

        private static void Refund(int coins, int crystals)
        {
            if (coins > 0) ResourceManager.Instance?.Add(ResourceType.Coins, coins);
            if (crystals > 0) Wallet.UpgradeCrystals += crystals;
        }

        /// <summary>The infinite-resources cheat covers the crystals too: a tester looking at level 5 should not need a wallet.</summary>
        private static bool CrystalsAreFree => ResourceManager.Instance != null && ResourceManager.Instance.InfiniteResources;

        // Rebuilt in place rather than allocated per call -- DescribeBlocker runs on every refresh of
        // a window holding dozens of rows.
        private readonly List<ResourceAmount> _coinCost = new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Coins, amount = 0 } };

        private List<ResourceAmount> CoinCost(int coins)
        {
            _coinCost[0].amount = coins;
            return _coinCost;
        }

        /// <summary>
        /// Marks a topic researched with no coins and no waiting. This is the cheats branch's tool
        /// (GameCheats/Research) and what the tests use to get past a gate they are not the ones
        /// under test -- never a gameplay path: TryStart is.
        /// </summary>
        public bool CompleteInstantly(string topicId)
        {
            var topic = ResearchCatalog.ById(topicId);
            if (topic == null || IsCompleted(topicId)) return false;

            // Prerequisites too, or granting a level 3 would leave a hole a real playthrough could
            // never produce.
            if (topic.PrerequisiteTopicId != null) CompleteInstantly(topic.PrerequisiteTopicId);

            _completed.Add(topicId);
            if (topic.Kind == ResearchKind.UnitLevel) SoldierUnit.RefreshStatsForAll();
            OnResearchChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Back to a settlement that has researched nothing. Only for tests, which share one loaded
        /// scene between them and must not inherit each other's unlocks. Refunds nothing: no coins
        /// were spent by whoever granted them.
        /// </summary>
        public void ResetForTesting()
        {
            _completed.Clear();
            _running.Clear();
            OnResearchChanged?.Invoke();
        }

        /// <summary>Every topic at once -- the cheat for looking at a fully developed settlement without playing to it.</summary>
        public int CompleteEverything()
        {
            var granted = 0;
            foreach (var topic in ResearchCatalog.All)
            {
                if (_completed.Add(topic.Id)) granted++;
            }

            if (granted > 0)
            {
                SoldierUnit.RefreshStatsForAll();
                OnResearchChanged?.Invoke();
            }
            return granted;
        }

        /// <summary>Everything researched, for the save file.</summary>
        public IEnumerable<string> CompletedTopicIds => _completed;

        /// <summary>What is under way, for the save file: each topic, how far in, and what was paid for it.</summary>
        public IEnumerable<Running> UnderWayForSave => _running;

        /// <summary>
        /// Restores a saved game's research. Ids that no longer exist (a renamed sheet row) are
        /// dropped with a warning rather than kept as dead entries -- the save's own coins were
        /// already spent, so the honest thing is to say what was lost.
        /// </summary>
        public void RestoreFromSave(IEnumerable<string> completedTopicIds, IEnumerable<Running> underWay)
        {
            _completed.Clear();
            if (completedTopicIds != null)
            {
                foreach (var id in completedTopicIds)
                {
                    if (string.IsNullOrEmpty(id)) continue;
                    if (ResearchCatalog.ById(id) == null)
                    {
                        Debug.LogWarning($"ResearchManager: the save names research '{id}', which the balance sheet no longer has. Dropping it.");
                        continue;
                    }
                    _completed.Add(id);
                }
            }

            _running.Clear();
            if (underWay != null)
            {
                foreach (var run in underWay)
                {
                    if (run == null || run.Topic == null || IsCompleted(run.Topic.Id) || IsRunning(run.Topic)) continue;
                    run.Elapsed = Mathf.Max(0f, run.Elapsed);
                    run.PaidCoins = Mathf.Max(0, run.PaidCoins);
                    run.PaidCrystals = Mathf.Max(0, run.PaidCrystals);
                    _running.Add(run);
                }
            }

            // Soldiers restored before this point were built at level 1; a save with researched
            // levels has to bring them up to date.
            SoldierUnit.RefreshStatsForAll();
            OnResearchChanged?.Invoke();
        }
    }
}
