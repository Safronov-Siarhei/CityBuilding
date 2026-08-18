using System;
using CityBuilder.Buildings;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// People arriving at the settlement, and people walking out of it.
    ///
    /// This is what contentment is FOR. Until it existed, happiness was a number the player could
    /// read and nothing in the game could: six factors averaged into a percentage that drove
    /// nothing. Now it sets the pace of the only resource that cannot be mined -- above the
    /// threshold settlers turn up and the happier the town the shorter the wait, below it they
    /// leave and the more miserable the town the faster they go. The threshold itself is a dead
    /// point where nobody moves at all.
    ///
    /// Two rules keep this from turning into a spiral the player cannot climb out of. Departures
    /// go through CitizenManager.RemoveCitizens and never count as deaths, so leaving does not
    /// drag the happiness score down and cause more leaving; and the settlement never empties --
    /// MigrationMinPopulation stays put however bad it gets, because unhappiness is meant to stall
    /// a town, not to end the map. Starvation is still the way to lose your people.
    ///
    /// Cheap by construction, which matters on a phone: one float per frame, no allocation, no
    /// scanning, and the two interval formulas are pure statics so an EditMode test can pin the
    /// curve without a scene.
    /// </summary>
    public class MigrationManager : MonoBehaviour
    {
        public static MigrationManager Instance { get; private set; }

        [SerializeField] private CitizenManager citizenManager;
        [SerializeField] private BuildingPlacer buildingPlacer;

        /// <summary>What the settlement is doing about its population right now -- the HUD says which of these it is and how long until the next one moves.</summary>
        public enum MigrationState
        {
            /// <summary>Nothing has been founded yet: no Town Hall, so there is nothing to move to.</summary>
            Dormant,

            /// <summary>The opening grace period. Migration is frozen while the player gets a town going; contentment is still reported honestly (see SettlingInRemaining).</summary>
            SettlingIn,

            /// <summary>Content enough to attract people, and room to put them.</summary>
            Arriving,

            /// <summary>Content enough to attract people, but every bed is taken.</summary>
            NoRoom,

            /// <summary>Sitting exactly on the threshold: nobody comes, nobody goes.</summary>
            Balanced,

            /// <summary>Too miserable to stay.</summary>
            Leaving,

            /// <summary>Miserable, but down to the last people the settlement is allowed to lose.</summary>
            Deserted,
        }

        public MigrationState State { get; private set; } = MigrationState.Dormant;

        /// <summary>Seconds until the next person arrives or leaves. Zero whenever the state is not Arriving or Leaving.</summary>
        public float SecondsUntilNextMove { get; private set; }

        /// <summary>Seconds of grace left after the Town Hall went up. Zero once the settlement is on its own.</summary>
        public float SettlingInRemaining { get; private set; }

        /// <summary>Raised whenever the state or the countdown changes enough for the HUD to care -- once a second, not once a frame.</summary>
        public event Action OnMigrationChanged;

        private float _timer;
        private bool _graceStarted;
        private int _lastReportedSecond = -1;
        private MigrationState _lastReportedState = MigrationState.Dormant;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (buildingPlacer != null) buildingPlacer.OnBuildingPlaced += HandleBuildingPlaced;
        }

        private void OnDestroy()
        {
            if (buildingPlacer != null) buildingPlacer.OnBuildingPlaced -= HandleBuildingPlaced;
        }

        private void HandleBuildingPlaced(BuildingData data)
        {
            if (data == null || data.buildingName != BuildingIds.TownHall || _graceStarted) return;

            // The clock starts at the founding, not at the scene loading: the player spends the
            // opening of a new game choosing where to put the Town Hall, and spending the grace
            // period on that decision would defeat the point of having one.
            _graceStarted = true;
            SettlingInRemaining = BalanceConfig.Instance.SettlingInSeconds;
        }

        /// <summary>
        /// Whether there is a settlement to migrate to at all. Asked of the building registry
        /// rather than remembered in a field of our own, so it cannot go stale -- a load restores
        /// buildings without going through BuildingPlacer, and a flag set from the placement event
        /// would have left every loaded game permanently Dormant.
        /// </summary>
        private static bool Founded => BuildingInstance.HasAny(BuildingIds.TownHall);

        private void Update()
        {
            Step(Time.deltaTime);
        }

        /// <summary>
        /// One step of the clock. Separate from Update and public so a PlayMode test can drive
        /// minutes of migration through it in a frame instead of waiting them out in real time.
        /// </summary>
        public void Step(float deltaTime)
        {
            if (citizenManager == null) citizenManager = CitizenManager.Instance;
            if (citizenManager == null) return;

            if (!Founded)
            {
                SetState(MigrationState.Dormant, 0f);
                return;
            }

            if (SettlingInRemaining > 0f)
            {
                SettlingInRemaining = Mathf.Max(0f, SettlingInRemaining - deltaTime);
                _timer = 0f;
                SetState(MigrationState.SettlingIn, SettlingInRemaining);
                return;
            }

            var config = BalanceConfig.Instance;
            var happiness = HappinessManager.Instance != null ? HappinessManager.Instance.HappinessPercent : 100;
            var threshold = config.MigrationHappinessThreshold;

            if (happiness == threshold)
            {
                _timer = 0f;
                SetState(MigrationState.Balanced, 0f);
                return;
            }

            var arriving = happiness > threshold;
            var blocked = arriving
                ? citizenManager.FreeSpace <= 0
                : citizenManager.TotalPopulation <= config.MigrationMinPopulation;

            if (blocked)
            {
                _timer = 0f;
                SetState(arriving ? MigrationState.NoRoom : MigrationState.Deserted, 0f);
                return;
            }

            // Turning around throws the wait away rather than carrying it over. A countdown the
            // player has been watching tick towards "a settler arrives" must not deliver a
            // departure the instant the mood dips past the threshold.
            var wanted = arriving ? MigrationState.Arriving : MigrationState.Leaving;
            if (State != wanted) _timer = 0f;

            var interval = arriving ? ArriveIntervalSeconds(happiness) : LeaveIntervalSeconds(happiness);
            _timer += deltaTime;

            if (_timer >= interval)
            {
                _timer = 0f;
                if (arriving) citizenManager.AddCitizens(1);
                else citizenManager.RemoveCitizens(1);
            }

            SetState(wanted, Mathf.Max(0f, interval - _timer));
        }

        /// <summary>
        /// How long the settlement waits for its next settler, at a given contentment.
        ///
        /// Anchored at the two ends the design names: the first point above the threshold is the
        /// longest wait, a perfectly content town the shortest, straight line between. Pure and
        /// static so an EditMode test can pin the curve without a scene or a Town Hall.
        /// </summary>
        public static float ArriveIntervalSeconds(int happiness)
        {
            var config = BalanceConfig.Instance;
            var slowest = config.MigrationArriveIntervalAtThresholdSeconds;
            var fastest = config.MigrationArriveIntervalAtFullSeconds;

            var start = config.MigrationHappinessThreshold + 1;
            if (happiness <= start || start >= 100) return slowest;

            return Mathf.Lerp(slowest, fastest, Mathf.Clamp01((happiness - start) / (float)(100 - start)));
        }

        /// <summary>
        /// The mirror of ArriveIntervalSeconds, below the threshold: the first point under it is the
        /// slowest trickle out, rock bottom the fastest. The band is narrower than the arrival one
        /// (29 points against 69), so a point of misery costs the settlement roughly 2,4x what a
        /// point of contentment buys it -- deliberate, and the reason a town in trouble empties
        /// faster than a happy one fills.
        /// </summary>
        public static float LeaveIntervalSeconds(int happiness)
        {
            var config = BalanceConfig.Instance;
            var slowest = config.MigrationLeaveIntervalAtThresholdSeconds;
            var fastest = config.MigrationLeaveIntervalAtZeroSeconds;

            var start = config.MigrationHappinessThreshold - 1;
            if (happiness >= start || start <= 0) return slowest;

            return Mathf.Lerp(slowest, fastest, Mathf.Clamp01((start - happiness) / (float)start));
        }

        /// <summary>
        /// Used by save/load: the settlement comes back mid-wait rather than with its clock reset,
        /// which would otherwise be a small pardon or a small punishment for reloading. Must run
        /// after the buildings are back, since Founded asks them whether there is a town at all.
        ///
        /// A save written before migration existed carries zeroes, which read correctly: an
        /// established town, no grace left, next arrival a full interval away.
        /// </summary>
        public void RestoreFromSave(float timer, float settlingInRemaining)
        {
            _timer = Mathf.Max(0f, timer);
            SettlingInRemaining = Mathf.Max(0f, settlingInRemaining);
            _graceStarted = Founded;
        }

        /// <summary>What the save has to carry -- see RestoreFromSave.</summary>
        public float Timer => _timer;

        /// <summary>
        /// Raises OnMigrationChanged only when the whole second on display actually changes, not on
        /// every frame: the HUD shows m:ss, and rebuilding that label sixty times a second to write
        /// the same characters is exactly the sort of per-frame UI churn a phone pays for.
        /// </summary>
        private void SetState(MigrationState state, float secondsRemaining)
        {
            State = state;
            SecondsUntilNextMove = secondsRemaining;

            var second = Mathf.CeilToInt(secondsRemaining);
            if (state == _lastReportedState && second == _lastReportedSecond) return;

            _lastReportedState = state;
            _lastReportedSecond = second;
            OnMigrationChanged?.Invoke();
        }
    }
}
