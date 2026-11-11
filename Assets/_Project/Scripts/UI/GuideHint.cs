using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The sandbox's «look here» (the user, 2026-11-11; card Design/features/hints.md): a yellow arrow
    /// bobbing over one button or card and the button blinking yellow, until it is tapped, hidden, or
    /// hint_sec have gone. One at a time: a new one replaces the old. BuildHints decides where it points.
    /// </summary>
    public sealed class GuideHint : MonoBehaviour
    {
        private static readonly Color Yellow = new Color(1f, 0.85f, 0.2f, 1f);
        private static GuideHint _instance;
        private static Sprite _arrowSprite;

        private RectTransform _target;
        private Graphic _graphic;
        private Color _colour;
        private Button _button;
        private RectTransform _arrow;
        private float _until;
        private float _clock;

        /// <summary>Points at this button or card (null or hidden: nothing). Scrolls it into view first.</summary>
        public static void Point(RectTransform target)
        {
            Stop();
            if (target == null || !target.gameObject.activeInHierarchy) return;
            Reveal(target);
            if (_instance == null)
            {
                var go = new GameObject("GuideHint");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<GuideHint>();
            }
            _instance.Begin(target);
        }

        public static void Stop()
        {
            if (_instance != null) _instance.End();
        }

        private void Begin(RectTransform target)
        {
            _target = target;
            _graphic = target.GetComponent<Graphic>();
            if (_graphic != null) _colour = _graphic.color;
            _button = target.GetComponent<Button>();
            if (_button != null) _button.onClick.AddListener(End);
            _until = Time.unscaledTime + Mathf.Max(1f, BalanceConfig.Instance.HintSeconds);
            _clock = 0f;

            var canvas = target.GetComponentInParent<Canvas>();
            var root = canvas != null ? canvas.rootCanvas.transform : target.root;
            if (_arrow == null || _arrow.parent != root)
            {
                if (_arrow != null) Destroy(_arrow.gameObject);
                _arrow = BuildArrow(root);
            }
            _arrow.gameObject.SetActive(true);
            _arrow.SetAsLastSibling();
            enabled = true;
        }

        private void End()
        {
            if (_button != null) _button.onClick.RemoveListener(End);
            if (_graphic != null) _graphic.color = _colour;
            _button = null;
            _graphic = null;
            _target = null;
            if (_arrow != null) _arrow.gameObject.SetActive(false);
            enabled = false;
        }

        private void LateUpdate()
        {
            if (_target == null || !_target.gameObject.activeInHierarchy || Time.unscaledTime > _until)
            {
                End();
                return;
            }
            _clock += Time.unscaledDeltaTime;
            var period = Mathf.Max(0.1f, BalanceConfig.Instance.HintBlinkSeconds);
            var pulse = 0.5f + 0.5f * Mathf.Sin(_clock / period * Mathf.PI * 2f);
            if (_graphic != null) _graphic.color = Color.Lerp(_colour, Yellow, 0.6f * pulse);

            // over everything on its canvas: a window opened after the hint would cover it
            var parent = _arrow.parent;
            if (_arrow.GetSiblingIndex() != parent.childCount - 1) _arrow.SetAsLastSibling();
            var top = _target.TransformPoint(new Vector3(_target.rect.center.x, _target.rect.yMax, 0f));
            _arrow.position = top;
            _arrow.anchoredPosition += new Vector2(0f, 20f + Mathf.Abs(Mathf.Sin(_clock * 4f)) * 24f);
        }

        private static RectTransform BuildArrow(Transform root)
        {
            var go = new GameObject("GuideArrow", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);
            var image = go.GetComponent<Image>();
            if (_arrowSprite == null) _arrowSprite = Tutorial.TutorialHud.ArrowSprite();
            image.sprite = _arrowSprite;
            image.color = Yellow;
            image.raycastTarget = false;
            go.AddComponent<Outline>().effectColor = new Color(0.3f, 0.15f, 0f, 1f);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(80f, 80f);
            return rect;
        }

        /// <summary>A target inside a scroll view (the build menu's cards, the Laboratory's rows) is scrolled to the middle of it.</summary>
        private static void Reveal(RectTransform target)
        {
            var scroll = target.GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.content == null) return;
            Canvas.ForceUpdateCanvases();
            var content = scroll.content;
            var viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
            var local = content.InverseTransformPoint(target.TransformPoint(target.rect.center));
            if (scroll.horizontal)
            {
                var spare = content.rect.width - viewport.rect.width;
                if (spare > 0f) scroll.horizontalNormalizedPosition = Mathf.Clamp01((local.x - content.rect.xMin - viewport.rect.width * 0.5f) / spare);
            }
            if (scroll.vertical)
            {
                var spare = content.rect.height - viewport.rect.height;
                if (spare > 0f) scroll.verticalNormalizedPosition = 1f - Mathf.Clamp01((content.rect.yMax - local.y - viewport.rect.height * 0.5f) / spare);
            }
            scroll.velocity = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
