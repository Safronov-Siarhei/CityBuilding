using System.Collections.Generic;
using System.IO;
using CityBuilder.Core;
using CityBuilder.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CityBuilder.EditorTools
{
    public static partial class SetupProject
    {
        /// <summary>
        /// «Обучение» over the paused sandbox (the user, 2026-11-17): the plague/fire banners' build from
        /// Tools/ui/tutorial_offer_concept.py's pieces (Window, Figures, Band, PlayButton) and layout.json;
        /// words are the sheet's (#tutorial_offer_*), the reward's number the code's, the crystal the game's icon.
        /// </summary>
        private static void BuildTutorialOfferBanner(Transform uiRoot)
        {
            var folder = UiTextureImport.TutorialOfferFolder;
            var layoutPath = $"{folder}/layout.json";
            if (!File.Exists(layoutPath))
            {
                Debug.LogWarning("BuildTutorialOfferBanner: no layout.json -- run Tools/ui/tutorial_offer_concept.py.");
                return;
            }
            var layout = JsonUtility.FromJson<RateUsLayout>(File.ReadAllText(layoutPath));
            foreach (var piece in layout.pieces)
            {
                var path = $"{folder}/{piece.name}.png";
                if (AssetImporter.GetAtPath(path) is TextureImporter ti && ti.textureType != TextureImporterType.Sprite)
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            var refW = layout.reference[0];
            var refH = layout.reference[1];

            var holder = new GameObject("TutorialOfferBanner", typeof(RectTransform));
            holder.transform.SetParent(uiRoot, false);
            StretchFull(holder.GetComponent<RectTransform>());
            var root = new GameObject("Window", typeof(RectTransform));
            root.transform.SetParent(holder.transform, false);
            StretchFull(root.GetComponent<RectTransform>());
            var backdrop = CreateImage(root.transform, "Backdrop", new Color(0.02f, 0.03f, 0.06f, 0.8f));
            StretchBleed(backdrop.GetComponent<RectTransform>());
            var window = new GameObject("Frame", typeof(RectTransform)).GetComponent<RectTransform>();
            window.SetParent(root.transform, false);
            window.anchorMin = window.anchorMax = window.pivot = new Vector2(0.5f, 0.5f);
            window.sizeDelta = new Vector2(refW, refH);
            window.localScale = Vector3.one * BannerScale;

            void Place(RectTransform rect, float[] r)
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2((r[0] + r[2]) / 2f - refW / 2f, refH / 2f - (r[1] + r[3]) / 2f);
                rect.sizeDelta = new Vector2(r[2] - r[0], r[3] - r[1]);
            }

            float[] TextRect(string name) => System.Array.Find(layout.texts, t => t.name == name).rect;

            var images = new Dictionary<string, Image>();
            foreach (var piece in layout.pieces)
            {
                var image = CreateImage(window, piece.name, Color.white);
                image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{piece.name}.png");
                image.raycastTarget = piece.name == "Window" || piece.name.EndsWith("Button");
                Place(image.rectTransform, piece.rect);
                images[piece.name] = image;
            }

            var title = Words(window, "Title", "#tutorial_offer_title", 78, UiTheme.Heavy, new Color(1f, 0.94f, 0.78f));
            Place(title.rectTransform, TextRect("Title"));
            Rim(title, UiTheme.Rgb(0x2c1250), 4f);
            var body = Words(window, "Text", "#tutorial_offer_text", 38, UiTheme.Regular, UiTheme.Rgb(0x3c3418));
            Place(body.rectTransform, TextRect("Body"));
            body.verticalOverflow = VerticalWrapMode.Overflow;

            // «Награда: 25» right-aligned up to the crystal (the number is the code's: tutorial_reward_crystals)
            var reward = Words(window, "Reward", "#tutorial_offer_reward", 40, UiTheme.Bold, UiTheme.Rgb(0x78501e));
            Place(reward.rectTransform, TextRect("Reward"));
            reward.alignment = TextAnchor.MiddleRight;
            Object.DestroyImmediate(reward.GetComponent<LocalizedText>());   // formatted at Show
            var gem = CreateImage(window, "Crystal", Color.white);
            gem.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiTextureImport.IconsFolder}/Res_UpgradeCrystal.png");
            gem.preserveAspect = true;
            gem.raycastTarget = false;
            Place(gem.rectTransform, TextRect("RewardIcon"));

            var playImage = images["PlayButton"];
            var play = playImage.gameObject.AddComponent<Button>();
            play.targetGraphic = playImage;
            var playLabel = Words(playImage.transform, "PlayLabel", "#tutorial_offer_play", 44, UiTheme.Heavy, Color.white);
            Place(playLabel.rectTransform, TextRect("PlayLabel"));
            playLabel.rectTransform.anchoredPosition -= playImage.rectTransform.anchoredPosition;
            Rim(playLabel, UiTheme.Rgb(0x1c5412), 4f);
            playLabel.resizeTextForBestFit = true;
            playLabel.resizeTextMinSize = 24;
            playLabel.resizeTextMaxSize = 44;

            var laterText = Words(window, "Later", "#tutorial_offer_later", 44, UiTheme.Bold, UiTheme.Rgb(0x786a3a));
            Place(laterText.rectTransform, TextRect("Later"));
            laterText.raycastTarget = true;
            var later = laterText.gameObject.AddComponent<Button>();
            later.targetGraphic = laterText;

            var banner = holder.AddComponent<TutorialOfferBanner>();
            var so = new SerializedObject(banner);
            so.FindProperty("root").objectReferenceValue = root;
            so.FindProperty("rewardText").objectReferenceValue = reward;
            so.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(play.onClick, banner.Play);
            UnityEventTools.AddPersistentListener(later.onClick, banner.Later);
            root.SetActive(false);
        }
    }
}
