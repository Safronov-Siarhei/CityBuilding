using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// A townsman's walk, for a model in one piece (the user's pick A, 2026-10-28: no swinging arms
    /// and legs for 150 people on a phone): a hop on each step and a sway from foot to foot,
    /// measured from how far the citizen went this frame, still when he stands.
    /// </summary>
    public class CitizenWaddle : MonoBehaviour
    {
        private const float StepMetres = 0.16f;     // one footfall per this much ground
        private const float HopMetres = 0.022f;
        private const float SwayDegrees = 5f;

        private Transform _figure;
        private Vector3 _rest;
        private Quaternion _restTurn;
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

        private void LateUpdate()
        {
            if (_figure == null) return;
            var dt = Time.deltaTime;
            if (dt <= 0f) return;
            var moved = transform.position - _last;
            moved.y = 0f;
            _last = transform.position;
            var distance = moved.magnitude;
            // eased in and out, so a stop is not a snap
            _walk = Mathf.MoveTowards(_walk, distance > 0.0005f ? 1f : 0f, dt * 6f);
            _phase += distance / StepMetres * Mathf.PI;
            var step = Mathf.Sin(_phase);
            _figure.localPosition = _rest + Vector3.up * (_walk * HopMetres * Mathf.Abs(step) / UnitSize.Scale);
            _figure.localRotation = Quaternion.Euler(0f, 0f, _walk * SwayDegrees * step) * _restTurn;
        }
    }
}
