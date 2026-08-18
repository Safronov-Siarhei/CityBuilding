using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// The little bar that fills over a tree being felled or a boulder being chipped at.
    ///
    /// Fifteen seconds is a long time to stare at a citizen standing next to a tree wondering
    /// whether anything is happening. This is the answer: the bar fills, the tree comes down, and
    /// the mechanic explains itself without a word.
    ///
    /// Built the same way BuildingHealthBar is -- two flat unlit quads facing the camera, not a
    /// world-space Canvas -- for the same reason: a Canvas per tree would be layout and batching
    /// cost on a phone for something this simple, and every other in-world indicator in this game
    /// is made of primitives. It fills rather than drains, because it measures work done, not a
    /// resource running out.
    /// </summary>
    public class HarvestProgressBar : MonoBehaviour
    {
        private const float BarWidth = 0.9f;
        private const float BarHeight = 0.14f;
        private const float BackdropPadding = 0.05f;
        // Trees and boulders are far shorter than buildings, and this has to clear the canopy
        // without floating off on its own.
        private const float HeightAboveNode = 2.2f;

        private static readonly Color BackdropColor = new Color(0.08f, 0.08f, 0.09f, 1f);
        private static readonly Color FillColor = new Color(0.95f, 0.82f, 0.35f);

        private Transform _fill;
        private Camera _camera;

        /// <summary>Creates the bar as a child of the node, hidden until the first Report call.</summary>
        public static HarvestProgressBar CreateFor(Transform node)
        {
            var root = new GameObject("HarvestProgress");
            root.transform.SetParent(node, false);
            root.transform.localPosition = new Vector3(0f, HeightAboveNode, 0f);

            var bar = root.AddComponent<HarvestProgressBar>();
            bar.Build();
            root.SetActive(false);
            return bar;
        }

        /// <summary>Shows the bar filled to this fraction (0..1) of the current dig.</summary>
        public void Report(float progress)
        {
            progress = Mathf.Clamp01(progress);

            if (!gameObject.activeSelf) gameObject.SetActive(true);

            // Grown from the left edge so it reads as filling up rather than expanding outwards.
            var width = BarWidth * progress;
            _fill.localScale = new Vector3(width, BarHeight, 1f);
            _fill.localPosition = new Vector3(-BarWidth * 0.5f + width * 0.5f, 0f, -0.01f);
        }

        /// <summary>Nobody is working this node. Deactivating the object stops Update entirely rather than leaving hundreds of bars billboarding themselves every frame.</summary>
        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_camera == null)
            {
                // Camera.main walks the scene, so it is resolved once, and only while a bar is
                // actually on screen.
                _camera = Camera.main;
                if (_camera == null) return;
            }

            // Matching the camera's orientation rather than aiming at its position keeps every bar
            // parallel to the view plane and undistorted -- what reads correctly for a flat UI
            // element sitting in the world.
            transform.rotation = _camera.transform.rotation;
        }

        private void Build()
        {
            var backdrop = CreateQuad("Backdrop", BackdropColor);
            backdrop.localScale = new Vector3(BarWidth + BackdropPadding, BarHeight + BackdropPadding, 1f);
            backdrop.localPosition = Vector3.zero;

            var fill = CreateQuad("Fill", FillColor);
            fill.localScale = new Vector3(BarWidth, BarHeight, 1f);
            // Slightly in front of the backdrop so the two never z-fight.
            fill.localPosition = new Vector3(0f, 0f, -0.01f);
            _fill = fill;
        }

        private Transform CreateQuad(string quadName, Color color)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = quadName;
            // Must go: a stray collider here would be picked up by the click and ground raycasts
            // the rest of the game fires, and this one sits right where the player taps a tree.
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(transform, false);

            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = color };
            quad.GetComponent<Renderer>().sharedMaterial = material;
            return quad.transform;
        }
    }
}
