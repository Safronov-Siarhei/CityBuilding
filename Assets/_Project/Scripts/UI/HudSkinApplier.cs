using System;
using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>What a label or a glyph is written on, which decides its colour in each look.</summary>
    public enum HudInk
    {
        /// <summary>Leave the colour alone (a picture in its own colours, a tint set by code).</summary>
        Keep = 0,
        /// <summary>The menu button.</summary>
        OnMain,
        /// <summary>A secondary round button: citizens, cancel, rotate.</summary>
        OnRound,
        /// <summary>The tax's minus and plus.</summary>
        OnStep,
        /// <summary>The menu's entries.</summary>
        OnAlt,
        /// <summary>A resource's number.</summary>
        OnChip,
        /// <summary>A plaque: the tax rate.</summary>
        OnPlaque,
        /// <summary>A building card: its name and price.</summary>
        OnCard,

        // The main menu's surfaces (Resources/MenuSkins), added after the HUD's so saved values keep their meaning.

        /// <summary>A menu window, a mode card's lower part, a list row.</summary>
        OnPanel,
        /// <summary>The plate a name sits on: a mode card's title, a section's heading.</summary>
        OnTitle,
        /// <summary>The call to action: Play, Load, Fight.</summary>
        OnButton,
        /// <summary>The menu's small label plates: "Continue", the points counter.</summary>
        OnMenuPlaque,
        /// <summary>A tab not picked.</summary>
        OnTab,
        /// <summary>The tab picked.</summary>
        OnTabSelected,
    }

    /// <summary>The HUD's looks as data: where their sprites live, and the ink each writes in.</summary>
    public static class HudSkins
    {
        /// <summary>Under Resources: a folder per look, the same file name per role in each (Tools/ui/make_hud_skins.py).</summary>
        public const string ResourcesFolder = "HudSkins";

        public static readonly HudSkin[] All = { HudSkin.Classic, HudSkin.Soft, HudSkin.Glass, HudSkin.Dock };

        public static HudSkin Current => GameSettings.HudSkin;

        /// <summary>Dock is the one look that also moves the pieces.</summary>
        public static bool IsDockLayout(HudSkin skin) => skin == HudSkin.Dock;

        private static readonly Color SoftInk = UiTheme.Rgb(0x4a3424);
        private static readonly Color Cream = UiTheme.Rgb(0xfbf1dc);
        private static readonly Color GlassGold = UiTheme.Rgb(0xf0c46a);
        private static readonly Color DarkOnGold = UiTheme.Rgb(0x4a2810);

        public static Color Ink(HudInk role) => Ink(Current, role);

        public static Color Ink(HudSkin skin, HudInk role)
        {
            if (role >= HudInk.OnPanel) return MenuInk(skin, role);
            switch (skin)
            {
                case HudSkin.Soft:
                    return role == HudInk.OnMain || role == HudInk.OnRound || role == HudInk.OnStep ? Color.white : SoftInk;
                case HudSkin.Glass:
                    return role == HudInk.OnMain || role == HudInk.OnStep ? GlassGold : Cream;
                case HudSkin.Dock:
                    switch (role)
                    {
                        case HudInk.OnMain: return DarkOnGold;
                        case HudInk.OnStep: return UiTheme.Rgb(0x3f2210);
                        case HudInk.OnRound: return UiTheme.OnWood;
                        case HudInk.OnChip: return UiTheme.Rgb(0xfbefcf);
                        default: return UiTheme.Ink;
                    }
                default:
                    switch (role)
                    {
                        case HudInk.OnMain: return DarkOnGold;
                        case HudInk.OnRound:
                        case HudInk.OnStep: return UiTheme.OnWood;
                        default: return UiTheme.Ink;
                    }
            }
        }

        /// <summary>The menu's inks: the wood looks write light on wood and dark on parchment, Soft white on its orange and brown on cream, Glass light on its dark glass throughout.</summary>
        private static Color MenuInk(HudSkin skin, HudInk role)
        {
            switch (skin)
            {
                case HudSkin.Soft:
                    return role == HudInk.OnTitle || role == HudInk.OnButton || role == HudInk.OnTabSelected ? Color.white : SoftInk;
                case HudSkin.Glass:
                    return role == HudInk.OnTitle || role == HudInk.OnButton ? GlassGold : Cream;
                default:
                    switch (role)
                    {
                        case HudInk.OnTitle:
                        case HudInk.OnMenuPlaque:
                        case HudInk.OnTab: return UiTheme.OnWood;
                        case HudInk.OnButton: return DarkOnGold;
                        default: return UiTheme.Ink;
                    }
            }
        }

        /// <summary>
        /// A status colour (UiTheme.Good, Warning, Danger, InkSoft -- chosen for a light surface)
        /// as the current look writes it. Glass is the one dark surface: there the colour keeps its
        /// hue but is lifted light enough to read on it.
        /// </summary>
        public static Color Status(Color onLight)
        {
            if (Current != HudSkin.Glass) return onLight;
            Color.RGBToHSV(onLight, out var h, out var s, out _);
            var lifted = Color.HSVToRGB(h, s * 0.55f, 0.95f);
            lifted.a = onLight.a;
            return lifted;
        }
    }

    /// <summary>An image the look draws: a role (the sprite's file name in each look's folder) and, for a glyph, its ink.</summary>
    [Serializable]
    public struct HudSkinImage
    {
        public Image image;
        /// <summary>Empty for a glyph that only takes the ink.</summary>
        public string role;
        public HudInk ink;
    }

    [Serializable]
    public struct HudSkinLabel
    {
        public Text text;
        public HudInk ink;
    }

    /// <summary>Where a RectTransform stands -- everything its anchoring is made of.</summary>
    [Serializable]
    public struct HudRect
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 position;
        public Vector2 size;

        public static HudRect Of(RectTransform rect)
        {
            return new HudRect
            {
                anchorMin = rect.anchorMin, anchorMax = rect.anchorMax, pivot = rect.pivot,
                position = rect.anchoredPosition, size = rect.sizeDelta,
            };
        }

        public void ApplyTo(RectTransform rect)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }

    /// <summary>A HUD piece that stands somewhere else in the Dock layout.</summary>
    [Serializable]
    public struct HudLayoutEntry
    {
        public RectTransform rect;
        public HudRect sketch;
        public HudRect dock;
    }

    /// <summary>
    /// Puts the HUD in the look picked in the settings (GameSettings.HudSkin), and again whenever
    /// the pick changes. Everything it touches was listed when the scene was generated
    /// (SetupProject), so a switch is one pass over those lists -- no search of the hierarchy, and
    /// nothing at all between switches: the HUD costs the same per frame in every look.
    ///
    /// Only the picked look's sprites are loaded (Resources, one folder per look); after a switch
    /// the previous look's become unreferenced and are unloaded.
    /// </summary>
    public class HudSkinApplier : MonoBehaviour
    {
        /// <summary>Under Resources: HudSkins for the HUD, MenuSkins for the main menu -- a folder per look either way.</summary>
        [SerializeField] private string resourcesFolder = HudSkins.ResourcesFolder;
        [SerializeField] private HudSkinImage[] images;
        [SerializeField] private HudSkinLabel[] labels;
        [SerializeField] private HudLayoutEntry[] layout;

        [Header("Build menu")]
        [SerializeField] private BuildingCategoryPanel buildMenu;
        [SerializeField] private float sketchClosedBarY;
        [SerializeField] private float sketchOpenBarY;
        [SerializeField] private float dockClosedBarY;
        [SerializeField] private float dockOpenBarY;

        private bool _applied;
        private HudSkin _skin;

        /// <summary>The look the HUD is drawn in right now.</summary>
        public HudSkin Applied => _skin;

        private void OnEnable()
        {
            GameSettings.OnChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            GameSettings.OnChanged -= Apply;
        }

        public void Apply()
        {
            var skin = GameSettings.HudSkin;
            if (_applied && skin == _skin) return;
            var switching = _applied;
            _applied = true;
            _skin = skin;

            var sprites = Load(resourcesFolder, skin);
            if (images != null)
            {
                foreach (var entry in images)
                {
                    if (entry.image == null) continue;
                    if (!string.IsNullOrEmpty(entry.role))
                    {
                        sprites.TryGetValue(entry.role, out var sprite);
                        entry.image.sprite = sprite;
                        // A role this look does not draw (Classic has no bar behind the resources,
                        // only Dock has rails) hides the image rather than showing a white box.
                        entry.image.enabled = sprite != null;
                        if (entry.ink == HudInk.Keep) entry.image.color = Color.white;
                    }
                    if (entry.ink != HudInk.Keep) entry.image.color = HudSkins.Ink(skin, entry.ink);
                }
            }

            if (labels != null)
            {
                foreach (var entry in labels)
                {
                    if (entry.text != null) entry.text.color = HudSkins.Ink(skin, entry.ink);
                }
            }

            var dock = HudSkins.IsDockLayout(skin);
            if (layout != null)
            {
                foreach (var entry in layout)
                {
                    if (entry.rect != null) (dock ? entry.dock : entry.sketch).ApplyTo(entry.rect);
                }
            }

            if (buildMenu != null)
            {
                sprites.TryGetValue("tab_selected", out var selected);
                sprites.TryGetValue("tab", out var idle);
                buildMenu.SetLook(selected, idle, dock ? dockClosedBarY : sketchClosedBarY, dock ? dockOpenBarY : sketchOpenBarY);
            }

            if (switching) UnityEngine.Resources.UnloadUnusedAssets();
        }

        private static Dictionary<string, Sprite> Load(string folder, HudSkin skin)
        {
            var result = new Dictionary<string, Sprite>();
            foreach (var sprite in UnityEngine.Resources.LoadAll<Sprite>($"{folder}/{skin}"))
            {
                result[sprite.name] = sprite;
            }
            return result;
        }
    }
}
