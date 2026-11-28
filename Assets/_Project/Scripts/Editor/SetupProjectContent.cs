using System.Collections.Generic;
using System.IO;
using CityBuilder.Core;
using CityBuilder.Maps;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Android;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets.Android;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The small first download (the user, 2026-11-28; Design/features/download-size.md): Addressables with Play Asset
    /// Delivery, set up from code the way the package's «Init Play Asset Delivery» menu does it (no dialog in batchmode),
    /// and ONE fast-follow group, <see cref="ContentPacks.PackName"/>, holding what the install leaves out. Today: the map
    /// definitions SetupProject writes to <see cref="MeshMapCatalog.LaterFolder"/> (every dungeon but the first), each
    /// labelled <see cref="ContentPacks.MapLabel"/>; whatever they reference (meshes, decor) rides in the same pack.
    /// Addressables are built with the player (BuildWithPlayer), so build_android.ps1 needs no extra step.
    /// </summary>
    public static class SetupProjectContent
    {
        private const string PadFolder = "Assets/AddressableAssetsData/PlayAssetDelivery";

        public static void Apply()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null) { Debug.LogError("SetupProjectContent: no Addressables settings"); return; }

            if (settings.InitializationObjects.FindIndex(i => i is PlayAssetDeliveryInitializationSettings) < 0)
                settings.AddInitializationObject(ScriptAsset<PlayAssetDeliveryInitializationSettings>("InitObjects"));
            if (settings.DataBuilders.FindIndex(b => b is BuildScriptPlayAssetDelivery) < 0)
                settings.AddDataBuilder(ScriptAsset<BuildScriptPlayAssetDelivery>("DataBuilders"));
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;

            var group = settings.FindGroup(ContentPacks.PackName)
                        ?? settings.CreateGroup(ContentPacks.PackName, false, false, true, null,
                            typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema), typeof(PlayAssetDeliverySchema));
            var pad = group.GetSchema<PlayAssetDeliverySchema>() ?? group.AddSchema<PlayAssetDeliverySchema>();
            pad.IncludeInCustomAssetPack = false;
            pad.AssetPackDeliveryType = DeliveryType.FastFollow;
            var bundled = group.GetSchema<BundledAssetGroupSchema>() ?? group.AddSchema<BundledAssetGroupSchema>();
            bundled.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            bundled.IncludeInBuild = true;

            settings.AddLabel(ContentPacks.MapLabel, false);
            var wanted = new HashSet<string>();
            if (AssetDatabase.IsValidFolder(MeshMapCatalog.LaterFolder))
                foreach (var guid in AssetDatabase.FindAssets("t:MeshMapDefinition", new[] { MeshMapCatalog.LaterFolder }))
                {
                    wanted.Add(guid);
                    var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                    entry.address = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
                    entry.SetLabel(ContentPacks.MapLabel, true, true, false);
                }
            // the archipelago's later islands (all but the start map's and the tutorial's), looked up by address
            var islandsGuid = AssetDatabase.AssetPathToGUID(CityBuilder.Maps.Archipelago.ArchipelagoLibrary.LaterPath);
            if (!string.IsNullOrEmpty(islandsGuid))
            {
                wanted.Add(islandsGuid);
                var entry = settings.CreateOrMoveEntry(islandsGuid, group, false, false);
                entry.address = CityBuilder.Maps.Archipelago.ArchipelagoLibrary.LaterAddress;
            }
            // campaign missions 2-3's film pieces (SetupProjectCityScene)
            var kitGuid = AssetDatabase.AssetPathToGUID(CityBuilder.Campaign.CampaignFilmKit.Path);
            if (!string.IsNullOrEmpty(kitGuid))
            {
                wanted.Add(kitGuid);
                settings.CreateOrMoveEntry(kitGuid, group, false, false).address = CityBuilder.Campaign.CampaignFilmKit.Address;
            }
            var stale = new List<AddressableAssetEntry>();
            foreach (var entry in group.entries) if (!wanted.Contains(entry.guid)) stale.Add(entry);
            foreach (var entry in stale) group.RemoveAssetEntry(entry, false);

            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(group);
            AssetDatabase.SaveAssets();
            Debug.Log($"SetupProjectContent: {ContentPacks.PackName} = {wanted.Count} later maps (fast-follow)");
        }

        private static T ScriptAsset<T>(string subfolder) where T : ScriptableObject
        {
            var folder = $"{PadFolder}/{subfolder}";
            Directory.CreateDirectory(folder);
            var path = $"{folder}/{typeof(T).Name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
