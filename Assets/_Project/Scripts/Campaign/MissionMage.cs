using System;
using System.Collections.Generic;
using UnityEngine;
using CityBuilder.Core;
using UnityEngine.Rendering;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 1's mage at the bridge (agreed with the user, 2026-11-11). Ардан comes off the bridge
    /// with the ambush orc behind him; the player's say is taken for a few seconds:
    ///   1. the camera comes down to the mage by the track;
    ///   2. he flings his left arm to the town -- «Беги в город, я их задержу» -- and Ардан runs past;
    ///   3. he turns to the bridge and strikes the staff down (the crystal flares);
    ///   4. his left palm thrusts: a violet orb flies into the bridge's middle piece, which breaks and
    ///      falls into the river with the orc on it, and the current carries the orc away;
    ///   5. the say comes back; the mage holds the bank, the staff glowing, while orcs gather on the
    ///      far bank. Nothing chases Ардан from here to the gate.
    /// Every number is in Mission1.json "mage". Steps by dt, so the batchmode check can run it.
    /// </summary>
    public class MissionMage
    {
        [Serializable] public class Config
        {
            public string figure, lineKey;
            public float[] stand, runTo;
            public float waveYaw, bridgeYaw, triggerZ;
            public float lineAt, lineSeconds, turnAt, turnSeconds, castAt, strike, release, orbSeconds, holdAt, returnAt;
            public float[] camFrom, camAt;
            public float camIn, camOut, camFov;
            public float[] violet;
            public float orbSize, orbLight, flashLight, crystalAlong, crystalLight, glowRange;
            public float crackSeconds, fallSeconds, sink, tiltX, tiltZ;
            public float orcSpeed, current, driftSeconds, orcSink;
            public float roarEvery;
            public Gatherer[] farBank;
        }

        [Serializable] public class Gatherer { public string figure; public float x, z; }

        private class Orc
        {
            public CineFigure fig;
            public Vector3 spot;
            public bool there;
            public float roar;
        }

        private readonly Config _c;
        private readonly OpeningVillage _village;
        private readonly CineFigure _mage;
        private readonly Transform _crystal, _palm;
        private readonly ParticleSystem _sparks, _splash;
        private readonly Light _orbLight, _crystalLight;
        private readonly List<Orc> _bank = new List<Orc>();
        private readonly Color _violet;
        private CineFigure _victim;
        private Transform _piece;
        private Vector3 _pieceAt;
        private Quaternion _pieceRot;
        private Vector3 _orbFrom, _victimOff;
        private bool _orbGone, _struck, _hit, _splashed;
        private float _victimSpeed;

        /// <summary>Seconds since the scene began; negative before it.</summary>
        public float T { get; private set; } = -1f;

        /// <summary>While the scene plays the player has no say and the camera is the scene's.</summary>
        public bool Playing => T >= 0f && T < _c.returnAt + _c.camOut;

        public bool Done => T >= _c.returnAt + _c.camOut;

        /// <summary>Is the camera on the scene's shot? The shot looks north at the bridge, the game's
        /// view south: a turn between them swung the whole world round, so each change is a cut
        /// hidden in a short dip to black.</summary>
        public bool OnShot => T >= _c.camIn * 0.5f && T < _c.returnAt + _c.camOut * 0.5f;

        /// <summary>0..1, the dip to black at each cut.</summary>
        public float Fade
        {
            get
            {
                if (T < 0f) return 0f;
                if (T < _c.camIn) return 1f - Mathf.Abs(T / _c.camIn * 2f - 1f);
                if (T < _c.returnAt) return 0f;
                if (T < _c.returnAt + _c.camOut) return 1f - Mathf.Abs((T - _c.returnAt) / _c.camOut * 2f - 1f);
                return 0f;
            }
        }

        public MissionMage(Config c, OpeningVillage village, Func<string, OpeningLife.CastMember?> cast,
            Material spark, Material dust, Material flame, Transform parent)
        {
            _c = c;
            _village = village;
            _violet = c.violet != null && c.violet.Length >= 3 ? new Color(c.violet[0], c.violet[1], c.violet[2]) : new Color(0.62f, 0.3f, 1f);
            _mage = Figure(c.figure, cast, parent, "Mage");
            if (_mage != null)
            {
                _mage.transform.SetPositionAndRotation(new Vector3(c.stand[0], 0f, c.stand[1]), Quaternion.Euler(0f, c.waveYaw, 0f));
                _mage.Play("Idle", 0f);
                _mage.Tick(0f);
                _crystal = Tip(_mage, "forearm.R", c.crystalAlong, "Crystal");
                _palm = Tip(_mage, "forearm.L", 1f, "Palm");
                if (_crystal != null) _crystalLight = PointLight(_crystal, "CrystalLight", c.glowRange);
            }
            var orb = new GameObject("Orb").transform;
            orb.SetParent(parent, false);
            _orbLight = PointLight(orb, "OrbLight", c.glowRange * 1.6f);
            _sparks = Particles("MageSparks", spark, parent);
            _splash = Particles("BridgeSplash", dust, parent);
            if (c.farBank != null)
                foreach (var g in c.farBank)
                {
                    var f = Figure(g.figure, cast, parent, "FarBank_" + g.figure);
                    if (f == null) continue;
                    if (g.figure == "CineOrcTorch" && flame != null) f.LightTorch(flame);
                    f.gameObject.SetActive(false);
                    _bank.Add(new Orc { fig = f, spot = new Vector3(g.x, 0f, g.z) });
                }
        }

        /// <summary>Has Ардан come off the bridge, and is it time?</summary>
        public bool Ready(Vector3 ardan) => T < 0f && _mage != null && ardan.z < _c.triggerZ;

        /// <summary>The scene begins: the orc (the ambush, if it was sprung) is the one the river takes.</summary>
        public void Begin(CineFigure victim)
        {
            T = 0f;
            _victim = victim;
            var pieces = _village.BridgePieces;
            if (pieces != null && pieces.Count > 0)
            {
                _piece = pieces[pieces.Count / 2];
                _pieceAt = _piece.position;
                _pieceRot = _piece.rotation;
            }
            if (_victim != null)
            {
                _victim.gameObject.SetActive(true);
                var body = _victim.GetComponent<CharacterController>();
                if (body != null) body.enabled = false;
                var to = Flat(_pieceAt - _victim.transform.position).magnitude;
                var hit = _c.castAt + _c.release + _c.orbSeconds;
                _victimSpeed = Mathf.Max(_c.orcSpeed, to / Mathf.Max(0.5f, hit - 0.4f));
            }
            foreach (var o in _bank)
            {
                // they come running down to the far bank from the forest behind it
                o.fig.gameObject.SetActive(true);
                o.fig.transform.SetPositionAndRotation(o.spot + new Vector3(0f, 0f, 7f + o.spot.x * 0.15f), Quaternion.Euler(0f, 180f, 0f));
            }
        }

        public void Step(float dt)
        {
            if (_mage == null) return;
            if (T < 0f)
            {
                _mage.Play("Idle", 0.2f);
                _mage.Tick(dt);
                return;
            }
            T += dt;
            StepMage(dt);
            StepOrb();
            StepBridge();
            StepVictim(dt);
            StepBank(dt);
            Sim(_sparks, dt);
            Sim(_splash, dt);
        }

        /// <summary>The scene's shot: the camera low by the mage, the bridge behind him.</summary>
        public void Shot(out Vector3 from, out Vector3 at)
        {
            var m = _mage.transform.position;
            from = m + V(_c.camFrom);
            at = m + V(_c.camAt);
        }

        public float Fov => _c.camFov;

        public string Line => Localization.Get(_c.lineKey);

        // ------------------------------------------------------------ the mage

        private void StepMage(float dt)
        {
            var t = T;
            var turn = Mathf.SmoothStep(0f, 1f, (t - _c.turnAt) / Mathf.Max(0.01f, _c.turnSeconds));
            _mage.transform.rotation = Quaternion.Euler(0f, Mathf.LerpAngle(_c.waveYaw, _c.bridgeYaw, turn), 0f);
            if (t < _c.turnAt) _mage.Play("Wave", 0.25f);
            else if (t < _c.castAt) _mage.Play("Idle", 0.25f);
            else if (t < _c.holdAt) _mage.Play("Cast", 0.15f);
            else _mage.Play("Hold", 0.4f);
            _mage.Tick(dt);
            // the crystal: dark until the strike, a flare on it, then the steady glow of the hold
            var strikeT = _c.castAt + _c.strike;
            var glow = 0f;
            if (t >= strikeT)
            {
                var since = t - strikeT;
                glow = Mathf.Max(0.35f + 0.1f * Mathf.Sin(t * 5f), Mathf.Exp(-since * 2.5f));
                if (_crystal == null) { }
                else if (!_struck)
                {
                    _struck = true;
                    Burst(_sparks, _crystal.position, 26, 0.9f, 0.05f, 0.6f);
                }
                else if (UnityEngine.Random.value < dt * 6f) Burst(_sparks, _crystal.position, 1, 0.15f, 0.03f, 0.7f);
            }
            if (_crystalLight != null) _crystalLight.intensity = glow * _c.crystalLight;
        }

        // ------------------------------------------------------------ the orb and the bridge

        private float HitTime => _c.castAt + _c.release + _c.orbSeconds;

        private void StepOrb()
        {
            var r0 = _c.castAt + _c.release;
            if (T < r0 || _orbGone || _piece == null)
            {
                _orbLight.intensity = 0f;
                if (T >= r0 && !_orbGone) _orbGone = true;
                return;
            }
            if (_orbFrom == Vector3.zero) _orbFrom = _palm != null ? _palm.position : _mage.transform.position + Vector3.up * 0.4f;
            var u = Mathf.Clamp01((T - r0) / _c.orbSeconds);
            var target = _pieceAt + Vector3.up * 0.3f;
            // a shallow arc, quickening as it goes
            var p = Vector3.Lerp(_orbFrom, target, u * u * (2f - u)) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.25f;
            _orbLight.transform.position = p;
            _orbLight.intensity = _c.orbLight;
            // the ball itself (a few big soft sparks on the spot) and its trail
            Emit(_sparks, p, Vector3.zero, _c.orbSize, 0.08f, Color.Lerp(_violet, Color.white, 0.35f));
            Emit(_sparks, p, Vector3.zero, _c.orbSize * 0.7f, 0.12f, _violet);
            Emit(_sparks, p + UnityEngine.Random.insideUnitSphere * _c.orbSize * 0.3f, UnityEngine.Random.insideUnitSphere * 0.2f, _c.orbSize * 0.3f, 0.45f, _violet);
            if (u >= 1f)
            {
                _orbGone = true;
                _hit = true;
                _orbLight.intensity = _c.flashLight;
                Burst(_sparks, target, 60, 2.2f, 0.06f, 0.7f);
            }
        }

        private void StepBridge()
        {
            if (_piece == null || !_hit) return;
            var since = T - HitTime;
            // a flash fading, the piece shaking on its cracked posts, then over and down
            if (since < 0.6f) _orbLight.intensity = _c.flashLight * (1f - since / 0.6f);
            else _orbLight.intensity = 0f;
            if (since < _c.crackSeconds)
            {
                var k = 0.025f * (1f - since / _c.crackSeconds);
                _piece.SetPositionAndRotation(_pieceAt + new Vector3(Mathf.Sin(since * 61f), 0f, Mathf.Sin(since * 47f)) * k, _pieceRot);
                return;
            }
            var u = Mathf.Clamp01((since - _c.crackSeconds) / _c.fallSeconds);
            var e = u * u;
            _piece.SetPositionAndRotation(_pieceAt + Vector3.down * _c.sink * e,
                Quaternion.Euler(_c.tiltX * Mathf.Sqrt(u), 0f, _c.tiltZ * Mathf.Sqrt(u)) * _pieceRot);
            if (!_splashed && u > 0.35f)
            {
                _splashed = true;
                _village.BreakBridge(_piece);
                for (var i = 0; i < 3; i++)
                    Burst(_splash, _pieceAt + new Vector3((i - 1) * 0.5f, 0.02f, 0f), 30, 1.6f, 0.07f, 0.9f, Vector3.up * 1.3f);
            }
            if (u >= 1f) _piece.gameObject.SetActive(false);
        }

        /// <summary>The orc: onto the middle piece (it halts there, facing Ардан), down with it, and
        /// away on the current, struggling, until the river has it.</summary>
        private void StepVictim(float dt)
        {
            if (_victim == null || !_victim.gameObject.activeSelf) return;
            var t = _victim.transform;
            if (!_hit)
            {
                var to = Flat(_pieceAt - t.position);
                if (to.magnitude > 0.08f)
                {
                    var step = Mathf.Min(to.magnitude, _victimSpeed * dt);
                    var d = to.normalized;
                    var p = t.position + d * step;
                    p.y = _village.HeightAt(new Vector2(p.x, p.z));
                    t.SetPositionAndRotation(p, Quaternion.Slerp(t.rotation, Quaternion.LookRotation(d), Mathf.Clamp01(8f * dt)));
                    var natural = _victim.Clips.runSpeed;
                    _victim.Play("Run", 0.2f, natural > 0f ? _victimSpeed / natural : 1f);
                }
                else
                {
                    // it stops dead on the bridge, glaring at the mage
                    t.rotation = Quaternion.Slerp(t.rotation, Quaternion.Euler(0f, 180f, 0f), Mathf.Clamp01(6f * dt));
                    _victim.Play("Idle", 0.2f);
                }
                _victimOff = t.position - _pieceAt;
                _victim.Tick(dt);
                return;
            }
            var since = T - HitTime;
            if (since < _c.crackSeconds + _c.fallSeconds * 0.6f)
            {
                // riding the piece down
                t.position = _piece.position + _victimOff;
                _victim.Play("Run", 0.15f, 1.8f);
            }
            else
            {
                // in the water: carried off downstream, flailing, lower and lower
                var w = since - _c.crackSeconds - _c.fallSeconds * 0.6f;
                var p = t.position;
                p.x += _c.current * dt;
                p.y = Mathf.Lerp(p.y, -_c.orcSink - 0.5f * Mathf.Clamp01(w / _c.driftSeconds - 0.6f) * 2.5f, Mathf.Clamp01(3f * dt))
                      + Mathf.Sin(w * 6f) * 0.004f;
                t.SetPositionAndRotation(p, t.rotation * Quaternion.Euler(0f, 40f * dt, 0f));
                _victim.Play("Run", 0.3f, 2.2f);
                if (w > _c.driftSeconds) _victim.gameObject.SetActive(false);
            }
            _victim.Tick(dt);
        }

        // ------------------------------------------------------------ the far bank

        private void StepBank(float dt)
        {
            foreach (var o in _bank)
            {
                var t = o.fig.transform;
                if (!o.there)
                {
                    var to = Flat(o.spot - t.position);
                    if (to.magnitude < 0.1f)
                    {
                        o.there = true;
                        o.roar = UnityEngine.Random.value * _c.roarEvery;
                    }
                    else
                    {
                        var d = to.normalized;
                        t.SetPositionAndRotation(t.position + d * Mathf.Min(to.magnitude, _c.orcSpeed * dt), Quaternion.LookRotation(d));
                        var natural = o.fig.Clips.runSpeed;
                        o.fig.Play("Run", 0.2f, natural > 0f ? _c.orcSpeed / natural : 1f);
                    }
                }
                else
                {
                    // across the water at the mage, roaring now and then (the champion has a roar)
                    var look = Flat(_mage.transform.position - t.position);
                    if (look.sqrMagnitude > 0f) t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(look), Mathf.Clamp01(4f * dt));
                    o.roar -= dt;
                    if (o.roar <= 0f)
                    {
                        o.roar = _c.roarEvery * (0.7f + 0.6f * UnityEngine.Random.value);
                        if (o.fig.Clips.Find("P_Roar") != null) o.fig.Play("P_Roar", 0.2f);
                    }
                    if (o.fig.Current != "P_Roar" || o.fig.ClipTime > 2.9f) o.fig.Play("Idle", 0.3f);
                }
                o.fig.Tick(dt);
            }
        }

        // ------------------------------------------------------------ helpers

        private static CineFigure Figure(string figure, Func<string, OpeningLife.CastMember?> cast, Transform parent, string name)
        {
            var m = cast(figure);
            if (!m.HasValue || m.Value.model == null) return null;
            var go = UnityEngine.Object.Instantiate(m.Value.model, parent);
            go.name = name;
            var fig = go.AddComponent<CineFigure>();
            fig.Init(m.Value.clips);
            return fig;
        }

        /// <summary>A point on a rigid part, `along` of the way from its joint to its farthest vertex
        /// (the staff's crystal on the right forearm, the left hand's palm).</summary>
        private static Transform Tip(CineFigure fig, string bone, float along, string name)
        {
            foreach (var mf in fig.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.transform.parent == null || mf.transform.parent.name != bone || mf.sharedMesh == null) continue;
                var tip = Vector3.zero;
                foreach (var v in mf.sharedMesh.vertices) if (v.sqrMagnitude > tip.sqrMagnitude) tip = v;
                var go = new GameObject(name).transform;
                go.SetParent(mf.transform, false);
                go.localPosition = tip * along;
                return go;
            }
            return null;
        }

        private Light PointLight(Transform parent, string name, float range)
        {
            var go = new GameObject(name, typeof(Light));
            go.transform.SetParent(parent, false);
            var l = go.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = _violet;
            l.range = range;
            l.intensity = 0f;
            l.shadows = LightShadows.None;
            return l;
        }

        private static ParticleSystem Particles(string name, Material material, Transform parent)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.SetParent(parent, false);
            var ps = go.GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.duration = 5f;
            main.playOnAwake = false;
            main.maxParticles = 600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = name == "BridgeSplash" ? 0.5f : 0f;
            var em = ps.emission;
            em.rateOverTime = 0f;
            var shape = ps.shape;
            shape.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }

        private void Burst(ParticleSystem ps, Vector3 at, int n, float speed, float size, float life, Vector3 lift = default)
        {
            var white = ps == _splash;
            for (var i = 0; i < n; i++)
            {
                var c = white ? Color.Lerp(new Color(0.75f, 0.88f, 1f), Color.white, UnityEngine.Random.value) : Color.Lerp(_violet, Color.white, UnityEngine.Random.value * 0.5f);
                Emit(ps, at, UnityEngine.Random.insideUnitSphere * speed + lift, size * (0.5f + UnityEngine.Random.value), life * (0.6f + 0.8f * UnityEngine.Random.value), c);
            }
        }

        private static void Emit(ParticleSystem ps, Vector3 at, Vector3 velocity, float size, float life, Color colour)
        {
            ps.Emit(new ParticleSystem.EmitParams
            {
                position = at,
                velocity = velocity,
                startSize = size,
                startLifetime = life,
                startColor = colour,
            }, 1);
        }

        private static void Sim(ParticleSystem ps, float dt)
        {
            if (!Application.isPlaying && ps != null) ps.Simulate(dt, true, false, false);
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
        private static Vector3 V(float[] a) => new Vector3(a[0], a[1], a[2]);
    }
}
