using CityBuilder.Core;
using CityBuilder.Saving;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The modes screen, v2 «лента по кругу» (the user, 2026-11-16; card hud-and-ui.md, concepts Design/ui/ModeWheel_*):
    /// six banners in ONE row on a CLOSED chain -- the picked one big and bright in the centre, one each side smaller
    /// and darker, one more each side smaller and darker still, the sixth off screen. Scrolled by the gold chevrons at
    /// the screen's edges, by a swipe, or by a tap on a side banner (it comes to the centre); a tap on the centred one
    /// or «Играть» starts it, a long press opens its few words. Order: Обучение, Песочница, Кампания, Нашествие,
    /// Онлайн, Мастерская; the last two are locked «Скоро». On entry the centre is Обучение at first, afterwards the
    /// last mode PLAYED. The chevrons nudge outward now and then (a twin trails, a glow breathes) until the player
    /// scrolls, and again after a while idle. The banners unroll from their perch when the screen comes up.
    /// </summary>
    public class ModeBannersController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public const int Tutorial = 0, Sandbox = 1, Invasion = 2, Campaign = 3, Online = 4, Workshop = 5;

        /// <summary>The row's order, left to right round the chain (the user, 2026-11-16).</summary>
        private static readonly int[] Order = { Tutorial, Sandbox, Campaign, Invasion, Online, Workshop };

        private const string PlayedKey = "menu.mode.played";
        private const float Ease = 10f;
        private const float UnrollSeconds = 0.38f;
        private const float UnrollStagger = 0.08f;
        private const float RewardIconGap = 6f;

        // the slots by distance from the centre, 0..3 (3 = off screen): x offset, scale, brightness, alpha, drop
        private static readonly float[] SlotX = { 0f, 415f, 684f, 900f };
        private static readonly float[] SlotScale = { 1.10f, 0.82f, 0.62f, 0.50f };
        private static readonly float[] SlotLight = { 1f, 0.70f, 0.50f, 0.40f };
        private static readonly float[] SlotAlpha = { 1f, 1f, 1f, 0f };
        private static readonly float[] SlotDrop = { 0f, 24f, 41f, 50f };
        private const float Top = 380f;

        // the chevrons' hint: every HintPeriod a HintStep step outward, a twin trailing, a glow breathing
        private const float HintPeriod = 2.4f, HintStep = 0.9f, HintPush = 14f, TwinGap = 23f, HintIdle = 12f;

        [SerializeField] private MainMenuController menu;
        [SerializeField] private CanvasGroup screen;
        [SerializeField] private RectTransform[] banners = new RectTransform[0];
        [SerializeField] private Image[] cloths = new Image[0];
        [SerializeField] private Text motto;
        [SerializeField] private Text[] lines = new Text[0];
        [SerializeField] private Image playButton;
        [SerializeField] private Image rewardIcon;
        [SerializeField] private Text rewardAmount;
        /// <summary>Left, right: the chevrons' roots (the right one mirrored), their picture, the trailing twin, the glow.</summary>
        [SerializeField] private RectTransform[] arrows = new RectTransform[0];
        [SerializeField] private RectTransform[] chevrons = new RectTransform[0];
        [SerializeField] private Image[] twins = new Image[0];
        [SerializeField] private Image[] glows = new Image[0];

        private static readonly string[] MottoKeys =
            { "#banner_motto_tutorial", "#banner_motto_sandbox", "#banner_motto_invasion", "#banner_motto_campaign", "#banner_motto_online", "#banner_motto_workshop" };
        private const string CampaignScene = "CampaignMap";

        private float _pos, _target;
        private bool _dragging;
        private float _dragStart;
        private bool _shown;
        private float _shownAt;
        private int _shownPicked = -1;
        private bool _scrolled;
        private float _lastTouch, _hintFrom;
        private readonly float[] _squeeze = new float[2];
        private Graphic[][] _graphics;
        private Color[][] _colours;
        private float[] _light;
        private int[] _ring;
        private readonly int[] _drawOrder = new int[6];
        private Canvas _canvas;

        public static bool Locked(int mode) => mode == Online || mode == Workshop;

        private void Awake()
        {
            _ring = new int[banners.Length];
            for (var r = 0; r < Order.Length; r++)
                if (Order[r] < _ring.Length) _ring[Order[r]] = r;
            _graphics = new Graphic[banners.Length][];
            _colours = new Color[banners.Length][];
            _light = new float[banners.Length];
            for (var i = 0; i < banners.Length; i++)
            {
                if (banners[i] == null) continue;
                _graphics[i] = banners[i].GetComponentsInChildren<Graphic>(true);
                _colours[i] = new Color[_graphics[i].Length];
                for (var g = 0; g < _graphics[i].Length; g++) _colours[i][g] = _graphics[i][g].color;
                _light[i] = -1f;
            }
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null) _canvas = _canvas.rootCanvas;
            CentreOnLastPlayed();
        }

        private void CentreOnLastPlayed()
        {
            var played = PlayerPrefs.GetInt(PlayedKey, Tutorial);
            if (played < 0 || played >= _ring.Length) played = Tutorial;
            _pos = _target = _ring[played];
        }

        /// <summary>The mode in the centre (or heading there).</summary>
        private int Picked => Order[Wrap(Mathf.RoundToInt(_target))];

        private static int Wrap(int r) => ((r % Order.Length) + Order.Length) % Order.Length;

        /// <summary>A ring index's signed distance from `from`, wrapped into -3..3.</summary>
        private static float Slot(int ring, float from)
        {
            var n = Order.Length;
            var s = (ring - from + n * 0.5f) % n;
            if (s < 0f) s += n;
            return s - n * 0.5f;
        }

        private static float Table(float[] table, float at)
        {
            at = Mathf.Clamp(at, 0f, table.Length - 1);
            var i = Mathf.Min((int)at, table.Length - 2);
            return Mathf.Lerp(table[i], table[i + 1], at - i);
        }

        private void Update()
        {
            // the screen coming up: back on the last played mode, the live lines read afresh, the banners unroll
            var visible = screen != null && screen.alpha > 0.01f;
            if (visible && !_shown)
            {
                _shownAt = Time.unscaledTime;
                _lastTouch = _hintFrom = _shownAt;
                _scrolled = false;
                CentreOnLastPlayed();
                Refresh();
            }
            _shown = visible;
            if (!visible)
            {
                // rolled up while away, so the next coming up starts from the perch
                for (var i = 0; i < banners.Length; i++)
                    if (banners[i] != null) banners[i].localScale = new Vector3(1f, 0.02f, 1f);
                return;
            }

            var dt = Time.unscaledDeltaTime;
            var now = Time.unscaledTime;
            if (!_dragging) _pos = Mathf.Lerp(_pos, _target, 1f - Mathf.Exp(-Ease * dt));
            if (Picked != _shownPicked) ShowPicked();

            for (var i = 0; i < banners.Length; i++)
            {
                var rect = banners[i];
                if (rect == null) continue;
                var s = Slot(_ring[i], _pos);
                var d = Mathf.Abs(s);
                var side = s < 0f ? -1f : 1f;
                var scale = Table(SlotScale, d);
                rect.anchoredPosition = new Vector2(side * Table(SlotX, d), Top - Table(SlotDrop, d));
                var unroll = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((now - _shownAt - d * UnrollStagger) / UnrollSeconds));
                rect.localScale = new Vector3(scale, scale * Mathf.Max(0.02f, unroll), 1f);
                // the cloth sways from its perch, the centred one more
                var centred = d < 0.5f;
                var sway = Mathf.Sin(now * (centred ? 1.6f : 1.1f) + i * 1.7f) * (centred ? 1.8f : 0.7f);
                rect.localRotation = Quaternion.Euler(0f, 0f, sway);
                Tint(i, Table(SlotLight, d), Table(SlotAlpha, d));
                _drawOrder[i] = i;
            }
            Restack();
            Arrows(now, dt);
        }

        /// <summary>Darker further out, see-through at the edge; only touched when it changes (no rebuild while still).</summary>
        private void Tint(int i, float light, float alpha)
        {
            var key = light + alpha * 10f;
            if (Mathf.Abs(key - _light[i]) < 0.004f || _graphics[i] == null) return;
            _light[i] = key;
            for (var g = 0; g < _graphics[i].Length; g++)
            {
                var c = _colours[i][g];
                _graphics[i][g].color = new Color(c.r * light, c.g * light, c.b * light, c.a * alpha);
            }
            var hit = cloths.Length > i && cloths[i] != null ? cloths[i] : null;
            if (hit != null) hit.raycastTarget = alpha > 0.5f;
        }

        /// <summary>The nearer a banner to the centre, the later it draws.</summary>
        private void Restack()
        {
            // insertion sort, farthest first: six items, no allocation (mobile-first: no per-frame garbage)
            var n = Mathf.Min(_drawOrder.Length, banners.Length);
            for (var k = 1; k < n; k++)
            {
                var item = _drawOrder[k];
                var far = Mathf.Abs(Slot(_ring[item], _pos));
                var j = k - 1;
                while (j >= 0 && Mathf.Abs(Slot(_ring[_drawOrder[j]], _pos)) < far)
                {
                    _drawOrder[j + 1] = _drawOrder[j];
                    j--;
                }
                _drawOrder[j + 1] = item;
            }
            for (var k = 0; k < _drawOrder.Length && k < banners.Length; k++)
            {
                var rect = banners[_drawOrder[k]];
                if (rect != null && rect.GetSiblingIndex() != k) rect.SetSiblingIndex(k);
            }
        }

        private void Arrows(float now, float dt)
        {
            var hinting = !_scrolled || now - _lastTouch > HintIdle;
            if (!hinting) _hintFrom = now;
            var t = (now - _hintFrom) % HintPeriod;
            var nudge = hinting && t < HintStep ? Mathf.Sin(Mathf.PI * t / HintStep) : 0f;
            nudge = nudge * nudge * (3f - 2f * nudge);
            for (var a = 0; a < arrows.Length && a < 2; a++)
            {
                _squeeze[a] = Mathf.MoveTowards(_squeeze[a], 0f, dt * 6f);
                var press = 1f - 0.15f * Mathf.Sin(Mathf.PI * _squeeze[a]);
                if (chevrons.Length > a && chevrons[a] != null)
                {
                    chevrons[a].anchoredPosition = new Vector2(-HintPush * nudge, 0f);
                    chevrons[a].localScale = Vector3.one * press;
                }
                if (twins.Length > a && twins[a] != null)
                {
                    twins[a].rectTransform.anchoredPosition = new Vector2(-HintPush * nudge + TwinGap, 0f);
                    twins[a].canvasRenderer.SetAlpha(nudge > 0.05f ? 0.45f * nudge : 0f);
                }
                if (glows.Length > a && glows[a] != null)
                {
                    glows[a].rectTransform.anchoredPosition = new Vector2(-HintPush * nudge, 0f);
                    glows[a].canvasRenderer.SetAlpha(hinting ? 0.25f + 0.35f * nudge : 0f);
                }
            }
        }

        private void Touched()
        {
            _scrolled = true;
            _lastTouch = Time.unscaledTime;
        }

        /// <summary>The left chevron: the banner on the left comes to the centre.</summary>
        public void ScrollLeft()
        {
            _squeeze[0] = 1f;
            Touched();
            _target = Mathf.Round(_target) - 1f;
        }

        /// <summary>The right chevron: the banner on the right comes to the centre.</summary>
        public void ScrollRight()
        {
            if (_squeeze.Length > 1) _squeeze[1] = 1f;
            Touched();
            _target = Mathf.Round(_target) + 1f;
        }

        /// <summary>A tap on a banner: the centred one starts, a side one comes to the centre.</summary>
        public void TapBanner(int index)
        {
            var press = banners.Length > index && banners[index] != null ? banners[index].GetComponent<LongPress>() : null;
            if (press != null && press.ConsumeFired()) return;
            if (index < 0 || index >= _ring.Length) return;
            var s = Mathf.Round(Slot(_ring[index], _target));
            if (Mathf.Abs(s) < 0.5f) { Play(); return; }
            Touched();
            _target = Mathf.Round(_target) + s;
        }

        /// <summary>A long press: the mode's few words (the sandbox's and the invasion's; the others have none).</summary>
        public void HoldBanner(int index)
        {
            if (index >= 0 && index < _ring.Length) _target = Mathf.Round(_target) + Mathf.Round(Slot(_ring[index], _target));
            if (menu == null) return;
            if (index == Sandbox) menu.ShowInfo((int)GameMode.Sandbox);
            else if (index == Invasion) menu.ShowInfo((int)GameMode.Invasion);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = true;
            _dragStart = Mathf.Round(_target);
            Touched();
        }

        public void OnDrag(PointerEventData eventData)
        {
            var scale = _canvas != null ? _canvas.scaleFactor : 1f;
            _pos -= eventData.delta.x / Mathf.Max(0.01f, scale) / SlotX[1];
            _target = _pos;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
            var nearest = Mathf.Round(_pos);
            // a short flick still turns one step
            if (Mathf.Approximately(nearest, _dragStart) && Mathf.Abs(_pos - _dragStart) > 0.12f) nearest = _dragStart + Mathf.Sign(_pos - _dragStart);
            _target = nearest;
            Touched();
        }

        /// <summary>«Играть»: the centred mode, remembered as the last played (the locked ones do nothing).</summary>
        public void Play()
        {
            var mode = Picked;
            if (menu == null || Locked(mode)) return;
            PlayerPrefs.SetInt(PlayedKey, mode);
            PlayerPrefs.Save();
            switch (mode)
            {
                case Tutorial: menu.StartTutorial(); break;
                case Sandbox: menu.ChooseMode((int)GameMode.Sandbox); break;
                case Invasion: menu.ChooseMode((int)GameMode.Invasion); break;
                // the campaign (the user, 2026-11-12): the loading screen, then the campaign map (pick a level, «Играть»)
                case Campaign: LoadingScreen.ToCampaignMap(CampaignScene); break;
            }
        }

        /// <summary>The centred mode's motto over «Играть», and «Играть» greyed for a locked one.</summary>
        private void ShowPicked()
        {
            _shownPicked = Picked;
            if (motto != null) motto.text = Localization.Get(MottoKeys[Mathf.Clamp(_shownPicked, 0, MottoKeys.Length - 1)]);
            if (playButton != null)
            {
                var locked = Locked(_shownPicked);
                playButton.color = locked ? new Color(0.62f, 0.62f, 0.62f, 1f) : Color.white;
                var button = playButton.GetComponent<Button>();
                if (button != null) button.interactable = !locked;
            }
        }

        /// <summary>The banners' live lines.</summary>
        private void Refresh()
        {
            ShowPicked();
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
            SetLine(Online, Localization.Get("#banner_soon"));
            SetLine(Workshop, Localization.Get("#banner_soon"));
            // texts changed under a tint: take their colours again next frame
            for (var i = 0; i < _light.Length; i++) _light[i] = -1f;
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
