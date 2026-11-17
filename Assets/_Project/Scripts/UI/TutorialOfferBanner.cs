using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Resources;
using CityBuilder.Tutorial;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// «Обучение» over a paused sandbox game (the user, 2026-11-17; art Tools/ui/tutorial_offer_concept.py, card
    /// Design/features/tutorial.md «In-game offer»): a player who skipped the tutorial and looks lost in the first
    /// minutes is offered it. Only in the sandbox, only while the tutorial was never won, once per world
    /// (TutorialOffer.Clock), never again after «Позже» tutorial_offer_max_declines times. Inside the first
    /// tutorial_offer_window_sec of play the FIRST of these opens it:
    ///   1. no Town Hall tutorial_offer_hall_sec after the start;
    ///   2. the start chain finished, then nothing new placed for tutorial_offer_stall_sec;
    ///   3. no touch at all for tutorial_offer_idle_sec;
    ///   4. tutorial_offer_raid_sec before the first raid with no Barracks and no soldier;
    ///   5. wood or stone short (under tutorial_offer_low_res, or a build refused for it) and neither has grown
    ///      for tutorial_offer_no_gain_sec.
    /// It waits for a free screen (nothing in hand, no window, the game running). Seconds are real play seconds.
    /// </summary>
    public class TutorialOfferBanner : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private UnityEngine.UI.Text rewardText;

        public static TutorialOfferBanner Instance { get; private set; }

        private BuildingPlacer _placer;
        private float _timeScale = 1f;
        private float _idle;
        private float _noHall;
        private float _stall;
        private float _noGain;
        private int _buildings = -1;
        private int _wood = -1;
        private int _stone = -1;
        private bool _raidCame;
        private float _raidTimer = -1f;
        private Vector3 _mouse;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            Instance = this;
            if (root != null) root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private static bool Eligible =>
            TutorialOffer.Clock >= 0f && PiggyTasks.InSandbox && !Dungeon.DungeonMode.IsActive && !TutorialOffer.TutorialDone
            && TutorialOffer.Declined < BalanceConfig.Instance.TutorialOfferMaxDeclines;

        private void Update()
        {
            if (IsOpen || BalanceConfig.Instance == null || !Eligible) return;
            if (Time.timeScale <= 0f) return;   // paused: nothing counts
            var config = BalanceConfig.Instance;
            var dt = Time.unscaledDeltaTime;
            TutorialOffer.Clock += dt;
            if (TutorialOffer.Clock > config.TutorialOfferWindowSeconds)
            {
                TutorialOffer.Clock = -1f;   // the window is over: this world will not ask
                return;
            }
            if (_placer == null) _placer = FindAnyObjectByType<BuildingPlacer>();

            // 3: any touch, click, key or mouse move resets the idle clock
            var touched = Input.anyKey || Input.touchCount > 0 || Input.mouseScrollDelta.sqrMagnitude > 0f
                          || (Input.mousePosition - _mouse).sqrMagnitude > 4f;
            _mouse = Input.mousePosition;
            _idle = touched ? 0f : _idle + dt;

            var hall = BuildingInstance.TownHall != null;
            // 1: no Town Hall yet
            _noHall = hall ? 0f : _noHall + dt;

            // 2: the chain done, then nothing new placed (any new building -- roads included -- resets it)
            var chainDone = hall && _placer != null && !_placer.IsPlacingMandatoryBuilding;
            var count = BuildingInstance.All.Count;
            if (count > _buildings || !chainDone) _stall = 0f;
            else _stall += dt;
            _buildings = count;

            // 5: wood or stone short and neither growing
            var resources = ResourceManager.Instance;
            var short_ = false;
            if (resources != null)
            {
                var wood = resources.GetAmount(ResourceType.Wood);
                var stone = resources.GetAmount(ResourceType.Stone);
                if (wood > _wood || stone > _stone) _noGain = 0f;
                else _noGain += dt;
                _wood = wood;
                _stone = stone;
                var low = config.TutorialOfferLowResource;
                var refused = _placer != null && Time.unscaledTime - _placer.ShortOfMaterialsAt < config.TutorialOfferNoGainSeconds;
                short_ = chainDone && (wood < low || stone < low || refused);
            }

            // 4: the first raid close and nothing to meet it
            var raids = OrcRaidManager.Instance;
            // the raid timer jumping UP means a raid has just been sent (the next interval started)
            if (raids != null)
            {
                if (_raidTimer >= 0f && raids.SecondsUntilNextRaid > _raidTimer + 1f) _raidCame = true;
                _raidTimer = raids.SecondsUntilNextRaid;
            }
            var raidSoon = !_raidCame && raids != null && !WorldSettings.Current.RaidsOff && hall
                           && raids.SecondsUntilNextRaid > 0f && raids.SecondsUntilNextRaid <= config.TutorialOfferRaidSeconds
                           && !BuildingInstance.HasAny(BuildingIds.Barracks) && SoldierUnit.All.Count == 0;

            var lost = _noHall >= config.TutorialOfferHallSeconds
                       || _stall >= config.TutorialOfferStallSeconds
                       || _idle >= config.TutorialOfferIdleSeconds
                       || raidSoon
                       || (short_ && _noGain >= config.TutorialOfferNoGainSeconds);
            if (!lost) return;
            // a free screen only: nothing in hand (the Town Hall's own placement is the hand at the start), no window
            if (ModalGate.IsBlocked) return;
            if (_placer != null && _placer.IsSelecting && !_placer.IsPlacingTownHall) return;
            Show();
        }

        /// <summary>The banner over a paused game (also the cheats' way to see it).</summary>
        public void Show()
        {
            if (root == null || IsOpen) return;
            _timeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            TutorialOffer.Clock = -1f;   // once per world
            if (rewardText != null) rewardText.text = Localization.Format("#tutorial_offer_reward", BalanceConfig.Instance.TutorialRewardCrystals);
            root.SetActive(true);
            root.transform.SetAsLastSibling();
        }

        /// <summary>«Пройти обучение»: the town saved, the tutorial loads.</summary>
        public void Play()
        {
            if (root != null) root.SetActive(false);
            TutorialOffer.LaunchFromGame();
        }

        /// <summary>«Позже»: the game goes on; after tutorial_offer_max_declines of these it never asks again.</summary>
        public void Later()
        {
            TutorialOffer.NoteDeclined();
            if (root != null) root.SetActive(false);
            Time.timeScale = _timeScale;
        }
    }
}
