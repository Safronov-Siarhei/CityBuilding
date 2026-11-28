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

            // the small FIRST pack (asked for first): the start map's level-1 dungeon
            var firstGroup = PackGroup(settings, ContentPacks.FirstPackName);
            settings.AddLabel(MeshMapCatalog.FirstMapLabel, false);
            var firstWanted = new HashSet<string>();
            AddMaps(settings, firstGroup, MeshMapCatalog.FirstFolder, MeshMapCatalog.FirstMapLabel, firstWanted);
            DropStale(firstGroup, firstWanted);

            // the big pack: every other map, the later islands, the campaign's films, the sandbox-only level models and icons
            var group = PackGroup(settings, ContentPacks.PackName);
            settings.AddLabel(ContentPacks.MapLabel, false);
            var wanted = new HashSet<string>();
            AddMaps(settings, group, MeshMapCatalog.LaterFolder, ContentPacks.MapLabel, wanted);
            foreach (var (kitPath, kitAddress) in new[]
                     {
                         (CityBuilder.Maps.Archipelago.ArchipelagoLibrary.LaterPath, CityBuilder.Maps.Archipelago.ArchipelagoLibrary.LaterAddress),
                         (CityBuilder.Campaign.CampaignFilmKit.Path, CityBuilder.Campaign.CampaignFilmKit.Address),
                         (CityBuilder.Buildings.BuildingLevelKit.Path, CityBuilder.Buildings.BuildingLevelKit.Address),
                         (CityBuilder.UI.LevelIconKit.Path, CityBuilder.UI.LevelIconKit.Address),
                     })
            {
                var kitGuid = AssetDatabase.AssetPathToGUID(kitPath);
                if (string.IsNullOrEmpty(kitGuid)) continue;
                wanted.Add(kitGuid);
                settings.CreateOrMoveEntry(kitGuid, group, false, false).address = kitAddress;
            }
            DropStale(group, wanted);

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"SetupProjectContent: {ContentPacks.FirstPackName} = {firstWanted.Count}, {ContentPacks.PackName} = {wanted.Count} entries (fast-follow)");
        }

        private static AddressableAssetGroup PackGroup(AddressableAssetSettings settings, string name)
        {
            var group = settings.FindGroup(name)
                        ?? settings.CreateGroup(name, false, false, true, null,
                            typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema), typeof(PlayAssetDeliverySchema));
            var pad = group.GetSchema<PlayAssetDeliverySchema>() ?? group.AddSchema<PlayAssetDeliverySchema>();
            pad.IncludeInCustomAssetPack = false;
            pad.AssetPackDeliveryType = DeliveryType.FastFollow;
            var bundled = group.GetSchema<BundledAssetGroupSchema>() ?? group.AddSchema<BundledAssetGroupSchema>();
            bundled.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            bundled.IncludeInBuild = true;
            EditorUtility.SetDirty(group);
            return group;
        }

        private static void AddMaps(AddressableAssetSettings settings, AddressableAssetGroup group, string folder, string label, HashSet<string> wanted)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return;
            foreach (var guid in AssetDatabase.FindAssets("t:MeshMapDefinition", new[] { folder }))
            {
                wanted.Add(guid);
                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                entry.address = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
                entry.SetLabel(label, true, true, false);
            }
        }

        private static void DropStale(AddressableAssetGroup group, HashSet<string> wanted)
        {
            var stale = new List<AddressableAssetEntry>();
            foreach (var entry in group.entries) if (!wanted.Contains(entry.guid)) stale.Add(entry);
            foreach (var entry in stale) group.RemoveAssetEntry(entry, false);
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
