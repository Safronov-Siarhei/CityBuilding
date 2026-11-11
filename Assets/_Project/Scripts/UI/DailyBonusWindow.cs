using System.Collections;
using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.Shop;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The daily login bonus's window (the user approved the concept 2026-11-11:
    /// Design/store/DailyBonus_Mockup.png; logic in <see cref="DailyBonus"/>): the offer shop's frame with
    /// «Награда за вход», ten day cards (taken with a tick / today glowing / ahead, days 5 and 10 blue),
    /// one line, «Забрать N💎» and «▶ ×2 2N💎» (a rewarded ad; hidden with no ad ready). Closing it any
    /// other way (the seal, a tap outside) still gives the plain reward: the bonus is never lost by a
    /// misclick. Shown in the main menu at the day's first visit (RateUsDirector). Crystal amounts: the
    /// number LEFT, the icon RIGHT. One prefab in Resources (SetupProjectDailyBonus).
    /// </summary>
    public class DailyBonusWindow : MonoBehaviour
    {
        private const string ResourcePath = "DailyBonusWindow";
        private const float PopSeconds = 0.22f;
        private const float CloseSeconds = 0.15f;
        private const float PopFromScale = 0.85f;
        /// <summary>How long today's card shows its new tick before the window goes.</summary>
        private const float TickSeconds = 0.6f;

        [System.Serializable]
        public class DayView
        {
            public Image card;
            public Image crystal;
            public Image crystalBig;
            public Image tick;
            public Text amount;
            public Text day;
        }

        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform window;
        [SerializeField] private Button shade;
        [SerializeField] private Button close;
        [SerializeField] private Button take;
        [SerializeField] private Button doubled;
        [SerializeField] private Text takeAmount;
        [SerializeField] private Text doubleAmount;
        [SerializeField] private Text doubleLabel;
        [SerializeField] private Text counterText;
        [SerializeField] private Image picture;
        [SerializeField] private float size = 1f;
        [SerializeField] private Sprite cardTaken, cardToday, cardAhead, cardBig, cardBigToday;
        [SerializeField] private DayView[] days;

        private static DailyBonusWindow _current;
        private readonly System.Collections.Generic.List<Canvas> _hidden = new System.Collections.Generic.List<Canvas>();
        private float _t;
        private bool _closing;
        private bool _busy;
        private int _today;

        /// <summary>The window is up.</summary>
        public static bool IsOpen => _current != null;

        /// <summary>Puts the window up if today's bonus waits (nothing if it is up, taken, or the prefab is missing).</summary>
        public static void Show()
        {
            if (_current != null || !DailyBonus.Due) return;
            var prefab = UnityEngine.Resources.Load<DailyBonusWindow>(ResourcePath);
            if (prefab == null) return;
            _current = Instantiate(prefab);
        }

        private void Awake()
        {
            shade.onClick.AddListener(() => Claim(false));
            close.onClick.AddListener(() => Claim(false));
            take.onClick.AddListener(() => Claim(false));
            doubled.onClick.AddListener(WatchAd);
            group.alpha = 0f;
            window.localScale = Vector3.one * (PopFromScale * size);
            HideTheInterfaceBehind();
            if (picture != null) picture.sprite = UnityEngine.Resources.Load<Sprite>("Shop/Window_" + ThemeId());

            _today = DailyBonus.TodayIndex;
            for (var i = 0; i < days.Length; i++) Draw(i, i < _today);
            var reward = DailyBonus.Reward(_today);
            var mult = BalanceConfig.Sandbox.DailyBonusAdMultiplier;
            takeAmount.text = reward.ToString();
            doubleAmount.text = Mathf.RoundToInt(reward * mult).ToString();
            doubleLabel.text = Localization.Format("#daily_double", mult.ToString("0.#"));
            // no ad to show: only «Забрать», in the middle (the shop's rule: no fill, no ad button)
            if (!RewardedAds.IsReady)
            {
                doubled.gameObject.SetActive(false);
                var rt = (RectTransform)take.transform;
                rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y);
            }
            Wallet.UpgradeCrystalsChanged += Count;
            Count();
        }

        private void Draw(int i, bool taken)
        {
            var view = days[i];
            var big = i == 4 || i == DailyBonus.Days - 1;
            var today = i == _today && !taken;
            view.card.sprite = taken ? cardTaken : today ? (big ? cardBigToday : cardToday) : big ? cardBig : cardAhead;
            view.crystal.gameObject.SetActive(!big);
            view.crystalBig.gameObject.SetActive(big);
            var dim = new Color(1f, 1f, 1f, taken ? 0.5f : 1f);
            view.crystal.color = view.crystalBig.color = dim;
            view.tick.gameObject.SetActive(taken);
            view.amount.text = DailyBonus.Reward(i).ToString();
            view.amount.color = taken ? new Color(0.78f, 0.73f, 0.63f) : new Color(1f, 0.965f, 0.87f);
            view.day.text = Localization.Format("#daily_day", i + 1);
        }

        private void Count() => counterText.text = Wallet.UpgradeCrystals.ToString();

        private static string ThemeId()
        {
            switch (SeasonStyle.Current)
            {
                case MapSeason.Spring: return MapThemes.Id(MapTheme.Spring);
                case MapSeason.Autumn: return MapThemes.Id(MapTheme.Autumn);
                case MapSeason.Winter: return MapThemes.Id(MapTheme.Winter);
                default: return MapThemes.Id(MapTheme.Summer);
            }
        }

        private void WatchAd()
        {
            if (_busy || _closing || !RewardedAds.IsReady) return;
            StartCoroutine(PlayAd());
        }

        /// <summary>The rewarded ad's stand-in, as the shop's: a few real seconds, then the doubled reward.</summary>
        private IEnumerator PlayAd()
        {
            _busy = true;
            take.interactable = doubled.interactable = close.interactable = false;
            yield return new WaitForSecondsRealtime(BalanceConfig.Sandbox.ShopAdStubSeconds);
            _busy = false;
            Claim(true);
        }

        private void Claim(bool twice)
        {
            if (_closing || _busy) return;
            DailyBonus.Claim(twice);
            StartCoroutine(TickAndClose());
        }

        private IEnumerator TickAndClose()
        {
            _busy = true;
            group.interactable = false;
            Draw(_today, true);
            yield return new WaitForSecondsRealtime(TickSeconds);
            _busy = false;
            _closing = true;
            _t = 0f;
            ShowTheInterfaceBehind();
        }

        private void HideTheInterfaceBehind()
        {
            var own = GetComponent<Canvas>();
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (canvas == own || !canvas.enabled || !canvas.isRootCanvas) continue;
                if (canvas.renderMode == RenderMode.WorldSpace || canvas.sortingOrder > own.sortingOrder) continue;
                canvas.enabled = false;
                _hidden.Add(canvas);
            }
        }

        private void ShowTheInterfaceBehind()
        {
            foreach (var canvas in _hidden)
                if (canvas != null) canvas.enabled = true;
            _hidden.Clear();
        }

        private void Update()
        {
            _t += Time.unscaledDeltaTime;
            if (_closing)
            {
                group.alpha = 1f - Mathf.Clamp01(_t / CloseSeconds);
                if (_t >= CloseSeconds) Destroy(gameObject);
                return;
            }
            var k = Mathf.Clamp01(_t / PopSeconds);
            group.alpha = k;
            var back = 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f);
            window.localScale = Vector3.one * (size * Mathf.LerpUnclamped(PopFromScale, 1f, back));
        }

        private void OnDestroy()
        {
            Wallet.UpgradeCrystalsChanged -= Count;
            ShowTheInterfaceBehind();
            if (_current != this) return;
            _current = null;
            UnityEngine.Resources.UnloadUnusedAssets();
        }
    }
}
