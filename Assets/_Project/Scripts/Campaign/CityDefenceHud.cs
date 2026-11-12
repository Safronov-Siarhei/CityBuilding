using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 2's screen (CityDefenceMode): the sandbox's building HUD put away, and in its place the
    /// part's goal and clock at the top, the three gates' health on the left (the line being held: outer
    /// in part 1, inner in part 2), Ардан's portrait with his health, the battle-cry button bottom right,
    /// the stick anywhere else, a banner when a part begins, the courier's line between the parts, and
    /// the end panel (won: to the campaign map; lost: «Заново» or the map). Built in code; texts #m2_*.
    /// </summary>
    public class CityDefenceHud : MonoBehaviour
    {
        // the sandbox's HUD not used by the mission (hidden by name, through a CanvasGroup: their own
        // controllers switch them on and off and would switch them back)
        private static readonly string[] Hidden =
        {
            "BuildingMenu", "ResourceHUD", "Heroes", "OfferShopHud", "BoosterHud", "ArmyButton", "ArmySquadRow",
            "ArmyMachineRow", "WorkforceButton", "InventoryButton", "SettlementStatus", "Happiness", "HappinessBreakdown",
            "Migration", "SelectionBadge", "HeroSkillBar", "SkillHint", "RotateButton", "CancelPlacementButton",
            "CancelSelectionButton", "BuildingFocusBar", "BuildingFocus",
        };

        private CityDefenceMode _mode;
        private FloatingJoystick _stick;
        private Text _goal, _clock, _banner, _middle, _ardenLine;
        private Image _ardenBar, _cryShade;
        private readonly Image[] _gateBars = new Image[3];
        private readonly Text[] _gateNames = new Text[3];
        private CanvasGroup _bannerGroup;
        private GameObject _endPanel;
        private Text _endTitle, _endLine;
        private GameObject _restartButton;
        private RectTransform _cryButton;
        private float _bannerFor;

        public void Bind(CityDefenceMode mode)
        {
            _mode = mode;
            HideSandboxHud();
            Build();
            mode.PartBegan += OnPart;
            mode.Ended += OnEnded;
        }

        private void OnDestroy()
        {
            if (_mode == null) return;
            _mode.PartBegan -= OnPart;
            _mode.Ended -= OnEnded;
        }

        private static void HideSandboxHud()
        {
            var names = new HashSet<string>(Hidden);
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas == null || !canvas.isRootCanvas) continue;
                foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
                {
                    if (!names.Contains(t.name)) continue;
                    var group = t.GetComponent<CanvasGroup>() ?? t.gameObject.AddComponent<CanvasGroup>();
                    group.alpha = 0f;
                    group.interactable = false;
                    group.blocksRaycasts = false;
                }
            }
        }

        private void Build()
        {
            var art = _mode.HudArt;
            var go = new GameObject("CityDefenceHud", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 1f;
            var root = go.transform;
            var safe = new GameObject("Safe", typeof(RectTransform)).GetComponent<RectTransform>();
            safe.SetParent(root, false);
            safe.anchorMin = Vector2.zero;
            safe.anchorMax = Vector2.one;
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            safe.gameObject.AddComponent<SafeAreaFitter>();

            // the goal and the clock, top middle
            var plaque = MissionSkin.Image(safe, "Goal", "plaque", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(460f, 86f));
            plaque.raycastTarget = false;
            _goal = MissionSkin.Label(plaque.transform, "", UiTheme.Bold, 22, UiTheme.Ink, TextAnchor.UpperCenter, Vector2.zero, Vector2.one, new Vector2(14f, 8f));
            _clock = MissionSkin.Label(plaque.transform, "", UiTheme.Heavy, 30, UiTheme.Danger, TextAnchor.LowerCenter, Vector2.zero, Vector2.one, new Vector2(14f, 6f));

            // the gates, left: an icon and a bar each (W, N, E)
            var names = new[] { "#m2_gate_w", "#m2_gate_n", "#m2_gate_e" };
            for (var i = 0; i < 3; i++)
            {
                var row = MissionSkin.Image(safe, "Gate" + i, "pillParchment", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -120f - i * 58f), new Vector2(250f, 50f));
                row.raycastTarget = false;
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(row.transform, false);
                var ir = icon.rectTransform;
                ir.anchorMin = ir.anchorMax = new Vector2(0f, 0.5f);
                ir.anchoredPosition = new Vector2(28f, 0f);
                ir.sizeDelta = new Vector2(42f, 42f);
                icon.sprite = _mode.GateIcon;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                _gateNames[i] = MissionSkin.Label(row.transform, Localization.Get(names[i]), UiTheme.Bold, 16, UiTheme.Ink, TextAnchor.UpperLeft, Vector2.zero, Vector2.one, new Vector2(56f, 4f));
                _gateBars[i] = Bar(row.transform, new Vector2(56f, 8f), new Vector2(180f, 12f));
            }

            // Ардан: portrait and health, bottom left
            var portrait = MissionSkin.Image(safe, "Arden", "round_value", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 18f), new Vector2(96f, 96f));
            portrait.raycastTarget = false;
            if (art.portrait != null)
            {
                var face = new GameObject("Face", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                face.transform.SetParent(portrait.transform, false);
                face.rectTransform.anchorMin = Vector2.zero;
                face.rectTransform.anchorMax = Vector2.one;
                face.rectTransform.offsetMin = new Vector2(8f, 8f);
                face.rectTransform.offsetMax = new Vector2(-8f, -8f);
                face.sprite = art.portrait;
                face.preserveAspect = true;
                face.raycastTarget = false;
            }
            _ardenBar = Bar(safe, new Vector2(124f, 30f), new Vector2(200f, 16f), new Vector2(0f, 0f));
            _ardenLine = MissionSkin.Label(safe, "", UiTheme.Bold, 18, UiTheme.OnWood, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero);
            var lr = _ardenLine.rectTransform;
            lr.anchoredPosition = new Vector2(124f, 54f);
            lr.sizeDelta = new Vector2(420f, 30f);
            lr.pivot = Vector2.zero;
            _ardenLine.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);

            // the battle cry, bottom right (a picture, no text)
            var cry = MissionSkin.Round(safe, "Cry", "round", HudInk.Keep, _mode.CryGlyph, new Vector2(1f, 0f), new Vector2(-90f, 90f), 128f);
            cry.onClick.AddListener(() => _mode.Cry());
            _cryButton = (RectTransform)cry.transform;
            var cryIcon = cry.transform.Find("Icon");
            if (cryIcon != null) cryIcon.GetComponent<Image>().color = Color.white;   // a picture in its own colours
            _cryShade = new GameObject("Cooldown", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            _cryShade.transform.SetParent(cry.transform, false);
            _cryShade.rectTransform.anchorMin = Vector2.zero;
            _cryShade.rectTransform.anchorMax = Vector2.one;
            _cryShade.rectTransform.offsetMin = _cryShade.rectTransform.offsetMax = Vector2.zero;
            _cryShade.sprite = art.mask;
            _cryShade.type = Image.Type.Filled;
            _cryShade.fillMethod = Image.FillMethod.Radial360;
            _cryShade.fillClockwise = false;
            _cryShade.color = new Color(0f, 0f, 0f, 0.55f);
            _cryShade.raycastTarget = false;

            // the banner (a part begins) and the middle line (the courier, Ардан down)
            _bannerGroup = new GameObject("Banner", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<CanvasGroup>();
            _bannerGroup.transform.SetParent(safe, false);
            var br = (RectTransform)_bannerGroup.transform;
            br.anchorMin = new Vector2(0.15f, 0.58f);
            br.anchorMax = new Vector2(0.85f, 0.74f);
            br.offsetMin = br.offsetMax = Vector2.zero;
            _bannerGroup.alpha = 0f;
            _bannerGroup.blocksRaycasts = false;
            _banner = MissionSkin.Label(br, "", UiTheme.Heavy, 44, UiTheme.Gold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero);
            _banner.gameObject.AddComponent<Outline>().effectColor = new Color(0.1f, 0.05f, 0f, 0.9f);
            _middle = MissionSkin.Label(safe, "", UiTheme.Heavy, 30, UiTheme.OnWood, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.42f), new Vector2(0.9f, 0.56f), Vector2.zero);
            _middle.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);

            BuildEndPanel(root);

            _stick = FloatingJoystick.Create(84f, 36f, 0.12f, 1.5f, 4f, Localization.Get("#m2_stick_hint"));
            _stick.transform.SetParent(transform, false);
            if (art.disc != null && art.rim != null) _stick.Skin(art.disc, art.rim, new Color(0.12f, 0.08f, 0.05f, 0.55f), UiTheme.Gold);
            _stick.NotOver.Add(_cryButton);
        }

        private static Image Bar(Transform parent, Vector2 at, Vector2 size, Vector2? anchor = null)
        {
            var a = anchor ?? new Vector2(0f, 0f);
            var back = new GameObject("Bar", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            back.transform.SetParent(parent, false);
            back.rectTransform.anchorMin = back.rectTransform.anchorMax = a;
            back.rectTransform.pivot = Vector2.zero;
            back.rectTransform.anchoredPosition = at;
            back.rectTransform.sizeDelta = size;
            back.color = new Color(0.18f, 0.1f, 0.06f, 0.85f);
            back.raycastTarget = false;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(back.transform, false);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = new Vector2(2f, 2f);
            fill.rectTransform.offsetMax = new Vector2(-2f, -2f);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.color = UiTheme.Good;
            fill.raycastTarget = false;
            return fill;
        }

        private void BuildEndPanel(Transform root)
        {
            _endPanel = new GameObject("End", typeof(RectTransform), typeof(Image));
            _endPanel.transform.SetParent(root, false);
            var er = (RectTransform)_endPanel.transform;
            er.anchorMin = Vector2.zero;
            er.anchorMax = Vector2.one;
            er.offsetMin = er.offsetMax = Vector2.zero;
            _endPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            var card = MissionSkin.Image(_endPanel.transform, "Card", "panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 340f));
            _endTitle = MissionSkin.Label(card.transform, "", UiTheme.Heavy, 42, UiTheme.Ink, TextAnchor.UpperCenter, Vector2.zero, Vector2.one, new Vector2(24f, 28f));
            _endLine = MissionSkin.Label(card.transform, "", UiTheme.Bold, 22, UiTheme.InkSoft, TextAnchor.MiddleCenter, new Vector2(0f, 0.3f), new Vector2(1f, 0.75f), new Vector2(30f, 0f));
            _restartButton = TextButton(card.transform, "#m2_restart", new Vector2(-120f, 46f), CityDefenceMode.Restart).gameObject;
            TextButton(card.transform, "#m2_to_map", new Vector2(120f, 46f), () =>
            {
                Time.timeScale = 1f;
                LoadingScreen.ToCampaignMap("CampaignMap");
            });
            _endPanel.SetActive(false);
        }

        private static Button TextButton(Transform parent, string key, Vector2 at, UnityEngine.Events.UnityAction act)
        {
            var img = MissionSkin.Image(parent, key, "pillWood", new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), at, new Vector2(210f, 64f));
            var b = img.gameObject.AddComponent<Button>();
            b.onClick.AddListener(act);
            MissionSkin.Label(img.transform, Localization.Get(key), UiTheme.Bold, 24, UiTheme.OnWood, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(8f, 4f));
            return b;
        }

        private void OnPart(int part)
        {
            _banner.text = Localization.Get(part == 1 ? "#m2_part1_banner" : "#m2_part2_banner");
            _bannerFor = 3.5f;
        }

        private void OnEnded(bool won)
        {
            _stick.Locked = true;
            _endTitle.text = Localization.Get(won ? "#m2_won" : "#m2_lost");
            _endTitle.color = won ? UiTheme.Good : UiTheme.Danger;
            _endLine.text = Localization.Get(won ? "#m2_won_line" : "#m2_lost_line");
            _restartButton.SetActive(!won);
            _endPanel.SetActive(true);
        }

        private void Update()
        {
            if (_mode == null) return;
            var dt = Time.unscaledDeltaTime;
            if (_stick != null) _mode.Stick = _stick.Locked ? Vector2.zero : _stick.Value;

            var stage = _mode.Current;
            var part2 = stage == CityDefenceMode.Stage.Part2 || (stage == CityDefenceMode.Stage.Won);
            _goal.text = Localization.Get(part2 ? "#m2_goal_part2" : "#m2_goal_part1");
            var seconds = Mathf.CeilToInt(Mathf.Max(0f, _mode.TimeLeft));
            _clock.text = Localization.Format(part2 ? "#m2_clock_help" : "#m2_clock_courier", $"{seconds / 60}:{seconds % 60:00}");

            var gates = part2 ? _mode.InnerGates : _mode.OuterGates;
            for (var i = 0; i < 3; i++)
            {
                var g = gates[i];
                var k = g != null && g.MaxHealth > 0 ? Mathf.Clamp01((float)g.CurrentHealth / g.MaxHealth) : 0f;
                _gateBars[i].rectTransform.anchorMax = new Vector2(k, 1f);
                _gateBars[i].color = k > 0.5f ? UiTheme.Good : k > 0.25f ? UiTheme.Warning : UiTheme.Danger;
            }

            var arden = _mode.Arden;
            var alive = arden != null && arden.CurrentHealth > 0;
            var hk = alive && arden.MaxHealth > 0 ? Mathf.Clamp01((float)arden.CurrentHealth / arden.MaxHealth) : 0f;
            _ardenBar.rectTransform.anchorMax = new Vector2(hk, 1f);
            _ardenLine.text = Localization.Get("#m2_arden");
            var cd = BalanceConfig.Instance.CityDefence.cryCooldownSeconds;
            _cryShade.fillAmount = cd > 0f ? Mathf.Clamp01(_mode.CryCooldown / cd) : 0f;

            if (stage == CityDefenceMode.Stage.Courier) _middle.text = Localization.Get("#m2_courier");
            else if (!alive && stage != CityDefenceMode.Stage.Won && stage != CityDefenceMode.Stage.Lost)
                _middle.text = Localization.Format("#m2_arden_rises", Mathf.CeilToInt(Mathf.Max(0f, _mode.ArdenRiseIn)));
            else _middle.text = "";

            if (_bannerFor > 0f) _bannerFor -= dt;
            _bannerGroup.alpha = Mathf.Clamp01(_bannerFor / 0.6f);
        }
    }
}
