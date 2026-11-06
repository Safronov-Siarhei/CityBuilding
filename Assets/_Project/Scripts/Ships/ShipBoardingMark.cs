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
        private readonly Dictionary<ShipUnit, float> _tops = new Dictionary<ShipUnit, float>();
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
                var at = ship.transform.position;
                _arrows[i].position = new Vector3(at.x, TopOf(ship) + ArrowGap + Mathf.Abs(Mathf.Sin(t * 3f)) * 0.45f, at.z);
                _arrows[i].rotation = Quaternion.Euler(0f, t * 90f, 0f);
                var ring = _rings[i];
                ring.transform.position = new Vector3(at.x, ShipManager.WaterLevel + 0.08f, at.z);
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
        private float TopOf(ShipUnit ship)
        {
            if (_tops.TryGetValue(ship, out var top)) return top;
            top = ship.transform.position.y + 2f;
            foreach (var r in ship.GetComponentsInChildren<MeshRenderer>())
                if (r != null && r.enabled) top = Mathf.Max(top, r.bounds.max.y);
            _tops[ship] = top;
            return top;
        }

        private void Build()
        {
            _arrows = new Transform[Marks];
            _rings = new LineRenderer[Marks];
            var mesh = ArrowMesh(0.32f, 0.55f);
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

        /// <summary>A four-sided arrowhead pointing down: a square `half` wide on top, its tip `tall` below; faces unshared (flat light).</summary>
        private static Mesh ArrowMesh(float half, float tall)
        {
            var tip = new Vector3(0f, 0f, 0f);
            var top = new[] { new Vector3(-half, tall, -half), new Vector3(half, tall, -half), new Vector3(half, tall, half), new Vector3(-half, tall, half) };
            var vertices = new List<Vector3>();
            for (var i = 0; i < 4; i++)
            {
                vertices.Add(tip); vertices.Add(top[i]); vertices.Add(top[(i + 1) % 4]);
            }
            // the lid
            vertices.Add(top[0]); vertices.Add(top[2]); vertices.Add(top[1]);
            vertices.Add(top[0]); vertices.Add(top[3]); vertices.Add(top[2]);
            var triangles = new int[vertices.Count];
            for (var i = 0; i < triangles.Length; i++) triangles[i] = i;
            var mesh = new Mesh { name = "BoardingArrow" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
