using CityBuilder.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Invasion
{
    /// <summary>
    /// Snow falling over the New Year map (InvasionNewYear), on the camera: flakes in three layers
    /// of one mesh each round where the camera looks, each layer falling and wrapping back to the
    /// top. One layer in calm weather; all three, faster and driven sideways, in a blizzard. Three
    /// draws and a few thousand triangles at most -- no particle shader to ship in the build.
    /// </summary>
    public class SnowFall : MonoBehaviour
    {
        private const int FlakesPerLayer = 450;
        private const float BoxHalf = 28f;
        // kept under the camera: a flake at its lens is a white sheet across the screen
        private const float BoxHeight = 8f;
        private const float FlakeSize = 0.07f;
        private const float FallSpeed = 1.6f;

        private readonly Transform[] _layers = new Transform[3];
        private readonly float[] _drop = new float[3];
        private Material _material;

        /// <summary>A blizzard: all the layers, faster, slanting.</summary>
        public bool Blizzard { get; set; }

        private void Start()
        {
            _material = new Material(RuntimeShaders.Unlit) { color = new Color(0.97f, 0.98f, 1f) };
            for (var i = 0; i < _layers.Length; i++)
            {
                var layer = new GameObject($"SnowLayer{i}", typeof(MeshFilter), typeof(MeshRenderer));
                layer.GetComponent<MeshFilter>().sharedMesh = BuildFlakes(1000 + i * 77);
                var renderer = layer.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = _material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                _layers[i] = layer.transform;
                _drop[i] = BoxHeight * i / _layers.Length;
            }
        }

        private void OnDestroy()
        {
            foreach (var layer in _layers)
            {
                if (layer != null) Destroy(layer.gameObject);
            }
        }

        private static Mesh BuildFlakes(int seed)
        {
            var random = new System.Random(seed);
            var vertices = new Vector3[FlakesPerLayer * 4];
            var triangles = new int[FlakesPerLayer * 12];
            for (var f = 0; f < FlakesPerLayer; f++)
            {
                var c = new Vector3((float)(random.NextDouble() * 2 - 1) * BoxHalf, (float)random.NextDouble() * BoxHeight,
                    (float)(random.NextDouble() * 2 - 1) * BoxHalf);
                var s = FlakeSize * (0.6f + (float)random.NextDouble() * 0.8f);
                // a little tetrahedron: seen from any side, no billboarding to keep up with the camera
                var v = f * 4;
                vertices[v] = c + new Vector3(0f, s, 0f);
                vertices[v + 1] = c + new Vector3(-s, -s * 0.5f, -s * 0.6f);
                vertices[v + 2] = c + new Vector3(s, -s * 0.5f, -s * 0.6f);
                vertices[v + 3] = c + new Vector3(0f, -s * 0.5f, s);
                var t = f * 12;
                int[] faces = { 0, 2, 1, 0, 3, 2, 0, 1, 3, 1, 2, 3 };
                for (var k = 0; k < 12; k++) triangles[t + k] = v + faces[k];
            }
            var mesh = new Mesh { name = "SnowFlakes", vertices = vertices, triangles = triangles };
            mesh.bounds = new Bounds(new Vector3(0f, BoxHeight * 0.5f, 0f), new Vector3(BoxHalf * 2f, BoxHeight * 2f, BoxHalf * 2f));
            return mesh;
        }

        private void LateUpdate()
        {
            // round where the camera looks: its ray down to the ground
            var forward = transform.forward;
            var focus = transform.position;
            if (forward.y < -0.05f) focus += forward * (-transform.position.y / forward.y);
            var speed = Blizzard ? FallSpeed * 2.4f : FallSpeed;
            var drift = Blizzard ? new Vector3(3.5f, 0f, 1.2f) : Vector3.zero;
            for (var i = 0; i < _layers.Length; i++)
            {
                var layer = _layers[i];
                if (layer == null) continue;
                var shown = i == 0 || Blizzard;
                if (layer.gameObject.activeSelf != shown) layer.gameObject.SetActive(shown);
                if (!shown) continue;
                _drop[i] = Mathf.Repeat(_drop[i] + speed * (1f + 0.2f * i) * Time.deltaTime, BoxHeight);
                var sway = new Vector3(Mathf.Sin(Time.time * 0.4f + i) * 0.6f, 0f, 0f) + drift * (_drop[i] / BoxHeight);
                layer.position = new Vector3(focus.x, -_drop[i], focus.z) + sway;
            }
        }
    }
}
