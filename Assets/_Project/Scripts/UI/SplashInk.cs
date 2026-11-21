using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The studio splash «Чернильное перо» (Design/features/studio-splash.md; the user, 2026-11-21): the first
    /// scene of the build. A quill draws the Depigo Games mark and writes the name on paper, the ink soaks into
    /// the petals, the picture fades to black and the main menu, which has been loading underneath the whole
    /// time, takes over. A tap skips (a short fade, then the menu as soon as it is loaded). If the menu is not
    /// ready when the splash ends, the finished logo simply stays on the paper until it is.
    /// Every shape and time comes from UI/Splash/splash_ink.json (see SplashInkData).
    /// </summary>
    public class SplashInk : MonoBehaviour
    {
        [SerializeField] private TextAsset dataJson;
        [SerializeField] private string nextScene = "MainMenu";
        [SerializeField] private RectTransform content;      // zoomed: the drawing, not the paper
        [SerializeField] private InkStrokeGraphic strokes;
        [SerializeField] private RawImage fill;
        [SerializeField] private RectTransform pen;
        [SerializeField] private Image fade;

        private SplashInkData _data;
        private AsyncOperation _load;
        private Material _fillMaterial;
        private float _t;
        private int _penIndex;
        private bool _skipping;
        private float _skipFrom, _skipT;
        private bool _released;

        private static readonly int RadiusId = Shader.PropertyToID("_Radius");

        private void Start()
        {
            _data = dataJson != null ? SplashInkData.Parse(dataJson.text) : null;
            _load = SceneManager.LoadSceneAsync(nextScene);
            if (_load != null) _load.allowSceneActivation = false;
            if (_data == null) { _skipping = true; return; }

            var ink = SplashInkData.Hex(_data.ink, Color.black);
            strokes.color = ink;
            strokes.Init(_data);

            var f = _data.fill;
            fill.color = ink;
            fill.rectTransform.anchoredPosition = new Vector2(f.x - _data.width * 0.5f, _data.height * 0.5f - f.y);
            fill.rectTransform.sizeDelta = new Vector2(f.size, f.size);
            _fillMaterial = new Material(fill.material);
            _fillMaterial.SetFloat("_Edge", f.edge / f.size);
            _fillMaterial.SetFloat("_NoiseAmp", f.noise * 0.5f / f.size);
            _fillMaterial.SetFloat("_NoiseScale", f.noiseScale * f.size);
            _fillMaterial.SetFloat(RadiusId, -1f);
            fill.material = _fillMaterial;

            pen.sizeDelta = new Vector2(_data.pen.w, _data.pen.h);
            pen.pivot = new Vector2(_data.pen.pivotX, _data.pen.pivotY);
            Apply();
        }

        private void Update()
        {
            // a hitch while the menu loads underneath must not make the pen jump
            var dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            var menuReady = _load == null || _load.progress >= 0.9f;

            if (!_skipping && Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                _skipping = true;
                _skipFrom = fade != null ? fade.color.a : 0f;
            }

            if (_skipping)
            {
                _skipT += dt;
                var a = Mathf.Lerp(_skipFrom, 1f, _data != null && _data.skipFade > 0f ? _skipT / _data.skipFade : 1f);
                SetFade(a);
                if (a >= 1f && menuReady) Release();
                return;
            }

            if (_data == null) return;
            // hold the finished logo on the paper while the menu is still loading
            if (_t + dt >= _data.fadeOut[0] && !menuReady) _t = Mathf.Max(_t, _data.fadeOut[0] - 1e-3f);
            else _t += dt;
            Apply();
            if (_t >= _data.total && menuReady) Release();
        }

        private void Apply()
        {
            var d = _data;
            content.localScale = Vector3.one * Mathf.Lerp(d.zoomFrom, 1f, SplashInkData.Ease(d.zoomEase, _t / d.fadeOut[0]));

            float a;
            if (_t < d.fadeIn[0] + d.fadeIn[1]) a = 1f - SplashInkData.Ease(d.fadeEase, (_t - d.fadeIn[0]) / d.fadeIn[1]);
            else if (_t >= d.fadeOut[0]) a = SplashInkData.Ease(d.fadeEase, (_t - d.fadeOut[0]) / d.fadeOut[1]);
            else a = 0f;
            SetFade(a);

            var f = d.fill;
            _fillMaterial.SetFloat(RadiusId, _t < f.t0 ? -1f : SplashInkData.Ease(f.ease, (_t - f.t0) / f.dur) * f.radius / f.size);

            strokes.SetTime(_t);
            MovePen();
        }

        private void MovePen()
        {
            var p = _data.pen;
            if (p.t == null || p.t.Length == 0) return;
            while (_penIndex + 1 < p.t.Length && p.t[_penIndex + 1] <= _t) _penIndex++;
            float x, y;
            if (_penIndex + 1 >= p.t.Length) { x = p.x[_penIndex]; y = p.y[_penIndex]; }
            else
            {
                var span = p.t[_penIndex + 1] - p.t[_penIndex];
                var u = span > 0f ? Mathf.Clamp01((_t - p.t[_penIndex]) / span) : 1f;
                x = Mathf.Lerp(p.x[_penIndex], p.x[_penIndex + 1], u);
                y = Mathf.Lerp(p.y[_penIndex], p.y[_penIndex + 1], u);
            }
            pen.anchoredPosition = new Vector2(x - _data.width * 0.5f, _data.height * 0.5f - y);
        }

        private void SetFade(float a)
        {
            if (fade == null) return;
            var c = fade.color; c.a = Mathf.Clamp01(a); fade.color = c;
        }

        private void Release()
        {
            if (_released) return;
            _released = true;
            if (_load != null) _load.allowSceneActivation = true;
            else SceneManager.LoadScene(nextScene);
        }

        private void OnDestroy()
        {
            if (_fillMaterial != null) Destroy(_fillMaterial);
        }
    }
}
