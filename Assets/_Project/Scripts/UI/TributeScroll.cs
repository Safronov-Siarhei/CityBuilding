using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The orcs' tribute (the user, 2026-11-22; concept C round 2, Design/concepts/tribute/Tribute_C1_Open.png):
    /// tribute_seconds before a table wave sails (OrcRaidManagerWaves) the chief's scroll opens -- the chief
    /// facing the player to the scroll's height, what he wants (up to five resources: need / have), the time
    /// to the attack, «Заплатить» (all or nothing; grey while the stores lack any of it), «Биться!» (the
    /// fleet sails at once), «Отложить» -- or the cross, the same -- rolls it into a badge under the resource
    /// bar's middle, its gold ring the time left; a tap opens it again. Side by side with a booster's badge
    /// (the scroll left, the booster right). The game is not paused.
    /// </summary>
    public class TributeScroll : MonoBehaviour
    {
        [System.Serializable]
        public class Row
        {
            public GameObject root;
            public Image plate;
            public Image icon;
            public Text name;
            public Text need;
            public Text have;
        }

        [SerializeField] private GameObject window;
        [SerializeField] private Text title;
        [SerializeField] private Text threat;
        [SerializeField] private Text attackIn;
        [SerializeField] private Text timer;
        [SerializeField] private Row[] rows;
        [SerializeField] private Button pay;
        [SerializeField] private Image payImage;
        [SerializeField] private Text payLabel;
        [SerializeField] private Button fight;
        [SerializeField] private Button later;
        [SerializeField] private Button close;
        [SerializeField] private RectTransform badge;
        [SerializeField] private Image badgeRing;
        [SerializeField] private Text badgeTimer;
        [SerializeField] private Button badgeButton;
        // a picture per ResourceType, by its number (SetupProject fills it)
        [SerializeField] private Sprite[] resourceIcons;

        private static readonly Color Enough = new Color(0.91f, 0.82f, 0.64f);
        private static readonly Color Short = new Color(0.94f, 0.77f, 0.69f);
        private static readonly Color HaveOk = new Color(0.31f, 0.43f, 0.16f);
        private static readonly Color HaveShort = new Color(0.69f, 0.16f, 0.13f);

        private enum State { Hidden, Open, Badge }

        private State _state;
        private bool _wasDue;
        private float _refresh;
        private float _ringFull = 150f;
        private Vector2 _home;

        public static TributeScroll Instance { get; private set; }

        /// <summary>The badge is up (BoosterHints moves its own right of it).</summary>
        public static bool BadgeShown => Instance != null && Instance._state == State.Badge;

        /// <summary>The badge's width (the pair's spacing).</summary>
        public static float BadgeWidth => Instance != null && Instance.badge != null ? Instance.badge.sizeDelta.x : 0f;

        private void Awake()
        {
            Instance = this;
            if (badge != null) _home = badge.anchoredPosition;
            pay.onClick.AddListener(Pay);
            fight.onClick.AddListener(Fight);
            later.onClick.AddListener(Later);
            if (close != null) close.onClick.AddListener(Later);
            if (badgeButton != null) badgeButton.onClick.AddListener(Open);
            Show(State.Hidden);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_state == State.Open) ModalGate.SetBlocked(false);
        }

        private void Update()
        {
            var raids = OrcRaidManager.Instance;
            var due = raids != null && raids.TributeDue;
            if (!due)
            {
                if (_state != State.Hidden) Show(State.Hidden);
                _wasDue = false;
                return;
            }
            if (!_wasDue)
            {
                _wasDue = true;
                _ringFull = Mathf.Max(1f, BalanceConfig.Instance.TributeSeconds);
                // a window already over the game: straight into the badge
                Show(ModalGate.IsBlocked ? State.Badge : State.Open);
            }
            var left = raids.SecondsToSail;
            if (_state == State.Open)
            {
                timer.text = Clock(left);
                _refresh -= Time.unscaledDeltaTime;
                if (_refresh <= 0f)
                {
                    _refresh = 0.5f;
                    Fill(raids.NextWaveRow, raids.NextWaveNumber);
                }
            }
            else if (_state == State.Badge)
            {
                if (badgeRing != null) badgeRing.fillAmount = Mathf.Clamp01(left / _ringFull);
                if (badgeTimer != null) badgeTimer.text = Clock(left);
                badge.anchoredPosition = BadgeHome();
            }
        }

        /// <summary>Under the resource bar's middle; left of the running boosters' row, or of a booster's badge.</summary>
        private Vector2 BadgeHome()
        {
            var row = Shop.BoosterHud.RowWidth;
            var w = badge.sizeDelta.x;
            if (row > 0f) return _home - new Vector2(row * 0.5f + w * 0.5f + 16f, 0f);
            var hints = Shop.BoosterHints.Instance;
            return hints != null && hints.Showing != null ? _home - new Vector2(w * 0.5f + 12f, 0f) : _home;
        }

        private static string Clock(float seconds)
        {
            var s = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            return $"{s / 60}:{s % 60:00}";
        }

        private void Fill(BalanceConfig.OrcWaveRow row, int wave)
        {
            if (row == null) return;
            title.text = Localization.Format("#tribute_title", Localization.Get("#tribute_chief"));
            threat.text = Localization.Format("#tribute_threat", wave);
            attackIn.text = Localization.Get("#tribute_attack_in");
            var resources = ResourceManager.Instance;
            for (var i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                var on = i < row.tribute.Count;
                r.root.SetActive(on);
                if (!on) continue;
                var cost = row.tribute[i];
                var have = resources != null ? resources.GetAmount(cost.type) : 0;
                var ok = have >= cost.amount;
                var index = (int)cost.type;
                r.icon.sprite = resourceIcons != null && index < resourceIcons.Length ? resourceIcons[index] : null;
                r.name.text = ResourceNames.Of(cost.type);
                r.need.text = cost.amount.ToString();
                r.have.text = "/ " + have;
                r.have.color = ok ? HaveOk : HaveShort;
                r.plate.color = ok ? Enough : Short;
            }
            var can = OrcRaidManager.CanPay(row);
            pay.interactable = can;
            if (payImage != null) payImage.color = can ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
            if (payLabel != null) payLabel.text = Localization.Get("#tribute_pay");
        }

        private void Show(State state)
        {
            var wasOpen = _state == State.Open;
            _state = state;
            window.SetActive(state == State.Open);
            badge.gameObject.SetActive(state == State.Badge);
            if (state == State.Open)
            {
                transform.SetAsLastSibling();
                _refresh = 0f;
                var raids = OrcRaidManager.Instance;
                if (raids != null) Fill(raids.NextWaveRow, raids.NextWaveNumber);
                timer.text = raids != null ? Clock(raids.SecondsToSail) : string.Empty;
            }
            if (state == State.Badge) badge.anchoredPosition = BadgeHome();
            if (state == State.Open && !wasOpen) ModalGate.SetBlocked(true);
            else if (state != State.Open && wasOpen) ModalGate.SetBlocked(false);
        }

        private void Open() => Show(State.Open);

        private void Later() => Show(State.Badge);

        private void Pay()
        {
            var raids = OrcRaidManager.Instance;
            if (raids != null && raids.PayTribute()) Show(State.Hidden);
        }

        private void Fight()
        {
            OrcRaidManager.Instance?.FightNow();
            Show(State.Hidden);
        }
    }
}
