using System;
using CityBuilder.Combat;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Invasion
{
    /// <summary>
    /// The run's points (the user, 2026-10-21/22): the waves survived, squared -- so they grow
    /// faster than the waves (10 → 100, 20 → 400, 30 → 900) -- plus a bonus for every giant slain
    /// and every orc-camp building razed (the walls and their gates do not count: a wall is many
    /// small pieces). Numbers: economy inv_score_*.
    ///
    /// The run is written down (InvasionRecords) once, when the castle falls; a run left or cut
    /// short never gets here and is not counted.
    /// </summary>
    public class InvasionScore : MonoBehaviour
    {
        public int BossesSlain { get; private set; }
        public int CampBuildingsRazed { get; private set; }

        /// <summary>The run has ended and been written down.</summary>
        public bool Finished { get; private set; }

        private InvasionWaves _waves;

        /// <summary>How long the run has gone on, in game seconds (the clock under the minimap, the user 2026-10-28); stops when it ends.</summary>
        public float RunSeconds { get; private set; }

        public int WavesSurvived => _waves != null ? _waves.WavesSurvived : 0;

        public int WavePoints => WavePointsFor(WavesSurvived, BalanceConfig.Instance.InvasionScorePerWaveSquared);
        public int BossPoints => BossesSlain * BalanceConfig.Instance.InvasionScoreBoss;
        public int CampPoints => CampBuildingsRazed * BalanceConfig.Instance.InvasionScoreCampBuilding;
        public int Total => WavePoints + BossPoints + CampPoints;

        public static int WavePointsFor(int wavesSurvived, int perWaveSquared) => wavesSurvived * wavesSurvived * perWaveSquared;

        /// <summary>The camp buildings that score: everything but the wall and its gates.</summary>
        public static bool Scores(OrcCampBuildingKind kind) => kind != OrcCampBuildingKind.Palisade && kind != OrcCampBuildingKind.Gate;

        public void Begin(InvasionWaves waves) => _waves = waves;

        private void OnEnable()
        {
            OrcUnit.Died += OnEnemyDied;
            OrcCampBuilding.OnDestroyed += OnCampBuildingFell;
        }

        private void OnDisable()
        {
            OrcUnit.Died -= OnEnemyDied;
            OrcCampBuilding.OnDestroyed -= OnCampBuildingFell;
            if (GameOverManager.Instance != null) GameOverManager.Instance.OnGameEnded -= OnGameEnded;
        }

        private void Start()
        {
            if (GameOverManager.Instance != null) GameOverManager.Instance.OnGameEnded += OnGameEnded;
        }

        private void Update()
        {
            if (Finished || _waves == null) return;
            if (GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver) return;
            RunSeconds += Time.deltaTime;
        }

        /// <summary>«m:ss», or «h:mm:ss» past an hour.</summary>
        public static string Clock(float seconds)
        {
            var s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return s >= 3600 ? $"{s / 3600}:{s / 60 % 60:00}:{s % 60:00}" : $"{s / 60}:{s % 60:00}";
        }

        private void OnEnemyDied(OrcUnit enemy)
        {
            if (Finished || enemy == null || enemy.Kind != OrcKind.Giant) return;
            BossesSlain++;
        }

        private void OnCampBuildingFell(OrcCampBuilding building)
        {
            if (Finished || building == null || !Scores(building.Kind)) return;
            CampBuildingsRazed++;
        }

        private void OnGameEnded(bool victory) => Finish();

        /// <summary>Ends the run and writes it down; the second and later calls do nothing. The end panel calls it too, whichever hears of the end first.</summary>
        public void Finish()
        {
            if (Finished) return;
            Finished = true;
            InvasionRecords.Record(Total, DateTime.Now, BalanceConfig.Instance.InvasionRunsCountedPerDay);
        }
    }
}
