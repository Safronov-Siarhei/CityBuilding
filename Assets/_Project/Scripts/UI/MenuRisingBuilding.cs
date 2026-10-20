using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>A house going up inside scaffolding: it rises out of the ground, stands finished a moment, and starts again.</summary>
    public class MenuRisingBuilding : MonoBehaviour
    {
        private Vector3 _scale;
        private float _start;
        private Transform _scaffold;
        private const float Cycle = 22f;

        public void Begin(Material wood)
        {
            _scale = transform.localScale;
            _start = Time.time;

            var bounds = new Bounds(transform.position, Vector3.zero);
            foreach (var r in GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
            _scaffold = new GameObject("Scaffold").transform;
            _scaffold.SetParent(transform.parent, false);
            // the model's bounds run to its plot; the poles stand round the walls, a little inside that
            var ext = new Vector3(bounds.extents.x * 0.72f + 0.08f, 0f, bounds.extents.z * 0.72f + 0.08f);
            var h = bounds.size.y + 0.25f;
            for (var sx = -1; sx <= 1; sx += 2)
            {
                for (var sz = -1; sz <= 1; sz += 2)
                {
                    Pole(new Vector3(bounds.center.x + sx * ext.x, transform.position.y + h * 0.5f, bounds.center.z + sz * ext.z), new Vector3(0.05f, h, 0.05f), wood);
                }
            }
            for (var level = 1; level <= 2; level++)
            {
                var y = transform.position.y + h * level / 2.5f;
                Pole(new Vector3(bounds.center.x, y, bounds.center.z - ext.z), new Vector3(ext.x * 2f, 0.04f, 0.04f), wood);
                Pole(new Vector3(bounds.center.x, y, bounds.center.z + ext.z), new Vector3(ext.x * 2f, 0.04f, 0.04f), wood);
                Pole(new Vector3(bounds.center.x - ext.x, y, bounds.center.z), new Vector3(0.04f, 0.04f, ext.z * 2f), wood);
                Pole(new Vector3(bounds.center.x + ext.x, y, bounds.center.z), new Vector3(0.04f, 0.04f, ext.z * 2f), wood);
            }
        }

        private void Pole(Vector3 at, Vector3 size, Material wood)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(_scaffold, false);
            go.transform.position = at;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = wood;
        }

        private void Update()
        {
            var t = Mathf.Repeat(Time.time - _start, Cycle);
            float rise;
            if (t < 15f) rise = Mathf.Lerp(0.08f, 1f, t / 15f);
            else if (t < 20f) rise = 1f;
            else rise = Mathf.Lerp(1f, 0.08f, (t - 20f) / 2f);
            transform.localScale = new Vector3(_scale.x, _scale.y * rise, _scale.z);
            if (_scaffold != null) _scaffold.gameObject.SetActive(t < 15.5f || t > 20f);
        }
    }
}
