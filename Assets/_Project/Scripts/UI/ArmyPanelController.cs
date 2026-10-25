using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The army panel on the right of the HUD: one row per group, each showing what the group is
    /// and how many are in it ("Ополчение x5"), plus that group's standing target priority.
    /// Tapping a row selects the group -- from then on a tap in the world is an order to it (see
    /// ArmyOrderInput) -- and tapping the selected row again releases command mode.
    ///
    /// Rows are built at runtime rather than laid out in SetupProject because groups come and go
    /// with recruitment; SetupProject only provides the container, the button sprite and the
    /// summary line. The whole panel hides itself while the player has no army at all, so an
    /// unarmed settlement isn't looking at an empty box.
    /// </summary>
    public class ArmyPanelController : MonoBehaviour
    {
        private const float RowHeight = 56f;
        private const float RowSpacing = 6f;
        private const float PriorityButtonWidth = 150f;

        private static readonly Color SelectedRowColor = UiTheme.Rgb(0xf5c95a);
        private static readonly Color RowColor = UiTheme.RowNormal;
        private static readonly Color SelectedTextColor = UiTheme.Ink;
        private static readonly Color TextColor = UiTheme.Ink;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform rowContainer;
        [SerializeField] private Text summaryLabel;
        [SerializeField] private Sprite buttonSprite;

        private readonly List<GameObject> _rows = new List<GameObject>();
        private readonly List<ArmyGroup> _subscribedGroups = new List<ArmyGroup>();

        private void Start()
        {
            var army = ArmyManager.Instance;
            if (army != null)
            {
                army.OnArmyChanged += Rebuild;
                army.OnSelectionChanged += Rebuild;
            }
            Rebuild();
        }

        private void OnDestroy()
        {
            var army = ArmyManager.Instance;
            if (army != null)
            {
                army.OnArmyChanged -= Rebuild;
                army.OnSelectionChanged -= Rebuild;
            }
            UnsubscribeGroups();
        }

        private float _baseY = float.NaN;

        /// <summary>Under the heroes' portraits when there are any (the sandbox has both since 2026-10-25), else where it always stood.</summary>
        private void LateUpdate()
        {
            if (panelRoot == null || !panelRoot.activeSelf) return;
            var rect = (RectTransform)panelRoot.transform;
            if (float.IsNaN(_baseY)) _baseY = rect.anchoredPosition.y;
            var y = HeroPortraitController.ShownRows > 0 ? Mathf.Min(_baseY, HeroPortraitController.StackBottom - 12f) : _baseY;
            if (!Mathf.Approximately(rect.anchoredPosition.y, y)) rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        }

        private void Rebuild()
        {
            var army = ArmyManager.Instance;
            // In a run the player commands only the heroes, from their portraits; the soldiers
            // guard the base on their own (InvasionGarrison) -- no army panel (the user, 2026-10-23).
            var hasArmy = !Invasion.InvasionMode.IsActive && army != null && army.SoldierCount > 0;

            if (panelRoot != null) panelRoot.SetActive(hasArmy);
            ClearRows();
            UnsubscribeGroups();
            if (!hasArmy) return;

            if (summaryLabel != null)
            {
                summaryLabel.text = Localization.Format("#army_summary", army.SoldierCount, SoldierStats.MaxArmySize, army.DailyUpkeep);
            }

            var index = 0;
            foreach (var group in army.Groups)
            {
                // the heroes are commanded from their portraits; the Queen's summons are not the player's to order about
                if (group.Count == 0 || SoldierStats.IsHero(group.Type) || group.IsSummoned) continue;

                // Re-render on the group's own changes too (a member dying, an order landing),
                // not just on army-wide ones -- the row shows a live count.
                group.OnChanged += Rebuild;
                _subscribedGroups.Add(group);

                CreateRow(army, group, index);
                index++;
            }
        }

        private void CreateRow(ArmyManager army, ArmyGroup group, int index)
        {
            var isSelected = army.SelectedGroup == group;
            var y = -index * (RowHeight + RowSpacing);

            var row = new GameObject($"ArmyGroupRow{group.Id}", typeof(RectTransform));
            row.transform.SetParent(rowContainer, false);
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.offsetMin = new Vector2(0f, 0f);
            rowRect.offsetMax = new Vector2(0f, 0f);
            rowRect.sizeDelta = new Vector2(0f, RowHeight);
            rowRect.anchoredPosition = new Vector2(0f, y);
            _rows.Add(row);

            var selectButton = CreateButton(row.transform, "Select",
                $"{SoldierStats.DisplayName(group.Type)} x{group.Count}",
                isSelected ? SelectedRowColor : RowColor,
                isSelected ? SelectedTextColor : TextColor);
            var selectRect = selectButton.GetComponent<RectTransform>();
            selectRect.anchorMin = new Vector2(0f, 0f);
            selectRect.anchorMax = new Vector2(1f, 1f);
            selectRect.offsetMin = new Vector2(0f, 0f);
            selectRect.offsetMax = new Vector2(-(PriorityButtonWidth + RowSpacing), 0f);
            var selected = group;
            selectButton.onClick.AddListener(() => army.ToggleSelection(selected));

            var priorityButton = CreateButton(row.transform, "Priority", PriorityLabel(group.Priority), RowColor, TextColor);
            var priorityRect = priorityButton.GetComponent<RectTransform>();
            priorityRect.anchorMin = new Vector2(1f, 0f);
            priorityRect.anchorMax = new Vector2(1f, 1f);
            priorityRect.pivot = new Vector2(1f, 0.5f);
            priorityRect.sizeDelta = new Vector2(PriorityButtonWidth, 0f);
            priorityRect.anchoredPosition = Vector2.zero;
            priorityButton.onClick.AddListener(() => selected.SetPriority(
                selected.Priority == TargetPriority.Units ? TargetPriority.Structures : TargetPriority.Units));
        }

        private static string PriorityLabel(TargetPriority priority)
        {
            return Localization.Get(priority == TargetPriority.Units ? "#army_target_units" : "#army_target_buildings");
        }

        private Button CreateButton(Transform parent, string name, string label, Color background, Color textColor)
        {
            var buttonGO = new GameObject(name, typeof(RectTransform));
            buttonGO.transform.SetParent(parent, false);

            var image = buttonGO.AddComponent<Image>();
            image.color = background;
            if (buttonSprite != null)
            {
                image.sprite = buttonSprite;
                image.type = Image.Type.Sliced;
            }

            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = image;

            var textGO = new GameObject("Label", typeof(RectTransform));
            textGO.transform.SetParent(buttonGO.transform, false);
            var text = textGO.AddComponent<Text>();
            text.text = label;
            // Fully qualified: inside CityBuilder.UI a bare "Resources" would bind to the sibling
            // CityBuilder.Resources namespace, not UnityEngine's.
            text.font = UiTheme.Regular;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = textColor;
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6f, 0f);
            textRect.offsetMax = new Vector2(-6f, 0f);

            return button;
        }

        private void ClearRows()
        {
            foreach (var row in _rows)
            {
                if (row == null) continue;
                // Deactivated as well as destroyed: Destroy only takes effect at end of frame, and
                // the replacement rows built moments later would otherwise draw on top of these
                // for one frame.
                row.SetActive(false);
                Destroy(row);
            }
            _rows.Clear();
        }

        private void UnsubscribeGroups()
        {
            foreach (var group in _subscribedGroups)
            {
                group.OnChanged -= Rebuild;
            }
            _subscribedGroups.Clear();
        }
    }
}
