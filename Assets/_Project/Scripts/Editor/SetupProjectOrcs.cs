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
    /// The orc faction's models handed to OrcRaidManager.
    /// </summary>
    public static partial class SetupProject
    {
        /// <summary>
        /// Hands the raid manager the orc faction's Blender models (Tools/blender/orc*.py, exported
        /// to Models/Orcs): the portal's two looks and its ruin, the grunt and the brute, and the
        /// camp's buildings with their ruins and the palisade piece. A missing
        /// file only warns -- the manager falls back to the cube figure it replaced.
        /// </summary>
        /// <summary>A model of the Invasion jungle (Models/Wild: the monsters and the shrine).</summary>
        private static GameObject LoadWild(string file)
        {
            var path = $"Assets/_Project/Models/Wild/{file}.fbx";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null) Debug.LogWarning($"[SetupProject] {path} is missing -- run Tools/blender/wild.py and export_fbx.py. The jungle falls back to cubes there.");
            return model;
        }

        private static void WireOrcModels(OrcRaidManager raidManager)
        {
            const string folder = "Assets/_Project/Models/Orcs";
            var so = new SerializedObject(raidManager);

            GameObject Load(string file)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{file}.fbx");
                if (model == null) Debug.LogWarning($"[SetupProject] {folder}/{file}.fbx is missing -- run Tools/blender/export_fbx.py. The orcs fall back to cubes there.");
                return model;
            }

            foreach (var (field, file) in new[]
                     {
                         ("portalClosedModel", "OrcPortal1-lvl1-Closed"), ("throwerModel", "OrcThrower"), ("chiefModel", "OrcChief"),
                         ("thiefModel", "OrcThief"), ("torchbearerModel", "OrcTorchbearer"), ("iceShamanModel", "OrcIceShaman"),
                         ("watchtowerRuinModel", "OrcWatchtower1-lvl1-Ruin"),
                         ("warTentModel", "OrcWarTent1-lvl1"), ("warTentRuinModel", "OrcWarTent1-lvl1-Ruin"),
                         ("totemRuinModel", "OrcTotem1-lvl1-Ruin"), ("gateRuinModel", "OrcGate1-lvl1-Ruin"),
                         ("gateTowerModel", "OrcGateTower1-lvl1"), ("gateTowerRuinModel", "OrcGateTower1-lvl1-Ruin"),
                         ("storehouseRuinModel", "OrcStorehouse1-lvl1-Ruin"),
                         ("brutePitModel", "OrcBrutePit1-lvl1"), ("brutePitRuinModel", "OrcBrutePit1-lvl1-Ruin"),
                         ("throwerRangeModel", "OrcThrowerRange1-lvl1"), ("throwerRangeRuinModel", "OrcThrowerRange1-lvl1-Ruin"),
                         // the main base's own set
                         ("mainPortalModel", "OrcMainPortal1"), ("mainPortalRuinModel", "OrcMainPortal1-Ruin"),
                         ("mainGateModel", "OrcStoneGate1"), ("mainGateRuinModel", "OrcStoneGate1-Ruin"),
                         ("mainTowerModel", "OrcStoneTower1"), ("mainTowerRuinModel", "OrcStoneTower1-Ruin"),
                         ("mainTotemModel", "OrcMainTotem1"), ("mainTotemRuinModel", "OrcMainTotem1-Ruin"),
                         ("mainStorehouseModel", "OrcMainStorehouse1"), ("mainStorehouseRuinModel", "OrcMainStorehouse1-Ruin"),
                         ("mainBarracksModel", "OrcMainBarracks1"), ("mainBarracksRuinModel", "OrcMainBarracks1-Ruin"),
                         ("mainBrutePitModel", "OrcMainBrutePit1"), ("mainBrutePitRuinModel", "OrcMainBrutePit1-Ruin"),
                         ("mainThrowerRangeModel", "OrcMainThrowerRange1"), ("mainThrowerRangeRuinModel", "OrcMainThrowerRange1-Ruin"),
                         ("chiefHouseModel", "OrcChiefHouse1"), ("chiefHouseRuinModel", "OrcChiefHouse1-Ruin"),
                         ("bossModel", "OrcChief-Boss"),
                         ("cageRuinModel", "OrcCage1-lvl1-Ruin"),
                         ("mainCageModel", "OrcMainCage1"), ("mainCageRuinModel", "OrcMainCage1-Ruin"),
                         ("mainTreasuryModel", "OrcMainTreasury1"), ("mainTreasuryRuinModel", "OrcMainTreasury1-Ruin"),
                         // the Invasion mode's round camps
                         ("ringWallModel", "OrcRingWall"), ("ringWallRuinModel", "OrcRingWall-Ruin"), ("ringGateModel", "OrcRingGate"),
                     })
            {
                so.FindProperty(field).objectReferenceValue = Load(file);
            }

            // the Invasion jungle's monsters (Tools/blender/wild.py)
            foreach (var (field, file) in new[] { ("wolfModel", "WildWolf"), ("bearModel", "WildBear"), ("trollModel", "WildTroll"), ("giantModel", "WildGiant") })
            {
                so.FindProperty(field).objectReferenceValue = LoadWild(file);
            }

            // the dungeon's monsters (Tools/blender/monsters.py)
            foreach (var (field, file) in new[]
                     {
                         ("ratModel", "MonsterRat"), ("batModel", "MonsterBat"), ("spiderModel", "MonsterSpider"),
                         ("spiderlingModel", "MonsterSpiderling"), ("skeletonModel", "MonsterSkeleton"), ("koboldModel", "MonsterKobold"),
                         ("stoneGolemModel", "MonsterStoneGolem"), ("spiderQueenModel", "MonsterSpiderQueen"),
                         ("spiderCocoonModel", "MonsterSpiderCocoon"), ("oreGolemModel", "MonsterOreGolem"),
                         ("rewardCrystalModel", "MonsterRewardCrystal"),
                         // the level-2 dungeon's (Tools/blender/monsters2.py)
                         ("drownedRatModel", "MonsterDrownedRat"), ("dwarfSkeletonModel", "MonsterDwarfSkeleton"),
                         ("stoneGuardModel", "MonsterStoneGuard"), ("drownedDwarfModel", "MonsterDrownedDwarf"),
                         ("eelModel", "MonsterEel"), ("forgeSpiritModel", "MonsterForgeSpirit"),
                         ("dwarfKingModel", "MonsterDwarfKing"), ("canalSerpentModel", "MonsterCanalSerpent"),
                         // the level-3 dungeon's (Tools/blender/monsters3.py)
                         ("fireImpModel", "MonsterFireImp"), ("lavaHoundModel", "MonsterLavaHound"), ("salamanderModel", "MonsterSalamander"), ("obsidianGolemModel", "MonsterObsidianGolem"), ("emberCultistModel", "MonsterEmberCultist"), ("magmaSpawnModel", "MonsterMagmaSpawn"), ("dragonModel", "MonsterDragon"), ("lavaTitanModel", "MonsterLavaTitan"),
                     })
            {
                var path = $"Assets/_Project/Models/Monsters/{file}.fbx";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null) Debug.LogWarning($"[SetupProject] {path} is missing -- run Tools/blender/monsters.py and export_fbx.py.");
                so.FindProperty(field).objectReferenceValue = model;
            }
            // the bot kingdom's soldiers at war: the player's soldiers' level-2 figures (Bot/BotWar)
            foreach (var (field, file) in new[] { ("botSoldierModel", "SoldierManAtArms-lvl2"), ("botArcherModel", "SoldierArcher-lvl2") })
            {
                var path = $"Assets/_Project/Models/Soldiers/{file}.fbx";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null) Debug.LogWarning($"[SetupProject] {path} is missing.");
                so.FindProperty(field).objectReferenceValue = model;
            }
            // Стражи Глубин, the archipelago caches' guards (Tools/blender/tide.py)
            foreach (var (field, file) in new[]
                     {
                         ("tideCrabModel", "TideCrab"), ("tideSharkModel", "TideShark"), ("tideShamanModel", "TideOctopus"),
                         ("tidePufferModel", "TidePuffer"), ("tideSerpentModel", "TideSerpent"),
                     })
            {
                var path = $"Assets/_Project/Models/Tide/{file}.fbx";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null) Debug.LogWarning($"[SetupProject] {path} is missing -- run Tools/blender/tide.py and export_fbx.py.");
                so.FindProperty(field).objectReferenceValue = model;
            }
            // the upgrade crystal's look (the user, 2026-11-01: bright, glowing, juicy -- it is paid content)
            so.FindProperty("crystalMaterial").objectReferenceValue = CrystalMaterial("Crystal", "CityBuilder/Crystal", null);
            so.FindProperty("crystalHaloMaterial").objectReferenceValue = CrystalMaterial("CrystalHalo", "CityBuilder/CrystalHalo", null);
            so.FindProperty("crystalGroundMaterial").objectReferenceValue =
                CrystalMaterial("CrystalGroundGlow", "CityBuilder/OreGlow", new Color(0.35f, 0.75f, 1f, 1f));
            // the orc portal's life (the user, 2026-11-01: A + B + D -- whirlpool, glow and runes, a flare per wave)
            so.FindProperty("portalVortexMaterial").objectReferenceValue = CrystalMaterial("PortalVortex", "CityBuilder/PortalVortex", null);
            so.FindProperty("portalRunesMaterial").objectReferenceValue = CrystalMaterial("PortalRunes", "CityBuilder/PortalRunes", null);
            var portalHalo = CrystalMaterial("PortalHalo", "CityBuilder/CrystalHalo", new Color(0.55f, 0.18f, 0.95f, 1f));
            if (portalHalo != null) portalHalo.SetColor("_SparkColor", new Color(0.6f, 1f, 0.4f, 1f));
            so.FindProperty("portalHaloMaterial").objectReferenceValue = portalHalo;
            // a dungeon's darkness (the user, 2026-11-01)
            so.FindProperty("dungeonDarkMaterial").objectReferenceValue = CrystalMaterial("DungeonDark", "CityBuilder/DungeonDark", null);
            so.FindProperty("sleepMaterial").objectReferenceValue = CrystalMaterial("SleepZ", "CityBuilder/SleepZ", null);
            // the living coasts of the archipelago (the user, 2026-11-02)
            so.FindProperty("shoreSurfMaterial").objectReferenceValue = CrystalMaterial("ShoreSurf", "CityBuilder/ShoreSurf", null);
            // the orcs' swamp island (the user, 2026-11-06): its haze, and the skeleton and dress models
            so.FindProperty("swampHazeMaterial").objectReferenceValue = CrystalMaterial("SwampHaze", "CityBuilder/SwampHaze", null);
            foreach (var (field, file) in new[] { ("skullGateModel", "OrcSkullGate1"), ("vertebraModel", "OrcVertebra1"),
                         ("skullStakeModel", "OrcSkullStake1"), ("wreckModel", "OrcWreck1"), ("crowModel", "OrcCrow1"),
                         // the main base's decor (living variant A, approved 2026-11-06)
                         ("campfireModel", "OrcCampfire1"), ("dummyModel", "OrcDummy1"), ("cauldronModel", "OrcCauldron1"),
                         ("meatRackModel", "OrcMeatRack1"), ("hideRackModel", "OrcHideRack1"), ("warDrumModel", "OrcWarDrum1"),
                         ("boneThroneModel", "OrcBoneThrone1"), ("runeStoneModel", "OrcRuneStone1"),
                         // «Гать»'s own (approved 2026-11-06)
                         ("causewayModel", "OrcCauseway1"), ("stiltHutModel", "OrcStiltHut1"), ("raftModel", "OrcRaft1"),
                         ("bogLanternModel", "OrcBogLantern1"), ("reedsModel", "OrcReeds1") })
                so.FindProperty(field).objectReferenceValue = Load(file);

            // By level, index 0 = level 1 (see OrcRaidManager).
            var fences = new List<string>();
            foreach (var level in new[] { 1, 2 })
            {
                foreach (var shape in new[] { "Straight", "Corner", "T", "Cross" }) fences.Add($"OrcFence1-lvl{level}-{shape}");
            }
            foreach (var (field, files) in new[]
                     {
                         ("portalActiveModels", new[] { "OrcPortal1-lvl1", "OrcPortal1-lvl2", "OrcPortal1-lvl3" }),
                         ("portalPassiveModels", new[] { "OrcPortal1-lvl1-Passive", "OrcPortal1-lvl2-Passive", "OrcPortal1-lvl3-Passive" }),
                         ("gruntModels", new[] { "OrcGrunt", "OrcGrunt-lvl2", "OrcGrunt-lvl3" }),
                         ("bruteModels", new[] { "OrcBrute", "OrcBrute-lvl2" }),
                         ("watchtowerModels", new[] { "OrcWatchtower1-lvl1", "OrcWatchtower1-lvl2", "OrcWatchtower1-lvl3" }),
                         ("totemModels", new[] { "OrcTotem1-lvl1", "OrcTotem1-lvl2" }),
                         ("gateModels", new[] { "OrcGate1-lvl1", "OrcGate1-lvl2" }),
                         ("storehouseModels", new[] { "OrcStorehouse1-lvl1", "OrcStorehouse1-lvl2" }),
                         ("cageModels", new[] { "OrcCage1-lvl1", "OrcCage1-lvl2", "OrcCage1-lvl3" }),
                         ("palisadeModels", fences.ToArray()),
                         ("mainWallModels", new[] { "OrcStoneWall1-Straight", "OrcStoneWall1-Corner", "OrcStoneWall1-T", "OrcStoneWall1-Cross" }),
                         ("mountainModels", new[] { "OrcMountain1-A", "OrcMountain1-B" }),
                         ("ribModels", new[] { "OrcRib1-A", "OrcRib1-B" }),
                         ("deadTreeModels", new[] { "OrcDeadTree1-A", "OrcDeadTree1-B", "OrcDeadTree1-C" }),
                     })
            {
                var property = so.FindProperty(field);
                property.arraySize = files.Length;
                for (var i = 0; i < files.Length; i++)
                {
                    property.GetArrayElementAtIndex(i).objectReferenceValue = Load(files[i]);
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>A material asset for the crystal on `shader` (made once; a material asset is what takes the shader into a build).</summary>
        private static Material CrystalMaterial(string name, string shader, Color? colour)
        {
            var path = $"Assets/_Project/Materials/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var found = Shader.Find(shader);
                if (found == null)
                {
                    Debug.LogWarning($"[SetupProject] Shader {shader} not found -- the crystals will not glow.");
                    return null;
                }
                material = new Material(found) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            if (colour.HasValue) material.SetColor("_BaseColor", colour.Value);
            return material;
        }
    }
}
