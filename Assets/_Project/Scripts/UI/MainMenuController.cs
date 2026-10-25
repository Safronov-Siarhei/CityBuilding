using CityBuilder.Core;
using CityBuilder.Saving;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>The ways to play. The campaign is shelved (the user, 2026-10-24) and has no screen.</summary>
    public enum GameMode
    {
        Campaign = 0,
        Sandbox = 1,
        Invasion = 2,
    }

    /// <summary>
    /// The main menu after the user's sketches (2026-10-25), screen by screen:
    ///
    ///   main     a live scene behind; «Продолжить» (to the modes), «Предложения» (the shop), the
    ///            settings at the top, Discord and YouTube below;
    ///   modes    «Песочница» and «Нашествие», each with a «!» that says what the mode is; back and
    ///            settings in the top corners, Discord and YouTube in the bottom ones;
    ///   sandbox  the maps in the theme picked, the saves (only here now), «Начать»;
    ///   invasion the hub: the theme's picture, the points, the best runs, «Забег».
    ///
    /// A «!» badge on the shop, Discord and YouTube stays until the player first opens each.
    /// Back (the button, Android's back key, Escape) walks the screens backwards; on the main screen
    /// it asks whether to quit -- there is no quit button (iOS forbids one).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        private enum Screen { Main, Modes, Sandbox, Invasion }

        [SerializeField] private string gameplaySceneName = "CityBuilder";

        [Header("Screens")]
        [SerializeField] private CanvasGroup mainScreen;
        [SerializeField] private CanvasGroup modesScreen;
        [SerializeField] private CanvasGroup sandboxScreen;
        [SerializeField] private CanvasGroup invasionScreen;

        [Header("Dialogs")]
        [SerializeField] private GameObject exitDialog;
        [SerializeField] private GameObject shopDialog;
        [SerializeField] private GameObject infoDialog;
        [SerializeField] private Text infoTitle;
        [SerializeField] private Text infoText;
        [SerializeField] private GameObject savesDialog;
        [SerializeField] private SettingsPanelController settingsPanel;

        [Header("Sandbox")]
        [SerializeField] private SandboxScreenController sandbox;
        [SerializeField] private LoadGamePanelController loadGamePanel;

        [Header("Badges: shown until the place is first opened")]
        [SerializeField] private GameObject[] shopBadges = new GameObject[0];
        [SerializeField] private GameObject[] discordBadges = new GameObject[0];
        [SerializeField] private GameObject[] youTubeBadges = new GameObject[0];

        private const float Ease = 11f;
        private const string SeenKey = "menu.seen.";
        private const string Shop = "shop", Discord = "discord", YouTube = "youtube";

        private Screen _screen = Screen.Main;

        /// <summary>The mode whose screen is up, if any.</summary>
        public GameMode? ChosenMode => _screen == Screen.Sandbox ? GameMode.Sandbox : _screen == Screen.Invasion ? GameMode.Invasion : (GameMode?)null;

        private void Start()
        {
            RefreshBadges();
            Snap();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Back();
            Animate(1f - Mathf.Exp(-Ease * Time.unscaledDeltaTime));
        }

        // ------------------------------------------------------------ navigation

        /// <summary>«Продолжить»: to the choice of mode.</summary>
        public void OpenModes() => _screen = Screen.Modes;

        public void ChooseMode(int mode)
        {
            if ((GameMode)mode == GameMode.Sandbox)
            {
                _screen = Screen.Sandbox;
                if (sandbox != null) sandbox.Open();
            }
            else if ((GameMode)mode == GameMode.Invasion)
            {
                _screen = Screen.Invasion;
            }
        }

        public void ShowMain() => _screen = Screen.Main;

        /// <summary>One step back: a dialog, then the settings, then the screens; on the main screen, the question whether to quit.</summary>
        public void Back()
        {
            foreach (var dialog in new[] { savesDialog, infoDialog, shopDialog, exitDialog })
            {
                if (dialog != null && dialog.activeSelf) { dialog.SetActive(false); return; }
            }
            if (settingsPanel != null && settingsPanel.gameObject.activeSelf) { settingsPanel.ClosePanel(); return; }

            switch (_screen)
            {
                case Screen.Sandbox:
                case Screen.Invasion: OpenModes(); break;
                case Screen.Modes: ShowMain(); break;
                default: if (exitDialog != null) exitDialog.SetActive(true); break;
            }
        }

        /// <summary>The way straight to a new sandbox game -- the sandbox's screen.</summary>
        public void StartNewGame()
        {
            ChooseMode((int)GameMode.Sandbox);
            Snap();
        }

        /// <summary>«Забег»: a new Invasion run on the theme's map, in the theme's season (see CityBuilder.Invasion.InvasionMode).</summary>
        public void StartInvasion()
        {
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = Invasion.InvasionMode.RunMapId;
            GameSessionIntent.NewGameSeason = MapThemes.SeasonOf(GameSettings.InvasionTheme);
            GameSessionIntent.NewGameIsInvasion = true;
            SceneManager.LoadScene(gameplaySceneName);
        }

        public void OpenShop()
        {
            MarkSeen(Shop);
            if (shopDialog != null) shopDialog.SetActive(true);
        }

        public void CloseShop() { if (shopDialog != null) shopDialog.SetActive(false); }
        public void CloseExitDialog() { if (exitDialog != null) exitDialog.SetActive(false); }

        /// <summary>The studio's Discord server (the user, 2026-10-25); the link is the sheet's #link_discord.</summary>
        public void OpenDiscord() => OpenLink(Discord, "#link_discord");

        /// <summary>The studio's YouTube channel; the link is the sheet's #link_youtube.</summary>
        public void OpenYouTube() => OpenLink(YouTube, "#link_youtube");

        private void OpenLink(string place, string key)
        {
            MarkSeen(place);
            var url = Localization.Get(key);
            if (!string.IsNullOrEmpty(url) && url.StartsWith("http")) Application.OpenURL(url);
        }

        /// <summary>The «!» by a mode: a few words about it.</summary>
        public void ShowInfo(int mode)
        {
            if (infoDialog == null) return;
            var id = (GameMode)mode == GameMode.Invasion ? "invasion" : "sandbox";
            if (infoTitle != null) infoTitle.text = Localization.Get($"#mode_{id}");
            if (infoText != null) infoText.text = Localization.Get($"#mode_info_{id}");
            infoDialog.SetActive(true);
        }

        public void CloseInfo() { if (infoDialog != null) infoDialog.SetActive(false); }

        /// <summary>The sandbox's saves: the only way back into a sandbox game (the user, 2026-10-25).</summary>
        public void OpenSaves()
        {
            if (savesDialog == null) return;
            savesDialog.SetActive(true);
            if (loadGamePanel != null) loadGamePanel.OpenPanel();
        }

        public void CloseSaves() { if (savesDialog != null) savesDialog.SetActive(false); }

        public void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        // ------------------------------------------------------------ badges

        private static bool Seen(string place) => PlayerPrefs.GetInt(SeenKey + place, 0) == 1;

        private void MarkSeen(string place)
        {
            if (!Seen(place))
            {
                PlayerPrefs.SetInt(SeenKey + place, 1);
                PlayerPrefs.Save();
            }
            RefreshBadges();
        }

        private void RefreshBadges()
        {
            Show(shopBadges, !Seen(Shop));
            Show(discordBadges, !Seen(Discord));
            Show(youTubeBadges, !Seen(YouTube));
        }

        private static void Show(GameObject[] badges, bool shown)
        {
            if (badges == null) return;
            foreach (var badge in badges) if (badge != null) badge.SetActive(shown);
        }

        // ------------------------------------------------------------ motion

        private void Snap() => Animate(1f);

        private void Animate(float k)
        {
            Fade(mainScreen, _screen == Screen.Main, k);
            Fade(modesScreen, _screen == Screen.Modes, k);
            Fade(sandboxScreen, _screen == Screen.Sandbox, k);
            Fade(invasionScreen, _screen == Screen.Invasion, k);
        }

        private static void Fade(CanvasGroup group, bool shown, float k)
        {
            if (group == null) return;
            group.alpha = Mathf.Lerp(group.alpha, shown ? 1f : 0f, k);
            group.blocksRaycasts = shown;
            group.interactable = shown;
        }
    }
}
