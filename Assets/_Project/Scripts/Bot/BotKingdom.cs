using System;
using System.Collections;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Diplomacy;
using CityBuilder.Grid;
using CityBuilder.Maps;
using UnityEngine;

namespace CityBuilder.Bot
{
    /// <summary>
    /// The bot kingdom (the user, 2026-11-03): already grown when the game starts, its castle
    /// stands on the archipelago's bot island, read from a base-editor blueprint
    /// (one of four town templates Bot/BotTownA..D.json picked by the map seed, format citybuilder-base/1 -- Design/bot_town_*.py) and turned
    /// so its harbour, the blueprint's south, faces the island's nearest water. Its buildings are
    /// the town's own kinds marked IsBot (BuildingInstance.InitializeAsBot): walls, towers, moat
    /// and bridges work as they do in the town, none of the town's sums see them.
    ///
    /// What a raid destroys lies in ruins (BuildingRuin, IsBot); after every
    /// bot_rebuild_every_raids-th raid (the user, 2026-11-06: every third), once no orc has been
    /// within bot_calm_radius_cells of its Town Hall for bot_rebuild_calm_sec, the bot raises every
    /// ruin again as a construction under scaffolding -- while that hall stands. Razed, the bot
    /// rebuilds nothing again, and a war with it ends (BotRelations.EndWar).
    /// </summary>
    public class BotKingdom : MonoBehaviour
    {
        // the town templates (the user, 2026-11-05: a random one per map seed, so the player does
        // not storm the same town on every map) -- Design/bot_town_*.py, the base editor's format
        [SerializeField] private TextAsset[] blueprints;
        // the bot's roofs, gold and banners in its own neutral straw, so the player sees at a glance
        // whose they are (the user, 2026-11-03): every renderer on the town's palette swapped onto the bot's copy
        [SerializeField] private Material buildingsPalette;
        [SerializeField] private Material botPalette;
        // its King's figure (on the figures' material) with the cape in the bot's straw
        [SerializeField] private Material figures;
        [SerializeField] private Material botKingPalette;

        public static BotKingdom Instance { get; private set; }

        /// <summary>The castle stands on this map (the archipelago).</summary>
        public bool Placed { get; private set; }
        public bool HallRazed { get; private set; }
        public BuildingInstance Hall { get; private set; }

        [Serializable]
        private class Blueprint
        {
            public string format;
            public string name;
            public int width, height;
            public List<Piece> buildings = new List<Piece>();
        }

        [Serializable]
        private class Piece
        {
            public string id;
            public int x, y, w, h, rot, level;
        }

        private class Slot
        {
            public BuildingData data;
            public Vector2Int origin;
            public int steps;
            public int level;
            public BuildingInstance instance;
        }

        private readonly List<Slot> _slots = new List<Slot>();
        private Vector2Int _centre;
        private int _turns;
        private float _calmFor;
        // the raids on the castle since its last rebuild, and whether the next calm raises its ruins
        private int _raidsSinceRebuild;
        private bool _rebuildDue;
        private float _raidIn = -1f;
        private bool _pendingRestore;
        private bool _pendingRazed;
        private List<int> _pendingMissing;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            BuildingInstance.OnDestroyedInCombat -= HandleDestroyed;
        }

        private void Start()
        {
            BuildingInstance.OnDestroyedInCombat += HandleDestroyed;
            StartCoroutine(PlaceWhenTheMapIsUp());
        }

        private IEnumerator PlaceWhenTheMapIsUp()
        {
            // the map is applied in its own Start; a save's restore may come in the same frame
            yield return null;
            yield return null;
            var map = MeshMapApplier.Instance;
            if (map == null || !map.ArchipelagoBotCell.HasValue || blueprints == null || blueprints.Length == 0) yield break;
            Place(map.ArchipelagoBotCell.Value, map);
            if (_pendingRestore) ApplyRestore(_pendingRazed, _pendingMissing);
        }

