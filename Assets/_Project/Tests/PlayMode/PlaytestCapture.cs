using System.Collections;
using System.IO;
using UnityEngine;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// Photographs the running game from a test and drops the PNG in `playtest_shots/` next to the
    /// project.
    ///
    /// Why a test suite takes photographs: three separate features in this project passed every
    /// assertion and were still wrong on screen, because what was broken was geometry -- an
    /// invisible slab over the forest, a model standing beside the cell it belongs to. Numbers can
    /// only be checked against numbers somebody thought to write down; a picture can be looked at.
    /// These shots are not asserted on, they are for a human (or a model) to open.
    ///
    /// The camera is left to the pipeline's own frame rather than driven by hand: Camera.Render()
    /// is not honoured under a scriptable render pipeline, and WaitForEndOfFrame never fires in
    /// batchmode -- it would hang the run rather than skip a photograph. Under -nographics there is
    /// no device to render with, so the whole thing turns itself off and the tests carry on --
    /// capture is a bonus, never a reason for a run to fail.
    /// </summary>
    public static class PlaytestCapture
    {
        public static bool Available => SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;

        private const int Width = 1600;
        private const int Height = 900;

        public static string OutputFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "playtest_shots"));

        /// <summary>Looks at `focus` from `distance` metres away, down `pitch` degrees, turned `yaw` degrees -- the same over-the-shoulder angle the game's own camera uses when pointed at something. `fieldOfView` defaults to a tighter lens than the game's own 60; pass 60 when the shot has to show exactly what the player would see from that distance.</summary>
        public static IEnumerator Shoot(string shotName, Vector3 focus, float distance = 14f, float pitch = 40f, float yaw = 30f, float fieldOfView = 45f)
        {
            return Shoot(shotName, focus, distance, pitch, yaw, fieldOfView, includeUi: false);
        }

        /// <summary>
        /// The same photograph with the interface on it. A screen-space overlay canvas is drawn
        /// straight to the screen, never into a camera's texture, so for the length of the shot
        /// every overlay canvas is switched to render through the photographing camera instead,
        /// then put back exactly as it was.
        /// </summary>
        public static IEnumerator ShootWithUi(string shotName, Vector3 focus, float distance = 40f, float pitch = 50f, float yaw = 25f)
        {
            return Shoot(shotName, focus, distance, pitch, yaw, 60f, includeUi: true);
        }

        private static IEnumerator Shoot(string shotName, Vector3 focus, float distance, float pitch, float yaw, float fieldOfView, bool includeUi)
        {
            if (!Available)
            {
                Debug.Log($"[Playtest] No graphics device -- skipping the '{shotName}' screenshot. Re-run without -nographics to get one.");
                yield break;
            }

            var cameraObject = new GameObject($"PlaytestCamera_{shotName}");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            camera.transform.position = focus - camera.transform.forward * distance;
            camera.fieldOfView = fieldOfView;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;

            var texture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            camera.targetTexture = texture;

            var switched = new System.Collections.Generic.List<Canvas>();
            if (includeUi)
            {
                foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 1f;
                    switched.Add(canvas);
                }
                // One more frame for the layout to settle at the texture's size.
                yield return null;
            }

            // An enabled camera with a target texture is drawn by the pipeline every frame like any
            // other, so two plain frames are enough to have one in the texture: one for the scene to
            // settle (anything placed a moment ago may still be waiting on its own Start), one to
            // render. No WaitForEndOfFrame -- see the class summary.
            yield return null;
            yield return null;

            Write(shotName, texture);

            foreach (var canvas in switched)
            {
                if (canvas == null) continue;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
            }

            camera.targetTexture = null;
            texture.Release();
            Object.Destroy(texture);
            Object.Destroy(cameraObject);
        }

        private static void Write(string shotName, RenderTexture texture)
        {
            try
            {
                var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
                var previous = RenderTexture.active;
                RenderTexture.active = texture;
                image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                image.Apply();
                RenderTexture.active = previous;

                Directory.CreateDirectory(OutputFolder);
                var path = Path.Combine(OutputFolder, $"{shotName}.png");
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.Destroy(image);

                Debug.Log($"[Playtest] Screenshot written: {path}");
            }
            catch (System.Exception e)
            {
                // A missing photograph is worth a line in the log and nothing more -- these tests
                // assert on geometry, not on pixels.
                Debug.Log($"[Playtest] Could not take the '{shotName}' screenshot: {e.Message}");
            }
        }
    }
}
