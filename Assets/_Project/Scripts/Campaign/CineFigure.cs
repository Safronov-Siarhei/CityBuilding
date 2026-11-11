using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Plays a cutscene figure's baked clips (CineClips) on its bones, crossfading from one clip to the
    /// next. Ticked by whoever owns the figure (OpeningLife, the director), so a scene can be stepped
    /// by hand in the editor exactly as it plays.
    /// </summary>
    public class CineFigure : MonoBehaviour
    {
        [SerializeField] private CineClips clips;

        private Transform[] _bones;
        private CineClips.Clip _clip;
        private CineClips.Clip _from;
        private float _time;
        private float _fromTime;
        private float _fade;
        private float _fadeSeconds;
        private float _rate = 1f;
        private float _fromRate = 1f;

        public CineClips Clips => clips;
        public string Current => _clip != null ? _clip.name : null;
        public float ClipTime => _time;

        public void Init(CineClips set)
        {
            clips = set;
            _bones = null;
            Rigidify();
            CineTwoSided.Apply(gameObject);
        }

        private ParticleSystem _torch;

        /// <summary>
        /// A living flame on the torch (the user, 2026-11-11: the modelled flame stood still and looked
        /// silly; the model now ends in the tarred rag). The rag's top is the left forearm part's vertex
        /// farthest from its joint -- the torch reaches far past the hand. Sized by the figure's height.
        /// </summary>
        public void LightTorch(Material flame)
        {
            if (flame == null || _torch != null) return;
            Transform bone = null;
            Mesh part = null;
            foreach (var mf in GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.transform.parent == null || mf.transform.parent.name != "forearm.L") continue;
                bone = mf.transform.parent;
                part = mf.sharedMesh;
            }
            if (bone == null || part == null) return;
            var tip = Vector3.zero;
            foreach (var v in part.vertices) if (v.sqrMagnitude > tip.sqrMagnitude) tip = v;
            var height = 0f;
            foreach (var r in GetComponentsInChildren<MeshRenderer>()) height = Mathf.Max(height, r.bounds.max.y - transform.position.y);
            height = Mathf.Max(0.2f, height);

            var go = new GameObject("TorchFlame", typeof(ParticleSystem));
            go.transform.SetParent(bone, false);
            go.transform.localPosition = tip;
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            var ps = go.GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.duration = 2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f * height, 0.45f * height);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f * height, 0.12f * height);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;
            var em = ps.emission;
            em.rateOverTime = 45f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.02f * height;
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.15f));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.92f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.55f, 0.12f), 0.45f), new GradientColorKey(new Color(0.8f, 0.18f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var r2 = go.GetComponent<ParticleSystemRenderer>();
            r2.sharedMaterial = flame;
            r2.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            ps.Simulate(0.5f, true, true, false);
            if (Application.isPlaying) ps.Play();
            _torch = ps;
        }

        // the rigid parts cut from each model's skinned mesh, shared by every copy of that model
        private static readonly Dictionary<Mesh, (int bone, Mesh part)[]> PartsByMesh = new Dictionary<Mesh, (int, Mesh)[]>();

        /// <summary>
        /// Swaps the skinned meshes for rigid parts riding their bones, as the game's own figures are
        /// built. Every vertex of a cutscene figure belongs to one bone, so nothing bends; and a skinned
        /// mesh is only re-skinned by the player loop, so a film rendered frame by frame in the editor
        /// showed every figure frozen in its bind pose (the user, 2026-11-10: the sack floating, the hand
        /// down). Cheaper on a phone too: no skinning at all.
        /// </summary>
        private void Rigidify()
        {
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = smr.sharedMesh;
                if (mesh == null) continue;
                if (!PartsByMesh.TryGetValue(mesh, out var parts))
                {
                    parts = Cut(mesh);
                    PartsByMesh[mesh] = parts;
                }
                var bones = smr.bones;
                foreach (var (bone, part) in parts)
                {
                    if (bone < 0 || bone >= bones.Length || bones[bone] == null) continue;
                    var go = new GameObject(smr.name + "_" + bones[bone].name, typeof(MeshFilter), typeof(MeshRenderer));
                    go.transform.SetParent(bones[bone], false);
                    go.GetComponent<MeshFilter>().sharedMesh = part;
                    var r = go.GetComponent<MeshRenderer>();
                    r.sharedMaterials = smr.sharedMaterials;
                    r.shadowCastingMode = smr.shadowCastingMode;
                }
                smr.enabled = false;
            }
        }

        /// <summary>The mesh split by the bone that carries each vertex, every piece in its bone's own space.</summary>
        private static (int, Mesh)[] Cut(Mesh mesh)
        {
            var weights = mesh.boneWeights;
            var bind = mesh.bindposes;
            var verts = mesh.vertices;
            var normals = mesh.normals;
            var uv0 = new List<Vector2>();
            var uv1 = new List<Vector2>();
            mesh.GetUVs(0, uv0);
            mesh.GetUVs(1, uv1);
            var colours = mesh.colors;
            var owner = new int[verts.Length];
            for (var i = 0; i < verts.Length; i++) owner[i] = weights.Length > i ? weights[i].boneIndex0 : 0;
            var result = new List<(int, Mesh)>();
            for (var b = 0; b < bind.Length; b++)
            {
                var map = new Dictionary<int, int>();
                var subTris = new List<int>[mesh.subMeshCount];
                var any = false;
                for (var s = 0; s < mesh.subMeshCount; s++)
                {
                    subTris[s] = new List<int>();
                    var tris = mesh.GetTriangles(s);
                    for (var t = 0; t + 2 < tris.Length; t += 3)
                    {
                        if (owner[tris[t]] != b) continue;
                        for (var k = 0; k < 3; k++)
                        {
                            var v = tris[t + k];
                            if (!map.TryGetValue(v, out var nv))
                            {
                                nv = map.Count;
                                map[v] = nv;
                            }
                            subTris[s].Add(nv);
                        }
                        any = true;
                    }
                }
                if (!any) continue;
                var n = map.Count;
                var pv = new Vector3[n];
                var pn = new Vector3[n];
                var p0 = new Vector2[n];
                var p1 = uv1.Count == verts.Length ? new Vector2[n] : null;
                var pc = colours.Length == verts.Length ? new Color[n] : null;
                foreach (var kv in map)
                {
                    pv[kv.Value] = bind[b].MultiplyPoint3x4(verts[kv.Key]);
                    pn[kv.Value] = normals.Length > kv.Key ? bind[b].MultiplyVector(normals[kv.Key]).normalized : Vector3.up;
                    if (uv0.Count == verts.Length) p0[kv.Value] = uv0[kv.Key];
                    if (p1 != null) p1[kv.Value] = uv1[kv.Key];
                    if (pc != null) pc[kv.Value] = colours[kv.Key];
                }
                var part = new Mesh { name = mesh.name + "_" + b };
                part.vertices = pv;
                part.normals = pn;
                part.uv = p0;
                if (p1 != null) part.uv2 = p1;
                if (pc != null) part.colors = pc;
                part.subMeshCount = mesh.subMeshCount;
                for (var s = 0; s < mesh.subMeshCount; s++) part.SetTriangles(subTris[s], s);
                part.RecalculateBounds();
                result.Add((b, part));
            }
            return result.ToArray();
        }

        /// <summary>Starts a clip, faded in over `fade` seconds from whatever played; `rate` scales its
        /// speed (a walk slowed to the pace it moves at). Playing the current clip again keeps its time.</summary>
        public void Play(string clipName, float fade = 0.2f, float rate = 1f, float startTime = 0f)
        {
            if (clips == null) return;
            var next = clips.Find(clipName);
            if (next == null) return;
            if (next == _clip)
            {
                _rate = rate;
                return;
            }
            _from = _clip;
            _fromTime = _time;
            _fromRate = _rate;
            _clip = next;
            _time = startTime;
            _rate = rate;
            _fadeSeconds = _from != null ? fade : 0f;
            _fade = 0f;
        }

        public void Tick(float dt)
        {
            if (_torch != null && !Application.isPlaying) _torch.Simulate(dt, true, false, false);
            if (_clip == null) return;
            if (_bones == null) Bind();
            _time += dt * _rate;
            _fromTime += dt * _fromRate;
            _fade += dt;
            var w = _fadeSeconds > 0f ? Mathf.Clamp01(_fade / _fadeSeconds) : 1f;
            if (w >= 1f) _from = null;
            for (var b = 0; b < _bones.Length; b++)
            {
                if (_bones[b] == null) continue;
                var q = Rotation(_clip, _time, b);
                if (_from != null) q = Quaternion.Slerp(Rotation(_from, _fromTime, b), q, w);
                _bones[b].localRotation = q;
            }
            for (var m = 0; m < clips.movingBones.Length; m++)
            {
                var t = _bones[clips.movingBones[m]];
                if (t == null) continue;
                var p = Position(_clip, _time, m);
                if (_from != null) p = Vector3.Lerp(Position(_from, _fromTime, m), p, w);
                t.localPosition = p;
            }
        }

        /// <summary>True once a clip that does not loop has played to its end.</summary>
        public bool Finished => _clip != null && !_clip.loop && _time >= _clip.seconds;

        private void Bind()
        {
            _bones = new Transform[clips.bones.Length];
            for (var i = 0; i < _bones.Length; i++) _bones[i] = transform.Find(clips.bones[i]);
        }

        private static void Frame(CineClips.Clip c, float time, out int a, out int b, out float f)
        {
            var span = c.frames - 1;
            var x = c.seconds > 0f ? time / c.seconds : 0f;
            x = c.loop ? x - Mathf.Floor(x) : Mathf.Clamp01(x);
            var at = x * span;
            a = Mathf.Min(span, (int)at);
            b = Mathf.Min(span, a + 1);
            f = at - a;
        }

        private Quaternion Rotation(CineClips.Clip c, float time, int bone)
        {
            Frame(c, time, out var a, out var b, out var f);
            var n = clips.bones.Length * 4;
            var i = a * n + bone * 4;
            var j = b * n + bone * 4;
            var qa = new Quaternion(c.rot[i], c.rot[i + 1], c.rot[i + 2], c.rot[i + 3]);
            var qb = new Quaternion(c.rot[j], c.rot[j + 1], c.rot[j + 2], c.rot[j + 3]);
            return Quaternion.Slerp(qa, qb, f);
        }

        private Vector3 Position(CineClips.Clip c, float time, int moving)
        {
            Frame(c, time, out var a, out var b, out var f);
            var n = clips.movingBones.Length * 3;
            var i = a * n + moving * 3;
            var j = b * n + moving * 3;
            return Vector3.Lerp(new Vector3(c.pos[i], c.pos[i + 1], c.pos[i + 2]), new Vector3(c.pos[j], c.pos[j + 1], c.pos[j + 2]), f);
        }
    }
}
