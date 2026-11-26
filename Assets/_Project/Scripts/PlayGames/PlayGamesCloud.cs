#if UNITY_ANDROID
using System;
using CityBuilder.Core;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
using UnityEngine;

namespace CityBuilder.PlayGames
{
    /// <summary>
    /// <see cref="CloudSave"/>'s backend on Android: Google Play Games saved games (plugin 2.3.0, Assets/GooglePlayGames).
    /// One saved game, «account», holds the snapshot; two phones writing it are settled by the longer play time (the
    /// snapshot carries PlayTime.Seconds), and CloudSave's own time stamp decides against this phone's progress.
    ///
    /// Only in an Android player with the Play Games app id set (Design/store/play_games_app_id.txt, put into the
    /// plugin's settings by PlayGamesSetup at the next setup); without it nothing is activated, the game runs on the
    /// phone's PlayerPrefs alone. Its own assembly (Android + Editor) so Windows builds never see the plugin.
    /// </summary>
    public sealed class PlayGamesCloud : ICloudBackend
    {
        private const string FileName = "account";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (Application.isEditor) return;
            var settings = PlayGamesSettings.LoadInstance();
            if (settings == null || string.IsNullOrEmpty(settings.AppId)) return;
            PlayGamesPlatform.Activate();
            CloudSave.Backend = new PlayGamesCloud();
        }

        private static ISavedGameClient Client => PlayGamesPlatform.Instance.SavedGame;

        /// <summary>This install already showed Google's sign-in window once («cloud.» = a key of the phone, never synced).</summary>
        private const string AskedKey = "cloud.signin_asked";

        /// <summary>
        /// The silent sign-in first. When it fails (no Play Games profile yet, or Google wants the player to choose an
        /// account) Google's own sign-in window is shown -- once per install, so a player who declines is not asked at
        /// every launch (2026-11-26 phone test: a fresh install was never offered a sign-in and the cloud stayed empty).
        /// </summary>
        public void SignIn(Action<bool> done) =>
            PlayGamesPlatform.Instance.Authenticate(status =>
            {
                Debug.Log("PlayGamesCloud: silent sign-in: " + status);
                if (status == SignInStatus.Success) { done(true); return; }
                if (PlayerPrefs.GetInt(AskedKey, 0) == 1) { done(false); return; }
                PlayerPrefs.SetInt(AskedKey, 1);
                PlayerPrefs.Save();
                PlayGamesPlatform.Instance.ManuallyAuthenticate(manual =>
                {
                    Debug.Log("PlayGamesCloud: manual sign-in: " + manual);
                    done(manual == SignInStatus.Success);
                });
            });

        public void Load(Action<bool, byte[]> done) =>
            Open(DataSource.ReadNetworkOnly, (ok, meta) =>
            {
                if (!ok) { done(false, null); return; }
                Client.ReadBinaryData(meta, (status, data) => done(status == SavedGameRequestStatus.Success, data));
            });

        public void Save(byte[] data, TimeSpan played, Action<bool> done) =>
            Open(DataSource.ReadCacheOrNetwork, (ok, meta) =>
            {
                if (!ok) { done(false); return; }
                var update = new SavedGameMetadataUpdate.Builder()
                    .WithUpdatedPlayedTime(played)
                    .WithUpdatedDescription("CityCraft")
                    .Build();
                Client.CommitUpdate(meta, update, data, (status, _) =>
                {
                    if (status != SavedGameRequestStatus.Success) Debug.LogWarning("PlayGamesCloud: save failed: " + status);
                    done(status == SavedGameRequestStatus.Success);
                });
            });

        private static void Open(DataSource source, Action<bool, ISavedGameMetadata> done)
        {
            var client = Client;
            if (client == null) { done(false, null); return; }
            client.OpenWithAutomaticConflictResolution(FileName, source, ConflictResolutionStrategy.UseLongestPlaytime,
                (status, meta) =>
                {
                    if (status != SavedGameRequestStatus.Success) Debug.LogWarning("PlayGamesCloud: open failed: " + status);
                    done(status == SavedGameRequestStatus.Success && meta != null, meta);
                });
        }
    }
}
#endif
