using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.Tutorial
{
    /// <summary>
    /// What the tutorial does to the screen (the user's first playtest, 2026-11-05; tutorial.md):
    /// the HUD cut down to the King's portrait, the menu button, the people counter and the build
    /// button (nothing at all but the hint while the Town Hall is placed); the build button blinking
    /// on a build step, an arrow over the card to build; a blinking gold ring on whatever the player
    /// should tap in the world. Owned by TutorialDirector; nothing here runs outside the tutorial.
    ///
    /// The HUD is hidden through a CanvasGroup on each element (alpha 0, no taps), not SetActive:
    /// their own controllers switch them on and off, and would switch them back.
    /// </summary>
    public sealed class TutorialHud
    {
        // gone for the whole tutorial: armies, people/inventory windows, Day, minimap, happiness,
        // the event feed, the rotate and cancel of placing, the selection badge, hero skills
        private static readonly string[] AlwaysHidden =
        {
            "ArmyButton", "ArmySquadRow", "ArmyMachineRow", "WorkforceButton", "InventoryButton",
            "SettlementStatus", "Minimap", "MinimapRing", "Happiness", "HappinessBreakdown", "Migration",
            "EventFeed", "SelectionBadge", "HeroSkillBar", "SkillHint", "RotateButton", "CancelPlacementButton",
            "Formation1", "Formation2", "Formation3", "Formation4", "Formation5", "Formation6",
            // the selection's cancel: neither the King nor the soldier needs it here (the user, 2026-11-06)
            "CancelSelectionButton",
        };

        // the game menu: no saving in the tutorial; the entries below it move up into its place (the user, 2026-11-06)
        private const string SaveEntry = "SaveEntry";
        private static readonly string[] BelowSave = { "SettingsEntry", "MainMenuEntry" };
        private const float MenuEntryStep = 112f;

        // and while the Town Hall is placed: the resource bar and the hero portraits too
        private static readonly string[] HiddenBeforeHall = { "ResourceHUD", "Heroes" };

        private static readonly Color Yellow = new Color(1f, 0.85f, 0.2f, 1f);

        private readonly List<CanvasGroup> _always = new List<CanvasGroup>();
        private readonly List<CanvasGroup> _beforeHall = new List<CanvasGroup>();
        private readonly Transform _uiRoot;

        private RectTransform _tab;
        private Graphic _tabGraphic;
        private Color _tabColour;
        private Vector3 _tabScale;
        private bool _tabBlinking;

        private Image _halo;               // the glow behind the blinking build button
        private readonly Sprite _circle;

        private RectTransform _arrow;
        private LineRenderer _ring;
        private float _clock;

        public TutorialHud(Transform uiRoot, Sprite circle)
        {
            _uiRoot = uiRoot;
            _circle = circle;
            var always = new HashSet<string>(AlwaysHidden);
            var beforeHall = new HashSet<string>(HiddenBeforeHall);
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas == null || !canvas.isRootCanvas) continue;
                foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
                {
                    if (always.Contains(t.name)) _always.Add(GroupOf(t));
                    else if (beforeHall.Contains(t.name)) _beforeHall.Add(GroupOf(t));
                    else if (t.name == SaveEntry) t.gameObject.SetActive(false);
                    else if (System.Array.IndexOf(BelowSave, t.name) >= 0 && t is RectTransform entry)
                        entry.anchoredPosition += new Vector2(0f, MenuEntryStep);
                }
            }
        }

        private static CanvasGroup GroupOf(Transform t)
        {
            var group = t.GetComponent<CanvasGroup>();
            if (group == null) group = t.gameObject.AddComponent<CanvasGroup>();
            // a hidden button in a row leaves no gap there
            var layout = t.GetComponent<LayoutElement>();
            if (layout == null) layout = t.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            return group;
        }

        /// <summary>Hides what the step does not show (called every check: other code may touch the groups).</summary>
        public void ApplyHidden(bool hallPlaced)
        {
            foreach (var g in _always) Hide(g, true);
            foreach (var g in _beforeHall) Hide(g, !hallPlaced);
        }

        /// <summary>
        /// The whole screen away but these (by name) -- the victory (the user, 2026-11-06: only the King's
        /// line and the victory panel). A branch holding one of them is gone into, the rest of it hidden.
        /// </summary>
        public void HideAllBut(params string[] keep)
        {
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas != null && canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace) HideBranch(canvas.transform, keep);
        }

        private static void HideBranch(Transform branch, string[] keep)
        {
            for (var i = 0; i < branch.childCount; i++)
            {
                var child = branch.GetChild(i);
                if (System.Array.IndexOf(keep, child.name) >= 0) continue;
                if (Holds(child, keep)) HideBranch(child, keep);
                else if (child.gameObject.activeSelf) Hide(GroupOf(child), true);
            }
        }

        private static bool Holds(Transform branch, string[] keep)
        {
            foreach (var t in branch.GetComponentsInChildren<Transform>(true))
                if (t != branch && System.Array.IndexOf(keep, t.name) >= 0) return true;
            return false;
        }

        private static void Hide(CanvasGroup group, bool hide)
        {
            if (group == null) return;
            var alpha = hide ? 0f : 1f;
            if (group.alpha != alpha) group.alpha = alpha;
            if (group.blocksRaycasts == hide) group.blocksRaycasts = !hide;
            if (group.interactable == hide) group.interactable = !hide;
        }

        /// <summary>
        /// Every frame: the build button blinks when `blinkTab`; the arrow stands over `card` (null:
        /// none); the ring is round `ringAt` with `ringRadius` (radius 0: none).
        /// </summary>
        public void Tick(RectTransform tab, bool blinkTab, RectTransform card, Vector3 ringAt, float ringRadius)
        {
            var period = Mathf.Max(0.1f, BalanceConfig.Instance.TutorialBlinkSeconds);
            _clock += Time.unscaledDeltaTime;
            var pulse = 0.5f + 0.5f * Mathf.Sin(_clock * Mathf.PI / period);   // 0..1, one blink per `period`
            BlinkTab(tab, blinkTab, pulse);
            PointAt(card);
            Ring(ringAt, ringRadius, pulse);
        }

        // ------------------------------------------------------------ the build button

        private void BlinkTab(RectTransform tab, bool blink, float pulse)
        {
            if (tab != _tab)
            {
                StopTab();
                _tab = tab;
                _tabGraphic = tab != null ? tab.GetComponent<Graphic>() : null;
                if (_tabGraphic != null) _tabColour = _tabGraphic.color;
                if (tab != null) _tabScale = tab.localScale;
            }
            if (_tab == null) return;
            if (!blink)
            {
                StopTab();
                return;
            }
            _tabBlinking = true;
            _tab.localScale = _tabScale * (1f + 0.22f * pulse);
            if (_tabGraphic != null) _tabGraphic.color = Color.Lerp(_tabColour, Yellow, pulse);
            // a gold glow swelling behind it: the colour alone hardly showed (the user, 2026-11-06)
            if (_halo == null) _halo = BuildHalo(_tab);
            if (_halo == null) return;
            if (!_halo.gameObject.activeSelf) _halo.gameObject.SetActive(true);
            var size = Mathf.Max(_tab.rect.width, _tab.rect.height);
            _halo.rectTransform.position = _tab.TransformPoint(_tab.rect.center);
            _halo.rectTransform.sizeDelta = Vector2.one * size * (1.25f + 0.45f * pulse);
            var c = Yellow;
            c.a = 0.95f - 0.55f * pulse;
            _halo.color = c;
        }

        private Image BuildHalo(RectTransform tab)
        {
            if (_circle == null || tab.parent == null) return null;
            var go = new GameObject("TutorialTabGlow", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            go.transform.SetParent(tab.parent, false);
            go.transform.SetSiblingIndex(tab.GetSiblingIndex());   // behind the button
            var image = go.GetComponent<Image>();
            image.sprite = _circle;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            return image;
        }

        private void StopTab()
        {
            if (_halo != null && _halo.gameObject.activeSelf) _halo.gameObject.SetActive(false);
            if (!_tabBlinking || _tab == null) return;
            _tabBlinking = false;
            _tab.localScale = _tabScale;
            if (_tabGraphic != null) _tabGraphic.color = _tabColour;
        }

        // ------------------------------------------------------------ the arrow

        private void PointAt(RectTransform card)
        {
            if (card == null)
            {
                if (_arrow != null && _arrow.gameObject.activeSelf) _arrow.gameObject.SetActive(false);
                return;
            }
            if (_arrow == null) _arrow = BuildArrow();
            if (!_arrow.gameObject.activeSelf) _arrow.gameObject.SetActive(true);
            _arrow.SetAsLastSibling();
            var bob = Mathf.Abs(Mathf.Sin(_clock * 4f)) * 24f;
            var top = card.TransformPoint(new Vector3(card.rect.center.x, card.rect.yMax, 0f));
            _arrow.position = top;
            _arrow.anchoredPosition += new Vector2(0f, 50f + bob);
        }

        private RectTransform BuildArrow()
        {
            var go = new GameObject("TutorialArrow", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_uiRoot, false);
            var image = go.GetComponent<Image>();
            image.sprite = ArrowSprite();
            image.color = Yellow;
            image.raycastTarget = false;
            go.AddComponent<Outline>().effectColor = new Color(0.3f, 0.15f, 0f, 1f);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(90f, 90f);
            return rect;
        }

        /// <summary>A plain down-pointing triangle, drawn once.</summary>
        private static Sprite ArrowSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                var half = (y + 0.5f) / size * (size * 0.5f);   // narrow at the bottom (the tip), wide at the top
                for (var x = 0; x < size; x++)
                {
                    var inside = Mathf.Abs(x + 0.5f - size * 0.5f) <= half;
                    pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0f));
        }

        // ------------------------------------------------------------ the ring in the world

        private const int RingPoints = 40;

        private void Ring(Vector3 at, float radius, float pulse)
        {
            if (radius <= 0f)
            {
                if (_ring != null && _ring.gameObject.activeSelf) _ring.gameObject.SetActive(false);
                return;
            }
            if (_ring == null) _ring = BuildRing();
            if (!_ring.gameObject.activeSelf) _ring.gameObject.SetActive(true);
            _ring.transform.position = at + Vector3.up * 0.06f;
            var r = radius * (1f + 0.08f * pulse);
            for (var i = 0; i < RingPoints; i++)
            {
                var a = i / (float)RingPoints * Mathf.PI * 2f;
                _ring.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }
            _ring.widthMultiplier = 0.08f + 0.1f * pulse;
            var c = Yellow;
            c.a = 0.35f + 0.65f * pulse;
            _ring.startColor = _ring.endColor = c;
            _ring.sharedMaterial.color = Color.Lerp(Color.white, Yellow, 0.4f + 0.6f * pulse);
        }

        private static LineRenderer BuildRing()
        {
            var go = new GameObject("TutorialRing");
            var ring = go.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = RingPoints;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            ring.sharedMaterial = new Material(RuntimeShaders.Unlit) { color = Yellow };
            return ring;
        }

        public void Dispose()
        {
            StopTab();
            if (_arrow != null) Object.Destroy(_arrow.gameObject);
            if (_halo != null) Object.Destroy(_halo.gameObject);
            if (_ring != null) Object.Destroy(_ring.gameObject);
        }
    }
}
