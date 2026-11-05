using CityBuilder.Buildings;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// Toggles UI panels around placement: the placement hint shows only while the mandatory Town
    /// Hall is being placed, and the build menu shows only when nothing is being placed at all --
    /// while a building is in hand the placement controls (cancel, rotate, confirm) take its
    /// place at the bottom of the screen, and it comes back once the building is placed or
    /// dropped (the user's layout, 2026-10-14).
    /// </summary>
    public class BuildingPlacerUIVisibility : MonoBehaviour
    {
        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private GameObject showWhilePlacingMandatory;
        [SerializeField] private GameObject hideWhilePlacingMandatory;
        /// <summary>The cancel/rotate/confirm row: it stands on the bottom edge, and is lifted above the hint while the hint is there.</summary>
        [SerializeField] private RectTransform placementControls;
        [SerializeField] private float liftWhilePlacingMandatory;

        private float _controlsY;
        private bool _lifted;

        private Text _hint;
        private string _townHallHint;
        private bool _lighthouseHint;

        private void Awake()
        {
            // the hint says the Town Hall (baked at setup), then the lighthouse (the user, 2026-11-02)
            _hint = showWhilePlacingMandatory != null ? showWhilePlacingMandatory.GetComponentInChildren<Text>(true) : null;
            if (_hint != null) _townHallHint = _hint.text;
            if (placementControls != null) _controlsY = placementControls.anchoredPosition.y;
        }

        private void Update()
        {
            if (buildingPlacer == null) return;
            var mandatory = buildingPlacer.IsPlacingMandatoryBuilding;
            if (_hint != null && _lighthouseHint != buildingPlacer.IsPlacingLighthouse)
            {
                _lighthouseHint = buildingPlacer.IsPlacingLighthouse;
                _hint.text = _lighthouseHint ? Localization.Get("#hud_place_lighthouse_hint") : _townHallHint;
            }
            if (showWhilePlacingMandatory != null && showWhilePlacingMandatory.activeSelf != mandatory)
            {
                showWhilePlacingMandatory.SetActive(mandatory);
            }
            if (placementControls != null && _lifted != mandatory)
            {
                _lifted = mandatory;
                var at = placementControls.anchoredPosition;
                placementControls.anchoredPosition = new Vector2(at.x, _controlsY + (mandatory ? liftWhilePlacingMandatory : 0f));
            }
            // a held building (BuildingFocus) puts its «!» where the build menu stands -- an orc's
            // or a dungeon's as well as one's own (the user, 2026-11-01)
            var held = BuildingFocus.Instance != null && BuildingFocus.Instance.Holding;
            // ...and a hero in hand puts his skills there (HeroSkillBar)
            // nothing is built below: no menu in a dungeon at all (the user, 2026-11-01)
            var showMenu = !mandatory && !buildingPlacer.IsSelecting && !held && !HeroSkillBar.Showing && !ShipActionBar.Showing && !ArmyBar.Showing
                && !Dungeon.DungeonMode.IsActive;
            if (hideWhilePlacingMandatory != null && hideWhilePlacingMandatory.activeSelf != showMenu)
            {
                hideWhilePlacingMandatory.SetActive(showMenu);
            }
        }
    }
}
