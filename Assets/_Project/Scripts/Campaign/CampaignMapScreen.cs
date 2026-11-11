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
        [SerializeField] private Sprite backGlyph, playGlyph, replayGlyph, lockGlyph;
        [SerializeField] private string menuScene = "MainMenu";

        private Level[] _levels = new Level[0];
        private readonly List<RectTransform> _flags = new List<RectTransform>();
        private readonly List<Image> _rings = new List<Image>();
        private int _picked = -1;
        private CanvasGroup _bar;
        private Text _title, _line, _hint;
        private Button _play, _replay;
        private float _shown;

        private void Start()
        {
            if (mapFile != null) _levels = JsonUtility.FromJson<MapFile>(mapFile.text).levels ?? new Level[0];
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Build();
        }

        private bool Open(int i) => i == 0 || CampaignProgress.Done(_levels[i - 1].id);
        private bool Playable(int i) => Open(i) && !string.IsNullOrEmpty(_levels[i].scene);

        private void Build()
        {
            var go = new GameObject("CampaignMap", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 1f;
            var root = go.transform;

            // the parchment fills the screen (cropped at the sides on a squarer screen); the flags ride on it
            var back = Stretch(root, "Backdrop");
            back.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.11f, 0.07f);
            var map = new GameObject("Picture", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter)).GetComponent<RectTransform>();
            map.SetParent(root, false);
            var img = map.GetComponent<Image>();
            img.sprite = picture;
            img.raycastTarget = false;
            var fit = map.GetComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = picture != null ? picture.rect.width / picture.rect.height : 2400f / 1080f;

            for (var i = 0; i < _levels.Length; i++) Flag(map, i);

            // the safe area: the buttons keep clear of a camera cut-out
            var safe = Stretch(root, "Safe");
            var area = Screen.safeArea;
            if (Screen.width > 0 && Screen.height > 0)
            {
                safe.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
                safe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            }
            MissionSkin.Round(safe, "Back", "round_main", HudInk.OnMain, backGlyph, new Vector2(0f, 1f), new Vector2(66f, -66f), 92f)
                .onClick.AddListener(() => LoadingScreen.OutToMenu(menuScene));

            _hint = MissionSkin.Label(safe, Localization.Get("#campaign_pick"), UiTheme.Bold, 30, UiTheme.Parchment, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero);
            Place(_hint.rectTransform, new Vector2(0f, 70f), new Vector2(600f, 50f));
            _hint.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2f, -2f);

            // the bar that lights up once a level is picked: its name, a line under it, «Играть»
            var bar = new GameObject("Bar", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
            bar.SetParent(safe, false);
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = new Vector2(0f, 18f);
            bar.sizeDelta = new Vector2(560f, 210f);
            _bar = bar.GetComponent<CanvasGroup>();
            var plaque = MissionSkin.Image(bar, "Plaque", "plaque", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(520f, 96f));
            var ink = MissionSkin.Ink(HudInk.OnPlaque);
            _title = MissionSkin.Label(plaque.transform, "", UiTheme.Bold, 30, ink, TextAnchor.MiddleCenter, new Vector2(0f, 0.45f), new Vector2(1f, 1f), new Vector2(16f, 2f));
            _line = MissionSkin.Label(plaque.transform, "", UiTheme.Regular, 22, ink, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(16f, 4f));

            var playImg = MissionSkin.Image(bar, "Play", "round_main", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(250f, 96f));
            _play = playImg.gameObject.AddComponent<Button>();
            _play.onClick.AddListener(() => Enter(false));
            var glyph = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            glyph.SetParent(playImg.transform, false);
            glyph.anchorMin = glyph.anchorMax = new Vector2(0f, 0.5f);
            glyph.anchoredPosition = new Vector2(52f, 2f);
            glyph.sizeDelta = new Vector2(52f, 52f);
            var gi = glyph.GetComponent<Image>();
            gi.sprite = playGlyph;
            gi.preserveAspect = true;
            gi.color = MissionSkin.Ink(HudInk.OnMain);
            gi.raycastTarget = false;
            var playText = MissionSkin.Label(playImg.transform, Localization.Get("#campaign_play"), UiTheme.Bold, 36, MissionSkin.Ink(HudInk.OnMain),
                TextAnchor.MiddleCenter, new Vector2(0.3f, 0f), new Vector2(1f, 1f), new Vector2(4f, 4f));
            playText.resizeTextForBestFit = true;
            playText.resizeTextMaxSize = 36;

            _replay = MissionSkin.Round(bar, "Replay", "round", HudInk.OnRound, replayGlyph, new Vector2(0.5f, 0f), new Vector2(-180f, 48f), 80f);
            _replay.onClick.AddListener(() => Enter(true));

            _bar.alpha = 0f;
            _bar.interactable = _bar.blocksRaycasts = false;
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
            _picked = i;
            _shown = 0f;
            var lv = _levels[i];
            _title.text = Localization.Get(lv.titleKey);
            var playable = Playable(i);
            if (string.IsNullOrEmpty(lv.scene)) _line.text = Localization.Get("#campaign_level_soon");
            else if (!Open(i)) _line.text = Localization.Get("#campaign_locked");
            else
            {
                var best = string.IsNullOrEmpty(lv.bestKey) ? 0f : PlayerPrefs.GetFloat(lv.bestKey, 0f);
                _line.text = best > 0f ? Localization.Format("#m1_best", MissionSkin.Clock(best)) : "";
            }
            _play.gameObject.SetActive(playable);
            _replay.gameObject.SetActive(playable && i == 0 && CampaignProgress.FilmSeen);
            _hint.gameObject.SetActive(false);
            _bar.interactable = _bar.blocksRaycasts = true;
        }

        private void Enter(bool film)
        {
            if (_picked < 0 || !Playable(_picked)) return;
            // level 1: the film the first time, or on a replay; otherwise straight into the mission
            if (_picked == 0) MissionOne.SkipFilm = !film && CampaignProgress.FilmSeen;
            LoadingScreen.IntoGame(_levels[_picked].scene);
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
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
