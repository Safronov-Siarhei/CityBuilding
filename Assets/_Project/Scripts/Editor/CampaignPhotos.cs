using System.IO;
using System.Linq;
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
            var rt = new RenderTexture(1920, 1080, 24) { antiAliasing = 4 };
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            // the first render skipped the ground while its shader still compiled in the background
            // (the first view showed the sky's lower half instead of the grass): compile up front
            // and throw a warm-up frame away
            var wasAsync = EditorSettings.asyncShaderCompilation;   // put back at the end: it is a project setting
            EditorSettings.asyncShaderCompilation = false;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            foreach (var (name, at, from, fov) in Views)
            {
                cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(at - from));
                cam.fieldOfView = fov;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
                RenderTexture.active = null;
                cam.targetTexture = null;
            }
            Debug.Log($"[CampaignPhotos] wrote {Views.Length} views to {dir}");

            // the living background, stepped by hand: calm, the panic, the fires
            var life = Object.FindAnyObjectByType<OpeningLife>();
            if (life != null)
            {
                life.Begin();
                Simulate(life, 12f);
                Shoot(cam, rt, tex, dir, "10_life_calm", new Vector3(0f, 0.3f, -1f), new Vector3(-7f, 7f, -11f), 45f);
                Shoot(cam, rt, tex, dir, "11_life_street", new Vector3(-1f, 0.5f, -16f), new Vector3(1.6f, 1.3f, -9.5f), 50f);
                life.Panic(Vector3.zero);
                life.ReleaseOrcs(new Vector3(0f, 0f, -0.95f));
                Simulate(life, 5f);
                Shoot(cam, rt, tex, dir, "12_life_panic", new Vector3(0f, 0.3f, -1f), new Vector3(-7f, 7f, -11f), 45f);
                var orc = life.FirstOrc;
                if (orc != null) Shoot(cam, rt, tex, dir, "12b_life_orc", orc.position + Vector3.up * 0.35f, orc.position + orc.forward * 2.2f + orc.right * 0.9f + Vector3.up * 0.8f, 45f);
                Simulate(life, 14f);
                Shoot(cam, rt, tex, dir, "13_life_fire", new Vector3(0f, 0f, 2f), new Vector3(-26f, 22f, -34f), 40f);
                if (life.FirstFire is Vector3 fire)
                {
                    var away = new Vector3(fire.x, 0f, fire.z).normalized;
                    Shoot(cam, rt, tex, dir, "14_life_house_fire", fire, fire + away * 6f + Vector3.up * 2.2f + Vector3.Cross(away, Vector3.up) * 2f, 45f);
                }
                Debug.Log($"[CampaignPhotos] life: {life.Villagers} villagers, {life.Fires / 2} fires");
            }
            EditorSettings.asyncShaderCompilation = wasAsync;
        }

        /// <summary>The whole opening film, frame by frame at 30 fps, as JPGs in RunLogs/campaign/film/
        /// (run.ps1 -Steps film; Tools/blender/cinematic/encode_frames.py joins them into an mp4).</summary>
        public static void Film()
        {
            EditorSceneManager.OpenScene(SetupProject.CampaignOpeningScenePath);
            var village = Object.FindAnyObjectByType<OpeningVillage>();
            village.Build();
            var director = Object.FindAnyObjectByType<OpeningDirector>();
            var cam = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "RunLogs", "campaign", "film");
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            var wasAsync = EditorSettings.asyncShaderCompilation;
            EditorSettings.asyncShaderCompilation = false;
            var rt = new RenderTexture(1920, 1080, 24) { antiAliasing = 4 };
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            director.Play();
            cam.targetTexture = rt;
            cam.Render();                                   // warm-up: shaders compiled before the first frame
            var frames = Mathf.CeilToInt(director.Length * 30f);
            var shots = new System.Text.StringBuilder();
            var lastShot = "";
            for (var f = 0; f < frames; f++)
            {
                if (f > 0) director.Step(1f / 30f);
                if (director.ShotName != lastShot)
                {
                    lastShot = director.ShotName;
                    shots.Append($"{f}:{lastShot} ");
                }
                Canvas.ForceUpdateCanvases();
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(dir, $"{f:D5}.jpg"), tex.EncodeToJPG(95));
                RenderTexture.active = null;
            }
            cam.targetTexture = null;
            EditorSettings.asyncShaderCompilation = wasAsync;
            Debug.Log($"[CampaignFilm] {frames} frames to {dir}; shots {shots}");
        }

        /// <summary>
        /// Mission 1's handover and controls (run.ps1 -Steps mission): the film run to its end unseen,
        /// then the stick steered by a script -- up the market street into the houses, then round --
        /// with Ардан's position logged each second and a few frames of the game's view saved to
        /// RunLogs/campaign/mission/.
        /// </summary>
        public static void Mission()
        {
            EditorSceneManager.OpenScene(SetupProject.CampaignOpeningScenePath);
            var village = Object.FindAnyObjectByType<OpeningVillage>();
            village.Build();
            var director = Object.FindAnyObjectByType<OpeningDirector>();
            var mission = Object.FindAnyObjectByType<MissionOne>();
            var cam = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "RunLogs", "campaign", "mission");
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            var wasAsync = EditorSettings.asyncShaderCompilation;
            EditorSettings.asyncShaderCompilation = false;
            var rt = new RenderTexture(1280, 720, 24) { antiAliasing = 4 };
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            director.Play();
            const float dt = 1f / 30f;
            while (director.Playing) director.Step(dt);
            mission.Begin();
            Physics.SyncTransforms();
            // the stick steered down the route's track, as a player would run it: towards the next
            // point, round whatever stops him (the fallen tree), a frame at each landmark
            var path = village.RoutePath;
            var shots = new System.Collections.Generic.Dictionary<int, string> { { 5, "field" }, { 9, "fallen_tree" }, { 13, "forest" }, { 14, "bridge_near" }, { 16, "bridge_over" }, { 17, "gate" } };
            var log = new System.Text.StringBuilder();
            var t = 0f;
            var nextLog = 0f;
            var wp = 1;
            var lastPos = mission.Ardan.position;
            var stuckFor = 0f;
            var dodge = 0f;
            var dodgeSide = 1f;
            var stuckShots = 0;
            var yaw = 180f * Mathf.Deg2Rad;
            var up = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
            var right = new Vector3(Mathf.Cos(yaw), 0f, -Mathf.Sin(yaw));
            void Shoot(string name)
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(dir, $"{name}_{t:F0}s.jpg"), tex.EncodeToJPG(90));
                RenderTexture.active = null;
                cam.targetTexture = null;
            }
            // the mage's scene: frames at its beats, by its own clock
            var mageShots = new[] { (1.2f, "mage_wave"), (3.8f, "mage_strike"), (4.45f, "mage_orb"), (5.3f, "mage_fall"), (6.4f, "mage_drift"), (9.5f, "mage_hold") };
            var mageNext = 0;
            while (path != null && wp < path.Count && t < 420f)
            {
                if (mission.Mage != null && mageNext < mageShots.Length && mission.Mage.T >= mageShots[mageNext].Item1)
                {
                    Shoot(mageShots[mageNext++].Item2);
                    log.Append($"[{mageShots[mageNext - 1].Item2} at ({mission.Ardan.position.x:F1},{mission.Ardan.position.z:F1})] ");
                }
                var pos = mission.Ardan.position;
                var to = new Vector3(path[wp].x - pos.x, 0f, path[wp].y - pos.z);
                if (to.magnitude < 1.0f)
                {
                    if (shots.TryGetValue(wp, out var shot)) Shoot(shot);
                    wp++;
                    continue;
                }
                var want = to.normalized;
                if (dodge > 0f)
                {
                    want = Vector3.Cross(Vector3.up, want) * dodgeSide;  // sidestep round what blocks him
                    dodge -= dt;
                }
                var stick = new Vector2(Vector3.Dot(want, right), Vector3.Dot(want, up));
                director.Step(dt);
                mission.Step(dt, stick);
                Physics.SyncTransforms();
                mission.StepCamera(dt);
                t += dt;
                var moved = (mission.Ardan.position - lastPos).magnitude;
                lastPos = mission.Ardan.position;
                stuckFor = moved < 0.2f * dt ? stuckFor + dt : 0f;
                if (stuckFor > 0.6f)
                {
                    dodge = 2.2f;
                    dodgeSide = -dodgeSide;                              // try the other way next time
                    stuckFor = 0f;
                    if (stuckShots++ == 0) Shoot("stuck");
                    log.Append($"[stuck@({lastPos.x:F1},{lastPos.z:F1})] ");
                }
                if (t >= nextLog)
                {
                    log.Append($"t{t:F0}=({lastPos.x:F1},{lastPos.z:F1}) [{mission.Chase?.Report()}] ");
                    nextLog += 5f;
                }
            }
            log.Append($"END t{t:F0} wp{wp}/{path?.Count} caught {mission.Chase?.Catches}");
            // then back north over the bridge: the mage brought its middle down, so he must stop at
            // the gap, and nothing may catch him
            var before = mission.Chase?.Catches ?? 0;
            for (var back = 0f; back < 70f; back += dt)
            {
                var pos = mission.Ardan.position;
                var want = new Vector3(-4f - pos.x, 0f, -176f - pos.z).normalized;
                director.Step(dt);
                mission.Step(dt, new Vector2(Vector3.Dot(want, right), Vector3.Dot(want, up)));
                Physics.SyncTransforms();
                mission.StepCamera(dt);
                t += dt;
            }
            for (var s2 = 0f; s2 < 3f; s2 += dt)
            {
                director.Step(dt);
                mission.Step(dt, Vector2.zero);
                Physics.SyncTransforms();
                mission.StepCamera(dt);
                t += dt;
            }
            var at2 = mission.Ardan.position;
            log.Append($" | BACK caught {mission.Chase?.Catches - before} more, stopped at ({at2.x:F1},{at2.z:F1}) mage t {mission.Mage?.T:F1}");
            // and into the town: the guards close behind him, black, the title, the summary
            var endShots = new[] { (1.2f, "end_guards"), (3.4f, "end_title"), (5.0f, "end_summary") };
            var endNext = 0;
            for (var e = 0f; e < 150f && (mission.End == null || mission.End.T < 5.5f); e += dt)
            {
                var pos = mission.Ardan.position;
                var target = e < 0f ? pos : (pos.z > -235f ? new Vector3(-4f, 0f, -236f) : new Vector3(-4f, 0f, -243f));
                var want = (target - pos).normalized;
                if (mission.End != null && endNext < endShots.Length && mission.End.T >= endShots[endNext].Item1) Shoot(endShots[endNext++].Item2);
                director.Step(dt);
                mission.Step(dt, new Vector2(Vector3.Dot(want, right), Vector3.Dot(want, up)));
                Physics.SyncTransforms();
                mission.StepCamera(dt);
                t += dt;
            }
            log.Append($" | END {(mission.End != null && mission.End.Started ? "reached" : "NOT reached")} t {mission.End?.T:F1} summary [{mission.End?.Report}]");
            EditorSettings.asyncShaderCompilation = wasAsync;
            Debug.Log($"[Mission1] path {log}");
        }

        private static void Simulate(OpeningLife life, float seconds)
        {
            for (var t = 0f; t < seconds; t += 1f / 30f) life.Step(1f / 30f);
        }

        private static void Shoot(Camera cam, RenderTexture rt, Texture2D tex, string dir, string name, Vector3 at, Vector3 from, float fov)
        {
            cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(at - from));
            cam.fieldOfView = fov;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
        }
    }
}