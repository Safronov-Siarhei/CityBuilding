using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.Grid;
using CityBuilder.Maps;
using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 2's look of a town under siege (the user's second playtest, 2026-11-12): wheat fields under
    /// the walls (some burnt to furrows of ash), the farmsteads outside the walls in ruins and some still
    /// burning, three sand roads from the fords to the gates the orcs come by, haystacks, hay bales and a
    /// cart in the town's yards, no mines on the map, and a green glow on everyone being healed. All of it
    /// comes from Mission2Town.json (Tools/campaign/mission2_town.py: fields, fires, decor, state "ruin").
    /// </summary>
    public partial class CityDefenceMode
    {
        // SetupProjectCityScene wires these
        [SerializeField] private Material fxGlow;          // additive soft dot: flames, the healing sparks
        [SerializeField] private Material fxSmoke;         // alpha soft dot: smoke
        [SerializeField] private Material wheatMaterial;   // CineGround mode 5
        [SerializeField] private Material ashMaterial;     // CineGround mode 3 (furrows), a burnt field
        [SerializeField] private Material sandMaterial;    // CineGround mode 2 (a dirt lane)
        [SerializeField] private GameObject wheatEars;     // CineWheat
        [SerializeField] private GameObject[] props = new GameObject[0];   // the decor kinds, by name

        [System.Serializable] private class Field { public float[] box; public bool burnt; }
        [System.Serializable] private class Spot { public float x, y; }
        [System.Serializable] private class Prop { public string id; public float x, y, rot; }

        private Transform _lookRoot;
        private ParticleSystem _healFx;
        private readonly Dictionary<SoldierUnit, float> _healShown = new Dictionary<SoldierUnit, float>();

        /// <summary>Cinematic props are made for mission 1's village, where a cell is 1.6 m.</summary>
        private const float CineScale = 1f / 1.6f;

        private void DressTheSiege()
        {
            _lookRoot = new GameObject("M2Look").transform;
            ClearMines();
            SandRoads();
            Fields();
            Decor();
            Fires();
            _healFx = Particles("M2Heal", fxGlow, 300, 0f);
            SoldierUnit.Healed += OnHealed;
        }

        // ------------------------------------------------------------------ the map

        /// <summary>No ore on this map (the user: «убери все рудники с карты, они не нужны»).</summary>
        private static void ClearMines()
        {
            foreach (var deposit in new List<OreDeposit>(OreDeposit.All))
                if (deposit != null) Destroy(deposit.gameObject);
            foreach (var node in new List<ResourceNode>(ResourceNode.All))
            {
                if (node == null) continue;
                var t = node.ResourceType;
                if (t == Resources.ResourceType.Iron || t == Resources.ResourceType.Coal || t == Resources.ResourceType.Gold)
                    Destroy(node.gameObject);
            }
        }

        /// <summary>The ground under a point (the map's surface), or the grid's ground height.</summary>
        private static float GroundAt(Vector3 at)
        {
            var from = new Vector3(at.x, at.y + 30f, at.z);
            var hits = Physics.RaycastAll(from, Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore);
            var best = float.MinValue;
            foreach (var hit in hits)
            {
                // the map's own ground, not a building or a unit on it
                if (hit.collider.GetComponentInParent<BuildingInstance>() != null || hit.collider.GetComponentInParent<SoldierUnit>() != null || hit.collider.GetComponentInParent<OrcUnit>() != null) continue;
                best = Mathf.Max(best, hit.point.y);
            }
            return best > float.MinValue ? best : GridManager.Instance.GroundHeight;
        }

        /// <summary>The orcs' roads from the fords to the outer gates (the user: «три песчаные дороги»): a sand ribbon along each lane up to its gate.</summary>
        private void SandRoads()
        {
            if (sandMaterial == null) return;
            var width = Mathf.Max(0.5f, B.sandRoadWidth);
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            var gates = new[] { World(28.5f, 40.5f), World(50.5f, 34.5f), World(72.5f, 40.5f) };
            for (var lane = 0; lane < 3 && lane < _plan.lanes.Count; lane++)
            {
                var p = _plan.lanes[lane].points;
                var pts = new List<Vector3>();
                for (var k = 0; k + 1 < p.Length; k += 2)
                {
                    var at = World(p[k], p[k + 1]);
                    pts.Add(at);
                    if (Flat(at - gates[lane]) < 1f) break;
                }
                // finely, so the ribbon follows the ground
                var fine = new List<Vector3>();
                for (var k = 0; k + 1 < pts.Count; k++)
                {
                    var n = Mathf.Max(1, Mathf.CeilToInt(Flat(pts[k + 1] - pts[k]) / 0.5f));
                    for (var s = 0; s < n; s++) fine.Add(Vector3.Lerp(pts[k], pts[k + 1], s / (float)n));
                }
                fine.Add(pts[pts.Count - 1]);
                var along = 0f;
                var start = v.Count;
                for (var k = 0; k < fine.Count; k++)
                {
                    var dir = fine[Mathf.Min(k + 1, fine.Count - 1)] - fine[Mathf.Max(k - 1, 0)];
                    dir.y = 0f;
                    dir = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector3.forward;
                    var side = new Vector3(dir.z, 0f, -dir.x) * (width * 0.5f);
                    if (k > 0) along += Flat(fine[k] - fine[k - 1]);
                    var l = fine[k] - side;
                    var r = fine[k] + side;
                    l.y = GroundAt(l) + 0.03f;
                    r.y = GroundAt(r) + 0.03f;
                    v.Add(l);
                    v.Add(r);
                    uv.Add(new Vector2(along / width, -0.5f));
                    uv.Add(new Vector2(along / width, 0.5f));
                    if (k == 0) continue;
                    var i = start + 2 * k;
                    tri.Add(i - 2); tri.Add(i); tri.Add(i - 1);
                    tri.Add(i - 1); tri.Add(i); tri.Add(i + 1);
                }
            }
            Slab("M2SandRoads", v, uv, tri, sandMaterial);
        }

        private void Slab(string name, List<Vector3> v, List<Vector2> uv, List<int> tri, Material material)
        {
            if (v.Count == 0) return;
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(v);
            if (uv != null) mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(_lookRoot, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>Distance (plan cells) from a plan point to the nearest orc lane.</summary>
        private float LaneDistance(float px, float py)
        {
            var best = float.MaxValue;
            var q = new Vector2(px, py);
            foreach (var lane in _plan.lanes)
            {
                var p = lane.points;
                for (var k = 0; k + 3 < p.Length; k += 2)
                {
                    var a = new Vector2(p[k], p[k + 1]);
                    var b = new Vector2(p[k + 2], p[k + 3]);
                    var ab = b - a;
                    var t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude) : 0f;
                    best = Mathf.Min(best, Vector2.Distance(q, a + ab * t));
                }
            }
            return best;
        }

        /// <summary>The wheat under the walls (the user: «возле стен города нет полей, как мы договаривались»): a
        /// golden slab in plan cells, cut off the orcs' roads and the ruins, ears along its edges; a burnt field is furrows of ash.</summary>
        private void Fields()
        {
            if (_plan.fields == null || wheatMaterial == null) return;
            var grid = GridManager.Instance;
            var height = Mathf.Max(0.05f, B.wheatHeight);
            var clear = B.sandRoadWidth * 0.5f + 0.6f;
            var earsLeft = Mathf.Max(0, B.wheatEarsMax);
            var earsRoot = new GameObject("M2Ears").transform;
            earsRoot.SetParent(_lookRoot, false);
            foreach (var burnt in new[] { false, true })
            {
                var v = new List<Vector3>();
                var tri = new List<int>();
                var top = burnt ? 0.03f : height;
                foreach (var field in _plan.fields)
                {
                    if (field.burnt != burnt || field.box == null || field.box.Length < 4) continue;
                    var x0 = Mathf.FloorToInt(field.box[0]);
                    var y0 = Mathf.FloorToInt(field.box[1]);
                    var x1 = Mathf.CeilToInt(field.box[2]);
                    var y1 = Mathf.CeilToInt(field.box[3]);
                    bool Wheat(int x, int y)
                    {
                        if (x < x0 || x >= x1 || y < y0 || y >= y1) return false;
                        if (LaneDistance(x + 0.5f, y + 0.5f) < clear) return false;
                        return grid.IsAreaFree(CellOf(x, y), Vector2Int.one);
                    }
                    for (var y = y0; y < y1; y++)
                    {
                        for (var x = x0; x < x1; x++)
                        {
                            if (!Wheat(x, y)) continue;
                            // the plan cell's corners in the world (the plan's y runs south)
                            var a = World(x, y);
                            var ground = GroundAt(World(x + 0.5f, y + 0.5f));
                            var nw = new Vector3(a.x, ground + top, a.z);
                            var ne = nw + new Vector3(1f, 0f, 0f);
                            var sw = nw + new Vector3(0f, 0f, -1f);
                            var se = nw + new Vector3(1f, 0f, -1f);
                            var s = v.Count;
                            v.Add(sw); v.Add(nw); v.Add(ne); v.Add(se);
                            tri.Add(s); tri.Add(s + 1); tri.Add(s + 2); tri.Add(s); tri.Add(s + 2); tri.Add(s + 3);
                            if (burnt) continue;
                            // the field's sides where it ends, and a row of ears along them
                            var edges = new[] { (x, y - 1, nw, ne), (x + 1, y, ne, se), (x, y + 1, se, sw), (x - 1, y, sw, nw) };
                            foreach (var (nx, ny, e0, e1) in edges)
                            {
                                if (Wheat(nx, ny)) continue;
                                var w = v.Count;
                                var d = new Vector3(0f, top, 0f);
                                v.Add(e0 - d); v.Add(e1 - d); v.Add(e1); v.Add(e0);
                                tri.Add(w); tri.Add(w + 1); tri.Add(w + 2); tri.Add(w); tri.Add(w + 2); tri.Add(w + 3);
                                if (wheatEars == null || earsLeft <= 0) continue;
                                for (var f = 0.25f; f < 1f && earsLeft > 0; f += 0.5f)
                                {
                                    var at = Vector3.Lerp(e0, e1, f) - d;
                                    var inward = (World(x + 0.5f, y + 0.5f) - at);
                                    inward.y = 0f;
                                    at += inward.normalized * 0.25f;
                                    var ear = Instantiate(wheatEars, earsRoot);
                                    ear.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, 90f * Random.Range(0, 4), 0f) * wheatEars.transform.rotation);
                                    ear.transform.localScale = wheatEars.transform.localScale * CineScale;
                                    earsLeft--;
                                }
                            }
                        }
                    }
                }
                Slab(burnt ? "M2Ash" : "M2Wheat", v, null, tri, burnt ? (ashMaterial != null ? ashMaterial : wheatMaterial) : wheatMaterial);
            }
        }

        private GameObject PropNamed(string id)
        {
            foreach (var p in props)
                if (p != null && p.name == id) return p;
            return null;
        }

        /// <summary>Haystacks, bales, a cart in the yards and by the fields: looks only.</summary>
        private void Decor()
        {
            if (_plan.decor == null) return;
            var root = new GameObject("M2Decor").transform;
            root.SetParent(_lookRoot, false);
            foreach (var d in _plan.decor)
            {
                var model = PropNamed(d.id);
                if (model == null) continue;
                var at = World(d.x, d.y);
                at.y = GroundAt(at);
                var go = Instantiate(model, root);
                go.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, d.rot, 0f) * model.transform.rotation);
                go.transform.localScale = model.transform.localScale * CineScale;
                foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            }
        }

        // ------------------------------------------------------------------ the ruins and the fires

        /// <summary>A piece the plan marks "ruin": its burnt shell (BuildingRuin) where it stood, quietly.</summary>
        private static void Ruin(BuildingInstance building)
        {
            var cells = new List<Vector2Int>(building.Cells());
            BuildingRuin.LeaveOf(building, cells);
            Destroy(building.gameObject);
        }

        private void Fires()
        {
            if (_plan.fires == null || fxGlow == null) return;
            foreach (var f in _plan.fires)
            {
                var at = World(f.x, f.y);
                at.y = GroundAt(at);
                Fire(at, Mathf.Max(0.3f, B.fireSize));
            }
        }

        private void Fire(Vector3 at, float size)
        {
            var root = new GameObject("M2Fire").transform;
            root.SetParent(_lookRoot, false);
            root.position = at;
            var flames = Looping(root, "Flames", fxGlow, 14f * size, 0.5f, 1.0f, 0.6f, 1.2f, 0.25f * size, 0.5f * size, size * 0.8f, false);
            var col = flames.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Fade(new Color(1f, 0.92f, 0.5f), new Color(1f, 0.55f, 0.12f), new Color(0.8f, 0.18f, 0.05f), 0.9f, 0.8f, 0f));
            if (fxSmoke == null) return;
            var smoke = Looping(root, "Smoke", fxSmoke, 5f * size, 2.5f, 4f, 0.5f, 0.9f, 0.4f * size, 0.7f * size, size * 0.5f, true);
            var sc = smoke.colorOverLifetime;
            sc.enabled = true;
            sc.color = new ParticleSystem.MinMaxGradient(Fade(new Color(0.3f, 0.27f, 0.25f), new Color(0.42f, 0.4f, 0.4f), new Color(0.55f, 0.55f, 0.56f), 0f, 0.5f, 0f));
        }

        private static Gradient Fade(Color a, Color b, Color c, float alphaA, float alphaB, float alphaC)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 0.5f), new GradientColorKey(c, 1f) },
                new[] { new GradientAlphaKey(alphaA, 0f), new GradientAlphaKey(alphaB, 0.5f), new GradientAlphaKey(alphaC, 1f) });
            return g;
        }

        private static ParticleSystem Looping(Transform parent, string name, Material material, float rate, float lifeMin, float lifeMax,
            float speedMin, float speedMax, float sizeMin, float sizeMax, float box, bool smoke)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var ps = go.GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.duration = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = smoke ? 40 : 60;
            var em = ps.emission;
            em.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(box, box, 0.1f);
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, smoke ? AnimationCurve.Linear(0f, 1f, 1f, 2.5f) : AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = material;
            r.sortingFudge = smoke ? 10f : 0f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }

        // ------------------------------------------------------------------ the healing

        private ParticleSystem Particles(string name, Material material, int max, float gravity)
        {
            if (material == null) return null;
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.SetParent(_lookRoot, false);
            var ps = go.GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            var em = ps.emission;
            em.rateOverTime = 0f;
            var shape = ps.shape;
            shape.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }

        /// <summary>The healer's work seen (the user: «целитель должен накладывать эффект лечения»): green sparks
        /// rising round whoever got health back, at most every m2_heal_fx_sec per soldier.</summary>
        private void OnHealed(SoldierUnit unit)
        {
            if (_healFx == null || unit == null) return;
            var now = Time.time;
            if (_healShown.TryGetValue(unit, out var last) && now - last < B.healFxSeconds) return;
            _healShown[unit] = now;
            if (_healShown.Count > 200) _healShown.Clear();
            var at = unit.transform.position;
            for (var i = 0; i < 10; i++)
            {
                var a = i / 10f * Mathf.PI * 2f;
                var ring = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.3f;
                _healFx.Emit(new ParticleSystem.EmitParams
                {
                    position = at + ring + Vector3.up * Random.Range(0.05f, 0.5f),
                    velocity = Vector3.up * Random.Range(0.4f, 0.8f) + ring * 0.4f,
                    startSize = Random.Range(0.08f, 0.16f),
                    startLifetime = Random.Range(0.6f, 1.0f),
                    startColor = Color.Lerp(new Color(0.35f, 1f, 0.4f), new Color(0.85f, 1f, 0.6f), Random.value),
                }, 1);
            }
        }
    }
}
