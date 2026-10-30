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
        private const float ShoalSpinDegrees = 25f;

        public sealed class Shoal
        {
            public Vector3 At;
            public int Stock;
            public float RespawnIn;
            public GameObject Visual;
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
            var rng = new SeedRandom(world.Seed * 31 + 7);
            var start = origin + new Vector3(world.StartCell.x + 0.5f, 0f, world.StartCell.y + 0.5f);
            for (var k = 0; k < count; k++)
            {
                var shoal = new Shoal { Stock = ShoalStock() };
                if (k < 2 ? TryShoreSea(start, 15f, 60f, rng, out shoal.At) : TryShoreSea(origin + new Vector3(world.Size * 0.5f, 0f, world.Size * 0.5f), 0f, world.Size * 0.48f, rng, out shoal.At))
                {
                    shoal.Visual = BuildShoal(shoal.At);
                    _shoals.Add(shoal);
                }
            }
        }

        private static int ShoalStock() => BalanceConfig.Instance != null ? BalanceConfig.Instance.FishShoalStock : 60;

        /// <summary>Sea a ship can reach within a fishing hut's reach of a beach, at a random spot in the ring round `centre`.</summary>
        private bool TryShoreSea(Vector3 centre, float minRadius, float maxRadius, SeedRandom rng, out Vector3 point)
        {
            point = default;
            var reach = HutReach() * 0.6f;
            for (var attempt = 0; attempt < 400; attempt++)
            {
                var a = rng.Range(0f, Mathf.PI * 2f);
                var r = rng.Range(minRadius, maxRadius);
                var probe = centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                if (!TrySampleSea(probe, 2f, out var sea) || !TrySampleLand(sea, reach, out _)) continue;
                point = sea;
                return true;
            }
            return false;
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
                    shoal.Visual.transform.Rotate(0f, ShoalSpinDegrees * t, 0f);
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
            }
        }

        /// <summary>A save's shoals: where each is, its stock, the wait of one fished out.</summary>
        public void RestoreShoals(List<Saving.ShoalEntry> entries)
        {
            ClearShoals();
            foreach (var e in entries)
            {
                var shoal = new Shoal { At = new Vector3(e.x, WaterLevel, e.z), Stock = e.stock, RespawnIn = e.respawnIn };
                if (shoal.Alive) shoal.Visual = BuildShoal(shoal.At);
                _shoals.Add(shoal);
            }
        }

        private void ClearShoals()
        {
            foreach (var shoal in _shoals) if (shoal.Visual != null) Destroy(shoal.Visual);
            _shoals.Clear();
        }

        /// <summary>Seven dark fish backs swimming round under the surface and a few glints on it: one mesh, two materials, spun as one.</summary>
        private GameObject BuildShoal(Vector3 at)
        {
            if (_fishMesh == null)
            {
                _fishMesh = BuildFishMesh();
                _fishMaterial = new Material(RuntimeShaders.Lit) { color = new Color(0.16f, 0.25f, 0.33f) };
                _glintMaterial = new Material(RuntimeShaders.Unlit) { color = new Color(0.92f, 0.97f, 1f) };
            }
            var go = new GameObject("FishShoal");
            go.transform.SetPositionAndRotation(new Vector3(at.x, WaterLevel + 0.03f, at.z), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            go.AddComponent<MeshFilter>().sharedMesh = _fishMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { _fishMaterial, _glintMaterial };
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }

        private static Mesh BuildFishMesh()
        {
            var vertices = new List<Vector3>();
            var fish = new List<int>();
            var glints = new List<int>();
            var rng = new SeedRandom(4242);
            for (var k = 0; k < 7; k++)
            {
                // a fish on a ring, nose along the way round: a diamond body and a tail
                var a = k * Mathf.PI * 2f / 7f + rng.Range(-0.2f, 0.2f);
                var r = rng.Range(0.7f, 1.6f);
                var centre = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                var along = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                var side = new Vector3(along.z, 0f, -along.x);
                var len = rng.Range(0.32f, 0.45f);
                var s = vertices.Count;
                vertices.Add(centre + along * len);
                vertices.Add(centre + side * (len * 0.3f));
                vertices.Add(centre - along * (len * 0.55f));
                vertices.Add(centre - side * (len * 0.3f));
                vertices.Add(centre - along * (len * 0.95f) + side * (len * 0.28f));
                vertices.Add(centre - along * (len * 0.95f) - side * (len * 0.28f));
                fish.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3, s + 2, s + 4, s + 5 });
                // a glint on the surface above every other fish
                if (k % 2 != 0) continue;
                var g = vertices.Count;
                var up = Vector3.up * 0.02f;
                vertices.Add(centre + up + along * 0.08f);
                vertices.Add(centre + up + side * 0.05f);
                vertices.Add(centre + up - along * 0.08f);
                vertices.Add(centre + up - side * 0.05f);
                glints.AddRange(new[] { g, g + 1, g + 2, g, g + 2, g + 3 });
            }
            var mesh = new Mesh { name = "FishShoal", subMeshCount = 2 };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(fish, 0);
            mesh.SetTriangles(glints, 1);
            // both faces up whichever way the winding came out: a fish seen from above
            var normals = new Vector3[vertices.Count];
            for (var i = 0; i < normals.Length; i++) normals[i] = Vector3.up;
            mesh.normals = normals;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
