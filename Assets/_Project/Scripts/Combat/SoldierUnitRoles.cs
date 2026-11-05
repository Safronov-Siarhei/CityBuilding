using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// The roster widening's roles (the user, 2026-11-05: roles and counters, not a ladder), all
    /// read from the units tab:
    /// - a blow does `vs_armoured` times its damage to an armoured enemy (the crossbowman's bolt)
    ///   and `vs_cavalry` times to a horseman (the spearman);
    /// - the knight's CHARGE: the first blow after a run of `charge_run` cells does `charge_mult`;
    /// - the monk is a HEALER: he never strikes; every attack interval he gives his attack damage
    ///   back as health to the most wounded soldier within his reach, and walks to one hurt within
    ///   his engage radius.
    /// </summary>
    public partial class SoldierUnit
    {
        private UnitBalance _row;
        private Vector3 _lastStep;
        private float _run;
        private float _stillFor;

        /// <summary>A horseman (units tab `cavalry`): walks no walls, a spear hits it harder.</summary>
        public bool IsCavalry => _row != null && _row.cavalry;

        /// <summary>A healer (the monk): no blows, health for the others.</summary>
        public bool IsHealer => _row != null && _row.healer;

        /// <summary>The cells run since the last blow (the charge's run-up). For the tests.</summary>
        public float RunUp => _run;

        /// <summary>Counts the ground covered since the last blow; standing a second spends the run-up.</summary>
        private void TrackRun()
        {
            var p = transform.position;
            var step = HorizontalDistance(p, _lastStep);
            _lastStep = p;
            if (step > 0.002f && step < 2f)
            {
                _run += step;
                _stillFor = 0f;
                return;
            }
            _stillFor += Time.deltaTime;
            if (_stillFor > 1f) _run = 0f;
        }

        /// <summary>This blow's damage on that target: the counters, then a charge spent.</summary>
        private int StrikeDamageOn(IDamageTarget target)
        {
            var damage = (float)StrikeDamage;
            if (_row != null && target is OrcUnit orc && orc.Row != null)
            {
                if (orc.Row.armoured) damage *= _row.vsArmoured;
                if (orc.Row.cavalry) damage *= _row.vsCavalry;
            }
            if (_row != null && _row.chargeRun > 0f && _run >= _row.chargeRun) damage *= _row.chargeMultiplier;
            _run = 0f;
            return Mathf.Max(1, Mathf.RoundToInt(damage));
        }

        /// <summary>The healer's turn: true when he did something (healed or walked to a hurt soldier).</summary>
        private bool TickHealer()
        {
            var reach = _unitAttackRange;
            SoldierUnit worst = null;
            SoldierUnit nearHurt = null;
            var worstShare = 1f;
            var nearDistance = _engageRadius;
            foreach (var other in _all)
            {
                if (other == null || other == this || other.CurrentHealth <= 0 || other.CurrentHealth >= other.MaxHealth) continue;
                var d = HorizontalDistance(other.transform.position, transform.position);
                if (d <= reach)
                {
                    var share = other.CurrentHealth / (float)Mathf.Max(1, other.MaxHealth);
                    if (share < worstShare)
                    {
                        worstShare = share;
                        worst = other;
                    }
                }
                else if (d < nearDistance)
                {
                    nearDistance = d;
                    nearHurt = other;
                }
            }

            if (worst != null)
            {
                FaceTowards(worst.transform.position);
                _attackTimer -= Time.deltaTime;
                if (_attackTimer > 0f) return true;
                _attackTimer = AttackInterval;
                Struck?.Invoke();
                worst.Heal(_attackDamage);
                return true;
            }
            if (nearHurt != null && !Marching)
            {
                WalkTowards(nearHurt.transform.position);
                return true;
            }
            return false;
        }
    }
}
