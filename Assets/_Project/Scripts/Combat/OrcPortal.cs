using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// Marks the (currently single, for this first slice) Orc portal -- the raid source
    /// OrcRaidManager spawns OrcUnits from. Deliberately not destructible yet: per the design
    /// backlog's win condition, destroying a portal is meant to require clearing its base first,
    /// which needs a player army/combat force that doesn't exist yet either. This is defense-only
    /// for now (see GameOverManager for the Town-Hall-loss defeat condition) -- there is no win
    /// condition in this slice.
    /// </summary>
    public class OrcPortal : MonoBehaviour
    {
    }
}
