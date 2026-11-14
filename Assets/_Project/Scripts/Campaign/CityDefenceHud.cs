using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.UI;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private Canvas _canvas;
        private GameMenuController _menu;
        private FloatingJoystick _stick;
        private Text _goal, _banner, _middle, _level;
        private Text _wave;   // the wave's number under the goal, for testing (the user's fourth playtest); not in a release
        // the goal's gate under the goal plaque, its health a ring round its picture that runs down (the user's
        // eighth playtest: «иконка ворот и убывающая окантовка хп у неё»)
        private GameObject _gateBadge;
        private Image _gateRing;
        // a red rim that blinks while the gate is being struck (the user's ninth playtest)
        private Image _gateHurt;
        private BuildingInstance _gateSeen;
        private int _gateHealthSeen;
        private float _gateHurtFor;
        private Image _ardenBar, _xpBar;
        private readonly Image[] _skillShade = new Image[CityDefenceMode.SkillCount];
        private readonly GameObject[] _skillLock = new GameObject[CityDefenceMode.SkillCount];
        private readonly Image[] _skillIcon = new Image[CityDefenceMode.SkillCount];
        private CanvasGroup _bannerGroup;
        private GameObject _endPanel;
        private RectTransform _endCard;
        private Text _endTitle, _endLine, _endStats;
        private GameObject _restartButton;
        private float _bannerFor, _endPop = -1f;
        // the skill hint: shown while a skill's button is held (the user's third playtest)
        private GameObject _hint;
        private Text _hintTitle, _hintLine;

        public void Bind(CityDefenceMode mode)
        {
            _mode = mode;
            HideSandboxHud();
            Build();
            mode.PartBegan += OnPart;
            mode.Ended += OnEnded;
            mode.ArdenLeveled += OnLevel;
            mode.GoalChanged += OnGoal;
        }

        private void OnDestroy()
        {
            if (_mode == null) return;
            _mode.PartBegan -= OnPart;
            _mode.Ended -= OnEnded;
            _mode.ArdenLeveled -= OnLevel;
            _mode.GoalChanged -= OnGoal;
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
            _canvas = canvas;
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
            BuildGateBadge(safe, art);
#if !CB_RELEASE
            _wave = MissionSkin.Label(plaque.transform, "", UiTheme.Bold, 18, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, -2.25f), new Vector2(1f, -1.65f), Vector2.zero);
            _wave.raycastTarget = false;
            _wave.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
