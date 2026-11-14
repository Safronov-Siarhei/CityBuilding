using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Citizens
{
    /// <summary>What a worker does with his hands at the place he works (the user, 2026-11-02).</summary>
    public enum WorkKind { None, Chop, Mine, Fish, Plough, Pick, Feed }

    /// <summary>
    /// A townsman's walk, for a model in one piece (the user's pick A, 2026-10-28: no swinging arms
    /// and legs for 150 people on a phone): he turns to face where he is going and rises a little
    /// on each step -- no sway from side to side (the user: «убери им тряску во все стороны»).
    /// Measured from how far the citizen went this frame; still when he stands.
    ///
    /// And his WORK (the user, 2026-11-02): standing at his tree, rock, field, orchard, pen or
    /// shore he takes up the tool of his trade (Tools/blender/tools.py: an axe, a pick, a rod, a hoe,
    /// a basket, a pail -- laid over the villager, its "Swing" part turned at the shoulder) and works
    /// it in a loop, leaning into each stroke; the fisherman casts, waits, and pulls a fish out
    /// ("Catch"). The tool is made the first time it is wanted and hidden while he walks.
    /// </summary>
    public class CitizenWaddle : MonoBehaviour
    {
        private const float StepMetres = 0.16f;     // one footfall per this much ground
        private const float HopMetres = 0.018f;
        private const float TurnPerSecond = 10f;     // how quickly he turns to his way
        // as FigureAnimator: a part turned "forward" (down, away from the body) is a negative angle about x
        private const float Forward = -1f;

        /// <summary>The tool models by WorkKind (index = kind - 1), set by CitizenVisualsManager.</summary>
        public static GameObject[] ToolModels;

        private Transform _figure;

        /// <summary>The model he is drawn with: a tool in the villagers' own space goes under it (tools.py).</summary>
        public Transform Figure => _figure;
        private Vector3 _rest;
        private Quaternion _restTurn;
        private Quaternion _facing = Quaternion.identity;
        private Vector3 _last;
        private float _phase;
        private float _walk;

        private WorkKind _work;
        private Vector3 _workFace;
        private float _workTime;
        private readonly GameObject[] _tools = new GameObject[6];
        private readonly Transform[] _swings = new Transform[6];
        private readonly Quaternion[] _swingRest = new Quaternion[6];
        private GameObject _catch;

        public void Bind(Transform figure)
        {
            _figure = figure;
            _rest = figure.localPosition;
            _restTurn = figure.localRotation;
            _last = transform.position;
        }

        /// <summary>Which way he faces now (for the tests).</summary>
        public Vector3 Facing => _facing * Vector3.forward;

        /// <summary>What he is working at now, or None.</summary>
        public WorkKind Working => _work;

        /// <summary>Takes up the tool of `kind` and works it, facing `faceTo` (a world point).</summary>
        public void Work(WorkKind kind, Vector3 faceTo)
        {
            if (kind == WorkKind.None)
            {
                StopWork();
                return;
            }
            if (_work != kind) StopWork();
            _work = kind;
            _workFace = faceTo;
            _workTime = Random.value;
            var tool = Tool(kind);
            if (tool != null && !tool.activeSelf) tool.SetActive(true);
        }

        /// <summary>Puts the tool away (he walks on).</summary>
        public void StopWork()
        {
            if (_work == WorkKind.None) return;
            var i = (int)_work - 1;
            if (_tools[i] != null) _tools[i].SetActive(false);
            if (_catch != null) _catch.SetActive(false);
            _work = WorkKind.None;
        }

        private GameObject Tool(WorkKind kind)
        {
            var i = (int)kind - 1;
            if (_tools[i] != null || _figure == null || ToolModels == null || i >= ToolModels.Length || ToolModels[i] == null) return _tools[i];
            // over the villager in his own space: same root transform as the figure (tools.py)
            var tool = Instantiate(ToolModels[i], _figure, false);
            tool.name = "Tool";
            tool.transform.localPosition = Vector3.zero;
            tool.transform.localRotation = Quaternion.identity;
            tool.transform.localScale = Vector3.one;
            foreach (var r in tool.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            foreach (var t in tool.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.EndsWith("Swing")) _swings[i] = t;
                else if (t.name.EndsWith("Catch")) _catch = t.gameObject;
            }
            if (_swings[i] != null) _swingRest[i] = _swings[i].localRotation;
            if (_catch != null) _catch.SetActive(false);
            _tools[i] = tool;
            return tool;
        }

        private void LateUpdate()
        {
            if (_figure == null) return;
            var dt = Time.deltaTime;
            if (dt <= 0f) return;
            var moved = transform.position - _last;
            moved.y = 0f;
            _last = transform.position;
            var distance = moved.magnitude;
            var walking = distance > 0.0005f;
            // eased in and out, so a stop is not a snap
            _walk = Mathf.MoveTowards(_walk, walking ? 1f : 0f, dt * 6f);
            if (walking)
            {
                // the root never turns (CitizenAgent moves it only), so the figure does
                var want = Quaternion.LookRotation(moved / distance, Vector3.up) * Quaternion.Inverse(transform.rotation);
                _facing = Quaternion.Slerp(_facing, want, 1f - Mathf.Exp(-TurnPerSecond * dt));
            }
            _phase += distance / StepMetres * Mathf.PI;
            var lift = _walk * HopMetres * Mathf.Abs(Mathf.Sin(_phase));
            var lean = 0f;
            if (_work != WorkKind.None && !walking) lean = TickWork(dt, ref lift);
            _figure.localPosition = _rest + Vector3.up * (lift / UnitSize.Scale);
            _figure.localRotation = _facing * Quaternion.AngleAxis(lean, Vector3.right) * _restTurn;
        }

        /// <summary>One frame of work: turns him to his work, moves the tool, returns his lean (degrees forward).</summary>
        private float TickWork(float dt, ref float lift)
        {
            var to = _workFace - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.0004f)
            {
                var want = Quaternion.LookRotation(to.normalized, Vector3.up) * Quaternion.Inverse(transform.rotation);
                _facing = Quaternion.Slerp(_facing, want, 1f - Mathf.Exp(-TurnPerSecond * dt));
            }
            var cycle = Cycle(_work);
            _workTime += dt / cycle;
            var t = _workTime - Mathf.Floor(_workTime);
            float swing, lean;
            switch (_work)
            {
                case WorkKind.Chop:
                case WorkKind.Mine:
                    // drawn back over the shoulder, struck down, a beat, back up
                    swing = t < 0.55f ? Mathf.Lerp(0f, -75f, Ease(t / 0.55f))
                        : t < 0.68f ? Mathf.Lerp(-75f, 45f, (t - 0.55f) / 0.13f)
                        : t < 0.8f ? 45f : Mathf.Lerp(45f, 0f, Ease((t - 0.8f) / 0.2f));
                    lean = Mathf.Clamp(swing, 0f, 45f) * 0.3f - Mathf.Clamp(-swing, 0f, 75f) * 0.06f;
                    break;
                case WorkKind.Plough:
                    // the hoe lifted and brought down into the earth
                    swing = t < 0.45f ? Mathf.Lerp(0f, -35f, Ease(t / 0.45f))
                        : t < 0.6f ? Mathf.Lerp(-35f, 30f, (t - 0.45f) / 0.15f) : Mathf.Lerp(30f, 0f, Ease((t - 0.6f) / 0.4f));
                    lean = 10f + Mathf.Clamp(swing, 0f, 30f) * 0.4f;
                    break;
                case WorkKind.Fish:
                    // cast back and out, wait with the line bobbing, pull the fish up, lower the rod
                    swing = t < 0.12f ? Mathf.Lerp(0f, -55f, Ease(t / 0.12f))
                        : t < 0.22f ? Mathf.Lerp(-55f, 25f, (t - 0.12f) / 0.1f)
                        : t < 0.8f ? 25f + Mathf.Sin(t * 60f) * 3f
                        : t < 0.88f ? Mathf.Lerp(25f, -45f, Ease((t - 0.8f) / 0.08f))
                        : Mathf.Lerp(-45f, 0f, Ease((t - 0.88f) / 0.12f));
                    lean = t < 0.8f ? 4f : -6f;
                    if (_catch != null && _catch.activeSelf != (t >= 0.82f && t < 0.98f)) _catch.SetActive(t >= 0.82f && t < 0.98f);
                    break;
                case WorkKind.Pick:
                    // up on his toes to the branches, then down to the basket
                    var up = t < 0.5f ? Mathf.Sin(t / 0.5f * Mathf.PI) : 0f;
                    lift += up * 0.03f;
                    lean = t < 0.5f ? -12f * up : 22f * Mathf.Sin((t - 0.5f) / 0.5f * Mathf.PI);
                    swing = -10f * up;
                    break;
                case WorkKind.Feed:
                    // the pail tipped out into the trough
                    swing = t < 0.3f ? Mathf.Lerp(0f, 20f, Ease(t / 0.3f))
                        : t < 0.6f ? Mathf.Lerp(20f, 80f, Ease((t - 0.3f) / 0.3f)) : Mathf.Lerp(80f, 0f, Ease((t - 0.6f) / 0.4f));
                    lean = swing * 0.2f;
                    break;
                default:
                    swing = 0f;
                    lean = 0f;
                    break;
            }
            var i = (int)_work - 1;
            if (_swings[i] != null) _swings[i].localRotation = Quaternion.AngleAxis(Forward * swing, Vector3.right) * _swingRest[i];
            return lean;
        }

        private static float Cycle(WorkKind kind) =>
            kind == WorkKind.Chop ? 1.1f : kind == WorkKind.Mine ? 1.3f : kind == WorkKind.Plough ? 1.4f
            : kind == WorkKind.Fish ? 5f : kind == WorkKind.Pick ? 1.8f : 2.2f;

        private static float Ease(float x) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(x));
    }
}
