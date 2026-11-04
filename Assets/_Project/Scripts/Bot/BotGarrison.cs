using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Bot
{
    /// <summary>
    /// The bot castle's garrison (the user, 2026-11-03): its King walks the ring of squares round
    /// the moat, archers keep watch on its walls, men-at-arms patrol the four avenues out to the
    /// gates. They are dungeon-hireling style soldiers (ArmyManager.SpawnHireling into groups of
    /// their own): not the player's army, never selectable, no upkeep, at bot_garrison_level's
    /// stats. A fallen one comes back from the Town Hall after bot_garrison_respawn_sec -- while
    /// the hall stands (razed, nobody comes back). Patrol and wall posts are blueprint offsets
    /// from the hall (Design/bot_castle_v2), turned with the castle.
    /// </summary>
    public class BotGarrison : MonoBehaviour
    {
        private enum Duty { King, Patrol, Wall }

        private class Post
        {
            public Duty duty;
            public SoldierType type;
            public int route;
            public Vector2Int wallCell;
            public SoldierUnit unit;
            public float downFor;
            public bool onWall;
        }

        // the routes, as offsets from the hall in blueprint cells (y south): the King round the
        // ring of squares, each patrol up and down an avenue between its drawbridge and its gate
        private static readonly Vector2Int[] KingRing = { new Vector2Int(0, -9), new Vector2Int(9, 0), new Vector2Int(0, 9), new Vector2Int(-9, 0) };
        private static readonly Vector2Int[][] Avenues =
        {
            new[] { new Vector2Int(0, -11), new Vector2Int(0, -15) },
            new[] { new Vector2Int(11, 0), new Vector2Int(15, 0) },
            new[] { new Vector2Int(0, 11), new Vector2Int(0, 15) },
            new[] { new Vector2Int(-11, 0), new Vector2Int(-15, 0) },
        };

        private readonly List<Post> _posts = new List<Post>();
        private ArmyGroup _kingGroup;
        private ArmyGroup[] _patrols;
        private int[] _patrolLeg;
        private int _kingLeg;
        private float _tick;
        private bool _manned;
        private bool _standDown;

        /// <summary>
        /// At war the town's-side garrison goes (BotWar puts the kingdom's hostile guards on the same
        /// posts); at peace it comes back, manned afresh.
        /// </summary>
        public void StandDown(bool down)
        {
            if (_standDown == down) return;
            _standDown = down;
            if (!down) return;
            var army = ArmyManager.Instance;
            foreach (var post in _posts)
            {
                if (post.unit != null && army != null) army.Dismiss(post.unit);
                post.unit = null;
                post.onWall = false;
                post.downFor = 0f;
            }
        }

        /// <summary>Where the garrison stands, for the hostile guards at war: the King's ring, the avenues' ends, the walls' cells.</summary>
        public static void WarPosts(BotKingdom kingdom, List<Vector3> melee, List<Vector3> ranged)
        {
            foreach (var at in KingRing) melee.Add(kingdom.WorldAt(at));
            foreach (var avenue in Avenues)
                foreach (var at in avenue) melee.Add(kingdom.WorldAt(at));
            // the archers behind the gates and the drawbridges: inside each avenue, a cell off its axis
            foreach (var avenue in Avenues)
            {
                var inner = avenue[0];
                var side = inner.x == 0 ? new Vector2Int(2, 0) : new Vector2Int(0, 2);
                ranged.Add(kingdom.WorldAt(inner + side));
                ranged.Add(kingdom.WorldAt(inner - side));
                ranged.Add(kingdom.WorldAt(avenue[1] + side));
                ranged.Add(kingdom.WorldAt(avenue[1] - side));
            }
        }

        private void Update()
        {
            var kingdom = BotKingdom.Instance;
            var army = ArmyManager.Instance;
            var config = BalanceConfig.Instance;
            if (kingdom == null || !kingdom.Placed || army == null || config == null) return;
            if (_standDown) return;
            if (!_manned)
            {
                Man(kingdom, config);
                _manned = true;
            }
            _tick += Time.deltaTime;
            if (_tick < 1f) return;
            var dt = _tick;
            _tick = 0f;

            var canRespawn = !kingdom.HallRazed && kingdom.Hall != null;
            foreach (var post in _posts)
            {
                if (post.unit != null && post.unit.CurrentHealth > 0)
                {
                    if (post.duty == Duty.Wall && !post.onWall) post.onWall = post.unit.GoToWall(post.wallCell);
                    continue;
                }
                post.unit = null;
                post.onWall = false;
                if (!canRespawn) continue;
                post.downFor += dt;
                if (post.downFor < config.BotGarrisonRespawnSeconds) continue;
                post.downFor = 0f;
                Raise(post, kingdom, config);
            }
            Walk(kingdom);
        }

        private void Man(BotKingdom kingdom, BalanceConfig config)
        {
            var at = kingdom.Hall != null ? kingdom.Hall.transform.position : kingdom.WorldAt(Vector2Int.zero);
            _kingGroup = new ArmyGroup(SoldierType.King, at);
            _posts.Add(new Post { duty = Duty.King, type = SoldierType.King });

            _patrols = new ArmyGroup[Avenues.Length];
            _patrolLeg = new int[Avenues.Length];
            var patrols = Mathf.Min(Avenues.Length, config.BotGarrisonPatrols);
            for (var r = 0; r < patrols; r++)
            {
                _patrols[r] = new ArmyGroup(SoldierType.ManAtArms, kingdom.WorldAt(Avenues[r][0]));
                for (var k = 0; k < config.BotGarrisonPatrolSize; k++)
                    _posts.Add(new Post { duty = Duty.Patrol, type = SoldierType.ManAtArms, route = r });
            }

            var walls = new List<Vector2Int>();
            kingdom.WallCells(walls);
            var archers = Mathf.Min(walls.Count, config.BotGarrisonArchers);
            for (var k = 0; k < archers; k++)
                _posts.Add(new Post { duty = Duty.Wall, type = SoldierType.Archer, wallCell = walls[k * walls.Count / archers] });

            foreach (var post in _posts) Raise(post, kingdom, config);
        }

        private void Raise(Post post, BotKingdom kingdom, BalanceConfig config)
        {
            var army = ArmyManager.Instance;
            var from = kingdom.Hall != null ? kingdom.Hall.transform.position : kingdom.WorldAt(Vector2Int.zero);
            // out of the hall's own carve, onto walkable ground beside it
            if (NavMesh.SamplePosition(from + new Vector3(0f, 0f, -3.5f), out var hit, 4f, NavMesh.AllAreas)) from = hit.position;
            var group = post.duty == Duty.King ? _kingGroup
                : post.duty == Duty.Patrol ? _patrols[post.route]
                : new ArmyGroup(post.type, from);
            var unit = army.SpawnHireling(post.type, from, group);
            if (unit == null) return;
            var stats = SoldierStats.StatsAt(post.type, config.BotGarrisonLevel);
            unit.SetHeroStats(stats.maxHealth, stats.attackDamage);
            unit.SetCurrentHealth(stats.maxHealth);
            post.unit = unit;
            post.downFor = 0f;
            if (post.duty == Duty.King) kingdom.DressKing(unit.gameObject);
            if (post.duty == Duty.Wall) post.onWall = unit.GoToWall(post.wallCell);
            else if (post.duty == Duty.Patrol) group.OrderMoveTo(kingdom.WorldAt(Avenues[post.route][_patrolLeg[post.route]]));
            else group.OrderMoveTo(kingdom.WorldAt(KingRing[_kingLeg]));
        }

        /// <summary>Each patrol on to the other end of its avenue once its first man is there; the King on round the ring.</summary>
        private void Walk(BotKingdom kingdom)
        {
            for (var r = 0; r < _patrols.Length; r++)
            {
                var group = _patrols[r];
                if (group == null || group.Count == 0) continue;
                var target = kingdom.WorldAt(Avenues[r][_patrolLeg[r]]);
                if (Flat(group.Members[0].transform.position - target) > 2.5f) continue;
                _patrolLeg[r] = 1 - _patrolLeg[r];
                group.OrderMoveTo(kingdom.WorldAt(Avenues[r][_patrolLeg[r]]));
            }
            if (_kingGroup != null && _kingGroup.Count > 0)
            {
                var target = kingdom.WorldAt(KingRing[_kingLeg]);
                if (Flat(_kingGroup.Members[0].transform.position - target) <= 2.5f)
                {
                    _kingLeg = (_kingLeg + 1) % KingRing.Length;
                    _kingGroup.OrderMoveTo(kingdom.WorldAt(KingRing[_kingLeg]));
                }
            }
        }

        private static float Flat(Vector3 v)
        {
            v.y = 0f;
            return v.magnitude;
        }
    }
}
