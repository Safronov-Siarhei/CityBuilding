using System;
using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The squad window (the user's mock-ups, 2026-11-05; Design/features/army-control.md, stage 2).
    /// Opened by the squad row's «＋» (on «Создание отрядов») or «!» (on «Управление отрядами» for the
    /// selected squad); the game stands still while it is open, like the menu.
    /// - «Создание отрядов»: two groups side by side, each picked from a drop-down (the Garrison,
    ///   the squads, «Новый отряд»); a tap on a soldier's card sends him to the other column; the
    ///   pencil renames a squad, the bin breaks it up (its soldiers go back to the Garrison).
    /// - «Управление отрядами»: one squad, its crest (a tap: the next one), its soldiers, and the
    ///   «Пополнять из Гарнизона» switch. Behaviours and formations come in stage 3.
    /// Built here at runtime rather than in SetupProject: nearly all of it is lists that change
    /// with the army; SetupProject only hands over the sprites and the HUD root.
    /// </summary>
    public class SquadWindow : MonoBehaviour
    {
        [SerializeField] private Transform uiRoot;
        [SerializeField] private HudHider hud;
        [SerializeField] private Sprite cardSprite;
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private Sprite circleSprite;
        [SerializeField] private Sprite shieldSprite;
        [SerializeField] private Sprite homeSprite;
        [SerializeField] private Sprite pencilSprite;
        [SerializeField] private Sprite binSprite;
        [SerializeField] private Sprite checkSprite;
        /// <summary>A portrait per SoldierType (by its int), null where there is none.</summary>
        [SerializeField] private Sprite[] portraits = Array.Empty<Sprite>();
        /// <summary>Стоять, охранять город, наступать, за героем.</summary>
        [SerializeField] private Sprite[] behaviourIcons = Array.Empty<Sprite>();
        /// <summary>Квадрат, треугольник, врассыпную.</summary>
        [SerializeField] private Sprite[] formationIcons = Array.Empty<Sprite>();

        public static SquadWindow Instance { get; private set; }

        public bool IsOpen => _root != null && _root.activeSelf;

        private GameObject _root;
        private Image _manageTab, _createTab;
        private GameObject _createView, _manageView;
        private Text _hint;
        private float _hintFor;
        private GameObject _help;
        private GameObject _popup;
        private InputField _rename;
        private float _timeScaleBefore = 1f;

        private class Column
        {
            public ArmyGroup Group;
            public Text Name;
            public GameObject Pencil, Bin;
            public RectTransform List;
            public Text Empty;
        }

        private readonly Column[] _columns = { new Column(), new Column() };

        // the manage tab
        private ArmyGroup _managed;
        private Text _manageName;
        private Image _crest;
        private RectTransform _manageList;
        private Text _manageEmpty;
        private Image _refillBox;
        private Text _refillLabel;
        private readonly List<Image> _behaviourButtons = new List<Image>();
        private readonly List<Image> _formationButtons = new List<Image>();
        private Text _followLabel;

        private void Awake() => Instance = this;

        private void Start() => ArmyBar.WindowRequested += Open;

        private void OnDestroy()
        {
            ArmyBar.WindowRequested -= Open;
            if (Instance == this) Instance = null;
            if (IsOpen) Time.timeScale = _timeScaleBefore;
        }

        // ---- open / close ----

        /// <summary>null: «Создание отрядов»; a squad: its «Управление отрядами».</summary>
        public void Open(ArmyGroup squad)
        {
            var army = ArmyManager.Instance;
            if (army == null) return;
            if (_root == null) Build();
            if (!IsOpen)
            {
                _timeScaleBefore = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
                ModalGate.SetBlocked(true);
                if (hud != null) hud.Hide(this);
            }
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            ClosePopups();
            if (squad != null)
            {
                _managed = squad;
                ShowManage();
            }
            else
            {
                _columns[0].Group = army.Garrison;
                _columns[1].Group = army.Squads.Count > 0 ? army.Squads[0] : null;
                ShowCreate();
            }
        }

        public void Close()
        {
            if (!IsOpen) return;
            ClosePopups();
            _root.SetActive(false);
            Time.timeScale = _timeScaleBefore;
            ModalGate.SetBlocked(false);
            if (hud != null) hud.Show(this);
        }

        public void ShowCreate()
        {
            SelectTab(false);
            RefreshCreate();
        }

        public void ShowManage()
        {
            var army = ArmyManager.Instance;
            if (_managed == null || !Contains(army.Squads, _managed)) _managed = army.Squads.Count > 0 ? army.Squads[0] : null;
            SelectTab(true);
            RefreshManage();
        }

        private void SelectTab(bool manage)
        {
            ClosePopups();
            _createView.SetActive(!manage);
            _manageView.SetActive(manage);
            _manageTab.color = manage ? UiTheme.Gold : UiTheme.TabIdle;
            _createTab.color = manage ? UiTheme.TabIdle : UiTheme.Gold;
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (_hintFor > 0f)
            {
                _hintFor -= Time.unscaledDeltaTime;
                if (_hintFor <= 0f) ShowHint(null);
            }
        }

        // ---- the create tab ----

        private void RefreshCreate()
        {
            var army = ArmyManager.Instance;
            for (var c = 0; c < 2; c++)
            {
                var column = _columns[c];
                if (column.Group != null && !column.Group.IsGarrison && !Contains(army.Squads, column.Group)) column.Group = null;
                var group = column.Group;
                column.Name.text = group != null ? army.SquadName(group) : Localization.Get("#squads_pick");
                column.Pencil.SetActive(group != null && group.IsSquad);
                column.Bin.SetActive(group != null && group.IsSquad);
                FillCards(column.List, column.Empty, group, c);
            }
        }

        private void FillCards(RectTransform list, Text empty, ArmyGroup group, int column)
        {
            for (var i = list.childCount - 1; i >= 0; i--) Destroy(list.GetChild(i).gameObject);
            var count = group != null ? group.Count : 0;
            empty.gameObject.SetActive(count == 0);
            empty.text = Localization.Get(group == null ? "#squads_pick_hint" : "#squads_empty");
            for (var i = 0; i < count; i++)
            {
                var unit = group.Members[i];
                if (unit == null) continue;
                var card = SoldierCard(list, unit);
                if (column < 0) continue;
                var button = card.AddComponent<Button>();
                var from = column;
                button.onClick.AddListener(() => TapSoldier(unit, from));
            }
        }

        private void TapSoldier(SoldierUnit unit, int column)
        {
            var army = ArmyManager.Instance;
            var to = _columns[1 - column].Group;
            if (to == null)
            {
                ShowHint(Localization.Get("#squads_pick_hint"));
                return;
            }
            var blocker = army.PlayerMove(unit, to);
            if (blocker != null) ShowHint(blocker);
            RefreshCreate();
        }

        private void PickFor(int column)
        {
            var army = ArmyManager.Instance;
            var options = new List<(string, Action)>();
            options.Add((Localization.Get("#army_garrison"), () => SetColumn(column, army.Garrison)));
            foreach (var squad in army.Squads)
            {
                var s = squad;
                options.Add((army.SquadName(s), () => SetColumn(column, s)));
            }
            if (army.Squads.Count < ArmyManager.MaxSquads)
                options.Add((Localization.Get("#squads_new"), () => SetColumn(column, army.CreateSquad())));
            OpenPopup(_columns[column].Name.transform.parent as RectTransform, options);
        }

        private void SetColumn(int column, ArmyGroup group)
        {
            // the same group on both sides: they swap
            if (group != null && _columns[1 - column].Group == group) _columns[1 - column].Group = _columns[column].Group;
            _columns[column].Group = group;
            RefreshCreate();
        }

        private void RenameColumn(int column)
        {
            var group = column >= 0 ? _columns[column].Group : _managed;
            if (group == null || !group.IsSquad) return;
            ClosePopups();
            _rename.gameObject.SetActive(true);
            _rename.text = group.Name;
            _rename.onEndEdit.RemoveAllListeners();
            _rename.onEndEdit.AddListener(text =>
            {
                ArmyManager.Instance.RenameSquad(group, text);
                _rename.gameObject.SetActive(false);
                if (_manageView.activeSelf) RefreshManage();
                else RefreshCreate();
            });
            _rename.ActivateInputField();
        }

        private void DeleteColumn(int column)
        {
            var army = ArmyManager.Instance;
            var group = _columns[column].Group;
            if (group == null || !group.IsSquad) return;
            var blocker = army.TransferBlocker(group, army.Garrison);
            if (blocker != null)
            {
                ShowHint(blocker);
                return;
            }
            army.DeleteSquad(group);
            _columns[column].Group = column == 0 ? army.Garrison : null;
            RefreshCreate();
        }

        // ---- the manage tab ----

        private void RefreshManage()
        {
            var army = ArmyManager.Instance;
            var squad = _managed;
            _manageName.text = squad != null ? army.SquadName(squad) : Localization.Get("#squads_pick");
            var picture = squad != null ? ArmyBar.CrestSprite(squad.Crest) : null;
            _crest.sprite = picture != null ? picture : shieldSprite;
            _crest.color = squad == null ? new Color(1f, 1f, 1f, 0.3f) : picture != null ? Color.white : ArmyBar.CrestColour(squad.Crest);
            _refillBox.color = squad != null && squad.AutoRefill ? UiTheme.Good : new Color(1f, 1f, 1f, 0.35f);
            FillCards(_manageList, _manageEmpty, squad, -1);
            if (squad == null) _manageEmpty.text = Localization.Get("#squads_none");
            for (var i = 0; i < _behaviourButtons.Count; i++)
                _behaviourButtons[i].color = squad != null && (int)squad.Behaviour == i ? UiTheme.Gold : UiTheme.TabIdle;
            for (var i = 0; i < _formationButtons.Count; i++)
                _formationButtons[i].color = squad != null && (int)squad.Formation == i ? UiTheme.Gold : UiTheme.TabIdle;
            if (_followLabel != null)
                _followLabel.text = squad != null && squad.Behaviour == SquadBehaviour.FollowHero
                    ? Localization.Get("#hero_" + SoldierStats.SheetIdOf(squad.FollowHero))
                    : Localization.Get("#squads_behaviour_3");
        }

        private void TapBehaviour(int behaviour)
        {
            var army = ArmyManager.Instance;
            if (_managed == null) return;
            if ((SquadBehaviour)behaviour != SquadBehaviour.FollowHero)
            {
                army.SetBehaviour(_managed, (SquadBehaviour)behaviour);
                RefreshManage();
                return;
            }
            // «За героем»: which one (the bought ones)
            var roster = HeroRoster.Instance;
            var options = new List<(string, Action)>();
            if (roster != null)
                foreach (var hero in roster.Heroes)
                {
                    if (hero == null) continue;
                    var type = hero.Type;
                    options.Add((Localization.Get("#hero_" + SoldierStats.SheetIdOf(type)), () =>
                    {
                        army.SetBehaviour(_managed, SquadBehaviour.FollowHero, type);
                        RefreshManage();
                    }));
                }
            if (options.Count == 0)
            {
                ShowHint(Localization.Get("#squads_no_hero"));
                return;
            }
            OpenPopup(_behaviourButtons[behaviour].rectTransform, options);
        }

        private void TapFormation(int formation)
        {
            if (_managed == null) return;
            ArmyManager.Instance.SetFormation(_managed, (SquadFormation)formation);
            RefreshManage();
        }

        /// <summary>A row of round picture buttons with a word under each.</summary>
        private void IconRow(Transform parent, string titleKey, Vector2 at, Sprite[] icons, string keyPrefix, int count, Action<int> tap, List<Image> into)
        {
            Label(parent, Localization.Get(titleKey), 24, TextAnchor.MiddleLeft, at + new Vector2(0f, 62f), new Vector2(500f, 34f), UiTheme.Ink, UiTheme.Bold);
            for (var i = 0; i < count; i++)
            {
                var index = i;
                var x = at.x - 150f + i * 120f;
                var button = Img(parent, "Choice", UiTheme.TabIdle, circleSprite, new Vector2(x, at.y), new Vector2(84f, 84f));
                button.gameObject.AddComponent<Button>().onClick.AddListener(() => tap(index));
                var icon = Img(button.transform, "Icon", Color.white, i < icons.Length ? icons[i] : null, Vector2.zero, new Vector2(58f, 58f));
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var word = Label(parent, Localization.Get(keyPrefix + i), 17, TextAnchor.UpperCenter, new Vector2(x, at.y - 62f), new Vector2(118f, 44f), UiTheme.InkSoft, UiTheme.Regular);
                if (keyPrefix == "#squads_behaviour_" && i == 3) _followLabel = word;
                into.Add(button);
            }
        }

        private void PickManaged()
        {
            var army = ArmyManager.Instance;
            var options = new List<(string, Action)>();
            // the Garrison is listed so that picking it says why it has nothing to set (the user, 2026-11-05)
            options.Add((Localization.Get("#army_garrison"), () => ShowHint(Localization.Get("#army_garrison_hint"))));
            foreach (var squad in army.Squads)
            {
                var s = squad;
                options.Add((army.SquadName(s), () => { _managed = s; RefreshManage(); }));
            }
            OpenPopup(_manageName.transform.parent as RectTransform, options);
        }

        private void TapCrest()
        {
            if (_managed == null) return;
            ArmyManager.Instance.NextCrest(_managed, ArmyBar.CrestCount);
            RefreshManage();
        }

        private void TapRefill()
        {
            if (_managed == null) return;
            ArmyManager.Instance.SetAutoRefill(_managed, !_managed.AutoRefill);
            if (_managed.AutoRefill) ShowHint(Localization.Get("#squads_refill_hint"));
            RefreshManage();
        }

        // ---- hint, help, popups ----

        private void ShowHint(string text)
        {
            _hint.text = text ?? Localization.Get("#squads_hint_tap");
            _hint.color = text == null ? UiTheme.Ink : UiTheme.Danger;
            _hintFor = text == null ? 0f : 3f;
        }

        private void ToggleHelp()
        {
            ClosePopups(keepHelp: true);
            _help.SetActive(!_help.activeSelf);
            _help.transform.SetAsLastSibling();
        }

        private void ClosePopups(bool keepHelp = false)
        {
            if (_popup != null) Destroy(_popup);
            _popup = null;
            if (_rename != null) _rename.gameObject.SetActive(false);
            if (!keepHelp && _help != null) _help.SetActive(false);
        }

        /// <summary>A drop-down's list: one button per option under `under`.</summary>
        private void OpenPopup(RectTransform under, List<(string label, Action pick)> options)
        {
            ClosePopups();
            const float rowH = 62f;
            _popup = Panel(_root.transform, "Popup", cardSprite, new Color(1f, 0.97f, 0.88f, 1f));
            var rect = (RectTransform)_popup.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            var corners = new Vector3[4];
            under.GetWorldCorners(corners);
            rect.position = (corners[0] + corners[3]) * 0.5f;
            rect.sizeDelta = new Vector2(340f, rowH * options.Count + 16f);
            for (var i = 0; i < options.Count; i++)
            {
                var pick = options[i].pick;
                var b = TextButton(_popup.transform, options[i].label, new Vector2(0f, -8f - rowH * (i + 0.5f)), new Vector2(320f, rowH - 6f), 26, () =>
                {
                    ClosePopups();
                    pick();
                });
                var br = (RectTransform)b.transform;
                br.anchorMin = br.anchorMax = new Vector2(0.5f, 1f);
            }
        }

        // ---- the soldier card ----

        private GameObject SoldierCard(Transform parent, SoldierUnit unit)
        {
            var card = Panel(parent, "Soldier", cardSprite, Color.white);
            var portrait = Img(card.transform, "Portrait", new Color(0.66f, 0.92f, 0.76f), circleSprite, new Vector2(-86f, 0f), new Vector2(84f, 84f));
            var face = Img(portrait.transform, "Face", Color.white, PortraitOf(unit.Type), Vector2.zero, new Vector2(76f, 76f));
            face.preserveAspect = true;
            var level = SoldierStats.CurrentLevel(unit.Type);
            Label(card.transform, SoldierStats.DisplayName(unit.Type), 23, TextAnchor.UpperLeft, new Vector2(44f, 22f), new Vector2(170f, 32f), UiTheme.Ink, UiTheme.Bold);
            Label(card.transform, Localization.Format("#squads_level", level), 20, TextAnchor.MiddleLeft, new Vector2(44f, -4f), new Vector2(170f, 26f), UiTheme.InkSoft, UiTheme.Regular);
            var track = Img(card.transform, "Health", new Color(0.17f, 0.11f, 0.07f, 0.9f), null, new Vector2(30f, -30f), new Vector2(140f, 12f));
            var fill = Img(track.transform, "Fill", UiTheme.Good, null, Vector2.zero, Vector2.zero);
            var fr = fill.rectTransform;
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = new Vector2(Mathf.Clamp01(unit.CurrentHealth / (float)Mathf.Max(1, unit.MaxHealth)), 1f);
            fr.offsetMin = fr.offsetMax = Vector2.zero;
            Label(card.transform, $"{unit.CurrentHealth}/{unit.MaxHealth}", 17, TextAnchor.MiddleLeft, new Vector2(150f, -30f), new Vector2(80f, 24f), UiTheme.InkSoft, UiTheme.Regular);
            return card;
        }

        private Sprite PortraitOf(SoldierType type)
        {
            var i = (int)type;
            return i >= 0 && i < portraits.Length ? portraits[i] : null;
        }

        // ---- building ----

        private void Build()
        {
            _root = new GameObject("SquadWindow", typeof(RectTransform));
            _root.transform.SetParent(uiRoot != null ? uiRoot : transform, false);
            _root.AddComponent<HudOverlay>();
            Stretch((RectTransform)_root.transform);
            var backdrop = Img(_root.transform, "Backdrop", new Color(0f, 0f, 0f, 0.55f), null, Vector2.zero, Vector2.zero);
            Stretch(backdrop.rectTransform);
            // bled past the SafeArea, so a phone's cut-out edge is dimmed too
            backdrop.rectTransform.offsetMin = new Vector2(-600f, -600f);
            backdrop.rectTransform.offsetMax = new Vector2(600f, 600f);
            backdrop.gameObject.AddComponent<Button>().onClick.AddListener(() => ClosePopups());

            var card = Panel(_root.transform, "Card", cardSprite, new Color(1f, 0.96f, 0.78f, 1f));
            var cr = (RectTransform)card.transform;
            cr.sizeDelta = new Vector2(1500f, 880f);
            card.AddComponent<Button>().onClick.AddListener(() => ClosePopups());

            _manageTab = TextButton(card.transform, Localization.Get("#squads_tab_manage"), new Vector2(-300f, 370f), new Vector2(360f, 84f), 30, ShowManage).GetComponent<Image>();
            _createTab = TextButton(card.transform, Localization.Get("#squads_tab_create"), new Vector2(300f, 370f), new Vector2(360f, 84f), 30, ShowCreate).GetComponent<Image>();
            var info = TextButton(card.transform, "!", new Vector2(680f, 380f), new Vector2(76f, 76f), 40, ToggleHelp);
            info.GetComponent<Image>().sprite = circleSprite;
            info.GetComponent<Image>().color = UiTheme.Gold;

            _hint = Label(card.transform, string.Empty, 24, TextAnchor.MiddleCenter, new Vector2(0f, -330f), new Vector2(1300f, 40f), UiTheme.Ink, UiTheme.Regular);
            TextButton(card.transform, Localization.Get("#ui_close"), new Vector2(0f, -392f), new Vector2(300f, 66f), 28, Close);

            // «Создание отрядов»
            _createView = Group(card.transform, "CreateView");
            for (var c = 0; c < 2; c++)
            {
                var x = c == 0 ? -360f : 360f;
                var column = _columns[c];
                var index = c;
                var pick = TextButton(_createView.transform, string.Empty, new Vector2(x - 40f, 270f), new Vector2(330f, 66f), 26, () => PickFor(index));
                column.Name = pick.GetComponentInChildren<Text>();
                Label(pick.transform, "v", 22, TextAnchor.MiddleRight, new Vector2(-14f, 0f), new Vector2(300f, 60f), UiTheme.Ink, UiTheme.Heavy);
                column.Pencil = IconButton(_createView.transform, pencilSprite, "Aa", new Vector2(x + 172f, 270f), () => RenameColumn(index));
                column.Bin = IconButton(_createView.transform, binSprite, "X", new Vector2(x + 250f, 270f), () => DeleteColumn(index));
                column.List = ScrollGrid(_createView.transform, new Vector2(x, -20f), new Vector2(640f, 500f), 2, out column.Empty);
            }

            // «Управление отрядами»
            _manageView = Group(card.transform, "ManageView");
            var manPick = TextButton(_manageView.transform, string.Empty, new Vector2(-120f, 270f), new Vector2(380f, 66f), 26, PickManaged);
            _manageName = manPick.GetComponentInChildren<Text>();
            Label(manPick.transform, "v", 22, TextAnchor.MiddleRight, new Vector2(-14f, 0f), new Vector2(350f, 60f), UiTheme.Ink, UiTheme.Heavy);
            IconButton(_manageView.transform, pencilSprite, "Aa", new Vector2(120f, 270f), () => RenameColumn(-1));
            var crestButton = Img(_manageView.transform, "Crest", Color.white, shieldSprite, new Vector2(-600f, 270f), new Vector2(96f, 96f));
            crestButton.preserveAspect = true;
            crestButton.gameObject.AddComponent<Button>().onClick.AddListener(TapCrest);
            _crest = crestButton;
            Label(_manageView.transform, Localization.Get("#squads_crest"), 20, TextAnchor.MiddleCenter, new Vector2(-600f, 205f), new Vector2(200f, 30f), UiTheme.InkSoft, UiTheme.Regular);
            _manageList = ScrollGrid(_manageView.transform, new Vector2(0f, 40f), new Vector2(1360f, 300f), 4, out _manageEmpty);
            // the user's mock-up: behaviours bottom left, formations bottom right
            IconRow(_manageView.transform, "#squads_behaviour", new Vector2(-470f, -190f), behaviourIcons, "#squads_behaviour_", 4, TapBehaviour, _behaviourButtons);
            IconRow(_manageView.transform, "#squads_formation", new Vector2(330f, -190f), formationIcons, "#squads_formation_", 3, TapFormation, _formationButtons);
            var refill = TextButton(_manageView.transform, Localization.Get("#squads_refill"), new Vector2(380f, 270f), new Vector2(440f, 62f), 22, TapRefill);
            _refillLabel = refill.GetComponentInChildren<Text>();
            _refillBox = Img(refill.transform, "Box", Color.white, checkSprite != null ? checkSprite : circleSprite, new Vector2(-185f, 0f), new Vector2(44f, 44f));
            _refillLabel.rectTransform.anchoredPosition = new Vector2(24f, 0f);

            // the help plaque and the rename field
            _help = Panel(card.transform, "Help", cardSprite, new Color(1f, 0.98f, 0.9f, 1f));
            ((RectTransform)_help.transform).sizeDelta = new Vector2(900f, 380f);
            Label(_help.transform, Localization.Get("#squads_help"), 24, TextAnchor.UpperLeft, Vector2.zero, new Vector2(840f, 340f), UiTheme.Ink, UiTheme.Regular);
            _help.AddComponent<Button>().onClick.AddListener(() => _help.SetActive(false));
            _help.SetActive(false);

            var field = Panel(card.transform, "Rename", cardSprite, Color.white);
            ((RectTransform)field.transform).sizeDelta = new Vector2(520f, 80f);
            var fieldText = Label(field.transform, string.Empty, 28, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(480f, 70f), UiTheme.Ink, UiTheme.Regular);
            fieldText.supportRichText = false;
            _rename = field.AddComponent<InputField>();
            _rename.textComponent = fieldText;
            _rename.characterLimit = 24;
            _rename.lineType = InputField.LineType.SingleLine;
            field.SetActive(false);

            ShowHint(null);
            _root.SetActive(false);
        }

        // ---- small runtime UI kit ----

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static GameObject Group(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            return go;
        }

        private static GameObject Panel(Transform parent, string name, Sprite sprite, Color colour)
        {
            var image = Img(parent, name, colour, sprite, Vector2.zero, new Vector2(300f, 110f));
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            return image.gameObject;
        }

        private static Image Img(Transform parent, string name, Color colour, Sprite sprite, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = colour;
            image.sprite = sprite;
            var rect = image.rectTransform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return image;
        }

        private static Text Label(Transform parent, string content, int size, TextAnchor anchor, Vector2 position, Vector2 box, Color colour, Font font)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = colour;
            text.text = content;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            var rect = text.rectTransform;
            rect.anchoredPosition = position;
            rect.sizeDelta = box;
            return text;
        }

        private GameObject TextButton(Transform parent, string label, Vector2 position, Vector2 size, int fontSize, Action click)
        {
            var image = Img(parent, "Button", UiTheme.TabIdle, buttonSprite, position, size);
            image.type = buttonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.gameObject.AddComponent<Button>().onClick.AddListener(() => click());
            Label(image.transform, label, fontSize, TextAnchor.MiddleCenter, Vector2.zero, size - new Vector2(20f, 6f), UiTheme.Ink, UiTheme.Bold);
            return image.gameObject;
        }

        private GameObject IconButton(Transform parent, Sprite icon, string fallback, Vector2 position, Action click)
        {
            var image = Img(parent, "IconButton", UiTheme.TabIdle, circleSprite, position, new Vector2(66f, 66f));
            image.gameObject.AddComponent<Button>().onClick.AddListener(() => click());
            if (icon != null)
            {
                var glyph = Img(image.transform, "Icon", UiTheme.Ink, icon, Vector2.zero, new Vector2(40f, 40f));
                glyph.preserveAspect = true;
                glyph.raycastTarget = false;
            }
            else Label(image.transform, fallback, 32, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(60f, 60f), UiTheme.Ink, UiTheme.Heavy);
            return image.gameObject;
        }

        /// <summary>A scrolling grid of soldier cards with a visible scroll bar on the right (the user's mock-up).</summary>
        private RectTransform ScrollGrid(Transform parent, Vector2 position, Vector2 size, int columns, out Text empty)
        {
            var frame = Img(parent, "Column", new Color(0.97f, 0.62f, 0.3f, 1f), cardSprite, position, size);
            frame.type = cardSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            var scroll = frame.gameObject.AddComponent<ScrollRect>();
            var viewport = Img(frame.transform, "Viewport", new Color(1f, 1f, 1f, 0.01f), null, Vector2.zero, Vector2.zero);
            var vr = viewport.rectTransform;
            vr.anchorMin = Vector2.zero;
            vr.anchorMax = Vector2.one;
            vr.offsetMin = new Vector2(14f, 14f);
            vr.offsetMax = new Vector2(-36f, -14f);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var cr = (RectTransform)content.transform;
            cr.anchorMin = new Vector2(0f, 1f);
            cr.anchorMax = new Vector2(1f, 1f);
            cr.pivot = new Vector2(0.5f, 1f);
            cr.anchoredPosition = Vector2.zero;
            cr.sizeDelta = Vector2.zero;
            var grid = content.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(290f, 110f);
            grid.spacing = new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(4, 4, 4, 4);
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var bar = Img(frame.transform, "Scrollbar", new Color(0f, 0f, 0f, 0.15f), null, Vector2.zero, Vector2.zero);
            var br = bar.rectTransform;
            br.anchorMin = new Vector2(1f, 0f);
            br.anchorMax = new Vector2(1f, 1f);
            br.pivot = new Vector2(1f, 0.5f);
            br.offsetMin = new Vector2(-28f, 14f);
            br.offsetMax = new Vector2(-10f, -14f);
            var handle = Img(bar.transform, "Handle", UiTheme.Wood, null, Vector2.zero, Vector2.zero);
            Stretch(handle.rectTransform);
            var scrollbar = bar.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            scroll.content = cr;
            scroll.viewport = vr;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scroll.scrollSensitivity = 30f;

            empty = Label(frame.transform, string.Empty, 24, TextAnchor.MiddleCenter, Vector2.zero, size - new Vector2(80f, 40f), UiTheme.Ink, UiTheme.Regular);
            return cr;
        }

        private static bool Contains(IReadOnlyList<ArmyGroup> list, ArmyGroup group)
        {
            for (var i = 0; i < list.Count; i++)
                if (list[i] == group) return true;
            return false;
        }
    }
}
