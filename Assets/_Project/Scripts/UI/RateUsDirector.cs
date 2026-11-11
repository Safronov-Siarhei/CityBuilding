using System;
using CityBuilder.Buildings;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CityBuilder.UI
{
    /// <summary>
    /// WHEN the rate-us window comes up (the user, 2026-11-11; card Design/features/store-listing.md):
    /// on a happy EVENT (`Note`, each switchable on the sheet: rateus_ev_&lt;key&gt;), never in the tutorial,
    /// a fight, placement or over another window. Not rated yet: first after rateus_first_min of play,
    /// then every rateus_gap_min of play; with no event for rateus_fallback_min, at the next calm moment.
    /// After «Оценить»: once a day, rateus_rated_session_min into a session (its «Позже» says «Уже
    /// оценил», which keeps that rhythm). Events from the menu side (an Invasion record, a campaign
    /// mission, a login day, the daily bonus's big days) wait for the main menu. Play time counts only
    /// while the app is in front. Also puts up the daily login bonus (DailyBonusWindow) at the day's
    /// first calm moment in the main menu. Lives for the whole app (made at start).
    /// </summary>
    public sealed class RateUsDirector : MonoBehaviour
    {
        private const string SincePref = "rate.play_since";
        private const string ShowsPref = "rate.shows";
        private const string ShownDayPref = "rate.shown_day";
        private const string MenuPendingPref = "rate.menu_pending";
        private const string LastDayPref = "rate.last_day";
        private const string DaysPref = "rate.days";

        private static RateUsDirector _instance;

        private float _since;          // play seconds since the last show (kept in PlayerPrefs)
        private float _session;        // play seconds this session
        private float _saveIn;
        private float _pendingUntil = -1f;
        private float _calmFor;
        private float _dailyCalm;
        private bool _menuScene;
        private Watch _watch;
        private BuildingPlacer _placer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (_instance != null) return;
            var go = new GameObject("RateUsDirector");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<RateUsDirector>();
        }

        /// <summary>
        /// A happy moment. `menu`: shown when the player is back in the main menu (a run's record, a
        /// mission done) rather than over the scene's own end screen.
        /// </summary>
        public static void Note(string key, bool menu = false)
        {
            if (_instance == null || string.IsNullOrEmpty(key) || Tutorial.TutorialMode.IsActive) return;
            var config = BalanceConfig.Instance;
            if (config != null && !config.RateUsEventOn(key)) return;
            if (menu)
            {
                PlayerPrefs.SetInt(MenuPendingPref, 1);
                return;
            }
            if (!_instance.Due()) return;
            _instance._pendingUntil = Time.unscaledTime + (config != null ? config.RateUsWaitSeconds : 120f);
        }

        private void Awake()
        {
            _since = PlayerPrefs.GetFloat(SincePref, 0f);
            CountLoginDay();
            SceneManager.sceneLoaded += OnSceneLoaded;
            Combat.OrcCampBuilding.OnDestroyed += Camp;
            Combat.OrcPortal.OnPortalDestroyed += Portal;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Combat.OrcCampBuilding.OnDestroyed -= Camp;
            Combat.OrcPortal.OnPortalDestroyed -= Portal;
            if (_instance == this) _instance = null;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _menuScene = FindAnyObjectByType<MainMenuController>() != null;
            _pendingUntil = -1f;
            _watch = _menuScene ? null : new Watch();
            _placer = _menuScene ? null : FindAnyObjectByType<BuildingPlacer>();
        }

        /// <summary>The second and the seventh calendar day the game is played on.</summary>
        private static void CountLoginDay()
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            if (PlayerPrefs.GetString(LastDayPref, string.Empty) == today) return;
            PlayerPrefs.SetString(LastDayPref, today);
            var days = PlayerPrefs.GetInt(DaysPref, 0) + 1;
            PlayerPrefs.SetInt(DaysPref, days);
            PlayerPrefs.Save();
            if ((days == 2 && On("day2")) || (days == 7 && On("day7"))) PlayerPrefs.SetInt(MenuPendingPref, 1);
        }

        private static bool On(string key) => BalanceConfig.Instance == null || BalanceConfig.Instance.RateUsEventOn(key);

        private static void Camp(Combat.OrcCampBuilding camp)
        {
            if (_instance != null && !_instance._menuScene) Note("orc_camp");
        }

        private static void Portal(Combat.OrcPortal portal)
        {
            if (_instance != null && !_instance._menuScene) Note("orc_portal");
        }

        /// <summary>The time rule allows a window now.</summary>
        private bool Due()
        {
            var config = BalanceConfig.Instance;
            if (config == null) return false;
            if (RateUsWindow.Rated)
                return PlayerPrefs.GetString(ShownDayPref, string.Empty) != DateTime.Now.ToString("yyyy-MM-dd")
                       && _session >= config.RateUsRatedSessionMinutes * 60f;
            var first = PlayerPrefs.GetInt(ShowsPref, 0) == 0;
            return _since >= (first ? config.RateUsFirstMinutes : config.RateUsGapMinutes) * 60f;
        }

        private void Update()
        {
            if (!Application.isFocused) return;
            var dt = Time.unscaledDeltaTime;
            if (!LoadingScreen.Busy)
            {
                _since += dt;
                _session += dt;
            }
            _saveIn -= dt;
            if (_saveIn <= 0f)
            {
                _saveIn = 10f;
                Save();
            }
            var config = BalanceConfig.Instance;
            if (config == null || Tutorial.TutorialMode.IsActive) return;

            _watch?.Tick(dt);

            // the daily login bonus: at the day's first calm moment in the main menu (the user, 2026-11-11)
            if (_menuScene && DailyBonus.Due && _pendingUntil < 0f)
            {
                _dailyCalm = Calm() ? _dailyCalm + dt : 0f;
                if (_dailyCalm >= config.RateUsDelaySeconds)
                {
                    _dailyCalm = 0f;
                    DailyBonusWindow.Show();
                }
            }

            // the menu's own events (a record, a mission, a login day, the big bonus days)
            if (_menuScene && PlayerPrefs.GetInt(MenuPendingPref, 0) == 1 && _pendingUntil < 0f && Due())
            {
                PlayerPrefs.SetInt(MenuPendingPref, 0);
                _pendingUntil = Time.unscaledTime + config.RateUsWaitSeconds;
            }
            // too long without an event: the next calm moment
            if (_pendingUntil < 0f && !RateUsWindow.Rated && config.RateUsFallbackMinutes > 0f
                && _since >= config.RateUsFallbackMinutes * 60f && Due())
                _pendingUntil = Time.unscaledTime + config.RateUsWaitSeconds;

            if (_pendingUntil < 0f) return;
            if (Time.unscaledTime > _pendingUntil)
            {
                _pendingUntil = -1f;
                return;
            }
            _calmFor = Calm() ? _calmFor + dt : 0f;
            if (_calmFor < config.RateUsDelaySeconds) return;
            _pendingUntil = -1f;
            _calmFor = 0f;
            Show();
        }

        /// <summary>Nothing else on the screen and nothing going on: no window, no placing, no fight, no loading.</summary>
        private bool Calm()
        {
            if (RateUsWindow.IsOpen || DailyBonusWindow.IsOpen || LoadingScreen.Busy || ModalGate.IsBlocked) return false;
            if (Shop.OfferShopWindow.IsOpen) return false;
            if (Time.timeScale <= 0f) return false;
            if (_menuScene) return true;
            if (_placer != null && _placer.IsSelecting) return false;
            var over = GameOverManager.Instance;
            if (over != null && over.IsGameOver) return false;
            return _watch == null || !_watch.Fighting;
        }

        private void Show()
        {
            RateUsWindow.Show();
            _since = 0f;
            PlayerPrefs.SetInt(ShowsPref, PlayerPrefs.GetInt(ShowsPref, 0) + 1);
            PlayerPrefs.SetString(ShownDayPref, DateTime.Now.ToString("yyyy-MM-dd"));
            Save();
        }

        private void Save()
        {
            PlayerPrefs.SetFloat(SincePref, _since);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// The game's happy moments that have no event of their own, sampled once a second; the first
        /// sample of a scene only sets the baseline (a loaded save is not an achievement).
        /// </summary>
        private sealed class Watch
        {
            private float _in;
            private bool _primed;
            private int _research, _people, _heroes, _heroLevels, _ships, _caches, _raiders;
            private bool _botRazed, _victory;
            private Diplomacy.BotStance _stance;

            public bool Fighting => _raiders > 0;

            public void Tick(float dt)
            {
                _in -= dt;
                if (_in > 0f) return;
                _in = 1f;

                var research = Research.ResearchManager.Instance != null ? Research.ResearchManager.Instance.CompletedCount : 0;
                var people = Citizens.CitizenManager.Instance != null ? Citizens.CitizenManager.Instance.TotalPopulation : 0;
                var roster = Combat.HeroRoster.Instance;
                var heroes = roster != null ? roster.Heroes.Count : 0;
                var heroLevels = 0;
                if (roster != null) foreach (var h in roster.Heroes) if (h != null) heroLevels += h.Level;
                var ships = Ships.ShipManager.Instance != null ? Ships.ShipManager.Instance.Ships.Count : 0;
                var caches = Maps.Archipelago.TideCaches.Looted.Count;
                var raiders = 0;
                foreach (var orc in Combat.OrcUnit.All) if (orc != null && orc.IsRaiding) raiders++;
                var bot = Bot.BotKingdom.Instance;
                var botRazed = bot != null && bot.HallRazed;
                var stance = Diplomacy.BotRelations.Stance;
                var over = GameOverManager.Instance;
                var victory = over != null && over.IsVictory;

                if (_primed)
                {
                    if (research > _research) Note("research");
                    var steps = BalanceConfig.Instance != null ? BalanceConfig.Instance.RateUsPeopleSteps : null;
                    if (steps != null)
                        foreach (var step in steps)
                            if (step > 0 && _people < step && people >= step) Note("people");
                    if (heroes > _heroes) Note("hero_bought");
                    else if (heroLevels > _heroLevels) Note("hero_level");
                    if (ships > _ships) Note("ship");
                    if (caches > _caches) Note("sea_cache");
                    if (_raiders > 0 && raiders == 0 && BuildingInstance.TownHall != null) Note("raid_repelled");
                    if (botRazed && !_botRazed) Note("bot_destroyed");
                    if (stance != _stance && (stance == Diplomacy.BotStance.Trade || _stance == Diplomacy.BotStance.War)) Note("bot_peace");
                    if (victory && !_victory) Note("victory");
                }
                _primed = true;
                _research = research;
                _people = Mathf.Max(_people, people);
                _heroes = heroes;
                _heroLevels = heroLevels;
                _ships = ships;
                _caches = caches;
                _raiders = raiders;
                _botRazed = botRazed;
                _stance = stance;
                _victory = victory;
            }
        }
    }
}