        private void Place(Vector2Int centre, MeshMapApplier map)
        {
            var template = TemplateFor(map.ArchipelagoSeed, blueprints.Length);
            TemplateName = blueprints[template] != null ? blueprints[template].name : null;
            var plan = blueprints[template] != null ? JsonUtility.FromJson<Blueprint>(blueprints[template].text) : null;
            var grid = GridManager.Instance;
            var placer = FindAnyObjectByType<BuildingPlacer>();
            if (plan == null || plan.buildings == null || grid == null || placer == null) return;
            var catalog = new Dictionary<string, BuildingData>();
            foreach (var data in placer.AvailableBuildings)
                if (data != null) catalog[data.buildingName] = data;
            if (placer.MandatoryFirstBuilding != null) catalog[placer.MandatoryFirstBuilding.buildingName] = placer.MandatoryFirstBuilding;

            // the town's own centre (the middle of its square body; the sea rows lie below it) -- not
            // its Town Hall: template B keeps its hall at the far end of the town
            var anchor = new Vector2Int(plan.width / 2, plan.width / 2);
            _half = plan.width / 2;
            var turns = HarbourTurns(centre, map);
            _centre = centre;
            _turns = turns;

            foreach (var piece in plan.buildings)
            {
                if (!catalog.TryGetValue(piece.id, out var data) || data.prefab == null) continue;
                // the blueprint's corner cells (x right, y down = south) -> map offsets (y north), turned
                var a = Turn(new Vector2Int(piece.x - anchor.x, anchor.y - piece.y), turns);
                var b = Turn(new Vector2Int(piece.x + piece.w - 1 - anchor.x, anchor.y - (piece.y + piece.h - 1)), turns);
                var origin = centre + new Vector2Int(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
                var slot = new Slot { data = data, origin = origin, steps = (piece.rot + turns) % 4, level = Mathf.Max(1, piece.level) };
                _slots.Add(slot);
                Raise(slot, map);
            }
            Routes(plan, anchor);
            Placed = true;
            gameObject.AddComponent<BotGarrison>();
            gameObject.AddComponent<BotWar>();
        }

        /// <summary>Which template a map seed gets: a hash of its own, so it does not follow the dungeons' picks.</summary>
        public static int TemplateFor(int seed, int count)
            => count <= 0 ? 0 : (int)((uint)(seed * 2654435761u + 40503u) >> 11) % count;

        /// <summary>The template standing on this map (its asset's name), for the cheats and the tests.</summary>
        public string TemplateName { get; private set; }

        /// <summary>The King's round: a loop of points about the Town Hall, offsets from the town's centre (blueprint cells, y south).</summary>
        public Vector2Int[] KingRing { get; private set; } = new Vector2Int[0];

        /// <summary>
        /// The patrols' rounds (the user, 2026-11-05: natural routes through the town, not back and
        /// forth): loops round the town at three depths -- the inner town, the middle town, the
        /// outskirts -- each walked one way and the other, so the patrols cross and meet.
        /// </summary>
        public Vector2Int[][] PatrolRoutes { get; private set; } = new Vector2Int[0][];

        private int _half = 17;
        private readonly HashSet<Vector2Int> _blocked = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> _roads = new HashSet<Vector2Int>();

        /// <summary>The garrison's rounds, read off the template (Routes): the King's loop, then the patrols' loops.</summary>
        private void Routes(Blueprint plan, Vector2Int anchor)
        {
            _blocked.Clear();
            _roads.Clear();
            var hall = Vector2Int.zero;
            foreach (var piece in plan.buildings)
            {
                var open = piece.id == "Road" || piece.id == "Square" || piece.id == "Drawbridge";
                for (var x = 0; x < piece.w; x++)
                for (var y = 0; y < piece.h; y++)
                {
                    var c = new Vector2Int(piece.x + x - anchor.x, piece.y + y - anchor.y);
                    if (open) _roads.Add(c);
                    else _blocked.Add(c);
                }
                if (piece.id == BuildingIds.TownHall) hall = new Vector2Int(piece.x + piece.w / 2 - anchor.x, piece.y + piece.h / 2 - anchor.y);
            }
            var king = Loop(hall, 10, 5);
            KingRing = king.Length > 0 ? king : new[] { hall + new Vector2Int(0, 4) };
            var loops = new List<Vector2Int[]>();
            foreach (var depth in new[] { 0.37f, 0.63f, 0.89f })
            {
                var loop = Loop(Vector2Int.zero, Mathf.RoundToInt(_half * depth), 5);
                if (loop.Length >= 4) loops.Add(loop);
            }
            var routes = new List<Vector2Int[]>();
            foreach (var loop in loops) routes.Add(loop);
            foreach (var loop in loops)
            {
                // the other way round, from the far side
                var back = new Vector2Int[loop.Length];
                for (var k = 0; k < loop.Length; k++) back[k] = loop[(loop.Length / 2 - k + loop.Length * 2) % loop.Length];
                routes.Add(back);
            }
            PatrolRoutes = routes.ToArray();
        }

        private bool OpenCell(Vector2Int c) => Mathf.Abs(c.x) <= _half - 1 && Mathf.Abs(c.y) <= _half - 1 && !_blocked.Contains(c);

        /// <summary>Points every `step` cells round the square of `radius` about `centre`, clockwise, each moved to open ground (a street first) within 2 cells; the unreachable ones left out.</summary>
        private Vector2Int[] Loop(Vector2Int centre, int radius, int step)
        {
            var points = new List<Vector2Int>();
            if (radius < 2) return points.ToArray();
            var ring = new List<Vector2Int>();
            for (var x = -radius; x < radius; x++) ring.Add(new Vector2Int(x, -radius));
            for (var y = -radius; y < radius; y++) ring.Add(new Vector2Int(radius, y));
            for (var x = radius; x > -radius; x--) ring.Add(new Vector2Int(x, radius));
            for (var y = radius; y > -radius; y--) ring.Add(new Vector2Int(-radius, y));
            for (var k = 0; k < ring.Count; k += step)
            {
                if (!Snap(centre + ring[k], out var at)) continue;
                if (points.Count > 0 && (at - points[points.Count - 1]).sqrMagnitude < 9) continue;
                points.Add(at);
            }
            return points.ToArray();
        }

        private bool Snap(Vector2Int c, out Vector2Int at)
        {
            at = c;
            for (var r = 0; r <= 2; r++)
            {
                Vector2Int? open = null;
                for (var dx = -r; dx <= r; dx++)
                for (var dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                    var p = c + new Vector2Int(dx, dy);
                    if (!OpenCell(p)) continue;
                    if (_roads.Contains(p)) { at = p; return true; }
                    open ??= p;
                }
                if (open.HasValue) { at = open.Value; return true; }
            }
            return false;
        }

        /// <summary>The tops of the bot's standing walk-walls (lvl 3+ fence), in the world: where its archers stand at war.</summary>
        public void WallTops(List<Vector3> into)
        {
            foreach (var slot in _slots)
                if (slot.instance != null && slot.data.connectsToFences && !slot.data.isGate && slot.level >= 3 && slot.data.footprintSize == Vector2Int.one)
                    into.Add(slot.instance.transform.position + Vector3.up * WallWalk.HeightFor(slot.level));
        }

        /// <summary>The map cell of a blueprint offset from the town's centre (x right, y down = south), turned as the castle is.</summary>
        public Vector2Int CellAt(Vector2Int blueprintOffset) => _centre + Turn(new Vector2Int(blueprintOffset.x, -blueprintOffset.y), _turns);

        /// <summary>The world point in the middle of that cell.</summary>
        public Vector3 WorldAt(Vector2Int blueprintOffset) =>
            GridManager.Instance != null ? GridManager.Instance.GetFootprintCenterWorld(CellAt(blueprintOffset), Vector2Int.one) : Vector3.zero;

        /// <summary>The bot's standing walk-wall cells (fence of level 3 and up): where its archers keep watch.</summary>
        public void WallCells(List<Vector2Int> into)
        {
            foreach (var slot in _slots)
                if (slot.instance != null && slot.data.connectsToFences && !slot.data.isGate && slot.level >= 3 && slot.data.footprintSize == Vector2Int.one)
                    into.Add(slot.origin);
        }

        /// <summary>The quarter turns that bring the blueprint's south (its harbour) round to the nearest water.</summary>
        private static int HarbourTurns(Vector2Int centre, MeshMapApplier map)
        {
            // blueprint south is the map's -y; a turn is a quarter anticlockwise: -y, +x, +y, -x
            var directions = new[] { Vector2Int.down, Vector2Int.right, Vector2Int.up, Vector2Int.left };
            int best = 0, bestDistance = int.MaxValue;
            for (var k = 0; k < 4; k++)
            {
                for (var d = 1; d < 80; d++)
                {
                    if (!map.IsWaterCell(centre + directions[k] * d)) continue;
                    if (d < bestDistance) { bestDistance = d; best = k; }
                    break;
                }
            }
            return best;
        }

        private static Vector2Int Turn(Vector2Int p, int turns)
        {
            for (var k = 0; k < turns; k++) p = new Vector2Int(-p.y, p.x);
            return p;
        }

        private void Raise(Slot slot, MeshMapApplier map)
        {
            var grid = GridManager.Instance;
            var footprint = slot.steps % 2 == 0 ? slot.data.footprintSize : new Vector2Int(slot.data.footprintSize.y, slot.data.footprintSize.x);
            if (!grid.IsWithinBounds(slot.origin, footprint)) return;
            // a land building never on water; a water one (the shipyard) wherever the plan put it
            if (!slot.data.isWaterCategory && !slot.data.isMoat)
            {
                for (var x = 0; x < footprint.x; x++)
                for (var y = 0; y < footprint.y; y++)
                    if (map.IsWaterCell(slot.origin + new Vector2Int(x, y))) return;
            }
            var centre = grid.GetFootprintCenterWorld(slot.origin, footprint);
            var go = Instantiate(slot.data.prefab, centre, Quaternion.Euler(0f, slot.steps * 90f, 0f));
            go.name = "Bot-" + slot.data.buildingName;
            var instance = go.GetComponent<BuildingInstance>();
            if (instance == null) instance = go.AddComponent<BuildingInstance>();
            instance.InitializeAsBot(slot.data, slot.origin, slot.steps, slot.level);
            Dress(go);
            grid.SetAreaOccupied(slot.origin, footprint, true);
            if (slot.data.isRoad && RoadNetwork.Instance != null)
            {
                for (var x = 0; x < footprint.x; x++)
                for (var y = 0; y < footprint.y; y++)
                    RoadNetwork.Instance.RegisterRoad(slot.origin + new Vector2Int(x, y));
            }
            slot.instance = instance;
            if (slot.data.buildingName == BuildingIds.TownHall) Hall = instance;
            // struck by the town's soldiers at war (BotWarTarget)
            if (instance.GetComponent<BotWarTarget>() == null) instance.gameObject.AddComponent<BotWarTarget>();
        }

        private void HandleDestroyed(BuildingInstance building)
        {
            if (building == null || !building.IsBot || building != Hall) return;
            HallRazed = true;
            BotRelations.EndWar();
        }

        private void Update()
        {
            if (!Placed || HallRazed || Hall == null) return;
            var config = BalanceConfig.Instance;
            if (config == null) return;
            RaidNow(config);
            // the bot rebuilds only after every bot_rebuild_every_raids-th raid (the user, 2026-11-06)
            if (!_rebuildDue)
            {
                _calmFor = 0f;
                return;
            }
            var reach = config.BotCalmRadiusCells * (GridManager.Instance != null ? GridManager.Instance.CellSize : 1f);
            var at = Hall.transform.position;
            foreach (var orc in OrcUnit.All)
            {
                if (orc == null || orc.CurrentHealth <= 0 || orc.BotSide) continue;
                var d = orc.transform.position - at;
                d.y = 0f;
                if (d.sqrMagnitude > reach * reach) continue;
                _calmFor = 0f;
                return;
            }
            // at war, the town's soldiers by the castle keep it from rebuilding too
            if (BotRelations.Stance == BotStance.War)
            {
                foreach (var soldier in SoldierUnit.All)
                {
                    if (soldier == null || soldier.CurrentHealth <= 0) continue;
                    var d = soldier.transform.position - at;
                    d.y = 0f;
                    if (d.sqrMagnitude > reach * reach) continue;
                    _calmFor = 0f;
                    return;
                }
            }
            _calmFor += Time.deltaTime;
            if (_calmFor < config.BotRebuildCalmSeconds) return;
            _calmFor = 0f;
            _rebuildDue = false;
            Rebuild();
        }

        /// <summary>
        /// The orcs raid the bot too (the user, 2026-11-03: massive, for show): every
        /// bot_raid_interval_sec (the first after bot_raid_first_sec) bot_raid_size orcs of
        /// bot_raid_level come out of the ground on one of the castle's three landward sides and go
        /// for its nearest building; its walls, towers and garrison meet them, and what they
        /// destroy is rebuilt once they are gone. None while bot_raid_size is 0.
        /// </summary>
        private void RaidNow(BalanceConfig config)
        {
            if (config.BotRaidSize <= 0) return;
            if (_raidIn < 0f) _raidIn = config.BotRaidFirstSeconds;
            _raidIn -= Time.deltaTime;
            if (_raidIn > 0f) return;
            _raidIn = config.BotRaidIntervalSeconds;
            Raid(config.BotRaidSize, config.BotRaidLevel);
        }

        /// <summary>One raid on the castle now: `count` orcs of `level` out on a landward side (also the cheats' and tests' way in).</summary>
        public void Raid(int count, int level)
        {
            var raids = OrcRaidManager.Instance;
            if (raids == null || !Placed) return;
            // the blueprint's north, east or west, beyond the outer wall (its south is the harbour)
            var o = _half + 3;
            var sides = new[] { new Vector2Int(0, -o), new Vector2Int(o, 0), new Vector2Int(-o, 0) };
            var at = WorldAt(sides[UnityEngine.Random.Range(0, sides.Length)]);
            if (!UnityEngine.AI.NavMesh.SamplePosition(at, out var hit, 6f, UnityEngine.AI.NavMesh.AllAreas)) return;
            raids.SpawnOrcs(hit.position, count, level);
            _raidsSinceRebuild++;
            var every = BalanceConfig.Instance != null ? BalanceConfig.Instance.BotRebuildEveryRaids : 3;
            if (_raidsSinceRebuild < every) return;
            _raidsSinceRebuild = 0;
            _rebuildDue = true;
        }

        /// <summary>
        /// Raises every slot whose building is gone (the raids' damage): its ruin cleared away and
        /// the building put back at its level as a construction, as long as the player's restoring
        /// of a ruin takes (its build times to its level x ruin_restore_time_fraction).
        /// </summary>
        public void Rebuild()
        {
            var map = MeshMapApplier.Instance;
            if (map == null || HallRazed) return;
            var share = BalanceConfig.Instance != null ? BalanceConfig.Instance.RuinRestoreTimeFraction : 0.5f;
            foreach (var slot in _slots)
            {
                if (slot.instance != null) continue;
                BuildingRuin.BotRuinAt(slot.data, slot.origin)?.Remove();
                Raise(slot, map);
                if (slot.instance == null || slot.data.isRoad) continue;
                var seconds = 0f;
                for (var l = 1; l <= slot.level; l++) seconds += slot.data.BuildSeconds(l);
                slot.instance.BuildOnTimer(seconds * share);
            }
        }

        /// <summary>For the save: the hall's fate and which of the blueprint's buildings are down.</summary>
        public void Capture(out bool razed, List<int> missing)
        {
            razed = HallRazed;
            for (var i = 0; i < _slots.Count; i++)
                if (_slots[i].instance == null) missing.Add(i);
        }

        /// <summary>A save's state: applied once the castle stands (now, or when it is placed).</summary>
        public void Restore(bool razed, List<int> missing)
        {
            if (!Placed)
            {
                _pendingRestore = true;
                _pendingRazed = razed;
                _pendingMissing = missing != null ? new List<int>(missing) : new List<int>();
                return;
            }
            ApplyRestore(razed, missing);
        }

        private void ApplyRestore(bool razed, List<int> missing)
        {
            _pendingRestore = false;
            HallRazed = razed;
            if (missing == null) return;
            var grid = GridManager.Instance;
            foreach (var i in missing)
            {
                if (i < 0 || i >= _slots.Count || _slots[i].instance == null) continue;
                var slot = _slots[i];
                var footprint = slot.steps % 2 == 0 ? slot.data.footprintSize : new Vector2Int(slot.data.footprintSize.y, slot.data.footprintSize.x);
                grid?.SetAreaOccupied(slot.origin, footprint, false);
                // down in the save: its ruin where it stood (but the hall, which leaves none)
                if (slot.data.buildingName != BuildingIds.TownHall && !slot.data.isMoat)
                {
                    var cells = new List<Vector2Int>();
                    for (var x = 0; x < footprint.x; x++)
                    for (var y = 0; y < footprint.y; y++)
                        cells.Add(slot.origin + new Vector2Int(x, y));
                    BuildingRuin.BotRuinAt(slot.data, slot.origin)?.Remove();
                    var ruin = BuildingRuin.LeaveOf(slot.instance, cells);
                    if (ruin != null) ruin.IsBot = true;
                }
                Destroy(slot.instance.gameObject);
                slot.instance = null;
            }
            if (razed) Hall = null;
        }

        private static readonly List<Renderer> DressRenderers = new List<Renderer>();

        /// <summary>Every renderer under `root` (the level models it keeps hidden too) on the town's palette moved onto the bot's.</summary>
        private void Dress(GameObject root) => Swap(root, buildingsPalette, botPalette);

        /// <summary>The bot's King in his own cape (BotGarrison): the figure's material swapped for the bot King's.</summary>
        /// <summary>The bot's Shipyard while it stands: its war fleet sails from the sea by it.</summary>
        public BuildingInstance Shipyard
        {
            get
            {
                foreach (var slot in _slots)
                    if (slot.instance != null && slot.instance.CurrentHealth > 0 && slot.data.buildingName == Ships.ShipManager.ShipyardId) return slot.instance;
                return null;
            }
        }

        /// <summary>A unit or a ship of the kingdom in its straw: figures' capes and the buildings' palette both.</summary>
        public void DressAsBot(GameObject root)
        {
            Swap(root, figures, botKingPalette);
            Swap(root, buildingsPalette, botPalette);
        }

        public void DressKing(GameObject king)
        {
            Swap(king, figures, botKingPalette);
            Swap(king, buildingsPalette, botKingPalette);
        }

        private static void Swap(GameObject root, Material from, Material to)
        {
            if (root == null || from == null || to == null) return;
            root.GetComponentsInChildren(true, DressRenderers);
            foreach (var renderer in DressRenderers)
            {
                var materials = renderer.sharedMaterials;
                var changed = false;
                for (var i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != from) continue;
                    materials[i] = to;
                    changed = true;
                }
                if (changed) renderer.sharedMaterials = materials;
            }
            DressRenderers.Clear();
        }
    }
}
