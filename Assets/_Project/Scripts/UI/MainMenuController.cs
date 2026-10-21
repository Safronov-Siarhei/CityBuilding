using System;
using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.Saving;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>The three ways to play, in the order their cards stand.</summary>
    public enum GameMode
    {
        Campaign = 0,
        Sandbox = 1,
        Invasion = 2,
    }

    /// <summary>
    /// The main menu after the user's sketch (2026-10-20), screen by screen:
    ///
    ///   main     a live scene behind; the shop, Play and the settings along the bottom; "Continue"
    ///            with what it continues in the bottom-right corner when there is a game to go back to;
    ///   modes    Play brings up three cards -- campaign, sandbox, invasion -- with back and settings
    ///            in the top corners;
    ///   a mode   the card picked slides to the left, the other two leave, and that mode's own panel
    ///            comes in on the right: the campaign's map of levels, the sandbox's new game and saves,
    ///            the invasion's hub.
    ///
    /// Back (the button, Android's back key, Escape) walks those steps backwards; on the main screen
    /// it asks whether to quit, which is the only way out -- there is no quit button (iOS forbids one).
    /// Everything moves by easing toward where it should be each frame, so a tap halfway through a
    /// slide simply turns it round.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        private enum Screen { Main, Modes, Mode }

        [SerializeField] private string gameplaySceneName = "CityBuilder";

        [Header("Screens")]
        [SerializeField] private CanvasGroup mainScreen;
        [SerializeField] private CanvasGroup modesScreen;

        [Header("Mode cards, in GameMode order")]
        [SerializeField] private RectTransform[] cards;
        [SerializeField] private Text[] cardStatus;
        [SerializeField] private CanvasGroup[] panels;
        [SerializeField] private float chosenCardX = -700f;

        [Header("Continue")]
        [SerializeField] private GameObject continueButton;
        [SerializeField] private Text continueDetail;

        [Header("Dialogs")]
        [SerializeField] private GameObject exitDialog;
        [SerializeField] private GameObject shopDialog;
        [SerializeField] private SettingsPanelController settingsPanel;

        [Header("Sandbox")]
        [SerializeField] private MapSelectPanelController mapSelectPanel;
        [SerializeField] private LoadGamePanelController loadGamePanel;
        [SerializeField] private GameObject newGameTabSelected;
        [SerializeField] private GameObject savesTabSelected;

        [Header("Invasion")]
        [SerializeField] private Text invasionTimer;

        private const float Ease = 11f;

        private Screen _screen = Screen.Main;
        private GameMode _mode;
        private Vector2[] _cardHome;
        private CanvasGroup[] _cardGroups;
        private Vector2[] _panelHome;
        private string _continueSave;
        private int _timerSecond = -1;

        public GameMode? ChosenMode => _screen == Screen.Mode ? _mode : (GameMode?)null;

        private void Awake()
        {
            _cardHome = new Vector2[cards.Length];
            _cardGroups = new CanvasGroup[cards.Length];
            for (var i = 0; i < cards.Length; i++)
            {
                _cardHome[i] = cards[i].anchoredPosition;
                _cardGroups[i] = cards[i].GetComponent<CanvasGroup>();
            }
            _panelHome = new Vector2[panels.Length];
            for (var i = 0; i < panels.Length; i++)
            {
                _panelHome[i] = ((RectTransform)panels[i].transform).anchoredPosition;
            }
        }

        private void Start()
        {
            FindContinue();
            FillCardStatus();
            Snap();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Back();

            var k = 1f - Mathf.Exp(-Ease * Time.unscaledDeltaTime);
            Animate(k);
            TickTimer();
        }

        // ------------------------------------------------------------ navigation

        public void OpenModes()
        {
            _screen = Screen.Modes;
            CloseSandboxLists();
        }

        public void ChooseMode(int mode)
        {
            _mode = (GameMode)mode;
            _screen = Screen.Mode;
            if (_mode == GameMode.Sandbox) ShowNewGameTab();
        }

        public void ShowMain()
        {
            _screen = Screen.Main;
            CloseSandboxLists();
        }

        /// <summary>One step back: a dialog, then the settings, then the screens; on the main screen, the question whether to quit.</summary>
        public void Back()
        {
            if (shopDialog != null && shopDialog.activeSelf) { shopDialog.SetActive(false); return; }
            if (exitDialog != null && exitDialog.activeSelf) { exitDialog.SetActive(false); return; }
            if (settingsPanel != null && settingsPanel.gameObject.activeSelf) { settingsPanel.ClosePanel(); return; }

            switch (_screen)
            {
                case Screen.Mode: OpenModes(); break;
                case Screen.Modes: ShowMain(); break;
                default: if (exitDialog != null) exitDialog.SetActive(true); break;
            }
        }

        /// <summary>The way straight to a new sandbox game -- the cards, the sandbox, its map list.</summary>
        public void StartNewGame()
        {
            OpenModes();
            ChooseMode((int)GameMode.Sandbox);
            Snap();
        }

        /// <summary>«В бой»: a new Invasion run on the mode's own map (see CityBuilder.Invasion.InvasionMode).</summary>
        public void StartInvasion()
        {
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = Invasion.InvasionMode.MapId;
            GameSessionIntent.NewGameIsInvasion = true;
            SceneManager.LoadScene(gameplaySceneName);
        }

        public void OpenShop() { if (shopDialog != null) shopDialog.SetActive(true); }
        public void CloseShop() { if (shopDialog != null) shopDialog.SetActive(false); }
        public void CloseExitDialog() { if (exitDialog != null) exitDialog.SetActive(false); }

        public void ShowNewGameTab()
        {
            if (loadGamePanel != null) loadGamePanel.ClosePanel();
            if (mapSelectPanel != null) mapSelectPanel.OpenPanel();
            if (newGameTabSelected != null) newGameTabSelected.SetActive(true);
            if (savesTabSelected != null) savesTabSelected.SetActive(false);
        }

        public void ShowSavesTab()
        {
            if (mapSelectPanel != null) mapSelectPanel.ClosePanel();
            if (loadGamePanel != null) loadGamePanel.OpenPanel();
            if (newGameTabSelected != null) newGameTabSelected.SetActive(false);
            if (savesTabSelected != null) savesTabSelected.SetActive(true);
        }

        public void ContinueGame()
        {
            if (string.IsNullOrEmpty(_continueSave)) return;
            GameSessionIntent.SaveNameToLoad = _continueSave;
            SceneManager.LoadScene(gameplaySceneName);
        }

        public void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void CloseSandboxLists()
        {
            if (mapSelectPanel != null) mapSelectPanel.ClosePanel();
            if (loadGamePanel != null) loadGamePanel.ClosePanel();
        }

        // ------------------------------------------------------------ motion

        private void Snap() => Animate(1f);

        private void Animate(float k)
        {
            Fade(mainScreen, _screen == Screen.Main, k);
            Fade(modesScreen, _screen != Screen.Main, k);

            for (var i = 0; i < cards.Length; i++)
            {
                var chosen = _screen == Screen.Mode && (int)_mode == i;
                var away = _screen == Screen.Mode && !chosen;
                var target = _cardHome[i];
                if (chosen) target = new Vector2(chosenCardX, _cardHome[i].y);
                else if (away) target = _cardHome[i] + AwayFrom(i);
                cards[i].anchoredPosition = Vector2.Lerp(cards[i].anchoredPosition, target, k);

                var group = _cardGroups[i];
                if (group == null) continue;
                group.alpha = Mathf.Lerp(group.alpha, away ? 0f : 1f, k);
                // A card answers taps only while the three stand side by side.
                group.blocksRaycasts = _screen == Screen.Modes;
            }

            for (var i = 0; i < panels.Length; i++)
            {
                var shown = _screen == Screen.Mode && (int)_mode == i;
                var rect = (RectTransform)panels[i].transform;
                var target = shown ? _panelHome[i] : _panelHome[i] + new Vector2(260f, 0f);
                rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, target, k);
                panels[i].alpha = Mathf.Lerp(panels[i].alpha, shown ? 1f : 0f, k);
                panels[i].blocksRaycasts = shown;
                panels[i].interactable = shown;
            }
        }

        /// <summary>Where a card that was not picked goes: the left one off to the left, the right one off to the right, the middle one down.</summary>
        private Vector2 AwayFrom(int i)
        {
            if (i == 0) return new Vector2(-1500f, 0f);
            if (i == cards.Length - 1) return new Vector2(1500f, 0f);
            return new Vector2(0f, -1300f);
        }

        private static void Fade(CanvasGroup group, bool shown, float k)
        {
            if (group == null) return;
            group.alpha = Mathf.Lerp(group.alpha, shown ? 1f : 0f, k);
            group.blocksRaycasts = shown;
            group.interactable = shown;
        }

        // ------------------------------------------------------------ what the screens say

        /// <summary>The newest save on a map still in the game: "Continue" goes back to it, and its line says where and when.</summary>
        private void FindContinue()
        {
            SaveSlotInfo? found = null;
            foreach (var save in SaveSystem.ListSaves())
            {
                if (found.HasValue && save.LastModifiedUtc <= found.Value.LastModifiedUtc) continue;
                found = save;
            }

            var newest = found.GetValueOrDefault();
            var data = found.HasValue ? SaveSystem.Load(newest.Name) : null;
            var mapGone = data != null && !string.IsNullOrEmpty(data.mapId)
                          && MeshMapCatalog.GetById(data.mapId) == null && MapCatalog.GetById(data.mapId) == null;
            if (data == null || mapGone)
            {
                if (continueButton != null) continueButton.SetActive(false);
                return;
            }

            _continueSave = newest.Name;
            if (continueButton != null) continueButton.SetActive(true);
            if (continueDetail == null) return;

            var map = string.IsNullOrEmpty(data.mapId) ? string.Empty : Localization.Get(MapSelectPanelController.NameKey(data.mapId));
            var where = string.IsNullOrEmpty(map) ? Localization.Get("#mode_sandbox") : $"{Localization.Get("#mode_sandbox")} · {map}";
            continueDetail.text = $"{where}\n{Localization.Format("#continue_day", data.currentDay)} · {When(newest.LastModifiedUtc)}";
        }

        private static string When(DateTime utc)
        {
            var local = utc.ToLocalTime();
            var clock = local.ToString("HH:mm");
            if (local.Date == DateTime.Now.Date) return Localization.Format("#when_today", clock);
            if (local.Date == DateTime.Now.Date.AddDays(-1)) return Localization.Format("#when_yesterday", clock);
            return local.ToString("dd.MM, HH:mm");
        }

        private void FillCardStatus()
        {
            if (cardStatus == null) return;
            for (var i = 0; i < cardStatus.Length; i++)
            {
                if (cardStatus[i] == null) continue;
                cardStatus[i].text = (GameMode)i == GameMode.Sandbox
                    ? Localization.Format("#mode_saves_count", SaveSystem.ListSaves().Count)
                    : Localization.Get("#mode_soon");
            }
        }

        /// <summary>The invasion hub's countdown to the next daily challenge, at local midnight -- written once a second, not every frame.</summary>
        private void TickTimer()
        {
            if (invasionTimer == null) return;
            var now = DateTime.Now;
            if (now.Second == _timerSecond) return;
            _timerSecond = now.Second;
            var left = now.Date.AddDays(1) - now;
            invasionTimer.text = Localization.Format("#invasion_changes_in", $"{(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}");
        }
    }
}
