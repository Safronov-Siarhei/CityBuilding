using System.Collections.Generic;
using System.IO;
using CityBuilder.Buildings;
using CityBuilder.CameraControl;
using CityBuilder.Cheats;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Resources;
using CityBuilder.Saving;
using CityBuilder.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// Maps: MeshMapDefinition assets from the exported Blender maps, their spots and decor, the
    /// ground material and the forest border.
    /// </summary>
    public static partial class SetupProject
    {
        private static void CreateForestBorder(float innerHalfWidth, float innerHalfDepth, float outerHalfWidth, float outerHalfDepth, Material trunkMaterial, Material canopyMaterial)
        {
            var root = new GameObject("Forest");

            // Fixed seed so re-running the setup script produces the same layout every time.
            Random.InitState(12345);
            const int treeCount = 160;
            var placed = 0;
            var attempts = 0;

            while (placed < treeCount && attempts < treeCount * 10)
            {
                attempts++;
                var x = Random.Range(-outerHalfWidth, outerHalfWidth);
                var z = Random.Range(-outerHalfDepth, outerHalfDepth);
                if (Mathf.Abs(x) < innerHalfWidth && Mathf.Abs(z) < innerHalfDepth) continue; // inside the buildable area

                var scale = Random.Range(0.8f, 1.3f);
                var trunkHeight = 1f * scale;
                var canopySize = 1.6f * scale;
                var canopyHeight = 1.4f * scale;

                var treeRoot = new GameObject("Tree");
                treeRoot.transform.position = new Vector3(x, 0f, z);
                treeRoot.transform.SetParent(root.transform, true);

                // Fully cubic tree: a box trunk with a box canopy on top — no rounded primitives.
                var trunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trunk.name = "Trunk";
                Object.DestroyImmediate(trunk.GetComponent<BoxCollider>());
                trunk.transform.SetParent(treeRoot.transform, false);
                trunk.transform.localScale = new Vector3(0.35f * scale, trunkHeight, 0.35f * scale);
                trunk.transform.localPosition = new Vector3(0f, trunkHeight * 0.5f, 0f);
                trunk.GetComponent<Renderer>().sharedMaterial = trunkMaterial;

                var canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
                canopy.name = "Canopy";
                Object.DestroyImmediate(canopy.GetComponent<BoxCollider>());
                canopy.transform.SetParent(treeRoot.transform, false);
                canopy.transform.localScale = new Vector3(canopySize, canopyHeight, canopySize);
                canopy.transform.localPosition = new Vector3(0f, trunkHeight + canopyHeight * 0.5f, 0f);
                canopy.GetComponent<Renderer>().sharedMaterial = canopyMaterial;

                placed++;
            }
        }

        /// <summary>
        /// Loads a hand-authored map's FBX prefabs by their fixed Models/ paths and writes a
        /// MeshMapDefinition asset under Resources/MeshMaps so MeshMapCatalog/MapSelector can find
        /// it. Parameterized rather than folder-scanning like the old MapImporter — a second map
        /// is just one more call to this with a different id/paths, not a new system.
        /// </summary>
        /// <param name="forcedSeason">A season the map always wears (the New Year map's winter).</param>
        /// <param name="treeNames">Only these tree models (Tree2, ...) for its forest; null for all of them.</param>
        private static void CreateMeshMapDefinition(string mapId, string prefix, Material waterMaterial,
            MapSeason? forcedSeason = null, string[] treeNames = null)
        {
            var folder = $"Assets/_Project/Models/{mapId}";
            var ground = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{prefix}-Ground.fbx");
            var water = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{prefix}-Water.fbx");
            var waterPlacementZone = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{prefix}-PlaceForWaterObjects.fbx");
            var treesArea = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{prefix}-TreesArea.fbx");
            // Every Tree*.fbx variant, so TreesAreaSpawner has the full set to pick from at random
            // (see SpawnOneTree). Adding a sixth model is one more file plus a bump of
            // TreeModelCount -- nothing here is written per-model.
            var trees = new GameObject[TreeModelCount];
            var treesMissing = false;
            for (var i = 0; i < TreeModelCount; i++)
            {
                trees[i] = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsTerrainFolder}/Tree{i + 1}.fbx");
                if (trees[i] == null) treesMissing = true;
            }

            // The hand-gathered boulders, Stone1 (pebbles) to Stone10 (an outcrop). Optional: with
            // none, RockSpawner falls back to its cube clusters rather than the map losing its stone.
            var rocks = new List<GameObject>();
            for (var i = 0; i < StoneModelCount; i++)
            {
                var rock = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsTerrainFolder}/Stone{i + 1}.fbx");
                if (rock != null) rocks.Add(rock);
                else Debug.LogWarning($"CreateMeshMapDefinition: {ModelsTerrainFolder}/Stone{i + 1}.fbx is missing -- the boulders will be drawn from the models that exist.");
            }

            if (ground == null || water == null || waterPlacementZone == null || treesArea == null || treesMissing)
            {
                Debug.LogWarning($"CreateMeshMapDefinition: one or more {mapId} FBX assets not found under {folder}/{ModelsTerrainFolder} -- skipping the {mapId} MeshMapDefinition.");
                return;
            }

            if (treeNames != null) trees = System.Array.FindAll(trees, t => t != null && System.Array.IndexOf(treeNames, t.name) >= 0);
            var map = ScriptableObject.CreateInstance<MeshMapDefinition>();
            map.EditorInitialize(mapId, ground, water, waterPlacementZone, treesArea, trees, rocks.ToArray(), waterMaterial);
            if (forcedSeason.HasValue) map.EditorForceSeason(forcedSeason.Value);

            // Map 3's extra parts, each optional: relief, stony clearings, the no-build slopes and
            // the props. A map with a relief mesh is a new-style map and keeps its water as modelled.
            var relief = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{prefix}-Relief.fbx");
            var rocksArea = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{prefix}-RocksArea.fbx");
            var noBuildArea = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{prefix}-NoBuildArea.fbx");
            var decor = CreateMapDecorData(mapId, folder, prefix);
            var terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(ModelImportDefaults.TerrainPaletteMaterialPath);
            map.EditorInitializeExtras(relief, rocksArea, noBuildArea, decor, terrainMaterial, keepWaterRot: relief != null);
            ReadOrcSpots(map, $"{folder}/{prefix}-Spots.csv");

            Directory.CreateDirectory(MeshMapsFolder);
            var path = $"{MeshMapsFolder}/{mapId}.asset";
            DeleteIfExists(path);
            AssetDatabase.CreateAsset(map, path);
        }

        /// <summary>
        /// The archipelago's template (Maps/Archipelago): no meshes -- the game generates the islands
        /// from the seed in the map's id -- just what they are dressed with: the terrain palette, the
        /// trees and the boulders. No orc raids over land.
        /// </summary>
        private static void CreateArchipelagoDefinition()
        {
            var trees = new List<GameObject>();
            for (var i = 0; i < TreeModelCount; i++)
            {
                var tree = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsTerrainFolder}/Tree{i + 1}.fbx");
                if (tree != null) trees.Add(tree);
            }
            var rocks = new List<GameObject>();
            for (var i = 0; i < StoneModelCount; i++)
            {
                var rock = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsTerrainFolder}/Stone{i + 1}.fbx");
                if (rock != null) rocks.Add(rock);
            }
            var terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(ModelImportDefaults.TerrainPaletteMaterialPath);
            var map = ScriptableObject.CreateInstance<MeshMapDefinition>();
            map.EditorInitialize(CityBuilder.Maps.Archipelago.ArchipelagoMapId.TemplateId, null, null, null, null, trees.ToArray(), rocks.ToArray(), null);
            map.EditorInitializeGenerated(terrainMaterial, withoutRaids: true);

            Directory.CreateDirectory(MeshMapsFolder);
            var path = $"{MeshMapsFolder}/{CityBuilder.Maps.Archipelago.ArchipelagoMapId.TemplateId}.asset";
            DeleteIfExists(path);
            AssetDatabase.CreateAsset(map, path);
        }

        /// <summary>
        /// A designed map's spots from <prefix>-Spots.csv (kind,x,y[,level] in Blender's axes:
        /// Blender (x, y) is Unity (x, z)). "main" is the main base's centre, "camp" a camp's and
        /// the level it grows to, "zone" a corner of the Town Hall's start zone; every other kind
        /// (the ruin, the Invasion map's bridges, monsters, shrines, boss) is kept by kind for the
        /// mode that reads it (MeshMapDefinition.SpotsOf). No file: the map has no fixed spots.
        /// </summary>
        private static void ReadOrcSpots(MeshMapDefinition map, string csvPath)
        {
            if (!File.Exists(csvPath)) return;
            Vector2? main = null;
            var camps = new List<Vector2>();
            var levels = new List<int>();
            var zone = new List<Vector2>();
            var others = new List<MapSpot>();
            foreach (var line in File.ReadAllLines(csvPath))
            {
                var cells = line.Split(',');
                if (cells.Length < 3 || cells[0] == "kind") continue;
                var spot = new Vector2(float.Parse(cells[1], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(cells[2], System.Globalization.CultureInfo.InvariantCulture));
                if (cells[0] == "main") main = spot;
                else if (cells[0] == "zone") zone.Add(spot);
                else if (cells[0] == "camp")
                {
                    camps.Add(spot);
                    levels.Add(cells.Length > 3 ? int.Parse(cells[3]) : OrcBaseLayout.MaxLevel);
                }
                else others.Add(new MapSpot { kind = cells[0], position = spot, level = cells.Length > 3 ? int.Parse(cells[3]) : 0 });
            }
            map.EditorInitializeSpots(main, camps.ToArray(), levels.ToArray(), zone.ToArray(), others.ToArray());
        }

        /// <summary>
        /// The props of a map from the list its generator exported (<prefix>-Decor.csv: kind,
        /// variant, x, y, z, rot, scale in Blender's axes) and the prototype meshes in
        /// <prefix>-DecorKit.fbx (one object per kind and variant, named <prefix>-DecorKit-kind_v).
        /// Blender (x, y, z) is Unity (x, z, y); a turn about Blender's up axis is the opposite
        /// turn about Unity's. Null when the map has no props.
        /// </summary>
        private static MapDecorData CreateMapDecorData(string mapId, string folder, string prefix)
        {
            var csvPath = $"{folder}/{prefix}-Decor.csv";
            var kit = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{prefix}-DecorKit.fbx");
            if (!File.Exists(csvPath) || kit == null) return null;

            var meshByName = new Dictionary<string, Mesh>();
            foreach (var filter in kit.GetComponentsInChildren<MeshFilter>(true))
            {
                var name = filter.gameObject.name;
                var at = name.IndexOf("DecorKit-", System.StringComparison.Ordinal);
                if (at < 0 || filter.sharedMesh == null) continue;
                meshByName[name.Substring(at + "DecorKit-".Length)] = filter.sharedMesh;
            }

            var prototypes = new List<Mesh>();
            var prototypeIndex = new Dictionary<string, ushort>();
            var index = new List<ushort>();
            var positions = new List<Vector3>();
            var yaw = new List<float>();
            var scales = new List<float>();
            var skipped = 0;
            var culture = System.Globalization.CultureInfo.InvariantCulture;

            foreach (var line in File.ReadAllLines(csvPath))
            {
                var f = line.Split(',');
                if (f.Length < 7 || f[0] == "kind") continue;
                var key = $"{f[0]}_{f[1]}";
                if (!prototypeIndex.TryGetValue(key, out var p))
                {
                    if (!meshByName.TryGetValue(key, out var mesh))
                    {
                        skipped++;
                        continue;
                    }
                    p = (ushort)prototypes.Count;
                    prototypes.Add(mesh);
                    prototypeIndex[key] = p;
                }
                index.Add(p);
                positions.Add(new Vector3(float.Parse(f[2], culture), float.Parse(f[4], culture), float.Parse(f[3], culture)));
                yaw.Add(-float.Parse(f[5], culture) * Mathf.Rad2Deg);
                scales.Add(float.Parse(f[6], culture));
            }
            if (skipped > 0) Debug.LogWarning($"CreateMapDecorData: {skipped} props of {mapId} name a model {prefix}-DecorKit.fbx does not have.");

            var data = ScriptableObject.CreateInstance<MapDecorData>();
            data.EditorInitialize(prototypes.ToArray(), index.ToArray(), positions.ToArray(), yaw.ToArray(), scales.ToArray());
            Directory.CreateDirectory(MeshMapsFolder);
            var path = $"{MeshMapsFolder}/{mapId}Decor.asset";
            DeleteIfExists(path);
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        private static Material CreateGroundMaterial()
        {
            var path = $"{MaterialsFolder}/Ground.mat";
            DeleteIfExists(path);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.42f, 0.62f, 0.32f) };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
