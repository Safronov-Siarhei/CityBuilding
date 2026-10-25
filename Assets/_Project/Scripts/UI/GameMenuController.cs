using CityBuilder.Core;
using CityBuilder.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CityBuilder.UI
{
    /// <summary>
    /// The in-game menu behind the top-left button: save, settings, back to the main menu -- three
    /// pictures, no words (the user's layout, 2026-10-14). While it is open the game is paused and
    /// the rest of the HUD is hidden; a tap outside its entries closes it.
    ///
    /// Save and settings open their own windows over the paused game, and closing those comes back
    /// here rather than to the game. "Main menu" autosaves first and leaves at once: with the
    /// autosave there is nothing to lose, so there is nothing to confirm.
    /// </summary>
    public class GameMenuController : MonoBehaviour
    {
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private HudHider hud;
        [SerializeField] private GameSaveController saveController;
        [SerializeField] private SaveDialogController saveDialog;
        [SerializeField] private GameObject saveDialogRoot;
        [SerializeField] private SettingsPanelController settingsPanel;
        [SerializeField] private GameObject settingsPanelRoot;

        private enum State { Closed, Menu, Save, Settings }

        private State _state = State.Closed;
        private float _timeScaleBefore = 1f;

        public bool IsOpen => _state != State.Closed;

        public void Open()
        {
            if (_state != State.Closed) return;
            _timeScaleBefore = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            if (hud != null) hud.Hide(this);
            ShowMenu();
        }

        public void Close()
        {
            if (_state == State.Closed) return;
            _state = State.Closed;
            if (menuRoot != null) menuRoot.SetActive(false);
            ModalGate.SetBlocked(false);
            Time.timeScale = _timeScaleBefore;
            if (hud != null) hud.Show(this);
        }

        /// <summary>The menu button: opens the menu, or closes it when it is the menu that is showing.</summary>
        public void Toggle()
        {
            if (_state == State.Menu) Close();
            else Open();
        }

        public void OpenSave()
        {
            if (_state != State.Menu || saveDialog == null) return;
            _state = State.Save;
            if (menuRoot != null) menuRoot.SetActive(false);
            saveDialog.OpenDialog();
        }

        public void OpenSettings()
        {
            if (_state != State.Menu || settingsPanel == null) return;
            _state = State.Settings;
            if (menuRoot != null) menuRoot.SetActive(false);
            settingsPanel.OpenPanel();
        }

        public void GoToMainMenu()
        {
            if (saveController != null) saveController.Autosave();
            Time.timeScale = 1f;
            ModalGate.Reset();
            LoadingScreen.OutToMenu(mainMenuSceneName);
        }

        private void ShowMenu()
        {
            _state = State.Menu;
            if (menuRoot != null) menuRoot.SetActive(true);
            ModalGate.SetBlocked(true);
        }

        private void Update()
        {
            // The save dialog and the settings card close themselves (and lift the modal gate as
            // they go); when one has, the player is back in the menu, not in the game.
            if (_state == State.Save && saveDialogRoot != null && !saveDialogRoot.activeSelf) ShowMenu();
            else if (_state == State.Settings && settingsPanelRoot != null && !settingsPanelRoot.activeSelf) ShowMenu();
        }

        private void OnDestroy()
        {
            // Leaving the scene with the menu open must not leave the next one frozen.
            if (_state != State.Closed) Time.timeScale = _timeScaleBefore;
        }
    }
}
