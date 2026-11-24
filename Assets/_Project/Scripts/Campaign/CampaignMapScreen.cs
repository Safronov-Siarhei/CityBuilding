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
    /// The campaign's level picker (the user, 2026-11-12): banner -> loading screen -> THIS screen -> pick a level ->
    /// «Играть» -> the level. Since 2026-11-24 a LIST (the user rejected the scroll as off the game's style and picked
    /// list variant A): on the left a panel of rows -- the level's preview, its number on a medal, name and one line
    /// (best time / goal / why it is shut), a tick once done, «Новый!» while open and not yet done -- and on the right
    /// the picked level big: its preview, «Уровень N. Name», a short story, the record and «Играть». Levels and texts
    /// in Campaign/CampaignMap.json + the localization sheet, previews baked by Tools/campaign/campaign_list_art.py.
    /// A level opens once the one before it is done; a level without a scene yet says «Скоро». Drawn in the main
    /// menu's look (MenuSkins) over the blurred menu village.
    /// </summary>
    public class CampaignMapScreen : MonoBehaviour
    {
        [Serializable]
        private class Level
        {
            public string id;
            public string titleKey;
            public string goalKey;   // the row's one line under the name
            public string descKey;   // the picked card's short story
            public string scene;
            public string bestKey;
            public string after;   // opens once this level is done (the level-3 branches both follow m2); empty: the one before it
            public string label;   // the medal's number (the branches share «3»); empty: its place in the list
            public bool film;      // a story scene, not a level (the level-3 prologue): a play glyph on its medal
            public bool closed;    // shut until release whatever the progress: a lock in place of «Играть» (the user, 2026-11-24)
        }

        [Serializable]
        private class MapFile
        {
            public string chapterKey;
            public Level[] levels = new Level[0];
        }

        [SerializeField] private TextAsset mapFile;
        // Preview_<level id> (Tools/campaign/campaign_list_art.py) and the blurred menu village behind the list
        [SerializeField] private Sprite[] previews = new Sprite[0];
        [SerializeField] private Sprite backdrop;
        [SerializeField] private Sprite backGlyph, playGlyph, replayGlyph, lockGlyph, settingsGlyph, checkGlyph;
        // the game's own settings card, built into the scene by setup (the user: a settings button top right)
        [SerializeField] private SettingsPanelController settings;
        [SerializeField] private string menuScene = "MainMenu";

        private Level[] _levels = new Level[0];
        private string _chapterKey;
        private readonly List<Image> _rows = new List<Image>();
        private int _picked = -1;
        private CanvasGroup _ui;
        private Image _bigPreview, _bigLock;
        private Text _bigName, _bigDesc, _record, _line;
        private Button _play;
        private GameObject _locked;
        private float _shown;

        // the list's layout on the 720-tall canvas; Side keeps both blocks clear of the back button, the settings and
        // the piggy bank at the screen's edges, the same on the left and the right (the user, 2026-11-24)
        private const float Top = 112f, Bottom = 22f, Side = 130f, Gap = 22f, RowH = 86f, RowGap = 8f, HeaderH = 52f;
        private const float Slice = 1.5f;   // the menu sprites are drawn for 1080 lines: their corners at 720
        private static readonly Color Brown = new Color(0.38f, 0.28f, 0.17f);
        private static readonly Color Shut = new Color(0.45f, 0.45f, 0.45f);

        private void Start()
        {
            if (mapFile != null)
            {
                var file = JsonUtility.FromJson<MapFile>(mapFile.text);
                _levels = file.levels ?? new Level[0];
                _chapterKey = file.chapterKey;
            }
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            Build();
            // the newest open level picked from the start (not one shut until release): the card is never empty
            var at = 0;
            for (var i = 0; i < _levels.Length; i++)
                if (Open(i) && !_levels[i].closed) at = i;
            if (_levels.Length > 0) Pick(at);
            Debug.Log($"[CampaignMap] up, {_levels.Length} levels");
        }

        private bool Open(int i)
        {
            var after = _levels[i].after;
            if (!string.IsNullOrEmpty(after)) return CampaignProgress.Done(after);
            return i == 0 || CampaignProgress.Done(_levels[i - 1].id);
        }
        private bool Playable(int i) => Open(i) && !_levels[i].closed && !string.IsNullOrEmpty(_levels[i].scene);
        private string Number(int i) => string.IsNullOrEmpty(_levels[i].label) ? (i + 1).ToString() : _levels[i].label;

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

            var back = Pic(root, "Backdrop", backdrop);
            Fill(back.rectTransform);
            back.color = backdrop != null ? Color.white : new Color(0.16f, 0.11f, 0.07f);

            // the safe area: everything keeps clear of a camera cut-out
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

            var title = Menu(safe, "Title", "title");
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(380f, 72f));
            MissionSkin.Label(title.transform, Localization.Get("#campaign_list_title"), UiTheme.Bold, 30, UiTheme.Ink, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(20f, 6f));

            // the safe area's width on the canvas: the panel takes up to 45% of it, the card the rest
            var safeW = Screen.height > 0 ? area.width / Screen.height * 720f : 1280f;
            var panelW = Mathf.Min(520f, (safeW - 2f * Side - Gap) * 0.45f);
            BuildList(safe, panelW);
            BuildCard(safe, Side + panelW + Gap);
        }

        private void BuildList(RectTransform safe, float width)
        {
            var panel = Menu(safe, "List", "panel");
            var r = panel.rectTransform;
            r.anchorMin = new Vector2(0f, 0f);
            r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 0.5f);
            r.offsetMin = new Vector2(Side, Bottom);
            r.offsetMax = new Vector2(Side + width, -Top);
            var chapter = MissionSkin.Label(panel.transform, Localization.Get(_chapterKey), UiTheme.Bold, 24, Brown, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), Vector2.one, Vector2.zero);
            chapter.rectTransform.offsetMin = new Vector2(20f, -HeaderH - 8f);
            chapter.rectTransform.offsetMax = new Vector2(-20f, -8f);

            // the rows in a vertical scroll (more chapters, more rows)
            var view = new GameObject("Rows", typeof(RectTransform), typeof(RectMask2D), typeof(Image)).GetComponent<RectTransform>();
            view.SetParent(panel.transform, false);
            view.anchorMin = Vector2.zero;
            view.anchorMax = Vector2.one;
            view.offsetMin = new Vector2(18f, 18f);
            view.offsetMax = new Vector2(-18f, -HeaderH - 10f);
            view.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);   // catches the drag between the rows
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(view, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, _levels.Length * (RowH + RowGap));
            for (var i = 0; i < _levels.Length; i++) Row(content, i);
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.decelerationRate = 0.12f;
        }

        private void Row(RectTransform list, int i)
        {
            var lv = _levels[i];
            var open = Open(i);
            var done = CampaignProgress.Done(lv.id);
            var row = Menu(list, $"Row_{lv.id}", "row");
            row.raycastTarget = true;
            var r = row.rectTransform;
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(0f, -i * (RowH + RowGap));
            r.sizeDelta = new Vector2(0f, RowH);
            if (!open) row.color = UiTheme.RowLocked;
            var index = i;
            var button = row.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => Pick(index));
            _rows.Add(row);

            // the preview, the medal on its corner
            const float thumbH = RowH - 16f, thumbW = thumbH * 588f / 340f;
            var thumb = Pic(row.transform, "Preview", Preview(lv.id));
            Anchor(thumb.rectTransform, new Vector2(0f, 0.5f), new Vector2(10f + thumbW * 0.5f, 0f), new Vector2(thumbW, thumbH));
            if (!open) thumb.color = Shut;
            Medal(row.transform, i, new Vector2(0f, 1f), new Vector2(26f, -24f), 34f, 18);

            // the name and its line
            var textX = 10f + thumbW + 14f;
            var name = MissionSkin.Label(row.transform, Localization.Get(lv.titleKey), UiTheme.Bold, 25, open ? UiTheme.Ink : Shut, TextAnchor.LowerLeft,
                new Vector2(0f, 0.5f), new Vector2(1f, 1f), Vector2.zero);
            name.rectTransform.offsetMin = new Vector2(textX, 0f);
            name.rectTransform.offsetMax = new Vector2(-96f, -8f);
            var line = MissionSkin.Label(row.transform, RowLine(i), UiTheme.Bold, 15, open ? Brown : Shut, TextAnchor.UpperLeft,
                new Vector2(0f, 0f), new Vector2(1f, 0.5f), Vector2.zero);
            line.rectTransform.offsetMin = new Vector2(textX, 6f);
            line.rectTransform.offsetMax = new Vector2(-20f, -4f);
            line.resizeTextForBestFit = true;
            line.resizeTextMinSize = 11;
            line.resizeTextMaxSize = 15;

            if (done && checkGlyph != null)
            {
                var tick = Pic(row.transform, "Done", checkGlyph);
                tick.preserveAspect = true;
                tick.color = UiTheme.Good;
                Anchor(tick.rectTransform, new Vector2(1f, 0.5f), new Vector2(-34f, 0f), new Vector2(34f, 34f));
            }
            else if (!done && Playable(i))
            {
                // «Новый!»: open and not played through yet
                var pill = Menu(row.transform, "New", "row");
                pill.pixelsPerUnitMultiplier = 3f;
                pill.color = UiTheme.Danger;
                Anchor(pill.rectTransform, new Vector2(1f, 0.5f), new Vector2(-54f, 10f), new Vector2(84f, 30f));
                MissionSkin.Label(pill.transform, Localization.Get("#campaign_new"), UiTheme.Bold, 15, UiTheme.OnWood, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, Vector2.zero);
            }
        }

        /// <summary>A row's line: the record once there is one, the goal while open, why it is shut.</summary>
        private string RowLine(int i)
        {
            var lv = _levels[i];
            if (!Open(i)) return Localization.Get("#campaign_locked_short");
            if (lv.closed || string.IsNullOrEmpty(lv.scene)) return Localization.Get("#campaign_level_soon");
            var best = !string.IsNullOrEmpty(lv.bestKey) ? PlayerPrefs.GetFloat(lv.bestKey, 0f) : 0f;
            if (best > 0f) return Localization.Format("#m1_best", MissionSkin.Clock(best));
            return Localization.Get(lv.goalKey);
        }

        private void BuildCard(RectTransform safe, float left)
        {
            var card = Menu(safe, "Picked", "card");
            var r = card.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(left, Bottom);
            r.offsetMax = new Vector2(-Side, -Top);

            // the preview across the top, its aspect kept, the name plaque over its lower edge
            var holder = new GameObject("PreviewBox", typeof(RectTransform)).GetComponent<RectTransform>();
            holder.SetParent(card.transform, false);
            holder.anchorMin = new Vector2(0f, 0.42f);
            holder.anchorMax = Vector2.one;
            holder.offsetMin = new Vector2(20f, 20f);
            holder.offsetMax = new Vector2(-20f, -20f);
            _bigPreview = Pic(holder, "Preview", null);
            _bigPreview.preserveAspect = true;
            Fill(_bigPreview.rectTransform);
            _bigLock = Pic(holder, "Lock", lockGlyph);
            _bigLock.preserveAspect = true;
            _bigLock.color = new Color(0.27f, 0.26f, 0.24f);
            Anchor(_bigLock.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 90f));

            var plaque = Menu(card.transform, "Name", "plaque");
            Anchor(plaque.rectTransform, new Vector2(0.5f, 0.42f), new Vector2(0f, 8f), new Vector2(360f, 54f));
            _bigName = MissionSkin.Label(plaque.transform, "", UiTheme.Bold, 24, UiTheme.Ink, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(22f, 4f));
            _bigName.resizeTextForBestFit = true;
            _bigName.resizeTextMinSize = 14;
            _bigName.resizeTextMaxSize = 24;

            _bigDesc = MissionSkin.Label(card.transform, "", UiTheme.Bold, 19, Brown, TextAnchor.UpperCenter,
                new Vector2(0f, 0.42f), new Vector2(1f, 0.42f), Vector2.zero);
            _bigDesc.rectTransform.offsetMin = new Vector2(40f, -110f);
            _bigDesc.rectTransform.offsetMax = new Vector2(-40f, -32f);
            _bigDesc.resizeTextForBestFit = true;
            _bigDesc.resizeTextMinSize = 13;
            _bigDesc.resizeTextMaxSize = 19;
            _record = MissionSkin.Label(card.transform, "", UiTheme.Bold, 16, UiTheme.InkSoft, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero);
            _record.rectTransform.offsetMin = new Vector2(30f, 112f);
            _record.rectTransform.offsetMax = new Vector2(-30f, 138f);

            // «Играть» as the mode screen has it (the user: «возьми из меню выбора режима кнопку Играть»), or why not
            var playImg = Menu(card.transform, "Play", "button");
            playImg.raycastTarget = true;
            Anchor(playImg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(300f, 92f));
            _play = playImg.gameObject.AddComponent<Button>();
            _play.onClick.AddListener(Enter);
            var playText = MissionSkin.Label(playImg.transform, Localization.Get("#newgame_play"), UiTheme.Bold, 40, MissionSkin.Ink(HudInk.OnButton),
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(16f, 8f));
            playText.rectTransform.anchoredPosition = new Vector2(0f, 4f);
            // a level shut until release: the same button, greyed, a lock on it (the user, 2026-11-24)
            var lockImg = Menu(card.transform, "Locked", "button");
            lockImg.color = UiTheme.RowLocked;
            Anchor(lockImg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(300f, 92f));
            var lockIcon = Pic(lockImg.transform, "Lock", lockGlyph);
            lockIcon.preserveAspect = true;
            lockIcon.color = new Color(0.27f, 0.26f, 0.24f);
            Anchor(lockIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(54f, 54f));
            _locked = lockImg.gameObject;
            _line = MissionSkin.Label(card.transform, "", UiTheme.Bold, 20, UiTheme.InkSoft, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero);
            _line.rectTransform.offsetMin = new Vector2(30f, 30f);
            _line.rectTransform.offsetMax = new Vector2(-30f, 90f);
        }

        /// <summary>A medal with the level's number (a play glyph on a story scene, a lock while shut).</summary>
        private void Medal(Transform parent, int i, Vector2 anchor, Vector2 at, float size, int font)
        {
            var lv = _levels[i];
            var open = Open(i);
            var glyph = !open || lv.closed ? lockGlyph : lv.film ? playGlyph : null;
            var medal = MissionSkin.Round(parent, "Medal", open ? "round_main" : "round", open ? HudInk.OnMain : HudInk.OnRound, glyph, anchor, at, size);
            medal.GetComponent<Image>().raycastTarget = false;
            medal.interactable = false;
            if (glyph == null)
            {
                var n = MissionSkin.Label(medal.transform, Number(i), UiTheme.Heavy, font, MissionSkin.Ink(HudInk.OnMain), TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, Vector2.zero);
                n.rectTransform.anchoredPosition = new Vector2(0f, 1f);
            }
        }

        private Sprite Preview(string id)
        {
            var name = $"Preview_{id}";
            foreach (var s in previews)
                if (s != null && s.name == name) return s;
            return null;
        }

        private void Pick(int i)
        {
            Debug.Log($"[CampaignMap] picked {_levels[i].id}");
            _picked = i;
            _shown = 0f;
            var lv = _levels[i];
            var open = Open(i);
            var playable = Playable(i);
            for (var k = 0; k < _rows.Count; k++) _rows[k].sprite = MenuSprite(k == i ? "row_selected" : "row");
            _bigPreview.sprite = Preview(lv.id);
            _bigPreview.color = open ? Color.white : Shut;
            _bigLock.gameObject.SetActive(!open);
            _bigName.text = lv.film ? Localization.Get(lv.titleKey) : Localization.Format("#campaign_level_n", Number(i), Localization.Get(lv.titleKey));
            _bigDesc.text = Localization.Get(lv.descKey);
            var best = playable && !string.IsNullOrEmpty(lv.bestKey) ? PlayerPrefs.GetFloat(lv.bestKey, 0f) : 0f;
            _record.text = best > 0f ? Localization.Format("#m1_best", MissionSkin.Clock(best)) : "";
            // a shut level says why in place of «Играть» (the user, 2026-11-12)
            _line.text = playable || lv.closed ? "" : Localization.Get(string.IsNullOrEmpty(lv.scene) ? "#campaign_level_soon" : "#campaign_locked");
            if (lv.closed) _record.text = Localization.Get("#campaign_level_soon");
            _play.gameObject.SetActive(playable);
            _locked.SetActive(lv.closed);
        }

