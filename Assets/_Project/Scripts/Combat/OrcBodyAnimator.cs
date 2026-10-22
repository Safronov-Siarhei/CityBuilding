using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// An orc's walk and blow without jointed limbs (the user, 2026-10-22: the orcs had no
    /// animation at all). The orc figures are one mesh each, so the whole body moves: a waddle --
    /// rolling side to side with a bob on every step -- that grows with its speed, and a lunge for
    /// each blow, a lean back and over forward. Its death is OrcCorpse's. The jungle's beasts are
    /// jointed and have FigureAnimator instead.
    ///
    /// A few sines a frame per orc on screen; off screen the figure is left alone.
    /// </summary>
    public class OrcBodyAnimator : MonoBehaviour
    {
        private const float FullSpeed = 1.3f;          // m/s at which the waddle is full
        private const float StepsPerMetre = 2.6f;
        private const float RollDegrees = 7f;
        private const float BobMetres = 0.03f;
        private const float WalkLeanDegrees = 5f;
        private const float StrikeSeconds = 0.4f;
        private const float StrikeLeanDegrees = 22f;
        private const float StrikeReachMetres = 0.08f;

        private Transform _figure;
        private Quaternion _restRotation;
        private Vector3 _restPosition;
        private float _unitScale = 1f;
        private Renderer _visibility;
        private OrcUnit _orc;
        private Vector3 _lastPosition;
        private float _phase;
        private float _run;
        private float _strike = -1f;

        /// <summary>0 standing .. 1 walking, as shown this frame -- for the tests.</summary>
        public float Run => _run;

        /// <summary>A blow is being shown.</summary>
        public bool IsStriking => _strike >= 0f;

        public void Bind(Transform figure)
        {
            _figure = figure;
            _restRotation = figure.localRotation;
            _restPosition = figure.localPosition;
            _unitScale = Mathf.Max(0.0001f, transform.lossyScale.y);
            _visibility = figure.GetComponentInChildren<Renderer>();
            _lastPosition = transform.position;
        }

        private void Start()
        {
            _orc = GetComponent<OrcUnit>();
            if (_orc != null) _orc.Struck += OnStruck;
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) _visibility = null;
        }

        private void OnDestroy()
        {
            if (_orc != null) _orc.Struck -= OnStruck;
        }

        private void OnStruck() => _strike = 0f;

        private void LateUpdate()
        {
            if (_figure == null || _figure.parent != transform) return;   // fallen: OrcCorpse has it
            var dt = Time.deltaTime;
            if (dt <= 0f) return;

            var position = transform.position;
            var speed = new Vector2(position.x - _lastPosition.x, position.z - _lastPosition.z).magnitude / dt;
            _lastPosition = position;
            if (_visibility != null && !_visibility.isVisible) return;

            _run = Mathf.MoveTowards(_run, Mathf.Clamp01(speed / FullSpeed), dt * 4f);
            _phase += speed * StepsPerMetre * Mathf.PI * dt;

            var roll = Mathf.Sin(_phase) * RollDegrees * _run;
            var bob = Mathf.Abs(Mathf.Sin(_phase)) * BobMetres * _run;
            var lean = WalkLeanDegrees * _run;
            var reach = 0f;
            if (_strike >= 0f)
            {
                _strike += dt;
                var t = _strike / StrikeSeconds;
                if (t >= 1f) _strike = -1f;
                else
                {
                    // a quick lean back to wind up, then over forward and back up
                    var swing = t < 0.25f ? -0.4f * (t / 0.25f) : Mathf.Sin((t - 0.25f) / 0.75f * Mathf.PI);
                    lean += StrikeLeanDegrees * swing;
                    reach = StrikeReachMetres * Mathf.Max(0f, swing);
                }
            }

            // about the root's own axes: +X leans it forward along the root's +Z, where it walks
            _figure.localRotation = Quaternion.Euler(lean, 0f, roll) * _restRotation;
            _figure.localPosition = _restPosition + new Vector3(0f, bob, reach) / _unitScale;
        }
    }
}
