using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The building kit (Tools/blender/kit.py) lays boxes into each other, and some of their faces end
    /// up in one plane: a gable roof's dark verge board ends flush with the slab's end, so the two
    /// faces share the same depth and the roof's sides flickered wherever the camera looked at them
    /// from the side (the user, 2026-11-17, the main menu's village). Re-exporting every building would
    /// mean rebuilding and saving the models file, so the fix is made here, on import: where two faces
    /// of one model lie in one plane, face the same way and overlap, the DARKER one (the trim -- the
    /// verge, a frame, a band) is moved out by <see cref="Nudge"/> along its normal, so it always wins.
    /// Same-coloured overlaps are left alone (nothing to see). Its vertices are cloned first, so the
    /// faces around it do not move.
    /// </summary>
    public class BuildingCoplanarFix : AssetPostprocessor
    {
        /// <summary>Raised when the rule changes, so the buildings reimport.</summary>
        public override uint GetVersion() => 1;

        private const float Nudge = 0.0025f;
        private const float PlaneTolerance = 0.0004f;
        private const float OverlapTolerance = 0.0005f;

        private static Color[] _palette;
        private static int _pw, _ph;

        private void OnPostprocessModel(GameObject root)
        {
            if (!assetPath.StartsWith("Assets/_Project/Models/Buildings/")) return;
            if (!LoadPalette()) return;
            var moved = 0;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh != null) moved += Fix(mf.sharedMesh);
            if (moved > 0) Debug.Log($"[CoplanarFix] {Path.GetFileName(assetPath)}: {moved} vertices moved out");
        }

        /// <summary>The palette, read from the file itself (it may not be imported yet).</summary>
        private static bool LoadPalette()
        {
            if (_palette != null) return true;
            if (!File.Exists(ModelImportDefaults.PaletteTexturePath)) return false;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(File.ReadAllBytes(ModelImportDefaults.PaletteTexturePath))) return false;
            _palette = tex.GetPixels();
            _pw = tex.width;
            _ph = tex.height;
            Object.DestroyImmediate(tex);
            return true;
        }

        private static float Luminance(Vector2 uv)
        {
            var x = Mathf.Clamp((int)(Mathf.Repeat(uv.x, 1f) * _pw), 0, _pw - 1);
            var y = Mathf.Clamp((int)(Mathf.Repeat(uv.y, 1f) * _ph), 0, _ph - 1);
            var c = _palette[y * _pw + x];
            return 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
        }

        public static int Fix(Mesh mesh)
        {
            var verts = new List<Vector3>();
            mesh.GetVertices(verts);
            var uv0 = new List<Vector2>();
            mesh.GetUVs(0, uv0);
            if (uv0.Count != verts.Count) return 0;

            // every triangle: its plane, its colour, its submesh
            var tris = new List<(int sub, int a, int b, int c)>();
            for (var s = 0; s < mesh.subMeshCount; s++)
            {
                if (mesh.GetTopology(s) != MeshTopology.Triangles) continue;
                var idx = mesh.GetTriangles(s);
                for (var i = 0; i + 2 < idx.Length; i += 3) tris.Add((s, idx[i], idx[i + 1], idx[i + 2]));
            }
            var n = new Vector3[tris.Count];
            var d = new float[tris.Count];
            var lum = new float[tris.Count];
            var buckets = new Dictionary<(int, int, int), List<int>>();
            for (var t = 0; t < tris.Count; t++)
            {
                var (_, a, b, c) = tris[t];
                var cr = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
                if (cr.sqrMagnitude < 1e-12f) { n[t] = Vector3.zero; continue; }
                n[t] = cr.normalized;
                d[t] = Vector3.Dot(n[t], verts[a]);
                lum[t] = Luminance((uv0[a] + uv0[b] + uv0[c]) / 3f);
                // by direction only (the plane's distance is checked per pair: a rounded distance would split
                // two faces that straddle a bucket's edge)
                var key = (Mathf.RoundToInt(n[t].x * 50f), Mathf.RoundToInt(n[t].y * 50f), Mathf.RoundToInt(n[t].z * 50f));
                if (!buckets.TryGetValue(key, out var list)) buckets[key] = list = new List<int>();
                list.Add(t);
            }

            var nudge = new HashSet<int>();
            foreach (var list in buckets.Values)
            {
                if (list.Count < 2) continue;
                for (var i = 0; i < list.Count; i++)
                for (var j = i + 1; j < list.Count; j++)
                {
                    int p = list[i], q = list[j];
                    if (Mathf.Abs(lum[p] - lum[q]) < 0.02f) continue;
                    if (Vector3.Dot(n[p], n[q]) < 0.999f || Mathf.Abs(d[p] - d[q]) > PlaneTolerance) continue;
                    if (!Overlap(verts, tris[p], tris[q], n[p])) continue;
                    nudge.Add(lum[p] < lum[q] ? p : q);
                }
            }
            if (nudge.Count == 0) return 0;

            // clone the nudged triangles' vertices (every channel), move the clones out
            var normals = new List<Vector3>(); mesh.GetNormals(normals);
            var tangents = new List<Vector4>(); mesh.GetTangents(tangents);
            var colors = new List<Color>(); mesh.GetColors(colors);
            var uvs = new List<Vector4>[8];
            for (var ch = 0; ch < 8; ch++)
            {
                uvs[ch] = new List<Vector4>();
                if (mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0 + ch)) mesh.GetUVs(ch, uvs[ch]);
            }
            var subs = new List<int>[mesh.subMeshCount];
            for (var s = 0; s < mesh.subMeshCount; s++) subs[s] = new List<int>();
            int Clone(int v, Vector3 dir)
            {
                verts.Add(verts[v] + dir * Nudge);
                if (normals.Count > v) normals.Add(normals[v]);
                if (tangents.Count > v) tangents.Add(tangents[v]);
                if (colors.Count > v) colors.Add(colors[v]);
                for (var ch = 0; ch < 8; ch++) if (uvs[ch].Count > v) uvs[ch].Add(uvs[ch][v]);
                return verts.Count - 1;
            }
            var original = verts.Count;
            for (var t = 0; t < tris.Count; t++)
            {
                var (s, a, b, c) = tris[t];
                if (nudge.Contains(t)) { a = Clone(a, n[t]); b = Clone(b, n[t]); c = Clone(c, n[t]); }
                subs[s].Add(a); subs[s].Add(b); subs[s].Add(c);
            }
            if (verts.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            if (normals.Count == verts.Count) mesh.SetNormals(normals);
            if (tangents.Count == verts.Count) mesh.SetTangents(tangents);
            if (colors.Count == verts.Count) mesh.SetColors(colors);
            for (var ch = 0; ch < 8; ch++) if (uvs[ch].Count == verts.Count) mesh.SetUVs(ch, uvs[ch]);
            for (var s = 0; s < mesh.subMeshCount; s++) mesh.SetTriangles(subs[s], s, false);
            mesh.RecalculateBounds();
            return verts.Count - original;
        }

        /// <summary>Whether two triangles in one plane overlap by more than a sliver (separating axes in 2D).</summary>
        private static bool Overlap(List<Vector3> v, (int, int a, int b, int c) t1, (int, int a, int b, int c) t2, Vector3 normal)
        {
            var u = Vector3.Cross(normal, Mathf.Abs(normal.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            var w = Vector3.Cross(normal, u);
            Vector2 P(int i) => new Vector2(Vector3.Dot(v[i], u), Vector3.Dot(v[i], w));
            var A = new[] { P(t1.a), P(t1.b), P(t1.c) };
            var B = new[] { P(t2.a), P(t2.b), P(t2.c) };
            return !Separated(A, B) && !Separated(B, A);
        }

        private static bool Separated(Vector2[] A, Vector2[] B)
        {
            for (var i = 0; i < 3; i++)
            {
                var e = A[(i + 1) % 3] - A[i];
                var ax = new Vector2(-e.y, e.x);
                if (ax.sqrMagnitude < 1e-12f) continue;
                ax.Normalize();
                float a0 = float.MaxValue, a1 = float.MinValue, b0 = float.MaxValue, b1 = float.MinValue;
                foreach (var p in A) { var x = Vector2.Dot(p, ax); a0 = Mathf.Min(a0, x); a1 = Mathf.Max(a1, x); }
                foreach (var p in B) { var x = Vector2.Dot(p, ax); b0 = Mathf.Min(b0, x); b1 = Mathf.Max(b1, x); }
                if (Mathf.Min(a1, b1) - Mathf.Max(a0, b0) < OverlapTolerance) return true;
            }
            return false;
        }
    }
}
