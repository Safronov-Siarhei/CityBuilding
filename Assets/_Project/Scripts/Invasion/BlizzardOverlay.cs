using CityBuilder.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Invasion
{
    /// <summary>A white veil over the screen in a blizzard, fading in and out.</summary>
    public class BlizzardOverlay : MonoBehaviour
    {
        private UnityEngine.UI.Image _veil;
        private float _target;

        public static BlizzardOverlay Create(Transform parent)
        {
            var canvasGo = new GameObject("BlizzardOverlay", typeof(Canvas));
            canvasGo.transform.SetParent(parent, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -10;   // under the HUD
            var veil = new GameObject("Veil", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            veil.transform.SetParent(canvasGo.transform, false);
            var rect = veil.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = veil.GetComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.92f, 0.95f, 1f, 0f);
            image.raycastTarget = false;
            var overlay = canvasGo.AddComponent<BlizzardOverlay>();
            overlay._veil = image;
            return overlay;
        }

        public void Show(bool shown) => _target = shown ? 0.38f : 0f;

        private void Update()
        {
            if (_veil == null) return;
            var c = _veil.color;
            c.a = Mathf.MoveTowards(c.a, _target, Time.deltaTime * 0.5f);
            _veil.color = c;
            _veil.enabled = c.a > 0.001f;
        }
    }
}
