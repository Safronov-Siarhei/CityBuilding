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
    /// rig. A small frame culls most of the map, so it can refresh more often. To move smoothly
    /// with the camera between refreshes (the user, 2026-10-22: it jumped), the camera frames a
    /// margin round the shown square and the picture's window (RawImage.uvRect) slides over it
    /// every frame; a refresh re-centres the camera.
    /// </summary>
    public class MinimapCameraUpdater : MonoBehaviour
    {
        private const float RefreshIntervalSeconds = 0.4f;
        private const float RunRefreshIntervalSeconds = 0.15f;

        [SerializeField] private Camera minimapCamera;
        [SerializeField] private RTSCameraController cameraRig;
        [SerializeField] private UnityEngine.UI.RawImage view;

        /// <summary>How much more than the shown square the camera frames, for the window to slide in.</summary>
        private const float Margin = 1.5f;

        private float _timer;
        private bool _follow;
        private float _radius;

        public Camera MinimapCamera => minimapCamera;

        /// <summary>Following the camera rig (a run): the minimap's middle is the rig, ViewRadius metres to its edge.</summary>
        public bool Follows => _follow;
        public Vector3 ViewCentre => cameraRig != null ? cameraRig.transform.position : Vector3.zero;
        public float ViewRadius => _radius;

        public void SetCameraRig(RTSCameraController rig) => cameraRig = rig;

        private void Start()
        {
            if (minimapCamera != null) minimapCamera.enabled = false;
            // batchmode tests run without a graphics device: nothing to draw, and rendering into
            // the texture there has crashed the editor (2026-10-22)
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) minimapCamera = null;
            _follow = Invasion.InvasionMode.IsActive && cameraRig != null && minimapCamera != null;
            if (!_follow) return;
            _radius = BalanceConfig.Instance.InvasionMinimapRadius;
            minimapCamera.orthographicSize = _radius * Margin;
        }

        private void LateUpdate()
        {
            if (!_follow || view == null) return;
            // the window over the picture: the shown square, centred on the rig, inside the frame
            var size = 1f / Margin;
            var offset = ViewCentre - minimapCamera.transform.position;
            var span = 2f * _radius * Margin;
            var u = 0.5f + offset.x / span - size * 0.5f;
            var v = 0.5f + offset.z / span - size * 0.5f;
            view.uvRect = new Rect(Mathf.Clamp(u, 0f, 1f - size), Mathf.Clamp(v, 0f, 1f - size), size, size);
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
