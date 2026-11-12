using System.Collections;
using CityBuilder.Core;
using CityBuilder.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// «Пропустить» on the opening film, bottom right (the user, 2026-11-12). A tap dips to black, runs
    /// the film to its end in one go (as MissionOne does for a restart: the director's own end stops
    /// the sounds and hands over to the mission), and fades back in on the mission. Shown once the
    /// loading screen is gone; gone with the film.
    /// </summary>
    public class FilmSkip : MonoBehaviour
    {
        private const float Step = 1f / 15f;
        private const float FadeSeconds = 0.35f;

        private OpeningDirector _director;
        private CanvasGroup _button;
        private Image _black;
        private bool _skipping;

        public static FilmSkip Attach(OpeningDirector director)
        {
            var go = new GameObject("FilmSkip", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;          // explicit: a canvas made in code at the default 0 is never sorted
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 1f;
            var skip = go.AddComponent<FilmSkip>();
            skip._director = director;
            skip.Build(go.transform);
            return skip;
        }

        private void Build(Transform root)
        {
            var black = new GameObject("Black", typeof(RectTransform), typeof(Image));
            black.transform.SetParent(root, false);
            var br = (RectTransform)black.transform;
            br.anchorMin = Vector2.zero;
            br.anchorMax = Vector2.one;
            br.offsetMin = new Vector2(-50f, -50f);
            br.offsetMax = new Vector2(50f, 50f);
            _black = black.GetComponent<Image>();
            _black.color = new Color(0f, 0f, 0f, 0f);
            _black.raycastTarget = false;

            var area = Screen.safeArea;
            var inset = Screen.width > 0 ? new Vector2((Screen.width - area.xMax) / Screen.width, area.yMin / Screen.height) : Vector2.zero;
            var go = new GameObject("Skip", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
            go.transform.SetParent(root, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = new Vector2(1f - inset.x, inset.y);
            r.pivot = new Vector2(1f, 0f);
            r.anchoredPosition = new Vector2(-20f, 20f);
            r.sizeDelta = new Vector2(125f, 39f);
            var img = go.GetComponent<Image>();
            img.sprite = UnityEngine.Resources.Load<Sprite>($"MenuSkins/{HudSkins.Current}/button")
                         ?? UnityEngine.Resources.Load<Sprite>($"MenuSkins/{HudSkin.Classic}/button");
            if (img.sprite != null && img.sprite.border.sqrMagnitude > 0f) img.type = Image.Type.Sliced;
            go.GetComponent<Button>().onClick.AddListener(Skip);
            var label = MissionSkin.Label(go.transform, Localization.Get("#film_skip"), UiTheme.Bold, 16, MissionSkin.Ink(HudInk.OnButton),
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(7f, 3f));
            label.rectTransform.anchoredPosition = new Vector2(0f, 1.5f);
            label.resizeTextForBestFit = true;
            label.resizeTextMaxSize = 16;
            _button = go.GetComponent<CanvasGroup>();
            _button.alpha = 0f;
            _button.blocksRaycasts = false;
        }

        private void Update()
        {
            if (_skipping) return;
            if (_director == null || (!_director.Playing && _director.Time > 0f))
            {
                Destroy(gameObject);
                return;
            }
            var show = !LoadingScreen.Busy && _director.Playing;
            _button.alpha = Mathf.MoveTowards(_button.alpha, show ? 1f : 0f, Time.unscaledDeltaTime * 4f);
            _button.blocksRaycasts = show;
        }

        private void Skip()
        {
            if (_skipping || _director == null || !_director.Playing) return;
            _skipping = true;
            _button.alpha = 0f;
            _button.blocksRaycasts = false;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            for (var t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime)
            {
                _black.color = new Color(0f, 0f, 0f, t / FadeSeconds);
                yield return null;
            }
            _black.color = Color.black;
            yield return null;
            // the rest of the film in one go: its end silences it and starts the mission
            while (_director != null && _director.Playing) _director.Step(Step);
            yield return null;
            for (var t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime)
            {
                _black.color = new Color(0f, 0f, 0f, 1f - t / FadeSeconds);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
