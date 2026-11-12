using CityBuilder.CameraControl;
using CityBuilder.Combat;
using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 2's view and edge (the user's first playtest, 2026-11-12): the camera at mission 1's
    /// distance, fixed, always on Ардан; past the fields and the fords it is dark (the dungeon's darkness
    /// shader over a mask of the lit rectangle m2_lit_*), and he cannot go there -- the player is kept on
    /// the level. The orcs come out of that dark.
    /// </summary>
    public partial class CityDefenceMode
    {
        private Texture2D _darkMask;
        private MeshRenderer _darkRenderer;
        private Camera _darkCamera;

        /// <summary>The darkness is the main camera's only: drawn by the minimap's camera too (no depth texture
        /// there) it turned the whole minimap black (the user's second playtest, 2026-11-12).</summary>
        private void OnCameraRendering(UnityEngine.Rendering.ScriptableRenderContext context, Camera cam)
        {
            if (_darkRenderer != null) _darkRenderer.enabled = cam == _darkCamera;
        }

        private void ForgetDarkness()
        {
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnCameraRendering;
        }

        private void SetUpView()
        {
            var rig = RTSCameraController.Instance;
            if (rig != null) rig.LockView(B.cameraDistance, B.cameraPitch);
        }

        /// <summary>The lit rectangle in world x/z, shrunk by `inset` cells.</summary>
        private Rect LitArea(float inset)
        {
            var b = B;
            var a = World(b.litX0 + inset, b.litY1 - inset);   // south-west (the plan's y runs south)
            var c = World(b.litX1 - inset, b.litY0 + inset);   // north-east
            return Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.z, c.z), Mathf.Max(a.x, c.x), Mathf.Max(a.z, c.z));
        }

        private void Darkness()
        {
            var cam = Camera.main;
            var raids = OrcRaidManager.Instance;
            var source = raids != null ? raids.DungeonDarkMaterial : null;
            if (cam == null || source == null) return;
            var lit = LitArea(0f);
            var soft = Mathf.Max(0.5f, B.darkSoft);
            // the mask over the lit rectangle and a band of dark round it
            var margin = soft * 3f + 8f;
            var min = new Vector2(lit.xMin - margin, lit.yMin - margin);
            var size = new Vector2(lit.width + margin * 2f, lit.height + margin * 2f);
            const float texel = 1f;
            var w = Mathf.CeilToInt(size.x / texel);
            var h = Mathf.CeilToInt(size.y / texel);
            _darkMask = new Texture2D(w, h, TextureFormat.R8, false, true) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "M2DarkMask" };
            var px = new byte[w * h];
            for (var j = 0; j < h; j++)
                for (var i = 0; i < w; i++)
                {
                    var p = min + new Vector2((i + 0.5f) * texel, (j + 0.5f) * texel);
                    // how far outside the lit rectangle (negative inside): lit fading to dark over `soft`
                    var dx = Mathf.Max(lit.xMin - p.x, p.x - lit.xMax);
                    var dz = Mathf.Max(lit.yMin - p.y, p.y - lit.yMax);
                    var outside = dx > 0f && dz > 0f ? Mathf.Sqrt(dx * dx + dz * dz) : Mathf.Max(dx, dz);
                    var k = Mathf.Clamp01(0.5f - outside / soft);
                    px[j * w + i] = (byte)Mathf.RoundToInt(k * 255f);
                }
            _darkMask.SetPixelData(px, 0);
            _darkMask.Apply(false);
            UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam).requiresDepthTexture = true;
            var material = new Material(source) { name = "M2Dark (runtime)" };
            material.SetTexture("_DarkMask", _darkMask);
            material.SetVector("_DarkRect", new Vector4(min.x, min.y, 1f / (w * texel), 1f / (h * texel)));
            material.SetFloat("_Strength", 1f);
            var quad = new GameObject("M2Darkness", typeof(MeshFilter), typeof(MeshRenderer));
            quad.transform.SetParent(cam.transform, false);
            quad.transform.localPosition = Vector3.forward * (cam.nearClipPlane + 0.5f);
            var mesh = new Mesh
            {
                name = "M2DarkQuad",
                vertices = new[] { new Vector3(-1f, -1f, 0f), new Vector3(1f, -1f, 0f), new Vector3(1f, 1f, 0f), new Vector3(-1f, 1f, 0f) },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
                // the shader puts it over the whole screen whatever the camera does: never cull it
                bounds = new Bounds(Vector3.zero, Vector3.one * 10000f),
            };
            quad.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = quad.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            _darkRenderer = r;
            _darkCamera = cam;
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += OnCameraRendering;
        }
    }
}
