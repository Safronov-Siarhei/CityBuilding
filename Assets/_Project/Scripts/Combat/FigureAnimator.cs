using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// Brings a jointed figure to life without an Animator: the King's model (Tools/blender/
    /// heroking.py) is a body with its legs, arms and head as child parts, each with its origin at
    /// its joint, and this turns them every frame (the user, 2026-10-21: «бег, удар, стояние»).
    ///
    /// - Standing: a slow breath -- the body rises a little, the arms sway, the head nods.
    /// - Running: legs stride in opposition, the arms swing against them, the body bobs on each
    ///   step and leans into the run. Blended in by how fast the figure is actually moving, so a
    ///   unit pushed sideways or slowing down does not snap between the two.
    /// - Striking: on each blow (SoldierUnit.Struck) the sword arm draws back and cuts forward.
    ///
    /// The Invasion jungle's beasts (Tools/blender/wild.py) are jointed the same way and moved by
    /// the same code, their blows coming from OrcUnit.Struck: a biped's striking arm is ArmStrike
    /// (its other ArmOff); a four-legged one has LegFL/LegFR/LegBL/LegBR, trots on diagonal pairs,
    /// and bites -- its head dips and snaps -- for a blow.
    ///
    /// The parts swing about the body's own side-to-side axis. One figure on the map, a handful of
    /// rotations a frame: nothing a phone notices, and no animation clips to import or keep in step
    /// with the model.
    /// </summary>
    public class FigureAnimator : MonoBehaviour
    {
        /// <summary>How long one blow takes, draw-back to recovery.</summary>
        public const float StrikeSeconds = 0.45f;

        /// <summary>Speed (m/s) at which the stride is full.</summary>
        private const float FullRunSpeed = 1.4f;
        private const float StrideRadiansPerSecond = 10f;
        private const float BreathRadiansPerSecond = 1.8f;
        private const float LegSwing = 38f;
        private const float ArmSwing = 22f;
        private const float StepBob = 0.025f;        // metres, on each footfall
        private const float BreathBob = 0.004f;
        private const float RunLean = 8f;

        // +1 when a positive turn about the body's X carries a part forward; the export's axis
        // conversion decides which, and this is the one place that knows.
        private const float Forward = -1f;

        private Transform _figure, _legL, _legR, _armSword, _armShield, _head;
        private Transform _legFL, _legFR, _legBL, _legBR;
        private Quaternion _figureRot, _legLRot, _legRRot, _swordRot, _shieldRot, _headRot;
        private Quaternion _legFLRot, _legFRRot, _legBLRot, _legBRRot;
        private Vector3 _figurePos;
        // One of the figure's meshes: while it is off every camera the figure is not moved. The
        // jungle holds some fifty beasts and the camera is usually on the base.
        private Renderer _visibility;
        private float _unitScale = 1f;

        private SoldierUnit _unit;
        private OrcUnit _orc;
        private Vector3 _lastPosition;
        private float _speed;
        private float _stride;
        private float _breath;
        private float _strike = -1f;

        /// <summary>0 standing .. 1 running, as shown this frame -- for the tests.</summary>
        public float Run { get; private set; }

        /// <summary>A blow is being shown.</summary>
        public bool IsStriking => _strike >= 0f;

        /// <summary>Called right after the figure is put under its unit's root.</summary>
        public void Bind(Transform figure)
        {
            _figure = figure;
            _legL = Part("LegL");
            _legR = Part("LegR");
            _armSword = Part("ArmSword") ?? Part("ArmStrike");
            _armShield = Part("ArmShield") ?? Part("ArmOff");
            _head = Part("Head");
            _legFL = Part("LegFL");
            _legFR = Part("LegFR");
            _legBL = Part("LegBL");
            _legBR = Part("LegBR");
            _figureRot = figure.localRotation;
            _figurePos = figure.localPosition;
            _legLRot = Rest(_legL);
            _legRRot = Rest(_legR);
            _swordRot = Rest(_armSword);
            _shieldRot = Rest(_armShield);
            _headRot = Rest(_head);
            _legFLRot = Rest(_legFL);
            _legFRRot = Rest(_legFR);
            _legBLRot = Rest(_legBL);
            _legBRRot = Rest(_legBR);
            _unitScale = Mathf.Max(0.0001f, transform.lossyScale.y);
            _visibility = figure.GetComponentInChildren<Renderer>();
            _lastPosition = transform.position;
        }

        private Transform Part(string suffix)
        {
            foreach (var child in _figure.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.EndsWith(suffix)) return child;
            }
            return null;
        }

        private static Quaternion Rest(Transform part) => part != null ? part.localRotation : Quaternion.identity;

        private void Start()
        {
            _unit = GetComponent<SoldierUnit>();
            if (_unit != null) _unit.Struck += OnStruck;
            _orc = GetComponent<OrcUnit>();
            if (_orc != null) _orc.Struck += OnStruck;
            // only the jungle's crowd sits out off screen -- the King is the one figure always
            // watched -- and nothing does without a graphics device, where nothing is "visible"
            if (_orc == null || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) _visibility = null;
        }

        private void OnDestroy()
        {
            if (_unit != null) _unit.Struck -= OnStruck;
            if (_orc != null) _orc.Struck -= OnStruck;
        }

        /// <summary>Starts a blow. Public for the tests and for anything that strikes without a SoldierUnit.</summary>
        public void OnStruck() => _strike = 0f;

        private void LateUpdate()
        {
            if (_figure == null) return;
            var dt = Time.deltaTime;
            if (dt <= 0f) return;

            var position = transform.position;
            var moved = new Vector2(position.x - _lastPosition.x, position.z - _lastPosition.z).magnitude / dt;
            _lastPosition = position;
            if (_visibility != null && !_visibility.isVisible)
            {
                _strike = -1f;
                return;
            }
            // a teleport (the King coming back at the castle) is not a sprint
            if (moved > 20f) moved = 0f;
            _speed = Mathf.Lerp(_speed, moved, 1f - Mathf.Exp(-dt * 10f));
            Run = Mathf.Clamp01(_speed / FullRunSpeed);

            _stride += dt * StrideRadiansPerSecond * Mathf.Lerp(0.6f, 1f, Run);
            _breath += dt * BreathRadiansPerSecond;
            var step = Mathf.Sin(_stride);
            var breath = Mathf.Sin(_breath);
            var stand = 1f - Run;

            Turn(_legL, _legLRot, Run * LegSwing * step);
            Turn(_legR, _legRRot, -Run * LegSwing * step);
            // four legs trot on the diagonals: front-left with back-right, front-right with back-left
            Turn(_legFL, _legFLRot, Run * LegSwing * step);
            Turn(_legBR, _legBRRot, Run * LegSwing * step);
            Turn(_legFR, _legFRRot, -Run * LegSwing * step);
            Turn(_legBL, _legBLRot, -Run * LegSwing * step);
            Turn(_armShield, _shieldRot, -Run * ArmSwing * 0.6f * step + stand * 2.5f * breath);

            var sword = Run * ArmSwing * step - stand * 2.5f * breath;
            var head = stand * 2f * Mathf.Sin(_breath * 0.5f) + Run * 4f;
            if (_strike >= 0f)
            {
                _strike += dt;
                var t = _strike / StrikeSeconds;
                if (t >= 1f) _strike = -1f;
                else if (_armSword != null) sword = StrikeAngle(t);
                else head = BiteAngle(t);      // a beast with no arm to swing bites
            }
            Turn(_armSword, _swordRot, sword);
            Turn(_head, _headRot, head);

            // the whole figure: a bob on each footfall or on the breath, a lean into the run
            var bob = Run * StepBob * Mathf.Abs(step) + stand * BreathBob * (breath + 1f) * 0.5f;
            _figure.localPosition = _figurePos + Vector3.up * (bob / _unitScale);
            // about the figure's OWN side axis: the model may stand turned on its root
            _figure.localRotation = _figureRot * Quaternion.AngleAxis(Forward * Run * RunLean, Vector3.right);
        }

        /// <summary>
        /// The sword arm through one blow, in degrees forward of rest: drawn back over the shoulder
        /// for the first third, a fast cut forward and down through the middle, eased back to guard.
        /// </summary>
        public static float StrikeAngle(float t)
        {
            if (t < 0.35f) return -55f * Mathf.SmoothStep(0f, 1f, t / 0.35f);
            if (t < 0.6f) return Mathf.Lerp(-55f, 75f, Mathf.SmoothStep(0f, 1f, (t - 0.35f) / 0.25f));
            return Mathf.Lerp(75f, 0f, Mathf.SmoothStep(0f, 1f, (t - 0.6f) / 0.4f));
        }

        /// <summary>A beast's head through one bite, in degrees forward (down) of rest: drawn up and back, then snapped down, then eased back.</summary>
        public static float BiteAngle(float t)
        {
            if (t < 0.3f) return -18f * Mathf.SmoothStep(0f, 1f, t / 0.3f);
            if (t < 0.5f) return Mathf.Lerp(-18f, 28f, Mathf.SmoothStep(0f, 1f, (t - 0.3f) / 0.2f));
            return Mathf.Lerp(28f, 0f, Mathf.SmoothStep(0f, 1f, (t - 0.5f) / 0.5f));
        }

        private static void Turn(Transform part, Quaternion rest, float degreesForward)
        {
            if (part == null) return;
            part.localRotation = Quaternion.AngleAxis(Forward * degreesForward, Vector3.right) * rest;
        }
    }
}
