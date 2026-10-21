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
        // One quad: a medallion with the resource's icon and a pill to its right, all drawn by
        // Shaders/HarvestBar (the user, 2026-10-21: the plain bar "выглядит очень просто").
        private const float BarWidth = 2.8f;
        private const float BarHeight = 0.7f;

        /// <summary>Clearance above whatever the node actually is -- measured, not guessed, because a fir and a knee-high boulder are nothing alike.</summary>
        private const float ClearanceAboveNode = 0.6f;

        /// <summary>Fallback height for a node with no renderers at all (a bare test node, or a model that failed to load).</summary>
        private const float FallbackNodeHeight = 1.5f;

        private static readonly int FillId = Shader.PropertyToID("_Fill");
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");
        private static readonly int IconTexId = Shader.PropertyToID("_IconTex");
        private static readonly int IconRectId = Shader.PropertyToID("_IconRect");
        private static readonly int HasIconId = Shader.PropertyToID("_HasIcon");

        /// <summary>The bar's material and the icons, handed over by HarvestBarSkin on the scene.</summary>
        public static Material SharedMaterial;
        public static System.Func<CityBuilder.Resources.ResourceType, Sprite> IconFor;

        private Renderer _renderer;
        private MaterialPropertyBlock _block;

        /// <summary>How far through the current dig the bar shows, 0..1.</summary>
        public float Progress { get; private set; }

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
        public static HarvestProgressBar CreateFor(Transform node, CityBuilder.Resources.ResourceType resource = CityBuilder.Resources.ResourceType.Wood)
        {
            var root = new GameObject("HarvestProgress");
            root.transform.position = node.position + Vector3.up * (MeasuredHeightOf(node) + ClearanceAboveNode);

            var bar = root.AddComponent<HarvestProgressBar>();
            bar.Build(resource);
            root.SetActive(false);
            return bar;
        }

        /// <summary>
        /// How tall this node stands, from its renderers' world bounds. World bounds are the right
        /// tool HERE (unlike the collider sizing in TreesAreaSpawner, which needed local space):
        /// what is wanted is a world-space height to clear, whatever rotation and scale the model
        /// arrived with.
        /// </summary>
        public static float MeasuredHeightOf(Transform node)
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

            Progress = progress;
            if (_renderer == null) return;
            _block.SetFloat(FillId, progress);
            _renderer.SetPropertyBlock(_block);
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

        private void Build(CityBuilder.Resources.ResourceType resource)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Bar";
            // Must go: a stray collider here would be picked up by the click and ground raycasts
            // the rest of the game fires, and this one sits right where the player taps a tree.
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(transform, false);
            quad.transform.localScale = new Vector3(BarWidth, BarHeight, 1f);

            _renderer = quad.GetComponent<Renderer>();
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            // The skin's one material for every bar; without a skin (a bare test scene) a plain
            // unlit gold quad still shows that something is happening.
            _renderer.sharedMaterial = SharedMaterial != null ? SharedMaterial : new Material(RuntimeShaders.Unlit) { color = new Color(0.95f, 0.82f, 0.35f) };

            _block = new MaterialPropertyBlock();
            _block.SetFloat(AspectId, BarWidth / BarHeight);
            var icon = IconFor?.Invoke(resource);
            if (icon != null)
            {
                var texture = icon.texture;
                var rect = icon.textureRect;
                _block.SetTexture(IconTexId, texture);
                _block.SetVector(IconRectId, new Vector4(rect.x / texture.width, rect.y / texture.height, rect.width / texture.width, rect.height / texture.height));
                _block.SetFloat(HasIconId, 1f);
            }
            _block.SetFloat(FillId, 0f);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
