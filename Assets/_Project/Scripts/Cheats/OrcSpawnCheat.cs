using CityBuilder.Combat;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Cheats
{
    /// <summary>
    /// Testing tool: spawns orc squads on demand instead of waiting out the 90-second raid clock.
    /// Configure it in the Inspector on GameCheats/OrcSpawn and press the Spawn button (added by
    /// OrcSpawnCheatEditor) during Play, or arm it to fire automatically on a given day.
    ///
    /// Conflict handling with the real raid system was called out explicitly: while
    /// suspendNormalRaids is on, OrcRaidManager's automatic waves are paused (see its
    /// RaidsSuspended) so hand-spawned squads can be watched in isolation. Portal placement is NOT
    /// paused -- the cheat needs a portal to aim at. Turning the flag off, or disabling this
    /// component, hands control straight back.
    ///
    /// Event-driven, with no Update of its own: this ships in builds like everything else in the
    /// generated scene, so it must cost nothing per frame when idle.
    /// </summary>
    public class OrcSpawnCheat : MonoBehaviour
    {
        public enum SpawnMoment
        {
            /// <summary>Only when the Inspector button is pressed.</summary>
            ButtonOnly,
            /// <summary>Also fires once automatically when the calendar reaches spawnOnDay.</summary>
            OnDay
        }

        [Header("Squad")]
        [Tooltip("Сколько орков заспавнить за одно срабатывание.")]
        [SerializeField, Min(1)] private int orcCount = 3;

        [Tooltip("Уровень орков. Здоровье и урон растут линейно: уровень 3 = 60 хп и 12 урона вместо 20 и 4.")]
        [SerializeField, Min(1)] private int orcLevel = 1;

        [Header("Место")]
        [Tooltip("Индекс портала из OrcPortal.All. Сейчас портал на карте один, так что 0.")]
        [SerializeField, Min(0)] private int portalIndex;

        [Header("Момент")]
        [SerializeField] private SpawnMoment spawnMoment = SpawnMoment.ButtonOnly;

        [Tooltip("День, на котором сработает спавн, если выбран режим OnDay.")]
        [SerializeField, Min(1)] private int spawnOnDay = 3;

        [Header("Конфликты")]
        [Tooltip("Пока включено, обычные набеги по таймеру не идут -- спавнятся только орки из этого чита.")]
        [SerializeField] private bool suspendNormalRaids = true;

        private bool _firedOnDay;

        private void OnEnable()
        {
            ApplyRaidSuspension();
            if (GameCalendar.Instance != null) GameCalendar.Instance.OnDayPassed += HandleDayPassed;
        }

        private void OnDisable()
        {
            if (GameCalendar.Instance != null) GameCalendar.Instance.OnDayPassed -= HandleDayPassed;
            // Never leave the real raid system switched off behind us.
            if (OrcRaidManager.Instance != null) OrcRaidManager.Instance.RaidsSuspended = false;
        }

        /// <summary>Also called from the custom Inspector so toggling the checkbox mid-play takes effect immediately.</summary>
        public void ApplyRaidSuspension()
        {
            if (OrcRaidManager.Instance != null) OrcRaidManager.Instance.RaidsSuspended = suspendNormalRaids;
        }

        /// <summary>Spawns the configured squad right now. Bound to the Inspector's Spawn button.</summary>
        public void SpawnNow()
        {
            var raidManager = OrcRaidManager.Instance;
            if (raidManager == null)
            {
                Debug.LogWarning("OrcSpawnCheat: нет OrcRaidManager в сцене -- спавн невозможен.");
                return;
            }

            if (!TryResolveSpawnPosition(out var position)) return;

            raidManager.SpawnOrcs(position, orcCount, orcLevel);
            EventLogManager.Instance?.Log($"[Чит] Заспавнено орков: {orcCount} (ур. {orcLevel})");
        }

        private bool TryResolveSpawnPosition(out Vector3 position)
        {
            var portals = OrcPortal.All;
            if (portals.Count == 0)
            {
                // The portal only appears once the Town Hall is placed (see OrcRaidManager), so
                // this is the expected message when cheating before the game has really started.
                Debug.LogWarning("OrcSpawnCheat: на карте нет ни одного портала. Портал появляется после постановки Ратуши.");
                position = default;
                return false;
            }

            var index = Mathf.Clamp(portalIndex, 0, portals.Count - 1);
            position = portals[index].transform.position;
            return true;
        }

        private void HandleDayPassed()
        {
            if (spawnMoment != SpawnMoment.OnDay || _firedOnDay) return;
            if (GameCalendar.Instance == null || GameCalendar.Instance.CurrentDay < spawnOnDay) return;

            _firedOnDay = true;
            SpawnNow();
        }
    }
}
