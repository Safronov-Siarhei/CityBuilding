using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// Loops the camera in a slow circular orbit above a point, always looking toward it -- the
    /// Main Menu's background flythrough over whichever map MainMenuBackground instantiated.
    /// Driven entirely by Time.time, so it loops seamlessly with no explicit reset step.
    /// </summary>
    public class MenuCameraFlythrough : MonoBehaviour
    {
        [SerializeField] private Vector3 center = Vector3.zero;
        [SerializeField] private float radius = 70f;
        [SerializeField] private float height = 45f;
        [SerializeField] private float orbitSeconds = 60f;
        [SerializeField] private float lookAtHeight = 0f;

        private void Update()
        {
            var angle = Time.time / orbitSeconds * Mathf.PI * 2f;
            var offset = new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
            transform.position = center + offset;
            transform.LookAt(new Vector3(center.x, lookAtHeight, center.z));
        }
    }
}
