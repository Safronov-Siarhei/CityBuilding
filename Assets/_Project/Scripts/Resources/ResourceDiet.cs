using System.Collections.Generic;

namespace CityBuilder.Resources
{
    /// <summary>
    /// What the settlement can actually eat.
    ///
    /// Deliberately not the same question as "which storehouse does this go in": flour shares the
    /// Кладовая with the bread it becomes but nobody eats it, and if the storage group decided the
    /// diet the mill's whole output would be eaten before it ever reached the bakery.
    /// </summary>
    public static class ResourceDiet
    {
        /// <summary>
        /// Every edible resource, in the order the settlement is served from them. Grows as the
        /// design's food types arrive (фрукты, мясо, рыба, овощи) -- each new one widens the table
        /// automatically, including the variety score in HappinessManager.
        /// </summary>
        public static readonly IReadOnlyList<ResourceType> Edible = new[]
        {
            ResourceType.Bread,
            ResourceType.Food,
        };

        public static bool IsEdible(ResourceType type)
        {
            for (var i = 0; i < Edible.Count; i++)
            {
                if (Edible[i] == type) return true;
            }
            return false;
        }
    }
}
