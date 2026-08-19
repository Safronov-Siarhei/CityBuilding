using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// A small floating bar over a building showing how much of its health is left, shown only
    /// while it's under attack -- it appears on the first hit and hides again once nothing has
    /// damaged the building for HideAfterSecondsWithoutDamage. Deliberately wordless: the bar
    /// shrinks, there are no numbers, matching how the rest of the game surfaces state (see the
    /// decay warning marker on the same buildings).
    ///
    /// Built from two flat quads rather than a world-space Canvas: a Canvas per building would
    /// drag in layout/batching cost for something this simple, and this project already draws
    /// every other in-world indicator (decay marker, citizen selection marker, cell highlights)
    /// out of primitives. Nothing runs while the bar is hidden -- it deactivates its own
    /// GameObject, so Update stops being called entirely rather than idling every frame on every
    /// building in the settlement.
    /// </summary>
    public class BuildingHealthBar : MonoBehaviour
    {
        private const float HideAfterSecondsWithoutDamage = 5f;
        private const float BarWidth = 1.5f;
        private const float BarHeight = 0.18f;
        private const float BackdropPadding = 0.06f;
        // Above the decay marker (3f) so a decaying building under attack shows both without
        // them overlapping.
        private const float HeightAboveBuilding = 3.9f;

        private static readonly Color BackdropColor = new Color(0.08f, 0.08f, 0.09f, 1f);

        private Transform _fill;
        private Material _fillMaterial;
        private Camera _camera;
        private float _hideTimer;

        /// <summary>Creates the bar as a child of the building, hidden until the first Report call.</summary>
        public static BuildingHealthBar CreateFor(Transform building)
        {
            var root = new GameObject("HealthBar");
            root.transform.SetParent(building, false);
            root.transform.localPosition = new Vector3(0f, HeightAboveBuilding, 0f);

            var bar = root.AddComponent<BuildingHealthBar>();
            bar.Build();
            root.SetActive(false);
            return bar;
        }

        /// <summary>Shows the bar at this health fraction (0..1) and restarts the auto-hide countdown. Called on every hit, so repeated damage keeps it on screen.</summary>
        public void Report(float healthFraction)
        {
            healthFraction = Mathf.Clamp01(healthFraction);

            gameObject.SetActive(true);
            _hideTimer = HideAfterSecondsWithoutDamage;

            // Grown from the left edge rather than the centre, so the bar drains in one direction
            // like a health bar is expected to.
            var width = BarWidth * healthFraction;
            _fill.localScale = new Vector3(width, BarHeight, 1f);
            _fill.localPosition = new Vector3(-BarWidth * 0.5f + width * 0.5f, 0f, -0.01f);

            if (_fillMaterial != null) _fillMaterial.color = ColorFor(healthFraction);
        }

        private void Update()
        {
            FaceCamera();

            _hideTimer -= Time.deltaTime;
            if (_hideTimer <= 0f) gameObject.SetActive(false);
        }

        /// <summary>Matches the camera's own orientation rather than aiming at its position -- that keeps every bar on screen parallel to the view plane and undistorted, which is what reads correctly for a flat UI element in world space.</summary>
        private void FaceCamera()
        {
            if (_camera == null)
            {
                // Camera.main walks the scene, so it's resolved once and only while a bar is
                // actually visible -- never every frame for every building.
                _camera = Camera.main;
                if (_camera == null) return;
            }

            transform.rotation = _camera.transform.rotation;
        }

        private void Build()
        {
            var backdrop = CreateQuad("Backdrop", BackdropColor, out _);
            backdrop.localScale = new Vector3(BarWidth + BackdropPadding, BarHeight + BackdropPadding, 1f);
            backdrop.localPosition = Vector3.zero;

            var fill = CreateQuad("Fill", ColorFor(1f), out _fillMaterial);
            fill.localScale = new Vector3(BarWidth, BarHeight, 1f);
            // Slightly in front of the backdrop so the two never z-fight.
            fill.localPosition = new Vector3(0f, 0f, -0.01f);
            _fill = fill;
        }

        private Transform CreateQuad(string quadName, Color color, out Material material)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = quadName;
            // Must go: a stray collider here would be picked up by the click/ground raycasts the
            // rest of the game fires (see CitizenAgent's ground probe, BuildingSelector).
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(transform, false);

            material = new Material(RuntimeShaders.Unlit) { color = color };
            quad.GetComponent<Renderer>().sharedMaterial = material;
            return quad.transform;
        }

        private static Color ColorFor(float healthFraction)
        {
            if (healthFraction > 0.6f) return new Color(0.45f, 0.8f, 0.35f);
            if (healthFraction > 0.3f) return new Color(0.9f, 0.8f, 0.3f);
            return new Color(0.9f, 0.3f, 0.25f);
        }
    }
}
