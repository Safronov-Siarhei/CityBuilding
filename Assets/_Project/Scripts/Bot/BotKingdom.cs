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
    /// (Resources/Bot/BotCastle.json, format citybuilder-base/1 -- Tools/base_editor) and turned
    /// so its harbour, the blueprint's south, faces the island's nearest water. Its buildings are
    /// the town's own kinds marked IsBot (BuildingInstance.InitializeAsBot): walls, towers, moat
    /// and bridges work as they do in the town, none of the town's sums see them.
    ///
    /// What a raid destroys comes back once no orc has been within bot_calm_radius_cells of its
    /// Town Hall for bot_rebuild_calm_sec -- while that hall stands. Razed, the bot rebuilds
    /// nothing again, and a war with it ends (BotRelations.EndWar).
    /// </summary>
    public class BotKingdom : MonoBehaviour
    {
        [SerializeField] private TextAsset blueprint;

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
        private float _calmFor;
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
            if (map == null || !map.ArchipelagoBotCell.HasValue || blueprint == null) yield break;
            Place(map.ArchipelagoBotCell.Value, map);
            if (_pendingRestore) ApplyRestore(_pendingRazed, _pendingMissing);
        }

        private void Place(Vector2Int centre, MeshMapApplier map)
        {
            var plan = JsonUtility.FromJson<Blueprint>(blueprint.text);
            var grid = GridManager.Instance;
            var placer = FindAnyObjectByType<BuildingPlacer>();
            if (plan == null || plan.buildings == null || grid == null || placer == null) return;
            var catalog = new Dictionary<string, BuildingData>();
            foreach (var data in placer.AvailableBuildings)
                if (data != null) catalog[data.buildingName] = data;
            if (placer.MandatoryFirstBuilding != null) catalog[placer.MandatoryFirstBuilding.buildingName] = placer.MandatoryFirstBuilding;

            // the blueprint's own centre: the cell in its Town Hall's middle
            var anchor = new Vector2Int(plan.width / 2, plan.height / 2);
            foreach (var piece in plan.buildings)
                if (piece.id == BuildingIds.TownHall) anchor = new Vector2Int(piece.x + piece.w / 2, piece.y + piece.h / 2);
            var turns = HarbourTurns(centre, map);

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
            Placed = true;
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
            grid.SetAreaOccupied(slot.origin, footprint, true);
            if (slot.data.isRoad && RoadNetwork.Instance != null)
            {
                for (var x = 0; x < footprint.x; x++)
                for (var y = 0; y < footprint.y; y++)
                    RoadNetwork.Instance.RegisterRoad(slot.origin + new Vector2Int(x, y));
            }
            slot.instance = instance;
            if (slot.data.buildingName == BuildingIds.TownHall) Hall = instance;
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
            var reach = config.BotCalmRadiusCells * (GridManager.Instance != null ? GridManager.Instance.CellSize : 1f);
            var at = Hall.transform.position;
            foreach (var orc in OrcUnit.All)
            {
                if (orc == null || orc.CurrentHealth <= 0) continue;
                var d = orc.transform.position - at;
                d.y = 0f;
                if (d.sqrMagnitude > reach * reach) continue;
                _calmFor = 0f;
                return;
            }
            _calmFor += Time.deltaTime;
            if (_calmFor < config.BotRebuildCalmSeconds) return;
            _calmFor = 0f;
            Rebuild();
        }

        /// <summary>Raises every slot whose building is gone (a beaten-off raid's damage).</summary>
        public void Rebuild()
        {
            var map = MeshMapApplier.Instance;
            if (map == null || HallRazed) return;
            foreach (var slot in _slots)
            {
                if (slot.instance != null) continue;
                Raise(slot, map);
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
                Destroy(slot.instance.gameObject);
                slot.instance = null;
            }
            if (razed) Hall = null;
        }
    }
}
