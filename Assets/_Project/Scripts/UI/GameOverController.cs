using CityBuilder.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CityBuilder.UI
{
    /// <summary>
    /// Full-screen defeat panel shown once GameOverManager reports the Town Hall destroyed --
    /// freezes gameplay via Time.timeScale (simpler and more complete than threading ModalGate
    /// through every new Combat script) and offers the only action available: back to the menu.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private GameObject panelRoot;

        private void Start()
        {
            if (GameOverManager.Instance != null) GameOverManager.Instance.OnGameOver += Show;
        }

        private void Show()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            ModalGate.SetBlocked(true);
            Time.timeScale = 0f;
        }

        public void ReturnToMenu()
        {
            // Must run before the scene unloads -- MainMenu would otherwise inherit a frozen
            // timescale and its own flythrough camera/UI would never animate.
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
