using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// The health over a building or a unit: an icon -- a heart over a unit, a shield over a
    /// building -- and beside it a rounded bar in a dark frame that drains from the right, green to
    /// yellow to red (the user, 2026-10-22: the old flat rectangles looked cheap). Shown on the
    /// first hit and hidden again once nothing has hurt it for HideAfterSecondsWithoutDamage, or
    /// kept up while the building is selected (Pin).
    ///
    /// Built from a few flat quads rather than a world-space Canvas: a Canvas per building would
    /// drag in layout/batching cost for something this simple. The pictures (a rounded bar, the
    /// heart, the shield) are drawn into small textures once and shared by every bar; the frame
    /// and icons share materials, only the fill's colour is set per bar (a property block). Nothing
    /// runs while the bar is hidden -- it deactivates its own GameObject.
    /// </summary>
    public class BuildingHealthBar : MonoBehaviour
    {
        public enum Icon { Shield, Heart }

        private const float HideAfterSecondsWithoutDamage = 5f;
        private const float BarWidth = 1.5f;
        private const float BarHeight = 0.24f;
        private const float FramePadding = 0.05f;
        private const float IconSize = 0.42f;
        private const float IconGap = 0.04f;
        // Above the decay marker (3f) so a decaying building under attack shows both without
        // them overlapping.
        private const float HeightAboveBuilding = 3.9f;

        private static readonly Color FrameColor = new Color(0.16f, 0.1f, 0.06f, 0.92f);
        private static readonly Color TrackColor = new Color(0.05f, 0.04f, 0.03f, 0.85f);
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private static Texture2D _barTexture, _heartTexture, _shieldTexture;
        private static Material _frameMaterial, _trackMaterial, _heartMaterial, _shieldMaterial, _fillMaterial;

        private Transform _fill;
        private Renderer _fillRenderer;
        private MaterialPropertyBlock _block;
        private Camera _camera;
        private float _hideTimer;
        private bool _pinned;
        private float _fraction = 1f;

        /// <summary>Creates the bar over a building, hidden until the first Report or Pin.</summary>
        public static BuildingHealthBar CreateFor(Transform building) => Create(building, HeightAboveBuilding, Icon.Shield);

        /// <summary>
        /// The bar over a unit (an orc, a beast): `height` metres over its feet and `width` metres
        /// across, whatever scale the unit's root carries; a heart beside it.
        /// </summary>
        public static BuildingHealthBar CreateFor(Transform unit, float height, float width)
        {
            var bar = Create(unit, height, Icon.Heart);
            var scale = unit.lossyScale;
            var t = bar.transform;
            var s = width / (BarWidth + IconSize + IconGap);
            t.localPosition = new Vector3(0f, height / Mathf.Max(0.0001f, scale.y), 0f);
            t.localScale = new Vector3(s / Mathf.Max(0.0001f, scale.x), s / Mathf.Max(0.0001f, scale.y), 1f / Mathf.Max(0.0001f, scale.z));
            return bar;
        }

        private static BuildingHealthBar Create(Transform parent, float height, Icon icon)
        {
            var root = new GameObject("HealthBar");
            root.AddComponent<WorldIndicator>();
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, height, 0f);

            var bar = root.AddComponent<BuildingHealthBar>();
            bar.Build(icon);
            root.SetActive(false);
            return bar;
        }

        /// <summary>Shows the bar at this health fraction (0..1) and restarts the auto-hide countdown. Called on every hit, so repeated damage keeps it on screen.</summary>
        public void Report(float healthFraction)
        {
            _fraction = Mathf.Clamp01(healthFraction);
            gameObject.SetActive(true);
            _hideTimer = HideAfterSecondsWithoutDamage;
            Paint();
        }

        /// <summary>Kept up while true (a selected building, BuildingFocus) at this fraction; let go, it hides as after a hit.</summary>
        public void Pin(bool pinned, float healthFraction)
        {
            _pinned = pinned;
            _fraction = Mathf.Clamp01(healthFraction);
            if (pinned)
            {
                gameObject.SetActive(true);
                Paint();
            }
            else _hideTimer = Mathf.Min(_hideTimer, 0.01f);
        }

        public bool IsShown => gameObject.activeSelf;

        private void Paint()
        {
            // drains from the right: the fill grows from the bar's left edge
            var width = BarWidth * _fraction;
            _fill.localScale = new Vector3(Mathf.Max(0.0001f, width), BarHeight, 1f);
            _fill.localPosition = new Vector3(BarLeft + width * 0.5f, 0f, -0.02f);
            _fillRenderer.enabled = _fraction > 0f;
            _block.SetColor(BaseColor, ColorFor(_fraction));
            _fillRenderer.SetPropertyBlock(_block);
        }

        private void Update()
        {
            FaceCamera();
            if (_pinned) return;
            _hideTimer -= Time.deltaTime;
            if (_hideTimer <= 0f) gameObject.SetActive(false);
        }

        /// <summary>Matches the camera's own orientation rather than aiming at its position, so every bar lies parallel to the view, undistorted.</summary>
        private void FaceCamera()
        {
            if (_camera == null)
            {
                // Camera.main walks the scene, so it's resolved once and only while a bar is shown
                _camera = Camera.main;
                if (_camera == null) return;
            }
            transform.rotation = _camera.transform.rotation;
        }

        // The whole bar is centred on the root: the icon on the left, the bar to its right.
        private static float TotalWidth => IconSize + IconGap + BarWidth + FramePadding * 2f;
        private static float BarLeft => -TotalWidth * 0.5f + IconSize + IconGap + FramePadding;

        private void Build(Icon icon)
        {
            EnsureShared();
            var barCentre = BarLeft + BarWidth * 0.5f;

            var frame = Quad("Frame", _frameMaterial);
            frame.localScale = new Vector3(BarWidth + FramePadding * 2f, BarHeight + FramePadding * 2f, 1f);
            frame.localPosition = new Vector3(barCentre, 0f, 0f);

            var track = Quad("Track", _trackMaterial);
            track.localScale = new Vector3(BarWidth, BarHeight, 1f);
            track.localPosition = new Vector3(barCentre, 0f, -0.01f);

            _fill = Quad("Fill", _fillMaterial);
            _fillRenderer = _fill.GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();

            var picture = Quad("Icon", icon == Icon.Heart ? _heartMaterial : _shieldMaterial);
            picture.localScale = new Vector3(IconSize, IconSize, 1f);
            picture.localPosition = new Vector3(-TotalWidth * 0.5f + IconSize * 0.5f, 0f, -0.03f);
            Paint();
        }

        private Transform Quad(string quadName, Material material)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = quadName;
            // Must go: a stray collider here would be picked up by the click/ground raycasts the
            // rest of the game fires (see CitizenAgent's ground probe, BuildingSelector).
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(transform, false);
            var renderer = quad.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return quad.transform;
        }

        private static Color ColorFor(float healthFraction)
        {
            if (healthFraction > 0.6f) return new Color(0.42f, 0.82f, 0.3f);
            if (healthFraction > 0.3f) return new Color(0.95f, 0.78f, 0.25f);
            return new Color(0.9f, 0.28f, 0.22f);
        }

        // ---------------------------------------------------------------- the shared pictures

        private static void EnsureShared()
        {
            if (_fillMaterial != null) return;
            _barTexture = Draw(64, 16, RoundedBar);
            _heartTexture = Draw(48, 48, Heart);
            _shieldTexture = Draw(48, 48, Shield);
            // Drawn in a fixed order, not sorted by distance: the layers are transparent quads a hair
            // apart, and sorted by their centres the track came out in front of a drained fill and
            // hid it -- a beast's bar looked full or empty at random when hit (playtest 4, 2026-10-22).
            _frameMaterial = Textured(_barTexture, FrameColor, 0);
            _trackMaterial = Textured(_barTexture, TrackColor, 1);
            _fillMaterial = Textured(_barTexture, Color.white, 2);
            _heartMaterial = Textured(_heartTexture, Color.white, 3);
            _shieldMaterial = Textured(_shieldTexture, Color.white, 3);
        }

        private static Material Textured(Texture2D texture, Color color, int layer)
        {
            var material = UI.MoveOrderMarker.CreateTransparentMaterial(color);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 50 + layer;
            material.SetTexture("_BaseMap", texture);
            material.mainTexture = texture;
            return material;
        }

        private delegate Color Pixel(float u, float v);

        private static Texture2D Draw(int width, int height, Pixel pixel)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++) pixels[y * width + x] = pixel((x + 0.5f) / width, (y + 0.5f) / height);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        /// <summary>A pill: white, its ends round, soft-edged.</summary>
        private static Color RoundedBar(float u, float v)
        {
            const float aspect = 4f;   // 64 x 16
            var x = (u - 0.5f) * aspect;
            var y = v - 0.5f;
            var r = 0.5f;
            var inner = aspect * 0.5f - r;
            var dx = Mathf.Max(Mathf.Abs(x) - inner, 0f);
            var d = Mathf.Sqrt(dx * dx + y * y) - r;
            return new Color(1f, 1f, 1f, Mathf.Clamp01(-d * 16f));
        }

        /// <summary>A red heart with a dark outline and a light highlight.</summary>
        private static Color Heart(float u, float v)
        {
            var x = (u - 0.5f) * 2.4f;
            var y = (v - 0.45f) * 2.4f;
            // the classic implicit heart: (x² + y² - 1)³ - x²y³ <= 0
            var a = x * x + y * y - 1f;
            var f = a * a * a - x * x * y * y * y;
            var inside = Mathf.Clamp01(-f * 6f);
            var outline = Mathf.Clamp01((-f + 0.15f) * 6f);
            if (outline <= 0f) return Color.clear;
            var body = new Color(0.86f, 0.16f, 0.18f);
            var shine = Mathf.Clamp01(1f - ((x + 0.45f) * (x + 0.45f) + (y - 0.35f) * (y - 0.35f)) * 9f);
            var c = Color.Lerp(new Color(0.25f, 0.05f, 0.05f), Color.Lerp(body, Color.white, shine * 0.6f), inside);
            c.a = outline;
            return c;
        }

        /// <summary>A steel shield with a gold rim.</summary>
        private static Color Shield(float u, float v)
        {
            var x = (u - 0.5f) * 2f;
            var y = (v - 0.5f) * 2f;
            // flat top, sides curving into a point at the bottom
            var halfWidth = y > 0.2f ? 0.82f : 0.82f * Mathf.Sqrt(Mathf.Clamp01((y + 0.95f) / 1.15f));
            var inTop = y < 0.85f;
            var edge = Mathf.Min(halfWidth - Mathf.Abs(x), 0.85f - y, y + 0.95f);
            if (!inTop || edge < -0.04f) return Color.clear;
            var alpha = Mathf.Clamp01((edge + 0.04f) * 25f);
            var rim = edge < 0.12f;
            var c = rim ? new Color(0.95f, 0.75f, 0.25f) : Color.Lerp(new Color(0.45f, 0.55f, 0.7f), new Color(0.75f, 0.82f, 0.92f), Mathf.Clamp01(-x * 0.6f + 0.5f));
            c.a = alpha;
            return c;
        }
    }
}
