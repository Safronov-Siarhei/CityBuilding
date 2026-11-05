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
            // one of the player's squad soldiers or a machine: that squad (army-control.md); his card is on a long press
            if (UI.ArmyBar.TrySelectSoldierAt(screenPosition, tapCamera)) return;
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
            // a dungeon's mouth: held like a town building, its «!» opens the card (the user, 2026-11-01)
            var entrance = Dungeon.DungeonEntrance.At(screenPosition, tapCamera);
            // a lever is used, not held: a hero by it pulls it, else the player is told to bring one
            if (entrance != null && entrance.Kind == Dungeon.DungeonEntrance.Place.Lever)
            {
                Dungeon.DungeonLevers.Highlight(entrance);
                entrance.OpenCard();
                return;
            }
            if (entrance != null)
            {
                if (UI.BuildingFocus.Instance != null) UI.BuildingFocus.Instance.SelectEntrance(entrance);
                else entrance.OpenCard();
                return;
            }
            if (TryReportCache(screenPosition, tapCamera)) return;
            if ((buildingSelector == null || !buildingSelector.HasBuildingAt(screenPosition)) && TryReportDeposit(screenPosition, tapCamera)) return;
            if ((buildingSelector == null || !buildingSelector.HasBuildingAt(screenPosition)) && TryShowFertileLand(screenPosition, tapCamera)) return;
            if ((buildingSelector == null || !buildingSelector.HasBuildingAt(screenPosition)) && TryShowShoal(screenPosition, tapCamera)) return;
            if (buildingSelector != null) buildingSelector.HandleWorldTap(screenPosition);
        }

        /// <summary>A tap on a guarded cache's ruin or chest on the archipelago: «Найден сундук с сокровищами».</summary>
        private static bool TryReportCache(Vector2 screenPosition, Camera tapCamera)
        {
            var map = Maps.MeshMapApplier.Instance;
            if (map == null || tapCamera == null || Maps.Archipelago.TideCaches.Instance == null) return false;
            if (!map.TryRaycastGround(tapCamera.ScreenPointToRay(screenPosition), out var hit)) return false;
            var cache = Maps.Archipelago.TideCaches.At(hit.point);
            if (cache == null) return false;
            Maps.Archipelago.TideCaches.Report(cache);
            return true;
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
        /// A tap on fertile soil lights up the edge of its patch and says so in the event feed
        /// (the user, 2026-10-31: «Обнаружена плодородная земля»).
        /// </summary>
        private static bool TryShowFertileLand(Vector2 screenPosition, Camera tapCamera)
        {
            var map = Maps.MeshMapApplier.Instance;
            var grid = Grid.GridManager.Instance;
            if (map == null || grid == null || tapCamera == null) return false;
            if (!map.TryRaycastGround(tapCamera.ScreenPointToRay(screenPosition), out var hit)) return false;
            var cell = grid.WorldToCell(hit.point);
            if (!map.IsFertileCell(cell)) return false;
            var patch = map.FertilePatchAt(cell);
            Maps.Archipelago.FertileLand.Flash(patch, map);
            Core.EventLogManager.Instance?.Post(Core.GameEvent.At(
                Core.Localization.Get("#log_fertile_found"), Core.EventSeverity.Good, Core.EventGlyph.Star, "Farm", hit.point));
            return true;
        }

        /// <summary>A tap on a school of fish rings it and says so in the feed (the user, 2026-11-01: as fertile land).</summary>
        private static bool TryShowShoal(Vector2 screenPosition, Camera tapCamera)
        {
            var ships = Ships.ShipManager.Instance;
            if (ships == null || tapCamera == null) return false;
            var ray = tapCamera.ScreenPointToRay(screenPosition);
            if (!new Plane(Vector3.up, new Vector3(0f, Ships.ShipManager.WaterLevel, 0f)).Raycast(ray, out var enter)) return false;
            var shoal = ships.ShoalAt(ray.GetPoint(enter));
            if (shoal == null) return false;
            ships.FlashShoal(shoal);
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

            if (citizenSelector != null && citizenSelector.HasSelection)
            {
                citizenSelector.Deselect();
                return;
            }
            if (Ships.ShipInput.HasSelection)
            {
                Ships.ShipInput.Select(null);
                return;
            }
            // nothing in hand: a fighter's card (a tap on a squad soldier selects his squad, the user 2026-11-05)
            if (buildingSelector != null) buildingSelector.ShowFighterAt(screenPosition);
        }
    }
}
