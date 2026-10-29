using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace CityBuilder.UI
{
    /// <summary>
    /// Turns a world tap into an order for the group the player selected in the army panel: tap an
    /// enemy to attack it, tap the ground to march there.
    ///
    /// Reached only through WorldInputDispatcher, and only while a group is actually selected --
    /// which is what keeps it from fighting with CitizenSelector and BuildingSelector over the
    /// same tap without any of the three having to know about the other two.
    ///
    /// The ground point comes from MeshMapApplier.TryRaycastGround rather than from whatever
    /// collider the ray hits first, for exactly the reasons documented there: over a forest the
    /// first collider is a tree's click box or a boulder, metres from where the player pointed.
    /// </summary>
    public class ArmyOrderInput : MonoBehaviour
    {
        /// <summary>How far an order point may be pulled onto walkable NavMesh -- same precision budget as a citizen move order.</summary>
        private const float DestinationSampleRadius = 2f;

        [SerializeField] private Camera targetCamera;
        [SerializeField] private GameObject orderFlagPrefab;

        // Same sizing rationale as CitizenSelector: a shallow camera ray crosses a lot of forest,
        // and RaycastNonAlloc drops hits arbitrarily once the buffer is full.
        private readonly RaycastHit[] _hits = new RaycastHit[64];

        private MoveOrderMarker _orderMarker;

        private void Update()
        {
            if (ModalGate.IsBlocked) return;

            var army = ArmyManager.Instance;
            if (army == null || army.SelectedGroup == null) return;

            // Desktop escape hatches. On touch the group's own icon does it (ArmyManager
            // .ToggleSelection), as does a long press or a tap on any building.
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[Key.Escape].wasPressedThisFrame)
            {
                army.SelectGroup(null);
                return;
            }
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) army.SelectGroup(null);
        }

        /// <summary>
        /// Returns false only for a tap on a BUILDING: command mode ends and the same tap opens
        /// that building's card. Everything else is an order, including one that resolves nowhere
        /// walkable -- that gets a red marker rather than the silence it used to get.
        /// </summary>
        public bool HandleWorldTap(Vector2 screenPosition)
        {
            if (targetCamera == null) return false;

            var army = ArmyManager.Instance;
            if (army == null || army.SelectedGroup == null) return false;

            // a ship under the finger: the group walks to the beach it stands at and goes aboard
            // (the user, 2026-10-28: select a unit, tap the ship)
            var ship = Ships.ShipInput.ShipAt(screenPosition, targetCamera);
            if (ship != null && Ships.ShipManager.Instance != null)
            {
                var boarding = army.SelectedGroup;
                var blocker = Ships.ShipManager.Instance.DescribeBoardBlocker(boarding, ship);
                if (blocker == null && Ships.ShipManager.Instance.OrderBoard(boarding, ship, out var landing))
                {
                    boarding.NotifyPlayerOrder();
                    if (boarding.Count > 0) OrderMarker.ShowFlagFor(boarding.Members[0].transform, landing);
                }
                else
                {
                    OrderMarker.HideOrder();
                    OrderMarker.ShowRefused(ship.transform.position);
                    if (blocker != null) EventLogManager.Instance?.Post(GameEvent.ForSettlement(blocker, EventSeverity.Warning, EventGlyph.Alert, null));
                }
                return true;
            }

            var ray = targetCamera.ScreenPointToRay(screenPosition);
            var hitCount = Physics.RaycastNonAlloc(ray, _hits, 500f, ~0);

            var enemy = FindNearestEnemy(hitCount);
            if (enemy != null)
            {
                army.SelectedGroup.OrderAttack(enemy);
                army.SelectedGroup.NotifyPlayerOrder();
                OrderMarker.HideOrder();
                OrderMarker.ShowRefused(enemy.Transform.position);
                return true;
            }

            for (var i = 0; i < hitCount; i++)
            {
                if (_hits[i].collider.GetComponentInParent<BuildingInstance>() == null) continue;
                army.SelectGroup(null);
                return false;
            }

            if (!TryResolveDestination(ray, hitCount, out var destination))
            {
                // A refused order used to look exactly like an order that landed off-screen.
                if (hitCount > 0) OrderMarker.ShowRefused(_hits[0].point);
                return true;
            }

            var group = army.SelectedGroup;
            group.OrderMoveTo(destination);
            group.NotifyPlayerOrder();
            if (group.Count > 0) OrderMarker.ShowFlagFor(group.Members[0].transform, destination);
            return true;
        }

        /// <summary>Nearest attackable thing under the finger. Scans every hit, not just the closest collider, so a tree standing in front of an orc doesn't swallow the order.</summary>
        private IDamageTarget FindNearestEnemy(int hitCount)
        {
            IDamageTarget nearest = null;
            var nearestDistance = float.MaxValue;

            for (var i = 0; i < hitCount; i++)
            {
                if (_hits[i].distance >= nearestDistance) continue;

                var candidate = _hits[i].collider.GetComponentInParent<IDamageTarget>();
                // a ship at sea is out of a soldier's reach: the war ships deal with the orcs' fleet
                if (candidate == null || !candidate.IsAlive || candidate is Ships.ShipUnit) continue;

                nearest = candidate;
                nearestDistance = _hits[i].distance;
            }

            return nearest;
        }

        private bool TryResolveDestination(Ray ray, int hitCount, out Vector3 destination)
        {
            var mapApplier = MeshMapApplier.Instance;
            var hasPoint = false;
            var point = Vector3.zero;

            if (mapApplier != null && mapApplier.TryRaycastGround(ray, out var groundHit))
            {
                point = groundHit.point;
                hasPoint = true;
            }
            else
            {
                // Legacy PNG maps (no ground mesh to ask): nearest solid hit, triggers skipped.
                for (var i = 0; i < hitCount; i++)
                {
                    if (_hits[i].collider.isTrigger) continue;
                    if (hasPoint && _hits[i].distance >= Vector3.Distance(ray.origin, point)) continue;
                    point = _hits[i].point;
                    hasPoint = true;
                }
            }

            if (!hasPoint)
            {
                destination = default;
                return false;
            }

            if (NavMesh.SamplePosition(point, out var navHit, DestinationSampleRadius, NavMesh.AllAreas))
            {
                destination = navHit.position;
                return true;
            }

            destination = point;
            return mapApplier == null || mapApplier.IsGroundAt(point);
        }

        /// <summary>
        /// The order on the ground, as a citizen's looks (MoveOrderMarker): a march plants the flag,
        /// which comes down when the group's first soldier -- or the King -- gets there; an attack or
        /// a refused tap is a red ripple. On a phone the group can be off-screen or slow to start, and
        /// without it an order that landed looks identical to one that didn't.
        /// </summary>
        private MoveOrderMarker OrderMarker
        {
            get
            {
                if (_orderMarker != null) return _orderMarker;
                // its own object: this input lives under the UI canvas
                _orderMarker = new GameObject("ArmyOrderMarker").AddComponent<MoveOrderMarker>();
                _orderMarker.Configure(orderFlagPrefab);
                return _orderMarker;
            }
        }
    }
}
