using CityBuilder.Buildings;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// «Перенести здание» (the user, 2026-11-17; Design/features/building-relocation.md): while one of the player's
    /// buildings that may move is held (BuildingPlacer.CanBeMoved -- the sandbox only), a round button with four
    /// arrows stands RIGHT of the «!». A tap picks the building up: its placement again with ✕ and ✓ (the ✓ shows
    /// the price, move_building_crystals), see BuildingPlacer.BeginMove.
    /// </summary>
    public class MoveBuildingBar : MonoBehaviour
    {
        [SerializeField] private GameObject button;

        private BuildingPlacer _placer;

        private BuildingInstance Target
        {
            get
            {
                var focus = BuildingFocus.Instance != null && BuildingFocus.Instance.ButtonsShown ? BuildingFocus.Instance.Current : null;
                if (focus == null || !BuildingPlacer.CanBeMoved(focus)) return null;
                if (_placer == null) _placer = FindAnyObjectByType<BuildingPlacer>();
                return _placer != null && !_placer.IsSelecting ? focus : null;
            }
        }

        /// <summary>The button (a persistent listener).</summary>
        public void Tap()
        {
            var target = Target;
            if (target == null) return;
            BuildingFocus.Instance.Clear();
            _placer.BeginMove(target);
        }

        private void Update()
        {
            var show = Target != null;
            if (button != null && button.activeSelf != show) button.SetActive(show);
        }
    }
}
