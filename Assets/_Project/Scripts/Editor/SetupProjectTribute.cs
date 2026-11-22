using System;
using System.IO;
using CityBuilder.Resources;
using CityBuilder.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The orcs' tribute scroll (the user, 2026-11-22; concept C round 2): the window -- a dim, the chief to the
    /// scroll's height on its left, the scroll with its title, threat, up to five tribute rows, the time and three
    /// buttons, a cross -- and the badge it rolls into under the resource bar. Pieces from UI/Tribute
    /// (Tools/ui/tribute_art.py); places from the concept's 1920x1080 picture, in the scroll picture's pixels.
    /// </summary>
    public static partial class SetupProject
    {
        private static Sprite TributeSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{UiTextureImport.TributeFolder}/{name}.png");

        private static void BuildTributeScroll(Transform uiRoot)
        {
            // pictures imported before the importer knew the folder: again under its rules
            if (Directory.Exists(UiTextureImport.TributeFolder))
                foreach (var file in Directory.GetFiles(UiTextureImport.TributeFolder, "*.png"))
                    AssetDatabase.ImportAsset(file.Replace('\\', '/'), ImportAssetOptions.ForceUpdate);

            var holder = new GameObject("TributeScroll", typeof(RectTransform));
            holder.transform.SetParent(uiRoot, false);
            StretchFull((RectTransform)holder.transform);
            holder.AddComponent<SandboxOnly>();

            // ---- the window
            var window = new GameObject("Window", typeof(RectTransform));
            window.transform.SetParent(holder.transform, false);
            StretchFull((RectTransform)window.transform);
            var dim = CreateImage(window.transform, "Dim", new Color(0f, 0f, 0f, 0.6f));
            StretchBleed(dim.rectTransform);

            var box = new[] { 0f, 0f, 1020f, 960f };
            var scrollImage = CreateImage(window.transform, "Scroll", Color.white);
            scrollImage.sprite = TributeSprite("Scroll");
            scrollImage.raycastTarget = true;
            var scroll = scrollImage.rectTransform;
            Anchor(scroll, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(170f, -20f), new Vector2(box[2], box[3]));
            scroll.localScale = new Vector3(0.92f, 0.92f, 1f);

            var chief = CreateImage(scroll, "Chief", Color.white);
            chief.sprite = TributeSprite("Chief");
            chief.preserveAspect = true;
            chief.raycastTarget = false;
            PlaceIn(chief.rectTransform, box, new[] { -413f, 28f, 170f, 948f });

            var red = UiTheme.Rgb(0x781610);
            var title = ShopText(scroll, "Title", null, 50, UiTheme.Heavy, red, TextAnchor.MiddleCenter);
            PlaceIn(title.rectTransform, box, new[] { 80f, 80f, 940f, 140f });
            Rim(title, new Color(0.94f, 0.87f, 0.69f), 3f);
            var threat = ShopText(scroll, "Threat", null, 28, UiTheme.Bold, UiTheme.Rgb(0x5a4224), TextAnchor.MiddleCenter);
            PlaceIn(threat.rectTransform, box, new[] { 80f, 144f, 940f, 184f });

            var rows = new TributeScroll.Row[5];
            for (var i = 0; i < rows.Length; i++)
            {
                var yc = 232f + 80f * i;
                var plate = CreateImage(scroll, "Row" + (i + 1), Color.white);
                plate.sprite = TributeSprite("Row");
                plate.type = Image.Type.Simple;
                plate.raycastTarget = false;
                var rowBox = new[] { 170f, yc - 34f, 850f, yc + 34f };
                PlaceIn(plate.rectTransform, box, rowBox);
                var icon = CreateImage(plate.transform, "Icon", Color.white);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                PlaceIn(icon.rectTransform, rowBox, new[] { 190f, yc - 27f, 244f, yc + 27f });
                var name = ShopText(plate.transform, "Name", null, 34, UiTheme.Bold, ShopInk, TextAnchor.MiddleLeft);
                PlaceIn(name.rectTransform, rowBox, new[] { 270f, yc - 30f, 520f, yc + 30f });
                var need = ShopText(plate.transform, "Need", null, 38, UiTheme.Heavy, ShopInk, TextAnchor.MiddleRight);
                PlaceIn(need.rectTransform, rowBox, new[] { 430f, yc - 30f, 580f, yc + 30f });
                var have = ShopText(plate.transform, "Have", null, 30, UiTheme.Bold, ShopInk, TextAnchor.MiddleLeft);
                PlaceIn(have.rectTransform, rowBox, new[] { 600f, yc - 30f, 830f, yc + 30f });
                rows[i] = new TributeScroll.Row { root = plate.gameObject, plate = plate, icon = icon, name = name, need = need, have = have };
            }

            var attackIn = ShopText(scroll, "AttackIn", null, 30, UiTheme.Bold, UiTheme.Rgb(0x503c22), TextAnchor.MiddleCenter);
            PlaceIn(attackIn.rectTransform, box, new[] { 80f, 635f, 940f, 675f });
            var timer = ShopText(scroll, "Timer", null, 56, UiTheme.Heavy, UiTheme.Rgb(0xc42822), TextAnchor.MiddleCenter);
            PlaceIn(timer.rectTransform, box, new[] { 80f, 670f, 940f, 740f });
            Rim(timer, new Color(0.94f, 0.87f, 0.69f), 3f);

            Button Make(string name, Sprite sprite, float[] r, string key, Color rim, out Image image, out Text label)
            {
                image = CreateImage(scroll, name, Color.white);
                image.sprite = sprite;
                PlaceIn(image.rectTransform, box, r);
                var button = image.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                label = ShopText(image.transform, "Label", key, 34, UiTheme.Heavy, Color.white, TextAnchor.MiddleCenter);
                PlaceIn(label.rectTransform, r, new[] { r[0], r[1] - 6f, r[2], r[3] - 6f });
                Rim(label, rim, 3f);
                return button;
            }
            var payRect = new[] { 150f, 760f, 390f, 850f };
            var pay = Make("Pay", AssetDatabase.LoadAssetAtPath<Sprite>($"{UiTextureImport.DevGiftFolder}/ClaimButton.png"), payRect,
                "#tribute_pay", UiTheme.Rgb(0x285a14), out var payImage, out var payLabel);
            var fight = Make("Fight", TributeSprite("ButtonRed"), new[] { 410f, 766f, 620f, 844f }, "#tribute_fight", UiTheme.Rgb(0x500e0a), out _, out _);
            var later = Make("Later", ShopSprite("ButtonOrange"), new[] { 640f, 760f, 870f, 850f }, "#tribute_later", UiTheme.Rgb(0x6e320a), out _, out _);
            var close = ShopButton(scroll, "Close", "Close", box, new[] { 900f, -12f, 996f, 84f });

            // ---- the badge: the booster badge's row and size (BuildBoosterHints), the rolled scroll on its disc
            const float disc = 104f;
            var badge = new GameObject("Badge", typeof(RectTransform)).GetComponent<RectTransform>();
            badge.SetParent(holder.transform, false);
            Anchor(badge, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -180f), new Vector2(disc, disc));
            var back = CreateImage(badge, "Disc", Color.white);
            back.sprite = TributeSprite("Badge");
            back.preserveAspect = true;
            Anchor(back.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(disc * 1.3125f, disc * 1.3125f));
            var badgeButton = back.gameObject.AddComponent<Button>();
            badgeButton.targetGraphic = back;
            var ring = CreateImage(badge, "Ring", new Color(1f, 0.78f, 0.24f));
            ring.sprite = ShopSprite("BoosterRing");
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = false;
            ring.raycastTarget = false;
            Anchor(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(disc + 12f, disc + 12f));
            var badgeTimer = CreateText(badge, "Time", string.Empty, 26, new Vector2(0f, -disc * 0.5f - 18f), new Vector2(120f, 34f), new Color(1f, 0.93f, 0.78f));
            badgeTimer.font = UiTheme.Heavy;
            badgeTimer.raycastTarget = false;
            Rim(badgeTimer, UiTheme.Rgb(0x50140e), 3f);

            var icons = new Sprite[Enum.GetValues(typeof(ResourceType)).Length];
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                if ((int)type < icons.Length) icons[(int)type] = CreateResourceIcon(type);

            var component = holder.AddComponent<TributeScroll>();
            var so = new SerializedObject(component);
            so.FindProperty("window").objectReferenceValue = window;
            so.FindProperty("title").objectReferenceValue = title;
            so.FindProperty("threat").objectReferenceValue = threat;
            so.FindProperty("attackIn").objectReferenceValue = attackIn;
            so.FindProperty("timer").objectReferenceValue = timer;
            var rowsProp = so.FindProperty("rows");
            rowsProp.arraySize = rows.Length;
            for (var i = 0; i < rows.Length; i++)
            {
                var e = rowsProp.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("root").objectReferenceValue = rows[i].root;
                e.FindPropertyRelative("plate").objectReferenceValue = rows[i].plate;
                e.FindPropertyRelative("icon").objectReferenceValue = rows[i].icon;
                e.FindPropertyRelative("name").objectReferenceValue = rows[i].name;
                e.FindPropertyRelative("need").objectReferenceValue = rows[i].need;
                e.FindPropertyRelative("have").objectReferenceValue = rows[i].have;
            }
            so.FindProperty("pay").objectReferenceValue = pay;
            so.FindProperty("payImage").objectReferenceValue = payImage;
            so.FindProperty("payLabel").objectReferenceValue = payLabel;
            so.FindProperty("fight").objectReferenceValue = fight;
            so.FindProperty("later").objectReferenceValue = later;
            so.FindProperty("close").objectReferenceValue = close;
            so.FindProperty("badge").objectReferenceValue = badge;
            so.FindProperty("badgeRing").objectReferenceValue = ring;
            so.FindProperty("badgeTimer").objectReferenceValue = badgeTimer;
            so.FindProperty("badgeButton").objectReferenceValue = badgeButton;
            var iconsProp = so.FindProperty("resourceIcons");
            iconsProp.arraySize = icons.Length;
            for (var i = 0; i < icons.Length; i++) iconsProp.GetArrayElementAtIndex(i).objectReferenceValue = icons[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
