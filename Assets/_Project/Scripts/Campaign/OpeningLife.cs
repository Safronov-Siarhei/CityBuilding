using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// The opening cutscene's living background (the user, 2026-11-10: «больше гуляющих людей,
    /// логики перемещения для орков»). Villagers stroll the roads out of the square and chat on it;
    /// on Panic they react, then run out of the village along the roads; orcs released at the portal
    /// spread down the roads -- torchbearers stop at houses and set the roofs alight, spearmen chase the
    /// nearest fleeing villager for a while (who outruns them). The director starts and drives it;
    /// Step(dt) runs it, so the editor can step it by hand for photos. Every number is in
    /// Campaign/OpeningLife.json.
    /// </summary>
    public class OpeningLife : MonoBehaviour
    {
        [Serializable]
        public struct CastMember
        {
            public string id;
            public GameObject model;
            public CineClips clips;
        }

        [Serializable]
        private class Config
        {
            public int seed;
            public int walkers;
            public int squareStanders;
            public string[] walkerFigures;
            public float laneSpread;
            public float squareInner;
            public float squareOuter;
            public float pauseChance;
            public float pauseMin;
            public float pauseMax;
            public float talkChance;
            public float walkSpeedJitter;
            public float reactMin;
            public float reactPerMetre;
            public float reactJitter;
            public float reactSeconds;
            public float fleeFactor;
            public float fleeBeyond;
            public int torchOrcs;
            public int spearOrcs;
            public float orcInterval;
            public float orcSpeedFactor;
            public float igniteReach;
            public float igniteSeconds;
            public float chaseRadius;
            public float chaseSeconds;
            public float orcLaneSpread;
            public float turnRate;
            public int[] orcSkipLanes;
            public FireConfig fire;
        }

        [Serializable]
        private class FireConfig
        {
            public float flameRate;
            public float flameLifeMin;
            public float flameLifeMax;
            public float flameSpeedMin;
            public float flameSpeedMax;
            public float flameSizeMin;
            public float flameSizeMax;
            public float smokeRate;
            public float smokeLifeMin;
            public float smokeLifeMax;
            public float smokeSpeed;
            public float smokeSizeMin;
            public float smokeSizeMax;
            public float smokeGrow;
            public float roofSink;
        }

        private enum Mode { Walk, Pause, Stand, React, ToLane, Flee, Gone, ToHouse, Ignite, Chase, Done }

        private class Agent
        {
            public Transform t;
            public CineFigure fig;
            public string id;
            public Mode mode;
            public int lane;
            public float s;
            public int dir;
            public float side;
            public float speed;
            public float timer;
            public bool orc;
            public bool torch;
            public Vector3 goal;
            public Agent prey;
            public Transform house;
            public Vector3 facing;
        }

        [SerializeField] private OpeningVillage village;
        [SerializeField] private CastMember[] cast = new CastMember[0];
        [SerializeField] private TextAsset config;
        [SerializeField] private Material flameMaterial;
        [SerializeField] private Material smokeMaterial;

        private Config _c;
        private Random _rnd;
        private Transform _root;
        private readonly List<Agent> _people = new List<Agent>();
        private readonly List<Agent> _orcs = new List<Agent>();
        private readonly List<ParticleSystem> _fires = new List<ParticleSystem>();
        private readonly HashSet<Transform> _burning = new HashSet<Transform>();
        private readonly HashSet<Transform> _claimed = new HashSet<Transform>();
        private float[][] _laneLength;
        private Vector3 _portal;
        private int _toRelease;
        private float _releaseTimer;
        private int _released;
        private int _torchesOut;

        public int Villagers => _people.Count;
        public int Fires => _fires.Count;
        public bool Panicking { get; private set; }
        /// <summary>Where the first roof caught (photos, the director's cut to the fires).</summary>
        public Vector3? FirstFire => _fires.Count > 0 ? _fires[0].transform.position : (Vector3?)null;
        /// <summary>The first orc out of the portal, wherever he is now.</summary>
        public Transform FirstOrc => _orcs.Count > 0 ? _orcs[0].t : null;

        /// <summary>Spawns the strolling villagers (the village must be built).</summary>
        public void Begin()
        {
            _c = JsonUtility.FromJson<Config>(config.text);
            _rnd = new Random(_c.seed);
            if (_root != null) Kill(_root.gameObject);
            _root = new GameObject("Life").transform;
            _root.SetParent(transform, false);
            _people.Clear();
            _orcs.Clear();
            _fires.Clear();
            _burning.Clear();
            _claimed.Clear();
            Panicking = false;
            _toRelease = 0;
            _released = 0;
            _torchesOut = 0;

            var lanes = village.Lanes;
            _laneLength = new float[lanes.Count][];
            for (var l = 0; l < lanes.Count; l++)
            {
                var acc = new float[lanes[l].Length];
                for (var i = 1; i < acc.Length; i++) acc[i] = acc[i - 1] + (lanes[l][i] - lanes[l][i - 1]).magnitude;
                _laneLength[l] = acc;
            }

            for (var i = 0; i < _c.walkers && lanes.Count > 0; i++)
            {
                var a = Spawn(_c.walkerFigures[_rnd.Next(_c.walkerFigures.Length)], false);
                a.lane = _rnd.Next(lanes.Count);
                a.s = Range(0f, Length(a.lane));
                a.dir = _rnd.Next(2) == 0 ? 1 : -1;
                a.side = Range(-_c.laneSpread, _c.laneSpread);
                a.speed = a.fig.Clips.walkSpeed * (1f + Range(-_c.walkSpeedJitter, _c.walkSpeedJitter));
                a.mode = Mode.Walk;
                a.fig.Play("Walk", 0f, a.speed / a.fig.Clips.walkSpeed, Range(0f, 1f));
                Place(a, LanePoint(a.lane, a.s, a.side), LaneTangent(a.lane, a.s) * a.dir, true);
            }

            // pairs chatting on the square
            for (var i = 0; i + 1 < _c.squareStanders; i += 2)
            {
                var ang = Range(0f, Mathf.PI * 2f);
                var r = Range(_c.squareInner, _c.squareOuter);
                var at = new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
                var off = new Vector3(-Mathf.Sin(ang), 0f, Mathf.Cos(ang)) * 0.28f;
                var p = Spawn(_c.walkerFigures[_rnd.Next(_c.walkerFigures.Length)], false);
                var q = Spawn(_c.walkerFigures[_rnd.Next(_c.walkerFigures.Length)], false);
                foreach (var (who, pos, other) in new[] { (p, at + off, at - off), (q, at - off, at + off) })
                {
                    who.mode = Mode.Stand;
                    who.fig.Play(_rnd.NextDouble() < 0.5 ? "Talk" : "Idle", 0f, 1f, Range(0f, 3f));
                    Place(who, pos, other - pos, true);
                }
            }
            Step(0f);
        }

        /// <summary>The portal opens at `origin`: every villager reacts, the nearer ones first, then runs out of the village.</summary>
        public void Panic(Vector3 origin)
        {
            if (Panicking) return;
            Panicking = true;
            foreach (var a in _people)
            {
                var d = Flat(a.t.position - origin).magnitude;
                a.mode = Mode.React;
                a.timer = -(_c.reactMin + d * _c.reactPerMetre + Range(0f, _c.reactJitter));
            }
        }

        /// <summary>Orcs come out of the portal at `portal`, one every orcInterval seconds.</summary>
        public void ReleaseOrcs(Vector3 portal)
        {
            _portal = portal;
            _toRelease = _c.torchOrcs + _c.spearOrcs;
            _releaseTimer = 0f;
        }

        /// <summary>Starts by itself in play (until the director takes over).</summary>
        [SerializeField] private bool autoBegin = true;

        private void Start()
        {
            if (!autoBegin || External) return;
            if (village.BuildingCount == 0) village.Build();
            Begin();
        }

        /// <summary>Set by the director, which steps the life itself on the film's clock.</summary>
        public bool External { get; set; }

        private void Update()
        {
            if (Application.isPlaying && _c != null && !External) Step(Time.deltaTime);
        }

        /// <summary>Sets alight the house nearest `at` that is not burning yet (the director's cue).</summary>
        public void IgniteNearest(Vector3 at)
        {
            Transform best = null;
            var bestD = float.MaxValue;
            foreach (var h in village.Houses)
            {
                if (_burning.Contains(h)) continue;
                var d = Flat(h.position - at).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = h;
                }
            }
            if (best != null)
            {
                _claimed.Add(best);
                Ignite(best);
            }
        }

        public void Step(float dt)
        {
            if (_c == null) return;
            if (_toRelease > 0)
            {
                _releaseTimer -= dt;
                while (_toRelease > 0 && _releaseTimer <= 0f)
                {
                    ReleaseOne();
                    _toRelease--;
                    _releaseTimer += _c.orcInterval;
                }
            }
            foreach (var a in _people) StepPerson(a, dt);
            foreach (var o in _orcs) StepOrc(o, dt);
            if (!Application.isPlaying)
            {
                foreach (var ps in _fires) ps.Simulate(dt, true, false, false);
            }
        }

        // ------------------------------------------------------------ the villagers

        private void StepPerson(Agent a, float dt)
        {
            switch (a.mode)
            {
                case Mode.Walk:
                {
                    a.s += a.dir * a.speed * dt;
                    var len = Length(a.lane);
                    if (a.s <= 0f || a.s >= len)
                    {
                        a.s = Mathf.Clamp(a.s, 0f, len);
                        a.dir = -a.dir;
                    }
                    else if (_rnd.NextDouble() < _c.pauseChance * dt)
                    {
                        a.mode = Mode.Pause;
                        a.timer = Range(_c.pauseMin, _c.pauseMax);
                        a.fig.Play(_rnd.NextDouble() < _c.talkChance ? "Talk" : "Idle", 0.3f);
                    }
                    Place(a, LanePoint(a.lane, a.s, a.side), LaneTangent(a.lane, a.s) * a.dir, false, dt);
                    break;
                }
                case Mode.Pause:
                    a.timer -= dt;
                    if (a.timer <= 0f)
                    {
                        a.mode = Mode.Walk;
                        a.fig.Play("Walk", 0.3f, a.speed / a.fig.Clips.walkSpeed);
                    }
                    break;
                case Mode.React:
                    a.timer += dt;
                    if (a.timer < 0f) break;                    // not yet: the news has not reached them
                    if (a.timer < dt + 1e-4f)
                    {
                        var react = a.fig.Clips.Find("Scream") != null ? "Scream" : a.fig.Clips.Find("Startle") != null ? "Startle"
                            : a.fig.Clips.Find("Panic") != null ? "Panic" : "Stare";
                        a.fig.Play(react, 0.15f);
                    }
                    if (a.timer >= _c.reactSeconds) StartFlight(a);
                    break;
                case Mode.ToLane:
                    if (MoveTo(a, a.goal, dt))
                    {
                        a.mode = Mode.Flee;
                        a.s = 0f;
                    }
                    break;
                case Mode.Flee:
                {
                    a.s += a.speed * dt;
                    var len = Length(a.lane);
                    var end = LanePoint(a.lane, len, a.side);
                    var dir = LaneTangent(a.lane, len);
                    var at = a.s <= len ? LanePoint(a.lane, a.s, a.side) : end + dir * (a.s - len);
                    Place(a, at, a.s <= len ? LaneTangent(a.lane, a.s) : dir, false, dt);
                    if (a.s > len + _c.fleeBeyond)
                    {
                        a.mode = Mode.Gone;
                        a.t.gameObject.SetActive(false);
                    }
                    break;
                }
            }
            if (a.mode != Mode.Gone) a.fig.Tick(dt);
        }

        private void StartFlight(Agent a)
        {
            a.speed = a.fig.Clips.runSpeed * _c.fleeFactor * (1f + Range(-_c.walkSpeedJitter, _c.walkSpeedJitter));
            a.fig.Play("Run", 0.2f, a.speed / a.fig.Clips.runSpeed);
            if (a.lane >= 0)
            {
                a.mode = Mode.Flee;                               // already on a road: run on along it, outwards
                return;
            }
            // from the square: to the start of the road whose mouth is nearest
            var best = 0;
            var bestD = float.MaxValue;
            for (var l = 0; l < village.Lanes.Count; l++)
            {
                var d = (LanePoint(l, 0f, 0f) - a.t.position).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = l;
                }
            }
            a.lane = best;
            a.side = Range(-_c.laneSpread, _c.laneSpread);
            a.goal = LanePoint(best, 0f, a.side);
            a.mode = Mode.ToLane;
        }

        // ------------------------------------------------------------ the orcs

        private void ReleaseOne()
        {
            // torchbearers and spearmen in turn, until one kind runs out
            var torchLeft = _c.torchOrcs - _torchesOut;
            var spearLeft = _c.spearOrcs - (_released - _torchesOut);
            var torch = spearLeft <= 0 || (torchLeft > 0 && _released % 2 == 0);
            if (torch) _torchesOut++;
            var o = Spawn(torch ? "CineOrcTorch" : "CineOrcSpear", true);
            o.torch = torch;
            // round the roads in turn, past the ones the orcs keep off (the heroes stand on the market road)
            var lanes = new List<int>();
            for (var l = 0; l < village.Lanes.Count; l++)
            {
                if (_c.orcSkipLanes == null || Array.IndexOf(_c.orcSkipLanes, l) < 0) lanes.Add(l);
            }
            o.lane = lanes.Count > 0 ? lanes[_released % lanes.Count] : 0;
            o.side = Range(-_c.orcLaneSpread, _c.orcLaneSpread);
            o.speed = o.fig.Clips.runSpeed * _c.orcSpeedFactor;
            o.goal = LanePoint(o.lane, 0f, o.side);
            o.mode = Mode.ToLane;
            o.fig.Play("Run", 0f, _c.orcSpeedFactor);
            Place(o, _portal, o.goal - _portal, true);
            _released++;
        }

        private void StepOrc(Agent o, float dt)
        {
            switch (o.mode)
            {
                case Mode.ToLane:
                    if (MoveTo(o, o.goal, dt)) o.mode = Mode.Flee;    // "Flee" here: running on down the road
                    break;
                case Mode.Flee:
                {
                    var len = Length(o.lane);
                    o.s = Mathf.Min(len, o.s + o.speed * dt);
                    Place(o, LanePoint(o.lane, o.s, o.side), LaneTangent(o.lane, o.s), false, dt);
                    if (o.s >= len)
                    {
                        o.mode = Mode.Done;
                        o.fig.Play("Idle", 0.3f);
                        break;
                    }
                    if (o.torch) LookForHouse(o);
                    else LookForPrey(o);
                    break;
                }
                case Mode.ToHouse:
                    if (MoveTo(o, o.goal, dt))
                    {
                        o.mode = Mode.Ignite;
                        o.timer = _c.igniteSeconds;
                        o.fig.Play("Idle", 0.2f);
                        Face(o, o.house.position - o.t.position, 1f);
                    }
                    break;
                case Mode.Ignite:
                    o.timer -= dt;
                    if (o.timer <= 0f)
                    {
                        Ignite(o.house);
                        BackToRoad(o);
                    }
                    break;
                case Mode.Chase:
                    o.timer -= dt;
                    if (o.prey == null || o.prey.mode == Mode.Gone || o.timer <= 0f)
                    {
                        BackToRoad(o);
                        break;
                    }
                    MoveTo(o, o.prey.t.position, dt);
                    break;
            }
            o.fig.Tick(dt);
        }

        private void LookForHouse(Agent o)
        {
            Transform best = null;
            var bestD = _c.igniteReach * _c.igniteReach;
            foreach (var h in village.Houses)
            {
                if (_burning.Contains(h) || _claimed.Contains(h)) continue;
                var d = Flat(h.position - o.t.position).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = h;
                }
            }
            if (best == null) return;
            _claimed.Add(best);
            o.house = best;
            var size = Footprint(best);
            var away = Flat(o.t.position - best.position).normalized;
            o.goal = best.position + away * (size * 0.5f + 0.25f);
            o.mode = Mode.ToHouse;
        }

        private void LookForPrey(Agent o)
        {
            Agent best = null;
            var bestD = _c.chaseRadius * _c.chaseRadius;
            foreach (var p in _people)
            {
                if (p.mode != Mode.Flee && p.mode != Mode.ToLane) continue;
                var d = (p.t.position - o.t.position).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = p;
                }
            }
            if (best == null) return;
            o.prey = best;
            o.timer = _c.chaseSeconds;
            o.mode = Mode.Chase;
        }

        private void BackToRoad(Agent o)
        {
            // rejoin the road at the nearest point ahead of where it left it
            var len = Length(o.lane);
            var bestS = o.s;
            var bestD = float.MaxValue;
            for (var s = o.s; s <= len; s += 0.5f)
            {
                var d = (LanePoint(o.lane, s, o.side) - o.t.position).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    bestS = s;
                }
            }
            o.s = bestS;
            o.goal = LanePoint(o.lane, o.s, o.side);
            o.prey = null;
            o.mode = Mode.ToLane;
            o.fig.Play("Run", 0.2f, _c.orcSpeedFactor);
        }

        // ------------------------------------------------------------ fire

        private void Ignite(Transform house)
        {
            if (!_burning.Add(house)) return;
            var bounds = Bounds(house);
            var f = _c.fire;
            var size = Mathf.Max(bounds.size.x, bounds.size.z);
            var fire = new GameObject("Fire").transform;
            fire.SetParent(_root, false);
            fire.position = new Vector3(bounds.center.x, bounds.max.y - f.roofSink * bounds.size.y, bounds.center.z);

            var flames = Particles(fire, "Flames", flameMaterial, f.flameRate * size, f.flameLifeMin, f.flameLifeMax,
                f.flameSpeedMin, f.flameSpeedMax, f.flameSizeMin * size, f.flameSizeMax * size,
                new Vector3(bounds.size.x * 0.6f, 0.1f, bounds.size.z * 0.6f), 1f, false);
            var col = flames.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Gradient(
                new[] { new Color(1f, 0.92f, 0.5f), new Color(1f, 0.55f, 0.12f), new Color(0.8f, 0.18f, 0.05f) },
                new[] { 0.9f, 0.8f, 0f }));
            var smoke = Particles(fire, "Smoke", smokeMaterial, f.smokeRate * size, f.smokeLifeMin, f.smokeLifeMax,
                f.smokeSpeed * 0.7f, f.smokeSpeed, f.smokeSizeMin * size, f.smokeSizeMax * size,
                new Vector3(bounds.size.x * 0.4f, 0.1f, bounds.size.z * 0.4f), f.smokeGrow, true);
            var sc = smoke.colorOverLifetime;
            sc.enabled = true;
            sc.color = new ParticleSystem.MinMaxGradient(Gradient(
                new[] { new Color(0.3f, 0.27f, 0.25f), new Color(0.42f, 0.4f, 0.4f), new Color(0.55f, 0.55f, 0.56f) },
                new[] { 0f, 0.55f, 0f }));
            _fires.Add(flames);
            _fires.Add(smoke);
        }

        private static ParticleSystem Particles(Transform parent, string name, Material material, float rate,
            float lifeMin, float lifeMax, float speedMin, float speedMax, float sizeMin, float sizeMax,
            Vector3 box, float grow, bool smoke)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);       // the cone and box emit up
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
            main.maxParticles = smoke ? 80 : 120;
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
            r.sortingFudge = smoke ? 10f : 0f;                 // flames drawn over their own smoke
            if (!smoke)
            {
                // tongues, not balls: each flame drawn long along the way it rises
                r.renderMode = ParticleSystemRenderMode.Stretch;
                r.velocityScale = 0.18f;
                r.lengthScale = 1.4f;
            }
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        private static Gradient Gradient(Color[] colours, float[] alphas)
        {
            var g = new Gradient();
            var ck = new GradientColorKey[colours.Length];
            var ak = new GradientAlphaKey[alphas.Length];
            for (var i = 0; i < colours.Length; i++) ck[i] = new GradientColorKey(colours[i], i / (float)(colours.Length - 1));
            for (var i = 0; i < alphas.Length; i++) ak[i] = new GradientAlphaKey(alphas[i], i / (float)(alphas.Length - 1));
            g.SetKeys(ck, ak);
            return g;
        }

        // ------------------------------------------------------------ helpers

        private Agent Spawn(string id, bool orc)
        {
            var member = Array.Find(cast, m => m.id == id);
            var go = Instantiate(member.model, _root);
            go.name = id;
            var fig = go.AddComponent<CineFigure>();
            fig.Init(member.clips);
            var a = new Agent { t = go.transform, fig = fig, id = id, orc = orc, lane = -1 };
            (orc ? _orcs : _people).Add(a);
            return a;
        }

        /// <summary>Moves toward `to` at the agent's speed; true on arrival.</summary>
        private bool MoveTo(Agent a, Vector3 to, float dt)
        {
            var d = Flat(to - a.t.position);
            var step = a.speed * dt;
            if (d.magnitude <= step)
            {
                Place(a, to, d, false, dt);
                return true;
            }
            Place(a, a.t.position + d.normalized * step, d, false, dt);
            return false;
        }

        private void Place(Agent a, Vector3 at, Vector3 facing, bool snap, float dt = 0f)
        {
            a.t.position = new Vector3(at.x, 0f, at.z);
            Face(a, facing, snap ? 1f : Mathf.Clamp01(_c.turnRate * dt));
        }

        private static void Face(Agent a, Vector3 facing, float w)
        {
            facing = Flat(facing);
            if (facing.sqrMagnitude < 1e-6f) return;
            a.t.rotation = Quaternion.Slerp(a.t.rotation, Quaternion.LookRotation(facing), w);   // a figure faces +Z
        }

        private float Length(int lane) => _laneLength[lane][_laneLength[lane].Length - 1];

        private Vector3 LanePoint(int lane, float s, float side)
        {
            var line = village.Lanes[lane];
            var acc = _laneLength[lane];
            var i = 0;
            while (i < acc.Length - 2 && s > acc[i + 1]) i++;
            var seg = acc[i + 1] - acc[i];
            var f = seg > 0f ? Mathf.Clamp01((s - acc[i]) / seg) : 0f;
            var p = Vector2.Lerp(line[i], line[i + 1], f);
            var dir = (line[i + 1] - line[i]).normalized;
            p += new Vector2(-dir.y, dir.x) * side;
            return new Vector3(p.x, 0f, p.y);
        }

        private Vector3 LaneTangent(int lane, float s)
        {
            var line = village.Lanes[lane];
            var acc = _laneLength[lane];
            var i = 0;
            while (i < acc.Length - 2 && s > acc[i + 1]) i++;
            var d = (line[i + 1] - line[i]).normalized;
            return new Vector3(d.x, 0f, d.y);
        }

        private static Bounds Bounds(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(t.position, Vector3.one);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        private static float Footprint(Transform t)
        {
            var b = Bounds(t);
            return Mathf.Max(b.size.x, b.size.z);
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private float Range(float a, float b) => a + (float)_rnd.NextDouble() * (b - a);

        private static void Kill(GameObject go)
        {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }
}
