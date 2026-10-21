using System.Collections.Generic;
using System.IO;
using CityBuilder.Buildings;
using CityBuilder.CameraControl;
using CityBuilder.Cheats;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Resources;
using CityBuilder.Saving;
using CityBuilder.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// Small UI builders every panel uses: text, buttons, images, input fields, cost rows, Localize.
    /// </summary>
    public static partial class SetupProject
    {
        private static Button CreateIconButton(Transform parent, Sprite backgroundSprite, Sprite iconSprite, string name, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var background = go.GetComponent<Image>();
            background.sprite = backgroundSprite;
            background.type = Image.Type.Sliced;
            background.color = new Color(0.26f, 0.29f, 0.24f, 0.95f);

            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(go.transform, false);
            var iconRect = iconGO.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            var pad = Mathf.Min(sizeDelta.x, sizeDelta.y) * 0.16f;
            iconRect.offsetMin = new Vector2(pad, pad);
            iconRect.offsetMax = new Vector2(-pad, -pad);
            var iconImage = iconGO.GetComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.preserveAspect = true;

            return go.GetComponent<Button>();
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go.GetComponent<Image>();
        }

        /// <summary>Empty horizontal row for BuildingInfoPanelController to fill with icon+number cost chips at runtime (see its BuildCostRow) -- the layout group handles spacing/centering, so runtime code only ever adds/removes leaf children.</summary>
        private static Transform CreateCostRow(Transform parent, string name, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(640f, 44f);

            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 18f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return go.transform;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static InputField CreateInputField(Transform parent, string name, string placeholderText, Vector2 anchoredPos, Vector2 sizeDelta, Sprite backgroundSprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var bgImage = go.GetComponent<Image>();
            bgImage.sprite = backgroundSprite;
            bgImage.type = Image.Type.Sliced;
            bgImage.color = Color.white;

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20f, 6f);
            textRect.offsetMax = new Vector2(-20f, -6f);
            var textComp = textGO.GetComponent<Text>();
            textComp.font = UiTheme.Regular;
            textComp.fontSize = 30;
            textComp.alignment = TextAnchor.MiddleLeft;
            textComp.color = Color.black;
            textComp.supportRichText = false;

            var placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeholderGO.transform.SetParent(go.transform, false);
            var placeholderRect = placeholderGO.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(20f, 6f);
            placeholderRect.offsetMax = new Vector2(-20f, -6f);
            var placeholderComp = placeholderGO.GetComponent<Text>();
            placeholderComp.font = textComp.font;
            placeholderComp.fontSize = 30;
            placeholderComp.alignment = TextAnchor.MiddleLeft;
            placeholderComp.color = new Color(0f, 0f, 0f, 0.4f);
            placeholderComp.text = placeholderText;
            placeholderComp.fontStyle = FontStyle.Italic;

            var inputField = go.GetComponent<InputField>();
            inputField.textComponent = textComp;
            inputField.placeholder = placeholderComp;
            inputField.characterLimit = 24;

            return inputField;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, Vector2 anchoredPos, Vector2 sizeDelta, Color? color = null, bool addShadow = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = UiTheme.Regular;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color ?? Color.white;

            // Only for standalone display text (e.g. the menu title) -- never on button labels,
            // which stay flat against their own carved-stone panel background.
            if (addShadow)
            {
                var shadow = go.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
                shadow.effectDistance = new Vector2(3f, -3f);
            }

            return text;
        }

        /// <summary>Hangs a LocalizedText on a fixed caption so it fills itself in and follows a language change. Returns the label, so it can wrap a CreateText call inline.</summary>
        private static Text Localize(Text label, string key)
        {
            label.gameObject.AddComponent<LocalizedText>().SetKey(key);
            return label;
        }

        /// <summary>
        /// A button whose caption comes from the localization sheet: `labelKey` is a key, not text.
        /// The label carries a LocalizedText, so it re-reads itself when the player switches
        /// language without anything having to hunt down every button in the scene.
        /// </summary>
        private static Button CreateButton(Transform parent, Sprite sprite, string name, string labelKey, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.26f, 0.29f, 0.24f, 0.95f);

            Localize(CreateText(go.transform, "Label", string.Empty, 28, Vector2.zero, sizeDelta - new Vector2(20f, 20f)), labelKey);
            return go.GetComponent<Button>();
        }

        /// <summary>
        /// A button whose caption its controller writes at play time. Deliberately NOT CreateButton:
        /// that hangs a LocalizedText on the label, which refills it from a fixed key and would wipe
        /// out a caption carrying a number in it (the cancel button's refund).
        /// </summary>
        private static Button CreateDynamicButton(Transform parent, Sprite sprite, string name, Vector2 anchoredPos, Vector2 sizeDelta, out Text label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.26f, 0.29f, 0.24f, 0.95f);

            label = CreateText(go.transform, "Label", string.Empty, 24, Vector2.zero, sizeDelta - new Vector2(20f, 20f));
            return go.GetComponent<Button>();
        }
    }
}
