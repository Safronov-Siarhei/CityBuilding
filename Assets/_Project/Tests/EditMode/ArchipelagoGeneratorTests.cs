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
