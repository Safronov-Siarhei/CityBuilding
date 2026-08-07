using CityBuilder.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CityBuilder.UI
{
    public class ExitToMenuController : MonoBehaviour
    {
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private GameObject dialogRoot;

        public void OpenDialog()
        {
            if (dialogRoot != null) dialogRoot.SetActive(true);
            ModalGate.SetBlocked(true);
        }

        public void CloseDialog()
        {
            if (dialogRoot != null) dialogRoot.SetActive(false);
            ModalGate.SetBlocked(false);
        }

        public void ConfirmExit()
        {
            ModalGate.SetBlocked(false);
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
