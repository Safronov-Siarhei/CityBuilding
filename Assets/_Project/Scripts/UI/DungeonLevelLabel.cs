using CityBuilder.Core;
using CityBuilder.Dungeon;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// «Уровень N» over a dungeon's mouth while it is held (the user, 2026-11-01: «подпись уровня
    /// над входом при выборе подземелья»): a parchment plaque on its own overlay canvas, over the
    /// top of the mine, gone the moment the mouth is let go. Not over the way out of a dungeon.
    /// Made once, the first time a mouth is picked up (BuildingFocus.SelectEntrance).
    /// </summary>
    public class DungeonLevelLabel : MonoBehaviour
    {
        private static readonly Color Paper = new Color(0.96f, 0.9f, 0.74f, 0.96f);
        private static readonly Color Rim = new Color(0.36f, 0.22f, 0.1f, 1f);
        private const float Clearance = 0.8f;

        private static DungeonLevelLabel _instance;

        private RectTransform _canvasRect;
        private RectTransform _plaque;
        private Text _text;
        private DungeonEntrance _target;
        private float _height;

        /// <summary>Is the plaque up, and over which mouth? -- for the tests.</summary>
        public static DungeonEntrance Shown => _instance != null && _instance._plaque.gameObject.activeSelf ? _instance._target : null;

        /// <summary>The plaque's words now -- for the tests.</summary>
        public static string Words => _instance != null ? _instance._text.text : null;

        public static void Follow(DungeonEntrance entrance)
        {
            if (entrance == null || entrance.IsExit) return;
            if (_instance == null) _instance = Create();
            _instance.Attach(entrance);
        }

        private static DungeonLevelLabel Create()
        {
            var go = new GameObject("DungeonLevelLabel", typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5; // over the world, under every panel
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var label = go.AddComponent<DungeonLevelLabel>();
            label._canvasRect = (RectTransform)go.transform;

            var plaque = new GameObject("Plaque", typeof(RectTransform), typeof(Image), typeof(Outline));
            plaque.transform.SetParent(go.transform, false);
            label._plaque = (RectTransform)plaque.transform;
            label._plaque.sizeDelta = new Vector2(220f, 58f);
            label._plaque.pivot = new Vector2(0.5f, 0f);
            var image = plaque.GetComponent<Image>();
            image.color = Paper;
            image.raycastTarget = false;
            var outline = plaque.GetComponent<Outline>();
            outline.effectColor = Rim;
            outline.effectDistance = new Vector2(3f, -3f);

            var words = new GameObject("Words", typeof(RectTransform), typeof(Text));
            words.transform.SetParent(plaque.transform, false);
            var rect = (RectTransform)words.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            label._text = words.GetComponent<Text>();
            label._text.font = UiTheme.Heavy;
            label._text.fontSize = 32;
            label._text.alignment = TextAnchor.MiddleCenter;
            label._text.color = UiTheme.Ink;
            label._text.raycastTarget = false;

            plaque.SetActive(false);
            return label;
        }

        private void Attach(DungeonEntrance entrance)
        {
            _target = entrance;
            _height = entrance.transform.position.y;
            foreach (var r in entrance.GetComponentsInChildren<Renderer>()) _height = Mathf.Max(_height, r.bounds.max.y);
            _height += Clearance;
            _text.text = string.Format(Localization.Get("#dungeon_entrance_level"), entrance.Level);
            _plaque.gameObject.SetActive(true);
            LateUpdate();
        }

        private void LateUpdate()
        {
            if (!_plaque.gameObject.activeSelf) return;
            var focus = BuildingFocus.Instance;
            var camera = Camera.main;
            // let go, or its card over it: the plaque goes
            if (_target == null || focus == null || focus.Entrance != _target || camera == null || ModalGate.IsBlocked)
            {
                _plaque.gameObject.SetActive(false);
                _target = null;
                return;
            }
            var at = _target.transform.position;
            var screen = camera.WorldToScreenPoint(new Vector3(at.x, _height, at.z));
            _plaque.localScale = screen.z > 0f ? Vector3.one : Vector3.zero;
            if (screen.z <= 0f) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out var local);
            _plaque.anchoredPosition = local;
        }
    }
}
