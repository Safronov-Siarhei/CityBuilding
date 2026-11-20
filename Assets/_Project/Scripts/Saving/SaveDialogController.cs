using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.Saving
{
    /// <summary>
    /// «Сохранить игру» (the user, 2026-11-20; Design/ui/Concept_SaveSlots.png, approved): the saves of THIS map listed
    /// newest first -- a tap picks one to overwrite (its name goes into the field, the button says «Перезаписать», a
    /// red line says which is replaced); typing another name makes a new save. Autosaves and other maps' saves are not
    /// listed. The rows are laid in at runtime under `slots` (a scrolling column built by SetupProject.BuildSaveUI).
    /// </summary>
    public class SaveDialogController : MonoBehaviour
    {
        private const float RowHeight = 96f;
        private const float RowGap = 10f;

        [SerializeField] private GameSaveController saveController;
        [SerializeField] private GameObject dialogRoot;
        [SerializeField] private InputField nameInput;
        [SerializeField] private RectTransform slots;
        [SerializeField] private Sprite rowSprite;
        [SerializeField] private Text warning;
        [SerializeField] private Text confirmLabel;

        private readonly List<(string name, Image image)> _rows = new List<(string, Image)>();
        private readonly HashSet<string> _names = new HashSet<string>();
        private string _picked;

        private void Awake()
        {
            // onValidateInput is a plain delegate, not a UnityEvent, so it can't be wired via
            // the editor script's SerializedObject calls — it has to be assigned here at runtime.
            if (nameInput != null)
            {
                nameInput.onValidateInput += ValidateChar;
                nameInput.onValueChanged.AddListener(_ => OnTyped());
            }
        }

        public void OpenDialog()
        {
            if (nameInput != null)
            {
                nameInput.text = saveController != null ? saveController.CurrentSaveName : string.Empty;
            }
            FillSlots();
            _picked = null;
            OnTyped();
            if (dialogRoot != null) dialogRoot.SetActive(true);
            ModalGate.SetBlocked(true);
        }

        public void CloseDialog()
        {
            if (dialogRoot != null) dialogRoot.SetActive(false);
            ModalGate.SetBlocked(false);
        }

        public void ConfirmSave()
        {
            if (nameInput == null || saveController == null) return;

            var name = SaveSystem.SanitizeName(nameInput.text);
            // nothing usable typed: the date and time, never a silent no-save
            if (string.IsNullOrEmpty(name)) name = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm");

            saveController.SaveGame(name);
            CloseDialog();
        }

        // ---------------------------------------------------------------- the slots

        /// <summary>This map's saves, newest first, as rows (no autosaves, nothing of other maps).</summary>
        private void FillSlots()
        {
            _rows.Clear();
            _names.Clear();
            if (slots == null) return;
            for (var i = slots.childCount - 1; i >= 0; i--) Destroy(slots.GetChild(i).gameObject);
            var map = saveController != null ? saveController.CurrentMapId : string.Empty;
            var y = 0f;
            foreach (var info in SaveSystem.ListSaves())
            {
                if (GameSaveController.IsAutosave(info.Name)) continue;
                GameSaveData data = null;
                try { data = SaveSystem.Load(info.Name); }
                catch { /* a broken file is not offered for overwriting */ }
                if (data == null || (!string.IsNullOrEmpty(map) && data.mapId != map)) continue;
                AddRow(info, data, y);
                _names.Add(info.Name);
                y += RowHeight + RowGap;
            }
            slots.sizeDelta = new Vector2(slots.sizeDelta.x, Mathf.Max(0f, y - RowGap));
            slots.anchoredPosition = Vector2.zero;
        }

        private void AddRow(SaveSlotInfo info, GameSaveData data, float y)
        {
            var go = new GameObject("Slot_" + info.Name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(slots, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -y);
            rect.sizeDelta = new Vector2(0f, RowHeight);
            var image = go.GetComponent<Image>();
            image.sprite = rowSprite;
            image.type = rowSprite != null && rowSprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            image.color = UiTheme.RowNormal;
            var name = info.Name;
            go.GetComponent<Button>().onClick.AddListener(() => Pick(name));

            Line(rect, "Name", name, UiTheme.Bold, 30, TextAnchor.MiddleLeft, 0.5f, 1f, 28f, -220f);
            Line(rect, "Details", Localization.Format("#save_row_stats", data.currentDay, data.population),
                UiTheme.Regular, 22, TextAnchor.MiddleLeft, 0f, 0.5f, 28f, -220f);
            var when = info.LastModifiedUtc.ToLocalTime();
            Line(rect, "When", when.ToString("dd.MM  HH:mm"), UiTheme.Regular, 22, TextAnchor.MiddleRight, 0f, 1f, 560f, -28f);
            _rows.Add((name, image));
        }

        private static void Line(RectTransform row, string name, string text, Font font, int size, TextAnchor align,
            float bottom, float top, float left, float right)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(row, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, bottom);
            rect.anchorMax = new Vector2(1f, top);
            rect.offsetMin = new Vector2(left, 4f);
            rect.offsetMax = new Vector2(right, -4f);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = align;
            t.color = UiTheme.Ink;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.text = text;
        }

        /// <summary>A row tapped: its name into the field, to be overwritten.</summary>
        private void Pick(string name)
        {
            _picked = name;
            if (nameInput != null) nameInput.text = name;   // OnTyped follows
        }

        /// <summary>The field changed: a name of a listed save = overwrite it (its row lit), any other = a new save.</summary>
        private void OnTyped()
        {
            var typed = nameInput != null ? SaveSystem.SanitizeName(nameInput.text) : string.Empty;
            var overwrite = !string.IsNullOrEmpty(typed) && (_names.Contains(typed) || SaveSystem.SaveExists(typed));
            _picked = overwrite ? typed : null;
            foreach (var (n, image) in _rows) if (image != null) image.color = n == _picked ? UiTheme.RowSelected : UiTheme.RowNormal;
            if (warning != null) warning.text = overwrite ? Localization.Format("#save_overwrite_warning", typed) : string.Empty;
            if (confirmLabel != null) confirmLabel.text = Localization.Get(overwrite ? "#save_overwrite" : "#save_confirm");
        }

        /// <summary>Letters/digits of any alphabet and space/-/_, matching SaveSystem.SanitizeName.</summary>
        private static char ValidateChar(string text, int charIndex, char addedChar)
        {
            // any alphabet (a Russian name came out empty and nothing was saved, the user 2026-11-18)
            var isLetter = char.IsLetterOrDigit(addedChar);
            var isAllowedSymbol = addedChar == ' ' || addedChar == '-' || addedChar == '_';
            return (isLetter || isAllowedSymbol) ? addedChar : '\0';
        }
    }
}
