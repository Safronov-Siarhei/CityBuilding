using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// One commandable formation of soldiers. The player never orders individual units around:
    /// they pick a group's icon in the army panel and order the whole group -- go there, attack
    /// that, or hold with a given target priority.
    ///
    /// Today ArmyManager keeps exactly one group per SoldierType, so the panel shows one icon per
    /// type ("Ополчение x5") and a newly recruited soldier joins its type's group automatically.
    /// The group is still modelled as its own object with an id and an explicit member list rather
    /// than as "everything of type X", because splitting one type into two squads (one defending
    /// home, one assaulting a portal) is a planned follow-up -- with this shape that becomes a
    /// change in how groups are created, not a rewrite of orders, targeting and UI.
    /// </summary>
    public class ArmyGroup
    {
        /// <summary>Spacing between neighbouring formation slots, in metres.</summary>
        private const float FormationSpacing = 0.9f;

        private static int _nextId = 1;

        private readonly List<SoldierUnit> _members = new List<SoldierUnit>();

        public ArmyGroup(SoldierType type, Vector3 holdPosition)
        {
            Id = _nextId++;
            Type = type;
            HoldPosition = holdPosition;
        }

        public int Id { get; }
        public SoldierType Type { get; }
        public IReadOnlyList<SoldierUnit> Members => _members;
        public int Count => _members.Count;

        /// <summary>Where the group stands when it has nothing to attack -- its rally point, moved by every "go there" order.</summary>
        public Vector3 HoldPosition { get; private set; }

        /// <summary>Persistent per-group mode (see TargetPriority), not consumed by executing an order.</summary>
        public TargetPriority Priority { get; set; } = TargetPriority.Units;

        /// <summary>An explicit "attack this" order. Units chase it across the map, ignoring the hold-position leash, until it dies.</summary>
        public IDamageTarget AttackTarget { get; private set; }

        /// <summary>Raised on membership, order and priority changes so the army panel can refresh without polling every frame.</summary>
        public event Action OnChanged;

        public void Add(SoldierUnit unit)
        {
            if (unit == null || _members.Contains(unit)) return;
            _members.Add(unit);
            OnChanged?.Invoke();
        }

        public void Remove(SoldierUnit unit)
        {
            if (!_members.Remove(unit)) return;
            OnChanged?.Invoke();
        }

        /// <summary>Walk there and hold. Clears any attack order -- a move order is the player overriding what the group was doing.</summary>
        public void OrderMoveTo(Vector3 destination)
        {
            HoldPosition = destination;
            AttackTarget = null;
            OnChanged?.Invoke();
        }

        /// <summary>
        /// Attack this specific enemy, wherever it is. The hold position moves to the target as
        /// well, so once the target dies the group stays where the fight was instead of walking
        /// all the way back to where it was standing when the order was given.
        /// </summary>
        public void OrderAttack(IDamageTarget target)
        {
            if (target == null || !target.IsAlive) return;
            AttackTarget = target;
            HoldPosition = target.Transform.position;
            OnChanged?.Invoke();
        }

        public void SetPriority(TargetPriority priority)
        {
            if (Priority == priority) return;
            Priority = priority;
            OnChanged?.Invoke();
        }

        /// <summary>Drops a finished attack order. Called by members once the target is gone, so the whole group reverts to holding together rather than each unit deciding separately.</summary>
        public void ClearFinishedAttackOrder()
        {
            if (AttackTarget == null || AttackTarget.IsAlive) return;
            AttackTarget = null;
            OnChanged?.Invoke();
        }

        /// <summary>Where this member stands relative to HoldPosition, so a group of ten doesn't pile into one point and shove each other around.</summary>
        public Vector3 SlotPositionFor(SoldierUnit unit)
        {
            var index = _members.IndexOf(unit);
            return HoldPosition + FormationOffset(index < 0 ? 0 : index);
        }

        /// <summary>
        /// Concentric rings around the centre: slot 0 at the middle, then 6 slots at one spacing,
        /// 12 at two, and so on. Pure and static so the EditMode tests can pin down that no two
        /// members ever share a slot.
        /// </summary>
        public static Vector3 FormationOffset(int memberIndex)
        {
            if (memberIndex <= 0) return Vector3.zero;

            var remaining = memberIndex - 1;
            var ring = 1;
            while (remaining >= ring * 6)
            {
                remaining -= ring * 6;
                ring++;
            }

            var slotsInRing = ring * 6;
            var angle = remaining / (float)slotsInRing * Mathf.PI * 2f;
            var radius = ring * FormationSpacing;
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }
    }
}
