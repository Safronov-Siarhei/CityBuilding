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
    /// <summary>
    /// What a group is (army control by squads, the user 2026-11-05, Design/features/army-control.md):
    /// a type group (a hero, the Invasion's per-type groups, hirelings, summons -- the old shape),
    /// the GARRISON every recruit joins, one of the player's SQUADS, or one siege MACHINE alone.
    /// Pinned ints: saved.
    /// </summary>
    public enum ArmyGroupKind
    {
        Type = 0,
        Garrison = 1,
        Squad = 2,
        Machine = 3,
    }

    public class ArmyGroup
    {
        /// <summary>Spacing between neighbouring formation slots, in metres.</summary>
        private const float FormationSpacing = 0.9f;

        private static int _nextId = 1;

        private readonly List<SoldierUnit> _members = new List<SoldierUnit>();

        public ArmyGroup(SoldierType type, Vector3 holdPosition, ArmyGroupKind kind = ArmyGroupKind.Type)
        {
            Id = _nextId++;
            Type = type;
            Kind = kind;
            HoldPosition = holdPosition;
        }

        public int Id { get; }

        /// <summary>The members' type for a type group or a machine; a garrison or a squad is mixed (read each member's own Type).</summary>
        public SoldierType Type { get; }

        public ArmyGroupKind Kind { get; }
        public bool IsGarrison => Kind == ArmyGroupKind.Garrison;
        public bool IsSquad => Kind == ArmyGroupKind.Squad;
        public bool IsMachine => Kind == ArmyGroupKind.Machine;

        /// <summary>A squad's own name (renamed in the squad window); empty = «Отряд N».</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>A squad's crest, an index into the crest set.</summary>
        public int Crest { get; set; }

        /// <summary>
        /// «Пополнять из Гарнизона» (the user, 2026-11-05): a squad standing on the main island takes
        /// soldiers of the types it lost from the Garrison (ArmyManager.TickRefill). Not hiring.
        /// </summary>
        public bool AutoRefill { get; set; }

        // The makeup the player gave the squad in the window, by SoldierType: what a refill tops up
        // to. Changed by the player's moves only, never by deaths.
        private readonly int[] _wanted = new int[32];

        public int WantedOf(SoldierType type) => (int)type >= 0 && (int)type < _wanted.Length ? _wanted[(int)type] : 0;

        public void SetWanted(SoldierType type, int count)
        {
            if ((int)type >= 0 && (int)type < _wanted.Length) _wanted[(int)type] = Mathf.Max(0, count);
        }

        /// <summary>How many of this type the squad has now.</summary>
        public int CountOf(SoldierType type)
        {
            var n = 0;
            for (var i = 0; i < _members.Count; i++)
                if (_members[i] != null && _members[i].Type == type) n++;
            return n;
        }

        // A garrison soldier stands at his own post (where he was hired) and acts alone (the user,
        // 2026-11-05: «солдаты действуют отдельно и защищают территорию"), not in rings round one point.
        private readonly Dictionary<SoldierUnit, Vector3> _posts = new Dictionary<SoldierUnit, Vector3>();

        /// <summary>A garrison soldier's post.</summary>
        public void SetPost(SoldierUnit unit, Vector3 post) => _posts[unit] = post;

        public bool TryGetPost(SoldierUnit unit, out Vector3 post) => _posts.TryGetValue(unit, out post);

        /// <summary>The Queen's Призыв: soldiers for a while, in no list of the army's (ArmyManager.SpawnSummons).</summary>
        public bool IsSummoned { get; set; }
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

        /// <summary>Raised when the PLAYER gives this group an order (ArmyOrderInput) -- not when something orders it on its own, as the King does when guarding the base (Hero).</summary>
        public event Action PlayerOrdered;

        public void NotifyPlayerOrder() => PlayerOrdered?.Invoke();

        public void Add(SoldierUnit unit)
        {
            if (unit == null || _members.Contains(unit)) return;
            _members.Add(unit);
            OnChanged?.Invoke();
        }

        public void Remove(SoldierUnit unit)
        {
            if (!_members.Remove(unit)) return;
            _posts.Remove(unit);
            OnChanged?.Invoke();
        }

        /// <summary>Walk there and hold. Clears any attack order -- a move order is the player overriding what the group was doing.</summary>
        public void OrderMoveTo(Vector3 destination)
        {
            // an order on the ground brings whoever is on the walls down first
            for (var i = 0; i < _members.Count; i++) _members[i].LeaveWall(destination);
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
            for (var i = 0; i < _members.Count; i++) _members[i].LeaveWall(target.Transform.position);
            AttackTarget = target;
            HoldPosition = target.Transform.position;
            OnChanged?.Invoke();
        }

        /// <summary>
        /// Puts a saved attack order back WITHOUT moving the rally point, which is the whole
        /// reason this is not just OrderAttack. OrderAttack sets HoldPosition to wherever the
        /// target stands at the moment of the order; replaying it on load would set it to wherever
        /// the target stands NOW, quietly overwriting the rally point the save just restored.
        /// </summary>
        public void RestoreAttackOrder(IDamageTarget target)
        {
            if (target == null || !target.IsAlive) return;
            AttackTarget = target;
            OnChanged?.Invoke();
        }

        /// <summary>Puts the rally point back without an order (a load, a new squad).</summary>
        public void SetHold(Vector3 position) => HoldPosition = position;

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
            if (IsGarrison && _posts.TryGetValue(unit, out var post)) return post;
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