#endif

            BuildPortrait(safe, art);
            BuildSkills(safe, art);
            BuildHint(safe);

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

            BuildEndPanel(root, art);

            _stick = FloatingJoystick.Create(84f, 36f, 0.12f, 1.5f, 4f, Localization.Get("#m2_stick_hint"));
            _stick.transform.SetParent(transform, false);
            if (art.disc != null && art.rim != null) _stick.Skin(art.disc, art.rim, new Color(0.12f, 0.08f, 0.05f, 0.55f), UiTheme.Gold);
            for (var i = 0; i < CityDefenceMode.SkillCount; i++)
                if (_skillIcon[i] != null) _stick.NotOver.Add((RectTransform)_skillIcon[i].transform.parent);
        }

        /// <summary>Ардан top right, as a sandbox hero: the face in a round frame, his level, health and experience under it.</summary>
        private void BuildPortrait(RectTransform safe, MissionHud.Art art)
        {
            if (CopyHeroPortrait(safe, art)) return;
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

        /// <summary>The sandbox King's portrait block copied for Ардан (the user's second playtest: the same form as in the
        /// sandbox): the round frame, the level badge, the health and experience bars on their plaque.</summary>
        private bool CopyHeroPortrait(RectTransform safe, MissionHud.Art art)
        {
            var king = HeroPortraitController.Of(Combat.SoldierType.King);
            if (king == null || king.Widget == null || king.LevelText == null || king.HealthFill == null || king.XpFill == null) return false;
            var source = king.Widget.transform;
            var copy = Instantiate(king.Widget, safe, false);
            copy.name = "ArdenPortrait";
            copy.SetActive(true);
            var rect = (RectTransform)copy.transform;
            var from = (RectTransform)source;
            rect.anchorMin = from.anchorMin;
            rect.anchorMax = from.anchorMax;
            rect.pivot = from.pivot;
            rect.anchoredPosition = from.anchoredPosition;
            rect.sizeDelta = from.sizeDelta;
            foreach (var button in copy.GetComponentsInChildren<Button>(true)) button.interactable = false;
            foreach (var g in copy.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            var selected = copy.transform.Find("Selected");
            if (selected != null) selected.gameObject.SetActive(false);
            var fallen = copy.transform.Find("Portrait/Fallen");
            if (fallen != null) fallen.gameObject.SetActive(false);
            var face = copy.transform.Find("Portrait/Mask/Face");
            if (face != null && art.portrait != null) face.GetComponent<Image>().sprite = art.portrait;
            _level = copy.transform.Find(PathUnder(source, king.LevelText.transform))?.GetComponent<Text>();
            _ardenBar = copy.transform.Find(PathUnder(source, king.HealthFill))?.GetComponent<Image>();
            _xpBar = copy.transform.Find(PathUnder(source, king.XpFill))?.GetComponent<Image>();
            if (_level != null && _ardenBar != null && _xpBar != null) return true;
            Destroy(copy);
            return false;
        }

        private static string PathUnder(Transform root, Transform t)
        {
            var path = t.name;
            for (var p = t.parent; p != null && p != root; p = p.parent) path = p.name + "/" + path;
            return path;
        }

        /// <summary>The four skills bottom middle: a picture each, no text.</summary>
        private void BuildSkills(RectTransform safe, MissionHud.Art art)
        {
            // 1.5 times smaller than at first (the user's second playtest)
            const float size = 70f, gap = 12f;
            var width = CityDefenceMode.SkillCount * size + (CityDefenceMode.SkillCount - 1) * gap;
            for (var i = 0; i < CityDefenceMode.SkillCount; i++)
            {
                var x = -width * 0.5f + size * 0.5f + i * (size + gap);
                var button = MissionSkin.Round(safe, "Skill" + i, "round", HudInk.Keep, _mode.SkillGlyph(i), new Vector2(0.5f, 0f), new Vector2(x, size * 0.5f + 14f), size);
                var skill = i;
                var hold = button.gameObject.AddComponent<SkillHold>();
                hold.Shown = held => ShowHint(held ? skill : -1);
                button.onClick.AddListener(() =>
                {
                    if (hold.WasHint) return;   // a hold read the hint: the release casts nothing
                    _mode.UseSkill(skill);
                });
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

        /// <summary>The plaque over the skills while one is held: its name and what it does.</summary>
        private void BuildHint(RectTransform safe)
        {
            var plaque = MissionSkin.Image(safe, "SkillHint", "plaque", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 112f), new Vector2(520f, 104f));
            plaque.raycastTarget = false;
            _hintTitle = MissionSkin.Label(plaque.transform, "", UiTheme.Heavy, 24, UiTheme.Ink, TextAnchor.UpperCenter, Vector2.zero, Vector2.one, new Vector2(18f, 10f));
            _hintLine = MissionSkin.Label(plaque.transform, "", UiTheme.Bold, 18, UiTheme.InkSoft, TextAnchor.LowerCenter, Vector2.zero, new Vector2(1f, 0.66f), new Vector2(18f, 10f));
            _hintLine.resizeTextForBestFit = true;
            _hintLine.resizeTextMaxSize = 18;
            _hintLine.resizeTextMinSize = 11;
            _hint = plaque.gameObject;
            _hint.SetActive(false);
        }

        private void ShowHint(int skill)
        {
            if (_hint == null) return;
            _hint.SetActive(skill >= 0);
            if (skill < 0) return;
            _hintTitle.text = Localization.Get("#m2_skill_" + skill);
            var line = Localization.Get("#m2_skill_" + skill + "_line");
            if (!_mode.SkillOpen(skill)) line = Localization.Format("#m2_skill_locked", CityDefenceMode.SkillLevel(skill)) + "\n" + line;
            _hintLine.text = line;
        }

        private void BuildGateBadge(RectTransform safe, MissionHud.Art art)
        {
            const float size = 76f;
            var disc = Picture(safe, "GateBadge", art.disc, new Vector2(0.5f, 1f), new Vector2(0f, -74f - size * 0.5f), new Vector2(size, size));
            if (art.disc == null) disc.color = new Color(0.2f, 0.12f, 0.06f, 0.9f);
            _gateBadge = disc.gameObject;
            var icon = Picture(disc.transform, "Gate", _mode.GateIcon, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size - 18f, size - 18f));
            icon.preserveAspect = true;
            // the spent part of the ring, dark under the live one
            var back = Picture(disc.transform, "RingBack", art.rim, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size + 8f, size + 8f));
            back.type = Image.Type.Simple;
            back.color = new Color(0.12f, 0.05f, 0.03f, 0.85f);
            _gateRing = Picture(disc.transform, "Ring", art.rim, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size + 8f, size + 8f));
            _gateRing.type = Image.Type.Filled;
            _gateRing.fillMethod = Image.FillMethod.Radial360;
            _gateRing.fillOrigin = (int)Image.Origin360.Top;
            _gateRing.fillClockwise = false;
            _gateRing.fillAmount = 1f;
            _gateHurt = Picture(disc.transform, "Hurt", art.rim, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size + 18f, size + 18f));
            _gateHurt.type = Image.Type.Simple;
            _gateHurt.color = new Color(1f, 0.12f, 0.08f, 0f);
            _gateHurt.transform.SetAsFirstSibling();
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

        /// <summary>
        /// The end window (the user's third playtest: the plain one «выглядит дёшево»): the screen dimmed, a parchment
        /// card in the game's frame popping in, the title on a wooden ribbon across its top edge, the reason, a
        /// parchment strip with the run's numbers (waves beaten, time held, Ардан's level), and two wooden buttons
        /// with their pictures. The game is PAUSED under it (Time.timeScale 0; both buttons put it back).
        /// </summary>
        private void BuildEndPanel(Transform root, MissionHud.Art art)
        {
            _endPanel = new GameObject("End", typeof(RectTransform), typeof(Image));
            _endPanel.transform.SetParent(root, false);
            var er = (RectTransform)_endPanel.transform;
            er.anchorMin = Vector2.zero;
            er.anchorMax = Vector2.one;
            er.offsetMin = er.offsetMax = Vector2.zero;
            _endPanel.GetComponent<Image>().color = new Color(0.04f, 0.02f, 0.01f, 0.72f);

            var card = Picture(_endPanel.transform, "Card", art.panel, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(620f, 400f));
            _endCard = card.rectTransform;
            // the title on a wooden ribbon over the card's top edge
            var ribbon = Picture(card.transform, "Ribbon", art.pillWood, new Vector2(0.5f, 1f), new Vector2(0f, 6f), new Vector2(440f, 84f));
            var shadow = Picture(ribbon.transform, "Shadow", null, new Vector2(0.5f, 0f), new Vector2(0f, -6f), new Vector2(400f, 8f));
            shadow.color = new Color(0f, 0f, 0f, 0.25f);
            _endTitle = MissionSkin.Label(ribbon.transform, "", UiTheme.Heavy, 44, UiTheme.Gold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(16f, 6f));
            _endTitle.gameObject.AddComponent<Outline>().effectColor = new Color(0.12f, 0.05f, 0f, 0.95f);
            _endTitle.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0f, -3f);
            // the reason
            _endLine = MissionSkin.Label(card.transform, "", UiTheme.Heavy, 26, UiTheme.Ink, TextAnchor.MiddleCenter, new Vector2(0f, 0.56f), new Vector2(1f, 0.8f), new Vector2(40f, 0f));
            // the numbers on a parchment strip
            var strip = Picture(card.transform, "Stats", art.pillParchment, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(520f, 58f));
            _endStats = MissionSkin.Label(strip.transform, "", UiTheme.Bold, 20, UiTheme.InkSoft, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(18f, 4f));
            _endStats.resizeTextForBestFit = true;
            _endStats.resizeTextMaxSize = 20;
            _endStats.resizeTextMinSize = 12;
            _restartButton = TextButton(card.transform, art, art.restartGlyph, "#m2_restart", new Vector2(-130f, 62f), CityDefenceMode.Restart).gameObject;
            TextButton(card.transform, art, art.homeGlyph, "#m2_to_map", new Vector2(130f, 62f), () =>
            {
                Time.timeScale = 1f;
                LoadingScreen.ToCampaignMap("CampaignMap");
            });
            _endPanel.SetActive(false);
        }

        private static Image Picture(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 at, Vector2 size)
        {
            var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(parent, false);
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = anchor;
            img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            img.rectTransform.anchoredPosition = at;
            img.rectTransform.sizeDelta = size;
            img.sprite = sprite;
            if (sprite != null && sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }

        private static Button TextButton(Transform parent, MissionHud.Art art, Sprite glyph, string key, Vector2 at, UnityEngine.Events.UnityAction act)
        {
            var img = Picture(parent, key, art.pillWood, new Vector2(0.5f, 0f), at, new Vector2(230f, 70f));
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            b.onClick.AddListener(act);
            var textLeft = 12f;
            if (glyph != null)
            {
                var icon = Picture(img.transform, "Glyph", glyph, new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(38f, 38f));
                icon.preserveAspect = true;
                textLeft = 62f;
            }
            var label = MissionSkin.Label(img.transform, Localization.Get(key), UiTheme.Heavy, 26, UiTheme.OnWood, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero);
            label.rectTransform.offsetMin = new Vector2(textLeft, 4f);
            label.rectTransform.offsetMax = new Vector2(-12f, -4f);
            label.gameObject.AddComponent<Outline>().effectColor = new Color(0.1f, 0.04f, 0f, 0.8f);
            return b;
        }

        private void OnPart(int part)
        {
            _banner.text = Localization.Get("#m2_part1_banner");
            _bannerFor = 3.5f;
        }

        private static string GoalKey(CityDefenceMode.Goal goal) => goal == CityDefenceMode.Goal.OuterGate ? "#m2_goal_outer"
            : goal == CityDefenceMode.Goal.InnerGate ? "#m2_goal_inner" : "#m2_goal_square";

        /// <summary>A gate is broken: the new goal as a banner too.</summary>
        private void OnGoal(CityDefenceMode.Goal goal)
        {
            _banner.text = Localization.Get(GoalKey(goal));
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
                : _mode.Loss == CityDefenceMode.LossReason.Castle ? "#m2_lost_castle" : "#m2_lost_square";
            _endLine.text = Localization.Get(won ? "#m2_won_line" : why);
            _endStats.text = Localization.Format("#m2_end_stats", Mathf.Max(0, _mode.Wave - (won ? 0 : 1)), MissionSkin.Clock(_mode.HeldSeconds), _mode.ArdenLevel);
            _restartButton.SetActive(!won);
            _endPanel.SetActive(true);
            _endPop = 0f;
            Time.timeScale = 0f;   // the game stops under the window (the user's third playtest)
        }

        private void Update()
        {
            if (_mode == null) return;
            var dt = Time.unscaledDeltaTime;
            // the game menu (and its settings) open: the mission's own HUD goes too (the user's thirteenth playtest)
            if (_menu == null) _menu = FindAnyObjectByType<GameMenuController>();
            var menuOpen = _menu != null && _menu.IsOpen;
            if (_canvas != null && _canvas.enabled == menuOpen) _canvas.enabled = !menuOpen;
            if (menuOpen) return;
            if (_stick != null) _mode.Stick = _stick.Locked ? Vector2.zero : _stick.Value;

            _goal.text = Localization.Get(GoalKey(_mode.CurrentGoal));
            var gate = _mode.GoalGate;
            _gateBadge.SetActive(gate != null);
            if (gate != null)
            {
                var gk = gate.MaxHealth > 0 ? Mathf.Clamp01(gate.CurrentHealth / (float)gate.MaxHealth) : 0f;
                _gateRing.fillAmount = gk;
                _gateRing.color = gk > 0.5f ? UiTheme.Good : gk > 0.25f ? UiTheme.Warning : UiTheme.Danger;
                // struck: the red rim blinks for a while after each blow
                if (gate == _gateSeen && gate.CurrentHealth < _gateHealthSeen) _gateHurtFor = 0.6f;
                _gateSeen = gate;
                _gateHealthSeen = gate.CurrentHealth;
            }
            _gateHurtFor = Mathf.Max(0f, _gateHurtFor - dt);
            if (_gateHurt != null)
            {
                var blink = _gateHurtFor > 0f ? (Mathf.Sin(Time.unscaledTime * 25f) > 0f ? 1f : 0.25f) : 0f;
                _gateHurt.color = new Color(1f, 0.12f, 0.08f, blink);
            }
            if (_wave != null) _wave.text = _mode.Wave > 0 ? string.Format(Localization.Get("#m2_wave_number"), _mode.Wave) : string.Empty;

            var arden = _mode.Arden;
            var alive = arden != null && arden.CurrentHealth > 0;
            var hk = alive && arden.MaxHealth > 0 ? Mathf.Clamp01((float)arden.CurrentHealth / arden.MaxHealth) : 0f;
            _ardenBar.rectTransform.anchorMax = new Vector2(hk, _ardenBar.rectTransform.anchorMax.y);
            _ardenBar.color = _mode.BeingHealed ? UiTheme.Gold : hk > 0.5f ? UiTheme.Good : hk > 0.25f ? UiTheme.Warning : UiTheme.Danger;
            var next = _mode.XpForNext;
            var from = _mode.XpOfLevel;
            var xk = next > from ? Mathf.Clamp01((_mode.ArdenXp - from) / (float)(next - from)) : 1f;
            _xpBar.rectTransform.anchorMax = new Vector2(xk, _xpBar.rectTransform.anchorMax.y);
            _level.text = _mode.ArdenLevel.ToString();

            for (var i = 0; i < CityDefenceMode.SkillCount; i++)
            {
                var open = _mode.SkillOpen(i);
                _skillLock[i].SetActive(!open);
                _skillIcon[i].color = open ? Color.white : new Color(0.35f, 0.35f, 0.35f, 1f);
                _skillShade[i].fillAmount = open ? _mode.SkillCooldownShare(i) : 0f;
            }

            _middle.text = _mode.RespawnLeft > 0f ? Localization.Format("#m2_respawn", Mathf.CeilToInt(_mode.RespawnLeft)) : "";

            // the end card pops in, on the unscaled clock (the game is paused)
            if (_endPop >= 0f && _endCard != null)
            {
                _endPop += dt;
                var k = Mathf.Clamp01(_endPop / 0.28f);
                var s = k < 1f ? Mathf.LerpUnclamped(0.82f, 1f, 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f)) : 1f;
                _endCard.localScale = Vector3.one * s;
                if (k >= 1f) _endPop = -1f;
            }

            if (_bannerFor > 0f) _bannerFor -= dt;
            _bannerGroup.alpha = Mathf.Clamp01(_bannerFor / 0.6f);
        }
    }
}
