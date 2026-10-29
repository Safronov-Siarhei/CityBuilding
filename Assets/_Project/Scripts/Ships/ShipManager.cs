using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Maps.Archipelago;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace CityBuilder.Ships
{
    /// <summary>
    /// The archipelago's ships (the user, 2026-10-28): the Shipyard builds them -- its level decides
    /// which -- and they appear on the water by it; they sail over a NavMesh of their own, built
    /// from the sea once the map is laid out: every water cell inside the storm, a hull's width
    /// off the shores. Kept here: the ships afloat, for the save, the limits and the selection.
    /// </summary>
    public class ShipManager : MonoBehaviour
    {
        public const string ShipyardId = "Shipyard";
        public const float WaterLevel = -0.5f;

        /// <summary>How far from the hull a beach may be for boarding and landing, metres.</summary>
        public const float LandingReach = 7f;
        private const float BoardDistance = 4f;

        public static ShipManager Instance { get; private set; }

        private class Boarding
        {
            public Combat.ArmyGroup Group;
            public ShipUnit Ship;
            public Vector3 Landing;
        }

        private readonly List<Boarding> _boarding = new List<Boarding>();

        [SerializeField] private GameObject[] models = new GameObject[0];   // by ShipKind

        private readonly List<ShipUnit> _ships = new List<ShipUnit>();
        private NavMeshDataInstance _sea;
        private int _agentType;
        private bool _hasSea;

        public IReadOnlyList<ShipUnit> Ships => _ships;
        public bool HasSea => _hasSea;
        public NavMeshQueryFilter Filter => new NavMeshQueryFilter { agentTypeID = _agentType, areaMask = NavMesh.AllAreas };

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
            if (_sea.valid) _sea.Remove();
        }

        /// <summary>
        /// The ships' NavMesh from the generated sea: the water cells inside the storm, merged into
        /// rectangles, baked for a hull (radius 1.4 m), so a course keeps off every shore.
        /// </summary>
        public void BuildSea(ArchipelagoMap map, Vector3 origin)
        {
            var n = map.Size;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var free = new bool[n * n];
            for (var z = 0; z < n; z++)
            for (var x = 0; x < n; x++)
            {
                var edge = Mathf.Min(Mathf.Min(x, z), Mathf.Min(n - 1 - x, n - 1 - z));
                free[z * n + x] = !map.Land[z * n + x] && edge >= ArchipelagoGenerator.Storm;
            }
            // greedy rectangles over the free cells
            for (var z = 0; z < n; z++)
            for (var x = 0; x < n; x++)
            {
                if (!free[z * n + x]) continue;
                var w = 1;
                while (x + w < n && free[z * n + x + w]) w++;
                var h = 1;
                while (z + h < n)
                {
                    var row = true;
                    for (var k = 0; k < w && row; k++) row = free[(z + h) * n + x + k];
                    if (!row) break;
                    h++;
                }
                for (var dz = 0; dz < h; dz++)
                for (var dx = 0; dx < w; dx++) free[(z + dz) * n + x + dx] = false;
                var s = vertices.Count;
                vertices.Add(origin + new Vector3(x, WaterLevel, z));
                vertices.Add(origin + new Vector3(x + w, WaterLevel, z));
                vertices.Add(origin + new Vector3(x + w, WaterLevel, z + h));
                vertices.Add(origin + new Vector3(x, WaterLevel, z + h));
                triangles.AddRange(new[] { s, s + 2, s + 1, s, s + 3, s + 2 });
            }
            var mesh = new Mesh { name = "ShipsSea", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();

            var settings = NavMesh.CreateSettings();
            settings.agentRadius = 1.4f;
            settings.agentHeight = 2f;
            settings.agentSlope = 5f;
            settings.agentClimb = 0.05f;
            settings.minRegionArea = 4f;
            settings.overrideVoxelSize = true;
            settings.voxelSize = 0.5f;
            _agentType = settings.agentTypeID;
            var sources = new List<NavMeshBuildSource>
            {
                new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Mesh, sourceObject = mesh, transform = Matrix4x4.identity, area = 0 },
            };
            var bounds = new Bounds(new Vector3(0f, WaterLevel, 0f), new Vector3(n + 20f, 4f, n + 20f));
            var data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            _sea = NavMesh.AddNavMeshData(data);
            // an agent type made at run time may have any id, a negative one too
            _hasSea = _sea.valid;
            Debug.Log($"ShipManager: the sea's NavMesh baked from {vertices.Count / 4} rectangles.");
        }

        public bool TrySampleSea(Vector3 near, float radius, out Vector3 point)
        {
            point = near;
            if (!HasSea) return false;
            near.y = WaterLevel;
            if (!NavMesh.SamplePosition(near, out var hit, radius, Filter)) return false;
            point = hit.position;
            return true;
        }

        /// <summary>A ship's own spot on the sea NavMesh (it rides a little above it).</summary>
        public Vector3 SeaPoint(Vector3 at) => TrySampleSea(at, 4f, out var p) ? p : new Vector3(at.x, WaterLevel, at.z);

        /// <summary>The nearest walkable land by a point (the land's own NavMesh, never the sea's).</summary>
        public static bool TrySampleLand(Vector3 near, float radius, out Vector3 point)
        {
            point = near;
            if (!NavMesh.SamplePosition(near, out var hit, radius, new NavMeshQueryFilter { agentTypeID = 0, areaMask = NavMesh.AllAreas })) return false;
            point = hit.position;
            return true;
        }

        /// <summary>
        /// Where the ship touches a beach: walkable land within reach of its hull. A cliff or the
        /// rocky rim is not walkable, so a ship under one finds nothing (the user, 2026-10-28:
        /// landing only at beaches).
        /// </summary>
        public bool TryFindLanding(ShipUnit ship, out Vector3 point)
        {
            point = default;
            return ship != null && TryFindLandingAt(ship.transform.position, out point);
        }

        /// <summary>The beach a ship standing here would touch (see TryFindLanding).</summary>
        public static bool TryFindLandingAt(Vector3 at, out Vector3 point)
        {
            if (!TrySampleLand(at, LandingReach, out point)) return false;
            var flat = point - at;
            flat.y = 0f;
            return flat.magnitude <= LandingReach;
        }

        /// <summary>Why this group cannot board this ship now, or null when it can.</summary>
        public string DescribeBoardBlocker(Combat.ArmyGroup group, ShipUnit ship)
        {
            if (group == null || ship == null) return Localization.Get("#ship_cannot_carry");
            if (ship.Capacity <= 0) return Localization.Format("#ship_cannot_carry", ShipStats.DisplayName(ship.Kind));
            if (ship.FreeSeats == 0) return Localization.Format("#ship_full", ShipStats.DisplayName(ship.Kind));
            if (!TryFindLanding(ship, out _)) return Localization.Get("#ship_no_beach");
            return null;
        }

        /// <summary>
        /// Sends the group to the beach the ship stands at; each member that gets there goes aboard
        /// while there is room (Update). A new order to the group calls it off.
        /// </summary>
        public bool OrderBoard(Combat.ArmyGroup group, ShipUnit ship, out Vector3 landing)
        {
            landing = default;
            if (DescribeBoardBlocker(group, ship) != null || !TryFindLanding(ship, out landing)) return false;
            _boarding.RemoveAll(b => b.Group == group);
            group.OrderMoveTo(landing);
            _boarding.Add(new Boarding { Group = group, Ship = ship, Landing = landing });
            return true;
        }

        /// <summary>Why the ship cannot land its people here, or null when it can.</summary>
        public string DescribeLandBlocker(ShipUnit ship)
        {
            if (ship == null || ship.Aboard.Count == 0) return Localization.Get("#ship_empty");
            if (!TryFindLanding(ship, out _)) return Localization.Get("#ship_no_beach");
            return null;
        }

        /// <summary>Everyone aboard onto the beach the ship stands at; how many landed (0 = no beach or nobody aboard).</summary>
        public int TryLand(ShipUnit ship)
        {
            if (DescribeLandBlocker(ship) != null || !TryFindLanding(ship, out var at)) return 0;
            return ship.Disembark(at);
        }

        private void Update()
        {
            for (var i = _boarding.Count - 1; i >= 0; i--)
            {
                var b = _boarding[i];
                // gone, sailed off, or the player sent the group elsewhere
                if (b.Ship == null || b.Group == null || b.Group.Count == 0 || (b.Group.HoldPosition - b.Landing).sqrMagnitude > 0.01f
                    || Flat(b.Ship.transform.position - b.Landing) > LandingReach + 1f)
                {
                    _boarding.RemoveAt(i);
                    continue;
                }
                var waiting = false;
                foreach (var unit in b.Group.Members)
                {
                    if (unit == null || !unit.gameObject.activeSelf) continue;
                    // at the landing, or as near its ring slot as the beach lets it stand
                    if (Flat(unit.transform.position - b.Landing) <= BoardDistance || Flat(unit.transform.position - b.Group.SlotPositionFor(unit)) <= 1f) b.Ship.Embark(unit);
                    if (unit.gameObject.activeSelf) waiting = true;
                }
                if (!waiting || b.Ship.FreeSeats == 0) _boarding.RemoveAt(i);
            }
        }

        private static float Flat(Vector3 v)
        {
            v.y = 0f;
            return v.magnitude;
        }

        /// <summary>A save's soldier back aboard the ship it sailed on (index into Ships).</summary>
        public void RestoreAboard(int shipIndex, Combat.SoldierUnit unit)
        {
            if (shipIndex >= 0 && shipIndex < _ships.Count && _ships[shipIndex] != null) _ships[shipIndex].Embark(unit);
        }

        /// <summary>The ship this one sails on, as an index into Ships; -1 on land.</summary>
        public int ShipIndexOf(Combat.SoldierUnit unit)
        {
            for (var i = 0; i < _ships.Count; i++) if (_ships[i] != null && _ships[i].Aboard.Contains(unit)) return i;
            return -1;
        }

        public int CountOf(ShipKind kind)
        {
            var count = 0;
            foreach (var ship in _ships) if (ship != null && ship.Kind == kind) count++;
            return count;
        }

        /// <summary>Why this ship cannot be built at this Shipyard now, or null when it can.</summary>
        public string DescribeBuildBlocker(ShipKind kind, BuildingInstance shipyard)
        {
            if (!HasSea) return Localization.Get("#ship_no_sea");
            var need = ShipStats.ShipyardLevel(kind);
            if (shipyard == null || shipyard.Level < need) return Localization.Format("#ship_need_shipyard", ShipStats.DisplayName(kind), need);
            var limit = ShipStats.MaxCount(kind);
            if (limit > 0 && CountOf(kind) >= limit) return Localization.Format("#ship_limit", ShipStats.DisplayName(kind), limit);
            var resources = ResourceManager.Instance;
            var price = ShipStats.PriceCoins(kind);
            if (resources != null && !resources.HasEnough(new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Coins, amount = price } }))
            {
                return Localization.Format("#ship_no_coins", ShipStats.DisplayName(kind), price);
            }
            return null;
        }

        /// <summary>Builds one at the Shipyard: paid, and afloat on the water beside it.</summary>
        public ShipUnit TryBuild(ShipKind kind, BuildingInstance shipyard)
        {
            if (DescribeBuildBlocker(kind, shipyard) != null) return null;
            if (!TrySampleSea(shipyard.transform.position, 12f, out var at)) return null;
            var resources = ResourceManager.Instance;
            var price = ShipStats.PriceCoins(kind);
            if (price > 0 && resources != null && !resources.TrySpend(new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Coins, amount = price } })) return null;
            var ship = Spawn(kind, at, Quaternion.LookRotation(at - shipyard.transform.position == Vector3.zero ? Vector3.forward : (at - shipyard.transform.position).normalized), ShipStats.MaxHealth(kind));
            EventLogManager.Instance?.Log(Localization.Format("#ship_built", ShipStats.DisplayName(kind)));
            return ship;
        }

        public ShipUnit Spawn(ShipKind kind, Vector3 at, Quaternion facing, int health)
        {
            var root = new GameObject($"Ship_{kind}_{_ships.Count}");
            root.transform.SetPositionAndRotation(new Vector3(at.x, WaterLevel, at.z), Quaternion.Euler(0f, facing.eulerAngles.y, 0f));
            var model = (int)kind < models.Length ? models[(int)kind] : null;
            if (model != null)
            {
                // modelled facing Blender -y, as every figure: turned to face the way it sails
                var figure = Instantiate(model, root.transform);
                figure.name = "Model";
                figure.transform.localPosition = Vector3.zero;
                figure.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * model.transform.rotation;
                foreach (var r in figure.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.On;
            }
            var box = root.AddComponent<BoxCollider>();
            var len = kind == ShipKind.Scout ? 3f : 5f;
            box.size = new Vector3(len * 0.45f, 2.5f, len);
            box.center = new Vector3(0f, 1f, 0f);
            var ship = root.AddComponent<ShipUnit>();
            ship.Initialize(kind, health);
            _ships.Add(ship);
            return ship;
        }

        public void Remove(ShipUnit ship)
        {
            _ships.Remove(ship);
            if (ship != null) Destroy(ship.gameObject);
        }

        public void ClearForLoad()
        {
            foreach (var ship in _ships) if (ship != null) Destroy(ship.gameObject);
            _ships.Clear();
            _boarding.Clear();
        }
    }
}
