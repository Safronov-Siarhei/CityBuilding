using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>The menu's camera over the scene: a fixed view that breathes -- a few degrees of drift and a little in and out.</summary>
    public class MenuVignetteCamera : MonoBehaviour
    {
        [SerializeField] private float distance = 7f;
        [SerializeField] private float pitch = 27f;
        [SerializeField] private float drift = 5f;

        private Vector3 _target;
        private float _yaw;

        public void Frame(Vector3 target, float yaw)
        {
            _target = target;
            _yaw = yaw;
            Update();
        }

        private void Update()
        {
            var t = Time.time;
            var yaw = _yaw + Mathf.Sin(t * 0.045f) * drift;
            var d = distance * (1f + Mathf.Sin(t * 0.07f) * 0.04f);
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            // Aimed a little short of the scene, so the work stands in the upper half of the screen
            // and the buttons along the bottom cover grass, not people.
            var flat = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            var aim = _target - flat * 1.1f;
            transform.SetPositionAndRotation(aim - rotation * Vector3.forward * d, rotation);
        }
    }
}
