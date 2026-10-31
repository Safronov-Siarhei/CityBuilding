using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Dungeon;
using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Maps.Archipelago
{
    /// <summary>
    /// The dungeons' entrances on the archipelago (the user, 2026-10-31: three dungeons a map, all
    /// leading to Dungeon 1 for now): an abandoned mine (MineAbandoned, 5 x 5 cells) on
    /// dungeon_entrance_count islands that are neither the start's, the orcs' nor the bot's.
    /// Nearer the start = lower level (1, 2, 3 -- the user: weaker monsters, smaller rewards near
    /// the Town Hall). Each stands on the plain nearest its island's middle, its mouth turned to
    /// the start. From the map's seed alone, so nothing is saved.
    /// </summary>
    public static class DungeonEntrances
    {
        public static readonly Vector2Int Footprint = new Vector2Int(5, 5);

        public static List<DungeonEntrance> Place(ArchipelagoMap world, MeshMapApplier applier, GameObject prefab, HashSet<Vector2Int> noBuild)
        {
            var placed = new List<DungeonEntrance>();
            var grid = GridManager.Instance;
            var balance = BalanceConfig.Instance;
            var want = balance != null ? balance.DungeonEntranceCount : 3;
            if (world == null || grid == null || want <= 0) return placed;

            var start = new Vector2(world.StartCell.x, world.StartCell.y);
            var islands = new List<Island>();
            foreach (var island in world.Islands)
            {
                if (island.Kind == ArchipelagoGenerator.Kind.Start || island.Kind == ArchipelagoGenerator.Kind.Orc
                    || island.Kind == ArchipelagoGenerator.Kind.Bot) continue;
                if (Vector2.Distance(island.Centre, new Vector2(world.OrcBaseCell.x, world.OrcBaseCell.y)) < 40f) continue;
                islands.Add(island);
            }
            islands.Sort((a, b) => Vector2.Distance(a.Centre, start).CompareTo(Vector2.Distance(b.Centre, start)));
            if (islands.Count == 0) return placed;

            // the nearest island, the farthest, and evenly between: level 1 nearest; an island with
            // no grassy spot gives way to its neighbour in the list
            var used = new HashSet<Island>();
            var level = 0;
            for (var k = 0; k < want && k < islands.Count; k++)
            {
                var index = want == 1 ? 0 : Mathf.RoundToInt(k * (islands.Count - 1) / (float)(want - 1));
                Vector2Int origin = default;
                Island chosen = null;
                for (var step = 0; step < islands.Count && chosen == null; step++)
                {
                    var i = index + ((step & 1) == 0 ? step / 2 : -(step + 1) / 2);
                    if (i < 0 || i >= islands.Count || used.Contains(islands[i])) continue;
                    if (TryFindSpot(world, islands[i], applier, out origin)) chosen = islands[i];
                }
                if (chosen == null) continue;
                used.Add(chosen);
                level++;
                var entrance = Build(world, grid, prefab, origin, level, start);
                foreach (var cell in Cells(origin)) noBuild.Add(cell);
                grid.SetAreaOccupied(origin, Footprint, true);
                placed.Add(entrance);
            }
            return placed;
        }

        private static IEnumerable<Vector2Int> Cells(Vector2Int origin)
        {
            for (var z = 0; z < Footprint.y; z++)
            for (var x = 0; x < Footprint.x; x++) yield return origin + new Vector2Int(x, z);
        }

        /// <summary>
        /// The footprint's corner cell nearest the island's middle whose whole 5 x 5 (and a cell
        /// round it) is plain, dry land -- and GRASS where it is drawn: the library's islands carry
        /// no beach flags, so their sand is only known by its colour (an entrance stood on the
        /// sand, the user's screenshot of 2026-10-31).
        /// </summary>
        private static bool TryFindSpot(ArchipelagoMap world, Island island, MeshMapApplier applier, out Vector2Int origin)
        {
            origin = default;
            var n = world.Size;
            var centre = island.Centre;
            var candidates = new List<(float d, Vector2Int at)>();
            for (var z = island.OffsetY; z < island.OffsetY + island.Frame - Footprint.y; z++)
            for (var x = island.OffsetX; x < island.OffsetX + island.Frame - Footprint.x; x++)
            {
                var d = (new Vector2(x + Footprint.x * 0.5f, z + Footprint.y * 0.5f) - centre).sqrMagnitude;
                var ok = true;
                for (var dz = -1; dz <= Footprint.y && ok; dz++)
                for (var dx = -1; dx <= Footprint.x && ok; dx++)
                {
                    int cx = x + dx, cz = z + dz;
                    if (cx < 0 || cz < 0 || cx >= n || cz >= n) { ok = false; break; }
                    var i = cz * n + cx;
                    ok = world.Land[i] && !world.IsRelief(i) && !world.Cliff[i] && !world.Beach[i] && !world.Shore[i];
                }
                if (ok) candidates.Add((d, new Vector2Int(x, z)));
            }
            candidates.Sort((a, b) => a.d.CompareTo(b.d));
            foreach (var (_, at) in candidates)
            {
                if (!AllGrass(at, applier)) continue;
                origin = at;
                return true;
            }
            return false;
        }

        private static bool AllGrass(Vector2Int origin, MeshMapApplier applier)
        {
            var grid = GridManager.Instance;
            if (applier == null || grid == null) return true;
            for (var dz = 0; dz < Footprint.y; dz++)
            for (var dx = 0; dx < Footprint.x; dx++)
            {
                var c = grid.GetFootprintCenterWorld(origin + new Vector2Int(dx, dz), Vector2Int.one);
                var ray = new Ray(new Vector3(c.x, c.y + 50f, c.z), Vector3.down);
                if (!applier.TryRaycastGround(ray, out var hit) || !(hit.collider is MeshCollider)
                    || !ArchipelagoMeshes.IsGrassUv(hit.textureCoord)) return false;
            }
            return true;
        }

        private static DungeonEntrance Build(ArchipelagoMap world, GridManager grid, GameObject prefab, Vector2Int origin, int level, Vector2 start)
        {
            var at = grid.GetFootprintCenterWorld(origin, Footprint);
            var root = new GameObject($"DungeonEntrance{level}");
            root.transform.position = at;
            // the mouth (the model's front, the game's -z) turned to the player's camera, which
            // never turns (the user, 2026-10-31: facing the camera, so it reads at a glance)
            var cam = Camera.main != null ? Camera.main.transform : null;
            var look = cam != null ? cam.forward : Vector3.forward;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f) root.transform.rotation = Quaternion.LookRotation(look.normalized);
            if (prefab != null)
            {
                var model = Object.Instantiate(prefab, root.transform);
                model.name = "Model";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = prefab.transform.localRotation;
                SceneryShadows.DisableCasting(model);
            }
            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(Footprint.x - 0.6f, 3f, Footprint.y - 0.6f);
            box.center = new Vector3(0f, 1.5f, 0f);
            var entrance = root.AddComponent<DungeonEntrance>();
            entrance.Initialize(level, DungeonTravel.FirstDungeonId, origin);
            return entrance;
        }
    }
}
