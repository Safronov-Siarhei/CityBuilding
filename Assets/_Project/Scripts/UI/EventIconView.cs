using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// One round icon of the event feed, built ahead of time by SetupProject and reused for every
    /// event that lands on it -- nothing is instantiated while the game runs.
    ///
    /// A tap sends the camera to the event (EventFeedController decides where). Holding the icon --
    /// or hovering it with a mouse -- shows the event's one line of text beside it instead, and a
    /// hold long enough to read that line is not also taken as a tap when the finger lifts.
    /// </summary>
    public class EventIconView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        [SerializeField] private CanvasGroup group;

        /// <summary>A radially filled ring in the event's colour, which drains as the icon's time runs out.</summary>
        [SerializeField] private Image rim;

        [SerializeField] private Image picture;

        /// <summary>The glyph drawn large, for an event with no picture of its own (a raid, a mood).</summary>
        [SerializeField] private Image bigGlyph;

        [SerializeField] private Image badge;
        [SerializeField] private Image badgeGlyph;

        [SerializeField] private GameObject counter;
        [SerializeField] private Text counterText;

        [SerializeField] private GameObject caption;
        [SerializeField] private Text captionText;

        /// <summary>Longer than this between press and release and the press was for reading the caption, not a tap.</summary>
        private const float ReadingHoldSeconds = 0.45f;

        public event Action<EventIconView> Clicked;

        public RectTransform Rect => (RectTransform)transform;

        private float _pressedAt = -1f;
        private int _shownCount = -1;

        public void Show(Sprite pictureSprite, Sprite glyphSprite, Color colour, string text, int count)
        {
            var hasPicture = pictureSprite != null;
            picture.sprite = pictureSprite;
            picture.enabled = hasPicture;

            // With a picture the glyph is the badge in its corner; without one it IS the picture.
            bigGlyph.sprite = glyphSprite;
            bigGlyph.enabled = !hasPicture && glyphSprite != null;
            bigGlyph.color = colour;
            badge.gameObject.SetActive(hasPicture && glyphSprite != null);
            badge.color = colour;
            badgeGlyph.sprite = glyphSprite;

            rim.color = colour;
            captionText.text = text;
            caption.SetActive(false);
            SetCount(count);
            gameObject.SetActive(true);
        }

        public void SetCount(int count)
        {
            if (count == _shownCount) return;
            _shownCount = count;

            counter.SetActive(count > 1);
            // Built only when the number changes, never per frame.
            if (count > 1) counterText.text = count.ToString();
        }

        /// <summary>How much of the icon's time is left, 0..1, and how visible it is.</summary>
        public void SetProgress(float remaining01, float alpha, float scale)
        {
            rim.fillAmount = remaining01;
            group.alpha = alpha;
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        public void Hide()
        {
            caption.SetActive(false);
            _pressedAt = -1f;
            _shownCount = -1;
            gameObject.SetActive(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressedAt = Time.unscaledTime;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var held = _pressedAt >= 0f ? Time.unscaledTime - _pressedAt : 0f;
            _pressedAt = -1f;
            caption.SetActive(false);
            if (held > ReadingHoldSeconds) return;

            Clicked?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            caption.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            caption.SetActive(false);
        }
    }
}
