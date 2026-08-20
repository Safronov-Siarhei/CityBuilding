using System.Collections;
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

        private const float MarkerSeconds = 1f;
        private const float MarkerHeightOffset = 0.04f;

        private static readonly Color MoveMarkerColor = new Color(0.95f, 0.82f, 0.2f);
        private static readonly Color AttackMarkerColor = new Color(0.9f, 0.3f, 0.25f);
        private static readonly Color RefusedMarkerColor = new Color(0.9f, 0.3f, 0.25f);

        [SerializeField] private Camera targetCamera;

        // Same sizing rationale as CitizenSelector: a shallow camera ray crosses a lot of forest,
        // and RaycastNonAlloc drops hits arbitrarily once the buffer is full.
        private readonly RaycastHit[] _hits = new RaycastHit[64];

        private GameObject _marker;
        private Material _markerMaterial;
        private Coroutine _markerRoutine;

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

            var ray = targetCamera.ScreenPointToRay(screenPosition);
            var hitCount = Physics.RaycastNonAlloc(ray, _hits, 500f, ~0);

            var enemy = FindNearestEnemy(hitCount);
            if (enemy != null)
            {
                army.SelectedGroup.OrderAttack(enemy);
                ShowMarker(enemy.Transform.position, AttackMarkerColor);
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
                if (hitCount > 0) ShowMarker(_hits[0].point, RefusedMarkerColor);
                return true;
            }

            army.SelectedGroup.OrderMoveTo(destination);
            ShowMarker(destination, MoveMarkerColor);
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
    }
}
