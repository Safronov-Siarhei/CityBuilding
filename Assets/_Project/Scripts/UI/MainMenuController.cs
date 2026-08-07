using CityBuilder.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string gameplaySceneName = "CityBuilder";
        [SerializeField] private Button loadGameButton;

        private void Start()
        {
            if (loadGameButton != null)
            {
                loadGameButton.interactable = SaveSystem.HasSave();
            }
        }

        public void StartNewGame()
        {
            GameSessionIntent.LoadSavedGame = false;
            SceneManager.LoadScene(gameplaySceneName);
        }

        public void LoadGame()
        {
            if (!SaveSystem.HasSave()) return;
            GameSessionIntent.LoadSavedGame = true;
            SceneManager.LoadScene(gameplaySceneName);
        }

        public void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
