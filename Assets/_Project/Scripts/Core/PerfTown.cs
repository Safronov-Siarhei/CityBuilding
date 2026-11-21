using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Grid;
using CityBuilder.Maps;
using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// A big town on demand for measuring on a phone (the user, 2026-11-20: «живые тесты на планшете Редми ... большой
    /// город с 100 зданий и 200+ жителей с капсулами и без»). PerfProbe's `town &lt;buildings&gt; &lt;people&gt;` line: puts the
    /// catalogue's land buildings round the Town Hall (the way PlaytestWorld does in the tests: no price, no rules,
    /// the game's own Initialize), finishes research, fills the stores, holds raids off, sets the headcount and staffs
    /// every workplace -- so the people walk. Not in a release build (PerfProbe is not created there).
    /// </summary>
    public static class PerfTown
    {
        public static string Build(int buildings, int people)
        {
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            var grid = GridManager.Instance;
            if (placer == null || grid == null) return "town: no placer/grid (start a sandbox game first)";

            Research.ResearchManager.Instance?.CompleteEverything();
            if (Combat.OrcRaidManager.Instance != null) Combat.OrcRaidManager.Instance.RaidsSuspended = true;
            var res = Resources.ResourceManager.Instance;
            if (res != null)
                foreach (Resources.ResourceType t in System.Enum.GetValues(typeof(Resources.ResourceType)))
                    res.SetAmount(t, 100000);

            var centre = BuildingInstance.TownHall != null
                ? BuildingInstance.TownHall.OriginCell
                : new Vector2Int(grid.GridSize.x / 2, grid.GridSize.y / 2);
            if (BuildingInstance.TownHall == null && placer.MandatoryFirstBuilding != null)
            {
                var hallCell = FindFreeAreaNear(centre, placer.MandatoryFirstBuilding.footprintSize);
                if (hallCell.x >= 0) Place(placer.MandatoryFirstBuilding, hallCell);
            }

            var catalogue = new List<BuildingData>();
            foreach (var data in placer.AvailableBuildings)
                if (data != null && !data.isWaterCategory && data.prefab != null) catalogue.Add(data);
            var placed = 0;
            for (var i = 0; i < buildings * 3 && placed < buildings && catalogue.Count > 0; i++)
            {
                var data = catalogue[i % catalogue.Count];
                // a street's gap round each one, so the people have room to walk between them
                var cell = FindFreeAreaNear(centre, data.footprintSize + new Vector2Int(2, 2));
                if (cell.x < 0) continue;
                if (Place(data, cell + Vector2Int.one) != null) placed++;
            }

            CitizenManager.Instance?.SetPopulation(people);
            var staffed = 0;
            foreach (var w in Object.FindObjectsByType<ProductionBuilding>(FindObjectsSortMode.None))
                while (w.TryAssignWorker()) staffed++;
            return "town: " + placed + " buildings, " + people + " people, " + staffed + " at work";
        }

        private static readonly List<Combat.SoldierUnit> BattleSoldiers = new List<Combat.SoldierUnit>();
        private static readonly List<Combat.OrcUnit> BattleOrcs = new List<Combat.OrcUnit>();

        /// <summary>
        /// A field battle on screen (the user, 2026-11-21: «сколько солдат разом выдержал бы планшет ... чтобы фпс не
        /// падал ниже 30»): `soldiers` of the player's kind (2/3 men-at-arms, 1/3 archers, in squads of 20) facing
        /// `orcs` raiders a few cells off, south of the hall, the camera put on them. Both sides `toughness` times their
        /// health so the fight lasts the measurement. PerfProbe `battle N M`; `battle 0 0` clears it.
        /// </summary>
        public static string Battle(int soldiers, int orcs, float toughness = 30f)
        {
            ClearBattle();
            if (soldiers <= 0 && orcs <= 0) return "battle: cleared";
            var army = Combat.ArmyManager.Instance;
            var raids = Combat.OrcRaidManager.Instance;
            var grid = GridManager.Instance;
            if (army == null || raids == null || grid == null) return "battle: no army/raids/grid";
            if (Combat.OrcRaidManager.Instance != null) Combat.OrcRaidManager.Instance.RaidsSuspended = true;

            var hall = BuildingInstance.TownHall;
            var centre = hall != null ? hall.transform.position + new Vector3(0f, 0f, -12f) : grid.GetFootprintCenterWorld(new Vector2Int(grid.GridSize.x / 2, grid.GridSize.y / 2), Vector2Int.one);
            if (UnityEngine.AI.NavMesh.SamplePosition(centre, out var hit, 10f, UnityEngine.AI.NavMesh.AllAreas)) centre = hit.position;
            var ours = centre + new Vector3(-4f, 0f, 0f);
            var theirs = centre + new Vector3(4f, 0f, 0f);

            Combat.ArmyGroup group = null;
            for (var i = 0; i < soldiers; i++)
            {
                var type = i % 3 == 2 ? Combat.SoldierType.Archer : Combat.SoldierType.ManAtArms;
                if (i % 20 == 0) group = new Combat.ArmyGroup(type, theirs);
                var at = ours + new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-5f, 5f));
                var unit = army.SpawnHireling(type, at, group);
                if (unit == null) continue;
                var stats = Combat.SoldierStats.StatsAt(type, 1);
                unit.SetHeroStats(Mathf.RoundToInt(stats.maxHealth * toughness), stats.attackDamage);
                unit.SetCurrentHealth(Mathf.RoundToInt(stats.maxHealth * toughness));
                BattleSoldiers.Add(unit);
            }
            if (group != null) group.OrderMoveTo(theirs);
            var before = Combat.OrcUnit.All.Count;
            for (var i = 0; i < orcs; i++)
                raids.SpawnOrcs(theirs + new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-5f, 5f)), 1, 1);
            for (var i = before; i < Combat.OrcUnit.All.Count; i++)
            {
                var orc = Combat.OrcUnit.All[i];
                if (orc == null) continue;
                orc.Toughen(toughness);
                BattleOrcs.Add(orc);
            }
            var cam = CameraControl.RTSCameraController.Instance;
            if (cam != null) cam.JumpTo(centre);
            return "battle: " + BattleSoldiers.Count + " soldiers vs " + BattleOrcs.Count + " orcs";
        }

        private static void ClearBattle()
        {
            var army = Combat.ArmyManager.Instance;
            foreach (var u in BattleSoldiers) if (u != null && army != null) army.Dismiss(u);
            foreach (var o in BattleOrcs) if (o != null) Object.Destroy(o.gameObject);
            BattleSoldiers.Clear();
            BattleOrcs.Clear();
        }

        private static BuildingInstance Place(BuildingData data, Vector2Int cell)
        {
            var grid = GridManager.Instance;
            var footprint = data.footprintSize;
            var instance = Object.Instantiate(data.prefab, grid.GetFootprintCenterWorld(cell, footprint), Quaternion.identity);
            var building = instance.GetComponent<BuildingInstance>();
            if (building == null) building = instance.AddComponent<BuildingInstance>();
            building.Initialize(data, cell, 0);
            grid.SetAreaOccupied(cell, footprint, true);
            return building;
        }

        private static Vector2Int FindFreeAreaNear(Vector2Int centre, Vector2Int size)
        {
            var grid = GridManager.Instance;
            var reach = Mathf.Max(grid.GridSize.x, grid.GridSize.y) / 2;
            for (var ring = 0; ring < reach; ring++)
                for (var dy = -ring; dy <= ring; dy++)
                    for (var dx = -ring; dx <= ring; dx++)
                    {
                        if (ring > 0 && Mathf.Abs(dx) != ring && Mathf.Abs(dy) != ring) continue;
                        var origin = centre + new Vector2Int(dx, dy);
                        if (origin.x < 1 || origin.y < 1) continue;
                        if (origin.x + size.x >= grid.GridSize.x - 1 || origin.y + size.y >= grid.GridSize.y - 1) continue;
                        if (Buildable(origin, size)) return origin;
                    }
            return new Vector2Int(-1, -1);
        }

        private static bool Buildable(Vector2Int origin, Vector2Int size)
        {
            var grid = GridManager.Instance;
            if (!grid.IsAreaFree(origin, size)) return false;
            var map = MeshMapApplier.Instance;
            if (map == null) return true;
            for (var x = 0; x < size.x; x++)
                for (var z = 0; z < size.y; z++)
                {
                    var c = origin + new Vector2Int(x, z);
                    if (map.IsWaterCell(c) || map.IsNoBuildCell(c)) return false;
                }
            return true;
        }
    }
}
