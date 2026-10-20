using System;
using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// How the building being placed is shown. Classic is the original look -- the whole model
    /// tinted flat green or red, which the user found cheap (2026-10-20); the others were built
    /// for them to choose between from photographs.
    /// </summary>
    public enum PlacementLook
    {
        /// <summary>The whole model tinted green or red.</summary>
        Classic,
        /// <summary>Variant 1: the building in its own colours, hovering, with a gold glow; a gold (or wine) plate on the ground under it.</summary>
        OwnColours,
        /// <summary>Variant 2: the building drawn in ink on parchment; an inked outline on the ground.</summary>
        Drawing,
        /// <summary>Variants 1 + 3: own colours, the plate, and every cell marked -- the ones in the way hatched.</summary>
        OwnColoursAndCells,
        /// <summary>Variants 2 + 3: the drawing, an inked outline, and every cell marked.</summary>
        DrawingAndCells,
        /// <summary>Set II (2026-10-20): the drawing standing on a sheet of parchment with torn edges; red ink blots where a cell is in the way.</summary>
        DrawingOnParchment,
    }

    /// <summary>
    /// Everything the placement ghost shows besides the model itself, for the looks other than
    /// Classic: the ghost's own material (CityBuilder/PlacementGhost), what is drawn on the ground
    /// under it, and the little hop it makes from cell to cell. Lives on the ghost; BuildingPlacer
    /// tells it where the building would go and whether it can.
    ///
    /// Cheap by construction: the ground mesh is rebuilt only when the cell, the footprint or the
    /// verdict changes, never per frame, and it is a few dozen quads.
    /// </summary>
    public class PlacementPreview : MonoBehaviour
    {
        /// <summary>The look in use. Classic until the user picks one.</summary>
        public static PlacementLook Look = PlacementLook.Classic;

        public static bool UsesPreview => Look != PlacementLook.Classic;
        private static bool IsDrawing => Look == PlacementLook.Drawing || Look == PlacementLook.DrawingAndCells || Look == PlacementLook.DrawingOnParchment;
        private static bool OnParchment => Look == PlacementLook.DrawingOnParchment;
        private static bool MarksCells => Look == PlacementLook.OwnColoursAndCells || Look == PlacementLook.DrawingAndCells;

        private const float Lift = 0.25f;           // the ghost hovers this far above its cell
        private const float HopSeconds = 0.14f;
        private const float HopHeight = 0.3f;
        private const float GroundHeight = 0.05f;
        private const float RimWidth = 0.18f;
        private const float CellInset = 0.1f;
        private const float CellLine = 0.05f;

        private static readonly Color GoldFill = new Color(1f, 0.84f, 0.36f, 0.28f);
        private static readonly Color GoldRim = new Color(1f, 0.8f, 0.28f, 0.95f);
        private static readonly Color WineFill = new Color(0.55f, 0.1f, 0.1f, 0.3f);
        private static readonly Color WineRim = new Color(0.62f, 0.12f, 0.1f, 0.95f);
        private static readonly Color InkRim = new Color(0.16f, 0.27f, 0.55f, 0.9f);
        private static readonly Color InkWrong = new Color(0.62f, 0.17f, 0.1f, 0.9f);
        private static readonly Color CellFree = new Color(1f, 0.97f, 0.86f, 0.55f);
        private static readonly Color CellBlocked = new Color(0.5f, 0.07f, 0.07f, 0.42f);
        private static readonly Color CellCross = new Color(0.45f, 0.05f, 0.05f, 0.95f);

        private static readonly int ValidId = Shader.PropertyToID("_Valid");
        private static readonly int LookId = Shader.PropertyToID("_Look");
        private static readonly Dictionary<Material, Material> GhostMaterials = new Dictionary<Material, Material>();
        private static Material[] _groundMaterials;

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private MaterialPropertyBlock _block;
        private GameObject _ground;
        private Mesh _groundMesh;
        private MeshRenderer _groundRenderer;

        private Vector3 _target;
        private Vector3 _hopFrom;
        private float _hopTime = -1f;
        private bool _placedOnce;

        private Vector2Int _shownCell;
        private Vector2Int _shownFootprint;
        private bool? _shownValid;
        private readonly List<bool> _shownBlocked = new List<bool>();
        private readonly List<bool> _blocked = new List<bool>();

        /// <summary>Puts a preview on a freshly made ghost: its renderers switch to the ghost material.</summary>
        public static PlacementPreview Attach(GameObject ghost, List<Renderer> renderers)
        {
            var preview = ghost.AddComponent<PlacementPreview>();
            preview._renderers.AddRange(renderers);
            preview._block = new MaterialPropertyBlock();
            foreach (var renderer in renderers)
            {
                var shared = renderer.sharedMaterials;
                var swapped = new Material[shared.Length];
                for (var i = 0; i < shared.Length; i++) swapped[i] = GhostMaterialFor(shared[i]);
                renderer.sharedMaterials = swapped;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            return preview;
        }

        /// <summary>
        /// Where the building would stand and whether it can. `blockedCell` says, per cell of the
        /// footprint, whether that cell itself is in the way -- the reason a whole building cannot
        /// stand may be elsewhere (money, the start zone), and then no cell is hatched.
        /// </summary>
        public void Show(Vector3 centre, Quaternion rotation, Vector2Int origin, Vector2Int footprint, bool valid, Func<Vector2Int, bool> blockedCell)
        {
            MoveTo(centre, rotation);

            if (_shownValid != valid)
            {
                foreach (var renderer in _renderers)
                {
                    if (renderer == null) continue;
                    renderer.GetPropertyBlock(_block);
                    _block.SetFloat(ValidId, valid ? 1f : 0f);
                    _block.SetFloat(LookId, IsDrawing ? 1f : 0f);
                    renderer.SetPropertyBlock(_block);
                }
            }

            _blocked.Clear();
            if (MarksCells || OnParchment)
            {
                for (var z = 0; z < footprint.y; z++)
                {
                    for (var x = 0; x < footprint.x; x++) _blocked.Add(blockedCell(origin + new Vector2Int(x, z)));
                }
            }

            if (_shownValid == valid && _shownCell == origin && _shownFootprint == footprint && SameBlocked()) return;
            _shownValid = valid;
            _shownCell = origin;
            _shownFootprint = footprint;
            _shownBlocked.Clear();
            _shownBlocked.AddRange(_blocked);
            BuildGround(centre.y, origin, footprint, valid);
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

        public void SetVisible(bool visible)
        {
            if (_ground != null && _ground.activeSelf != visible) _ground.SetActive(visible);
        }

        // ------------------------------------------------------------------ the hop

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
        }

        // ------------------------------------------------------------------ the ground

        private enum Part { Fill, Rim, Free, Blocked, Cross }

        private void BuildGround(float y, Vector2Int origin, Vector2Int footprint, bool valid)
        {
            if (_ground == null)
            {
                _ground = new GameObject("PlacementGround");
                _groundMesh = new Mesh { name = "PlacementGround" };
                _ground.AddComponent<MeshFilter>().sharedMesh = _groundMesh;
                _groundRenderer = _ground.AddComponent<MeshRenderer>();
                _groundRenderer.shadowCastingMode = ShadowCastingMode.Off;
                _groundRenderer.receiveShadows = false;
            }

            if (OnParchment)
            {
                ParchmentSheet.Build(_groundMesh, _groundRenderer, y, origin, footprint, valid, _blocked);
                return;
            }

            var grid = GridManager.Instance;
            var size = grid.CellSize;
            var corner = grid.CellToWorld(origin);
            var x0 = corner.x;
            var z0 = corner.z;
            var x1 = x0 + footprint.x * size;
            var z1 = z0 + footprint.y * size;
            var h = y + GroundHeight;

            var vertices = new List<Vector3>();
            var parts = new Dictionary<Part, List<int>>();
            void Quad(Part part, float ax, float az, float bx, float bz, float lift = 0f)
            {
                if (!parts.TryGetValue(part, out var list)) parts[part] = list = new List<int>();
                var i = vertices.Count;
                vertices.Add(new Vector3(ax, h + lift, az));
                vertices.Add(new Vector3(ax, h + lift, bz));
                vertices.Add(new Vector3(bx, h + lift, bz));
                vertices.Add(new Vector3(bx, h + lift, az));
                list.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }
            void Frame(Part part, float ax, float az, float bx, float bz, float w, float lift)
            {
                Quad(part, ax, az, bx, az + w, lift);
                Quad(part, ax, bz - w, bx, bz, lift);
                Quad(part, ax, az + w, ax + w, bz - w, lift);
                Quad(part, bx - w, az + w, bx, bz - w, lift);
            }
            void Bar(Part part, Vector3 a, Vector3 b, float w, float lift)
            {
                if (!parts.TryGetValue(part, out var list)) parts[part] = list = new List<int>();
                var side = Vector3.Cross(Vector3.up, (b - a).normalized) * (w * 0.5f);
                var i = vertices.Count;
                vertices.Add(new Vector3(a.x - side.x, h + lift, a.z - side.z));
                vertices.Add(new Vector3(b.x - side.x, h + lift, b.z - side.z));
                vertices.Add(new Vector3(b.x + side.x, h + lift, b.z + side.z));
                vertices.Add(new Vector3(a.x + side.x, h + lift, a.z + side.z));
                list.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }

            if (!IsDrawing) Quad(Part.Fill, x0, z0, x1, z1);
            Frame(Part.Rim, x0 - RimWidth * 0.5f, z0 - RimWidth * 0.5f, x1 + RimWidth * 0.5f, z1 + RimWidth * 0.5f, RimWidth, 0.004f);

            if (MarksCells)
            {
                var k = 0;
                for (var z = 0; z < footprint.y; z++)
                {
                    for (var x = 0; x < footprint.x; x++, k++)
                    {
                        var ax = x0 + x * size + CellInset;
                        var az = z0 + z * size + CellInset;
                        var bx = x0 + (x + 1) * size - CellInset;
                        var bz = z0 + (z + 1) * size - CellInset;
                        if (_blocked[k])
                        {
                            Quad(Part.Blocked, ax, az, bx, bz, 0.002f);
                            Bar(Part.Cross, new Vector3(ax + 0.12f, 0f, az + 0.12f), new Vector3(bx - 0.12f, 0f, bz - 0.12f), 0.09f, 0.006f);
                            Bar(Part.Cross, new Vector3(ax + 0.12f, 0f, bz - 0.12f), new Vector3(bx - 0.12f, 0f, az + 0.12f), 0.09f, 0.006f);
                        }
                        else
                        {
                            Frame(Part.Free, ax, az, bx, bz, CellLine, 0.002f);
                        }
                    }
                }
            }

            var materials = GroundMaterials();
            var used = new List<Material>();
            _groundMesh.Clear();
            _groundMesh.SetVertices(vertices);
            _groundMesh.subMeshCount = parts.Count;
            var sub = 0;
            foreach (var part in new[] { Part.Fill, Part.Rim, Part.Free, Part.Blocked, Part.Cross })
            {
                if (!parts.TryGetValue(part, out var list)) continue;
                _groundMesh.SetTriangles(list, sub++);
                used.Add(MaterialFor(part, valid, materials));
            }
            _groundMesh.RecalculateBounds();
            _groundRenderer.sharedMaterials = used.ToArray();
        }

        private static Material MaterialFor(Part part, bool valid, Material[] m)
        {
            switch (part)
            {
                case Part.Fill: return valid ? m[0] : m[2];
                case Part.Rim: return IsDrawing ? (valid ? m[4] : m[5]) : (valid ? m[1] : m[3]);
                case Part.Free: return m[6];
                case Part.Blocked: return m[7];
                default: return m[8];
            }
        }

        private static Material[] GroundMaterials()
        {
            if (_groundMaterials != null && _groundMaterials[0] != null) return _groundMaterials;
            _groundMaterials = new[]
            {
                Transparent(GoldFill), Transparent(GoldRim), Transparent(WineFill), Transparent(WineRim),
                Transparent(InkRim), Transparent(InkWrong), Transparent(CellFree), Transparent(CellBlocked), Transparent(CellCross),
            };
            return _groundMaterials;
        }

        private static Material Transparent(Color color)
        {
            var material = new Material(RuntimeShaders.Unlit) { color = color };
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        private static Material GhostMaterialFor(Material original)
        {
            if (original == null) return null;
            if (GhostMaterials.TryGetValue(original, out var ghost) && ghost != null) return ghost;
            ghost = new Material(RuntimeShaders.PlacementGhost) { name = original.name + " (ghost)" };
            if (original.HasProperty("_BaseMap")) ghost.SetTexture("_BaseMap", original.GetTexture("_BaseMap"));
            if (original.HasProperty("_BaseColor")) ghost.SetColor("_BaseColor", original.GetColor("_BaseColor"));
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

        private static Mesh Quad()
        {
            if (_quad != null) return _quad;
            _quad = new Mesh { name = "DustMote" };
            _quad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(0.5f, -0.5f, 0f) };
            _quad.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            _quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            _quad.RecalculateBounds();
            return _quad;
        }

        private static Material Material()
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
