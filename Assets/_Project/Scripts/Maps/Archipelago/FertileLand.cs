using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Maps.Archipelago
{
    /// <summary>
    /// Fertile land on the archipelago (the user, 2026-10-28): patches of dark soil on the islands'
    /// plain where a Farm yields more -- farms only. Drawn from the map's own seed apart from the
    /// generator's draws, so the islands, and every map code, stay as they were; nothing to save.
    /// </summary>
    public static class FertileLand
    {
        private static readonly Color Soil = new Color(0.36f, 0.25f, 0.15f);
        private static readonly Color Furrow = new Color(0.29f, 0.2f, 0.12f);
        private const float OrcClearance = 60f;

        /// <summary>
        /// The fertile cells: fertile_patches_per_island blobs of fertile_patch_radius cells per
        /// island, on plain ground only (no forest, stones, beach, cliff or relief), none on the
        /// orcs' island; one always near the start, where the first farms go.
        /// </summary>
        public static HashSet<Vector2Int> Compute(ArchipelagoMap world)
        {
            var cells = new HashSet<Vector2Int>();
            var n = world.Size;
            var balance = BalanceConfig.Instance;
            var perIsland = balance != null ? balance.FertilePatchesPerIsland : 2;
            var radius = balance != null ? balance.FertilePatchRadius : 5f;
            if (perIsland <= 0 || radius <= 0f) return cells;

            var plain = new List<int>();
            for (var i = 0; i < n * n; i++)
            {
                if (!IsPlain(world, i)) continue;
                var x = i % n;
                var z = i / n;
                if (Vector2Int.Distance(new Vector2Int(x, z), world.OrcBaseCell) < OrcClearance) continue;
                plain.Add(i);
            }
            if (plain.Count == 0) return cells;

            var rng = new SeedRandom(world.Seed * 7919 + 101);
            // near the start first: the plain cell nearest a point 12-30 cells out from the Town Hall's spot
            var a = rng.Range(0f, Mathf.PI * 2f);
            var want = world.StartCell + Vector2Int.RoundToInt(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rng.Range(12f, 30f));
            var best = plain[0];
            var bestD = float.MaxValue;
            foreach (var i in plain)
            {
                var d = (new Vector2Int(i % n, i / n) - want).sqrMagnitude;
                if (d >= bestD) continue;
                bestD = d;
                best = i;
            }
            Patch(world, cells, best % n, best / n, radius, rng);

            var patches = world.Islands.Count * perIsland - 1;
            for (var k = 0; k < patches; k++)
            {
                var i = plain[rng.RangeInt(0, plain.Count)];
                Patch(world, cells, i % n, i / n, radius * rng.Range(0.7f, 1.2f), rng);
            }
            return cells;
        }

        private static bool IsPlain(ArchipelagoMap world, int i) =>
            world.Land[i] && !world.IsRelief(i) && !world.Cliff[i] && !world.Forest[i] && !world.Stony[i] && !world.Beach[i];

        /// <summary>A blob with a ragged edge: a circle whose radius wobbles with the angle.</summary>
        private static void Patch(ArchipelagoMap world, HashSet<Vector2Int> cells, int cx, int cz, float radius, SeedRandom rng)
        {
            var n = world.Size;
            var r = Mathf.CeilToInt(radius * 1.3f);
            var p1 = rng.Range(0f, 6.3f);
            var p2 = rng.Range(0f, 6.3f);
            for (var dz = -r; dz <= r; dz++)
            for (var dx = -r; dx <= r; dx++)
            {
                int x = cx + dx, z = cz + dz;
                if (x < 0 || z < 0 || x >= n || z >= n || !IsPlain(world, z * n + x)) continue;
                var angle = Mathf.Atan2(dz, dx);
                var edge = radius * (1f + 0.2f * Mathf.Sin(angle * 3f + p1) + 0.1f * Mathf.Sin(angle * 5f + p2));
                if (dx * dx + dz * dz <= edge * edge) cells.Add(new Vector2Int(x, z));
            }
        }

        /// <summary>
        /// The patches drawn: a quad of dark soil on each cell, a hair over the ground, every other
        /// row a shade darker for the furrows. One mesh, two materials, no shadows -- two draws.
        /// </summary>
        public static GameObject Build(HashSet<Vector2Int> cells, Transform parent, MeshMapApplier applier)
        {
            var grid = GridManager.Instance;
            if (cells.Count == 0 || grid == null) return null;
            var vertices = new List<Vector3>();
            var soil = new List<int>();
            var furrows = new List<int>();
            var half = grid.CellSize * 0.5f;
            foreach (var cell in cells)
            {
                var c = grid.GetFootprintCenterWorld(cell, Vector2Int.one);
                var ray = new Ray(new Vector3(c.x, c.y + 50f, c.z), Vector3.down);
                var y = applier.TryRaycastGround(ray, out var hit) ? hit.point.y : grid.GroundHeight;
                y += 0.03f;
                var s = vertices.Count;
                vertices.Add(new Vector3(c.x - half, y, c.z - half));
                vertices.Add(new Vector3(c.x - half, y, c.z + half));
                vertices.Add(new Vector3(c.x + half, y, c.z + half));
                vertices.Add(new Vector3(c.x + half, y, c.z - half));
                (cell.y % 2 == 0 ? soil : furrows).AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
            }
            var mesh = new Mesh { name = "FertileLand", indexFormat = IndexFormat.UInt32, subMeshCount = 2 };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(soil, 0);
            mesh.SetTriangles(furrows, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject("FertileLand");
            // the vertices are in world space: keep the object where the world's origin is
            go.transform.SetParent(parent, true);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[]
            {
                new Material(RuntimeShaders.Lit) { color = Soil },
                new Material(RuntimeShaders.Lit) { color = Furrow },
            };
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }
    }
}
