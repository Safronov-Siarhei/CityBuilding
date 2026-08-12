using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// Throttles the minimap's dedicated top-down camera (see SetupProject.CreateMinimapCamera)
    /// to a fixed refresh rate instead of Unity's default of rendering it every single frame like
    /// the main camera -- a minimap doesn't need to be perfectly live, and this camera renders the
    /// entire scene from scratch into its own RenderTexture, so leaving it on the default cadence
    /// meant the whole game was effectively being drawn twice per frame for no visible benefit.
    /// Disabling automatic rendering and calling Camera.Render() manually on an interval is the
    /// standard Unity pattern for this -- Render() still honors targetTexture with the camera
    /// disabled.
    /// </summary>
    public class MinimapCameraUpdater : MonoBehaviour
    {
        private const float RefreshIntervalSeconds = 0.15f;

        [SerializeField] private Camera minimapCamera;

        private float _timer;

        private void Start()
        {
            if (minimapCamera != null) minimapCamera.enabled = false;
        }

        private void Update()
        {
            if (minimapCamera == null) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = RefreshIntervalSeconds;

            minimapCamera.Render();
        }
    }
}
