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
        }

        public void CloseDialog()
        {
            if (dialogRoot != null) dialogRoot.SetActive(false);
        }

        public void ConfirmSave()
        {
            if (nameInput == null || saveController == null) return;

            var name = SaveSystem.SanitizeName(nameInput.text);
            if (string.IsNullOrEmpty(name)) return;

            saveController.SaveGame(name);
            CloseDialog();
        }

        /// <summary>English letters/digits and space/-/_ only, matching SaveSystem.SanitizeName.</summary>
        private static char ValidateChar(string text, int charIndex, char addedChar)
        {
            var isLetter = (addedChar >= 'a' && addedChar <= 'z') || (addedChar >= 'A' && addedChar <= 'Z');
            var isDigit = addedChar >= '0' && addedChar <= '9';
            var isAllowedSymbol = addedChar == ' ' || addedChar == '-' || addedChar == '_';
            return (isLetter || isDigit || isAllowedSymbol) ? addedChar : '\0';
        }
    }
}
