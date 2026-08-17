using System;
using System.Collections.Generic;
using System.Text;
using CityBuilder.Resources;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// One thing a building knows how to make: what goes in, what comes out.
    ///
    /// A building holds a LIST of these, and the player picks which one it is working on (see
    /// ProductionBuilding.SelectedRecipe). Most buildings have exactly one and never show a choice
    /// -- a sawmill only knows how to fell trees. The Плавильня is what this exists for: one
    /// furnace, three metals, and the player deciding which one the settlement needs right now.
    ///
    /// The amounts here are one BATCH. How many batches a worker gets through in a tick is the
    /// building's own BuildingLevelStats.batchesPerWorkerPerTick, which is what an upgrade
    /// improves -- so a level 3 furnace melts more of whatever it is set to, and the recipe stays
    /// a statement about the metal rather than about the building.
    /// </summary>
    [Serializable]
    public class BuildingRecipe
    {
        /// <summary>Stable id, saved with the building so a reloaded furnace is still set to the metal the player chose.</summary>
        public string id = string.Empty;

        /// <summary>The recipes tab's own Russian label. What the player actually reads is <see cref="LocalizedName"/>.</summary>
        public string displayName = string.Empty;

        /// <summary>What goes on the selector button -- the localization sheet's `recipe.<id>`, falling back to the sheet's own label.</summary>
        public string LocalizedName => Core.Localization.GetOrDefault("recipe." + id, displayName);

        /// <summary>
        /// Everything one batch consumes. Empty for a gatherer: a mine makes ore out of the ground,
        /// not out of another resource. Several entries is the point -- smelting takes ore AND the
        /// coal to fire it, which one input per building could never express.
        /// </summary>
        public List<ResourceAmount> inputs = new List<ResourceAmount>();

        public ResourceType output = ResourceType.Wood;
        public int outputAmount = 1;

        public bool HasInputs => inputs != null && inputs.Count > 0;

        /// <summary>
        /// The recipe in words: "2 железная руда + 1 уголь -> 1 железный слиток", or just
        /// "1 дерево" for something gathered. Used by the building card and the workforce list, and
        /// pure so an EditMode test can pin the wording down.
        /// </summary>
        public string Describe()
        {
            var made = $"{outputAmount} {ResourceNames.Of(output)}";
            if (!HasInputs) return made;

            var eaten = new StringBuilder();
            foreach (var input in inputs)
            {
                if (eaten.Length > 0) eaten.Append(" + ");
                eaten.Append(input.amount).Append(' ').Append(ResourceNames.Of(input.type));
            }

            return Core.Localization.Format("recipe.conversion", eaten.ToString(), made);
        }
    }
}
