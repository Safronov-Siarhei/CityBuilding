using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// Lays the ore veins on a map (the user, 2026-10-21, variant 1: a mine stands only on a vein,
    /// and a vein never runs dry). Coal and copper lie within reach of the start, iron further out,
    /// gold far away -- out where the orcs are -- so the richest ore is the one worth fighting for.
    ///
    /// Placed by a random generator seeded with the map's id, and before the forest and the
    /// boulders take any ground: the same map always deals the same veins, so a save needs to say
    /// nothing about them. The distances are fractions of how far the map's dry land reaches from
    /// the start zone, so one rule fits a small map and a big one.
    /// </summary>
    public class OreDepositSpawner : MonoBehaviour
    {
        public static OreDepositSpawner Instance { get; private set; }

        // By OreKind: Iron, Coal, Copper, Gold.
        [SerializeField] private GameObject[] veinModels = new GameObject[4];
        // The halo under every vein (Shaders/OreGlow): additive, breathing, the ore's colour.
        [SerializeField] private Material glowMaterial;

        // The glow's colour by OreKind -- not the ore's paint (coal is black) but the light it
        // gives off: icy blue iron, ember coal, verdigris copper, warm gold.
        private static readonly Color[] GlowColours =
        {
            new Color(0.35f, 0.55f, 1f), new Color(1f, 0.32f, 0.12f), new Color(0.25f, 0.95f, 0.65f), new Color(1f, 0.78f, 0.25f),
        };

        private const float GlowSize = 2.8f;
        private static Mesh _glowQuad;

        // How many of each, and the band of distance from the start (fractions of the map's reach).
        private static readonly (OreKind kind, int count, float near, float far)[] Plan =
        {
            (OreKind.Coal, 3, 0.15f, 0.40f),
            (OreKind.Copper, 3, 0.20f, 0.45f),
            (OreKind.Iron, 3, 0.30f, 0.60f),
            (OreKind.Gold, 2, 0.60f, 0.95f),
        };

        private const float MinSpacingMetres = 9f;
        // The orcs' ground is theirs: a level-3 camp's walls, and the main base's far wider ones.
        private const float AwayFromCampMetres = 20f;
        private const float AwayFromMainBaseMetres = 35f;
        private const int AttemptsPerVein = 400;

        private readonly List<OreDeposit> _placed = new List<OreDeposit>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <param name="treesZone">The forest's cells: veins stay out of it, where trees would hide them.</param>
        public void Initialize(MeshMapDefinition map, ICollection<Vector2Int> treesZone)
        {
            var grid = GridManager.Instance;
            var applier = MeshMapApplier.Instance;
            if (grid == null || applier == null || map == null) return;
            // An Invasion run gathers wood and stone only (the user, 2026-10-21): no veins.
            if (Invasion.InvasionMode.IsActive) return;

            var start = map.TownHallZone.Length > 0
                ? StartZone.Centre(map.TownHallZone)
                : grid.GetFootprintCenterWorld(grid.GridSize / 2, Vector2Int.one);
            var camps = new List<Vector2>(map.CampSpots);
            var mainBase = new List<Vector2>();
            if (map.HasMainBaseSpot) mainBase.Add(map.MainBaseSpot);

            // Every 2x2 plot a vein could take, and how far the map's land reaches from the start.
            var candidates = new List<(Vector2Int origin, Vector3 centre, float distance)>();
            var reach = 1f;
            for (var x = 0; x < grid.GridSize.x - 1; x += 2)
            {
                for (var z = 0; z < grid.GridSize.y - 1; z += 2)
                {
                    var origin = new Vector2Int(x, z);
                    if (!Buildable(grid, applier, origin, treesZone, map)) continue;
                    var centre = grid.GetFootprintCenterWorld(origin, OreDeposit.Size);
                    if (NearAny(centre, camps, AwayFromCampMetres) || NearAny(centre, mainBase, AwayFromMainBaseMetres)) continue;
                    var distance = Flat(centre - start).magnitude;
                    reach = Mathf.Max(reach, distance);
                    candidates.Add((origin, centre, distance));
                }
            }

            var random = new System.Random(StableHash(map.MapId));
            foreach (var (kind, count, near, far) in Plan)
            {
                for (var n = 0; n < count; n++)
                {
                    for (var attempt = 0; attempt < AttemptsPerVein && candidates.Count > 0; attempt++)
                    {
                        var c = candidates[random.Next(candidates.Count)];
                        var t = c.distance / reach;
                        if (t < near || t > far) continue;
                        if (!grid.IsAreaFree(c.origin, OreDeposit.Size) || TooClose(c.centre)) continue;
                        Place(kind, c.origin, c.centre);
                        break;
                    }
                }
            }

            if (_placed.Count == 0) Debug.LogWarning($"OreDepositSpawner: no vein found room on {map.MapId} -- no mine can be built here.");
        }

        private void Place(OreKind kind, Vector2Int origin, Vector3 centre)
        {
            var root = new GameObject($"OreVein{kind}");
            root.transform.SetParent(transform, false);
            root.transform.position = centre;

            // Everything a mine standing on the vein hides: the patch and its glow.
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var source = veinModels != null && (int)kind < veinModels.Length ? veinModels[(int)kind] : null;
            if (source != null)
            {
                var model = Instantiate(source, visual.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = source.transform.localRotation;
                SceneryShadows.DisableCasting(model);
            }
            if (glowMaterial != null) AddGlow(visual.transform, kind, origin);

            // A click target, so a tap can say what it is; a trigger so nobody bumps into it.
            var box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, 0.35f, 0f);
            box.size = new Vector3(OreDeposit.Size.x - 0.1f, 0.7f, OreDeposit.Size.y - 0.1f);

            var deposit = root.AddComponent<OreDeposit>();
            deposit.Initialize(kind, origin, visual);
            GridManager.Instance.SetAreaOccupied(origin, OreDeposit.Size, true);
            _placed.Add(deposit);
        }

        private void AddGlow(Transform parent, OreKind kind, Vector2Int origin)
        {
            var glow = new GameObject("Glow", typeof(MeshFilter), typeof(MeshRenderer));
            glow.transform.SetParent(parent, false);
            glow.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            glow.GetComponent<MeshFilter>().sharedMesh = GlowQuad();
            var renderer = glow.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = glowMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", GlowColours[(int)kind] * 1.25f);
            block.SetFloat("_Phase", (origin.x * 0.37f + origin.y * 0.61f) % 6.283f);
            renderer.SetPropertyBlock(block);
        }

        /// <summary>One flat quad on the ground, shared by every glow.</summary>
        private static Mesh GlowQuad()
        {
            if (_glowQuad != null) return _glowQuad;
            var h = GlowSize * 0.5f;
            _glowQuad = new Mesh
            {
                name = "OreGlowQuad",
                vertices = new[] { new Vector3(-h, 0f, -h), new Vector3(h, 0f, -h), new Vector3(h, 0f, h), new Vector3(-h, 0f, h) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) },
                triangles = new[] { 0, 2, 1, 0, 3, 2 },
            };
            _glowQuad.RecalculateBounds();
            return _glowQuad;
        }

        private static bool Buildable(GridManager grid, MeshMapApplier applier, Vector2Int origin, ICollection<Vector2Int> treesZone, MeshMapDefinition map)
        {
            if (!grid.IsWithinBounds(origin, OreDeposit.Size) || !grid.IsAreaFree(origin, OreDeposit.Size)) return false;
            if (map.TownHallZone.Length > 0)
            {
                // the start zone is kept clear for the Town Hall and the first houses
                for (var x = 0; x < OreDeposit.Size.x; x++)
                for (var z = 0; z < OreDeposit.Size.y; z++)
                {
                    if (StartZone.Allows(origin + new Vector2Int(x, z), Vector2Int.one)) return false;
                }
            }
            for (var x = 0; x < OreDeposit.Size.x; x++)
            {
                for (var z = 0; z < OreDeposit.Size.y; z++)
                {
                    var cell = origin + new Vector2Int(x, z);
                    if (applier.IsWaterCell(cell) || applier.IsNoBuildCell(cell)) return false;
                    if (treesZone != null && treesZone.Contains(cell)) return false;
                    if (!applier.IsGroundAt(grid.GetFootprintCenterWorld(cell, Vector2Int.one))) return false;
                }
            }
            return true;
        }

        private bool TooClose(Vector3 centre)
        {
            foreach (var other in _placed)
            {
                if (Flat(other.transform.position - centre).sqrMagnitude < MinSpacingMetres * MinSpacingMetres) return true;
            }
            return false;
        }

        private static bool NearAny(Vector3 centre, List<Vector2> spots, float metres)
        {
            foreach (var spot in spots)
            {
                if ((new Vector2(centre.x, centre.z) - spot).sqrMagnitude < metres * metres) return true;
            }
            return false;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        /// <summary>A hash of the map's id that is the same on every run and every device (string.GetHashCode is not).</summary>
        private static int StableHash(string text)
        {
            unchecked
            {
                var h = 23;
                foreach (var ch in text ?? string.Empty) h = h * 31 + ch;
                return h;
            }
        }
    }
}
