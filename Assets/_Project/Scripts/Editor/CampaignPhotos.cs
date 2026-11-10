using System.IO;
using CityBuilder.Campaign;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// Photographs the opening cutscene's village for the user (run.ps1 -Steps campaign, with
    /// graphics): opens the scene, lays the village out in the editor and renders a few views to
    /// RunLogs/campaign/. The scene is not saved afterwards.
    /// </summary>
    public static class CampaignPhotos
    {
        private static readonly (string name, Vector3 at, Vector3 from, float fov)[] Views =
        {
            ("1_over_village", new Vector3(0f, 0f, 2f), new Vector3(-26f, 22f, -34f), 40f),
            ("2_square", new Vector3(0f, 0.6f, 1.5f), new Vector3(3.2f, 3.2f, -10f), 45f),
            ("3_market_street", new Vector3(1.2f, 0.6f, -4.5f), new Vector3(-0.6f, 1.6f, -11f), 45f),
            ("4_church", new Vector3(0f, 1.6f, 6.5f), new Vector3(-4.5f, 1.4f, -1.5f), 45f),
            ("5_far_fields", new Vector3(0f, 0f, 0f), new Vector3(60f, 30f, -55f), 40f),
            ("6_street_level", new Vector3(14f, 0.8f, 3f), new Vector3(5f, 1.1f, 0.6f), 50f),
        };

        public static void Capture()
        {
            EditorSceneManager.OpenScene(SetupProject.CampaignOpeningScenePath);
            var village = Object.FindAnyObjectByType<OpeningVillage>();
            village.Build();
            Debug.Log($"[CampaignPhotos] village: {village.BuildingCount} buildings");
            var cam = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "RunLogs", "campaign");
            Directory.CreateDirectory(dir);
            var rt = new RenderTexture(1280, 720, 24) { antiAliasing = 4 };
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            foreach (var (name, at, from, fov) in Views)
            {
                cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(at - from));
                cam.fieldOfView = fov;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
                RenderTexture.active = null;
                cam.targetTexture = null;
            }
            Debug.Log($"[CampaignPhotos] wrote {Views.Length} views to {dir}");
        }
    }
}
