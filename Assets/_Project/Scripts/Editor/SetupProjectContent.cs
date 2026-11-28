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
    /// and one fast-follow group per <see cref="ContentPacks.Pack"/> (the user: «поэтапно»): the map definitions of the
    /// pack folders by ContentPacks.PackForMap, the kits by the mode they serve; whatever they reference (meshes, decor)
    /// rides in the same pack.
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

            // one Addressables group = one fast-follow Play pack per ContentPacks.Pack, asked for in that order
            var packs = (ContentPacks.Pack[])System.Enum.GetValues(typeof(ContentPacks.Pack));
            var groups = new Dictionary<ContentPacks.Pack, AddressableAssetGroup>();
            var wanted = new Dictionary<ContentPacks.Pack, HashSet<string>>();
            foreach (var pack in packs)
            {
                groups[pack] = PackGroup(settings, ContentPacks.NameOf(pack));
                wanted[pack] = new HashSet<string>();
                if (pack != ContentPacks.Pack.Sandbox) settings.AddLabel(ContentPacks.MapLabelOf(pack), false);
            }
            // the single big pack of the first version of this (2026-11-28 morning) is gone
            var old = settings.FindGroup("LaterContent");
            if (old != null) settings.RemoveGroup(old);

            // the map definitions: each to the pack ContentPacks.PackForMap names, labelled for MeshMapCatalog
            foreach (var folder in new[] { MeshMapCatalog.FirstFolder, MeshMapCatalog.LaterFolder })
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                foreach (var guid in AssetDatabase.FindAssets("t:MeshMapDefinition", new[] { folder }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var mapId = Path.GetFileNameWithoutExtension(path);
                    var pack = ContentPacks.PackForMap(mapId);
                    if (pack == null) { Debug.LogError($"SetupProjectContent: {path} is in a pack folder but ContentPacks.PackForMap does not know it"); continue; }
                    wanted[pack.Value].Add(guid);
                    var entry = settings.CreateOrMoveEntry(guid, groups[pack.Value], false, false);
                    entry.address = mapId;
                    entry.SetLabel(ContentPacks.MapLabelOf(pack.Value), true, true, false);
                }
            }
            // the kits, each in the pack of the mode it serves
            foreach (var (kitPath, kitAddress, pack) in new[]
                     {
                         (CityBuilder.Maps.Archipelago.ArchipelagoLibrary.LaterPath, CityBuilder.Maps.Archipelago.ArchipelagoLibrary.LaterAddress, ContentPacks.Pack.Sandbox),
                         (CityBuilder.Buildings.BuildingLevelKit.Path, CityBuilder.Buildings.BuildingLevelKit.Address, ContentPacks.Pack.Sandbox),
                         (CityBuilder.UI.LevelIconKit.Path, CityBuilder.UI.LevelIconKit.Address, ContentPacks.Pack.Sandbox),
                         (CityBuilder.Campaign.CampaignFilmKit.Path, CityBuilder.Campaign.CampaignFilmKit.Address, ContentPacks.Pack.Campaign),
                     })
            {
                var kitGuid = AssetDatabase.AssetPathToGUID(kitPath);
                if (string.IsNullOrEmpty(kitGuid)) { Debug.LogError($"SetupProjectContent: no {kitPath}"); continue; }
                wanted[pack].Add(kitGuid);
                settings.CreateOrMoveEntry(kitGuid, groups[pack], false, false).address = kitAddress;
            }
            var report = new List<string>();
            foreach (var pack in packs)
            {
                DropStale(groups[pack], wanted[pack]);
                report.Add($"{ContentPacks.NameOf(pack)} = {wanted[pack].Count}");
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("SetupProjectContent: " + string.Join(", ", report) + " (fast-follow)");
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
