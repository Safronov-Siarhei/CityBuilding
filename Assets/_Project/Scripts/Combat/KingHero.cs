using System;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// The King of the Invasion mode as a hero (the user, 2026-10-21): he gains experience from
    /// every enemy that falls near him, levels up -- more health and damage each level, skills later -- and when he
    /// falls he is not lost but gets up again at the castle after a wait that grows with his level,
    /// as in Dota.
    ///
    /// Sits beside the SoldierUnit that does his fighting: everything about walking, targeting and
    /// orders is the army's, so the orcs fight him and the player commands him exactly as they do
    /// any soldier. Experience for a kill is the enemy's health -- a brute is worth more than an
    /// orc without a table to keep -- and the curve is the economy tab's king_* keys.
    /// </summary>
    public class KingHero : MonoBehaviour
    {
        private SoldierUnit _unit;
        private CharacterController _controller;
        private Renderer[] _renderers;

        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }

        /// <summary>Experience still needed for the next level; 0 at the top level.</summary>
        public int ExperienceToNext => Level >= BalanceConfig.Instance.KingMaxLevel ? 0 : XpForLevel(Level) - Experience;

        /// <summary>Down and waiting to come back.</summary>
        public bool IsFallen { get; private set; }

        /// <summary>Seconds until he is back on his feet; 0 while he stands.</summary>
        public float RespawnRemaining { get; private set; }

        /// <summary>Where he comes back: the castle.</summary>
        public Vector3 Home { get; set; }

        public SoldierUnit Unit => _unit;

        /// <summary>Raised on a level gained, a fall and a return -- for the hero's portrait on the HUD.</summary>
        public event Action Changed;

        /// <summary>Experience from one level to the next: king_xp_per_level times the level he is at.</summary>
        public static int XpForLevel(int level) => BalanceConfig.Instance.KingXpPerLevel * Mathf.Max(1, level);

        /// <summary>How long a fall keeps him down at a level.</summary>
        public static float RespawnSecondsAt(int level) =>
            BalanceConfig.Instance.KingRespawnBaseSeconds + BalanceConfig.Instance.KingRespawnPerLevelSeconds * (Mathf.Max(1, level) - 1);

        /// <summary>His health and damage at a level: level 1 is the economy tab's king_max_health and king_attack_damage.</summary>
        public static int MaxHealthAt(int level) =>
            BalanceConfig.Instance.KingUnit.LevelStats(1).maxHealth + BalanceConfig.Instance.KingHealthPerLevel * (Mathf.Max(1, level) - 1);

        public static int DamageAt(int level) =>
            BalanceConfig.Instance.KingUnit.LevelStats(1).attackDamage + BalanceConfig.Instance.KingDamagePerLevel * (Mathf.Max(1, level) - 1);

        /// <summary>Called by ArmyManager.SpawnKing right after the unit is built.</summary>
        public void Initialize(Vector3 home)
        {
            Home = home;
            _unit = GetComponent<SoldierUnit>();
            _controller = GetComponent<CharacterController>();
            _renderers = GetComponentsInChildren<Renderer>(true);
            OrcUnit.Died += OnOrcDied;
            ApplyLevel();
        }

        private void OnDestroy()
        {
            OrcUnit.Died -= OnOrcDied;
        }

        /// <summary>Experience for one enemy he struck down: its health.</summary>
        public static int ExperienceFor(IDamageTarget target) => target is OrcUnit orc ? Mathf.Max(1, orc.MaxHealth) : 0;

        /// <summary>
        /// As in Dota, the hero learns from every enemy that falls near him, not only from his own
        /// blows: a tower's or a soldier's kill beside him counts (the small base island's towers
        /// would otherwise take most of his kills).
        /// </summary>
        private void OnOrcDied(OrcUnit orc)
        {
            if (IsFallen || orc == null) return;
            var offset = orc.transform.position - transform.position;
            offset.y = 0f;
            var radius = BalanceConfig.Instance.KingXpRadius;
            if (offset.sqrMagnitude <= radius * radius) AddExperience(ExperienceFor(orc));
        }

        /// <summary>Adds experience and takes every level it pays for. Public for the tests and for experience that does not come from a blow of his own.</summary>
        public void AddExperience(int amount)
        {
            if (amount <= 0) return;
            var maxLevel = BalanceConfig.Instance.KingMaxLevel;
            if (Level >= maxLevel) return;

            Experience += amount;
            var levelled = false;
            while (Level < maxLevel && Experience >= XpForLevel(Level))
            {
                Experience -= XpForLevel(Level);
                Level++;
                levelled = true;
            }
            if (Level >= maxLevel) Experience = 0;
            if (!levelled) return;

            ApplyLevel();
            // A level is a fresh breath, as in Dota: what he gains is added to what he has.
            Changed?.Invoke();
        }

        private void ApplyLevel()
        {
            var before = _unit.MaxHealth;
            _unit.SetHeroStats(MaxHealthAt(Level), DamageAt(Level));
            if (_unit.MaxHealth > before) _unit.SetCurrentHealth(_unit.CurrentHealth + (_unit.MaxHealth - before));
        }

        /// <summary>
        /// Struck down: out of the fight (no orc sees him, the army panel's orders do nothing) until
        /// the wait for his level runs out. The body is hidden, not destroyed, so his level and
        /// experience stay where they are.
        /// </summary>
        public void Fall()
        {
            if (IsFallen) return;
            IsFallen = true;
            RespawnRemaining = RespawnSecondsAt(Level);
            _unit.enabled = false;
            if (_controller != null) _controller.enabled = false;
            SetVisible(false);
            Changed?.Invoke();
        }

        private void Update()
        {
            if (!IsFallen) return;
            RespawnRemaining -= Time.deltaTime;
            if (RespawnRemaining <= 0f) Rise();
        }

        /// <summary>Back at the castle, at full health, holding there.</summary>
        public void Rise()
        {
            if (!IsFallen) return;
            IsFallen = false;
            RespawnRemaining = 0f;
            transform.position = Home;
            _unit.Group?.OrderMoveTo(Home);
            _unit.Revive();
            if (_controller != null) _controller.enabled = true;
            _unit.enabled = true;
            SetVisible(true);
            Changed?.Invoke();
        }

        private void SetVisible(bool visible)
        {
            if (_renderers == null) return;
            foreach (var r in _renderers)
            {
                if (r != null) r.enabled = visible;
            }
        }
    }
}
