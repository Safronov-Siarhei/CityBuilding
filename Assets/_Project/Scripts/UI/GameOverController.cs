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
        // the tutorial's reward, shown as the crystal and its number (the user, 2026-11-06)
        [SerializeField] private Sprite crystalIcon;

        private void Start()
        {
            if (GameOverManager.Instance != null) GameOverManager.Instance.OnGameEnded += Show;
        }

        private void Show(bool victory)
        {
            if (Invasion.InvasionMode.IsActive)
            {
                ShowRunEnd();
                return;
            }
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
            if (continueButton != null) continueButton.SetActive(victory && !Tutorial.TutorialMode.IsActive);
            if (victory && Tutorial.TutorialMode.IsActive) ShowTutorialReward();

            if (panelRoot != null) panelRoot.SetActive(true);
            ModalGate.SetBlocked(true);
            Time.timeScale = 0f;
        }

        /// <summary>
        /// The tutorial won (the user's playtest, 2026-11-06: the panel was dull): no sentence, the crystal and
        /// its number in the middle, the one button «В главное меню» centred under them.
        /// </summary>
        private void ShowTutorialReward()
        {
            if (reasonLabel == null) return;
            reasonLabel.text = BalanceConfig.Instance.TutorialRewardCrystals.ToString();
            reasonLabel.font = UiTheme.Heavy;
            reasonLabel.fontSize = 64;
            reasonLabel.alignment = TextAnchor.MiddleLeft;
            reasonLabel.color = UiTheme.Ink;
            var label = reasonLabel.rectTransform;
            var centre = label.anchoredPosition;
            const float icon = 96f;
            const float gap = 14f;
            // the pair measured and centred as one (the user, 2026-11-06: it sat off the middle)
            var textWidth = reasonLabel.preferredWidth;
            var left = centre.x - (icon + gap + textWidth) * 0.5f;
            label.sizeDelta = new Vector2(textWidth + 4f, label.sizeDelta.y);
            label.anchoredPosition = new Vector2(left + icon + gap + label.sizeDelta.x * 0.5f, centre.y);
            if (crystalIcon != null && label.parent.Find("TutorialCrystal") == null)
            {
                var go = new GameObject("TutorialCrystal", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(label.parent, false);
                var image = go.GetComponent<Image>();
                image.sprite = crystalIcon;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.rectTransform.anchorMin = image.rectTransform.anchorMax = label.anchorMin;
                image.rectTransform.sizeDelta = new Vector2(icon, icon);
                image.rectTransform.anchoredPosition = new Vector2(left + icon * 0.5f, centre.y);
            }
            var menu = panelRoot != null ? panelRoot.transform.Find("Card/GameOverMenuButton") as RectTransform : null;
            if (menu != null) menu.anchoredPosition = new Vector2(0f, menu.anchoredPosition.y);
        }

        /// <summary>
        /// The end of an Invasion run (it only ends with the castle): the waves held, the run's
        /// points and what they come from, and where today's points stand now that it is written down.
        /// </summary>
        private void ShowRunEnd()
        {
            var score = Invasion.InvasionMode.Instance.Score;
            if (titleLabel != null)
            {
                titleLabel.text = Localization.Get("#over_inv_defeat");
                titleLabel.color = DefeatColor;
            }
            if (reasonLabel != null && score != null)
            {
                score.Finish();
                var counted = BalanceConfig.Instance.InvasionRunsCountedPerDay;
                var days = Invasion.InvasionRecords.LoadDays();
                var today = System.DateTime.Now;
                reasonLabel.text = Localization.Format("#over_inv_waves", score.WavesSurvived) + "\n"
                    + Localization.Format("#over_inv_points", score.Total, score.WavePoints, score.BossPoints, score.CampPoints) + "\n"
                    + Localization.Format("#over_inv_today", Invasion.InvasionRecords.DayPoints(days, today, counted),
                        Invasion.InvasionRecords.RunsCounted(days, today, counted), counted);
            }
            if (continueButton != null) continueButton.SetActive(false);
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
            LoadingScreen.OutToMenu(mainMenuSceneName);
        }
    }
}
