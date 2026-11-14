using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// A wandering resident now and then steps into a building (the user, 2026-11-14; sandbox only):
    /// every visit_every_sec an idle one walks to the door of a burnable building within
    /// visit_reach_cells, goes in (hidden) for visit_min_sec..visit_max_sec and comes out again. If the
    /// building catches fire while he is in (FireManager.Ignite), he dies: his body is laid on the
    /// nearest open ground (FireRules.OpenGroundNear -- a road or a square counts, never the burning
    /// house or its neighbour, so the player sees it) and joins the dead (Remains).
    /// At most visit_max are inside at once. A visitor the town needs (taken for a job, a patient)
    /// is let out at once.
    /// </summary>
    public class ResidentVisits : MonoBehaviour
    {
        public static ResidentVisits Instance { get; private set; }

        private const float WalkTimeout = 15f;

        private class Visit
        {
            public CitizenAgent Agent;
            public BuildingInstance Building;
            public Vector3 Door;
            public bool Inside;
            public float Left;
        }

        private readonly List<Visit> _visits = new List<Visit>();
        private float _next;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            var visuals = CitizenVisualsManager.Instance;
            if (!FireRules.Active || visuals == null)
            {
                if (_visits.Count > 0) LetAllOut();
                return;
            }
            for (var i = _visits.Count - 1; i >= 0; i--)
                if (!Step(_visits[i], visuals)) End(i);

            _next -= Time.deltaTime;
            if (_next > 0f) return;
            var config = BalanceConfig.Instance;
            _next = Mathf.Max(1f, config.VisitEverySeconds);
            if (_visits.Count >= config.VisitMax) return;
            Begin(visuals, config);
        }

        private void Begin(CitizenVisualsManager visuals, BalanceConfig config)
        {
            var agent = visuals.RandomIdle();
            if (agent == null || !agent.gameObject.activeSelf) return;
            foreach (var v in _visits) if (v.Agent == agent) return;
            var grid = Grid.GridManager.Instance;
            var reach = config.VisitReachCells * (grid != null ? grid.CellSize : 1f);
            BuildingInstance best = null;
            var bestD = reach * reach;
            foreach (var b in BuildingInstance.All)
            {
                if (!FireRules.Burns(b) || b.GetComponent<BuildingFire>() != null || b.Data.IsGroundWork) continue;
                var d = (b.transform.position - agent.transform.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = b; }
            }
            if (best == null) return;
            var visit = new Visit { Agent = agent, Building = best, Door = visuals.DoorOf(best), Left = WalkTimeout };
            _visits.Add(visit);
            agent.MoveTo(visit.Door);
        }

        /// <summary>One visit's frame; false when it is over.</summary>
        private bool Step(Visit v, CitizenVisualsManager visuals)
        {
            if (v.Agent == null) return false;
            if (v.Building == null || v.Building.CurrentHealth <= 0) return false;
            // the town took him for something (a job, a bed): out he comes
            if (!visuals.IsIdle(v.Agent)) return false;
            v.Left -= Time.deltaTime;
            if (!v.Inside)
            {
                var to = v.Door - v.Agent.transform.position;
                to.y = 0f;
                if (to.magnitude > 0.6f && v.Agent.ManualMoving)
                    return v.Left > 0f;
                v.Inside = true;
                var config = BalanceConfig.Instance;
                v.Left = Random.Range(config.VisitMinSeconds, Mathf.Max(config.VisitMinSeconds, config.VisitMaxSeconds));
                v.Agent.gameObject.SetActive(false);
                return true;
            }
            return v.Left > 0f;
        }

        private void End(int index)
        {
            var v = _visits[index];
            _visits.RemoveAt(index);
            if (v.Agent == null) return;
            if (v.Inside)
            {
                v.Agent.gameObject.SetActive(true);
                v.Agent.Teleport(v.Door);
            }
        }

        private void LetAllOut()
        {
            for (var i = _visits.Count - 1; i >= 0; i--) End(i);
        }

        /// <summary>This building caught fire: whoever is inside dies there, laid out on open ground.</summary>
        public void Burned(BuildingInstance building)
        {
            var visuals = CitizenVisualsManager.Instance;
            for (var i = _visits.Count - 1; i >= 0; i--)
            {
                var v = _visits[i];
                if (v.Building != building || !v.Inside) continue;
                _visits.RemoveAt(i);
                if (v.Agent == null || visuals == null) continue;
                visuals.KillAgentAt(v.Agent, FireRules.OpenGroundNear(v.Door));
                EventLogManager.Instance?.Post(GameEvent.About(building, Localization.Format("#log_fire_victim",
                    building.Data != null ? building.Data.LocalizedName : string.Empty), EventSeverity.Danger, EventGlyph.Skull));
            }
        }
    }
}
