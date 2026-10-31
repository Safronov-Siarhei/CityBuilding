using UnityEngine;

namespace CityBuilder.Maps.Archipelago
{
    /// <summary>
    /// A fertile patch's lit edge (FertileLand.Flash): it pulses gold to pale for a few seconds,
    /// then is gone along with its mesh and material.
    /// </summary>
    public class FertileFlash : MonoBehaviour
    {
        public static readonly Color Gold = new Color(1f, 0.82f, 0.25f, 1f);
        private static readonly Color Pale = new Color(1f, 0.97f, 0.78f, 1f);
        private const float Seconds = 4f;

        private float _age;
        private Material _material;
        private Mesh _mesh;

        private void Start()
        {
            var renderer = GetComponent<MeshRenderer>();
            _material = renderer != null ? renderer.sharedMaterial : null;
            var filter = GetComponent<MeshFilter>();
            _mesh = filter != null ? filter.sharedMesh : null;
        }

        private void Update()
        {
            _age += Time.unscaledDeltaTime;
            if (_age >= Seconds)
            {
                Destroy(gameObject);
                return;
            }
            if (_material != null) _material.color = Color.Lerp(Gold, Pale, 0.5f + 0.5f * Mathf.Sin(_age * 6f));
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
            if (_mesh != null) Destroy(_mesh);
        }
    }
}
