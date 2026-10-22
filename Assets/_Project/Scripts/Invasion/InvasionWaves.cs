using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Invasion
{
    /// <summary>
    /// The Invasion run's orc waves (the user's rules, 2026-10-21): the first comes
    /// inv_first_wave_sec after the start; a wave is over when its last orc falls, then a
    /// breather of inv_breather_sec with a countdown, then the next, stronger one. The lanes
    /// that send orcs grow with the waves -- one, then two, then all three -- and so do the
    /// orcs: more of them on each lane, a level higher every few waves, throwers and brutes among
    /// them, and every few waves a chief at the head of one lane.
    ///
    /// Gold: every orc that falls in a run pays by its kind, and a wave survived pays
    /// inv_gold_per_wave × its number.
    ///
    /// A wave whose last orcs got stuck somewhere must not stop the run: past
    /// inv_wave_overtime_sec it counts as survived and the breather begins; the stragglers walk on.
    /// </summary>
    public class InvasionWaves : MonoBehaviour
    {
        public enum Phase { NotStarted, Breather, Underway }

        public Phase Current { get; private set; } = Phase.NotStarted;

        /// <summary>The number of the last wave sent; 0 before the first.</summary>
        public int Wave { get; private set; }

        /// <summary>Waves the base has stood through -- what the run's score will count.</summary>
        public int WavesSurvived { get; private set; }

        /// <summary>The countdown to the next wave, during a breather.</summary>
        public float SecondsUntilNextWave { get; private set; }

        public int OrcsLeft => _waveOrcs.Count;

        /// <summary>The orcs of the wave underway still standing.</summary>
        public IReadOnlyList<OrcUnit> WaveOrcs => _waveOrcs;

        /// <summary>Where each lane's wave steps out, one per orc camp.</summary>
        public IReadOnlyList<Vector3> LaneStarts => _laneStarts;

        /// <summary>The lanes the last wave came down, as indices into LaneStarts.</summary>
        public IReadOnlyList<int> LastLanes => _lastLanes;

        private readonly List<Vector3> _laneStarts = new List<Vector3>();
        private readonly List<int> _lastLanes = new List<int>();
        private readonly List<OrcUnit> _waveOrcs = new List<OrcUnit>();
        private readonly Dictionary<OrcUnit, int> _laneOf = new Dictionary<OrcUnit, int>();
        private float _waveSeconds;

        private void OnEnable() => OrcUnit.Died += OnOrcDied;

        private void OnDisable() => OrcUnit.Died -= OnOrcDied;

        /// <summary>Starts the clock: each lane's wave steps out at its round camp's portal (InvasionOrcCamp.WaveExit) and marches out through the gates.</summary>
        public void Begin(IEnumerable<Vector3> exits)
        {
            _laneStarts.Clear();
            foreach (var exit in exits)
            {
                var start = exit;
                if (NavMesh.SamplePosition(start, out var hit, 6f, NavMesh.AllAreas)) start = hit.position;
                else Debug.LogError($"InvasionWaves: no ground to walk on at the camp exit {exit} -- its lane's orcs may not move.");
                _laneStarts.Add(start);
            }
            if (_laneStarts.Count == 0) Debug.LogError("InvasionWaves: the map has no camps -- no waves will come.");

            SecondsUntilNextWave = BalanceConfig.Instance.InvasionFirstWaveSeconds;
            Current = Phase.Breather;
        }

        private void Update()
        {
            if (Current == Phase.NotStarted || _laneStarts.Count == 0) return;
            if (GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver) return;

            if (Current == Phase.Breather)
            {
                SecondsUntilNextWave -= Time.deltaTime;
                if (SecondsUntilNextWave <= 0f) SendNextWave();
                return;
            }

            _waveSeconds += Time.deltaTime;
            _waveOrcs.RemoveAll(orc => orc == null);   // one taken off some other way than falling
            if (_waveOrcs.Count == 0 || _waveSeconds >= BalanceConfig.Instance.InvasionWaveOvertimeSeconds) EndWave();
        }

        /// <summary>
        /// Where the wave underway is on `lane`: its orc nearest the castle (the head of the column),
        /// or false when none of the wave is on that lane any more. What the minimap's alerts mark.
        /// </summary>
        public bool TryLaneFront(int lane, Vector3 castle, out Vector3 front)
        {
            front = default;
            var best = float.MaxValue;
            foreach (var orc in _waveOrcs)
            {
                if (orc == null || !_laneOf.TryGetValue(orc, out var of) || of != lane) continue;
                var d = (orc.transform.position - castle).sqrMagnitude;
                if (d >= best) continue;
                best = d;
                front = orc.transform.position;
            }
            return best < float.MaxValue;
        }

        /// <summary>Sends the next wave now. Public for the tests, which do not wait out the clock.</summary>
        public void SendNextWave()
        {
            var raids = OrcRaidManager.Instance;
            if (raids == null || _laneStarts.Count == 0) return;

            var config = BalanceConfig.Instance;
            Wave++;
            var lanes = LanesAt(Wave, config.InvasionTwoLanesFromWave, config.InvasionThreeLanesFromWave, _laneStarts.Count);
            var perLane = OrcsPerLane(Wave, config.InvasionWaveBaseOrcs, config.InvasionWaveExtraOrcEvery, config.InvasionWaveMaxOrcs);
            var level = OrcLevel(Wave, config.InvasionWaveLevelEvery, config.InvasionWaveMaxOrcLevel);
            var boss = IsBossWave(Wave, config.InvasionBossWaveEvery);

            PickLanes(lanes);
            _waveOrcs.Clear();
            _laneOf.Clear();
            OrcUnit leader = null;
            for (var l = 0; l < _lastLanes.Count; l++)
            {
                var start = _laneStarts[_lastLanes[l]];
                for (var i = 0; i < perLane; i++)
                {
                    var kind = KindAt(i, Wave, boss && l == 0, config.InvasionThrowerFromWave, config.InvasionThrowerEvery, config.InvasionBruteFromWave, config.InvasionBruteEvery);
                    var orc = raids.SpawnOrcOfKind(start, level, kind);
                    if (orc == null) continue;
                    orc.Toughen(config.InvasionOrcHealthMultiplier);
                    _waveOrcs.Add(orc);
                    _laneOf[orc] = _lastLanes[l];
                    if (leader == null) leader = orc;
                }
            }

            _waveSeconds = 0f;
            Current = Phase.Underway;
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Format("#log_inv_wave", Wave, _waveOrcs.Count),
                EventSeverity.Danger, EventGlyph.Swords, null, _laneStarts[_lastLanes[0]], leader != null ? leader.transform : null));
        }

        private void EndWave()
        {
            _waveOrcs.Clear();
            _laneOf.Clear();
            WavesSurvived = Wave;
            var gold = GoldForWave(Wave, BalanceConfig.Instance.InvasionGoldPerWave);
            ResourceManager.Instance?.Add(InvasionRules.Money, gold);
            EventLogManager.Instance?.Post(GameEvent.Nowhere(Localization.Format("#log_inv_wave_survived", Wave, gold),
                EventSeverity.Good, EventGlyph.Check, null));
            SecondsUntilNextWave = BalanceConfig.Instance.InvasionBreatherSeconds;
            Current = Phase.Breather;
        }

        /// <summary>Every enemy that falls in a run pays by its kind: a wave's orc, a camp's, a jungle monster.</summary>
        private void OnOrcDied(OrcUnit orc)
        {
            if (orc == null) return;
            _waveOrcs.Remove(orc);
            var gold = GoldForKill(orc.Kind, orc.Level);
            ResourceManager.Instance?.Add(InvasionRules.Money, gold);
            // the gold rises over where it fell (the user, 2026-10-22)
            UI.ResourceGainPopups.Instance?.ShowCoins(orc.transform.position, gold);
        }

        /// <summary>`count` different lanes at random -- which side the next wave comes from is not something to settle once.</summary>
        private void PickLanes(int count)
        {
            _lastLanes.Clear();
            for (var i = 0; i < _laneStarts.Count; i++) _lastLanes.Add(i);
            for (var i = _lastLanes.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (_lastLanes[i], _lastLanes[j]) = (_lastLanes[j], _lastLanes[i]);
            }
            _lastLanes.RemoveRange(count, _lastLanes.Count - count);
        }

        // ---------------------------------------------------------------- the ramp, pure, its numbers passed in

        /// <summary>How many lanes send orcs in this wave: one, two from `twoFrom`, three from `threeFrom` (never more than the map has).</summary>
        public static int LanesAt(int wave, int twoFrom, int threeFrom, int lanesOnMap)
        {
            var lanes = 1;
            if (twoFrom > 0 && wave >= twoFrom) lanes = 2;
            if (threeFrom > 0 && wave >= threeFrom) lanes = 3;
            return Mathf.Clamp(lanes, 0, lanesOnMap);
        }

        /// <summary>Orcs on each lane: `baseOrcs` at wave 1, one more every `extraEvery` waves, up to `maxOrcs`.</summary>
        public static int OrcsPerLane(int wave, int baseOrcs, int extraEvery, int maxOrcs)
        {
            var extra = extraEvery > 0 ? (Mathf.Max(1, wave) - 1) / extraEvery : 0;
            return Mathf.Clamp(baseOrcs + extra, 1, Mathf.Max(1, maxOrcs));
        }

        /// <summary>The wave's orc level: 1, a level higher every `levelEvery` waves, up to `maxLevel`.</summary>
        public static int OrcLevel(int wave, int levelEvery, int maxLevel)
        {
            var level = levelEvery > 0 ? 1 + (Mathf.Max(1, wave) - 1) / levelEvery : 1;
            return Mathf.Clamp(level, 1, Mathf.Max(1, maxLevel));
        }

        public static bool IsBossWave(int wave, int every) => every > 0 && wave > 0 && wave % every == 0;

        /// <summary>
        /// What the orc at `index` of a lane is: the chief leads a boss wave's lane; from their waves
        /// every `bruteEvery`-th orc is a brute and every `throwerEvery`-th throws (a brute wins
        /// where both fall on one orc); the rest are raiders.
        /// </summary>
        public static OrcKind KindAt(int index, int wave, bool withChief, int throwerFromWave, int throwerEvery, int bruteFromWave, int bruteEvery)
        {
            if (withChief && index == 0) return OrcKind.Chief;
            if (bruteFromWave > 0 && bruteEvery > 0 && wave >= bruteFromWave && (index + 1) % bruteEvery == 0) return OrcKind.Brute;
            if (throwerFromWave > 0 && throwerEvery > 0 && wave >= throwerFromWave && (index + 1) % throwerEvery == 0) return OrcKind.Thrower;
            return OrcKind.Raider;
        }

        /// <summary>The gold an orc of this kind pays when it falls in a run.</summary>
        public static int GoldFor(OrcKind kind)
        {
            var config = BalanceConfig.Instance;
            switch (kind)
            {
                case OrcKind.Thrower: return config.InvasionGoldThrower;
                case OrcKind.Brute: return config.InvasionGoldBrute;
                case OrcKind.Chief:
                case OrcKind.Boss: return config.InvasionGoldChief;
                case OrcKind.Wolf: return config.InvasionGoldWolf;
                case OrcKind.Bear: return config.InvasionGoldBear;
                case OrcKind.Troll: return config.InvasionGoldTroll;
                case OrcKind.Giant: return 0;      // the boss pays through InvasionJungle
                default: return config.InvasionGoldOrc;
            }
        }

        /// <summary>
        /// What a kill pays: an orc its kind's gold (the user's fixed numbers), a jungle monster its
        /// kind's gold × its level -- the monsters grow with the run and so does what they are worth.
        /// </summary>
        public static int GoldForKill(OrcKind kind, int level) => OrcUnit.IsMonster(kind) ? GoldFor(kind) * Mathf.Max(1, level) : GoldFor(kind);

        public static int GoldForWave(int wave, int perWave) => Mathf.Max(0, perWave) * Mathf.Max(0, wave);
    }
}
