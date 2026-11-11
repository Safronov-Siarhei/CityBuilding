using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 1's orcs and the chase (the user, 2026-11-11: a CHASE, orcs slower than Ардан):
    ///   - patrols walk their beats (the village streets, the field's cart track, the forest);
    ///   - one SEES him within `sight` -- but not in the tall wheat unless right beside him -- and
    ///     runs at him, a little slower than he runs; it gives up past `lose`, and walks back. While
    ///     it sees him a RED EYE hangs over it, and the first sight of him brings a ROAR (Spotted);
    ///   - HUNTERS (the user's playtest: «добавь орков которые будут преследовать игрока из самого
    ///     города»): once he leaves the village they come after him from it; out of sight they go
    ///     to where they last saw him and search round it, then pick up his trail again, until he
    ///     reaches the forest -- the field becomes a game of hiding in the wheat;
    ///   - the ambush orc bursts out of the bushes by the bridge and closes in to `follow` metres
    ///     behind him, so at the crossing it is right on his heels (the mage's scene takes it);
    ///   - an orc that stops against a tree or a house steps round it (it used to stand there);
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
            public float detourSeconds, eyeHeight, eyeSize, roarCooldown;
            public string checkpointKey;
            public Checkpoint[] checkpoints;
            public Patrol[] orcs;
            public Ambush ambush;
            public Hunters hunters;
        }

        [Serializable] public class Checkpoint { public float x, z, r; }
        [Serializable] public class Patrol { public string figure; public float[] beat; }
        [Serializable] public class Ambush { public string figure; public float[] hide, trigger; public float triggerRadius, runSpeed, follow, catchUp; }
        [Serializable] public class Hunters { public string[] figures; public float[] spawn; public float startZ, giveUpZ, searchSeconds, searchRadius, trailSpeed; }

        private enum Hunt { Waiting, Chase, ToLastSeen, Search, Trail, Gone }

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
            public bool hunter;
            public Hunt hunt;
            public Vector2 lastSeen, searchAt;
            public float timer;
            public Vector3 home;
            public float detour, detourSide = 1f;
            public float roarAt = -99f;
            public SpriteRenderer eye;
        }

        private readonly Config _c;
        private readonly OpeningVillage _village;
        private readonly CineFigure _ardan;
        private readonly CharacterController _ardanBody;
        private readonly List<Orc> _orcs = new List<Orc>();
        private readonly Func<string, string> _text;
        private readonly System.Random _rnd = new System.Random(11);
        private Vector3 _respawn;
        private Quaternion _respawnFacing;
        private int _reached = -1;
        private float _caught = -1f;
        private float _clock;
        private bool _huntersOut;
        private static Sprite _eyeSprite;

        /// <summary>An orc has just seen him (for the roar): where it stands.</summary>
        public event Action<Vector3> Spotted;

        /// <summary>Set while the player has no say: caught, fading, coming back.</summary>
        public bool Frozen => _caught >= 0f;

        /// <summary>0..1, the black over the screen while he is caught and brought back.</summary>
        public float Fade { get; private set; }

        /// <summary>The checkpoint notice's text and how much of it shows (0..1).</summary>
        public string Notice { get; private set; }
        public float NoticeAlpha { get; private set; }
        private float _noticeT = -1f;

        public int Catches { get; private set; }

        /// <summary>The last checkpoint reached (-1: none yet).</summary>
        public int Reached => _reached;

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
                if (o.eye != null) o.eye.enabled = false;
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
            if (c.hunters?.figures != null)
                for (var i = 0; i < c.hunters.figures.Length; i++)
                {
                    var o = Spawn(c.hunters.figures[i], cast, flame, parent);
                    if (o == null) continue;
                    o.hunter = true;
                    o.home = new Vector3(c.hunters.spawn[i * 2], 0f, c.hunters.spawn[i * 2 + 1]);
                    _orcs.Add(o);
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
            // the red eye over its head while it sees him (the user's playtest)
            var eye = new GameObject("SeesYou", typeof(SpriteRenderer));
            eye.transform.SetParent(go.transform, false);
            eye.transform.localPosition = Vector3.up * _c.eyeHeight;
            var sr = eye.GetComponent<SpriteRenderer>();
            sr.sprite = EyeSprite();
            sr.enabled = false;
            sr.sortingOrder = 10;
            var scale = _c.eyeSize / Mathf.Max(0.01f, sr.sprite.bounds.size.x);
            eye.transform.localScale = Vector3.one * scale / Mathf.Max(0.01f, go.transform.lossyScale.x);
            return new Orc { fig = fig, body = body, eye = sr };
        }

        /// <summary>A small DRAWN eye, a game icon (the user: «глаз выглядит реалистичным, а должен быть
        /// маленьким и нарисованным»): a thick dark outline, a flat red almond, a round black pupil and
        /// one white glint -- no shading.</summary>
        private static Sprite EyeSprite()
        {
            if (_eyeSprite != null) return _eyeSprite;
            const int w = 64, h = 40;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "OrcEye" };
            var px = new Color32[w * h];
            var ink = new Color32(40, 12, 10, 255);
            var red = new Color32(230, 40, 30, 255);
            var black = new Color32(20, 6, 6, 255);
            var white = new Color32(255, 250, 235, 255);
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    // the almond in -1..1 across, its half-height 0.9 at the middle
                    var u = (x + 0.5f) / w * 2.2f - 1.1f;
                    var v = (y + 0.5f) / h * 2.2f - 1.1f;
                    var lid = 0.9f * (1f - u * u);
                    var c = new Color32(0, 0, 0, 0);
                    if (lid > 0f && Mathf.Abs(v) < lid + 0.16f && Mathf.Abs(u) < 1.08f) c = ink;
                    if (lid > 0.12f && Mathf.Abs(v) < lid - 0.04f) c = red;
                    var r2 = u * u + v * v * 0.8f;
                    if (c.a > 0 && c.r == red.r && r2 < 0.12f) c = black;
                    if (c.a > 0 && (u + 0.16f) * (u + 0.16f) + (v - 0.2f) * (v - 0.2f) < 0.012f) c = white;
                    px[y * w + x] = c;
                }
            tex.SetPixels32(px);
            tex.Apply();
            _eyeSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            return _eyeSprite;
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
                o.detour = 0f;
                o.hunt = Hunt.Waiting;
                o.fig.gameObject.SetActive(!o.ambush && !o.hunter);
                if (o.eye != null) o.eye.enabled = false;
                o.fig.Play("Idle", 0f);
            }
            _huntersOut = false;
        }

        public void Step(float dt)
        {
            if (Off) return;
            if (_caught >= 0f)
            {
                StepCaught(dt);
                return;
            }
            _clock += dt;
            var me = _ardan.transform.position;
            var me2 = new Vector2(me.x, me.z);
            Checkpoints(me2, dt);
            var hidden = _village.InWheat(me2);
            // the hunters come out of the village once he is out of it (and only until the forest)
            var h = _c.hunters;
            if (h != null && !_huntersOut && me.z < h.startZ && me.z > h.giveUpZ)
            {
                _huntersOut = true;
                foreach (var o in _orcs)
                    if (o.hunter)
                    {
                        Place(o.body, o.home);
                        o.fig.gameObject.SetActive(true);
                        o.hunt = Hunt.Trail;
                    }
            }
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
                        Spotted?.Invoke(o.fig.transform.position);
                    }
                    continue;
                }
                var p = o.fig.transform.position;
                var to = new Vector2(me.x - p.x, me.z - p.z);
                var d = to.magnitude;
                var sees = d < _c.sight && (!hidden || d < _c.wheatSight);
                var was = o.chasing;
                if (o.ambush)
                {
                    // it closes in to `follow` behind him and holds there: on his heels at the bridge
                    var a = _c.ambush;
                    var his = _ardan.Clips != null ? _ardan.Clips.runSpeed : a.runSpeed;
                    var speed = Mathf.Clamp(his * (1f + (d - a.follow) * a.catchUp), his * 0.5f, his * 1.6f);
                    Move(o, to, speed, true, dt);
                }
                else if (o.hunter) StepHunter(o, me2, to, d, sees, dt);
                else
                {
                    if (sees) o.chasing = true;
                    else if (d > _c.lose) o.chasing = false;
                    if (o.chasing) Move(o, to, _c.runSpeed, true, dt);
                    else Walk(o, dt);
                }
                if (o.eye != null)
                {
                    o.eye.enabled = o.chasing && sees;
                    if (o.eye.enabled && Camera.main != null) o.eye.transform.rotation = Camera.main.transform.rotation;
                }
                if (o.chasing && !was && _clock - o.roarAt > _c.roarCooldown)
                {
                    o.roarAt = _clock;
                    Spotted?.Invoke(p);
                }
                if (d < _c.catchDistance)
                {
                    _caught = 0f;
                    Catches++;
                    _ardan.Play("Idle", 0.1f);
                    foreach (var q in _orcs)
                    {
                        q.fig.Play("Idle", 0.2f);
                        if (q.eye != null) q.eye.enabled = false;
                    }
                    return;
                }
            }
        }

        /// <summary>A hunter: runs at him while it sees him; when it loses him, to where it last saw
        /// him, searches round there a while, then follows his trail (it knows roughly where he went)
        /// at a walk; past the forest's edge it gives up for good.</summary>
        private void StepHunter(Orc o, Vector2 me, Vector2 to, float d, bool sees, float dt)
        {
            var h = _c.hunters;
            var p = o.fig.transform.position;
            var at = new Vector2(p.x, p.z);
            if (me.y < h.giveUpZ)
            {
                if (o.hunt != Hunt.Gone)
                {
                    o.hunt = Hunt.Gone;
                    o.chasing = false;
                }
                Move(o, new Vector2(o.home.x, o.home.z) - at, _c.walkSpeed, false, dt);
                return;
            }
            if (sees)
            {
                o.hunt = Hunt.Chase;
                o.chasing = true;
                o.lastSeen = me;
            }
            switch (o.hunt)
            {
                case Hunt.Chase:
                    if (!sees)
                    {
                        o.hunt = Hunt.ToLastSeen;
                        o.chasing = false;
                    }
                    Move(o, sees ? to : o.lastSeen - at, _c.runSpeed, true, dt);
                    break;
                case Hunt.ToLastSeen:
                    if ((o.lastSeen - at).magnitude < 0.5f)
                    {
                        o.hunt = Hunt.Search;
                        o.timer = h.searchSeconds;
                        o.searchAt = o.lastSeen;
                    }
                    else Move(o, o.lastSeen - at, _c.runSpeed * 0.85f, true, dt);
                    break;
                case Hunt.Search:
                    o.timer -= dt;
                    if (o.timer <= 0f)
                    {
                        o.hunt = Hunt.Trail;
                        break;
                    }
                    if ((o.searchAt - at).magnitude < 0.4f)
                    {
                        var ang = (float)_rnd.NextDouble() * Mathf.PI * 2f;
                        o.searchAt = o.lastSeen + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * h.searchRadius * (float)_rnd.NextDouble();
                    }
                    Move(o, o.searchAt - at, _c.walkSpeed, false, dt);
                    break;
                case Hunt.Trail:
                    // on his trail: towards him, at a brisk walk -- he can stay ahead, but not rest
                    Move(o, to, h.trailSpeed, h.trailSpeed > _c.walkSpeed * 1.5f, dt);
                    break;
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
            // stuck against a trunk or a wall: step round it for a moment, now this side, now that
            if (o.detour > 0f)
            {
                o.detour -= dt;
                dir = (dir * 0.35f + Vector3.Cross(Vector3.up, dir) * o.detourSide).normalized;
            }
            var before = t.position;
            o.body.Move(dir * speed * dt);
            var p = t.position;
            var moved = new Vector2(p.x - before.x, p.z - before.z).magnitude;
            if (o.detour <= 0f && speed * dt > 1e-4f && moved < speed * dt * 0.3f && to.magnitude > 0.5f)
            {
                o.detour = _c.detourSeconds;
                o.detourSide = -o.detourSide;
            }
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
