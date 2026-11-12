using System;
using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// The campaign map (the user, 2026-11-12): the way in is banner -> loading screen -> THIS map -> tap
    /// a level -> «Играть» lights up bottom centre -> the level. A painted parchment chart
    /// (Tools/campaign/campaign_map.py) filling the screen, a flag on each level point of
    /// Campaign/CampaignMap.json. A level opens once the one before it is done; a level without a
    /// scene yet says «Скоро». Level 1 starts with the opening film the first time; after that a
    /// replay button beside «Играть» shows the film again. Drawn in the sandbox HUD's kit (MissionSkin).
    /// </summary>
    public class CampaignMapScreen : MonoBehaviour
    {
        [Serializable]
        private class Level
        {
            public string id;
            public float x, y;
            public string titleKey;
            public string scene;
            public string bestKey;
        }

        [Serializable]
        private class MapFile
        {
            public Level[] levels = new Level[0];
        }

        [SerializeField] private TextAsset mapFile;
        [SerializeField] private Sprite picture;
        [SerializeField] private Sprite backGlyph, playGlyph, replayGlyph, lockGlyph, settingsGlyph;
        // the game's own settings card, built into the scene by setup (the user: a settings button top right)
        [SerializeField] private SettingsPanelController settings;
        [SerializeField] private string menuScene = "MainMenu";

        private Level[] _levels = new Level[0];
        private readonly List<RectTransform> _flags = new List<RectTransform>();
        private readonly List<Image> _rings = new List<Image>();
        private int _picked = -1;
        private CanvasGroup _bar, _ui, _flagsGroup;
        private Text _record, _line, _hint;
        private Button _play;
        private float _shown;

        private void Start()
        {
            if (mapFile != null) _levels = JsonUtility.FromJson<MapFile>(mapFile.text).levels ?? new Level[0];
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            Build();
            Debug.Log($"[CampaignMap] up, {_levels.Length} levels");
        }

        private bool Open(int i) => i == 0 || CampaignProgress.Done(_levels[i - 1].id);
        private bool Playable(int i) => Open(i) && !string.IsNullOrEmpty(_levels[i].scene);

        private void Build()
        {
            var go = new GameObject("CampaignMap", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // under everything else (settings 10, loading screen 1000). Set it EXPLICITLY: a canvas made in
            // code and left at its default 0 is never sorted -- it drew last, over the loading screen and
            // the settings card, which still took the taps unseen (the user's Editor log, 2026-11-12)
            canvas.sortingOrder = -10;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 1f;
            var root = go.transform;

            // the parchment fills the screen (cropped at the sides on a squarer screen); the flags ride on it
            var back = Stretch(root, "Backdrop");
            back.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.11f, 0.07f);
            // the fitter sizes a frame; the picture inside it can slide (a fitter zeroes its own position)
            var frame = new GameObject("Frame", typeof(RectTransform), typeof(AspectRatioFitter)).GetComponent<RectTransform>();
            frame.SetParent(root, false);
            var map = Stretch(frame, "Picture");
            var img = map.gameObject.AddComponent<Image>();
            img.sprite = picture;
            img.raycastTarget = false;
            var fit = frame.GetComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = picture != null ? picture.rect.width / picture.rect.height : 2400f / 1080f;
            Frame(map, fit);

            // the flags on their own layer, so the settings card can hide them with the buttons
            var flags = Stretch(map, "Flags");
            _flagsGroup = flags.gameObject.AddComponent<CanvasGroup>();
            for (var i = 0; i < _levels.Length; i++) Flag(flags, i);

            // the safe area: the buttons keep clear of a camera cut-out
            var safe = Stretch(root, "Safe");
            var area = Screen.safeArea;
            if (Screen.width > 0 && Screen.height > 0)
            {
                safe.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
                safe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            }
            _ui = safe.gameObject.AddComponent<CanvasGroup>();
            MissionSkin.Round(safe, "Back", "round_main", HudInk.OnMain, backGlyph, new Vector2(0f, 1f), new Vector2(66f, -66f), 92f)
                .onClick.AddListener(() =>
                {
                    // back to the choice of mode, where the campaign was picked (the user)
                    MainMenuController.OpenOnModes = true;
                    LoadingScreen.OutToMenu(menuScene);
                });
            if (settings != null)
                MissionSkin.Round(safe, "Settings", "round_main", HudInk.OnMain, settingsGlyph, new Vector2(1f, 1f), new Vector2(-66f, -66f), 92f)
                    .onClick.AddListener(settings.OpenPanel);

            // white words with a shadow over the chart (the user, 2026-11-12): the hint and «Скоро» at the
            // bottom centre, the picked level's record at the top centre
            _hint = Caption(safe, Localization.Get("#campaign_pick"), new Vector2(0.5f, 0f), new Vector2(0f, 70f));
            _record = Caption(safe, "", new Vector2(0.5f, 1f), new Vector2(0f, -60f));

            // the bar that lights up once a level is picked: «Играть», or why it cannot be played
            var bar = new GameObject("Bar", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
            bar.SetParent(safe, false);
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = new Vector2(0f, 18f);
            bar.sizeDelta = new Vector2(560f, 140f);
            _bar = bar.GetComponent<CanvasGroup>();
            _line = Caption(bar, "", new Vector2(0.5f, 0f), new Vector2(0f, 52f));

            // «Играть» as the mode screen has it (the user: «возьми из меню выбора режима кнопку Играть»):
            // the menu look's own button, its text in the button ink
            var playImg = new GameObject("Play", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            playImg.transform.SetParent(bar, false);
            var pr = playImg.rectTransform;
            pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0f);
            pr.pivot = new Vector2(0.5f, 0f);
            pr.sizeDelta = new Vector2(420f, 120f);
            playImg.sprite = MenuSprite("button");
            if (playImg.sprite != null && playImg.sprite.border.sqrMagnitude > 0f) playImg.type = Image.Type.Sliced;
            _play = playImg.gameObject.AddComponent<Button>();
            _play.onClick.AddListener(Enter);
            var playText = MissionSkin.Label(playImg.transform, Localization.Get("#newgame_play"), UiTheme.Bold, 48, MissionSkin.Ink(HudInk.OnButton),
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(20f, 10f));
            playText.rectTransform.anchoredPosition = new Vector2(0f, 4f);

            _bar.alpha = 0f;
            _bar.interactable = _bar.blocksRaycasts = false;
        }

        private static Text Caption(Transform parent, string text, Vector2 anchor, Vector2 at)
        {
            var t = MissionSkin.Label(parent, text, UiTheme.Bold, 34, Color.white, TextAnchor.MiddleCenter, anchor, anchor, Vector2.zero);
            Place(t.rectTransform, at, new Vector2(900f, 56f));
            t.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2.5f, -2.5f);
            return t;
        }

        private void Flag(RectTransform map, int i)
        {
            var lv = _levels[i];
            var anchor = new Vector2(lv.x, 1f - lv.y);
            var holder = new GameObject($"Level_{lv.id}", typeof(RectTransform)).GetComponent<RectTransform>();
            holder.SetParent(map, false);
            holder.anchorMin = holder.anchorMax = anchor;
            holder.sizeDelta = new Vector2(120f, 120f);

            // the ring that marks the picked level, behind the medal
            var ring = MissionSkin.Image(holder, "Ring", "round", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(112f, 112f));
            ring.type = Image.Type.Simple;
            ring.color = new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0f);
            ring.raycastTarget = false;
            _rings.Add(ring);

            var open = Open(i);
            var medal = MissionSkin.Round(holder, "Medal", open ? "round_main" : "round", open ? HudInk.OnMain : HudInk.OnRound,
                open ? null : lockGlyph, new Vector2(0.5f, 0.5f), Vector2.zero, 84f);
            var index = i;
            medal.onClick.AddListener(() => Pick(index));
            if (open)
            {
                var n = MissionSkin.Label(medal.transform, (i + 1).ToString(), UiTheme.Heavy, 40, MissionSkin.Ink(HudInk.OnMain), TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, Vector2.zero);
                n.rectTransform.anchoredPosition = new Vector2(0f, 2f);
            }
            if (CampaignProgress.Done(lv.id))
            {
                var star = MissionSkin.Image(holder, "Done", "round_value", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 30f), new Vector2(34f, 34f));
                star.type = Image.Type.Simple;
                star.color = UiTheme.Gold;
                star.raycastTarget = false;
            }

            var name = MissionSkin.Image(holder, "Name", "plaque", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(220f, 44f));
            name.raycastTarget = false;
            var t = MissionSkin.Label(name.transform, Localization.Get(lv.titleKey), UiTheme.Bold, 20, MissionSkin.Ink(HudInk.OnPlaque), TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(10f, 2f));
            t.resizeTextForBestFit = true;
            t.resizeTextMaxSize = 20;
            t.resizeTextMinSize = 12;
            _flags.Add(holder);
        }

        private void Pick(int i)
        {
            Debug.Log($"[CampaignMap] picked {_levels[i].id}");
            _picked = i;
            _shown = 0f;
            var lv = _levels[i];
            var playable = Playable(i);
            // the name is on the map already; a playable level shows its record top centre, a closed one
            // says why in place of «Играть» (the user, 2026-11-12)
            _line.text = playable ? "" : Localization.Get(string.IsNullOrEmpty(lv.scene) ? "#campaign_level_soon" : "#campaign_locked");
            var best = playable && !string.IsNullOrEmpty(lv.bestKey) ? PlayerPrefs.GetFloat(lv.bestKey, 0f) : 0f;
            _record.text = best > 0f ? Localization.Format("#m1_best", MissionSkin.Clock(best)) : "";
            _play.gameObject.SetActive(playable);
            _hint.gameObject.SetActive(false);
            _bar.interactable = _bar.blocksRaycasts = true;
        }

        private void Enter()
        {
            Debug.Log($"[CampaignMap] play {(_picked >= 0 ? _levels[_picked].id : "-")}");
            if (_picked < 0 || !Playable(_picked)) return;
            // level 1: the film the first time; after that straight into the mission (the film has its own «Пропустить»)
            if (_picked == 0) MissionOne.SkipFilm = CampaignProgress.FilmSeen;
            // level 2 is played on the gameplay scene, on its own map (CityDefenceMode)
            if (_levels[_picked].id == CityDefenceMode.LevelId)
            {
                CityDefenceMode.Begin();
                return;
            }
            LoadingScreen.IntoGame(_levels[_picked].scene);
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            // the settings card hides the map's buttons and flags behind it (the user, 2026-11-12)
            var hidden = settings != null && settings.gameObject.activeInHierarchy;
            if (_ui != null)
            {
                _ui.alpha = _flagsGroup.alpha = hidden ? 0f : 1f;
                _ui.blocksRaycasts = _flagsGroup.blocksRaycasts = !hidden;
            }
            if (_picked >= 0 && _bar != null)
            {
                // «Играть» lights up: the bar fades in and the button breathes
                _shown += dt;
                _bar.alpha = Mathf.Clamp01(_shown / 0.25f);
                if (_play.gameObject.activeSelf)
                    _play.transform.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(_shown * 4f));
            }
            for (var i = 0; i < _rings.Count; i++)
            {
                var on = i == _picked;
                var c = _rings[i].color;
                c.a = Mathf.MoveTowards(c.a, on ? 0.85f : 0f, dt * 4f);
                _rings[i].color = c;
                _flags[i].localScale = Vector3.one * (on ? 1.12f + 0.03f * Mathf.Sin(Time.unscaledTime * 3f) : 1f);
            }
        }

        /// <summary>A sprite of the menu's look (Resources/MenuSkins/&lt;look&gt;/&lt;role&gt;), as the mode screen draws.</summary>
        private static Sprite MenuSprite(string role)
        {
            var s = UnityEngine.Resources.Load<Sprite>($"MenuSkins/{HudSkins.Current}/{role}");
            return s != null ? s : UnityEngine.Resources.Load<Sprite>($"MenuSkins/{HudSkin.Classic}/{role}");
        }

        /// <summary>The chart fills the screen, slid sideways so every level stays on it; a screen too narrow
        /// for all of them (a tablet) gets the whole chart with dark margins.</summary>
        private void Frame(RectTransform map, AspectRatioFitter fit)
        {
            if (_levels.Length == 0 || Screen.height <= 0) return;
            const float margin = 0.06f;
            var min = 1f;
            var max = 0f;
            foreach (var lv in _levels)
            {
                min = Mathf.Min(min, lv.x - margin);
                max = Mathf.Max(max, lv.x + margin);
            }
            var seen = Screen.width / (float)Screen.height / fit.aspectRatio;
            if (seen >= 1f) return;
            if (seen < max - min)
            {
                fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                return;
            }
            var centre = Mathf.Clamp(0.5f, max - seen * 0.5f, min + seen * 0.5f);
            // the canvas matches height (720 tall), so the chart is 720 * aspect wide
            map.anchoredPosition = new Vector2((0.5f - centre) * 720f * fit.aspectRatio, 0f);
        }

        private static RectTransform Stretch(Transform parent, string name)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false);
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }

        private static void Place(RectTransform r, Vector2 at, Vector2 size)
        {
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = at;
            r.sizeDelta = size;
        }
    }
}
