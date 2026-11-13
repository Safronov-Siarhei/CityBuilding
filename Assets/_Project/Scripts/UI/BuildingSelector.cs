using CityBuilder.Buildings;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// Picks up an already-placed building with a tap (BuildingFocus: the camera, the rings, its
    /// reach, the «!» that opens its card -- worker assignment, upgrade, repair, demolition).
    ///
    /// Driven by WorldInputDispatcher rather than by its own pointer polling. The tap that gets
    /// here has already been confirmed as a tap (finger released without travelling) and has
    /// already lost to placement, army orders and citizen orders, so all the mutual stand-down
    /// checks this class used to carry are gone.
    /// </summary>
    public class BuildingSelector : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private BuildingInfoPanelController infoPanel;

        /// <summary>The Laboratory opens this instead of the ordinary card -- see ResearchPanelController, whose header carries the worker/upgrade/repair controls it displaces.</summary>
        [SerializeField] private ResearchPanelController researchPanel;

        [SerializeField] private LayerMask raycastMask = ~0;

        /// <summary>Holds the tapped building; its «!» opens the card. Without one, the tap opens the card at once, as it used to.</summary>
        [SerializeField] private BuildingFocus focus;

        // Reused across clicks rather than using Physics.RaycastAll, which allocates every call.
        // Sized for a shallow camera ray crossing tens of metres of forest: RaycastNonAlloc
        // returns hits unsorted, so a buffer that fills up can drop the clicked building itself
        // and keep a dozen tree click boxes instead. Same sizing as CitizenSelector.
        private readonly RaycastHit[] _hits = new RaycastHit[64];

        /// <summary>
        /// Opens the card of the building under this screen point. A tap on open ground closes
        /// whatever card is open instead -- on a phone the card covers a good part of the screen
        /// and hunting for its close button is a chore.
        /// </summary>
        public void HandleWorldTap(Vector2 screenPosition)
        {
            if (targetCamera == null) return;

            // a fighter under the finger, of any side: his card (the user, 2026-11-05)
            if (TryShowFighter(screenPosition))
            {
                if (focus != null) focus.Clear();
                if (infoPanel != null) infoPanel.Close();
                if (researchPanel != null) researchPanel.Close();
                return;
            }
            // campaign level 2: the town is not the player's to manage -- no building is picked by a tap (the user, 2026-11-13)
            if (Campaign.CityDefenceMode.IsActive) return;

            var nearest = FindBuilding(screenPosition);
            var foreign = nearest == null && focus != null ? FindForeign(screenPosition) : null;
            if (foreign != null)
            {
                // another side's building: a red ring and its «!»; a second tap opens its card
                if (infoPanel != null) infoPanel.Close();
                if (researchPanel != null) researchPanel.Close();
                if (focus.Foreign == foreign) focus.OpenDetails();
                else focus.SelectForeign(foreign);
                return;
            }
            if (nearest != null && nearest.IsBot)
            {
                // the bot kingdom's building: its card, read only (the user, 2026-11-03)
                if (focus != null) focus.Clear();
                if (infoPanel != null) infoPanel.Close();
                if (researchPanel != null) researchPanel.Close();
                ForeignBuildingCard.Instance?.Show(nearest);
                return;
            }
            if (nearest == null)
            {
                // a ruin of the player's: its card (restore / clear / close)
                var ruin = FindRuin(screenPosition);
                if (ruin != null && RuinCard.Instance != null)
                {
                    if (focus != null) focus.Clear();
                    if (infoPanel != null) infoPanel.Close();
                    if (researchPanel != null) researchPanel.Close();
                    RuinCard.Instance.Show(ruin);
                    return;
                }
                if (focus != null) focus.Clear();
                if (infoPanel != null) infoPanel.Close();
                if (researchPanel != null) researchPanel.Close();
                return;
            }

            if (focus != null)
            {
                // a second tap on the held building opens its card, like its «!»
                if (focus.Current == nearest) focus.OpenDetails();
                else
                {
                    if (infoPanel != null) infoPanel.Close();
                    if (researchPanel != null) researchPanel.Close();
                    focus.Select(nearest);
                }
                return;
            }

            if (researchPanel != null && nearest.Data != null
                && nearest.Data.buildingName == Research.ResearchManager.LaboratoryBuildingId)
            {
                researchPanel.Show(nearest);
                return;
            }

            if (infoPanel != null) infoPanel.Show(nearest);
        }

        /// <summary>
        /// The nearest building along the ray, looking past everything else. All hits, not just
        /// the closest collider: a tree's click box is a box around its whole canopy, so one
        /// standing between the camera and a building would otherwise swallow the tap entirely.
        /// </summary>
        /// <summary>
        /// The fighter nearest the tap on screen within a finger's width -- a soldier, a hero, an orc,
        /// a monster, the bot's men -- gets the read-only card. Screen distance, not a ray: units
        /// carry no colliders of their own to hit, and a small figure needs a forgiving target.
        /// </summary>
        /// <summary>A long press on a fighter: his card (a tap on one of the player's squads selects the squad instead, ArmyBar).</summary>
        public bool ShowFighterAt(Vector2 screenPosition)
        {
            if (targetCamera == null || !TryShowFighter(screenPosition)) return false;
            if (focus != null) focus.Clear();
            if (infoPanel != null) infoPanel.Close();
            if (researchPanel != null) researchPanel.Close();
            return true;
        }

        private bool TryShowFighter(Vector2 screenPosition)
        {
            var card = ForeignBuildingCard.Instance;
            if (card == null) return false;
            var reach = 34f * Mathf.Clamp(Screen.dpi / 160f, 1f, 3f);
            var best = reach * reach;
            Combat.SoldierUnit soldier = null;
            Combat.OrcUnit orc = null;
            foreach (var s in Combat.SoldierUnit.All)
            {
                if (s == null || !s.enabled || s.CurrentHealth <= 0) continue;
                var d = ScreenDistanceSq(s.transform.position, screenPosition);
                if (d >= best) continue;
                best = d;
                soldier = s;
            }
            foreach (var o in Combat.OrcUnit.All)
            {
                if (o == null || o.CurrentHealth <= 0 || !o.gameObject.activeInHierarchy) continue;
                var d = ScreenDistanceSq(o.transform.position, screenPosition);
                if (d >= best) continue;
                best = d;
                orc = o;
                soldier = null;
            }
            if (orc != null) card.Show(orc);
            else if (soldier != null) card.Show(soldier);
            else return false;
            return true;
        }

        private float ScreenDistanceSq(Vector3 world, Vector2 screen)
        {
            var p = targetCamera.WorldToScreenPoint(world + Vector3.up * 0.4f);
            if (p.z <= 0f) return float.MaxValue;
            return ((Vector2)p - screen).sqrMagnitude;
        }

        /// <summary>Whether a tap here would open a building's card.</summary>
        public bool HasBuildingAt(Vector2 screenPosition) => targetCamera != null && !Campaign.CityDefenceMode.IsActive && FindBuilding(screenPosition) != null;

        /// <summary>The nearest standing building of another side (an orc camp's) along the ray.</summary>
        private Combat.OrcCampBuilding FindForeign(Vector2 screenPosition)
        {
            var ray = targetCamera.ScreenPointToRay(screenPosition);
            var hitCount = Physics.RaycastNonAlloc(ray, _hits, 500f, raycastMask);
            Combat.OrcCampBuilding nearest = null;
            var nearestDistance = float.MaxValue;
            for (var i = 0; i < hitCount; i++)
            {
                if (_hits[i].distance >= nearestDistance) continue;
                var building = _hits[i].collider.GetComponentInParent<Combat.OrcCampBuilding>();
                if (building == null || building.CurrentHealth <= 0) continue;
                nearest = building;
                nearestDistance = _hits[i].distance;
            }
            return nearest;
        }

        private BuildingRuin FindRuin(Vector2 screenPosition)
        {
            if (BuildingRuin.All.Count == 0) return null;
            var ray = targetCamera.ScreenPointToRay(screenPosition);
            var hitCount = Physics.RaycastNonAlloc(ray, _hits, 500f, raycastMask);
            BuildingRuin nearest = null;
            var nearestDistance = float.MaxValue;
            for (var i = 0; i < hitCount; i++)
            {
                if (_hits[i].distance >= nearestDistance) continue;
                var ruin = _hits[i].collider.GetComponent<BuildingRuin>();
                if (ruin == null || ruin.IsBot) continue;
                nearest = ruin;
                nearestDistance = _hits[i].distance;
            }
            return nearest;
        }

        private BuildingInstance FindBuilding(Vector2 screenPosition)
        {
            var ray = targetCamera.ScreenPointToRay(screenPosition);
            var hitCount = Physics.RaycastNonAlloc(ray, _hits, 500f, raycastMask);

            BuildingInstance nearest = null;
            var nearestDistance = float.MaxValue;
            for (var i = 0; i < hitCount; i++)
            {
                if (_hits[i].distance >= nearestDistance) continue;
                var instance = _hits[i].collider.GetComponent<BuildingInstance>();
                if (instance == null) continue;

                nearest = instance;
                nearestDistance = _hits[i].distance;
            }

            return nearest;
        }
    }
}
