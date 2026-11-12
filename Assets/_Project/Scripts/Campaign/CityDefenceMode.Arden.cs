using System.Collections;
using System.Collections.Generic;
using CityBuilder.CameraControl;
using CityBuilder.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 2's Ардан (the user's first playtest, 2026-11-12): run by the stick, MORTAL (dead = the
    /// mission lost), he levels on the orcs he fells himself (experience = the orc's health) and each level
    /// opens a skill: 1 «Боевой клич» (he and the allies near strike harder, the orcs near stagger),
    /// 2 «Круговой удар» (a blow round him that throws them back), 3 «Натиск» (a dash the stick's way,
    /// striking and staggering all on the line), 4 «Второе дыхание» (he and the allies near heal fast).
    /// The healers on the square mend him while he stands among them -- the way out of a losing fight.
    /// </summary>
    public partial class CityDefenceMode
    {
        public const int SkillCount = 4;
        public const int Cry = 0, Whirl = 1, Dash = 2, Breath = 3;

        public SoldierUnit Arden { get; private set; }
        public int ArdenLevel { get; private set; } = 1;
        public int ArdenXp { get; private set; }

        /// <summary>A level was reached (the HUD's banner): the new level.</summary>
        public event System.Action<int> ArdenLeveled;

        /// <summary>A skill was used (the HUD's flash).</summary>
        public event System.Action<int> SkillUsed;

        /// <summary>The stick's reading (screen up = away from the camera = north), from the HUD.</summary>
        public Vector2 Stick { get; set; }

        private ArmyGroup _ardenGroup;
        private readonly float[] _cooldown = new float[SkillCount];
        private float _healCarry;

        public bool SkillOpen(int skill) => skill >= 0 && skill < SkillCount && ArdenLevel > skill;

        /// <summary>0 ready .. 1 just used.</summary>
        public float SkillCooldownShare(int skill)
        {
            var full = CooldownOf(skill);
            return full > 0f ? Mathf.Clamp01(_cooldown[skill] / full) : 0f;
        }

        private static float CooldownOf(int skill)
        {
            var b = B;
            switch (skill)
            {
                case Cry: return b.cryCooldownSeconds;
                case Whirl: return b.whirlCooldownSeconds;
                case Dash: return b.dashCooldownSeconds;
                default: return b.breathCooldownSeconds;
            }
        }

        /// <summary>Experience to reach the next level (all told), or 0 at the last.</summary>
        public int XpForNext => ArdenLevel == 1 ? B.ardenXp2 : ArdenLevel == 2 ? B.ardenXp3 : ArdenLevel == 3 ? B.ardenXp4 : 0;

        /// <summary>Experience the current level began at.</summary>
        public int XpOfLevel => ArdenLevel == 2 ? B.ardenXp2 : ArdenLevel == 3 ? B.ardenXp3 : ArdenLevel >= 4 ? B.ardenXp4 : 0;

        private bool ArdenAlive => Arden != null && Arden.CurrentHealth > 0;

        private void RaiseArden()
        {
            var army = ArmyManager.Instance;
            if (army == null) return;
            var at = _start;
            if (NavMesh.SamplePosition(at, out var hit, 4f, NavMesh.AllAreas)) at = hit.position;
            _ardenGroup = new ArmyGroup(SoldierType.ManAtArms, at);
            Arden = army.SpawnHireling(SoldierType.ManAtArms, at, _ardenGroup);
            if (Arden == null) return;
            Arden.name = "Arden";
            Arden.SteerArea = LitArea(1f);
            ApplyLevel(fullHealth: true);
            JumpToArden();
        }

        private void JumpToArden()
        {
            if (Arden == null) return;
            RTSCameraController.Instance?.JumpTo(Arden.transform.position);
        }

        private void ApplyLevel(bool fullHealth)
        {
            var b = B;
            var max = b.ardenHealth + b.ardenHealthPerLevel * (ArdenLevel - 1);
            var damage = b.ardenDamage + b.ardenDamagePerLevel * (ArdenLevel - 1);
            var share = !fullHealth && Arden.MaxHealth > 0 ? Arden.CurrentHealth / (float)Arden.MaxHealth : 1f;
            Arden.SetHeroStats(max, damage);
            Arden.SetCurrentHealth(Mathf.Max(1, Mathf.RoundToInt(max * share)));
        }

        private void OnOrcDied(OrcUnit orc)
        {
            if (orc == null || Arden == null || orc.KilledBy != Arden || ArdenLevel >= SkillCount) return;
            ArdenXp += Mathf.Max(1, orc.MaxHealth);
            while (ArdenLevel < SkillCount && XpForNext > 0 && ArdenXp >= XpForNext)
            {
                ArdenLevel++;
                ApplyLevel(fullHealth: false);
                ArdenLeveled?.Invoke(ArdenLevel);
            }
        }

        /// <summary>Cheat (the user, 2026-11-12): Ардан a level up (the skills to try).</summary>
        public void CheatLevelUp()
        {
            if (Arden == null || ArdenLevel >= SkillCount) return;
            ArdenXp = Mathf.Max(ArdenXp, XpForNext);
            ArdenLevel++;
            ApplyLevel(fullHealth: true);
            ArdenLeveled?.Invoke(ArdenLevel);
        }

        private void TickArden(float dt)
        {
            for (var i = 0; i < SkillCount; i++)
                if (_cooldown[i] > 0f) _cooldown[i] -= dt;
            if (Current == Stage.Courier) return;
            if (!ArdenAlive)
            {
                // mortal: his fall is the mission's (the user's first playtest)
                if (Current == Stage.Part1 || Current == Stage.Part2) Lose(LossReason.Arden);
                return;
            }
            // the camera looks north: the stick's up is +z
            var steer = new Vector3(Stick.x, 0f, Stick.y);
            var was = Arden.IsSteered;
            Arden.Steer = steer;
            if (was && !Arden.IsSteered) _ardenGroup.OrderMoveTo(Arden.transform.position);   // let go: he holds there and fights
            RTSCameraController.Instance?.FocusOn(Arden.transform.position);
        }

        /// <summary>A skill button: true when it went off.</summary>
        public bool UseSkill(int skill)
        {
            if (!SkillOpen(skill) || _cooldown[skill] > 0f || !ArdenAlive || (Current != Stage.Part1 && Current != Stage.Part2)) return false;
            var b = B;
            var at = Arden.transform.position;
            switch (skill)
            {
                case Cry:
                    Arden.Enrage(b.cryDamageMult, b.crySeconds);
                    foreach (var guard in _garrison)
                        if (guard != null && guard.CurrentHealth > 0 && Flat(guard.transform.position - at) <= b.cryRadius) guard.Enrage(b.cryDamageMult, b.crySeconds);
                    foreach (var orc in OrcUnit.All)
                        if (orc != null && orc.CurrentHealth > 0 && Flat(orc.transform.position - at) <= b.cryRadius) orc.BusyFor = Mathf.Max(orc.BusyFor, b.cryStunSeconds);
                    break;
                case Whirl:
                    var blow = Mathf.RoundToInt(Arden.AttackDamage * b.whirlDamageMult);
                    foreach (var orc in new List<OrcUnit>(OrcUnit.All))
                    {
                        if (orc == null || orc.CurrentHealth <= 0) continue;
                        var away = orc.transform.position - at;
                        away.y = 0f;
                        if (away.magnitude > b.whirlRadius) continue;
                        orc.TakeDamage(blow, Arden);
                        if (orc != null && orc.CurrentHealth > 0)
                            orc.Knock((away.sqrMagnitude > 0.01f ? away.normalized : Random.insideUnitSphere) * b.whirlPush, 0.25f);
                    }
                    break;
                case Dash:
                    var dir = Stick.sqrMagnitude > 0.01f ? new Vector3(Stick.x, 0f, Stick.y) : Arden.transform.forward;
                    StartCoroutine(Dashing(dir));
                    break;
                default:
                    StartCoroutine(Breathing());
                    break;
            }
            _cooldown[skill] = CooldownOf(skill);
            SkillUsed?.Invoke(skill);
            return true;
        }

        private IEnumerator Dashing(Vector3 dir)
        {
            var b = B;
            Arden.Dash(dir, b.dashDistance, b.dashSeconds);
            var hit = new HashSet<OrcUnit>();
            var blow = Mathf.RoundToInt(Arden.AttackDamage * b.dashDamageMult);
            while (Arden != null && Arden.IsDashing)
            {
                var at = Arden.transform.position;
                foreach (var orc in new List<OrcUnit>(OrcUnit.All))
                {
                    if (orc == null || orc.CurrentHealth <= 0 || hit.Contains(orc) || Flat(orc.transform.position - at) > b.dashWidth) continue;
                    hit.Add(orc);
                    orc.BusyFor = Mathf.Max(orc.BusyFor, b.dashStunSeconds);
                    orc.TakeDamage(blow, Arden);
                }
                yield return null;
            }
            if (Arden != null) _ardenGroup.OrderMoveTo(Arden.transform.position);
        }

        private IEnumerator Breathing()
        {
            var b = B;
            var at = Arden.transform.position;
            foreach (var guard in _garrison)
                if (guard != null && guard.CurrentHealth > 0 && Flat(guard.transform.position - at) <= b.breathRadius)
                    guard.Heal(Mathf.RoundToInt(guard.MaxHealth * b.breathAllyHeal));
            var total = Arden.MaxHealth * b.breathHeal;
            var seconds = Mathf.Max(0.1f, b.breathSeconds);
            var given = 0f;
            for (var t = 0f; t < seconds && ArdenAlive; t += Time.deltaTime)
            {
                var due = total * Mathf.Clamp01(t / seconds) - given;
                var whole = Mathf.FloorToInt(due);
                if (whole > 0)
                {
                    Arden.Heal(whole);
                    given += whole;
                }
                yield return null;
            }
        }

        // ------------------------------------------------------------------ the healers on the square

        private LineRenderer _healRing;

        private void RaiseHealers()
        {
            var army = ArmyManager.Instance;
            var b = B;
            if (army != null)
            {
                var stats = SoldierStats.StatsAt(SoldierType.Monk, b.guardLevel);
                for (var i = 0; i < b.squareHealers; i++)
                {
                    var angle = (i + 0.5f) / Mathf.Max(1, b.squareHealers) * Mathf.PI * 2f;
                    var at = _square + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (b.healRadius * 0.6f);
                    if (NavMesh.SamplePosition(at, out var hit, 2f, NavMesh.AllAreas)) at = hit.position;
                    var group = new ArmyGroup(SoldierType.Monk, at);
                    var monk = army.SpawnHireling(SoldierType.Monk, at, group);
                    if (monk == null) continue;
                    monk.SetHeroStats(stats.maxHealth, stats.attackDamage);
                    monk.SetCurrentHealth(stats.maxHealth);
                    group.OrderMoveTo(at);
                }
            }
            // where they mend him: a soft green ring on the square
            var go = new GameObject("HealersRing", typeof(LineRenderer));
            go.transform.SetParent(transform, false);
            _healRing = go.GetComponent<LineRenderer>();
            const int points = 48;
            _healRing.positionCount = points;
            _healRing.loop = true;
            _healRing.useWorldSpace = true;
            _healRing.widthMultiplier = 0.08f;
            _healRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _healRing.receiveShadows = false;
            _healRing.material = new Material(Shader.Find("Sprites/Default"));
            _healRing.startColor = _healRing.endColor = new Color(0.45f, 1f, 0.5f, 0.75f);
            for (var i = 0; i < points; i++)
            {
                var a = i / (float)points * Mathf.PI * 2f;
                _healRing.SetPosition(i, _square + new Vector3(Mathf.Cos(a) * b.healRadius, 0.06f, Mathf.Sin(a) * b.healRadius));
            }
        }

        /// <summary>He stands among the healers: he mends (m2_heal_per_sec of his health a second).</summary>
        public bool BeingHealed { get; private set; }

        private void TickHealers(float dt)
        {
            BeingHealed = false;
            if (!ArdenAlive || Arden.CurrentHealth >= Arden.MaxHealth) return;
            if (Flat(Arden.transform.position - _square) > B.healRadius) return;
            BeingHealed = true;
            _healCarry += Arden.MaxHealth * B.healPerSecond * dt;
            var whole = Mathf.FloorToInt(_healCarry);
            if (whole <= 0) return;
            _healCarry -= whole;
            Arden.Heal(whole);
        }
    }
}
