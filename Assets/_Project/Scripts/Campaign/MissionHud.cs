using System;
using CityBuilder.Core;
using CityBuilder.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 1's own HUD (the user's playtests, 2026-11-11), drawn in the SANDBOX HUD's kit
    /// (MissionSkin: the same skin sprites and inks, the same 108-in-1920 round buttons -- the second
    /// playtest found the first, home-made look out of keeping with the game):
    ///   - top left the MENU button (round_main): as in the sandbox, the screen dims and round buttons
    ///     drop under it -- go on, start over, to the menu; the same spot closes it. Time stands;
    ///   - top right the CURRENT OBJECTIVE on a plaque, changing as he gets on;
    ///   - top centre ARDAN's portrait in the King's frame (round_value, parchment circle, no health,
    ///     no level), «Ардан» on a plaque under it; a tap pauses and opens his short story;
    ///   - bottom centre the RUN'S CLOCK (the second playtest: records, and how long each stretch
    ///     took): at every checkpoint the stretch's time shows over it for a while, against the best.
    /// Pausing is Time.timeScale = 0: the film's world and the mission step by deltaTime and stand.
    /// </summary>
    public class MissionHud
    {
        [Serializable] public class Config
        {
            public float button, portrait, margin, timerWidth, splitSeconds;
            public string nameKey, storyKey, objectiveKey, pauseKey, resumeKey, restartKey, menuKey, closeKey, menuScene, splitKey;
            public Objective[] objectives;
        }

        [Serializable] public class Objective { public string key; public float untilZ; }

        [Serializable] public struct Art
        {
            public Sprite disc, rim, mask, panel, pillParchment, pillWood, menuGlyph, closeGlyph, portrait, playGlyph, restartGlyph, homeGlyph;
        }

        private readonly Config _c;
        private readonly Art _art;
        private readonly Canvas _canvas;
        private readonly RectTransform _safe;
        private readonly Text _objective, _clock, _split;
        private GameObject _modal;
        private int _stage = -1;
        private float _splitT = -1f;

        /// <summary>The objective's text now, for the batchmode check.</summary>
        public string ObjectiveText => _objective != null ? _objective.text : null;
        /// <summary>The clock's and the last split's text, for the batchmode check.</summary>
        public string ClockText => _clock != null ? _clock.text : null;
        public string SplitText { get; private set; }

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
            _safe = SafeArea(go.transform);
            var b = c.button;

            // the menu, top left
            MissionSkin.Round(_safe, "Menu", "round_main", HudInk.OnMain, art.menuGlyph, new Vector2(0f, 1f),
                new Vector2(c.margin + b * 0.5f, -c.margin - b * 0.5f), b).onClick.AddListener(OpenPause);

            // the objective, top right, on a plaque
            var plaque = MissionSkin.Image(_safe, "Objective", "plaque", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-c.margin, -c.margin), new Vector2(380f, 70f));
            MissionSkin.Label(plaque.transform, Localization.Get(c.objectiveKey), UiTheme.Bold, 17, UiTheme.InkSoft, TextAnchor.MiddleCenter, new Vector2(0f, 0.56f), new Vector2(1f, 0.95f), new Vector2(14f, 0f));
            _objective = MissionSkin.Label(plaque.transform, "", UiTheme.Bold, 22, UiTheme.Ink, TextAnchor.MiddleCenter, new Vector2(0f, 0.08f), new Vector2(1f, 0.62f), new Vector2(14f, 0f));

            // Ардан, top centre, in the King's frame
            var p = c.portrait;
            var frame = MissionSkin.Image(_safe, "Ardan", "round_value", new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -c.margin - p * 0.5f), new Vector2(p, p));
            frame.type = Image.Type.Simple;
            var face = frame.gameObject.AddComponent<Button>();
            var mask = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
            mask.transform.SetParent(frame.transform, false);
            var mr = (RectTransform)mask.transform;
            mr.anchorMin = mr.anchorMax = new Vector2(0.5f, 0.5f);
            mr.sizeDelta = new Vector2(p * (1f - 16f / 108f), p * (1f - 16f / 108f));
            var mi = mask.GetComponent<Image>();
            mi.sprite = art.mask;
            mi.color = UiTheme.Parchment;
            mi.raycastTarget = false;
            mask.GetComponent<Mask>().showMaskGraphic = true;
            var pic = new GameObject("Face", typeof(RectTransform), typeof(Image));
            pic.transform.SetParent(mask.transform, false);
            Fill((RectTransform)pic.transform);
            pic.GetComponent<Image>().sprite = art.portrait;
            pic.GetComponent<Image>().preserveAspect = true;
            pic.GetComponent<Image>().raycastTarget = false;
            face.onClick.AddListener(OpenStory);
            var tag = MissionSkin.Image(_safe, "Name", "plaque", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -c.margin - p - 2f), new Vector2(150f, 40f));
            MissionSkin.Label(tag.transform, Localization.Get(c.nameKey), UiTheme.Bold, 22, UiTheme.Ink, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero);

            // the run's clock, bottom centre, and the stretch's time over it at a checkpoint
            var clock = MissionSkin.Image(_safe, "Clock", "plaque", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, c.margin), new Vector2(c.timerWidth, 52f));
            _clock = MissionSkin.Label(clock.transform, MissionSkin.Clock(0f), UiTheme.Heavy, 28, UiTheme.Ink, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero);
            _split = MissionSkin.Label(_safe, "", UiTheme.Bold, 22, Color.clear, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero);
            var sr = (RectTransform)_split.transform;
            sr.sizeDelta = new Vector2(600f, 34f);
            sr.anchoredPosition = new Vector2(0f, c.margin + 52f + 22f);
            _split.gameObject.AddComponent<Outline>().effectColor = new Color(0.1f, 0.06f, 0.03f, 0.9f);
        }

        /// <summary>The objective for where he is now (by how far south he has come), and the clock.</summary>
        public void Step(Vector3 ardan, float elapsed, float dt)
        {
            if (_clock != null) _clock.text = MissionSkin.Clock(elapsed);
            if (_splitT >= 0f)
            {
                _splitT += dt;
                var a = Mathf.Clamp01(Mathf.Min(_splitT / 0.25f, (_c.splitSeconds - _splitT) / 0.6f));
                var col = _split.color;
                col.a = a;
                _split.color = col;
                if (_splitT > _c.splitSeconds) _splitT = -1f;
            }
            if (_c.objectives == null || _objective == null) return;
            var stage = _c.objectives.Length - 1;
            for (var i = 0; i < _c.objectives.Length; i++)
                if (ardan.z > _c.objectives[i].untilZ) { stage = i; break; }
            if (stage == _stage) return;
            _stage = stage;
            _objective.text = Localization.Get(_c.objectives[stage].key);
        }

        /// <summary>A stretch run: «Участок N: m:ss.t», and against the best one (green ahead, red behind).</summary>
        public void Split(int stretch, float seconds, float best)
        {
            var text = string.Format(Localization.Get(_c.splitKey), stretch, MissionSkin.Clock(seconds));
            var col = UiTheme.Parchment;
            if (best > 0f)
            {
                var d = seconds - best;
                text += $"  ({(d < 0f ? "-" : "+")}{MissionSkin.Clock(Mathf.Abs(d))})";
                col = d < 0f ? new Color(0.55f, 0.95f, 0.45f) : new Color(1f, 0.55f, 0.45f);
            }
            SplitText = text;
            _split.text = text;
            col.a = 0f;
            _split.color = col;
            _splitT = 0f;
        }

        /// <summary>Hidden for the end (the summary takes the screen).</summary>
        public void Hide() => _canvas.gameObject.SetActive(false);

        // ------------------------------------------------------------ the windows

        /// <summary>The sandbox's game menu: the screen dims, the menu button stays on top to close
        /// it, round buttons drop under it -- go on, start over, to the menu.</summary>
        private void OpenPause()
        {
            if (_modal != null) return;
            Dim();
            var c = _c;
            var b = c.button;
            MissionSkin.Round(_modal.transform, "Close", "round_main", HudInk.OnMain, _art.menuGlyph, new Vector2(0f, 1f),
                new Vector2(c.margin + b * 0.5f, -c.margin - b * 0.5f), b).onClick.AddListener(Close);
            var entries = new (Sprite glyph, UnityEngine.Events.UnityAction act)[]
            {
                (_art.playGlyph, Close),
                (_art.restartGlyph, () =>
                {
                    Close();
                    MissionOne.SkipFilm = true;
                    LoadingScreen.IntoGame(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                }),
                (_art.homeGlyph, () =>
                {
                    Close();
                    LoadingScreen.OutToMenu(c.menuScene);
                }),
            };
            var step = b * 0.9f + 12f;
            for (var i = 0; i < entries.Length; i++)
                MissionSkin.Round(_modal.transform, "Entry", "round", HudInk.OnRound, entries[i].glyph, new Vector2(0f, 1f),
                    new Vector2(c.margin + b * 0.5f, -c.margin - b - 12f - step * 0.5f - i * step), b * 0.9f).onClick.AddListener(entries[i].act);
        }

        private void OpenStory()
        {
            if (_modal != null) return;
            Dim();
            var panel = MissionSkin.Image(_modal.transform, "Story", "panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 440f));
            var title = MissionSkin.Image(panel.transform, "Title", "plaque", new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 58f));
            MissionSkin.Label(title.transform, Localization.Get(_c.nameKey), UiTheme.Heavy, 30, UiTheme.Ink, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero);
            var frame = MissionSkin.Image(panel.transform, "Portrait", "round_value", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -60f), new Vector2(150f, 150f));
            frame.type = Image.Type.Simple;
            var mask = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
            mask.transform.SetParent(frame.transform, false);
            var mr = (RectTransform)mask.transform;
            mr.anchorMin = mr.anchorMax = new Vector2(0.5f, 0.5f);
            mr.sizeDelta = new Vector2(128f, 128f);
            mask.GetComponent<Image>().sprite = _art.mask;
            mask.GetComponent<Image>().color = UiTheme.Parchment;
            var pic = new GameObject("Face", typeof(RectTransform), typeof(Image));
            pic.transform.SetParent(mask.transform, false);
            Fill((RectTransform)pic.transform);
            pic.GetComponent<Image>().sprite = _art.portrait;
            pic.GetComponent<Image>().preserveAspect = true;
            var story = MissionSkin.Label(panel.transform, Localization.Get(_c.storyKey), UiTheme.Regular, 21, MissionSkin.Ink(HudInk.OnPanel), TextAnchor.UpperLeft, Vector2.zero, Vector2.one, Vector2.zero);
            var sr = (RectTransform)story.transform;
            sr.offsetMin = new Vector2(214f, 40f);
            sr.offsetMax = new Vector2(-40f, -60f);
            story.verticalOverflow = VerticalWrapMode.Truncate;
            story.resizeTextForBestFit = true;
            story.resizeTextMinSize = 14;
            story.resizeTextMaxSize = 21;
            MissionSkin.Round(panel.transform, "Close", "round_main", HudInk.OnMain, _art.closeGlyph, new Vector2(1f, 1f), new Vector2(-14f, -14f), 64f).onClick.AddListener(Close);
        }

        private void Dim()
        {
            Time.timeScale = 0f;
            _modal = new GameObject("Modal", typeof(RectTransform), typeof(Image), typeof(Button));
            _modal.transform.SetParent(_safe, false);
            var r = (RectTransform)_modal.transform;
            r.anchorMin = new Vector2(-1f, -1f);
            r.anchorMax = new Vector2(2f, 2f);
            r.offsetMin = r.offsetMax = Vector2.zero;
            _modal.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
            var bb = _modal.GetComponent<Button>();
            bb.transition = Selectable.Transition.None;
            bb.onClick.AddListener(Close);
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

        private static void Fill(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }
    }
}
