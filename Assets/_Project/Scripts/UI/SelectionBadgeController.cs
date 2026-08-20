using CityBuilder.Combat;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// A small badge naming whatever the player currently has selected, with a cancel button on it.
    ///
    /// This is the fix for a hole that cost a phone player the ability to change their mind at all:
    /// a selected citizen could be released with Escape or a right click and by nothing else, so on
    /// a touchscreen there was no way to deselect one -- the only escape was to select somebody
    /// else. Selection is not cosmetic either (while it holds, every world tap is an order), so a
    /// stuck selection meant a stuck input mode.
    ///
    /// Touch now has four ways out -- tap the citizen again, tap a building, long press, and this
    /// button -- but this is the only one that is VISIBLE, which is what makes it the important one.
    /// </summary>
    public class SelectionBadgeController : MonoBehaviour
    {
        [SerializeField] private CitizenSelector citizenSelector;
        [SerializeField] private GameObject badgeRoot;
        [SerializeField] private Text label;

        private void Update()
        {
            var army = ArmyManager.Instance;
            var hasGroup = army != null && army.SelectedGroup != null;
            var hasCitizen = citizenSelector != null && citizenSelector.HasSelection;
            var visible = (hasGroup || hasCitizen) && !ModalGate.IsBlocked;

            if (badgeRoot != null && badgeRoot.activeSelf != visible) badgeRoot.SetActive(visible);
            if (!visible || label == null) return;

            label.text = hasGroup
                ? Localization.Get("#selection_army")
                : Localization.Get("#selection_citizen");
        }

        /// <summary>The badge's own X. Clears whichever selection is showing -- the army group wins, matching the order the dispatcher resolves taps in.</summary>
        public void ClearSelection()
        {
            var army = ArmyManager.Instance;
            if (army != null && army.SelectedGroup != null)
            {
                army.SelectGroup(null);
                return;
            }

            if (citizenSelector != null) citizenSelector.Deselect();
        }
    }
}
