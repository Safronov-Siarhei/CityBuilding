using CityBuilder.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// Full-screen end-of-map panel, shown once GameOverManager reports the map won or lost --
    /// freezes gameplay via Time.timeScale (simpler and more complete than threading ModalGate
    /// through every new Combat script) and offers the only action available: back to the menu.
    /// One panel serves both outcomes, re-titled on the spot, since neither offers any choice
    /// beyond leaving.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        private static readonly Color DefeatColor = new Color(0.9f, 0.35f, 0.3f);
        private static readonly Color VictoryColor = new Color(0.55f, 0.85f, 0.45f);

        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text reasonLabel;

        private void Start()
        {
            if (GameOverManager.Instance != null) GameOverManager.Instance.OnGameEnded += Show;
        }

        private void Show(bool victory)
        {
            if (titleLabel != null)
            {
                titleLabel.text = victory ? "Победа" : "Поражение";
                titleLabel.color = victory ? VictoryColor : DefeatColor;
            }
            if (reasonLabel != null)
            {
                reasonLabel.text = victory ? "Все порталы орков закрыты." : "Ратуша разрушена.";
            }

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
