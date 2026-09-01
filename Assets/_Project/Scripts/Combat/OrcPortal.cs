using System;
using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// An Orc portal: a raid source OrcRaidManager spawns OrcUnits from, and -- since the player
    /// now has an army that can reach it -- one of the map's objectives. There are several per map
    /// (see PortalPlacement); destroying the LAST one wins the map (see GameOverManager).
    ///
    /// The health pool is deliberately large: per the design, a portal is not a quick strike but
    /// something a group has to stand and work on, long enough for the defenders (today: the raids
    /// it keeps spawning; later: the base built around it) to be a real problem. Nothing else in
    /// the game can damage it -- only SoldierUnit, through IDamageTarget.
    ///
    /// Self-registering (same pattern as ResourceNode.All) so soldiers can enumerate portals as
    /// targets and the win check can count the survivors without scanning the scene.
    /// </summary>
    public class OrcPortal : MonoBehaviour, IDamageTarget
    {
        private static readonly List<OrcPortal> _all = new List<OrcPortal>();

        /// <summary>Every portal currently in the scene, in spawn order.</summary>
        public static IReadOnlyList<OrcPortal> All => _all;

        /// <summary>Raised after a portal is destroyed and has already left the registry, so a handler can read All.Count as the number of portals still standing.</summary>
        public static event Action<OrcPortal> OnPortalDestroyed;

        /// <summary>From the balance sheet (portal_max_health): deliberately a long grind, so closing a portal is a siege rather than a quick strike.</summary>
        public int MaxHealth { get; private set; }

        public int CurrentHealth { get; private set; }

        /// <summary>
        /// The grid cell this portal stands on and has reserved. Carried by the portal rather than
        /// by the spawner, because with several per map the save has to write out each one's place
        /// next to its own health -- and a portal already destroyed must not leave a cell behind in
        /// a list the survivors are indexed by.
        /// </summary>
        public Vector2Int Cell { get; private set; }

        private void Awake()
        {
            MaxHealth = BalanceConfig.Instance.PortalMaxHealth;
            CurrentHealth = MaxHealth;
        }

        /// <summary>Tells the portal where it was put. Called by OrcRaidManager immediately after spawning it, on both the fresh and the restored path.</summary>
        public void SetCell(Vector2Int cell)
        {
            Cell = cell;
        }

        /// <summary>Puts a loaded portal back on the health the player had ground it down to. Clamped to at least 1: a portal restored at zero would stand there whole and unkillable, since only TakeDamage retires one.</summary>
        public void SetCurrentHealth(int health)
        {
            CurrentHealth = Mathf.Clamp(health, 1, MaxHealth);
        }

        Transform IDamageTarget.Transform => this != null ? transform : null;
        bool IDamageTarget.IsAlive => this != null && CurrentHealth > 0;

        /// <summary>A structure, so a group set to TargetPriority.Structures makes straight for it past the orcs.</summary>
        bool IDamageTarget.IsStructure => true;

        private void OnEnable()
        {
            _all.Add(this);
        }

        private void OnDisable()
        {
            _all.Remove(this);
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            if (CurrentHealth > 0) return;

            // Out of the registry before the event fires: handlers (the win check) ask how many
            // portals are left, and a destroyed one must not still be counted among them.
            _all.Remove(this);
            // With several portals per map, "the portal is destroyed" no longer tells the player
            // whether the map is over -- the count of what is still standing is the whole point of
            // taking one down. The bare message is kept for the last one, where the victory line
            // follows it immediately and a "0 left" would only be in its way.
            EventLogManager.Instance?.Log(_all.Count > 0
                ? Localization.Format("#log_portal_destroyed_remaining", _all.Count)
                : Localization.Get("#log_portal_destroyed"));
            OnPortalDestroyed?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
