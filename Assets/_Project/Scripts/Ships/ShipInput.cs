using UnityEngine;

namespace CityBuilder.Ships
{
    /// <summary>
    /// Ships under the player's hand (the user, 2026-10-28: «управление как у армии»): a tap on a
    /// ship selects it; with one selected, a tap on the sea sends it there, a tap on another ship
    /// selects that one, and a tap on the land puts it down and lets the tap through (a building's
    /// card, a citizen). Reached through WorldInputDispatcher.
    /// </summary>
    public static class ShipInput
    {
        public static ShipUnit Selected { get; private set; }

        private static readonly RaycastHit[] Hits = new RaycastHit[32];

        public static bool HasSelection => Selected != null;

        public static void Select(ShipUnit ship)
        {
            if (Selected == ship) return;
            if (Selected != null) Selected.SetSelected(false);
            Selected = ship;
            if (ship != null) ship.SetSelected(true);
        }

        /// <summary>The ship under the tap, if any.</summary>
        public static ShipUnit ShipAt(Vector2 screen, Camera camera)
        {
            if (camera == null || ShipManager.Instance == null || ShipManager.Instance.Ships.Count == 0) return null;
            var ray = camera.ScreenPointToRay(screen);
            var count = Physics.RaycastNonAlloc(ray, Hits, 1000f, ~0, QueryTriggerInteraction.Collide);
            ShipUnit best = null;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                var ship = Hits[i].collider.GetComponentInParent<ShipUnit>();
                if (ship == null || Hits[i].distance >= bestDistance) continue;
                best = ship;
                bestDistance = Hits[i].distance;
            }
            return best;
        }

        /// <summary>A plain tap: true when it picked a ship.</summary>
        public static bool TrySelectAt(Vector2 screen, Camera camera)
        {
            var ship = ShipAt(screen, camera);
            if (ship == null) return false;
            Select(ship);
            return true;
        }

        /// <summary>A tap while a ship is selected: false when it was the land, so the tap goes on.</summary>
        public static bool HandleTap(Vector2 screen, Camera camera)
        {
            if (Selected == null) return false;
            var other = ShipAt(screen, camera);
            if (other != null)
            {
                Select(other);
                return true;
            }
            var ray = camera.ScreenPointToRay(screen);
            var sea = new Plane(Vector3.up, new Vector3(0f, ShipManager.WaterLevel, 0f));
            if (!sea.Raycast(ray, out var distance)) return false;
            var point = ray.GetPoint(distance);
            var applier = Maps.MeshMapApplier.Instance;
            var grid = Grid.GridManager.Instance;
            // land under the tap (raycast against the islands): the ship is put down, the tap goes on
            if (applier != null && applier.TryRaycastGround(ray, out _) && grid != null && !applier.IsWaterCell(grid.WorldToCell(point)))
            {
                Select(null);
                return false;
            }
            Selected.SailTo(point);
            return true;
        }
    }
}
