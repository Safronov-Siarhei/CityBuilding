using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>A tree that falls to the axes every few seconds, lies, sinks away and grows back.</summary>
    public class MenuFallingTree : MonoBehaviour
    {
        private Vector3 _towards;
        private Vector3 _base;
        private Quaternion _upright;
        private Vector3 _scale;
        private float _start;
        private const float Cycle = 9f;

        public void Fall(Vector3 towards)
        {
            _towards = towards.normalized;
            _base = transform.position;
            _upright = transform.rotation;
            _scale = transform.localScale;
            _start = Time.time + Random.Range(0f, 2f);
        }

        private void Update()
        {
            var t = Mathf.Repeat(Time.time - _start, Cycle);
            var axis = Vector3.Cross(Vector3.up, _towards);
            float tilt = 0f, size = 1f, sink = 0f;
            if (t < 5f) tilt = Mathf.Sin(t * 30f) * (t > 4f ? 2f : 0f);                       // standing; a shiver before it goes
            else if (t < 6.2f) { var k = (t - 5f) / 1.2f; tilt = 88f * k * k; }              // falling, faster and faster
            else if (t < 7.4f) tilt = 88f;                                                     // down
            else if (t < 7.9f) { tilt = 88f; sink = (t - 7.4f) / 0.5f; }                       // taken away
            else { size = Mathf.SmoothStep(0f, 1f, (t - 7.9f) / 1.1f); }                      // a new one grows

            transform.SetPositionAndRotation(_base - Vector3.up * sink * 0.6f, Quaternion.AngleAxis(tilt, axis) * _upright);
            transform.localScale = _scale * Mathf.Max(0.001f, size * (1f - sink));
        }
    }
}
