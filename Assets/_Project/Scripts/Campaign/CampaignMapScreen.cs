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
    /// a level -> «Играть» lights up bottom centre -> the level. Since 2026-11-24 the map is a parchment
    /// SCROLL on a desk (the user approved scroll v2: «Подходит», «Да, встраивай»), swiped left/right between
    /// its two rolled ends; every level is a little scene drawn on it -- in watercolour + ink once DONE, in ink
    /// only while OPEN, blank paper while closed -- with a medal and a name plaque (Campaign/CampaignMap.json,
    /// art baked by Tools/campaign/campaign_scroll.py). A level opens once the one before it is done; a level
    /// without a scene yet says «Скоро». Drawn in the sandbox HUD's kit (MissionSkin).
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
            public string after;   // opens once this level is done (the level-3 branches both follow m2); empty: the one before it
            public string label;   // the medal's number (the branches share «3»); empty: its place in the list
            public bool film;      // a story scene, not a level (the level-3 prologue): a video glyph on its medal
            public float sx, sy, sw, sh;   // its scene on the paper (fractions from the paper's top left)
            public float nx, ny;           // its name plaque
        }

        [Serializable]
        private class MapFile
        {
            public Level[] levels = new Level[0];
            public float paperAspect = 3.6f;
        }

        [SerializeField] private TextAsset mapFile;
        // the scroll (Tools/campaign/campaign_scroll.py): the paper in tiles left to right, the rolled end, the
        // paper's shade by it, the desk; every level's scene as Scroll_<id>_Color (done) / Scroll_<id>_Ink (open)
        [SerializeField] private Sprite[] paperTiles = new Sprite[0];
        [SerializeField] private Sprite rod, curl, table;
        [SerializeField] private Sprite[] scenes = new Sprite[0];
        [SerializeField] private Sprite backGlyph, playGlyph, replayGlyph, lockGlyph, settingsGlyph;
        // the game's own settings card, built into the scene by setup (the user: a settings button top right)
        [SerializeField] private SettingsPanelController settings;
        [SerializeField] private string menuScene = "MainMenu";

        private Level[] _levels = new Level[0];
        private float _paperAspect = 3.6f;
        private ScrollRect _scroll;
        private float _paperW, _viewW;
        // the paper's band on the 720-tall canvas: under the top buttons, over the «Играть» bar
        private const float PaperTop = 100f, PaperBottom = 135f, RodRoom = 60f;
        private readonly List<RectTransform> _flags = new List<RectTransform>();
        private readonly List<Image> _rings = new List<Image>();
        private int _picked = -1;
        private CanvasGroup _bar, _ui, _flagsGroup;
        private Text _record, _line, _hint;
        private Button _play;
        private float _shown;

        private void Start()
        {
            if (mapFile != null)
            {
                var file = JsonUtility.FromJson<MapFile>(mapFile.text);
                _levels = file.levels ?? new Level[0];
                if (file.paperAspect > 0f) _paperAspect = file.paperAspect;
            }
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            Build();
            Debug.Log($"[CampaignMap] up, {_levels.Length} levels");
        }

        private bool Open(int i)
        {
            var after = _levels[i].after;
            if (!string.IsNullOrEmpty(after)) return CampaignProgress.Done(after);
            return i == 0 || CampaignProgress.Done(_levels[i - 1].id);
        }
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

            // the desk, the scroll's paper band across it, the rolled ends at the screen's sides
            var back = Stretch(root, "Desk");
            var desk = back.gameObject.AddComponent<Image>();
            desk.sprite = table;
            desk.color = table != null ? Color.white : new Color(0.16f, 0.11f, 0.07f);
            desk.raycastTarget = false;
            BuildScroll(root);

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
            // the piggy bank under the settings
            UI.PiggyButton.Create(safe, new Vector2(1f, 1f), new Vector2(9f, -120f), 150f);

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

        /// <summary>The paper band with the scenes, swiped sideways (a ScrollRect); centred, not scrolled,
        /// when the whole paper fits the screen.</summary>
        private void BuildScroll(Transform root)
        {
            var paperH = 720f - PaperTop - PaperBottom;
            var canvasW = Screen.height > 0 ? Screen.width / (float)Screen.height * 720f : 1280f;
            _paperW = paperH * _paperAspect;
            _viewW = canvasW - 2f * RodRoom;

            var view = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image)).GetComponent<RectTransform>();
            view.SetParent(root, false);
            view.anchorMin = Vector2.zero;
            view.anchorMax = Vector2.one;
            view.offsetMin = new Vector2(RodRoom, PaperBottom);
            view.offsetMax = new Vector2(-RodRoom, -PaperTop);
            view.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);   // catches the swipe between the scenes

            var content = new GameObject("Paper", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(view, false);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.sizeDelta = new Vector2(_paperW, 0f);
            var n = paperTiles.Length;
            for (var t = 0; t < n; t++)
            {
                var tile = new GameObject($"Tile_{t}", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                tile.SetParent(content, false);
                tile.anchorMin = new Vector2(t / (float)n, 0f);
                tile.anchorMax = new Vector2((t + 1) / (float)n, 1f);
                tile.offsetMin = tile.offsetMax = Vector2.zero;
                var img = tile.GetComponent<Image>();
                img.sprite = paperTiles[t];
                img.raycastTarget = false;
            }
            // the scenes: coloured once done, ink while open, nothing while closed; a tap on one picks it
            for (var i = 0; i < _levels.Length; i++) Scene(content, i);
            // the medals and plaques on their own layer, so the settings card can hide them with the buttons
            var flags = Stretch(content, "Flags");
            _flagsGroup = flags.gameObject.AddComponent<CanvasGroup>();
            for (var i = 0; i < _levels.Length; i++) Flag(flags, i, paperH);

            _scroll = view.gameObject.AddComponent<ScrollRect>();
            _scroll.content = content;
            _scroll.viewport = view;
            _scroll.vertical = false;
            _scroll.movementType = ScrollRect.MovementType.Elastic;
            _scroll.inertia = true;
            _scroll.decelerationRate = 0.12f;
            if (_paperW <= _viewW)
            {
                _scroll.horizontal = false;
                content.anchoredPosition = new Vector2((_viewW - _paperW) * 0.5f, 0f);
            }
            else
            {
                // open at the newest open level, the done ones to its left
                var at = 0;
                for (var i = 0; i < _levels.Length; i++)
                    if (Open(i)) at = i;
                var lv = _levels.Length > 0 ? _levels[at] : null;
                var centre = lv != null ? (lv.sx + lv.sw * 0.5f) * _paperW : 0f;
                content.anchoredPosition = new Vector2(-Mathf.Clamp(centre - _viewW * 0.5f, 0f, _paperW - _viewW), 0f);
            }

            // the rolled ends, with the paper's shade where it curls into them
            for (var side = 0; side < 2; side++)
            {
                var right = side == 1;
                if (curl != null)
                {
                    var c = new GameObject(right ? "CurlR" : "CurlL", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    c.SetParent(root, false);
                    c.anchorMin = new Vector2(right ? 1f : 0f, 0f);
                    c.anchorMax = new Vector2(right ? 1f : 0f, 1f);
                    c.pivot = new Vector2(0f, 0.5f);
                    c.offsetMin = new Vector2(0f, PaperBottom);
                    c.offsetMax = new Vector2(0f, -PaperTop);
                    c.anchoredPosition = new Vector2(right ? -RodRoom : RodRoom, c.anchoredPosition.y);
                    c.sizeDelta = new Vector2(40f, c.sizeDelta.y);
                    c.localScale = new Vector3(right ? -1f : 1f, 1f, 1f);
                    var ci = c.GetComponent<Image>();
                    ci.sprite = curl;
                    ci.raycastTarget = false;
                }
                var r = new GameObject(right ? "RodR" : "RodL", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                r.SetParent(root, false);
                r.anchorMin = new Vector2(right ? 1f : 0f, 0f);
                r.anchorMax = new Vector2(right ? 1f : 0f, 1f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.offsetMin = new Vector2(0f, PaperBottom - 18f);
                r.offsetMax = new Vector2(0f, -PaperTop + 18f);
                r.anchoredPosition = new Vector2(right ? -RodRoom + 6f : RodRoom - 6f, r.anchoredPosition.y);
                r.sizeDelta = new Vector2(46f, r.sizeDelta.y);
                var ri = r.GetComponent<Image>();
                ri.sprite = rod;
                ri.color = rod != null ? Color.white : new Color(0.85f, 0.75f, 0.56f);
                ri.raycastTarget = false;
            }
        }

        private Sprite SceneSprite(string id, string state)
        {
            var name = $"Scroll_{id}_{state}";
            foreach (var s in scenes)
                if (s != null && s.name == name) return s;
            return null;
        }

        private void Scene(RectTransform paper, int i)
        {
            var lv = _levels[i];
            if (lv.sw <= 0f) return;
            var sprite = CampaignProgress.Done(lv.id) ? SceneSprite(lv.id, "Color") : Open(i) ? SceneSprite(lv.id, "Ink") : null;
            if (sprite == null) return;      // closed: blank paper, its medal shows the lock
            var r = new GameObject($"Scene_{lv.id}", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<RectTransform>();
            r.SetParent(paper, false);
            r.anchorMin = new Vector2(lv.sx, 1f - lv.sy - lv.sh);
            r.anchorMax = new Vector2(lv.sx + lv.sw, 1f - lv.sy);
            r.offsetMin = r.offsetMax = Vector2.zero;
            var img = r.GetComponent<Image>();
            img.sprite = sprite;
            var b = r.GetComponent<Button>();
            b.transition = Selectable.Transition.None;
            var index = i;
            b.onClick.AddListener(() => Pick(index));
        }

        private static Text Caption(Transform parent, string text, Vector2 anchor, Vector2 at)
        {
            var t = MissionSkin.Label(parent, text, UiTheme.Bold, 34, Color.white, TextAnchor.MiddleCenter, anchor, anchor, Vector2.zero);
            Place(t.rectTransform, at, new Vector2(900f, 56f));
            t.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2.5f, -2.5f);
            return t;
        }

        private void Flag(RectTransform map, int i, float paperH)
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
                open ? (lv.film ? playGlyph : null) : lockGlyph, new Vector2(0.5f, 0.5f), Vector2.zero, 84f);
            var index = i;
            medal.onClick.AddListener(() => Pick(index));
            if (open && !lv.film)
            {
                var n = MissionSkin.Label(medal.transform, string.IsNullOrEmpty(lv.label) ? (i + 1).ToString() : lv.label, UiTheme.Heavy, 40, MissionSkin.Ink(HudInk.OnMain), TextAnchor.MiddleCenter,
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

            // the name under its scene (the medal sits at the scene's top left)
            var under = lv.sw > 0f ? new Vector2((lv.nx - lv.x) * _paperW, (lv.y - lv.ny) * paperH) : new Vector2(0f, -68f);
            var name = MissionSkin.Image(holder, "Name", "plaque", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), under, new Vector2(200f, 42f));
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

#if !CB_RELEASE
        // Cheat (the user, 2026-11-12): skip levels. Marks the picked level done (the next one opens) and
        // redraws the map; «все» marks every level. Left out of a release build like the cheat window.
        private void OnGUI()
        {
            var s = Screen.height / 720f;
            var w = 230f * s;
            var h = 40f * s;
            var area = Screen.safeArea;
            var x = area.xMax - w - 10f * s;
            var y = Screen.height - area.yMax + 10f * s;
            if (_picked >= 0 && GUI.Button(new Rect(x, y, w, h), "Чит: пройти уровень"))
            {
                CampaignProgress.MarkDone(_levels[_picked].id);
                Reopen();
            }
            if (GUI.Button(new Rect(x, y + h + 6f * s, w, h), "Чит: пройти все"))
            {
                foreach (var lv in _levels) CampaignProgress.MarkDone(lv.id);
                Reopen();
            }
        }

        private static void Reopen()
        {
            PlayerPrefs.Save();
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
#endif

        private void Enter()
        {
            Debug.Log($"[CampaignMap] play {(_picked >= 0 ? _levels[_picked].id : "-")}");
            if (_picked < 0 || !Playable(_picked)) return;
            // level 1: the film EVERY time it is started from the map (the user, 2026-11-18: «катсцена проигрывается
            // всегда при старте уровня»); it has its own «Пропустить», and only a restart from inside skips it
            if (_picked == 0) MissionOne.SkipFilm = false;
            // level 2 is played on the gameplay scene, on its own map (CityDefenceMode)
            // the level-3 prologue: mission 2's town and the scene alone, then back here (the user, 2026-11-23)
            if (_levels[_picked].film)
            {
                CityDefenceMode.BeginPrologue();
                return;
            }
            if (_levels[_picked].id == CityDefenceMode.LevelId)
            {
                CityDefenceMode.Begin();
                return;
            }
            // level 3b «Клетка»: the same mode, the escort on its own map (the user, 2026-11-23)
            if (_levels[_picked].id == CityDefenceMode.EscortLevelId)
            {
                CityDefenceMode.BeginEscort();
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
