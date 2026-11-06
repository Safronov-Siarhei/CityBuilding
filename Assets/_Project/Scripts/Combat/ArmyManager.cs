using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// Owns the player's army: recruitment at the Barracks, the groups the player commands, the
    /// daily upkeep, and what a soldier costs the settlement when it dies.
    ///
    /// The population rule, as decided: recruiting takes one IDLE citizen out of the town (so the
    /// army is paid for in working hands, not just coins), disbanding walks that citizen back home,
    /// and a soldier killed in battle is a citizen the settlement never gets back. That asymmetry
    /// is the whole point -- losing a fight has to cost more than changing your mind about having
    /// an army.
    /// </summary>
    public class ArmyManager : MonoBehaviour
    {
        public static ArmyManager Instance { get; private set; }

        /// <summary>Soldiers appear a short distance from the barracks that recruited them so a stack of them doesn't materialise inside its walls.</summary>
        private const float SpawnJitterRadius = 1.2f;

        [SerializeField] private GameCalendar gameCalendar;

        /// <summary>The King's figure (Tools/blender/heroking.py, Models/Heroes); without it he is drawn in cubes like the soldiers.</summary>
        [SerializeField] private GameObject kingModel;
        [SerializeField] private GameObject queenModel;
        [SerializeField] private GameObject captainModel;

        /// <summary>The dungeon's gnome hirelings (Tools/blender/gnomes.py, Models/Monsters): the miner, the crossbowman, the ore guardian.</summary>
        [SerializeField] private GameObject[] hirelingModels = new GameObject[3];

        /// <summary>
        /// The soldiers' figures (Tools/blender/soldiers.py, Models/Soldiers), one per type and
        /// researched level: index type * SoldierLooks + level - 1 (metal at level 2, gold at 3).
        /// A missing level wears the one below; a type with none is drawn in cubes.
        /// </summary>
        [SerializeField] private GameObject[] soldierModels = new GameObject[17 * SoldierLooks];

        public const int SoldierLooks = 3;

        // Cube-primitive silhouettes, in the same style as every other prop in the game. Each tier
        // has to be tellable from the others at the camera's height and at a glance, so what
        // separates them is COLOUR and SHAPE rather than detail: a militiaman is brown with a
        // pitchfork over one shoulder, a spearman is blue with a shaft taller than he is, an archer
        // is green with a bow held out in front, and a man-at-arms is bare steel and visibly bigger.
        private static readonly Color MilitiaClothingColor = new Color(0.45f, 0.36f, 0.24f);
        private static readonly Color SpearmanClothingColor = new Color(0.3f, 0.34f, 0.5f);
        private static readonly Color ArcherClothingColor = new Color(0.24f, 0.4f, 0.26f);
        private static readonly Color ArmourColor = new Color(0.62f, 0.64f, 0.68f);
        private static readonly Color TabardColor = new Color(0.5f, 0.16f, 0.16f);
        private static readonly Color SkinColor = new Color(0.85f, 0.68f, 0.52f);
        private static readonly Color WoodColor = new Color(0.45f, 0.33f, 0.2f);
        private static readonly Color SteelColor = new Color(0.55f, 0.55f, 0.58f);
        private static readonly Color RoyalColor = new Color(0.36f, 0.18f, 0.52f);
        private static readonly Color CapeColor = new Color(0.66f, 0.12f, 0.14f);
        private static readonly Color GoldColor = new Color(0.95f, 0.76f, 0.2f);

        private readonly List<ArmyGroup> _groups = new List<ArmyGroup>();

        // Keyed by colour rather than held in a field each: four tiers over eight colours is more
        // than a field list stays readable at, and every soldier of a type shares one material,
        // which is what keeps them batching.
        private readonly Dictionary<Color, Material> _materials = new Dictionary<Color, Material>();

        /// <summary>Every group, in creation order -- the order the army panel lists them in.</summary>
        public IReadOnlyList<ArmyGroup> Groups => _groups;

        /// <summary>Raised when a group is created/emptied or the army's size changes. Order and priority changes fire ArmyGroup.OnChanged instead.</summary>
        public event Action OnArmyChanged;

        /// <summary>
        /// The group the player is currently commanding, or null. Kept here rather than in the
        /// army panel because it arbitrates world clicks: while a group is selected, a tap on the
        /// world is an order to it, and CitizenSelector/BuildingSelector stand down so one tap
        /// can't both move a group and open a building's panel.
        /// </summary>
        public ArmyGroup SelectedGroup { get; private set; }

        public event Action OnSelectionChanged;

        public void SelectGroup(ArmyGroup group)
        {
            if (SelectedGroup == group) return;
            // the tutorial hands the King and the soldier over only at the steps that ask for them (the user, 2026-11-06)
            if (group != null && Tutorial.TutorialMode.IsActive && !Tutorial.TutorialRules.MaySelect(group)) return;
            SelectedGroup = group;
            OnSelectionChanged?.Invoke();
        }

        /// <summary>Tapping the selected group's own icon again puts the player back in normal (build/inspect) mode -- the only way out of command mode on touch, where there's no Escape key.</summary>
        public void ToggleSelection(ArmyGroup group)
        {
            SelectGroup(SelectedGroup == group ? null : group);
        }

        /// <summary>How many soldiers of this type the army holds (the summoned ones aside). Per member: a squad is mixed.</summary>
        public int CountOf(SoldierType type)
        {
            var total = 0;
            foreach (var group in _groups)
            {
                if (group.IsSummoned) continue;
                var members = group.Members;
                for (var i = 0; i < members.Count; i++)
                    if (members[i] != null && members[i].Type == type) total++;
            }
            return total;
        }

        // ---- squads (the user, 2026-11-05, Design/features/army-control.md) ----

        /// <summary>Army control by squads: everywhere but an Invasion run, which keeps its per-type groups (InvasionGarrison).</summary>
        public static bool UsesSquads => !Invasion.InvasionMode.IsActive;

        private ArmyGroup _garrison;

        /// <summary>Every recruit joins it; it stays home and defends the main island, soldier by soldier. Made on first ask.</summary>
        public ArmyGroup Garrison
        {
            get
            {
                if (_garrison != null) return _garrison;
                var hall = Buildings.BuildingInstance.TownHall;
                _garrison = new ArmyGroup(SoldierType.Militia, hall != null ? hall.transform.position : transform.position, ArmyGroupKind.Garrison);
                _groups.Add(_garrison);
                return _garrison;
            }
        }

        private readonly List<ArmyGroup> _squads = new List<ArmyGroup>();
        private readonly List<ArmyGroup> _machines = new List<ArmyGroup>();

        /// <summary>The player's squads, in the order the squad row shows them.</summary>
        public IReadOnlyList<ArmyGroup> Squads => _squads;

        /// <summary>One group per siege machine: each is selected and ordered alone (the user, 2026-11-05).</summary>
        public IReadOnlyList<ArmyGroup> Machines => _machines;

        /// <summary>economy `army_max_squads` (5).</summary>
        public static int MaxSquads => BalanceConfig.Instance != null ? BalanceConfig.Instance.ArmyMaxSquads : 5;

        /// <summary>A new empty squad standing before the Town Hall; null at the cap. Its crest is the first one no squad wears.</summary>
        public ArmyGroup CreateSquad()
        {
            if (_squads.Count >= MaxSquads) return null;
            var hall = Buildings.BuildingInstance.TownHall;
            var at = hall != null ? OpenGroundNear(hall.transform.position + Vector3.back * 3f) : transform.position;
            var squad = new ArmyGroup(SoldierType.Militia, at, ArmyGroupKind.Squad) { Crest = FreeCrest() };
            _squads.Add(squad);
            _groups.Add(squad);
            OnArmyChanged?.Invoke();
            return squad;
        }

        private int FreeCrest()
        {
            for (var crest = 0; ; crest++)
            {
                var taken = false;
                foreach (var squad in _squads) taken |= squad.Crest == crest;
                if (!taken) return crest;
            }
        }

        /// <summary>The squad is broken up: its soldiers go back to the Garrison (the user, 2026-11-05).</summary>
        public void DeleteSquad(ArmyGroup squad)
        {
            if (squad == null || !squad.IsSquad) return;
            while (squad.Count > 0) MoveSoldier(squad.Members[squad.Count - 1], Garrison);
            if (SelectedGroup == squad) SelectGroup(null);
            _squads.Remove(squad);
            _groups.Remove(squad);
            OnArmyChanged?.Invoke();
        }

        /// <summary>One soldier from his group into another (the squad window). Into the Garrison: a post round the Town Hall.</summary>
        public void MoveSoldier(SoldierUnit unit, ArmyGroup to)
        {
            if (unit == null || to == null || unit.Group == to || unit.Group == null) return;
            if (SoldierStats.IsMachine(unit.Type) || SoldierStats.IsHero(unit.Type)) return;
            var from = unit.Group;
            from.Remove(unit);
            unit.Regroup(to);
            to.Add(unit);
            if (to.IsGarrison) to.SetPost(unit, GarrisonPost());
            OnArmyChanged?.Invoke();
        }

        /// <summary>
        /// The player moving a soldier in the squad window: the move, and the squads' wanted makeup
        /// (what a refill tops up to) follows it. Null when done, else why not -- a soldier aboard a
        /// ship, or a squad away from the main island (the Garrison never leaves it, the user 2026-11-05).
        /// </summary>
        public string PlayerMove(SoldierUnit unit, ArmyGroup to)
        {
            if (unit == null || to == null || unit.Group == null || unit.Group == to) return null;
            var blocker = TransferBlocker(unit.Group, to, unit);
            if (blocker != null) return blocker;
            var from = unit.Group;
            MoveSoldier(unit, to);
            if (from.IsSquad) from.SetWanted(unit.Type, from.CountOf(unit.Type));
            if (to.IsSquad) to.SetWanted(unit.Type, to.CountOf(unit.Type));
            return null;
        }

        /// <summary>Why a soldier cannot pass between these two groups now, or null.</summary>
        public string TransferBlocker(ArmyGroup from, ArmyGroup to, SoldierUnit unit = null)
        {
            if (unit != null && !unit.gameObject.activeInHierarchy) return Localization.Get("#army_move_aboard");
            if ((from != null && from.IsSquad && !AtHome(from)) || (to != null && to.IsSquad && !AtHome(to)))
                return Localization.Get("#army_move_not_home");
            return null;
        }

        /// <summary>The squad stands on the main island (the Town Hall's), none of it aboard a ship.</summary>
        public bool AtHome(ArmyGroup squad)
        {
            if (squad == null) return false;
            var hall = Buildings.BuildingInstance.TownHall;
            var map = Maps.MeshMapApplier.Instance;
            if (hall == null || map == null) return true;
            if (squad.Count == 0) return map.SameIsland(squad.HoldPosition, hall.transform.position);
            for (var i = 0; i < squad.Count; i++)
            {
                var member = squad.Members[i];
                if (member == null) continue;
                if (!member.gameObject.activeInHierarchy || !map.SameIsland(member.transform.position, hall.transform.position)) return false;
            }
            return true;
        }

        private float _refillIn;

        /// <summary>Every two seconds: a squad with «Пополнять» on, at home, takes the soldiers it lost from the Garrison.</summary>
        private float _behaviourIn;

        private void Update()
        {
            _behaviourIn -= Time.deltaTime;
            if (_behaviourIn <= 0f)
            {
                _behaviourIn = 0.5f;
                TickBehaviours();
            }
            _refillIn -= Time.deltaTime;
            if (_refillIn > 0f) return;
            _refillIn = 2f;
            TickRefill();
        }

        // ---- squads' behaviours (the user, 2026-11-05, army-control.md stage 3) ----

        private static float Cells(float cells) => cells * (Grid.GridManager.Instance != null ? Grid.GridManager.Instance.CellSize : 1f);

        /// <summary>Twice a second: each squad with no order of the player's to finish does what its behaviour says. Public for a test.</summary>
        public void TickBehaviours()
        {
            foreach (var squad in _squads)
            {
                if (squad.Count == 0 || squad.HasPlayerAttack) continue;
                switch (squad.Behaviour)
                {
                    case SquadBehaviour.Guard: TickGuard(squad); break;
                    case SquadBehaviour.Advance: TickAdvance(squad); break;
                    case SquadBehaviour.FollowHero: TickFollow(squad); break;
                }
            }
        }

        /// <summary>«Охранять город»: a round of the Town Hall; an enemy fighter inside the town's zone is chased while it stays in it.</summary>
        private void TickGuard(ArmyGroup squad)
        {
            var hall = Buildings.BuildingInstance.TownHall;
            if (hall == null || !AtHome(squad)) return;
            var centre = hall.transform.position;
            var zone = Cells(BalanceConfig.Instance != null ? BalanceConfig.Instance.SquadGuardZoneCells : 22f);
            if (squad.AttackTarget != null)
            {
                if (!squad.AttackTarget.IsAlive || HorizontalGap(squad.AttackTarget.Transform.position, centre) > zone) squad.DropOwnAttack();
                else return;
            }
            var foe = NearestFoe(squad.Centre(), t => !t.IsStructure && HorizontalGap(t.Transform.position, centre) <= zone, false);
            if (foe != null)
            {
                squad.AttackOnItsOwn(foe);
                return;
            }
            // on to the next point of the round once the squad is at this one
            if (HorizontalGap(squad.Centre(), squad.HoldPosition) > 2.5f) return;
            var radius = Cells(BalanceConfig.Instance != null ? BalanceConfig.Instance.SquadGuardPatrolCells : 10f);
            squad.PatrolStep = (squad.PatrolStep + 1) % 6;
            var angle = squad.PatrolStep * Mathf.PI / 3f;
            var point = centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            if (UnityEngine.AI.NavMesh.SamplePosition(point, out var hit, 3f, UnityEngine.AI.NavMesh.AllAreas)) point = hit.position;
            squad.SetHold(point, point - squad.Centre());
        }

        /// <summary>«Наступать»: the nearest enemy on the squad's island, whatever the distance.</summary>
        private void TickAdvance(ArmyGroup squad)
        {
            if (squad.AttackTarget != null && squad.AttackTarget.IsAlive) return;
            var map = Maps.MeshMapApplier.Instance;
            var from = squad.Centre();
            var foe = NearestFoe(from, t => map == null || map.SameIsland(from, t.Transform.position), squad.Priority == TargetPriority.Structures);
            if (foe != null) squad.AttackOnItsOwn(foe);
        }

        /// <summary>«За героем»: the squad's rally point a little behind its hero; nothing while he is down or aboard.</summary>
        private void TickFollow(ArmyGroup squad)
        {
            var roster = HeroRoster.Instance;
            var hero = roster != null ? roster.HeroOf(squad.FollowHero) : null;
            if (hero == null || hero.IsFallen || hero.Unit == null || !hero.gameObject.activeInHierarchy) return;
            var forward = hero.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = squad.Facing;
            var spot = hero.transform.position - forward.normalized * Cells(BalanceConfig.Instance != null ? BalanceConfig.Instance.SquadFollowCells : 2f);
            if (HorizontalGap(spot, squad.HoldPosition) < 1.2f) return;
            squad.SetHold(spot, forward);
        }

        private static float HorizontalGap(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }

        /// <summary>The nearest live enemy that passes `fits`: fighters first unless `structuresFirst` (a camp, a portal not shielded).</summary>
        public static IDamageTarget NearestFoe(Vector3 from, Func<IDamageTarget, bool> fits, bool structuresFirst)
        {
            IDamageTarget best = null, other = null;
            float bestGap = float.MaxValue, otherGap = float.MaxValue;
            void Consider(IDamageTarget t)
            {
                if (t == null || !t.IsAlive || !fits(t)) return;
                if (t is OrcPortal portal && portal.IsShielded) return;
                var gap = HorizontalGap(t.Transform.position, from);
                if (t.IsStructure == structuresFirst)
                {
                    if (gap < bestGap) { bestGap = gap; best = t; }
                }
                else if (gap < otherGap) { otherGap = gap; other = t; }
            }
            foreach (var orc in OrcUnit.All) if (orc != null && orc.gameObject.activeInHierarchy) Consider(orc);
            foreach (var portal in OrcPortal.All) Consider(portal);
            foreach (var building in OrcCampBuilding.All) Consider(building);
            return best ?? other;
        }

        public void SetBehaviour(ArmyGroup squad, SquadBehaviour behaviour, SoldierType hero = SoldierType.King)
        {
            if (squad == null || !squad.IsSquad) return;
            if (behaviour == SquadBehaviour.FollowHero) squad.FollowHero = hero;
            squad.SetBehaviour(behaviour);
            OnArmyChanged?.Invoke();
        }

        public void SetFormation(ArmyGroup squad, SquadFormation formation)
        {
            if (squad == null || !squad.IsSquad) return;
            squad.SetFormation(formation);
            OnArmyChanged?.Invoke();
        }

        /// <summary>The refill pass (public for a test).</summary>
        public void TickRefill()
        {
            if (_garrison == null || _garrison.Count == 0) return;
            foreach (var squad in _squads)
            {
                if (!squad.AutoRefill || !AtHome(squad)) continue;
                for (var g = _garrison.Count - 1; g >= 0; g--)
                {
                    var soldier = _garrison.Members[g];
                    if (soldier == null || squad.CountOf(soldier.Type) >= squad.WantedOf(soldier.Type)) continue;
                    MoveSoldier(soldier, squad);
                }
            }
        }

        /// <summary>A squad renamed in the window; empty brings «Отряд N» back.</summary>
        public void RenameSquad(ArmyGroup squad, string name)
        {
            if (squad == null || !squad.IsSquad) return;
            squad.Name = (name ?? string.Empty).Trim();
            if (squad.Name.Length > 24) squad.Name = squad.Name.Substring(0, 24);
            OnArmyChanged?.Invoke();
        }

        /// <summary>The next crest no other squad wears.</summary>
        public void NextCrest(ArmyGroup squad, int crestCount)
        {
            if (squad == null || !squad.IsSquad || crestCount <= 0) return;
            for (var step = 1; step <= crestCount; step++)
            {
                var crest = (squad.Crest + step) % crestCount;
                var taken = false;
                foreach (var other in _squads) taken |= other != squad && other.Crest == crest;
                if (taken) continue;
                squad.Crest = crest;
                break;
            }
            OnArmyChanged?.Invoke();
        }

        public void SetAutoRefill(ArmyGroup squad, bool on)
        {
            if (squad == null || !squad.IsSquad) return;
            squad.AutoRefill = on;
            OnArmyChanged?.Invoke();
        }

        /// <summary>A free spot round the Town Hall for a soldier coming back to the Garrison.</summary>
        private Vector3 GarrisonPost()
        {
            var hall = Buildings.BuildingInstance.TownHall;
            var centre = hall != null ? hall.transform.position : Garrison.HoldPosition;
            return OpenGroundNear(centre + ArmyGroup.FormationOffset(7 + Garrison.Count) * 2.2f);
        }

        /// <summary>A loaded squad, as it was saved.</summary>
        public ArmyGroup RestoreSquad(Vector3 holdPosition, string squadName, int crest)
        {
            var squad = new ArmyGroup(SoldierType.Militia, holdPosition, ArmyGroupKind.Squad) { Name = squadName ?? string.Empty, Crest = crest };
            _squads.Add(squad);
            _groups.Add(squad);
            return squad;
        }

        /// <summary>A machine's own group (hired or loaded).</summary>
        public ArmyGroup RestoreMachine(SoldierType type, Vector3 holdPosition)
        {
            var group = new ArmyGroup(type, holdPosition, ArmyGroupKind.Machine);
            _machines.Add(group);
            _groups.Add(group);
            return group;
        }

        /// <summary>«Гарнизон», or «Отряд N» unless renamed.</summary>
        public string SquadName(ArmyGroup squad)
        {
            if (squad == null) return string.Empty;
            if (squad.IsGarrison) return Localization.Get("#army_garrison");
            if (!string.IsNullOrEmpty(squad.Name)) return squad.Name;
            return Localization.Format("#army_squad_name", _squads.IndexOf(squad) + 1);
        }

        public int SoldierCount
        {
            get
            {
                var total = 0;
                foreach (var group in _groups)
                {
                    if (SoldierStats.IsHero(group.Type) || group.IsSummoned) continue;   // heroes and the Queen's summons are no soldiers the army cap counts
                    // a knight takes two places (the user, 2026-11-05); per member, a squad being mixed
                    var members = group.Members;
                    for (var i = 0; i < members.Count; i++)
                        if (members[i] != null) total += SoldierStats.ArmySlots(members[i].Type);
                }
                return total;
            }
        }

        /// <summary>
        /// Coins this army costs per game day: NOTHING since 2026-11-05 (the user: «один раз нанял и
        /// больше не тратишься», both modes). Kept as the one place to switch it back on.
        /// </summary>
        public static readonly bool UpkeepCharged = false;

        /// <summary>Coins this army costs per game day at its current size (0 while UpkeepCharged is off).</summary>
        public int DailyUpkeep
        {
            get
            {
                if (!UpkeepCharged) return 0;
                var total = 0;
                foreach (var group in _groups)
                {
                    // a hero is bought once and never paid by the day; nor are the Queen's summons
                    if (SoldierStats.IsHero(group.Type) || group.IsSummoned) continue;
                    total += group.Count * SoldierStats.UpkeepCoinsPerDay(group.Type);
                }
                return total;
            }
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

        private void OnEnable()
        {
            var calendar = gameCalendar != null ? gameCalendar : GameCalendar.Instance;
            if (calendar != null) calendar.OnDayPassed += ChargeDailyUpkeep;
        }

        private void OnDisable()
        {
            var calendar = gameCalendar != null ? gameCalendar : GameCalendar.Instance;
            if (calendar != null) calendar.OnDayPassed -= ChargeDailyUpkeep;
        }

        /// <summary>
        /// Why recruitment would be refused right now, or null when it's possible. Returned as a
        /// message rather than a bool so the Barracks panel can tell the player which of the three
        /// limits they've hit instead of just greying a button out.
        /// </summary>
        public string DescribeRecruitBlocker(SoldierType type, int barracksLevel = int.MaxValue)
        {
            // First, because it is the only one of the four the player cannot fix in the next
            // minute: the others say "wait" and this one says "go and research it".
            if (!SoldierStats.IsUnlocked(type)) return Localization.Get("#army_not_researched");

            // then the hiring building's own level: each Barracks level opens the next tier (the user,
            // 2026-10-28); the Stable and the Church say it in their own words
            var need = SoldierStats.BarracksLevel(type);
            var hall = SoldierStats.HiredAt(type);
            if (barracksLevel < need)
                return hall == BuildingIds.Barracks
                    ? Localization.Format("#army_need_barracks", SoldierStats.DisplayName(type), need)
                    : Localization.Format("#army_need_building", SoldierStats.DisplayName(type), Localization.Get("#building_" + hall.ToLowerInvariant()), need);

            if (SoldierCount + SoldierStats.ArmySlots(type) > SoldierStats.MaxArmySize) return Localization.Format("#army_full", SoldierStats.MaxArmySize);

            // a type of its own cap per level of its building: the knights 4, the monks 2 a level (the user, 2026-11-05)
            var cap = SoldierStats.CapPerLevel(type);
            if (cap > 0 && barracksLevel != int.MaxValue && CountOf(type) >= cap * barracksLevel)
                return Localization.Format("#army_type_cap", SoldierStats.DisplayName(type), cap * barracksLevel);

            var citizens = CitizenManager.Instance;
            if (citizens == null || citizens.IdlePopulation < SoldierStats.Crew(type)) return Localization.Get("#army_no_citizens");

            var resources = ResourceManager.Instance;
            if (resources != null && !resources.HasEnough(SoldierStats.RecruitCost(type))) return Localization.Get("#army_no_coins");

            return null;
        }

        /// <summary>Recruits one soldier at the given position (the Barracks that ordered it). False if any of the three limits in DescribeRecruitBlocker applies.</summary>
        public bool TryRecruit(SoldierType type, Vector3 position, int barracksLevel = int.MaxValue)
        {
            if (DescribeRecruitBlocker(type, barracksLevel) != null) return false;

            var resources = ResourceManager.Instance;
            if (resources != null && !resources.TrySpend(SoldierStats.RecruitCost(type))) return false;

            // Checked in DescribeRecruitBlocker a moment ago, so this can only fail if something
            // else took the last idle citizen in between -- impossible on one thread, but refunding
            // rather than silently pocketing the coins is the honest way to handle it anyway.
            // a machine takes its whole crew (2 citizens), a soldier one
            var crew = SoldierStats.Crew(type);
            var taken = 0;
            while (taken < crew && CitizenManager.Instance.TryTakeIdleCitizen()) taken++;
            if (taken < crew)
            {
                for (var i = 0; i < taken; i++) CitizenManager.Instance.ReturnCitizen();
                // Nothing to refund when the cheat is on -- TrySpend reported success without
                // deducting anything, and paying it back would mint coins out of nowhere.
                if (resources != null && !resources.InfiniteResources) RefundCost(resources, SoldierStats.RecruitCost(type));
                return false;
            }

            var group = RecruitGroup(type, position);
            var unit = SpawnSoldier(type, position, group);
            group.Add(unit);
            // a garrison soldier keeps the spot he stepped out at (army-control.md)
            if (group.IsGarrison) group.SetPost(unit, unit.transform.position);

            EventLogManager.Instance?.Log(Localization.Format("#log_recruited", SoldierStats.DisplayName(type), SoldierCount, SoldierStats.MaxArmySize));
            OnArmyChanged?.Invoke();
            return true;
        }

        /// <summary>Sends a soldier home: the unit disappears and the settlement gets its citizen back. Used by the panel's disband action and by unpayable upkeep.</summary>
        public void Disband(SoldierUnit unit)
        {
            if (unit == null) return;

            RemoveFromGroup(unit);
            for (var i = 0; i < SoldierStats.Crew(unit.Type); i++) CitizenManager.Instance?.ReturnCitizen();
            Destroy(unit.gameObject);
            OnArmyChanged?.Invoke();
        }

        /// <summary>Killed in battle -- same removal as Disband, but the citizen is gone for good (see the class summary).</summary>
        public void NotifySoldierKilled(SoldierUnit unit)
        {
            if (unit == null) return;

            // A hero does not die: he falls, and gets up again at the castle (Hero).
            var hero = unit.GetComponent<Hero>();
            if (hero != null)
            {
                hero.Fall();
                return;
            }

            RemoveFromGroup(unit);
            // the figure falls and sinks where he stood, as an orc's does
            OrcCorpse.Leave(unit.transform, unit.HealthBar);
            Destroy(unit.gameObject);
            // Where they fell, which is the fight the player may want to look at.
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_soldier_died"), EventSeverity.Danger, EventGlyph.Skull, "Barracks", unit.transform.position));
            OnArmyChanged?.Invoke();
        }

        /// <summary>
        /// Went down with a ship (ShipManager.Sink): a soldier is lost with no body to leave; a hero
        /// falls -- no last-moment escape at sea -- and rises at the Town Hall as from any fall.
        /// </summary>
        public void NotifyDrowned(SoldierUnit unit, Vector3 at)
        {
            if (unit == null) return;
            unit.transform.position = at;
            var hero = unit.GetComponent<Hero>();
            if (hero != null)
            {
                unit.gameObject.SetActive(true);
                hero.Fall(true);
                return;
            }
            RemoveFromGroup(unit);
            Destroy(unit.gameObject);
            OnArmyChanged?.Invoke();
        }

        /// <summary>
        /// Puts a saved group back, with the rally point and target priority the player had given
        /// it. Empty of members until RestoreSoldier fills it -- the two are separate so the group
        /// exists before its first soldier asks for a formation slot in it.
        /// </summary>
        public ArmyGroup RestoreGroup(SoldierType type, Vector3 holdPosition, TargetPriority priority)
        {
            var group = GetOrCreateGroup(type, holdPosition);
            group.OrderMoveTo(holdPosition);
            group.SetPriority(priority);
            return group;
        }

        /// <summary>
        /// Puts one saved soldier back where it stood, on the health it had.
        ///
        /// Deliberately NOT TryRecruit: it takes no citizen and no coins. The citizen was taken when
        /// this soldier was first recruited, and the saved population already has them subtracted --
        /// charging again on load would quietly eat the town one reload at a time.
        /// </summary>
        public SoldierUnit RestoreSoldier(ArmyGroup group, Vector3 position, int currentHealth, SoldierType? type = null)
        {
            if (group == null) return null;

            var unit = SpawnSoldier(type ?? group.Type, position, group, scatter: false);
            group.Add(unit);
            unit.SetCurrentHealth(currentHealth);
            OnArmyChanged?.Invoke();
            return unit;
        }

        /// <summary>
        /// Daily pay, charged once per game day. When the treasury can't cover the whole army,
        /// soldiers are disbanded one at a time (each one walking home as a citizen) until what's
        /// left is affordable -- per the design, an unpaid soldier simply leaves, with no further
        /// penalty.
        ///
        /// Affordability is asked of ResourceManager.HasEnough rather than compared against
        /// GetAmount by hand. That distinction is not pedantry: HasEnough is where the infinite-
        /// resources cheat lives, and reading the raw coin count instead disbanded the player's
        /// whole army on day 2 while the cheat was on and every other cost in the game was free.
        ///
        /// Public so a test can charge a day directly instead of waiting two real minutes for the
        /// calendar to tick.
        /// </summary>
        public void ChargeDailyUpkeep()
        {
            // soldiers are hired once and never paid by the day: a run's since 2026-10-21, the
            // sandbox's since 2026-11-05 (the user)
            if (!UpkeepCharged || Invasion.InvasionMode.IsActive) return;
            var resources = ResourceManager.Instance;
            if (resources == null) return;

            var disbanded = 0;
            while (DailyUpkeep > 0 && !resources.HasEnough(UpkeepCost(DailyUpkeep)) && TryDisbandCheapest())
            {
                disbanded++;
            }

            if (disbanded > 0)
            {
                EventLogManager.Instance?.Post(GameEvent.ForSettlement(Localization.Format("#log_disbanded", disbanded), EventSeverity.Warning, EventGlyph.Alert, "Res_Coins", "Barracks"));
            }

            var upkeep = DailyUpkeep;
            if (upkeep <= 0) return;

            resources.TrySpend(UpkeepCost(upkeep));
        }

        // Rebuilt in place rather than allocated per call: this runs every game day, and every
        // affordability probe inside the disband loop above needs one too.
        private readonly List<ResourceAmount> _upkeepCost = new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Coins, amount = 0 } };

        private List<ResourceAmount> UpkeepCost(int coins)
        {
            // ResourceAmount is a class, so the existing element is updated in place rather than
            // replaced -- otherwise the "reused list" would allocate an object per call anyway.
            _upkeepCost[0].amount = coins;
            return _upkeepCost;
        }

        private bool TryDisbandCheapest()
        {
            for (var i = _groups.Count - 1; i >= 0; i--)
            {
                var group = _groups[i];
                if (group.Count == 0 || SoldierStats.IsHero(group.Type) || group.IsSummoned) continue;

                Disband(group.Members[group.Count - 1]);
                return true;
            }
            return false;
        }

        private void RemoveFromGroup(SoldierUnit unit)
        {
            var group = unit.Group;
            if (group == null) return;

            group.Remove(unit);
            // a machine's group is the machine: gone with it
            if (group.IsMachine && group.Count == 0)
            {
                _machines.Remove(group);
                _groups.Remove(group);
                if (SelectedGroup == group) SelectGroup(null);
            }
            // Empty groups are kept, not dropped: the group carries the player's chosen target
            // priority and rally point, and losing those because the last member died would be a
            // nasty surprise the next time they recruit.
        }

        /// <summary>Where a recruit goes: a machine into its own group, a soldier into the Garrison; in a run, his type's group as before.</summary>
        private ArmyGroup RecruitGroup(SoldierType type, Vector3 position)
        {
            if (!UsesSquads) return GetOrCreateGroup(type, position);
            if (SoldierStats.IsMachine(type)) return RestoreMachine(type, position);
            // the tutorial's one soldier is tapped and sent aboard (its hints): he joins a squad, not the
            // Garrison, which takes no orders
            if (Tutorial.TutorialMode.IsActive) return _squads.Count > 0 ? _squads[0] : CreateSquad() ?? Garrison;
            return Garrison;
        }

        private ArmyGroup GetOrCreateGroup(SoldierType type, Vector3 holdPosition)
        {
            foreach (var group in _groups)
            {
                // a type group only: the Garrison and the squads carry Militia as a stand-in type
                if (group.Kind == ArmyGroupKind.Type && !group.IsSummoned && group.Type == type) return group;
            }

            var created = new ArmyGroup(type, holdPosition);
            _groups.Add(created);
            return created;
        }

        private static void RefundCost(ResourceManager resources, List<ResourceAmount> cost)
        {
            foreach (var amount in cost)
            {
                resources.Add(amount.type, amount.amount);
            }
        }

        /// <summary>
        /// An Invasion hero (the King, the Queen, the Captain): one soldier in a group of his own,
        /// which is what lets the orders and the orcs treat him like any other fighter, plus the Hero
        /// that gives him levels and his coming back, and his skills. Takes no citizen.
        /// </summary>
        public Hero SpawnHero(SoldierType type, Vector3 position)
        {
            var group = GetOrCreateGroup(type, position);
            var unit = SpawnSoldier(type, position, group, scatter: false);
            group.Add(unit);
            var hero = unit.gameObject.AddComponent<Hero>();
            hero.Initialize(position, type);
            unit.gameObject.AddComponent<HeroSkills>().Bind(hero, SoldierStats.SheetIdOf(type));
            OnArmyChanged?.Invoke();
            return hero;
        }

        public Hero SpawnKing(Vector3 position) => SpawnHero(SoldierType.King, position);

        /// <summary>
        /// A dungeon hireling (Dungeon.DungeonHirelings) into the party's own group, which is not
        /// one of the army's: no citizen, no upkeep, no icon, not in the army's count.
        /// </summary>
        public SoldierUnit SpawnHireling(SoldierType type, Vector3 position, ArmyGroup party)
        {
            var unit = SpawnSoldier(type, position, party, scatter: false);
            party.Add(unit);
            return unit;
        }

        /// <summary>
        /// The Queen's Призыв (the user, 2026-10-23): `count` soldiers of `type` at `level` --
        /// whatever the Laboratory says -- in a group of their own holding where she stands; they
        /// fight there for `seconds` and are gone. No citizen, no upkeep, not in the army's count.
        /// </summary>
        public void SpawnSummons(SoldierType type, Vector3 position, int count, int level, float seconds)
        {
            var group = new ArmyGroup(type, position) { IsSummoned = true };
            var stats = SoldierStats.StatsAt(type, level);
            for (var i = 0; i < count; i++)
            {
                var unit = SpawnSoldier(type, position, group, lookLevel: level);
                group.Add(unit);
                unit.SetHeroStats(stats.maxHealth, stats.attackDamage);
                unit.SetCurrentHealth(stats.maxHealth);
                unit.gameObject.AddComponent<SummonedLifetime>().Begin(seconds);
            }
            group.OrderMoveTo(position);
        }

        /// <summary>A summoned soldier's time is up: gone, as a soldier killed is, without the log line.</summary>
        public void Dismiss(SoldierUnit unit)
        {
            if (unit == null) return;
            RemoveFromGroup(unit);
            Destroy(unit.gameObject);
        }

        /// <summary>
        /// The nearest point to `origin` a soldier can walk away from: on the NavMesh (not inside
        /// the barracks' own carve), and with a way to the castle (the Town Hall) when there is one -- a recruit put into the
        /// gap between the barracks and the house beside it stood there for good (the user,
        /// 2026-10-23). Rings of 12 points a metre apart, out to 6 m; `origin` itself if none works.
        /// </summary>
        private static Vector3 OpenGroundNear(Vector3 origin)
        {
            var castle = Buildings.BuildingInstance.TownHall != null
                ? Buildings.BuildingInstance.TownHall.transform.position
                : (Vector3?)null;
            var goal = Vector3.zero;
            if (castle.HasValue)
            {
                if (UnityEngine.AI.NavMesh.SamplePosition(castle.Value, out var castleHit, 5f, UnityEngine.AI.NavMesh.AllAreas)) goal = castleHit.position;
                else castle = null;
            }

            _spawnPath ??= new UnityEngine.AI.NavMeshPath();
            for (var ring = 0; ring <= 6; ring++)
            {
                var points = ring == 0 ? 1 : 12;
                for (var i = 0; i < points; i++)
                {
                    var angle = i * Mathf.PI * 2f / points;
                    var candidate = origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ring;
                    if (!UnityEngine.AI.NavMesh.SamplePosition(candidate, out var hit, 0.6f, UnityEngine.AI.NavMesh.AllAreas)) continue;
                    if (!castle.HasValue) return hit.position;
                    if (UnityEngine.AI.NavMesh.CalculatePath(hit.position, goal, UnityEngine.AI.NavMesh.AllAreas, _spawnPath)
                        && _spawnPath.status == UnityEngine.AI.NavMeshPathStatus.PathComplete) return hit.position;
                }
            }
            return origin;
        }

        private static UnityEngine.AI.NavMeshPath _spawnPath;

        /// <summary>Builds the unit. `scatter` is what separates a fresh recruit walking out of the barracks from a loaded one, who has to land exactly where the save says.</summary>
        private SoldierUnit SpawnSoldier(SoldierType type, Vector3 origin, ArmyGroup group, bool scatter = true, int lookLevel = 0)
        {
            var jitter = scatter ? UnityEngine.Random.insideUnitCircle * SpawnJitterRadius : Vector2.zero;
            var spawnPosition = origin + new Vector3(jitter.x, 0f, jitter.y);
            if (scatter) spawnPosition = OpenGroundNear(spawnPosition);

            var root = new GameObject($"Soldier ({SoldierStats.DisplayName(type)})");
            root.transform.SetParent(transform, false);
            root.transform.position = spawnPosition;
            root.transform.localScale = Vector3.one * UnitSize.Scale;

            if (lookLevel <= 0) lookLevel = SoldierStats.CurrentLevel(type);
            BuildAppearance(root.transform, type, lookLevel);

            var controller = root.AddComponent<CharacterController>();
            controller.height = 0.72f;
            controller.radius = 0.15f;
            controller.center = new Vector3(0f, 0.36f, 0f);
            controller.skinWidth = 0.02f;
            controller.minMoveDistance = 0f;

            var unit = root.AddComponent<SoldierUnit>();
            unit.LookLevel = lookLevel;
            unit.Initialize(type, group);
            return unit;
        }

        /// <summary>
        /// The tier's silhouette. Every one of them starts from the same cube body and head
        /// CitizenVisualsManager gives an ordinary citizen -- these are townsfolk who picked
        /// something up, not a separate species -- and then differs in colour and in what it is
        /// holding.
        /// </summary>
        private void BuildAppearance(Transform root, SoldierType type, int level)
        {
            var soldierModel = SoldierModel(type, level);
            if (soldierModel != null && SoldierStats.IsMachine(type))
            {
                // a machine is modelled at its real size, its moving parts animated by SiegeMachine
                var machine = AddFigure(root, soldierModel, type, false);
                root.gameObject.AddComponent<SiegeMachine>().Bind(machine.transform, type);
                return;
            }
            if (soldierModel != null)
            {
                AddFigure(root, soldierModel, type);
                return;
            }
            if (SoldierStats.IsHireling(type))
            {
                var index = type - SoldierType.GnomeMiner;
                var gnome = hirelingModels != null && index < hirelingModels.Length ? hirelingModels[index] : null;
                if (gnome != null)
                {
                    AddFigure(root, gnome, type);
                    return;
                }
            }
            switch (type)
            {
                case SoldierType.King when kingModel != null:
                case SoldierType.Queen when queenModel != null:
                case SoldierType.Captain when captainModel != null:
                {
                    // Authored at its real size (0.79 m to the King's crown's tip), so the root's
                    // unit scale is divided back out, as the orc figures do.
                    AddFigure(root, type == SoldierType.Queen ? queenModel : type == SoldierType.Captain ? captainModel : kingModel, type);
                    break;
                }

                case SoldierType.Queen:
                case SoldierType.Captain:
                case SoldierType.King:
                    // (no model in the project: cubes) A head taller and broader than any soldier, in royal purple with a red cape,
                    // a gold crown and a long sword: the one figure on the map the player looks for.
                    AddCubePart(root, "Body", new Vector3(0f, 0.3f, 0f), new Vector3(0.38f, 0.6f, 0.38f), MaterialFor(RoyalColor));
                    AddCubePart(root, "Cape", new Vector3(0f, 0.32f, -0.2f), new Vector3(0.36f, 0.56f, 0.04f), MaterialFor(CapeColor));
                    AddCubePart(root, "Head", new Vector3(0f, 0.73f, 0f), new Vector3(0.26f, 0.26f, 0.26f), MaterialFor(SkinColor));
                    AddCubePart(root, "Crown", new Vector3(0f, 0.9f, 0f), new Vector3(0.28f, 0.1f, 0.28f), MaterialFor(GoldColor));
                    AddCubePart(root, "SwordBlade", new Vector3(0.27f, 0.62f, 0f), new Vector3(0.06f, 0.78f, 0.03f), MaterialFor(SteelColor));
                    AddCubePart(root, "SwordGuard", new Vector3(0.27f, 0.25f, 0f), new Vector3(0.22f, 0.05f, 0.05f), MaterialFor(GoldColor));
                    break;

                case SoldierType.Spearman:
                    AddBodyAndHead(root, SpearmanClothingColor);
                    // Held upright and taller than he is: the two-metre reach in the sheet is this
                    // tier's whole point, and it should be visible before the first fight.
                    AddCubePart(root, "SpearShaft", new Vector3(0.19f, 0.6f, 0f), new Vector3(0.045f, 1.2f, 0.045f), MaterialFor(WoodColor));
                    AddCubePart(root, "SpearTip", new Vector3(0.19f, 1.24f, 0f), new Vector3(0.07f, 0.18f, 0.07f), MaterialFor(SteelColor));
                    break;

                case SoldierType.Archer:
                    AddBodyAndHead(root, ArcherClothingColor);
                    // Out in FRONT rather than over the shoulder, so the one tier that fights at
                    // range is the one whose weapon points where it is looking.
                    AddCubePart(root, "Bow", new Vector3(0f, 0.45f, 0.2f), new Vector3(0.04f, 0.62f, 0.04f), MaterialFor(WoodColor));
                    AddCubePart(root, "BowString", new Vector3(0f, 0.45f, 0.14f), new Vector3(0.02f, 0.5f, 0.02f), MaterialFor(SteelColor));
                    break;

                case SoldierType.ManAtArms:
                    // Wider and a head taller than the rest of the line, before a single stat is
                    // read: forty-five health against a militiaman's twelve has to look like
                    // something.
                    AddCubePart(root, "Body", new Vector3(0f, 0.27f, 0f), new Vector3(0.36f, 0.54f, 0.36f), MaterialFor(ArmourColor));
                    AddCubePart(root, "Tabard", new Vector3(0f, 0.3f, 0.19f), new Vector3(0.16f, 0.34f, 0.02f), MaterialFor(TabardColor));
                    AddCubePart(root, "Helmet", new Vector3(0f, 0.66f, 0f), new Vector3(0.25f, 0.25f, 0.25f), MaterialFor(ArmourColor));
                    AddCubePart(root, "SwordBlade", new Vector3(0.23f, 0.6f, 0f), new Vector3(0.06f, 0.7f, 0.03f), MaterialFor(SteelColor));
                    AddCubePart(root, "SwordGuard", new Vector3(0.23f, 0.26f, 0f), new Vector3(0.2f, 0.05f, 0.05f), MaterialFor(SteelColor));
                    break;

                case SoldierType.Militia:
                default:
                    AddBodyAndHead(root, MilitiaClothingColor);
                    AddCubePart(root, "PitchforkShaft", new Vector3(0.19f, 0.45f, 0f), new Vector3(0.05f, 0.9f, 0.05f), MaterialFor(SteelColor));
                    AddCubePart(root, "PitchforkHead", new Vector3(0.19f, 0.92f, 0f), new Vector3(0.18f, 0.06f, 0.05f), MaterialFor(SteelColor));
                    break;
            }
        }

        /// <summary>The figure a soldier of `type` wears at `level`, or the nearest level below that has one; null for cubes.</summary>
        private GameObject SoldierModel(SoldierType type, int level)
        {
            if (SoldierStats.IsHero(type) || SoldierStats.IsHireling(type) || soldierModels == null) return null;
            for (var l = Mathf.Clamp(level, 1, SoldierLooks); l >= 1; l--)
            {
                var index = (int)type * SoldierLooks + l - 1;
                if (index < soldierModels.Length && soldierModels[index] != null) return soldierModels[index];
            }
            return null;
        }

        /// <summary>
        /// Puts a living soldier in its type's look for `level` (a level researched while it stands
        /// on the map: SoldierUnit.RefreshStats). The figure is swapped, the animator kept.
        /// </summary>
        public void Redress(SoldierUnit unit, int level)
        {
            if (unit == null || unit.LookLevel == level) return;
            var model = SoldierModel(unit.Type, level);
            var animator = unit.GetComponent<FigureAnimator>();
            var old = unit.transform.Find(unit.Type.ToString());
            unit.LookLevel = level;
            if (model == null || animator == null || old == null) return;
            Destroy(old.gameObject);
            old.name = "Undressed";
            animator.Bind(AddFigure(unit.transform, model, unit.Type, animate: false).transform);
        }

        /// <summary>A jointed figure (a hero's or a soldier's) under the unit's root: it stands, runs, strikes and, on death, falls (OrcCorpse).</summary>
        private static GameObject AddFigure(Transform root, GameObject model, SoldierType type, bool animate = true)
        {
            var figure = Instantiate(model, root, false);
            figure.name = type.ToString();
            figure.transform.localScale = Vector3.one / UnitSize.Scale;
            // modelled facing Blender's -y, which lands facing back along the root's
            // forward: turned round, or he walks backwards
            figure.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * figure.transform.localRotation;
            SceneryShadows.DisableCasting(figure);
            Maps.SeasonStyle.NoSnow(figure);
            if (animate) root.gameObject.AddComponent<FigureAnimator>().Bind(figure.transform);
            return figure;
        }

        private void AddBodyAndHead(Transform root, Color clothing)
        {
            AddCubePart(root, "Body", new Vector3(0f, 0.25f, 0f), new Vector3(0.3f, 0.5f, 0.3f), MaterialFor(clothing));
            AddCubePart(root, "Head", new Vector3(0f, 0.61f, 0f), new Vector3(0.22f, 0.22f, 0.22f), MaterialFor(SkinColor));
        }

        private static void AddCubePart(Transform parent, string partName, Vector3 localPosition, Vector3 size, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = partName;
            Destroy(part.GetComponent<BoxCollider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        /// <summary>One shared material per colour, built the first time a soldier wants it.</summary>
        private Material MaterialFor(Color color)
        {
            if (_materials.TryGetValue(color, out var material) && material != null) return material;

            material = new Material(RuntimeShaders.Lit) { color = color };
            _materials[color] = material;
            return material;
        }
    }
}
