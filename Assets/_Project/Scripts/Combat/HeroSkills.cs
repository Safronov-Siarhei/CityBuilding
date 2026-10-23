using System;
using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// A hero's four skills (the user, 2026-10-23): each opens at a hero level, works when the
    /// PLAYER presses it -- never on its own -- and then needs its cooldown, which keeps running
    /// while the hero is down. Their numbers grow with the hero's level (HeroSkillBalance); the
    /// skills are the economy tab's skill_&lt;hero&gt;_* rows, in the order they open.
    ///
    /// The King's: Лечение (a share of his health back), Неуязвимость (no damage for a while),
    /// Ярость (strikes faster and harder), Благословение (nothing of the player's takes damage).
    /// </summary>
    public class HeroSkills : MonoBehaviour
    {
        public const string KingHeal = "king_heal";
        public const string KingShield = "king_invuln";
        public const string KingRage = "king_rage";
        public const string KingBlessing = "king_bless";

        public class Slot
        {
            public HeroSkillBalance Balance;
            public float Cooldown;
        }

        private readonly List<Slot> _slots = new List<Slot>();
        private KingHero _hero;
        private LineRenderer _aura;

        public IReadOnlyList<Slot> Slots => _slots;

        /// <summary>A skill was used (its index) -- for the HUD and the effects.</summary>
        public event Action<int> Used;

        /// <summary>Takes this hero's skills from the sheet: every skill whose id starts with `heroId`_.</summary>
        public void Bind(KingHero hero, string heroId)
        {
            _hero = hero;
            _slots.Clear();
            var prefix = heroId + "_";
            foreach (var skill in BalanceConfig.Instance.HeroSkills)
            {
                if (skill.id.StartsWith(prefix, StringComparison.Ordinal)) _slots.Add(new Slot { Balance = skill });
            }
            _slots.Sort((a, b) => a.Balance.unlockLevel.CompareTo(b.Balance.unlockLevel));
        }

        public int Level => _hero != null ? _hero.Level : 1;

        public bool IsOpen(int index) => index >= 0 && index < _slots.Count && Level >= _slots[index].Balance.unlockLevel;

        public bool IsReady(int index) => IsOpen(index) && _slots[index].Cooldown <= 0f && _hero != null && !_hero.IsFallen;

        /// <summary>Uses the skill if it is open, off cooldown and the hero stands. False otherwise.</summary>
        public bool Use(int index)
        {
            if (!IsReady(index)) return false;
            var slot = _slots[index];
            Apply(slot.Balance);
            slot.Cooldown = slot.Balance.cooldownSeconds;
            Used?.Invoke(index);
            return true;
        }

        private void Apply(HeroSkillBalance skill)
        {
            var unit = _hero.Unit;
            var power = skill.PowerAt(Level);
            var seconds = skill.DurationAt(Level);
            switch (skill.id)
            {
                case KingHeal:
                    unit.SetCurrentHealth(unit.CurrentHealth + Mathf.RoundToInt(unit.MaxHealth * power));
                    UI.RespawnFlash.Play(transform.position);
                    break;
                case KingShield:
                    unit.Shield(seconds);
                    break;
                case KingRage:
                    unit.Enrage(power, seconds);
                    break;
                case KingBlessing:
                    HeroBlessing.Begin(seconds);
                    var castle = Invasion.InvasionMode.Instance != null ? Invasion.InvasionMode.Instance.Castle : null;
                    UI.RespawnFlash.Play(castle != null ? castle.transform.position : transform.position);
                    break;
                default:
                    Debug.LogWarning($"HeroSkills: no effect is written for the skill '{skill.id}'.");
                    break;
            }
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            for (var i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Cooldown > 0f) _slots[i].Cooldown = Mathf.Max(0f, _slots[i].Cooldown - dt);
            }
            ShowAura();
        }

        private static readonly Color ShieldColour = new Color(0.45f, 0.75f, 1f, 0.95f);
        private static readonly Color RageColour = new Color(1f, 0.35f, 0.15f, 0.95f);

        /// <summary>A ring at his feet while a skill holds: blue for the shield, red for the rage.</summary>
        private void ShowAura()
        {
            var unit = _hero != null ? _hero.Unit : null;
            var shielded = unit != null && unit.ShieldLeft > 0f;
            var raging = unit != null && unit.RageLeft > 0f;
            var show = (shielded || raging) && !_hero.IsFallen;
            if (_aura == null)
            {
                if (!show) return;
                _aura = BuildAura();
            }
            if (_aura.gameObject.activeSelf != show) _aura.gameObject.SetActive(show);
            if (!show) return;
            var colour = shielded ? ShieldColour : RageColour;
            colour.a *= 0.65f + 0.35f * Mathf.Sin(Time.time * 8f);
            _aura.material.color = colour;
        }

        private LineRenderer BuildAura()
        {
            var go = new GameObject("HeroSkillAura");
            go.transform.SetParent(transform, false);
            var ring = go.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 28;
            ring.widthMultiplier = 0.09f;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            ring.material = UI.MoveOrderMarker.CreateTransparentMaterial(Color.white);
            var scale = Mathf.Max(0.0001f, transform.lossyScale.x);
            for (var i = 0; i < 28; i++)
            {
                var a = i / 28f * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a), 0.06f, Mathf.Sin(a)) * (0.7f / scale));
            }
            return ring;
        }
    }
}
