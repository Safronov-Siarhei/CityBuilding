using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// Marks a child object as an in-world indicator rather than as part of the building itself --
    /// the level disc, the decay diamond, the health bar. All three hang off the building's
    /// transform, because that is what makes them follow it, but they are HUD drawn in world
    /// space: nothing that measures what a building LOOKS like (its click box, its footprint, one
    /// level's model against another's) may measure them.
    ///
    /// Empty on purpose. It exists to be found, not to do anything.
    /// </summary>
    public class WorldIndicator : MonoBehaviour
    {
    }
}
