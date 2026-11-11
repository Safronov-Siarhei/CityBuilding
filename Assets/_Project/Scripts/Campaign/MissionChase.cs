using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 1's orcs and the chase (the user, 2026-11-11: a CHASE, orcs slower than Ардан):
    ///   - patrols walk their beats (the village streets, the field's cart track, the forest);
    ///   - one SEES him within `sight` -- but not in the tall wheat unless right beside him -- and
    ///     runs at him, a little slower than he runs; it gives up past `lose`, and walks back;
    ///   - the ambush orc waits hidden in the bushes by the bridge and bursts out as he comes near;
    ///     it stops at the bridge's far end for now (the mage brings the bridge down on it later);
    ///   - caught: a beat, a fade to black, and he is back at the last checkpoint with the orcs
    ///     back on their beats. Checkpoints: the village's way out, the forest edge, before the bridge.
    /// Every number is in Mission1.json "chase".
    /// </summary>
    public class MissionChase
    {
        [Serializable] public class Config
        {
            public float sight, lose, catchDistance, wheatSight, walkSpeed, runSpeed, turnRate, radius, height;
            public float caughtSeconds, fadeSeconds, noticeSeconds;
            public string checkpointKey;
            public Checkpoint[] checkpoints;
            public Patrol[] orcs;
            public Ambush ambush;
        }

        [Serializable] public class Checkpoint { public float x, z, r; }
        [Serializable] public class Patrol { public string figure; public float[] beat; }
        [Serializable] public class Ambush { public string figure; public float[] hide, trigger; public float triggerRadius, runSpeed, stopZ; }

        private class Orc
        {
            public CineFigure fig;
            public CharacterController body;
            public Vector2[] beat;
            public int next;
            public int dir = 1;
            public bool chasing;
            public bool ambush;
            public bool woken;
            public Vector3 home;
        }

        private readonly Config _c;
        private readonly OpeningVillage _village;
        private readonly CineFigure _ardan;
        private readonly CharacterController _ardanBody;
        private readonly List<Orc> _orcs = new List<Orc>();
        private readonly Func<string, string> _text;
        private Vector3 _respawn;
        private Quaternion _respawnFacing;
        private int _reached = -1;
        private float _caught = -1f;

        /// <summary>Set while the player has no say: caught, fading, coming back.</summary>
        public bool Frozen => _caught >= 0f;

        /// <summary>0..1, the black over the screen while he is caught and brought back.</summary>
        public float Fade { get; private set; }

        /// <summary>The checkpoint notice's text and how much of it shows (0..1).</summary>
        public string Notice { get; private set; }
        public float NoticeAlpha { get; private set; }
        private float _noticeT = -1f;

        public int Catches { get; private set; }

        /// <summary>Set once the mage holds the bank: nothing chases him any more.</summary>
        public bool Off { get; private set; }

        /// <summary>The mage's scene begins: the ambush orc is handed over (sprung if it was not), the
        /// patrols far behind are put away, and the chase is over.</summary>
        public CineFigure Stop()
        {
            Off = true;
            CineFigure ambush = null;
            foreach (var o in _orcs)
            {
                if (o.ambush)
                {
                    ambush = o.fig;
                    if (!o.fig.gameObject.activeSelf)
                    {
                        o.fig.gameObject.SetActive(true);
                        Place(o.body, o.home);
                    }
                }
                else o.fig.gameObject.SetActive(false);
            }
            _noticeT = -1f;
            NoticeAlpha = 0f;
            return ambush;
        }

        /// <summary>For the check: how many orcs there are, how many are chasing, the nearest's distance.</summary>
        public string Report()
        {
            var me = _ardan.transform.position;
            var near = float.MaxValue;
            var chasing = 0;
            var active = 0;
            foreach (var o in _orcs)
            {
                if (!o.fig.gameObject.activeSelf) continue;
                active++;
                if (o.chasing) chasing++;
                near = Mathf.Min(near, (o.fig.transform.position - me).magnitude);
            }
            return $"orcs {active}/{_orcs.Count} chasing {chasing} nearest {near:F1}";
        }

        public MissionChase(Config c, OpeningVillage village, CineFigure ardan, CharacterController ardanBody,
            Func<string, OpeningLife.CastMember?> cast, Material flame, Transform parent, Func<string, string> text)
        {
            _c = c;
            _village = village;
            _ardan = ardan;
            _ardanBody = ardanBody;
            _text = text;
            _respawn = ardan.transform.position;
            _respawnFacing = ardan.transform.rotation;
            if (c.orcs != null)
                foreach (var p in c.orcs)
                {
                    var o = Spawn(p.figure, cast, flame, parent);
                    if (o == null) continue;
                    o.beat = Points(p.beat);
                    o.home = new Vector3(o.beat[0].x, 0f, o.beat[0].y);
                    _orcs.Add(o);
                }
            if (c.ambush != null)
            {
                var o = Spawn(c.ambush.figure, cast, flame, parent);
                if (o != null)
                {
                    o.ambush = true;
                    o.home = new Vector3(c.ambush.hide[0], 0f, c.ambush.hide[1]);
                    _orcs.Add(o);
                }
            }
            ResetOrcs();
        }

        private Orc Spawn(string figure, Func<string, OpeningLife.CastMember?> cast, Material flame, Transform parent)
        {
            var m = cast(figure);
            if (!m.HasValue || m.Value.model == null) return null;
            var go = UnityEngine.Object.Instantiate(m.Value.model, parent);
            go.name = "Chaser_" + figure;
            var fig = go.AddComponent<CineFigure>();
            fig.Init(m.Value.clips);
            if (figure == "CineOrcTorch" && flame != null) fig.LightTorch(flame);
            var body = go.AddComponent<CharacterController>();
            body.radius = _c.radius;
            body.height = Mathf.Max(_c.height, _c.radius * 2f);
            body.center = new Vector3(0f, body.height * 0.5f, 0f);
            body.skinWidth = _c.radius * 0.1f;
            body.minMoveDistance = 0f;
            body.stepOffset = 0f;
            return new Orc { fig = fig, body = body };
        }

        private void ResetOrcs()
        {
            foreach (var o in _orcs)
            {
                Place(o.body, o.home);
                o.next = o.beat != null && o.beat.Length > 1 ? 1 : 0;
                o.dir = 1;
                o.chasing = false;
                o.woken = false;
                o.fig.gameObject.SetActive(!o.ambush);
                o.fig.Play("Idle", 0f);
            }
        }

        public void Step(float dt)
        {
            if (Off) return;
            if (_caught >= 0f)
            {
                StepCaught(dt);
                return;
            }
            var me = _ardan.transform.position;
            var me2 = new Vector2(me.x, me.z);
            Checkpoints(me2, dt);
            var hidden = _village.InWheat(me2);
            foreach (var o in _orcs)
            {
                if (!o.fig.gameObject.activeSelf)
                {
                    // the ambush: out of the bushes when he comes near the bridge
                    var a = _c.ambush;
                    if (o.ambush && !o.woken && (me2 - new Vector2(a.trigger[0], a.trigger[1])).magnitude < a.triggerRadius)
                    {
                        o.woken = true;
                        o.chasing = true;
                        o.fig.gameObject.SetActive(true);
                    }
                    continue;
                }
                var p = o.fig.transform.position;
                var to = new Vector2(me.x - p.x, me.z - p.z);
                var d = to.magnitude;
                if (o.ambush)
                {
                    // it runs him down to the bridge's far end and stops there (the mage's place, later)
                    if (p.z <= _c.ambush.stopZ)
                    {
                        // it waits there; it still catches him if he comes back to it
                        o.fig.Play("Idle", 0.2f);
                        o.fig.Tick(dt);
                    }
                    else Move(o, to, _c.ambush.runSpeed, true, dt);
                }
                else
                {
                    var sees = d < _c.sight && (!hidden || d < _c.wheatSight);
                    if (sees) o.chasing = true;
                    else if (d > _c.lose) o.chasing = false;
                    if (o.chasing) Move(o, to, _c.runSpeed, true, dt);
                    else Walk(o, dt);
                }
                if (d < _c.catchDistance)
                {
                    _caught = 0f;
                    Catches++;
                    _ardan.Play("Idle", 0.1f);
                    foreach (var q in _orcs) q.fig.Play("Idle", 0.2f);
                    return;
                }
            }
        }

        private void Walk(Orc o, float dt)
        {
            if (o.beat == null || o.beat.Length < 2) { o.fig.Play("Idle", 0.2f); o.fig.Tick(dt); return; }
            var p = o.fig.transform.position;
            var target = o.beat[o.next];
            var to = new Vector2(target.x - p.x, target.y - p.z);
            if (to.magnitude < 0.4f)
            {
                // to and fro along the beat
                if (o.next + o.dir >= o.beat.Length || o.next + o.dir < 0) o.dir = -o.dir;
                o.next += o.dir;
                return;
            }
            Move(o, to, _c.walkSpeed, false, dt);
        }

        private void Move(Orc o, Vector2 to, float speed, bool run, float dt)
        {
            var t = o.fig.transform;
            var dir = new Vector3(to.x, 0f, to.y).normalized;
            o.body.Move(dir * speed * dt);
            var p = t.position;
            p.y = _village.HeightAt(new Vector2(p.x, p.z));
            t.position = p;
            if (dir.sqrMagnitude > 0f) t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(dir), Mathf.Clamp01(_c.turnRate * dt));
            var clips = o.fig.Clips;
            var natural = run ? clips.runSpeed : clips.walkSpeed;
            o.fig.Play(run ? "Run" : "Walk", 0.2f, natural > 0f ? speed / natural : 1f);
            o.fig.Tick(dt);
        }

        private void Checkpoints(Vector2 me, float dt)
        {
            if (_c.checkpoints != null)
                for (var i = _reached + 1; i < _c.checkpoints.Length; i++)
                {
                    var k = _c.checkpoints[i];
                    if ((me - new Vector2(k.x, k.z)).magnitude > k.r) continue;
                    _reached = i;
                    _respawn = new Vector3(k.x, 0f, k.z);
                    _respawnFacing = _ardan.transform.rotation;
                    Notice = _text(_c.checkpointKey);
                    _noticeT = 0f;
                }
            if (_noticeT >= 0f)
            {
                _noticeT += dt;
                var n = _c.noticeSeconds;
                NoticeAlpha = Mathf.Clamp01(Mathf.Min(_noticeT / 0.3f, (n - _noticeT) / 0.6f));
                if (_noticeT > n) { _noticeT = -1f; NoticeAlpha = 0f; }
            }
        }

        /// <summary>Caught: a beat to see it, a fade to black, back to the checkpoint, a fade back in.</summary>
        private void StepCaught(float dt)
        {
            _caught += dt;
            var hold = _c.caughtSeconds;
            var f = _c.fadeSeconds;
            _ardan.Tick(dt);
            foreach (var o in _orcs) if (o.fig.gameObject.activeSelf) o.fig.Tick(dt);
            if (_caught < hold) Fade = 0f;
            else if (_caught < hold + f) Fade = (_caught - hold) / f;
            else if (_caught < hold + f + dt * 1.5f)
            {
                Fade = 1f;
                Place(_ardanBody, _respawn);
                _ardan.transform.rotation = _respawnFacing;
                _ardan.Play("Idle", 0f);
                ResetOrcs();
                // an ambush already behind him stays sprung only if he is past it
                foreach (var o in _orcs)
                    if (o.ambush && _respawn.z < _c.ambush.trigger[1] - _c.ambush.triggerRadius) o.fig.gameObject.SetActive(false);
            }
            else if (_caught < hold + 2f * f + dt * 1.5f) Fade = 1f - (_caught - hold - f - dt * 1.5f) / f;
            else
            {
                Fade = 0f;
                _caught = -1f;
            }
        }

        private static void Place(CharacterController body, Vector3 at)
        {
            body.enabled = false;
            body.transform.position = at;
            body.enabled = true;
        }

        private static Vector2[] Points(float[] xz)
        {
            var p = new Vector2[xz.Length / 2];
            for (var i = 0; i < p.Length; i++) p[i] = new Vector2(xz[i * 2], xz[i * 2 + 1]);
            return p;
        }
    }
}
