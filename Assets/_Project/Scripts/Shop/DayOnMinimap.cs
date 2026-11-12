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
        // the plaque's centre this far over the minimap's foot: whole above the screen's edge (the user, 2026-11-12: «поднять выше»)
        [SerializeField] private float liftOverFoot = 52f;   // renamed from `lift` (22) so the scene's old value does not hold it down
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
            transform.position = minimap.TransformPoint(new Vector3(r.center.x, r.yMin + liftOverFoot, 0f));
        }
    }
}
