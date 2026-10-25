using System.Collections;
using System.IO;
using System.Linq;
using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Invasion;
using CityBuilder.Maps;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// Takes the main menu's pictures in the game itself (2026-10-20): a mode card shows a scene of
    /// that mode in our own models, our own light and ground, rather than a drawing of it -- the
    /// detail the user found missing. Writes Assets/_Project/UI/MenuArt/Card_*.png and Map_*.png,
    /// which SetupProject puts on the cards and the map list.
    ///
    /// Not part of a normal run ([Explicit]): it rewrites assets. Run it with graphics when the
    /// models or the maps change:
    ///     run.ps1 -Steps playmode -Filter MenuArtBake -Graphics
    /// then run setup so the new pictures are imported.
    /// </summary>
    [Explicit("Rewrites the menu's pictures; run on its own with graphics.")]
    public class MenuArtBake
    {
        private const string GameSceneName = "CityBuilder";
        private const int CardWidth = 800;
        private const int CardHeight = 900;

        private static string ArtFolder => Path.Combine(Application.dataPath, "_Project", "UI", "MenuArt");

        [UnityTest]
        public IEnumerator Bake_TheModeCards()
        {
            Assume.That(PlaytestCapture.Available, "No graphics device: run with -Graphics.");
            yield return Load("Map3");

            // The Town Hall opens the portals, which raise the orcs' main base.
            var castle = PlaytestWorld.Building("Castle");
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            Assert.IsNotNull(castle, placer == null ? "No BuildingPlacer in the scene." :
                "No Castle; mandatory=" + (placer.MandatoryFirstBuilding != null ? placer.MandatoryFirstBuilding.buildingName : "null") +
                " available=" + string.Join(",", placer.AvailableBuildings.Where(b => b != null).Select(b => b.buildingName)));
            var castleCell = PlaytestWorld.FindFreeAreaNearTheMiddle(castle.footprintSize + new Vector2Int(14, 14)) + new Vector2Int(7, 7);
            var hall = PlaytestWorld.Place(castle, castleCell);
            for (var frame = 0; frame < 240 && !OrcRaidManager.Instance.PortalsPlaced; frame++) yield return null;
            if (OrcRaidManager.Instance != null) OrcRaidManager.Instance.RaidsSuspended = true;
            // The pictures show the land, not the fog over what is not explored yet.
            if (FogOfWarManager.Instance != null) FogOfWarManager.Instance.RevealPermanent(castleCell, 400);

            // A village round it: houses, a windmill and fields, a well, a square, trees.
            var village = new[]
            {
                "Cottage", "Cottage", "Hovel", "Cottage", "Manor", "Windmill", "Farm", "Well", "TownSquare",
                "Baker", "Hovel", "Cottage", "Orchard", "DecTree", "DecTree", "DecTree", "Flag", "Flag", "DecGarden", "Tower",
            };
            var rng = new System.Random(7);
            foreach (var id in village)
            {
                var data = PlaytestWorld.Building(id);
                if (data == null) continue;
                PlaceNear(data, castleCell + castle.footprintSize / 2, rng);
            }

            // Nothing in hand: the placer would otherwise still be offering the Town Hall, its ink
            // drawing in the middle of the picture.
            placer.ClearSelection();
            foreach (var preview in Object.FindObjectsByType<PlacementPreview>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                preview.gameObject.SetActive(false);
            }

            // Let the town come alive -- people out on the streets, smoke from the chimneys.
            Time.timeScale = 4f;
            yield return new WaitForSeconds(6f);
            Time.timeScale = 1f;

            var centre = hall.transform.position;
            yield return PlaytestCapture.ShootTo(Path.Combine(ArtFolder, "Card_Sandbox.png"), CardWidth, CardHeight,
                centre + new Vector3(2f, 0f, 2f), 21f, 40f, 205f, 40f);
            yield return PlaytestCapture.ShootTo(Path.Combine(ArtFolder, "Card_Campaign.png"), CardWidth, CardHeight,
                centre + new Vector3(0f, 1.5f, 0f), 17f, 24f, 160f, 40f);

            // The orcs' main base at dusk: a low warm sun for the one card about war.
            var portal = OrcPortal.All.FirstOrDefault(p => p.IsMain);
            Assert.IsNotNull(portal, "No main base opened on the map.");
            var camp = OrcCamp.Of(portal);
            yield return new WaitForSeconds(0.5f);
            var sun = RenderSettings.sun != null ? RenderSettings.sun : Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
            if (sun != null)
            {
                sun.color = new Color(1f, 0.62f, 0.42f);
                sun.intensity *= 0.9f;
                sun.transform.rotation = Quaternion.Euler(22f, sun.transform.eulerAngles.y + 40f, 0f);
            }
            var yaw = camp.Quarters * 90f + 20f;
            yield return PlaytestCapture.ShootTo(Path.Combine(ArtFolder, "Card_Invasion.png"), CardWidth, CardHeight,
                camp.LocalToWorld(new Vector2(0f, 3f)), 24f, 40f, yaw, 42f);
        }

        [UnityTest]
        public IEnumerator Bake_TheMapPreviews()
        {
            Assume.That(PlaytestCapture.Available, "No graphics device: run with -Graphics.");
            foreach (var map in MapSelector.Choosable())
            {
                yield return Load(map.MapId);
                yield return new WaitForSeconds(0.3f);
                yield return PlaytestCapture.ShootTo(Path.Combine(ArtFolder, $"Map_{map.MapId}.png"), 640, 400,
                    new Vector3(0f, 0f, 2f), 178f, 62f, 0f, 50f);
            }
        }

        /// <summary>
        /// The sandbox's map cards in each season (the user, 2026-10-25: the theme buttons pick the
        /// season, and the cards show the maps in it): Map_&lt;id&gt;_&lt;theme&gt;.png.
        /// </summary>
        [UnityTest]
        public IEnumerator Bake_TheMapsInEverySeason()
        {
            Assume.That(PlaytestCapture.Available, "No graphics device: run with -Graphics.");
            try
            {
                foreach (var map in MapSelector.Choosable())
                {
                    foreach (var theme in MapThemes.All)
                    {
                        if (!MapThemes.InSandbox(theme)) continue;
                        SeasonStyle.Override = MapThemes.SeasonOf(theme);
                        yield return Load(map.MapId);
                        yield return new WaitForSeconds(0.3f);
                        yield return PlaytestCapture.ShootTo(Path.Combine(ArtFolder, $"Map_{map.MapId}_{MapThemes.Id(theme)}.png"), 640, 360,
                            new Vector3(0f, 0f, 2f), 178f, 62f, 0f, 50f);
                    }
                }
            }
            finally
            {
                SeasonStyle.Override = null;
            }
        }

        /// <summary>
        /// The invasion screen's picture for each theme (the user, 2026-10-25: a settlement on the
        /// theme's map, seen a little from above and from the side): the run's own start with a few
        /// more buildings round the castle, Inv_&lt;theme&gt;.png, as wide as the screen's picture.
        /// </summary>
        [UnityTest]
        public IEnumerator Bake_TheInvasionThemes()
        {
            Assume.That(PlaytestCapture.Available, "No graphics device: run with -Graphics.");
            var keep = GameSettings.InvasionTheme;
            try
            {
                foreach (var theme in MapThemes.All)
                {
                    GameSettings.InvasionTheme = theme;
                    SeasonStyle.Override = MapThemes.SeasonOf(theme);
                    var mapId = Invasion.InvasionMode.RunMapId;
                    InvasionRecords.FilePathOverride = Path.Combine(Application.temporaryCachePath, "invasion_records_bake.json");
                    GameSessionIntent.NewGameIsInvasion = true;
                    yield return Load(mapId);
                    for (var frame = 0; frame < 300 && (Invasion.InvasionMode.Instance == null || !Invasion.InvasionMode.Instance.RunStarted); frame++) yield return null;
                    var castle = Invasion.InvasionMode.Instance.Castle;
                    var at = GridManager.Instance.WorldToCell(castle.transform.position);
                    var rng = new System.Random(11);
                    foreach (var id in new[] { "Tower", "Tower", "Sawmill", "Quarry", "Barracks", "HealerHouse", "Tower" })
                    {
                        var data = PlaytestWorld.Building(id);
                        if (data != null) PlaceNear(data, at, rng);
                    }
                    if (FogOfWarManager.Instance != null) FogOfWarManager.Instance.RevealPermanent(at, 80);
                    yield return new WaitForSeconds(FogOfWarManager.RevealSeconds + 1f);
                    yield return PlaytestCapture.ShootTo(Path.Combine(ArtFolder, $"Inv_{MapThemes.Id(theme)}.png"), 1512, 340,
                        castle.transform.position + new Vector3(0f, 1f, 0f), 17f, 26f, 205f, 24f);
                }
            }
            finally
            {
                GameSettings.InvasionTheme = keep;
                SeasonStyle.Override = null;
                GameSessionIntent.NewGameIsInvasion = false;
                InvasionRecords.FilePathOverride = null;
            }
        }

        private static IEnumerator Load(string mapId)
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = mapId;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            // The old scene's map and nodes are still registered for a frame or two after the load
            // is asked for: let it go before asking whether the new one is ready.
            yield return null;
            yield return null;
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(mapId);
            yield return new WaitForSeconds(1.5f);
            ModalGate.SetBlocked(false);
        }

        /// <summary>The first free spot in widening rings round `centre` where the building fits, turned at random.</summary>
        private static void PlaceNear(BuildingData data, Vector2Int centre, System.Random rng)
        {
            var grid = GridManager.Instance;
            var turns = rng.Next(4);
            var size = turns % 2 == 0 ? data.footprintSize : new Vector2Int(data.footprintSize.y, data.footprintSize.x);
            for (var ring = 3; ring < 18; ring++)
            {
                for (var tries = 0; tries < ring * 6; tries++)
                {
                    var angle = rng.NextDouble() * Mathf.PI * 2f;
                    var cell = centre + new Vector2Int(Mathf.RoundToInt(Mathf.Cos((float)angle) * ring), Mathf.RoundToInt(Mathf.Sin((float)angle) * ring)) - size / 2;
                    // a lane round every building, so the village has streets rather than a wall of roofs
                    var padded = cell - Vector2Int.one;
                    if (!grid.IsWithinBounds(padded, size + 2 * Vector2Int.one) || !grid.IsAreaFree(padded, size + 2 * Vector2Int.one)) continue;
                    if (!Dry(cell, size)) continue;
                    PlaytestWorld.Place(data, cell, turns);
                    return;
                }
            }
        }

        private static bool Dry(Vector2Int origin, Vector2Int size)
        {
            var map = MeshMapApplier.Instance;
            if (map == null) return true;
            for (var x = 0; x < size.x; x++)
            {
                for (var z = 0; z < size.y; z++)
                {
                    var cell = origin + new Vector2Int(x, z);
                    if (map.IsWaterCell(cell) || map.IsNoBuildCell(cell)) return false;
                }
            }
            return true;
        }
    }
}
