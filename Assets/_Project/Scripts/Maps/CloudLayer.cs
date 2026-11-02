using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Maps
{
    /// <summary>
    /// Clouds over the land (the user, 2026-11-02: «тени облаков ... на максимальном отдалении
    /// игрок будет видеть и сами облака, иначе это будет просто неподтверждённая тень»). One
    /// drifting field (Shaders/CloudField.hlsl) both darkens the ground under it -- every
    /// BuildingDetail surface: the land, the buildings, the trees -- and is drawn as a soft sheet
    /// Height metres up, under the camera, fading in only over the last of the zoom (the camera
    /// stands 4..19 m over the ground; the clouds at 10 are behind and above it when it is low).
    /// The field drifts the wind's way (BuildingDetail's WindOffset). Not in a dungeon. Its
    /// globals are cleared when it goes, so nothing else is ever shaded by a stale sky.
    /// </summary>
    public class CloudLayer : MonoBehaviour
    {
        private const float Height = 10f;
        private const float TileMeters = 70f;
        private const float Cover = 0.56f;
        private const float Softness = 0.16f;
        private const float ShadowStrength = 0.28f;
        private const float DriftMetersPerSecond = 1.1f;
        private static readonly Vector2 WindDirection = new Vector2(0.94f, 0.34f);
        // the camera's height over the ground at which the clouds start to show, and are whole
        private const float ShowFrom = 15.5f;
        private const float ShowAt = 18.5f;
        private const float SheetMeters = 140f;

        private static readonly int NoiseId = Shader.PropertyToID("_CloudNoise");
        private static readonly int ParamsId = Shader.PropertyToID("_CloudParams");
        private static readonly int PlaceId = Shader.PropertyToID("_CloudPlace");
        private static readonly int VisibleId = Shader.PropertyToID("_Visible");

        private Vector2 _drift;
        private MeshRenderer _sheet;
        private Material _material;
        private Camera _camera;
        private float _shown = -1f;

        public static CloudLayer Begin(Transform parent, Texture noise, Material sheet)
        {
            if (noise == null) return null;
            var go = new GameObject("Clouds", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            var layer = go.AddComponent<CloudLayer>();
            layer.Dress(noise, sheet);
            return layer;
        }

        private void Dress(Texture noise, Material sheet)
        {
            Shader.SetGlobalTexture(NoiseId, noise);
            Shader.SetGlobalVector(ParamsId, new Vector4(1f / TileMeters, Cover, Softness, ShadowStrength));
            Shader.SetGlobalVector(PlaceId, new Vector4(0f, 0f, Height, 0f));
            GetComponent<MeshFilter>().sharedMesh = Sheet();
            _sheet = GetComponent<MeshRenderer>();
            _sheet.shadowCastingMode = ShadowCastingMode.Off;
            _sheet.receiveShadows = false;
            if (sheet != null)
            {
                _material = new Material(sheet) { name = "Clouds" };
                _sheet.sharedMaterial = _material;
            }
            _sheet.enabled = false;
        }

        private static Mesh Sheet()
        {
            var h = SheetMeters * 0.5f;
            var mesh = new Mesh
            {
                name = "CloudSheet",
                vertices = new[] { new Vector3(-h, 0f, -h), new Vector3(-h, 0f, h), new Vector3(h, 0f, h), new Vector3(h, 0f, -h) },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
            };
            mesh.RecalculateBounds();
            return mesh;
        }

        private void Update()
        {
            _drift += WindDirection * (DriftMetersPerSecond * Time.deltaTime);
            Shader.SetGlobalVector(PlaceId, new Vector4(_drift.x, _drift.y, Height, 0f));

            if (_camera == null) _camera = Camera.main;
            if (_camera == null || _material == null) return;
            var at = _camera.transform.position;
            var ground = Grid.GridManager.Instance != null ? Grid.GridManager.Instance.GroundHeight : 0f;
            var shown = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ShowFrom, ShowAt, at.y - ground));
            if (!Mathf.Approximately(shown, _shown))
            {
                _shown = shown;
                _material.SetFloat(VisibleId, shown);
                _sheet.enabled = shown > 0.001f;
            }
            if (!_sheet.enabled) return;
            // the sheet under the camera, a little ahead of it: the view looks forward and down
            var ahead = _camera.transform.forward;
            ahead.y = 0f;
            transform.position = new Vector3(at.x, Height, at.z) + ahead.normalized * (SheetMeters * 0.25f);
        }

        private void OnDestroy()
        {
            // no stale sky over the next scene
            Shader.SetGlobalVector(ParamsId, Vector4.zero);
            if (_material != null) Destroy(_material);
        }
    }
}
