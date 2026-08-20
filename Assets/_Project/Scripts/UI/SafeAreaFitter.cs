using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// Keeps the UI inside <see cref="Screen.safeArea"/> -- the part of the display that is not a
    /// camera cutout, a rounded corner or the system's gesture bar.
    ///
    /// Nothing in this project accounted for it, so on any recent phone the HUD ran under the
    /// notch and the bottom of the hotbar sat beneath the gesture bar, where a tap scrolls the
    /// system rather than the game. The rig is anchored in normalised screen space, so it survives
    /// a rotation or a resolution change; the last applied rect is remembered because Unity can
    /// report a different safe area at any time (folding phones, split screen).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreen;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea == _lastSafeArea
                && Screen.width == _lastScreen.x
                && Screen.height == _lastScreen.y) return;

            Apply();
        }

        private void Apply()
        {
            if (_rect == null) _rect = GetComponent<RectTransform>();

            var safeArea = Screen.safeArea;
            var width = Screen.width;
            var height = Screen.height;
            if (width <= 0 || height <= 0) return;

            _lastSafeArea = safeArea;
            _lastScreen = new Vector2Int(width, height);

            var min = new Vector2(safeArea.xMin / width, safeArea.yMin / height);
            var max = new Vector2(safeArea.xMax / width, safeArea.yMax / height);

            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
