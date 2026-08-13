using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// Anything the player's army can attack: an OrcUnit, an OrcPortal, and later the buildings of
    /// an orc base. Exists so SoldierUnit can hold one target reference and one attack path instead
    /// of branching per enemy kind every frame -- and so the group's target priority (see
    /// TargetPriority) can sort candidates of different kinds against each other.
    ///
    /// Player buildings deliberately do NOT implement this: they're damaged by orcs through
    /// BuildingInstance.TryDamage, and keeping the two sides on separate paths means no amount of
    /// targeting-code confusion can ever have the player's own army attack the player's own town.
    /// </summary>
    public interface IDamageTarget
    {
        /// <summary>Null once the underlying object is destroyed -- Unity's fake-null makes this the safe way to check liveness through an interface reference.</summary>
        Transform Transform { get; }

        bool IsAlive { get; }

        /// <summary>Which priority bucket this target falls into for a group's TargetPriority setting.</summary>
        bool IsStructure { get; }

        void TakeDamage(int amount);
    }
}
