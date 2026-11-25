using CityBuilder.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CityBuilder.Tutorial
{
    /// <summary>
    /// The tutorial offered inside a sandbox game (the user, 2026-11-17; card Design/features/tutorial.md «In-game offer»):
    /// what is remembered about it. Per WORLD: <see cref="Clock"/> (saved, GameSaveData.tutorialOfferClock), the play
    /// seconds counted towards the offer's window; -1 = this world will not offer it (shown already, or a world from
    /// before the offer existed). Per ACCOUNT (PlayerPrefs): how many times «Позже» was pressed, and the town to go
    /// back to once the tutorial launched from the banner is won.
    /// </summary>
    public static class TutorialOffer
    {
        private const string DeclinedKey = "tutorial.offer.declined";
        private const string ReturnSlotKey = "tutorial.return.slot";
        private const string ReturnNameKey = "tutorial.return.name";
        private const string RewardedKey = "tutorial.rewarded";

        /// <summary>This world's play seconds towards the offer; -1 = it offers nothing.</summary>
        public static float Clock = -1f;

        /// <summary>The tutorial has been won once on this device.</summary>
        public static bool TutorialDone => PlayerPrefs.GetInt(RewardedKey, 0) != 0;

        public static int Declined => PlayerPrefs.GetInt(DeclinedKey, 0);

        public static void NoteDeclined()
        {
            CityBuilder.Core.Prefs.SetInt(DeclinedKey, Declined + 1);
            PlayerPrefs.Save();
        }

        /// <summary>A town is waiting for the player after the tutorial (its slot exists).</summary>
        public static bool HasReturn => SaveSystem.SaveExists(PlayerPrefs.GetString(ReturnSlotKey, string.Empty));

        /// <summary>«Пройти обучение»: the town written into its slot, then the tutorial loads.</summary>
        public static void LaunchFromGame()
        {
            Clock = -1f;   // written into the save below: this world will not ask again
            var save = Object.FindAnyObjectByType<GameSaveController>();
            var slot = save != null ? save.WriteReturnSlot() : null;
            if (!string.IsNullOrEmpty(slot))
            {
                CityBuilder.Core.Prefs.SetString(ReturnSlotKey, slot);
                CityBuilder.Core.Prefs.SetString(ReturnNameKey, save.CurrentSaveName ?? string.Empty);
                PlayerPrefs.Save();
            }
            Time.timeScale = 1f;
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = Maps.Archipelago.ArchipelagoMapId.TutorialId;
            GameSessionIntent.NewGameSeason = Core.MapSeason.Summer;
            GameSessionIntent.NewGameIsInvasion = false;
            UI.LoadingScreen.IntoGame(SceneManager.GetActiveScene().name);
        }

        /// <summary>The tutorial won: back into the town it was launched from (under its own name, if it had one).</summary>
        public static void GoBack()
        {
            var slot = PlayerPrefs.GetString(ReturnSlotKey, string.Empty);
            var name = PlayerPrefs.GetString(ReturnNameKey, string.Empty);
            ForgetReturn();
            Time.timeScale = 1f;
            GameSessionIntent.SaveNameToLoad = slot;
            GameSessionIntent.SaveNameAfterLoad = string.IsNullOrEmpty(name) ? null : name;
            GameSessionIntent.NewGameIsInvasion = false;
            UI.LoadingScreen.IntoGame(SceneManager.GetActiveScene().name);
        }

        /// <summary>The way back is dropped (back in the main menu: the town stays in the saves list).</summary>
        public static void ForgetReturn()
        {
            CityBuilder.Core.Prefs.DeleteKey(ReturnSlotKey);
            CityBuilder.Core.Prefs.DeleteKey(ReturnNameKey);
        }
    }
}
