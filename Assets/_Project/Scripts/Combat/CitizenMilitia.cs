using System.Collections.Generic;
using CityBuilder.Citizens;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// The Captain's Боевой дух (the user, 2026-10-23): for a while every citizen without a job
    /// takes up a pitchfork -- runs at the nearest enemy within reach and strikes it, hard enough
    /// to hold an orc off (skill power per blow, the Captain's Сила on top). Workers stay at their
    /// work. When it is over they go back to what they were doing. The orcs' blows still hurt them.
    /// </summary>
    public class CitizenMilitia : MonoBehaviour
    {
        private const float TickSeconds = 0.3f;
        private const float StrikeReach = 1.1f;
        private const float StrikeSeconds = 1f;

        private static CitizenMilitia _instance;

        private readonly List<CitizenAgent> _fighters = new List<CitizenAgent>();
        private readonly Dictionary<CitizenAgent, float> _nextBlow = new Dictionary<CitizenAgent, float>();
        private float _until, _tick, _reach;
        private int _damage;

        /// <summary>The people fighting now (for the tests and the HUD).</summary>
        public static int Fighting => _instance != null && _instance.enabled ? _instance._fighters.Count : 0;

        /// <summary>Every jobless citizen fights for `seconds`, `damage` a blow, at enemies within `reach` metres of him.</summary>
        public static int Raise(float seconds, int damage, float reach)
        {
            if (_instance == null) _instance = new GameObject("CitizenMilitia").AddComponent<CitizenMilitia>();
            return _instance.Begin(seconds, damage, reach);
        }

        public static void Disband()
        {
            if (_instance != null) _instance.End();
        }

        private int Begin(float seconds, int damage, float reach)
        {
            enabled = true;
            _until = Time.time + seconds;
            _damage = Mathf.Max(1, damage);
            _reach = Mathf.Max(1f, reach);
            _fighters.Clear();
            var visuals = CitizenVisualsManager.Instance;
            if (visuals == null) return 0;
            foreach (var agent in visuals.AllAgents)
            {
                if (agent == null || !agent.gameObject.activeInHierarchy || visuals.WorkplaceOf(agent) != null) continue;
                _fighters.Add(agent);
            }
            return _fighters.Count;
        }

        private void End()
        {
            foreach (var agent in _fighters)
            {
                // stop where he stands: MoveTo there hands him back to his wandering on arrival
                if (agent != null && agent.gameObject.activeInHierarchy) agent.MoveTo(agent.transform.position);
            }
            _fighters.Clear();
            _nextBlow.Clear();
            enabled = false;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Update()
        {
            if (Time.time >= _until)
            {
                End();
                return;
            }
            _tick -= Time.deltaTime;
            if (_tick > 0f) return;
            _tick = TickSeconds;

            for (var i = _fighters.Count - 1; i >= 0; i--)
            {
                var agent = _fighters[i];
                if (agent == null || !agent.gameObject.activeInHierarchy)
                {
                    _fighters.RemoveAt(i);
                    continue;
                }
                var enemy = NearestEnemy(agent.transform.position);
                if (enemy == null) continue;
                var to = enemy.transform.position - agent.transform.position;
                to.y = 0f;
                if (to.magnitude > StrikeReach)
                {
                    agent.MoveTo(enemy.transform.position);
                    continue;
                }
                if (_nextBlow.TryGetValue(agent, out var next) && Time.time < next) continue;
                _nextBlow[agent] = Time.time + StrikeSeconds;
                enemy.TakeDamage(Mathf.RoundToInt(_damage * HeroAuras.MightAt(agent.transform.position)));
            }
        }

        private OrcUnit NearestEnemy(Vector3 at)
        {
            OrcUnit nearest = null;
            var best = _reach * _reach;
            foreach (var orc in OrcUnit.All)
            {
                if (orc == null || orc.CurrentHealth <= 0) continue;
                var d = orc.transform.position - at;
                d.y = 0f;
                if (d.sqrMagnitude > best) continue;
                best = d.sqrMagnitude;
                nearest = orc;
            }
            return nearest;
        }
    }
}
