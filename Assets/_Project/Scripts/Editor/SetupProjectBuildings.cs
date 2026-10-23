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
    /// Buildings as data: BuildingData assets from the balance sheet (CreateBuildingData and its
    /// call sites, per-level stats, recipes, costs, requirements) and the authored FBX models with
    /// their level models.
    /// </summary>
    public static partial class SetupProject
    {
        /// <summary>
        /// Architectural archetype driving which procedural generator builds a prefab. What makes
        /// one building look like ITSELF on top of the shell is its BuildingMotif -- see
        /// SetupProjectMotifs.cs.
        ///
        /// `Plot` is the shell for everything that is not really a building: a well, a flag, a
        /// garden, a gate you walk through, an arena with an open floor. It lays a paved base the
        /// size of the footprint and leaves the rest to the motif.
        /// </summary>
        private enum BuildingStyle { Hut, Fortification, Tower, Road, Plot }

        private static BuildingData CreateBuildingData(
            string id, Vector2Int footprint, float height, Color wallColor, Color roofColor,
            BuildingStyle style, Material trimMaterial, Material windowMaterial, Material doorMaterial = null,
            bool hasChimney = false,
            bool isRoad = false, bool keepSelectedAfterPlacement = false, bool isWaterCategory = false,
            bool providesWalkableSurface = false, bool connectsToFences = false,
            BuildingMotif motif = BuildingMotif.None)
        {
            // Two of the sheet's numbers shape the prefab rather than just sitting on the data asset,
            // so the row has to be read before it's built: worker slots become geometry, and a real
            // defence stat decides whether the building shoots back at all.
            var balance = Balance(id);

            // An authored model wins over anything generated here: the moment `<id>1-lvl1.fbx`
            // exists, the placeholder below stops being built and the model's own size becomes the
            // building's footprint. That is the whole loop for adding art -- drop the file in, run
            // the build, see it in the game. No code change, which matters when there are ~48
            // buildings to get through and most of them will arrive as rough stand-ins first. The
            // footprint passed in is what the placeholder falls back to until then.
            GameObject prefab;
            var authoredModel = AuthoredModel(id);
            if (authoredModel != null)
            {
                prefab = CreateAuthoredModelPrefab(id, height, authoredModel, balance, footprint);
            }
            else switch (style)
            {
                case BuildingStyle.Hut:
                    // Level 1's worker count: the hut's geometry is built once, and its level-2
                    // model is a separate FBX rather than a re-generated mesh.
                    prefab = CreateHutPrefab(id, footprint, height, wallColor, roofColor, LevelOne(balance).maxWorkers, hasChimney, trimMaterial, doorMaterial, windowMaterial, motif);
                    break;
                case BuildingStyle.Fortification:
                    prefab = CreateFortificationPrefab(id, footprint, height, wallColor, roofColor, trimMaterial, isTower: false, motif: motif);
                    break;
                case BuildingStyle.Tower:
                    prefab = CreateFortificationPrefab(id, footprint, height, wallColor, roofColor, trimMaterial, isTower: true, motif: motif);
                    break;
                case BuildingStyle.Road:
                    prefab = CreateRoadPrefab(id, wallColor, roofColor);
                    break;
                case BuildingStyle.Plot:
                    prefab = CreatePlotPrefab(id, footprint, height, wallColor, roofColor, LevelOne(balance).maxWorkers, trimMaterial, motif);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(style), style, null);
            }

            // Any building with a real defense stat (Wall/Tower/Barracks/Gate) fights back against
            // OrcUnit raiders on its own -- see DefensiveBuilding. Attached here (once, at prefab
            // creation) rather than conditionally at placement time, same as ProductionBuilding's
            // maxWorkers check in CreateHutPrefab.
            // Attached if the building EVER fights back, not just at level 1 -- a structure that
            // only gains defence when upgraded still needs the component from the start, since
            // upgrading swaps stats and models but never rebuilds the prefab.
            if (FightsBackAtAnyLevel(balance)) prefab.AddComponent<DefensiveBuilding>();

            var data = ScriptableObject.CreateInstance<BuildingData>();
            data.buildingName = id;
            data.prefab = prefab;
            data.footprintSize = footprint;
            ApplyBalance(data, balance);
            data.isRoad = isRoad;
            data.providesWalkableSurface = providesWalkableSurface;
            data.keepSelectedAfterPlacement = keepSelectedAfterPlacement;
            data.isWaterCategory = isWaterCategory;
            data.connectsToFences = connectsToFences;
            data.isGate = id == "Gate" || id == "FortifiedGate";

            Directory.CreateDirectory(BuildingDataFolder);
            var dataPath = $"{BuildingDataFolder}/{id}.asset";
            DeleteIfExists(dataPath);
            AssetDatabase.CreateAsset(data, dataPath);
            return data;
        }

        /// <summary>
        /// The authored model for one of a building's levels, found by name rather than wired up in
        /// code: `<id>1-lvl<level>.fbx` in Models/Buildings, matching the naming the models
        /// themselves use (the digit is the building variant, lvl1 its upgrade level). Null while
        /// nobody has drawn it -- at level 1 the procedural placeholder stands in, above it the
        /// level below keeps being shown (see AddLevelModels).
        /// </summary>
        private static GameObject AuthoredModel(string id, int level = 1)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsBuildingsFolder}/{id}1-lvl{level}.fbx");
        }

        /// <summary>
        /// A building whose look comes entirely from an FBX. Everything else about it -- its stats,
        /// its components -- is unchanged from the generated version, so swapping the file in
        /// changes the picture and nothing else.
        ///
        /// The HEIGHT is measured from the model, because it is what the player's clicks meet: a
        /// hand-written height that disagrees with the model gives either a roof that cannot be
        /// clicked or an invisible box beside the building that eats clicks meant for the ground.
        ///
        /// The FOOTPRINT is the written one, not measured. It used to be measured, back when the
        /// models were stand-in boxes exactly their plot's size; the Blender set (Tools/blender) is
        /// modelled INSIDE the plot with a margin, so measuring it rounds wrongly -- a fortified
        /// gate drawn 1.04 m wide inside its 2x2 plot came out 1x2, a treasury 2.5 m wide inside
        /// 3x2 came out 2x2. The written footprints are the same plots the models were drawn in
        /// (both come from the user's placeholder file). A model that measures LARGER than its plot
        /// is warned about, since it will hang over the neighbours.
        /// </summary>
        private static GameObject CreateAuthoredModelPrefab(string id, float fallbackHeight, GameObject source, BuildingBalance balance, Vector2Int footprint)
        {
            var root = new GameObject(id);
            root.AddComponent<BuildingInstance>();
            // Same rule as DefensiveBuilding below: attached if the building EVER employs anyone,
            // since upgrading swaps stats and models but never rebuilds the prefab.
            if (EmploysAtAnyLevel(balance)) root.AddComponent<ProductionBuilding>();

            var model = Object.Instantiate(source, Vector3.zero, source.transform.rotation, root.transform);
            var size = FitModelToGround(model, id);

            WarnIfOverhangs(size, footprint, id);
            var height = size.y > 0.01f ? size.y : fallbackHeight;

            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(footprint.x * CellSize - BuildingInset, height, footprint.y * CellSize - BuildingInset);
            collider.center = new Vector3(0f, height * 0.5f, 0f);

            AddLevelModels(root, id, model, height, footprint);

            // Not merged: BuildingLevelAppearance switches these level models on and off at
            // runtime, so they have to stay separate objects.
            return SavePrefab(root, id, combineParts: false);
        }

        /// <summary>
        /// Hangs the models for levels 2 and 3 in the same prefab, where the artist has drawn them:
        /// `<id>1-lvl2.fbx`, `<id>1-lvl3.fbx`, found by name exactly the way level 1 is. Upgrading
        /// then swaps which one is active (see BuildingLevelAppearance) instead of rebuilding the
        /// building, so its workers, damage, storage and NavMesh hole all survive the upgrade.
        ///
        /// Two rules worth keeping in mind before exporting a level-2 model:
        ///
        /// The FOOTPRINT stays level 1's, whatever the later models measure. A level-2 model a cell
        /// wider would otherwise have to fit into ground the player has already built over -- an
        /// upgrade that fails on geometry at the exact moment it has been paid for. A wider model is
        /// warned about here and left to overhang, the same tolerance a too-wide level-1 model gets.
        ///
        /// The HEIGHT does follow the level, and carries the click box with it, so a tower that
        /// doubles in height at level 3 is still clickable at the top.
        ///
        /// Nothing is attached at all when only level 1 exists, which is every building today: the
        /// component would resolve every level back to the one model and cost a component on 45
        /// prefabs to do it. The day an FBX lands, this build wires it up.
        /// </summary>
        private static void AddLevelModels(GameObject root, string id, GameObject levelOneModel, float levelOneHeight, Vector2Int footprint)
        {
            var models = new List<GameObject> { levelOneModel };
            var heights = new List<float> { levelOneHeight };
            var anyDrawn = false;

            for (var level = 2; level <= BuildingInstance.MaxLevel; level++)
            {
                var source = AuthoredModel(id, level);
                if (source == null)
                {
                    // A hole, not a shortened list: the levels have to stay at their own index, or
                    // a building with only levels 1 and 3 drawn would show level 3's model at 2.
                    models.Add(null);
                    heights.Add(0f);
                    continue;
                }

                anyDrawn = true;
                var model = Object.Instantiate(source, Vector3.zero, source.transform.rotation, root.transform);
                model.name = $"{id}-lvl{level}";
                var size = FitModelToGround(model, $"{id} lvl{level}");
                // The building keeps level 1's plot whatever this model measures: a wider one
                // overhangs its neighbours instead of claiming more ground.
                WarnIfOverhangs(size, footprint, $"{id} lvl{level}");

                // Level 1 is what a newly placed building shows; the rest wait to be upgraded into.
                model.SetActive(false);
                models.Add(model);
                heights.Add(size.y);
            }

            if (!anyDrawn) return;
            root.AddComponent<BuildingLevelAppearance>().SetModels(models, heights);
        }

        /// <summary>
        /// Centres a model on its own origin, stands it on the ground, and reports how big it turned
        /// out to be.
        ///
        /// Measuring instead of trusting the FBX's origin is what makes a model droppable: these
        /// files come out of Blender with the geometry wherever the scene happened to leave it (the
        /// fence pair were exported side by side with a zero translation and would otherwise have
        /// stood metres from their cell). Only the horizontal centre and the base are corrected --
        /// nothing is ever scaled.
        /// </summary>
        private static Vector3 FitModelToGround(GameObject model, string id)
        {
            var renderers = model.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0)
            {
                Debug.LogError($"FitModelToGround: '{id}' has an authored model with no MeshRenderer -- nothing to place or measure.");
                return Vector3.zero;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            // Only the base is corrected. The horizontal origin is trusted: Tools/blender/export_fbx.py
            // writes every model with its origin at the centre of its plot, and the models are
            // deliberately not symmetric in it (a yard in front, a stack in one corner), so
            // centring on the bounds would slide a building up to a tenth of a cell off its plot.
            // An origin far from the geometry still means a bad export, and is reported.
            var off = new Vector2(bounds.center.x - model.transform.position.x, bounds.center.z - model.transform.position.z);
            if (off.magnitude > CellSize * 0.35f)
            {
                Debug.LogWarning($"FitModelToGround: '{id}' has its geometry {off.magnitude:0.##} m from its origin -- was it exported with the origin at its plot's centre (Tools/blender/export_fbx.py)?");
            }
            model.transform.position -= new Vector3(0f, bounds.min.y, 0f);
            return bounds.size;
        }

        /// <summary>
        /// A model drawn wider than its plot hangs over the neighbours' ground. Allowed -- the
        /// footprint is the written one either way -- but said out loud, since it is nearly always
        /// a roof overhang or a prop nobody meant to leave outside. A few centimetres are tolerated.
        /// </summary>
        private static void WarnIfOverhangs(Vector3 size, Vector2Int footprint, string id)
        {
            const float tolerance = 0.03f;
            if (size.x > footprint.x * CellSize + tolerance || size.z > footprint.y * CellSize + tolerance)
            {
                Debug.LogWarning($"[SetupProject] '{id}' measures {size.x:0.##}x{size.z:0.##} m, wider than its {footprint.x}x{footprint.y} plot -- it will overhang its neighbours.");
            }
        }

        /// <summary>
        /// The balance sheet's row for a building. A missing row is a build error rather than a
        /// silent default: the asset would still be generated, but full of placeholder numbers that
        /// look deliberate in the Inspector and would be very hard to trace back to a renamed row.
        /// </summary>
        private static BuildingBalance Balance(string id)
        {
            var row = _balance != null ? _balance.Building(id) : null;
            if (row != null) return row;

            Debug.LogError($"SetupProject: the balance sheet's buildings tab has no row with id '{id}'. " +
                           $"'{id}' is being generated with placeholder numbers -- add the row to {BalanceImporter.BuildingsCsvPath} and rebuild.");
            return new BuildingBalance { id = id, displayName = id };
        }

        /// <summary>Copies every balanced number from a sheet row onto a building's data asset. Shape (footprint, prefab, road/water flags) is the caller's business.</summary>
        private static void ApplyBalance(BuildingData data, BuildingBalance balance) => data.ApplyBalance(balance);

        /// <summary>The level-1 stats of a sheet row, or defaults if the row is missing its levels entirely (already reported by Balance).</summary>
        private static BuildingLevelStats LevelOne(BuildingBalance balance)
        {
            return balance.levels != null && balance.levels.Count > 0 && balance.levels[0] != null
                ? balance.levels[0]
                : new BuildingLevelStats();
        }

        /// <summary>Whether this building has a defence stat at any of its levels -- see the DefensiveBuilding attachment.</summary>
        private static bool FightsBackAtAnyLevel(BuildingBalance balance)
        {
            if (balance.levels == null) return false;
            foreach (var level in balance.levels)
            {
                if (level != null && level.defense > 0) return true;
            }
            return false;
        }

        /// <summary>Whether this building has worker slots at any of its levels -- see the ProductionBuilding attachment. Same reasoning as FightsBackAtAnyLevel: a workshop that only takes hands on at level 2 is upgraded, never rebuilt.</summary>
        private static bool EmploysAtAnyLevel(BuildingBalance balance)
        {
            if (balance.levels == null) return false;
            foreach (var level in balance.levels)
            {
                if (level != null && level.maxWorkers > 0) return true;
            }
            return false;
        }

        /// <summary>
        /// Wires BuildingData.requiredBuilding from the sheet's "requires" column, once every asset
        /// exists -- a prerequisite is a reference to another building, so it cannot be resolved
        /// while the buildings are still being created one by one.
        ///
        /// CreateAsset has already written each asset to disk by now, so mutating one afterwards
        /// needs an explicit SetDirty; without it the closing SaveAssets silently drops the edit.
        /// </summary>
        private static void ResolveBuildingRequirements(List<BuildingData> buildings)
        {
            var byId = new Dictionary<string, BuildingData>(buildings.Count);
            foreach (var building in buildings)
            {
                byId[building.buildingName] = building;
            }

            foreach (var building in buildings)
            {
                var requiredId = Balance(building.buildingName).requiredBuildingId;
                if (string.IsNullOrWhiteSpace(requiredId)) continue;

                if (!byId.TryGetValue(requiredId.Trim(), out var required))
                {
                    Debug.LogError($"SetupProject: '{building.buildingName}' requires '{requiredId}', which is not a building id. " +
                                   $"Check the requires column in {BalanceImporter.BuildingsCsvPath} -- the prerequisite is being left empty.");
                    continue;
                }

                building.requiredBuilding = required;
                EditorUtility.SetDirty(building);
            }
        }
    }
}
