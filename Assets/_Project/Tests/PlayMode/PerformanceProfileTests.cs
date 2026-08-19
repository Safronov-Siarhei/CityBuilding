using System.Collections;
using System.Collections.Generic;
using System.Text;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Resources;
using CityBuilder.Saving;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// What a frame costs, measured rather than guessed -- the game targets phones and nobody has
    /// ever put a number on either half of the budget.
    ///
    /// Deliberately NOT a pass/fail test of one system. It grows a town in stages and measures each
    /// one, so the DELTA between two stages is the price of what was just added: the buildings, the
    /// citizens standing idle, the same citizens actually working, the fight, then twice the
    /// population. That is the only way to attribute cost without first instrumenting every
    /// Update() with a marker -- and it costs nothing in the shipped game, since the whole file
    /// lives in the test assembly.
    ///
    /// Three numbers, and they answer different questions:
    /// - **BehaviourUpdate ms** is the sum of every MonoBehaviour.Update in the frame. This is the
    ///   game's own code and nothing else -- no editor overhead, no render thread -- so the growth
    ///   between stages transfers to a device even though the absolute value does not.
    /// - **GC allocated per frame** is what decides how often a phone stops to collect. In an
    ///   editor run the baseline is dominated by the editor itself, so only the DELTA between
    ///   stages says anything about the game.
    /// - **Wall ms** is kept only as a sanity check. The first run of this file showed a flat
    ///   16,67 in every stage: FrameRateController caps the game at 60 and the wall clock was
    ///   measuring sleep. The cap is lifted below for exactly that reason.
    ///
    /// It asserts nothing about performance on purpose: a threshold picked before the first
    /// measurement is a guess wearing a test's clothes. Once these numbers are known a budget can be
    /// pinned here, and this becomes a regression guard.
    /// </summary>
    public class PerformanceProfileTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map1";

        /// <summary>Frames thrown away before each stage's samples: the frames right after a change carry its one-off cost (spawning, NavMesh carving, a Recompute), which is not what a per-frame budget is about.</summary>
        private const int WarmupFrames = 40;

        /// <summary>Enough samples that one hitch cannot move the median, few enough that six stages stay inside a normal suite run.</summary>
        private const int SampleFrames = 200;

        private const int TownBuildings = 30;
        private const int TownPopulation = 60;
        private const int CrowdedPopulation = 150;
        private const int Soldiers = 12;
        private const int Orcs = 8;

        private readonly List<Stage> _stages = new List<Stage>();

        private struct Stage
        {
            public string Label;
            public double UpdateMs;
            public double UpdateMsP95;
            public double WallMs;
            public double GcBytes;
            public int Behaviours;
        }

        [UnitySetUp]
        public IEnumerator PrepareScene()
        {
            // -nographics has no render target, so the minimap's RenderTexture logs an error the
            // runner would otherwise count as a failure.
            LogAssert.ignoreFailingMessages = true;

            Time.timeScale = 1f;
            GameSessionIntent.NewGameMapId = MapId;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);

            // FrameRateController pins the game to 60 in Awake, which is right for a phone and
            // useless for a measurement: every frame would sleep to fill 16,67ms and every stage
            // would look identical. Uncapped, a frame takes as long as the work in it.
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;

            ModalGate.SetBlocked(false);

            // A raid arriving mid-measurement would land in whichever stage it felt like.
            if (OrcRaidManager.Instance != null) OrcRaidManager.Instance.RaidsSuspended = true;

            // Research gates would otherwise decide which buildings can be staffed, which is not
            // what is being measured.
            CityBuilder.Research.ResearchManager.Instance?.CompleteEverything();

            var resources = ResourceManager.Instance;
            resources.SetAmount(ResourceType.Coins, 100000);
            resources.SetAmount(ResourceType.Food, 100000);
            yield return null;
        }

        [TearDown]
        public void RestoreFrameRate()
        {
            Time.timeScale = 1f;
            Application.targetFrameRate = 60;
        }

        [UnityTest]
        public IEnumerator ProfileAFrameStageByStage()
        {
            _stages.Clear();

            yield return Measure("1. bare map");

            var placed = PlaceTown(TownBuildings);
            yield return null;
            yield return Measure("2. + " + placed + " buildings");

            CitizenManager.Instance.SetPopulation(TownPopulation);
            yield return null;
            yield return Measure("3. + " + TownPopulation + " citizens (idle)");

            var staffed = StaffEveryWorkplace();
            yield return Measure("4. " + staffed + " of them working");

            var recruited = Recruit(Soldiers);
            SpawnOrcs(Orcs);
            yield return Measure("5. + " + recruited + " soldiers, " + Orcs + " orcs");

            CitizenManager.Instance.SetPopulation(CrowdedPopulation);
            yield return null;
            yield return Measure("6. population " + CrowdedPopulation);

            Debug.Log(BuildReport());

            Assert.Greater(placed, 0, "Nothing was built, so the stages measure nothing.");
        }

        private IEnumerator Measure(string label)
        {
            for (var i = 0; i < WarmupFrames; i++) yield return null;

            // "BehaviourUpdate" is the player loop's own marker around every MonoBehaviour.Update
            // in the frame -- the game's scripts and nothing else.
            var updateRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "BehaviourUpdate");
            // "GC Allocated In Frame" is the managed bytes handed out that frame -- the quantity
            // that decides how often a phone has to stop and collect.
            var gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");

            var updateMs = new List<double>(SampleFrames);
            var wallMs = new List<double>(SampleFrames);
            var gcBytes = new List<long>(SampleFrames);

            for (var i = 0; i < SampleFrames; i++)
            {
                var before = Time.realtimeSinceStartupAsDouble;
                yield return null;
                wallMs.Add((Time.realtimeSinceStartupAsDouble - before) * 1000.0);

                if (updateRecorder.Valid) updateMs.Add(updateRecorder.LastValue / 1_000_000.0);
                if (gcRecorder.Valid) gcBytes.Add(gcRecorder.LastValue);
            }

            updateRecorder.Dispose();
            gcRecorder.Dispose();

            updateMs.Sort();
            wallMs.Sort();
            gcBytes.Sort();

            _stages.Add(new Stage
            {
                Label = label,
                UpdateMs = Median(updateMs),
                UpdateMsP95 = Percentile(updateMs, 0.95),
                WallMs = Median(wallMs),
                GcBytes = MedianLong(gcBytes),
                Behaviours = ActiveBehaviourCount(),
            });
        }

        private string BuildReport()
        {
            var text = new StringBuilder();
            text.AppendLine();
            text.AppendLine("=== FRAME PROFILE (editor, -nographics, frame cap lifted) ===");
            text.AppendLine("stage                              | Update ms | p95 ms | d Update | wall ms |  GC B/frame |    d GC | behaviours");

            for (var i = 0; i < _stages.Count; i++)
            {
                var stage = _stages[i];
                var deltaUpdate = i == 0 ? 0.0 : stage.UpdateMs - _stages[i - 1].UpdateMs;
                var deltaGc = i == 0 ? 0.0 : stage.GcBytes - _stages[i - 1].GcBytes;

                text.AppendLine(string.Format(
                    "{0,-34} | {1,9:F3} | {2,6:F3} | {3,8:F3} | {4,7:F2} | {5,11:F0} | {6,7:F0} | {7,10}",
                    stage.Label, stage.UpdateMs, stage.UpdateMsP95, deltaUpdate, stage.WallMs, stage.GcBytes, deltaGc, stage.Behaviours));
            }

            text.AppendLine();
            text.AppendLine(DescribeScene());
            return text.ToString();
        }

        /// <summary>Builds a spread-out town out of whatever the catalogue offers, skipping water buildings -- they need a shore, which the free-area search does not look for.</summary>
        private static int PlaceTown(int wanted)
        {
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            if (placer == null) return 0;

            var catalogue = new List<BuildingData>();
            if (placer.MandatoryFirstBuilding != null) catalogue.Add(placer.MandatoryFirstBuilding);
            foreach (var data in placer.AvailableBuildings)
            {
                if (data == null || data.isWaterCategory || data.prefab == null) continue;
                catalogue.Add(data);
            }
            if (catalogue.Count == 0) return 0;

            var placed = 0;
            for (var i = 0; i < wanted; i++)
            {
                var data = catalogue[i % catalogue.Count];
                var cell = PlaytestWorld.FindFreeArea(data.footprintSize);
                if (cell.x < 0) continue;
                if (PlaytestWorld.Place(data, cell) != null) placed++;
            }
            return placed;
        }

        /// <summary>Fills every workplace that will take a worker. This is the stage that turns standing citizens into walking ones, which is the expensive half.</summary>
        private static int StaffEveryWorkplace()
        {
            var staffed = 0;
            foreach (var workplace in Object.FindObjectsByType<ProductionBuilding>(FindObjectsSortMode.None))
            {
                while (workplace.TryAssignWorker()) staffed++;
            }
            return staffed;
        }

        private static int Recruit(int wanted)
        {
            var army = ArmyManager.Instance;
            var grid = GridManager.Instance;
            if (army == null || grid == null) return 0;

            var spawn = PlaytestWorld.CellCenter(new Vector2Int(grid.GridSize.x / 2, grid.GridSize.y / 2));
            var recruited = 0;
            for (var i = 0; i < wanted; i++)
            {
                if (army.TryRecruit(SoldierType.Militia, spawn + new Vector3(i * 0.7f, 0f, 0f))) recruited++;
            }
            return recruited;
        }

        private static void SpawnOrcs(int count)
        {
            var raids = OrcRaidManager.Instance;
            var grid = GridManager.Instance;
            if (raids == null || grid == null) return;

            var origin = PlaytestWorld.CellCenter(new Vector2Int(grid.GridSize.x / 2 + 6, grid.GridSize.y / 2));
            raids.SpawnOrcs(origin, count, 1);
        }

        /// <summary>What is actually in the scene when the last stage was measured -- the denominator for every number above.</summary>
        private static string DescribeScene()
        {
            var byType = new Dictionary<string, int>();
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (behaviour == null || !behaviour.isActiveAndEnabled) continue;
                var typeName = behaviour.GetType().Name;
                byType.TryGetValue(typeName, out var count);
                byType[typeName] = count + 1;
            }

            var ranked = new List<KeyValuePair<string, int>>(byType);
            ranked.Sort((a, b) => b.Value.CompareTo(a.Value));

            var text = new StringBuilder();
            text.AppendLine("Most numerous active MonoBehaviours (the ones whose Update multiplies):");
            for (var i = 0; i < ranked.Count && i < 15; i++)
            {
                text.AppendLine("  " + ranked[i].Value + " x " + ranked[i].Key);
            }
            return text.ToString();
        }

        private static int ActiveBehaviourCount()
        {
            var active = 0;
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (behaviour != null && behaviour.isActiveAndEnabled) active++;
            }
            return active;
        }

        private static double Median(List<double> sorted)
        {
            return sorted.Count == 0 ? 0.0 : sorted[sorted.Count / 2];
        }

        private static double MedianLong(List<long> sorted)
        {
            return sorted.Count == 0 ? 0.0 : sorted[sorted.Count / 2];
        }

        private static double Percentile(List<double> sorted, double fraction)
        {
            if (sorted.Count == 0) return 0.0;
            var index = Mathf.Clamp(Mathf.FloorToInt((float)(sorted.Count * fraction)), 0, sorted.Count - 1);
            return sorted[index];
        }
    }
}
