using CityBuilder.Combat;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Invasion
{
    /// <summary>
    /// A run's soldiers from the Barracks (the user, 2026-10-23): nobody commands them -- the player
    /// commands only the heroes. Like the King when he is not in hand, each type's group stands
    /// before the castle and goes for the orc nearest the castle within inv_soldier_guard_radius,
    /// then comes back. The heroes and the Queen's summons keep their own ways.
    /// </summary>
    public class InvasionGarrison : MonoBehaviour
    {
        private const float ScanSeconds = 0.5f;
        /// <summary>How far before the castle each type's group stands, on its own side of it.</summary>
        private const float PostMeters = 4.5f;

        private Vector3 _castle;
        private float _scan;

        public void Begin(Vector3 castle) => _castle = castle;

        /// <summary>Where a type's group waits: round the castle, the types spread so they do not stand on the King (who waits south of it).</summary>
        public Vector3 PostOf(SoldierType type)
        {
            var angle = (90f + 72f * (int)type) * Mathf.Deg2Rad;
            return _castle + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * PostMeters;
        }

        private void Update()
        {
            _scan -= Time.deltaTime;
            if (_scan > 0f) return;
            _scan = ScanSeconds;

            var army = ArmyManager.Instance;
            if (army == null) return;
            var threat = NearestThreat();
            foreach (var group in army.Groups)
            {
                if (group == null || group.Count == 0 || group.IsSummoned || SoldierStats.IsHero(group.Type)) continue;
                if (threat != null)
                {
                    if (group.AttackTarget != (IDamageTarget)threat) group.OrderAttack(threat);
                    continue;
                }
                var post = PostOf(group.Type);
                if (group.AttackTarget == null && Flat(group.HoldPosition - post).sqrMagnitude > 1f) group.OrderMoveTo(post);
            }
        }

        /// <summary>The enemy nearest the castle among those inside the guard radius, or null.</summary>
        private OrcUnit NearestThreat()
        {
            var radius = BalanceConfig.Instance.InvasionSoldierGuardRadius;
            OrcUnit nearest = null;
            var best = radius * radius;
            foreach (var orc in OrcUnit.All)
            {
                if (orc == null || orc.CurrentHealth <= 0) continue;
                var d = Flat(orc.transform.position - _castle).sqrMagnitude;
                if (d > best) continue;
                best = d;
                nearest = orc;
            }
            return nearest;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
