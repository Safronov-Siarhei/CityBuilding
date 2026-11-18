using System;
using System.Collections.Generic;
using System.IO;
using CityBuilder.Resources;
using CityBuilder.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The Diplomacy Hall's windows and banners (the user's approved concepts, 2026-11-17; card bot.md), built
    /// from baked art: Tools/ui/diplomacy_game_art.py writes UI/Diplomacy/&lt;screen&gt;/*.png + layout.json, and
    /// BuildBaked places every piece by its rect (1920 x 1080 reference, y down), texts over the art so they
    /// localize, empty slots for the icons the code fills. The windows at full size, the banners at the
    /// plague banner's 0.6.
    /// </summary>
    public static partial class SetupProject
    {
        private const float DiplomacyBannerSize = 0.9f;   // x1.5 like every banner (the user, 2026-11-18; was 0.6)

        [Serializable]
        private class BakedItem
        {
            public string kind, name, sprite, parent, key, font, color, rim, align;
            public float[] rect;
            public bool button;
            public int size;
            public float rot;
        }

        [Serializable]
        private class BakedLayout
        {
            public float[] reference;
            public BakedItem[] items;
        }

        private static void BuildDiplomacy(Transform uiRoot, ResourceIconLibrary icons)
        {
            var crystal = AssetDatabase.LoadAssetAtPath<Sprite>(UiTextureImport.IconsFolder + "/Res_UpgradeCrystal.png");
            var dim = new Color(0f, 0f, 0f, 0.55f);
            var dark = new Color(0.02f, 0.03f, 0.06f, 0.8f);

            var window = BuildBaked(uiRoot, "Window", 1f, dim, icons, crystal);
            if (window != null) Wire(window.gameObject.AddComponent<DiplomacyWindow>(), window);
            var gift = BuildBaked(uiRoot, "Gift", 1f, dim, icons, crystal);
            if (gift != null) Wire(gift.gameObject.AddComponent<DiplomacyGiftWindow>(), gift);
            var trade = BuildBaked(uiRoot, "Trade", 1f, dim, icons, crystal);
            if (trade != null) Wire(trade.gameObject.AddComponent<DiplomacyTradeWindow>(), trade);

            var holder = new GameObject("DiplomacyBanners", typeof(RectTransform));
            holder.transform.SetParent(uiRoot, false);
            holder.AddComponent<HudOverlay>();
            StretchFull((RectTransform)holder.transform);
            var banners = holder.AddComponent<DiplomacyBanners>();
            var so = new SerializedObject(banners);
            void Banner(string field, string screen, float scale)
            {
                var baked = BuildBaked(holder.transform, screen, scale, dark, icons, crystal);
                so.FindProperty(field).objectReferenceValue = baked;
            }
            Banner("help", "BannerHelp", DiplomacyBannerSize);
            Banner("gift", "BannerGift", DiplomacyBannerSize);
            Banner("edge", "BannerEdge", DiplomacyBannerSize);
            Banner("war", "BannerWar", DiplomacyBannerSize);
            Banner("razed", "Razed", 1f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Wire(MonoBehaviour controller, BakedUi ui)
        {
            var so = new SerializedObject(controller);
            so.FindProperty("ui").objectReferenceValue = ui;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>One screen of UI/Diplomacy: a holder (always on, carries the BakedUi) with a Root that shows and hides.</summary>
        private static BakedUi BuildBaked(Transform parent, string screen, float scale, Color backdropColor, ResourceIconLibrary icons, Sprite crystal)
        {
            var folder = UiTextureImport.DiplomacyFolder + "/" + screen;
            var layoutPath = folder + "/layout.json";
            if (!File.Exists(layoutPath))
            {
                Debug.LogWarning($"BuildBaked: no {layoutPath} -- run Tools/ui/diplomacy_game_art.py.");
                return null;
            }
            var layout = JsonUtility.FromJson<BakedLayout>(File.ReadAllText(layoutPath));
            var refW = layout.reference[0];
            var refH = layout.reference[1];
            // pictures imported before UiTextureImport knew the folder stay plain textures: again, under its rules
            foreach (var item in layout.items)
            {
                if (item.kind != "p" || string.IsNullOrEmpty(item.sprite)) continue;
                var path = $"{folder}/{item.sprite}.png";
                if (AssetImporter.GetAtPath(path) is TextureImporter ti && ti.textureType != TextureImporterType.Sprite)
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            var holder = new GameObject("Diplomacy" + screen, typeof(RectTransform));
            holder.transform.SetParent(parent, false);
            holder.AddComponent<HudOverlay>();
            StretchFull((RectTransform)holder.transform);
            var root = new GameObject("Root", typeof(RectTransform));
            root.transform.SetParent(holder.transform, false);
            StretchFull((RectTransform)root.transform);
            var backdrop = CreateImage(root.transform, "Backdrop", backdropColor);
            StretchBleed(backdrop.rectTransform);
            var frame = new GameObject("Frame", typeof(RectTransform)).GetComponent<RectTransform>();
            frame.SetParent(root.transform, false);
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0.5f, 0.5f);
            frame.sizeDelta = new Vector2(refW, refH);
            frame.localScale = Vector3.one * scale;

            var names = new List<string>();
            var objects = new List<GameObject>();
            var centres = new Dictionary<string, Vector2>();
            var placed = new Dictionary<string, Transform>();
            foreach (var item in layout.items)
            {
                var r = item.rect;
                var centre = new Vector2((r[0] + r[2]) / 2f, (r[1] + r[3]) / 2f);
                var size = new Vector2(r[2] - r[0], r[3] - r[1]);
                var hasParent = !string.IsNullOrEmpty(item.parent) && placed.ContainsKey(item.parent);
                var under = hasParent ? placed[item.parent] : frame;
                var origin = hasParent ? centres[item.parent] : new Vector2(refW / 2f, refH / 2f);

                GameObject go;
                switch (item.kind)
                {
                    case "g":
                        go = new GameObject(item.name, typeof(RectTransform));
                        go.transform.SetParent(under, false);
                        break;
                    case "t":
                    {
                        var text = BakedText(under, item);
                        go = text.gameObject;
                        if (item.button)
                        {
                            text.raycastTarget = true;
                            go.AddComponent<Button>().targetGraphic = text;
                        }
                        break;
                    }
                    default:
                    {
                        var image = CreateImage(under, item.name, Color.white);
                        if (!string.IsNullOrEmpty(item.sprite))
                            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{item.sprite}.png");
                        else
                        {
                            image.color = Color.clear;   // a slot: the code puts an icon in it
                            image.preserveAspect = true;
                        }
                        image.raycastTarget = item.button || item.name == "Window";
                        if (item.button) image.gameObject.AddComponent<Button>().targetGraphic = image;
                        go = image.gameObject;
                        break;
                    }
                }
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(centre.x - origin.x, origin.y - centre.y);
                rect.sizeDelta = size;
                if (Mathf.Abs(item.rot) > 0.01f) rect.localEulerAngles = new Vector3(0f, 0f, item.rot);

                centres[item.name] = centre;
                placed[item.name] = go.transform;
                names.Add(item.name);
                objects.Add(go);
            }

            var baked = holder.AddComponent<BakedUi>();
            baked.EditorSet(root, names.ToArray(), objects.ToArray(), icons, crystal);
            root.SetActive(false);
            return baked;
        }

        private static Text BakedText(Transform parent, BakedItem item)
        {
            var size = Mathf.Max(12, item.size);
            var text = CreateText(parent, item.name, string.Empty, size, Vector2.zero, new Vector2(100f, 100f), Hex(item.color));
            if (!string.IsNullOrEmpty(item.key)) Localize(text, item.key);
            text.font = item.font == "b" ? UiTheme.Bold : item.font == "m" ? UiTheme.Regular : UiTheme.Heavy;
            text.alignment = item.align == "l" ? TextAnchor.MiddleLeft : item.align == "r" ? TextAnchor.MiddleRight : TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            // a longer language shrinks instead of spilling out of its box
            text.resizeTextForBestFit = true;
            text.resizeTextMaxSize = size;
            text.resizeTextMinSize = Mathf.Max(10, Mathf.RoundToInt(size * 0.6f));
            foreach (var shadow in text.GetComponents<Shadow>()) Object.DestroyImmediate(shadow);
            if (!string.IsNullOrEmpty(item.rim)) Rim(text, Hex(item.rim), Mathf.Max(2f, size / 18f));
            return text;
        }

        private static Color Hex(string hex) =>
            !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString("#" + hex, out var c) ? c : Color.white;
    }
}
