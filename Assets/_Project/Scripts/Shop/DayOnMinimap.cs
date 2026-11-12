using UnityEngine;

namespace CityBuilder.Shop
{
    /// <summary>
    /// «День N» on the minimap's foot in the sandbox (the user, 2026-11-12: the running boosters' row took its
    /// place under the resource bar). An Invasion run keeps its wave line where it was. Follows the minimap,
    /// which the HUD's edge anchoring may move.
    /// </summary>
    public class DayOnMinimap : MonoBehaviour
    {
        [SerializeField] private RectTransform minimap;
        [SerializeField] private float lift = 22f;
        [SerializeField] private Vector2 size = new Vector2(220f, 46f);

        private bool _on;

        private void Start()
        {
            _on = minimap != null && !Invasion.InvasionMode.IsActive;
            if (_on) ((RectTransform)transform).sizeDelta = size;
        }

        private void LateUpdate()
        {
            if (!_on) return;
            var r = minimap.rect;
            transform.position = minimap.TransformPoint(new Vector3(r.center.x, r.yMin + lift, 0f));
        }
    }
}
