using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The sandbox's screen in the main menu (the user's sketch, 2026-10-25; since 2026-11-10 the
    /// user's pick «Знамя и доска»: the sandbox banner on the left, a board with theme tabs, the map,
    /// the map code, Saves and Start on the right): a card per map
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
        // the archipelago's code (the user, 2026-10-28): empty starts a random map, a friend's code their map
        [SerializeField] private InputField mapCode;
        [SerializeField] private Text mapCodeError;
        /// <summary>The round arrows by the code (2026-11-10): writes a fresh random code in, so the map's code is seen before the game.</summary>
        [SerializeField] private Button randomCodeButton;

        [Header("A card per map, in MapSelector.PlayerMapIds order")]
        [SerializeField] private string[] mapIds = new string[0];
        [SerializeField] private Button[] cards = new Button[0];
        [SerializeField] private Image[] pictures = new Image[0];
        [SerializeField] private GameObject[] pickedFrames = new GameObject[0];

        [Header("Pictures: map id + '_' + theme id (MenuArtBake); a map with none in a theme shows its plain one")]
        [SerializeField] private string[] pictureKeys = new string[0];
        [SerializeField] private Sprite[] pictureSprites = new Sprite[0];

        private string _picked;
        // the chart: one texture reused, the map id it shows, and when a typed code is drawn
        private const float TypingPause = 0.4f;
        private Texture2D _chart;
        private Sprite _chartSprite;
        private string _charted;
        private float _redrawAt = -1f;

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
            if (randomCodeButton != null) randomCodeButton.onClick.AddListener(RandomCode);
            if (mapCode != null) mapCode.onValueChanged.AddListener(_ => { _redrawAt = Time.unscaledTime + TypingPause; enabled = true; });
            enabled = false;
        }

        private void Update()
        {
            if (_redrawAt < 0f || Time.unscaledTime < _redrawAt) return;
            _redrawAt = -1f;
            enabled = false;
            Show();
        }

        private void OnDestroy()
        {
            if (_chartSprite != null) Destroy(_chartSprite);
            if (_chart != null) Destroy(_chart);
        }

        /// <summary>Called as the screen comes in: the newest map picked unless the player picked another.</summary>
        public void Open()
        {
            if (string.IsNullOrEmpty(_picked) && mapIds.Length > 0) _picked = mapIds[0];
            // a code from the start, so the chart shows the map «Начать» will make
            if (mapCode != null && string.IsNullOrWhiteSpace(mapCode.text)) RandomCode();
            Show();
        }

        public void Pick(string mapId)
        {
            _picked = mapId;
            Show();
        }

        public void RandomCode()
        {
            if (mapCode == null) return;
            mapCode.text = Maps.Archipelago.ArchipelagoMapId.Code(Maps.Archipelago.ArchipelagoMapId.NewRandom());
            if (mapCodeError != null) mapCodeError.gameObject.SetActive(false);
            _redrawAt = -1f;
            enabled = false;
            Show();
        }

        public void StartGame()
        {
            var map = MeshMapCatalog.GetById(_picked);
            if (map == null || !Shown(_picked)) return;
            MapSelector.MarkChosen(map);
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameIsInvasion = false;
            // a generated map starts on a fresh seed of its own, or on the code typed in
            var id = _picked;
            if (map.IsGenerated)
            {
                var typed = mapCode != null ? mapCode.text : string.Empty;
                if (string.IsNullOrWhiteSpace(typed)) id = ArchipelagoMapIdFor(map);
                else if (!Maps.Archipelago.ArchipelagoMapId.TryParseCode(typed, out id, out var otherVersion))
                {
                    if (mapCodeError != null)
                    {
                        mapCodeError.gameObject.SetActive(true);
                        mapCodeError.text = Localization.Get(otherVersion ? "#sandbox_code_old" : "#sandbox_code_bad");
                    }
                    return;
                }
            }
            if (mapCodeError != null) mapCodeError.gameObject.SetActive(false);
            GameSessionIntent.NewGameMapId = id;
            GameSessionIntent.NewGameSeason = MapThemes.SeasonOf(Theme);
            LoadingScreen.IntoGame(gameplaySceneName);
        }

        private static string ArchipelagoMapIdFor(MeshMapDefinition map) => Maps.Archipelago.ArchipelagoMapId.NewRandom();

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
                    var sprite = ChartOf(mapIds[i]) ?? PictureOf(mapIds[i], theme);
                    if (sprite != null) pictures[i].sprite = sprite;
                }
                if (i < pickedFrames.Length && pickedFrames[i] != null) pickedFrames[i].SetActive(mapIds[i] == _picked);
            }
            if (noMapsLabel != null) noMapsLabel.SetActive(!any);
            if (startButton != null) startButton.interactable = any && Shown(_picked);
        }

        /// <summary>
        /// A generated map's ink chart for the code in the box (InkMapPainter), redrawn only when the
        /// code changes; plain, no markers (the user, 2026-11-10); null (the plain picture then) for a map that is not generated or
        /// a code that does not parse.
        /// </summary>
        private Sprite ChartOf(string mapId)
        {
            var def = MeshMapCatalog.GetById(mapId);
            if (def == null || !def.IsGenerated) return null;
            var typed = mapCode != null ? mapCode.text : string.Empty;
            if (!Maps.Archipelago.ArchipelagoMapId.TryParseCode(typed, out var id, out _)
                || !Maps.Archipelago.ArchipelagoMapId.TryParse(id, out var seed, out var version))
                return null;
            if (id != _charted || _chartSprite == null)
            {
                var map = Maps.Archipelago.ArchipelagoGenerator.Generate(seed, version, preview: true);
                if (_chart == null)
                {
                    _chart = InkMapPainter.NewTexture(map.Size);
                    _chartSprite = Sprite.Create(_chart, new Rect(0f, 0f, map.Size, map.Size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                }
                InkMapPainter.Paint(map, _chart);
                _charted = id;
            }
            return _chartSprite;
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
