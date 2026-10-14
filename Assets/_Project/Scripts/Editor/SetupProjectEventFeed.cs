using System.IO;
using CityBuilder.CameraControl;
using CityBuilder.Core;
using CityBuilder.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The event feed (see EventFeedController): a column of round icons down the left edge, where
    /// the text log used to hang. Every icon it will ever show is built here, hidden, so the game
    /// never instantiates one.
    /// </summary>
    public static partial class SetupProject
    {
        private const int EventFeedSlots = 4;
        private const float EventIconSize = 92f;
        private const float EventIconSpacing = 104f;

        /// <summary>
        /// Below the tax control (y 318) and above the round minimap (whose top is near y -300), at
        /// the left edge -- the strip the old log panel occupied, so nothing else had to move. Four
        /// rows end at about y -150.
        /// </summary>
        private static void BuildEventFeed(Transform canvasParent, RTSCameraController cameraRig)
        {
            var feed = new GameObject("EventFeed", typeof(RectTransform));
            feed.transform.SetParent(canvasParent, false);
            var feedRect = (RectTransform)feed.transform;
            // The left edge under the menu button, the zone the user's layout keeps for it.
            feedRect.anchorMin = feedRect.anchorMax = new Vector2(0f, 1f);
            feedRect.pivot = new Vector2(0.5f, 1f);
            feedRect.anchoredPosition = new Vector2(HudMargin + EventIconSize * 0.5f + 8f, -200f);
            feedRect.sizeDelta = new Vector2(EventIconSize, EventIconSize + EventIconSpacing * (EventFeedSlots - 1));

            // A canvas of its own: the rings drain every frame, and a change inside a nested canvas
            // rebuilds that canvas's handful of images rather than the whole HUD's.
            feed.AddComponent<Canvas>();
            feed.AddComponent<GraphicRaycaster>();

            var views = new EventIconView[EventFeedSlots];
            for (var i = 0; i < EventFeedSlots; i++)
            {
                views[i] = BuildEventIcon(feed.transform, i);
            }

            var controller = feed.AddComponent<EventFeedController>();
            var so = new SerializedObject(controller);
            var viewsProperty = so.FindProperty("views");
            viewsProperty.arraySize = views.Length;
            for (var i = 0; i < views.Length; i++) viewsProperty.GetArrayElementAtIndex(i).objectReferenceValue = views[i];
            so.FindProperty("library").objectReferenceValue = CreateEventIconLibrary();
            so.FindProperty("cameraRig").objectReferenceValue = cameraRig;
            so.FindProperty("spacing").floatValue = EventIconSpacing;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static EventIconView BuildEventIcon(Transform feed, int index)
        {
            var root = new GameObject($"EventIcon{index}", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(feed, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(EventIconSize, EventIconSize);
            rect.anchoredPosition = new Vector2(0f, -EventIconSize * 0.5f - index * EventIconSpacing);

            // The disc is the one image that takes the tap; the view on the root above it handles it.
            var disc = EventIconPart(root.transform, "Disc", SkinSprite("event_disc"), Vector2.zero, EventIconSize);
            disc.raycastTarget = true;

            var rim = EventIconPart(root.transform, "Rim", SkinSprite("event_rim"), Vector2.zero, EventIconSize);
            rim.type = Image.Type.Filled;
            rim.fillMethod = Image.FillMethod.Radial360;
            rim.fillOrigin = (int)Image.Origin360.Top;
            rim.fillClockwise = true;

            var picture = EventIconPart(root.transform, "Picture", null, Vector2.zero, 62f);
            var bigGlyph = EventIconPart(root.transform, "Glyph", null, Vector2.zero, 48f);

            var badge = EventIconPart(root.transform, "Badge", SkinSprite("event_badge"), new Vector2(31f, -31f), 40f);
            var badgeGlyph = EventIconPart(badge.transform, "BadgeGlyph", null, Vector2.zero, 24f);

            var counter = CreateSkinImage(root.transform, "Counter", "pill_gold", new Vector2(32f, 32f), new Vector2(42f, 36f));
            counter.pixelsPerUnitMultiplier = 2f;
            counter.raycastTarget = false;
            var counterText = CreateText(counter.transform, "Count", "2", 22, new Vector2(0f, 2f), new Vector2(42f, 36f), UiTheme.Ink);
            counterText.font = UiTheme.Bold;
            counterText.raycastTarget = false;
            counterText.horizontalOverflow = HorizontalWrapMode.Overflow;
            counterText.verticalOverflow = VerticalWrapMode.Overflow;

            var caption = BuildEventCaption(root.transform, out var captionText);

            var view = root.AddComponent<EventIconView>();
            var so = new SerializedObject(view);
            so.FindProperty("group").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("rim").objectReferenceValue = rim;
            so.FindProperty("picture").objectReferenceValue = picture;
            so.FindProperty("bigGlyph").objectReferenceValue = bigGlyph;
            so.FindProperty("badge").objectReferenceValue = badge;
            so.FindProperty("badgeGlyph").objectReferenceValue = badgeGlyph;
            so.FindProperty("counter").objectReferenceValue = counter.gameObject;
            so.FindProperty("counterText").objectReferenceValue = counterText;
            so.FindProperty("caption").objectReferenceValue = caption;
            so.FindProperty("captionText").objectReferenceValue = captionText;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return view;
        }

        private static Image EventIconPart(Transform parent, string name, Sprite sprite, Vector2 position, float size)
        {
            var image = CreateImage(parent, name, Color.white);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);
            return image;
        }

        /// <summary>
        /// The event's one line, on a wooden pill to the icon's right, shown only while the icon is
        /// held or hovered. It sizes itself to the line, so it never wraps.
        /// </summary>
        private static GameObject BuildEventCaption(Transform icon, out Text text)
        {
            var pill = CreateSkinImage(icon, "Caption", "pill_wood", Vector2.zero, new Vector2(200f, 52f));
            pill.pixelsPerUnitMultiplier = 1.6f;
            pill.raycastTarget = false;
            var rect = pill.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(10f, 0f);

            var layout = pill.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 8, 12);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fitter = pill.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            text = CreateText(pill.transform, "Text", string.Empty, 22, Vector2.zero, new Vector2(160f, 30f), UiTheme.OnWood);
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            pill.gameObject.SetActive(false);
            return pill.gameObject;
        }

        /// <summary>
        /// Every icon rendered from the models, by file name -- so an event can name a building by
        /// its id or a resource by "Res_" + its type and get the same picture the hotbar and the
        /// resource bar show -- and one glyph per EventGlyph, by the value's name.
        /// </summary>
        private static EventIconLibrary CreateEventIconLibrary()
        {
            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { UiTextureImport.IconsFolder });
            var names = new string[guids.Length];
            var sprites = new Sprite[guids.Length];
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                names[i] = Path.GetFileNameWithoutExtension(path);
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            var glyphNames = System.Enum.GetNames(typeof(EventGlyph));
            var glyphs = new Sprite[glyphNames.Length];
            for (var i = 1; i < glyphNames.Length; i++)
            {
                glyphs[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiTextureImport.GlyphsFolder}/{glyphNames[i]}.png");
                if (glyphs[i] == null) Debug.LogWarning($"SetupProject: no glyph for EventGlyph.{glyphNames[i]} in {UiTextureImport.GlyphsFolder} -- run Tools/ui/make_event_glyphs.py.");
            }

            var library = ScriptableObject.CreateInstance<EventIconLibrary>();
            library.EditorInitialize(names, sprites, glyphs);

            Directory.CreateDirectory(ResourceScriptableObjectsFolder);
            var assetPath = $"{ResourceScriptableObjectsFolder}/EventIconLibrary.asset";
            DeleteIfExists(assetPath);
            AssetDatabase.CreateAsset(library, assetPath);
            return library;
        }
    }
}
