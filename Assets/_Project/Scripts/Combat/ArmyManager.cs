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

            RemoveFromGroup(unit);
            Destroy(unit.gameObject);
            EventLogManager.Instance?.Log(Localization.Get("#log_soldier_died"));
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
            var resources = ResourceManager.Instance;
            if (resources == null) return;

            var disbanded = 0;
            while (DailyUpkeep > 0 && !resources.HasEnough(UpkeepCost(DailyUpkeep)) && TryDisbandCheapest())
            {
                disbanded++;
            }

            if (disbanded > 0)
            {
                EventLogManager.Instance?.Log(Localization.Format("#log_disbanded", disbanded));
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
                if (group.Count == 0) continue;

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

        /// <summary>Builds the unit. `scatter` is what separates a fresh recruit walking out of the barracks from a loaded one, who has to land exactly where the save says.</summary>
        private SoldierUnit SpawnSoldier(SoldierType type, Vector3 origin, ArmyGroup group, bool scatter = true)
        {
            var jitter = scatter ? UnityEngine.Random.insideUnitCircle * SpawnJitterRadius : Vector2.zero;
            var spawnPosition = origin + new Vector3(jitter.x, 0f, jitter.y);

            var root = new GameObject($"Soldier ({SoldierStats.DisplayName(type)})");
            root.transform.SetParent(transform, false);
            root.transform.position = spawnPosition;

            BuildAppearance(root.transform, type);

            var controller = root.AddComponent<CharacterController>();
            controller.height = 0.72f;
            controller.radius = 0.15f;
            controller.center = new Vector3(0f, 0.36f, 0f);
            controller.skinWidth = 0.02f;
            controller.minMoveDistance = 0f;

            var unit = root.AddComponent<SoldierUnit>();
            unit.Initialize(type, group);
            return unit;
        }

        /// <summary>
        /// The tier's silhouette. Every one of them starts from the same cube body and head
        /// CitizenVisualsManager gives an ordinary citizen -- these are townsfolk who picked
        /// something up, not a separate species -- and then differs in colour and in what it is
        /// holding.
        /// </summary>
        private void BuildAppearance(Transform root, SoldierType type)
        {
            switch (type)
            {
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
