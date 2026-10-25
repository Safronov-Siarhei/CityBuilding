using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The main menu's helpers and dialogs; the screens after the user's sketches (2026-10-25) are in
    /// SetupProjectMainMenuScreens -- see MainMenuController for the screens and the steps between them. Everything is drawn in the menu's own sprites
    /// (Resources/MenuSkins, Tools/ui/make_menu_kit.py), one set per HUD look, and a HudSkinApplier
    /// reading that folder switches the whole menu when the player picks another look in the settings.
    ///
    /// The map cards and the invasion's picture are taken in the game itself
    /// (Assets/_Project/UI/MenuArt, MenuArtBake); while one is missing its place stays dark.
    /// </summary>
    public static partial class SetupProject
    {
        private static Sprite MenuSprite(string role)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{UiTextureImport.MenuSkinsFolder}/{GameSettings.DefaultHudSkin}/{role}.png");
        }

        private static Sprite MenuArt(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{UiTextureImport.MenuArtFolder}/{name}.png");
        }

        /// <summary>An image drawn by the menu's look; centred, untinted.</summary>
        private static Image MenuImage(Transform parent, string name, string role, Vector2 position, Vector2 size, Image.Type type = Image.Type.Sliced)
        {
            var image = CreateImage(parent, name, Color.white);
            Anchor(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            image.sprite = MenuSprite(role);
            image.type = type;
            SkinImages.Add(new HudSkinImage { image = image, role = role, ink = HudInk.Keep });
            return image;
        }

        private static Image MenuGlyph(Transform parent, string glyph, HudInk ink, float size)
        {
            var icon = CreateImage(parent, "Icon", Color.white);
            icon.sprite = HudGlyph(glyph);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Anchor(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(size, size));
            // a soft dark under the glyph, so it reads as pressed into the button
            var shadow = icon.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -3f);
            SkinInk(icon, ink);
            return icon;
        }

        /// <summary>A label written in the look's ink for what it stands on; `key` null for one the controller writes.</summary>
        private static Text MenuLabel(Transform parent, string name, string key, int size, Vector2 position, Vector2 box, HudInk ink, bool bold = false, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var text = CreateText(parent, name, string.Empty, size, position, box);
            if (key != null) Localize(text, key);
            text.font = bold ? UiTheme.Bold : UiTheme.Regular;
            text.alignment = align;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            SkinInk(text, ink);
            return text;
        }

        private static Button MenuRoundButton(Transform parent, string name, string role, string glyph, HudInk ink, float size, float glyphScale = 0.46f)
        {
            var image = MenuImage(parent, name, role, Vector2.zero, new Vector2(size, size), Image.Type.Simple);
            image.preserveAspect = true;
            var button = image.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            MenuGlyph(image.transform, glyph, ink, size * glyphScale);
            return button;
        }

        private static Button MenuTextButton(Transform parent, string name, string role, string key, HudInk ink, Vector2 position, Vector2 size, int fontSize = 38)
        {
            var image = MenuImage(parent, name, role, position, size);
            var button = image.gameObject.AddComponent<Button>();
            MenuLabel(image.transform, "Label", key, fontSize, new Vector2(0f, 4f), size - new Vector2(40f, 20f), ink, bold: true);
            return button;
        }

        /// <summary>A tab: the idle look, with the picked look stacked over it and switched on by the controller.</summary>
        private static (Button button, GameObject selected) MenuTab(Transform parent, string name, string key, Vector2 position, Vector2 size)
        {
            var idle = MenuImage(parent, name, "tab", position, size);
            var button = idle.gameObject.AddComponent<Button>();
            MenuLabel(idle.transform, "Label", key, 32, new Vector2(0f, 3f), size - new Vector2(30f, 10f), HudInk.OnTab, bold: true);
            var picked = MenuImage(idle.transform, "Selected", "tab_selected", Vector2.zero, size);
            picked.raycastTarget = false;
            MenuLabel(picked.transform, "Label", key, 32, new Vector2(0f, 3f), size - new Vector2(30f, 10f), HudInk.OnTabSelected, bold: true);
            picked.gameObject.SetActive(false);
            return (button, picked.gameObject);
        }

        private static CanvasGroup Group(GameObject go)
        {
            var group = go.GetComponent<CanvasGroup>();
            return group != null ? group : go.AddComponent<CanvasGroup>();
        }

        private static RectTransform StretchedChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            StretchFull(rect);
            return rect;
        }

        // ---------------------------------------------------------------- dialogs

        private static GameObject BuildMenuDialog(Transform canvas, string name, string titleKey, string textKey, Vector2 size, out Transform card)
        {
            var root = StretchedChild(canvas, name);
            var backdrop = CreateImage(root, "Backdrop", new Color(0.04f, 0.02f, 0.01f, 0.65f));
            StretchFull(backdrop.rectTransform);
            var panel = MenuImage(root, "Card", "panel", Vector2.zero, size, Image.Type.Tiled);
            card = panel.transform;
            var title = MenuImage(card, "TitlePlate", "title", new Vector2(0f, size.y * 0.5f - 6f), new Vector2(560f, 110f));
            MenuLabel(title.transform, "Title", titleKey, 42, new Vector2(0f, 4f), new Vector2(500f, 70f), HudInk.OnTitle, bold: true);
            if (textKey != null) MenuLabel(card, "Text", textKey, 32, new Vector2(0f, size.y * 0.5f - 150f), new Vector2(size.x - 160f, 90f), HudInk.OnPanel);
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        /// <summary>The menu's own HudSkinApplier: the same switch the HUD has, reading Resources/MenuSkins.</summary>
        private static void BuildMenuSkinApplier(Transform canvas)
        {
            var applier = canvas.gameObject.AddComponent<HudSkinApplier>();
            var so = new SerializedObject(applier);
            so.FindProperty("resourcesFolder").stringValue = MenuRowLook.ResourcesFolder;
            var images = so.FindProperty("images");
            images.arraySize = SkinImages.Count;
            for (var i = 0; i < SkinImages.Count; i++)
            {
                var element = images.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("image").objectReferenceValue = SkinImages[i].image;
                element.FindPropertyRelative("role").stringValue = SkinImages[i].role;
                element.FindPropertyRelative("ink").enumValueIndex = (int)SkinImages[i].ink;
            }
            var labels = so.FindProperty("labels");
            labels.arraySize = SkinLabels.Count;
            for (var i = 0; i < SkinLabels.Count; i++)
            {
                var element = labels.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("text").objectReferenceValue = SkinLabels[i].text;
                element.FindPropertyRelative("ink").enumValueIndex = (int)SkinLabels[i].ink;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
