using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Ships
{
    /// <summary>
    /// The archipelago's lighthouse (the user, 2026-10-28; paid in coins): its button puts the next
    /// tap on the map in its hand -- a spot within lighthouse_range_meters of it is lit up, the fog
    /// over lighthouse_reveal_cells round it opened for good (the fog keeps what it opened, as for
    /// the scout), for lighthouse_reveal_coins; then the lantern needs lighthouse_cooldown_seconds.
    /// A lighthouse stands only at the shore (BuildingPlacer: water within lighthouse_coast_cells).
    /// </summary>
    public static class LighthouseAim
    {
        public const string BuildingId = "Lighthouse";

        private static readonly Dictionary<BuildingInstance, float> ReadyAt = new Dictionary<BuildingInstance, float>();

        /// <summary>The lighthouse whose light the next tap places, or null.</summary>
        public static BuildingInstance Aiming { get; private set; }

        private static BalanceConfig Balance => BalanceConfig.Instance;
        public static float Range => Balance != null ? Balance.LighthouseRangeMeters : 80f;
        public static int Price => Balance != null ? Balance.LighthouseRevealCoins : 30;

        /// <summary>Seconds until this lighthouse can light again; 0 when ready.</summary>
        public static float CooldownLeft(BuildingInstance lighthouse) =>
            lighthouse != null && ReadyAt.TryGetValue(lighthouse, out var at) ? Mathf.Max(0f, at - Time.time) : 0f;

        /// <summary>Why this lighthouse cannot light now, or null.</summary>
        public static string DescribeBlocker(BuildingInstance lighthouse)
        {
            if (lighthouse == null || !lighthouse.IsOperational) return Localization.Get("#lighthouse_not_ready");
            var wait = CooldownLeft(lighthouse);
            if (wait > 0f) return Localization.Format("#lighthouse_cooldown", Mathf.CeilToInt(wait));
            if (ResourceManager.Instance != null && !ResourceManager.Instance.HasEnough(PriceList())) return Localization.Format("#lighthouse_no_coins", Price);
            return null;
        }

        private static List<ResourceAmount> PriceList() => new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Coins, amount = Price } };

        /// <summary>The button: the next tap on the map chooses where to light (a second press calls it off).</summary>
        public static bool Begin(BuildingInstance lighthouse)
        {
            if (Aiming != null)
            {
                Cancel();
                return false;
            }
            var blocker = DescribeBlocker(lighthouse);
            if (blocker != null)
            {
                Say(blocker, EventSeverity.Warning);
                return false;
            }
            Aiming = lighthouse;
            Grid.HarvestRadiusOverlay.ShowFor(lighthouse.transform.position, Mathf.RoundToInt(Range));
            Say(Localization.Get("#lighthouse_aim"), EventSeverity.Good);
            return true;
        }

        public static void Cancel()
        {
            Aiming = null;
            Grid.HarvestRadiusOverlay.HideIfShown();
        }

        /// <summary>A tap while aiming: lights that spot if it is in reach (and pays); always ends the aiming. Reached through WorldInputDispatcher.</summary>
        public static void HandleTap(Vector2 screen, Camera camera)
        {
            var lighthouse = Aiming;
            Cancel();
            if (lighthouse == null || camera == null) return;
            var ray = camera.ScreenPointToRay(screen);
            Vector3 point;
            var applier = MeshMapApplier.Instance;
            if (applier != null && applier.TryRaycastGround(ray, out var hit)) point = hit.point;
            else if (new Plane(Vector3.up, new Vector3(0f, ShipManager.WaterLevel, 0f)).Raycast(ray, out var d)) point = ray.GetPoint(d);
            else return;
            LightAt(lighthouse, point);
        }

        /// <summary>Lights this spot from this lighthouse: false (and nothing paid) when it is out of reach or the lighthouse cannot.</summary>
        public static bool LightAt(BuildingInstance lighthouse, Vector3 point)
        {
            var blocker = DescribeBlocker(lighthouse);
            var flat = point - lighthouse.transform.position;
            flat.y = 0f;
            if (blocker == null && flat.magnitude > Range) blocker = Localization.Get("#lighthouse_far");
            if (blocker != null)
            {
                Say(blocker, EventSeverity.Warning);
                return false;
            }
            if (ResourceManager.Instance != null && !ResourceManager.Instance.TrySpend(PriceList())) return false;
            var grid = Grid.GridManager.Instance;
            var cells = Balance != null ? Balance.LighthouseRevealCells : 18;
            if (grid != null) FogOfWarManager.Instance?.RevealPermanent(grid.WorldToCell(point), cells);
            ReadyAt[lighthouse] = Time.time + (Balance != null ? Balance.LighthouseCooldownSeconds : 90f);
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_lighthouse"), EventSeverity.Good, EventGlyph.Star, BuildingId, point));
            return true;
        }

        private static void Say(string text, EventSeverity severity) =>
            EventLogManager.Instance?.Post(GameEvent.ForSettlement(text, severity, EventGlyph.Alert, null));
    }
}
