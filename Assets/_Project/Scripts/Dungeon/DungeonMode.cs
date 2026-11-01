using System.Collections;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.CameraControl;
using CityBuilder.Combat;
using CityBuilder.Maps;
using CityBuilder.Saving;
using UnityEngine;

namespace CityBuilder.Dungeon
{
    /// <summary>
    /// The game while the heroes are in a dungeon (the user's design, 2026-10-30/31; card
    /// Design/features/dungeon.md). Sits on GameManagers in every game and does nothing unless
    /// DungeonTravel.Enter sent the heroes down (GameSessionIntent.DungeonId). Then the scene is
    /// the dungeon's map, not a town: what a town runs -- raids, illness, fires, food, taxes --
    /// is switched off, nothing may be built, the town's HUD pieces are hidden, and no save is
    /// written (the town waits in its own slot). Once the map is down the heroes who came step out
    /// at the camp by the way in, the first of them held. The way out is a DungeonEntrance of its
    /// own (IsExit): tapped it is held with a ring and the «!», a hero sent to it opens its card,
    /// and the card's «Выйти» takes the party home -- walking past it no longer does (the user,
    /// 2026-11-01: «такой принцип как при входе»). A hero stands where he is left (Hero.HoldWhereLeft)
    /// and one who falls rises at the way out.
    ///
    /// Monsters, hirelings, the shop and the crystals come later; this is the walk.
    /// </summary>
    public class DungeonMode : MonoBehaviour
    {
        // how far from the way out (cells, towards the camp) a fallen hero rises
        private const float RiseFromExitCells = 2f;
        // the box a tap on the way out's rock hits (cells): its foot round the exit spot
        private static readonly Vector3 ExitTapBox = new Vector3(3f, 2.5f, 2.5f);   // just the cave's mouth (the user, 2026-11-01: was too wide)
        // the box a tap on a camp building hits when it has no model to measure (cells)
        private static readonly Vector3 CampTapBox = new Vector3(3f, 3f, 3f);

        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private MonoBehaviour[] notInDungeon = new MonoBehaviour[0];
        // the town's HUD pieces that mean nothing below: building, the hotbar, taxes (by name: the
        // UI is built after the managers, see SetupProjectGameplayUI)
        [SerializeField] private string[] hideInDungeon = { "BuildingMenu", "Hotbar", "TaxRate" };

        public static DungeonMode Instance { get; private set; }

        /// <summary>The heroes are in a dungeon: this game is the dungeon, not the town.</summary>
        public static bool IsActive => Instance != null && Instance._active;

        private bool _active;
        private List<HeroEntry> _arriving;

        public string DungeonId { get; private set; }


        /// <summary>The level of the entrance the heroes came down by: the monsters' strength and the rewards.</summary>
        public int Level { get; private set; } = 1;

        /// <summary>The town's save name, given back to it when the heroes come up.</summary>
        public string ReturnName { get; private set; }

        /// <summary>Where the heroes step out (the camp by the way in) and the way out itself, world x/z.</summary>
        public Vector3 StartSpot { get; private set; }
        public Vector3 ExitSpot { get; private set; }

        /// <summary>The heroes are down and standing at the camp.</summary>
        public bool Arrived { get; private set; }

        /// <summary>The coins paid out below (gnomes, potions): taken off the town's treasury on the way up.</summary>
        public int CoinsSpent { get; private set; }

        /// <summary>The camp's hut and the potion shop, as places held and sent to like the way out.</summary>
        public DungeonEntrance HireCamp { get; private set; }
        public DungeonEntrance Shop { get; private set; }

        /// <summary>The treasury on the bar holds `coins`.</summary>
        public bool CanPay(int coins)
        {
            var resources = Resources.ResourceManager.Instance;
            return resources != null && (resources.InfiniteResources || resources.GetAmount(HeroRoster.Money) >= coins);
        }

        /// <summary>Pays `coins` out of the treasury on the bar, noting it for the town; false when it is short.</summary>
        public bool TrySpendCoins(int coins)
        {
            if (!CanPay(coins)) return false;
            var resources = Resources.ResourceManager.Instance;
            if (resources.InfiniteResources) return true;
            resources.SetAmount(HeroRoster.Money, resources.GetAmount(HeroRoster.Money) - coins);
            CoinsSpent += coins;
            return true;
        }

        private void Awake()
        {
            Instance = this;
            _active = !string.IsNullOrEmpty(GameSessionIntent.DungeonId);
            DungeonId = GameSessionIntent.DungeonId;
            Level = GameSessionIntent.DungeonLevel;
            GameSessionIntent.DungeonLevel = 1;
            _arriving = GameSessionIntent.DungeonHeroes ?? new List<HeroEntry>();
            ReturnName = GameSessionIntent.DungeonReturnName;
            GameSessionIntent.DungeonId = null;
            GameSessionIntent.DungeonHeroes = null;
            GameSessionIntent.DungeonReturnName = null;
            Hero.HoldWhereLeft = _active;
            if (!_active) return;

            foreach (var behaviour in notInDungeon)
            {
                if (behaviour != null) behaviour.enabled = false;
            }
            // no Town Hall to place: there is no town down here
            if (buildingPlacer != null) buildingPlacer.MarkMandatoryBuildingAlreadyPlaced();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            Hero.HoldWhereLeft = false;
        }

