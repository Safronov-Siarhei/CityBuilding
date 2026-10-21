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
    /// Sprites drawn in code: panel sprites and every procedural icon (categories, buildings,
    /// resources, HUD signs); a rendered PNG in UI/Icons wins where it exists.
    /// </summary>
    public static partial class SetupProject
    {
        /// <summary>The tick on a researched row. Drawn rather than typed: the built-in font has no check-mark glyph, and a missing one renders as an empty box.</summary>
        private static Sprite CreateCheckIcon()
        {
            return CreateIconSprite("Action_Check", 64, (p, s) =>
            {
                var stroke = new Color(0.95f, 0.98f, 0.95f);
                FillIconStroke(p, s, new Vector2(0.16f, 0.52f), new Vector2(0.4f, 0.26f), 0.11f, stroke);
                FillIconStroke(p, s, new Vector2(0.4f, 0.26f), new Vector2(0.86f, 0.76f), 0.11f, stroke);
            });
        }

        /// <summary>A thick line between two normalized points, as two triangles -- the icon painters only rasterize rects and triangles.</summary>
        private static void FillIconStroke(Color[] pixels, int size, Vector2 from, Vector2 to, float thickness, Color color)
        {
            var direction = (to - from).normalized;
            var offset = new Vector2(-direction.y, direction.x) * (thickness * 0.5f);

            var a = from + offset;
            var b = to + offset;
            var c = to - offset;
            var d = from - offset;
            FillIconTriangle(pixels, size, a, b, c, color);
            FillIconTriangle(pixels, size, a, c, d, color);
        }

        /// <summary>
        /// "Резной камень" (Style A): a carved-stone bevel -- a dark recessed groove around the
        /// rim, then a directional raised bevel (light catching the top/left edges, shadow on the
        /// bottom/right, the classic raised-panel look), dark rivet squares in the corners, flat
        /// mid-grey fill in the middle. Sharp 90-degree corners throughout (the game's cubic/
        /// angular style, no rounded UI). Colors are pushed to near-black/near-white specifically
        /// so the bevel still reads clearly after the heavy per-context dark .color tints used
        /// across the UI (a subtler greyscale range washed out to near-invisible once multiplied
        /// by those tints). 9-sliced (see the returned sprite's border) so it stretches cleanly to
        /// any button/panel size without smearing the bevel or rivets.
        /// </summary>
        private static Sprite CreatePanelSprite()
        {
            // The skin's parchment panel when it is there (see SetupProjectTheme); the carved stone
            // below is what a project without the skin files falls back to.
            var skinPanel = SkinSprite("panel");
            if (skinPanel != null) return skinPanel;

            // Thick border (roughly a third of the texture per side) and near-black/near-white
            // contrast -- an earlier, subtler version (thin 8px border in a 32px texture) turned
            // out imperceptible once shrunk to actual button size and compressed in a screenshot.
            const int size = 48;
            const int border = 16;

            var fill = new Color(0.55f, 0.55f, 0.52f);
            var groove = new Color(0.02f, 0.02f, 0.02f);
            var lightEdge = new Color(0.96f, 0.96f, 0.92f);
            var darkEdge = new Color(0.1f, 0.09f, 0.08f);
            var rivet = new Color(0.02f, 0.02f, 0.02f);

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distLeft = x;
                    var distRight = size - 1 - x;
                    var distTop = size - 1 - y;
                    var distBottom = y;
                    var minEdge = Mathf.Min(Mathf.Min(distLeft, distRight), Mathf.Min(distTop, distBottom));

                    Color color;
                    if (minEdge < 3) color = groove;
                    else if (minEdge < border)
                    {
                        var isLightSide = distLeft <= distRight && distLeft <= distTop && distLeft <= distBottom
                            || distTop <= distRight && distTop <= distLeft && distTop <= distBottom;
                        color = isLightSide ? lightEdge : darkEdge;
                    }
                    else color = fill;
                    pixels[y * size + x] = color;
                }
            }

            void Rivet(int cx, int cy)
            {
                for (var dy = 0; dy < 4; dy++)
                {
                    for (var dx = 0; dx < 4; dx++)
                    {
                        pixels[(cy + dy) * size + (cx + dx)] = rivet;
                    }
                }
            }
            Rivet(5, 5);
            Rivet(size - 9, 5);
            Rivet(5, size - 9);
            Rivet(size - 9, size - 9);

            texture.SetPixels(pixels);
            texture.Apply();

            Directory.CreateDirectory(TexturesFolder);
            var texPath = $"{TexturesFolder}/UI_Panel.asset";
            DeleteIfExists(texPath);
            AssetDatabase.CreateAsset(texture, texPath);

            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.name = "UI_Panel_Sprite";
            AssetDatabase.AddObjectToAsset(sprite, texture);
            AssetDatabase.ImportAsset(texPath);
            return sprite;
        }

        private static Sprite CreateIconSprite(string key, int size, System.Action<Color[], int> paint)
        {
            if (_iconSpriteCache.TryGetValue(key, out var cached) && cached != null) return cached;

            var pixels = new Color[size * size];
            paint(pixels, size);

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(pixels);
            texture.Apply();

            Directory.CreateDirectory(TexturesFolder);
            var path = $"{TexturesFolder}/Icon_{key}.asset";
            DeleteIfExists(path);
            AssetDatabase.CreateAsset(texture, path);

            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = $"Icon_{key}_Sprite";
            AssetDatabase.AddObjectToAsset(sprite, texture);
            AssetDatabase.ImportAsset(path);

            // Re-read the sprite through the AssetDatabase rather than handing back the instance
            // built above: ImportAsset re-creates the objects behind that path, so the local
            // reference is no longer the one that actually lives in the asset, and serializing it
            // into a scene writes a reference to nothing.
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite imported)
                {
                    _iconSpriteCache[key] = imported;
                    return imported;
                }
            }

            _iconSpriteCache[key] = sprite;
            return sprite;
        }

        /// <summary>Normalized (0,0)=bottom-left..(1,1)=top-right, axis-aligned fill.</summary>
        private static void FillIconRect(Color[] pixels, int size, float x0, float y0, float x1, float y1, Color color)
        {
            var px0 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(x0, x1) * size), 0, size);
            var px1 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(x0, x1) * size), 0, size);
            var py0 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(y0, y1) * size), 0, size);
            var py1 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(y0, y1) * size), 0, size);
            for (var y = py0; y < py1; y++)
            {
                for (var x = px0; x < px1; x++)
                {
                    pixels[y * size + x] = color;
                }
            }
        }

        /// <summary>Normalized (0,0)=bottom-left..(1,1)=top-right triangle fill (simple edge-function rasterization).</summary>
        private static void FillIconTriangle(Color[] pixels, int size, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            a *= size; b *= size; c *= size;
            var minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), 0, size);
            var maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))), 0, size);
            var minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))), 0, size);
            var maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))), 0, size);

            for (var y = minY; y < maxY; y++)
            {
                for (var x = minX; x < maxX; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    var d1 = IconTriangleSign(p, a, b);
                    var d2 = IconTriangleSign(p, b, c);
                    var d3 = IconTriangleSign(p, c, a);
                    var hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
                    var hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
                    if (!(hasNeg && hasPos)) pixels[y * size + x] = color;
                }
            }
        }

        private static float IconTriangleSign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        private static Sprite CreateCategoryIcon(BuildingCategory category)
        {
            if (CategoryIconBuilding.TryGetValue(category, out var stand) && IconSprite(stand) != null) return IconSprite(stand);
            switch (category)
            {
                case BuildingCategory.City:
                    return CreateIconSprite("Cat_City", 64, (p, s) =>
                    {
                        FillIconTriangle(p, s, new Vector2(0.15f, 0.55f), new Vector2(0.85f, 0.55f), new Vector2(0.5f, 0.88f), new Color(0.55f, 0.18f, 0.16f));
                        FillIconRect(p, s, 0.22f, 0.14f, 0.78f, 0.55f, new Color(0.82f, 0.68f, 0.5f));
                        FillIconRect(p, s, 0.44f, 0.14f, 0.56f, 0.38f, new Color(0.35f, 0.22f, 0.12f));
                    });
                case BuildingCategory.Food:
                    return CreateIconSprite("Cat_Food", 64, (p, s) =>
                    {
                        FillIconRect(p, s, 0.15f, 0.12f, 0.85f, 0.4f, new Color(0.45f, 0.32f, 0.18f));
                        FillIconRect(p, s, 0.22f, 0.4f, 0.42f, 0.62f, new Color(0.75f, 0.18f, 0.16f));
                        FillIconRect(p, s, 0.42f, 0.4f, 0.58f, 0.68f, new Color(0.86f, 0.68f, 0.24f));
                        FillIconRect(p, s, 0.58f, 0.4f, 0.78f, 0.6f, new Color(0.35f, 0.55f, 0.28f));
                    });
                case BuildingCategory.Defence:
                    return CreateIconSprite("Cat_Defence", 64, (p, s) =>
                    {
                        var shield = new Color(0.45f, 0.46f, 0.5f);
                        FillIconRect(p, s, 0.22f, 0.42f, 0.78f, 0.85f, shield);
                        FillIconTriangle(p, s, new Vector2(0.22f, 0.42f), new Vector2(0.78f, 0.42f), new Vector2(0.5f, 0.12f), shield);
                        var trim = new Color(0.7f, 0.62f, 0.3f);
                        FillIconRect(p, s, 0.46f, 0.2f, 0.54f, 0.78f, trim);
                        FillIconRect(p, s, 0.3f, 0.58f, 0.7f, 0.66f, trim);
                    });
                case BuildingCategory.Storage:
                    return CreateIconSprite("Cat_Storage", 64, (p, s) =>
                    {
                        // Stacked crates -- the category is four storehouses and their big versions.
                        var crate = new Color(0.58f, 0.44f, 0.26f);
                        var band = new Color(0.36f, 0.28f, 0.18f);
                        FillIconRect(p, s, 0.1f, 0.1f, 0.5f, 0.5f, crate);
                        FillIconRect(p, s, 0.1f, 0.28f, 0.5f, 0.34f, band);
                        FillIconRect(p, s, 0.52f, 0.1f, 0.9f, 0.5f, crate);
                        FillIconRect(p, s, 0.52f, 0.28f, 0.9f, 0.34f, band);
                        FillIconRect(p, s, 0.3f, 0.52f, 0.7f, 0.9f, crate);
                        FillIconRect(p, s, 0.3f, 0.7f, 0.7f, 0.76f, band);
                    });
                case BuildingCategory.Water:
                    return CreateIconSprite("Cat_Water", 64, (p, s) =>
                    {
                        // Two waves under a jetty. The category is a stub in the design and a stub
                        // here: bridge, dock and water mill sit in it until it gets thought through.
                        var water = new Color(0.32f, 0.55f, 0.8f);
                        var deck = new Color(0.5f, 0.38f, 0.24f);
                        FillIconRect(p, s, 0.08f, 0.16f, 0.92f, 0.28f, water);
                        FillIconRect(p, s, 0.08f, 0.36f, 0.92f, 0.48f, water);
                        FillIconRect(p, s, 0.1f, 0.6f, 0.9f, 0.72f, deck);
                        FillIconRect(p, s, 0.22f, 0.48f, 0.3f, 0.6f, deck);
                        FillIconRect(p, s, 0.7f, 0.48f, 0.78f, 0.6f, deck);
                    });
                case BuildingCategory.Entertainment:
                    return CreateIconSprite("Cat_Entertainment", 64, (p, s) =>
                    {
                        // A little tree: the one thing every decoration in this category has in
                        // common is that it is there to look at.
                        FillIconRect(p, s, 0.45f, 0.1f, 0.55f, 0.42f, new Color(0.42f, 0.28f, 0.16f));
                        FillIconTriangle(p, s, new Vector2(0.16f, 0.42f), new Vector2(0.84f, 0.42f), new Vector2(0.5f, 0.92f), new Color(0.32f, 0.6f, 0.28f));
                    });
                default: // Production
                    return CreateIconSprite("Cat_Production", 64, (p, s) =>
                    {
                        FillIconRect(p, s, 0.44f, 0.12f, 0.56f, 0.62f, new Color(0.42f, 0.28f, 0.16f));
                        FillIconRect(p, s, 0.24f, 0.6f, 0.76f, 0.82f, new Color(0.52f, 0.52f, 0.55f));
                    });
            }
        }

        private static Sprite CreateBuildingIcon(string buildingId)
        {
            // Rendered from the building's own model when it has one (Tools/blender/render_icons.py).
            var rendered = IconSprite(buildingId);
            if (rendered != null) return rendered;
            switch (buildingId)
            {
                case "Hovel":
                    return CreateIconSprite("Bld_Hovel", 64, (p, s) =>
                    {
                        FillIconTriangle(p, s, new Vector2(0.15f, 0.52f), new Vector2(0.85f, 0.52f), new Vector2(0.5f, 0.88f), new Color(0.5f, 0.14f, 0.14f));
                        FillIconRect(p, s, 0.22f, 0.14f, 0.78f, 0.52f, new Color(0.75f, 0.55f, 0.35f));
                        FillIconRect(p, s, 0.44f, 0.14f, 0.56f, 0.36f, new Color(0.32f, 0.18f, 0.09f));
                        FillIconRect(p, s, 0.64f, 0.62f, 0.72f, 0.82f, new Color(0.4f, 0.4f, 0.42f));
                    });
                case "Cottage":
                    return CreateIconSprite("Bld_Cottage", 64, (p, s) =>
                    {
                        FillIconTriangle(p, s, new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.5f), new Vector2(0.5f, 0.86f), new Color(0.35f, 0.2f, 0.4f));
                        FillIconRect(p, s, 0.16f, 0.14f, 0.84f, 0.5f, new Color(0.85f, 0.75f, 0.55f));
                        FillIconRect(p, s, 0.42f, 0.14f, 0.58f, 0.38f, new Color(0.32f, 0.18f, 0.09f));
                        FillIconRect(p, s, 0.22f, 0.28f, 0.32f, 0.4f, new Color(0.4f, 0.55f, 0.65f));
                        FillIconRect(p, s, 0.68f, 0.28f, 0.78f, 0.4f, new Color(0.4f, 0.55f, 0.65f));
                    });
                case "FisherHut":
                    return CreateIconSprite("Bld_FisherHut", 64, (p, s) =>
                    {
                        var fish = new Color(0.3f, 0.52f, 0.68f);
                        FillIconTriangle(p, s, new Vector2(0.15f, 0.5f), new Vector2(0.7f, 0.72f), new Vector2(0.7f, 0.28f), fish);
                        FillIconTriangle(p, s, new Vector2(0.7f, 0.28f), new Vector2(0.7f, 0.72f), new Vector2(0.92f, 0.5f), fish);
                    });
                case "Farm":
                    return CreateIconSprite("Bld_Farm", 64, (p, s) =>
                    {
                        var stem = new Color(0.42f, 0.58f, 0.24f);
                        var grain = new Color(0.86f, 0.68f, 0.24f);
                        FillIconRect(p, s, 0.47f, 0.12f, 0.53f, 0.55f, stem);
                        FillIconTriangle(p, s, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.9f), new Vector2(0.22f, 0.65f), grain);
                        FillIconTriangle(p, s, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.9f), new Vector2(0.78f, 0.65f), grain);
                    });
                case "Sawmill":
                    return CreateIconSprite("Bld_Sawmill", 64, (p, s) =>
                    {
                        FillIconRect(p, s, 0.46f, 0.12f, 0.54f, 0.7f, new Color(0.42f, 0.28f, 0.16f));
                        FillIconTriangle(p, s, new Vector2(0.3f, 0.55f), new Vector2(0.3f, 0.85f), new Vector2(0.62f, 0.7f), new Color(0.55f, 0.55f, 0.58f));
                    });
                case "Quarry":
                    return CreateIconSprite("Bld_Quarry", 64, (p, s) =>
                    {
                        var head = new Color(0.5f, 0.5f, 0.52f);
                        FillIconRect(p, s, 0.46f, 0.12f, 0.54f, 0.62f, new Color(0.42f, 0.28f, 0.16f));
                        FillIconTriangle(p, s, new Vector2(0.5f, 0.7f), new Vector2(0.15f, 0.85f), new Vector2(0.5f, 0.55f), head);
                        FillIconTriangle(p, s, new Vector2(0.5f, 0.7f), new Vector2(0.85f, 0.85f), new Vector2(0.5f, 0.55f), head);
                    });
                case "IronMine":
                    return CreateIconSprite("Bld_IronMine", 64, (p, s) =>
                    {
                        var ore = new Color(0.58f, 0.62f, 0.68f);
                        FillIconRect(p, s, 0.15f, 0.15f, 0.85f, 0.4f, new Color(0.35f, 0.35f, 0.38f));
                        FillIconRect(p, s, 0.25f, 0.4f, 0.45f, 0.58f, ore);
                        FillIconRect(p, s, 0.45f, 0.4f, 0.65f, 0.65f, ore);
                        FillIconRect(p, s, 0.6f, 0.4f, 0.78f, 0.55f, ore);
                        FillIconRect(p, s, 0.2f, 0.08f, 0.32f, 0.15f, new Color(0.15f, 0.15f, 0.15f));
                        FillIconRect(p, s, 0.68f, 0.08f, 0.8f, 0.15f, new Color(0.15f, 0.15f, 0.15f));
                    });
                case "CoalMine":
                    return CreateIconSprite("Bld_CoalMine", 64, (p, s) =>
                    {
                        var coal = new Color(0.12f, 0.12f, 0.13f);
                        FillIconRect(p, s, 0.15f, 0.15f, 0.85f, 0.4f, new Color(0.3f, 0.28f, 0.27f));
                        FillIconRect(p, s, 0.25f, 0.4f, 0.45f, 0.58f, coal);
                        FillIconRect(p, s, 0.45f, 0.4f, 0.65f, 0.65f, coal);
                        FillIconRect(p, s, 0.6f, 0.4f, 0.78f, 0.55f, coal);
                        FillIconRect(p, s, 0.2f, 0.08f, 0.32f, 0.15f, new Color(0.15f, 0.15f, 0.15f));
                        FillIconRect(p, s, 0.68f, 0.08f, 0.8f, 0.15f, new Color(0.15f, 0.15f, 0.15f));
                    });
                case "Warehouse":
                    return CreateIconSprite("Bld_Warehouse", 64, (p, s) =>
                    {
                        var crate = new Color(0.58f, 0.44f, 0.26f);
                        var band = new Color(0.36f, 0.28f, 0.18f);
                        FillIconRect(p, s, 0.12f, 0.12f, 0.88f, 0.7f, crate);
                        FillIconRect(p, s, 0.12f, 0.38f, 0.88f, 0.44f, band);
                        FillIconRect(p, s, 0.46f, 0.12f, 0.54f, 0.7f, band);
                        FillIconTriangle(p, s, new Vector2(0.06f, 0.7f), new Vector2(0.94f, 0.7f), new Vector2(0.5f, 0.94f), band);
                    });
                case "Barn":
                    return CreateIconSprite("Bld_Barn", 64, (p, s) =>
                    {
                        var wall = new Color(0.72f, 0.5f, 0.28f);
                        var grain = new Color(0.88f, 0.74f, 0.34f);
                        FillIconTriangle(p, s, new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.62f), new Vector2(0.5f, 0.94f), new Color(0.5f, 0.34f, 0.18f));
                        FillIconRect(p, s, 0.16f, 0.1f, 0.84f, 0.64f, wall);
                        FillIconRect(p, s, 0.38f, 0.1f, 0.62f, 0.46f, grain);
                    });
                case "Treasury":
                    return CreateIconSprite("Bld_Treasury", 64, (p, s) =>
                    {
                        var stone = new Color(0.52f, 0.5f, 0.46f);
                        var gold = new Color(0.85f, 0.7f, 0.24f);
                        FillIconRect(p, s, 0.12f, 0.1f, 0.88f, 0.66f, stone);
                        FillIconRect(p, s, 0.12f, 0.66f, 0.88f, 0.76f, new Color(0.36f, 0.34f, 0.32f));
                        FillIconRect(p, s, 0.3f, 0.28f, 0.7f, 0.5f, gold);
                        FillIconRect(p, s, 0.44f, 0.18f, 0.56f, 0.3f, new Color(0.3f, 0.28f, 0.26f));
                    });
                // The second tier of each storehouse: the same pictogram, doubled up, so the pair
                // reads as "more of the same" in the hotbar rather than as two unrelated buildings.
                case "BigWarehouse":
                    return CreateIconSprite("Bld_BigWarehouse", 64, (p, s) =>
                    {
                        var crate = new Color(0.58f, 0.44f, 0.26f);
                        var band = new Color(0.36f, 0.28f, 0.18f);
                        FillIconRect(p, s, 0.06f, 0.08f, 0.52f, 0.6f, crate);
                        FillIconRect(p, s, 0.06f, 0.3f, 0.52f, 0.36f, band);
                        FillIconRect(p, s, 0.48f, 0.24f, 0.94f, 0.82f, crate);
                        FillIconRect(p, s, 0.48f, 0.5f, 0.94f, 0.56f, band);
                        FillIconTriangle(p, s, new Vector2(0.42f, 0.82f), new Vector2(1f, 0.82f), new Vector2(0.71f, 0.98f), band);
                    });
                case "BigBarn":
                    return CreateIconSprite("Bld_BigBarn", 64, (p, s) =>
                    {
                        var wall = new Color(0.72f, 0.5f, 0.28f);
                        var grain = new Color(0.88f, 0.74f, 0.34f);
                        var roof = new Color(0.5f, 0.34f, 0.18f);
                        FillIconTriangle(p, s, new Vector2(0.02f, 0.5f), new Vector2(0.56f, 0.5f), new Vector2(0.29f, 0.74f), roof);
                        FillIconRect(p, s, 0.06f, 0.06f, 0.52f, 0.52f, wall);
                        FillIconTriangle(p, s, new Vector2(0.44f, 0.66f), new Vector2(0.98f, 0.66f), new Vector2(0.71f, 0.94f), roof);
                        FillIconRect(p, s, 0.48f, 0.22f, 0.94f, 0.68f, wall);
                        FillIconRect(p, s, 0.62f, 0.22f, 0.8f, 0.5f, grain);
                    });
                case "BigTreasury":
                    return CreateIconSprite("Bld_BigTreasury", 64, (p, s) =>
                    {
                        var stone = new Color(0.52f, 0.5f, 0.46f);
                        var gold = new Color(0.85f, 0.7f, 0.24f);
                        FillIconRect(p, s, 0.06f, 0.06f, 0.52f, 0.54f, stone);
                        FillIconRect(p, s, 0.06f, 0.54f, 0.52f, 0.62f, new Color(0.36f, 0.34f, 0.32f));
                        FillIconRect(p, s, 0.48f, 0.2f, 0.94f, 0.76f, stone);
                        FillIconRect(p, s, 0.48f, 0.76f, 0.94f, 0.84f, new Color(0.36f, 0.34f, 0.32f));
                        FillIconRect(p, s, 0.6f, 0.38f, 0.82f, 0.6f, gold);
                    });
                case "Fence":
                    return CreateIconSprite("Bld_Fence", 64, (p, s) =>
                    {
                        // Two posts and two rails, read as a wooden fence rather than the stone
                        // courses the Wall icon uses.
                        var post = new Color(0.45f, 0.3f, 0.16f);
                        var rail = new Color(0.62f, 0.45f, 0.25f);
                        FillIconRect(p, s, 0.12f, 0.46f, 0.88f, 0.58f, rail);
                        FillIconRect(p, s, 0.12f, 0.24f, 0.88f, 0.36f, rail);
                        FillIconRect(p, s, 0.2f, 0.12f, 0.34f, 0.78f, post);
                        FillIconRect(p, s, 0.66f, 0.12f, 0.8f, 0.78f, post);
                    });
                case "Tower":
                    return CreateIconSprite("Bld_Tower", 64, (p, s) =>
                    {
                        var stone = new Color(0.4f, 0.38f, 0.34f);
                        FillIconRect(p, s, 0.28f, 0.1f, 0.72f, 0.8f, stone);
                        FillIconRect(p, s, 0.22f, 0.8f, 0.36f, 0.92f, stone);
                        FillIconRect(p, s, 0.44f, 0.8f, 0.56f, 0.92f, stone);
                        FillIconRect(p, s, 0.64f, 0.8f, 0.78f, 0.92f, stone);
                    });
                case "Barracks":
                    return CreateIconSprite("Bld_Barracks", 64, (p, s) =>
                    {
                        FillIconRect(p, s, 0.16f, 0.12f, 0.84f, 0.55f, new Color(0.5f, 0.48f, 0.44f));
                        FillIconTriangle(p, s, new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.55f), new Vector2(0.5f, 0.8f), new Color(0.35f, 0.3f, 0.26f));
                        FillIconRect(p, s, 0.48f, 0.8f, 0.52f, 0.95f, new Color(0.3f, 0.2f, 0.1f));
                        FillIconRect(p, s, 0.52f, 0.85f, 0.72f, 0.95f, new Color(0.75f, 0.18f, 0.16f));
                    });
                case "Gate":
                    return CreateIconSprite("Bld_Gate", 64, (p, s) =>
                    {
                        var stone = new Color(0.4f, 0.38f, 0.34f);
                        FillIconRect(p, s, 0.14f, 0.12f, 0.32f, 0.85f, stone);
                        FillIconRect(p, s, 0.68f, 0.12f, 0.86f, 0.85f, stone);
                        FillIconRect(p, s, 0.14f, 0.78f, 0.86f, 0.9f, stone);
                    });
                // The fortified pair: the same silhouettes in darker stone, with the battlements
                // filled in solid, so the tier reads at hotbar size without a second look.
                case "FortifiedTower":
                    return CreateIconSprite("Bld_FortifiedTower", 64, (p, s) =>
                    {
                        var stone = new Color(0.3f, 0.29f, 0.27f);
                        var band = new Color(0.52f, 0.5f, 0.46f);
                        FillIconRect(p, s, 0.24f, 0.08f, 0.76f, 0.8f, stone);
                        FillIconRect(p, s, 0.24f, 0.44f, 0.76f, 0.52f, band);
                        FillIconRect(p, s, 0.18f, 0.8f, 0.34f, 0.94f, stone);
                        FillIconRect(p, s, 0.42f, 0.8f, 0.58f, 0.94f, stone);
                        FillIconRect(p, s, 0.66f, 0.8f, 0.82f, 0.94f, stone);
                    });
                case "FortifiedGate":
                    return CreateIconSprite("Bld_FortifiedGate", 64, (p, s) =>
                    {
                        var stone = new Color(0.3f, 0.29f, 0.27f);
                        var band = new Color(0.52f, 0.5f, 0.46f);
                        FillIconRect(p, s, 0.1f, 0.1f, 0.34f, 0.86f, stone);
                        FillIconRect(p, s, 0.66f, 0.1f, 0.9f, 0.86f, stone);
                        FillIconRect(p, s, 0.1f, 0.76f, 0.9f, 0.92f, stone);
                        FillIconRect(p, s, 0.34f, 0.1f, 0.66f, 0.2f, band);
                    });
                case "Road":
                    return CreateIconSprite("Bld_Road", 64, (p, s) =>
                    {
                        FillIconRect(p, s, 0.1f, 0.1f, 0.9f, 0.9f, new Color(0.35f, 0.35f, 0.36f));
                        var stripe = new Color(0.85f, 0.75f, 0.3f);
                        FillIconRect(p, s, 0.46f, 0.14f, 0.54f, 0.32f, stripe);
                        FillIconRect(p, s, 0.46f, 0.42f, 0.54f, 0.6f, stripe);
                        FillIconRect(p, s, 0.46f, 0.7f, 0.54f, 0.86f, stripe);
                    });
                case "Bridge":
                    return CreateIconSprite("Bld_Bridge", 64, (p, s) =>
                    {
                        var water = new Color(0.3f, 0.5f, 0.75f);
                        var plank = new Color(0.5f, 0.35f, 0.2f);
                        FillIconRect(p, s, 0.05f, 0.1f, 0.95f, 0.9f, water);
                        FillIconRect(p, s, 0.1f, 0.42f, 0.9f, 0.58f, plank);
                        FillIconRect(p, s, 0.16f, 0.58f, 0.22f, 0.72f, plank);
                        FillIconRect(p, s, 0.78f, 0.58f, 0.84f, 0.72f, plank);
                        FillIconRect(p, s, 0.16f, 0.28f, 0.22f, 0.42f, plank);
                        FillIconRect(p, s, 0.78f, 0.28f, 0.84f, 0.42f, plank);
                    });
                case "WaterMill":
                    return CreateIconSprite("Bld_WaterMill", 64, (p, s) =>
                    {
                        var wall = new Color(0.5f, 0.4f, 0.28f);
                        var roof = new Color(0.32f, 0.22f, 0.15f);
                        var wheel = new Color(0.35f, 0.24f, 0.14f);
                        FillIconRect(p, s, 0.14f, 0.16f, 0.56f, 0.6f, wall);
                        FillIconTriangle(p, s, new Vector2(0.1f, 0.6f), new Vector2(0.6f, 0.6f), new Vector2(0.35f, 0.82f), roof);
                        FillIconRect(p, s, 0.6f, 0.22f, 0.9f, 0.72f, wheel);
                        FillIconRect(p, s, 0.68f, 0.14f, 0.82f, 0.8f, wheel);
                    });
                case "Dock":
                    return CreateIconSprite("Bld_Dock", 64, (p, s) =>
                    {
                        var water = new Color(0.3f, 0.5f, 0.75f);
                        var deck = new Color(0.55f, 0.4f, 0.24f);
                        var crate = new Color(0.68f, 0.5f, 0.3f);
                        FillIconRect(p, s, 0.05f, 0.1f, 0.95f, 0.5f, water);
                        FillIconRect(p, s, 0.1f, 0.44f, 0.9f, 0.58f, deck);
                        FillIconRect(p, s, 0.2f, 0.58f, 0.42f, 0.8f, crate);
                        FillIconRect(p, s, 0.5f, 0.58f, 0.68f, 0.74f, crate);
                    });
                default:
                    return CreateIconSprite($"Bld_{buildingId}", 64, (p, s) =>
                    {
                        FillIconRect(p, s, 0.25f, 0.25f, 0.75f, 0.75f, new Color(0.6f, 0.6f, 0.6f));
                    });
            }
        }

        /// <summary>One stacked ingot, tinted per metal -- the three bars differ only in colour, which is exactly how a player tells them apart at icon size.</summary>
        private static Sprite CreateBarIcon(string name, Color metal, Color shine)
        {
            return CreateIconSprite(name, 64, (p, s) =>
            {
                FillIconRect(p, s, 0.14f, 0.26f, 0.86f, 0.54f, metal);
                FillIconRect(p, s, 0.24f, 0.54f, 0.76f, 0.7f, metal);
                FillIconRect(p, s, 0.2f, 0.32f, 0.66f, 0.38f, shine);
            });
        }

        /// <summary>Small pictogram per ResourceType, used by the top resource HUD bar in place of plain-text resource names (see ResourceHUDController/BuildResourceHUD).</summary>
        private static Sprite CreateResourceIcon(ResourceType type)
        {
            var rendered = IconSprite("Res_" + type);
            if (rendered != null) return rendered;
            switch (type)
            {
                case ResourceType.Wood:
                    return CreateIconSprite("Res_Wood", 64, (p, s) =>
                    {
                        var log = new Color(0.55f, 0.36f, 0.2f);
                        var ring = new Color(0.78f, 0.6f, 0.4f);
                        FillIconRect(p, s, 0.12f, 0.36f, 0.88f, 0.64f, log);
                        FillIconRect(p, s, 0.12f, 0.36f, 0.24f, 0.64f, ring);
                        FillIconRect(p, s, 0.76f, 0.36f, 0.88f, 0.64f, ring);
                        FillIconRect(p, s, 0.16f, 0.44f, 0.2f, 0.56f, log);
                        FillIconRect(p, s, 0.8f, 0.44f, 0.84f, 0.56f, log);
                    });
                case ResourceType.Stone:
                    return CreateIconSprite("Res_Stone", 64, (p, s) =>
                    {
                        var stone = new Color(0.58f, 0.56f, 0.52f);
                        var shade = new Color(0.42f, 0.4f, 0.37f);
                        FillIconTriangle(p, s, new Vector2(0.12f, 0.2f), new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.2f), stone);
                        FillIconTriangle(p, s, new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.78f), new Vector2(0.88f, 0.2f), shade);
                    });
                case ResourceType.Iron:
                    return CreateIconSprite("Res_Iron", 64, (p, s) =>
                    {
                        var ingot = new Color(0.62f, 0.65f, 0.7f);
                        var shine = new Color(0.82f, 0.85f, 0.88f);
                        FillIconRect(p, s, 0.14f, 0.3f, 0.86f, 0.58f, ingot);
                        FillIconRect(p, s, 0.22f, 0.58f, 0.78f, 0.68f, ingot);
                        FillIconRect(p, s, 0.2f, 0.36f, 0.32f, 0.52f, shine);
                    });
                case ResourceType.Coal:
                    return CreateIconSprite("Res_Coal", 64, (p, s) =>
                    {
                        var coal = new Color(0.14f, 0.14f, 0.15f);
                        var shine = new Color(0.34f, 0.34f, 0.36f);
                        FillIconRect(p, s, 0.18f, 0.18f, 0.5f, 0.5f, coal);
                        FillIconRect(p, s, 0.42f, 0.3f, 0.82f, 0.62f, coal);
                        FillIconRect(p, s, 0.22f, 0.55f, 0.5f, 0.82f, coal);
                        FillIconRect(p, s, 0.48f, 0.4f, 0.56f, 0.48f, shine);
                    });
                case ResourceType.Coins:
                    // A stack of square coins (not circular discs -- straight edges only, same
                    // "no circles" rule every other icon in this project follows) in a brighter,
                    // lighter yellow than the Gold bar's more amber tone, so the two read as
                    // visually distinct currencies at a glance.
                    return CreateIconSprite("Res_Coins", 64, (p, s) =>
                    {
                        var coin = new Color(0.95f, 0.82f, 0.3f);
                        var edge = new Color(0.7f, 0.58f, 0.16f);
                        FillIconRect(p, s, 0.18f, 0.14f, 0.7f, 0.32f, edge);
                        FillIconRect(p, s, 0.2f, 0.16f, 0.68f, 0.28f, coin);
                        FillIconRect(p, s, 0.26f, 0.34f, 0.78f, 0.52f, edge);
                        FillIconRect(p, s, 0.28f, 0.36f, 0.76f, 0.48f, coin);
                        FillIconRect(p, s, 0.34f, 0.54f, 0.86f, 0.72f, edge);
                        FillIconRect(p, s, 0.36f, 0.56f, 0.84f, 0.68f, coin);
                    });
                case ResourceType.CopperOre:
                    // Ore, not metal: rough lumps with a copper tint, matching the Stone icon's
                    // silhouette so the three ores read as a family.
                    return CreateIconSprite("Res_CopperOre", 64, (p, s) =>
                    {
                        var rock = new Color(0.48f, 0.44f, 0.4f);
                        var ore = new Color(0.78f, 0.45f, 0.24f);
                        FillIconRect(p, s, 0.16f, 0.2f, 0.84f, 0.6f, rock);
                        FillIconRect(p, s, 0.28f, 0.6f, 0.66f, 0.72f, rock);
                        FillIconRect(p, s, 0.26f, 0.3f, 0.4f, 0.44f, ore);
                        FillIconRect(p, s, 0.54f, 0.38f, 0.7f, 0.52f, ore);
                    });
                case ResourceType.IronBar:
                    return CreateBarIcon("Res_IronBar", new Color(0.62f, 0.65f, 0.7f), new Color(0.85f, 0.88f, 0.92f));
                case ResourceType.CopperBar:
                    return CreateBarIcon("Res_CopperBar", new Color(0.78f, 0.45f, 0.24f), new Color(0.93f, 0.66f, 0.42f));
                case ResourceType.GoldBar:
                    return CreateBarIcon("Res_GoldBar", new Color(0.85f, 0.68f, 0.24f), new Color(0.97f, 0.88f, 0.55f));
                case ResourceType.Grain:
                    // A sheaf: three upright stalks with the ears blocked in above them. Straight
                    // edges only, same as every other icon here.
                    return CreateIconSprite("Res_Grain", 64, (p, s) =>
                    {
                        var straw = new Color(0.85f, 0.7f, 0.3f);
                        var ear = new Color(0.95f, 0.83f, 0.45f);
                        FillIconRect(p, s, 0.22f, 0.12f, 0.3f, 0.6f, straw);
                        FillIconRect(p, s, 0.46f, 0.12f, 0.54f, 0.7f, straw);
                        FillIconRect(p, s, 0.7f, 0.12f, 0.78f, 0.6f, straw);
                        FillIconRect(p, s, 0.18f, 0.56f, 0.34f, 0.78f, ear);
                        FillIconRect(p, s, 0.42f, 0.66f, 0.58f, 0.88f, ear);
                        FillIconRect(p, s, 0.66f, 0.56f, 0.82f, 0.78f, ear);
                        FillIconRect(p, s, 0.14f, 0.24f, 0.86f, 0.32f, ear);
                    });
                case ResourceType.Flour:
                    // A sack, tied at the neck, with a spill of white in front of it -- flour is
                    // otherwise the same pale colour as nothing at all.
                    return CreateIconSprite("Res_Flour", 64, (p, s) =>
                    {
                        var sack = new Color(0.82f, 0.76f, 0.62f);
                        var tie = new Color(0.6f, 0.52f, 0.38f);
                        var powder = new Color(0.96f, 0.95f, 0.92f);
                        FillIconRect(p, s, 0.2f, 0.16f, 0.7f, 0.68f, sack);
                        FillIconRect(p, s, 0.3f, 0.68f, 0.6f, 0.78f, tie);
                        FillIconRect(p, s, 0.26f, 0.78f, 0.64f, 0.86f, sack);
                        FillIconRect(p, s, 0.62f, 0.16f, 0.88f, 0.24f, powder);
                        FillIconRect(p, s, 0.7f, 0.24f, 0.84f, 0.3f, powder);
                    });
                case ResourceType.Bread:
                    // A loaf: a darker crust with the slashes cut across its top.
                    return CreateIconSprite("Res_Bread", 64, (p, s) =>
                    {
                        var crust = new Color(0.72f, 0.48f, 0.24f);
                        var crumb = new Color(0.88f, 0.68f, 0.4f);
                        FillIconRect(p, s, 0.12f, 0.26f, 0.88f, 0.66f, crust);
                        FillIconRect(p, s, 0.2f, 0.66f, 0.8f, 0.74f, crust);
                        FillIconRect(p, s, 0.28f, 0.58f, 0.36f, 0.7f, crumb);
                        FillIconRect(p, s, 0.46f, 0.58f, 0.54f, 0.72f, crumb);
                        FillIconRect(p, s, 0.64f, 0.58f, 0.72f, 0.7f, crumb);
                    });
                case ResourceType.Food:
                    // Everything edible that isn't bread yet: fish, fruit, meat. One generic
                    // basket rather than an attempt to draw all three.
                    return CreateIconSprite("Res_Food", 64, (p, s) =>
                    {
                        var basket = new Color(0.6f, 0.42f, 0.24f);
                        var weave = new Color(0.75f, 0.56f, 0.34f);
                        var produce = new Color(0.72f, 0.24f, 0.22f);
                        var leaf = new Color(0.35f, 0.6f, 0.28f);
                        FillIconRect(p, s, 0.32f, 0.6f, 0.52f, 0.8f, produce);
                        FillIconRect(p, s, 0.54f, 0.62f, 0.7f, 0.78f, leaf);
                        FillIconRect(p, s, 0.16f, 0.2f, 0.84f, 0.62f, basket);
                        FillIconRect(p, s, 0.16f, 0.38f, 0.84f, 0.44f, weave);
                    });
                default: // Gold
                    return CreateIconSprite("Res_Gold", 64, (p, s) =>
                    {
                        var bar = new Color(0.85f, 0.68f, 0.24f);
                        var shine = new Color(0.95f, 0.86f, 0.5f);
                        FillIconRect(p, s, 0.14f, 0.28f, 0.86f, 0.58f, bar);
                        FillIconRect(p, s, 0.22f, 0.58f, 0.78f, 0.7f, bar);
                        FillIconRect(p, s, 0.2f, 0.34f, 0.68f, 0.4f, shine);
                    });
            }
        }

        /// <summary>
        /// Runtime-queryable counterpart to CreateResourceIcon/CreatePopulationIcon -- those two
        /// only ever get called from Editor code (this script), but BuildingInfoPanelController's
        /// upgrade/repair cost chips need the same sprites at play time, so they're captured into
        /// a saved ScriptableObject asset indexed by ResourceType (see ResourceIconLibrary).
        /// </summary>
        private static ResourceIconLibrary CreateResourceIconLibrary()
        {
            // Sized off the enum rather than a literal: this array used to be `new Sprite[8]` and
            // silently stopped covering the enum the moment a resource was added -- the library's
            // bounds check then answered "no icon" for it, which looks exactly like a drawing that
            // was never written.
            // (System.Enum spelled out: a plain `using System;` here would make the file's bare
            // `Random.Range` calls ambiguous with System.Random.)
            var icons = new Sprite[System.Enum.GetValues(typeof(ResourceType)).Length];
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                icons[(int)type] = type == ResourceType.Population
                    ? CreatePopulationIcon()
                    : CreateResourceIcon(type);
            }

            var library = ScriptableObject.CreateInstance<ResourceIconLibrary>();
            library.EditorInitialize(icons);

            Directory.CreateDirectory(ResourceScriptableObjectsFolder);
            var path = $"{ResourceScriptableObjectsFolder}/ResourceIconLibrary.asset";
            DeleteIfExists(path);
            AssetDatabase.CreateAsset(library, path);
            return library;
        }

        private static Sprite CreatePopulationIcon()
        {
            var rendered = IconSprite("Res_Population");
            if (rendered != null) return rendered;
            return CreateIconSprite("Res_Population", 64, (p, s) =>
            {
                var body = new Color(0.85f, 0.82f, 0.75f);
                FillIconRect(p, s, 0.38f, 0.56f, 0.62f, 0.74f, body);
                FillIconTriangle(p, s, new Vector2(0.2f, 0.1f), new Vector2(0.8f, 0.1f), new Vector2(0.5f, 0.54f), body);
            });
        }

        /// <summary>An open square bracket (three sides) with an arrowhead where the fourth side would start -- reads as "turn/cycle" without needing a curved stroke (straight-edges-only, matching every other icon here).</summary>
        private static Sprite CreateRotateIcon()
        {
            return CreateIconSprite("Action_Rotate", 64, (p, s) =>
            {
                var line = new Color(0.92f, 0.92f, 0.9f);
                FillIconRect(p, s, 0.2f, 0.78f, 0.8f, 0.86f, line);
                FillIconRect(p, s, 0.72f, 0.24f, 0.8f, 0.86f, line);
                FillIconRect(p, s, 0.2f, 0.24f, 0.8f, 0.32f, line);
                FillIconTriangle(p, s, new Vector2(0.06f, 0.44f), new Vector2(0.28f, 0.56f), new Vector2(0.28f, 0.32f), line);
            });
        }

        /// <summary>A tick, drawn as two thick strokes -- the short one down-right, the long one up-right. Straight edges only, like every other icon here.</summary>
        private static Sprite CreateConfirmIcon()
        {
            return CreateIconSprite("Action_Confirm", 64, (p, s) =>
            {
                var line = new Color(0.82f, 0.98f, 0.82f);
                const int halfThickness = 3;

                // Short stroke: down from the left toward the elbow.
                var elbow = new Vector2Int(Mathf.RoundToInt(s * 0.42f), Mathf.RoundToInt(s * 0.28f));
                var start = new Vector2Int(Mathf.RoundToInt(s * 0.18f), Mathf.RoundToInt(s * 0.52f));
                var end = new Vector2Int(Mathf.RoundToInt(s * 0.84f), Mathf.RoundToInt(s * 0.78f));

                StrokeIconLine(p, s, start, elbow, halfThickness, line);
                StrokeIconLine(p, s, elbow, end, halfThickness, line);
            });
        }

        /// <summary>A straight line between two pixel positions, thickened square. Used by the tick; kept general because every icon here is drawn from straight strokes.</summary>
        private static void StrokeIconLine(Color[] pixels, int size, Vector2Int from, Vector2Int to, int halfThickness, Color color)
        {
            var steps = Mathf.Max(Mathf.Abs(to.x - from.x), Mathf.Abs(to.y - from.y));
            if (steps <= 0) return;

            for (var i = 0; i <= steps; i++)
            {
                var t = i / (float)steps;
                var cx = Mathf.RoundToInt(Mathf.Lerp(from.x, to.x, t));
                var cy = Mathf.RoundToInt(Mathf.Lerp(from.y, to.y, t));

                for (var dy = -halfThickness; dy <= halfThickness; dy++)
                {
                    for (var dx = -halfThickness; dx <= halfThickness; dx++)
                    {
                        var x = cx + dx;
                        var y = cy + dy;
                        if (x < 0 || x >= size || y < 0 || y >= size) continue;
                        pixels[y * size + x] = color;
                    }
                }
            }
        }

        /// <summary>A plain X, drawn as two thick diagonals. Deliberately not a "back" arrow: this abandons the placement, it does not step anywhere.</summary>
        private static Sprite CreateCancelIcon()
        {
            return CreateIconSprite("Action_Cancel", 64, (p, s) =>
            {
                var line = new Color(0.95f, 0.72f, 0.68f);
                var margin = Mathf.RoundToInt(s * 0.18f);
                // Thick enough to survive being scaled down to a phone-sized button.
                const int halfThickness = 3;

                for (var i = margin; i < s - margin; i++)
                {
                    for (var offset = -halfThickness; offset <= halfThickness; offset++)
                    {
                        var x = i + offset;
                        if (x < 0 || x >= s) continue;
                        p[i * s + x] = line;
                        p[(s - 1 - i) * s + x] = line;
                    }
                }
            });
        }
    }
}
