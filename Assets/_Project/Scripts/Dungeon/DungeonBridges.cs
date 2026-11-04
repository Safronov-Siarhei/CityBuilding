using System.Collections.Generic;
using System.Text.RegularExpressions;
using CityBuilder.Combat;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Dungeon
{
    /// <summary>
    /// Dungeon 3's crumbling bridges (the user, 2026-11-04: «8, восстанавливаются»): every bridge
    /// on a loop (`-Bridges-f<i>` stones + `-Bridges-f<i>-deck`) falls into the lava
    /// dungeon3_bridge_fall_delay seconds after the last hero has stepped off it, cutting that way
    /// (a carving NavMeshObstacle over its span), and re-forms dungeon3_bridge_reform_seconds later.
    /// The other way round is always open: only bridges with a detour crumble (dungeon3.py).
    /// </summary>
    public class DungeonBridges : MonoBehaviour
    {
        private const float CheckSeconds = 0.2f;
        private const float FallSeconds = 1.2f;
        private const float FallDepth = 2.5f;
        private const float RiseSeconds = 1.5f;

        private class Span
        {
            public readonly List<Transform> Parts = new List<Transform>();
            public readonly List<Vector3> Homes = new List<Vector3>();
            public readonly List<Collider> Decks = new List<Collider>();
            public Bounds Area;
            public bool Crossed;        // a hero has been on it since it last stood
            public float Clock;
            public int State;           // 0 standing, 1 counting down, 2 falling, 3 fallen, 4 rising
            public NavMeshObstacle Gap;
        }

        private readonly List<Span> _spans = new List<Span>();
        private float _check;

        /// <summary>How many bridges are down now (the tests).</summary>
        public int Fallen { get; private set; }

        public void Begin(GameObject bridges)
        {
            if (bridges == null) return;
            var byIndex = new Dictionary<int, Span>();
            var pattern = new Regex(@"-f(\d+)(-deck)?$");
            foreach (var renderer in bridges.GetComponentsInChildren<Renderer>(true))
            {
                var match = pattern.Match(renderer.name);
                if (!match.Success) continue;
                var index = int.Parse(match.Groups[1].Value);
                if (!byIndex.TryGetValue(index, out var span))
                {
                    span = new Span { Area = renderer.bounds };
                    byIndex[index] = span;
                }
                span.Parts.Add(renderer.transform);
                span.Homes.Add(renderer.transform.position);
                span.Area.Encapsulate(renderer.bounds);
                var deck = renderer.GetComponent<Collider>();
                if (deck != null) span.Decks.Add(deck);
            }
            foreach (var span in byIndex.Values)
            {
                // the gap it leaves: carved out of the deck and the lava under it, switched on when it falls
                var holder = new GameObject("BridgeGap");
                holder.transform.SetParent(transform, false);
                holder.transform.position = span.Area.center;
                var gap = holder.AddComponent<NavMeshObstacle>();
                gap.shape = NavMeshObstacleShape.Box;
                gap.size = span.Area.size + new Vector3(0.2f, 2f, 0.2f);
                gap.carving = true;
                gap.carveOnlyStationary = false;
                gap.enabled = false;
                span.Gap = gap;
                _spans.Add(span);
            }
        }

        private void Update()
        {
            if (_spans.Count == 0) return;
            var dt = Time.deltaTime;
            _check -= dt;
            var look = _check <= 0f;
            if (look) _check = CheckSeconds;
            var c = BalanceConfig.Instance;
            foreach (var span in _spans)
            {
                span.Clock -= dt;
                switch (span.State)
                {
                    case 0:
                    case 1:
                        if (!look) break;
                        if (HeroOn(span))
                        {
                            span.Crossed = true;
                            span.State = 0;
                        }
                        else if (span.Crossed && span.State == 0)
                        {
                            span.State = 1;
                            span.Clock = c.BridgeFallDelay;
                        }
                        else if (span.State == 1 && span.Clock <= 0f) Fall(span);
                        break;
                    case 2:
                        Sink(span, 1f - Mathf.Clamp01(span.Clock / FallSeconds));
                        if (span.Clock > 0f) break;
                        foreach (var part in span.Parts) part.gameObject.SetActive(false);
                        span.State = 3;
                        span.Clock = c.BridgeReformSeconds;
                        break;
                    case 3:
                        if (span.Clock > 0f) break;
                        foreach (var part in span.Parts) part.gameObject.SetActive(true);
                        span.State = 4;
                        span.Clock = RiseSeconds;
                        break;
                    default:
                        Sink(span, Mathf.Clamp01(span.Clock / RiseSeconds));
                        if (span.Clock > 0f) break;
                        Sink(span, 0f);
                        foreach (var deck in span.Decks) deck.enabled = true;
                        span.Gap.enabled = false;
                        span.State = 0;
                        span.Crossed = false;
                        Fallen--;
                        break;
                }
            }
        }

        private static bool HeroOn(Span span)
        {
            var area = span.Area;
            area.Expand(new Vector3(0.3f, 2f, 0.3f));
            foreach (var soldier in SoldierUnit.All)
                if (soldier != null && soldier.CurrentHealth > 0 && area.Contains(soldier.transform.position)) return true;
            return false;
        }

        private void Fall(Span span)
        {
            span.State = 2;
            span.Clock = FallSeconds;
            foreach (var deck in span.Decks) deck.enabled = false;
            span.Gap.enabled = true;
            Fallen++;
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_dungeon_bridge_falls"), EventSeverity.Warning,
                EventGlyph.Skull, null, span.Area.center));
        }

        /// <summary>The stones `down` of the way into the lava, shaking as they go.</summary>
        private static void Sink(Span span, float down)
        {
            var shake = down > 0f && down < 1f ? Mathf.Sin(Time.time * 40f) * 0.04f : 0f;
            for (var i = 0; i < span.Parts.Count; i++)
                span.Parts[i].position = span.Homes[i] + new Vector3(shake, -FallDepth * down * down, 0f);
        }

        /// <summary>Drops every bridge at once -- for the tests.</summary>
        public void DropAllNow()
        {
            foreach (var span in _spans)
                if (span.State <= 1) Fall(span);
        }
    }
}
