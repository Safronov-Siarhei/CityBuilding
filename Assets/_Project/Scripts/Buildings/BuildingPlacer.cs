using System;
using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CityBuilder.Buildings
{
    public class BuildingPlacer : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask groundLayerMask = ~0;
        [SerializeField] private List<BuildingData> availableBuildings = new List<BuildingData>();
        [SerializeField] private BuildingData mandatoryFirstBuilding;
        [SerializeField] private Color validColor = new Color(0.2f, 1f, 0.2f, 0.6f);
        [SerializeField] private Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.6f);

        private BuildingData _selectedBuilding;
        private GameObject _ghostInstance;
        private readonly List<Renderer> _ghostRenderers = new List<Renderer>();
        // Cached once per renderer in SelectBuilding instead of reading Renderer.materials (which
        // allocates a fresh array, instantiating unique Material copies on top of that the first
        // time) on every single Update() while the ghost is following the cursor -- placement can
        // sit open for several seconds, so that was a steady per-frame GC allocation the whole time.
        private readonly List<Material[]> _ghostMaterials = new List<Material[]>();
        private Color? _lastGhostColor;
        private bool _mandatoryBuildingPlaced;

        /// <summary>Seconds between two "you cannot afford this" lines, so a player tapping repeatedly at an unaffordable building does not push everything else out of an eight-line log.</summary>
        private const float AffordabilityComplaintCooldown = 3f;

        private float _lastAffordabilityComplaint = -999f;

        // 0-3, each a 90-degree step around Y. Reset to 0 on every new SelectBuilding so rotation
        // never carries over from whatever the player was placing before.
        private int _rotationSteps;

        public bool IsPlacingMandatoryBuilding => mandatoryFirstBuilding != null && !_mandatoryBuildingPlaced;
        public bool IsSelecting => _selectedBuilding != null;
        public IReadOnlyList<BuildingData> AvailableBuildings => availableBuildings;

        /// <summary>The building the player has to put down before anything else (the Town Hall). Deliberately not in AvailableBuildings -- it never appears in the hotbar -- which makes this the only way to reach it from outside.</summary>
        public BuildingData MandatoryFirstBuilding => mandatoryFirstBuilding;
        public event Action<BuildingData> OnBuildingPlaced;

        private void Start()
        {
            if (IsPlacingMandatoryBuilding)
            {
                SelectBuilding(mandatoryFirstBuilding);
            }
        }

        /// <summary>
        /// Called by save/load when restoring a game where the Town Hall was already placed,
        /// so Start() doesn't force-select it again for a fresh mandatory-placement flow.
        /// </summary>
        public void MarkMandatoryBuildingAlreadyPlaced()
        {
            _mandatoryBuildingPlaced = true;
        }

        private void Update()
        {
            if (ModalGate.IsBlocked) return;

            var pointer = Pointer.current;
            if (pointer == null) return;

            var forcedSelection = IsPlacingMandatoryBuilding;

            if (!forcedSelection)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    for (var i = 0; i < availableBuildings.Count && i < 9; i++)
                    {
                        if (keyboard[Key.Digit1 + i].wasPressedThisFrame)
                        {
                            SelectBuilding(availableBuildings[i]);
                        }
                    }

                    if (keyboard[Key.Escape].wasPressedThisFrame)
                    {
                        ClearSelection();
                    }
                }

                if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                {
                    ClearSelection();
                }
            }

            if (_selectedBuilding == null) return;

            // Rotation is allowed even while placing the mandatory Town Hall (it's not a
            // cancel/switch action, so it doesn't need the forcedSelection guard above).
            var rotateKeyboard = Keyboard.current;
            if (rotateKeyboard != null && rotateKeyboard[Key.R].wasPressedThisFrame)
            {
                RotateSelection();
            }

            if (IsPointerOverUI()) return;

            if (TryGetGroundCell(pointer, out var cell))
            {
                UpdateGhost(cell);

                if (pointer.press.wasPressedThisFrame)
                {
                    TryPlace(cell);
                }
            }
        }

        public void SelectBuilding(BuildingData data)
        {
            // A building still locked in the Laboratory cannot be picked up at all -- the hotbar
            // already hides it (see BuildingCategoryPanel), so this catches the number keys and
            // anything else reaching in from outside.
            if (data != null && data != mandatoryFirstBuilding
                && !Research.ResearchManager.BuildingUnlocked(data.buildingName)) return;

            ClearSelection();
            _selectedBuilding = data;
            _rotationSteps = 0;
            if (_selectedBuilding.prefab == null) return;

            _ghostInstance = Instantiate(_selectedBuilding.prefab);
            _ghostRenderers.Clear();
            _ghostRenderers.AddRange(_ghostInstance.GetComponentsInChildren<Renderer>());
            foreach (var col in _ghostInstance.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }

            _ghostMaterials.Clear();
            foreach (var rend in _ghostRenderers)
            {
                // One-time instantiation of this renderer's material copies -- every later
                // UpdateGhost call reuses this same array instead of re-fetching/re-instantiating.
                _ghostMaterials.Add(rend.materials);
            }
            _lastGhostColor = null;
        }

        public void ClearSelection()
        {
            _selectedBuilding = null;
            HarvestRadiusOverlay.HideIfShown();
            if (_ghostInstance != null) Destroy(_ghostInstance);
            _ghostInstance = null;
            _ghostRenderers.Clear();
            _ghostMaterials.Clear();
            _lastGhostColor = null;
        }

        /// <summary>Rotates the current ghost/pending placement by 90 degrees -- PC 'R' key, or the mobile rotate button (see SetupProject's ShowWhileSelectingBuilding-gated button).</summary>
        public void RotateSelection()
        {
            if (_selectedBuilding == null) return;
            _rotationSteps = (_rotationSteps + 1) % 4;
        }

        /// <summary>Swaps X/Z for a 90 or 270 degree rotation -- a non-square footprint occupies different grid cells once turned on its side.</summary>
        private Vector2Int RotatedFootprint(Vector2Int footprint)
        {
            return _rotationSteps % 2 == 0 ? footprint : new Vector2Int(footprint.y, footprint.x);
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

        private bool TryGetGroundCell(Pointer pointer, out Vector2Int cell)
        {
            cell = default;
            if (targetCamera == null) return false;

            var ray = targetCamera.ScreenPointToRay(pointer.position.ReadValue());

            // The ground mesh asked directly, when there is one (see MeshMapApplier), instead of
            // a scene-wide ray that reports whatever collider it meets first. Anything standing on
            // the ground -- a tree's click box, a boulder, an authored zone volume -- is hit
            // metres above and beside the ground under the cursor, and the ghost then previews a
            // cell up to a metre off from where the player is pointing.
            var mapApplier = MeshMapApplier.Instance;
            if (mapApplier != null)
            {
                if (!mapApplier.TryRaycastGround(ray, out var groundHit)) return false;
                cell = GridManager.Instance.WorldToCell(groundHit.point);
                return true;
            }

            // Legacy PNG maps have no ground mesh to query -- fall back to the scene ray, still
            // ignoring triggers so tree/boulder click boxes stay out of it.
            if (!Physics.Raycast(ray, out var hit, 500f, groundLayerMask, QueryTriggerInteraction.Ignore)) return false;

            cell = GridManager.Instance.WorldToCell(hit.point);
            return true;
        }

        private void UpdateGhost(Vector2Int cell)
        {
            if (_ghostInstance == null) return;

            var footprint = RotatedFootprint(_selectedBuilding.footprintSize);
            var center = GridManager.Instance.GetFootprintCenterWorld(cell, footprint);
            _ghostInstance.transform.position = center;
            _ghostInstance.transform.rotation = Quaternion.Euler(0f, _rotationSteps * 90f, 0f);

            // What a gatherer will be able to reach from here, laid out in cells under the ghost.
            // Level 1's radius, because that is what is about to be built -- see
            // HarvestRadiusOverlay for why this is worth showing before the decision rather than
            // after it. Costs nothing for the other 47 buildings: a zero radius hides it.
            HarvestRadiusOverlay.ShowFor(center, _selectedBuilding.LevelStats(1).harvestRadius);

            var canPlace = CanPlaceSelectedBuilding(cell, footprint);
            var canAfford = ResourceManager.Instance == null || ResourceManager.Instance.HasEnough(_selectedBuilding.cost);
            var hasRequirement = HasRequiredBuilding();
            var color = (canPlace && canAfford && hasRequirement) ? validColor : invalidColor;

            // The color only actually changes when validity flips (moving between two invalid
            // cells re-applies the same red every frame otherwise) -- skips the Material.color
            // write (and the shader property-block dirtying that comes with it) on every one of
            // the many frames where nothing about the ghost's tint needs to change.
            if (_lastGhostColor.HasValue && _lastGhostColor.Value == color) return;
            _lastGhostColor = color;

            foreach (var materials in _ghostMaterials)
            {
                foreach (var mat in materials)
                {
                    if (mat != null) mat.color = color;
                }
            }
        }

        /// <summary>
        /// GridManager.CanPlace plus a mesh-map-aware exception: a water-category building
        /// (BuildingData.isWaterCategory) may go where a normal building can't (a cell that's
        /// water-blocked), but only within the map's water-placement zone, and its footprint must
        /// be entirely water -- never touching dry Ground. A normal building is the mirror image:
        /// every footprint cell must be dry land. Water itself isn't tracked in GridManager's
        /// occupancy set (see MeshMapApplier), specifically so this exception is possible.
        /// </summary>
        private bool CanPlaceSelectedBuilding(Vector2Int cell, Vector2Int footprint)
        {
            if (!GridManager.Instance.IsWithinBounds(cell, footprint) || !GridManager.Instance.IsAreaFree(cell, footprint)) return false;

            var mapApplier = MeshMapApplier.Instance;
            if (mapApplier == null) return true;

            for (var x = 0; x < footprint.x; x++)
            {
                for (var z = 0; z < footprint.y; z++)
                {
                    var c = cell + new Vector2Int(x, z);
                    var isWater = mapApplier.IsWaterCell(c);

                    if (_selectedBuilding.isWaterCategory)
                    {
                        if (isWater && mapApplier.IsWaterPlacementZone(c)) continue;
                        return false;
                    }

                    if (!isWater) continue;
                    return false;
                }
            }
            return true;
        }

        /// <summary>True if _selectedBuilding.requiredBuilding is unset, or at least one instance of it already exists (see BuildingInstance.HasAny) -- e.g. a Cottage needs a House placed first.</summary>
        private bool HasRequiredBuilding()
        {
            var required = _selectedBuilding.requiredBuilding;
            return required == null || BuildingInstance.HasAny(required.buildingName);
        }

        private void TryPlace(Vector2Int cell)
        {
            var footprint = RotatedFootprint(_selectedBuilding.footprintSize);
            if (!CanPlaceSelectedBuilding(cell, footprint)) return;
            if (!HasRequiredBuilding()) return;
            if (ResourceManager.Instance != null && !ResourceManager.Instance.TrySpend(_selectedBuilding.cost))
            {
                // Said out loud, because the silent version is genuinely confusing: the tap does
                // nothing, the building stays on the finger, and the only difference between "you
                // cannot afford this" and "the game is broken" is a number in the HUD the player
                // was not looking at. Throttled so holding a finger down does not flood the log.
                if (Time.unscaledTime - _lastAffordabilityComplaint > AffordabilityComplaintCooldown)
                {
                    _lastAffordabilityComplaint = Time.unscaledTime;
                    EventLogManager.Instance?.Log(Localization.Format("#log_build_no_resources", _selectedBuilding.LocalizedName));
                }
                return;
            }

            var center = GridManager.Instance.GetFootprintCenterWorld(cell, footprint);
            var rotation = Quaternion.Euler(0f, _rotationSteps * 90f, 0f);
            var instance = Instantiate(_selectedBuilding.prefab, center, rotation);

            var buildingInstance = instance.GetComponent<BuildingInstance>();
            if (buildingInstance == null) buildingInstance = instance.AddComponent<BuildingInstance>();
            buildingInstance.Initialize(_selectedBuilding, cell, _rotationSteps);

            GridManager.Instance.SetAreaOccupied(cell, footprint, true);

            if (_selectedBuilding.isRoad && RoadNetwork.Instance != null)
            {
                for (var x = 0; x < footprint.x; x++)
                {
                    for (var z = 0; z < footprint.y; z++)
                    {
                        RoadNetwork.Instance.RegisterRoad(cell + new Vector2Int(x, z));
                    }
                }
            }

            var placedData = _selectedBuilding;
            var wasMandatory = _selectedBuilding == mandatoryFirstBuilding;

            // Roads excluded -- laying a dozen tiles in a row would otherwise flood the log out
            // of usefulness (see EventLogManager).
            if (!placedData.isRoad) EventLogManager.Instance?.Log(Localization.Format("#log_built", placedData.LocalizedName));

            // Roads (and anything else marked keepSelectedAfterPlacement) stay selected so the
            // player can lay several tiles in a row without reopening the hotbar each time.
            if (!placedData.keepSelectedAfterPlacement)
            {
                _selectedBuilding = null;
                if (_ghostInstance != null) Destroy(_ghostInstance);
                _ghostInstance = null;
                _ghostRenderers.Clear();
            }
            if (wasMandatory)
            {
                _mandatoryBuildingPlaced = true;
                // The mandatory Town Hall's own reveal already ran above (Initialize runs before
                // this point) -- fog only starts covering the rest of the map from here.
                FogOfWarManager.Instance?.Activate();
            }

            OnBuildingPlaced?.Invoke(placedData);
        }
    }
}
