using CityBuilder.CameraControl;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// Keeps a small diamond marker on the minimap tracking the RTS camera rig's ground position
    /// -- the rig's own transform (what RTSCameraController pans/clamps), not the physical Camera
    /// transform, which sits offset back/up from it by the current zoom distance and would read
    /// wrong the moment the player zooms. Lets the player tell where their current view is
    /// looking from on the wider map instead of having to guess from the live minimap render
    /// alone.
    /// </summary>
    public class MinimapCameraIndicator : MonoBehaviour
    {
        [SerializeField] private RTSCameraController cameraRig;
        [SerializeField] private RectTransform marker;
        // UI units per world unit on the minimap -- see SetupProject.BuildMinimap, where this is
        // derived from the same GridCellsX/CellSize span the minimap camera is framed to.
        [SerializeField] private float worldToMapScale = 0.5f;
        [SerializeField] private float mapHalfExtent = 100f;

        private void LateUpdate()
        {
            if (cameraRig == null || marker == null) return;

            var pos = cameraRig.transform.position;
            var x = Mathf.Clamp(pos.x * worldToMapScale, -mapHalfExtent, mapHalfExtent);
            var y = Mathf.Clamp(pos.z * worldToMapScale, -mapHalfExtent, mapHalfExtent);
            marker.anchoredPosition = new Vector2(x, y);
        }
    }
}
