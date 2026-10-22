using CityBuilder.CameraControl;
using CityBuilder.Core;
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
    ///
    /// **In an Invasion run** the minimap does not show the whole map (the user, 2026-10-22): it
    /// frames inv_minimap_radius metres round where the player is looking and follows the camera
    /// rig. A small frame culls most of the map, so it can refresh more often.
    /// </summary>
    public class MinimapCameraUpdater : MonoBehaviour
    {
        private const float RefreshIntervalSeconds = 0.4f;
        private const float RunRefreshIntervalSeconds = 0.15f;

        [SerializeField] private Camera minimapCamera;
        [SerializeField] private RTSCameraController cameraRig;

        private float _timer;
        private bool _follow;

        public Camera MinimapCamera => minimapCamera;

        public void SetCameraRig(RTSCameraController rig) => cameraRig = rig;

        private void Start()
        {
            if (minimapCamera != null) minimapCamera.enabled = false;
            _follow = Invasion.InvasionMode.IsActive && cameraRig != null && minimapCamera != null;
            if (_follow) minimapCamera.orthographicSize = BalanceConfig.Instance.InvasionMinimapRadius;
        }

        private void Update()
        {
            if (minimapCamera == null) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = _follow ? RunRefreshIntervalSeconds : RefreshIntervalSeconds;

            if (_follow)
            {
                var at = cameraRig.transform.position;
                var t = minimapCamera.transform;
                t.position = new Vector3(at.x, t.position.y, at.z);
            }
            minimapCamera.Render();
        }
    }
}
