using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// Mirrors EventLogManager.Entries into a single multi-line Text, refreshed whenever a new entry
    /// is logged -- and fits its panel to that text, up to maxHeight: two lines of log in a panel
    /// built for fourteen was a large empty sheet over the town.
    /// </summary>
    public class EventLogController : MonoBehaviour
    {
        [SerializeField] private Text logText;

        // The panel behind the text, hung from its top edge so it grows downwards. Optional: without
        // it the panel keeps the size it was built at.
        [SerializeField] private RectTransform frame;
        [SerializeField] private float maxHeight = 320f;

        // The text's inset inside the frame, top plus bottom -- read once, from how it was built.
        private float _verticalPadding;

        private void Start()
        {
            if (frame != null && logText != null)
            {
                _verticalPadding = logText.rectTransform.offsetMin.y - logText.rectTransform.offsetMax.y;
            }

            if (EventLogManager.Instance != null) EventLogManager.Instance.OnLogChanged += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            if (logText == null || EventLogManager.Instance == null) return;
            logText.text = string.Join("\n", EventLogManager.Instance.Entries);
            FitFrame();
        }

        private void FitFrame()
        {
            if (frame == null) return;

            var empty = string.IsNullOrEmpty(logText.text);
            frame.gameObject.SetActive(!empty);
            if (empty) return;

            // preferredHeight wraps at the text's current width, which the frame's width fixes.
            var height = Mathf.Min(maxHeight, logText.preferredHeight + _verticalPadding);
            frame.sizeDelta = new Vector2(frame.sizeDelta.x, height);
        }
    }
}
