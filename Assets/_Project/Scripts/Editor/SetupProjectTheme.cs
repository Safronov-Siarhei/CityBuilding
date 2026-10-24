using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Resources;
using CityBuilder.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The interface's skin -- style "D" from the mockups (see UiTheme): parchment pills and panels
    /// with a dark-wood outline and a hard shadow, gold accents, Rubik, and icons rendered from the
    /// game's own models.
    ///
    /// The panels throughout this generator are still built the way they always were -- a panel
    /// sprite tinted dark, white text on it -- and ApplyTheme turns a finished canvas into the skin
    /// in one pass at the end, rather than each of some twenty panel builders choosing colours of
    /// its own. The HUD's own pieces (resource chips, tabs, hotbar cards, the round minimap) are
    /// built in the skin directly.
    /// </summary>
    public static partial class SetupProject
    {
        private static Sprite SkinSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{UiTextureImport.SkinFolder}/{name}.png");
        }

        /// <summary>An icon rendered from the models (Tools/blender/render_icons.py), or null when there is none.</summary>
        private static Sprite IconSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{UiTextureImport.IconsFolder}/{name}.png");
        }

        /// <summary>Makes sure the skin and icon files carry UiTextureImport's settings, the same way ForceReimportModels does for the models.</summary>
        private static void EnsureUiTextures()
        {
            foreach (var folder in new[] { UiTextureImport.SkinFolder, UiTextureImport.IconsFolder, UiTextureImport.GlyphsFolder, UiTextureImport.HudSkinsFolder, UiTextureImport.MenuSkinsFolder, UiTextureImport.MenuArtFolder })
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    AssetDatabase.ImportAsset(folder, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
                }
            }
        }

        private static bool IsWoodSprite(Sprite sprite)
        {
            return sprite != null && (sprite.name == "pill_wood" || sprite.name == "pill_wood_light");
        }

        private static bool IsSkinSprite(Sprite sprite)
        {
            return sprite != null && AssetDatabase.GetAssetPath(sprite).StartsWith(UiTextureImport.SkinFolder);
        }

        /// <summary>
        /// Turns a canvas built in the old style into the skin. Every image showing the panel
        /// sprite becomes a parchment panel -- or a pill when it is a button -- in its own colours
        /// (white, untinted). Every label takes Rubik, and its colour from what it is written on:
        /// on parchment the dark counterpart of the colour it was given (UiTheme.OnParchment), on
        /// wood the parchment colour, floating over the world it keeps its light colour and shadow.
        /// The translucent wells inside panels (a list's background) become a faint wood shade.
        /// </summary>
        private static void ApplyTheme(Transform root)
        {
            var panel = SkinSprite("panel");
            var pill = SkinSprite("pill_parchment");
            if (panel == null || pill == null)
            {
                Debug.LogWarning("ApplyTheme: the UI skin is missing (Assets/_Project/UI/Skin, run Tools/ui/make_skin.py) -- the interface keeps its old look.");
                return;
            }

            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == panel)
                {
                    if (image.GetComponent<Button>() != null) image.sprite = pill;
                    image.type = Image.Type.Sliced;
                    image.color = Color.white;
                    continue;
                }

                // A plain dark well inside a panel (a scroll list's viewport) is a shade of wood on
                // parchment, not a hole of black.
                if (image.sprite == null && image.color.a < 0.5f && image.color.maxColorComponent < 0.2f
                    && BackingImage(image.transform.parent) != null)
                {
                    image.color = UiTheme.Rgb(0x5a3218, 0.12f);
                }
            }

            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                ApplyFont(text);

                // A caption is one line on a button sized for it. Rubik's line is taller than
                // Arial's, and a legacy Text whose line does not fit its box draws NOTHING rather
                // than a clipped line -- which left the tax control's 52-unit +/- buttons blank.
                if (text.transform.parent != null && text.transform.parent.GetComponent<Button>() != null)
                {
                    text.verticalOverflow = VerticalWrapMode.Overflow;
                }

                var backing = BackingImage(text.transform.parent);
                if (backing == null) continue;          // over the world: keeps its light colour and shadow

                text.color = IsWoodSprite(backing.sprite) ? UiTheme.OnWood : UiTheme.OnParchment(text.color);
                foreach (var shadow in text.GetComponents<Shadow>()) Object.DestroyImmediate(shadow);
            }

            foreach (var input in root.GetComponentsInChildren<InputField>(true))
            {
                if (input.textComponent != null) input.textComponent.color = UiTheme.Ink;
            }
        }

        private static void ApplyFont(Text text)
        {
            if (text.fontStyle == FontStyle.Bold || text.fontStyle == FontStyle.BoldAndItalic)
            {
                text.font = UiTheme.Bold;
                text.fontStyle = text.fontStyle == FontStyle.BoldAndItalic ? FontStyle.Italic : FontStyle.Normal;
            }
            else if (text.font == null || text.font.name == "LegacyRuntime")
            {
                text.font = UiTheme.Regular;
            }
        }

        /// <summary>The nearest image above a label that is drawn in the skin -- what the label is written on.</summary>
        private static Image BackingImage(Transform from)
        {
            for (var t = from; t != null; t = t.parent)
            {
                var image = t.GetComponent<Image>();
                if (image != null && IsSkinSprite(image.sprite)) return image;
                if (t.GetComponent<Canvas>() != null) return null;
            }
            return null;
        }

        /// <summary>An image showing one of the skin's sprites, 9-sliced, untinted.</summary>
        private static Image CreateSkinImage(Transform parent, string name, string sprite, Vector2 anchoredPos, Vector2 size)
        {
            var image = CreateImage(parent, name, Color.white);
            image.sprite = SkinSprite(sprite);
            image.type = Image.Type.Sliced;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            return image;
        }

        /// <summary>A button on a skin sprite with a caption from the localization sheet.</summary>
        private static Button CreateSkinButton(Transform parent, string sprite, string name, string labelKey, Vector2 anchoredPos, Vector2 size)
        {
            var image = CreateSkinImage(parent, name, sprite, anchoredPos, size);
            var button = image.gameObject.AddComponent<Button>();
            var label = Localize(CreateText(image.transform, "Label", string.Empty, 26, new Vector2(0f, 3f), size - new Vector2(24f, 12f)), labelKey);
            label.font = UiTheme.Bold;
            label.color = IsWoodSprite(image.sprite) ? UiTheme.OnWood : UiTheme.Ink;
            return button;
        }

        /// <summary>
        /// One building on the hotbar as a card: its rendered icon, its name, what it costs -- the
        /// mockup's card, so the player reads the price before picking rather than after.
        /// </summary>
        private static Button CreateHotbarCard(Transform parent, BuildingData data, Vector2 size)
        {
            var card = HudImage(parent, $"Building_{data.buildingName}", "card", size);
            var button = card.gameObject.AddComponent<Button>();

            var icon = CreateImage(card.transform, "Icon", Color.white);
            icon.sprite = CreateBuildingIcon(data.buildingName);
            icon.preserveAspect = true;
            var iconRect = icon.rectTransform;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -6f);
            iconRect.sizeDelta = new Vector2(size.x - 18f, size.x - 18f);

            var name = Localize(CreateText(card.transform, "Name", data.displayName, 19, new Vector2(0f, -size.y * 0.5f + 46f), new Vector2(size.x - 8f, 26f), UiTheme.Ink),
                "#building_" + data.buildingName.ToLowerInvariant());
            name.font = UiTheme.Bold;
            SkinInk(name, HudInk.OnCard);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 13;
            name.resizeTextMaxSize = 19;

            // The price, icon and number per resource, centred under the name.
            var row = new GameObject("Cost", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(card.transform, false);
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = rowRect.anchorMax = new Vector2(0.5f, 0f);
            rowRect.pivot = new Vector2(0.5f, 0f);
            rowRect.anchoredPosition = new Vector2(0f, 6f);
            rowRect.sizeDelta = new Vector2(size.x - 6f, 26f);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 2f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var shown = 0;
            foreach (var cost in data.cost)
            {
                if (cost.amount <= 0 || shown >= 3) continue;
                shown++;
                CreateCardPriceChip(row.transform, cost.type, cost.amount);
            }
            // An Invasion run sets the card to the run's price (BuildingCategoryPanel.ShowRunPrice),
            // which may name wood, stone or gold where the sandbox's has none: their chips wait hidden.
            foreach (var type in new[] { ResourceType.Wood, ResourceType.Stone, ResourceType.Coins })
            {
                if (row.transform.Find($"Icon_{type}") != null) continue;
                var (chipIcon, chipAmount) = CreateCardPriceChip(row.transform, type, 0);
                chipIcon.gameObject.SetActive(false);
                chipAmount.gameObject.SetActive(false);
            }
            return button;
        }

        private static (Image icon, Text amount) CreateCardPriceChip(Transform row, ResourceType type, int value)
        {
            var costIcon = CreateImage(row, $"Icon_{type}", Color.white);
            costIcon.sprite = CreateResourceIcon(type);
            costIcon.preserveAspect = true;
            costIcon.rectTransform.sizeDelta = new Vector2(24f, 24f);
            var amount = CreateText(row, $"Amount_{type}", value.ToString(), 18, Vector2.zero, new Vector2(value >= 100 ? 36f : 26f, 26f), UiTheme.Ink);
            amount.font = UiTheme.Heavy;
            amount.alignment = TextAnchor.MiddleLeft;
            SkinInk(amount, HudInk.OnCard);
            return (costIcon, amount);
        }

        /// <summary>The building whose rendered icon stands for a whole category on its tab.</summary>
        private static readonly Dictionary<BuildingCategory, string> CategoryIconBuilding = new Dictionary<BuildingCategory, string>
        {
            { BuildingCategory.City, "Castle" },
            { BuildingCategory.Storage, "Warehouse" },
            { BuildingCategory.Entertainment, "Tavern" },
            { BuildingCategory.Defence, "Tower" },
            { BuildingCategory.Production, "IronMine" },
            { BuildingCategory.Food, "Farm" },
        };
    }
}
