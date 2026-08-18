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
    /// - One research at a time. Coins are paid when it STARTS; cancelling pays most of them back,
    ///   and losing the Laboratory mid-research pays all of them back (the player did not choose that).
    /// - Time is real seconds. Scientists shorten it: the first one is what makes it run at all, and
    ///   each one after that cuts a fixed number of seconds off the total -- so pulling a scientist
    ///   off mid-research adds those seconds back, exactly as if they had never been there.
    /// - With no scientists the research stands still rather than being lost.
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

        /// <summary>What is being researched, or null. Never set without coins having been taken for it.</summary>
        public ResearchTopic Current { get; private set; }

        private float _elapsedSeconds;
        private int _paidCoins;

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

        /// <summary>How long the current research will take in total at the present number of scientists. Zero when nothing is running.</summary>
        public float CurrentTotalSeconds => Current != null ? DurationSeconds(Current.BaseSeconds, LabWorkers) : 0f;

        /// <summary>Seconds left of the current research at the present number of scientists -- which is why taking a scientist away makes this number jump up.</summary>
        public float RemainingSeconds => Mathf.Max(0f, CurrentTotalSeconds - _elapsedSeconds);

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
            if (Current == null) return;

            if (Lab == null)
            {
                AbandonForLostLab();
                return;
            }

            // Standing still, not lost: the player took the scientists off, and they may put them back.
            if (LabWorkers <= 0) return;

            _elapsedSeconds += Time.deltaTime;
            if (_elapsedSeconds >= CurrentTotalSeconds) Complete();
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

        /// <summary>
        /// How long a research of this base length takes with this many scientists. The first
        /// `research_free_workers` of them buy no speed -- one scientist is what makes the research
        /// run in the first place -- and the total can never fall below `research_min_seconds`.
        ///
        /// Pure and static so the arithmetic is pinned down by an EditMode test: the interesting
        /// cases are one scientist (no bonus), ten (the full bonus) and a research short enough for
        /// the floor to bite.
        /// </summary>
        public static float DurationSeconds(float baseSeconds, int workers, float secondsPerWorker, int freeWorkers, float minSeconds)
        {
            var paidWorkers = Mathf.Max(0, workers - Mathf.Max(0, freeWorkers));
            return Mathf.Max(minSeconds, baseSeconds - paidWorkers * secondsPerWorker);
        }

        private static float DurationSeconds(float baseSeconds, int workers)
        {
            var config = BalanceConfig.Instance;
            return DurationSeconds(baseSeconds, workers, config.ResearchSecondsPerWorker, config.ResearchFreeWorkers, config.ResearchMinSeconds);
        }

        /// <summary>What a cancelled research pays back. Rounded down, so cancelling is never free money.</summary>
        public static int RefundCoins(int paidCoins, int refundPercent)
        {
            return Mathf.Max(0, paidCoins * Mathf.Clamp(refundPercent, 0, 100) / 100);
        }

        /// <summary>Coins the player gets back if they cancel right now -- shown on the cancel button, so the number they see is the number they get.</summary>
        public int CurrentCancelRefund => RefundCoins(_paidCoins, BalanceConfig.Instance.ResearchCancelRefundPercent);

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
            if (Current != null) return Localization.Get("#research_blocked_busy");

            if (Lab == null) return Localization.Get("#research_blocked_no_lab");
            if (LabLevel < topic.RequiredLabLevel) return Localization.Format("#research_blocked_lab_level", topic.RequiredLabLevel);

            if (topic.PrerequisiteTopicId != null && !IsCompleted(topic.PrerequisiteTopicId))
            {
                var prerequisite = ResearchCatalog.ById(topic.PrerequisiteTopicId);
                return Localization.Format("#research_blocked_prereq", prerequisite != null ? prerequisite.Title : topic.PrerequisiteTopicId);
            }

            if (LabWorkers <= 0) return Localization.Get("#research_blocked_workers");

            var resources = ResourceManager.Instance;
            if (resources != null && !resources.HasEnough(CoinCost(topic.Coins))) return Localization.Get("#research_blocked_coins");

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
            _paidCoins = resources != null && resources.InfiniteResources ? 0 : topic.Coins;
            Current = topic;
            _elapsedSeconds = 0f;

            EventLogManager.Instance?.Log(Localization.Format("#log_research_started", topic.Title));
            OnResearchChanged?.Invoke();
            return true;
        }

        /// <summary>The player changing their mind: most of the coins come back, the progress does not.</summary>
        public void CancelCurrent()
        {
            if (Current == null) return;

            Refund(CurrentCancelRefund);
            EventLogManager.Instance?.Log(Localization.Format("#log_research_cancelled", Current.Title));
            ClearCurrent();
        }

        /// <summary>
        /// The Laboratory is gone mid-research. All of the coins come back, unlike a cancellation:
        /// the player did not choose this, an orc did.
        /// </summary>
        private void AbandonForLostLab()
        {
            if (Current == null) return;

            Refund(_paidCoins);
            EventLogManager.Instance?.Log(Localization.Format("#log_research_lost", Current.Title));
            ClearCurrent();
        }

        private void Complete()
        {
            var finished = Current;
            _completed.Add(finished.Id);
            ClearCurrent();

            // Every soldier of the type, including the ones already standing: the whole point of a
            // level being researched rather than bought per recruit.
            if (finished.Kind == ResearchKind.UnitLevel) SoldierUnit.RefreshStatsForAll();

            EventLogManager.Instance?.Log(Localization.Format("#log_research_done", finished.Title));
        }

        private void ClearCurrent()
        {
            Current = null;
            _elapsedSeconds = 0f;
            _paidCoins = 0;
            OnResearchChanged?.Invoke();
        }

        private static void Refund(int coins)
        {
            if (coins <= 0) return;
            ResourceManager.Instance?.Add(ResourceType.Coins, coins);
        }

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
            Current = null;
            _elapsedSeconds = 0f;
            _paidCoins = 0;
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

        /// <summary>What is under way, for the save file: the topic, how far in it is, and how much was paid for it.</summary>
        public void ReadCurrentForSave(out string topicId, out float elapsedSeconds, out int paidCoins)
        {
            topicId = Current != null ? Current.Id : string.Empty;
            elapsedSeconds = Current != null ? _elapsedSeconds : 0f;
            paidCoins = Current != null ? _paidCoins : 0;
        }

        /// <summary>
        /// Restores a saved game's research. Ids that no longer exist (a renamed sheet row) are
        /// dropped with a warning rather than kept as dead entries -- the save's own coins were
        /// already spent, so the honest thing is to say what was lost.
        /// </summary>
        public void RestoreFromSave(IEnumerable<string> completedTopicIds, string currentTopicId, float elapsedSeconds, int paidCoins)
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

            Current = ResearchCatalog.ById(currentTopicId);
            _elapsedSeconds = Current != null ? Mathf.Max(0f, elapsedSeconds) : 0f;
            _paidCoins = Current != null ? Mathf.Max(0, paidCoins) : 0;

            // Soldiers restored before this point were built at level 1; a save with researched
            // levels has to bring them up to date.
            SoldierUnit.RefreshStatsForAll();
            OnResearchChanged?.Invoke();
        }
    }
}
