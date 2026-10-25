using CityBuilder.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Invasion
{
    /// <summary>The salute's bursts (InvasionNewYear.Salute): coloured sparks flying out and falling, gone in a second and a half.</summary>
    public class Fireworks : MonoBehaviour
    {
        private const int Sparks = 26;
        private const float Life = 1.8f;
        private const float SparkSize = 0.3f;
        private static readonly Color[] Colours =
        {
            new Color(1f, 0.3f, 0.3f), new Color(1f, 0.85f, 0.3f), new Color(0.4f, 0.7f, 1f), new Color(0.5f, 1f, 0.5f), new Color(1f, 0.5f, 1f),
        };
        private static Mesh _cube;

        private Vector3[] _velocity;
        private Transform[] _spark;
        private float _clock;

        /// <summary>`bursts` bursts in the sky over `centre`, scattered a little and a little apart in time.</summary>
        public static void Over(Vector3 centre, int bursts)
        {
            for (var i = 0; i < bursts; i++)
            {
                var at = centre + new Vector3(Random.Range(-7f, 7f), Random.Range(5f, 8f), Random.Range(-7f, 7f));
                var go = new GameObject("Firework");
                go.transform.position = at;
                var firework = go.AddComponent<Fireworks>();
                firework._clock = -i * 0.25f;
                firework.Build(Colours[Random.Range(0, Colours.Length)]);
            }
        }

        private void Build(Color colour)
        {
            if (_cube == null)
            {
                var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _cube = temp.GetComponent<MeshFilter>().sharedMesh;
                Destroy(temp);
            }
            var material = new Material(RuntimeShaders.Unlit) { color = colour };
            _velocity = new Vector3[Sparks];
            _spark = new Transform[Sparks];
            for (var i = 0; i < Sparks; i++)
            {
                var spark = new GameObject("Spark", typeof(MeshFilter), typeof(MeshRenderer));
                spark.GetComponent<MeshFilter>().sharedMesh = _cube;
                var renderer = spark.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                spark.transform.SetParent(transform, false);
                spark.transform.localScale = Vector3.one * SparkSize;
                spark.SetActive(false);
                _spark[i] = spark.transform;
                _velocity[i] = Random.onUnitSphere * Random.Range(3.5f, 5.5f);
            }
        }

        private void Update()
        {
            _clock += Time.deltaTime;
            if (_clock < 0f) return;
            if (_clock > Life)
            {
                Destroy(gameObject);
                return;
            }
            var fade = 1f - _clock / Life;
            for (var i = 0; i < _spark.Length; i++)
            {
                if (!_spark[i].gameObject.activeSelf) _spark[i].gameObject.SetActive(true);
                _velocity[i] += Vector3.down * 4f * Time.deltaTime;
                _spark[i].localPosition += _velocity[i] * Time.deltaTime;
                _spark[i].localScale = Vector3.one * SparkSize * fade;
            }
        }
    }
}
