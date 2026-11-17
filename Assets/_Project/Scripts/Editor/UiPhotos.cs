using System.IO;
using CityBuilder.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// A picture of a game window for the user (run.ps1 -Steps uishot, needs graphics): opens the
    /// game scene unsaved, opens the window and renders the HUD canvas through a camera into
    /// RunLogs/ui/&lt;name&gt;.png. The scene is never saved.
    /// </summary>
    public static class UiPhotos
    {
        public static void Diplomacy()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/CityBuilder.unity");
            DiplomacyWindow window = null;
            foreach (var w in UnityEngine.Resources.FindObjectsOfTypeAll<DiplomacyWindow>())
                if (w.gameObject.scene.IsValid()) window = w;
            if (window == null)
            {
                Debug.LogError("[UiPhotos] no DiplomacyWindow in the scene");
                return;
            }
            window.Show(null);
            Shoot(window.GetComponentInParent<Canvas>().rootCanvas, "diplomacy");
        }

        private static void Shoot(Canvas canvas, string name)
        {
            const int w = 1920, h = 1080;
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "RunLogs", "ui");
            Directory.CreateDirectory(dir);
            var camGo = new GameObject("UiPhotoCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.32f, 0.42f, 0.30f);
            cam.cullingMask = ~0;
            cam.transform.position = new Vector3(0f, -5000f, 0f);   // far below the world: only the canvas in view
            var rt = new RenderTexture(w, h, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            for (var i = 0; i < 3; i++)
            {
                Canvas.ForceUpdateCanvases();
                cam.Render();
            }
            var rect = (RectTransform)canvas.transform;
            var shown = 0;
            foreach (var g in canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>(false)) shown++;
            Debug.Log($"[UiPhotos] canvas {rect.rect.size} layer {canvas.gameObject.layer} scale {rect.localScale} active graphics {shown}");
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            var path = Path.Combine(dir, name + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log($"[UiPhotos] {path}");
        }
    }
}
