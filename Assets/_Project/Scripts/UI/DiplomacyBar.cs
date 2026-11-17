using CityBuilder.Buildings;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// «Дипломатия» (the user, 2026-11-17): while the player's finished Diplomacy Hall is held, a round button
    /// with the treaty and its seal stands left of the «!»; a tap opens the Diplomacy window. The «!» opens
    /// the hall's ordinary card (upgrade, repair). Not in the tutorial.
    /// </summary>
    public class DiplomacyBar : MonoBehaviour
    {
        [SerializeField] private GameObject button;

        private static BuildingInstance Target
        {
            get
            {
                if (Tutorial.TutorialMode.IsActive) return null;
                var focus = BuildingFocus.Instance != null && BuildingFocus.Instance.ButtonsShown ? BuildingFocus.Instance.Current : null;
                return focus != null && !focus.IsBot && focus.IsOperational && focus.Data != null
                       && focus.Data.buildingName == BuildingIds.DiplomacyHall ? focus : null;
            }
        }

        /// <summary>The button (a persistent listener).</summary>
        public void Tap()
        {
            var target = Target;
            if (target != null && DiplomacyWindow.Instance != null) DiplomacyWindow.Instance.Show(target);
        }

        private void Update()
        {
            var show = Target != null && (DiplomacyWindow.Instance == null || !DiplomacyWindow.Instance.IsOpen);
            if (button != null && button.activeSelf != show) button.SetActive(show);
        }
    }
}