        /// <summary>The way out, as a mouth the heroes are sent to and its card opens from.</summary>
        public DungeonEntrance Exit { get; private set; }

        private IEnumerator Start()
        {
            if (!_active) yield break;
            foreach (var name in hideInDungeon)
            {
                var go = GameObject.Find(name);
                if (go != null) go.SetActive(false);
            }
            // no days pass below: the day's plaque goes, not just its words (the user, 2026-11-01:
            // «пустой прямоугольник, на котором ничего не написано»)
            var day = GameObject.Find("SettlementStatus");
            if (day != null) day.SetActive(false);
            // no citizens below: the inventory takes their button's place (the user, 2026-11-01)
            var citizens = GameObject.Find("WorkforceButton");
            var inventory = GameObject.Find("InventoryButton");
            if (citizens != null && inventory != null)
            {
                ((RectTransform)inventory.transform).anchoredPosition = ((RectTransform)citizens.transform).anchoredPosition;
                citizens.SetActive(false);
            }
            // MeshMapApplier lays the map down in its own Start
            while (MeshMapApplier.Instance == null || MeshMapApplier.Instance.CurrentMap == null) yield return null;
            yield return null;
            var map = MeshMapApplier.Instance.CurrentMap;
            StartSpot = SpotOr(map, "start", Vector3.zero);
            ExitSpot = SpotOr(map, "exit", StartSpot);

            var rig = FindAnyObjectByType<RTSCameraController>();
            if (rig != null)
            {
                var half = MapHalf = HalfSize(map);
                rig.SetPanBounds(new Vector2(-half, -half), new Vector2(half, half));
                // one fixed view, no zoom (the user, 2026-11-01)
                var balance = Core.BalanceConfig.Instance;
                rig.LockView(balance != null ? balance.DungeonCameraDistance : 12f, balance != null ? balance.DungeonCameraPitch : 50f);
                rig.JumpTo(StartSpot);
            }
            Exit = MakeExit();
            // the stone spider queen in the lake, a crystal turning over her
            DungeonStatue.RaiseAll(map, transform);
            HireCamp = MakeCamp(map, "hirecamp", DungeonEntrance.Place.HireCamp);
            Shop = MakeCamp(map, "potionshop", DungeonEntrance.Place.Shop);
            SpawnHeroes();
            // only the tunnels the heroes reach, and the minimap drawn from them
            gameObject.AddComponent<DungeonFog>().Begin(MapHalf);
            // the monsters in their lairs and the bosses the town has not slain on this level yet
            var town = SaveSystem.Load(SaveSystem.HiddenTownSlot);
            // the coins on the bar are the town's (what is earned below is added to them on the way up)
            var coins = town?.resources?.Find(r => r.type == HeroRoster.Money);
            if (Resources.ResourceManager.Instance != null)
                Resources.ResourceManager.Instance.SetAmount(HeroRoster.Money, coins != null ? coins.amount : 0);
            gameObject.AddComponent<DungeonMonsters>().Begin(map, DungeonId, Level, town?.dungeonBossesSlain);
            // the crystals the town has not taken yet, each by its guards
            gameObject.AddComponent<DungeonCrystals>().Begin(map, DungeonId, Level, town?.dungeonCrystalsTaken);
            // the gnomes hired last time, waiting by the way in
            gameObject.AddComponent<DungeonHirelings>().Begin(DungeonId, StartSpot + Vector3.back * 1.5f,
                HireCamp != null ? HireCamp.ApproachFrom(StartSpot) : StartSpot, town?.dungeonHirelings);
            // the potions not drunk last time, back under the portraits
            gameObject.AddComponent<DungeonPotions>().Begin(town?.dungeonPotions);
            Arrived = true;
        }

        private float _wipeCheck;

        /// <summary>False in the tests, which strike their heroes down on purpose and must stay below.</summary>
        public static bool LeaveWhenAllFall = true;

        /// <summary>Every hero down below: nobody is left to lead, the dungeon is left (they rise at the Town Hall).</summary>
        private void Update()
        {
            if (!_active || !Arrived || !LeaveWhenAllFall || UI.LoadingScreen.Busy) return;
            _wipeCheck -= Time.deltaTime;
            if (_wipeCheck > 0f) return;
            _wipeCheck = 1f;
            var roster = HeroRoster.Instance;
            if (roster == null || roster.Heroes.Count == 0) return;
            foreach (var hero in roster.Heroes)
                if (hero != null && !hero.IsFallen) return;
            DungeonTravel.Leave();
        }

