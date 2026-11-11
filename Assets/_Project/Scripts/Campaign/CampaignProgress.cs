using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// What the player has done in the campaign, kept in PlayerPrefs: the levels finished (a level on
    /// the campaign map opens once the one before it is done) and whether the opening film has been
    /// seen (the user, 2026-11-12: the film plays before level 1 the first time; the map can replay it).
    /// </summary>
    public static class CampaignProgress
    {
        public const string FilmSeenKey = "campaign_film_seen";
        private const string DonePrefix = "campaign_done_";

        public static bool FilmSeen => PlayerPrefs.GetInt(FilmSeenKey, 0) == 1;

        public static bool Done(string levelId) => !string.IsNullOrEmpty(levelId) && PlayerPrefs.GetInt(DonePrefix + levelId, 0) == 1;

        public static void MarkDone(string levelId)
        {
            if (!string.IsNullOrEmpty(levelId)) PlayerPrefs.SetInt(DonePrefix + levelId, 1);
        }
    }
}
