using System.Collections;
using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CityBuilder.UI
{
    /// <summary>
    /// Turns a tap on the world into an order for the group the player has selected in the army
    /// panel: tap an enemy to attack it, tap the ground to march there. Does nothing at all while
    /// no group is selected, which is what keeps it from fighting with CitizenSelector and
    /// BuildingSelector over the same tap -- those two stand down for the opposite reason (see
    /// their ArmyManager.SelectedGroup checks).
    ///
    /// The ground point comes from MeshMapApplier.TryRaycastGround rather than from whatever
    /// collider the ray hits first, for exactly the reasons documented there: over a forest the
    /// first collider is a tree's click box or a boulder, metres from where the player pointed.
    /// </summary>
    public class ArmyOrderInput : MonoBehaviour
    {
        /// <summary>How far an order point may be pulled onto walkable NavMesh -- same precision budget as a citizen move order.</summary>
        private const float DestinationSampleRadius = 2f;

        private const float MarkerSeconds = 1f;
        private const float MarkerHeightOffset = 0.04f;

        private static readonly Color MoveMarkerColor = new Color(0.95f, 0.82f, 0.2f);
        private static readonly Color AttackMarkerColor = new Color(0.9f, 0.3f, 0.25f);

        [SerializeField] private Camera targetCamera;
        [SerializeField] private BuildingPlacer buildingPlacer;

        // Same sizing rationale as CitizenSelector: a shallow camera ray crosses a lot of forest,
        // and RaycastNonAlloc drops hits arbitrarily once the buffer is full.
        private readonly RaycastHit[] _hits = new RaycastHit[64];

        private GameObject _marker;
        private Material _markerMaterial;
        private Coroutine _markerRoutine;

        private void Update()
        {
            if (ModalGate.IsBlocked) return;
            if (targetCamera == null) return;
            if (buildingPlacer != null && buildingPlacer.IsSelecting) return;

            var army = ArmyManager.Instance;
            if (army == null || army.SelectedGroup == null) return;

            // Escape / right click releases command mode on desktop; on touch the group's own icon
            // does it (ArmyManager.ToggleSelection).
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[Key.Escape].wasPressedThisFrame)
            {
                army.SelectGroup(null);
                return;
            }
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                army.SelectGroup(null);
                return;
            }

            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;
            if (IsPointerOverUI()) return;

            var ray = targetCamera.ScreenPointToRay(pointer.position.ReadValue());
            var hitCount = Physics.RaycastNonAlloc(ray, _hits, 500f, ~0);

            var enemy = FindNearestEnemy(hitCount);
            if (enemy != null)
            {
                army.SelectedGroup.OrderAttack(enemy);
                ShowMarker(enemy.Transform.position, AttackMarkerColor);
                return;
            }

            if (!TryResolveDestination(ray, hitCount, out var destination)) return;

            army.SelectedGroup.OrderMoveTo(destination);
            ShowMarker(destination, MoveMarkerColor);
        }

        /// <summary>Nearest attackable thing under the cursor. Scans every hit, not just the closest collider, so a tree standing in front of an orc doesn't swallow the order.</summary>
        private IDamageTarget FindNearestEnemy(int hitCount)
        {
            IDamageTarget nearest = null;
            var nearestDistance = float.MaxValue;

            for (var i = 0; i < hitCount; i++)
            {
                if (_hits[i].distance >= nearestDistance) continue;

                var candidate = _hits[i].collider.GetComponentInParent<IDamageTarget>();
                if (candidate == null || !candidate.IsAlive) continue;

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

        /// <summary>Brief flat ring at the ordered spot -- on a phone the group can be off-screen or slow to start, and without it an order that landed looks identical to one that didn't.</summary>
        private void ShowMarker(Vector3 worldPosition, Color color)
        {
            EnsureMarker();

            var grid = GridManager.Instance;
            var y = (grid != null ? grid.GroundHeight : worldPosition.y) + MarkerHeightOffset;
            _marker.transform.position = new Vector3(worldPosition.x, y, worldPosition.z);
            _markerMaterial.color = color;
            _marker.SetActive(true);

            if (_markerRoutine != null) StopCoroutine(_markerRoutine);
            _markerRoutine = StartCoroutine(HideMarkerAfterDelay());
        }

        private IEnumerator HideMarkerAfterDelay()
        {
            yield return new WaitForSeconds(MarkerSeconds);
            if (_marker != null) _marker.SetActive(false);
            _markerRoutine = null;
        }

        private void EnsureMarker()
        {
            if (_marker != null) return;

            _marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _marker.name = "ArmyOrderMarker";
            Destroy(_marker.GetComponent<Collider>());
            // Flat on the ground (local -Z up) rather than standing upright like a default Quad.
            _marker.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _marker.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

            _markerMaterial = new Material(RuntimeShaders.Unlit);
            _marker.GetComponent<Renderer>().sharedMaterial = _markerMaterial;
            _marker.SetActive(false);
        }

        private static bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;

            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
            {
                return EventSystem.current.IsPointerOverGameObject(touchscreen.primaryTouch.touchId.ReadValue());
            }

            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
