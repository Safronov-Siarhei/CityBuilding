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
        // The minimap's camera, when it moves (an Invasion run's follows the rig): the marker is
        // placed relative to where that camera is and how much it frames.
        [SerializeField] private Camera minimapCamera;
        // In a run the shown square follows the rig and slides over a bigger picture.
        [SerializeField] private MinimapCameraUpdater follow;

        private void LateUpdate()
        {
            if (cameraRig == null || marker == null) return;
            marker.anchoredPosition = ToMap(cameraRig.transform.position);
        }

        /// <summary>A world point on the minimap (UI units from its middle), clamped into its square.</summary>
        public Vector2 ToMap(Vector3 world)
        {
            var at = ToMapUnclamped(world);
            return new Vector2(Mathf.Clamp(at.x, -mapHalfExtent, mapHalfExtent), Mathf.Clamp(at.y, -mapHalfExtent, mapHalfExtent));
        }

        /// <summary>A world point on the minimap's plane, however far outside what it frames.</summary>
        public Vector2 ToMapUnclamped(Vector3 world)
        {
            var scale = worldToMapScale;
            var centre = Vector3.zero;
            if (follow != null && follow.Follows)
            {
                scale = mapHalfExtent / Mathf.Max(1f, follow.ViewRadius);
                centre = follow.ViewCentre;
            }
            else if (minimapCamera != null)
            {
                scale = mapHalfExtent / Mathf.Max(1f, minimapCamera.orthographicSize);
                centre = minimapCamera.transform.position;
            }
            return new Vector2((world.x - centre.x) * scale, (world.z - centre.z) * scale);
        }

        public float MapHalfExtent => mapHalfExtent;
    }
}
