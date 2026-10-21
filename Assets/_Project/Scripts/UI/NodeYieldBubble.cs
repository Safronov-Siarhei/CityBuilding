using CityBuilder.Maps;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The little round badge over a tree or a boulder the player tapped: how much is left in it
    /// (the user, 2026-10-21). It follows the node and counts down while a worker chips at it --
    /// 5, 4, 3 -- and goes after a few seconds, or with the node.
    ///
    /// One badge for the whole game, on its own overlay canvas, created on the first tap: a
    /// world-space canvas per node would be layout and batching cost on a phone for a number
    /// nobody looks at until they ask.
    /// </summary>
    public class NodeYieldBubble : MonoBehaviour
    {
        private const float ShowSeconds = 4f;
        private const float Diameter = 64f;
        private const float ClearanceAboveNode = 1.5f; // above the harvest bar, which hangs at +0.6

        private static NodeYieldBubble _instance;

        private RectTransform _canvasRect;
        private RectTransform _badge;
        private Text _label;
        private ResourceNode _node;
        private Camera _camera;
        private float _heightAbove;
        private float _hideIn;
        private int _shown = -1;

        /// <summary>The node it currently stands over, or null -- for the tests.</summary>
        public static ResourceNode Showing => _instance != null && _instance._badge.gameObject.activeSelf ? _instance._node : null;

        /// <summary>The number on it -- for the tests.</summary>
        public static string Text => _instance != null ? _instance._label.text : null;

        public static void Show(ResourceNode node, Camera camera)
        {
            if (node == null || camera == null) return;
            if (_instance == null) _instance = Create();
            _instance.Attach(node, camera);
        }

        private static NodeYieldBubble Create()
        {
            var go = new GameObject("NodeYieldBubble", typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5; // over the world, under every panel
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var bubble = go.AddComponent<NodeYieldBubble>();
            bubble._canvasRect = (RectTransform)go.transform;

            var badge = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(go.transform, false);
            bubble._badge = (RectTransform)badge.transform;
            bubble._badge.sizeDelta = new Vector2(Diameter, Diameter);
            var image = badge.GetComponent<Image>();
            image.sprite = CircleSprite();
            image.raycastTarget = false;

            var label = new GameObject("Amount", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(badge.transform, false);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            bubble._label = label.GetComponent<Text>();
            bubble._label.font = UiTheme.Heavy;
            bubble._label.fontSize = 30;
            bubble._label.alignment = TextAnchor.MiddleCenter;
            bubble._label.color = UiTheme.Ink;
            bubble._label.raycastTarget = false;

            badge.SetActive(false);
            return bubble;
        }

        private void Attach(ResourceNode node, Camera camera)
        {
            _node = node;
            _camera = camera;
            _heightAbove = HarvestProgressBar.MeasuredHeightOf(node.transform) + ClearanceAboveNode;
            _hideIn = ShowSeconds;
            _shown = -1;
            _badge.gameObject.SetActive(true);
            LateUpdate();
        }

        private void LateUpdate()
        {
            if (!_badge.gameObject.activeSelf) return;

            _hideIn -= Time.unscaledDeltaTime;
            if (_node == null || !_node.isActiveAndEnabled || _node.IsDepleted || _camera == null || _hideIn <= 0f)
            {
                _badge.gameObject.SetActive(false);
                _node = null;
                return;
            }

            // Normally an overlay over the game camera; when something renders this canvas through
            // a camera of its own (the playtest photographs), that camera is the one to follow.
            var canvas = _canvasRect.GetComponent<Canvas>();
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var view = uiCamera != null ? uiCamera : _camera;
            var screen = view.WorldToScreenPoint(_node.transform.position + Vector3.up * _heightAbove);
            // Behind the camera: folded away, not switched off, so it comes back when turned to.
            _badge.localScale = screen.z > 0f ? Vector3.one : Vector3.zero;
            if (screen.z <= 0f) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, uiCamera, out var local);
            _badge.anchoredPosition = local;

            if (_node.RemainingYield != _shown)
            {
                _shown = _node.RemainingYield;
                _label.text = _shown.ToString();
            }
        }

        /// <summary>A parchment disc with an ink rim, drawn once.</summary>
        private static Sprite CircleSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            var centre = (size - 1) * 0.5f;
            Color32 fill = UiTheme.Parchment, rim = UiTheme.Ink;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = Mathf.Sqrt((x - centre) * (x - centre) + (y - centre) * (y - centre));
                    var edge = Mathf.Clamp01(centre - d + 0.5f);          // antialiased outer edge
                    var colour = d > centre - 4f ? rim : fill;            // a 4-px rim
                    colour.a = (byte)(255 * edge);
                    pixels[y * size + x] = colour;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
