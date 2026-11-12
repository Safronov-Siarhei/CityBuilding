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
        private const string OfferShortagePrefabPath = "Assets/_Project/Resources/OfferShortageWindow.prefab";
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
            public float[] good0, good1, good2, good3, amount0, amount1, amount2, amount3;
            // the «Не хватает ресурсов» window: its frame, title, seal, crystals, line and its one strip
            public float[] s_window, s_frame, s_title, s_close, s_counter, s_counter_icon, s_counter_text, s_toast, s_strip;
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
            raw.good0 = lists.goods_0; raw.good1 = lists.goods_1; raw.good2 = lists.goods_2; raw.good3 = lists.goods_3;
            raw.amount0 = lists.amounts_0; raw.amount1 = lists.amounts_1; raw.amount2 = lists.amounts_2; raw.amount3 = lists.amounts_3;
            return raw;
        }

        [Serializable]
        private class OfferShopLists
        {
            public float[] strips_0, strips_1, strips_2, strips_3, goods_0, goods_1, goods_2, goods_3, amounts_0, amounts_1, amounts_2, amounts_3;
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
            BuildOfferShopWindow(false);
            BuildOfferShopWindow(true);
        }

        /// <summary>The shop window; with `shortage` the «Не хватает ресурсов» one (the user, 2026-11-11): the same parts on a short frame, one strip, no timer.</summary>
        private static void BuildOfferShopWindow(bool shortage)
        {
            var layout = ReadOfferShopLayout();
            if (layout == null)
            {
                Debug.LogWarning("BuildOfferShopWindow: no UI/Shop/layout.json -- run Tools/store/offer_shop_art.py.");
                return;
            }
            if (shortage)
            {
                if (layout.s_strip == null || layout.s_strip.Length < 4)
                {
                    Debug.LogError("BuildOfferShopWindow: layout.json has no short window -- run Tools/store/offer_shop_art.py.");
                    return;
                }
                layout.window = layout.s_window; layout.frame = layout.s_frame; layout.title = layout.s_title;
                layout.close = layout.s_close; layout.counter = layout.s_counter; layout.counter_icon = layout.s_counter_icon;
                layout.counter_text = layout.s_counter_text; layout.toast = layout.s_toast;
            }
            else ReimportShopArt();
            var refW = layout.reference[0];
            var refH = layout.reference[1];
            var screenRect = new[] { 0f, 0f, refW, refH };

            var root = new GameObject(shortage ? "OfferShortageWindow" : "OfferShopWindow", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
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

            var title = ShopText(window, "Title", shortage ? "#shop_shortage_title" : "#shop_title", 72, UiTheme.Heavy, new Color(1f, 0.94f, 0.78f), TextAnchor.MiddleCenter);
            PlaceIn(title.rectTransform, screenRect, layout.title);
            Rim(title, new Color(0.35f, 0.05f, 0.06f), 4f);

            var strips = shortage ? new[] { layout.s_strip } : new[] { layout.strip0, layout.strip1, layout.strip2, layout.strip3 };
            // four places: a building's price, and so its shortfall, can ask for four resources
            var goods = new[] { layout.good0, layout.good1, layout.good2, layout.good3 };
            var amounts = new[] { layout.amount0, layout.amount1, layout.amount2, layout.amount3 };
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
                if (!shortage) BuildBoosterPart(slot, box, view);
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
            if (!shortage) BuildShopTabs(window, screenRect, screen);

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
            so.FindProperty("shortage").boolValue = shortage;
            so.FindProperty("pictureName").stringValue = shortage ? "Shop/WindowShort_" : "Shop/Window_";
            so.ApplyModifiedPropertiesWithoutUndo();
            // the views hold plain C# lists of references: set through the component, then saved with it
            var field = typeof(OfferShopWindow).GetField("slots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(screen, views);
            EditorUtility.SetDirty(screen);

            var path = shortage ? OfferShortagePrefabPath : OfferShopPrefabPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        /// <summary>
        /// The strip's «Бустеры» look (mock-up Design/store/Booster_Shop.png): the booster's picture on the left
        /// with the stock «×N» on its corner, the name, what it does, how long. Hidden on the offers tab.
        /// </summary>
        private static void BuildBoosterPart(RectTransform slot, float[] box, OfferShopWindow.SlotView view)
        {
            var part = new GameObject("Booster", typeof(RectTransform)).GetComponent<RectTransform>();
            part.SetParent(slot, false);
            StretchFull(part);
            view.boosterPart = part.gameObject;
            view.boosterIcon = CreateImage(part, "Icon", Color.white);
            view.boosterIcon.preserveAspect = true;
            view.boosterIcon.raycastTarget = false;
            PlaceIn(view.boosterIcon.rectTransform, box, On(box, new[] { 24f, 0f, 174f, 150f }));
            view.boosterName = ShopText(part, "Name", null, 40, UiTheme.Heavy, ShopInk, TextAnchor.MiddleLeft);
            PlaceIn(view.boosterName.rectTransform, box, On(box, new[] { 196f, 8f, 760f, 58f }));
            view.boosterDesc = ShopText(part, "Desc", null, 30, UiTheme.Bold, ShopInk, TextAnchor.MiddleLeft);
            PlaceIn(view.boosterDesc.rectTransform, box, On(box, new[] { 196f, 60f, 960f, 100f }));
            view.boosterTime = ShopText(part, "Time", null, 30, UiTheme.Heavy, UiTheme.Rgb(0x6a3fa0), TextAnchor.MiddleLeft);
            PlaceIn(view.boosterTime.rectTransform, box, On(box, new[] { 196f, 102f, 760f, 142f }));
            var stock = ShopImage(part, "Stock", "Badge", box, On(box, new[] { 112f, 100f, 196f, 148f }));
            view.boosterStockText = ShopText(stock.transform, "Text", null, 30, UiTheme.Heavy, Color.white, TextAnchor.MiddleCenter);
            StretchFull(view.boosterStockText.rectTransform);
            view.boosterStockText.rectTransform.offsetMin = new Vector2(0f, 4f);
            Rim(view.boosterStockText, UiTheme.Rgb(0x600c0e), 3f);
            view.boosterStock = stock.gameObject;
            part.gameObject.SetActive(false);
        }

        /// <summary>The two tabs hanging under the frame (the user, 2026-11-12): «Предложения» | «Бустеры».</summary>
        private static void BuildShopTabs(RectTransform window, float[] screenRect, OfferShopWindow screen)
        {
            Button Tab(string name, string key, float[] rect, out GameObject lit)
            {
                var button = ShopButton(window, name, "TabOff", screenRect, rect);
                var on = ShopImage(button.transform, "On", "TabOn", rect, rect);
                lit = on.gameObject;
                var label = ShopText(button.transform, "Label", key, 36, UiTheme.Heavy, ShopCream, TextAnchor.MiddleCenter);
                StretchFull(label.rectTransform);
                Rim(label, new Color(0.3f, 0.16f, 0.05f), 3f);
                return button;
            }
            var offers = Tab("TabOffers", "#shop_tab_offers", new[] { 610f, 1004f, 940f, 1082f }, out var offersLit);
            var boosters = Tab("TabBoosters", "#shop_tab_boosters", new[] { 980f, 1004f, 1310f, 1082f }, out var boostersLit);
            var so = new SerializedObject(screen);
            so.FindProperty("tabOffers").objectReferenceValue = offers;
            so.FindProperty("tabBoosters").objectReferenceValue = boosters;
            so.FindProperty("tabOffersLit").objectReferenceValue = offersLit;
            so.FindProperty("tabBoostersLit").objectReferenceValue = boostersLit;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- the HUD and the menu

        /// <summary>The OfferShop, its stall icon under the sack with the loud notice, and the menu's cards. After BuildGameMenu and BuildInventoryPanel.</summary>
        private static void BuildOfferShopHud(Transform uiRoot, ResourceIconLibrary iconLibrary)
        {
            var shopGO = new GameObject("OfferShop");
            shopGO.transform.SetParent(uiRoot, false);
            shopGO.AddComponent<OfferShop>();
            // the boosters' places in the shop and the running boosters' clocks (Shop/BoosterShop.cs)
            shopGO.AddComponent<BoosterShop>();

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
            BuildBoosterHud(uiRoot);
        }

        /// <summary>
        /// The booster hint's banner (Design/store/Booster_Hints.png): above the build menu, a parchment strip --
        /// the booster's picture, its name, what it does, «Включить» / «▶ Смотреть», a violet bar running down.
        /// </summary>
        private static void BuildBoosterHints(Transform uiRoot)
        {
            var holder = new GameObject("BoosterHints", typeof(RectTransform));
            holder.transform.SetParent(uiRoot, false);
            StretchFull((RectTransform)holder.transform);
            holder.AddComponent<SandboxOnly>();
            const float w = 880f, h = 130f;
            var banner = CreateImage(holder.transform, "Banner", Color.white);
            banner.sprite = ShopSprite("Strip");
            var rect = banner.rectTransform;
            Anchor(rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 240f), new Vector2(w, h));
            var group = banner.gameObject.AddComponent<CanvasGroup>();
            var box = new[] { 0f, 0f, w, h };
            var icon = CreateImage(rect, "Icon", Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            PlaceIn(icon.rectTransform, box, new[] { 12f, 5f, 132f, 125f });
            var title = ShopText(rect, "Title", null, 34, UiTheme.Heavy, ShopInk, TextAnchor.MiddleLeft);
            PlaceIn(title.rectTransform, box, new[] { 144f, 16f, 600f, 62f });
            var line = ShopText(rect, "Line", null, 26, UiTheme.Bold, UiTheme.Rgb(0x6e4824), TextAnchor.MiddleLeft);
            PlaceIn(line.rectTransform, box, new[] { 144f, 66f, 610f, 108f });
            var useRect = new[] { 620f, 18f, 860f, 112f };
            var use = ShopButton(rect, "Use", "ButtonGreen", box, useRect);
            var play = ShopImage(use.transform, "Play", "Play", useRect, new[] { 632f, 41f, 680f, 89f });
            var useLabel = ShopText(use.transform, "Label", null, 32, UiTheme.Heavy, Color.white, TextAnchor.MiddleCenter);
            PlaceIn(useLabel.rectTransform, useRect, new[] { 660f, 18f, 860f, 106f });
            Rim(useLabel, UiTheme.Rgb(0x1c5412), 3f);
            var close = ShopButton(rect, "Close", "Close", box, new[] { 836f, -18f, 896f, 42f });
            var bar = CreateImage(rect, "Bar", new Color(0.75f, 0.43f, 1f, 0.9f));
            bar.raycastTarget = false;
            PlaceIn(bar.rectTransform, box, new[] { 30f, 116f, w - 30f, 124f });
            var barPos = bar.rectTransform.anchoredPosition;
            bar.rectTransform.pivot = new Vector2(0f, 0.5f);
            bar.rectTransform.anchoredPosition = barPos - new Vector2((w - 60f) * 0.5f, 0f);

            var hints = holder.AddComponent<BoosterHints>();
            var so = new SerializedObject(hints);
            so.FindProperty("banner").objectReferenceValue = rect;
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("title").objectReferenceValue = title;
            so.FindProperty("line").objectReferenceValue = line;
            so.FindProperty("use").objectReferenceValue = use;
            so.FindProperty("useLabel").objectReferenceValue = useLabel;
            so.FindProperty("play").objectReferenceValue = play.gameObject;
            so.FindProperty("close").objectReferenceValue = close;
            so.FindProperty("bar").objectReferenceValue = bar.rectTransform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The running boosters' row under the resource bar, where «День» was (Design/store/Booster_Hud.png):
        /// round chips -- a dark disc, the booster's picture, a gold ring of time left, the time under it.
        /// </summary>
        private static void BuildBoosterHud(Transform uiRoot)
        {
            var rowGO = new GameObject("BoosterHud", typeof(RectTransform));
            rowGO.transform.SetParent(uiRoot, false);
            var infoPanel = uiRoot.Find("BuildingInfoPanel");
            if (infoPanel != null) rowGO.transform.SetSiblingIndex(infoPanel.GetSiblingIndex());
            var row = (RectTransform)rowGO.transform;
            Anchor(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(10f, 10f));
            rowGO.AddComponent<SandboxOnly>();
            const int count = 11;
            const float disc = 92f;
            var chips = new BoosterHud.Chip[count];
            for (var i = 0; i < count; i++)
            {
                var back = CreateImage(row, "Chip" + (i + 1), Color.white);
                back.sprite = ShopSprite("BoosterDisc");
                back.rectTransform.sizeDelta = new Vector2(disc, disc);
                var button = back.gameObject.AddComponent<Button>();
                button.targetGraphic = back;
                var ring = CreateImage(back.transform, "Ring", new Color(1f, 0.82f, 0.31f));
                ring.sprite = ShopSprite("BoosterRing");
                ring.type = Image.Type.Filled;
                ring.fillMethod = Image.FillMethod.Radial360;
                ring.fillOrigin = (int)Image.Origin360.Top;
                ring.fillClockwise = false;
                ring.raycastTarget = false;
                StretchFull(ring.rectTransform);
                var icon = CreateImage(back.transform, "Icon", Color.white);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.rectTransform.sizeDelta = new Vector2(disc * 0.9f, disc * 0.9f);
                var time = CreateText(back.transform, "Time", string.Empty, 24, new Vector2(0f, -disc * 0.5f - 14f), new Vector2(110f, 30f), Color.white);
                time.font = UiTheme.Heavy;
                time.raycastTarget = false;
                Rim(time, new Color(0.12f, 0.08f, 0.04f), 3f);
                chips[i] = new BoosterHud.Chip { button = button, ring = ring, icon = icon, time = time };
            }
            BuildBoosterHints(uiRoot);
            var hud = rowGO.AddComponent<BoosterHud>();
            var so = new SerializedObject(hud);
            var prop = so.FindProperty("chips");
            prop.arraySize = count;
            for (var i = 0; i < count; i++)
            {
                var element = prop.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("button").objectReferenceValue = chips[i].button;
                element.FindPropertyRelative("ring").objectReferenceValue = chips[i].ring;
                element.FindPropertyRelative("icon").objectReferenceValue = chips[i].icon;
                element.FindPropertyRelative("time").objectReferenceValue = chips[i].time;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
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
