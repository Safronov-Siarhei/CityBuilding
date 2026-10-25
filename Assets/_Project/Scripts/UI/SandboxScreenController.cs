using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The sandbox's screen in the main menu (the user's sketch, 2026-10-25): a card per map
    /// (MapSelector.PlayerMapIds) with its picture in the theme picked below, the picked card lit;
    /// a map that does not come in the theme is not shown; «Начать» starts a new game on the picked
    /// map, dressed in the theme's season.
    /// </summary>
    public class SandboxScreenController : MonoBehaviour
    {
        [SerializeField] private string gameplaySceneName = "CityBuilder";
        [SerializeField] private ThemePicker themes;
        [SerializeField] private Button startButton;
        [SerializeField] private GameObject noMapsLabel;

        [Header("A card per map, in MapSelector.PlayerMapIds order")]
        [SerializeField] private string[] mapIds = new string[0];
        [SerializeField] private Button[] cards = new Button[0];
        [SerializeField] private Image[] pictures = new Image[0];
        [SerializeField] private GameObject[] pickedFrames = new GameObject[0];

        [Header("Pictures: map id + '_' + theme id (MenuArtBake); a map with none in a theme shows its plain one")]
        [SerializeField] private string[] pictureKeys = new string[0];
        [SerializeField] private Sprite[] pictureSprites = new Sprite[0];

        private string _picked;

        public string PickedMapId => _picked;

        private void Awake()
        {
            for (var i = 0; i < cards.Length && i < mapIds.Length; i++)
            {
                var id = mapIds[i];
                if (cards[i] != null) cards[i].onClick.AddListener(() => Pick(id));
            }
            if (themes != null) themes.Changed += _ => Show();
            if (startButton != null) startButton.onClick.AddListener(StartGame);
        }

        /// <summary>Called as the screen comes in: the newest map picked unless the player picked another.</summary>
        public void Open()
        {
            if (string.IsNullOrEmpty(_picked) && mapIds.Length > 0) _picked = mapIds[0];
            Show();
        }

        public void Pick(string mapId)
        {
            _picked = mapId;
            Show();
        }

        public void StartGame()
        {
            var map = MeshMapCatalog.GetById(_picked);
            if (map == null || !Shown(_picked)) return;
            MapSelector.MarkChosen(map);
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameIsInvasion = false;
            GameSessionIntent.NewGameMapId = _picked;
            GameSessionIntent.NewGameSeason = MapThemes.SeasonOf(Theme);
            SceneManager.LoadScene(gameplaySceneName);
        }

        private MapTheme Theme => themes != null ? themes.Current : GameSettings.SandboxTheme;

        /// <summary>Whether the map is on the screen: imported, and made in the theme (every season for now; the New Year is the Invasion's).</summary>
        private bool Shown(string mapId) => MeshMapCatalog.GetById(mapId) != null && MapThemes.InSandbox(Theme);

        private void Show()
        {
            var theme = Theme;
            var any = false;
            for (var i = 0; i < mapIds.Length; i++)
            {
                var shown = Shown(mapIds[i]);
                any |= shown;
                if (i < cards.Length && cards[i] != null) cards[i].gameObject.SetActive(shown);
                if (i < pictures.Length && pictures[i] != null)
                {
                    var sprite = PictureOf(mapIds[i], theme);
                    if (sprite != null) pictures[i].sprite = sprite;
                }
                if (i < pickedFrames.Length && pickedFrames[i] != null) pickedFrames[i].SetActive(mapIds[i] == _picked);
            }
            if (noMapsLabel != null) noMapsLabel.SetActive(!any);
            if (startButton != null) startButton.interactable = any && Shown(_picked);
        }

        private Sprite PictureOf(string mapId, MapTheme theme)
        {
            var key = $"{mapId}_{MapThemes.Id(theme)}";
            Sprite plain = null;
            for (var i = 0; i < pictureKeys.Length && i < pictureSprites.Length; i++)
            {
                if (pictureKeys[i] == key) return pictureSprites[i];
                if (pictureKeys[i] == mapId) plain = pictureSprites[i];
            }
            return plain;
        }
    }
}
