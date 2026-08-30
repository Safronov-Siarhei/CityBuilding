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
    ///
    /// **The interval is the whole cost of the minimap**, because this camera is framed on the
    /// entire 200x200 map: nothing it draws is ever culled, so one refresh costs more than a
    /// normal frame does. What it shows changes slowly -- a building appearing, a tree felled --
    /// and the "where am I looking" marker is a separate UI element updated every frame in
    /// MinimapCameraIndicator.LateUpdate, so it stays live no matter how slow this is.
    /// </summary>
    public class MinimapCameraUpdater : MonoBehaviour
    {
        private const float RefreshIntervalSeconds = 0.4f;

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
