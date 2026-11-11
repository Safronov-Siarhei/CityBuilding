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

        /// <summary>
        /// «Ещё раз» / «Начать заново» (the user's second playtest, 2026-11-11: after a restart the
        /// world stayed very dark): the mission entered with the film skipped, through the loading
        /// screen as the game does it, must be as light as one entered after the film.
        /// </summary>
        [UnityTest]
        public IEnumerator TheMission_AfterARestart_IsAsLightAsTheFirstTime()
        {
            SceneManager.LoadScene("CampaignOpening", LoadSceneMode.Single);
            yield return null;
            Time.timeScale = 8f;
            for (var t = 0f; t < 40f && !Running(); t += Time.unscaledDeltaTime) yield return null;
            Time.timeScale = 1f;
            for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(Running(), "the mission never began after the film");
            var first = Measure("first");

            MissionOne.SkipFilm = true;
            CityBuilder.UI.LoadingScreen.IntoGame("CampaignOpening");
            for (var t = 0f; t < 60f && (CityBuilder.UI.LoadingScreen.Busy || !Running()); t += Time.unscaledDeltaTime) yield return null;
            for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(Running(), "the mission never began after the restart");
            var again = Measure("restart");
            Assert.Greater(again.light, first.light * 0.8f, "the restarted mission is much darker than the first");
            Assert.Less(again.black, 0.05f, "a black/red overlay is left over the restarted mission");
        }

        private static bool Running()
        {
            var m = Object.FindAnyObjectByType<MissionOne>();
            return m != null && m.Running;
        }

        /// <summary>The frame's mean brightness (the camera alone) and the mission overlay's alpha.</summary>
        private static (float light, float black) Measure(string label)
        {
            var cam = Camera.main;
            var rt = new RenderTexture(320, 180, 24);
            var tex = new Texture2D(320, 180, TextureFormat.RGB24, false);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 320, 180), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            var sum = 0f;
            foreach (var c in tex.GetPixels()) sum += c.grayscale;
            var light = sum / (320 * 180);
            var black = 0f;
            foreach (var img in Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsSortMode.None))
                if (img.name == "Black" && img.isActiveAndEnabled) black = Mathf.Max(black, img.color.a);
            var sun = RenderSettings.sun != null ? RenderSettings.sun.intensity : -1f;
            var dark = GameObject.Find("CorridorDarkness");
            var cams = string.Join(",", System.Array.ConvertAll(Camera.allCameras, c => $"{c.name}:{c.tag}:{c.depth}"));
            var urp = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam);
            if (dark != null)
            {
                var m = dark.GetComponent<Renderer>().sharedMaterial;
                var world = Object.FindAnyObjectByType<MissionOne>().World;
                var mask = world != null ? world.Mask : null;
                Debug.Log($"[CampaignPlay] {label}: same material {(world != null && world.DarkMaterial == m)}");
                var rect = m.GetVector("_DarkRect");
                var centre = mask != null ? mask.GetPixelBilinear((0f - rect.x) * rect.z, (0f - rect.y) * rect.w).r : -1f;
                Debug.Log($"[CampaignPlay] {label}: dark rect {rect} mask {(mask != null ? mask.width + "x" + mask.height : "none")} at square {centre:F2} strength {m.GetFloat("_Strength")} queue {m.renderQueue} quad at {dark.transform.localPosition} near {cam.nearClipPlane}");
            }
            Debug.Log($"[CampaignPlay] {label}: main {cam.name} cams [{cams}] darkness under {(dark != null ? dark.transform.parent.name : "none")} depthTex {urp.requiresDepthTexture} volumeProfile {Object.FindAnyObjectByType<UnityEngine.Rendering.Volume>()?.sharedProfile?.name}");
            Debug.Log($"[CampaignPlay] {label}: light {light:F3} overlay {black:F2} sun {sun:F2} ambient {RenderSettings.ambientSkyColor} fog {RenderSettings.fogColor} timeScale {Time.timeScale}");
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "RunLogs", "campaign");
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, $"play_{label}.jpg"), tex.EncodeToJPG(80));
            return (light, black);
        }
    }
}
