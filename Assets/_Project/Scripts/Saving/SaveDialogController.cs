using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.Saving
{
    public class SaveDialogController : MonoBehaviour
    {
        [SerializeField] private GameSaveController saveController;
        [SerializeField] private GameObject dialogRoot;
        [SerializeField] private InputField nameInput;

        private void Awake()
        {
            // onValidateInput is a plain delegate, not a UnityEvent, so it can't be wired via
            // the editor script's SerializedObject calls — it has to be assigned here at runtime.
            if (nameInput != null)
            {
                nameInput.onValidateInput += ValidateChar;
            }
        }

        public void OpenDialog()
        {
            if (nameInput != null)
            {
                nameInput.text = saveController != null ? saveController.CurrentSaveName : string.Empty;
            }
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

        /// <summary>Letters/digits of any alphabet and space/-/_, matching SaveSystem.SanitizeName.</summary>
        private static char ValidateChar(string text, int charIndex, char addedChar)
        {
            // any alphabet (a Russian name came out empty and nothing was saved, the user 2026-11-18)
            var isLetter = char.IsLetterOrDigit(addedChar);
            var isDigit = false;
            var isAllowedSymbol = addedChar == ' ' || addedChar == '-' || addedChar == '_';
            return (isLetter || isDigit || isAllowedSymbol) ? addedChar : '\0';
        }
    }
}
