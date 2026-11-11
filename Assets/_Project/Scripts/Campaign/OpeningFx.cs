using System;
using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Random = System.Random;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// The opening film's effects (the user's choice, 2026-11-10): the portal -- runes lighting one by
    /// one, glowing cracks running out of the platform when the shaking starts, blue arcs and dust as
    /// the stones fly into the arch, the swirl opening from its heart with a flash, a shock ring and
    /// flying debris, sparks drawn into it; the morning turning to a lilac-crimson pall once it is
    /// open; bloom, grading, a vignette and depth of field on the close shots; chimney smoke over the
    /// peaceful village; the title. Every number is in the "fx" block of Campaign/OpeningScript.json;
    /// the director steps it on the film's clock (Step), so the editor renders it frame by frame.
    /// </summary>
    public class OpeningFx : MonoBehaviour
    {
        [Serializable] private class File { public Fx fx; }

        [Serializable] private class Fx
        {
            public float runesStart, runesStep, runesRamp, runesBrightness;
            public float[] runeColor;
            public float cracksStart, cracksSeconds, cracksRadius, cracksInner, cracksHeight;
            public float[] cracksColor;
            public float assemblyStart, assemblySeconds, arcRefresh, arcWidth, arcJitter;
            public int arcCount, dustPerStone;
            public float swirlStart, openDelay, openSeconds, swirlSpeed;
            public float[] doorCentre, doorSize;
            public float[] vortexA, vortexB;
            public float blastFlash, blastLight, blastLightRange, ringSeconds, ringRadius, debrisSpeed, debrisSize;
            public int debris;
            public float sparksRate, sparksRadius, sparksLife, sparksSize;
            public float moodStart, moodSeconds, sunIntensity, skyExposure, skyAtmosphere, fogEnd, fogStart;
            public float stormStart, stormSeconds, stormScale, stormSpeed;
            public float[] stormDark, stormLight;
            public float[] sunColor, skyTint, fogColor, ambientSky, ambientEquator, ambientGround;
            public float bloomThreshold, bloomIntensity, vignette, saturation, contrast, dofAperture;
            public float smokeRate, smokeLife, smokeSize, smokeSpeed, smokeStop;
            public float[] chimneySigns;
            public float amuletGlowSize;
            public string titleKey;
            public float titleStart, titleSeconds, titleFade;
        }

        [Serializable] private class Chimneys { public House[] houses; }
        [Serializable] private class House { public string id; public float[] tops; }
        [Serializable] private class Tracks { public float fps; public ObjectTrack[] objects; }
        [Serializable] private class ObjectTrack { public string name; public string piece; public int frames; public float[] m; }

        [SerializeField] private OpeningVillage village;
        [SerializeField] private TextAsset scriptFile;
        [SerializeField] private TextAsset tracksFile;
        [SerializeField] private TextAsset chimneysFile;
        [SerializeField] private Material vortexMaterial;
        [SerializeField] private Material groundMaterial;
        [SerializeField] private Material sparkMaterial;
        [SerializeField] private Material dustMaterial;
        [SerializeField] private Material debrisMaterial;
        [SerializeField] private Material stormMaterial;
        [SerializeField] private Volume volume;
        [SerializeField] private Text title;

        private Fx _c;
        private Tracks _k;
        private Transform _root;
        private readonly Random _rnd = new Random(3);
        private readonly List<ParticleSystem> _systems = new List<ParticleSystem>();
        private readonly List<(Renderer r, float start)> _runes = new List<(Renderer, float)>();
        private readonly List<(ObjectTrack track, float lift, float land)> _flights = new List<(ObjectTrack, float, float)>();
        private readonly List<LineRenderer> _arcs = new List<LineRenderer>();
        private MaterialPropertyBlock _mpb;
        private Material _vortex, _cracks, _ring, _sky, _glow;
        private Transform _vortexQuad;
        private ParticleSystem _sparks, _debris, _dust;
        private readonly List<ParticleSystem> _chimneySmoke = new List<ParticleSystem>();
        private Light _blast;
        private float _arcTimer;
        private bool _blasted, _dusted;
        private readonly HashSet<int> _dustDone = new HashSet<int>();
        private float _sunI0, _exp0, _atm0, _fogEnd0, _fogStart0;
        private Material _storm;
        private Transform _stormDome;
        private Color _sunC0, _tint0, _fog0, _amb0, _equ0, _gnd0;
        private Bloom _bloom;
        private DepthOfField _dof;

        public void Begin()
        {
            _c = JsonUtility.FromJson<File>(scriptFile.text).fx;
            _k = JsonUtility.FromJson<Tracks>(tracksFile.text);
            if (_root != null) Kill(_root.gameObject);
            _root = new GameObject("Fx").transform;
            _root.SetParent(transform, false);
            _systems.Clear();
            _runes.Clear();
            _flights.Clear();
            _arcs.Clear();
            _chimneySmoke.Clear();
            _dustDone.Clear();
            _blasted = _dusted = false;
            _mpb = new MaterialPropertyBlock();

            _glow = new Material(groundMaterial);
            _glow.SetFloat("_Mode", 2f);
            _glow.SetFloat("_Fade", 1f);
            Runes();
            _cracks = Ground("Cracks", _c.cracksRadius, 0f, _c.cracksColor, _c.cracksHeight);
            _cracks.SetFloat("_Inner", _c.cracksInner);
            _ring = Ground("ShockRing", _c.ringRadius, 1f, _c.vortexB, 0.012f);
            Flights();
            Vortex();
            Smoke();
            Mood();
            Post();
            if (title != null) title.text = string.Empty;
        }

        /// <summary>The film's clock, the shot's camera and whether it wants a shallow focus.</summary>
        public void Step(float time, float dt, Vector3 camFrom, Vector3 camAt, float lens, bool shallow)
        {
            if (_c == null) return;
            _time = time;
            StepRunes(time);
            StepCracks(time);
            StepAssembly(time, dt);
            StepVortex(time);
            if (_stormDome != null) _stormDome.position = camFrom;
            StepMood(time);
            StepSmoke(time);
            StepTitle(time);
            if (_dof != null)
            {
                _dof.active = shallow;
                _dof.focusDistance.value = Vector3.Distance(camFrom, camAt);
                _dof.focalLength.value = lens;
            }
            if (!Application.isPlaying)
            {
                foreach (var ps in _systems) ps.Simulate(dt, true, false, false);
            }
        }

        // ------------------------------------------------------------ the runes and the cracks

        private void Runes()
        {
            var i = 0;
            foreach (var (stone, glow) in village.StoneGlows())
            {
                var model = village.SetPieceModel(glow);
                if (model == null || stone == null) continue;
                var go = Instantiate(model, stone);
                go.name = glow;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    r.sharedMaterial = _glow;
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    _runes.Add((r, _c.runesStart + _c.runesStep * i));
                    SetGlow(r, 0f);
                }
                i++;
            }
        }

        private void StepRunes(float time)
        {
            foreach (var (r, start) in _runes)
            {
                var u = time - start;
                var g = u < 0f ? 0f : Mathf.Min(1f, u / _c.runesRamp) * (0.75f + 0.25f * Mathf.Sin(time * 13f + start * 37f));
                SetGlow(r, g * _c.runesBrightness);
            }
        }

        private void SetGlow(Renderer r, float g)
        {
            r.GetPropertyBlock(_mpb);
            var c = Col(_c.runeColor) * g;
            c.a = 1f;
            _mpb.SetColor("_Color", c);
            r.SetPropertyBlock(_mpb);
            r.enabled = g > 0.01f;
        }

        private Material Ground(string name, float radius, float mode, float[] colour, float height)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Kill(go.GetComponent<Collider>());
            go.transform.SetParent(_root, false);
            go.transform.SetPositionAndRotation(new Vector3(0f, height, 0f), Quaternion.Euler(90f, 0f, 0f));
            go.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            var m = new Material(groundMaterial);
            m.SetFloat("_Mode", mode);
            m.SetColor("_Color", Col(colour));
            m.SetFloat("_Grow", 0f);
            m.SetFloat("_Fade", 0f);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return m;
        }

        private void StepCracks(float time)
        {
            var u = Mathf.Clamp01((time - _c.cracksStart) / _c.cracksSeconds);
            _cracks.SetFloat("_Grow", 1f - (1f - u) * (1f - u));
            _cracks.SetFloat("_Fade", time < _c.cracksStart ? 0f : 1f);
            _cracks.SetFloat("_T", time);
        }

        // ------------------------------------------------------------ the stones flying into the arch

        private void Flights()
        {
            foreach (var tr in _k.objects)
            {
                if (!tr.name.StartsWith("assembly/")) continue;
                var y0 = tr.m[1];
                float lift = -1f, land = -1f;
                for (var f = 0; f < tr.frames; f++)
                {
                    if (lift < 0f && tr.m[f * 7 + 1] > y0 + 0.06f) lift = f / _k.fps;
                }
                for (var f = tr.frames - 1; f > 0; f--)
                {
                    var d = new Vector3(tr.m[f * 7] - tr.m[(tr.frames - 1) * 7], tr.m[f * 7 + 1] - tr.m[(tr.frames - 1) * 7 + 1], tr.m[f * 7 + 2] - tr.m[(tr.frames - 1) * 7 + 2]);
                    if (d.sqrMagnitude > 1e-6f)
                    {
                        land = (f + 1) / _k.fps;
                        break;
                    }
                }
                if (lift >= 0f) _flights.Add((tr, lift, land));
            }
            for (var i = 0; i < _c.arcCount; i++)
            {
                var go = new GameObject("Arc", typeof(LineRenderer));
                go.transform.SetParent(_root, false);
                var lr = go.GetComponent<LineRenderer>();
                lr.sharedMaterial = sparkMaterial;
                lr.positionCount = 9;
                lr.widthMultiplier = _c.arcWidth;
                lr.startColor = lr.endColor = Col(_c.runeColor);
                lr.shadowCastingMode = ShadowCastingMode.Off;
                lr.enabled = false;
                _arcs.Add(lr);
            }
            _dust = Burst("StoneDust", dustMaterial, false);
        }

        private void StepAssembly(float time, float dt)
        {
            var u = time - _c.assemblyStart;
            var flying = new List<Vector3>();
            for (var i = 0; i < _flights.Count; i++)
            {
                var (tr, lift, land) = _flights[i];
                if (u >= lift && u <= land)
                {
                    var piece = village.Piece(tr.piece);
                    if (piece != null) flying.Add(piece.position);
                }
                if (u >= lift - 0.05f && _dustDone.Add(i))
                {
                    // a puff of dust where the stone left the platform
                    var at = new Vector3(tr.m[0], tr.m[1], tr.m[2]);
                    var emit = new ParticleSystem.EmitParams { applyShapeToPosition = true };
                    for (var k = 0; k < _c.dustPerStone; k++)
                    {
                        emit.position = at + new Vector3(Range(-0.25f, 0.25f), 0.02f, Range(-0.25f, 0.25f));
                        emit.velocity = new Vector3(Range(-0.5f, 0.5f), Range(0.1f, 0.5f), Range(-0.5f, 0.5f));
                        emit.startSize = Range(0.25f, 0.5f);
                        emit.startLifetime = Range(1.0f, 1.8f);
                        emit.startColor = new Color(0.72f, 0.68f, 0.6f, 0.55f);
                        _dust.Emit(emit, 1);
                    }
                }
            }
            _arcTimer -= dt;
            var refresh = _arcTimer <= 0f;
            if (refresh) _arcTimer = _c.arcRefresh;
            for (var i = 0; i < _arcs.Count; i++)
            {
                var lr = _arcs[i];
                var on = flying.Count >= 1 && u >= 0f && u <= _c.assemblySeconds;
                lr.enabled = on;
                if (!on || !refresh) continue;
                // from a flying stone to the next one (or down to the platform's heart)
                var a = flying[_rnd.Next(flying.Count)];
                var b = flying.Count > 1 ? flying[_rnd.Next(flying.Count)] : new Vector3(0f, 0.15f, 0f);
                if ((a - b).sqrMagnitude < 0.01f) b = new Vector3(0f, 0.15f, 0f);
                for (var p = 0; p < lr.positionCount; p++)
                {
                    var f = p / (float)(lr.positionCount - 1);
                    var j = (p == 0 || p == lr.positionCount - 1) ? Vector3.zero
                        : new Vector3(Range(-1f, 1f), Range(-1f, 1f), Range(-1f, 1f)) * _c.arcJitter;
                    lr.SetPosition(p, Vector3.Lerp(a, b, f) + j);
                }
            }
        }

        // ------------------------------------------------------------ the swirl

        private void Vortex()
        {
            // the arch's opening (village frame): its middle, width and height, facing along Z
            var centre = V3(_c.doorCentre);
            var face = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Vortex";
            Kill(go.GetComponent<Collider>());
            go.transform.SetParent(_root, false);
            go.transform.SetPositionAndRotation(centre, face);
            go.transform.localScale = new Vector3(_c.doorSize[0], _c.doorSize[1], 1f);
            _vortex = new Material(vortexMaterial);
            _vortex.SetColor("_ColorA", Col(_c.vortexA));
            _vortex.SetColor("_ColorB", Col(_c.vortexB));
            _vortex.SetFloat("_Aspect", _c.doorSize[0] / Mathf.Max(0.01f, _c.doorSize[1]));
            _vortex.SetFloat("_Open", 0f);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = _vortex;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.enabled = false;
            _vortexQuad = go.transform;

            // sparks and leaves drawn in from round the arch
            _sparks = Burst("PortalSparks", sparkMaterial, true);
            _sparks.transform.SetPositionAndRotation(centre, face);
            var main = _sparks.main;
            main.startLifetime = _c.sparksLife;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(_c.sparksSize * 0.5f, _c.sparksSize);
            main.startColor = new ParticleSystem.MinMaxGradient(Col(_c.vortexB), Color.white);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var shape = _sparks.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = _c.sparksRadius;
            shape.radiusThickness = 0.15f;
            var vel = _sparks.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.radial = new ParticleSystem.MinMaxCurve(-_c.sparksRadius / _c.sparksLife);
            vel.orbitalZ = new ParticleSystem.MinMaxCurve(2.5f);
            vel.x = vel.y = vel.z = new ParticleSystem.MinMaxCurve(0f);
            vel.orbitalX = vel.orbitalY = new ParticleSystem.MinMaxCurve(0f);
            var em = _sparks.emission;
            em.rateOverTime = 0f;

            _debris = Burst("Debris", debrisMaterial, false);
            var dm = _debris.main;
            dm.gravityModifier = 1f;
            var dr = _debris.GetComponent<ParticleSystemRenderer>();
            dr.renderMode = ParticleSystemRenderMode.Mesh;
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dr.mesh = cube.GetComponent<MeshFilter>().sharedMesh;
            Kill(cube);
            dr.shadowCastingMode = ShadowCastingMode.On;

            var lightGO = new GameObject("BlastLight", typeof(Light));
            lightGO.transform.SetParent(_root, false);
            lightGO.transform.position = centre + Vector3.back * 0.6f;
            _blast = lightGO.GetComponent<Light>();
            _blast.type = LightType.Point;
            _blast.color = Col(_c.vortexB);
            _blast.range = _c.blastLightRange;
            _blast.intensity = 0f;
        }

        private void StepVortex(float time)
        {
            if (_vortex == null) return;
            var t0 = _c.swirlStart + _c.openDelay;
            var u = Mathf.Clamp01((time - t0) / _c.openSeconds);
            _vortexQuad.GetComponent<Renderer>().enabled = time >= t0;
            _vortex.SetFloat("_Open", 1f - (1f - u) * (1f - u) * (1f - u));
            _vortex.SetFloat("_T", (time - t0) * _c.swirlSpeed);
            var since = time - t0;
            var flash = since >= 0f ? Mathf.Exp(-since / Mathf.Max(0.01f, _c.blastFlash)) : 0f;
            _vortex.SetFloat("_Flash", flash * 0.8f);
            _blast.intensity = flash * _c.blastLight;
            var em = _sparks.emission;
            em.rateOverTime = time >= t0 ? _c.sparksRate * Mathf.Clamp01(since / 0.8f) : 0f;

            var ring = Mathf.Clamp01(since / _c.ringSeconds);
            _ring.SetFloat("_Grow", since >= 0f ? 1f - (1f - ring) * (1f - ring) : 0f);
            _ring.SetFloat("_Fade", since >= 0f ? 1f - ring : 0f);
            if (!_blasted && since >= 0f)
            {
                _blasted = true;
                var emit = new ParticleSystem.EmitParams();
                for (var k = 0; k < _c.debris; k++)
                {
                    var a = Range(0f, Mathf.PI * 2f);
                    var dir = new Vector3(Mathf.Cos(a), Range(0.4f, 1.1f), Mathf.Sin(a)).normalized;
                    emit.position = new Vector3(Mathf.Cos(a) * Range(0.8f, 1.6f), Range(0.1f, 0.4f), Mathf.Sin(a) * Range(0.8f, 1.6f));
                    emit.velocity = dir * Range(0.5f, 1f) * _c.debrisSpeed;
                    emit.startSize = Range(0.4f, 1f) * _c.debrisSize;
                    emit.startLifetime = Range(1.2f, 2f);
                    emit.rotation3D = new Vector3(Range(0f, 360f), Range(0f, 360f), Range(0f, 360f));
                    emit.angularVelocity3D = new Vector3(Range(-400f, 400f), Range(-400f, 400f), Range(-400f, 400f));
                    emit.startColor = Color.Lerp(new Color(0.45f, 0.43f, 0.42f), new Color(0.62f, 0.6f, 0.57f), Range(0f, 1f));
                    _debris.Emit(emit, 1);
                }
            }
        }

        // ------------------------------------------------------------ the sky turning, the post

        private void Mood()
        {
            var sun = RenderSettings.sun;
            _sunI0 = sun != null ? sun.intensity : 1f;
            _sunC0 = sun != null ? sun.color : Color.white;
            if (RenderSettings.skybox != null)
            {
                _sky = new Material(RenderSettings.skybox);
                RenderSettings.skybox = _sky;
                _tint0 = _sky.GetColor("_SkyTint");
                _exp0 = _sky.GetFloat("_Exposure");
                _atm0 = _sky.GetFloat("_AtmosphereThickness");
            }
            _fog0 = RenderSettings.fogColor;
            _fogEnd0 = RenderSettings.fogEndDistance;
            _fogStart0 = RenderSettings.fogStartDistance;
            if (stormMaterial != null)
            {
                var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dome.name = "StormSky";
                Kill(dome.GetComponent<Collider>());
                dome.transform.SetParent(_root, false);
                dome.transform.localScale = Vector3.one * 300f;
                _storm = new Material(stormMaterial);
                _storm.SetColor("_Dark", Col(_c.stormDark));
                _storm.SetColor("_Light", Col(_c.stormLight));
                _storm.SetFloat("_Scale", _c.stormScale);
                _storm.SetFloat("_Speed", _c.stormSpeed);
                _storm.SetFloat("_Cover", 0f);
                var r = dome.GetComponent<Renderer>();
                r.sharedMaterial = _storm;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                _stormDome = dome.transform;
            }
            _amb0 = RenderSettings.ambientSkyColor;
            _equ0 = RenderSettings.ambientEquatorColor;
            _gnd0 = RenderSettings.ambientGroundColor;
        }

        /// <summary>
        /// How far the portal's storm has let go, 0..1 (mission 1, the user, 2026-11-11: the film's
        /// crimson light lay over the whole route and the wheat read orange). MissionOne raises it
        /// as Ардан runs away from the village; the sun, sky, fog, ambient and the storm dome ease
        /// back towards the morning by it.
        /// </summary>
        public float Calm { get; set; }

        /// <summary>The soft glowing dot (sparks, the mage's orb) and the soft puff (dust, splashes).</summary>
        public Material SparkMaterial => sparkMaterial;
        public Material DustMaterial => dustMaterial;

        private void StepMood(float time)
        {
            var w = Mathf.Clamp01((time - _c.moodStart) / _c.moodSeconds);
            w = w * w * (3f - 2f * w) * (1f - Mathf.Clamp01(Calm));
            var sun = RenderSettings.sun;
            if (sun != null)
            {
                sun.intensity = Mathf.Lerp(_sunI0, _c.sunIntensity, w);
                sun.color = Color.Lerp(_sunC0, Col(_c.sunColor), w);
            }
            if (_sky != null)
            {
                _sky.SetColor("_SkyTint", Color.Lerp(_tint0, Col(_c.skyTint), w));
                _sky.SetFloat("_Exposure", Mathf.Lerp(_exp0, _c.skyExposure, w));
                _sky.SetFloat("_AtmosphereThickness", Mathf.Lerp(_atm0, _c.skyAtmosphere, w));
            }
            RenderSettings.fogColor = Color.Lerp(_fog0, Col(_c.fogColor), w);
            RenderSettings.fogEndDistance = Mathf.Lerp(_fogEnd0, _c.fogEnd, w);
            RenderSettings.fogStartDistance = Mathf.Lerp(_fogStart0, _c.fogStart, w);
            if (_storm != null)
            {
                var k = Mathf.Clamp01((time - _c.stormStart) / _c.stormSeconds);
                _storm.SetFloat("_Cover", k * k * (3f - 2f * k) * (1f - Mathf.Clamp01(Calm)));
                _storm.SetFloat("_T", time);
                _stormDome.gameObject.SetActive(time >= _c.stormStart);
            }
            RenderSettings.ambientSkyColor = Color.Lerp(_amb0, Col(_c.ambientSky), w);
            RenderSettings.ambientEquatorColor = Color.Lerp(_equ0, Col(_c.ambientEquator), w);
            RenderSettings.ambientGroundColor = Color.Lerp(_gnd0, Col(_c.ambientGround), w);
        }

        private void Post()
        {
            if (volume == null || volume.sharedProfile == null) return;
            volume.sharedProfile = Instantiate(volume.sharedProfile);
            var p = volume.sharedProfile;
            if (p.TryGet(out _bloom))
            {
                _bloom.threshold.value = _c.bloomThreshold;
                _bloom.intensity.value = _c.bloomIntensity;
            }
            if (p.TryGet(out Vignette v)) v.intensity.value = _c.vignette;
            if (p.TryGet(out ColorAdjustments ca))
            {
                ca.saturation.value = _c.saturation;
                ca.contrast.value = _c.contrast;
            }
            if (p.TryGet(out _dof))
            {
                _dof.mode.value = DepthOfFieldMode.Bokeh;
                _dof.aperture.value = _c.dofAperture;
                _dof.active = false;
            }
        }

        // ------------------------------------------------------------ chimney smoke, the title

        private void Smoke()
        {
            if (chimneysFile == null) return;
            var tops = new Dictionary<string, float[]>();
            foreach (var h in JsonUtility.FromJson<Chimneys>(chimneysFile.text).houses) tops[h.id] = h.tops;
            var sx = _c.chimneySigns != null && _c.chimneySigns.Length >= 2 ? _c.chimneySigns[0] : 1f;
            var sz = _c.chimneySigns != null && _c.chimneySigns.Length >= 2 ? _c.chimneySigns[1] : 1f;
            foreach (var house in village.Houses)
            {
                if (!tops.TryGetValue(house.name, out var t)) continue;
                var mf = house.GetComponentInChildren<MeshFilter>();
                if (mf == null) continue;
                for (var i = 0; i + 2 < t.Length; i += 3)
                {
                    // Blender's (x, y, z) in the model is the mesh's (x, z, y), signs as the exporter turns them
                    var at = mf.transform.TransformPoint(new Vector3(t[i] * sx, t[i + 2], t[i + 1] * sz));
                    var ps = Burst("ChimneySmoke", dustMaterial, true);
                    ps.transform.SetPositionAndRotation(at, Quaternion.Euler(-90f, 0f, 0f));
                    var main = ps.main;
                    main.startLifetime = _c.smokeLife;
                    main.startSpeed = _c.smokeSpeed;
                    main.startSize = new ParticleSystem.MinMaxCurve(_c.smokeSize * 0.6f, _c.smokeSize);
                    main.startColor = new Color(0.86f, 0.86f, 0.88f, 0.45f);
                    var shape = ps.shape;
                    shape.enabled = true;
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = 8f;
                    shape.radius = 0.04f;
                    var sol = ps.sizeOverLifetime;
                    sol.enabled = true;
                    sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 3f));
                    var col = ps.colorOverLifetime;
                    col.enabled = true;
                    var g = new Gradient();
                    g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                        new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
                    col.color = new ParticleSystem.MinMaxGradient(g);
                    var force = ps.forceOverLifetime;
                    force.enabled = true;
                    force.space = ParticleSystemSimulationSpace.World;
                    force.x = new ParticleSystem.MinMaxCurve(0.12f);
                    force.y = force.z = new ParticleSystem.MinMaxCurve(0f);
                    var em = ps.emission;
                    em.rateOverTime = _c.smokeRate;
                    _chimneySmoke.Add(ps);
                    // already smoking when the film starts
                    ps.Simulate(_c.smokeLife, true, true, false);
                }
            }
        }

        private void StepSmoke(float time)
        {
            foreach (var ps in _chimneySmoke)
            {
                var em = ps.emission;
                em.rateOverTime = time < _c.smokeStop ? _c.smokeRate : 0f;
            }
        }

        private void StepTitle(float time)
        {
            if (title == null) return;
            var u = time - _c.titleStart;
            if (u < 0f || u > _c.titleSeconds)
            {
                title.text = string.Empty;
                return;
            }
            title.text = Localization.Get(_c.titleKey);
            var a = Mathf.Clamp01(u / _c.titleFade) * Mathf.Clamp01((_c.titleSeconds - u) / _c.titleFade);
            var c = title.color;
            c.a = a;
            title.color = c;
        }

        // ------------------------------------------------------------ the amulet's glow

        private float _time;
        private Transform _amuletGlow;
        private Material _amuletMat;

        /// <summary>A soft blue halo on Ardan's amulet stone (the director puts it there each frame;
        /// a point light alone only lit the ground under him -- the user, 2026-11-11).</summary>
        public void AmuletGlow(Vector3 at, float strength, Camera cam)
        {
            if (_c == null) return;
            if (_amuletGlow == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "AmuletGlow";
                Kill(go.GetComponent<Collider>());
                go.transform.SetParent(_root, false);
                _amuletMat = new Material(sparkMaterial);
                var r = go.GetComponent<Renderer>();
                r.sharedMaterial = _amuletMat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                _amuletGlow = go.transform;
            }
            var on = strength > 0.01f;
            _amuletGlow.gameObject.SetActive(on);
            if (!on) return;
            _amuletGlow.position = at;
            _amuletGlow.rotation = Quaternion.LookRotation(at - cam.transform.position);
            _amuletGlow.localScale = Vector3.one * _c.amuletGlowSize * (0.8f + 0.2f * Mathf.Sin(_time * 9f));
            var c = Col(_c.runeColor) * Mathf.Clamp01(strength);
            c.a = Mathf.Clamp01(strength);
            _amuletMat.SetColor("_BaseColor", c);
        }

        // ------------------------------------------------------------ helpers

        private ParticleSystem Burst(string name, Material material, bool looping)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.SetParent(_root, false);
            var ps = go.GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = looping;
            main.duration = 5f;
            main.playOnAwake = false;
            main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = 0f;
            var shape = ps.shape;
            shape.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            ps.Play();
            _systems.Add(ps);
            return ps;
        }

        private float Range(float a, float b) => a + (float)_rnd.NextDouble() * (b - a);
        private static Vector3 V3(float[] a) => new Vector3(a[0], a[1], a[2]);
        private static Color Col(float[] a) => a != null && a.Length >= 3 ? new Color(a[0], a[1], a[2]) : Color.white;

        private static void Kill(UnityEngine.Object o)
        {
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }
    }
}
