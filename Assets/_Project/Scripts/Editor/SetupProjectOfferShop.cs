using System;
using System.IO;
using CityBuilder.Resources;
using CityBuilder.Shop;
using CityBuilder.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The sandbox offer shop (the user, 2026-11-11; card Design/features/monetization.md): the window
    /// prefab in Resources (Shop/OfferShopWindow.cs), the stall icon + loud notice on the HUD
    /// (OfferShopHud), the four cards in the game menu (OfferMenuStrip) and the OfferShop itself.
    /// Pieces and places from Tools/store/offer_shop_art.py (UI/Shop/*.png + layout.json, the 1920 x
    /// 1080 reference, y down; a strip's inner rects from its top-left). Words are localization keys
    /// (#shop_*); a bundle's name is #bundle_&lt;id&gt; from the «Наборы» tab.
    /// </summary>
    public static partial class SetupProject
    {
        private const string OfferShopPrefabPath = "Assets/_Project/Resources/OfferShopWindow.prefab";
        /// <summary>The window against the art's reference size: big enough for its buttons on a phone.</summary>
        private const float OfferShopSize = 0.8f;
        /// <summary>The shop's stall icon, under the sack (the user, 2026-11-11).</summary>
        private const float ShopButtonTop = InventoryButtonTop - WorkforceButtonSize - 18f;
        /// <summary>The menu's offer cards against Card.png's own size.</summary>
        private const float OfferCardScale = 0.8f;

        private static readonly Color ShopInk = new Color(84 / 255f, 50 / 255f, 24 / 255f);
        private static readonly Color ShopCream = new Color(1f, 246 / 255f, 222 / 255f);

        [Serializable]
        private class OfferShopLayout
        {
            public float[] reference;
            public float[] window;
            public float[] frame;
            public float[] title;
            public float pad;
            public float[] strip0, strip1, strip2, strip3;
            public float[] close, counter, counter_icon, counter_text, toast;
            public float[] name, timer, timer_text, watch, watch_play, watch_label, buy, buy_price, buy_crystal, free, free_label, stamp, badge, wide_play, wide_ad_label, wide_price, wide_crystal;
            public float[] good0, good1, good2, amount0, amount1, amount2;
        }

        private static Sprite ShopSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{UiTextureImport.ShopFolder}/{name}.png");

        private static OfferShopLayout ReadOfferShopLayout()
        {
            var path = $"{UiTextureImport.ShopFolder}/layout.json";
            if (!File.Exists(path)) return null;
            // JsonUtility reads no nested arrays: the script's lists are flattened to named fields here
            var json = File.ReadAllText(path);
            var raw = JsonUtility.FromJson<OfferShopLayout>(json);
            var lists = JsonUtility.FromJson<OfferShopLists>(Flatten(json));
            raw.strip0 = lists.strips_0; raw.strip1 = lists.strips_1; raw.strip2 = lists.strips_2; raw.strip3 = lists.strips_3;
            raw.good0 = lists.goods_0; raw.good1 = lists.goods_1; raw.good2 = lists.goods_2;
            raw.amount0 = lists.amounts_0; raw.amount1 = lists.amounts_1; raw.amount2 = lists.amounts_2;
            return raw;
        }

        [Serializable]
        private class OfferShopLists
        {
            public float[] strips_0, strips_1, strips_2, strips_3, goods_0, goods_1, goods_2, amounts_0, amounts_1, amounts_2;
        }

        /// <summary>"strips": [[a],[b],[c]] -> "strips_0": [a], "strips_1": [b], ... (only the three lists of lists).</summary>
        private static string Flatten(string json)
        {
            var sb = new System.Text.StringBuilder("{");
            foreach (var key in new[] { "strips", "goods", "amounts" })
            {
                var at = json.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
                if (at < 0) continue;
                var open = json.IndexOf('[', at);
                var depth = 0;
                var index = 0;
                var start = -1;
                for (var i = open; i < json.Length; i++)
                {
                    var c = json[i];
                    if (c == '[')
                    {
                        depth++;
                        if (depth == 2) start = i;
                    }
                    else if (c == ']')
                    {
                        if (depth == 2)
                        {
                            if (sb.Length > 1) sb.Append(',');
                            sb.Append('"').Append(key).Append('_').Append(index++).Append("\":").Append(json, start, i - start + 1);
                        }
                        depth--;
                        if (depth == 0) break;
                    }
                }
            }
            return sb.Append('}').ToString();
        }

        /// <summary>Places `rect` (a child of something whose reference rect is `parent`) at the reference rect `r`.</summary>
        private static void PlaceIn(RectTransform rect, float[] parent, float[] r)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2((r[0] + r[2]) / 2f - (parent[0] + parent[2]) / 2f, (parent[1] + parent[3]) / 2f - (r[1] + r[3]) / 2f);
            rect.sizeDelta = new Vector2(r[2] - r[0], r[3] - r[1]);
        }

        /// <summary>A strip-relative rect moved onto the strip at `strip`.</summary>
        private static float[] On(float[] strip, float[] r) => new[] { strip[0] + r[0], strip[1] + r[1], strip[0] + r[2], strip[1] + r[3] };

        private static float[] Grow(float[] r, float by) => new[] { r[0] - by, r[1] - by, r[2] + by, r[3] + by };

        private static Image ShopImage(Transform parent, string name, string sprite, float[] parentRect, float[] rect)
        {
            var image = CreateImage(parent, name, Color.white);
            image.sprite = ShopSprite(sprite);
            image.raycastTarget = false;
            PlaceIn(image.rectTransform, parentRect, rect);
            return image;
        }

        private static Text ShopText(Transform parent, string name, string key, int size, Font font, Color color, TextAnchor align)
        {
            var text = CreateText(parent, name, string.Empty, size, Vector2.zero, new Vector2(100f, 100f), color);
            if (key != null) Localize(text, key);
            text.font = font;
            text.alignment = align;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button ShopButton(Transform parent, string name, string sprite, float[] parentRect, float[] rect)
        {
            var image = ShopImage(parent, name, sprite, parentRect, rect);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static void ReimportShopArt()
        {
            // pictures imported before UiTextureImport knew the folder stay plain textures: again under its rules
            var files = Directory.GetFiles(UiTextureImport.ShopFolder, "*.png", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var path = file.Replace(Path.DirectorySeparatorChar, '/');
                // also when it does not load as a sprite: Close/Counter came in through a bare meta
                // (guid only) in an open editor and stayed plain textures in the Library although
                // their meta said Sprite -- the prefab got white boxes (2026-11-11)
                if (AssetImporter.GetAtPath(path) is TextureImporter ti
                    && (ti.textureType != TextureImporterType.Sprite || ti.GetPlatformTextureSettings("Android").maxTextureSize != 1024
                        || AssetDatabase.LoadAssetAtPath<Sprite>(path) == null))
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == null)
                    Debug.LogError($"BuildOfferShopWindow: {path} does not load as a sprite.");
            }
        }

        private static ResourceIconLibrary ShopIcons() =>
            AssetDatabase.LoadAssetAtPath<ResourceIconLibrary>($"{ResourceScriptableObjectsFolder}/ResourceIconLibrary.asset");

        private static Sprite CrystalIcon() => AssetDatabase.LoadAssetAtPath<Sprite>($"{UiTextureImport.IconsFolder}/Res_UpgradeCrystal.png");

        // ---------------------------------------------------------------- the window

        private static void BuildOfferShopWindow()
        {
            var layout = ReadOfferShopLayout();
            if (layout == null)
            {
                Debug.LogWarning("BuildOfferShopWindow: no UI/Shop/layout.json -- run Tools/store/offer_shop_art.py.");
                return;
            }
            ReimportShopArt();
            var refW = layout.reference[0];
            var refH = layout.reference[1];
            var screenRect = new[] { 0f, 0f, refW, refH };

            var root = new GameObject("OfferShopWindow", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;   // as the rate-us window: over the interface, under the loading screen
            ConfigureScaler(root.GetComponent<CanvasScaler>());
            var screen = root.AddComponent<OfferShopWindow>();

            var shade = CreateImage(root.transform, "Shade", new Color(0.04f, 0.03f, 0.02f, 0.63f));
            StretchFull(shade.rectTransform);
            var shadeButton = shade.gameObject.AddComponent<Button>();
            shadeButton.transition = Selectable.Transition.None;

            var window = new GameObject("Window", typeof(RectTransform)).GetComponent<RectTransform>();
            window.SetParent(root.transform, false);
            window.anchorMin = window.anchorMax = window.pivot = new Vector2(0.5f, 0.5f);
            window.sizeDelta = new Vector2(refW, refH);

            var hit = CreateImage(window, "FrameHit", new Color(0f, 0f, 0f, 0f));
            PlaceIn(hit.rectTransform, screenRect, layout.frame);
            // the window's picture per map season, loaded by the window (Resources/Shop/Window_<theme>)
            var picture = CreateImage(window, "Picture", Color.white);
            picture.raycastTarget = false;
            PlaceIn(picture.rectTransform, screenRect, layout.window);

            var title = ShopText(window, "Title", "#shop_title", 72, UiTheme.Heavy, new Color(1f, 0.94f, 0.78f), TextAnchor.MiddleCenter);
            PlaceIn(title.rectTransform, screenRect, layout.title);
            Rim(title, new Color(0.35f, 0.05f, 0.06f), 4f);

            var strips = new[] { layout.strip0, layout.strip1, layout.strip2, layout.strip3 };
            var goods = new[] { layout.good0, layout.good1, layout.good2 };
            var amounts = new[] { layout.amount0, layout.amount1, layout.amount2 };
            var views = new OfferShopWindow.SlotView[strips.Length];
            for (var i = 0; i < strips.Length; i++)
            {
                var box = strips[i];
                var slot = new GameObject($"Slot{i + 1}", typeof(RectTransform)).GetComponent<RectTransform>();
                slot.SetParent(window, false);
                PlaceIn(slot, screenRect, box);
                var view = new OfferShopWindow.SlotView { root = slot.gameObject };
                view.back = ShopImage(slot, "Back", "Strip", box, Grow(box, layout.pad)).gameObject;
                view.backGold = ShopImage(slot, "BackGold", "StripGold", box, Grow(box, layout.pad)).gameObject;
                view.backCrystal = ShopImage(slot, "BackCrystal", "StripCrystal", box, Grow(box, layout.pad)).gameObject;
                view.backPaid = ShopImage(slot, "BackPaid", "StripPaid", box, Grow(box, layout.pad)).gameObject;

                // the crystal offers' «+N%» over the strip
                var badge = ShopImage(slot, "Badge", "Badge", box, On(box, layout.badge));
                view.badgeText = ShopText(badge.transform, "Text", null, 34, UiTheme.Heavy, Color.white, TextAnchor.MiddleCenter);
                StretchFull(view.badgeText.rectTransform);
                view.badgeText.rectTransform.offsetMin = new Vector2(0f, 6f);
                Rim(view.badgeText, UiTheme.Rgb(0x600c0e), 3f);
                view.badge = badge.gameObject;

                view.name = ShopText(slot, "Name", null, 36, UiTheme.Heavy, ShopInk, TextAnchor.MiddleLeft);
                PlaceIn(view.name.rectTransform, box, On(box, layout.name));
                view.goodIcons = new Image[goods.Length];
                view.goodAmounts = new Text[goods.Length];
                for (var g = 0; g < goods.Length; g++)
                {
                    var icon = CreateImage(slot, $"Good{g + 1}", Color.white);
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                    PlaceIn(icon.rectTransform, box, On(box, goods[g]));
                    view.goodIcons[g] = icon;
                    var amount = ShopText(slot, $"Amount{g + 1}", null, 44, UiTheme.Heavy, ShopInk, TextAnchor.MiddleLeft);
                    PlaceIn(amount.rectTransform, box, On(box, amounts[g]));
                    Rim(amount, ShopCream, 3f);
                    view.goodAmounts[g] = amount;
                }

                var timerRect = On(box, layout.timer);
                var timer = ShopImage(slot, "Timer", "Timer", box, timerRect);
                view.timer = ShopText(timer.transform, "Text", null, 38, UiTheme.Heavy, new Color(1f, 240 / 255f, 210 / 255f), TextAnchor.MiddleLeft);
                PlaceIn(view.timer.rectTransform, timerRect, On(box, layout.timer_text));

                var watchRect = On(box, layout.watch);
                view.watch = ShopButton(slot, "Watch", "ButtonGreen", box, watchRect);
                ShopImage(view.watch.transform, "Play", "Play", watchRect, On(box, layout.watch_play));
                view.watchLabel = ShopText(view.watch.transform, "Label", null, 36, UiTheme.Heavy, Color.white, TextAnchor.MiddleCenter);
                PlaceIn(view.watchLabel.rectTransform, watchRect, On(box, layout.watch_label));
                Rim(view.watchLabel, UiTheme.Rgb(0x1c5412), 3f);

                var buyRect = On(box, layout.buy);
                view.buy = ShopButton(slot, "Buy", "ButtonBlue", box, buyRect);
                view.price = ShopText(view.buy.transform, "Price", null, 50, UiTheme.Heavy, Color.white, TextAnchor.MiddleCenter);
                PlaceIn(view.price.rectTransform, buyRect, On(box, layout.buy_price));
                Rim(view.price, UiTheme.Rgb(0x103c70), 3f);
                var crystal = CreateImage(view.buy.transform, "Crystal", Color.white);
                crystal.sprite = CrystalIcon();
                crystal.preserveAspect = true;
                crystal.raycastTarget = false;
                PlaceIn(crystal.rectTransform, buyRect, On(box, layout.buy_crystal));

                var freeRect = On(box, layout.free);
                view.free = ShopButton(slot, "Free", "ButtonGold", box, freeRect);
                var freeLabel = ShopText(view.free.transform, "Label", "#shop_take_free", 38, UiTheme.Heavy, Color.white, TextAnchor.MiddleCenter);
                PlaceIn(freeLabel.rectTransform, freeRect, On(box, layout.free_label));
                Rim(freeLabel, UiTheme.Rgb(0x764608), 3f);

                // crystals for an ad: one wide green button; for money: one wide orange one (a stand-in: no store yet)
                view.watchWide = ShopButton(slot, "WatchWide", "ButtonGreenWide", box, freeRect);
                ShopImage(view.watchWide.transform, "Play", "Play", freeRect, On(box, layout.wide_play));
                view.watchWideLabel = ShopText(view.watchWide.transform, "Label", null, 38, UiTheme.Heavy, Color.white, TextAnchor.MiddleCenter);
                PlaceIn(view.watchWideLabel.rectTransform, freeRect, On(box, layout.wide_ad_label));
                Rim(view.watchWideLabel, UiTheme.Rgb(0x1c5412), 3f);
                // a goods bundle for crystals only (no ad to show, or worth more than shop_ad_max_ads ads): one wide blue button
                view.buyWide = ShopButton(slot, "BuyWide", "ButtonBlueWide", box, freeRect);
                view.buyWidePrice = ShopText(view.buyWide.transform, "Price", null, 54, UiTheme.Heavy, Color.white, TextAnchor.MiddleRight);
                PlaceIn(view.buyWidePrice.rectTransform, freeRect, On(box, layout.wide_price));
                Rim(view.buyWidePrice, UiTheme.Rgb(0x103c70), 3f);
                var wideCrystal = CreateImage(view.buyWide.transform, "Crystal", Color.white);
                wideCrystal.sprite = CrystalIcon();
                wideCrystal.preserveAspect = true;
                wideCrystal.raycastTarget = false;
                PlaceIn(wideCrystal.rectTransform, freeRect, On(box, layout.wide_crystal));
                view.money = ShopButton(slot, "Money", "ButtonOrange", box, freeRect);
                view.moneyLabel = ShopText(view.money.transform, "Label", null, 50, UiTheme.Heavy, Color.white, TextAnchor.MiddleCenter);
                PlaceIn(view.moneyLabel.rectTransform, freeRect, On(box, layout.free_label));
                Rim(view.moneyLabel, UiTheme.Rgb(0x7c3408), 3f);

                var stampRect = On(box, layout.stamp);
                var stamp = ShopImage(slot, "Stamp", "Stamp", box, stampRect);
                stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -4f);
                var stampText = ShopText(stamp.transform, "Text", "#shop_taken", 48, UiTheme.Heavy, UiTheme.Rgb(0x348424), TextAnchor.MiddleCenter);
                StretchFull(stampText.rectTransform);
                view.stamp = stamp.gameObject;
                // a new bundle's flash, over the whole strip (OfferShopWindow fades it)
                view.flash = ShopImage(slot, "Flash", "Strip", box, Grow(box, layout.pad));
                view.flash.color = new Color(1f, 0.97f, 0.85f, 0f);
                view.flash.gameObject.SetActive(false);
                views[i] = view;
            }

            // the player's crystals on the frame's top-right, and the line when they are too few
            var counter = new GameObject("Crystals", typeof(RectTransform)).GetComponent<RectTransform>();
            counter.SetParent(window, false);
            PlaceIn(counter, screenRect, layout.counter);
            var counterIcon = CreateImage(counter.transform, "Icon", Color.white);
            counterIcon.sprite = CrystalIcon();
            counterIcon.preserveAspect = true;
            counterIcon.raycastTarget = false;
            PlaceIn(counterIcon.rectTransform, layout.counter, layout.counter_icon);
            var counterText = ShopText(counter.transform, "Count", null, 48, UiTheme.Heavy, new Color(1f, 240 / 255f, 210 / 255f), TextAnchor.MiddleRight);
            PlaceIn(counterText.rectTransform, layout.counter, layout.counter_text);
            var toast = ShopText(window, "Toast", null, 40, UiTheme.Heavy, OfferShopWindow.WarnColor, TextAnchor.MiddleRight);
            PlaceIn(toast.rectTransform, screenRect, layout.toast);
            Rim(toast, new Color(0.25f, 0.04f, 0.03f), 3f);
            Rim(counterText, new Color(0.24f, 0.14f, 0.06f), 3f);
            // the close seal (the user, 2026-11-11: a tap outside closes too, the seal shows how)
            var close = ShopButton(window, "Close", "Close", screenRect, layout.close);

            var so = new SerializedObject(screen);
            so.FindProperty("close").objectReferenceValue = close;
            so.FindProperty("counter").objectReferenceValue = counter;
            so.FindProperty("counterText").objectReferenceValue = counterText;
            so.FindProperty("toast").objectReferenceValue = toast;
            so.FindProperty("group").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("window").objectReferenceValue = window;
            so.FindProperty("shade").objectReferenceValue = shadeButton;
            so.FindProperty("picture").objectReferenceValue = picture;
            so.FindProperty("size").floatValue = OfferShopSize;
            so.FindProperty("icons").objectReferenceValue = ShopIcons();
            so.FindProperty("crystal").objectReferenceValue = CrystalIcon();
            so.ApplyModifiedPropertiesWithoutUndo();
            // the views hold plain C# lists of references: set through the component, then saved with it
            var field = typeof(OfferShopWindow).GetField("slots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(screen, views);
            EditorUtility.SetDirty(screen);

            Directory.CreateDirectory(Path.GetDirectoryName(OfferShopPrefabPath));
            PrefabUtility.SaveAsPrefabAsset(root, OfferShopPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        // ---------------------------------------------------------------- the HUD and the menu

        /// <summary>The OfferShop, its stall icon under the sack with the loud notice, and the menu's cards. After BuildGameMenu and BuildInventoryPanel.</summary>
        private static void BuildOfferShopHud(Transform uiRoot, ResourceIconLibrary iconLibrary)
        {
            var shopGO = new GameObject("OfferShop");
            shopGO.transform.SetParent(uiRoot, false);
            shopGO.AddComponent<OfferShop>();

            var hudGO = new GameObject("OfferShopHud", typeof(RectTransform));
            hudGO.transform.SetParent(uiRoot, false);
            StretchFull((RectTransform)hudGO.transform);
            var hud = hudGO.AddComponent<OfferShopHud>();

            // the icon with its glow behind and its timer and dot over it, as one piece that hides and shows
            var holder = new GameObject("ShopButton", typeof(RectTransform)).GetComponent<RectTransform>();
            holder.SetParent(hudGO.transform, false);
            Anchor(holder, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(HudMargin + (RoundButton - WorkforceButtonSize) * 0.5f, ShopButtonTop), new Vector2(WorkforceButtonSize, WorkforceButtonSize));
            var glow = CreateImage(holder, "Glow", Color.white);
            glow.sprite = ShopSprite("Glow");
            glow.raycastTarget = false;
            Anchor(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(WorkforceButtonSize * 1.9f, WorkforceButtonSize * 1.9f));
            var open = CreateGlyphButton(holder, "Open", "round", HudInk.Keep, null, ShopSprite("Stall"), WorkforceButtonSize, 0.72f);
            StretchFull(open.GetComponent<RectTransform>());
            var pill = CreateImage(holder, "Timer", Color.white);
            pill.sprite = ShopSprite("HudTimer");
            pill.raycastTarget = false;
            Anchor(pill.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(92f, 36f));
            var timer = ShopText(pill.transform, "Text", null, 26, UiTheme.Heavy, new Color(1f, 240 / 255f, 210 / 255f), TextAnchor.MiddleCenter);
            StretchFull(timer.rectTransform);
            var dot = CreateImage(holder, "Dot", Color.white);
            dot.sprite = ShopSprite("Dot");
            dot.raycastTarget = false;
            // half of it on the round button (the user, 2026-11-11): its centre on the button's rim at 45 degrees
            var rim = WorkforceButtonSize * 0.5f * 0.7071f;
            Anchor(dot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(rim, rim), new Vector2(32f, 32f));

            // the loud notice, to the right of the icon
            const float noticeScale = 0.8f;
            var notice = CreateImage(hudGO.transform, "ShopNotice", Color.white);
            notice.sprite = ShopSprite("Notice");
            var noticeButton = notice.gameObject.AddComponent<Button>();
            noticeButton.targetGraphic = notice;
            var nw = 756f * noticeScale;
            var nh = 186f * noticeScale;
            Anchor(notice.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                new Vector2(HudMargin + RoundButton + 12f, ShopButtonTop - WorkforceButtonSize * 0.5f), new Vector2(nw, nh));
            var m = 28f * noticeScale;
            var stallIcon = CreateImage(notice.transform, "Stall", Color.white);
            stallIcon.sprite = ShopSprite("Stall");
            stallIcon.preserveAspect = true;
            stallIcon.raycastTarget = false;
            Anchor(stallIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(m + 14f, 4f), new Vector2(84f, 84f));
            var noticeTitle = ShopText(notice.transform, "Title", "#shop_notice_title", 34, UiTheme.Heavy, UiTheme.Rgb(0x965c0a), TextAnchor.MiddleLeft);
            Anchor(noticeTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(m + 112f, -m - 6f), new Vector2(nw - m * 2 - 120f, 44f));
            var noticeBody = ShopText(notice.transform, "Body", "#shop_notice_body", 26, UiTheme.Regular, ShopInk, TextAnchor.MiddleLeft);
            Anchor(noticeBody.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(m + 112f, -m - 50f), new Vector2(nw - m * 2 - 120f, 36f));
            var track = CreateImage(notice.transform, "Track", new Color(218 / 255f, 184 / 255f, 128 / 255f));
            track.raycastTarget = false;
            var barW = nw - m * 2 - 40f;
            Anchor(track.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(m + 20f, m + 8f), new Vector2(barW, 12f));
            var fill = CreateImage(track.transform, "Fill", new Color(230 / 255f, 166 / 255f, 36 / 255f));
            fill.raycastTarget = false;
            Anchor(fill.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(barW, 12f));

            var so = new SerializedObject(hud);
            so.FindProperty("button").objectReferenceValue = holder.gameObject;
            so.FindProperty("open").objectReferenceValue = open;
            so.FindProperty("timer").objectReferenceValue = timer;
            so.FindProperty("dot").objectReferenceValue = dot.gameObject;
            so.FindProperty("glow").objectReferenceValue = glow.rectTransform;
            so.FindProperty("notice").objectReferenceValue = notice.gameObject;
            so.FindProperty("noticeButton").objectReferenceValue = noticeButton;
            so.FindProperty("noticeBar").objectReferenceValue = fill.rectTransform;
            so.ApplyModifiedPropertiesWithoutUndo();

            BuildOfferMenuStrip(uiRoot, iconLibrary);
        }

        /// <summary>Three small cards in the game menu, right of its entries; a tap opens the shop.</summary>
        private static void BuildOfferMenuStrip(Transform uiRoot, ResourceIconLibrary iconLibrary)
        {
            var menu = uiRoot.Find("GameMenu");
            if (menu == null)
            {
                Debug.LogWarning("BuildOfferMenuStrip: no GameMenu under the UI root.");
                return;
            }
            var stripGO = new GameObject("OfferMenuStrip", typeof(RectTransform));
            stripGO.transform.SetParent(menu, false);
            StretchFull((RectTransform)stripGO.transform);
            var strip = stripGO.AddComponent<OfferMenuStrip>();
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(stripGO.transform, false);
            StretchFull((RectTransform)content.transform);

            const float cardW = 460f, cardH = 190f;
            var x = HudMargin + RoundButton + 36f;
            var top = MenuButtonTop - RoundButton - 18f + 20f * OfferCardScale;
            var header = ShopText(content.transform, "Header", "#shop_title", 38, UiTheme.Heavy, new Color(1f, 0.94f, 0.78f), TextAnchor.LowerLeft);
            Anchor(header.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(x + 24f, top - 8f), new Vector2(400f, 50f));
            Rim(header, new Color(0.2f, 0.12f, 0.05f), 3f);

            var cards = new OfferMenuStrip.Card[OfferRotation.DefaultPlaces.Length];
            for (var i = 0; i < cards.Length; i++)
            {
                var image = CreateImage(content.transform, $"Card{i + 1}", Color.white);
                image.sprite = ShopSprite("Card");
                var rect = image.rectTransform;
                Anchor(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, top - i * (cardH - 30f) * OfferCardScale), new Vector2(cardW, cardH));
                rect.localScale = new Vector3(OfferCardScale, OfferCardScale, 1f);
                var button = image.gameObject.AddComponent<Button>();
                button.targetGraphic = image;

                void TopLeft(RectTransform r, float x0, float y0, float x1, float y1) =>
                    Anchor(r, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x0, -y0), new Vector2(x1 - x0, y1 - y0));

                var card = new OfferMenuStrip.Card { button = button };
                card.name = ShopText(rect, "Name", null, 30, UiTheme.Heavy, ShopInk, TextAnchor.MiddleLeft);
                TopLeft(card.name.rectTransform, 44f, 30f, 320f, 74f);
                card.timer = ShopText(rect, "Timer", null, 28, UiTheme.Bold, UiTheme.Rgb(0x8c623a), TextAnchor.MiddleRight);
                TopLeft(card.timer.rectTransform, 300f, 30f, 420f, 74f);
                card.goodIcons = new Image[3];
                card.goodAmounts = new Text[3];
                for (var g = 0; g < 3; g++)
                {
                    var icon = CreateImage(rect, $"Good{g + 1}", Color.white);
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                    TopLeft(icon.rectTransform, 44f + g * 126f, 92f, 100f + g * 126f, 148f);
                    card.goodIcons[g] = icon;
                    var amount = ShopText(rect, $"Amount{g + 1}", null, 30, UiTheme.Heavy, ShopInk, TextAnchor.MiddleLeft);
                    TopLeft(amount.rectTransform, 106f + g * 126f, 92f, 168f + g * 126f, 148f);
                    card.goodAmounts[g] = amount;
                }
                cards[i] = card;
            }

            var so = new SerializedObject(strip);
            so.FindProperty("content").objectReferenceValue = content;
            so.FindProperty("icons").objectReferenceValue = iconLibrary;
            so.FindProperty("crystal").objectReferenceValue = CrystalIcon();
            so.ApplyModifiedPropertiesWithoutUndo();
            var field = typeof(OfferMenuStrip).GetField("cards", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(strip, cards);
            EditorUtility.SetDirty(strip);
        }
    }
}
