using System;
using System.Collections;
using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The piggy bank's window (approved 2026-11-16: Design/store/Piggy_Window_Concept.png/.webp, the «!» popup
    /// Piggy_Window_Info_Concept.png, the break Piggy_Break_Concept.webp; card Design/features/piggy-bank.md). Left: the
    /// pig of the current level in a golden shine with four crystals bobbing in their halos (it wobbles a little while
    /// filling, more when full), the fill bar, «Разбить за $X» (grey «Разбить от 150» below piggy_break_min) and a
    /// «+N %» badge with the REAL gain now (none below break-even). Right: today's three tasks and the time to the next
    /// set. «!» explains it all; nothing else is written. Breaking it plays the shatter: the pig flies to the centre,
    /// cracks, bursts, its crystals fly into the wallet and the next level's pig assembles from its shards.
    ///
    /// Built in code over its pictures (Resources/Shop/Piggy, Tools/store/piggy_art.py); positions are the concept's,
    /// 1920 x 1080, y down (R converts). Pauses the game while open, like the other windows.
    /// </summary>
    public class PiggyWindow : MonoBehaviour
    {
        private const int Order = 965;
        private const string Art = PiggyUi.Art;
        private const float PX = 640f;          // the pig column's centre
        private const float Feet = 738f;
        private const float PigW = 400f;

        // (dx from PX, y, height, tilt, phase, period) -- the crystals beside the pig
        private static readonly float[,] Gems = { { -250, 390, 70, -12, 0.0f, 1.9f }, { 250, 370, 60, 14, 1.3f, 2.3f }, { -275, 600, 52, 8, 2.4f, 2.1f }, { 275, 590, 56, -10, 0.7f, 1.7f } };

        private static PiggyWindow _current;
        private const string AutoKey = "piggy.auto_shown";

        public static bool IsOpen => _current != null;

        private RectTransform _root, _column, _controls, _tasks, _fx;
        private GameObject _info;
        private Image _pig, _shine, _rays, _barFill, _buttonImage;
        private RectTransform _walletGem;
        private readonly List<Canvas> _hidden = new List<Canvas>();
        private readonly RectTransform[] _gems = new RectTransform[4];
        private readonly Image[] _halos = new Image[4];
        private Text _wallet, _count, _buttonText, _timer;
        private RectTransform _countGem, _full, _badge;
        private Text _badgeText;
        private bool _breaking;
        private float _timeScaleBefore = 1f;
        private bool _gateBefore;
        private float _timerTick;

        // ------------------------------------------------------------ opening

        public static void Show()
        {
            if (_current != null) return;
            var go = new GameObject("PiggyWindow", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = Order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            _current = go.AddComponent<PiggyWindow>();
        }

        /// <summary>
        /// The main menu calls it as it opens: a full piggy opens itself at most every piggy_full_banner_hours, and once
        /// after a task's crystals were lost to a full piggy.
        /// </summary>
        public static void OfferInMenu()
        {
            if (_current != null) return;
            var lost = PiggyTasks.LostWhileFull;
            if (!PiggyBank.Full && !lost) return;
            var config = BalanceConfig.Sandbox;
            var hours = config != null ? config.PiggyFullBannerHours : 4f;
            var now = DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond;
            long.TryParse(PlayerPrefs.GetString(AutoKey, "0"), out var last);
            if (!lost && now - last < hours * 3600f) return;
            PlayerPrefs.SetString(AutoKey, now.ToString());
            PiggyTasks.LostWhileFull = false;
            Show();
        }

        /// <summary>Android's back key: the «!» popup first, then the window; not while it is breaking.</summary>
        public static void BackCurrent()
        {
            if (_current == null || _current._breaking) return;
            if (_current._info != null) Destroy(_current._info);
            else _current.Close();
        }

        public void Close() => Destroy(gameObject);

        private void Start()
        {
            _root = (RectTransform)transform;
            _timeScaleBefore = Time.timeScale;
            Time.timeScale = 0f;
            _gateBefore = ModalGate.IsBlocked;
            ModalGate.SetBlocked(true);
            HideTheInterfaceBehind();
            Build();
            PiggyBank.Changed += Redraw;
            PiggyBank.Broken += OnBroken;
            PiggyTasks.Changed += Redraw;
            Store.Changed += Redraw;
            Wallet.UpgradeCrystalsChanged += RedrawWallet;
            Redraw();
        }

        private void OnDestroy()
        {
            PiggyBank.Changed -= Redraw;
            PiggyBank.Broken -= OnBroken;
            PiggyTasks.Changed -= Redraw;
            Store.Changed -= Redraw;
            Wallet.UpgradeCrystalsChanged -= RedrawWallet;
            Time.timeScale = _timeScaleBefore;
            ModalGate.SetBlocked(_gateBefore);
            foreach (var canvas in _hidden) if (canvas != null) canvas.enabled = true;
            _hidden.Clear();
            if (_current == this) _current = null;
        }

        /// <summary>Every other interface off while it is open (the user 2026-11-16), as the Лавка does; back on close.</summary>
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

        /// <summary>The concept's point (1920 x 1080, y down) in the canvas (centre 0,0, y up).</summary>
        private static Vector2 R(float x, float y) => new Vector2(x - 960f, 540f - y + Lift);

        /// <summary>The concept's frame spans y 148..1080; lifted by half the gap, it stands centred (the user 2026-11-16).</summary>
        private const float Lift = 74f;

        private static Vector2 Centre(float x0, float y0, float x1, float y1) => R((x0 + x1) / 2f, (y0 + y1) / 2f);

        // ------------------------------------------------------------ building

        private void Build()
        {
            var dim = PiggyUi.Img<Image>(_root, "Dim");
            Stretch(dim.rectTransform);
            dim.color = new Color(0.055f, 0.024f, 0.12f, 0.8f);
            dim.raycastTarget = true;

            var frame = PiggyUi.Pic(_root, Art + "Frame", Centre(14, 148, 1920, 1080), new Vector2(1906, 932));
            frame.raycastTarget = true;
            PiggyUi.Stroke(PiggyUi.Label(_root, Localization.Get("#piggy_title"), 70, PiggyUi.Cream, R(960, 241), new Vector2(760, 100)), 5f, PiggyUi.Ribbon);

            // the wallet, top left: what a break pays into
            var walletGem = PiggyUi.Pic(_root, PiggyUi.Crystal, new Vector2(72, -60), new Vector2(72, 72));
            walletGem.name = "WalletGem";
            walletGem.rectTransform.anchorMin = walletGem.rectTransform.anchorMax = new Vector2(0f, 1f);
            _walletGem = walletGem.rectTransform;
            _wallet = PiggyUi.Stroke(PiggyUi.Label(_root, "", 44, PiggyUi.Cream, new Vector2(116 + 200, -60), new Vector2(400, 60)), 3f, PiggyUi.Ink);
            _wallet.rectTransform.anchorMin = _wallet.rectTransform.anchorMax = new Vector2(0f, 1f);
            _wallet.alignment = TextAnchor.MiddleLeft;

            RoundButton(R(1840, 250), PiggyUi.Red, Close, true);
            RoundButton(R(80, 250), new Color(0.235f, 0.47f, 0.78f), ShowInfo, false);

            // the pig column
            _column = Group(_root, "Column");
            _shine = PiggyUi.Pic(_column, Art + "Shine", R(PX, 560), new Vector2(640, 520));
            _rays = PiggyUi.Pic(_column, Art + "Rays", R(PX, 560), new Vector2(780, 780));
            var shadow = PiggyUi.Pic(_column, Art + "Dot", R(PX, 731), new Vector2(320, 42));
            shadow.color = new Color(0f, 0f, 0f, 0.4f);
            for (var i = 0; i < 4; i++)
            {
                _halos[i] = PiggyUi.Pic(_column, Art + "Halo", R(PX + Gems[i, 0], Gems[i, 1]), Vector2.one * Gems[i, 2] * 2.6f);
                _gems[i] = PiggyUi.PicH(_column, PiggyUi.Crystal, R(PX + Gems[i, 0], Gems[i, 1]), Gems[i, 2] * 1.25f).rectTransform;
            }
            _pig = PiggyUi.Pic(_column, Art + "Pig1", R(PX, Feet), Vector2.one);
            _pig.rectTransform.pivot = new Vector2(0.5f, 0f);

            _controls = Group(_root, "Controls");
            PiggyUi.Pic(_controls, Art + "BarBack", R(PX, 808), new Vector2(500, 56));
            _barFill = PiggyUi.Pic(_controls, Art + "BarFill", R(PX, 808), new Vector2(486, 42));
            _barFill.type = Image.Type.Filled;
            _barFill.fillMethod = Image.FillMethod.Horizontal;
            _count = PiggyUi.Stroke(PiggyUi.Label(_controls, "", 36, PiggyUi.Cream, R(PX, 806), new Vector2(400, 50)), 3f, PiggyUi.Ink);
            _countGem = PiggyUi.PicH(_controls, PiggyUi.Crystal, R(PX, 806), 52).rectTransform;
            _full = PiggyUi.FullPill(_controls, R(PX + 230, 767), 28);

            _buttonImage = PiggyUi.Pic(_controls, Art + "Button", R(PX, 912), new Vector2(420, 96));
            _buttonImage.raycastTarget = true;
            var button = _buttonImage.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;   // the grey sprite says it; no see-through tint (the user 2026-11-16)
            button.onClick.AddListener(BreakIt);
            _buttonText = PiggyUi.Stroke(PiggyUi.Label(_buttonImage.transform, "", 40, PiggyUi.Cream, new Vector2(0, 2), new Vector2(400, 80)), 3f, PiggyUi.Ink);
            var badge = PiggyUi.Pic(_controls, Art + "Dot", R(845, 866), new Vector2(88, 88));
            badge.color = new Color(0.98f, 0.76f, 0.2f);
            badge.gameObject.AddComponent<Outline>().effectColor = PiggyUi.Ink;
            _badge = badge.rectTransform;
            _badge.SetParent(_buttonImage.transform, true);   // breathes with the button (the user 2026-11-16)
            _badgeText = PiggyUi.Stroke(PiggyUi.Label(_badge, "", 26, PiggyUi.Cream, new Vector2(0, 0), new Vector2(120, 40)), 2f, PiggyUi.Ink);

            // the tasks
            PiggyUi.Pic(_root, Art + "Panel", Centre(1180, 334, 1793, 983), new Vector2(613, 649));
            PiggyUi.Stroke(PiggyUi.Label(_root, Localization.Get("#piggy_tasks"), 40, PiggyUi.Gold, R(1446, 386), new Vector2(300, 56)), 3f, PiggyUi.Ink);
            Clock(_root, R(1606, 386), 32f, PiggyUi.Cream);
            _timer = PiggyUi.Stroke(PiggyUi.Label(_root, "", 26, new Color(0.86f, 0.78f, 0.96f), R(1632 + 100, 386), new Vector2(200, 40)), 2f, PiggyUi.Ink);
            _timer.alignment = TextAnchor.MiddleLeft;
            _tasks = Group(_root, "Tasks");
            // a sandbox world made easier («Свобода песочницы»): its tasks do not count -- said over them (the user 2026-11-17)
            if (PiggyTasks.InSandbox && WorldSettings.Current.Easier && Maps.MeshMapApplier.Instance != null)
            {
                PiggyUi.Pic(_root, Art + "EasyWarn", R(1486, 304), new Vector2(600, 52));
                var warn = PiggyUi.Label(_root, Localization.Get("#world_warn_piggy"), 20, new Color(0.5f, 0.1f, 0.08f), R(1514, 304), new Vector2(520, 46));
                warn.resizeTextForBestFit = true;
                warn.resizeTextMinSize = 12;
                warn.resizeTextMaxSize = 20;
            }

            _fx = Group(_root, "Fx");
        }

        private static RectTransform Group(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = Vector2.zero;
            return rt;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static Image Bar(Transform parent, Vector2 at, Vector2 size, float angle, Color colour)
        {
            var bar = PiggyUi.Pic(parent, Art + "Pill", at, size);
            bar.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            bar.color = colour;
            return bar;
        }

        /// <summary>A round button: ✕ drawn as two bars (close), or «!» (the info popup).</summary>
        private void RoundButton(Vector2 at, Color colour, Action onTap, bool cross)
        {
            var dot = PiggyUi.Pic(_root, Art + "DotRing", at, new Vector2(84, 84));
            dot.color = colour;
            dot.raycastTarget = true;
            dot.gameObject.AddComponent<Button>().onClick.AddListener(() => { if (!_breaking) onTap(); });
            if (cross)
            {
                Bar(dot.transform, Vector2.zero, new Vector2(44, 10), 45, PiggyUi.Cream);
                Bar(dot.transform, Vector2.zero, new Vector2(44, 10), -45, PiggyUi.Cream);
            }
            else PiggyUi.Stroke(PiggyUi.Label(dot.transform, "!", 48, PiggyUi.Cream, new Vector2(0, 2), new Vector2(60, 70)), 2f, PiggyUi.Ink);
        }

        private static void Clock(Transform parent, Vector2 at, float size, Color colour)
        {
            var ring = PiggyUi.Pic(parent, Art + "DotRing", at, new Vector2(size, size));
            ring.color = colour;
            var hand1 = Bar(ring.transform, new Vector2(0, size * 0.15f), new Vector2(3, size * 0.32f), 0, PiggyUi.Ink);
            hand1.rectTransform.sizeDelta = new Vector2(3f, size * 0.32f);
            Bar(ring.transform, new Vector2(size * 0.12f, 0), new Vector2(size * 0.26f, 3f), 0, PiggyUi.Ink);
        }

        private static void Tick(Transform parent, Vector2 at, float scale, Color colour)
        {
            Bar(parent, at + new Vector2(-10, -2) * scale, new Vector2(22, 9) * scale, -45, colour);
            Bar(parent, at + new Vector2(7, 2) * scale, new Vector2(36, 9) * scale, 50, colour);
        }

        // ------------------------------------------------------------ redrawing

        private void RedrawWallet()
        {
            if (_wallet != null && !_breaking) _wallet.text = PiggyUi.Number(Wallet.UpgradeCrystals);
        }

        private void Redraw()
        {
            if (_pig == null || _breaking) return;
            RedrawWallet();
            var level = PiggyBank.Level;
            SetPig(_pig, level, PigW);
            var stored = PiggyBank.Stored;
            var cap = PiggyBank.Capacity;
            var full = PiggyBank.Full;
            _shine.color = new Color(1f, 1f, 1f, full ? 0.67f : 0.43f);
            _rays.color = new Color(1f, 1f, 1f, full ? 0.2f : 0.12f);
            _barFill.fillAmount = Mathf.Clamp01(stored / (float)Mathf.Max(1, cap));
            _barFill.color = full ? new Color(1f, 0.8f, 0.27f) : new Color(0.85f, 0.42f, 0.92f);
            _count.text = PiggyUi.Number(stored) + " / " + PiggyUi.Number(cap);
            _count.color = full ? PiggyUi.Gold : PiggyUi.Cream;
            var w = _count.preferredWidth;
            var gw = _countGem.sizeDelta.x;
            _count.rectTransform.anchoredPosition = R(PX - (gw + 10f) / 2f, 806);
            _countGem.anchoredPosition = R(PX + w / 2f + 10f - (gw + 10f) / 2f + gw / 2f, 806);
            _full.gameObject.SetActive(full);

            var product = PiggyBank.ProductId;
            var pending = Store.IsPending(product);
            var can = PiggyBank.CanBreak && !pending;
            _buttonImage.sprite = PiggyUi.Sprite(Art + (can ? "Button" : "ButtonGrey"));
            _buttonImage.GetComponent<Button>().interactable = can;
            _buttonText.text = pending ? Localization.Get("#shop_pending")
                : PiggyBank.CanBreak ? Localization.Format("#piggy_break", Store.PriceText(product))
                : Localization.Format("#piggy_break_min", PiggyBank.BreakMin);
            _buttonText.fontSize = pending ? 30 : 40;
            var gain = PiggyBank.CanBreak ? PiggyBank.GainPercent(stored) : 0;
            _badge.gameObject.SetActive(gain > 0);
            _badgeText.text = Localization.Format("#piggy_gain", gain);
            RedrawTasks(full);
            RedrawTimer();
        }

        private static void SetPig(Image image, int level, float width)
        {
            var s = PiggyUi.Sprite(Art + "Pig" + (level + 1));
            image.sprite = s;
            var h = s != null ? width * s.rect.height / Mathf.Max(1f, s.rect.width) : width;
            image.rectTransform.sizeDelta = new Vector2(width, h);
        }

        private void RedrawTasks(bool full)
        {
            for (var c = _tasks.childCount - 1; c >= 0; c--) Destroy(_tasks.GetChild(c).gameObject);
            for (var i = 0; i < PiggyTasks.Count && i < 3; i++)
            {
                var task = PiggyTasks.Task(i);
                if (task == null) continue;
                var done = PiggyTasks.Done(i);
                var have = PiggyTasks.Progress(i);
                float y = 444 + 169 * i, mid = y + 75.5f;
                var card = PiggyUi.Pic(_tasks, Art + "Card", R(1486.5f, mid), new Vector2(572, 159));
                if (done) card.color = new Color(0.87f, 0.83f, 0.74f);
                var name = PiggyUi.Label(_tasks, PiggyTasks.Name(task), 30, PiggyUi.Dark, R(1230 + 200, mid - 28), new Vector2(400, 44));
                name.alignment = TextAnchor.MiddleLeft;
                PiggyUi.Pic(_tasks, Art + "TaskBarBack", R(1426.5f, mid + 23), new Vector2(393, 32));
                var fill = PiggyUi.Pic(_tasks, Art + "TaskBarFill", R(1426.5f, mid + 23), new Vector2(383, 22));
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillAmount = Mathf.Clamp01(have / (float)Mathf.Max(1, task.target));
                fill.color = done ? new Color(0.34f, 0.67f, 0.24f) : new Color(0.85f, 0.42f, 0.92f);
                PiggyUi.Stroke(PiggyUi.Label(_tasks, PiggyUi.Number(have) + "/" + PiggyUi.Number(task.target), 22, PiggyUi.Cream, R(1426.5f, mid + 23), new Vector2(380, 30)), 2f, PiggyUi.Ink);
                const float rx = 1698f;
                if (done)
                {
                    var tick = PiggyUi.Pic(_tasks, Art + "DotRing", R(rx, mid), new Vector2(72, 72));
                    tick.color = new Color(0.34f, 0.67f, 0.24f);
                    Tick(tick.transform, Vector2.zero, 1f, PiggyUi.Cream);
                }
                else
                {
                    var col = full ? new Color(0.59f, 0.55f, 0.51f) : PiggyUi.Cream;
                    var reward = PiggyUi.Stroke(PiggyUi.Label(_tasks, "+" + task.reward, 32, col, R(rx - 4 - 60, mid), new Vector2(120, 44)), 3f, PiggyUi.Ink);
                    reward.alignment = TextAnchor.MiddleRight;
                    var gem = PiggyUi.PicH(_tasks, PiggyUi.Crystal, R(rx + 25, mid), 52);
                    if (full) gem.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
                }
            }
        }

        private void RedrawTimer()
        {
            if (_timer == null) return;
            var left = PiggyTasks.UntilNext;
            if (left < TimeSpan.Zero) left = TimeSpan.Zero;
            _timer.text = $"{(int)left.TotalHours:00}:{left.Minutes:00}";
        }

        // ------------------------------------------------------------ living

        private void Update()
        {
            if (_pig == null) return;
            var t = Time.unscaledTime;
            var full = PiggyBank.Full;
            if (!_breaking)
            {
                float dx, rot;
                if (full) PiggyUi.Wobble(t, 1.8f, 0.9f, 4f, 3f, out dx, out rot);
                else PiggyUi.Wobble(t, 2.6f, 0.8f, 2f, 2f, out dx, out rot);
                _pig.rectTransform.anchoredPosition = R(PX + dx, Feet);
                _pig.rectTransform.localRotation = Quaternion.Euler(0, 0, rot);
                var amp = full ? 8f : 3.5f;
                for (var i = 0; i < 4; i++)
                {
                    PiggyUi.Drift(t, Gems[i, 4], Gems[i, 5], amp, out var gx, out var gy, out var tilt);
                    var at = R(PX + Gems[i, 0] + gx, Gems[i, 1] + gy);
                    _gems[i].anchoredPosition = at;
                    _gems[i].localRotation = Quaternion.Euler(0, 0, -(Gems[i, 3] + tilt * (full ? 1f : 0.45f)));
                    _halos[i].rectTransform.anchoredPosition = at;
                    var pulse = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * t / Gems[i, 5] + Gems[i, 4]);
                    _halos[i].color = new Color(1f, 1f, 1f, 0.55f + 0.45f * pulse);
                    _halos[i].rectTransform.localScale = Vector3.one * (0.9f + 0.2f * pulse);
                }
                // the break button breathes while it can be pressed (the user 2026-11-16: no glow round it)
                var breath = PiggyBank.CanBreak ? 1f + 0.045f * Mathf.Sin(2f * Mathf.PI * t / 1.1f) : 1f;
                _buttonImage.rectTransform.localScale = Vector3.one * breath;
            }
            _rays.rectTransform.localRotation = Quaternion.Euler(0, 0, -t * 6f);
            _timerTick -= Time.unscaledDeltaTime;
            if (_timerTick <= 0f)
            {
                _timerTick = 1f;
                RedrawTimer();
            }
        }

        // ------------------------------------------------------------ the «!» popup

        private void ShowInfo()
        {
            if (_info != null) return;
            var root = Group(_root, "Info");
            Stretch(root);
            _info = root.gameObject;
            var dim = PiggyUi.Img<Image>(root, "Dim");
            Stretch(dim.rectTransform);
            dim.color = new Color(0.04f, 0.016f, 0.08f, 0.67f);
            dim.raycastTarget = true;
            dim.gameObject.AddComponent<Button>().onClick.AddListener(() => Destroy(_info));
            var sheet = PiggyUi.Pic(root, Art + "Info", Centre(520, 130, 1401, 981), new Vector2(881, 851));
            sheet.raycastTarget = true;
            PiggyUi.Label(root, Localization.Get("#piggy_info_title"), 46, PiggyUi.Dark, R(960, 192), new Vector2(820, 60));
            PiggyUi.Label(root, Localization.Format("#piggy_info_level", PiggyBank.Level + 1, PiggyBank.Levels), 30, new Color(0.55f, 0.27f, 0.67f), R(960, 242), new Vector2(820, 44));
            var lines = new[]
            {
                Localization.Get("#piggy_info_minute"), Localization.Get("#piggy_info_tasks"),
                Localization.Format("#piggy_info_min", PiggyBank.BreakMin), Localization.Format("#piggy_info_gain", PiggyBank.FullGainPercent),
                Localization.Get("#piggy_info_next"),
            };
            for (var k = 0; k < lines.Length; k++)
            {
                var y = 330f + 112f * k;
                var dot = PiggyUi.Pic(root, Art + "DotRing", R(630, y), new Vector2(84, 84));
                dot.color = PiggyUi.Violet;
                switch (k)
                {
                    case 0:
                        PiggyUi.Stroke(PiggyUi.Label(dot.transform, "+1", 26, PiggyUi.Cream, new Vector2(-12, 6), new Vector2(60, 36)), 2f, PiggyUi.Ink);
                        PiggyUi.PicH(dot.transform, PiggyUi.Crystal, new Vector2(14, -6), 44);
                        break;
                    case 1:
                        Tick(dot.transform, Vector2.zero, 1f, PiggyUi.Cream);
                        break;
                    case 2:
                        PiggyUi.Stroke(PiggyUi.Label(dot.transform, PiggyBank.BreakMin.ToString(), 26, PiggyUi.Cream, Vector2.zero, new Vector2(80, 36)), 2f, PiggyUi.Ink);
                        break;
                    case 3:
                        PiggyUi.Stroke(PiggyUi.Label(dot.transform, Localization.Format("#piggy_gain", PiggyBank.FullGainPercent), 22, PiggyUi.Cream, Vector2.zero, new Vector2(80, 36)), 2f, PiggyUi.Ink);
                        break;
                    default:
                        Bar(dot.transform, new Vector2(0, -6), new Vector2(10, 30), 0, PiggyUi.Cream);
                        Bar(dot.transform, new Vector2(-8, 8), new Vector2(26, 10), 45, PiggyUi.Cream);
                        Bar(dot.transform, new Vector2(8, 8), new Vector2(26, 10), -45, PiggyUi.Cream);
                        break;
                }
                var line = PiggyUi.Label(root, lines[k], 32, PiggyUi.Dark, R(700 + 330, y), new Vector2(660, 50), false);
                line.alignment = TextAnchor.MiddleLeft;
            }
            var ok = PiggyUi.Label(root, Localization.Get("#piggy_info_ok"), 40, new Color(0.47f, 0.34f, 0.16f), R(960, 940), new Vector2(400, 60));
            ok.raycastTarget = true;
            ok.gameObject.AddComponent<Button>().onClick.AddListener(() => Destroy(_info));
        }

        // ------------------------------------------------------------ breaking

        private void BreakIt()
        {
            if (_breaking || !Store.CanBuy(PiggyBank.ProductId)) return;
            // the stand-in gives it at once (Broken plays the shatter); the billing will answer later the same way
            Store.Buy(PiggyBank.ProductId);
        }

        private void OnBroken()
        {
            if (_breaking || _pig == null) return;
            StartCoroutine(Shatter(PiggyBank.LastBrokenLevel, PiggyBank.Level, PiggyBank.LastBrokenCrystals, Wallet.UpgradeCrystals));
        }

        [Serializable] private class ShardLevel { public int[] size; public int[] atlas; public float[] flat; }
        [Serializable] private class ShardMeta { public int pigWidth; public ShardLevel[] levels; }

        private static ShardMeta _shardMeta;

        private static ShardMeta Meta()
        {
            if (_shardMeta != null) return _shardMeta;
            var json = UnityEngine.Resources.Load<TextAsset>(Art + "shards");
            _shardMeta = json != null ? JsonUtility.FromJson<ShardMeta>(json.text) : null;
            return _shardMeta;
        }

        private struct Shard
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Home, From, Velocity;
            public float Spin;
        }

        /// <summary>The level's pig as ~22 shard images under `parent`, each at its place in a pig `width` wide centred at 0,0.</summary>
        private static List<Shard> MakeShards(Transform parent, int level, float width)
        {
            var list = new List<Shard>();
            var meta = Meta();
            var tex = UnityEngine.Resources.Load<Texture2D>(Art + "Shards" + (level + 1));
            if (meta == null || meta.levels == null || level >= meta.levels.Length || tex == null) return list;
            var lv = meta.levels[level];
            if (lv.flat == null || lv.atlas == null || lv.size == null) return list;
            var kx = tex.width / (float)lv.atlas[0];
            var ky = tex.height / (float)lv.atlas[1];
            var k = width / lv.size[0];
            for (var i = 0; i + 5 < lv.flat.Length; i += 6)
            {
                float ax = lv.flat[i], ay = lv.flat[i + 1], w = lv.flat[i + 2], h = lv.flat[i + 3], cx = lv.flat[i + 4], cy = lv.flat[i + 5];
                var rect = new Rect(ax * kx, tex.height - (ay + h) * ky, w * kx, h * ky);
                var sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
                var img = PiggyUi.Img<Image>(parent, "Shard");
                img.sprite = sprite;
                img.rectTransform.sizeDelta = new Vector2(w * k, h * k);
                var home = new Vector2((cx - lv.size[0] / 2f) * k, (lv.size[1] / 2f - cy) * k);
                img.rectTransform.anchoredPosition = home;
                list.Add(new Shard { Rect = img.rectTransform, Image = img, Home = home });
            }
            return list;
        }

        private static float EaseOut(float x) => 1f - Mathf.Pow(1f - Mathf.Clamp01(x), 3f);

        private static float EaseInOut(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        /// <summary>
        /// The approved shatter (Tools/store/piggy_break_concept.py): 0-0.6 s the pig flies to the centre growing x1.35;
        /// to 1.9 it shakes and cracks in three steps; 1.9 a flash and rays, the shards fly; 2.0-4.0 its crystals fly into
        /// the wallet; 3.8 «Уровень N» and the next pig assembles (ready 4.75); 5.1 it goes back to its place.
        /// </summary>
        private IEnumerator Shatter(int oldLevel, int newLevel, int crystals, int walletAfter)
        {
            _breaking = true;
            _controls.gameObject.SetActive(false);
            foreach (var g in _gems) g.gameObject.SetActive(false);
            foreach (var h in _halos) h.gameObject.SetActive(false);
            var walletBefore = walletAfter - crystals;
            _wallet.text = PiggyUi.Number(walletBefore);

            // the flying pig, over everything
            var flyer = PiggyUi.Img<Image>(_fx, "Flyer");
            SetPig(flyer, oldLevel, PigW);
            var pigSize = flyer.rectTransform.sizeDelta;
            var start = _pig.rectTransform.anchoredPosition + new Vector2(0f, pigSize.y / 2f);
            flyer.rectTransform.anchoredPosition = start;
            var cracks = PiggyUi.Img<Image>(flyer.transform, "Cracks");
            Stretch(cracks.rectTransform);
            cracks.enabled = false;
            _pig.enabled = false;
            const float Big = 1.35f;
            var centre = Vector2.zero;

            var t = 0f;
            var stage = 0;
            List<Shard> shards = null, next = null;
            Image flash = null, rays = null, built = null;
            Text levelText = null;
            var flying = new List<(RectTransform rect, Vector2 from, float start)>();
            var walletAt = (Vector2)_fx.InverseTransformPoint(_walletGem.position);
            var walletShown = walletBefore;
            const int GemCount = 18;
            var rnd = new System.Random(crystals + oldLevel * 31);
            float Rand(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

            while (t < 5.6f)
            {
                t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                // the column's own rays and shine go while the burst has its rays, and come back with the new pig
                var back = t < 0.6f ? 1f - t / 0.6f : t < 5.1f ? 0f : (t - 5.1f) / 0.5f;
                _rays.canvasRenderer.SetAlpha(Mathf.Clamp01(back));
                _shine.canvasRenderer.SetAlpha(Mathf.Clamp01(back));
                // 1. the flight to the centre
                if (t < 0.6f)
                {
                    var e = EaseOut(t / 0.6f);
                    flyer.rectTransform.anchoredPosition = Vector2.Lerp(start, centre, e);
                    flyer.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, Big, e);
                }
                // 2. the shaking and the cracks
                else if (t < 1.9f)
                {
                    flyer.rectTransform.anchoredPosition = centre;
                    flyer.rectTransform.localScale = Vector3.one * Big;
                    var amp = Mathf.Lerp(3f, 12f, (t - 0.6f) / 1.3f);
                    flyer.rectTransform.anchoredPosition = centre + new Vector2(Mathf.Sin(t * 70f) * amp, Mathf.Sin(t * 53f) * amp * 0.4f);
                    flyer.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 47f) * amp * 0.35f);
                    var step = t >= 1.6f ? 3 : t >= 1.2f ? 2 : t >= 0.8f ? 1 : 0;
                    if (step > 0)
                    {
                        cracks.enabled = true;
                        cracks.sprite = PiggyUi.Sprite(Art + "Cracks" + (oldLevel + 1) + "_" + step);
                    }
                }
                // 3. the burst
                if (t >= 1.9f && stage == 0)
                {
                    stage = 1;
                    flyer.gameObject.SetActive(false);
                    rays = PiggyUi.Pic(_fx, Art + "Rays", centre, new Vector2(1100, 1100));
                    flash = PiggyUi.Pic(_fx, Art + "Shine", centre, new Vector2(900, 900));
                    var holder = Group(_fx, "Shards");
                    holder.localScale = Vector3.one * Big;
                    shards = MakeShards(holder, oldLevel, PigW);
                    for (var i = 0; i < shards.Count; i++)
                    {
                        var s = shards[i];
                        var dir = s.Home.sqrMagnitude > 1f ? s.Home.normalized : new Vector2(Rand(-1f, 1f), 1f).normalized;
                        s.Velocity = dir * Rand(500f, 950f) + new Vector2(0f, Rand(150f, 420f));
                        s.Spin = Rand(-360f, 360f);
                        shards[i] = s;
                    }
                }
                if (stage >= 1)
                {
                    var since = t - 1.9f;
                    if (flash != null)
                    {
                        flash.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - since / 0.45f));
                        flash.rectTransform.localScale = Vector3.one * (0.6f + since * 1.2f);
                    }
                    if (rays != null)
                    {
                        rays.color = new Color(1f, 1f, 1f, 0.6f * Mathf.Clamp01(1f - since / 1.5f));
                        rays.rectTransform.localRotation = Quaternion.Euler(0, 0, -since * 40f);
                    }
                    if (shards != null && t < 3.5f)
                    {
                        var fly = Mathf.Max(0f, t - 1.95f);
                        foreach (var s in shards)
                        {
                            s.Rect.anchoredPosition = s.Home + s.Velocity * fly + new Vector2(0f, -1600f) * (fly * fly * 0.5f);
                            s.Rect.localRotation = Quaternion.Euler(0, 0, s.Spin * fly);
                            s.Image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - (t - 2.6f) / 0.8f));
                        }
                    }
                    else if (shards != null)
                    {
                        foreach (var s in shards) if (s.Rect != null) Destroy(s.Rect.gameObject);
                        shards = null;
                    }
                }
                // 4. the crystals into the wallet
                if (t >= 2.0f)
                {
                    while (flying.Count < GemCount && t >= 2.0f + flying.Count * 0.06f)
                    {
                        var gem = PiggyUi.PicH(_fx, PiggyUi.Crystal, centre, 64f).rectTransform;
                        flying.Add((gem, centre + new Vector2(Rand(-160f, 160f), Rand(-120f, 160f)), 2.0f + flying.Count * 0.06f));
                    }
                    var arrived = 0;
                    foreach (var f in flying)
                    {
                        if (f.rect == null) { arrived++; continue; }
                        var k = (t - f.start) / 0.9f;
                        if (k >= 1f)
                        {
                            Destroy(f.rect.gameObject);
                            arrived++;
                            continue;
                        }
                        // out to its scatter point, then along a curve into the wallet
                        var p = k < 0.3f ? Vector2.Lerp(centre, f.from, EaseOut(k / 0.3f))
                            : Vector2.Lerp(f.from, walletAt, EaseInOut((k - 0.3f) / 0.7f)) + new Vector2(0f, Mathf.Sin(Mathf.PI * (k - 0.3f) / 0.7f) * 120f);
                        f.rect.anchoredPosition = p;
                        f.rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, k);
                    }
                    var shown = walletBefore + Mathf.RoundToInt(crystals * Mathf.Clamp01(arrived / (float)GemCount));
                    if (t >= 4.0f) shown = walletAfter;
                    if (shown != walletShown)
                    {
                        walletShown = shown;
                        _wallet.text = PiggyUi.Number(shown);
                    }
                }
                // 5. the next level's pig assembles
                if (t >= 3.8f && stage == 1)
                {
                    stage = 2;
                    levelText = PiggyUi.Stroke(PiggyUi.Label(_fx, Localization.Format("#piggy_level_up", newLevel + 1), 64, PiggyUi.Gold, centre + new Vector2(0f, 330f), new Vector2(700, 90)), 4f, PiggyUi.Ink);
                    var holder = Group(_fx, "Next");
                    holder.localScale = Vector3.one * Big;
                    next = MakeShards(holder, newLevel, PigW);
                    for (var i = 0; i < next.Count; i++)
                    {
                        var s = next[i];
                        var a = Rand(0f, Mathf.PI * 2f);
                        s.From = s.Home + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Rand(350f, 600f);
                        s.Spin = Rand(-200f, 200f);
                        next[i] = s;
                    }
                }
                if (stage == 2 && next != null)
                {
                    var k = EaseOut((t - 3.8f) / 0.95f);
                    foreach (var s in next)
                    {
                        s.Rect.anchoredPosition = Vector2.Lerp(s.From, s.Home, k);
                        s.Rect.localRotation = Quaternion.Euler(0, 0, s.Spin * (1f - k));
                        s.Image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(k * 2f));
                    }
                    var pop = Mathf.Clamp01((t - 3.8f) / 0.25f);
                    levelText.rectTransform.localScale = Vector3.one * (0.6f + 0.4f * EaseOut(pop) + 0.08f * Mathf.Sin(pop * Mathf.PI));
                    if (t >= 4.75f)
                    {
                        stage = 3;
                        foreach (var s in next) if (s.Rect != null) Destroy(s.Rect.parent.gameObject);
                        next = null;
                        built = PiggyUi.Img<Image>(_fx, "Built");
                        SetPig(built, newLevel, PigW);
                        built.rectTransform.anchoredPosition = centre;
                        built.rectTransform.localScale = Vector3.one * Big;
                    }
                }
                // 6. back to its place
                if (stage == 3 && built != null && t >= 5.1f)
                {
                    var k = EaseInOut((t - 5.1f) / 0.5f);
                    var home = R(PX, Feet) + new Vector2(0f, built.rectTransform.sizeDelta.y / 2f);
                    built.rectTransform.anchoredPosition = Vector2.Lerp(centre, home, k);
                    built.rectTransform.localScale = Vector3.one * Mathf.Lerp(Big, 1f, k);
                    if (levelText != null) levelText.color = new Color(PiggyUi.Gold.r, PiggyUi.Gold.g, PiggyUi.Gold.b, 1f - k);
                }
                yield return null;
            }

            for (var c = _fx.childCount - 1; c >= 0; c--) Destroy(_fx.GetChild(c).gameObject);
            _rays.canvasRenderer.SetAlpha(1f);
            _shine.canvasRenderer.SetAlpha(1f);
            _pig.enabled = true;
            _controls.gameObject.SetActive(true);
            foreach (var g in _gems) g.gameObject.SetActive(true);
            foreach (var h in _halos) h.gameObject.SetActive(true);
            _breaking = false;
            Redraw();
        }
    }
}
