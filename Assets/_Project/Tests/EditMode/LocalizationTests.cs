using System.Collections.Generic;
using System.Text.RegularExpressions;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Resources;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The localization sheet.
    ///
    /// Text breaks differently from code: nothing here can fail to compile. A key renamed in the
    /// sheet, a building added without its row, a translator dropping the {0} out of a sentence --
    /// each of those ships a game that runs perfectly and shows "#building_smelter" or throws
    /// inside a UI refresh. These check the shapes that no compiler can.
    /// </summary>
    public class LocalizationTests
    {
        private static LocalizationConfig Config()
        {
            var config = UnityEngine.Resources.Load<LocalizationConfig>(LocalizationConfig.ResourcePath);
            Assert.IsNotNull(config,
                $"No LocalizationConfig at Resources/{LocalizationConfig.ResourcePath}. Rebuild it from the CSVs " +
                "-- without it every label in the game shows its key.");
            return config;
        }

        [Test]
        public void TheSheetHasLanguages()
        {
            var config = Config();

            Assert.IsNotEmpty(config.Languages, "The localization tab has no language columns beside 'key'.");
            Assert.AreEqual("ru", config.Languages[0],
                "Russian must stay the first column: it is the fallback every untranslated cell falls back to.");
        }

        /// <summary>Every resource shows its name somewhere -- the HUD, a recipe line, a cost chip. A missing one reads as "#resource_copperore" on screen.</summary>
        [Test]
        public void EveryResourceHasAName()
        {
            var config = Config();

            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                var key = ResourceNames.KeyFor(type);
                Assert.IsNotNull(config.Find(key, 0), $"The localization tab has no row for '{key}'.");
            }
        }

        [Test]
        public void EveryBuildingHasAName()
        {
            var config = Config();
            var balance = UnityEngine.Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);
            Assert.IsNotNull(balance);

            foreach (var building in balance.Buildings)
            {
                Assert.IsNotNull(config.Find("#building_" + building.id.ToLowerInvariant(), 0),
                    $"The localization tab has no row for '#building_{building.id.ToLowerInvariant()}'.");
            }
        }

        /// <summary>The recipe name is what a Плавильня's metal buttons read; without it the player picks between three blanks.</summary>
        [Test]
        public void EveryRecipeHasAName()
        {
            var config = Config();
            var balance = UnityEngine.Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);

            foreach (var building in balance.Buildings)
            {
                foreach (var recipe in building.recipes)
                {
                    Assert.IsNotNull(config.Find("#recipe_" + recipe.id.ToLowerInvariant(), 0),
                        $"The localization tab has no row for '#recipe_{recipe.id.ToLowerInvariant()}' ({building.id}).");
                }
            }
        }

        /// <summary>
        /// The one that actually bites: a translation that loses a {0}, or grows one the code never
        /// passes. The first shows the player a sentence with a hole in it; the second throws a
        /// FormatException from inside a UI refresh, which takes the whole panel down.
        /// </summary>
        [Test]
        public void TranslationsKeepTheirPlaceholders()
        {
            var config = Config();

            foreach (var key in KeysWithPlaceholders)
            {
                var reference = config.Find(key, 0);
                Assert.IsNotNull(reference, $"The localization tab has no row for '{key}'.");

                var expected = Placeholders(reference);
                for (var language = 1; language < config.Languages.Count; language++)
                {
                    var translated = config.Find(key, language);
                    // Find falls back to the first language for an empty cell, so an untranslated
                    // row compares against itself and passes -- which is right: not translated yet
                    // is not the same failure as translated wrongly.
                    CollectionAssert.AreEquivalent(expected, Placeholders(translated),
                        $"'{key}' in '{config.Languages[language]}' does not use the same placeholders as '{config.Languages[0]}': \"{translated}\"");
                }
            }
        }

        /// <summary>
        /// The same check, swept over the WHOLE sheet rather than over the hand-written list below.
        ///
        /// It exists because the list is hand-written: a key added to the sheet and to the code but
        /// not to that list is checked by nothing, and on 2026-08-19 exactly that let a row through
        /// whose Russian cell held an unquoted comma -- the CSV split it in two, the English column
        /// silently became the back half of the Russian sentence, and the only visible symptom
        /// would have been an English player reading "уровень {1})".
        ///
        /// Sweeping needs the raw rows rather than Find, which falls back to Russian for an empty
        /// cell and would compare the reference against itself.
        /// </summary>
        [Test]
        public void NoTranslationAnywhereLosesOrGainsAPlaceholder()
        {
            var config = Config();

            foreach (var entry in config.Entries)
            {
                if (entry == null || entry.values.Count == 0) continue;

                var expected = Placeholders(entry.values[0]);
                for (var language = 1; language < entry.values.Count; language++)
                {
                    if (string.IsNullOrEmpty(entry.values[language])) continue;

                    CollectionAssert.AreEquivalent(expected, Placeholders(entry.values[language]),
                        $"'{entry.key}' in '{config.Languages[language]}' does not use the same placeholders as '{config.Languages[0]}': " +
                        $"[{entry.values[0]}] vs [{entry.values[language]}]");
                }
            }
        }

        /// <summary>Every key the code passes to Localization.Format. Listed by hand, because a placeholder mismatch is invisible until the string is actually formatted.</summary>
        private static readonly string[] KeysWithPlaceholders =
        {
            "#hud_status", "#hud_place_hint", "#load_selected",
            "#building_level", "#building_condition", "#building_workers", "#building_idle_citizens",
            "#building_produces", "#recipe_conversion", "#recipe_batches",
            "#workforce_summary", "#workforce_gives",
            "#happiness_title", "#happiness_breakdown", "#tax_rate",
            "#army_summary", "#army_full",
            "#log_day", "#log_built", "#log_destroyed_decay", "#log_destroyed_combat",
            "#log_no_input", "#log_no_storage", "#log_hungry", "#log_starved",
            "#log_recruited", "#log_disbanded", "#log_raid",
            "#log_happiness_low", "#log_happiness_recovered", "#log_tier_up",
            "#research_running", "#research_paused", "#research_cancel", "#research_cost",
            "#research_unit_gain", "#research_scientists", "#research_lab_state",
            "#research_unlock", "#research_level",
            "#research_blocked_lab_level", "#research_blocked_prereq", "#building_upgrade_locked",
            "#log_research_started", "#log_research_done", "#log_research_cancelled", "#log_research_lost",
            "#log_raid_levelled",
        };

        private static List<string> Placeholders(string text)
        {
            var found = new List<string>();
            if (string.IsNullOrEmpty(text)) return found;

            foreach (Match match in Regex.Matches(text, @"\{(\d+)\}"))
            {
                if (!found.Contains(match.Value)) found.Add(match.Value);
            }
            return found;
        }

        /// <summary>
        /// Google Sheets parses a cell that starts with =, +, - or @ as a formula, and the string
        /// comes back as "#NAME?" -- which is what the two worker buttons did on 2026-08-17, having
        /// been written "+ Назначить" and "- Снять". Nothing downstream can tell that from a real
        /// translation, so it has to be caught here.
        /// </summary>
        [Test]
        public void NoTextStartsWithSomethingTheSheetWouldReadAsAFormula()
        {
            var config = Config();

            foreach (var key in EveryKey(config))
            {
                for (var language = 0; language < config.Languages.Count; language++)
                {
                    var text = config.Find(key, language);
                    if (string.IsNullOrEmpty(text)) continue;

                    Assert.IsFalse("=+-@".IndexOf(text[0]) >= 0,
                        $"'{key}' in '{config.Languages[language]}' starts with '{text[0]}', which Google Sheets turns into a formula: \"{text}\"");
                }
            }
        }

        /// <summary>Every key in the sheet, gathered the only way the config exposes -- by asking about the ones the game builds plus the ones written by hand.</summary>
        private static IEnumerable<string> EveryKey(LocalizationConfig config)
        {
            foreach (var key in KeysWithPlaceholders) yield return key;

            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                yield return ResourceNames.KeyFor(type);
            }

            var balance = UnityEngine.Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);
            foreach (var building in balance.Buildings)
            {
                yield return "#building_" + building.id.ToLowerInvariant();
                foreach (var recipe in building.recipes) yield return "#recipe_" + recipe.id.ToLowerInvariant();
            }

            // The fixed captions SetupProject hangs on buttons and titles.
            foreach (var key in new[]
            {
                "#menu_title", "#menu_subtitle", "#menu_new_game", "#menu_load_game", "#menu_settings", "#menu_quit",
                "#settings_title", "#settings_language", "#settings_back",
                "#ui_close", "#ui_cancel", "#ui_back", "#ui_free",
                "#hud_menu", "#hud_save", "#hud_workforce",
                "#exit_title", "#exit_warning", "#exit_confirm",
                "#save_title", "#save_confirm", "#load_title", "#load_empty", "#load_confirm",
                "#building_no_decay", "#building_assign", "#building_unassign", "#building_upgrade",
                "#building_repair", "#building_recruit",
                "#workforce_title", "#workforce_empty", "#workforce_idle_building",
                "#army_target_units", "#army_target_buildings", "#army_no_citizens", "#army_no_coins",
                "#unit_militia", "#over_victory", "#over_defeat", "#over_victory_reason",
                "#over_defeat_reason", "#over_to_menu",
                "#log_fed", "#log_soldier_died", "#log_portal_opened", "#log_portal_destroyed",
                "#log_defeat_townhall", "#log_defeat_empty", "#log_victory",
                "#tier_hamlet", "#tier_village", "#tier_town", "#tier_city", "#tier_kingdom",
                "#research_tab_buildings", "#research_tab_units", "#research_empty", "#research_start",
                "#research_done", "#research_idle", "#research_blocked_busy", "#research_blocked_no_lab",
                "#research_blocked_workers", "#research_blocked_coins", "#research_blocked_unknown",
                "#workforce_research",
                // The Laboratory's building tab heads its sections with these.
                "#category_city", "#category_storage", "#category_entertainment", "#category_defence",
                "#category_production", "#category_food", "#category_water",
            })
            {
                yield return key;
            }
        }

        /// <summary>A key the sheet does not have must be obvious, not blank -- see Localization.Get.</summary>
        [Test]
        public void AMissingKeyShowsItself()
        {
            Assert.AreEqual("#no_such_key_exists", Localization.Get("#no_such_key_exists"));
        }

        /// <summary>Names that the balance sheet also carries fall back to it, so a building added to one tab and not the other reads in Russian rather than as its key.</summary>
        [Test]
        public void AMissingNameFallsBackToTheBalanceSheet()
        {
            Assert.AreEqual("Плавильня", Localization.GetOrDefault("#building_nosuchbuilding", "Плавильня"));
        }

        /// <summary>A recipe with no localization row still names itself from the recipes tab -- BuildingRecipe.LocalizedName is what the metal buttons read.</summary>
        [Test]
        public void ARecipeFallsBackToItsSheetLabel()
        {
            var recipe = new BuildingRecipe { id = "no_such_recipe", displayName = "Медный слиток" };

            Assert.AreEqual("Медный слиток", recipe.LocalizedName);
        }
    }
}
