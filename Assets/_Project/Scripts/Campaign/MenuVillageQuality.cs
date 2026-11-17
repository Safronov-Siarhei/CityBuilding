using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// What the main menu's village costs a phone (concept 1 stage 3, the user 2026-11-17: «все четыре»).
    /// The editor census found ~1 600 draws and 283 000 triangles in the menu's view, two thirds of the
    /// draws the folk -- each figure a renderer per body part, each renderer with a back-face twin.
    /// Here, for the menu only (the film and the missions keep theirs):
    ///   * a figure keeps its one skinned mesh (CineFigure.KeepSkinned) and nothing gets back-face twins
    ///     (CineTwoSided.Off) -- the menu's camera stays far and high;
    ///   * on the Low graphics tier the figures cast no shadows and only a share of the folk come out;
    ///   * the camera draws only so far, a haze hiding that edge (the sheet's menu_* keys).
    /// Set in Awake, before anything is built or spawned; undone when the menu goes.
    /// </summary>
    public class MenuVillageQuality : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private OpeningLife life;

        private void Awake()
        {
            var low = GraphicsQuality.Effective == GraphicsTier.Low;
            CineFigure.KeepSkinned = true;
            CineTwoSided.Off = true;
            CineFigure.NoShadows = low;
            var config = BalanceConfig.Instance;
            if (config == null) return;
            if (life != null) life.FolkShare = low ? config.MenuLowFolkShare : 1f;
            if (cam != null) cam.farClipPlane = config.MenuFarClip;
            RenderSettings.fogStartDistance = config.MenuFogStart;
            RenderSettings.fogEndDistance = Mathf.Min(config.MenuFogEnd, config.MenuFarClip);
        }

        private void OnDestroy()
        {
            CineFigure.KeepSkinned = false;
            CineTwoSided.Off = false;
            CineFigure.NoShadows = false;
        }
    }
}
