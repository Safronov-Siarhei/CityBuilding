using System;
using System.Collections.Generic;
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

        /// <summary>
        /// The soldiers' figures (Tools/blender/soldiers.py, Models/Soldiers), one per type and
        /// researched level: index type * SoldierLooks + level - 1 (metal at level 2, gold at 3).
        /// A missing level wears the one below; a type with none is drawn in cubes.
        /// </summary>
        [SerializeField] private GameObject[] soldierModels = new GameObject[4 * SoldierLooks];

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
            SelectedGroup = group;
            OnSelectionChanged?.Invoke();
        }

        /// <summary>Tapping the selected group's own icon again puts the player back in normal (build/inspect) mode -- the only way out of command mode on touch, where there's no Escape key.</summary>
        public void ToggleSelection(ArmyGroup group)
        {
            SelectGroup(SelectedGroup == group ? null : group);
        }

        public int SoldierCount
        {
            get
            {
                var total = 0;
                foreach (var group in _groups)
                {
                    if (SoldierStats.IsHero(group.Type) || group.IsSummoned) continue;   // heroes and the Queen's summons are no soldiers the army cap counts
                    total += group.Count;
                }
                return total;
            }
        }

        /// <summary>Coins this army costs per game day at its current size.</summary>
        public int DailyUpkeep
        {
            get
            {
                var total = 0;
                foreach (var group in _groups)
                {
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
        public string DescribeRecruitBlocker(SoldierType type)
        {
            // First, because it is the only one of the four the player cannot fix in the next
            // minute: the others say "wait" and this one says "go and research it".
            if (!SoldierStats.IsUnlocked(type)) return Localization.Get("#army_not_researched");

            if (SoldierCount >= SoldierStats.MaxArmySize) return Localization.Format("#army_full", SoldierStats.MaxArmySize);

            var citizens = CitizenManager.Instance;
            if (citizens == null || citizens.IdlePopulation <= 0) return Localization.Get("#army_no_citizens");

            var resources = ResourceManager.Instance;
            if (resources != null && !resources.HasEnough(SoldierStats.RecruitCost(type))) return Localization.Get("#army_no_coins");

            return null;
        }

        /// <summary>Recruits one soldier at the given position (the Barracks that ordered it). False if any of the three limits in DescribeRecruitBlocker applies.</summary>
        public bool TryRecruit(SoldierType type, Vector3 position)
        {
            if (DescribeRecruitBlocker(type) != null) return false;

            var resources = ResourceManager.Instance;
            if (resources != null && !resources.TrySpend(SoldierStats.RecruitCost(type))) return false;

            // Checked in DescribeRecruitBlocker a moment ago, so this can only fail if something
            // else took the last idle citizen in between -- impossible on one thread, but refunding
            // rather than silently pocketing the coins is the honest way to handle it anyway.
            if (!CitizenManager.Instance.TryTakeIdleCitizen())
            {
                // Nothing to refund when the cheat is on -- TrySpend reported success without
                // deducting anything, and paying it back would mint coins out of nowhere.
                if (resources != null && !resources.InfiniteResources) RefundCost(resources, SoldierStats.RecruitCost(type));
                return false;
            }

            var group = GetOrCreateGroup(type, position);
            var unit = SpawnSoldier(type, position, group);
            group.Add(unit);

            EventLogManager.Instance?.Log(Localization.Format("#log_recruited", SoldierStats.DisplayName(type), SoldierCount, SoldierStats.MaxArmySize));
            OnArmyChanged?.Invoke();
            return true;
        }

        /// <summary>Sends a soldier home: the unit disappears and the settlement gets its citizen back. Used by the panel's disband action and by unpayable upkeep.</summary>
        public void Disband(SoldierUnit unit)
        {
            if (unit == null) return;

            RemoveFromGroup(unit);
            CitizenManager.Instance?.ReturnCitizen();
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
        public SoldierUnit RestoreSoldier(ArmyGroup group, Vector3 position, int currentHealth)
        {
            if (group == null) return null;

            var unit = SpawnSoldier(group.Type, position, group, scatter: false);
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
            // a run's soldiers are hired once and never paid by the day (the user, 2026-10-21)
            if (Invasion.InvasionMode.IsActive) return;
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
            // Empty groups are kept, not dropped: the group carries the player's chosen target
            // priority and rally point, and losing those because the last member died would be a
            // nasty surprise the next time they recruit.
        }

        private ArmyGroup GetOrCreateGroup(SoldierType type, Vector3 holdPosition)
        {
            foreach (var group in _groups)
            {
                if (group.Type == type) return group;
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
        /// the barracks' own carve), and in a run with a way to the castle -- a recruit put into the
        /// gap between the barracks and the house beside it stood there for good (the user,
        /// 2026-10-23). Rings of 12 points a metre apart, out to 6 m; `origin` itself if none works.
        /// </summary>
        private static Vector3 OpenGroundNear(Vector3 origin)
        {
            var castle = Invasion.InvasionMode.IsActive && Invasion.InvasionMode.Instance.Castle != null
                ? Invasion.InvasionMode.Instance.Castle.transform.position
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
            if (soldierModel != null)
            {
                AddFigure(root, soldierModel, type);
                return;
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
            if (SoldierStats.IsHero(type) || soldierModels == null) return null;
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
