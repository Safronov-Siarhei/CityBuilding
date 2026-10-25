using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Invasion
{
    /// <summary>
    /// The New Year map's own rules (the user, 2026-10-25: «новогодняя карта... со своими
    /// нюансами»; he picked gifts and the thief, the blizzard, the midnight chimes and the Ice
    /// Shaman, and no ice on the rivers). Every number is the invasion tab's inv_ny_* keys.
    ///
    /// - The look: a decorated tree before the castle, a snowman by each house, snow falling.
    /// - Gifts: after each wave a few boxes lie on the island; the King, a soldier or an islander
    ///   walking up to one takes it -- wood, stone or gold. From inv_ny_thief_from_wave a Thief comes
    ///   with the wave out of the portal nearest the gifts: he runs to one and back to his portal with
    ///   it, fights no one; killed, he drops it.
    /// - The blizzard: now and then a wave brings one, announced beforehand: the towers reach
    ///   shorter and everyone walks slower while it lasts. Never on the midnight wave.
    /// - Midnight: every inv_ny_chimes_every-th wave is bigger and brings the Ice Shaman, who freezes
    ///   the towers near him; a few seconds after it comes out a salute bursts over the island and
    ///   takes a share of every orc's health -- but not the Shaman's.
    /// </summary>
    public class InvasionNewYear : MonoBehaviour
    {
        public static InvasionNewYear Instance { get; private set; }

        private InvasionWaves _waves;
        private Vector3 _castle;
        private GameObject[] _giftModels;
        private readonly List<NewYearGift> _gifts = new List<NewYearGift>();
        private readonly List<OrcUnit> _thieves = new List<OrcUnit>();
        private SnowFall _snow;
        private BlizzardOverlay _overlay;

        private float _blizzardIn = -1f;
        private float _blizzardWarnIn = -1f;
        private float _blizzardLeft;
        private float _saluteIn = -1f;
        private OrcUnit _shaman;

        /// <summary>The gifts lying about or being carried off.</summary>
        public IReadOnlyList<NewYearGift> Gifts => _gifts;

        /// <summary>The Thieves out now.</summary>
        public IReadOnlyList<OrcUnit> Thieves => _thieves;

        public bool BlizzardOn => _blizzardLeft > 0f;

        /// <summary>A blizzard is coming (announced) or blowing.</summary>
        public bool BlizzardComing => _blizzardIn >= 0f;

        public OrcUnit Shaman => _shaman;

        private static BalanceConfig Config => BalanceConfig.Instance;

        public static bool IsMidnight(int wave, int every) => every > 0 && wave > 0 && wave % every == 0;

        public void Begin(InvasionWaves waves, Vector3 castle, IEnumerable<BuildingInstance> houses,
            GameObject tree, GameObject snowman, GameObject[] gifts)
        {
            Instance = this;
            _waves = waves;
            _castle = castle;
            _giftModels = gifts;
            Weather.Clear();

            PlaceTree(tree);
            if (snowman != null)
            {
                foreach (var house in houses)
                {
                    if (house == null) continue;
                    var at = house.transform.position + house.transform.rotation * new Vector3(1.3f, 0f, -1.3f);
                    var figure = Instantiate(snowman, at, Quaternion.Euler(0f, Random.Range(150f, 210f), 0f), transform);
                    figure.name = "Snowman";
                    SceneryShadows.DisableCasting(figure);
                }
            }
            var camera = Camera.main;
            if (camera != null) _snow = camera.gameObject.AddComponent<SnowFall>();
            _overlay = BlizzardOverlay.Create(transform);

            if (_waves != null)
            {
                _waves.WaveSent += OnWaveSent;
                _waves.WaveEnded += OnWaveEnded;
                _waves.WaveSizeMultiplier = w => IsMidnight(w, Config.NyChimesEvery) ? Config.NyChimesOrcsMult : 1f;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_waves != null)
            {
                _waves.WaveSent -= OnWaveSent;
                _waves.WaveEnded -= OnWaveEnded;
            }
            Weather.Clear();
            if (_snow != null) Destroy(_snow);
        }

        // ------------------------------------------------------------------------------ the look

        /// <summary>The decorated tree on 2x2 open ground before the castle, the cells taken so nothing is built over it.</summary>
        private void PlaceTree(GameObject tree)
        {
            var grid = GridManager.Instance;
            if (tree == null || grid == null) return;
            var size = new Vector2Int(2, 2);
            var origin = grid.WorldToCell(_castle + new Vector3(0f, 0f, -7.5f)) - Vector2Int.one;
            for (var ring = 0; ring <= 6; ring++)
            {
                for (var dx = -ring; dx <= ring; dx++)
                for (var dz = -ring; dz <= ring; dz++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != ring) continue;
                    var cell = origin + new Vector2Int(dx, dz);
                    if (!grid.IsWithinBounds(cell, size) || !grid.IsAreaFree(cell, size)) continue;
                    var at = grid.GetFootprintCenterWorld(cell, size);
                    if (!NavMesh.SamplePosition(at, out _, 0.5f, NavMesh.AllAreas)) continue;
                    grid.SetAreaOccupied(cell, size, true);
                    var figure = Instantiate(tree, at, tree.transform.rotation, transform);
                    figure.name = "NewYearTree";
                    // green and dressed, not buried: winter's snow stays off it
                    Maps.SeasonStyle.NoSnow(figure);
                    var obstacle = figure.AddComponent<NavMeshObstacle>();
                    obstacle.shape = NavMeshObstacleShape.Capsule;
                    obstacle.radius = 0.9f;
                    obstacle.height = 2.5f;
                    obstacle.carving = true;
                    Tree = figure;
                    return;
                }
            }
        }

        /// <summary>The decorated tree, once it stands (for the tests and the photographs).</summary>
        public GameObject Tree { get; private set; }

        // ------------------------------------------------------------------------------ the waves

        private void OnWaveSent(int wave)
        {
            var config = Config;
            if (IsMidnight(wave, config.NyChimesEvery))
            {
                EventLogManager.Instance?.Post(GameEvent.Nowhere(Localization.Get("#log_ny_midnight"), EventSeverity.Danger, EventGlyph.Star, null));
                SendShaman(wave);
                _saluteIn = config.NySaluteDelaySeconds;
                return;   // never a blizzard at midnight
            }
            if (wave >= config.NyThiefFromWave) SendThieves(config.NyThievesPerWave);
            if (_blizzardIn < 0f && _blizzardLeft <= 0f && Random.value < config.NyBlizzardChance)
            {
                _blizzardIn = config.NyBlizzardWarningSeconds + Random.Range(0f, 10f);
                _blizzardWarnIn = _blizzardIn - config.NyBlizzardWarningSeconds;
            }
        }

        private void OnWaveEnded(int wave) => LayGifts(Config.NyGiftsPerWave);

        private void Update()
        {
            var dt = Time.deltaTime;
            _gifts.RemoveAll(g => g == null);
            _thieves.RemoveAll(t => t == null);

            if (_blizzardWarnIn >= 0f)
            {
                _blizzardWarnIn -= dt;
                if (_blizzardWarnIn < 0f)
                    EventLogManager.Instance?.Post(GameEvent.Nowhere(Localization.Format("#log_ny_blizzard_soon", Mathf.RoundToInt(Config.NyBlizzardWarningSeconds)),
                        EventSeverity.Warning, EventGlyph.Alert, null));
            }
            if (_blizzardIn >= 0f)
            {
                _blizzardIn -= dt;
                if (_blizzardIn < 0f) StartBlizzard(Config.NyBlizzardSeconds);
            }
            if (_blizzardLeft > 0f)
            {
                _blizzardLeft -= dt;
                if (_blizzardLeft <= 0f) EndBlizzard();
            }
            if (_saluteIn >= 0f)
            {
                _saluteIn -= dt;
                if (_saluteIn < 0f) Salute();
            }
        }

        // ------------------------------------------------------------------------------ gifts

        /// <summary>`count` gifts on open ground between inv_ny_gift_min_radius and _max_radius of the castle, no more than inv_ny_gifts_max lying about. Public for the tests.</summary>
        public void LayGifts(int count)
        {
            var config = Config;
            for (var i = 0; i < count && _gifts.Count < config.NyGiftsMax; i++)
            {
                for (var attempt = 0; attempt < 12; attempt++)
                {
                    var angle = Random.Range(0f, Mathf.PI * 2f);
                    var r = Random.Range(config.NyGiftMinRadius, config.NyGiftMaxRadius);
                    var at = _castle + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
                    if (!NavMesh.SamplePosition(at, out var hit, 1.5f, NavMesh.AllAreas)) continue;
                    LayGift(hit.position, (ResourceType)new[] { (int)ResourceType.Wood, (int)ResourceType.Stone, (int)InvasionRules.Money }[Random.Range(0, 3)]);
                    break;
                }
            }
        }

        /// <summary>One gift of `kind` at `at`. Public for the tests.</summary>
        public NewYearGift LayGift(Vector3 at, ResourceType kind)
        {
            var root = new GameObject("Gift");
            root.transform.SetParent(transform, false);
            root.transform.position = at;
            var models = _giftModels;
            var model = models != null && models.Length > 0 ? models[Random.Range(0, models.Length)] : null;
            if (model != null)
            {
                var figure = Instantiate(model, root.transform, false);
                figure.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * model.transform.rotation;
                SceneryShadows.DisableCasting(figure);
            }
            var gift = root.AddComponent<NewYearGift>();
            gift.Kind = kind;
            _gifts.Add(gift);
            return gift;
        }

        /// <summary>What a gift of this kind holds: wood, stone or the run's gold, from the sheet.</summary>
        public static int AmountOf(ResourceType kind)
        {
            var config = Config;
            if (kind == ResourceType.Wood) return config.NyGiftWood;
            if (kind == ResourceType.Stone) return config.NyGiftStone;
            return config.NyGiftGold;
        }

        /// <summary>Someone of the island took the gift: its contents into the stock, a popup over it.</summary>
        public void Collect(NewYearGift gift)
        {
            if (gift == null) return;
            var amount = AmountOf(gift.Kind);
            ResourceManager.Instance?.Add(gift.Kind, amount);
            UI.ResourceGainPopups.Instance?.Show(gift.transform.position + Vector3.up * 0.6f, amount, gift.Kind);
            _gifts.Remove(gift);
            Destroy(gift.gameObject);
        }

        /// <summary>A Thief got home with it: gone.</summary>
        public void Lose(NewYearGift gift)
        {
            if (gift == null) return;
            EventLogManager.Instance?.Post(GameEvent.Nowhere(Localization.Get("#log_ny_gift_stolen"), EventSeverity.Warning, EventGlyph.Sad, null));
            _gifts.Remove(gift);
            Destroy(gift.gameObject);
        }

        // ------------------------------------------------------------------------------ the thief

        /// <summary>`count` Thieves out of a portal at random, while gifts lie about. Public for the tests.</summary>
        public void SendThieves(int count)
        {
            var raids = OrcRaidManager.Instance;
            if (raids == null || _waves == null || _waves.LaneStarts.Count == 0) return;
            if (!AnyGiftFree()) return;
            for (var i = 0; i < count; i++)
            {
                // out of the portal nearest a gift lying about
                var home = NearestPortalTo(NearestFreeGift(_castle));
                var thief = raids.SpawnOrcOfKind(home, 1, OrcKind.Thief);
                if (thief == null) continue;
                thief.gameObject.AddComponent<GiftThief>().Begin(this, home);
                _thieves.Add(thief);
            }
            EventLogManager.Instance?.Post(GameEvent.Nowhere(Localization.Get("#log_ny_thief"), EventSeverity.Warning, EventGlyph.Alert, null));
        }

        private Vector3 NearestPortalTo(NewYearGift gift)
        {
            var starts = _waves.LaneStarts;
            var best = starts[0];
            if (gift == null) return best;
            foreach (var start in starts)
            {
                if ((start - gift.transform.position).sqrMagnitude < (best - gift.transform.position).sqrMagnitude) best = start;
            }
            return best;
        }

        private bool AnyGiftFree()
        {
            foreach (var gift in _gifts)
            {
                if (gift != null && gift.Carrier == null) return true;
            }
            return false;
        }

        /// <summary>The free gift nearest `from`, or null.</summary>
        public NewYearGift NearestFreeGift(Vector3 from)
        {
            NewYearGift best = null;
            var bestSq = float.MaxValue;
            foreach (var gift in _gifts)
            {
                if (gift == null || gift.Carrier != null) continue;
                var d = (gift.transform.position - from).sqrMagnitude;
                if (d >= bestSq) continue;
                bestSq = d;
                best = gift;
            }
            return best;
        }

        // ------------------------------------------------------------------------------ the blizzard

        /// <summary>Starts a blizzard of `seconds` now. Public for the tests.</summary>
        public void StartBlizzard(float seconds)
        {
            _blizzardIn = -1f;
            _blizzardWarnIn = -1f;
            _blizzardLeft = seconds;
            Weather.SpeedMultiplier = Config.NyBlizzardSpeedMult;
            Weather.TowerRangeMultiplier = Config.NyBlizzardRangeMult;
            if (_snow != null) _snow.Blizzard = true;
            if (_overlay != null) _overlay.Show(true);
            EventLogManager.Instance?.Post(GameEvent.Nowhere(Localization.Get("#log_ny_blizzard"), EventSeverity.Danger, EventGlyph.Alert, null));
        }

        private void EndBlizzard()
        {
            _blizzardLeft = 0f;
            Weather.Clear();
            if (_snow != null) _snow.Blizzard = false;
            if (_overlay != null) _overlay.Show(false);
            EventLogManager.Instance?.Post(GameEvent.Nowhere(Localization.Get("#log_ny_blizzard_over"), EventSeverity.Good, EventGlyph.Check, null));
        }

        // ------------------------------------------------------------------------------ midnight

        private void SendShaman(int wave)
        {
            var raids = OrcRaidManager.Instance;
            if (raids == null || _waves == null || _waves.LaneStarts.Count == 0) return;
            var lane = _waves.LastLanes.Count > 0 ? _waves.LastLanes[0] : 0;
            var level = InvasionWaves.OrcLevel(wave, Config.InvasionWaveLevelEvery, Config.InvasionWaveMaxOrcLevel);
            _shaman = raids.SpawnOrcOfKind(_waves.LaneStarts[lane], level, OrcKind.IceShaman);
            if (_shaman == null) return;
            _shaman.Toughen(Config.InvasionOrcHealthMultiplier, Config.InvasionOrcDamageMultiplier);
            _shaman.gameObject.AddComponent<IceShamanPower>();
            _waves.AddToWave(_shaman, lane);
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_ny_shaman"), EventSeverity.Danger, EventGlyph.Skull, null,
                _shaman.transform.position, _shaman.transform));
        }

        /// <summary>
        /// The salute: bursts over the island, and every orc of the run (a wave's, the raiding
        /// party's, a Thief) loses inv_ny_salute_damage_share of its health -- the Ice Shaman and the
        /// jungle's beasts excepted. Public for the tests.
        /// </summary>
        public void Salute()
        {
            _saluteIn = -1f;
            var share = Config.NySaluteDamageShare;
            var hit = new List<OrcUnit>();
            foreach (var orc in OrcUnit.All)
            {
                if (orc == null || orc.CurrentHealth <= 0 || OrcUnit.IsMonster(orc.Kind) || orc.Kind == OrcKind.IceShaman) continue;
                hit.Add(orc);
            }
            foreach (var orc in hit) orc.TakeDamage(Mathf.Max(1, Mathf.CeilToInt(orc.MaxHealth * share)));
            Fireworks.Over(_castle, 10);
            if (_waves != null)
            {
                foreach (var start in _waves.LaneStarts) Fireworks.Over(Vector3.Lerp(_castle, start, 0.25f), 4);
            }
        }
    }
}
