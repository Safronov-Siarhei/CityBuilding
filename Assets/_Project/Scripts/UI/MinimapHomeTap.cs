using CityBuilder.Buildings;
using CityBuilder.CameraControl;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CityBuilder.UI
{
    /// <summary>
    /// A tap on the minimap brings the camera home to the Town Hall (the user, 2026-11-02). Nothing
    /// in a dungeon, where the town is put away, or before the hall stands.
    /// </summary>
    public class MinimapHomeTap : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private RTSCameraController cameraRig;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Dungeon.DungeonMode.IsActive) return;
            var hall = BuildingInstance.TownHall;
            if (hall == null) return;
            if (cameraRig == null) cameraRig = FindAnyObjectByType<RTSCameraController>();
            if (cameraRig != null) cameraRig.JumpTo(hall.transform.position);
        }
    }
}
