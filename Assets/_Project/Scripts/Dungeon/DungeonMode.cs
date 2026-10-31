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
    /// at the camp by the way in, and walking back out through it takes them home.
    ///
    /// Monsters, hirelings, the shop and the crystals come later; this is the walk.
    /// </summary>
    public class DungeonMode : MonoBehaviour
    {
        /// <summary>A hero this close to the way out (cells) goes home -- once he has been further in than ExitArmCells.</summary>
        public const float ExitCells = 1.6f;
        private const float ExitArmCells = 4f;

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
        private bool _armed;

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
            if (Instance == this) Instance = null;
        }

        private IEnumerator Start()
        {
            if (!_active) yield break;
            foreach (var name in hideInDungeon)
            {
                var go = GameObject.Find(name);
                if (go != null) go.SetActive(false);
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
                var half = HalfSize(map);
                rig.SetPanBounds(new Vector2(-half, -half), new Vector2(half, half));
                rig.JumpTo(StartSpot);
            }
            SpawnHeroes();
            // the monsters in their lairs and the bosses the town has not slain on this level yet
            var slain = SaveSystem.Load(SaveSystem.HiddenTownSlot)?.dungeonBossesSlain;
            gameObject.AddComponent<DungeonMonsters>().Begin(map, DungeonId, Level, slain);
            Arrived = true;
        }

        private static float HalfSize(MeshMapDefinition map)
        {
            var spots = map.SpotsOf("exit");
            // the dungeon is square round the origin and its way out sits on the south edge
            return spots.Count > 0 ? Mathf.Abs(spots[0].position.y) + 2f : 50f;
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
                hero.Home = spot;
                hero.GuardCentre = spot;
            }
        }

        private void Update()
        {
            if (!_active || !Arrived || HeroRoster.Instance == null) return;
            var nearest = float.MaxValue;
            foreach (var hero in HeroRoster.Instance.Heroes)
            {
                if (hero == null || hero.IsFallen) continue;
                var p = hero.transform.position;
                nearest = Mathf.Min(nearest, new Vector2(p.x - ExitSpot.x, p.z - ExitSpot.z).magnitude);
            }
            if (nearest == float.MaxValue) return;
            // armed once the party has walked in, so arriving next to the way out does not send them back
            if (nearest > ExitArmCells) _armed = true;
            else if (_armed && nearest <= ExitCells) DungeonTravel.Leave();
        }
    }
}
