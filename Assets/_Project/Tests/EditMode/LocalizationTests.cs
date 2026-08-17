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
    /// each of those ships a game that runs perfectly and shows "building.Smelter" or throws
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

        /// <summary>Every resource shows its name somewhere -- the HUD, a recipe line, a cost chip. A missing one reads as "resource.CopperOre" on screen.</summary>
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
                Assert.IsNotNull(config.Find("building." + building.id, 0),
                    $"The localization tab has no row for 'building.{building.id}'.");
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
                    Assert.IsNotNull(config.Find("recipe." + recipe.id, 0),
                        $"The localization tab has no row for 'recipe.{recipe.id}' ({building.id}).");
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

        /// <summary>Every key the code passes to Localization.Format. Listed by hand, because a placeholder mismatch is invisible until the string is actually formatted.</summary>
        private static readonly string[] KeysWithPlaceholders =
        {
            "hud.status", "hud.place_hint", "load.selected",
            "building.level", "building.condition", "building.workers", "building.idle_citizens",
            "building.produces", "recipe.conversion", "recipe.batches",
            "workforce.summary", "workforce.gives",
            "happiness.title", "happiness.breakdown", "tax.rate",
            "army.summary", "army.full",
            "log.day", "log.built", "log.destroyed_decay", "log.destroyed_combat",
            "log.no_input", "log.no_storage", "log.hungry", "log.starved",
            "log.recruited", "log.disbanded", "log.raid",
            "log.happiness_low", "log.happiness_recovered", "log.tier_up",
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

        /// <summary>A key the sheet does not have must be obvious, not blank -- see Localization.Get.</summary>
        [Test]
        public void AMissingKeyShowsItself()
        {
            Assert.AreEqual("no.such.key.exists", Localization.Get("no.such.key.exists"));
        }

        /// <summary>Names that the balance sheet also carries fall back to it, so a building added to one tab and not the other reads in Russian rather than as its key.</summary>
        [Test]
        public void AMissingNameFallsBackToTheBalanceSheet()
        {
            Assert.AreEqual("Плавильня", Localization.GetOrDefault("building.NoSuchBuilding", "Плавильня"));
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
