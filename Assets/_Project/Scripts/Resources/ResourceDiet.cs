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
        /// Every edible resource, in the order the settlement is served from them: Food alone (the
        /// user, 2026-10-25: one food counter). The food buildings differ in how much Food they
        /// make -- the longer the chain behind one, the more per worker (fish and fruit, pork smoked
        /// in the Коптильня, bread) -- not in what is eaten.
        /// </summary>
        public static readonly IReadOnlyList<ResourceType> Edible = new[]
        {
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
