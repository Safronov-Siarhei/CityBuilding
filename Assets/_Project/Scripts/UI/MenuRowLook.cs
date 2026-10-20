using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// A list row in the main menu's look (Resources/MenuSkins, the look picked in the settings):
    /// a row sprite and a picked-row sprite rather than one sprite tinted, and the ink the look
    /// writes on them. Read once when a list is filled.
    /// </summary>
    public sealed class MenuRowLook
    {
        public const string ResourcesFolder = "MenuSkins";

        public Sprite Normal { get; private set; }
        public Sprite Selected { get; private set; }
        public Color Ink { get; private set; }

        public static MenuRowLook Current()
        {
            var skin = GameSettings.HudSkin;
            return new MenuRowLook
            {
                Normal = UnityEngine.Resources.Load<Sprite>($"{ResourcesFolder}/{skin}/row"),
                Selected = UnityEngine.Resources.Load<Sprite>($"{ResourcesFolder}/{skin}/row_selected"),
                Ink = HudSkins.Ink(skin, HudInk.OnPanel),
            };
        }

        public void Style(Image row, bool selected)
        {
            row.sprite = selected ? Selected : Normal;
            row.type = Image.Type.Sliced;
            row.color = Color.white;
        }
    }
}
