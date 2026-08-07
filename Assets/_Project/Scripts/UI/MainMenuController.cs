using CityBuilder.Maps;
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
        [SerializeField] private LoadGamePanelController loadGamePanel;

        private void Start()
        {
            if (loadGameButton != null)
            {
                loadGameButton.interactable = SaveSystem.HasAnySave();
            }
        }

        public void StartNewGame()
        {
            GameSessionIntent.SaveNameToLoad = null;
            var map = MapSelector.PickForNewGame();
            GameSessionIntent.NewGameMapId = map != null ? map.MapId : null;
            SceneManager.LoadScene(gameplaySceneName);
        }

        public void LoadGame()
        {
            if (loadGamePanel != null) loadGamePanel.OpenPanel();
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
