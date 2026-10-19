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

        // The two looks a standing portal has (Tools/blender/orcportal1.py): the whirlpool
        // burning bright on the portal the NEXT wave will come out of, banked down on the rest.
        // Both are children, switched rather than swapped, so a look change costs nothing.
        private GameObject _activeLook;
        private GameObject _passiveLook;

        /// <summary>Whether this portal is showing the bright look -- the source of the next wave (see OrcRaidManager).</summary>
        public bool IsShownActive { get; private set; } = true;

        /// <summary>
        /// Set by its camp while the camp's totem stands (see OrcCamp): a shielded portal takes no
        /// damage, so the camp has to be broken first. Soldiers left to choose their own targets
        /// pass a shielded portal by; one ordered at it is told why nothing happens.
        /// </summary>
        public bool IsShielded { get; set; }

        /// <summary>The great portal of the main base: 4x4, sends no raids, and closing it wins the map (the user's rule, 2026-10-19).</summary>
        public bool IsMain { get; set; }

        // The "shielded" line at most this often, so a group hammering the portal does not flood
        // the event feed with one icon per swing.
        private const float ShieldMessageIntervalSeconds = 8f;
        private float _lastShieldMessageTime = float.NegativeInfinity;

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

        /// <summary>Hands the portal its two looks. Without them (no models in the build) it keeps whatever it was built with and the switch does nothing.</summary>
        public void SetLooks(GameObject activeLook, GameObject passiveLook)
        {
            // Off the portal at once, not at the end of the frame: until then a Find by name would
            // still turn up the old look.
            if (_activeLook != null && _activeLook != activeLook) { _activeLook.transform.SetParent(null, false); Destroy(_activeLook); }
            if (_passiveLook != null && _passiveLook != passiveLook) { _passiveLook.transform.SetParent(null, false); Destroy(_passiveLook); }
            _activeLook = activeLook;
            _passiveLook = passiveLook;
            ShowActive(IsShownActive);
        }

        /// <summary>Shows the bright look (this portal sends the next wave) or the banked-down one.</summary>
        public void ShowActive(bool active)
        {
            IsShownActive = active;
            if (_activeLook != null) _activeLook.SetActive(active);
            if (_passiveLook != null) _passiveLook.SetActive(!active);
        }

        /// <summary>The level of its camp (see OrcCamp): it decides the portal's health and look.</summary>
        public int Level { get; private set; } = 1;

        /// <summary>Takes its camp's level -- whole, on the new level's health: a camp that grows is rebuilt.</summary>
        public void SetLevel(int level, int maxHealth)
        {
            Level = Mathf.Max(1, level);
            MaxHealth = Mathf.Max(1, maxHealth);
            CurrentHealth = MaxHealth;
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
            if (IsShielded)
            {
                if (Time.time - _lastShieldMessageTime >= ShieldMessageIntervalSeconds)
                {
                    _lastShieldMessageTime = Time.time;
                    EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_portal_shielded"),
                        EventSeverity.Warning, EventGlyph.Alert, null, transform.position));
                }
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            if (CurrentHealth > 0) return;

            // Out of the registry before the event fires: handlers (the win check) ask how many
            // portals are left, and a destroyed one must not still be counted among them.
            _all.Remove(this);
            // With several portals per map, "the portal is destroyed" no longer tells the player
            // whether the map is over -- the count of what is still standing is the whole point of
            // taking one down. The bare message is kept for the last one, where the victory line
            // follows it immediately and a "0 left" would only be in its way.
            EventLogManager.Instance?.Post(GameEvent.At(_all.Count > 0
                    ? Localization.Format("#log_portal_destroyed_remaining", _all.Count)
                    : Localization.Get("#log_portal_destroyed"),
                EventSeverity.Good, EventGlyph.Portal, null, transform.position));
            OnPortalDestroyed?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
