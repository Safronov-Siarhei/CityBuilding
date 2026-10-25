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
        public const string RecipesCsvPath = BalanceFolder + "/recipes.csv";
        public const string LocalizationCsvPath = BalanceFolder + "/localization.csv";
        public const string LocalizationAssetPath = "Assets/_Project/Resources/LocalizationConfig.asset";
        public const string SettingsAssetPath = BalanceFolder + "/BalanceSheetSettings.asset";
        public const string ConfigAssetPath = "Assets/_Project/Resources/BalanceConfig.asset";
        /// <summary>The sheet's invasion tab: the Invasion mode's own numbers, laid over the other tabs for a run.</summary>
        public const string InvasionCsvPath = BalanceFolder + "/invasion.csv";
        public const string RunConfigAssetPath = "Assets/_Project/Resources/" + BalanceConfig.RunResourcePath + ".asset";

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
            var unitRows = ReadCsv(UnitsCsvPath);
            var buildingRows = ReadCsv(BuildingsCsvPath);
            var recipes = ReadRecipes(RecipesCsvPath);
            var units = ReadUnits(unitRows, UnitsCsvPath);
            var buildings = ReadBuildings(buildingRows, BuildingsCsvPath);
            AttachRecipes(buildings, recipes);
            var economy = ReadEconomy(ReadCsv(EconomyCsvPath), EconomyCsvPath);
            var invasion = InvasionTab.Read(InvasionCsvPath);

            // The sandbox has the run's own keys too (the heroes', the waves'...), so anything that
            // asks for one outside a run -- an EditMode test of the wave ramp -- reads the sheet's
            // value; the invasion tab's overrides of sandbox keys stay the run's alone.
            var sandboxEconomy = new Dictionary<string, float>(economy);
            foreach (var pair in invasion.Keys)
            {
                if (!sandboxEconomy.ContainsKey(pair.Key)) sandboxEconomy[pair.Key] = pair.Value;
            }
            var config = WriteConfig(ConfigAssetPath, units, buildings, sandboxEconomy);

            // The run's config: the same tabs with the invasion tab's cells laid over them.
            var runUnits = ReadUnits(invasion.LayOver(unitRows, invasion.Units, InvasionCsvPath), InvasionCsvPath);
            var runBuildings = ReadBuildings(invasion.LayOver(buildingRows, invasion.Buildings, InvasionCsvPath, wholeRow: true), InvasionCsvPath);
            AttachRecipes(runBuildings, recipes);
            var runEconomy = new Dictionary<string, float>(economy);
            foreach (var pair in invasion.Keys) runEconomy[pair.Key] = pair.Value;
            WriteConfig(RunConfigAssetPath, runUnits, runBuildings, runEconomy);

            var localizationKeys = RebuildLocalization();
            AssetDatabase.SaveAssets();

            Debug.Log($"BalanceImporter: {units.Count} units, {buildings.Count} buildings, {economy.Count} economy keys " +
                      $"and {localizationKeys} localization keys -> {ConfigAssetPath}; the invasion tab's " +
                      $"{Math.Max(0, invasion.Buildings.Count - 1)} buildings, {Math.Max(0, invasion.Units.Count - 1)} units " +
                      $"and {invasion.Keys.Count} keys -> {RunConfigAssetPath}");
            return config;
        }

        private static BalanceConfig WriteConfig(string assetPath, List<UnitBalance> units, List<BuildingBalance> buildings, Dictionary<string, float> economy)
        {
            var config = AssetDatabase.LoadAssetAtPath<BalanceConfig>(assetPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<BalanceConfig>();
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath) ?? string.Empty);
                AssetDatabase.CreateAsset(config, assetPath);
            }

            config.OverwriteFrom(units, buildings, economy);
            EditorUtility.SetDirty(config);
            return config;
        }

        /// <summary>
        /// The sheet's invasion tab (the user, 2026-10-23: the whole mode on a sheet of its own).
        /// Three tables one under another, each opened by a header row whose first cell names it:
        /// `building` (columns of the buildings tab, plus max_count), `unit` (columns of the units
        /// tab) and `key` (key, value, comment -- like economy). A building's or a unit's row
        /// REPLACES, for a run, the cells of the columns its table has -- an empty cell is an empty
        /// cell, as in the buildings tab (a higher level repeats the one below, a price is nothing);
        /// a column the units table does not have keeps the sandbox's value; a BUILDING of the run is
        /// the buildings table's row and nothing else (the user, 2026-10-24: the two modes must not
        /// touch -- a column the table lacks is empty for it). A price the table sets is the
        /// whole price: the run's upgrade into level 2 is up2_wood/up2_stone/up2_gold as written,
        /// not those plus the sandbox's iron. A key replaces the economy tab's, or adds a run-only one.
        /// </summary>
        private class InvasionTab
        {
            /// <summary>Each table header first, as the tab has it; empty when the tab has no such table.</summary>
            public readonly List<List<string>> Buildings = new List<List<string>>();
            public readonly List<List<string>> Units = new List<List<string>>();
            public readonly Dictionary<string, float> Keys = new Dictionary<string, float>();

            /// <summary>The price columns' prefixes: one in a table makes the table the whole price for it.</summary>
            private static readonly string[] Prices = { "cost_", "up2_", "up3_", "up4_", "up5_", "recruit_" };

            public static InvasionTab Read(string path)
            {
                var tab = new InvasionTab();
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"BalanceImporter: no {path} -- an Invasion run plays on the sandbox's numbers.");
                    return tab;
                }

                List<List<string>> table = null;
                List<List<string>> keys = null;
                foreach (var row in ReadCsv(path))
                {
                    var first = row.Count > 0 ? row[0].Trim().TrimStart('\uFEFF') : string.Empty;
                    switch (first)
                    {
                        case "building": table = tab.Buildings; table.Clear(); table.Add(Trimmed(row)); continue;
                        case "unit": table = tab.Units; table.Clear(); table.Add(Trimmed(row)); continue;
                        case "key": keys = new List<List<string>> { Trimmed(row) }; table = keys; continue;
                    }
                    if (table == null || first.Length == 0 || IsNoteRow(row)) continue;
                    table.Add(row);
                }
                if (keys != null)
                {
                    foreach (var pair in ReadEconomy(keys, path)) tab.Keys[pair.Key] = pair.Value;
                }
                return tab;
            }

            private static List<string> Trimmed(List<string> row)
            {
                var copy = new List<string>(row.Count);
                foreach (var cell in row) copy.Add(cell.Trim().TrimStart('\uFEFF'));
                return copy;
            }

            /// <summary>The sandbox's tab (header first) with this table's rows laid over the rows of the same id.</summary>
            /// <param name="wholeRow">True for the buildings: a column the table lacks is emptied, never the sandbox's.</param>
            public List<List<string>> LayOver(List<List<string>> sandbox, List<List<string>> table, string path, bool wholeRow = false)
            {
                if (sandbox.Count == 0 || table.Count == 0) return sandbox;

                var header = Trimmed(sandbox[0]);
                var tableHeader = table[0];
                var into = new int[tableHeader.Count];
                for (var c = 0; c < tableHeader.Count; c++)
                {
                    var name = tableHeader[c];
                    into[c] = -1;
                    if (c == 0 || name.Length == 0 || name == "comment") continue;
                    var index = header.IndexOf(name);
                    if (index < 0)
                    {
                        header.Add(name);
                        index = header.Count - 1;
                    }
                    into[c] = index;
                }

                var priced = new List<string>();
                foreach (var prefix in Prices)
                {
                    foreach (var name in tableHeader)
                    {
                        if (!name.StartsWith(prefix, StringComparison.Ordinal)) continue;
                        priced.Add(prefix);
                        break;
                    }
                }

                var rows = new List<List<string>> { header };
                for (var i = 1; i < sandbox.Count; i++)
                {
                    var copy = new List<string>(sandbox[i]);
                    while (copy.Count < header.Count) copy.Add(string.Empty);
                    rows.Add(copy);
                }

                var idColumn = header.IndexOf("id");
                for (var r = 1; r < table.Count; r++)
                {
                    var row = table[r];
                    var id = row[0].Trim();
                    List<string> target = null;
                    for (var i = 1; i < rows.Count && idColumn >= 0; i++)
                    {
                        if (rows[i][idColumn].Trim() == id) target = rows[i];
                    }
                    if (target == null)
                    {
                        // a row of the run's own (the New Year map's orcs, 2026-10-25): the sandbox never meets it
                        target = new List<string>();
                        while (target.Count < header.Count) target.Add(string.Empty);
                        if (idColumn >= 0) target[idColumn] = id;
                        rows.Add(target);
                    }

                    for (var c = 0; c < header.Count; c++)
                    {
                        foreach (var prefix in priced)
                        {
                            if (header[c].StartsWith(prefix, StringComparison.Ordinal) && tableHeader.IndexOf(header[c]) < 0) target[c] = string.Empty;
                        }
                    }
                    if (wholeRow)
                    {
                        for (var c = 0; c < header.Count; c++)
                        {
                            if (c != idColumn && tableHeader.IndexOf(header[c]) < 0) target[c] = string.Empty;
                        }
                    }
                    for (var c = 0; c < into.Length; c++)
                    {
                        if (into[c] >= 0) target[into[c]] = c < row.Count ? row[c] : string.Empty;
                    }
                }
                return rows;
            }
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
            downloaded += TryDownload(settings.recipesCsvUrl, RecipesCsvPath) ? 1 : 0;
            downloaded += TryDownload(settings.localizationCsvUrl, LocalizationCsvPath) ? 1 : 0;
            downloaded += TryDownload(settings.invasionCsvUrl, InvasionCsvPath) ? 1 : 0;

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

        private static List<UnitBalance> ReadUnits(List<List<string>> rows, string path)
        {
            var units = new List<UnitBalance>();
            if (rows.Count == 0) return units;

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

                units.Add(new UnitBalance
                {
                    id = Text(header, row, "id"),
                    displayName = Text(header, row, "display_name"),
                    attackRangeUnits = Number(header, row, "attack_range_units", path),
                    attackRangeStructures = Number(header, row, "attack_range_structures", path),
                    engageRadius = Number(header, row, "engage_radius", path),
                    levels = ReadUnitLevels(header, row, path),
                    startsUnlocked = Flag(header, row, "starts_unlocked", true, path),
                    recruitIronBars = CostAmount(header, row, "recruit_ironbar", path),
                    recruitCopperBars = CostAmount(header, row, "recruit_copperbar", path),
                    unlockResearch = ReadResearchStep(header, row, "research_coins", "research_sec", path),
                    levelResearch = ReadLevelResearch(header, row, UnitBalance.MaxLevel, path),
                });
            }
            return units;
        }

        /// <summary>
        /// A unit's three levels, on exactly the same inheritance rule as a building's (see
        /// ReadLevels): the plain column is level 1, `_2`/`_3` are the Laboratory's upgrades, and a
        /// blank higher-level cell means that level leaves the stat alone.
        /// </summary>
        private static List<UnitLevelStats> ReadUnitLevels(List<string> header, List<string> row, string path)
        {
            var levels = new List<UnitLevelStats>(UnitBalance.MaxLevel);

            for (var level = 1; level <= UnitBalance.MaxLevel; level++)
            {
                var previous = levels.Count > 0 ? levels[levels.Count - 1] : null;
                levels.Add(new UnitLevelStats
                {
                    maxHealth = LevelNumber(header, row, "max_health", level, previous?.maxHealth, path),
                    attackDamage = LevelNumber(header, row, "attack_damage", level, previous?.attackDamage, path),
                    attackIntervalSeconds = LevelFloat(header, row, "attack_interval_sec", level, previous?.attackIntervalSeconds, path),
                    walkSpeed = LevelFloat(header, row, "walk_speed", level, previous?.walkSpeed, path),
                    recruitCoins = LevelNumber(header, row, "recruit_coins", level, previous?.recruitCoins, path),
                    upkeepCoinsPerDay = LevelNumber(header, row, "upkeep_coins_per_day", level, previous?.upkeepCoinsPerDay, path),
                });
            }

            return levels;
        }

        /// <summary>
        /// The research that reaches levels 2 up to maxLevel, from research_coins_2/research_sec_2 onwards.
        /// A row that names neither is a thing whose levels are not gated at all -- which is exactly
        /// how the Laboratory escapes its own gate, by leaving those cells at zero.
        /// </summary>
        private static List<ResearchStep> ReadLevelResearch(List<string> header, List<string> row, int maxLevel, string path)
        {
            var steps = new List<ResearchStep>(maxLevel - 1);
            for (var level = 2; level <= maxLevel; level++)
            {
                steps.Add(ReadResearchStep(header, row, $"research_coins_{level}", $"research_sec_{level}", path));
            }
            return steps;
        }

        /// <summary>One coins/seconds pair. Absent columns are not an error: a sheet that predates research simply gates nothing.</summary>
        private static ResearchStep ReadResearchStep(List<string> header, List<string> row, string coinsColumn, string secondsColumn, string path)
        {
            return new ResearchStep
            {
                coins = CostAmount(header, row, coinsColumn, path),
                seconds = OptionalNumber(header, row, secondsColumn, path),
            };
        }

        /// <summary>
        /// A 1/0 (or true/false) cell. A MISSING column keeps the fallback rather than reading as
        /// false -- an un-migrated sheet must not lock every building in the game behind research
        /// nobody can reach.
        /// </summary>
        private static bool Flag(List<string> header, List<string> row, string column, bool fallback, string path)
        {
            if (header.IndexOf(column) < 0) return fallback;

            var raw = Text(header, row, column);
            if (raw.Length == 0) return fallback;
            if (bool.TryParse(raw, out var parsed)) return parsed;
            if (TryParseNumber(raw, out var value)) return value > 0.5f;

            Debug.LogError($"BalanceImporter: {path} column '{column}' has '{raw}', which is neither 1/0 nor true/false. Using {fallback}.");
            return fallback;
        }

        /// <summary>
        /// The buildings tab: one row per building, numbers only. Costs are spread across one column
        /// per resource (cost_wood, up2_stone, ...) rather than packed into a single cell, so the
        /// sheet can total and compare them -- "what does the defence line cost in stone" is a column
        /// sum, and the upgrade columns are formulas over the base cost.
        /// </summary>
        private static List<BuildingBalance> ReadBuildings(List<List<string>> rows, string path)
        {
            var buildings = new List<BuildingBalance>();
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
                    productionIntervalSeconds = Number(header, row, "production_interval_sec", path),
                    fogRevealRadius = (int)Number(header, row, "fog_reveal_radius", path),
                    citizensOnBuild = (int)OptionalNumber(header, row, "citizens_on_build", 0f, path),
                    storageGroup = ParseEnum(header, row, "storage_group", ResourceStorageGroup.None, path),
                    maxCount = (int)OptionalNumber(header, row, "max_count", 0f, path),
                    levels = ReadLevels(header, row, path),
                    requiredBuildingId = Text(header, row, "requires"),
                    upgradeToLevel2Cost = ReadCost(header, row, "up2_", path),
                    upgradeToLevel3Cost = ReadCost(header, row, "up3_", path),
                    upgradeToLevel4Cost = ReadCost(header, row, "up4_", path),
                    upgradeToLevel5Cost = ReadCost(header, row, "up5_", path),
                    startsUnlocked = Flag(header, row, "starts_unlocked", true, path),
                    unlockResearch = ReadResearchStep(header, row, "research_coins", "research_sec", path),
                    levelResearch = ReadLevelResearch(header, row, BuildingInstance.MaxLevel, path),
                });
            }
            return buildings;
        }

        /// <summary>
        /// The localization tab into its own asset: `key` then one column per language, in whatever
        /// order the sheet has them. Adding a third language is a column in the sheet and nothing
        /// here -- the columns after `key` and `comment` ARE the language list.
        /// </summary>
        private static int RebuildLocalization()
        {
            var rows = ReadCsv(LocalizationCsvPath);
            if (rows.Count == 0) return 0;

            var header = rows[0];
            for (var i = 0; i < header.Count; i++)
            {
                header[i] = header[i].Trim();
            }

            var languageColumns = new List<int>();
            var languages = new List<string>();
            for (var i = 0; i < header.Count; i++)
            {
                if (header[i] == "key" || header[i] == "comment" || header[i].Length == 0) continue;
                languageColumns.Add(i);
                languages.Add(header[i]);
            }

            if (languages.Count == 0)
            {
                Debug.LogError($"BalanceImporter: {LocalizationCsvPath} has no language columns beside 'key' and 'comment'.");
                return 0;
            }

            var entries = new List<LocalizationConfig.Entry>();
            var seen = new HashSet<string>();
            for (var i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count == 0) continue;

                var key = row[0].Trim();
                // Keys never contain spaces, and the prose under the table always does -- the same
                // rule IsNoteRow uses, applied here because a note row's later cells are empty and
                // would otherwise import as a key with no text at all.
                if (key.Length == 0 || key.Contains(" ")) continue;

                if (!seen.Add(key))
                {
                    Debug.LogError($"BalanceImporter: '{key}' appears twice in the localization tab. The first one wins.");
                    continue;
                }

                var entry = new LocalizationConfig.Entry { key = key, values = new List<string>() };
                foreach (var column in languageColumns)
                {
                    var value = column < row.Count ? row[column].Trim() : string.Empty;
                    // A two-line label is written "\n" in the sheet, not as a real line break in
                    // the cell: a cell containing a newline comes back from a published CSV as two
                    // rows, and pasting one back into the sheet splits it in half.
                    entry.values.Add(value.Replace("\\n", "\n"));
                }
                entries.Add(entry);
            }

            var asset = AssetDatabase.LoadAssetAtPath<LocalizationConfig>(LocalizationAssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<LocalizationConfig>();
                Directory.CreateDirectory(Path.GetDirectoryName(LocalizationAssetPath) ?? string.Empty);
                AssetDatabase.CreateAsset(asset, LocalizationAssetPath);
            }

            asset.OverwriteFrom(languages, entries);
            EditorUtility.SetDirty(asset);
            return entries.Count;
        }

        /// <summary>
        /// The recipes tab: one row per thing a building can make. A building's rows are matched to
        /// it by the `building` column, so adding a second metal to the furnace is a new row and
        /// nothing else -- no schema, no widening of the buildings tab.
        /// </summary>
        private static List<(string building, BuildingRecipe recipe)> ReadRecipes(string path)
        {
            var recipes = new List<(string, BuildingRecipe)>();
            var rows = ReadCsv(path);
            if (rows.Count == 0) return recipes;

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

                var recipe = new BuildingRecipe
                {
                    id = Text(header, row, "recipe_id"),
                    displayName = Text(header, row, "display_name"),
                    output = ParseEnum(header, row, "out", ResourceType.Wood, path),
                    outputAmount = (int)Number(header, row, "out_amount", path),
                    inputs = ReadRecipeInputs(header, row, path),
                };

                if (recipe.outputAmount <= 0)
                {
                    Debug.LogError($"BalanceImporter: recipe '{recipe.id}' of {Text(header, row, "building")} produces {recipe.outputAmount} -- a recipe that makes nothing would consume its inputs for free.");
                    continue;
                }

                recipes.Add((Text(header, row, "building"), recipe));
            }
            return recipes;
        }

        /// <summary>
        /// A recipe's inputs, read from the in1/in2/... pairs. A blank slot is simply not an
        /// ingredient -- a mine names none of them, and smelting names two.
        /// </summary>
        private static List<ResourceAmount> ReadRecipeInputs(List<string> header, List<string> row, string path)
        {
            var inputs = new List<ResourceAmount>();

            for (var slot = 1; slot <= MaxRecipeInputs; slot++)
            {
                var typeColumn = $"in{slot}";
                var amountColumn = $"in{slot}_amount";
                if (header.IndexOf(typeColumn) < 0) continue;

                var raw = Text(header, row, typeColumn);
                if (raw.Length == 0) continue;

                var amount = CostAmount(header, row, amountColumn, path);
                if (amount <= 0)
                {
                    Debug.LogError($"BalanceImporter: {path} names '{raw}' in {typeColumn} but gives it no amount. Ignoring that ingredient.");
                    continue;
                }

                inputs.Add(new ResourceAmount { type = ParseEnum(header, row, typeColumn, ResourceType.Wood, path), amount = amount });
            }

            return inputs;
        }

        /// <summary>How many in{n}/in{n}_amount column pairs the importer looks for. Widening the tab is one number here.</summary>
        private const int MaxRecipeInputs = 3;

        /// <summary>
        /// Hands each recipe to its building. A recipe naming a building that does not exist is a
        /// loud error rather than a silent drop -- it means a renamed id, and the symptom would be
        /// a workshop that mysteriously stopped producing.
        /// </summary>
        private static void AttachRecipes(List<BuildingBalance> buildings, List<(string building, BuildingRecipe recipe)> recipes)
        {
            foreach (var building in buildings)
            {
                building.recipes = new List<BuildingRecipe>();
            }

            foreach (var (buildingId, recipe) in recipes)
            {
                var owner = buildings.Find(b => b.id == buildingId);
                if (owner == null)
                {
                    Debug.LogError($"BalanceImporter: recipe '{recipe.id}' names building '{buildingId}', which has no row in the buildings tab.");
                    continue;
                }
                owner.recipes.Add(recipe);
            }
        }

        /// <summary>
        /// The upgrade levels of one building, MaxLevel of them.
        ///
        /// Level 1 comes from the plain column, levels 2..5 from the same name with a _2 .. _5
        /// suffix, and anything the sheet doesn't say repeats the level below -- an upgrade that
        /// leaves a stat alone is the common case, and making every building spell out every stat
        /// three times would be a wall of duplicated numbers nobody would keep honest.
        ///
        /// Lookup is by column name, so giving a stat its own per-level column later needs a line
        /// here and nothing else -- no schema, no migration, no fixed column order.
        /// </summary>
        private static List<BuildingLevelStats> ReadLevels(List<string> header, List<string> row, string path)
        {
            var levels = new List<BuildingLevelStats>(BuildingInstance.MaxLevel);

            for (var level = 1; level <= BuildingInstance.MaxLevel; level++)
            {
                var previous = levels.Count > 0 ? levels[levels.Count - 1] : null;
                levels.Add(new BuildingLevelStats
                {
                    maxHealth = LevelNumber(header, row, "max_health", level, previous?.maxHealth, path),
                    defense = LevelNumber(header, row, "defense", level, previous?.defense, path),
                    housingCapacity = LevelNumber(header, row, "housing_capacity", level, previous?.housingCapacity, path),
                    maxWorkers = LevelNumber(header, row, "max_workers", level, previous?.maxWorkers, path),
                    batchesPerWorkerPerTick = LevelNumber(header, row, "batches_per_tick", level, previous?.batchesPerWorkerPerTick, path),
                    storageCapacity = LevelNumber(header, row, "storage_capacity", level, previous?.storageCapacity, path),
                    happiness = LevelNumber(header, row, "happiness", level, previous?.happiness, path),
                    harvestRadius = LevelNumber(header, row, "harvest_radius", level, previous?.harvestRadius, path),
                    serviceRadius = LevelNumber(header, row, "service_radius", level, previous?.serviceRadius, path),
                    buildSeconds = LevelNumber(header, row, "build_sec", level, previous?.buildSeconds, path),
                });
            }

            return levels;
        }

        /// <summary>
        /// One stat at one level: "max_health" for level 1, "max_health_2" for level 2, and so on.
        /// Level 1 is required and says so when it's missing; a higher level silently inherits,
        /// which is the whole point -- only the stats an upgrade actually changes get typed in.
        /// </summary>
        private static int LevelNumber(List<string> header, List<string> row, string column, int level, int? inherited, string path)
        {
            var name = level == 1 ? column : $"{column}_{level}";

            if (header.IndexOf(name) < 0 || Text(header, row, name).Length == 0)
            {
                if (inherited.HasValue) return inherited.Value;
                return (int)Number(header, row, column, path); // level 1: report the miss properly
            }

            return (int)Number(header, row, name, path);
        }

        /// <summary>LevelNumber for a stat that is not a whole number -- an attack interval or a walking speed.</summary>
        private static float LevelFloat(List<string> header, List<string> row, string column, int level, float? inherited, string path)
        {
            var name = level == 1 ? column : $"{column}_{level}";

            if (header.IndexOf(name) < 0 || Text(header, row, name).Length == 0)
            {
                if (inherited.HasValue) return inherited.Value;
                return Number(header, row, column, path); // level 1: report the miss properly
            }

            return Number(header, row, name, path);
        }

        /// <summary>Number() for a column that is allowed to be absent or blank -- quiet about both, loud only about a cell holding something that is not a number.</summary>
        private static float OptionalNumber(List<string> header, List<string> row, string column, string path)
        {
            if (header.IndexOf(column) < 0) return 0f;

            var raw = Text(header, row, column);
            if (raw.Length == 0) return 0f;
            if (TryParseNumber(raw, out var value)) return value;

            Debug.LogError($"BalanceImporter: {path} has '{raw}' in column '{column}', which is not a number. Treating it as 0.");
            return 0f;
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
            // Smelted iron, so the Плавильня's output has somewhere to go: the three buildings that
            // used to be paid for in raw ore are what the furnace is for.
            ResourceType.IronBar,
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
        private static Dictionary<string, float> ReadEconomy(List<List<string>> rows, string path)
        {
            var values = new Dictionary<string, float>();
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
        /// <summary>
        /// The sheets end with a few rows of prose for whoever is editing them ("Жёлтые ячейки --
        /// правишь ты"), which must never be read as data.
        ///
        /// Two ways to tell. The obvious one: everything after the first cell is empty. The
        /// second one exists because a note is a sentence, and a sentence with a comma in it comes
        /// back from Google split across two cells -- at which point the first test passes it
        /// through as a building whose id is a paragraph of Russian and whose every number is zero.
        /// An id never contains a space; a note always does.
        /// </summary>
        private static bool IsNoteRow(List<string> row)
        {
            if (row.Count > 0 && row[0] != null && row[0].Trim().Contains(" ")) return true;

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

        /// <summary>
        /// Number for a column most rows leave blank. An empty cell means the fallback rather than
        /// an error -- citizens_on_build is filled in for exactly one building out of forty-nine,
        /// and making the other forty-eight type a zero would be noise nobody would keep honest.
        /// </summary>
        private static float OptionalNumber(List<string> header, List<string> row, string column, float fallback, string path)
        {
            if (header.IndexOf(column) < 0) return fallback;

            var raw = Text(header, row, column);
            if (raw.Length == 0) return fallback;
            if (TryParseNumber(raw, out var value)) return value;

            Debug.LogError($"BalanceImporter: {path} column '{column}' has '{raw}', which is not a number. Using {fallback}.");
            return fallback;
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
