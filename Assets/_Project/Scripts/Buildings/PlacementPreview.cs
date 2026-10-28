using System;
using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// How the building being placed is shown (the user's pick, 2026-10-20, after the flat
    /// green/red tint was found cheap and six looks were photographed): an ink drawing of the
    /// building (CityBuilder/PlacementGhost) hovering over a pen sketch of its plot (SketchMarks).
    /// When it is picked up the pen sketches the plot side by side and the drawing rises out of the
    /// ground behind a line of fresh ink; on every move to a new cell the drawing hops there and
    /// the plot is sketched again, quicker. Where it cannot stand the ink turns red -- only over
    /// the cells in the way, which are hatched, or all of it when the reason is elsewhere (money,
    /// the start zone). Lives on the ghost; BuildingPlacer says where the building would go and
    /// whether it can.
    ///
    /// Cheap by construction: the ground mesh is rebuilt when the cell, the footprint or the
    /// verdict changes and every frame only for the fraction of a second the pen is drawing; it is
    /// a few dozen strips.
    /// </summary>
    public class PlacementPreview : MonoBehaviour
    {
        private const float Lift = 0.25f;           // the drawing hovers this far above its cell
        private const float HopSeconds = 0.14f;
        private const float HopHeight = 0.3f;
        private const float SketchSeconds = 0.45f;  // the plot, when the building is picked up
        private const float ResketchSeconds = 0.22f; // the plot again, on each new cell
        private const float DrawingDelay = 0.12f;
        private const float DrawingSeconds = 0.5f;

        private static readonly int UseMaskId = Shader.PropertyToID("_UseMask");
        private static readonly int ValidId = Shader.PropertyToID("_Valid");
        private static readonly int BlockMaskId = Shader.PropertyToID("_BlockMask");
        private static readonly int BlockRectId = Shader.PropertyToID("_BlockRect");
        private static readonly int RevealYId = Shader.PropertyToID("_RevealY");
        private static readonly Dictionary<Material, Material> GhostMaterials = new Dictionary<Material, Material>();

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private MaterialPropertyBlock _block;
        private GameObject _ground;
        private Mesh _groundMesh;
        private MeshRenderer _groundRenderer;
        private Texture2D _mask;

        private Vector3 _target;
        private Vector3 _hopFrom;
        private float _hopTime = -1f;
        private bool _placedOnce;

        private float _sketchStart = -1f;
        private float _sketchLength;
        private float _drawingStart = -1f;
        private float _modelHeight = 3f;
        private float _groundY;
        private bool _sketchedOnce;

        private Vector2Int _shownCell;
        private Vector2Int _shownFootprint;
        private bool? _shownValid;
        private readonly List<bool> _shownBlocked = new List<bool>();
        private readonly List<bool> _blocked = new List<bool>();

        /// <summary>Puts a preview on a freshly made ghost: its renderers switch to the ghost material, drawn in by the pen.</summary>
        public static PlacementPreview Attach(GameObject ghost, List<Renderer> renderers)
        {
            var preview = ghost.AddComponent<PlacementPreview>();
            preview._renderers.AddRange(renderers);
            preview._block = new MaterialPropertyBlock();

            var bounds = new Bounds();
            var first = true;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (first) bounds = r.bounds;
                else bounds.Encapsulate(r.bounds);
                first = false;
            }
            preview._modelHeight = Mathf.Max(0.5f, bounds.max.y - ghost.transform.position.y);

            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                var shared = renderer.sharedMaterials;
                var swapped = new Material[shared.Length];
                for (var i = 0; i < shared.Length; i++) swapped[i] = GhostMaterialFor(shared[i]);
                renderer.sharedMaterials = swapped;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                // the pen has not started yet: nothing of the drawing is there
                renderer.GetPropertyBlock(preview._block);
                preview._block.SetFloat(RevealYId, -100000f);
                renderer.SetPropertyBlock(preview._block);
            }
            return preview;
        }

        /// <summary>
        /// Where the building would stand and whether it can. `blockedCell` says, per cell of the
        /// footprint, whether that cell itself is in the way.
        /// </summary>
        public void Show(Vector3 centre, Quaternion rotation, Vector2Int origin, Vector2Int footprint, bool valid, Func<Vector2Int, bool> blockedCell)
        {
            MoveTo(centre, rotation);

            _blocked.Clear();
            for (var z = 0; z < footprint.y; z++)
            {
                for (var x = 0; x < footprint.x; x++) _blocked.Add(blockedCell(origin + new Vector2Int(x, z)));
            }

            if (_shownValid == valid && _shownCell == origin && _shownFootprint == footprint && SameBlocked()) return;
            if (!_sketchedOnce || _shownCell != origin || _shownFootprint != footprint)
            {
                var now = Time.unscaledTime;
                _sketchStart = now;
                _sketchLength = _sketchedOnce ? ResketchSeconds : SketchSeconds;
                if (!_sketchedOnce) _drawingStart = now + DrawingDelay;
                _sketchedOnce = true;
            }
            _groundY = centre.y;
            _shownValid = valid;
            _shownCell = origin;
            _shownFootprint = footprint;
            _shownBlocked.Clear();
            _shownBlocked.AddRange(_blocked);
            PaintMask(origin, footprint, valid);
            EnsureGround();
            SketchMarks.Build(_groundMesh, _groundRenderer, _groundY, origin, footprint, valid, _shownBlocked, SketchReveal());
        }

        public void SetVisible(bool visible)
        {
            if (_ground != null && _ground.activeSelf != visible) _ground.SetActive(visible);
        }

        private bool SameBlocked()
        {
            if (_blocked.Count != _shownBlocked.Count) return false;
            for (var i = 0; i < _blocked.Count; i++)
            {
                if (_blocked[i] != _shownBlocked[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// Tells the drawing whether it can stand and which of its cells are in the way, so only the
        /// part standing on them turns red. With nothing in the way but the building still refused,
        /// the whole drawing turns red.
        /// </summary>
        private void PaintMask(Vector2Int origin, Vector2Int footprint, bool valid)
        {
            var any = false;
            foreach (var b in _blocked) any |= b;
            if (_mask == null || _mask.width != footprint.x || _mask.height != footprint.y)
            {
                if (_mask != null) Destroy(_mask);
                _mask = new Texture2D(footprint.x, footprint.y, TextureFormat.R8, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            }
            var k = 0;
            for (var z = 0; z < footprint.y; z++)
            {
                for (var x = 0; x < footprint.x; x++, k++) _mask.SetPixel(x, z, _blocked[k] ? Color.white : Color.black);
            }
            _mask.Apply(false);

            var grid = GridManager.Instance;
            var corner = grid.CellToWorld(origin);
            var size = grid.CellSize;
            var rect = new Vector4(corner.x, corner.z, 1f / (footprint.x * size), 1f / (footprint.y * size));
            foreach (var renderer in _renderers)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(_block);
                _block.SetFloat(ValidId, valid ? 1f : 0f);
                _block.SetTexture(BlockMaskId, _mask);
                _block.SetVector(BlockRectId, rect);
                _block.SetFloat(UseMaskId, !valid && any ? 1f : 0f);
                renderer.SetPropertyBlock(_block);
            }
        }

        private void EnsureGround()
        {
            if (_ground != null) return;
            _ground = new GameObject("PlacementGround");
            _groundMesh = new Mesh { name = "PlacementGround" };
            _groundMesh.MarkDynamic();
            _ground.AddComponent<MeshFilter>().sharedMesh = _groundMesh;
            _groundRenderer = _ground.AddComponent<MeshRenderer>();
            _groundRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _groundRenderer.receiveShadows = false;
        }

        // ------------------------------------------------------------------ motion

        private void MoveTo(Vector3 centre, Quaternion rotation)
        {
            transform.rotation = rotation;
            if (!_placedOnce)
            {
                _placedOnce = true;
                _target = centre;
                transform.position = centre + Vector3.up * Lift;
                return;
            }
            if (centre == _target) return;
            _hopFrom = transform.position - Vector3.up * Lift;
            _target = centre;
            _hopTime = 0f;
        }

        private float SketchReveal() => _sketchStart < 0f ? 1f : Mathf.Clamp01((Time.unscaledTime - _sketchStart) / _sketchLength);

        private void Update()
        {
            // the plot, stroke by stroke
            if (_sketchStart >= 0f && _shownValid.HasValue && _groundMesh != null)
            {
                var reveal = SketchReveal();
                SketchMarks.Build(_groundMesh, _groundRenderer, _groundY, _shownCell, _shownFootprint, _shownValid.Value, _shownBlocked, reveal);
                if (reveal >= 1f) _sketchStart = -1f;
            }

            // the drawing, rising from the ground behind a line of fresh ink
            if (_drawingStart >= 0f)
            {
                var t = Mathf.Clamp01((Time.unscaledTime - _drawingStart) / DrawingSeconds);
                var y = t >= 1f ? 100000f : _groundY + Lift + Mathf.SmoothStep(-0.1f, _modelHeight + 0.3f, t);
                foreach (var renderer in _renderers)
                {
                    if (renderer == null) continue;
                    renderer.GetPropertyBlock(_block);
                    _block.SetFloat(RevealYId, y);
                    renderer.SetPropertyBlock(_block);
                }
                if (t >= 1f) _drawingStart = -1f;
            }
        }

        private void LateUpdate()
        {
            if (_hopTime < 0f) return;
            _hopTime += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(_hopTime / HopSeconds);
            var eased = 1f - (1f - t) * (1f - t);
            transform.position = Vector3.Lerp(_hopFrom, _target, eased) + Vector3.up * (Lift + HopHeight * Mathf.Sin(Mathf.PI * t));
            if (t >= 1f) _hopTime = -1f;
        }

        private void OnDestroy()
        {
            if (_ground != null) Destroy(_ground);
            if (_groundMesh != null) Destroy(_groundMesh);
            if (_mask != null) Destroy(_mask);
        }

        private static Material GhostMaterialFor(Material original)
        {
            if (original == null) return null;
            if (GhostMaterials.TryGetValue(original, out var ghost) && ghost != null) return ghost;
            ghost = new Material(RuntimeShaders.PlacementGhost) { name = original.name + " (ghost)" };
            GhostMaterials[original] = ghost;
            return ghost;
        }
    }

    /// <summary>
    /// A puff of dust where a building has just been placed: a ring of soft billboards that drift
    /// out and up, swell and fade in under a second, then the whole thing removes itself. Built
    /// from quads rather than a ParticleSystem so it needs no particle shader in the build.
    /// </summary>
    public class DustPuff : MonoBehaviour
    {
        private const float Seconds = 0.75f;
        private static readonly Color Dust = new Color(0.86f, 0.78f, 0.62f, 0.75f);
        private static Material _material;
        private static Mesh _quad;

        private readonly List<(Transform t, Vector3 velocity, float size)> _motes = new List<(Transform, Vector3, float)>();
        private MaterialPropertyBlock _block;
        private float _age;

        public static void Spawn(Vector3 centre, Vector2 footprintMetres)
        {
            var go = new GameObject("DustPuff");
            go.transform.position = centre;
            var puff = go.AddComponent<DustPuff>();
            puff._block = new MaterialPropertyBlock();
            var halfX = footprintMetres.x * 0.5f;
            var halfZ = footprintMetres.y * 0.5f;
            var count = Mathf.Clamp(Mathf.RoundToInt(4f * (footprintMetres.x + footprintMetres.y)), 8, 28);
            for (var i = 0; i < count; i++)
            {
                // round the footprint's edge, pushed outward
                var angle = i * Mathf.PI * 2f / count + UnityEngine.Random.Range(-0.15f, 0.15f);
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var scale = 1f / Mathf.Max(Mathf.Abs(dir.x) / halfX, Mathf.Abs(dir.z) / halfZ);
                var start = dir * scale;
                var mote = new GameObject("Mote");
                mote.transform.SetParent(go.transform, false);
                mote.transform.localPosition = start + Vector3.up * 0.15f;
                mote.AddComponent<MeshFilter>().sharedMesh = Quad();
                var renderer = mote.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = Material();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                var velocity = dir * UnityEngine.Random.Range(0.9f, 1.6f) + Vector3.up * UnityEngine.Random.Range(0.3f, 0.8f);
                puff._motes.Add((mote.transform, velocity, UnityEngine.Random.Range(0.45f, 0.8f)));
            }
        }

        private void Update()
        {
            _age += Time.unscaledDeltaTime;
            var t = _age / Seconds;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            var camera = UnityEngine.Camera.main;
            var slow = 1f - t * 0.7f;
            var colour = Dust;
            colour.a *= (1f - t) * (1f - t);
            foreach (var (mote, velocity, size) in _motes)
            {
                mote.localPosition += velocity * (Time.unscaledDeltaTime * slow);
                mote.localScale = Vector3.one * (size * (1f + 1.6f * t));
                if (camera != null) mote.rotation = camera.transform.rotation;
                var renderer = mote.GetComponent<MeshRenderer>();
                renderer.GetPropertyBlock(_block);
                _block.SetColor("_BaseColor", colour);
                renderer.SetPropertyBlock(_block);
            }
        }

        internal static Mesh Quad()
        {
            if (_quad != null) return _quad;
            _quad = new Mesh { name = "DustMote" };
            _quad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(0.5f, -0.5f, 0f) };
            _quad.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            _quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            _quad.RecalculateBounds();
            return _quad;
        }

        internal static Material Material()
        {
            if (_material != null) return _material;
            // a soft round blot, made once
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f)) / (size * 0.5f);
                    var a = Mathf.Clamp01(1f - d);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }
            texture.Apply();
            _material = new Material(RuntimeShaders.Unlit) { color = Dust, mainTexture = texture };
            _material.SetFloat("_Surface", 1f);
            _material.SetFloat("_Blend", 0f);
            _material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            _material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            _material.SetFloat("_ZWrite", 0f);
            _material.SetFloat("_Cull", (float)CullMode.Off);
            _material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _material.renderQueue = (int)RenderQueue.Transparent;
            return _material;
        }
    }
}
