using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Combat
{
    /// <summary>
    /// The sandbox's orc raids by the table (the orcwaves tab, the user 2026-11-22): wave N's wait,
    /// its orcs and siege machines, its level and the tribute that buys it off. The first wave sails
    /// delay_sec after the Town Hall goes down; each later one delay_sec after the last orc of the
    /// one before has died (or the tribute was paid). tribute_seconds before the fleet sails the
    /// chief's scroll comes up (UI/TributeScroll): pay, fight now, or put it off into a badge.
    ///
    /// Help after a heavy wave (the user's rule + options 1 and 3): a wave that broke raid_heavy_share
    /// of the buildings it found on its island (no roads, fences or decor) makes the next one weaker
    /// (raid_heavy_mult health and attack) and later (+raid_heavy_rest_sec); a Town Hall below
    /// raid_hall_retreat_share sends the wave back to its beach. A wave nobody of which struck, was
    /// hurt or moved for raid_stuck_sec is over (its stuck orcs are taken off).
    /// </summary>
    public partial class OrcRaidManager
    {
        public enum WavePhase { Waiting = 0, Out = 1 }

        private WavePhase _wavePhase;
        private int _wavesDone;                // raids sent or bought off so far: the next is _wavesDone + 1
        private int _waveRaid;                 // the ships' raid number of the wave out
        private readonly List<OrcKind> _waveQueue = new List<OrcKind>();     // still aboard, in landing order
        private readonly List<int> _waveSiegeQueue = new List<int>();        // 0 none, 1 ram, 2 tower -- parallel to _waveQueue
        private readonly List<OrcUnit> _waveOrcs = new List<OrcUnit>();
        private readonly List<Vector3> _wavePositions = new List<Vector3>();
        private readonly List<BuildingInstance> _waveSnapshot = new List<BuildingInstance>();
        private bool _waveSnapped;
        private bool _waveHelped, _helpNext, _waveRetreating;
        private float _waveActivity, _waveSample, _retreatUntil;
        private Vector3 _waveBeach;
        private int _waveLevel = 1;

        /// <summary>Raids come by the table: the archipelago sandbox, with rows in the orcwaves tab.</summary>
        public static bool TableWaves
        {
            get
            {
                var map = MeshMapApplier.Instance != null ? MeshMapApplier.Instance.CurrentMap : null;
                var config = BalanceConfig.Instance;
                return map != null && map.NoRaids && config != null && config.OrcWaves.Count > 0;
            }
        }

        public WavePhase CurrentWavePhase => _wavePhase;
        public int NextWaveNumber => _wavesDone + 1;
        public IReadOnlyList<OrcUnit> WaveOrcs => _waveOrcs;
        public bool WaveRetreating => _waveRetreating;

        /// <summary>The row of the next wave (past the table's end, its last row).</summary>
        public BalanceConfig.OrcWaveRow NextWaveRow => BalanceConfig.Instance != null ? BalanceConfig.Instance.OrcWave(NextWaveNumber) : null;

        /// <summary>The chief's scroll is due: the next wave sails within tribute_seconds, it has a tribute, and its fleet can sail.</summary>
        public bool TributeDue
        {
            get
            {
                if (!TableWaves || _wavePhase != WavePhase.Waiting || RaidsSuspended || WorldSettings.Current.RaidsOff) return false;
                if (Tutorial.TutorialMode.IsActive || Ships.ShipManager.Instance == null || Ships.ShipManager.Instance.OrcShipyard == null) return false;
                var row = NextWaveRow;
                return row != null && row.tribute.Count > 0 && _raidTimer <= BalanceConfig.Instance.TributeSeconds;
            }
        }

        /// <summary>Seconds until the next wave sails (the scroll's and the badge's timer).</summary>
        public float SecondsToSail => Mathf.Max(0f, _raidTimer);

        /// <summary>The first wave's wait from the Town Hall: its row's delay, never inside the world's quiet start.</summary>
        private float FirstWaveSeconds()
        {
            var row = BalanceConfig.Instance.OrcWave(1);
            var delay = row != null ? row.delay * WorldSettings.Current.RaidIntervalFactor : BalanceConfig.Instance.RaidIntervalSeconds;
            return Mathf.Max(delay, WorldSettings.Current.QuietSeconds);
        }

        private float DelayOf(int wave)
        {
            var row = BalanceConfig.Instance.OrcWave(wave);
            return (row != null ? row.delay : 600f) * WorldSettings.Current.RaidIntervalFactor;
        }

        private void TickTableWaves(float dt)
        {
            if (_wavePhase == WavePhase.Out)
            {
                TickWaveOut();
                return;
            }
            // the shipyard broken: no fleet can sail, so the clock waits (and no scroll comes up)
            var ships = Ships.ShipManager.Instance;
            if (ships == null || ships.OrcShipyard == null) return;
            _raidTimer -= dt;
            if (_raidTimer > 0f) return;
            LaunchTableWave();
        }

        /// <summary>The cheat window: the next wave's scroll now (its clock set to tribute_seconds).</summary>
        public bool TributeNow()
        {
            if (!TableWaves || _wavePhase != WavePhase.Waiting) return false;
            _raidTimer = Mathf.Min(_raidTimer, BalanceConfig.Instance.TributeSeconds);
            return TributeDue;
        }

        /// <summary>«Биться!»: the scroll shut, the fleet sails now.</summary>
        public void FightNow()
        {
            if (_wavePhase == WavePhase.Waiting) _raidTimer = 0f;
        }

        /// <summary>«Заплатить»: all of the tribute or none of it, out of the stores; the wave counts and does not come.</summary>
        public bool PayTribute()
        {
            if (!TributeDue) return false;
            var row = NextWaveRow;
            var resources = Resources.ResourceManager.Instance;
            if (row == null || resources == null || !resources.TrySpend(row.tribute)) return false;
            _wavesDone++;
            _raidTimer = DelayOf(NextWaveNumber);
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_tribute_paid"), EventSeverity.Good, EventGlyph.Swords, null, Vector3.zero));
            return true;
        }

        /// <summary>Whether the stores hold the whole tribute.</summary>
        public static bool CanPay(BalanceConfig.OrcWaveRow row)
        {
            var resources = Resources.ResourceManager.Instance;
            return row != null && resources != null && resources.HasEnough(row.tribute);
        }

        private int Scaled(int n) => n <= 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(n * WorldSettings.Current.RaidSizeFactor));

        private void LaunchTableWave()
        {
            var row = NextWaveRow;
            var ships = Ships.ShipManager.Instance;
            if (row == null || ships == null) return;

            // aboard: the chiefs first (one leads the landing), the machines, then the rest
            var kinds = new List<OrcKind>();
            var siege = new List<int>();
            void Add(OrcKind kind, int count, int machine)
            {
                for (var i = 0; i < count; i++)
                {
                    kinds.Add(kind);
                    siege.Add(machine);
                }
            }
            Add(OrcKind.Chief, Scaled(row.chiefs), 0);
            Add(OrcKind.Brute, Scaled(row.rams), 1);
            Add(OrcKind.Brute, Scaled(row.towers), 2);
            Add(OrcKind.Raider, Scaled(row.raiders), 0);
            Add(OrcKind.Thrower, Scaled(row.throwers), 0);
            Add(OrcKind.Brute, Scaled(row.brutes), 0);
            if (kinds.Count == 0)
            {
                // an empty row: nothing comes, and it counts
                _wavesDone++;
                _raidTimer = DelayOf(NextWaveNumber);
                return;
            }

            // into transports by seats: a machine takes raid_siege_seats, an orc one
            var capacity = Ships.ShipManager.OrcTransportSeats;
            var machineSeats = Mathf.Clamp(BalanceConfig.Instance.RaidSiegeSeats, 1, capacity);
            var loads = new List<int>();
            int count = 0, used = 0;
            foreach (var machine in siege)
            {
                var seats = machine != 0 ? machineSeats : 1;
                if (used + seats > capacity && count > 0)
                {
                    loads.Add(count);
                    count = 0;
                    used = 0;
                }
                count++;
                used += seats;
            }
            if (count > 0) loads.Add(count);

            var raid = ships.LaunchOrcLoads(loads, row.level);
            if (raid == 0)
            {
                // nowhere to land just now: try again in a while
                _raidTimer = 30f;
                return;
            }
            _wavesDone++;
            _wavePhase = WavePhase.Out;
            _waveRaid = raid;
            _waveLevel = row.level;
            _waveQueue.Clear();
            _waveQueue.AddRange(kinds);
            _waveSiegeQueue.Clear();
            _waveSiegeQueue.AddRange(siege);
            _waveOrcs.Clear();
            _wavePositions.Clear();
            _waveSnapshot.Clear();
            _waveSnapped = false;
            _waveHelped = _helpNext;
            _helpNext = false;
            _waveRetreating = false;
            _waveActivity = Time.time;
        }

        /// <summary>A transport of the table wave ashore: its `count` off the wave's queue.</summary>
        private OrcUnit LandTableWave(Vector3 beach, int count)
        {
            EnsureMaterials();
            if (!_waveSnapped) SnapIsland(beach);
            OrcUnit leader = null;
            for (var i = 0; i < count && _waveQueue.Count > 0; i++)
            {
                var kind = _waveQueue[0];
                var machine = _waveSiegeQueue.Count > 0 ? _waveSiegeQueue[0] : 0;
                _waveQueue.RemoveAt(0);
                if (_waveSiegeQueue.Count > 0) _waveSiegeQueue.RemoveAt(0);
                var orc = SpawnOrc(beach, _waveLevel, kind);
                if (orc == null) continue;
                if (machine != 0) MakeSiege(orc, machine == 2);
                if (_waveHelped) Help(orc);
                AdoptWave(orc);
                if (leader == null) leader = orc;
            }
            // the fire-raisers come with the first boat, as with every raid
            if (_waveOrcs.Count > 0 && !_torchesLanded)
            {
                _torchesLanded = true;
                var before = OrcUnit.All.Count;
                SpawnTorchbearers(beach, _waveLevel);
                for (var i = before; i < OrcUnit.All.Count; i++)
                {
                    var torch = OrcUnit.All[i];
                    if (torch == null) continue;
                    if (_waveHelped) Help(torch);
                    AdoptWave(torch);
                }
            }
            _waveActivity = Time.time;
            return leader;
        }

        private bool _torchesLanded;

        private void Help(OrcUnit orc)
        {
            var m = BalanceConfig.Instance.RaidHeavyMult;
            orc.Toughen(m, m);
        }

        /// <summary>One of the wave, watched: its blows keep the wave alive (the stuck rule).</summary>
        private void AdoptWave(OrcUnit orc)
        {
            _waveOrcs.Add(orc);
            _wavePositions.Add(orc.transform.position);
            orc.Struck += () => _waveActivity = Time.time;
        }

        /// <summary>The buildings on the landing's island as the wave comes ashore -- roads, fences and decor not counted.</summary>
        private void SnapIsland(Vector3 beach)
        {
            _waveSnapped = true;
            _waveBeach = beach;
            _waveSnapshot.Clear();
            var island = Citizens.HousePeople.IslandOf(beach);
            foreach (var b in BuildingInstance.All)
                if (Counts(b) && Citizens.HousePeople.Same(island, Citizens.HousePeople.IslandOf(b))) _waveSnapshot.Add(b);
        }

        private static bool Counts(BuildingInstance b) =>
            b != null && !b.IsBot && b.Data != null && !b.Data.IsGroundWork && b.Data.buildingName != WallWalk.FenceId
            && !Citizens.DecorBonus.IsDecor(b.Data) && b.CurrentHealth > 0;

        private void TickWaveOut()
        {
            var ships = Ships.ShipManager.Instance;
            var outbound = ships != null && ships.OrcRaidOutbound(_waveRaid);
            // the ships still to land were sunk: their orcs never come
            if (!outbound) { _waveQueue.Clear(); _waveSiegeQueue.Clear(); }

            for (var i = _waveOrcs.Count - 1; i >= 0; i--)
            {
                if (_waveOrcs[i] == null || _waveOrcs[i].CurrentHealth <= 0)
                {
                    _waveOrcs.RemoveAt(i);
                    _wavePositions.RemoveAt(i);
                    continue;
                }
                if (_waveOrcs[i].LastHurtAt > _waveActivity) _waveActivity = _waveOrcs[i].LastHurtAt;
            }

            if (_waveRetreating)
            {
                TickRetreat();
                if (_waveOrcs.Count == 0 && !outbound) EndTableWave();
                return;
            }

            // moving counts as alive, sampled every few seconds
            if (Time.time >= _waveSample)
            {
                _waveSample = Time.time + 5f;
                for (var i = 0; i < _waveOrcs.Count; i++)
                {
                    var at = _waveOrcs[i].transform.position;
                    if ((at - _wavePositions[i]).sqrMagnitude > 0.25f) _waveActivity = Time.time;
                    _wavePositions[i] = at;
                }
                if (_waveOrcs.Count > 0 && HallBroken()) StartRetreat();
            }

            if (outbound || _waveQueue.Count > 0) return;
            if (_waveOrcs.Count > 0 && Time.time - _waveActivity < BalanceConfig.Instance.RaidStuckSeconds) return;
            // stuck for good: the wave is over and its orcs are taken off
            foreach (var orc in _waveOrcs)
                if (orc != null) Destroy(orc.gameObject);
            _waveOrcs.Clear();
            _wavePositions.Clear();
            EndTableWave();
        }

        /// <summary>The player's Town Hall below raid_hall_retreat_share of its health.</summary>
        private static bool HallBroken()
        {
            var share = BalanceConfig.Instance.RaidHallRetreatShare;
            foreach (var b in BuildingInstance.All)
                if (b != null && !b.IsBot && b.Data != null && b.Data.buildingName == BuildingIds.TownHall && b.CurrentHealth > 0
                    && b.CurrentHealth < b.MaxHealth * share) return true;
            return false;
        }

        private void StartRetreat()
        {
            _waveRetreating = true;
            _retreatUntil = Time.time + 40f;
            foreach (var orc in _waveOrcs)
                if (orc != null) orc.Steered = true;
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_orcs_retreat"), EventSeverity.Good, EventGlyph.Swords, null, _waveBeach));
        }

        /// <summary>Back to the beach they came by, and gone there (or after a while, wherever they are).</summary>
        private void TickRetreat()
        {
            var late = Time.time >= _retreatUntil;
            for (var i = _waveOrcs.Count - 1; i >= 0; i--)
            {
                var orc = _waveOrcs[i];
                orc.Steered = true;
                if (!late && orc.SteerTo(_waveBeach)) continue;
                Destroy(orc.gameObject);
                _waveOrcs.RemoveAt(i);
                _wavePositions.RemoveAt(i);
            }
        }

        private void EndTableWave()
        {
            var config = BalanceConfig.Instance;
            var broken = 0;
            foreach (var b in _waveSnapshot)
                if (b == null || b.CurrentHealth <= 0) broken++;
            _helpNext = _waveSnapshot.Count > 0 && broken >= _waveSnapshot.Count * config.RaidHeavyShare;
            _wavePhase = WavePhase.Waiting;
            _waveRetreating = false;
            _torchesLanded = false;
            _waveSnapshot.Clear();
            _waveSnapped = false;
            _raidTimer = DelayOf(NextWaveNumber) + (_helpNext ? config.RaidHeavyRestSeconds : 0f);
        }

        // ------------------------------------------------------------ siege machines

        [SerializeField] private GameObject siegeRamModel;
        [SerializeField] private GameObject siegeTowerModel;

        /// <summary>A brute made a ram or a siege tower (raid_ram_* / raid_tower_*), its errand on it.</summary>
        private void MakeSiege(OrcUnit orc, bool tower)
        {
            var config = BalanceConfig.Instance;
            orc.name = tower ? "OrcSiegeTower" : "OrcRam";
            orc.Toughen(tower ? config.RaidTowerHealthMult : config.RaidRamHealthMult, tower ? 1f : config.RaidRamDamageMult);
            orc.Slow(config.RaidSiegeSpeed);
            orc.IgnoresSoldiers = true;
            orc.LaneOnly = true;
            DressSiege(orc, tower ? siegeTowerModel : siegeRamModel);
            orc.gameObject.AddComponent<SiegeErrand>().Tower = tower;
        }

        /// <summary>The brute's own figure hidden, the machine's model in its place (as the campaign's, CityDefenceMode.Siege).</summary>
        private static void DressSiege(OrcUnit orc, GameObject model)
        {
            if (model == null) return;
            foreach (var r in orc.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            var go = Instantiate(model, orc.transform, false);
            go.name = model.name;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            go.transform.localScale = model.transform.localScale / Mathf.Max(0.01f, orc.transform.lossyScale.x);
        }

        // ------------------------------------------------------------ saving

        /// <summary>What the save keeps of the table waves.</summary>
        public void CaptureWaves(out int phase, out int done, out int raid, List<int> queue, List<int> siege, out bool helped, out bool helpNext,
            out bool snapped, List<Vector2Int> snapshot, out int snapshotBroken, out Vector3 beach, out int level, out bool torches)
        {
            phase = (int)_wavePhase;
            done = _wavesDone;
            raid = _waveRaid;
            foreach (var k in _waveQueue) queue.Add((int)k);
            siege.AddRange(_waveSiegeQueue);
            helped = _waveHelped;
            helpNext = _helpNext;
            snapped = _waveSnapped;
            snapshotBroken = 0;
            foreach (var b in _waveSnapshot)
            {
                if (b == null || b.CurrentHealth <= 0) snapshotBroken++;
                else snapshot.Add(b.OriginCell);
            }
            beach = _waveBeach;
            level = _waveLevel;
            torches = _torchesLanded;
        }

        /// <summary>The table waves as the save left them (before the orcs come back: they join through RestoreWaveOrc).</summary>
        public void RestoreWaves(int phase, int done, int raid, List<int> queue, List<int> siege, bool helped, bool helpNext,
            bool snapped, List<Vector2Int> snapshot, int snapshotBroken, Vector3 beach, int level, bool torches)
        {
            _wavePhase = (WavePhase)Mathf.Clamp(phase, 0, 1);
            _wavesDone = Mathf.Max(0, done);
            _waveRaid = raid;
            _waveQueue.Clear();
            if (queue != null) foreach (var k in queue) _waveQueue.Add((OrcKind)k);
            _waveSiegeQueue.Clear();
            if (siege != null) _waveSiegeQueue.AddRange(siege);
            while (_waveSiegeQueue.Count < _waveQueue.Count) _waveSiegeQueue.Add(0);
            _waveHelped = helped;
            _helpNext = helpNext;
            _waveSnapped = snapped;
            _waveSnapshot.Clear();
            for (var i = 0; i < snapshotBroken; i++) _waveSnapshot.Add(null);
            if (snapshot != null)
                foreach (var cell in snapshot)
                {
                    BuildingInstance hit = null;
                    foreach (var b in BuildingInstance.All)
                        if (b != null && !b.IsBot && b.OriginCell == cell) { hit = b; break; }
                    _waveSnapshot.Add(hit);
                }
            _waveBeach = beach;
            _waveLevel = Mathf.Max(1, level);
            _torchesLanded = torches;
            _waveOrcs.Clear();
            _wavePositions.Clear();
            _waveActivity = Time.time;
        }

        /// <summary>A saved orc of the wave back in it: its machine dressed again, its help again, its health as saved.</summary>
        public void RestoreWaveOrc(OrcUnit orc, int siege, int health)
        {
            if (orc == null) return;
            if (siege != 0) MakeSiege(orc, siege == 2);
            if (_waveHelped) Help(orc);
            orc.SetCurrentHealth(health);
            AdoptWave(orc);
        }

        /// <summary>The machine an orc is (0 none, 1 ram, 2 tower) -- the save's.</summary>
        public static int SiegeOf(OrcUnit orc)
        {
            var errand = orc != null ? orc.GetComponent<SiegeErrand>() : null;
            return errand == null ? 0 : errand.Tower ? 2 : 1;
        }

        public bool IsWaveOrc(OrcUnit orc) => _waveOrcs.Contains(orc);
    }

    /// <summary>
    /// A raid's siege machine (the user, 2026-11-22): a ram rolls for the nearest gate or wall it
    /// can reach -- else any building -- and breaks it; a tower rolls up against a wall and the
    /// wave's orcs beside it climb over it into the town (without a wall in reach it rolls for a
    /// building like a ram). Neither ever enters a moat: a target whose way crosses moat water is
    /// passed over, and with none left the machine waits where it stands.
    /// </summary>
    public class SiegeErrand : MonoBehaviour
    {
        public bool Tower;

        private OrcUnit _orc;
        private BuildingInstance _target;
        private float _think, _dockedUntil, _nextClimb, _still;
        private bool _docked, _wall;
        private Vector3 _lastAt;
        private static NavMeshPath _path;
        private static NavMeshPath Path => _path ??= new NavMeshPath();

        private void Awake() => _orc = GetComponent<OrcUnit>();

        private void Update()
        {
            if (_orc == null || _orc.CurrentHealth <= 0) return;
            var raids = OrcRaidManager.Instance;
            if (raids != null && raids.WaveRetreating) return;
            var config = BalanceConfig.Instance;
            if (_docked)
            {
                TickDocked(raids, config);
                return;
            }
            var at = transform.position;
            _still = (at - _lastAt).sqrMagnitude < 0.0004f ? _still + Time.deltaTime : 0f;
            _lastAt = at;
            if (Tower && _wall && _target != null && _target.CurrentHealth > 0 && AgainstWall(at))
            {
                Dock(config);
                return;
            }
            _think -= Time.deltaTime;
            if (_think > 0f && _target != null && _target.CurrentHealth > 0) return;
            _think = 2f;
            Pick();
        }

        private float Cell => GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;

        private bool AgainstWall(Vector3 at)
        {
            var d = Flat(at - _target.transform.position);
            return d <= 1.6f * Cell || (_still >= 1.2f && d <= 3f * Cell);
        }

        /// <summary>The nearest reachable target off the moat; none: stand and wait.</summary>
        private void Pick()
        {
            var here = transform.position;
            var island = Citizens.HousePeople.IslandOf(here);
            var walls = new List<BuildingInstance>();
            var others = new List<BuildingInstance>();
            foreach (var b in BuildingInstance.All)
            {
                if (b == null || b.IsBot || b.Data == null || b.Data.IsGroundWork || b.CurrentHealth <= 0) continue;
                if (!Citizens.HousePeople.Same(island, Citizens.HousePeople.IslandOf(b))) continue;
                (WallWalk.Concerns(b.Data.buildingName) ? walls : others).Add(b);
            }
            int Near(BuildingInstance a, BuildingInstance b) =>
                (a.transform.position - here).sqrMagnitude.CompareTo((b.transform.position - here).sqrMagnitude);
            walls.Sort(Near);
            others.Sort(Near);
            if (TryRoll(walls, true)) return;
            if (TryRoll(others, false)) return;
            _target = null;
            _orc.OnlyTarget = null;
            _orc.FollowLane(null);
            _orc.BusyFor = Mathf.Max(_orc.BusyFor, 2f);
        }

        private bool TryRoll(List<BuildingInstance> list, bool wall)
        {
            var here = transform.position;
            for (var i = 0; i < list.Count && i < 8; i++)
            {
                var b = list[i];
                if (!NavMesh.SamplePosition(b.transform.position, out var end, 4f * Cell, NavMesh.AllAreas)) continue;
                if (!NavMesh.CalculatePath(here, end.position, NavMesh.AllAreas, Path) || Path.status != NavMeshPathStatus.PathComplete) continue;
                var corners = Path.corners;
                if (corners.Length == 0 || CrossesMoat(corners)) continue;
                if (b == _target) return true;
                _target = b;
                _wall = wall;
                _orc.NeverStrikes = Tower && wall;
                _orc.OnlyTarget = b;
                _orc.FollowLane(new OrcLane(corners));
                return true;
            }
            return false;
        }

        private static bool CrossesMoat(Vector3[] corners)
        {
            for (var i = 0; i + 1 < corners.Length; i++)
            {
                var a = corners[i];
                var b = corners[i + 1];
                var steps = Mathf.CeilToInt(Flat(b - a) / 0.35f);
                for (var k = 0; k <= steps; k++)
                    if (Moat.Wading(Vector3.Lerp(a, b, steps > 0 ? k / (float)steps : 0f))) return true;
            }
            return false;
        }

        private void Dock(BalanceConfig config)
        {
            _docked = true;
            _orc.FollowLane(null);
            _orc.OnlyTarget = null;
            _orc.BusyFor = 1f;
            var to = _target.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to.normalized);
            _dockedUntil = Time.time + config.RaidTowerStaySeconds;
            _nextClimb = Time.time + 0.5f;
        }

        /// <summary>Against the wall: one of the wave beside it over the wall every climb gap, then the tower is left.</summary>
        private void TickDocked(OrcRaidManager raids, BalanceConfig config)
        {
            _orc.BusyFor = 1f;
            if (Time.time >= _dockedUntil || _target == null)
            {
                Destroy(gameObject);
                return;
            }
            if (Time.time < _nextClimb || raids == null) return;
            _nextClimb = Time.time + config.RaidTowerClimbGapSeconds;
            var foot = transform.position;
            var over = _target.transform.position - foot;
            over.y = 0f;
            if (over.sqrMagnitude < 0.01f) return;
            var drop = _target.transform.position + over.normalized * (2.5f * Cell);
            if (!NavMesh.SamplePosition(drop, out var hit, 2f * Cell, NavMesh.AllAreas) || Moat.Wading(hit.position)) return;
            foreach (var orc in raids.WaveOrcs)
            {
                if (orc == null || orc == _orc || orc.GetComponent<SiegeErrand>() != null) continue;
                if (Flat(orc.transform.position - foot) > 4f * Cell) continue;
                // already over the wall (nearer the drop than the foot)
                if (Flat(orc.transform.position - hit.position) < Flat(orc.transform.position - foot)) continue;
                orc.transform.position = hit.position;
                break;
            }
        }

        private static float Flat(Vector3 v)
        {
            v.y = 0f;
            return v.magnitude;
        }
    }
}
