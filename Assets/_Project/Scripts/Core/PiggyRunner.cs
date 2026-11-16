using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using CityBuilder.Combat;

namespace CityBuilder.Core
{
    /// <summary>
    /// Keeps the piggy bank filling (card Design/features/piggy-bank.md): a crystal (piggy_minute_crystals) for every
    /// piggy_minute_seconds of ACTIVE play -- the game scene, not paused, no window over it, a touch/click/key within
    /// piggy_idle_seconds -- and the play-minute task; listens for fallen orcs for the tasks that count them. Lives
    /// across scenes, made once at start.
    /// </summary>
    public class PiggyRunner : MonoBehaviour
    {
        private const string GameScene = "CityBuilder";

        private float _played;
        private float _idle;
        private float _flushIn = 5f;
        private Invasion.InvasionMode _run;
        private int _kingKills;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindAnyObjectByType<PiggyRunner>() != null) return;
            var go = new GameObject("PiggyRunner");
            DontDestroyOnLoad(go);
            go.AddComponent<PiggyRunner>();
        }

        private void OnEnable() => OrcUnit.Died += OnOrcDied;

        private void OnDisable() => OrcUnit.Died -= OnOrcDied;

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit() => Save();

        private static void Save()
        {
            PiggyTasks.Flush();
            PlayerPrefs.Save();
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            _flushIn -= dt;
            if (_flushIn <= 0f)
            {
                _flushIn = 5f;
                PiggyTasks.Flush();
            }
            if (Touched()) _idle = 0f; else _idle += dt;
            if (!Active()) return;
            _played += dt;
            var config = BalanceConfig.Sandbox;
            var minute = config != null ? Mathf.Max(1f, config.PiggyMinuteSeconds) : 60f;
            if (_played < minute) return;
            _played -= minute;
            PiggyBank.Add(config != null ? config.PiggyMinuteCrystals : 1);
            if (PiggyTasks.InSandbox && !Dungeon.DungeonMode.IsActive)
            {
                PiggyTasks.Report(PiggyTasks.SandboxMinute);
                var map = Maps.MeshMapApplier.Instance != null ? Maps.MeshMapApplier.Instance.CurrentMapId : null;
                if (Maps.Archipelago.ArchipelagoMapId.IsArchipelago(map)) PiggyTasks.MarkSeen(PiggyTasks.ModeArchipelago);
            }
        }

        private bool Active()
        {
            if (Time.timeScale <= 0f || ModalGate.IsBlocked) return false;
            var config = BalanceConfig.Sandbox;
            if (_idle > (config != null ? config.PiggyIdleSeconds : 120f)) return false;
            return SceneManager.GetActiveScene().name == GameScene;
        }

        private static bool Touched()
        {
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed) return true;
            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed || mouse.scroll.ReadValue().sqrMagnitude > 0f
                                  || mouse.delta.ReadValue().sqrMagnitude > 0f)) return true;
            var keys = Keyboard.current;
            return keys != null && keys.anyKey.isPressed;
        }

        private void OnOrcDied(OrcUnit orc)
        {
            if (orc == null) return;
            var kind = orc.Kind;
            if (Dungeon.DungeonMode.IsActive)
            {
                if (OrcUnit.IsMonster(kind)) PiggyTasks.Report(PiggyTasks.DungeonMonster);
                if (OrcUnit.IsBossKind(kind)) PiggyTasks.Report(PiggyTasks.DungeonBoss);
                if (OrcUnit.IsMonster(kind)) return;
            }
            if (OrcUnit.IsMonster(kind)) return;
            PiggyTasks.Report(PiggyTasks.OrcKill);
            // the King's own kills, counted per run (a new run = a new InvasionMode switched on)
            var run = Invasion.InvasionMode.IsActive ? Invasion.InvasionMode.Instance : null;
            if (run != null)
            {
                if (run != _run) { _run = run; _kingKills = 0; }
                var king = run.King != null ? run.King.Unit : null;
                if (king != null && orc.KilledBy == king) PiggyTasks.ReportMax(PiggyTasks.KingKill, ++_kingKills);
            }
            // a raid repelled: the last raider of it fell
            if (orc.IsRaiding)
            {
                foreach (var other in OrcUnit.All)
                    if (other != null && other != orc && other.IsRaiding && other.CurrentHealth > 0) return;
                PiggyTasks.Report(PiggyTasks.Raid);
            }
        }
    }
}
