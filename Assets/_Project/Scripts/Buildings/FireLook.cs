using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// A burning building's look (sandbox fires v2, the user 2026-11-14): flames and smoke rising off
    /// its roof -- the opening film's house fires (Campaign/OpeningLife) at the game's own scale -- that
    /// grow with the fire's strength, and a puff of steam where a bucket lands.
    /// </summary>
    public class FireLook
    {
        private readonly ParticleSystem _flames;
        private readonly ParticleSystem _smoke;
        private readonly float _flameRate;
        private readonly float _smokeRate;
        private readonly GameObject _root;

        public FireLook(Transform building, Material flame, Material smoke)
        {
            var bounds = Bounds(building);
            var size = Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.z), 0.6f, 4f);
            _root = new GameObject("FireLook");
            _root.transform.SetParent(building, false);
            _root.transform.position = new Vector3(bounds.center.x, bounds.max.y - 0.25f * bounds.size.y, bounds.center.z);
            _flameRate = 22f * size;
            _smokeRate = 5f * size;
            if (flame != null)
            {
                _flames = Particles(_root.transform, "Flames", flame, _flameRate, 0.5f, 1f, 0.8f, 1.6f, 0.12f * size, 0.22f * size,
                    new Vector3(bounds.size.x * 0.6f, 0.1f, bounds.size.z * 0.6f), 1f, false);
                var col = _flames.colorOverLifetime;
                col.enabled = true;
                col.color = new ParticleSystem.MinMaxGradient(Gradient(
                    new[] { new Color(1f, 0.92f, 0.5f), new Color(1f, 0.55f, 0.12f), new Color(0.8f, 0.18f, 0.05f) },
                    new[] { 0.9f, 0.8f, 0f }));
            }
            if (smoke != null)
            {
                _smoke = Particles(_root.transform, "Smoke", smoke, _smokeRate, 3f, 5f, 0.7f, 1f, 0.2f * size, 0.35f * size,
                    new Vector3(bounds.size.x * 0.4f, 0.1f, bounds.size.z * 0.4f), 2.5f, true);
                var sc = _smoke.colorOverLifetime;
                sc.enabled = true;
                sc.color = new ParticleSystem.MinMaxGradient(Gradient(
                    new[] { new Color(0.3f, 0.27f, 0.25f), new Color(0.42f, 0.4f, 0.4f), new Color(0.55f, 0.55f, 0.56f) },
                    new[] { 0f, 0.55f, 0f }));
            }
        }

        /// <summary>The fire's strength 0..100: a small fire is a few licks, a full one the whole roof.</summary>
        public void Strength(float strength)
        {
            var k = Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(strength / 100f));
            if (_flames != null)
            {
                var em = _flames.emission;
                em.rateOverTime = _flameRate * k;
            }
            if (_smoke != null)
            {
                var em = _smoke.emission;
                em.rateOverTime = _smokeRate * k;
            }
        }

        /// <summary>A bucket landed: a puff of white steam.</summary>
        public void Steam()
        {
            if (_smoke == null) return;
            var p = new ParticleSystem.EmitParams { startColor = new Color(0.92f, 0.94f, 0.96f, 0.9f), applyShapeToPosition = true };
            _smoke.Emit(p, 8);
        }

        public void Destroy()
        {
            if (_root != null) Object.Destroy(_root);
        }

        public static Bounds Bounds(Transform t)
        {
            var renderers = t.GetComponentsInChildren<Renderer>();
            var b = new Bounds(t.position + Vector3.up, Vector3.one);
            var first = true;
            foreach (var r in renderers)
            {
                if (r is ParticleSystemRenderer || r.name == "FireMark") continue;
                if (first) { b = r.bounds; first = false; }
                else b.Encapsulate(r.bounds);
            }
            return b;
        }

        public static ParticleSystem Particles(Transform parent, string name, Material material, float rate,
            float lifeMin, float lifeMax, float speedMin, float speedMax, float sizeMin, float sizeMax,
            Vector3 box, float grow, bool smoke)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);       // the box emits up
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
            main.maxParticles = smoke ? 50 : 70;     // mobile: a town may burn in several places
            var em = ps.emission;
            em.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(box.x, box.z, box.y);
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, smoke ? AnimationCurve.Linear(0f, 1f, 1f, grow) : AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = material;
            r.sortingFudge = smoke ? 10f : 0f;
            if (!smoke)
            {
                r.renderMode = ParticleSystemRenderMode.Stretch;
                r.velocityScale = 0.18f;
                r.lengthScale = 1.4f;
            }
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        public static Gradient Gradient(Color[] colours, float[] alphas)
        {
            var g = new Gradient();
            var ck = new GradientColorKey[colours.Length];
            var ak = new GradientAlphaKey[alphas.Length];
            for (var i = 0; i < colours.Length; i++) ck[i] = new GradientColorKey(colours[i], i / (float)(colours.Length - 1));
            for (var i = 0; i < alphas.Length; i++) ak[i] = new GradientAlphaKey(alphas[i], i / (float)(alphas.Length - 1));
            g.SetKeys(ck, ak);
            return g;
        }
    }
}
