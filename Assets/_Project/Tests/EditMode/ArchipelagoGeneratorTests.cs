using System.IO;
using CityBuilder.Maps.Archipelago;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The archipelago's rules (the user, 2026-10-29): a seed is the same map every time; 12-20
    /// islands; the start, the orcs' main base and the bot each have their zone on land; the orcs far
    /// from the start; every island has a beach; nothing reaches into the storm. A few seeds are drawn
    /// to RunLogs for the eye.
    /// </summary>
    public class ArchipelagoGeneratorTests
    {
        private static readonly int[] Seeds = { 1, 2, 3, 42, 777, 2026 };

        [Test]
        public void ASeed_IsTheSameMapEveryTime()
        {
            var a = ArchipelagoGenerator.Generate(42);
            var b = ArchipelagoGenerator.Generate(42);
            Assert.AreEqual(a.Islands.Count, b.Islands.Count);
            Assert.AreEqual(a.StartCell, b.StartCell);
            Assert.AreEqual(a.OrcBaseCell, b.OrcBaseCell);
            CollectionAssert.AreEqual(a.Land, b.Land, "The same seed drew a different coast.");
            CollectionAssert.AreEqual(a.Forest, b.Forest, "The same seed grew a different forest.");
        }

        /// <summary>The code a player shares is the map's id back again; a bad or old-version code is refused, not turned into some other map.</summary>
        /// <summary>
        /// The sandbox board's ink chart lays a code out from the cells-only library (no meshes in
        /// the menu): it must be the very map the game makes from that code (2026-11-10). Seed 42's
        /// chart goes to RunLogs for the eye, with the time it took.
        /// </summary>
        [Test]
        public void TheMenuChart_IsTheMapTheGameMakes()
        {
            Assert.IsNotNull(ArchipelagoPreviewLibrary.Load(), "no Resources/ArchipelagoPreview -- run setup");
            foreach (var seed in Seeds)
            {
                var game = ArchipelagoGenerator.Generate(seed);
                var chart = ArchipelagoGenerator.Generate(seed, preview: true);
                Assert.AreEqual(game.StartCell, chart.StartCell, $"seed {seed}: start");
                Assert.AreEqual(game.OrcBaseCell, chart.OrcBaseCell, $"seed {seed}: orcs");
                Assert.AreEqual(game.BotCell, chart.BotCell, $"seed {seed}: bot");
                CollectionAssert.AreEqual(game.Land, chart.Land, $"seed {seed}: land");
                CollectionAssert.AreEqual(game.Forest, chart.Forest, $"seed {seed}: forest");
            }
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var map = ArchipelagoGenerator.Generate(42, preview: true);
            var made = watch.ElapsedMilliseconds;
            var texture = CityBuilder.UI.InkMapPainter.NewTexture(map.Size);
            CityBuilder.UI.InkMapPainter.Paint(map, texture);
            Debug.Log($"INKMAP generate {made} ms, paint {watch.ElapsedMilliseconds - made} ms");
            Directory.CreateDirectory("RunLogs");
            File.WriteAllBytes("RunLogs/ink_map_42.png", texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void AMapCode_IsTheMapBackAgain()
        {
            var id = ArchipelagoMapId.For(482915);
            var code = ArchipelagoMapId.Code(id);
            Assert.AreEqual($"{ArchipelagoGenerator.Version}-482915", code);
            Assert.IsTrue(ArchipelagoMapId.TryParseCode($"  {code} ", out var back, out _));
            Assert.AreEqual(id, back);
            Assert.IsFalse(ArchipelagoMapId.TryParseCode("hello", out _, out var old));
            Assert.IsFalse(old);
            Assert.IsFalse(ArchipelagoMapId.TryParseCode($"{ArchipelagoGenerator.Version + 7}-482915", out _, out old));
            Assert.IsTrue(old, "A code from another version is not told apart from a bad one.");
        }

        /// <summary>
        /// At least four library variants of every kind (the user, 2026-11-03); a new map takes the
        /// new ones, while a version-2 map (an old save, an old code) is still drawn from variants 1-2
        /// only -- the islands it was saved on.
        /// </summary>
        [Test]
        public void EveryKind_HasFourVariants_AndAnOldMapKeepsItsIslands()
        {
            var library = ArchipelagoLibrary.Load();
            Assert.IsNotNull(library, "No island library.");
            foreach (ArchipelagoGenerator.Kind kind in System.Enum.GetValues(typeof(ArchipelagoGenerator.Kind)))
            {
                if (kind == ArchipelagoGenerator.Kind.Tutorial) continue;      // one frame, the tutorial's own map
                var count = 0;
                foreach (var asset in library.Islands)
                {
                    if (asset == null || asset.Kind != kind) continue;
                    count++;
                    // islands.py found no room for its zone: the island would carry no base / no castle
                    if (kind == ArchipelagoGenerator.Kind.Orc) Assert.IsTrue(asset.HasMainBase, $"{asset.IslandName} has no main base spot.");
                    if (kind == ArchipelagoGenerator.Kind.Bot) Assert.IsTrue(asset.HasBot, $"{asset.IslandName} has no spot for the bot's castle.");
                }
                Assert.GreaterOrEqual(count, 4, $"Only {count} library variants of the {kind} island.");
            }

            Assert.IsTrue(ArchipelagoMapId.TryParseCode("2-482915", out var oldId, out _), "A version-2 code is refused though it can still be drawn.");
            Assert.AreEqual("Archipelago-v2-482915", oldId);

            var newVariantSeen = false;
            foreach (var seed in Seeds)
            {
                foreach (var island in ArchipelagoGenerator.Generate(seed, 2).Islands)
                {
                    if (island.Asset != null) Assert.LessOrEqual(island.Asset.Variant, 2, $"Seed {seed}: a version-2 map took {island.Asset.IslandName}.");
                }
                foreach (var island in ArchipelagoGenerator.Generate(seed).Islands)
                {
                    if (island.Asset != null && island.Asset.Variant > 2) newVariantSeen = true;
                }
            }
            Assert.IsTrue(newVariantSeen, "No new map took a variant 3 or 4.");

            // the orcs' main base is laid out unturned: on a turned orc island it did not fit
            for (var seed = 1; seed <= 40; seed++)
            {
                foreach (var island in ArchipelagoGenerator.Generate(seed).Islands)
                {
                    if (island.Kind == ArchipelagoGenerator.Kind.Orc) Assert.AreEqual(0, island.Turns, $"Seed {seed}: the orcs' island is turned.");
                }
            }
        }

        [Test]
        public void EverySeed_KeepsTheRules()
        {
            foreach (var seed in Seeds)
            {
                var map = ArchipelagoGenerator.Generate(seed);
                var n = map.Size;
                Assert.That(map.Islands.Count, Is.InRange(12, 20), $"Seed {seed}: {map.Islands.Count} islands.");

                foreach (var (name, cell) in new[] { ("start", map.StartCell), ("orc base", map.OrcBaseCell), ("bot", map.BotCell) })
                {
                    var i = cell.y * n + cell.x;
                    Assert.IsTrue(map.Land[i] && !map.IsRelief(i), $"Seed {seed}: the {name} zone at {cell} is not on the plain.");
                }
                Assert.GreaterOrEqual(Vector2Int.Distance(map.StartCell, map.OrcBaseCell), 0.4f * n, $"Seed {seed}: the orcs are next door.");

                for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                {
                    if (!map.Land[y * n + x]) continue;
                    var edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(n - 1 - x, n - 1 - y));
                    Assert.Greater(edge, ArchipelagoGenerator.Storm, $"Seed {seed}: land at ({x},{y}) reaches into the storm.");
                }

                foreach (var island in map.Islands)
                {
                    if (island.Asset != null)
                    {
                        Assert.Greater(island.Asset.Beaches.Length, 0, $"Seed {seed}: library island {island.Asset.IslandName} has no beach.");
                        continue;
                    }
                    var beach = false;
                    for (var i = 0; i < island.Beach.Length && !beach; i++) beach = island.Beach[i];
                    Assert.IsTrue(beach, $"Seed {seed}: a {island.Kind} island has no beach to land on.");
                }

                Draw(map, Path.Combine(Application.dataPath, "..", "RunLogs", $"archipelago-{seed}.png"));
            }
        }

        private static void Draw(ArchipelagoMap map, string path)
        {
            var n = map.Size;
            var texture = new Texture2D(n, n, TextureFormat.RGB24, false);
            var pixels = new Color32[n * n];
            for (var i = 0; i < pixels.Length; i++)
            {
                Color32 c = new Color32(47, 111, 176, 255);
                if (map.Land[i])
                {
                    var h = map.Height[i];
                    c = new Color32(124, 192, 79, 255);
                    if (map.Forest[i]) c = new Color32(62, 140, 63, 255);
                    if (map.Stony[i]) c = new Color32(173, 162, 133, 255);
                    if (h >= ArchipelagoGenerator.ReliefFrom) c = new Color32(167, 180, 106, 255);
                    if (h >= 0.62f) c = new Color32(140, 135, 125, 255);
                    if (map.Beach[i]) c = new Color32(232, 211, 138, 255);
                    if (map.Cliff[i]) c = new Color32(90, 78, 64, 255);
                }
                pixels[i] = c;
            }
            foreach (var (cell, colour) in new[] { (map.StartCell, new Color32(244, 207, 90, 255)), (map.OrcBaseCell, new Color32(178, 50, 30, 255)), (map.BotCell, new Color32(44, 79, 168, 255)) })
            {
                for (var dy = -4; dy <= 4; dy++)
                for (var dx = -4; dx <= 4; dx++)
                {
                    int x = cell.x + dx, y = cell.y + dy;
                    if (x >= 0 && y >= 0 && x < n && y < n) pixels[y * n + x] = colour;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
    }
}
