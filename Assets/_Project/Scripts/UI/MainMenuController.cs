using UnityEngine;
using UnityEngine.SceneManagement;

namespace CityBuilder.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string gameplaySceneName = "CityBuilder";

        public void StartNewGame()
        {
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
