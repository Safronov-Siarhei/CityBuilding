using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// Marks an Orc portal -- the raid source OrcRaidManager spawns OrcUnits from. Deliberately
    /// not destructible yet: per the design backlog's win condition, destroying a portal is meant
    /// to require clearing its base first, which needs a player army that doesn't exist yet. This
    /// is defense-only for now (see GameOverManager for the Town-Hall-loss defeat condition) --
    /// there is no win condition in this slice.
    ///
    /// Self-registering (same pattern as ResourceNode.All) so anything needing to address a
    /// specific portal -- currently the OrcSpawn cheat -- can enumerate them without scanning the
    /// scene. Only one portal exists today; the registry is what makes the eventual several-per-map
    /// design addressable without changing callers.
    /// </summary>
    public class OrcPortal : MonoBehaviour
    {
        private static readonly List<OrcPortal> _all = new List<OrcPortal>();

        /// <summary>Every portal currently in the scene, in spawn order.</summary>
        public static IReadOnlyList<OrcPortal> All => _all;

        private void OnEnable()
        {
            _all.Add(this);
        }

        private void OnDisable()
        {
            _all.Remove(this);
        }
    }
}
