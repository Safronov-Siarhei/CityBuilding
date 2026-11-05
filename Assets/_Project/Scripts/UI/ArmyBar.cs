using System;
using CityBuilder.Combat;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// Army control by squads (the user's mock-ups, 2026-11-05; Design/features/army-control.md):
    /// - the ARMY ICON under the heroes' portraits (moving up when fewer than three are bought);
    ///   a tap drops whatever is selected and opens the SQUAD ROW where the build menu stands;
    /// - the row: the Garrison's crest (it takes no orders: a tap says so), the player's squads
    ///   (crest, soldier count), «＋» (the squad window), and «!» beside it while a squad is
    ///   selected (that squad's «Управление»);
    /// - above it, a smaller icon per siege machine: each is selected and ordered alone.
    /// A tap on a squad selects it (a second tap lets it go); from then on a tap in the world is an
    /// order (ArmyOrderInput), and the round cancel button (SelectionBadgeController) lets it go.
    /// Not in an Invasion run (its soldiers are its own, InvasionGarrison) nor below in a dungeon.
    /// </summary>
    public class ArmyBar : MonoBehaviour
    {
        private const float RefreshSeconds = 0.25f;

        [SerializeField] private GameObject armyButton;
        [SerializeField] private GameObject row;
        [SerializeField] private Text garrisonCount;
        [SerializeField] private GameObject[] squadButtons = Array.Empty<GameObject>();
        [SerializeField] private Image[] squadCrests = Array.Empty<Image>();
        [SerializeField] private Text[] squadCounts = Array.Empty<Text>();
        [SerializeField] private GameObject[] squadRings = Array.Empty<GameObject>();
        [SerializeField] private GameObject plusButton;
        [SerializeField] private GameObject infoButton;
        [SerializeField] private GameObject machineRow;
        [SerializeField] private GameObject[] machineButtons = Array.Empty<GameObject>();
        [SerializeField] private Image[] machineIcons = Array.Empty<Image>();
        [SerializeField] private GameObject[] machineRings = Array.Empty<GameObject>();
        /// <summary>Ram, ballista, catapult, siege tower: SoldierType 13..16.</summary>
        [SerializeField] private Sprite[] machinePortraits = Array.Empty<Sprite>();
        /// <summary>Stand-in crest colours until the crest set is drawn (stage 5).</summary>
        [SerializeField] private Color[] crestColours = Array.Empty<Color>();
        /// <summary>Квадрат, треугольник, врассыпную: under the army icon while a squad is in hand (the user's mock-up).</summary>
        [SerializeField] private GameObject[] formationButtons = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] formationRings = Array.Empty<GameObject>();

        private static ArmyBar _instance;

        /// <summary>How many crests there are to choose from (stand-in colours until the set is drawn).</summary>
        public static int CrestCount => _instance != null && _instance.crestColours.Length > 0 ? _instance.crestColours.Length : 10;

        /// <summary>A crest's colour (stand-in for its picture).</summary>
        public static Color CrestColour(int crest)
        {
            if (_instance == null || _instance.crestColours.Length == 0) return Color.white;
            return _instance.crestColours[Mathf.Abs(crest) % _instance.crestColours.Length];
        }

        private void Awake() => _instance = this;

        /// <summary>The squad row is up: the build menu steps aside (BuildingPlacerUIVisibility).</summary>
        public static bool Showing { get; private set; }

        /// <summary>«＋» (null) or «!» (the squad): the squad window answers (stage 2).</summary>
        public static event Action<ArmyGroup> WindowRequested;

        private bool _open;
        private float _refreshIn;
        private float _armyBaseY = float.NaN;

        private static bool Available =>
            ArmyManager.Instance != null && ArmyManager.UsesSquads && !Dungeon.DungeonMode.IsActive;

        private void Start()
        {
            if (ArmyManager.Instance != null) ArmyManager.Instance.OnSelectionChanged += OnSelectionChanged;
        }

        private void OnDestroy()
        {
            if (ArmyManager.Instance != null) ArmyManager.Instance.OnSelectionChanged -= OnSelectionChanged;
            Showing = false;
        }

        private void OnSelectionChanged()
        {
            var selected = ArmyManager.Instance != null ? ArmyManager.Instance.SelectedGroup : null;
            // a squad or a machine in hand keeps the row up; a hero (his portrait) puts it away
            if (selected != null) _open = selected.IsSquad || selected.IsMachine;
            _refreshIn = 0f;
        }

        // ---- buttons ----

        /// <summary>The army icon: whatever was selected is let go, the row opens (a second tap closes it).</summary>
        public void TapArmy()
        {
            var army = ArmyManager.Instance;
            if (army == null) return;
            var wasOpen = _open;
            army.SelectGroup(null);
            if (BuildingFocus.Instance != null) BuildingFocus.Instance.Clear();
            if (Ships.ShipInput.HasSelection) Ships.ShipInput.Select(null);
            _open = !wasOpen;
            _refreshIn = 0f;
        }

        /// <summary>The Garrison guards the island by itself: no orders (the user, 2026-11-05).</summary>
        public void TapGarrison()
        {
            EventLogManager.Instance?.Post(GameEvent.ForSettlement(Localization.Get("#army_garrison_hint"), EventSeverity.Warning, EventGlyph.Alert, null));
        }

        public void TapSquad(int index)
        {
            var army = ArmyManager.Instance;
            if (army == null || index < 0 || index >= army.Squads.Count) return;
            army.ToggleSelection(army.Squads[index]);
            _refreshIn = 0f;
        }

        public void TapMachine(int index)
        {
            var army = ArmyManager.Instance;
            if (army == null || index < 0 || index >= army.Machines.Count) return;
            army.ToggleSelection(army.Machines[index]);
            _refreshIn = 0f;
        }

        /// <summary>«＋»: the squad window, on «Создание отрядов». Until it exists (stage 2), a new empty squad.</summary>
        public void TapPlus()
        {
            if (WindowRequested != null)
            {
                WindowRequested(null);
                return;
            }
            var army = ArmyManager.Instance;
            if (army == null) return;
            if (army.CreateSquad() == null)
                EventLogManager.Instance?.Post(GameEvent.ForSettlement(Localization.Format("#army_squads_full", ArmyManager.MaxSquads), EventSeverity.Warning, EventGlyph.Alert, null));
            _refreshIn = 0f;
        }

        /// <summary>A quick formation button: the selected squad stands so from its next step.</summary>
        public void TapFormation(int formation)
        {
            var army = ArmyManager.Instance;
            var squad = army != null ? army.SelectedGroup : null;
            if (squad == null || !squad.IsSquad) return;
            army.SetFormation(squad, (SquadFormation)formation);
            _refreshIn = 0f;
        }

        /// <summary>«!»: the selected squad's «Управление отрядами».</summary>
        public void TapInfo()
        {
            var army = ArmyManager.Instance;
            if (army == null || army.SelectedGroup == null || !army.SelectedGroup.IsSquad) return;
            WindowRequested?.Invoke(army.SelectedGroup);
        }

        /// <summary>
        /// A tap on one of the player's soldiers in a squad, or on a machine: that squad (machine) is
        /// selected and the row opens (the user, 2026-11-05); his card is on a long press now. A
        /// garrison soldier is not taken: the tap goes on to his card. Screen distance, as the card's.
        /// </summary>
        public static bool TrySelectSoldierAt(Vector2 screenPosition, Camera camera)
        {
            var army = ArmyManager.Instance;
            if (camera == null || !Available) return false;
            var reach = 34f * Mathf.Clamp(Screen.dpi / 160f, 1f, 3f);
            var best = reach * reach;
            ArmyGroup picked = null;
            foreach (var unit in SoldierUnit.All)
            {
                if (unit == null || !unit.enabled || unit.CurrentHealth <= 0) continue;
                var group = unit.Group;
                if (group == null || !(group.IsSquad || group.IsMachine)) continue;
                var p = camera.WorldToScreenPoint(unit.transform.position + Vector3.up * 0.4f);
                if (p.z <= 0f) continue;
                var d = ((Vector2)p - screenPosition).sqrMagnitude;
                if (d >= best) continue;
                best = d;
                picked = group;
            }
            if (picked == null) return false;
            if (BuildingFocus.Instance != null) BuildingFocus.Instance.Clear();
            army.SelectGroup(picked);
            return true;
        }

        // ---- showing ----

        private void Update()
        {
            var available = Available;
            if (armyButton != null && armyButton.activeSelf != available) armyButton.SetActive(available);
            if (!available) _open = false;
            // a building, a ship or a hero taken in hand puts the row away: theirs is the same place
            if (_open && ((BuildingFocus.Instance != null && BuildingFocus.Instance.Holding) || Ships.ShipInput.HasSelection || HeroInHand())) _open = false;
            Showing = _open;
            if (row != null && row.activeSelf != _open) row.SetActive(_open);

            _refreshIn -= Time.unscaledDeltaTime;
            if (_refreshIn > 0f) return;
            _refreshIn = RefreshSeconds;
            if (_open) Refresh();
            else
            {
                if (machineRow != null && machineRow.activeSelf) machineRow.SetActive(false);
                foreach (var b in formationButtons) if (b != null && b.activeSelf) b.SetActive(false);
            }
        }

        private static bool HeroInHand()
        {
            var selected = ArmyManager.Instance != null ? ArmyManager.Instance.SelectedGroup : null;
            return selected != null && SoldierStats.IsHero(selected.Type);
        }

        /// <summary>Under the lowest hero portrait, like the old army panel.</summary>
        private void LateUpdate()
        {
            if (armyButton == null || !armyButton.activeSelf) return;
            var rect = (RectTransform)armyButton.transform;
            if (float.IsNaN(_armyBaseY)) _armyBaseY = rect.anchoredPosition.y;
            var y = HeroPortraitController.ShownRows > 0 ? HeroPortraitController.StackBottom - 14f : _armyBaseY;
            if (!Mathf.Approximately(rect.anchoredPosition.y, y)) rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
            // the formation buttons in a column under it
            var below = y - rect.sizeDelta.y - 16f;
            for (var i = 0; i < formationButtons.Length; i++)
            {
                if (formationButtons[i] == null) continue;
                var r = (RectTransform)formationButtons[i].transform;
                var at = new Vector2(r.anchoredPosition.x, below - i * (r.sizeDelta.y + 14f));
                if (r.anchoredPosition != at) r.anchoredPosition = at;
            }
        }

        private void Refresh()
        {
            var army = ArmyManager.Instance;
            if (army == null) return;
            var selected = army.SelectedGroup;
            if (garrisonCount != null) garrisonCount.text = army.Garrison.Count.ToString();

            for (var i = 0; i < squadButtons.Length; i++)
            {
                var has = i < army.Squads.Count;
                if (squadButtons[i] != null && squadButtons[i].activeSelf != has) squadButtons[i].SetActive(has);
                if (!has) continue;
                var squad = army.Squads[i];
                if (i < squadCrests.Length && squadCrests[i] != null) squadCrests[i].color = CrestColour(squad.Crest);
                if (i < squadCounts.Length && squadCounts[i] != null) squadCounts[i].text = squad.Count.ToString();
                if (i < squadRings.Length && squadRings[i] != null) squadRings[i].SetActive(selected == squad);
            }
            if (plusButton != null) plusButton.SetActive(army.Squads.Count < ArmyManager.MaxSquads || WindowRequested != null);
            var info = selected != null && selected.IsSquad;
            if (infoButton != null && infoButton.activeSelf != info) infoButton.SetActive(info);
            for (var i = 0; i < formationButtons.Length; i++)
            {
                if (formationButtons[i] != null && formationButtons[i].activeSelf != info) formationButtons[i].SetActive(info);
                if (info && i < formationRings.Length && formationRings[i] != null) formationRings[i].SetActive((int)selected.Formation == i);
            }

            var machines = army.Machines.Count;
            if (machineRow != null && machineRow.activeSelf != machines > 0) machineRow.SetActive(machines > 0);
            for (var i = 0; i < machineButtons.Length; i++)
            {
                var has = i < machines;
                if (machineButtons[i] != null && machineButtons[i].activeSelf != has) machineButtons[i].SetActive(has);
                if (!has) continue;
                var machine = army.Machines[i];
                var picture = (int)machine.Type - (int)SoldierType.Ram;
                if (i < machineIcons.Length && machineIcons[i] != null && picture >= 0 && picture < machinePortraits.Length)
                    machineIcons[i].sprite = machinePortraits[picture];
                if (i < machineRings.Length && machineRings[i] != null) machineRings[i].SetActive(selected == machine);
            }
        }
    }
}
