using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Maps.Archipelago
{
    /// <summary>
    /// The generated archipelago as meshes, built in the game the way the Blender maps are built
    /// offline (Tools/blender/map3.py): the PLAIN at the ground's height, every island's coast
    /// dipping to meet the sea (the cells with no plain are the water -- that is how the NavMesh and
    /// every land test tell them apart); the RELIEF -- hills and mountains -- as its own mesh, rising
    /// from the plain's edge, out of the NavMesh; one colour per metre square from the terrain's
    /// palette (Tools/blender/terrain.py), so the seasons repaint it like every other map; and a sea
    /// plane under it all. Both land meshes are cut into chunks, so the ones off screen are not drawn.
    /// </summary>
    public static class ArchipelagoMeshes
    {
        private const int Chunk = 50;
        private const float ShoreDrop = -0.42f;    // the coast meets the sea just above it (the water is at -0.5)
        public const float WaterLevel = -0.5f;
        private const float ReliefTop = 12f;      // metres over the plain at the highest summit

        // Tools/blender/terrain.py TERRAIN_PALETTE, 8 x 6 cells: the index of each colour used here
        private const int Cols = 8, Rows = 6;
        private const int GrassLight = 0, Grass = 1, GrassMid = 2, GrassLush = 3, GrassDark = 4, ForestFloor = 5, Moss = 6, GrassDry = 7;
        private const int Sand = 8, SandWet = 9, Dirt = 10;
        public const int SoilLight = 10, SoilDark = 11;
        public const int FoamIndex = Foam, WaterDeepIndex = WaterDeep, RockMidIndex = RockMid, RockDarkIndex = RockDark;
        private const int Pebble = 15, RockLight = 16, RockStone = 17, RockMid = 18, RockDark = 19, RockDeep = 20;
        private const int WaterShallow = 21, Water = 22, WaterDeep = 23, Foam = 24;
        private const int GrassB = 40, GrassLightB = 41, GrassMidB = 42, GrassLushB = 43, GrassDarkB = 44, ForestFloorB = 45, GrassDryB = 46, Stony = 47;

        /// <summary>The plain and the relief, each a parent of chunk children; `origin` is the world position of cell (0, 0)'s corner.</summary>
        public static (GameObject ground, GameObject relief) Build(ArchipelagoMap map, Vector3 origin, Material terrain, Transform parent)
        {
            var n = map.Size;
            var toWater = DistanceToWater(map);
            var tone = Noise.Fbm(n, new SeedRandom(map.Seed * 31 + 7), n / 12, 3);
            var alt = Noise.Fbm(n, new SeedRandom(map.Seed * 31 + 11), n / 40, 2);

            var ground = new GameObject("ArchipelagoGround");
            ground.transform.SetParent(parent, false);
            var relief = new GameObject("ArchipelagoRelief");
            relief.transform.SetParent(parent, false);

            for (var cz = 0; cz < n; cz += Chunk)
            for (var cx = 0; cx < n; cx += Chunk)
            {
                var plain = new Builder();
                var high = new Builder();
                // the level squares of one colour merge into rectangles: most of an island is flat,
                // and a metre square each would be four times the triangles of a Blender map
                var flat = new FlatGrid(cx, cz, Chunk, Chunk);
                for (var z = cz; z < Mathf.Min(n, cz + Chunk); z++)
                for (var x = cx; x < Mathf.Min(n, cx + Chunk); x++)
                {
                    var i = z * n + x;
                    if (!map.Land[i]) continue;
                    if (map.IsRelief(i))
                    {
                        high.Quad(origin, x, z,
                            ReliefHeight(map, x, z), ReliefHeight(map, x + 1, z), ReliefHeight(map, x + 1, z + 1), ReliefHeight(map, x, z + 1),
                            ReliefColour(map.Height[i], alt[i]));
                        continue;
                    }
                    float h00 = PlainHeight(map, toWater, x, z), h10 = PlainHeight(map, toWater, x + 1, z);
                    float h11 = PlainHeight(map, toWater, x + 1, z + 1), h01 = PlainHeight(map, toWater, x, z + 1);
                    var colour = PlainColour(map, toWater, i, tone[i], alt[i]);
                    if (h00 == 0f && h10 == 0f && h11 == 0f && h01 == 0f) flat.Set(x, z, colour);
                    else plain.Quad(origin, x, z, h00, h10, h11, h01, colour);
                }
                flat.Merge(plain, origin, 0f);
                plain.Emit($"Ground-{cx / Chunk:00}-{cz / Chunk:00}", ground.transform, terrain);
                high.Emit($"Relief-{cx / Chunk:00}-{cz / Chunk:00}", relief.transform, terrain);
            }
            return (ground, relief);
        }

        /// <summary>
        /// The sea, a flat sheet in metre cells merged into rectangles, in the depth bands of the Blender maps' water: shallow
        /// at the coast, then blue, then deep; foam where the calm sea meets the storm, and the storm
        /// itself deep and dark, out to past the world's edge.
        /// </summary>
        public static GameObject Sea(ArchipelagoMap map, Vector3 origin, Material terrain, Transform parent, bool bands = true)
        {
            const int margin = 60;
            const int block = Chunk * 2;
            var n = map.Size;
            var toLand = DistanceToLand(map);
            var sea = new GameObject("ArchipelagoSea");
            sea.transform.SetParent(parent, false);
            for (var cz = -margin; cz < n + margin; cz += block)
            for (var cx = -margin; cx < n + margin; cx += block)
            {
                var b = new Builder();
                var flat = new FlatGrid(cx, cz, block, block);
                for (var z = cz; z < Mathf.Min(n + margin, cz + block); z++)
                for (var x = cx; x < Mathf.Min(n + margin, cx + block); x++)
                {
                    var inside = x >= 0 && z >= 0 && x < n && z < n;
                    if (bands && inside && map.Land[z * n + x]) continue;
                    var edge = Mathf.Min(Mathf.Min(x, z), Mathf.Min(n - 1 - x, n - 1 - z));
                    int colour;
                    // the storm beyond the ships' limit is StormEdge's, drawn over this
                    if (edge < ArchipelagoGenerator.Storm) colour = WaterDeep;
                    else
                    {
                        var near = inside ? toLand[z * n + x] : int.MaxValue;
                        colour = !bands ? WaterDeep : near <= 4 ? WaterShallow : near <= 14 ? Water : WaterDeep;
                    }
                    flat.Set(x, z, colour);
                }
                // under the islands' own coastal water when they bring it
                flat.Merge(b, origin, bands ? WaterLevel : WaterLevel - 0.04f);
                b.Emit($"Sea-{cx}-{cz}", sea.transform, terrain);
            }
            return sea;
        }

        private static int[] DistanceToLand(ArchipelagoMap map)
        {
            var n = map.Size;
            var dist = new int[n * n];
            var queue = new Queue<int>();
            for (var i = 0; i < dist.Length; i++)
            {
                if (map.Land[i])
                {
                    dist[i] = 0;
                    queue.Enqueue(i);
                }
                else dist[i] = int.MaxValue;
            }
            while (queue.Count > 0)
            {
                var i = queue.Dequeue();
                int x = i % n, z = i / n;
                var next = dist[i] + 1;
                if (x > 0 && dist[i - 1] > next) { dist[i - 1] = next; queue.Enqueue(i - 1); }
                if (x < n - 1 && dist[i + 1] > next) { dist[i + 1] = next; queue.Enqueue(i + 1); }
                if (z > 0 && dist[i - n] > next) { dist[i - n] = next; queue.Enqueue(i - n); }
                if (z < n - 1 && dist[i + n] > next) { dist[i + n] = next; queue.Enqueue(i + n); }
            }
            return dist;
        }

        // -- heights ----------------------------------------------------------------------------------

        /// <summary>A plain corner: level, except where it touches the sea (down to meet it) and on a beach (a slope three metres deep).</summary>
        private static float PlainHeight(ArchipelagoMap map, int[] toWater, int vx, int vz)
        {
            var n = map.Size;
            var nearest = int.MaxValue;
            var beach = false;
            for (var dz = -1; dz <= 0; dz++)
            for (var dx = -1; dx <= 0; dx++)
            {
                int x = vx + dx, z = vz + dz;
                if (x < 0 || z < 0 || x >= n || z >= n) { nearest = 0; continue; }
                var i = z * n + x;
                nearest = Mathf.Min(nearest, map.Land[i] ? toWater[i] : 0);
                beach |= map.Land[i] && map.Beach[i];
            }
            if (nearest == 0) return ShoreDrop;
            if (beach && nearest <= 3) return Mathf.Lerp(ShoreDrop * 0.6f, 0f, (nearest - 1) / 2f);
            return 0f;
        }

        /// <summary>A relief corner: level with the plain where it meets the plain, rising with the land's height inside.</summary>
        private static float ReliefHeight(ArchipelagoMap map, int vx, int vz)
        {
            var n = map.Size;
            var sum = 0f;
            var count = 0;
            for (var dz = -1; dz <= 0; dz++)
            for (var dx = -1; dx <= 0; dx++)
            {
                int x = vx + dx, z = vz + dz;
                if (x < 0 || z < 0 || x >= n || z >= n) return 0f;
                var i = z * n + x;
                if (!map.IsRelief(i)) return map.Land[i] ? 0f : ShoreDrop;
                sum += map.Height[i];
                count++;
            }
            var h = sum / count;
            var t = Mathf.Clamp01((h - ArchipelagoGenerator.ReliefFrom) / (1f - ArchipelagoGenerator.ReliefFrom));
            return Mathf.Pow(t, 0.8f) * ReliefTop + 0.3f;
        }

        // -- colours ----------------------------------------------------------------------------------

        private static int PlainColour(ArchipelagoMap map, int[] toWater, int i, float tone, float alt)
        {
            var b = alt > 0.5f;
            if (map.Cliff[i]) return toWater[i] <= 1 ? RockDark : RockMid;
            if (map.Beach[i]) return toWater[i] <= 1 ? SandWet : Sand;
            if (map.Forest[i]) return tone > 0.5f ? (b ? ForestFloorB : ForestFloor) : (b ? GrassDarkB : GrassDark);
            if (map.Stony[i]) return alt > 0.55f ? Stony : alt > 0.4f ? Pebble : GrassMidB;
            if (tone > 0.66f) return b ? GrassLushB : GrassLush;
            if (tone < 0.3f) return b ? GrassDryB : GrassDry;
            if (tone < 0.42f) return b ? GrassLightB : GrassLight;
            if (tone > 0.55f) return b ? GrassMidB : GrassMid;
            return b ? GrassB : Grass;
        }

        private static int ReliefColour(float h, float alt)
        {
            if (h < 0.52f) return alt > 0.5f ? Moss : GrassDark;
            if (h < 0.62f) return alt > 0.5f ? RockLight : Dirt;
            if (h < 0.74f) return RockStone;
            if (h < 0.85f) return RockMid;
            return alt > 0.5f ? RockDark : RockDeep;
        }

        /// <summary>
        /// An island's coastal water with its blue band painted the shallows' light turquoise
        /// (the user, 2026-10-31): the light water is exactly the strip a water building may stand
        /// on (MeshMapApplier's placement zone, 6 m out), and what is beyond it reads as deep sea.
        /// A copy per mesh; the shared asset is left alone.
        /// </summary>
        public static Mesh LightenCoast(Mesh source)
        {
            if (source == null) return null;
            var mesh = Object.Instantiate(source);
            mesh.name = source.name + "-Light";
            var uvs = new List<Vector2>();
            mesh.GetUVs(0, uvs);
            var from = PaletteUv(Water);
            var to = PaletteUv(WaterShallow);
            var halfCell = new Vector2(0.5f / Cols, 0.5f / Rows);
            for (var i = 0; i < uvs.Count; i++)
            {
                var d = uvs[i] - from;
                if (Mathf.Abs(d.x) < halfCell.x && Mathf.Abs(d.y) < halfCell.y) uvs[i] = to + d;
            }
            mesh.SetUVs(0, uvs);
            return mesh;
        }

        /// <summary>Where one of the palette's colours sits on the terrain texture (for things drawn in the ground's own material).</summary>
        public static Vector2 PaletteUvOf(int index) => PaletteUv(index);

        /// <summary>True for a uv on one of the palette's grasses (not sand, rock, dirt or water).</summary>
        public static bool IsGrassUv(Vector2 uv)
        {
            var col = Mathf.Clamp(Mathf.FloorToInt(uv.x * Cols), 0, Cols - 1);
            var row = Mathf.Clamp(Mathf.FloorToInt((1f - uv.y) * Rows), 0, Rows - 1);
            var index = row * Cols + col;
            return (index >= GrassLight && index <= GrassDry) || (index >= GrassB && index <= GrassDryB);
        }

        private static Vector2 PaletteUv(int index)
        {
            int col = index % Cols, row = index / Cols;
            return new Vector2((col + 0.5f) / Cols, 1f - (row + 0.5f) / Rows);
        }

        private static int[] DistanceToWater(ArchipelagoMap map)
        {
            var n = map.Size;
            var dist = new int[n * n];
            var queue = new Queue<int>();
            for (var i = 0; i < dist.Length; i++)
            {
                if (map.Land[i]) dist[i] = int.MaxValue;
                else
                {
                    dist[i] = 0;
                    queue.Enqueue(i);
                }
            }
            while (queue.Count > 0)
            {
                var i = queue.Dequeue();
                int x = i % n, z = i / n;
                var next = dist[i] + 1;
                if (x > 0 && dist[i - 1] > next) { dist[i - 1] = next; queue.Enqueue(i - 1); }
                if (x < n - 1 && dist[i + 1] > next) { dist[i + 1] = next; queue.Enqueue(i + 1); }
                if (z > 0 && dist[i - n] > next) { dist[i - n] = next; queue.Enqueue(i - n); }
                if (z < n - 1 && dist[i + n] > next) { dist[i + n] = next; queue.Enqueue(i + n); }
            }
            return dist;
        }

        /// <summary>A block of level cells by colour, merged greedily into as few rectangles as cover it.</summary>
        private sealed class FlatGrid
        {
            private readonly int _x0, _z0, _w, _h;
            private readonly int[] _colour;

            public FlatGrid(int x0, int z0, int w, int h)
            {
                _x0 = x0;
                _z0 = z0;
                _w = w;
                _h = h;
                _colour = new int[w * h];
                for (var i = 0; i < _colour.Length; i++) _colour[i] = -1;
            }

            public void Set(int x, int z, int colour) => _colour[(z - _z0) * _w + (x - _x0)] = colour;

            public void Merge(Builder into, Vector3 origin, float height)
            {
                for (var z = 0; z < _h; z++)
                for (var x = 0; x < _w; x++)
                {
                    var c = _colour[z * _w + x];
                    if (c < 0) continue;
                    var w = 1;
                    while (x + w < _w && _colour[z * _w + x + w] == c) w++;
                    var h = 1;
                    while (z + h < _h)
                    {
                        var row = true;
                        for (var k = 0; k < w && row; k++) row = _colour[(z + h) * _w + x + k] == c;
                        if (!row) break;
                        h++;
                    }
                    for (var dz = 0; dz < h; dz++)
                    for (var dx = 0; dx < w; dx++) _colour[(z + dz) * _w + x + dx] = -1;
                    into.Rect(origin, _x0 + x, _z0 + z, w, h, height, c);
                }
            }
        }

        /// <summary>Quads with their own corners (flat colour per square), into one chunk mesh.</summary>
        private sealed class Builder
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Vector2> _uvs = new List<Vector2>();
            private readonly List<int> _triangles = new List<int>();

            public void Quad(Vector3 origin, int x, int z, float h00, float h10, float h11, float h01, int colour, int size = 1)
            {
                var start = _vertices.Count;
                _vertices.Add(origin + new Vector3(x, h00, z));
                _vertices.Add(origin + new Vector3(x + size, h10, z));
                _vertices.Add(origin + new Vector3(x + size, h11, z + size));
                _vertices.Add(origin + new Vector3(x, h01, z + size));
                var uv = PaletteUv(colour);
                for (var k = 0; k < 4; k++) _uvs.Add(uv);
                // split along the flatter diagonal, so a slope does not fold
                if (Mathf.Abs(h00 - h11) <= Mathf.Abs(h10 - h01))
                {
                    _triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
                }
                else
                {
                    _triangles.AddRange(new[] { start, start + 3, start + 1, start + 1, start + 3, start + 2 });
                }
            }

            /// <summary>A level rectangle of one colour, w by h metres.</summary>
            public void Rect(Vector3 origin, int x, int z, int w, int h, float height, int colour)
            {
                var start = _vertices.Count;
                _vertices.Add(origin + new Vector3(x, height, z));
                _vertices.Add(origin + new Vector3(x + w, height, z));
                _vertices.Add(origin + new Vector3(x + w, height, z + h));
                _vertices.Add(origin + new Vector3(x, height, z + h));
                var uv = PaletteUv(colour);
                for (var k = 0; k < 4; k++) _uvs.Add(uv);
                _triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
            }

            public void Emit(string name, Transform parent, Material material)
            {
                if (_vertices.Count == 0) return;
                var mesh = new Mesh { name = name, indexFormat = _vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(_vertices);
                mesh.SetUVs(0, _uvs);
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(parent, false);
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                go.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
        }
    }
}
