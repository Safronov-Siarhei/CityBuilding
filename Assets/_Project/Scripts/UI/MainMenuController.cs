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
        [SerializeField] private MapSelectPanelController mapSelectPanel;

        private void Start()
        {
            if (loadGameButton != null)
            {
                loadGameButton.interactable = SaveSystem.HasAnySave();
            }
        }

        /// <summary>Opens the map picker when there is a choice to make, otherwise starts on the default map.</summary>
        public void StartNewGame()
        {
            if (mapSelectPanel != null && MapSelector.Choosable().Count > 1)
            {
                mapSelectPanel.OpenPanel();
                return;
            }

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
