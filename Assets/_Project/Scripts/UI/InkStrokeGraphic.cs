using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// Draws the splash's pen strokes and ink dots as one UI mesh, each stroke only as far as the pen has got
    /// at the current time (SplashInk sets it every frame). Strokes are quads per segment plus a join per
    /// corner (round discs, or a miter that falls back to a bevel past 2x, as the preview's SVG draws them);
    /// the rect is the 1920x1080 splash space, centred, so a point (x, y-down) maps to (x - W/2, H/2 - y).
    /// The mesh is rebuilt only while something is still being drawn.
    /// </summary>
    public class InkStrokeGraphic : MaskableGraphic
    {
        private const float MiterLimit = 2f;

        private SplashInkData _data;
        private float _time;
        private float[][] _lengths;          // per stroke: running length at the end of each poly
        private readonly List<Vector2> _pts = new List<Vector2>(256);
        private bool _finished;

        public void Init(SplashInkData data)
        {
            _data = data;
            _lengths = new float[data.strokes.Length][];
            for (var s = 0; s < data.strokes.Length; s++)
            {
                var polys = data.strokes[s].polys;
                _lengths[s] = new float[polys.Length];
                var run = 0f;
                for (var k = 0; k < polys.Length; k++)
                {
                    var p = polys[k].p;
                    for (var i = 2; i + 1 < p.Length; i += 2)
                        run += Vector2.Distance(new Vector2(p[i - 2], p[i - 1]), new Vector2(p[i], p[i + 1]));
                    _lengths[s][k] = run;
                }
            }
            _finished = false;
            SetVerticesDirty();
        }

        public void SetTime(float t)
        {
            if (_data == null || _finished) return;
            _time = t;
            SetVerticesDirty();
            if (t > LastMoment()) _finished = true;   // one last rebuild at the final state, then stop
        }

        private float LastMoment()
        {
            var last = 0f;
            foreach (var s in _data.strokes) last = Mathf.Max(last, s.t0 + s.dur);
            foreach (var d in _data.dots) last = Mathf.Max(last, d.t0 + d.dur);
            return last;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_data == null) return;
            var col = (Color32)color;

            for (var s = 0; s < _data.strokes.Length; s++)
            {
                var st = _data.strokes[s];
                if (_time <= st.t0 || st.dur <= 0f) continue;
                var total = _lengths[s][_lengths[s].Length - 1];
                var drawn = total * SplashInkData.Ease(_data.strokeEase, (_time - st.t0) / st.dur);
                var before = 0f;
                for (var k = 0; k < st.polys.Length && drawn > before; k++)
                {
                    CollectPoints(st.polys[k].p, drawn - before);
                    Polyline(vh, st.width * 0.5f, st.round, st.roundCaps, col);
                    before = _lengths[s][k];
                }
            }

            foreach (var d in _data.dots)
            {
                if (_time <= d.t0) continue;
                Disc(vh, ToLocal(d.x, d.y), d.r * DotScale((_time - d.t0) / d.dur), col);
            }
        }

        /// <summary>The dot's pop: 0 -> a little over full -> full (the preview's 0;12.5;10).</summary>
        private float DotScale(float u)
        {
            var v = _data.dotScale; var k = _data.dotKeys;
            if (v == null || k == null || v.Length < 3) return Mathf.Clamp01(u);
            if (u >= 1f) return v[v.Length - 1];
            for (var i = 1; i < k.Length; i++)
            {
                if (u > k[i]) continue;
                var f = Mathf.SmoothStep(0f, 1f, (u - k[i - 1]) / (k[i] - k[i - 1]));
                return Mathf.Lerp(v[i - 1], v[i], f);
            }
            return v[v.Length - 1];
        }

        private Vector2 ToLocal(float x, float y) => new Vector2(x - _data.width * 0.5f, _data.height * 0.5f - y);

        /// <summary>The poly's points up to `length` along it, the last one cut mid-segment.</summary>
        private void CollectPoints(float[] p, float length)
        {
            _pts.Clear();
            if (p.Length < 2) return;
            var prev = ToLocal(p[0], p[1]);
            _pts.Add(prev);
            var run = 0f;
            for (var i = 2; i + 1 < p.Length; i += 2)
            {
                var cur = ToLocal(p[i], p[i + 1]);
                var seg = Vector2.Distance(prev, cur);
                if (run + seg >= length)
                {
                    if (seg > 1e-4f) _pts.Add(Vector2.Lerp(prev, cur, (length - run) / seg));
                    return;
                }
                if (seg > 1e-4f) _pts.Add(cur);
                run += seg;
                prev = cur;
            }
        }

        private void Polyline(VertexHelper vh, float hw, bool round, bool roundCaps, Color32 col)
        {
            var n = _pts.Count;
            if (n < 2) return;
            for (var i = 0; i + 1 < n; i++)
            {
                var a = _pts[i]; var b = _pts[i + 1];
                var dir = (b - a).normalized;
                var nrm = new Vector2(-dir.y, dir.x) * hw;
                Quad(vh, a + nrm, a - nrm, b - nrm, b + nrm, col);
            }
            for (var i = 1; i + 1 < n; i++)
            {
                if (round) { Disc(vh, _pts[i], hw, col); continue; }
                var p = _pts[i];
                var d1 = (p - _pts[i - 1]).normalized; var d2 = (_pts[i + 1] - p).normalized;
                var n1 = new Vector2(-d1.y, d1.x); var n2 = new Vector2(-d2.y, d2.x);
                if (Mathf.Abs(d1.x * d2.y - d1.y * d2.x) < 1e-4f) continue;
                var side = Vector2.Dot(n1, d2) > 0f ? -1f : 1f;     // the outer side of the turn
                var a = p + n1 * (side * hw); var b = p + n2 * (side * hw);
                var mid = (n1 + n2).normalized;
                var cos = Vector2.Dot(mid, n1);
                if (cos > 1f / MiterLimit)
                {
                    var m = p + mid * (side * hw / cos);
                    Tri(vh, p, a, m, col); Tri(vh, p, m, b, col);
                }
                else Tri(vh, p, a, b, col);
            }
            if (roundCaps) { Disc(vh, _pts[0], hw, col); Disc(vh, _pts[n - 1], hw, col); }
        }

        private static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 col)
        {
            var i = vh.currentVertCount;
            vh.AddVert(a, col, Vector4.zero); vh.AddVert(b, col, Vector4.zero);
            vh.AddVert(c, col, Vector4.zero); vh.AddVert(d, col, Vector4.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        private static void Tri(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color32 col)
        {
            var i = vh.currentVertCount;
            vh.AddVert(a, col, Vector4.zero); vh.AddVert(b, col, Vector4.zero); vh.AddVert(c, col, Vector4.zero);
            vh.AddTriangle(i, i + 1, i + 2);
        }

        private static void Disc(VertexHelper vh, Vector2 c, float r, Color32 col)
        {
            if (r <= 0f) return;
            var segs = Mathf.Clamp(Mathf.CeilToInt(r * 1.2f), 6, 24);
            var centre = vh.currentVertCount;
            vh.AddVert(c, col, Vector4.zero);
            for (var i = 0; i <= segs; i++)
            {
                var a = i * Mathf.PI * 2f / segs;
                vh.AddVert(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, col, Vector4.zero);
                if (i > 0) vh.AddTriangle(centre, centre + i, centre + i + 1);
            }
        }
    }
}
