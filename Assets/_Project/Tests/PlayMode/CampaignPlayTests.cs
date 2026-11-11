using System.Collections;
using System.IO;
using System.Linq;
using CityBuilder.Campaign;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The opening film in real Play mode (the user, 2026-11-11: an empty village -- no villagers,
    /// no houses -- that the batchmode photo checks, run in edit mode, never showed: edit mode reads
    /// meshes Play mode may not). The village must have its houses and streets, and the film's
    /// figures their cut parts. A frame goes to RunLogs/campaign/play_probe.jpg.
    /// </summary>
    public class CampaignPlayTests
    {
        [UnityTest]
        public IEnumerator TheOpeningFilm_InPlayMode_HasItsVillageAndItsPeople()
        {
            SceneManager.LoadScene("CampaignOpening", LoadSceneMode.Single);
            for (var t = 0f; t < 3f; t += Time.unscaledDeltaTime) yield return null;
            var village = Object.FindAnyObjectByType<OpeningVillage>();
            Assert.IsNotNull(village, "no village in the scene");
            var root = village.transform;
            var names = root.GetComponentsInChildren<Transform>(true).Select(x => x.name).ToList();
            var streets = names.Count(n => n == "Street");
            var renderers = village.GetComponentsInChildren<MeshRenderer>(true).Length;
            var figures = Object.FindObjectsByType<CineFigure>(FindObjectsSortMode.None);
            var empty = figures.Count(f => f.GetComponentsInChildren<MeshFilter>(true).Length == 0);
            var villages = names.Count(n => n == "Village");
            Debug.Log($"[CampaignPlay] village roots {villages} buildings {village.BuildingCount} streets {streets} renderers {renderers} figures {figures.Length} empty {empty}");
            var cam = Camera.main;
            if (cam != null)
            {
                var rt = new RenderTexture(1280, 720, 24);
                var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex.Apply();
                var dir = Path.Combine(Directory.GetCurrentDirectory(), "RunLogs", "campaign");
                Directory.CreateDirectory(dir);
                File.WriteAllBytes(Path.Combine(dir, "play_probe.jpg"), tex.EncodeToJPG(85));
                RenderTexture.active = null;
                cam.targetTexture = null;
            }
            Assert.Greater(village.BuildingCount, 60, "the village's houses are missing (91 when whole; 9 when the curved streets broke the walk along them)");
            Assert.Greater(streets, 5, "the village's streets are missing");
            Assert.AreEqual(0, empty, "film figures came out with no parts (unreadable meshes?)");
        }
    }
}
