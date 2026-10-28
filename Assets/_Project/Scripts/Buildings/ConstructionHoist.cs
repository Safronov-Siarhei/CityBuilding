using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// The hoist on an upgrade's scaffolding (the user's pick В, 2026-10-28): a pallet of stones
    /// winched up from the ground to the top on its rope, a moment there, lowered empty-handed again,
    /// over and over while the work goes on. Game time, so it stops with the game paused.
    /// </summary>
    public class ConstructionHoist : MonoBehaviour
    {
        private const float CycleSeconds = 4.2f;
        private static readonly Color StoneColour = new Color(0.63f, 0.6f, 0.55f);
        private static Material _stone;

        private Transform _pallet;
        private Transform _stones;
        private Transform _rope;
        private Vector3 _hang;
        private float _low, _high;
        private float _phase;

        /// <summary>Hung from `hang` (the site's local space, on the ground) under a drum at `drumY`; its meshes join `meshes`, which the site frees.</summary>
        public static ConstructionHoist Raise(Transform site, Vector3 hang, float drumY, Material wood, List<Mesh> meshes)
        {
            var go = new GameObject("Hoist");
            go.transform.SetParent(site, false);
            var hoist = go.AddComponent<ConstructionHoist>();
            hoist._hang = hang;
            hoist._low = 0.06f;
            hoist._high = Mathf.Max(0.3f, drumY - 0.35f);

            var pallet = new ConstructionLook.MeshBits();
            pallet.Box(new Vector3(0f, 0.025f, 0f), new Vector3(0.34f, 0.05f, 0.24f), Quaternion.identity);
            pallet.Box(new Vector3(-0.15f, 0.2f, 0f), new Vector3(0.02f, 0.3f, 0.02f), Quaternion.Euler(0f, 0f, -25f));
            pallet.Box(new Vector3(0.15f, 0.2f, 0f), new Vector3(0.02f, 0.3f, 0.02f), Quaternion.Euler(0f, 0f, 25f));
            hoist._pallet = hoist.Part("Pallet", pallet, wood, meshes);
            var stones = new ConstructionLook.MeshBits();
            stones.Box(new Vector3(-0.07f, 0.1f, 0f), new Vector3(0.13f, 0.1f, 0.13f), Quaternion.Euler(0f, 15f, 0f));
            stones.Box(new Vector3(0.07f, 0.09f, 0.02f), new Vector3(0.12f, 0.08f, 0.12f), Quaternion.Euler(0f, 40f, 0f));
            stones.Box(new Vector3(0f, 0.17f, -0.01f), new Vector3(0.1f, 0.07f, 0.1f), Quaternion.Euler(0f, 70f, 0f));
            hoist._stones = hoist.Part("Stones", stones, StoneMaterial(), meshes);
            hoist._stones.SetParent(hoist._pallet, false);
            var rope = new ConstructionLook.MeshBits();
            // a unit length hanging down from its top, stretched to the drum each frame
            rope.Box(new Vector3(0f, -0.5f, 0f), new Vector3(0.02f, 1f, 0.02f), Quaternion.identity);
            hoist._rope = hoist.Part("Rope", rope, wood, meshes);
            hoist._rope.localPosition = new Vector3(hang.x, drumY, hang.z);
            hoist._phase = Random.Range(0f, CycleSeconds);
            hoist.Place();
            return hoist;
        }

        private Transform Part(string name, ConstructionLook.MeshBits bits, Material material, List<Mesh> meshes)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            var mesh = bits.ToMesh(name);
            meshes.Add(mesh);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return go.transform;
        }

        private void Update()
        {
            _phase = Mathf.Repeat(_phase + Time.deltaTime, CycleSeconds);
            Place();
        }

        /// <summary>Up loaded (45%), a pause on top as it is unloaded (15%), down empty (30%), a pause to load (10%).</summary>
        private void Place()
        {
            var t = _phase / CycleSeconds;
            float lift;
            bool loaded;
            if (t < 0.45f) { lift = Mathf.SmoothStep(0f, 1f, t / 0.45f); loaded = true; }
            else if (t < 0.6f) { lift = 1f; loaded = t < 0.52f; }
            else if (t < 0.9f) { lift = 1f - Mathf.SmoothStep(0f, 1f, (t - 0.6f) / 0.3f); loaded = false; }
            else { lift = 0f; loaded = t > 0.95f; }
            var y = Mathf.Lerp(_low, _high, lift);
            // it swings a little on the rope as it goes
            var sway = Mathf.Sin(_phase * 3.1f) * 3f * (lift > 0f && lift < 1f ? 1f : 0.3f);
            _pallet.localPosition = new Vector3(_hang.x, y, _hang.z);
            _pallet.localRotation = Quaternion.Euler(sway, 0f, sway * 0.5f);
            if (_stones.gameObject.activeSelf != loaded) _stones.gameObject.SetActive(loaded);
            var length = Mathf.Max(0.05f, _rope.localPosition.y - (y + 0.33f));
            _rope.localScale = new Vector3(1f, length, 1f);
        }

        private static Material StoneMaterial() => _stone != null ? _stone : _stone = new Material(RuntimeShaders.Lit) { name = "HoistStone", color = StoneColour };
    }
}
