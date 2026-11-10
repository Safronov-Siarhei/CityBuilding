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
        // (read off the town template by BotKingdom.Routes, the user 2026-11-05: four templates)
        private static Vector2Int[] KingRing => BotKingdom.Instance != null ? BotKingdom.Instance.KingRing : new Vector2Int[0];
        private static Vector2Int[][] Avenues => BotKingdom.Instance != null ? BotKingdom.Instance.PatrolRoutes : new Vector2Int[0][];

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

        private void Update()
        {
            var kingdom = BotKingdom.Instance;
            var army = ArmyManager.Instance;
            var config = BalanceConfig.Instance;
            if (kingdom == null || !kingdom.Placed || army == null || config == null) return;
            if (_standDown) return;
            // the garrison mans, walks and refills only while the player can see the castle
            if (kingdom.Hall != null && !Maps.FogCulling.Seen(kingdom.Hall.transform.position, BotKingdom.SeenRadiusCells)) return;
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

        private static readonly HashSet<SoldierUnit> Mine = new HashSet<SoldierUnit>();

        /// <summary>A soldier of the bot's garrison (not the player's, for all he is a SoldierUnit): the unit card's owner.</summary>
        public static bool Owns(SoldierUnit unit) => unit != null && Mine.Contains(unit);

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
            Mine.RemoveWhere(u => u == null);
            Mine.Add(unit);
            var stats = SoldierStats.StatsAt(post.type, config.BotGarrisonLevel);
            unit.SetHeroStats(stats.maxHealth, stats.attackDamage);
            unit.SetCurrentHealth(stats.maxHealth);
            post.unit = unit;
            post.downFor = 0f;
            // not drawn under the fog (the user, 2026-11-10)
            Maps.FogCulling.Cull(unit.gameObject, 1);
            if (post.duty == Duty.King) kingdom.DressKing(unit.gameObject);
            if (post.duty == Duty.Wall) post.onWall = unit.GoToWall(post.wallCell);
            else if (post.duty == Duty.Patrol) group.OrderMoveTo(kingdom.WorldAt(Avenues[post.route][_patrolLeg[post.route]]));
            else if (KingRing.Length > 0) group.OrderMoveTo(kingdom.WorldAt(KingRing[_kingLeg % KingRing.Length]));
        }

        /// <summary>Each patrol on round its loop once its first man reaches the next point; the King on round his.</summary>
        private void Walk(BotKingdom kingdom)
        {
            for (var r = 0; r < _patrols.Length; r++)
            {
                var group = _patrols[r];
                if (group == null || group.Count == 0) continue;
                var target = kingdom.WorldAt(Avenues[r][_patrolLeg[r]]);
                if (Flat(group.Members[0].transform.position - target) > 2.5f) continue;
                _patrolLeg[r] = (_patrolLeg[r] + 1) % Avenues[r].Length;
                group.OrderMoveTo(kingdom.WorldAt(Avenues[r][_patrolLeg[r]]));
            }
            if (_kingGroup != null && _kingGroup.Count > 0 && KingRing.Length > 0)
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
