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
        /// <summary>
        /// The round cancel button that stands for the badge while a squad, a machine or a hero is in
        /// hand (the user, 2026-11-05: «значок отмены выбора армии», the same for a hero instead of
        /// «Выбран король»); the badge with its words is left for a citizen.
        /// </summary>
        [SerializeField] private GameObject cancelRoot;

        /// <summary>From the bottom-right corner's edges, in an Invasion run.</summary>
        private static readonly Vector2 RunCornerOffset = new Vector2(-24f, 24f);

        /// <summary>
        /// In an Invasion run the badge sits in the bottom-right corner (the user, 2026-10-22),
        /// which the sandbox gives the tax and a run leaves empty -- not under the resource bar,
        /// where the wave line and the King's portrait are.
        /// </summary>
        private void Start()
        {
            // a dungeon has no tax panel either: the very corner (the user, 2026-11-01)
            if (!(Invasion.InvasionMode.IsActive || Dungeon.DungeonMode.IsActive) || badgeRoot == null) return;
            var rect = (RectTransform)badgeRoot.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = RunCornerOffset;
            if (cancelRoot == null) return;
            var cancel = (RectTransform)cancelRoot.transform;
            cancel.anchorMin = cancel.anchorMax = cancel.pivot = new Vector2(1f, 0f);
            cancel.anchoredPosition = RunCornerOffset;
        }

        private void Update()
        {
            var army = ArmyManager.Instance;
            var hasGroup = army != null && army.SelectedGroup != null;
            var hasCitizen = citizenSelector != null && citizenSelector.HasSelection;
            var visible = !hasGroup && hasCitizen && !ModalGate.IsBlocked;
            var round = hasGroup && !ModalGate.IsBlocked;
            if (cancelRoot != null && cancelRoot.activeSelf != round) cancelRoot.SetActive(round);

            if (badgeRoot != null && badgeRoot.activeSelf != visible) badgeRoot.SetActive(visible);
            if (!visible || label == null) return;

            label.text = Localization.Get("#selection_citizen");
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
