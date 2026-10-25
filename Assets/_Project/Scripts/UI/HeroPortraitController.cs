using System.Collections.Generic;
using CityBuilder.CameraControl;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Invasion;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// A hero's portrait (top-right, where contentment used to be; an Invasion run first, the
    /// sandbox too since 2026-10-25): his
    /// face, his level, a health bar and an experience bar; greyed with a countdown while he is
    /// down; a gold ring while he is selected. A tap on it selects him -- the next tap on the map
    /// is his order -- and brings the camera to him. One per hero (the King, the Queen, the
    /// Captain, the user 2026-10-23), stacked down the right edge in that order; a hero not yet
    /// bought has none shown, and those below move up into the gap.
    ///
    /// Also answers the world tap that lands on a hero himself (WorldInputDispatcher), so he can
    /// be picked up where he stands, and draws the ring on the ground under him while selected.
    /// </summary>
    public class HeroPortraitController : MonoBehaviour
    {
        /// <summary>How close to a hero's figure on screen a tap has to land to pick him, at a 1080-high screen.</summary>
        private const float PickPixels = 70f;
        private const float RefreshSeconds = 0.1f;

        [SerializeField] private SoldierType heroType = SoldierType.King;
        [SerializeField] private GameObject widget;
        [SerializeField] private Text levelText;
        [SerializeField] private RectTransform healthFill;
        [SerializeField] private RectTransform xpFill;
        [SerializeField] private GameObject fallenOverlay;
        [SerializeField] private Text fallenText;
        [SerializeField] private GameObject selectedFrame;
        [SerializeField] private GameObject guardMark;
        /// <summary>Where the first portrait stands and how far down each next one does.</summary>
        [SerializeField] private float top;
        [SerializeField] private float step;

        private static readonly List<HeroPortraitController> Instances = new List<HeroPortraitController>();
        private bool _wasFallen;
        private bool _seen;
        private float _refreshIn;
        private LineRenderer _ring;

        public SoldierType HeroType => heroType;

        private Hero CurrentHero => HeroRoster.Instance != null ? HeroRoster.Instance.HeroOf(heroType) : null;

        private void Awake() => Instances.Add(this);

        private void OnDestroy()
        {
            Instances.Remove(this);
            if (Instances.Count == 0) ShownRows = 0;
        }

        private void Start()
        {
            if (widget != null) widget.SetActive(false);
        }

        /// <summary>The portrait tapped: select him and look at him.</summary>
        public void SelectHero()
        {
            var hero = CurrentHero;
            var army = ArmyManager.Instance;
            if (hero == null || army == null) return;
            if (!hero.IsFallen) army.SelectGroup(hero.Unit.Group);
            var rig = FindAnyObjectByType<RTSCameraController>();
            if (rig != null) rig.JumpTo(hero.IsFallen ? hero.Home : hero.transform.position);
        }

        /// <summary>A world tap on a hero himself selects him -- the nearest on screen. False when the tap was on none.</summary>
        public static bool TrySelectAt(Vector2 screenPosition, Camera camera)
        {
            var roster = HeroRoster.Instance;
            var army = ArmyManager.Instance;
            if (roster == null || camera == null || army == null || Instances.Count == 0) return false;
            var reach = PickPixels * Screen.height / 1080f;
            var best = reach * reach;
            Hero picked = null;
            foreach (var hero in roster.Heroes)
            {
                if (hero == null || hero.IsFallen) continue;
                var onScreen = camera.WorldToScreenPoint(hero.transform.position + Vector3.up * 0.4f);
                if (onScreen.z <= 0f) continue;
                var d = ((Vector2)onScreen - screenPosition).sqrMagnitude;
                if (d > best) continue;
                best = d;
                picked = hero;
            }
            if (picked == null) return false;
            army.SelectGroup(picked.Unit.Group);
            return true;
        }

        private void Update()
        {
            _refreshIn -= Time.unscaledDeltaTime;
            if (_refreshIn > 0f) return;
            _refreshIn = RefreshSeconds;
            Refresh();
        }

        private void Refresh()
        {
            var hero = CurrentHero;
            var has = hero != null && hero.Unit != null;
            if (widget != null && widget.activeSelf != has)
            {
                widget.SetActive(has);
                Stack();
            }
            if (!has) return;
            if (!_seen)
            {
                // a hero just bought steps out of the castle: show him
                _seen = true;
                if (hero.Type != SoldierType.King) ShowReturn(hero);
            }

            if (_wasFallen && !hero.IsFallen) ShowReturn(hero);
            _wasFallen = hero.IsFallen;

            var selected = ArmyManager.Instance != null && hero.Unit.Group != null && ArmyManager.Instance.SelectedGroup == hero.Unit.Group;
            if (levelText != null) levelText.text = hero.Level.ToString();
            SetFill(healthFill, hero.IsFallen ? 0f : hero.Unit.CurrentHealth / (float)Mathf.Max(1, hero.Unit.MaxHealth));
            var toNext = Hero.XpForLevel(hero.Level);
            SetFill(xpFill, hero.ExperienceToNext <= 0 ? 1f : hero.Experience / (float)Mathf.Max(1, toNext));
            if (fallenOverlay != null) fallenOverlay.SetActive(hero.IsFallen);
            if (fallenText != null && hero.IsFallen) fallenText.text = Mathf.CeilToInt(hero.RespawnRemaining).ToString();
            if (selectedFrame != null) selectedFrame.SetActive(selected && !hero.IsFallen);
            if (guardMark != null) guardMark.SetActive(hero.Guarding && !hero.IsFallen);
            ShowRing(hero, selected && !hero.IsFallen);
        }

        /// <summary>How many portraits stand down the right edge now (the army panel goes under them).</summary>
        public static int ShownRows { get; private set; }

        /// <summary>The lowest shown portrait's foot, in its anchored space (top-right).</summary>
        public static float StackBottom { get; private set; }

        /// <summary>The shown portraits down the right edge in hero order, with no gap for one not bought.</summary>
        private static void Stack()
        {
            Instances.Sort((a, b) => a.heroType.CompareTo(b.heroType));
            var row = 0;
            foreach (var portrait in Instances)
            {
                if (portrait.widget == null || !portrait.widget.activeSelf) continue;
                var rect = (RectTransform)portrait.widget.transform;
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, portrait.top - portrait.step * row);
                StackBottom = rect.anchoredPosition.y - rect.sizeDelta.y;
                row++;
            }
            ShownRows = row;
        }

        /// <summary>He is back (the user, 2026-10-22: the player must see it): the camera goes to him, a column of light, a line in the log.</summary>
        private static void ShowReturn(Hero hero)
        {
            var rig = FindAnyObjectByType<RTSCameraController>();
            if (rig != null) rig.JumpTo(hero.transform.position);
            RespawnFlash.Play(hero.transform.position);
            var line = hero.Type == SoldierType.King
                ? Localization.Get("#log_inv_king_back")
                : Localization.Format("#log_inv_hero_back", Localization.Get("#hero_" + hero.Id));
            EventLogManager.Instance?.Post(GameEvent.At(line, EventSeverity.Good, EventGlyph.Check, null, hero.transform.position, hero.transform));
        }

        private static void SetFill(RectTransform fill, float fraction)
        {
            if (fill == null) return;
            fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), fill.anchorMax.y);
        }

        /// <summary>A gold ring on the ground under the selected hero.</summary>
        private void ShowRing(Hero hero, bool show)
        {
            if (_ring == null)
            {
                if (!show) return;
                var go = new GameObject("HeroSelectionRing");
                _ring = go.AddComponent<LineRenderer>();
                _ring.useWorldSpace = false;
                _ring.loop = true;
                _ring.positionCount = 24;
                _ring.widthMultiplier = 0.05f;
                _ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _ring.receiveShadows = false;
                _ring.material = new Material(RuntimeShaders.Unlit) { color = UiTheme.Gold };
                for (var i = 0; i < 24; i++)
                {
                    var a = i / 24f * Mathf.PI * 2f;
                    _ring.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.45f, 0.04f, Mathf.Sin(a) * 0.45f));
                }
            }
            _ring.gameObject.SetActive(show);
            if (show) _ring.transform.position = hero.transform.position;
        }

        private void LateUpdate()
        {
            // the ring follows him between refreshes
            var hero = CurrentHero;
            if (_ring != null && _ring.gameObject.activeSelf && hero != null) _ring.transform.position = hero.transform.position;
        }
    }
}
