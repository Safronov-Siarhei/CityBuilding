using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.UI;
using UnityEngine;

namespace CityBuilder.InputControl
{
    /// <summary>
    /// Decides WHICH system a world tap belongs to, in one place and in a fixed order.
    ///
    /// This replaces a set of mutual stand-down checks that each input system used to carry about
    /// every other one -- BuildingSelector and CitizenSelector both refused to act while
    /// <c>buildingPlacer.IsSelecting</c> or <c>ArmyManager.SelectedGroup != null</c>, and
    /// ArmyOrderInput checked the placer too. Every new interaction had to be taught to every
    /// existing one, and any gap between those checks was a tap that two systems both answered.
    ///
    /// The order below is the schema's mode priority. A handler returns false for "not mine",
    /// which is what lets a tap on a BUILDING while a citizen is selected do the sensible thing:
    /// the citizen handler drops the selection, declines the tap, and it falls through to the
    /// building card in the same frame.
    /// </summary>
    public class WorldInputDispatcher : MonoBehaviour
    {
        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private ArmyOrderInput armyOrderInput;
        [SerializeField] private CitizenSelector citizenSelector;
        [SerializeField] private BuildingSelector buildingSelector;

        private void Start()
        {
            Unsubscribe();
            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            var router = TouchInputRouter.Instance;
            if (router == null) return;

            router.Tapped += HandleTap;
            router.LongPressed += HandleLongPress;
        }

        private void Unsubscribe()
        {
            var router = TouchInputRouter.Instance;
            if (router == null) return;

            router.Tapped -= HandleTap;
            router.LongPressed -= HandleLongPress;
        }

        private void HandleTap(Vector2 screenPosition)
        {
            // 1. Placement and drawing own every world tap while they are open.
            if (buildingPlacer != null && buildingPlacer.IsSelecting)
            {
                buildingPlacer.HandleWorldTap(screenPosition);
                return;
            }

            // 2. A selected ship: a tap on the sea sends it; on the land it is put down and the tap goes on.
            var tapCamera = citizenSelector != null ? citizenSelector.TapCamera : Camera.main;

            // a lighthouse's light waiting for its spot takes the tap
            if (Ships.LighthouseAim.Aiming != null)
            {
                Ships.LighthouseAim.HandleTap(screenPosition, tapCamera);
                return;
            }
            if (Ships.ShipInput.HasSelection && Ships.ShipInput.HandleTap(screenPosition, tapCamera)) return;

            // 2. A selected army group turns a tap into an order.
            if (armyOrderInput != null && ArmyManager.Instance != null && ArmyManager.Instance.SelectedGroup != null)
            {
                if (armyOrderInput.HandleWorldTap(screenPosition)) return;
            }

            // 3. A selected citizen does the same, one rung down.
            if (citizenSelector != null && citizenSelector.HasSelection)
            {
                if (citizenSelector.HandleWorldTap(screenPosition)) return;
            }

            // 4. Otherwise the tap is plain selection: a citizen, a building card, a tree or a
            //    boulder (how much is left in it), or nothing. A building behind a tree's wide
            //    click box still wins -- the card is what the player was reaching for.
            if (citizenSelector != null && HeroPortraitController.TrySelectAt(screenPosition, citizenSelector.TapCamera)) return;
            if (Ships.ShipInput.TrySelectAt(screenPosition, tapCamera)) return;
            if (citizenSelector != null && citizenSelector.TrySelectCitizen(screenPosition)) return;
            if (citizenSelector != null && (buildingSelector == null || !buildingSelector.HasBuildingAt(screenPosition)))
            {
                var node = citizenSelector.FindNodeAt(screenPosition);
                if (node != null)
                {
                    NodeYieldBubble.Show(node, citizenSelector.TapCamera);
                    return;
                }
            }
            // a dungeon's mouth: its card, with its level and «Войти»
            var entrance = Dungeon.DungeonEntrance.At(screenPosition, tapCamera);
            if (entrance != null && UI.DungeonEntranceCard.Instance != null)
            {
                UI.DungeonEntranceCard.Instance.Show(entrance);
                return;
            }
            if ((buildingSelector == null || !buildingSelector.HasBuildingAt(screenPosition)) && TryReportDeposit(screenPosition, tapCamera)) return;
            if (buildingSelector != null) buildingSelector.HandleWorldTap(screenPosition);
        }

        /// <summary>
        /// A tap on a bare ore vein says what it is in the event feed (the user, 2026-10-31:
        /// «Обнаружено месторождение: Железо») -- the patch alone does not tell iron from copper.
        /// </summary>
        private static bool TryReportDeposit(Vector2 screenPosition, Camera tapCamera)
        {
            var map = Maps.MeshMapApplier.Instance;
            var grid = Grid.GridManager.Instance;
            if (map == null || grid == null || tapCamera == null) return false;
            if (!map.TryRaycastGround(tapCamera.ScreenPointToRay(screenPosition), out var hit)) return false;
            var deposit = Maps.OreDeposit.At(grid.WorldToCell(hit.point));
            if (deposit == null || deposit.Mine != null) return false;
            var ore = Core.Localization.Get("#ore_" + deposit.Kind.ToString().ToLowerInvariant());
            Core.EventLogManager.Instance?.Post(Core.GameEvent.At(
                string.Format(Core.Localization.Get("#log_deposit_found"), ore), Core.EventSeverity.Good, Core.EventGlyph.Star,
                deposit.Kind + "Mine", deposit.transform.position));
            return true;
        }

        /// <summary>
        /// A long press is an escape hatch, nothing more. It cancels a selection, and in plain
        /// browsing it does nothing at all -- demolition, which used to be its reason to exist,
        /// lives on the building card instead.
        /// </summary>
        private void HandleLongPress(Vector2 screenPosition)
        {
            if (buildingPlacer != null && buildingPlacer.IsSelecting) return;

            if (ArmyManager.Instance != null && ArmyManager.Instance.SelectedGroup != null)
            {
                ArmyManager.Instance.SelectGroup(null);
                return;
            }

            if (citizenSelector != null && citizenSelector.HasSelection) citizenSelector.Deselect();
            if (Ships.ShipInput.HasSelection) Ships.ShipInput.Select(null);
        }
    }
}
