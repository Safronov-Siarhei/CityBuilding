using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Invasion
{
    /// <summary>
    /// One of the Invasion run's three round orc camps at the far end of a lane (InvasionCampLayout,
    /// the user's description, 2026-10-21). The user's rules for them: a camp can be broken but its
    /// portal cannot; every building and every guard is of the top level; the player should hardly
    /// ever have time to attack one -- storming it means leaving the base to the waves.
    ///
    /// - The buildings are the walled camps' own (OrcCampBuilding at inv_camp_building_level):
    ///   towers shoot, walls block, each leaves its ruin. The portal is only a look.
    /// - The garrison: inv_camp_raiders orcs and inv_camp_throwers throwers at
    ///   inv_camp_guard_level, each holding a post and fighting soldiers within
    ///   inv_camp_guard_radius of it. While a war tent stands a fallen orc is replaced every
    ///   inv_camp_guard_respawn_sec, while a range stands a fallen thrower; with all of a kind's
    ///   spawners down, that kind is gone for good.
    /// - The waves step out in front of the portal (WaveExit) and march out through both gates.
    /// </summary>
    public class InvasionOrcCamp : MonoBehaviour
    {
        private class Guard
        {
            public OrcKind Kind;
            public Vector3 Post;
            public OrcUnit Unit;
        }

        public Vector3 Centre { get; private set; }

        /// <summary>Where this camp's waves step out: in front of the portal, towards the base.</summary>
        public Vector3 WaveExit { get; private set; }

        /// <summary>The camp's buildings still standing.</summary>
        public IReadOnlyList<OrcCampBuilding> Buildings => _buildings;

        public int GuardsStanding
        {
            get
            {
                var n = 0;
                foreach (var g in _guards) if (g.Unit != null && g.Unit.CurrentHealth > 0) n++;
                return n;
            }
        }

        /// <summary>The garrison standing now.</summary>
        public IEnumerable<OrcUnit> GuardUnits
        {
            get
            {
                foreach (var g in _guards) if (g.Unit != null && g.Unit.CurrentHealth > 0) yield return g.Unit;
            }
        }

        /// <summary>Guards of a kind standing now.</summary>
        public int GuardsOf(OrcKind kind)
        {
            var n = 0;
            foreach (var g in _guards) if (g.Kind == kind && g.Unit != null && g.Unit.CurrentHealth > 0) n++;
            return n;
        }

        private readonly List<OrcCampBuilding> _buildings = new List<OrcCampBuilding>();
        private readonly List<Guard> _guards = new List<Guard>();
        private float _refill;

        private void OnEnable() => OrcCampBuilding.OnDestroyed += OnBuildingFell;

        private void OnDisable() => OrcCampBuilding.OnDestroyed -= OnBuildingFell;

        /// <summary>Puts the whole camp up round `centre`, its gates facing `facing` (the base), and musters its garrison.</summary>
        public void Build(Vector3 centre, Vector3 facing)
        {
            var raids = OrcRaidManager.Instance;
            if (raids == null) return;
            var config = BalanceConfig.Instance;
            Centre = centre;
            transform.position = centre;
            var front = InvasionCampLayout.YawTowards(centre, facing);
            var frontDir = Quaternion.Euler(0f, front, 0f) * Vector3.forward;

            foreach (var piece in InvasionCampLayout.Pieces())
            {
                InvasionCampLayout.ToWorld(piece, centre, front, out var at, out var yaw);
                at.y = Ground(at).y;
                if (piece.Kind == InvasionCampLayout.PieceKind.Gate)
                {
                    raids.SpawnRingGate(at, yaw).transform.SetParent(transform, true);
                    continue;
                }
                var building = raids.SpawnLooseCampBuilding(piece.Building, config.InvasionCampBuildingLevel, at, yaw, piece.Size);
                building.transform.SetParent(transform, true);
                _buildings.Add(building);
            }
            raids.SpawnPortalLook(Ground(centre), front).transform.SetParent(transform, true);

            WaveExit = Ground(centre + frontDir * InvasionCampLayout.WaveExitMeters);

            // the garrison's posts: the orcs round the yard, the throwers inside the inner wall
            for (var i = 0; i < config.InvasionCampRaiders; i++)
            {
                var angle = 30f + i * 300f / Mathf.Max(1, config.InvasionCampRaiders);
                _guards.Add(new Guard { Kind = OrcKind.Raider, Post = PostAt(centre, front, InvasionCampLayout.YardRadius - 1.5f, angle) });
            }
            for (var i = 0; i < config.InvasionCampThrowers; i++)
            {
                var angle = i * 360f / Mathf.Max(1, config.InvasionCampThrowers);
                _guards.Add(new Guard { Kind = OrcKind.Thrower, Post = PostAt(centre, front, 7f, angle) });
            }
            foreach (var guard in _guards) Muster(guard);
            _refill = config.InvasionCampGuardRespawnSeconds;
        }

        private void Update() => Tick(Time.deltaTime);

        /// <summary>The garrison's refill clock. Public so the tests can run it on.</summary>
        public void Tick(float dt)
        {
            _refill -= dt;
            if (_refill > 0f) return;
            _refill = BalanceConfig.Instance.InvasionCampGuardRespawnSeconds;
            // one guard a tick, the first fallen one whose spawner still stands
            foreach (var guard in _guards)
            {
                if (guard.Unit != null && guard.Unit.CurrentHealth > 0) continue;
                if (!HasSpawnerFor(guard.Kind)) continue;
                Muster(guard);
                return;
            }
        }

        private void Muster(Guard guard)
        {
            var config = BalanceConfig.Instance;
            guard.Unit = OrcRaidManager.Instance.SpawnRingGuard(guard.Post, config.InvasionCampGuardLevel, guard.Kind, config.InvasionCampGuardRadius);
        }

        private bool HasSpawnerFor(OrcKind kind)
        {
            var spawner = kind == OrcKind.Thrower ? OrcCampBuildingKind.ThrowerRange : OrcCampBuildingKind.WarTent;
            foreach (var building in _buildings)
            {
                if (building != null && building.Kind == spawner && building.CurrentHealth > 0) return true;
            }
            return false;
        }

        private void OnBuildingFell(OrcCampBuilding building) => _buildings.Remove(building);

        private static Vector3 PostAt(Vector3 centre, float front, float radius, float angle)
        {
            var rad = (front + angle) * Mathf.Deg2Rad;
            return Ground(centre + new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * radius);
        }

        private static Vector3 Ground(Vector3 at)
        {
            return NavMesh.SamplePosition(at, out var hit, 3f, NavMesh.AllAreas) ? hit.position : at;
        }
    }
}
