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

    /// <summary>What a squad does with no order of the player's to carry out (the user, 2026-11-05). Pinned ints: saved.</summary>
    public enum SquadBehaviour
    {
        /// <summary>«Стоять»: holds its place, fights what comes near, a short way after a shooter.</summary>
        Hold = 0,
        /// <summary>«Охранять город»: walks round the Town Hall, chases what comes into the town's zone, comes back.</summary>
        Guard = 1,
        /// <summary>«Наступать»: goes after the nearest enemy on its island, and after any shooter.</summary>
        Advance = 2,
        /// <summary>«За героем»: walks behind a hero and fights beside him.</summary>
        FollowHero = 3,
    }

    /// <summary>How a squad stands (the user, 2026-11-05: квадрат, треугольник, врассыпную). Pinned ints: saved.</summary>
    public enum SquadFormation
    {
        Square = 0,
        Triangle = 1,
        Scatter = 2,
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

        public SquadBehaviour Behaviour { get; private set; } = SquadBehaviour.Hold;
        public SquadFormation Formation { get; private set; } = SquadFormation.Square;

        /// <summary>The hero a squad «За героем» walks behind.</summary>
        public SoldierType FollowHero { get; set; } = SoldierType.King;

        /// <summary>Where the squad faces: the way it was last sent. Its front rank stands on that side.</summary>
        public Vector3 Facing { get; private set; } = Vector3.back;

        /// <summary>The «Охранять город» round: which point of it the squad walks to.</summary>
        public int PatrolStep { get; set; }

        public void SetBehaviour(SquadBehaviour behaviour)
        {
            if (Behaviour == behaviour) return;
            Behaviour = behaviour;
            // a target the last behaviour chose is not the new one's business
            if (AttackTarget != null && !_playerAttack) AttackTarget = null;
            OnChanged?.Invoke();
        }

        public void SetFormation(SquadFormation formation)
        {
            if (Formation == formation) return;
            Formation = formation;
            _slotsDirty = true;
            OnChanged?.Invoke();
        }

        /// <summary>The rally point moved by the squad's own behaviour (a patrol step, behind its hero), not an order.</summary>
        public void SetHold(Vector3 position, Vector3 facing)
        {
            HoldPosition = position;
            TurnTo(facing);
        }

        private void TurnTo(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.25f) Facing = direction.normalized;
        }

        /// <summary>The middle of the squad as it stands now (the hold point when empty).</summary>
        public Vector3 Centre()
        {
            var sum = Vector3.zero;
            var n = 0;
            for (var i = 0; i < _members.Count; i++)
            {
                if (_members[i] == null) continue;
                sum += _members[i].transform.position;
                n++;
            }
            return n > 0 ? sum / n : HoldPosition;
        }

        // the attack order is the player's (ArmyOrderInput), not one a behaviour picked
        private bool _playerAttack;

        /// <summary>A behaviour's own target (Advance, Guard): moves the rally point like an order but is not the player's.</summary>
        public void AttackOnItsOwn(IDamageTarget target)
        {
            if (target == null || !target.IsAlive) return;
            AttackTarget = target;
            _playerAttack = false;
            TurnTo(target.Transform.position - Centre());
            HoldPosition = target.Transform.position;
            OnChanged?.Invoke();
        }

        /// <summary>A behaviour's own target dropped (it left the town's zone).</summary>
        public void DropOwnAttack()
        {
            if (AttackTarget == null || _playerAttack) return;
            AttackTarget = null;
            OnChanged?.Invoke();
        }

        public bool HasPlayerAttack => AttackTarget != null && _playerAttack && AttackTarget.IsAlive;

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
            _slotsDirty = true;
            OnChanged?.Invoke();
        }

        public void Remove(SoldierUnit unit)
        {
            if (!_members.Remove(unit)) return;
            _posts.Remove(unit);
            _slotsDirty = true;
            OnChanged?.Invoke();
        }

        /// <summary>Walk there and hold. Clears any attack order -- a move order is the player overriding what the group was doing.</summary>
        public void OrderMoveTo(Vector3 destination)
        {
            // an order on the ground brings whoever is on the walls down first
            for (var i = 0; i < _members.Count; i++) _members[i].LeaveWall(destination);
            TurnTo(destination - Centre());
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
            TurnTo(target.Transform.position - Centre());
            AttackTarget = target;
            _playerAttack = true;
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
            _playerAttack = true;
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
            if (IsSquad) return HoldPosition + FormationSlot(unit);
            var index = _members.IndexOf(unit);
            return HoldPosition + FormationOffset(index < 0 ? 0 : index);
        }

        // ---- formations (squads) ----

        private bool _slotsDirty = true;
        private readonly Dictionary<SoldierUnit, int> _slotOf = new Dictionary<SoldierUnit, int>();
        private readonly List<SoldierUnit> _byRank = new List<SoldierUnit>();

        /// <summary>Front to back: fighters on foot and riders, then shooters, then the monk (the user's Линия idea kept in every formation).</summary>
        private static int Rank(SoldierUnit unit)
        {
            if (unit == null) return 9;
            if (unit.IsHealer) return 2;
            if (unit.IsRanged) return 1;
            return 0;
        }

        private void RebuildSlots()
        {
            _slotsDirty = false;
            _byRank.Clear();
            _slotOf.Clear();
            for (var i = 0; i < _members.Count; i++) if (_members[i] != null) _byRank.Add(_members[i]);
            // stable: within a rank, the order they joined in -- a resort must not shuffle the ranks
            for (var i = 0; i < _byRank.Count; i++) _slotOf[_byRank[i]] = i;
            _byRank.Sort((a, b) =>
            {
                var r = Rank(a).CompareTo(Rank(b));
                return r != 0 ? r : _slotOf[a].CompareTo(_slotOf[b]);
            });
            _slotOf.Clear();
            for (var i = 0; i < _byRank.Count; i++) _slotOf[_byRank[i]] = i;
        }

        /// <summary>The unit's place in the squad's formation, turned to its facing, from the hold point.</summary>
        private Vector3 FormationSlot(SoldierUnit unit)
        {
            if (_slotsDirty) RebuildSlots();
            if (!_slotOf.TryGetValue(unit, out var index)) index = 0;
            var local = FormationOffset(Formation, index, _byRank.Count);
            var forward = Facing;
            var right = new Vector3(forward.z, 0f, -forward.x);
            return right * local.x + forward * local.y;
        }

        /// <summary>
        /// A formation's slot `index` of `count`, as (right, forward) from the middle; slot 0 in the front.
        /// Pure, for the EditMode tests. Spacing from economy (squad_formation_spacing, squad_scatter_spacing).
        /// </summary>
        public static Vector2 FormationOffset(SquadFormation formation, int index, int count)
        {
            var spacing = Core.BalanceConfig.Instance != null ? Core.BalanceConfig.Instance.SquadFormationSpacing : 0.9f;
            count = Mathf.Max(1, count);
            switch (formation)
            {
                case SquadFormation.Triangle:
                {
                    // row r holds r + 1: the apex leads
                    var row = 0;
                    var first = 0;
                    while (index >= first + row + 1)
                    {
                        first += row + 1;
                        row++;
                    }
                    var lastRow = 0;
                    var filled = 0;
                    while (filled + lastRow + 1 < count)
                    {
                        filled += lastRow + 1;
                        lastRow++;
                    }
                    var k = index - first;
                    return new Vector2((k - row * 0.5f) * spacing, (lastRow * 0.5f - row) * spacing);
                }
                case SquadFormation.Scatter:
                {
                    var wide = Core.BalanceConfig.Instance != null ? Core.BalanceConfig.Instance.SquadScatterSpacing : 1.8f;
                    var cols = Mathf.CeilToInt(Mathf.Sqrt(count));
                    var rows = Mathf.CeilToInt(count / (float)cols);
                    var col = index % cols;
                    var row = index / cols;
                    // a fixed shake per slot, so a scattered squad does not stand in a grid
                    var jx = Mathf.Sin(index * 12.9898f) * 0.3f;
                    var jz = Mathf.Sin(index * 78.233f) * 0.3f;
                    return new Vector2((col - (cols - 1) * 0.5f + jx) * wide, ((rows - 1) * 0.5f - row + jz) * wide);
                }
                default:
                {
                    var cols = Mathf.CeilToInt(Mathf.Sqrt(count));
                    var rows = Mathf.CeilToInt(count / (float)cols);
                    var col = index % cols;
                    var row = index / cols;
                    return new Vector2((col - (cols - 1) * 0.5f) * spacing, ((rows - 1) * 0.5f - row) * spacing);
                }
            }
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
