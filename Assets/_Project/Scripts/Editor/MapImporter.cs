using System.IO;
using CityBuilder.Maps;
using UnityEditor;
using UnityEngine;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// Converts reference map images (100x100 logical cells, flat-color palette: blue water,
    /// light green grass, dark green forest, gray stone — optionally framed by a dark border,
    /// which is auto-cropped) into MapDefinition assets under Resources/Maps, where MapCatalog
    /// picks them up automatically at runtime — no further wiring needed per map.
    ///
    /// Workflow: drop a PNG/JPG into Assets/_Project/MapsSource, then run
    /// "CityBuilder/Import Maps From Source Folder" (menu, or -executeMethod in batchmode).
    /// </summary>
    public static class MapImporter
    {
        private const string SourceFolder = "Assets/_Project/MapsSource";
        private const string OutputFolder = "Assets/_Project/Resources/Maps";
        private const int MapSize = 100;

        // Reference palette for nearest-color classification. Only the relative distance between
        // these four matters — source images don't need to match them exactly.
        private static readonly Color32 WaterColor = new Color32(33, 191, 235, 255);
        private static readonly Color32 GrassColor = new Color32(173, 219, 51, 255);
        private static readonly Color32 ForestColor = new Color32(51, 140, 89, 255);
        private static readonly Color32 StoneColor = new Color32(173, 173, 173, 255);

        // Border/frame pixels darker than this on every channel are excluded when finding the
        // map's actual content bounds, rather than misclassified as terrain.
        private const byte BorderThreshold = 20;

        [MenuItem("CityBuilder/Import Maps From Source Folder")]
        public static void ImportAll()
        {
            if (!Directory.Exists(SourceFolder))
            {
                Debug.LogWarning($"[MapImporter] No source folder at {SourceFolder} — nothing to import.");
                return;
            }

            Directory.CreateDirectory(OutputFolder);

            var imported = 0;
            foreach (var path in Directory.GetFiles(SourceFolder))
            {
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext != ".png" && ext != ".jpg" && ext != ".jpeg") continue;

                if (ImportOne(path.Replace('\\', '/'))) imported++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MapImporter] Imported {imported} map(s) into {OutputFolder}.");
        }

        private static bool ImportOne(string assetPath)
        {
            if (!(AssetImporter.GetAtPath(assetPath) is TextureImporter importer))
            {
                Debug.LogWarning($"[MapImporter] {assetPath} is not an image asset, skipping.");
                return false;
            }

            if (!importer.isReadable || importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.isReadable = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                Debug.LogWarning($"[MapImporter] Failed to load {assetPath}, skipping.");
                return false;
            }

            var pixels = texture.GetPixels32();
            var width = texture.width;
            var height = texture.height;

            if (!TryFindContentBounds(pixels, width, height, out var minX, out var minY, out var maxX, out var maxY))
            {
                Debug.LogWarning($"[MapImporter] {assetPath} looks entirely blank/border, skipping.");
                return false;
            }

            var cropWidth = maxX - minX + 1;
            var cropHeight = maxY - minY + 1;

            var cells = new byte[MapSize * MapSize];
            for (var y = 0; y < MapSize; y++)
            {
                // Map row 0 = the image's TOP row (matches how a human reads the source picture);
                // texture rows are bottom-up, so this flips it back.
                var srcY = Mathf.Clamp(minY + (int)((1f - (y + 0.5f) / MapSize) * cropHeight), minY, maxY);

                for (var x = 0; x < MapSize; x++)
                {
                    var srcX = Mathf.Clamp(minX + (int)((x + 0.5f) / MapSize * cropWidth), minX, maxX);
                    cells[y * MapSize + x] = (byte)Classify(pixels[srcY * width + srcX]);
                }
            }

            var mapId = Path.GetFileNameWithoutExtension(assetPath);
            var outputPath = $"{OutputFolder}/{mapId}.asset";

            var definition = AssetDatabase.LoadAssetAtPath<MapDefinition>(outputPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<MapDefinition>();
                AssetDatabase.CreateAsset(definition, outputPath);
            }

            definition.EditorInitialize(mapId, MapSize, MapSize, cells);
            EditorUtility.SetDirty(definition);
            return true;
        }

        private static bool TryFindContentBounds(Color32[] pixels, int width, int height, out int minX, out int minY, out int maxX, out int maxY)
        {
            minX = width;
            minY = height;
            maxX = -1;
            maxY = -1;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (IsBorder(pixels[y * width + x])) continue;

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            return maxX >= minX && maxY >= minY;
        }

        private static bool IsBorder(Color32 pixel)
        {
            return pixel.r <= BorderThreshold && pixel.g <= BorderThreshold && pixel.b <= BorderThreshold;
        }

        private static TerrainType Classify(Color32 pixel)
        {
            var best = TerrainType.Grass;
            var bestDistance = int.MaxValue;

            Consider(TerrainType.Water, WaterColor);
            Consider(TerrainType.Grass, GrassColor);
            Consider(TerrainType.Forest, ForestColor);
            Consider(TerrainType.Stone, StoneColor);

            return best;

            void Consider(TerrainType type, Color32 reference)
            {
                var dr = pixel.r - reference.r;
                var dg = pixel.g - reference.g;
                var db = pixel.b - reference.b;
                var distance = dr * dr + dg * dg + db * db;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = type;
                }
            }
        }
    }
}
