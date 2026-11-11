using System;
using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 1's end at the town gate (the user, 2026-11-11: «Стража и затемнение», then «Итог + в
    /// меню»; the first playtest: the guards stand IN FRONT of the gate and do not move, the mission
    /// ends before the gate, not inside). Ардан reaches the light before the gate and the player's
    /// say is taken; the two spearmen (the sandbox's level-3 Spearman) stand either side of the way,
    /// the screen goes black, «Миссия выполнена» shows, and then the summary -- the run's time and how
    /// often he was caught -- with «Ещё раз» and «В меню».
    /// Every number and key is in Mission1.json "finish".
    /// </summary>
    public class MissionEnd
    {
        [Serializable] public class Config
        {
            public float guardScale, triggerZ, closeAt, closeSeconds, fadeAt, fadeSeconds, titleAt, summaryAt;
            public float[] runTo;
            public Guard[] guards;
            public string titleKey, timeKey, caughtKey, retryKey, menuKey, menuScene, bestKey, recordKey;
        }

        [Serializable] public class Guard { public float x, z, toX, toZ, yaw; }

        private readonly Config _c;
        private readonly List<(Transform root, Vector3 from, Vector3 to)> _guards = new List<(Transform, Vector3, Vector3)>();
        private Canvas _canvas;
        private Text _title;
        private GameObject _summary;

        /// <summary>Seconds since Ардан came into the gate; negative before.</summary>
        public float T { get; private set; } = -1f;

        public bool Started => T >= 0f;

        /// <summary>0..1, the black over the screen at the end.</summary>
        public float Fade => T < _c.fadeAt ? 0f : Mathf.Clamp01((T - _c.fadeAt) / _c.fadeSeconds);

        /// <summary>The summary's lines, for the batchmode check.</summary>
        public string Report { get; private set; }

        private readonly Sprite _retryGlyph, _menuGlyph;

        public MissionEnd(Config c, GameObject guardModel, Transform parent, Sprite retryGlyph = null, Sprite menuGlyph = null)
        {
            _c = c;
            _retryGlyph = retryGlyph;
            _menuGlyph = menuGlyph;
            if (guardModel == null || c.guards == null) return;
            foreach (var g in c.guards)
            {
                var root = new GameObject("TownGuard").transform;
                root.SetParent(parent, false);
                root.SetPositionAndRotation(new Vector3(g.x, 0f, g.z), Quaternion.Euler(0f, g.yaw, 0f));
                var fig = UnityEngine.Object.Instantiate(guardModel, root, false);
                fig.name = "Spearman";
                fig.transform.localScale = Vector3.one * c.guardScale;
                // modelled facing Blender's -y: turned round, as the army does it
                fig.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * fig.transform.localRotation;
                root.gameObject.AddComponent<Combat.FigureAnimator>().Bind(fig.transform);
                _guards.Add((root, root.position, new Vector3(g.toX, 0f, g.toZ)));
            }
        }

        public bool Ready(Vector3 ardan) => T < 0f && ardan.z < _c.triggerZ;

        public void Begin() => T = 0f;

        public Vector2 RunTo => new Vector2(_c.runTo[0], _c.runTo[1]);

        /// <summary>One frame of the end; `elapsed`, `caught` and the best run before this one
        /// (0: none) go into the summary.</summary>
        public void Step(float dt, Camera cam, float elapsed, int caught, float best = 0f)
        {
            if (T < 0f) return;
            var before = T;
            T += dt;
            var u = Mathf.SmoothStep(0f, 1f, (T - _c.closeAt) / _c.closeSeconds);
            foreach (var g in _guards) g.root.position = Vector3.Lerp(g.from, g.to, u);
            if (before < _c.titleAt && T >= _c.titleAt) Title(cam);
            if (_title != null) _title.color = new Color(1f, 0.92f, 0.7f, Mathf.Clamp01((T - _c.titleAt) / 0.6f));
            if (before < _c.summaryAt && T >= _c.summaryAt) Summary(elapsed, caught, best);
        }

        // ------------------------------------------------------------ the title and the summary

        private void Canvas(Camera cam)
        {
            if (_canvas != null) return;
            if (UnityEngine.EventSystems.EventSystem.current == null)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            var go = new GameObject("MissionEnd", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas = go.GetComponent<Canvas>();
            // an overlay above the mission's own (the joystick's black is an overlay at 50): drawn by
            // the camera it lay UNDER that black, and the end was a black screen (the user's playtest)
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 70;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 1f;
            var black = Box(go.transform, "Black", Vector2.zero, Vector2.one, Vector2.zero, Color.black);
            black.offsetMin = black.offsetMax = Vector2.zero;
        }

        private void Title(Camera cam)
        {
            Canvas(cam);
            _title = Label(_canvas.transform, Localization.Get(_c.titleKey), UiTheme.Heavy, 64, new Vector2(0.5f, 0.62f), new Vector2(900f, 100f));
            _title.color = Color.clear;
        }

        /// <summary>The run's time (and the record: the best before, or «Новый рекорд!»), how often
        /// he was caught; «Ещё раз» and «В меню» as the sandbox's round buttons, on the skin's panel
        /// (the user's second playtest: the campaign's screens in the game's own kit).</summary>
        private void Summary(float elapsed, int caught, float best)
        {
            var time = string.Format(Localization.Get(_c.timeKey), MissionSkin.Clock(elapsed));
            var times = string.Format(Localization.Get(_c.caughtKey), caught);
            var record = best <= 0f || elapsed < best ? Localization.Get(_c.recordKey) : string.Format(Localization.Get(_c.bestKey), MissionSkin.Clock(best));
            Report = time + " | " + record + " | " + times;
            var panel = MissionSkin.Image(_canvas.transform, "Summary", "panel", new Vector2(0.5f, 0.34f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(580f, 320f));
            _summary = panel.gameObject;
            var ink = MissionSkin.Ink(HudInk.OnPanel);
            var lines = new[] { (time, 0.86f, ink), (record, 0.69f, best <= 0f || elapsed < best ? UiTheme.Good : ink), (times, 0.52f, ink) };
            foreach (var (text, y, colour) in lines)
            {
                var t = Label(panel.transform, text, UiTheme.Bold, 32, new Vector2(0.5f, y), new Vector2(540f, 46f));
                t.color = colour;
                t.GetComponent<Shadow>().enabled = false;
            }
            Round(panel.transform, _retryGlyph, "round_main", HudInk.OnMain, Localization.Get(_c.retryKey), new Vector2(0.3f, 0.22f), () =>
            {
                MissionOne.SkipFilm = true;
                LoadingScreen.IntoGame(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            });
            Round(panel.transform, _menuGlyph, "round", HudInk.OnRound, Localization.Get(_c.menuKey), new Vector2(0.7f, 0.22f), () => LoadingScreen.OutToMenu(_c.menuScene));
        }

        private static void Round(Transform panel, Sprite glyph, string role, HudInk ink, string text, Vector2 at, UnityEngine.Events.UnityAction onClick)
        {
            var b = MissionSkin.Round(panel, "Button", role, ink, glyph, at, Vector2.zero, 84f);
            b.onClick.AddListener(onClick);
            var plaque = MissionSkin.Image(b.transform, "Label", "plaque", new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 4f), new Vector2(170f, 36f));
            plaque.raycastTarget = false;
            var t = MissionSkin.Label(plaque.transform, text, UiTheme.Bold, 20, UiTheme.Ink, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero);
            t.raycastTarget = false;
        }

        private static RectTransform Box(Transform parent, string name, Vector2 min, Vector2 max, Vector2 size, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = min;
            r.anchorMax = max;
            r.sizeDelta = size;
            go.GetComponent<Image>().color = colour;
            return r;
        }

        private static Text Label(Transform parent, string text, Font font, int size, Vector2 at, Vector2 box)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Shadow));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = at;
            r.sizeDelta = box;
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.raycastTarget = false;
            t.text = text;
            go.GetComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.35f);
            return t;
        }
    }
}
