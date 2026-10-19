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
        private static readonly Color DefeatColor = UiTheme.Danger;
        private static readonly Color VictoryColor = UiTheme.Good;

        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text reasonLabel;
        // Shown only on a win: the user's free mode, keep playing the map after it is won.
        [SerializeField] private GameObject continueButton;

        private void Start()
        {
            if (GameOverManager.Instance != null) GameOverManager.Instance.OnGameEnded += Show;
        }

        private void Show(bool victory)
        {
            if (titleLabel != null)
            {
                titleLabel.text = Localization.Get(victory ? "#over_victory" : "#over_defeat");
                titleLabel.color = victory ? VictoryColor : DefeatColor;
            }
            if (reasonLabel != null)
            {
                var won = victory && GameOverManager.Instance != null && GameOverManager.Instance.WonByMainPortal
                    ? "#over_victory_reason_main"
                    : "#over_victory_reason";
                reasonLabel.text = Localization.Get(victory ? won : "#over_defeat_reason");
            }
            if (continueButton != null) continueButton.SetActive(victory);

            if (panelRoot != null) panelRoot.SetActive(true);
            ModalGate.SetBlocked(true);
            Time.timeScale = 0f;
        }

        /// <summary>Closes the victory panel and lets the map run on as a sandbox.</summary>
        public void ContinuePlaying()
        {
            GameOverManager.Instance?.ContinueInFreeMode();
            if (panelRoot != null) panelRoot.SetActive(false);
            ModalGate.SetBlocked(false);
            Time.timeScale = 1f;
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
