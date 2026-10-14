using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The HUD's switchable looks (GameSettings.HudSkin, HudSkinApplier). While the HUD is built,
    /// every piece a look draws is noted here -- its image and role, a label and its ink, a piece
    /// that stands elsewhere in the Dock layout -- and at the end the lists are written into one
    /// HudSkinApplier, so switching at play time is a pass over them and never a search.
    ///
    /// The scene itself is built in the default look, so it is right before the first pass.
    /// </summary>
    public static partial class SetupProject
    {
        // The Dock layout: a wooden rail along the top holding the resources and the day, and a
        // dock along the bottom holding the tabs, the minimap's foot and the tax.
        private const float DockTopHeight = 84f;
        private const float DockBottomHeight = 142f;

        private static readonly List<HudSkinImage> SkinImages = new List<HudSkinImage>();
        private static readonly List<HudSkinLabel> SkinLabels = new List<HudSkinLabel>();
        private static readonly List<(RectTransform rect, HudRect dock)> DockMoves = new List<(RectTransform, HudRect)>();

        private static void BeginHudSkins()
        {
            SkinImages.Clear();
            SkinLabels.Clear();
            DockMoves.Clear();
        }

        /// <summary>A role's sprite in the default look (Tools/ui/make_hud_skins.py), or null when that look does not draw it.</summary>
        private static Sprite HudSprite(string role)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{UiTextureImport.HudSkinsFolder}/{GameSettings.DefaultHudSkin}/{role}.png");
        }

        /// <summary>A new image drawn by the look: centred, untinted, hidden while the look has no sprite for its role.</summary>
        private static Image HudImage(Transform parent, string name, string role, Vector2 size, bool sliced = true)
        {
            var image = CreateImage(parent, name, Color.white);
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            SkinRole(image, role, sliced);
            return image;
        }

        /// <summary>Hands an existing image over to the look.</summary>
        private static void SkinRole(Image image, string role, bool sliced = true)
        {
            image.sprite = HudSprite(role);
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.color = Color.white;
            image.enabled = image.sprite != null;
            SkinImages.Add(new HudSkinImage { image = image, role = role, ink = HudInk.Keep });
        }

        /// <summary>A white glyph written in the look's ink for what it stands on.</summary>
        private static void SkinInk(Image glyph, HudInk ink)
        {
            glyph.color = HudSkins.Ink(GameSettings.DefaultHudSkin, ink);
            SkinImages.Add(new HudSkinImage { image = glyph, role = string.Empty, ink = ink });
        }

        private static void SkinInk(Text text, HudInk ink)
        {
            text.color = HudSkins.Ink(GameSettings.DefaultHudSkin, ink);
            SkinLabels.Add(new HudSkinLabel { text = text, ink = ink });
        }

        /// <summary>Where a piece stands in the Dock layout; everywhere else it stays where it was built.</summary>
        private static void DockAt(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            DockMoves.Add((rect, new HudRect { anchorMin = anchorMin, anchorMax = anchorMax, pivot = pivot, position = position, size = size }));
        }

        private static void DockAt(RectTransform rect, Vector2 position)
        {
            var at = HudRect.Of(rect);
            DockAt(rect, at.anchorMin, at.anchorMax, at.pivot, position, at.size);
        }

        /// <summary>
        /// The two rails of the Dock layout, made first so everything draws over them. Other looks
        /// have no sprite for them, which hides them. They take taps, so a tap on the dock never
        /// reaches the map under it.
        /// </summary>
        private static void BuildDockRails(Transform uiRoot)
        {
            var top = HudImage(uiRoot, "DockTop", "dock_top", Vector2.zero);
            Anchor(top.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            top.rectTransform.anchorMin = new Vector2(0f, 1f);
            top.rectTransform.anchorMax = new Vector2(1f, 1f);
            top.rectTransform.sizeDelta = new Vector2(0f, DockTopHeight);

            var bottom = HudImage(uiRoot, "DockBottom", "dock_bottom", Vector2.zero);
            Anchor(bottom.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            bottom.rectTransform.anchorMin = new Vector2(0f, 0f);
            bottom.rectTransform.anchorMax = new Vector2(1f, 0f);
            bottom.rectTransform.sizeDelta = new Vector2(0f, DockBottomHeight);
        }

        /// <summary>The Dock layout's places for the pieces that move, then the applier holding everything noted.</summary>
        private static void BuildHudSkinApplier(Transform uiRoot, BuildingCategoryPanel buildMenu, float closedBarY, float openBarY)
        {
            // The resources along the top rail, between the day (after the menu button) and the
            // happiness number; the day on the rail to the menu button's right.
            var resources = uiRoot.Find("ResourceHUD") as RectTransform;
            if (resources != null)
            {
                const float left = 340f, right = 160f;
                DockAt(resources, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                    new Vector2((left - right) * 0.5f, -DockTopHeight * 0.5f), new Vector2(-(left + right), resources.sizeDelta.y));
            }
            var day = uiRoot.Find("SettlementStatus") as RectTransform;
            if (day != null)
            {
                DockAt(day, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                    new Vector2(HudMargin + RoundButton + 100f, -DockTopHeight * 0.5f), new Vector2(180f, 44f));
            }
            // The cards open above the dock; the tabs stay down in it.
            var hotbar = buildMenu != null ? buildMenu.transform.Find("Hotbar") as RectTransform : null;
            if (hotbar != null) DockAt(hotbar, new Vector2(0f, DockBottomHeight - 6f));
            var dockBarY = (DockBottomHeight - 96f) * 0.5f;

            var applier = uiRoot.gameObject.AddComponent<HudSkinApplier>();
            var so = new SerializedObject(applier);

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

            var layout = so.FindProperty("layout");
            layout.arraySize = DockMoves.Count;
            for (var i = 0; i < DockMoves.Count; i++)
            {
                var element = layout.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("rect").objectReferenceValue = DockMoves[i].rect;
                WriteRect(element.FindPropertyRelative("sketch"), HudRect.Of(DockMoves[i].rect));
                WriteRect(element.FindPropertyRelative("dock"), DockMoves[i].dock);
            }

            so.FindProperty("buildMenu").objectReferenceValue = buildMenu;
            so.FindProperty("sketchClosedBarY").floatValue = closedBarY;
            so.FindProperty("sketchOpenBarY").floatValue = openBarY;
            so.FindProperty("dockClosedBarY").floatValue = dockBarY;
            so.FindProperty("dockOpenBarY").floatValue = dockBarY;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteRect(SerializedProperty property, HudRect rect)
        {
            property.FindPropertyRelative("anchorMin").vector2Value = rect.anchorMin;
            property.FindPropertyRelative("anchorMax").vector2Value = rect.anchorMax;
            property.FindPropertyRelative("pivot").vector2Value = rect.pivot;
            property.FindPropertyRelative("position").vector2Value = rect.position;
            property.FindPropertyRelative("size").vector2Value = rect.size;
        }
    }
}
