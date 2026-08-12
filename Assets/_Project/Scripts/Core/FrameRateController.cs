using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// Caps the frame rate explicitly instead of relying solely on QualitySettings.vSyncCount --
    /// mobile platforms (this project's hotbar/rotate-button UI is touch-aware, see
    /// BuildingPlacerUIVisibility/ShowWhileSelectingBuilding) often ignore or only partially honor
    /// VSync, letting the game render uncapped. That burns battery/generates heat for no visual
    /// benefit above 60fps, and makes the occasional GC-pause stutter more noticeable against a
    /// higher, more variable baseline frame rate.
    /// </summary>
    public class FrameRateController : MonoBehaviour
    {
        private const int TargetFrameRate = 60;

        private void Awake()
        {
            Application.targetFrameRate = TargetFrameRate;
        }
    }
}
