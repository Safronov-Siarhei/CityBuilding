using System.Collections.Generic;
using CityBuilder.Combat;
using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// A stunned orc shows it (the user's thirteenth playtest: «над оглушёнными орками -- крутящийся вихрь»): a small
    /// flat spiral turning over its head for as long as the stun lasts. One spiral mesh, one material, pooled marks.
    /// </summary>
    public partial class CityDefenceMode
    {
        private class StunMark
        {
            public OrcUnit orc;
            public float until;
            public float height;
            public Transform mark;
        }

        private readonly List<StunMark> _stuns = new List<StunMark>();
        private readonly Stack<Transform> _stunPool = new Stack<Transform>();
        private static Mesh _swirlMesh;
        private static Material _swirlMaterial;

        /// <summary>Holds the orc still for `seconds` and marks it.</summary>
        private void Stun(OrcUnit orc, float seconds)
        {
            if (orc == null || orc.CurrentHealth <= 0 || seconds <= 0f) return;
            orc.BusyFor = Mathf.Max(orc.BusyFor, seconds);
            foreach (var s in _stuns)
            {
                if (s.orc != orc) continue;
                s.until = Mathf.Max(s.until, Time.time + seconds);
                return;
            }
            var height = 1f;
            foreach (var r in orc.GetComponentsInChildren<Renderer>())
                if (r.enabled) height = Mathf.Max(height, r.bounds.max.y - orc.transform.position.y);
            var mark = _stunPool.Count > 0 ? _stunPool.Pop() : NewSwirl();
            mark.gameObject.SetActive(true);
            _stuns.Add(new StunMark { orc = orc, until = Time.time + seconds, height = height + 0.15f * Cell, mark = mark });
        }

        private void TickStuns()
        {
            for (var i = _stuns.Count - 1; i >= 0; i--)
            {
                var s = _stuns[i];
                if (s.orc == null || s.orc.CurrentHealth <= 0 || Time.time >= s.until)
                {
                    s.mark.gameObject.SetActive(false);
                    _stunPool.Push(s.mark);
                    _stuns.RemoveAt(i);
                    continue;
                }
                s.mark.position = s.orc.transform.position + Vector3.up * s.height;
                s.mark.rotation = Quaternion.Euler(0f, Time.time * 360f, 0f);
            }
        }

        private Transform NewSwirl()
        {
            var go = new GameObject("M2Stun", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * (0.45f * Cell);
            go.GetComponent<MeshFilter>().sharedMesh = SwirlMesh();
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = SwirlMaterial();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go.transform;
        }

        /// <summary>A flat two-armed spiral ribbon, unit size, lying in the XZ plane, seen from both sides.</summary>
        private static Mesh SwirlMesh()
        {
            if (_swirlMesh != null) return _swirlMesh;
            var v = new List<Vector3>();
            var t = new List<int>();
            const int steps = 22;
            for (var arm = 0; arm < 2; arm++)
            {
                var start = v.Count;
                for (var k = 0; k <= steps; k++)
                {
                    var f = k / (float)steps;
                    var a = arm * Mathf.PI + f * Mathf.PI * 2.2f;
                    var r = 0.12f + 0.38f * f;
                    var w = 0.03f + 0.07f * f;
                    var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    v.Add(dir * (r - w * 0.5f) + Vector3.up * (0.06f * f));
                    v.Add(dir * (r + w * 0.5f) + Vector3.up * (0.06f * f));
                }
                for (var k = 0; k < steps; k++)
                {
                    var a0 = start + k * 2;
                    t.AddRange(new[] { a0, a0 + 2, a0 + 1, a0 + 1, a0 + 2, a0 + 3 });
                    t.AddRange(new[] { a0, a0 + 1, a0 + 2, a0 + 1, a0 + 3, a0 + 2 });
                }
            }
            _swirlMesh = new Mesh { name = "M2StunSwirl" };
            _swirlMesh.SetVertices(v);
            _swirlMesh.SetTriangles(t, 0);
            _swirlMesh.RecalculateNormals();
            _swirlMesh.RecalculateBounds();
            return _swirlMesh;
        }

        private static Material SwirlMaterial()
        {
            if (_swirlMaterial != null) return _swirlMaterial;
            _swirlMaterial = new Material(Core.RuntimeShaders.Unlit) { color = new Color(1f, 0.92f, 0.35f, 1f), name = "M2StunSwirl" };
            return _swirlMaterial;
        }
    }
}
