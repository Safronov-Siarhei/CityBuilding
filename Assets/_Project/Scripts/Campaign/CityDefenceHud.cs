using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 2's screen (CityDefenceMode; the user's first playtest, 2026-11-12): of the sandbox's HUD only
    /// the menu button and the minimap stay (the minimap's plaque shows the part's clock, as an Invasion run's
    /// shows its wave -- SettlementStatusController). Added: the goal top middle; Ардан top right, as a
    /// sandbox hero's portrait (face, level, health, experience); his four skills bottom middle (a closed
    /// one greyed under a lock until his level opens it; a used one shaded round as it recharges); the stick
    /// anywhere else; a banner when a part begins or he levels; the courier's line between the parts; the
    /// end panel (won: to the campaign map; lost: «Заново» or the map). The gates show their own health bars
    /// over them once hit (BuildingInstance). Built in code; texts #m2_*.
    /// </summary>
    public class CityDefenceHud : MonoBehaviour
    {
        // the sandbox's HUD not used by the mission (hidden by name, through a CanvasGroup: their own
        // controllers switch them on and off and would switch them back)
        private static readonly string[] Hidden =
        {
            "BuildingMenu", "ResourceHUD", "Heroes", "OfferShopHud", "BoosterHud", "ArmyButton", "ArmySquadRow",
            "ArmyMachineRow", "WorkforceButton", "InventoryButton", "Happiness", "HappinessBreakdown",
            "Migration", "SelectionBadge", "HeroSkillBar", "SkillHint", "RotateButton", "CancelPlacementButton",
            "CancelSelectionButton", "BuildingFocusBar", "BuildingFocus", "EventFeed", "TaxButton", "PopulationHud",
        };

        private CityDefenceMode _mode;
        private FloatingJoystick _stick;
        private Text _goal, _banner, _middle, _level;
        private Image _ardenBar, _xpBar;
        private readonly Image[] _skillShade = new Image[CityDefenceMode.SkillCount];
        private readonly GameObject[] _skillLock = new GameObject[CityDefenceMode.SkillCount];
        private readonly Image[] _skillIcon = new Image[CityDefenceMode.SkillCount];
        private CanvasGroup _bannerGroup;
        private GameObject _endPanel;
        private Text _endTitle, _endLine;
        private GameObject _restartButton;
        private float _bannerFor;

        public void Bind(CityDefenceMode mode)
        {
            _mode = mode;
            HideSandboxHud();
            Build();
            mode.PartBegan += OnPart;
            mode.Ended += OnEnded;
            mode.ArdenLeveled += OnLevel;
        }

        private void OnDestroy()
        {
            if (_mode == null) return;
            _mode.PartBegan -= OnPart;
            _mode.Ended -= OnEnded;
            _mode.ArdenLeveled -= OnLevel;
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
                    // NOT `GetComponent() ?? AddComponent()`: a missing component is Unity's fake null, which
                    // `??` takes for a real object (the user's first playtest: the mission never started)
                    var group = t.GetComponent<CanvasGroup>();
                    if (group == null) group = t.gameObject.AddComponent<CanvasGroup>();
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

            // the goal, top middle
            var plaque = MissionSkin.Image(safe, "Goal", "plaque", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(460f, 56f));
            plaque.raycastTarget = false;
            _goal = MissionSkin.Label(plaque.transform, "", UiTheme.Bold, 22, UiTheme.Ink, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(14f, 6f));
            _goal.resizeTextForBestFit = true;
            _goal.resizeTextMaxSize = 22;
            _goal.resizeTextMinSize = 12;

            BuildPortrait(safe, art);
            BuildSkills(safe, art);

            // the banner (a part begins, a level) and the middle line (the courier)
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
            for (var i = 0; i < CityDefenceMode.SkillCount; i++)
                if (_skillIcon[i] != null) _stick.NotOver.Add((RectTransform)_skillIcon[i].transform.parent);
        }

        /// <summary>Ардан top right, as a sandbox hero: the face in a round frame, his level, health and experience under it.</summary>
        private void BuildPortrait(RectTransform safe, MissionHud.Art art)
        {
            var portrait = MissionSkin.Image(safe, "Arden", "round_value", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -12f), new Vector2(92f, 92f));
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
            var badge = MissionSkin.Image(portrait.transform, "Level", "round_main", new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(10f, 10f), new Vector2(34f, 34f));
            badge.raycastTarget = false;
            _level = MissionSkin.Label(badge.transform, "1", UiTheme.Heavy, 20, MissionSkin.Ink(HudInk.OnMain), TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero);
            _ardenBar = Bar(safe, new Vector2(-18f, -112f), new Vector2(120f, 14f), new Vector2(1f, 1f));
            _xpBar = Bar(safe, new Vector2(-18f, -128f), new Vector2(120f, 8f), new Vector2(1f, 1f));
            _xpBar.color = UiTheme.Gold;
        }

        /// <summary>The four skills bottom middle: a picture each, no text.</summary>
        private void BuildSkills(RectTransform safe, MissionHud.Art art)
        {
            const float size = 104f, gap = 14f;
            var width = CityDefenceMode.SkillCount * size + (CityDefenceMode.SkillCount - 1) * gap;
            for (var i = 0; i < CityDefenceMode.SkillCount; i++)
            {
                var x = -width * 0.5f + size * 0.5f + i * (size + gap);
                var button = MissionSkin.Round(safe, "Skill" + i, "round", HudInk.Keep, _mode.SkillGlyph(i), new Vector2(0.5f, 0f), new Vector2(x, size * 0.5f + 14f), size);
                var skill = i;
                button.onClick.AddListener(() => _mode.UseSkill(skill));
                var icon = button.transform.Find("Icon");
                _skillIcon[i] = icon != null ? icon.GetComponent<Image>() : button.GetComponent<Image>();
                if (icon != null) _skillIcon[i].color = Color.white;   // a picture in its own colours
                var shade = new GameObject("Cooldown", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                shade.transform.SetParent(button.transform, false);
                shade.rectTransform.anchorMin = Vector2.zero;
                shade.rectTransform.anchorMax = Vector2.one;
                shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;
                shade.sprite = art.mask;
                shade.type = Image.Type.Filled;
                shade.fillMethod = Image.FillMethod.Radial360;
                shade.fillClockwise = false;
                shade.color = new Color(0f, 0f, 0f, 0.55f);
                shade.raycastTarget = false;
                _skillShade[i] = shade;
                var locked = new GameObject("Lock", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                locked.transform.SetParent(button.transform, false);
                locked.rectTransform.anchorMin = locked.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                locked.rectTransform.sizeDelta = new Vector2(size * 0.42f, size * 0.42f);
                locked.sprite = _mode.LockGlyph;
                locked.preserveAspect = true;
                locked.raycastTarget = false;
                _skillLock[i] = locked.gameObject;
            }
        }

        private static Image Bar(Transform parent, Vector2 at, Vector2 size, Vector2 anchor)
        {
            var back = new GameObject("Bar", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            back.transform.SetParent(parent, false);
            back.rectTransform.anchorMin = back.rectTransform.anchorMax = anchor;
            back.rectTransform.pivot = anchor;
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

        private void OnLevel(int level)
        {
            _banner.text = Localization.Format("#m2_level_up", level);
            _bannerFor = 2.5f;
        }

        private void OnEnded(bool won)
        {
            _stick.Locked = true;
            _endTitle.text = Localization.Get(won ? "#m2_won" : "#m2_lost");
            _endTitle.color = won ? UiTheme.Good : UiTheme.Danger;
            var why = _mode.Loss == CityDefenceMode.LossReason.Arden ? "#m2_lost_arden"
                : _mode.Loss == CityDefenceMode.LossReason.Castle ? "#m2_lost_castle" : "#m2_lost_line";
            _endLine.text = Localization.Get(won ? "#m2_won_line" : why);
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

            var arden = _mode.Arden;
            var alive = arden != null && arden.CurrentHealth > 0;
            var hk = alive && arden.MaxHealth > 0 ? Mathf.Clamp01((float)arden.CurrentHealth / arden.MaxHealth) : 0f;
            _ardenBar.rectTransform.anchorMax = new Vector2(hk, 1f);
            _ardenBar.color = _mode.BeingHealed ? UiTheme.Gold : hk > 0.5f ? UiTheme.Good : hk > 0.25f ? UiTheme.Warning : UiTheme.Danger;
            var next = _mode.XpForNext;
            var from = _mode.XpOfLevel;
            var xk = next > from ? Mathf.Clamp01((_mode.ArdenXp - from) / (float)(next - from)) : 1f;
            _xpBar.rectTransform.anchorMax = new Vector2(xk, 1f);
            _level.text = _mode.ArdenLevel.ToString();

            for (var i = 0; i < CityDefenceMode.SkillCount; i++)
            {
                var open = _mode.SkillOpen(i);
                _skillLock[i].SetActive(!open);
                _skillIcon[i].color = open ? Color.white : new Color(0.35f, 0.35f, 0.35f, 1f);
                _skillShade[i].fillAmount = open ? _mode.SkillCooldownShare(i) : 0f;
            }

            _middle.text = stage == CityDefenceMode.Stage.Courier ? Localization.Get("#m2_courier") : "";

            if (_bannerFor > 0f) _bannerFor -= dt;
            _bannerGroup.alpha = Mathf.Clamp01(_bannerFor / 0.6f);
        }
    }
}
