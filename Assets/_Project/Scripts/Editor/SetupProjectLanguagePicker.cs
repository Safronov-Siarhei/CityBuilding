using System;
using System.IO;
using CityBuilder.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The main menu's language picker (UI/LanguagePicker; the user 2026-11-14, concept v2 approved,
    /// Design/ui/LanguagePicker_Concept.png): a small parchment pill in the main screen's top-right
    /// corner and the list it opens. The languages, their own names and flags come from
    /// Tools/ui/language_flags.py (UI/Icons/Flags/languages.json + Flag_&lt;code&gt;.png).
    /// </summary>
    public static partial class SetupProject
    {
        private const string FlagsFolder = UiTextureImport.IconsFolder + "/Flags";
        private static readonly Vector2 PickerPill = new Vector2(244f, 60f);
        private const float PickerInset = 40f;
        private const float PickerRow = 62f;
        private const float PickerPad = 18f;
        private const float PickerListWidth = 318f;
        private static readonly Color PickerInk = new Color32(0x54, 0x32, 0x18, 0xFF);

        [Serializable] private class PickerLanguage { public string code; public string name; }
        [Serializable] private class PickerLanguages { public PickerLanguage[] languages; }

        private static void BuildLanguagePicker(Transform screen)
        {
            var path = $"{FlagsFolder}/languages.json";
            if (!File.Exists(path))
            {
                Debug.LogWarning("BuildLanguagePicker: no UI/Icons/Flags/languages.json -- run Tools/ui/language_flags.py.");
                return;
            }
            var langs = JsonUtility.FromJson<PickerLanguages>(File.ReadAllText(path)).languages;
            Sprite Icon(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{FlagsFolder}/{name}.png");

            var root = StretchedChild(screen, "LanguagePicker");
            var picker = root.gameObject.AddComponent<LanguagePicker>();

            // ---- the pill: flag, the language's own name, a chevron
            var pill = MenuImage(root, "Pill", "plaque", Vector2.zero, PickerPill);
            Anchor(pill.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-PickerInset, -PickerInset), PickerPill);
            var pillButton = pill.gameObject.AddComponent<Button>();
            var pillFlag = PickerIcon(pill.transform, "Flag", Icon("Flag_en"), new Vector2(18f + 24f, 0f), new Vector2(48f, 32f));
            var pillName = PickerText(pill.transform, "Name", 26, new Vector2(76f, 0f), new Vector2(PickerPill.x - 76f - 44f, PickerPill.y));
            var chevron = PickerIcon(pill.transform, "Chevron", Icon("Chevron"), new Vector2(PickerPill.x - 26f, 0f), new Vector2(24f, 24f));

            // ---- the list: a tap outside it closes it
            var list = StretchedChild(root, "List");
            var blocker = CreateImage(list, "Outside", new Color(0f, 0f, 0f, 0.35f));
            StretchBleed(blocker.rectTransform);
            var outside = blocker.gameObject.AddComponent<Button>();
            outside.transition = Selectable.Transition.None;
            var size = new Vector2(PickerListWidth, PickerPad * 2f + PickerRow * langs.Length);
            var card = MenuImage(list, "Card", "card", Vector2.zero, size);
            Anchor(card.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-PickerInset, -PickerInset - PickerPill.y - 18f), size);

            var codes = new string[langs.Length];
            var names = new string[langs.Length];
            var flags = new Sprite[langs.Length];
            var marks = new GameObject[langs.Length];
            for (var i = 0; i < langs.Length; i++)
            {
                codes[i] = langs[i].code;
                names[i] = langs[i].name;
                flags[i] = Icon("Flag_" + langs[i].code);
                var rowSize = new Vector2(size.x - 32f, PickerRow - 8f);
                var row = CreateImage(card.transform, "Row_" + langs[i].code, new Color(1f, 1f, 1f, 0f));
                Anchor(row.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -PickerPad - PickerRow * (i + 0.5f)), rowSize);
                var button = row.gameObject.AddComponent<Button>();
                UnityEventTools.AddIntPersistentListener(button.onClick, picker.Pick, i);
                // the current one: gold, with a tick
                var mark = MenuImage(row.transform, "Current", "row_selected", Vector2.zero, rowSize);
                mark.raycastTarget = false;
                StretchFull(mark.rectTransform);
                PickerIcon(mark.transform, "Tick", Icon("Tick"), new Vector2(rowSize.x - 34f, 0f), new Vector2(36f, 36f));
                marks[i] = mark.gameObject;
                PickerIcon(row.transform, "Flag", flags[i], new Vector2(14f + 27f, 0f), new Vector2(54f, 36f));
                var name = PickerText(row.transform, "Name", 28, new Vector2(86f, 0f), new Vector2(rowSize.x - 86f - 50f, rowSize.y));
                name.text = names[i];
            }
            list.gameObject.SetActive(false);

            var so = new SerializedObject(picker);
            so.FindProperty("pillFlag").objectReferenceValue = pillFlag;
            so.FindProperty("pillName").objectReferenceValue = pillName;
            so.FindProperty("chevron").objectReferenceValue = chevron.rectTransform;
            so.FindProperty("list").objectReferenceValue = list.gameObject;
            SetArray(so.FindProperty("codes"), codes, (p, v) => p.stringValue = v);
            SetArray(so.FindProperty("names"), names, (p, v) => p.stringValue = v);
            SetArray(so.FindProperty("flags"), flags, (p, v) => p.objectReferenceValue = v);
            SetArray(so.FindProperty("marks"), marks, (p, v) => p.objectReferenceValue = v);
            so.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(pillButton.onClick, picker.Toggle);
            UnityEventTools.AddPersistentListener(outside.onClick, picker.Close);
        }

        private static void SetArray<T>(SerializedProperty array, T[] values, Action<SerializedProperty, T> set)
        {
            array.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) set(array.GetArrayElementAtIndex(i), values[i]);
        }

        /// <summary>A picture placed from its parent's left edge (x = its centre), vertically centred.</summary>
        private static Image PickerIcon(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
        {
            var image = CreateImage(parent, name, Color.white);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            Anchor(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            return image;
        }

        /// <summary>A left-aligned name (not localized: a language's name is written in that language).</summary>
        private static Text PickerText(Transform parent, string name, int size, Vector2 position, Vector2 box)
        {
            var text = CreateText(parent, name, string.Empty, size, Vector2.zero, box, PickerInk);
            text.font = UiTheme.Bold;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.raycastTarget = false;
            Anchor(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), position, box);
            return text;
        }
    }
}
