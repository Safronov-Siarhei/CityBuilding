using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// The plague (the user, 2026-11-13; sandbox only, per island, never spreads to another island):
    ///
    /// - It breaks out when illness killed plague_death_share (10%) of an island's people within
    ///   plague_window_sec (5 minutes) -- Infirmary.Die reports each death here.
    /// - While it lasts the island's houses make no one (HousePeople); the rest of its harm is the
    ///   dying itself. A green haze lies over the island and crows circle it.
    /// - Notices: «Чума! Стройте лечебницы» (the shop's offer of what a building lacks already opens
    ///   when the player places one he cannot pay for). Once illness has killed plague_banner_share
    ///   (40%) of the island since it broke out, the game pauses and PlagueBanner offers to end it for a
    ///   rewarded ad or plague_end_crystals.
    /// - It ends by itself after plague_calm_sec with no illness death on the island (the open
    ///   question's proposal, 2026-11-13), or by the banner. Ended by the banner (ad or crystals), the
    ///   player is shielded for plague_shield_sec (the user, 2026-11-13: 7 minutes): no plague breaks
    ///   out on any island meanwhile.
    /// </summary>
    public class Plague : MonoBehaviour
    {
        public static Plague Instance { get; private set; }

        [SerializeField] private Material hazeMaterial;

        public class Outbreak
        {
            public int Island;
            /// <summary>The island's residents (and its dead) when it broke out -- what the banner's share is of.</summary>
            public int People;
            public int Deaths;
            /// <summary>Seconds since the last illness death on the island (the calm that ends it).</summary>
            public float Calm;
            public bool BannerShown;
            public GameObject Look;
        }

        private readonly Dictionary<int, List<float>> _deaths = new Dictionary<int, List<float>>();
        private readonly Dictionary<int, Outbreak> _on = new Dictionary<int, Outbreak>();
        private readonly List<int> _ending = new List<int>();
        private static Mesh _hazeQuad;

        /// <summary>Seconds of the shield the banner gave (game time); no outbreak while it lasts.</summary>
        public float ShieldLeft { get; private set; }

        public static bool Active => Infirmary.Active;

        /// <summary>The plague is on this island now.</summary>
        public static bool On(int island) => Active && Instance != null && Instance._on.ContainsKey(island);

        public IEnumerable<Outbreak> Outbreaks => _on.Values;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Illness killed somebody on this island (Infirmary.Die).</summary>
        public void RecordIllnessDeath(int island)
        {
            if (!Active) return;
            var config = BalanceConfig.Instance;
            if (_on.TryGetValue(island, out var outbreak))
            {
                outbreak.Deaths++;
                outbreak.Calm = 0f;
                if (!outbreak.BannerShown && outbreak.Deaths >= config.PlagueBannerShare * Mathf.Max(1, outbreak.People))
                {
                    outbreak.BannerShown = true;
                    UI.PlagueBanner.Instance?.Show(island);
                }
                return;
            }
            if (ShieldLeft > 0f) return;   // the banner's shield: these deaths start nothing
            if (!_deaths.TryGetValue(island, out var times)) _deaths[island] = times = new List<float>();
            var now = Time.time;
            times.Add(now);
            times.RemoveAll(t => now - t > config.PlagueWindowSeconds);
            var residents = HousePeople.Instance != null ? HousePeople.Instance.CitizensOn(island) : 0;
            // of the people the island had over the window: the living and these dead
            var people = residents + times.Count;
            if (times.Count >= config.PlagueDeathShare * Mathf.Max(1, people)) BreakOut(island, residents);
        }

        private void BreakOut(int island, int residents)
        {
            var outbreak = new Outbreak { Island = island, People = Mathf.Max(1, residents) };
            _on[island] = outbreak;
            _deaths.Remove(island);
            DressUp(outbreak);
            var at = IslandCentre(island, out _);
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_plague_start"), EventSeverity.Danger, EventGlyph.Skull,
                Infirmary.HealerId, at));
        }

        /// <summary>The plague on this island is over (the calm, or the banner).</summary>
        public void End(int island)
        {
            if (!_on.TryGetValue(island, out var outbreak)) return;
            _on.Remove(island);
            if (outbreak.Look != null) Destroy(outbreak.Look);
            var at = IslandCentre(island, out _);
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_plague_end"), EventSeverity.Good, EventGlyph.Heal,
                Infirmary.HealerId, at));
        }

        /// <summary>The banner ended it (ad or crystals): the plague goes and the shield goes up.</summary>
        public void EndByBanner(int island)
        {
            End(island);
            ShieldLeft = Mathf.Max(ShieldLeft, BalanceConfig.Instance.PlagueShieldSeconds);
            _deaths.Clear();
            var minutes = Mathf.RoundToInt(ShieldLeft / 60f);
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Format("#log_plague_shield", minutes), EventSeverity.Good, EventGlyph.Heal,
                Infirmary.HealerId, IslandCentre(island, out _)));
        }

        private void Update()
        {
            if (ShieldLeft > 0f) ShieldLeft = Mathf.Max(0f, ShieldLeft - Time.deltaTime);
            if (_on.Count == 0) return;
            if (!Active)
            {
                foreach (var o in _on.Values) if (o.Look != null) Destroy(o.Look);
                _on.Clear();
                return;
            }
            var calm = BalanceConfig.Instance.PlagueCalmSeconds;
            _ending.Clear();
            foreach (var o in _on.Values)
            {
                o.Calm += Time.deltaTime;
                if (o.Calm >= calm) _ending.Add(o.Island);
            }
            foreach (var island in _ending) End(island);
        }

        // ---------------------------------------------------------------- the look

        /// <summary>The middle of the island's houses and how far they spread.</summary>
        private static Vector3 IslandCentre(int island, out float radius)
        {
            var sum = Vector3.zero;
            var n = 0;
            foreach (var b in BuildingInstance.All)
            {
                if (b == null || b.IsBot || !HousePeople.Same(HousePeople.IslandOf(b), island)) continue;
                sum += b.transform.position;
                n++;
            }
            var centre = n > 0 ? sum / n : Vector3.zero;
            radius = 6f;
            foreach (var b in BuildingInstance.All)
            {
                if (b == null || b.IsBot || !HousePeople.Same(HousePeople.IslandOf(b), island)) continue;
                radius = Mathf.Max(radius, Vector3.Distance(b.transform.position, centre));
            }
            return centre;
        }

        /// <summary>A green haze over the island's buildings and crows circling them.</summary>
        private void DressUp(Outbreak outbreak)
        {
            var centre = IslandCentre(outbreak.Island, out var radius);
            var root = new GameObject($"Plague-{outbreak.Island}");
            root.transform.SetParent(transform, false);
            outbreak.Look = root;
            var random = new System.Random(outbreak.Island * 7919 + 17);
            float Range(float a, float b) => a + (float)random.NextDouble() * (b - a);
            if (hazeMaterial != null)
            {
                var placed = 0;
                foreach (var b in BuildingInstance.All)
                {
                    if (placed >= 10) break;
                    if (b == null || b.IsBot || !HousePeople.Same(HousePeople.IslandOf(b), outbreak.Island)) continue;
                    if (random.NextDouble() > 0.6) continue;
                    var patch = new GameObject("PlagueHaze", typeof(MeshFilter), typeof(MeshRenderer));
                    patch.transform.SetParent(root.transform, false);
                    var at = b.transform.position + new Vector3(Range(-3f, 3f), Range(0.3f, 0.8f), Range(-3f, 3f));
                    patch.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, Range(0f, 360f), 0f));
                    var size = Range(9f, 16f);
                    patch.transform.localScale = new Vector3(size, 1f, size * Range(0.6f, 0.9f));
                    patch.GetComponent<MeshFilter>().sharedMesh = HazeQuad();
                    var renderer = patch.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = hazeMaterial;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    placed++;
                }
            }
            var crow = Combat.OrcRaidManager.Instance != null ? Combat.OrcRaidManager.Instance.CrowModel : null;
            if (crow != null)
            {
                var flock = new GameObject("PlagueCrows").AddComponent<Combat.CrowFlock>();
                flock.transform.SetParent(root.transform, false);
                flock.Initialize(crow, centre, centre + new Vector3(radius * 0.3f, 0f, -radius * 0.2f));
            }
        }

        /// <summary>A flat square lying on the ground, 1 x 1 (the haze shader rounds its edges).</summary>
        private static Mesh HazeQuad()
        {
            if (_hazeQuad != null) return _hazeQuad;
            var h = 0.5f;
            _hazeQuad = new Mesh
            {
                name = "PlagueHazeQuad",
                vertices = new[] { new Vector3(-h, 0f, -h), new Vector3(h, 0f, -h), new Vector3(h, 0f, h), new Vector3(-h, 0f, h) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) },
                triangles = new[] { 0, 2, 1, 0, 3, 2 },
            };
            _hazeQuad.RecalculateNormals();
            return _hazeQuad;
        }

        // ---------------------------------------------------------------- save

        /// <summary>A save's outbreaks (after the buildings: the look is laid over them).</summary>
        public void RestoreFromSave(List<Outbreak> outbreaks, float shieldLeft = 0f)
        {
            ShieldLeft = Mathf.Max(0f, shieldLeft);
            foreach (var o in _on.Values) if (o.Look != null) Destroy(o.Look);
            _on.Clear();
            _deaths.Clear();
            if (outbreaks == null) return;
            foreach (var o in outbreaks)
            {
                _on[o.Island] = o;
                DressUp(o);
            }
        }
    }
}
