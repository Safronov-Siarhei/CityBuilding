using CityBuilder.Core;
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
        // Bigger than the building health bar (1.5 x 0.18), which is a bar you glance at: this is
        // one the player watches for fifteen seconds, and at the game's camera height the first
        // version came out about twenty pixels wide and three tall -- drawn, and invisible.
        private const float BarWidth = 2f;
        private const float BarHeight = 0.26f;
        private const float BackdropPadding = 0.07f;

        /// <summary>Clearance above whatever the node actually is -- measured, not guessed, because a fir and a knee-high boulder are nothing alike.</summary>
        private const float ClearanceAboveNode = 0.6f;

        /// <summary>Fallback height for a node with no renderers at all (a bare test node, or a model that failed to load).</summary>
        private const float FallbackNodeHeight = 1.5f;

        private static readonly Color BackdropColor = new Color(0.08f, 0.08f, 0.09f, 1f);
        private static readonly Color FillColor = new Color(0.95f, 0.82f, 0.35f);

        private Transform _fill;
        private Camera _camera;

        /// <summary>
        /// Creates the bar hovering over the node, hidden until the first Report call.
        ///
        /// Deliberately NOT a child of the node, which is how the first version of this got it
        /// wrong twice over. The tree prefabs are FBX models carrying a corrective 90-degree root
        /// rotation from Blender's Z-up authoring (the same trap TreesAreaSpawner.AddClickCollider
        /// documents), so a local +Y offset does not point up at all -- it points sideways, and
        /// the bar sat two metres beside the trunk at ground level instead of over the crown. And
        /// a sapling under TreeGrowth is scaled down to a tenth, which would have shrunk the bar
        /// along with it. Living in world space is immune to both.
        ///
        /// The owning node destroys it (see ResourceNode.OnDestroy), since nothing else would.
        /// </summary>
        public static HarvestProgressBar CreateFor(Transform node)
        {
            var root = new GameObject("HarvestProgress");
            root.transform.position = node.position + Vector3.up * (MeasuredHeightOf(node) + ClearanceAboveNode);

            var bar = root.AddComponent<HarvestProgressBar>();
            bar.Build();
            root.SetActive(false);
            return bar;
        }

        /// <summary>
        /// How tall this node stands, from its renderers' world bounds. World bounds are the right
        /// tool HERE (unlike the collider sizing in TreesAreaSpawner, which needed local space):
        /// what is wanted is a world-space height to clear, whatever rotation and scale the model
        /// arrived with.
        /// </summary>
        private static float MeasuredHeightOf(Transform node)
        {
            var measured = false;
            var top = node.position.y;

            foreach (var renderer in node.GetComponentsInChildren<Renderer>())
            {
                var max = renderer.bounds.max.y;
                if (!measured || max > top) top = max;
                measured = true;
            }

            return measured ? Mathf.Max(0f, top - node.position.y) : FallbackNodeHeight;
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

            var material = new Material(RuntimeShaders.Unlit) { color = color };
            quad.GetComponent<Renderer>().sharedMaterial = material;
            return quad.transform;
        }
    }
}
