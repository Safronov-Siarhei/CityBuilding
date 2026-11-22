using CityBuilder.Buildings;
using CityBuilder.CameraControl;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CityBuilder.UI
{
    /// <summary>
    /// A tap on the minimap brings the camera home to the Town Hall (the user, 2026-11-02). Nothing
    /// in a dungeon, where the town is put away, in the campaign, or before the hall stands.
    /// </summary>
    public class MinimapHomeTap : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private RTSCameraController cameraRig;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Dungeon.DungeonMode.IsActive) return;
            // the campaign: the minimap only helps to find the way, a tap takes nobody anywhere (the user, 2026-11-22)
            if (Campaign.CityDefenceMode.IsActive || FindAnyObjectByType<Campaign.MissionOne>() != null) return;
            var hall = BuildingInstance.TownHall;
            if (hall == null) return;
            if (cameraRig == null) cameraRig = FindAnyObjectByType<RTSCameraController>();
            if (cameraRig != null) cameraRig.JumpTo(hall.transform.position);
        }
    }
}
