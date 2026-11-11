using System;
using CityBuilder.Core;
using CityBuilder.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 1's own HUD (the user's first playtest, 2026-11-11), in the game's parchment-and-gold kit:
    ///   - top left the MENU button: pauses, «Продолжить» / «Начать заново» / «В меню»;
    ///   - top right the CURRENT OBJECTIVE, changing as he gets on (Mission1.json "hud" objectives);
    ///   - top centre ARDAN's portrait, like the King's in the other modes (no health, no level), «Ардан»
    ///     under it; a tap pauses the game and opens his short story.
    /// Pausing is Time.timeScale = 0: the film's world and the mission step by deltaTime and stand.
    /// </summary>
    public class MissionHud
    {
        [Serializable] public class Config
        {
            public float button, portrait, margin;
            public string nameKey, storyKey, objectiveKey, pauseKey, resumeKey, restartKey, menuKey, closeKey, menuScene;
            public Objective[] objectives;
        }

        [Serializable] public class Objective { public string key; public float untilZ; }

        [Serializable] public struct Art
        {
            public Sprite disc, rim, mask, panel, pillParchment, pillWood, menuGlyph, closeGlyph, portrait;
        }

        private readonly Config _c;
        private readonly Art _art;
        private readonly Canvas _canvas;
        private readonly Text _objective;
        private GameObject _modal;
        private int _stage = -1;

        /// <summary>The objective's text now, for the batchmode check.</summary>
        public string ObjectiveText => _objective != null ? _objective.text : null;

        public bool Paused => _modal != null;

        public MissionHud(Config c, Art art, Transform parent)
        {
            _c = c;
            _art = art;
            var go = new GameObject("MissionHud", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 55;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 1f;
            if (UnityEngine.EventSystems.EventSystem.current == null)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            var safe = SafeArea(go.transform);

            // the menu, top left
            var menu = RoundButton(safe, "Menu", new Vector2(0f, 1f), new Vector2(c.margin + c.button * 0.5f, -c.margin - c.button * 0.5f), c.button, art.menuGlyph);
            menu.onClick.AddListener(OpenPause);

            // the objective, top right
            var pill = Box(safe, "Objective", art.pillParchment, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-c.margin, -c.margin), new Vector2(380f, 64f));
            Label(pill, Localization.Get(c.objectiveKey), UiTheme.Bold, 18, new Vector2(0f, 0.55f), new Vector2(1f, 1f), UiTheme.InkSoft, TextAnchor.MiddleCenter);
            _objective = Label(pill, "", UiTheme.Bold, 22, new Vector2(0f, 0f), new Vector2(1f, 0.62f), UiTheme.Ink, TextAnchor.MiddleCenter);

            // Ардан, top centre
            var face = RoundButton(safe, "Ardan", new Vector2(0.5f, 1f), new Vector2(0f, -c.margin - c.portrait * 0.5f), c.portrait, null);
            var maskGo = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskGo.transform.SetParent(face.transform, false);
            Fill((RectTransform)maskGo.transform, 0.1f);
            maskGo.GetComponent<Image>().sprite = art.mask;
            maskGo.GetComponent<Mask>().showMaskGraphic = false;
            var pic = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            pic.transform.SetParent(maskGo.transform, false);
            Fill((RectTransform)pic.transform, 0f);
            pic.GetComponent<Image>().sprite = art.portrait;
            pic.GetComponent<Image>().raycastTarget = false;
            face.transform.Find("Rim")?.SetAsLastSibling();
            face.onClick.AddListener(OpenStory);
            var tag = Box(safe, "Name", art.pillWood, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -c.margin - c.portrait - 4f), new Vector2(150f, 36f));
            tag.pivot = new Vector2(0.5f, 1f);
            Label(tag, Localization.Get(c.nameKey), UiTheme.Bold, 22, Vector2.zero, Vector2.one, UiTheme.OnWood, TextAnchor.MiddleCenter);
        }

        /// <summary>The objective for where he is now (by how far south he has come).</summary>
        public void Step(Vector3 ardan)
        {
            if (_c.objectives == null || _objective == null) return;
            var stage = _c.objectives.Length - 1;
            for (var i = 0; i < _c.objectives.Length; i++)
                if (ardan.z > _c.objectives[i].untilZ) { stage = i; break; }
            if (stage == _stage) return;
            _stage = stage;
            _objective.text = Localization.Get(_c.objectives[stage].key);
        }

        /// <summary>Hidden for the end (the summary takes the screen).</summary>
        public void Hide() => _canvas.gameObject.SetActive(false);

        // ------------------------------------------------------------ the windows

        private void OpenPause()
        {
            if (_modal != null) return;
            var panel = Modal(new Vector2(460f, 380f), Localization.Get(_c.pauseKey));
            WideButton(panel, Localization.Get(_c.resumeKey), 0.62f, UiTheme.Good, Close);
            WideButton(panel, Localization.Get(_c.restartKey), 0.40f, UiTheme.Wood, () =>
            {
                Close();
                MissionOne.SkipFilm = true;
                LoadingScreen.IntoGame(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            });
            WideButton(panel, Localization.Get(_c.menuKey), 0.18f, UiTheme.Wood, () =>
            {
                Close();
                LoadingScreen.OutToMenu(_c.menuScene);
            });
        }

        private void OpenStory()
        {
            if (_modal != null) return;
            var panel = Modal(new Vector2(640f, 420f), Localization.Get(_c.nameKey));
            var pic = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            pic.transform.SetParent(panel, false);
            var pr = (RectTransform)pic.transform;
            pr.anchorMin = pr.anchorMax = new Vector2(0f, 1f);
            pr.pivot = new Vector2(0f, 1f);
            pr.anchoredPosition = new Vector2(34f, -86f);
            pr.sizeDelta = new Vector2(150f, 150f);
            pic.GetComponent<Image>().sprite = _art.portrait;
            var story = Label(panel, Localization.Get(_c.storyKey), UiTheme.Regular, 21, new Vector2(0f, 0f), new Vector2(1f, 1f), UiTheme.Ink, TextAnchor.UpperLeft);
            var sr = (RectTransform)story.transform;
            sr.offsetMin = new Vector2(204f, 96f);
            sr.offsetMax = new Vector2(-34f, -86f);
            story.horizontalOverflow = HorizontalWrapMode.Wrap;
            story.verticalOverflow = VerticalWrapMode.Truncate;
            story.resizeTextForBestFit = true;
            story.resizeTextMinSize = 14;
            story.resizeTextMaxSize = 21;
            WideButton(panel, Localization.Get(_c.closeKey), 0.11f, UiTheme.Good, Close);
        }

        private RectTransform Modal(Vector2 size, string title)
        {
            Time.timeScale = 0f;
            _modal = new GameObject("Modal", typeof(RectTransform), typeof(Image));
            _modal.transform.SetParent(_canvas.transform, false);
            Fill((RectTransform)_modal.transform, 0f);
            _modal.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            var panel = Box(_modal.transform, "Panel", _art.panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            panel.pivot = new Vector2(0.5f, 0.5f);
            var t = Label(panel, title, UiTheme.Heavy, 34, new Vector2(0f, 1f), new Vector2(1f, 1f), UiTheme.Ink, TextAnchor.MiddleCenter);
            var tr = (RectTransform)t.transform;
            tr.offsetMin = new Vector2(20f, -78f);
            tr.offsetMax = new Vector2(-20f, -18f);
            return panel;
        }

        private void Close()
        {
            if (_modal != null) UnityEngine.Object.Destroy(_modal);
            _modal = null;
            Time.timeScale = 1f;
        }

        // ------------------------------------------------------------ helpers

        private static RectTransform SafeArea(Transform parent)
        {
            var go = new GameObject("Safe", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            var s = Screen.safeArea;
            var w = Mathf.Max(1f, Screen.width);
            var h = Mathf.Max(1f, Screen.height);
            r.anchorMin = new Vector2(s.xMin / w, s.yMin / h);
            r.anchorMax = new Vector2(s.xMax / w, s.yMax / h);
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }

        private Button RoundButton(Transform parent, string name, Vector2 anchor, Vector2 at, float size, Sprite glyph)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = anchor;
            r.anchoredPosition = at;
            r.sizeDelta = new Vector2(size, size);
            var disc = go.GetComponent<Image>();
            disc.sprite = _art.disc;
            disc.color = UiTheme.Wood;
            var rim = new GameObject("Rim", typeof(RectTransform), typeof(Image));
            rim.transform.SetParent(go.transform, false);
            Fill((RectTransform)rim.transform, 0f);
            rim.GetComponent<Image>().sprite = _art.rim;
            rim.GetComponent<Image>().raycastTarget = false;
            if (glyph != null)
            {
                var g = new GameObject("Glyph", typeof(RectTransform), typeof(Image));
                g.transform.SetParent(go.transform, false);
                Fill((RectTransform)g.transform, 0.24f);
                g.GetComponent<Image>().sprite = glyph;
                g.GetComponent<Image>().color = UiTheme.OnWood;
                g.GetComponent<Image>().raycastTarget = false;
            }
            return go.GetComponent<Button>();
        }

        private static void Fill(RectTransform r, float inset)
        {
            r.anchorMin = new Vector2(inset, inset);
            r.anchorMax = new Vector2(1f - inset, 1f - inset);
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        private static RectTransform Box(Transform parent, string name, Sprite sprite, Vector2 min, Vector2 max, Vector2 at, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = min;
            r.anchorMax = max;
            r.pivot = new Vector2(max.x >= 1f ? 1f : max.x <= 0f ? 0f : 0.5f, max.y >= 1f ? 1f : max.y <= 0f ? 0f : 0.5f);
            r.anchoredPosition = at;
            r.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            if (sprite != null && sprite.border.sqrMagnitude > 0f) img.type = Image.Type.Sliced;
            return r;
        }

        private static Text Label(Transform parent, string text, Font font, int size, Vector2 min, Vector2 max, Color colour, TextAnchor align)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = min;
            r.anchorMax = max;
            r.offsetMin = new Vector2(10f, 0f);
            r.offsetMax = new Vector2(-10f, 0f);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = align;
            t.color = colour;
            t.text = text;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            return t;
        }

        private void WideButton(RectTransform panel, string text, float y, Color colour, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(panel, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, y);
            r.sizeDelta = new Vector2(300f, 62f);
            var img = go.GetComponent<Image>();
            img.sprite = _art.pillWood;
            if (img.sprite != null && img.sprite.border.sqrMagnitude > 0f) img.type = Image.Type.Sliced;
            img.color = colour == UiTheme.Wood ? Color.white : colour;
            go.GetComponent<Button>().onClick.AddListener(onClick);
            Label(r, text, UiTheme.Bold, 26, Vector2.zero, Vector2.one, UiTheme.OnWood, TextAnchor.MiddleCenter);
        }
    }
}
