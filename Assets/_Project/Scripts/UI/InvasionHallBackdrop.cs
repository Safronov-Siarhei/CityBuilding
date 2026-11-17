using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The Invasion screen's background (the user, 2026-11-17: «возьми 3д сцену зала короля из второй миссии, добавь ей
    /// затемнение или размытие ... + мягкий пролёт»): mission 2's throne room (M2ThroneSet, its still court in it), far
    /// from the menu's world, seen by its own camera drifting slowly down the hall and back. The camera draws into a
    /// small texture (`renderHeight` rows, whatever the screen), halved once more, and the screen stretches that:
    /// the halving and the stretch are the blur (the same on every phone), and the cost is a few percent of a frame. The screen's tint and vignette darken it. Nothing of it exists until the screen first
    /// shows; while the screen is fully up the menu's own camera draws nothing (it still clears).
    /// </summary>
    public class InvasionHallBackdrop : MonoBehaviour
    {
        [SerializeField] private GameObject hallSet;
        [SerializeField] private RawImage target;
        [SerializeField] private CanvasGroup screen;
        [SerializeField] private Camera menuCamera;
        /// <summary>The flight, in the hall's own space (x across, y up, z from the doors -3.5 to the throne +3.5).</summary>
        [SerializeField] private Vector3 flightFrom = new Vector3(-0.35f, 1.45f, -3.15f);
        [SerializeField] private Vector3 flightTo = new Vector3(0.35f, 1.15f, 0.6f);
        [SerializeField] private Vector3 lookAt = new Vector3(0f, 0.95f, 3.4f);
        [SerializeField] private float flightSeconds = 32f;
        [SerializeField] private float sway = 0.25f;
        [SerializeField] private int renderHeight = 360;

        private static readonly Vector3 Far = new Vector3(3000f, 0f, 3000f);

        private Transform _hall;
        private Camera _camera;
        private RenderTexture _texture;
        private RenderTexture _soft;
        private int _menuMask = ~0;
        private bool _menuMuted;
        private float _clock;

        private void Update()
        {
            var shown = screen != null && screen.alpha > 0.01f;
            if (shown && _hall == null) Build();
            if (_camera != null && _camera.enabled != shown) _camera.enabled = shown;
            MuteMenu(screen != null && screen.alpha > 0.99f);
            if (!shown || _camera == null) return;

            FitTexture();
            Graphics.Blit(_texture, _soft);      // last frame's picture, halved: the blur
            _clock += Time.unscaledDeltaTime;
            // there and back, eased at both ends, a slow sideways sway on top
            var t = Mathf.PingPong(_clock / Mathf.Max(1f, flightSeconds), 1f);
            t = t * t * (3f - 2f * t);
            var at = Vector3.Lerp(flightFrom, flightTo, t);
            at.x += Mathf.Sin(_clock * 0.17f) * sway;
            at.y += Mathf.Sin(_clock * 0.23f) * 0.05f;
            var look = lookAt + new Vector3(Mathf.Sin(_clock * 0.11f) * 0.4f, 0f, 0f);
            _camera.transform.SetPositionAndRotation(_hall.TransformPoint(at), Quaternion.LookRotation(_hall.TransformPoint(look) - _hall.TransformPoint(at)));
        }

        private void Build()
        {
            _hall = new GameObject("InvasionHall").transform;
            _hall.position = Far;
            if (hallSet != null)
            {
                var set = Instantiate(hallSet, _hall, false);
                set.transform.localScale = hallSet.transform.localScale;
            }
            // the room's own warm light: the menu's sun barely reaches inside its walls
            foreach (var (pos, intensity) in new[] { (new Vector3(0f, 2.1f, -1.2f), 1.6f), (new Vector3(0f, 2.0f, 2.4f), 1.9f) })
            {
                var lamp = new GameObject("Lamp", typeof(Light)).GetComponent<Light>();
                lamp.transform.SetParent(_hall, false);
                lamp.transform.localPosition = pos;
                lamp.type = LightType.Point;
                lamp.color = new Color(1f, 0.82f, 0.6f);
                lamp.range = 6f;
                lamp.intensity = intensity;
                lamp.shadows = LightShadows.None;
            }
            _camera = new GameObject("InvasionHallCamera", typeof(Camera)).GetComponent<Camera>();
            _camera.transform.SetParent(_hall, false);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.08f, 0.06f, 0.05f);
            _camera.fieldOfView = 52f;
            _camera.nearClipPlane = 0.05f;
            _camera.farClipPlane = 25f;
            _camera.allowMSAA = false;
            _camera.allowHDR = false;
            FitTexture();
        }

        private void FitTexture()
        {
            var h = Mathf.Max(90, renderHeight);
            var w = Mathf.Max(160, Mathf.RoundToInt(h * (float)Screen.width / Mathf.Max(1, Screen.height)));
            if (_texture != null && _texture.width == w && _texture.height == h) return;
            Release();
            _texture = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Bilinear, name = "InvasionHall" };
            _soft = new RenderTexture(w / 2, h / 2, 0, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Bilinear, name = "InvasionHallSoft" };
            _camera.targetTexture = _texture;
            _camera.aspect = (float)w / h;
            if (target != null) target.texture = _soft;
        }

        private void Release()
        {
            if (_camera != null) _camera.targetTexture = null;
            foreach (var t in new[] { _texture, _soft })
                if (t != null) { t.Release(); Destroy(t); }
            _texture = _soft = null;
        }

        private void MuteMenu(bool mute)
        {
            if (menuCamera == null || mute == _menuMuted) return;
            _menuMuted = mute;
            if (mute) { _menuMask = menuCamera.cullingMask; menuCamera.cullingMask = 0; }
            else menuCamera.cullingMask = _menuMask;
        }

        private void OnDestroy()
        {
            MuteMenu(false);
            Release();
            if (_hall != null) Destroy(_hall.gameObject);
        }
    }
}
