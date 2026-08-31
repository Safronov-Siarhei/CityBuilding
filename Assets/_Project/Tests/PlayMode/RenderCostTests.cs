using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Resources;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// What the scene costs to DRAW, counted off the live scene rather than off the assets.
    ///
    /// The frame's CPU side has been measured since 2026-08-19 (PerformanceProfileTests) and the
    /// answer was "walking citizens, nothing else". The GPU side has only ever been ESTIMATED --
    /// the 2026-08-29 audit counted renderers by reading spawner code and multiplying, which is
    /// how "roughly 2 000-2 500 renderers" got into the notes. Nobody has ever asked the running
    /// game.
    ///
    /// This does, and it does it in a way `-nographics` cannot invalidate: it counts SUBMESH
    /// DRAWS, not GPU time. One renderer with two materials is two draws whether or not there is
    /// a graphics device attached, so the number is the same headless as on a phone. What it
    /// cannot tell you is milliseconds -- for that the build has to run on the device.
    ///
    /// Three numbers, and they answer different questions:
    /// - **draws** -- submesh draws in the forward pass. This is the number a mobile GPU driver
    ///   pays per frame, and the one every batching technique exists to cut.
    /// - **shadow draws** -- the same census over everything still casting. The 2026-08-30 work
    ///   took the scenery out of this pass; the column is here so a regression is visible as a
    ///   number rather than as a frame rate.
    /// - **distinct (mesh, material) pairs** -- the FLOOR. If every draw sharing a mesh and a
    ///   material were merged into one call, this is what would be left. `draws` minus this is
    ///   exactly what batching has to win, and no technique can go below it.
    ///
    /// It asserts nothing about performance, for the same reason PerformanceProfileTests does not:
    /// a threshold picked before the first measurement is a guess wearing a test's clothes.
    /// </summary>
    public class RenderCostTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map1";

        private const int TownBuildings = 30;
        private const int TownPopulation = 60;

        private readonly List<Census> _stages = new List<Census>();

        private struct Census
        {
            public string Label;
            public int Renderers;
            public int Draws;
            public int ShadowDraws;
            public int MeshMaterialPairs;
            public int Materials;
            public int InstancedMaterials;
            public long Triangles;
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

            ModalGate.SetBlocked(false);

            // A raid arriving mid-census would put orcs in whichever stage it felt like.
            if (CityBuilder.Combat.OrcRaidManager.Instance != null)
                CityBuilder.Combat.OrcRaidManager.Instance.RaidsSuspended = true;

            CityBuilder.Research.ResearchManager.Instance?.CompleteEverything();

            var resources = ResourceManager.Instance;
            resources.SetAmount(ResourceType.Coins, 100000);
            resources.SetAmount(ResourceType.Food, 100000);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CountTheDrawsStageByStage()
        {
            _stages.Clear();

            _stages.Add(Take("1. bare map (trees, rocks, fog)"));

            var placed = PlaceTown(TownBuildings);
            yield return null;
            _stages.Add(Take("2. + " + placed + " buildings"));

            CitizenManager.Instance.SetPopulation(TownPopulation);
            yield return null;
            yield return null;
            _stages.Add(Take("3. + " + TownPopulation + " citizens"));

            Debug.Log(BuildReport());

            Assert.Greater(placed, 0, "Nothing was built, so the stages measure nothing.");
        }

        /// <summary>
        /// Walks every renderer that would actually be submitted and adds up its submesh draws.
        ///
        /// Disabled renderers and inactive objects are excluded deliberately: the authored
        /// placement zones ship with their renderers switched off (MeshMapApplier), and counting
        /// them would inflate every stage by geometry the player never sees. Missing materials
        /// (a null slot) are counted too -- Unity still issues that draw, with the error shader.
        /// </summary>
        private static Census Take(string label)
        {
            var census = new Census { Label = label };
            // Keyed on the objects themselves rather than on instance ids: GetInstanceID is an
            // outright compile ERROR in 6000.5, and reference identity is what grouping wants
            // anyway -- two renderers share a batch exactly when they point at the same mesh and
            // the same material asset.
            var pairs = new HashSet<(Mesh mesh, Material material, int submesh)>();
            var materials = new HashSet<Material>();
            var instanced = new HashSet<Material>();

            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
            {
                if (!renderer.enabled) continue;

                var slots = renderer.sharedMaterials;
                if (slots == null || slots.Length == 0) continue;

                census.Renderers++;
                census.Draws += slots.Length;
                if (renderer.shadowCastingMode != ShadowCastingMode.Off) census.ShadowDraws += slots.Length;

                var mesh = MeshOf(renderer);
                if (mesh != null)
                {
                    for (var i = 0; i < mesh.subMeshCount && i < slots.Length; i++)
                        census.Triangles += mesh.GetIndexCount(i) / 3;
                }

                for (var i = 0; i < slots.Length; i++)
                {
                    var material = slots[i];
                    pairs.Add((mesh, material, i));

                    if (material == null) continue;
                    materials.Add(material);
                    if (material.enableInstancing) instanced.Add(material);
                }
            }

            census.MeshMaterialPairs = pairs.Count;
            census.Materials = materials.Count;
            census.InstancedMaterials = instanced.Count;
            return census;
        }

        /// <summary>The mesh a renderer draws, for the two kinds this game has. A particle or line renderer answers null, and is still counted as a draw -- it just cannot be grouped.</summary>
        private static Mesh MeshOf(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned) return skinned.sharedMesh;
            var filter = renderer.GetComponent<MeshFilter>();
            return filter == null ? null : filter.sharedMesh;
        }

        private string BuildReport()
        {
            var text = new StringBuilder();
            text.AppendLine();
            text.AppendLine("=== DRAW CENSUS (live scene, submesh draws -- not GPU time) ===");
            text.AppendLine("stage                              | renderers |    draws |  d draws | shadow | mesh+mat floor | materials | instancing on |    tris");

            for (var i = 0; i < _stages.Count; i++)
            {
                var stage = _stages[i];
                var delta = i == 0 ? 0 : stage.Draws - _stages[i - 1].Draws;

                text.AppendLine(string.Format(
                    "{0,-34} | {1,9} | {2,8} | {3,8} | {4,6} | {5,14} | {6,9} | {7,13} | {8,7}",
                    stage.Label, stage.Renderers, stage.Draws, delta, stage.ShadowDraws,
                    stage.MeshMaterialPairs, stage.Materials, stage.InstancedMaterials, stage.Triangles));
            }

            text.AppendLine();
            text.AppendLine(DescribeWorstOffenders());
            return text.ToString();
        }

        /// <summary>
        /// The (mesh, material) groups with the most copies of themselves in the scene, largest
        /// first. This is the list any batching work should be aimed at, in order -- a group of
        /// 600 is worth all the attention a group of 4 is not.
        /// </summary>
        private static string DescribeWorstOffenders()
        {
            var groups = new Dictionary<(Mesh, Material), (string name, int count)>();

            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
            {
                if (!renderer.enabled) continue;
                var slots = renderer.sharedMaterials;
                if (slots == null) continue;

                var mesh = MeshOf(renderer);
                if (mesh == null) continue;

                foreach (var material in slots)
                {
                    if (material == null) continue;
                    var key = (mesh, material);
                    var label = mesh.name + " / " + material.name;
                    if (groups.TryGetValue(key, out var entry)) groups[key] = (entry.name, entry.count + 1);
                    else groups[key] = (label, 1);
                }
            }

            var text = new StringBuilder();
            text.AppendLine("--- the ten biggest (mesh, material) groups: each is one call today, one call TOTAL if batched ---");
            foreach (var entry in groups.Values.OrderByDescending(g => g.count).Take(10))
                text.AppendLine(string.Format("{0,6} x  {1}", entry.count, entry.name));
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
    }
}
