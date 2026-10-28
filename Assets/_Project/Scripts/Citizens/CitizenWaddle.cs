using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// A townsman's walk, for a model in one piece (the user's pick A, 2026-10-28: no swinging arms
    /// and legs for 150 people on a phone): he turns to face where he is going and rises a little
    /// on each step -- no sway from side to side (the user: «убери им тряску во все стороны»).
    /// Measured from how far the citizen went this frame; still when he stands.
    /// </summary>
    public class CitizenWaddle : MonoBehaviour
    {
        private const float StepMetres = 0.16f;     // one footfall per this much ground
        private const float HopMetres = 0.018f;
        private const float TurnPerSecond = 10f;     // how quickly he turns to his way

        private Transform _figure;
        private Vector3 _rest;
        private Quaternion _restTurn;
        private Quaternion _facing = Quaternion.identity;
        private Vector3 _last;
        private float _phase;
        private float _walk;

        public void Bind(Transform figure)
        {
            _figure = figure;
            _rest = figure.localPosition;
            _restTurn = figure.localRotation;
            _last = transform.position;
        }

        /// <summary>Which way he faces now (for the tests).</summary>
        public Vector3 Facing => _facing * Vector3.forward;

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
            _figure.localPosition = _rest + Vector3.up * (_walk * HopMetres * Mathf.Abs(Mathf.Sin(_phase)) / UnitSize.Scale);
            _figure.localRotation = _facing * _restTurn;
        }
    }
}
