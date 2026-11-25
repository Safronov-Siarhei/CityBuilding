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

        public void SignIn(Action<bool> done) =>
            PlayGamesPlatform.Instance.Authenticate(status =>
            {
                if (status != SignInStatus.Success) Debug.Log("PlayGamesCloud: not signed in: " + status);
                done(status == SignInStatus.Success);
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
