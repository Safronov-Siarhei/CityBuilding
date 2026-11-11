using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 1's world round the player (the user's first playtest, 2026-11-11):
    ///   - a CORRIDOR: the level is a run, so what lies off the way is dark, as in the dungeons (the
    ///     dungeon's darkness shader over a mask: the village, the field, a band along the track, the
    ///     town's gate), with an invisible wall where the light ends;
    ///   - MARKS on the map: a flag and a glowing ring at each checkpoint (green once reached), a
    ///     pillar of light at the mission's end before the town gate;
    ///   - a golden ARROW at his feet pointing on along the way when he stands lost a moment;
    ///   - LIFE along the way: the film's last villagers run on ahead, an orc runs a villager down,
    ///     dead villagers lie in the streets, pigs from a smashed pen and deer and hares bolt across,
    ///     refugees wait before the town gate.
    /// Every number is in Mission1.json "world". Steps by dt, so the batchmode check runs it.
    /// </summary>
    public class MissionWorld
    {
        [Serializable] public class Config
        {
            public Corridor corridor;
            public Markers markers;
            public Guide guide;
            public Runner[] leftovers;
            public Hunt[] hunts;
            public Placed[] dead;
            public Placed[] folk;
            public Placed[] props;
            public Runner[] animals;
        }

        [Serializable] public class Corridor
        {
            public float texel, villageRadius, trackHalf, fieldMargin, gateHalf, gateFront, gateBack;
            public float soft, wobble, strength, wallHeight;
            public float[] color;
        }

        [Serializable] public class Markers
        {
            public string flag;
            public float ringRadius, ringWidth, flagScale, beaconHeight, beaconWidth, pulse;
            public float[] ringColor, reachedColor, beaconColor;
        }

        [Serializable] public class Guide { public float idleSeconds, ahead, distance, size, offRoute; public float[] color; }

        /// <summary>Someone (or something) running a path once he comes within `radius` of `trigger`,
        /// then gone: the film's villagers (`id` = a film actor), animals (`model`), pigs.</summary>
        [Serializable] public class Runner
        {
            public string id, model, figure;
            public float[] path, trigger;
            public float radius, speed, hop, scale, yaw;
        }

        /// <summary>An orc running a villager down: both run `path`; at its end the villager falls.</summary>
        [Serializable] public class Hunt
        {
            public string victim, orc;
            public float[] path, trigger;
            public float radius, speed, gap;
        }

        [Serializable] public class Placed { public string figure, model, clip; public float x, z, yaw, scale; public bool lying; }

        private class Run
        {
            public Transform t;
            public CineFigure fig;
            public Vector3[] path;
            public float s, speed, hop, len;
            public Vector2 trigger;
            public float radius;
            public bool on, done;
            public CineFigure chaser;
            public float gap;
            public bool falls;
        }

        private readonly Config _c;
        private readonly OpeningVillage _village;
        private readonly Transform _root;
        private readonly List<Run> _runs = new List<Run>();
        private readonly List<(Transform ring, Material mat)> _rings = new List<(Transform, Material)>();
        private readonly MissionChase.Checkpoint[] _checkpoints;
        private Material _beacon;
        private Transform _arrow;
        private Material _arrowMat;
        private float _idle;
        private float _clock;

        public MissionWorld(Config c, OpeningVillage village, Transform parent, Material dark, Material spark,
            MissionChase.Checkpoint[] checkpoints, Vector2 finish, Func<string, OpeningLife.CastMember?> cast,
            Func<string, CineFigure> takeActor)
        {
            _c = c;
            _village = village;
            _checkpoints = checkpoints;
            _root = new GameObject("MissionWorld").transform;
            _root.SetParent(parent, false);
            if (c.corridor != null && dark != null) Darkness(dark, finish);
            if (c.markers != null && spark != null) Marks(spark, finish);
            if (c.guide != null && spark != null) Arrow(spark);
            if (c.leftovers != null)
                foreach (var r in c.leftovers)
                {
                    var fig = takeActor(r.id);
                    if (fig != null) _runs.Add(MakeRun(fig.transform, fig, r));
                }
            if (c.animals != null)
                foreach (var r in c.animals)
                {
                    var model = village.Model(r.model);
                    if (model == null) continue;
                    var go = UnityEngine.Object.Instantiate(model, _root);
                    go.transform.localScale = model.transform.localScale * (r.scale > 0f ? r.scale : 1f);
                    CineTwoSided.Apply(go);
                    _runs.Add(MakeRun(go.transform, null, r));
                }
            if (c.hunts != null)
                foreach (var h in c.hunts)
                {
                    var victim = Figure(cast, h.victim);
                    var orc = Figure(cast, h.orc);
                    if (victim == null || orc == null) continue;
                    var run = MakeRun(victim.transform, victim, new Runner { path = h.path, trigger = h.trigger, radius = h.radius, speed = h.speed });
                    run.chaser = orc;
                    run.gap = h.gap;
                    run.falls = true;
                    orc.gameObject.SetActive(false);
                    _runs.Add(run);
                }
            if (c.dead != null) foreach (var p in c.dead) Put(cast, p, true);
            if (c.folk != null) foreach (var p in c.folk) Put(cast, p, false);
            if (c.props != null)
                foreach (var p in c.props)
                {
                    var model = village.Model(p.model);
                    if (model == null) continue;
                    var go = UnityEngine.Object.Instantiate(model, _root);
                    go.transform.SetPositionAndRotation(new Vector3(p.x, 0f, p.z), Quaternion.Euler(0f, p.yaw, 0f) * model.transform.rotation);
                    go.transform.localScale = model.transform.localScale * (p.scale > 0f ? p.scale : 1f);
                    CineTwoSided.Apply(go);
                }
        }

        public void Step(float dt, Vector3 ardan, bool moving, int reached)
        {
            _clock += dt;
            var me = new Vector2(ardan.x, ardan.z);
            foreach (var r in _runs) StepRun(r, me, dt);
            // the checkpoints' rings: gold ahead, green once reached
            for (var i = 0; i < _rings.Count; i++)
            {
                var c = i <= reached ? Col(_c.markers.reachedColor) : Col(_c.markers.ringColor);
                c *= 0.75f + 0.25f * Mathf.Sin(_clock * _c.markers.pulse + i);
                _rings[i].mat.SetColor("_BaseColor", c);
            }
            if (_beacon != null)
                _beacon.SetColor("_BaseColor", Col(_c.markers.beaconColor) * (0.8f + 0.2f * Mathf.Sin(_clock * _c.markers.pulse)));
            StepArrow(dt, ardan, moving);
        }

        // ------------------------------------------------------------ the corridor

        private void Darkness(Material source, Vector2 finish)
        {
            var k = _c.corridor;
            var track = _village.RouteTrack;
            if (track == null || track.Count < 2) return;
            // the mask's bounds: the village and everything along the way
            var min = new Vector2(-k.villageRadius, -k.villageRadius);
            var max = new Vector2(k.villageRadius, k.villageRadius);
            foreach (var p in track)
            {
                min = Vector2.Min(min, p - Vector2.one * (k.trackHalf + 4f));
                max = Vector2.Max(max, p + Vector2.one * (k.trackHalf + 4f));
            }
            var field = _village.FieldBounds;
            min = Vector2.Min(min, field.min - Vector2.one * 4f);
            max = Vector2.Max(max, field.max + Vector2.one * 4f);
            var w = Mathf.CeilToInt((max.x - min.x) / k.texel);
            var h = Mathf.CeilToInt((max.y - min.y) / k.texel);
            var open = new bool[w, h];
            var tex = new Texture2D(w, h, TextureFormat.R8, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "CorridorMask" };
            var px = new byte[w * h];
            for (var j = 0; j < h; j++)
                for (var i = 0; i < w; i++)
                {
                    var p = min + new Vector2((i + 0.5f) * k.texel, (j + 0.5f) * k.texel);
                    var lit = p.magnitude < k.villageRadius
                              || _village.TrackDistance(p) < k.trackHalf
                              || _village.FieldSigned(p) > -k.fieldMargin
                              || (Mathf.Abs(p.x - finish.x) < k.gateHalf && p.y < finish.y + k.gateFront && p.y > finish.y - k.gateBack);
                    open[i, j] = lit;
                    px[j * w + i] = lit ? (byte)255 : (byte)0;
                }
            tex.SetPixelData(px, 0);
            tex.Apply();
            var cam = Camera.main;
            if (cam == null) return;
            UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam).requiresDepthTexture = true;
            var m = new Material(source) { name = "CorridorDark (runtime)" };
            m.SetTexture("_DarkMask", tex);
            m.SetVector("_DarkRect", new Vector4(min.x, min.y, 1f / (w * k.texel), 1f / (h * k.texel)));
            m.SetFloat("_Strength", k.strength);
            m.SetFloat("_Soft", k.soft);
            m.SetFloat("_Wobble", k.wobble);
            m.SetFloat("_MapEdge", 0f);
            m.SetColor("_DarkColor", Col(k.color));
            var quad = new GameObject("CorridorDarkness", typeof(MeshFilter), typeof(MeshRenderer));
            quad.transform.SetParent(cam.transform, false);
            quad.transform.localPosition = Vector3.forward * (cam.nearClipPlane + 0.5f);
            var mesh = new Mesh
            {
                name = "CorridorDarkQuad",
                vertices = new[] { new Vector3(-1f, -1f, 0f), new Vector3(1f, -1f, 0f), new Vector3(1f, 1f, 0f), new Vector3(-1f, 1f, 0f) },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
                bounds = new Bounds(Vector3.zero, Vector3.one * 10000f),
            };
            quad.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = quad.GetComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            // the invisible wall: a box on every dark cell that touches a lit one
            var walls = new GameObject("CorridorWalls").transform;
            walls.SetParent(_root, false);
            var size = new Vector3(k.texel, k.wallHeight, k.texel);
            for (var j = 0; j < h; j++)
                for (var i = 0; i < w; i++)
                {
                    if (open[i, j]) continue;
                    var edge = (i > 0 && open[i - 1, j]) || (i + 1 < w && open[i + 1, j]) || (j > 0 && open[i, j - 1]) || (j + 1 < h && open[i, j + 1]);
                    if (!edge) continue;
                    var go = new GameObject("Wall", typeof(BoxCollider));
                    go.transform.SetParent(walls, false);
                    go.transform.position = new Vector3(min.x + (i + 0.5f) * k.texel, k.wallHeight * 0.5f, min.y + (j + 0.5f) * k.texel);
                    go.GetComponent<BoxCollider>().size = size;
                }
            // the mask's edge itself is dark too: close it off where the lit area meets it
            for (var i = 0; i < w; i++) { EdgeWall(walls, open, i, 0, min, k, size); EdgeWall(walls, open, i, h - 1, min, k, size); }
            for (var j = 0; j < h; j++) { EdgeWall(walls, open, 0, j, min, k, size); EdgeWall(walls, open, w - 1, j, min, k, size); }
        }

        private static void EdgeWall(Transform walls, bool[,] open, int i, int j, Vector2 min, Corridor k, Vector3 size)
        {
            if (!open[i, j]) return;
            var go = new GameObject("Wall", typeof(BoxCollider));
            go.transform.SetParent(walls, false);
            go.transform.position = new Vector3(min.x + (i + 0.5f) * k.texel, k.wallHeight * 0.5f, min.y + (j + 0.5f) * k.texel);
            go.GetComponent<BoxCollider>().size = size;
        }

        // ------------------------------------------------------------ the marks

        private void Marks(Material spark, Vector2 finish)
        {
            var m = _c.markers;
            var flag = _village.Model(m.flag);
            if (_checkpoints != null)
                for (var i = 0; i < _checkpoints.Length; i++)
                {
                    var k = _checkpoints[i];
                    var at = new Vector3(k.x, 0f, k.z);
                    var mat = new Material(spark) { name = "CheckpointRing" };
                    var ring = Ring("CheckpointRing", at, m.ringRadius, m.ringWidth, mat);
                    _rings.Add((ring, mat));
                    if (flag != null)
                    {
                        var go = UnityEngine.Object.Instantiate(flag, _root);
                        go.name = "CheckpointFlag";
                        go.transform.SetPositionAndRotation(at + new Vector3(m.ringRadius * 0.8f, 0f, 0f), flag.transform.rotation);
                        go.transform.localScale = flag.transform.localScale * m.flagScale;
                        CineTwoSided.Apply(go);
                    }
                }
            // the end: a pillar of light before the gate, and a ring at its foot
            _beacon = new Material(spark) { name = "FinishBeacon" };
            var end = new Vector3(finish.x, 0f, finish.y);
            Ring("FinishRing", end, m.ringRadius * 1.3f, m.ringWidth, _beacon);
            var pillar = new GameObject("FinishBeacon", typeof(MeshFilter), typeof(MeshRenderer));
            pillar.transform.SetParent(_root, false);
            pillar.transform.position = end;
            var mesh = new Mesh { name = "Beacon" };
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            for (var q = 0; q < 3; q++)
            {
                var a = q * Mathf.PI / 3f;
                var side = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * m.beaconWidth * 0.5f;
                var b = v.Count;
                v.Add(-side); v.Add(side); v.Add(side + Vector3.up * m.beaconHeight); v.Add(-side + Vector3.up * m.beaconHeight);
                uv.Add(new Vector2(0f, 0.5f)); uv.Add(new Vector2(1f, 0.5f)); uv.Add(new Vector2(1f, 0.5f)); uv.Add(new Vector2(0f, 0.5f));
                tri.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3, b, b + 2, b + 1, b, b + 3, b + 2 });
            }
            // fading upwards: the colour's alpha by height
            var colours = new List<Color>();
            foreach (var p in v) colours.Add(new Color(1f, 1f, 1f, 1f - p.y / m.beaconHeight));
            mesh.SetVertices(v);
            mesh.SetUVs(0, uv);
            mesh.SetColors(colours);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            pillar.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = pillar.GetComponent<MeshRenderer>();
            r.sharedMaterial = _beacon;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>A flat glowing ring on the ground (the spark's soft dot stretched round it).</summary>
        private Transform Ring(string name, Vector3 at, float radius, float width, Material mat)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(_root, false);
            go.transform.position = at + Vector3.up * 0.03f;
            const int n = 40;
            var v = new Vector3[n * 2 + 2];
            var uv = new Vector2[v.Length];
            var tri = new int[n * 6];
            for (var i = 0; i <= n; i++)
            {
                var a = Mathf.PI * 2f * i / n;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v[i * 2] = d * (radius - width * 0.5f);
                v[i * 2 + 1] = d * (radius + width * 0.5f);
                uv[i * 2] = new Vector2(0.5f, 0f);
                uv[i * 2 + 1] = new Vector2(0.5f, 1f);
                if (i == n) break;
                var k = i * 6;
                tri[k] = i * 2; tri[k + 1] = i * 2 + 2; tri[k + 2] = i * 2 + 1;
                tri[k + 3] = i * 2 + 1; tri[k + 4] = i * 2 + 2; tri[k + 5] = i * 2 + 3;
            }
            var mesh = new Mesh { name = name, vertices = v, uv = uv, triangles = tri };
            mesh.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return go.transform;
        }

        // ------------------------------------------------------------ the arrow

        private void Arrow(Material spark)
        {
            var g = _c.guide;
            var go = new GameObject("GuideArrow", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(_root, false);
            var s = g.size;
            // a flat arrow pointing +z: a stem and a head, the dot's bright middle all over it
            var v = new[]
            {
                new Vector3(-0.12f, 0f, -0.5f) * s, new Vector3(0.12f, 0f, -0.5f) * s, new Vector3(0.12f, 0f, 0.05f) * s, new Vector3(-0.12f, 0f, 0.05f) * s,
                new Vector3(-0.32f, 0f, 0.05f) * s, new Vector3(0.32f, 0f, 0.05f) * s, new Vector3(0f, 0f, 0.5f) * s,
            };
            var uv = new Vector2[v.Length];
            for (var i = 0; i < uv.Length; i++) uv[i] = new Vector2(0.5f, 0.5f);
            var mesh = new Mesh { name = "GuideArrow", vertices = v, uv = uv, triangles = new[] { 0, 3, 2, 0, 2, 1, 4, 6, 5 } };
            mesh.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            _arrowMat = new Material(spark) { name = "GuideArrow" };
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = _arrowMat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            _arrow = go.transform;
            _arrow.gameObject.SetActive(false);
        }

        /// <summary>Shown when he has stood still a moment, or strayed from the track: at his feet,
        /// pointing to the track `ahead` metres on from the nearest point of it.</summary>
        private void StepArrow(float dt, Vector3 ardan, bool moving)
        {
            if (_arrow == null) return;
            var g = _c.guide;
            _idle = moving ? 0f : _idle + dt;
            var me = new Vector2(ardan.x, ardan.z);
            var off = _village.TrackDistance(me) > g.offRoute;
            var show = _idle > g.idleSeconds || off;
            var target = _village.TrackAhead(me, g.ahead);
            var to = target - me;
            if (!show || to.sqrMagnitude < 0.25f)
            {
                _arrow.gameObject.SetActive(false);
                return;
            }
            _arrow.gameObject.SetActive(true);
            var dir = new Vector3(to.x, 0f, to.y).normalized;
            var bob = 0.12f * Mathf.Sin(_clock * 4f);
            _arrow.SetPositionAndRotation(ardan + dir * (g.distance + bob) + Vector3.up * 0.04f, Quaternion.LookRotation(dir));
            _arrowMat.SetColor("_BaseColor", Col(g.color) * (0.75f + 0.25f * Mathf.Sin(_clock * 4f)));
        }

        // ------------------------------------------------------------ the runners

        private Run MakeRun(Transform t, CineFigure fig, Runner r)
        {
            var path = new Vector3[r.path.Length / 2];
            for (var i = 0; i < path.Length; i++) path[i] = new Vector3(r.path[i * 2], 0f, r.path[i * 2 + 1]);
            var len = 0f;
            for (var i = 0; i + 1 < path.Length; i++) len += (path[i + 1] - path[i]).magnitude;
            var run = new Run
            {
                t = t, fig = fig, path = path, len = len, speed = r.speed, hop = r.hop,
                trigger = r.trigger != null && r.trigger.Length >= 2 ? new Vector2(r.trigger[0], r.trigger[1]) : Vector2.zero,
                radius = r.trigger != null && r.trigger.Length >= 2 ? r.radius : -1f,
            };
            t.position = path[0];
            if (path.Length > 1) t.rotation = Quaternion.LookRotation(path[1] - path[0]) * (fig != null ? Quaternion.identity : Quaternion.Euler(0f, r.yaw, 0f));
            if (fig != null) fig.Play("Idle", 0f);
            // the film's people are already in sight: they set off at once; the rest wait to be seen
            if (run.radius < 0f) run.on = true;
            else t.gameObject.SetActive(false);
            return run;
        }

        private void StepRun(Run r, Vector2 me, float dt)
        {
            if (r.done) { if (r.fig != null && r.t.gameObject.activeSelf) r.fig.Tick(dt); if (r.chaser != null && r.chaser.gameObject.activeSelf) r.chaser.Tick(dt); return; }
            if (!r.on)
            {
                if ((me - r.trigger).magnitude > r.radius) return;
                r.on = true;
                r.t.gameObject.SetActive(true);
                if (r.chaser != null)
                {
                    r.chaser.gameObject.SetActive(true);
                    r.chaser.transform.position = r.path[0] - (r.path[1] - r.path[0]).normalized * r.gap;
                }
            }
            r.s += r.speed * dt;
            var at = Along(r.path, Mathf.Min(r.s, r.len), out var dir);
            var hop = r.hop > 0f ? Mathf.Abs(Mathf.Sin(r.s * 6f)) * r.hop : 0f;
            r.t.position = at + Vector3.up * hop;
            if (dir.sqrMagnitude > 0f) r.t.rotation = Quaternion.Slerp(r.t.rotation, Quaternion.LookRotation(dir), Mathf.Clamp01(10f * dt));
            if (r.fig != null)
            {
                var natural = r.fig.Clips != null ? r.fig.Clips.runSpeed : r.speed;
                r.fig.Play("Run", 0.2f, natural > 0f ? r.speed / natural : 1f);
                r.fig.Tick(dt);
            }
            if (r.chaser != null)
            {
                var back = Along(r.path, Mathf.Max(0f, Mathf.Min(r.s, r.len) - r.gap), out var cdir);
                r.chaser.transform.SetPositionAndRotation(back, cdir.sqrMagnitude > 0f ? Quaternion.LookRotation(cdir) : r.chaser.transform.rotation);
                var natural = r.chaser.Clips != null ? r.chaser.Clips.runSpeed : r.speed;
                r.chaser.Play("Run", 0.2f, natural > 0f ? r.speed / natural : 1f);
                r.chaser.Tick(dt);
            }
            if (r.s < r.len) return;
            r.done = true;
            if (r.falls && r.fig != null)
            {
                // run down: the villager falls and lies; the orc stands over him
                Lie(r.fig.transform);
                r.fig.Play("Idle", 0.1f);
                r.chaser?.Play("Idle", 0.2f);
            }
            else r.t.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------ the placed

        private void Put(Func<string, OpeningLife.CastMember?> cast, Placed p, bool dead)
        {
            var fig = Figure(cast, p.figure);
            if (fig == null) return;
            fig.transform.SetPositionAndRotation(new Vector3(p.x, 0f, p.z), Quaternion.Euler(0f, p.yaw, 0f));
            fig.Play(string.IsNullOrEmpty(p.clip) ? "Idle" : p.clip, 0f, 1f, Mathf.Repeat(p.x * 7.3f + p.z, 2f));
            fig.Tick(0f);
            if (dead || p.lying) Lie(fig.transform);
            else _runs.Add(new Run { t = fig.transform, fig = fig, done = true, path = new[] { fig.transform.position } });
        }

        /// <summary>Lying on his back where he fell: the figure turned over flat.</summary>
        private static void Lie(Transform t)
        {
            t.rotation = Quaternion.Euler(-90f, t.eulerAngles.y, 0f);
            t.position += Vector3.up * 0.06f;
        }

        private CineFigure Figure(Func<string, OpeningLife.CastMember?> cast, string figure)
        {
            var m = cast(figure);
            if (!m.HasValue || m.Value.model == null) return null;
            var go = UnityEngine.Object.Instantiate(m.Value.model, _root);
            go.name = "World_" + figure;
            var fig = go.AddComponent<CineFigure>();
            fig.Init(m.Value.clips);
            return fig;
        }

        private static Vector3 Along(Vector3[] path, float s, out Vector3 dir)
        {
            for (var i = 0; i + 1 < path.Length; i++)
            {
                var seg = path[i + 1] - path[i];
                var l = seg.magnitude;
                if (l < 1e-5f) continue;
                dir = seg / l;
                if (s <= l || i + 2 == path.Length) return path[i] + dir * Mathf.Min(s, l);
                s -= l;
            }
            dir = Vector3.zero;
            return path[path.Length - 1];
        }

        private static Color Col(float[] a) => a != null && a.Length >= 3 ? new Color(a[0], a[1], a[2], a.Length > 3 ? a[3] : 1f) : Color.white;
    }
}
