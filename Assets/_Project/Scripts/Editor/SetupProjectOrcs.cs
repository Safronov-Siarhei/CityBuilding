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
                     })
            {
                so.FindProperty(field).objectReferenceValue = Load(file);
            }

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
    }
}