#if !CB_RELEASE
        // Cheat (the user, 2026-11-12): skip levels. Marks the picked level done (the next one opens) and
        // redraws the screen; «все» marks every level. Left out of a release build like the cheat window.
        private void OnGUI()
        {
            var s = Screen.height / 720f;
            var w = 230f * s;
            var h = 40f * s;
            var area = Screen.safeArea;
            var x = area.xMax - w - 10f * s;
            var y = Screen.height - area.yMax + 10f * s + 230f * s;   // under the piggy bank
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
            // the settings card hides the screen's buttons behind it (the user, 2026-11-12)
            var hidden = settings != null && settings.gameObject.activeInHierarchy;
            if (_ui != null)
            {
                _ui.alpha = hidden ? 0f : 1f;
                _ui.blocksRaycasts = !hidden;
            }
            // «Играть» breathes
            _shown += Time.unscaledDeltaTime;
            if (_play != null && _play.gameObject.activeSelf)
                _play.transform.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(_shown * 4f));
        }

        /// <summary>A sprite of the menu's look (Resources/MenuSkins/&lt;look&gt;/&lt;role&gt;), as the mode screen draws.</summary>
        private static Sprite MenuSprite(string role)
        {
            var s = UnityEngine.Resources.Load<Sprite>($"MenuSkins/{HudSkins.Current}/{role}");
            return s != null ? s : UnityEngine.Resources.Load<Sprite>($"MenuSkins/{HudSkin.Classic}/{role}");
        }

        /// <summary>An image in the menu's look, sliced, its corners sized for the 720-tall canvas.</summary>
        private static Image Menu(Transform parent, string name, string role)
        {
            var img = Pic(parent, name, MenuSprite(role));
            if (img.sprite != null && img.sprite.border.sqrMagnitude > 0f)
            {
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = Slice;
            }
            if (img.sprite == null) img.color = UiTheme.Parchment;
            return img;
        }

        private static Image Pic(Transform parent, string name, Sprite sprite)
        {
            var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(parent, false);
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        private static RectTransform Stretch(Transform parent, string name)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false);
            Fill(r);
            return r;
        }

        private static void Anchor(RectTransform r, Vector2 anchor, Vector2 at, Vector2 size)
        {
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = at;
            r.sizeDelta = size;
        }

        private static void Fill(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }
    }
}
