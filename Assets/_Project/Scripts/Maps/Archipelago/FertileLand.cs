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
        /// Drops the cells whose ground is not grass where it is drawn: the library's islands carry
        /// no beach flags, so their sand is only known by its colour (the user, 2026-10-31: no
        /// fertile land on a sandy shore). A ray per cell, once, when the map is laid.
        /// </summary>
        public static void KeepGrass(HashSet<Vector2Int> cells, MeshMapApplier applier)
        {
            var grid = GridManager.Instance;
            if (grid == null || cells.Count == 0) return;
            var drop = new List<Vector2Int>();
            foreach (var cell in cells)
            {
                var c = grid.GetFootprintCenterWorld(cell, Vector2Int.one);
                var ray = new Ray(new Vector3(c.x, c.y + 50f, c.z), Vector3.down);
                if (!applier.TryRaycastGround(ray, out var hit) || !(hit.collider is MeshCollider)
                    || !ArchipelagoMeshes.IsGrassUv(hit.textureCoord)) drop.Add(cell);
            }
            foreach (var cell in drop) cells.Remove(cell);
        }

        /// <summary>
        /// The patches drawn as part of the ground (the user, 2026-10-31: not squares laid on the
        /// map but the map's own soil): smooth outlines traced round the cells (marching squares
        /// over a softened field), lying on the ground in the ground's own material and palette --
        /// so a season's palette dresses them with it -- dark tilled soil inside, a lighter rim
        /// where it meets the grass. One mesh, one material: one draw.
        /// </summary>
        public static GameObject Build(HashSet<Vector2Int> cells, Transform parent, MeshMapApplier applier, Material terrain)
        {
            var grid = GridManager.Instance;
            if (cells.Count == 0 || grid == null || terrain == null) return null;
            var min = new Vector2Int(int.MaxValue, int.MaxValue);
            var max = new Vector2Int(int.MinValue, int.MinValue);
            foreach (var c in cells)
            {
                min = Vector2Int.Min(min, c);
                max = Vector2Int.Max(max, c);
            }
            min -= Vector2Int.one * 2;
            max += Vector2Int.one * 2;
            var w = max.x - min.x + 1;
            var h = max.y - min.y + 1;

            // membership, softened: half the cell itself, half its eight neighbours
            var field = new float[w * h];
            for (var z = 1; z < h - 1; z++)
            for (var x = 1; x < w - 1; x++)
            {
                var around = 0f;
                for (var dz = -1; dz <= 1; dz++)
                for (var dx = -1; dx <= 1; dx++)
                    if ((dx != 0 || dz != 0) && cells.Contains(new Vector2Int(min.x + x + dx, min.y + z + dz))) around += 1f;
                field[z * w + x] = (cells.Contains(new Vector2Int(min.x + x, min.y + z)) ? 0.5f : 0f) + around / 16f;
            }

            var heights = new Dictionary<int, float>();
            float HeightAt(int x, int z)
            {
                var key = z * w + x;
                if (heights.TryGetValue(key, out var y)) return y;
                var c = grid.GetFootprintCenterWorld(new Vector2Int(min.x + x, min.y + z), Vector2Int.one);
                var ray = new Ray(new Vector3(c.x, c.y + 50f, c.z), Vector3.down);
                y = applier.TryRaycastGround(ray, out var hit) ? hit.point.y : grid.GroundHeight;
                heights[key] = y;
                return y;
            }
            Vector3 Corner(int x, int z)
            {
                var c = grid.GetFootprintCenterWorld(new Vector2Int(min.x + x, min.y + z), Vector2Int.one);
                return new Vector3(c.x, HeightAt(x, z) + 0.035f, c.z);
            }

            var dark = ArchipelagoMeshes.PaletteUvOf(ArchipelagoMeshes.SoilDark);
            var light = ArchipelagoMeshes.PaletteUvOf(ArchipelagoMeshes.SoilLight);
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            var polygon = new List<Vector3>(8);
            const float Level = 0.5f;
            var sx = new[] { 0, 1, 1, 0 };
            var sz = new[] { 0, 0, 1, 1 };
            for (var z = 0; z < h - 1; z++)
            for (var x = 0; x < w - 1; x++)
            {
                var inside = 0;
                for (var k = 0; k < 4; k++)
                    if (field[(z + sz[k]) * w + x + sx[k]] >= Level) inside++;
                if (inside == 0) continue;
                // the square's piece inside the outline: its corners inside, and where an edge crosses
                polygon.Clear();
                for (var k = 0; k < 4; k++)
                {
                    int ax = x + sx[k], az = z + sz[k], bx = x + sx[(k + 1) % 4], bz = z + sz[(k + 1) % 4];
                    var va = field[az * w + ax];
                    var vb = field[bz * w + bx];
                    var pa = Corner(ax, az);
                    if (va >= Level) polygon.Add(pa);
                    if ((va >= Level) != (vb >= Level)) polygon.Add(Vector3.Lerp(pa, Corner(bx, bz), (Level - va) / (vb - va)));
                }
                if (polygon.Count < 3) continue;
                // the rim: squares the outline crosses, or touching it, take the lighter soil
                var uv = inside == 4 && Deep(field, w, h, x, z) ? dark : light;
                var s = vertices.Count;
                foreach (var v in polygon)
                {
                    vertices.Add(v);
                    uvs.Add(uv);
                }
                // wound clockwise seen from above (Unity's front face): the corners run x+, then z+
                for (var k = 1; k < polygon.Count - 1; k++) triangles.AddRange(new[] { s, s + k + 1, s + k });
            }
            if (triangles.Count == 0) return null;
            var mesh = new Mesh { name = "FertileLand", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject("FertileLand");
            // the vertices are in world space: keep the object where the world's origin is
            go.transform.SetParent(parent, true);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = terrain;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            SeasonStyle.DressTerrain(go);
            return go;
        }

        /// <summary>The cells joined to `start` (eight ways round), out of `cells`.</summary>
        public static HashSet<Vector2Int> PatchAt(HashSet<Vector2Int> cells, Vector2Int start)
        {
            var patch = new HashSet<Vector2Int>();
            if (cells == null || !cells.Contains(start)) return patch;
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            patch.Add(start);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                for (var dz = -1; dz <= 1; dz++)
                for (var dx = -1; dx <= 1; dx++)
                {
                    var next = c + new Vector2Int(dx, dz);
                    if (cells.Contains(next) && patch.Add(next)) queue.Enqueue(next);
                }
            }
            return patch;
        }

        private static GameObject _flash;

        /// <summary>
        /// Lights up a patch's edge for a few seconds (FertileFlash): a gold ribbon traced on the
        /// same softened outline the soil is drawn with, a hand's breadth over the ground. One at a
        /// time -- a new tap takes the last one down.
        /// </summary>
        public static GameObject Flash(HashSet<Vector2Int> patch, MeshMapApplier applier)
        {
            if (_flash != null) Object.Destroy(_flash);
            var grid = GridManager.Instance;
            if (patch == null || patch.Count == 0 || grid == null) return null;
            var min = new Vector2Int(int.MaxValue, int.MaxValue);
            var max = new Vector2Int(int.MinValue, int.MinValue);
            foreach (var c in patch)
            {
                min = Vector2Int.Min(min, c);
                max = Vector2Int.Max(max, c);
            }
            min -= Vector2Int.one * 2;
            max += Vector2Int.one * 2;
            var w = max.x - min.x + 1;
            var h = max.y - min.y + 1;
            var field = new float[w * h];
            for (var z = 1; z < h - 1; z++)
            for (var x = 1; x < w - 1; x++)
            {
                var around = 0f;
                for (var dz = -1; dz <= 1; dz++)
                for (var dx = -1; dx <= 1; dx++)
                    if ((dx != 0 || dz != 0) && patch.Contains(new Vector2Int(min.x + x + dx, min.y + z + dz))) around += 1f;
                field[z * w + x] = (patch.Contains(new Vector2Int(min.x + x, min.y + z)) ? 0.5f : 0f) + around / 16f;
            }
            Vector3 Point(float x, float z)
            {
                var c = grid.GetFootprintCenterWorld(new Vector2Int(min.x, min.y), Vector2Int.one) + new Vector3(x, 0f, z) * grid.CellSize;
                var ray = new Ray(new Vector3(c.x, c.y + 50f, c.z), Vector3.down);
                c.y = (applier != null && applier.TryRaycastGround(ray, out var hit) ? hit.point.y : grid.GroundHeight) + 0.07f;
                return c;
            }

            const float Level = 0.5f;
            const float Half = 0.09f;
            var sx = new[] { 0, 1, 1, 0 };
            var sz = new[] { 0, 0, 1, 1 };
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var crossings = new List<Vector2>(4);
            for (var z = 0; z < h - 1; z++)
            for (var x = 0; x < w - 1; x++)
            {
                crossings.Clear();
                for (var k = 0; k < 4; k++)
                {
                    int ax = x + sx[k], az = z + sz[k], bx = x + sx[(k + 1) % 4], bz = z + sz[(k + 1) % 4];
                    var va = field[az * w + ax];
                    var vb = field[bz * w + bx];
                    if ((va >= Level) == (vb >= Level)) continue;
                    var t = (Level - va) / (vb - va);
                    crossings.Add(new Vector2(Mathf.Lerp(ax, bx, t), Mathf.Lerp(az, bz, t)));
                }
                for (var k = 0; k + 1 < crossings.Count; k += 2)
                {
                    var a = Point(crossings[k].x, crossings[k].y);
                    var b = Point(crossings[k + 1].x, crossings[k + 1].y);
                    var along = b - a;
                    along.y = 0f;
                    if (along.sqrMagnitude < 1e-6f) continue;
                    var side = Vector3.Cross(Vector3.up, along.normalized) * Half;
                    // a little past each end, so the pieces overlap into one line
                    var reach = along.normalized * Half;
                    var s = vertices.Count;
                    vertices.Add(a - reach - side);
                    vertices.Add(a - reach + side);
                    vertices.Add(b + reach + side);
                    vertices.Add(b + reach - side);
                    // both windings: the crossings come in either order
                    triangles.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3, s, s + 2, s + 1, s, s + 3, s + 2 });
                }
            }
            if (triangles.Count == 0) return null;
            var mesh = new Mesh { name = "FertileFlash", indexFormat = vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject("FertileFlash");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(RuntimeShaders.Unlit) { name = "FertileFlash", color = FertileFlash.Gold };
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            go.AddComponent<FertileFlash>();
            _flash = go;
            return go;
        }

        /// <summary>True when every lattice point round the square is well inside: not the rim.</summary>
        private static bool Deep(float[] field, int w, int h, int x, int z)
        {
            for (var dz = -1; dz <= 2; dz++)
            for (var dx = -1; dx <= 2; dx++)
            {
                int px = x + dx, pz = z + dz;
                if (px < 0 || pz < 0 || px >= w || pz >= h || field[pz * w + px] < 0.5f) return false;
            }
            return true;
        }
    }
}
