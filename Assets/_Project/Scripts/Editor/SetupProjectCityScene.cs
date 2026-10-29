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
    /// The game scene: BuildCityScene wires every manager, the map, the camera, the cheats, the ore
    /// veins and the harvest-bar skin.
    /// </summary>
    public static partial class SetupProject
    {
        private static void BuildCityScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Building data/prefab assets are created here, after the scene switch above, so no
            // further NewScene() call happens before they're consumed below. EditorSceneManager.
            // NewScene() unloads objects that aren't rooted in the new scene yet, which silently
            // turns freshly created ScriptableObject asset references stale (Unity "fake null").
            var trimMaterial = CreateLitMaterial("Trim", new Color(0.14f, 0.11f, 0.08f));
            var doorMaterial = CreateLitMaterial("Door", new Color(0.32f, 0.18f, 0.09f));
            var windowMaterial = CreateLitMaterial("Window", new Color(0.88f, 0.78f, 0.48f));

            // Only shape below: every number these buildings are balanced on -- cost, health,
            // production, defence, upgrades, prerequisites -- comes from the balance sheet's
            // buildings tab, looked up by the id in the first argument (see ApplyBalance).
            var houseData = CreateBuildingData(
                "Hovel", new Vector2Int(2, 2), height: 2.4f,
                wallColor: new Color(0.75f, 0.55f, 0.35f), roofColor: new Color(0.25f, 0.45f, 0.65f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                hasChimney: true);

            var cottageData = CreateBuildingData(
                "Cottage", new Vector2Int(2, 2), height: 3f,
                wallColor: new Color(0.62f, 0.42f, 0.55f), roofColor: new Color(0.3f, 0.2f, 0.4f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                hasChimney: true);

            // The third rung of the housing ladder: Лачуга -> Коттедж -> Поместье, told apart by
            // size and height rather than by any ornament -- a manor is a bigger house.
            var manorData = CreateBuildingData(
                "Manor", new Vector2Int(3, 3), height: 3.8f,
                wallColor: new Color(0.78f, 0.72f, 0.6f), roofColor: new Color(0.35f, 0.28f, 0.42f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                hasChimney: true);

            // Decorations, and the town square people gather on. They cost something, they stand
            // somewhere, and (bar the happiness they carry) that is all they do -- but a town of
            // nothing but workshops does not look like a town.
            var townSquareData = CreateBuildingData(
                "TownSquare", new Vector2Int(2, 2), height: 1f,
                wallColor: new Color(0.62f, 0.6f, 0.56f), roofColor: new Color(0.5f, 0.48f, 0.45f),
                style: BuildingStyle.Plot, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.Fountain);

            var flagData = CreateBuildingData(
                "Flag", new Vector2Int(1, 1), height: 2.4f,
                wallColor: new Color(0.6f, 0.58f, 0.54f), roofColor: new Color(0.45f, 0.32f, 0.18f),
                style: BuildingStyle.Plot, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.FlagPole);

            var decTreeData = CreateBuildingData(
                "DecTree", new Vector2Int(1, 1), height: 2.6f,
                wallColor: new Color(0.42f, 0.5f, 0.34f), roofColor: new Color(0.36f, 0.44f, 0.3f),
                style: BuildingStyle.Plot, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.Tree);

            var decBushData = CreateBuildingData(
                "DecBush", new Vector2Int(1, 1), height: 0.9f,
                wallColor: new Color(0.44f, 0.5f, 0.36f), roofColor: new Color(0.38f, 0.46f, 0.32f),
                style: BuildingStyle.Plot, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.Bush);

            var decGardenData = CreateBuildingData(
                "DecGarden", new Vector2Int(3, 1), height: 0.6f,
                wallColor: new Color(0.5f, 0.55f, 0.4f), roofColor: new Color(0.46f, 0.52f, 0.36f),
                style: BuildingStyle.Plot, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.GardenBeds);

            // The one building whose loss ends the game, so it is the one silhouette that has to be
            // findable from anywhere on the map: a walled base with a keep and a banner over it.
            var townHallData = CreateBuildingData(
                BuildingIds.TownHall, new Vector2Int(5, 5), height: 4.4f,
                wallColor: new Color(0.5f, 0.48f, 0.44f), roofColor: new Color(0.42f, 0.24f, 0.22f),
                style: BuildingStyle.Fortification, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.Keep);

            var fishermanHutData = CreateBuildingData(
                "FisherHut", new Vector2Int(1, 1), height: 2f,
                wallColor: new Color(0.55f, 0.52f, 0.45f), roofColor: new Color(0.2f, 0.5f, 0.55f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.DryingRacks);

            var farmData = CreateBuildingData(
                "Farm", new Vector2Int(1, 1), height: 1.9f,
                wallColor: new Color(0.68f, 0.55f, 0.3f), roofColor: new Color(0.42f, 0.58f, 0.24f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.Field);

            var lumberjackData = CreateBuildingData(
                "Sawmill", new Vector2Int(2, 2), height: 2.4f,
                wallColor: new Color(0.45f, 0.3f, 0.18f), roofColor: new Color(0.32f, 0.22f, 0.13f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.LogPile);

            var quarryData = CreateBuildingData(
                "Quarry", new Vector2Int(2, 2), height: 2f,
                wallColor: new Color(0.55f, 0.53f, 0.48f), roofColor: new Color(0.3f, 0.29f, 0.27f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.StoneYard);

            // All four mines share the pit-head frame and are told apart by what comes out of the
            // ground: the ore heap and the walls carry the metal's own colour.
            var mineData = CreateBuildingData(
                "IronMine", new Vector2Int(2, 2), height: 2.2f,
                wallColor: new Color(0.4f, 0.38f, 0.36f), roofColor: new Color(0.5f, 0.5f, 0.56f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.Headframe);

            var coalMineData = CreateBuildingData(
                "CoalMine", new Vector2Int(2, 2), height: 2.2f,
                wallColor: new Color(0.3f, 0.28f, 0.27f), roofColor: new Color(0.14f, 0.14f, 0.15f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.Headframe);

            var towerData = CreateBuildingData(
                "Tower", new Vector2Int(1, 1), height: 4.2f,
                wallColor: new Color(0.4f, 0.38f, 0.34f), roofColor: new Color(0.42f, 0.24f, 0.22f),
                style: BuildingStyle.Tower, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                connectsToFences: true);

            var barracksData = CreateBuildingData(
                "Barracks", new Vector2Int(3, 3), height: 2.8f,
                wallColor: new Color(0.5f, 0.48f, 0.44f), roofColor: new Color(0.42f, 0.24f, 0.22f),
                style: BuildingStyle.Fortification, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.Banner);

            // Towers and gates stand IN the fence line rather than beside it: connectsToFences
            // registers their cells with FenceNetwork, so the segments either side shape themselves
            // as if the line ran straight through. A gate is built on the flat shell rather than the
            // fortified one on purpose -- it has to read as something you can walk through.
            var gateData = CreateBuildingData(
                "Gate", new Vector2Int(1, 1), height: 2.2f,
                wallColor: new Color(0.4f, 0.38f, 0.34f), roofColor: new Color(0.34f, 0.32f, 0.3f),
                style: BuildingStyle.Plot, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                connectsToFences: true, motif: BuildingMotif.Archway);

            // The second tier of the defensive line. Separate buildings, not upgrade levels -- the
            // taxonomy treats them that way, and each still has its own three levels on top.
            var fortifiedTowerData = CreateBuildingData(
                "FortifiedTower", new Vector2Int(2, 2), height: 5f,
                wallColor: new Color(0.46f, 0.45f, 0.42f), roofColor: new Color(0.42f, 0.24f, 0.22f),
                style: BuildingStyle.Tower, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                connectsToFences: true, motif: BuildingMotif.Banner);

            var fortifiedGateData = CreateBuildingData(
                "FortifiedGate", new Vector2Int(1, 3), height: 2.8f,
                wallColor: new Color(0.44f, 0.42f, 0.4f), roofColor: new Color(0.34f, 0.32f, 0.3f),
                style: BuildingStyle.Plot, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                connectsToFences: true, motif: BuildingMotif.Archway);
            // its end walls turn toward a fence meeting them from the side (the user, 2026-10-25)
            AddGateArms(fortifiedGateData);

            // One storehouse per resource family, so every group has somewhere to go -- what each
            // one holds is the storage_group column, not anything decided here. A store is a wide
            // double door and no windows; a granary is bins; a treasury is a strongbox behind walls.
            var warehouseData = CreateBuildingData(
                "Warehouse", new Vector2Int(2, 2), height: 2.6f,
                wallColor: new Color(0.58f, 0.46f, 0.3f), roofColor: new Color(0.36f, 0.3f, 0.24f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.BarnDoors);

            var barnData = CreateBuildingData(
                "Barn", new Vector2Int(1, 1), height: 2.4f,
                wallColor: new Color(0.7f, 0.5f, 0.28f), roofColor: new Color(0.5f, 0.36f, 0.2f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.BarnDoors);

            var treasuryData = CreateBuildingData(
                "Treasury", new Vector2Int(3, 2), height: 2.4f,
                wallColor: new Color(0.5f, 0.48f, 0.44f), roofColor: new Color(0.72f, 0.6f, 0.24f),
                style: BuildingStyle.Fortification, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.Vault);

            // The second tier of each storehouse. Nothing new mechanically -- storage_group and
            // storage_capacity already do all of it -- so what these add is the reason to keep
            // building outward once the first three are full. Each needs its smaller version
            // standing first (the requires column), which is also what keeps them out of the
            // opening minutes of a game.
            var bigWarehouseData = CreateBuildingData(
                "BigWarehouse", new Vector2Int(3, 3), height: 3.2f,
                wallColor: new Color(0.52f, 0.41f, 0.27f), roofColor: new Color(0.3f, 0.25f, 0.2f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.BarnDoors);

            var bigBarnData = CreateBuildingData(
                "BigBarn", new Vector2Int(2, 2), height: 3.4f,
                wallColor: new Color(0.66f, 0.45f, 0.24f), roofColor: new Color(0.45f, 0.31f, 0.17f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.BarnDoors);

            var bigTreasuryData = CreateBuildingData(
                "BigTreasury", new Vector2Int(4, 3), height: 3f,
                wallColor: new Color(0.46f, 0.44f, 0.41f), roofColor: new Color(0.78f, 0.66f, 0.28f),
                style: BuildingStyle.Fortification, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.Vault);

            // Two models, one prefab: a fence segment shows the straight run or the corner depending
            // on the neighbours it finds (see FenceShape), and neither is a separate building.
            var fenceData = CreateFenceBuildingData("Fence", trimMaterial);

            var roadData = CreateBuildingData(
                "Road", new Vector2Int(1, 1), height: 0.05f,
                wallColor: new Color(0.32f, 0.32f, 0.34f), roofColor: new Color(0.85f, 0.75f, 0.3f),
                style: BuildingStyle.Road, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                isRoad: true, keepSelectedAfterPlacement: true);

            var bridgeData = CreateBuildingData(
                "Bridge", new Vector2Int(1, 1), height: 0.05f,
                wallColor: new Color(0.5f, 0.35f, 0.2f), roofColor: new Color(0.36f, 0.24f, 0.13f),
                style: BuildingStyle.Road, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                isRoad: true, keepSelectedAfterPlacement: true, isWaterCategory: true,
                // The whole point of a bridge: it's the only building that makes previously
                // uncrossable terrain walkable (see BuildingData.providesWalkableSurface).
                providesWalkableSurface: true);

            var waterMillData = CreateWaterBuildingData(
                "WaterMill", new Vector2Int(2, 2),
                deckColor: new Color(0.5f, 0.4f, 0.28f), accentColor: new Color(0.35f, 0.24f, 0.14f),
                addWheel: true);

            var dockData = CreateWaterBuildingData(
                "Dock", new Vector2Int(2, 2),
                deckColor: new Color(0.55f, 0.4f, 0.24f), accentColor: new Color(0.68f, 0.5f, 0.3f),
                addCrates: true);

            // Buildings whose models arrived ahead of their mechanics. They can be placed, they
            // cost what the sheet says and they do nothing whatsoever -- no production, no storage,
            // no defence, no workers. Each is waiting on a system that does not exist yet
            // (processing chains, fire, illness, happiness, soldier upgrades), and the balance row
            // for each says so in its comment. They are in the game because a town that can only
            // build the implemented half of the design does not look like the design.
            var shellBuildings = new List<BuildingData>();
            foreach (var shell in new[]
            {
                ("Granary", new Vector2Int(2, 1), 2.6f, new Color(0.68f, 0.56f, 0.34f), new Color(0.45f, 0.35f, 0.22f), BuildingStyle.Hut, BuildingMotif.Silo),
                ("BigGranary", new Vector2Int(3, 2), 3.2f, new Color(0.64f, 0.52f, 0.3f), new Color(0.4f, 0.31f, 0.2f), BuildingStyle.Hut, BuildingMotif.Silo),
                ("Baker", new Vector2Int(1, 2), 2.4f, new Color(0.8f, 0.66f, 0.46f), new Color(0.5f, 0.3f, 0.2f), BuildingStyle.Hut, BuildingMotif.Stack),
                ("Windmill", new Vector2Int(2, 2), 3.4f, new Color(0.75f, 0.72f, 0.64f), new Color(0.42f, 0.34f, 0.24f), BuildingStyle.Hut, BuildingMotif.Sails),
                ("Smokehouse", new Vector2Int(1, 2), 2.4f, new Color(0.5f, 0.4f, 0.32f), new Color(0.3f, 0.24f, 0.2f), BuildingStyle.Hut, BuildingMotif.Stack),
                ("PigFarm", new Vector2Int(3, 4), 1.9f, new Color(0.72f, 0.6f, 0.5f), new Color(0.45f, 0.36f, 0.26f), BuildingStyle.Hut, BuildingMotif.Pen),
                ("Orchard", new Vector2Int(3, 3), 2.6f, new Color(0.46f, 0.5f, 0.32f), new Color(0.4f, 0.46f, 0.28f), BuildingStyle.Plot, BuildingMotif.Orchard),
                ("Smelter", new Vector2Int(2, 3), 2.8f, new Color(0.42f, 0.4f, 0.38f), new Color(0.28f, 0.26f, 0.25f), BuildingStyle.Hut, BuildingMotif.Stack),
                ("CopperMine", new Vector2Int(2, 2), 2.2f, new Color(0.55f, 0.4f, 0.3f), new Color(0.5f, 0.5f, 0.55f), BuildingStyle.Hut, BuildingMotif.Headframe),
                ("GoldMine", new Vector2Int(2, 2), 2.2f, new Color(0.6f, 0.55f, 0.35f), new Color(0.5f, 0.5f, 0.55f), BuildingStyle.Hut, BuildingMotif.Headframe),
                ("Laboratory", new Vector2Int(4, 4), 3f, new Color(0.55f, 0.5f, 0.6f), new Color(0.3f, 0.28f, 0.4f), BuildingStyle.Hut, BuildingMotif.Dome),
                ("HealerHouse", new Vector2Int(2, 4), 2.6f, new Color(0.86f, 0.84f, 0.8f), new Color(0.6f, 0.25f, 0.22f), BuildingStyle.Hut, BuildingMotif.Cross),
                ("FireBrigade", new Vector2Int(2, 2), 2.6f, new Color(0.7f, 0.3f, 0.24f), new Color(0.35f, 0.3f, 0.28f), BuildingStyle.Hut, BuildingMotif.HoseTower),
                ("Well", new Vector2Int(1, 1), 1.4f, new Color(0.55f, 0.53f, 0.5f), new Color(0.45f, 0.43f, 0.4f), BuildingStyle.Plot, BuildingMotif.WellHead),
                ("Church", new Vector2Int(3, 3), 3.4f, new Color(0.82f, 0.8f, 0.76f), new Color(0.35f, 0.4f, 0.5f), BuildingStyle.Hut, BuildingMotif.Spire),
                ("Tavern", new Vector2Int(2, 1), 2.6f, new Color(0.66f, 0.48f, 0.3f), new Color(0.4f, 0.28f, 0.18f), BuildingStyle.Hut, BuildingMotif.TavernSign),
                ("Theater", new Vector2Int(4, 4), 3.2f, new Color(0.78f, 0.74f, 0.66f), new Color(0.5f, 0.34f, 0.3f), BuildingStyle.Hut, BuildingMotif.Colonnade),
                ("Colosseum", new Vector2Int(4, 6), 3.6f, new Color(0.72f, 0.68f, 0.6f), new Color(0.5f, 0.47f, 0.42f), BuildingStyle.Plot, BuildingMotif.Arena),
            })
            {
                shellBuildings.Add(CreateBuildingData(
                    shell.Item1, shell.Item2, height: shell.Item3,
                    wallColor: shell.Item4, roofColor: shell.Item5,
                    style: shell.Item6, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                    motif: shell.Item7));
            }

            var hotbarBuildingData = new List<BuildingData>
            {
                houseData, cottageData, manorData, fishermanHutData, farmData,
                lumberjackData, quarryData, mineData, coalMineData,
                warehouseData, barnData, treasuryData,
                bigWarehouseData, bigBarnData, bigTreasuryData,
                roadData, fenceData, towerData, barracksData, gateData,
                fortifiedTowerData, fortifiedGateData,
                bridgeData, waterMillData, dockData,
                townSquareData, flagData, decTreeData, decBushData, decGardenData
            };
            hotbarBuildingData.AddRange(shellBuildings);
            var allBuildingData = new List<BuildingData>(hotbarBuildingData) { townHallData };

            ResolveBuildingRequirements(allBuildingData);

            AssetDatabase.SaveAssets();

            // The buildable area matches GridManager's logical grid exactly, but the visible
            // ground extends further out with a decorative forest border around it (see
            // CreateForestBorder), so the map doesn't just stop at a bare edge.
            var groundMaterial = CreateGroundMaterial();
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            var groundCellsX = GridCellsX + ForestMarginCells * 2;
            var groundCellsZ = GridCellsZ + ForestMarginCells * 2;
            var groundWidth = groundCellsX * CellSize;
            var groundDepth = groundCellsZ * CellSize;
            ground.transform.localScale = new Vector3(groundWidth / 10f, 1f, groundDepth / 10f);
            ground.transform.position = Vector3.zero; // GroundOrigin is symmetric around world origin
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;

            // Also handed to MapTerrainGenerator below, so its Forest/Stone resource-node props
            // (trees/rocks) share the exact same materials as the decorative forest border.
            var treeTrunkMaterial = CreateLitMaterial("TreeTrunk", new Color(0.36f, 0.24f, 0.14f));
            var treeCanopyMaterial = CreateLitMaterial("TreeCanopy", new Color(0.16f, 0.38f, 0.18f));
            var rockMaterial = CreateLitMaterial("Rock", new Color(0.55f, 0.53f, 0.48f));

            CreateForestBorder(GridCellsX * CellSize * 0.5f, GridCellsZ * CellSize * 0.5f, groundWidth * 0.5f, groundDepth * 0.5f, treeTrunkMaterial, treeCanopyMaterial);

            var minimapTexture = CreateMinimapCamera();

            var mainCameraGO = GameObject.Find("Main Camera");
            var camera = mainCameraGO.GetComponent<Camera>();

            var rig = new GameObject("CameraRig");
            var pivot = new GameObject("Pivot");
            pivot.transform.SetParent(rig.transform, false);
            pivot.transform.localRotation = Quaternion.Euler(50f, 0f, 0f);

            mainCameraGO.transform.SetParent(pivot.transform, false);
            // The zoom distance the game opens on, and it must sit inside RTSCameraController's
            // min/max or the first zoom input snaps the view somewhere the player did not ask for.
            mainCameraGO.transform.localPosition = new Vector3(0f, 0f, -15f);
            mainCameraGO.transform.localRotation = Quaternion.identity;
            rig.transform.position = Vector3.zero;

            var rtsCamera = rig.AddComponent<RTSCameraController>();
            var rigSO = new SerializedObject(rtsCamera);
            rigSO.FindProperty("pivot").objectReferenceValue = pivot.transform;
            rigSO.FindProperty("cameraTransform").objectReferenceValue = mainCameraGO.transform;
            rigSO.FindProperty("targetCamera").objectReferenceValue = camera;
            var boundsMargin = 6f;
            rigSO.FindProperty("panBoundsMin").vector2Value = new Vector2(GroundOrigin.x + boundsMargin, GroundOrigin.z + boundsMargin);
            rigSO.FindProperty("panBoundsMax").vector2Value = new Vector2(-GroundOrigin.x - boundsMargin, -GroundOrigin.z - boundsMargin);
            rigSO.ApplyModifiedPropertiesWithoutUndo();

            var managers = new GameObject("GameManagers");
            managers.AddComponent<FrameRateController>();

            var gridManager = managers.AddComponent<GridManager>();
            var gridSO = new SerializedObject(gridManager);
            gridSO.FindProperty("cellSize").floatValue = CellSize;
            gridSO.FindProperty("originWorldPosition").vector3Value = GroundOrigin;
            gridSO.FindProperty("gridSize").vector2IntValue = new Vector2Int(GridCellsX, GridCellsZ);
            gridSO.FindProperty("groundHeight").floatValue = GroundHeight;
            gridSO.ApplyModifiedPropertiesWithoutUndo();

            // Every world gesture in the game is recognised here and nowhere else -- added before
            // the camera, the placer and the selectors so its Instance exists when they subscribe.
            managers.AddComponent<CityBuilder.InputControl.TouchInputRouter>();

            managers.AddComponent<ResourceManager>();
            managers.AddComponent<RoadNetwork>();
            managers.AddComponent<FenceNetwork>();
            var gameCalendar = managers.AddComponent<GameCalendar>();
            managers.AddComponent<EventLogManager>();

            var placer = managers.AddComponent<BuildingPlacer>();
            var placerSO = new SerializedObject(placer);
            placerSO.FindProperty("targetCamera").objectReferenceValue = camera;
            placerSO.FindProperty("mandatoryFirstBuilding").objectReferenceValue = townHallData;
            var availableProp = placerSO.FindProperty("availableBuildings");
            availableProp.arraySize = hotbarBuildingData.Count;
            for (var i = 0; i < hotbarBuildingData.Count; i++)
            {
                availableProp.GetArrayElementAtIndex(i).objectReferenceValue = hotbarBuildingData[i];
            }
            placerSO.ApplyModifiedPropertiesWithoutUndo();

            // The placer switches the camera's inertia off while a building is being aimed: a
            // glide after the finger lifts would slide the aim point off the cell just lined up.
            var placerCameraSO = new SerializedObject(placer);
            placerCameraSO.FindProperty("cameraController").objectReferenceValue = rtsCamera;
            placerCameraSO.ApplyModifiedPropertiesWithoutUndo();

            var citizenManager = managers.AddComponent<CitizenManager>();
            var citizenManagerSO = new SerializedObject(citizenManager);
            citizenManagerSO.FindProperty("buildingPlacer").objectReferenceValue = placer;
            citizenManagerSO.ApplyModifiedPropertiesWithoutUndo();

            var settlementTierManager = managers.AddComponent<SettlementTierManager>();
            var settlementTierManagerSO = new SerializedObject(settlementTierManager);
            settlementTierManagerSO.FindProperty("citizenManager").objectReferenceValue = citizenManager;
            settlementTierManagerSO.ApplyModifiedPropertiesWithoutUndo();

            var taxManager = managers.AddComponent<TaxManager>();
            var taxManagerSO = new SerializedObject(taxManager);
            taxManagerSO.FindProperty("citizenManager").objectReferenceValue = citizenManager;
            taxManagerSO.ApplyModifiedPropertiesWithoutUndo();

            // The settlement's daily meal. HappinessManager reads its numbers for two of the five
            // happiness factors, and GameOverManager ends the map when it starves the town out.
            var foodConsumptionManager = managers.AddComponent<FoodConsumptionManager>();
            var foodConsumptionManagerSO = new SerializedObject(foodConsumptionManager);
            foodConsumptionManagerSO.FindProperty("citizenManager").objectReferenceValue = citizenManager;
            foodConsumptionManagerSO.ApplyModifiedPropertiesWithoutUndo();

            // Illness, on the same daily clock as the meal above and reading it: hunger is half of
            // why people fall ill, and a well being out of reach is the other half.
            var sicknessManager = managers.AddComponent<CityBuilder.Citizens.SicknessManager>();
            var sicknessManagerSO = new SerializedObject(sicknessManager);
            sicknessManagerSO.FindProperty("citizenManager").objectReferenceValue = citizenManager;
            sicknessManagerSO.ApplyModifiedPropertiesWithoutUndo();

            // Fires. No wiring: it finds what is standing, what is burning and who can reach it by
            // scanning, once a game day, and every number it needs is in the sheet.
            var fireManager = managers.AddComponent<FireManager>();

            var happinessManager = managers.AddComponent<HappinessManager>();

            // Where the people come from in both modes since 2026-10-25: the houses make them
            // (migration no longer moves anyone); and the heroes, bought in the Town Hall.
            managers.AddComponent<HousePeople>();
            managers.AddComponent<CityBuilder.Combat.HeroRoster>();

            // What contentment is finally FOR: it sets how fast people find their way here, or
            // away. Needs the placer as well as the citizens, because the settling-in grace period
            // starts when the Town Hall is placed rather than when the scene loads.
            var migrationManager = managers.AddComponent<MigrationManager>();
            var migrationManagerSO = new SerializedObject(migrationManager);
            migrationManagerSO.FindProperty("citizenManager").objectReferenceValue = citizenManager;
            migrationManagerSO.FindProperty("buildingPlacer").objectReferenceValue = placer;
            migrationManagerSO.ApplyModifiedPropertiesWithoutUndo();

            managers.AddComponent<GameOverManager>();

            // No wiring: raids are sized and paced against the player's progression score rather
            // than the calendar (see PlayerProgression), so the raid clock stopped needing the day
            // count the moment that changed.
            var raidManager = managers.AddComponent<OrcRaidManager>();
            WireOrcModels(raidManager);

            // The player's side of the war: recruitment, groups and the daily upkeep charge (hence
            // the calendar reference, same as the raid clock's).
            var armyManager = managers.AddComponent<ArmyManager>();
            var armyManagerSO = new SerializedObject(armyManager);
            armyManagerSO.FindProperty("gameCalendar").objectReferenceValue = gameCalendar;
            var kingModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Heroes/HeroKing.fbx");
            if (kingModel == null) Debug.LogWarning("[SetupProject] Models/Heroes/HeroKing.fbx is missing -- run Tools/blender/export_fbx.py. The King falls back to cubes.");
            armyManagerSO.FindProperty("kingModel").objectReferenceValue = kingModel;
            // the heroes bought in an Invasion run's castle (Tools/blender/heroes2.py)
            armyManagerSO.FindProperty("queenModel").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Heroes/HeroQueen.fbx");
            armyManagerSO.FindProperty("captainModel").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Heroes/HeroCaptain.fbx");
            // the army (Tools/blender/soldiers.py), in SoldierType order, a look per researched level
            var soldierModels = armyManagerSO.FindProperty("soldierModels");
            var soldierNames = new[] { "SoldierMilitia", "SoldierSpearman", "SoldierArcher", "SoldierManAtArms" };
            soldierModels.arraySize = soldierNames.Length * ArmyManager.SoldierLooks;
            for (var i = 0; i < soldierNames.Length; i++)
            for (var level = 1; level <= ArmyManager.SoldierLooks; level++)
            {
                var file = level == 1 ? soldierNames[i] : $"{soldierNames[i]}-lvl{level}";
                soldierModels.GetArrayElementAtIndex(i * ArmyManager.SoldierLooks + level - 1).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Models/Soldiers/{file}.fbx");
            }
            armyManagerSO.ApplyModifiedPropertiesWithoutUndo();

            // The Invasion mode: idle in a sandbox game; in a run started from the menu's «В бой»
            // it switches off what a run does not have -- the sandbox's raids, illness, fires,
            // food, taxes, contentment and migration (its people come from its houses) -- and sets
            // the run up.
            var invasionMode = managers.AddComponent<CityBuilder.Invasion.InvasionMode>();
            var invasionModeSO = new SerializedObject(invasionMode);
            invasionModeSO.FindProperty("buildingPlacer").objectReferenceValue = placer;
            invasionModeSO.FindProperty("shrineModel").objectReferenceValue = LoadWild("WildShrine");
            // the New Year map's tree, snowman and gift boxes (Tools/blender/newyear.py)
            GameObject LoadNewYear(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Models/Buildings/{name}.fbx");
            invasionModeSO.FindProperty("newYearTreeModel").objectReferenceValue = LoadNewYear("NYTree");
            invasionModeSO.FindProperty("newYearSnowmanModel").objectReferenceValue = LoadNewYear("NYSnowman");
            WriteArray(invasionModeSO.FindProperty("newYearGiftModels"), new Object[] { LoadNewYear("NYGift1"), LoadNewYear("NYGift2"), LoadNewYear("NYGift3") });
            WriteArray(invasionModeSO.FindProperty("notInARun"), new Object[] { raidManager, sicknessManager, fireManager, foodConsumptionManager, taxManager, happinessManager, migrationManager });
            invasionModeSO.ApplyModifiedPropertiesWithoutUndo();

            // The Laboratory's brain. No wiring of its own: it finds the Laboratory through
            // BuildingInstance's registry, because the building comes and goes while it does not.
            var researchManager = managers.AddComponent<CityBuilder.Research.ResearchManager>();

            var citizenVisualsManager = managers.AddComponent<CitizenVisualsManager>();
            var citizenVisualsManagerSO = new SerializedObject(citizenVisualsManager);
            citizenVisualsManagerSO.FindProperty("citizenManager").objectReferenceValue = citizenManager;
            citizenVisualsManagerSO.FindProperty("gridManager").objectReferenceValue = gridManager;
            // the townsfolk (Tools/blender/villagers.py): four kinds, three colourings each
            var villagers = new List<Object>();
            var clothes = citizenVisualsManagerSO.FindProperty("villagerClothes");
            var portraits = new List<Object>();
            clothes.arraySize = 0;
            foreach (var kind in new[] { "Peasant", "Woman", "Craftsman", "Elder" })
            for (var n = 1; n <= 3; n++)
            {
                var villager = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Models/Citizens/Villager{kind}{n}.fbx");
                if (villager == null) continue;
                villagers.Add(villager);
                clothes.arraySize++;
                clothes.GetArrayElementAtIndex(clothes.arraySize - 1).colorValue = MainColourOf(villager);
                portraits.Add(AssetDatabase.LoadAssetAtPath<Sprite>($"{UiTextureImport.IconsFolder}/PortraitVillager{kind}{n}.png"));
            }
            WriteArray(citizenVisualsManagerSO.FindProperty("villagerPortraits"), portraits.ToArray());
            WriteArray(citizenVisualsManagerSO.FindProperty("villagerModels"), villagers.ToArray());
            citizenVisualsManagerSO.ApplyModifiedPropertiesWithoutUndo();

            var fogOfWarManager = managers.AddComponent<FogOfWarManager>();
            var fogOfWarManagerSO = new SerializedObject(fogOfWarManager);
            fogOfWarManagerSO.FindProperty("buildingPlacer").objectReferenceValue = placer;
            fogOfWarManagerSO.FindProperty("citizenVisualsManager").objectReferenceValue = citizenVisualsManager;
            fogOfWarManagerSO.FindProperty("cloudMaterial").objectReferenceValue = CreateFogCloudMaterial();
            fogOfWarManagerSO.FindProperty("parchmentMaterial").objectReferenceValue = CreateFogParchmentMaterial();
            fogOfWarManagerSO.ApplyModifiedPropertiesWithoutUndo();

            var saveController = managers.AddComponent<GameSaveController>();
            var saveControllerSO = new SerializedObject(saveController);
            saveControllerSO.FindProperty("buildingPlacer").objectReferenceValue = placer;
            saveControllerSO.FindProperty("citizenManager").objectReferenceValue = citizenManager;
            saveControllerSO.FindProperty("gameCalendar").objectReferenceValue = gameCalendar;
            saveControllerSO.FindProperty("taxManager").objectReferenceValue = taxManager;
            saveControllerSO.FindProperty("researchManager").objectReferenceValue = researchManager;
            var knownBuildingsProp = saveControllerSO.FindProperty("knownBuildings");
            knownBuildingsProp.arraySize = allBuildingData.Count;
            for (var i = 0; i < allBuildingData.Count; i++)
            {
                knownBuildingsProp.GetArrayElementAtIndex(i).objectReferenceValue = allBuildingData[i];
            }
            saveControllerSO.ApplyModifiedPropertiesWithoutUndo();

            // Maps 1 and 2 (the flat first maps) were deleted on 2026-10-20 at the user's word; a
            // save made on one is listed as unloadable (see LoadGamePanelController).
            // Map 3, the summer valley (Tools/blender/map3.py), redrawn on 2026-10-21 to the designed
            // maps' rules: an island, a lake under the northern hills, a river cutting it in two, the
            // town's start meadow and the orcs' spots fixed. Its water keeps its imported (palette)
            // material -- the depth bands are in the mesh.
            CreateMeshMapDefinition("Map3", "Map-3", null);
            // Map 4, the Two Rivers Valley (Tools/blender/map4.py): the first designed map -- an
            // island, rivers and mountains nobody walks through, and the orcs' spots fixed.
            CreateMeshMapDefinition("Map4", "Map-4", null);
            // Map 5, the Caldera (Tools/blender/map5.py): the town on a volcano's floor, a ring of
            // mountains that four gorges cut through, the orcs outside it.
            CreateMeshMapDefinition("Map5", "Map-5", null);
            // The archipelago (the user, 2026-10-29): the sandbox's map from now on, generated in the
            // game from a seed -- only its template is made here.
            CreateArchipelagoDefinition();
            CreateArchipelagoLibrary();
            // The Invasion mode's first map (Tools/blender/invasion1.py): the base island in the
            // middle, three lanes over three bridges to three orc camps, jungles between them. Not
            // in the sandbox's map picker -- only «В бой» plays it.
            CreateMeshMapDefinition(CityBuilder.Invasion.InvasionMode.MapId, "Inv-1", null);
            // The Invasion's New Year map (Tools/blender/invasion2.py): map 1's structure drawn anew,
            // always winter, a fir forest only (the conifers of trees1.py), its own New Year rules.
            CreateMeshMapDefinition(CityBuilder.Invasion.InvasionMode.NewYearMapId, "Inv-2", null,
                MapSeason.Winter, new[] { "Tree2", "Tree5", "Tree6", "Tree9" });

            // Added BEFORE MapTerrainGenerator (Unity runs Start() in add-order on the same
            // GameObject) so it consumes GameSessionIntent.NewGameMapId first for a mesh-map new
            // game; MapTerrainGenerator's existing "not found in catalog" no-op then harmlessly
            // skips it, and vice versa for a legacy PNG map id.
            var meshMapApplier = managers.AddComponent<MeshMapApplier>();
            var meshMapApplierSO = new SerializedObject(meshMapApplier);
            meshMapApplierSO.FindProperty("saveController").objectReferenceValue = saveController;
            meshMapApplierSO.FindProperty("baseGroundToHide").objectReferenceValue = ground;
            meshMapApplierSO.FindProperty("baseForestBorderToHide").objectReferenceValue = GameObject.Find("Forest");
            meshMapApplierSO.ApplyModifiedPropertiesWithoutUndo();

            managers.AddComponent<TreesAreaSpawner>();
            managers.AddComponent<RockSpawner>();
            ConfigureOreDeposits(managers.AddComponent<OreDepositSpawner>());

            BuildCheats();

            // Picks up whichever map MainMenuController/MapSelector chose for a new game, or the
            // one stored in a loaded save (see GameSaveController.LoadedMapId), and paints it.
            var mapTerrainGenerator = managers.AddComponent<MapTerrainGenerator>();
            var mapTerrainGeneratorSO = new SerializedObject(mapTerrainGenerator);
            mapTerrainGeneratorSO.FindProperty("saveController").objectReferenceValue = saveController;
            mapTerrainGeneratorSO.FindProperty("treeTrunkMaterial").objectReferenceValue = treeTrunkMaterial;
            mapTerrainGeneratorSO.FindProperty("treeCanopyMaterial").objectReferenceValue = treeCanopyMaterial;
            mapTerrainGeneratorSO.FindProperty("rockMaterial").objectReferenceValue = rockMaterial;
            mapTerrainGeneratorSO.ApplyModifiedPropertiesWithoutUndo();

            var saveControllerMapFieldSO = new SerializedObject(saveController);
            saveControllerMapFieldSO.FindProperty("mapTerrainGenerator").objectReferenceValue = mapTerrainGenerator;
            saveControllerMapFieldSO.FindProperty("meshMapApplier").objectReferenceValue = meshMapApplier;
            saveControllerMapFieldSO.ApplyModifiedPropertiesWithoutUndo();

            BuildGameplayUI(placer, saveController, camera, rtsCamera, settlementTierManager, gameCalendar, hotbarBuildingData, minimapTexture, townHallData);

            Directory.CreateDirectory(ScenesFolder);
            DeleteIfExists($"{ScenesFolder}/CityBuilder.unity");
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/CityBuilder.unity");
        }

        /// <summary>
        /// Testing-only tools, kept in their own GameCheats branch of the hierarchy rather than
        /// mixed into GameManagers so it's obvious at a glance which objects are the game and
        /// which are scaffolding. Each cheat is its own child so it gets its own Inspector with
        /// its own buttons (see CheatInspectors). Nothing here runs unless the tester acts --
        /// they're event-driven with no Update.
        /// </summary>
        private static Texture2D _paletteForColours;

        /// <summary>
        /// What most of a palette model is, by area: each triangle's UV points at a palette cell, so
        /// summing the areas per cell and reading the biggest cell's colour is the model's main
        /// colour -- a townsman's tunic or dress (the workforce panel's row draws him in it).
        /// </summary>
        private static Color MainColourOf(GameObject model)
        {
            var filter = model.GetComponentInChildren<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return Color.grey;
            if (_paletteForColours == null)
            {
                _paletteForColours = new Texture2D(2, 2);
                _paletteForColours.LoadImage(File.ReadAllBytes(ModelImportDefaults.PaletteTexturePath));
            }
            var mesh = filter.sharedMesh;
            var vertices = mesh.vertices;
            var uv = mesh.uv;
            var triangles = mesh.triangles;
            var areas = new Dictionary<Color32, float>();
            for (var i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var area = Vector3.Cross(vertices[triangles[i + 1]] - a, vertices[triangles[i + 2]] - a).magnitude;
                var at = uv[triangles[i]];
                Color32 colour = _paletteForColours.GetPixelBilinear(at.x, at.y);
                // skin and hair are everyone's; the clothes are what tells them apart
                if (colour.r > 220 && colour.g > 160 && colour.g < 190 && colour.b > 120 && colour.b < 150) continue;
                areas.TryGetValue(colour, out var sum);
                areas[colour] = sum + area;
            }
            var best = Color.grey;
            var most = 0f;
            foreach (var pair in areas)
            {
                if (pair.Value <= most) continue;
                most = pair.Value;
                best = pair.Key;
            }
            return best;
        }

        private static void BuildCheats()
        {
            var root = new GameObject("GameCheats");

            var orcSpawn = new GameObject("OrcSpawn");
            orcSpawn.transform.SetParent(root.transform, false);
            orcSpawn.AddComponent<OrcSpawnCheat>();

            var resources = new GameObject("Resources");
            resources.transform.SetParent(root.transform, false);
            resources.AddComponent<ResourceCheat>();

            // The Laboratory gates thirty-one buildings and every upgrade level, so testing anything
            // late in the game without this would mean playing the whole tech list first.
            var research = new GameObject("Research");
            research.transform.SetParent(root.transform, false);
            research.AddComponent<ResearchCheat>();

            // The one cheat a phone can reach: an on-screen panel (see DebugPanel).
            var panel = new GameObject("DebugPanel");
            panel.transform.SetParent(root.transform, false);
            panel.AddComponent<DebugPanel>();
        }

        /// <summary>
        /// A dedicated top-down orthographic camera rendering into a RenderTexture for the HUD
        /// minimap (see BuildMinimap) -- a live view of the actual scene rather than a separately
        /// drawn icon map, so buildings/trees/roads the player places show up automatically.
        /// Sized to exactly frame the GridCellsX x GridCellsZ world span.
        /// </summary>
        private static RenderTexture CreateMinimapCamera()
        {
            var rtPath = $"{TexturesFolder}/Minimap_RT.renderTexture";
            Directory.CreateDirectory(TexturesFolder);
            DeleteIfExists(rtPath);
            var renderTexture = new RenderTexture(512, 512, 16) { name = "Minimap_RT" };
            AssetDatabase.CreateAsset(renderTexture, rtPath);

            var cameraGO = new GameObject("MinimapCamera", typeof(Camera));
            cameraGO.transform.position = new Vector3(0f, 150f, 0f);
            cameraGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var minimapCamera = cameraGO.GetComponent<Camera>();
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = GridCellsX * CellSize * 0.5f;
            minimapCamera.nearClipPlane = 1f;
            minimapCamera.farClipPlane = 400f;
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = new Color(0.08f, 0.14f, 0.22f);
            minimapCamera.targetTexture = renderTexture;
            minimapCamera.cullingMask = ~0;
            // No shadows in a 512x512 top-down minimap: at that scale a building's shadow is a
            // pixel or two, but rendering it means a full shadow map pass on every refresh, over
            // the WHOLE map (this camera frames all of it, so nothing it draws is ever culled).
            minimapCamera.GetUniversalAdditionalCameraData().renderShadows = false;

            var updater = cameraGO.AddComponent<MinimapCameraUpdater>();
            var updaterSO = new SerializedObject(updater);
            updaterSO.FindProperty("minimapCamera").objectReferenceValue = minimapCamera;
            updaterSO.ApplyModifiedPropertiesWithoutUndo();

            return renderTexture;
        }

        /// <summary>
        /// The ore veins' four patch models (Tools/blender/orevein1.py, by OreKind) and the glow
        /// under them (Shaders/OreGlow). A missing model leaves that vein a glow alone.
        /// </summary>
        private static void ConfigureOreDeposits(OreDepositSpawner spawner)
        {
            var models = new GameObject[4];
            var names = new[] { "OreVeinIron", "OreVeinCoal", "OreVeinCopper", "OreVeinGold" };
            for (var i = 0; i < names.Length; i++)
            {
                models[i] = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Models/Buildings/{names[i]}.fbx");
                if (models[i] == null) Debug.LogWarning($"[SetupProject] No {names[i]}.fbx -- that vein shows only its glow.");
            }

            const string glowPath = "Assets/_Project/Materials/OreGlow.mat";
            var glow = AssetDatabase.LoadAssetAtPath<Material>(glowPath);
            if (glow == null)
            {
                var shader = Shader.Find("CityBuilder/OreGlow");
                if (shader != null)
                {
                    glow = new Material(shader) { name = "OreGlow" };
                    AssetDatabase.CreateAsset(glow, glowPath);
                }
                else Debug.LogWarning("[SetupProject] Shader CityBuilder/OreGlow not found -- the veins will not glow.");
            }

            var so = new SerializedObject(spawner);
            var array = so.FindProperty("veinModels");
            array.arraySize = models.Length;
            for (var i = 0; i < models.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = models[i];
            so.FindProperty("glowMaterial").objectReferenceValue = glow;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>The harvest bars' shared material (Shaders/HarvestBar) and their icons, handed to them by a HarvestBarSkin on the scene.</summary>
        private static void BuildHarvestBarSkin(ResourceIconLibrary icons)
        {
            const string path = "Assets/_Project/Materials/HarvestBar.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("CityBuilder/HarvestBar");
                if (shader == null) Debug.LogWarning("[SetupProject] Shader CityBuilder/HarvestBar not found -- harvest bars fall back to a plain quad.");
                else
                {
                    material = new Material(shader) { name = "HarvestBar" };
                    AssetDatabase.CreateAsset(material, path);
                }
            }

            var go = new GameObject("HarvestBarSkin");
            var skin = go.AddComponent<HarvestBarSkin>();
            var so = new SerializedObject(skin);
            so.FindProperty("material").objectReferenceValue = material;
            so.FindProperty("icons").objectReferenceValue = icons;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
