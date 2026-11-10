using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The player's frame counter (the user, 2026-11-10: switched on in the graphics settings,
    /// GraphicsQuality.ShowFps): a small number at the bottom left, right of the minimap, in every
    /// scene. It lives on its own overlay canvas made at start and kept across scenes; the number is
    /// refreshed twice a second from a table of ready strings, so it allocates nothing per frame.
    /// </summary>
    public sealed class FpsCounter : MonoBehaviour
    {
        private const float Refresh = 0.5f;
        private const int MaxShown = 240;
        private static string[] _numbers;

        private Text _text;
        private float _clock;
        private int _frames;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Application.isBatchMode) return;
            var go = new GameObject("FpsCounter", typeof(Canvas), typeof(CanvasScaler));
            DontDestroyOnLoad(go);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            go.AddComponent<FpsCounter>();
        }

        private void Awake()
        {
            if (_numbers == null)
            {
                _numbers = new string[MaxShown + 1];
                for (var i = 0; i <= MaxShown; i++) _numbers[i] = "FPS " + i;
            }

            var label = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(Outline));
            label.transform.SetParent(transform, false);
            var rect = label.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(250f, 12f);
            rect.sizeDelta = new Vector2(200f, 40f);
            _text = label.GetComponent<Text>();
            _text.font = UiTheme.Regular;
            _text.fontSize = 28;
            _text.alignment = TextAnchor.LowerLeft;
            _text.color = Color.white;
            _text.raycastTarget = false;
            _text.text = _numbers[0];
            label.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);

            GraphicsQuality.ShowFpsChanged += Show;
            Show();
        }

        private void OnDestroy() => GraphicsQuality.ShowFpsChanged -= Show;

        private void Show()
        {
            var on = GraphicsQuality.ShowFps;
            if (_text != null) _text.enabled = on;
            enabled = on;
            _clock = 0f;
            _frames = 0;
        }

        private void Update()
        {
            _frames++;
            _clock += Time.unscaledDeltaTime;
            if (_clock < Refresh) return;
            _text.text = _numbers[Mathf.Clamp(Mathf.RoundToInt(_frames / _clock), 0, MaxShown)];
            _clock = 0f;
            _frames = 0;
        }
    }
}
