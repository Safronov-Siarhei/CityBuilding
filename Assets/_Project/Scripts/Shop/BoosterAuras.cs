using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Shop
{
    /// <summary>
    /// The violet stream round every building a running booster works on (the user approved, 2026-11-12;
    /// look C of Design/store/Booster_Hud.png): Лесоруб -- the sawmills, Каменотёс -- the quarries,
    /// Изобилие -- the food producers, Казна -- the Tax House and the Town Hall, Зодчий -- the building
    /// sites, Учёный -- the Laboratory, Боевой дух and Вербовка -- the Barracks, Stable and Workshop,
    /// Стойкость -- the Town Hall. One spiral mesh (made once) drawn with Shaders/BoosterStream, one
    /// object per building, pooled; the set is looked over twice a second, not every frame.
    /// </summary>
    public class BoosterAuras : MonoBehaviour
    {
        [SerializeField] private Material material;

        private const float CheckSeconds = 0.5f;
        private const float SpinDegreesPerSecond = 40f;

        private readonly Dictionary<BuildingInstance, Transform> _auras = new Dictionary<BuildingInstance, Transform>();
        private readonly HashSet<BuildingInstance> _wanted = new HashSet<BuildingInstance>();
        private readonly List<BuildingInstance> _drop = new List<BuildingInstance>();
        private readonly Stack<Transform> _pool = new Stack<Transform>();
        private float _checkIn;
        private static Mesh _mesh;

        private void Update()
        {
            if (material == null) return;
            var dt = Time.deltaTime;
            // the spin: a transform turn on a handful of objects
            foreach (var aura in _auras.Values)
                if (aura != null) aura.Rotate(0f, SpinDegreesPerSecond * dt, 0f, Space.Self);
            _checkIn -= Time.unscaledDeltaTime;
            if (_checkIn > 0f) return;
            _checkIn = CheckSeconds;
            Gather();
            _drop.Clear();
            foreach (var pair in _auras)
                if (pair.Key == null || !_wanted.Contains(pair.Key)) _drop.Add(pair.Key);
            foreach (var key in _drop)
            {
                var aura = _auras[key];
                _auras.Remove(key);
                if (aura == null) continue;
                aura.gameObject.SetActive(false);
                aura.SetParent(transform, false);
                _pool.Push(aura);
            }
            foreach (var building in _wanted)
                if (!_auras.ContainsKey(building)) _auras[building] = Take(building);
        }

        /// <summary>The player's buildings some running booster works on.</summary>
        private void Gather()
        {
            _wanted.Clear();
            if (!Boosters.Here) return;
            var wood = Boosters.IsActive(Boosters.Lumberjack);
            var stone = Boosters.IsActive(Boosters.Stonecutter);
            var food = Boosters.IsActive(Boosters.Abundance);
            var tax = Boosters.IsActive(Boosters.Treasury);
            var build = Boosters.IsActive(Boosters.Architect);
            var lab = Boosters.IsActive(Boosters.Scholar);
            var army = Boosters.IsActive(Boosters.BattleSpirit) || Boosters.IsActive(Boosters.Recruit);
            var hall = Boosters.IsActive(Boosters.Fortitude);
            if (!(wood || stone || food || tax || build || lab || army || hall)) return;
            foreach (var building in BuildingInstance.All)
            {
                if (building == null || building.IsBot || building.Data == null) continue;
                if (build && !building.IsOperational)
                {
                    _wanted.Add(building);
                    continue;
                }
                if (!building.IsOperational) continue;
                var id = building.Data.buildingName;
                if ((wood && id == "Sawmill")
                    || (stone && id == "Quarry")
                    || (food && FoodOutput.IsProducer(id))
                    || (tax && (id == BuildingIds.TaxHouse || id == BuildingIds.TownHall))
                    || (lab && id == "Laboratory")
                    || (army && (id == BuildingIds.Barracks || id == BuildingIds.Stable || id == BuildingIds.Workshop))
                    || (hall && id == BuildingIds.TownHall))
                    _wanted.Add(building);
            }
        }

        private Transform Take(BuildingInstance building)
        {
            var aura = _pool.Count > 0 ? _pool.Pop() : Make();
            // round the building: as wide as its footprint, a little taller than its roof
            var bounds = new Bounds(building.transform.position, Vector3.zero);
            foreach (var r in building.GetComponentsInChildren<MeshRenderer>())
                if (r.transform != aura) bounds.Encapsulate(r.bounds);
            var radius = Mathf.Max(0.6f, Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.05f);
            var height = Mathf.Max(1f, (bounds.max.y - building.transform.position.y) * 1.15f);
            aura.SetParent(building.transform, true);
            aura.position = new Vector3(bounds.center.x, building.transform.position.y + 0.05f, bounds.center.z);
            aura.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var parentScale = building.transform.lossyScale;
            aura.localScale = new Vector3(radius / Mathf.Max(0.01f, parentScale.x), height / Mathf.Max(0.01f, parentScale.y), radius / Mathf.Max(0.01f, parentScale.z));
            aura.gameObject.SetActive(true);
            return aura;
        }

        private Transform Make()
        {
            var go = new GameObject("BoosterStream");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = Spiral();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go.transform;
        }

        /// <summary>Two ribbons winding up a unit cylinder (radius 1, height 1), half a turn apart.</summary>
        private static Mesh Spiral()
        {
            if (_mesh != null) return _mesh;
            const int ribbons = 2;
            const int segments = 48;
            const float turns = 1.25f;
            const float width = 0.22f;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (var r = 0; r < ribbons; r++)
            {
                var start = vertices.Count;
                var offset = r * Mathf.PI * 2f / ribbons;
                for (var i = 0; i <= segments; i++)
                {
                    var t = i / (float)segments;
                    var a = offset + t * turns * Mathf.PI * 2f;
                    // the ribbon leans in near the top, like air drawn up
                    var radius = Mathf.Lerp(1f, 0.75f, t);
                    var y = t * (1f - width) + width * 0.5f;
                    var p = new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);
                    vertices.Add(p + Vector3.down * width * 0.5f);
                    vertices.Add(p + Vector3.up * width * 0.5f);
                    uvs.Add(new Vector2(t, 0f));
                    uvs.Add(new Vector2(t, 1f));
                    if (i == segments) continue;
                    var v = start + i * 2;
                    triangles.Add(v); triangles.Add(v + 1); triangles.Add(v + 2);
                    triangles.Add(v + 1); triangles.Add(v + 3); triangles.Add(v + 2);
                }
            }
            _mesh = new Mesh { name = "BoosterSpiral" };
            _mesh.SetVertices(vertices);
            _mesh.SetUVs(0, uvs);
            _mesh.SetTriangles(triangles, 0);
            _mesh.RecalculateBounds();
            return _mesh;
        }
    }
}
