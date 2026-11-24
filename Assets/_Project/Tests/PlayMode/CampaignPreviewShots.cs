using System.Collections;
using System.IO;
using System.Reflection;
using CityBuilder.Campaign;
using CityBuilder.Core;
using CityBuilder.Maps;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// NOT A TEST: a photo run for the campaign list's previews (the user, 2026-11-24: «хочу чтобы в рамочках были кадры
    /// из соответствующих сценок»). Plays mission 2's opening, the level-3 prologue and 3b «Клетка»'s opening and saves
    /// what the game camera sees every 1.5 s into RunLogs/previews/&lt;level&gt;/ for the user to pick from. Explicit: the
    /// general run skips it; run it by name with graphics:
    ///   run.ps1 -Steps playmode -Graphics -Filter CampaignPreviewShots
    /// </summary>
    [Explicit("a photo run, not a check")]
    public class CampaignPreviewShots
    {
        private const float Every = 1.5f, For = 48f;

        [UnityTearDown]
        public IEnumerator Reset()
        {
            Time.timeScale = 1f;
            CityDefenceMode.Pending = false;
            SetStatic("_filmOnly", false);
            yield return null;
        }

        [UnityTest] public IEnumerator Shoot_m2() => Shoot("m2", CityDefenceMode.Mission.Siege, CityDefenceMode.MapId, false);
        [UnityTest] public IEnumerator Shoot_m3p() => Shoot("m3p", CityDefenceMode.Mission.Siege, CityDefenceMode.MapId, true);
        [UnityTest] public IEnumerator Shoot_m3b() => Shoot("m3b", CityDefenceMode.Mission.Escort, CityDefenceMode.EscortMapId, false);

        private static void SetStatic(string name, object value)
        {
            var t = typeof(CityDefenceMode);
            var f = t.GetField(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (f != null) { f.SetValue(null, value); return; }
            t.GetProperty(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)?.SetValue(null, value);
        }

        private static IEnumerator Shoot(string level, CityDefenceMode.Mission kind, string map, bool filmOnly)
        {
            Time.timeScale = 1f;
            PlayerPrefs.DeleteKey(CityDefenceMode.Part2Key);
            SetStatic("_skipIntroOnce", false);
            SetStatic("_filmOnly", filmOnly);
            SetStatic("Kind", kind);
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = map;
            GameSessionIntent.NewGameSeason = MapSeason.Summer;
            GameSessionIntent.NewGameIsInvasion = false;
            CityDefenceMode.Pending = true;
            SceneManager.LoadScene(CityDefenceMode.GameplayScene, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(map);

            var dir = Path.Combine(Directory.GetCurrentDirectory(), "RunLogs", "previews", level);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            var rt = new RenderTexture(1280, 720, 24) { antiAliasing = 4 };
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var t = 0f;
            var next = 0.5f;
            var saved = 0;
            while (t < For)
            {
                yield return null;
                t += Time.unscaledDeltaTime;
                if (t < next) continue;
                next += Every;
                var cam = Camera.main;
                if (cam == null || SceneManager.GetActiveScene().name != CityDefenceMode.GameplayScene) break;   // the prologue went back to the map
                var was = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = was;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(dir, $"{t:00.0}s.jpg"), tex.EncodeToJPG(90));
                saved++;
            }
            Debug.Log($"[PreviewShots] {level}: {saved} frames to {dir}");
            Assert.Greater(saved, 0, $"No frames for {level}.");
        }
    }
}
