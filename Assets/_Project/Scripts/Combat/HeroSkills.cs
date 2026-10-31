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
    /// The Queen's: Выстрел (the strongest enemy in reach dies; a boss loses a share), Залп (a wave
    /// of bolts over every enemy in a cone), Призыв (soldiers of a fixed level for a while), Жертва
    /// (unhurtable and fierce, then she falls). The Captain's: Сила (allies round him strike
    /// harder), Спасение (the castle unhurtable), Боевой дух (the jobless people fight), Бессмертие
    /// (the one skill that works by itself: ready, it has him up again where he fell).
    /// </summary>
    public class HeroSkills : MonoBehaviour
    {
        public const string KingHeal = "king_heal";
        public const string KingShield = "king_invuln";
        public const string KingRage = "king_rage";
        public const string KingBlessing = "king_bless";
        public const string QueenShot = "queen_shot";
        public const string QueenVolley = "queen_volley";
        public const string QueenSummon = "queen_summon";
        public const string QueenSacrifice = "queen_sacrifice";
        public const string CaptainMight = "captain_might";
        public const string CaptainRescue = "captain_rescue";
        public const string CaptainSpirit = "captain_spirit";
        public const string CaptainImmortal = "captain_immortal";

        /// <summary>The Queen's Призыв calls this kind of soldier (the sheet says how many, what level and how long).</summary>
        public const SoldierType SummonedType = SoldierType.ManAtArms;

        /// <summary>The volley's cone: this many degrees either side of the line to the nearest enemy.</summary>
        private const float VolleyHalfAngle = 35f;

        public class Slot
        {
            public HeroSkillBalance Balance;
            public float Cooldown;
        }

        private readonly List<Slot> _slots = new List<Slot>();
        private Hero _hero;
        private float _sacrificeLeft = -1f;
        private LineRenderer _aura;

        public IReadOnlyList<Slot> Slots => _slots;

        /// <summary>A skill was used (its index) -- for the HUD and the effects.</summary>
        public event Action<int> Used;

        /// <summary>Takes this hero's skills from the sheet: every skill whose id starts with `heroId`_.</summary>
        /// <summary>The cheat window's switch: a skill is ready again as soon as it is used.</summary>
        public static bool NoCooldownCheat;

        public void Bind(Hero hero, string heroId)
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

        /// <summary>Works by itself and has no button to press (the Captain's Бессмертие).</summary>
        public static bool IsPassive(HeroSkillBalance skill) => skill.id == CaptainImmortal;

        /// <summary>Uses the skill if it is open, off cooldown, not a passive one, and the hero stands. False otherwise -- also when there is nothing for it to act on (no enemy in reach of a shot).</summary>
        public bool Use(int index)
        {
            if (!IsReady(index) || IsPassive(_slots[index].Balance)) return false;
            var slot = _slots[index];
            if (!Apply(slot.Balance)) return false;
            slot.Cooldown = NoCooldownCheat ? 0f : slot.Balance.cooldownSeconds;
            Used?.Invoke(index);
            return true;
        }

        /// <summary>
        /// The Captain's Бессмертие, asked by Hero.Fall: when it is open and recharged he stays on
        /// his feet where he fell -- on its share of his health, unhurtable for its few seconds --
        /// and it starts recharging. False when there is no such skill ready: he falls as usual.
        /// </summary>
        public bool TryCheatDeath()
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                var skill = _slots[i].Balance;
                if (skill.id != CaptainImmortal || !IsOpen(i) || _slots[i].Cooldown > 0f) continue;
                var unit = _hero.Unit;
                unit.Revive();
                unit.SetCurrentHealth(Mathf.Max(1, Mathf.RoundToInt(unit.MaxHealth * skill.PowerAt(Level))));
                unit.Shield(skill.DurationAt(Level));
                _slots[i].Cooldown = skill.cooldownSeconds;
                UI.RespawnFlash.Play(transform);
                Used?.Invoke(i);
                return true;
            }
            return false;
        }

        private bool Apply(HeroSkillBalance skill)
        {
            var unit = _hero.Unit;
            var power = skill.PowerAt(Level);
            var seconds = skill.DurationAt(Level);
            switch (skill.id)
            {
                case KingHeal:
                    unit.SetCurrentHealth(unit.CurrentHealth + Mathf.RoundToInt(unit.MaxHealth * power));
                    UI.RespawnFlash.Play(transform);
                    break;
                case KingShield:
                    unit.Shield(seconds);
                    break;
                case KingRage:
                    unit.Enrage(power, seconds);
                    break;
                case KingBlessing:
                {
                    HeroBlessing.Begin(seconds);
                    var castle = Castle;
                    if (castle != null) UI.RespawnFlash.Play(castle.transform.position); else UI.RespawnFlash.Play(transform);
                    break;
                }
                case QueenShot:
                {
                    var target = StrongestEnemyWithin(Reach(skill));
                    if (target == null) return false;
                    // a boss loses a share of its health; anything else dies
                    var damage = IsBoss(target) ? Mathf.RoundToInt(target.MaxHealth * power) : target.CurrentHealth;
                    CombatProjectiles.Fire(ProjectileKind.Arrow, CombatProjectiles.MuzzleOfUnit(transform), target, Mathf.Max(1, damage), unit);
                    break;
                }
                case QueenVolley:
                {
                    var reach = Reach(skill);
                    var nearest = NearestEnemyWithin(reach);
                    if (nearest == null) return false;
                    var along = Flat(nearest.transform.position - transform.position).normalized;
                    var damage = Mathf.Max(1, Mathf.RoundToInt(unit.AttackDamage * power));
                    var muzzle = CombatProjectiles.MuzzleOfUnit(transform);
                    foreach (var orc in OrcUnit.All)
                    {
                        if (orc == null || orc.CurrentHealth <= 0) continue;
                        var to = Flat(orc.transform.position - transform.position);
                        if (to.sqrMagnitude > reach * reach) continue;
                        if (to.sqrMagnitude > 0.01f && Vector3.Angle(along, to) > VolleyHalfAngle) continue;
                        CombatProjectiles.Fire(ProjectileKind.Arrow, muzzle, orc, damage, unit);
                    }
                    break;
                }
                case QueenSummon:
                    if (ArmyManager.Instance == null) return false;
                    ArmyManager.Instance.SpawnSummons(SummonedType, transform.position, Mathf.Max(1, Mathf.RoundToInt(power)),
                        Mathf.Max(1, skill.tier), seconds);
                    UI.RespawnFlash.Play(transform);
                    break;
                case QueenSacrifice:
                    unit.Shield(seconds);
                    unit.Enrage(power, seconds);
                    _sacrificeLeft = seconds;
                    break;
                case CaptainMight:
                    HeroAuras.Might(transform, Reach(skill), power, seconds);
                    break;
                case CaptainRescue:
                {
                    var castle = Castle;
                    if (castle == null) return false;
                    castle.Shield(seconds);
                    UI.RespawnFlash.Play(castle.transform.position);
                    break;
                }
                case CaptainSpirit:
                    CitizenMilitia.Raise(seconds, Mathf.RoundToInt(power), Reach(skill));
                    break;
                default:
                    Debug.LogWarning($"HeroSkills: no effect is written for the skill '{skill.id}'.");
                    return false;
            }
            return true;
        }

        private static Buildings.BuildingInstance Castle => HeroRoster.Castle;

        /// <summary>A skill's reach: its own radius, else the hero's engage radius.</summary>
        private float Reach(HeroSkillBalance skill) =>
            skill.radius > 0f ? skill.radius : (_hero != null && _hero.Balance != null ? _hero.Balance.engageRadius : 8f);

        /// <summary>The giant, a chief, the boss: the Queen's shot takes only a share of them.</summary>
        public static bool IsBoss(OrcUnit orc) => orc.Kind == OrcKind.Giant || orc.Kind == OrcKind.Chief || orc.Kind == OrcKind.Boss;

        private OrcUnit StrongestEnemyWithin(float reach)
        {
            OrcUnit strongest = null;
            foreach (var orc in OrcUnit.All)
            {
                if (orc == null || orc.CurrentHealth <= 0) continue;
                if (Flat(orc.transform.position - transform.position).sqrMagnitude > reach * reach) continue;
                if (strongest == null || orc.MaxHealth > strongest.MaxHealth) strongest = orc;
            }
            return strongest;
        }

        private OrcUnit NearestEnemyWithin(float reach)
        {
            OrcUnit nearest = null;
            var best = reach * reach;
            foreach (var orc in OrcUnit.All)
            {
                if (orc == null || orc.CurrentHealth <= 0) continue;
                var d = Flat(orc.transform.position - transform.position).sqrMagnitude;
                if (d > best) continue;
                best = d;
                nearest = orc;
            }
            return nearest;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private void Update()
        {
            var dt = Time.deltaTime;
            for (var i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Cooldown > 0f) _slots[i].Cooldown = Mathf.Max(0f, _slots[i].Cooldown - dt);
            }
            // the Queen's Жертва: when it is over she falls
            if (_sacrificeLeft >= 0f)
            {
                _sacrificeLeft -= dt;
                if (_sacrificeLeft < 0f && _hero != null && !_hero.IsFallen) _hero.Fall();
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
