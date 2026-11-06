using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Ships
{
    /// <summary>
    /// The mark on a ship that people are walking to board (the user's pick, 2026-11-06, instead of the
    /// move flag on the beach): a gold arrow bobbing over the ship and a gold ring pulsing round it, for
    /// as long as anyone is still on the way. A few marks pooled on the ShipManager; nothing allocated
    /// per frame.
    /// </summary>
    public class ShipBoardingMark : MonoBehaviour
    {
        private const int Marks = 4;
        private const int RingPoints = 40;
        private const float RingRadius = 2.6f;
        private const float ArrowGap = 0.9f;        // over the ship's highest point
        private static readonly Color Gold = new Color(1f, 0.82f, 0.18f, 1f);

        private readonly List<ShipUnit> _ships = new List<ShipUnit>();
        private readonly Dictionary<ShipUnit, Vector3> _tops = new Dictionary<ShipUnit, Vector3>();   // the hull's top middle, in the ship's own space
        private Transform[] _arrows;
        private LineRenderer[] _rings;

        private void LateUpdate()
        {
            var manager = ShipManager.Instance;
            if (manager == null) return;
            _ships.Clear();
            manager.BoardingShips(_ships);
            if (_ships.Count == 0 && _arrows == null) return;
            if (_arrows == null) Build();
            var t = Time.time;
            var pulse = 0.5f + 0.5f * Mathf.Sin(t * 5f);
            for (var i = 0; i < Marks; i++)
            {
                var ship = i < _ships.Count ? _ships[i] : null;
                var on = ship != null;
                if (_arrows[i].gameObject.activeSelf != on) _arrows[i].gameObject.SetActive(on);
                if (_rings[i].gameObject.activeSelf != on) _rings[i].gameObject.SetActive(on);
                if (!on) continue;
                // over the hull's middle (the ship's pivot is not), a flat arrow turned to the camera: from above the
                // turning pyramid read as a yellow cube beside the ship (the user, 2026-11-06)
                var top = ship.transform.TransformPoint(TopOf(ship));
                _arrows[i].position = new Vector3(top.x, top.y + ArrowGap + Mathf.Abs(Mathf.Sin(t * 3f)) * 0.45f, top.z);
                var cam = Camera.main;
                if (cam != null)
                {
                    var toCam = cam.transform.position - _arrows[i].position;
                    toCam.y = 0f;
                    if (toCam.sqrMagnitude > 0.01f) _arrows[i].rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
                }
                var ring = _rings[i];
                ring.transform.position = new Vector3(top.x, ShipManager.WaterLevel + 0.08f, top.z);
                var r = RingRadius * (1f + 0.08f * pulse);
                for (var p = 0; p < RingPoints; p++)
                {
                    var a = p / (float)RingPoints * Mathf.PI * 2f;
                    ring.SetPosition(p, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
                }
                ring.widthMultiplier = 0.1f + 0.1f * pulse;
                var c = Gold;
                c.a = 0.45f + 0.55f * pulse;
                ring.startColor = ring.endColor = c;
            }
            if (_tops.Count > 16) _tops.Clear();
        }

        /// <summary>The ship's highest point, measured once per ship (its masts).</summary>
        private Vector3 TopOf(ShipUnit ship)
        {
            if (_tops.TryGetValue(ship, out var top)) return top;
            var any = false;
            var bounds = new Bounds(ship.transform.position, Vector3.zero);
            foreach (var r in ship.GetComponentsInChildren<MeshRenderer>())
            {
                if (r == null || !r.enabled) continue;
                if (!any) bounds = r.bounds;
                else bounds.Encapsulate(r.bounds);
                any = true;
            }
            top = ship.transform.InverseTransformPoint(new Vector3(bounds.center.x, Mathf.Max(bounds.max.y, ship.transform.position.y + 2f), bounds.center.z));
            _tops[ship] = top;
            return top;
        }

        private void Build()
        {
            _arrows = new Transform[Marks];
            _rings = new LineRenderer[Marks];
            var mesh = ArrowMesh(0.55f, 1.1f);
            var gold = new Material(RuntimeShaders.Lit) { color = Gold };
            var line = new Material(RuntimeShaders.Unlit) { color = Gold };
            for (var i = 0; i < Marks; i++)
            {
                var arrow = new GameObject("BoardingArrow", typeof(MeshFilter), typeof(MeshRenderer));
                arrow.transform.SetParent(transform, false);
                arrow.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = arrow.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = gold;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                arrow.SetActive(false);
                _arrows[i] = arrow.transform;

                var ringGo = new GameObject("BoardingRing");
                ringGo.transform.SetParent(transform, false);
                var ring = ringGo.AddComponent<LineRenderer>();
                ring.useWorldSpace = false;
                ring.loop = true;
                ring.positionCount = RingPoints;
                ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ring.receiveShadows = false;
                ring.sharedMaterial = line;
                ringGo.SetActive(false);
                _rings[i] = ring;
            }
        }

        /// <summary>A flat arrow pointing down, its tip at the origin: a head `half` wide and as tall, a shaft over it; both faces.</summary>
        private static Mesh ArrowMesh(float half, float tall)
        {
            var shaft = half * 0.42f;
            var v = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f), new Vector3(-half, half, 0f), new Vector3(half, half, 0f),          // the head
                new Vector3(-shaft, half, 0f), new Vector3(shaft, half, 0f),                                  // the shaft
                new Vector3(shaft, half + tall * 0.7f, 0f), new Vector3(-shaft, half + tall * 0.7f, 0f),
            };
            var front = new List<int> { 0, 1, 2, 3, 6, 5, 3, 5, 4 };
            var tris = new List<int>(front);
            for (var i = 0; i < front.Count; i += 3) { tris.Add(front[i]); tris.Add(front[i + 2]); tris.Add(front[i + 1]); }
            var mesh = new Mesh { name = "BoardingArrow" };
            mesh.SetVertices(v);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
