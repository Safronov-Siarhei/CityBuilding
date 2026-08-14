using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEditor;
using UnityEngine;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// Where the balance spreadsheet becomes game data.
    ///
    /// The chain is: Google Sheet (authoring, with derived columns and formatting that make the
    /// numbers mean something) -> CSV files committed in Assets/_Project/Balance -> the
    /// BalanceConfig asset the game reads.
    ///
    /// The CSVs are committed deliberately: a batchmode build on any machine, and the whole test
    /// suite, must never depend on the network or on anyone's Google session. "Pull From Google
    /// Sheets" is a manual refresh of those files, not a build step.
    /// </summary>
    public static class BalanceImporter
    {
        public const string BalanceFolder = "Assets/_Project/Balance";
        public const string UnitsCsvPath = BalanceFolder + "/units.csv";
        public const string EconomyCsvPath = BalanceFolder + "/economy.csv";
        public const string BuildingsCsvPath = BalanceFolder + "/buildings.csv";
        public const string SettingsAssetPath = BalanceFolder + "/BalanceSheetSettings.asset";
        public const string ConfigAssetPath = "Assets/_Project/Resources/BalanceConfig.asset";

        [MenuItem("CityBuilder/Balance/Rebuild Config From CSV")]
        public static void RebuildConfigMenuItem()
        {
            RebuildConfig();
            EditorUtility.DisplayDialog("Баланс", $"Конфиг пересобран из CSV:\n{ConfigAssetPath}", "OK");
        }

        /// <summary>
        /// Regenerates the BalanceConfig asset from the committed CSVs. Called by SetupProject.Run
        /// too, so a full project rebuild can never leave the asset behind the CSVs.
        /// </summary>
        public static BalanceConfig RebuildConfig()
        {
            var units = ReadUnits(UnitsCsvPath);
            var buildings = ReadBuildings(BuildingsCsvPath);
            var economy = ReadEconomy(EconomyCsvPath);

            var config = AssetDatabase.LoadAssetAtPath<BalanceConfig>(ConfigAssetPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<BalanceConfig>();
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigAssetPath) ?? string.Empty);
                AssetDatabase.CreateAsset(config, ConfigAssetPath);
            }

            config.OverwriteFrom(units, buildings, economy);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            Debug.Log($"BalanceImporter: {units.Count} units, {buildings.Count} buildings and {economy.Count} economy keys -> {ConfigAssetPath}");
            return config;
        }

        /// <summary>
        /// Downloads each sheet tab published as CSV (Sheets: Файл -> Поделиться -> Опубликовать в
        /// интернете -> вкладка -> CSV) into the committed files, then rebuilds the asset. URLs live
        /// in the BalanceSheetSettings asset next to the CSVs.
        /// </summary>
        [MenuItem("CityBuilder/Balance/Pull From Google Sheets")]
        public static void PullFromGoogleSheets()
        {
            var settings = LoadOrCreateSettings();
            if (string.IsNullOrWhiteSpace(settings.unitsCsvUrl) && string.IsNullOrWhiteSpace(settings.economyCsvUrl)
                && string.IsNullOrWhiteSpace(settings.buildingsCsvUrl))
            {
                Selection.activeObject = settings;
                EditorUtility.DisplayDialog("Баланс",
                    "Ссылки на опубликованные вкладки не заданы.\n\n" +
                    "В Google Sheets: Файл → Поделиться → Опубликовать в интернете → выбрать вкладку → формат CSV.\n" +
                    "Полученные ссылки вставь в BalanceSheetSettings (открыт в инспекторе).", "OK");
                return;
            }

            var downloaded = 0;
            downloaded += TryDownload(settings.unitsCsvUrl, UnitsCsvPath) ? 1 : 0;
            downloaded += TryDownload(settings.economyCsvUrl, EconomyCsvPath) ? 1 : 0;
            downloaded += TryDownload(settings.buildingsCsvUrl, BuildingsCsvPath) ? 1 : 0;

            AssetDatabase.Refresh();
            if (downloaded == 0)
            {
                EditorUtility.DisplayDialog("Баланс", "Ничего не скачалось — см. ошибки в консоли. CSV в проекте не тронуты.", "OK");
                return;
            }

            RebuildConfig();
            EditorUtility.DisplayDialog("Баланс", $"Обновлено вкладок: {downloaded}. Конфиг пересобран.", "OK");
        }

        private static bool TryDownload(string url, string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;

            try
            {
                // Editor-only and deliberately synchronous: this is a menu item a human just
                // clicked, and a blocking call keeps it far simpler than an editor coroutine.
                using (var client = new WebClient { Encoding = Encoding.UTF8 })
                {
                    var csv = client.DownloadString(url);
                    if (string.IsNullOrWhiteSpace(csv) || csv.TrimStart().StartsWith("<", StringComparison.Ordinal))
                    {
                        // A published CSV link returns CSV; HTML means the link points at the
                        // document's viewer instead, or the publication was revoked.
                        Debug.LogError($"BalanceImporter: {url} returned HTML rather than CSV. Re-publish that tab as CSV and copy the link it gives you.");
                        return false;
                    }

                    File.WriteAllText(destinationPath, csv, new UTF8Encoding(false));
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"BalanceImporter: could not download {url} -- {e.Message}");
                return false;
            }
        }

        private static BalanceSheetSettings LoadOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<BalanceSheetSettings>(SettingsAssetPath);
            if (settings != null) return settings;

            settings = ScriptableObject.CreateInstance<BalanceSheetSettings>();
            Directory.CreateDirectory(BalanceFolder);
            AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static List<UnitBalance> ReadUnits(string path)
        {
            var units = new List<UnitBalance>();
            var rows = ReadCsv(path);
            if (rows.Count == 0) return units;

            var header = rows[0];
            for (var i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count == 0 || string.IsNullOrWhiteSpace(row[0])) continue;
                if (IsNoteRow(row)) continue;

                units.Add(new UnitBalance
                {
                    id = Text(header, row, "id"),
                    displayName = Text(header, row, "display_name"),
                    maxHealth = (int)Number(header, row, "max_health", path),
                    attackDamage = (int)Number(header, row, "attack_damage", path),
                    attackIntervalSeconds = Number(header, row, "attack_interval_sec", path),
                    attackRangeUnits = Number(header, row, "attack_range_units", path),
                    attackRangeStructures = Number(header, row, "attack_range_structures", path),
                    walkSpeed = Number(header, row, "walk_speed", path),
                    engageRadius = Number(header, row, "engage_radius", path),
                    recruitCoins = (int)Number(header, row, "recruit_coins", path),
                    upkeepCoinsPerDay = (int)Number(header, row, "upkeep_coins_per_day", path),
                });
            }
            return units;
        }

        /// <summary>
        /// The buildings tab: one row per building, numbers only. Costs are spread across one column
        /// per resource (cost_wood, up2_stone, ...) rather than packed into a single cell, so the
        /// sheet can total and compare them -- "what does the defence line cost in stone" is a column
        /// sum, and the upgrade columns are formulas over the base cost.
        /// </summary>
        private static List<BuildingBalance> ReadBuildings(string path)
        {
            var buildings = new List<BuildingBalance>();
            var rows = ReadCsv(path);
            if (rows.Count == 0) return buildings;

            var header = rows[0];
            for (var i = 0; i < header.Count; i++)
            {
                header[i] = header[i].Trim();
            }

            for (var i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count == 0 || string.IsNullOrWhiteSpace(row[0])) continue;
                if (IsNoteRow(row)) continue;

                buildings.Add(new BuildingBalance
                {
                    id = Text(header, row, "id"),
                    displayName = Text(header, row, "display_name"),
                    category = ParseEnum(header, row, "category", BuildingCategory.Production, path),
                    cost = ReadCost(header, row, "cost_", path),
                    citizensGranted = (int)Number(header, row, "citizens_granted", path),
                    maxWorkers = (int)Number(header, row, "max_workers", path),
                    producesResource = ParseEnum(header, row, "produces", ResourceType.Wood, path),
                    productionPerWorkerPerTick = (int)Number(header, row, "production_per_tick", path),
                    productionIntervalSeconds = Number(header, row, "production_interval_sec", path),
                    maxHealth = (int)Number(header, row, "max_health", path),
                    defense = (int)Number(header, row, "defense", path),
                    fogRevealRadius = (int)Number(header, row, "fog_reveal_radius", path),
                    requiredBuildingId = Text(header, row, "requires"),
                    upgradeToLevel2Cost = ReadCost(header, row, "up2_", path),
                    upgradeToLevel3Cost = ReadCost(header, row, "up3_", path),
                });
            }
            return buildings;
        }

        /// <summary>
        /// The resource columns sharing one prefix, as a cost list. Only the five resources a
        /// building can actually cost are looked for; a blank or zero cell means "not part of this
        /// cost" and is dropped, so the generated asset carries the same short lists it always did.
        /// </summary>
        private static List<ResourceAmount> ReadCost(List<string> header, List<string> row, string prefix, string path)
        {
            var cost = new List<ResourceAmount>();
            foreach (var type in CostResources)
            {
                var amount = CostAmount(header, row, prefix + type.ToString().ToLowerInvariant(), path);
                if (amount > 0) cost.Add(new ResourceAmount { type = type, amount = amount });
            }
            return cost;
        }

        /// <summary>
        /// Resources a building can be paid for, in the order the cost list is built -- which is the
        /// order the UI shows them in, so it stays the same for every building.
        /// </summary>
        private static readonly ResourceType[] CostResources =
        {
            ResourceType.Wood, ResourceType.Stone, ResourceType.Iron, ResourceType.Coal, ResourceType.Gold,
        };

        /// <summary>Quiet about an absent column or an empty cell (that is simply a cost this building doesn't have), loud about a cell holding something that isn't a number.</summary>
        private static int CostAmount(List<string> header, List<string> row, string column, string path)
        {
            if (header.IndexOf(column) < 0) return 0;

            var raw = Text(header, row, column);
            if (raw.Length == 0) return 0;
            if (TryParseNumber(raw, out var value)) return (int)value;

            Debug.LogError($"BalanceImporter: {path} has '{raw}' in column '{column}', which is not a number. Treating it as 0.");
            return 0;
        }

        /// <summary>An enum-valued cell read by name ("Housing", "Food"). Empty keeps the fallback -- a building with no production names no resource.</summary>
        private static TEnum ParseEnum<TEnum>(List<string> header, List<string> row, string column, TEnum fallback, string path)
            where TEnum : struct
        {
            var raw = Text(header, row, column);
            if (raw.Length == 0) return fallback;
            if (Enum.TryParse(raw, true, out TEnum parsed)) return parsed;

            Debug.LogError($"BalanceImporter: {path} column '{column}' has '{raw}', which is not a valid {typeof(TEnum).Name} " +
                           $"(expected one of: {string.Join(", ", Enum.GetNames(typeof(TEnum)))}). Using {fallback}.");
            return fallback;
        }

        /// <summary>
        /// The economy tab as key -> number. Columns are found by their header name ("key",
        /// "value"), not by position, so the sheet is free to grow derived columns -- a published
        /// CSV exports every column of the tab, including formula results, and those must be able
        /// to sit anywhere without breaking the import.
        /// </summary>
        private static Dictionary<string, float> ReadEconomy(string path)
        {
            var values = new Dictionary<string, float>();
            var rows = ReadCsv(path);
            if (rows.Count == 0) return values;

            var header = rows[0];
            for (var i = 0; i < header.Count; i++)
            {
                header[i] = header[i].Trim();
            }

            var keyColumn = header.IndexOf("key");
            var valueColumn = header.IndexOf("value");
            if (keyColumn < 0 || valueColumn < 0)
            {
                Debug.LogError($"BalanceImporter: {path} needs columns named 'key' and 'value' (found: {string.Join(", ", header)}).");
                return values;
            }

            for (var i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count <= valueColumn || string.IsNullOrWhiteSpace(row[keyColumn])) continue;
                if (IsNoteRow(row)) continue;

                var key = row[keyColumn].Trim();
                if (!TryParseNumber(row[valueColumn], out var value))
                {
                    Debug.LogError($"BalanceImporter: {path} row {i + 1} ('{key}') has a non-numeric value '{row[valueColumn]}'. Skipped.");
                    continue;
                }
                values[key] = value;
            }
            return values;
        }

        /// <summary>
        /// A note the author left under the table, not data. The sheet's tabs end with prose ("жёлтые
        /// ячейки — правишь ты"), and a published CSV exports it as an ordinary row whose first cell
        /// -- the id/key column -- is full of text. Such a row is recognised by everything *after*
        /// the first cell being empty, which no real row ever is: a unit always has a display name,
        /// and an economy key always has either a value or a comment beside it. That way an
        /// accidentally emptied value in a real row still shouts instead of being mistaken for prose.
        /// </summary>
        private static bool IsNoteRow(List<string> row)
        {
            for (var i = 1; i < row.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(row[i])) return false;
            }
            return true;
        }

        private static string Text(List<string> header, List<string> row, string column)
        {
            var index = header.IndexOf(column);
            return index >= 0 && index < row.Count ? row[index].Trim() : string.Empty;
        }

        private static float Number(List<string> header, List<string> row, string column, string path)
        {
            var raw = Text(header, row, column);
            if (TryParseNumber(raw, out var value)) return value;

            Debug.LogError($"BalanceImporter: {path} has no usable number in column '{column}' (got '{raw}'). Using 0.");
            return 0f;
        }

        /// <summary>Accepts both 1.5 and 1,5 -- a sheet edited in a Russian locale exports decimal commas, and silently reading those as zero would be a nasty way to lose a balance change.</summary>
        private static bool TryParseNumber(string raw, out float value)
        {
            raw = (raw ?? string.Empty).Trim();
            if (raw.Length == 0)
            {
                value = 0f;
                return false;
            }

            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                   || float.TryParse(raw.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>Minimal CSV reader: quoted fields (so a comment may contain commas) and doubled quotes inside them. Anything fancier belongs in the sheet, not here.</summary>
        private static List<List<string>> ReadCsv(string path)
        {
            var rows = new List<List<string>>();
            if (!File.Exists(path))
            {
                Debug.LogError($"BalanceImporter: {path} is missing -- the balance config cannot be built without it.");
                return rows;
            }

            foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var fields = new List<string>();
                var field = new StringBuilder();
                var inQuotes = false;

                for (var i = 0; i < line.Length; i++)
                {
                    var c = line[i];
                    if (inQuotes)
                    {
                        if (c != '"')
                        {
                            field.Append(c);
                        }
                        else if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                        continue;
                    }

                    if (c == '"') inQuotes = true;
                    else if (c == ',') { fields.Add(field.ToString()); field.Clear(); }
                    else field.Append(c);
                }
                fields.Add(field.ToString());
                rows.Add(fields);
            }

            return rows;
        }
    }
}
