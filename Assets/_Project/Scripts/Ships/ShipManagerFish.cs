using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Maps.Archipelago;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Ships
{
    /// <summary>
    /// The archipelago's fish (the user, 2026-10-28): shoals you can see on the shore water -- a
    /// knot of dark backs turning under the surface. A Fisher's Hut with one in reach catches
    /// fish_shoal_bonus more; what it catches beyond its own share comes out of the shoal's stock,
    /// and a shoal fished out is gone -- after fish_shoal_respawn_seconds one turns up somewhere
    /// else. Two of the first are by the start island, where the first huts go.
    /// </summary>
    public partial class ShipManager
    {
        public const string FisherHutId = "FisherHut";
        private const int FishPerShoal = 9;
        private const float FishSeconds = 2.4f;     // between one fish leaping and the next, on average

        public sealed class Shoal
        {
            public Vector3 At;
            public int Stock;
            public float RespawnIn;
            public GameObject Visual;
            /// <summary>The fish of its visual, each on its own circuit (radius, speed, phase); and which one is in the air.</summary>
            public Transform[] Fish;
            public Vector3[] Swim;
            public int Leaper = -1;
            public float LeapClock;
            public bool Alive => Stock > 0;
        }

        private readonly List<Shoal> _shoals = new List<Shoal>();
        private Mesh _fishMesh;
        private Material _fishMaterial;
        private Material _glintMaterial;

        public IReadOnlyList<Shoal> Shoals => _shoals;

        /// <summary>The first shoals of a new map, from its seed: two by the start island, the rest along any shore.</summary>
        private void SeedShoals(ArchipelagoMap world, Vector3 origin)
        {
            ClearShoals();
            var count = BalanceConfig.Instance != null ? BalanceConfig.Instance.FishShoalCount : 12;
            // the tutorial: a few, for the look (the user, 2026-11-05: too many)
            if (Tutorial.TutorialMode.IsActive && BalanceConfig.Instance != null) count = Mathf.Min(count, BalanceConfig.Instance.TutorialFishShoals);
            var rng = new SeedRandom(world.Seed * 31 + 7);
            var start = origin + new Vector3(world.StartCell.x + 0.5f, 0f, world.StartCell.y + 0.5f);
            for (var k = 0; k < count; k++)
            {
                var shoal = new Shoal { Stock = ShoalStock() };
                if (k < 2 ? TryShoreSea(start, 15f, 60f, rng, out shoal.At) : TryShoreSea(origin + new Vector3(world.Size * 0.5f, 0f, world.Size * 0.5f), 0f, world.Size * 0.48f, rng, out shoal.At))
                {
                    shoal.Visual = BuildShoal(shoal.At);
                    FillShoal(shoal);
                    _shoals.Add(shoal);
                }
            }
        }

        private static int ShoalStock() => BalanceConfig.Instance != null ? BalanceConfig.Instance.FishShoalStock : 60;

        /// <summary>Sea a ship can reach within a fishing hut's reach of a beach, at a random spot in the ring round `centre`.</summary>
        private bool TryShoreSea(Vector3 centre, float minRadius, float maxRadius, SeedRandom rng, out Vector3 point)
        {
            point = default;
            // right by the shore (the user, 2026-11-01: «ближе к берегу, чтобы был смысл в рыбацкой хижине»)
            var reach = BalanceConfig.Instance != null ? BalanceConfig.Instance.FishShoalShoreMeters : 4f;
            for (var attempt = 0; attempt < 400; attempt++)
            {
                var a = rng.Range(0f, Mathf.PI * 2f);
                var r = rng.Range(minRadius, maxRadius);
                var probe = centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                if (!TrySampleSea(probe, 2f, out var sea) || !TrySampleLand(sea, reach, out _)) continue;
                // on the light coastal water only (the user, 2026-10-31): a shoal far out at sea is
                // no use to a hut on the shore
                var map = Maps.MeshMapApplier.Instance;
                var grid = Grid.GridManager.Instance;
                if (map != null && grid != null && !map.IsWaterPlacementZone(grid.WorldToCell(sea))) continue;
                // never side by side with another shoal (the user, 2026-11-01: two lay in one ring)
                if (TooNearAShoal(sea)) continue;
                point = sea;
                return true;
            }
            return false;
        }

        /// <summary>Another shoal (live or waiting to come back) within fish_shoal_min_spacing metres.</summary>
        private bool TooNearAShoal(Vector3 at)
        {
            var spacing = BalanceConfig.Instance != null ? BalanceConfig.Instance.FishShoalMinSpacing : 14f;
            foreach (var shoal in _shoals)
            {
                if (shoal == null) continue;
                var d = new Vector2(at.x - shoal.At.x, at.z - shoal.At.z);
                if (d.sqrMagnitude < spacing * spacing) return true;
            }
            return false;
        }

        /// <summary>The live shoal under a point on the water, within its fish's circle and a little more; null if none.</summary>
        public Shoal ShoalAt(Vector3 point)
        {
            // the tutorial's fish take no taps: one stole the tap meant for the soldier (the user, 2026-11-05)
            if (Tutorial.TutorialMode.IsActive) return null;
            Shoal best = null;
            var bestD = ShoalTapRadius;
            foreach (var shoal in _shoals)
            {
                if (!shoal.Alive || shoal.Visual == null) continue;
                var d = new Vector2(point.x - shoal.At.x, point.z - shoal.At.z).magnitude;
                if (d > bestD) continue;
                bestD = d;
                best = shoal;
            }
            return best;
        }

        // the fish swim up to 2.2 m out (FillShoal); a tap a little wider still finds them
        private const float ShoalTapRadius = 3.5f;

        private static GameObject _shoalFlash;

        /// <summary>
        /// A tap on a shoal (the user, 2026-11-01: like fertile land): a gold ring round its fish,
        /// pulsing for a few seconds (FertileFlash), and «Обнаружен косяк рыб» in the feed.
        /// </summary>
        public void FlashShoal(Shoal shoal)
        {
            if (shoal == null) return;
            if (_shoalFlash != null) Destroy(_shoalFlash);
            const int Sides = 40;
            const float Radius = 3f;
            const float Half = 0.12f;
            var vertices = new Vector3[Sides * 2];
            var triangles = new int[Sides * 12];
            for (var k = 0; k < Sides; k++)
            {
                var a = k * Mathf.PI * 2f / Sides;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                vertices[k * 2] = dir * (Radius - Half);
                vertices[k * 2 + 1] = dir * (Radius + Half);
                int s = k * 2, n = (k + 1) % Sides * 2, t = k * 12;
                // both windings, so it shows from any side
                triangles[t] = s; triangles[t + 1] = n; triangles[t + 2] = s + 1;
                triangles[t + 3] = s + 1; triangles[t + 4] = n; triangles[t + 5] = n + 1;
                triangles[t + 6] = s; triangles[t + 7] = s + 1; triangles[t + 8] = n;
                triangles[t + 9] = s + 1; triangles[t + 10] = n + 1; triangles[t + 11] = n;
            }
            var mesh = new Mesh { name = "ShoalFlash", vertices = vertices, triangles = triangles };
            mesh.RecalculateBounds();
            var go = new GameObject("ShoalFlash");
            go.transform.position = new Vector3(shoal.At.x, WaterLevel + 0.08f, shoal.At.z);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(RuntimeShaders.Unlit) { name = "ShoalFlash", color = Maps.Archipelago.FertileFlash.Gold };
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            go.AddComponent<Maps.Archipelago.FertileFlash>();
            _shoalFlash = go;
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_shoal_found"), EventSeverity.Good, EventGlyph.Star,
                FisherHutId, shoal.At));
        }

        private static float HutReach() => BalanceConfig.Instance != null ? BalanceConfig.Instance.FishHutReachMeters : 16f;

        /// <summary>The live shoal nearest a hut within its reach, or null.</summary>
        public Shoal ShoalNear(Vector3 at)
        {
            Shoal best = null;
            var bestSq = HutReach() * HutReach();
            foreach (var shoal in _shoals)
            {
                if (!shoal.Alive) continue;
                var d = shoal.At - at;
                var sq = d.x * d.x + d.z * d.z;
                if (sq >= bestSq) continue;
                bestSq = sq;
                best = shoal;
            }
            return best;
        }

        /// <summary>What a Fisher's Hut here catches over its own share: 1 + fish_shoal_bonus by a live shoal, else 1.</summary>
        public float FishMultiplier(Vector3 hut)
        {
            if (ShoalNear(hut) == null) return 1f;
            return 1f + (BalanceConfig.Instance != null ? BalanceConfig.Instance.FishShoalBonus : 1f);
        }

        /// <summary>Takes a catch out of the shoal by this hut; a shoal fished out is gone, to turn up elsewhere later.</summary>
        public void TakeFish(Vector3 hut, int amount)
        {
            var shoal = ShoalNear(hut);
            if (shoal == null || amount <= 0) return;
            shoal.Stock = Mathf.Max(0, shoal.Stock - amount);
            if (shoal.Alive) return;
            if (shoal.Visual != null) Destroy(shoal.Visual);
            shoal.Visual = null;
            shoal.RespawnIn = BalanceConfig.Instance != null ? BalanceConfig.Instance.FishShoalRespawnSeconds : 180f;
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_shoal_gone"), EventSeverity.Warning, EventGlyph.Alert, FisherHutId, shoal.At));
        }

        private void TickShoals()
        {
            var t = Time.deltaTime;
            foreach (var shoal in _shoals)
            {
                if (shoal.Visual != null)
                {
                    SwimShoal(shoal, t);
                    continue;
                }
                if (shoal.Alive) continue;
                shoal.RespawnIn -= t;
                if (shoal.RespawnIn > 0f) continue;
                // the fish come back somewhere else on the archipelago's shores
                var grid = Grid.GridManager.Instance;
                var half = grid != null ? grid.GridSize.x * grid.CellSize * 0.48f : 200f;
                var rng = new SeedRandom(Random.Range(int.MinValue, int.MaxValue));
                if (!TryShoreSea(Vector3.zero, 0f, half, rng, out var at))
                {
                    shoal.RespawnIn = 30f;
                    continue;
                }
                shoal.At = at;
                shoal.Stock = ShoalStock();
                shoal.Visual = BuildShoal(at);
                FillShoal(shoal);
            }
        }

        /// <summary>A save's shoals: where each is, its stock, the wait of one fished out.</summary>
        public void RestoreShoals(List<Saving.ShoalEntry> entries)
        {
            ClearShoals();
            foreach (var e in entries)
            {
                var shoal = new Shoal { At = new Vector3(e.x, WaterLevel, e.z), Stock = e.stock, RespawnIn = e.respawnIn };
                if (shoal.Alive)
                {
                    shoal.Visual = BuildShoal(shoal.At);
                    FillShoal(shoal);
                }
                _shoals.Add(shoal);
            }
        }

        private void ClearShoals()
        {
            foreach (var shoal in _shoals) if (shoal.Visual != null) Destroy(shoal.Visual);
            _shoals.Clear();
        }

        /// <summary>
        /// A shoal: boxy fish (the game's look -- the user, 2026-10-31: «прямоугольные модельки рыб»)
        /// swimming round where the fish are, each on its own circuit at the surface, and every
        /// couple of seconds one leaping out -- so a good fishing spot reads from far off.
        /// </summary>
        private GameObject BuildShoal(Vector3 at)
        {
            if (_fishMesh == null)
            {
                _fishMesh = BuildFishMesh();
                _fishMaterial = new Material(RuntimeShaders.Lit) { color = new Color(0.55f, 0.66f, 0.74f) };
                _glintMaterial = new Material(RuntimeShaders.Lit) { color = new Color(0.2f, 0.32f, 0.44f) };
            }
            var go = new GameObject("FishShoal");
            go.transform.position = new Vector3(at.x, WaterLevel, at.z);
            return go;
        }

        private void FillShoal(Shoal shoal)
        {
            if (shoal.Visual == null) return;
            var rng = new SeedRandom(Mathf.RoundToInt(shoal.At.x * 31f + shoal.At.z * 17f));
            shoal.Fish = new Transform[FishPerShoal];
            shoal.Swim = new Vector3[FishPerShoal];
            for (var k = 0; k < FishPerShoal; k++)
            {
                var fish = new GameObject("Fish");
                fish.transform.SetParent(shoal.Visual.transform, false);
                fish.AddComponent<MeshFilter>().sharedMesh = _fishMesh;
                var renderer = fish.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = new[] { _fishMaterial, _glintMaterial };
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                var scale = rng.Range(0.85f, 1.2f);
                fish.transform.localScale = Vector3.one * scale;
                // radius, angular speed (either way round), phase
                shoal.Swim[k] = new Vector3(rng.Range(0.6f, 2.2f), rng.Range(0.5f, 1.1f) * (k % 3 == 0 ? -1f : 1f), rng.Range(0f, Mathf.PI * 2f));
                shoal.Fish[k] = fish.transform;
            }
            shoal.LeapClock = rng.Range(0.5f, FishSeconds);
            SwimShoal(shoal, 0f);
        }

        /// <summary>Moves every fish along its circuit, nose first, and lifts the leaper on its arc.</summary>
        private static void SwimShoal(Shoal shoal, float dt)
        {
            if (shoal.Fish == null) return;
            var time = Time.time;
            shoal.LeapClock -= dt;
            if (shoal.LeapClock <= 0f)
            {
                shoal.Leaper = Random.Range(0, shoal.Fish.Length);
                shoal.LeapClock = FishSeconds * Random.Range(0.6f, 1.4f);
            }
            for (var k = 0; k < shoal.Fish.Length; k++)
            {
                var fish = shoal.Fish[k];
                if (fish == null) continue;
                var swim = shoal.Swim[k];
                var a = swim.z + time * swim.y;
                // a lazy wobble in and out, so the circle never looks drawn with compasses
                var r = swim.x * (1f + 0.18f * Mathf.Sin(time * 0.7f + swim.z * 3f));
                var pos = new Vector3(Mathf.Cos(a) * r, 0.02f, Mathf.Sin(a) * r);
                var heading = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)) * Mathf.Sign(swim.y);
                var pitch = 0f;
                if (k == shoal.Leaper)
                {
                    // the leap: up and over in the last 0.7 s before the next one is chosen
                    var t = 1f - Mathf.Clamp01(shoal.LeapClock / 0.7f);
                    if (shoal.LeapClock < 0.7f)
                    {
                        pos.y += Mathf.Sin(t * Mathf.PI) * 0.55f;
                        pitch = Mathf.Lerp(-50f, 50f, t);
                    }
                }
                fish.localPosition = pos;
                fish.localRotation = Quaternion.LookRotation(heading) * Quaternion.Euler(pitch, 0f, Mathf.Sin(time * 6f + k) * 8f);
            }
        }

        /// <summary>One boxy fish, nose along +z: a body, a tail fin, a back fin (sub-mesh 1 = the darker fins and back).</summary>
        private static Mesh BuildFishMesh()
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var body = new List<int>();
            var fins = new List<int>();
            void Box(Vector3 c, Vector3 half, List<int> into)
            {
                var faces = new[]
                {
                    (Vector3.up, Vector3.right, Vector3.forward), (Vector3.down, Vector3.forward, Vector3.right),
                    (Vector3.right, Vector3.forward, Vector3.up), (Vector3.left, Vector3.up, Vector3.forward),
                    (Vector3.forward, Vector3.up, Vector3.right), (Vector3.back, Vector3.right, Vector3.up),
                };
                foreach (var (n, u, v) in faces)
                {
                    var o = c + Vector3.Scale(n, half);
                    var du = Vector3.Scale(u, half);
                    var dv = Vector3.Scale(v, half);
                    var s = vertices.Count;
                    vertices.Add(o - du - dv);
                    vertices.Add(o - du + dv);
                    vertices.Add(o + du + dv);
                    vertices.Add(o + du - dv);
                    for (var i = 0; i < 4; i++) normals.Add(n);
                    into.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
                }
            }
            Box(new Vector3(0f, 0f, 0.02f), new Vector3(0.07f, 0.055f, 0.19f), body);       // the body
            Box(new Vector3(0f, 0.045f, 0.03f), new Vector3(0.072f, 0.015f, 0.17f), fins);  // its dark back
            Box(new Vector3(0f, 0.08f, 0f), new Vector3(0.012f, 0.03f, 0.06f), fins);       // the back fin
            Box(new Vector3(0f, 0f, -0.21f), new Vector3(0.012f, 0.065f, 0.035f), fins);    // the tail
            var mesh = new Mesh { name = "Fish", subMeshCount = 2 };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(body, 0);
            mesh.SetTriangles(fins, 1);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
