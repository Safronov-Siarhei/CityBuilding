using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Ардан's blows made visible (the user's third playtest, 2026-11-12: «атаки ардана скучные, нужно добавить им
    /// вариацию и эффекты атаки»): every blow leaves a bright crescent where the blade went -- three in turn, from the
    /// right, from the left and from above -- and a spray of sparks where it lands; each skill has its own:
    ///   Cry    -- a golden ring rushing out over the ground and embers flying up;
    ///   Whirl  -- a full circle of the blade round him and sparks thrown outwards;
    ///   Dash   -- dust kicked up behind him all along the dash;
    ///   Breath -- green motes spiralling up round him.
    /// One shared world-space particle system (fxGlow) takes every burst through Emit, and a handful of pooled
    /// crescent meshes fade out: nothing is made or destroyed per blow.
    /// </summary>
    public partial class CityDefenceMode
    {
        private ParticleSystem _fx;
        private readonly List<(Transform t, Material m, float age, float life, float spin)> _arcs = new List<(Transform, Material, float, float, float)>();
        private readonly Stack<(Transform t, Material m)> _arcPool = new Stack<(Transform, Material)>();
        private Mesh _arcMesh, _ringMesh;
        private int _blow;

        private static readonly Color SlashColour = new Color(1f, 0.96f, 0.82f, 0.9f);
        private static readonly Color SparkColour = new Color(1f, 0.78f, 0.35f, 1f);
        private static readonly Color CryColour = new Color(1f, 0.82f, 0.3f, 1f);
        private static readonly Color BreathColour = new Color(0.55f, 1f, 0.55f, 1f);
        private static readonly Color DustColour = new Color(0.72f, 0.62f, 0.48f, 0.7f);

        private void StartFx()
        {
            if (_fx != null || fxGlow == null) return;
            var go = new GameObject("M2Fx", typeof(ParticleSystem));
            go.transform.SetParent(transform, false);
            _fx = go.GetComponent<ParticleSystem>();
            _fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _fx.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 600;
            main.gravityModifier = 0.6f;
            var em = _fx.emission;
            em.rateOverTime = 0f;
            var shape = _fx.shape;
            shape.enabled = false;
            var sol = _fx.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = fxGlow;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            _fx.Play();
            _arcMesh = Crescent(0.75f, 0.18f, 150f, 20);
            _ringMesh = Crescent(1f, 0.16f, 360f, 48);
        }

        /// <summary>A flat crescent (XZ plane, round +Z) whose ends fade; UVs on the soft dot's bright middle.</summary>
        private static Mesh Crescent(float radius, float width, float degrees, int steps)
        {
            var v = new List<Vector3>();
            var c = new List<Color>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            for (var i = 0; i <= steps; i++)
            {
                var k = i / (float)steps;
                var a = Mathf.Deg2Rad * (-degrees * 0.5f + degrees * k);
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                var fade = degrees >= 360f ? 1f : Mathf.Sin(k * Mathf.PI);
                var thick = width * (0.25f + 0.75f * fade);
                v.Add(dir * (radius - thick * 0.5f));
                v.Add(dir * (radius + thick * 0.5f));
                c.Add(new Color(1f, 1f, 1f, 0f));
                c.Add(new Color(1f, 1f, 1f, fade));
                uv.Add(new Vector2(0.5f, 0.5f));
                uv.Add(new Vector2(0.5f, 0.5f));
                if (i == 0) continue;
                var j = 2 * i;
                tri.AddRange(new[] { j - 2, j - 1, j, j - 1, j + 1, j });
            }
            var mesh = new Mesh { name = "M2Crescent" };
            mesh.SetVertices(v);
            mesh.SetColors(c);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private void Arc(Mesh mesh, Vector3 at, Quaternion turn, float scale, Color colour, float life, float spin)
        {
            if (mesh == null) return;
            (Transform t, Material m) arc;
            if (_arcPool.Count > 0) arc = _arcPool.Pop();
            else
            {
                var go = new GameObject("M2Arc", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(transform, false);
                var m = new Material(fxGlow);
                var r = go.GetComponent<MeshRenderer>();
                r.sharedMaterial = m;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                arc = (go.transform, m);
            }
            arc.t.GetComponent<MeshFilter>().sharedMesh = mesh;
            arc.t.SetPositionAndRotation(at, turn);
            arc.t.localScale = Vector3.one * scale;
            arc.m.color = colour;
            arc.t.gameObject.SetActive(true);
            _arcs.Add((arc.t, arc.m, 0f, life, spin));
        }

        private void TickFx(float dt)
        {
            for (var i = _arcs.Count - 1; i >= 0; i--)
            {
                var (t, m, age, life, spin) = _arcs[i];
                age += dt;
                if (age >= life || t == null)
                {
                    if (t != null)
                    {
                        t.gameObject.SetActive(false);
                        _arcPool.Push((t, m));
                    }
                    _arcs.RemoveAt(i);
                    continue;
                }
                var k = age / life;
                var c = m.color;
                c.a = Mathf.Lerp(0.95f, 0f, k * k);
                m.color = c;
                t.localScale *= 1f + dt * 1.5f;
                if (spin != 0f) t.Rotate(0f, spin * dt, 0f, Space.Self);
                _arcs[i] = (t, m, age, life, spin);
            }
        }

        private void Sparks(Vector3 at, Color colour, int count, float speed, float size, float life, Vector3 bias, float up = 0.5f)
        {
            if (_fx == null) return;
            var p = new ParticleSystem.EmitParams();
            for (var i = 0; i < count; i++)
            {
                var dir = Random.insideUnitSphere;
                dir.y = Mathf.Abs(dir.y) + up;
                p.position = at;
                p.velocity = (dir.normalized + bias) * speed * Random.Range(0.6f, 1.2f);
                p.startColor = colour;
                p.startSize = size * Random.Range(0.6f, 1.3f);
                p.startLifetime = life * Random.Range(0.6f, 1.2f);
                _fx.Emit(p, 1);
            }
        }

        // ------------------------------------------------------------------ what calls them

        /// <summary>A blow of his: the crescent (right, left, overhead in turn) and sparks where it lands.</summary>
        private void BlowFx()
        {
            if (Arden == null) return;
            StartFx();
            var t = Arden.transform;
            var chest = t.position + Vector3.up * 0.45f + t.forward * 0.25f;
            var face = Quaternion.LookRotation(t.forward);
            Quaternion tilt;
            switch (_blow++ % 3)
            {
                case 0: tilt = Quaternion.Euler(0f, 0f, 25f); break;            // from the right, falling
                case 1: tilt = Quaternion.Euler(0f, 0f, -25f) * Quaternion.Euler(0f, 0f, 180f); break;   // back from the left
                default: tilt = Quaternion.Euler(-70f, 0f, 0f) * Quaternion.Euler(0f, 0f, 90f); break;   // from above
            }
            Arc(_arcMesh, chest, face * tilt, 1f, SlashColour, 0.18f, 0f);
            Sparks(t.position + Vector3.up * 0.45f + t.forward * 0.65f, SparkColour, 7, 2.2f, 0.07f, 0.35f, t.forward * 0.4f);
        }

        private void SkillFx(int skill)
        {
            if (Arden == null) return;
            StartFx();
            var at = Arden.transform.position;
            var b = B;
            switch (skill)
            {
                case Cry:
                    Arc(_ringMesh, at + Vector3.up * 0.08f, Quaternion.identity, 0.6f, CryColour, 0.55f, 0f);
                    StartCoroutine(Grow(_arcs.Count - 1, b.cryRadius));
                    Sparks(at + Vector3.up * 0.6f, CryColour, 30, 2.6f, 0.09f, 0.8f, Vector3.up * 0.6f, 0.2f);
                    break;
                case Whirl:
                    Arc(_ringMesh, at + Vector3.up * 0.45f, Quaternion.identity, Mathf.Max(0.6f, b.whirlRadius * 0.55f), SlashColour, 0.35f, 900f);
                    for (var i = 0; i < 16; i++)
                    {
                        var a = i / 16f * Mathf.PI * 2f;
                        var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                        Sparks(at + Vector3.up * 0.45f + dir * 0.6f, SparkColour, 1, 3.5f, 0.08f, 0.4f, dir * 1.5f, 0.1f);
                    }
                    break;
                case Dash:
                    StartCoroutine(DashDust());
                    break;
                default:
                    StartCoroutine(BreathSwirl(Mathf.Max(0.5f, b.breathSeconds)));
                    break;
            }
        }

        private System.Collections.IEnumerator Grow(int index, float radius)
        {
            if (index < 0 || index >= _arcs.Count) yield break;
            var t = _arcs[index].t;
            var target = Mathf.Max(0.5f, radius);
            for (var k = 0f; k < 1f && t != null && t.gameObject.activeSelf; k += Time.deltaTime / 0.45f)
            {
                t.localScale = Vector3.one * Mathf.Lerp(0.6f, target, 1f - (1f - k) * (1f - k));
                yield return null;
            }
        }

        private System.Collections.IEnumerator DashDust()
        {
            while (Arden != null && Arden.IsDashing)
            {
                var t = Arden.transform;
                Sparks(t.position + Vector3.up * 0.1f - t.forward * 0.3f, DustColour, 3, 0.9f, 0.22f, 0.6f, -t.forward * 0.5f, 0.3f);
                yield return null;
            }
        }

        private System.Collections.IEnumerator BreathSwirl(float seconds)
        {
            for (var time = 0f; time < seconds && Arden != null; time += Time.deltaTime)
            {
                var at = Arden.transform.position;
                var a = time * 9f;
                for (var s = 0; s < 2; s++)
                {
                    var angle = a + s * Mathf.PI;
                    var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    Sparks(at + dir * 0.45f + Vector3.up * 0.1f, BreathColour, 1, 0.9f, 0.1f, 0.9f, Vector3.up * 1.2f, 0.8f);
                }
                yield return null;
            }
        }
    }
}
