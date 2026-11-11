using System;
using System.IO;
using CityBuilder.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The daily login bonus window (UI/DailyBonusWindow.cs) as a prefab in Resources. The frame,
    /// picture, close seal and buttons are the offer shop's (UI/Shop); the day cards, pill and tick come
    /// from Tools/store/daily_bonus_art.py (UI/DailyBonus/*.png + layout.json, rects in the 1920 x 1080
    /// reference, y down). Words are keys on the localization sheet (#daily_*). After BuildOfferShopWindow
    /// (it re-imports the shop's art).
    /// </summary>
    public static partial class SetupProject
    {
        private const string DailyBonusPrefabPath = "Assets/_Project/Resources/DailyBonusWindow.prefab";

        [Serializable]
        private class DailyBonusLayout
        {
            public float[] reference;
            public float pad;
            public float[] card_crystal, card_crystal_big, card_tick, card_day, card_pill, card_pill_text, card_pill_crystal;
            public float[] line, take, double_, take_label, take_amount, take_crystal, double_play, double_label, double_amount, double_crystal;
            public float[] cards_0, cards_1, cards_2, cards_3, cards_4, cards_5, cards_6, cards_7, cards_8, cards_9;
        }

        private static Sprite DailySprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{UiTextureImport.DailyBonusFolder}/{name}.png");

        private static void BuildDailyBonusWindow()
        {
            var shop = ReadOfferShopLayout();
            var path = $"{UiTextureImport.DailyBonusFolder}/layout.json";
            if (shop == null || !File.Exists(path))
            {
                Debug.LogWarning("BuildDailyBonusWindow: no UI/DailyBonus/layout.json -- run Tools/store/daily_bonus_art.py.");
                return;
            }
            // "double" is a C# keyword
            var json = File.ReadAllText(path).Replace("\"double\"", "\"double_\"");
            var layout = JsonUtility.FromJson<DailyBonusLayout>(json);
            var cards = new[] { layout.cards_0, layout.cards_1, layout.cards_2, layout.cards_3, layout.cards_4,
                                layout.cards_5, layout.cards_6, layout.cards_7, layout.cards_8, layout.cards_9 };
            if (Array.Exists(cards, c => c == null || c.Length < 4))
            {
                Debug.LogError("BuildDailyBonusWindow: layout.json lacks a card's rect -- run Tools/store/daily_bonus_art.py.");
                return;
            }
            // pictures that came in before UiTextureImport knew the folder: again under its rules
            foreach (var file in Directory.GetFiles(UiTextureImport.DailyBonusFolder, "*.png"))
            {
                var p = file.Replace(Path.DirectorySeparatorChar, '/');
                if (AssetImporter.GetAtPath(p) is TextureImporter ti
                    && (ti.textureType != TextureImporterType.Sprite || AssetDatabase.LoadAssetAtPath<Sprite>(p) == null))
                    AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
            }

            var refW = layout.reference[0];
            var refH = layout.reference[1];
            var screenRect = new[] { 0f, 0f, refW, refH };

            var root = new GameObject("DailyBonusWindow", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            ConfigureScaler(root.GetComponent<CanvasScaler>());
            var screen = root.AddComponent<DailyBonusWindow>();

            var shade = CreateImage(root.transform, "Shade", new Color(0.04f, 0.03f, 0.02f, 0.63f));
            StretchFull(shade.rectTransform);
            var shadeButton = shade.gameObject.AddComponent<Button>();
            shadeButton.transition = Selectable.Transition.None;

            var window = new GameObject("Window", typeof(RectTransform)).GetComponent<RectTransform>();
            window.SetParent(root.transform, false);
            window.anchorMin = window.anchorMax = window.pivot = new Vector2(0.5f, 0.5f);
            window.sizeDelta = new Vector2(refW, refH);

            var hit = CreateImage(window, "FrameHit", new Color(0f, 0f, 0f, 0f));
            PlaceIn(hit.rectTransform, screenRect, shop.frame);
            var picture = CreateImage(window, "Picture", Color.white);
            picture.raycastTarget = false;
            PlaceIn(picture.rectTransform, screenRect, shop.window);

            var title = ShopText(window, "Title", "#daily_title", 72, UiTheme.Heavy, new Color(1f, 0.94f, 0.78f), TextAnchor.MiddleCenter);
            PlaceIn(title.rectTransform, screenRect, shop.title);
            Rim(title, new Color(0.35f, 0.05f, 0.06f), 4f);

            // the player's crystals, top right: the number left of the crystal (the shop's counter)
            var counter = new GameObject("Crystals", typeof(RectTransform)).GetComponent<RectTransform>();
            counter.SetParent(window, false);
            PlaceIn(counter, screenRect, shop.counter);
            var counterIcon = CreateImage(counter, "Icon", Color.white);
            counterIcon.sprite = CrystalIcon();
            counterIcon.preserveAspect = true;
            counterIcon.raycastTarget = false;
            PlaceIn(counterIcon.rectTransform, shop.counter, shop.counter_icon);
            var counterText = ShopText(counter, "Count", null, 48, UiTheme.Heavy, new Color(1f, 240 / 255f, 210 / 255f), TextAnchor.MiddleRight);
            PlaceIn(counterText.rectTransform, shop.counter, shop.counter_text);
            Rim(counterText, new Color(0.24f, 0.14f, 0.06f), 3f);

            float[] At(float[] card, float[] r) => new[] { card[0] + r[0], card[1] + r[1], card[0] + r[2], card[1] + r[3] };

            var views = new DailyBonusWindow.DayView[cards.Length];
            for (var i = 0; i < cards.Length; i++)
            {
                var c = cards[i];
                var day = new GameObject($"Day{i + 1}", typeof(RectTransform)).GetComponent<RectTransform>();
                day.SetParent(window, false);
                PlaceIn(day, screenRect, c);
                var view = new DailyBonusWindow.DayView();
                view.card = CreateImage(day, "Card", Color.white);
                view.card.sprite = DailySprite("Card_ahead");
                view.card.raycastTarget = false;
                PlaceIn(view.card.rectTransform, c, Grow(c, layout.pad));

                Image Crystal(string name, float[] r)
                {
                    var image = CreateImage(day, name, Color.white);
                    image.sprite = CrystalIcon();
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                    PlaceIn(image.rectTransform, c, At(c, r));
                    return image;
                }

                view.crystal = Crystal("Crystal", layout.card_crystal);
                view.crystalBig = Crystal("CrystalBig", layout.card_crystal_big);

                view.day = ShopText(day, "DayName", null, 28, UiTheme.Heavy, new Color(1f, 0.95f, 0.84f), TextAnchor.MiddleCenter);
                PlaceIn(view.day.rectTransform, c, At(c, layout.card_day));
                Rim(view.day, new Color(0.25f, 0.07f, 0.07f), 2f);

                var pillRect = At(c, layout.card_pill);
                var pill = CreateImage(day, "Pill", Color.white);
                pill.sprite = DailySprite("Pill");
                pill.raycastTarget = false;
                PlaceIn(pill.rectTransform, c, pillRect);
                view.amount = ShopText(pill.transform, "Amount", null, 44, UiTheme.Heavy, new Color(1f, 0.965f, 0.87f), TextAnchor.MiddleRight);
                PlaceIn(view.amount.rectTransform, pillRect, At(c, layout.card_pill_text));
                Rim(view.amount, new Color(0.12f, 0.06f, 0.02f), 2f);
                var pillCrystal = CreateImage(pill.transform, "Crystal", Color.white);
                pillCrystal.sprite = CrystalIcon();
                pillCrystal.preserveAspect = true;
                pillCrystal.raycastTarget = false;
                PlaceIn(pillCrystal.rectTransform, pillRect, At(c, layout.card_pill_crystal));

                view.tick = CreateImage(day, "Tick", Color.white);
                view.tick.sprite = DailySprite("Tick");
                view.tick.raycastTarget = false;
                PlaceIn(view.tick.rectTransform, c, At(c, layout.card_tick));
                views[i] = view;
            }

            var line = ShopText(window, "Line", "#daily_line", 40, UiTheme.Heavy, ShopCream, TextAnchor.MiddleCenter);
            PlaceIn(line.rectTransform, screenRect, layout.line);
            Rim(line, new Color(0.24f, 0.14f, 0.06f), 3f);

            // «Забрать 10💎» and «▶ ×2 20💎»: the number right-aligned, the crystal after it
            var take = ShopButton(window, "Take", "ButtonBlueWide", screenRect, layout.take);
            var takeLabel = ShopText(take.transform, "Label", "#daily_take", 46, UiTheme.Heavy, Color.white, TextAnchor.MiddleCenter);
            PlaceIn(takeLabel.rectTransform, layout.take, layout.take_label);
            Rim(takeLabel, UiTheme.Rgb(0x103c70), 3f);
            var takeAmount = ShopText(take.transform, "Amount", null, 46, UiTheme.Heavy, Color.white, TextAnchor.MiddleRight);
            PlaceIn(takeAmount.rectTransform, layout.take, layout.take_amount);
            Rim(takeAmount, UiTheme.Rgb(0x103c70), 3f);
            var takeCrystal = CreateImage(take.transform, "Crystal", Color.white);
            takeCrystal.sprite = CrystalIcon();
            takeCrystal.preserveAspect = true;
            takeCrystal.raycastTarget = false;
            PlaceIn(takeCrystal.rectTransform, layout.take, layout.take_crystal);

            var doubled = ShopButton(window, "Double", "ButtonGreenWide", screenRect, layout.double_);
            ShopImage(doubled.transform, "Play", "Play", layout.double_, layout.double_play);
            var doubleLabel = ShopText(doubled.transform, "Label", null, 52, UiTheme.Heavy, Color.white, TextAnchor.MiddleCenter);
            PlaceIn(doubleLabel.rectTransform, layout.double_, layout.double_label);
            Rim(doubleLabel, UiTheme.Rgb(0x1c5412), 3f);
            var doubleAmount = ShopText(doubled.transform, "Amount", null, 46, UiTheme.Heavy, Color.white, TextAnchor.MiddleRight);
            PlaceIn(doubleAmount.rectTransform, layout.double_, layout.double_amount);
            Rim(doubleAmount, UiTheme.Rgb(0x1c5412), 3f);
            var doubleCrystal = CreateImage(doubled.transform, "Crystal", Color.white);
            doubleCrystal.sprite = CrystalIcon();
            doubleCrystal.preserveAspect = true;
            doubleCrystal.raycastTarget = false;
            PlaceIn(doubleCrystal.rectTransform, layout.double_, layout.double_crystal);

            var close = ShopButton(window, "Close", "Close", screenRect, shop.close);

            var so = new SerializedObject(screen);
            so.FindProperty("group").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("window").objectReferenceValue = window;
            so.FindProperty("shade").objectReferenceValue = shadeButton;
            so.FindProperty("close").objectReferenceValue = close;
            so.FindProperty("take").objectReferenceValue = take;
            so.FindProperty("doubled").objectReferenceValue = doubled;
            so.FindProperty("takeAmount").objectReferenceValue = takeAmount;
            so.FindProperty("doubleAmount").objectReferenceValue = doubleAmount;
            so.FindProperty("doubleLabel").objectReferenceValue = doubleLabel;
            so.FindProperty("counterText").objectReferenceValue = counterText;
            so.FindProperty("picture").objectReferenceValue = picture;
            so.FindProperty("size").floatValue = OfferShopSize;
            so.FindProperty("cardTaken").objectReferenceValue = DailySprite("Card_taken");
            so.FindProperty("cardToday").objectReferenceValue = DailySprite("Card_today");
            so.FindProperty("cardAhead").objectReferenceValue = DailySprite("Card_ahead");
            so.FindProperty("cardBig").objectReferenceValue = DailySprite("Card_big");
            so.FindProperty("cardBigToday").objectReferenceValue = DailySprite("Card_big_today");
            so.ApplyModifiedPropertiesWithoutUndo();
            var field = typeof(DailyBonusWindow).GetField("days", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(screen, views);
            EditorUtility.SetDirty(screen);

            foreach (var name in new[] { "Card_taken", "Card_today", "Card_ahead", "Card_big", "Card_big_today", "Pill", "Tick" })
                if (DailySprite(name) == null) Debug.LogError($"BuildDailyBonusWindow: UI/DailyBonus/{name}.png does not load as a sprite.");

            Directory.CreateDirectory(Path.GetDirectoryName(DailyBonusPrefabPath));
            PrefabUtility.SaveAsPrefabAsset(root, DailyBonusPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
