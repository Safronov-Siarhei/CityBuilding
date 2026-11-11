using CityBuilder.Core;
using CityBuilder.Saving;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The modes screen as a hall of banners (the user, 2026-11-10, «Зал знамён»): one banner per
    /// mode -- Tutorial, Sandbox, Invasion, Campaign -- in the HUD's Classic kit
    /// (Tools/ui/make_mode_banners.py). A tap picks a banner (bigger, swaying, the rest dimmed); a
    /// tap on the picked one or «Играть» starts it; a long press opens the mode's few words. The
    /// banners unroll when the screen comes up and each carries live lines: the tutorial's reward,
    /// the sandbox's saves, the invasion's points today; the campaign is «Скоро».
    /// </summary>
    public class ModeBannersController : MonoBehaviour
    {
        public const int Tutorial = 0, Sandbox = 1, Invasion = 2, Campaign = 3;

        private const string PickedKey = "menu.mode";
        private const float PickedScale = 1.14f;
        private const float Ease = 12f;
        private const float UnrollSeconds = 0.38f;
        private const float UnrollStagger = 0.07f;
        // the gap between the reward's number and its crystal
        private const float RewardIconGap = 6f;

        [SerializeField] private MainMenuController menu;
        [SerializeField] private CanvasGroup screen;
        [SerializeField] private RectTransform[] banners = new RectTransform[0];
        [SerializeField] private Image[] cloths = new Image[0];
        [SerializeField] private Text motto;
        [SerializeField] private Text[] lines = new Text[0];
        [SerializeField] private Image playButton;
        /// <summary>The upgrade crystal after the tutorial's reward number (the user, 2026-11-10: an icon, not the word); hidden once rewarded.</summary>
        [SerializeField] private Image rewardIcon;
        /// <summary>The reward's number, on its own line under «Награда:», the crystal right of it, the pair centred (the user, 2026-11-10).</summary>
        [SerializeField] private Text rewardAmount;

        private static readonly string[] MottoKeys = { "#banner_motto_tutorial", "#banner_motto_sandbox", "#banner_motto_invasion", "#banner_motto_campaign" };
        private const string CampaignScene = "CampaignMap";
        private static readonly Color Dimmed = new Color(0.72f, 0.72f, 0.72f, 1f);

        private int _picked = Sandbox;
        private float[] _scale;
        private bool _shown;
        private float _shownAt;

        private void Awake()
        {
            _picked = Mathf.Clamp(PlayerPrefs.GetInt(PickedKey, Sandbox), Tutorial, Campaign);
            _scale = new float[banners.Length];
            for (var i = 0; i < _scale.Length; i++) _scale[i] = 1f;
        }

        private void Update()
        {
            // the screen coming up: the live lines read afresh, the banners unroll from their perch
            // from the first faded-in frame (was alpha > 0.5: the banners showed whole while the screen
            // faded in, then folded and unrolled -- the user, 2026-11-10: «исчезают и снова открываются»)
            var visible = screen != null && screen.alpha > 0.01f;
            if (visible && !_shown)
            {
                _shownAt = Time.unscaledTime;
                Refresh();
            }
            _shown = visible;
            if (!visible)
            {
                // rolled up while away, so the next coming up starts from the perch
                for (var i = 0; i < banners.Length; i++)
                    if (banners[i] != null) banners[i].localScale = new Vector3(_scale[i], _scale[i] * 0.02f, 1f);
                return;
            }

            var k = 1f - Mathf.Exp(-Ease * Time.unscaledDeltaTime);
            var now = Time.unscaledTime;
            for (var i = 0; i < banners.Length; i++)
            {
                if (banners[i] == null) continue;
                var picked = i == _picked;
                _scale[i] = Mathf.Lerp(_scale[i], picked ? PickedScale : 1f, k);
                var unroll = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((now - _shownAt - i * UnrollStagger) / UnrollSeconds));
                banners[i].localScale = new Vector3(_scale[i], _scale[i] * Mathf.Max(0.02f, unroll), 1f);
                // the cloth sways from its perch, the picked one more
                var sway = Mathf.Sin(now * (picked ? 1.6f : 1.1f) + i * 1.7f) * (picked ? 1.8f : 0.7f);
                banners[i].localRotation = Quaternion.Euler(0f, 0f, sway);
                if (cloths.Length > i && cloths[i] != null)
                    cloths[i].color = Color.Lerp(cloths[i].color, picked ? Color.white : Dimmed, k);
            }
        }

        /// <summary>A tap on a banner: picks it, or starts it when it is picked already.</summary>
        public void TapBanner(int index)
        {
            var press = banners.Length > index && banners[index] != null ? banners[index].GetComponent<LongPress>() : null;
            if (press != null && press.ConsumeFired()) return;
            if (index == _picked) Play();
            else Pick(index);
        }

        /// <summary>A long press: the mode's few words (the sandbox's and the invasion's; the others have none).</summary>
        public void HoldBanner(int index)
        {
            Pick(index);
            if (menu == null) return;
            if (index == Sandbox) menu.ShowInfo((int)GameMode.Sandbox);
            else if (index == Invasion) menu.ShowInfo((int)GameMode.Invasion);
        }

        /// <summary>«Играть»: the picked mode.</summary>
        public void Play()
        {
            if (menu == null) return;
            switch (_picked)
            {
                case Tutorial: menu.StartTutorial(); break;
                case Sandbox: menu.ChooseMode((int)GameMode.Sandbox); break;
                case Invasion: menu.ChooseMode((int)GameMode.Invasion); break;
                // the campaign (the user, 2026-11-12): the loading screen, then the campaign map (pick a level, «Играть»)
                case Campaign: LoadingScreen.ToCampaignMap(CampaignScene); break;
            }
        }

        private void Pick(int index)
        {
            if (index < 0 || index >= banners.Length) return;
            _picked = index;
            // the last mode picked is the one waiting next time
            PlayerPrefs.SetInt(PickedKey, index);
            PlayerPrefs.Save();
            // the picked banner in front of its neighbours
            banners[index].SetAsLastSibling();
            ShowMotto();
        }

        /// <summary>The banners' live lines.</summary>
        private void Refresh()
        {
            ShowMotto();
            var config = BalanceConfig.Instance;
            var rewarded = PlayerPrefs.GetInt("tutorial.rewarded", 0) != 0;
            SetLine(Tutorial, Localization.Get(rewarded ? "#banner_tutorial_done" : "#banner_reward"));
            ShowReward(rewarded ? -1 : config != null ? config.TutorialRewardCrystals : 0);
            var saves = SaveSystem.ListSaves().Count;
            SetLine(Sandbox, saves > 0 ? Localization.Format("#mode_saves_count", saves) : Localization.Get("#banner_no_saves"));
            var counted = config != null ? config.InvasionRunsCountedPerDay : 5;
            var points = CityBuilder.Invasion.InvasionRecords.DayPoints(CityBuilder.Invasion.InvasionRecords.LoadDays(), System.DateTime.Now, counted);
            SetLine(Invasion, Localization.Format("#banner_points_today", points));
            SetLine(Campaign, Localization.Get("#banner_campaign_line"));
        }

        /// <summary>The picked mode's motto, over «Играть».</summary>
        private void ShowMotto()
        {
            if (motto != null) motto.text = Localization.Get(MottoKeys[Mathf.Clamp(_picked, 0, MottoKeys.Length - 1)]);
        }

        /// <summary>«25» and the crystal on the line under «Награда:», centred as a pair; amount below 0 hides both.</summary>
        private void ShowReward(int amount)
        {
            var shown = amount >= 0;
            if (rewardAmount != null) rewardAmount.gameObject.SetActive(shown);
            if (rewardIcon != null) rewardIcon.gameObject.SetActive(shown);
            if (!shown || rewardAmount == null) return;
            rewardAmount.text = amount.ToString();
            var number = rewardAmount.preferredWidth;
            var icon = rewardIcon != null ? rewardIcon.rectTransform.sizeDelta.x + RewardIconGap : 0f;
            // the number's centre and the crystal's, so number + gap + crystal sits centred on the cloth
            var left = -(number + icon) * 0.5f;
            var amountRect = rewardAmount.rectTransform;
            amountRect.anchoredPosition = new Vector2(left + number * 0.5f, amountRect.anchoredPosition.y);
            if (rewardIcon != null)
            {
                var iconRect = rewardIcon.rectTransform;
                iconRect.anchoredPosition = new Vector2(left + number + RewardIconGap + iconRect.sizeDelta.x * 0.5f, iconRect.anchoredPosition.y);
            }
        }

        private void SetLine(int index, string text)
        {
            if (lines.Length > index && lines[index] != null) lines[index].text = text;
        }
    }
}