        /// <summary>Half the dungeon's side, in cells: it is square round the origin.</summary>
        public float MapHalf { get; private set; } = 50f;

        private static float HalfSize(MeshMapDefinition map)
        {
            // the "edge" spot marks the middle of the south edge (dungeon1.py); the way out is no
            // longer on it -- it is in the camp's north wall, facing the camera
            var spots = map.SpotsOf("edge");
            return spots.Count > 0 ? Mathf.Abs(spots[0].position.y) : 50f;
        }

        private static Vector3 SpotOr(MeshMapDefinition map, string kind, Vector3 fallback)
        {
            var spots = map.SpotsOf(kind);
            if (spots.Count == 0) return fallback;
            var p = spots[0].position;
            var at = new Vector3(p.x, 0f, p.y);
            if (MeshMapApplier.Instance.TryGetGroundHeight(at, out var y)) at.y = y;
            return at;
        }

        private DungeonEntrance MakeExit()
        {
            var go = new GameObject("DungeonExit");
            go.transform.position = ExitSpot;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = ExitTapBox;
            box.center = new Vector3(0f, ExitTapBox.y * 0.5f, 0f);
            var exit = go.AddComponent<DungeonEntrance>();
            exit.InitializeExit(DungeonId);
            return exit;
        }

        /// <summary>
        /// A camp building (the gnomes' hut, the potion shop) made a place to tap: a box round the
        /// model that stands on the `kind` spot (MeshMapApplier.StandOnSpot), or a plain one there.
        /// </summary>
        private DungeonEntrance MakeCamp(MeshMapDefinition map, string kind, DungeonEntrance.Place place)
        {
            if (map.SpotsOf(kind).Count == 0) return null;
            var at = SpotOr(map, kind, StartSpot);
            var size = CampTapBox;
            var centre = at;
            foreach (var renderer in MeshMapApplier.Instance.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.name.ToLowerInvariant().Contains(kind) && !renderer.transform.root.name.ToLowerInvariant().Contains(kind)
                    && (renderer.transform.parent == null || !renderer.transform.parent.name.ToLowerInvariant().Contains(kind))) continue;
                var b = renderer.bounds;
                size = new Vector3(b.size.x, Mathf.Max(b.size.y, CampTapBox.y), b.size.z);
                centre = new Vector3(b.center.x, at.y, b.center.z);
                break;
            }
            var go = new GameObject("Dungeon" + place);
            go.transform.position = centre;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            box.center = new Vector3(0f, size.y * 0.5f, 0f);
            var camp = go.AddComponent<DungeonEntrance>();
            camp.InitializeCamp(place, DungeonId, Mathf.Max(size.x, size.z) * 0.5f);
            return camp;
        }

        /// <summary>Where a fallen hero rises: just in front of the way out, on the camp's side, on the NavMesh.</summary>
        private Vector3 RiseSpot(int index)
        {
            var towardsCamp = StartSpot - ExitSpot;
            towardsCamp.y = 0f;
            var forward = towardsCamp.sqrMagnitude > 0.01f ? towardsCamp.normalized : Vector3.back;
            var side = new Vector3(forward.z, 0f, -forward.x);
            var at = ExitSpot + forward * RiseFromExitCells + side * ((index % 2 == 1 ? -1.2f : 1.2f) * ((index + 1) / 2));
            return UnityEngine.AI.NavMesh.SamplePosition(at, out var hit, 3f, UnityEngine.AI.NavMesh.AllAreas) ? hit.position : at;
        }

        private void SpawnHeroes()
        {
            var roster = HeroRoster.Instance;
            if (roster == null) return;
            // a hero-less town still sends someone down when a cheat opens the dungeon: the King
            if (_arriving.Count == 0) _arriving.Add(new HeroEntry { type = SoldierType.King, level = 1 });
            for (var i = 0; i < _arriving.Count; i++)
            {
                var entry = _arriving[i];
                var spot = StartSpot + new Vector3((i % 2 == 1 ? -1.2f : 1.2f) * ((i + 1) / 2), 0f, 0f);
                var hero = roster.Restore(entry.type, spot, entry.level, entry.experience, entry.currentHealth, false, 0f);
                if (hero == null) continue;
                // he rises at the way out (the user, 2026-11-01), and stands where he stepped out:
                // the roster gave him a home by a Town Hall there is none of down here
                hero.Home = RiseSpot(i);
                hero.GuardCentre = spot;
                hero.Unit.Group?.OrderMoveTo(spot);
            }
            // the first hero held, as he was when he went down (the user, 2026-11-01: the selection
            // dropped on the way in)
            var first = roster.Heroes.Count > 0 ? roster.Heroes[0] : null;
            if (first != null && !first.IsFallen && ArmyManager.Instance != null) ArmyManager.Instance.SelectGroup(first.Unit.Group);
        }

    }
}
