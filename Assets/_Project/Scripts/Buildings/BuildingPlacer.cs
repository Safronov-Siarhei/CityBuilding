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
        private bool _mandatoryBuildingPlaced;

        // 0-3, each a 90-degree step around Y. Reset to 0 on every new SelectBuilding so rotation
        // never carries over from whatever the player was placing before.
        private int _rotationSteps;

        public bool IsPlacingMandatoryBuilding => mandatoryFirstBuilding != null && !_mandatoryBuildingPlaced;
        public bool IsSelecting => _selectedBuilding != null;
        public IReadOnlyList<BuildingData> AvailableBuildings => availableBuildings;
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
        }

        public void ClearSelection()
        {
            _selectedBuilding = null;
            if (_ghostInstance != null) Destroy(_ghostInstance);
            _ghostInstance = null;
            _ghostRenderers.Clear();
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
            if (!Physics.Raycast(ray, out var hit, 500f, groundLayerMask)) return false;

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

            var canPlace = CanPlaceSelectedBuilding(cell, footprint);
            var canAfford = ResourceManager.Instance == null || ResourceManager.Instance.HasEnough(_selectedBuilding.cost);
            var color = (canPlace && canAfford) ? validColor : invalidColor;

            foreach (var rend in _ghostRenderers)
            {
                if (rend == null) continue;
                foreach (var mat in rend.materials)
                {
                    mat.color = color;
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

        private void TryPlace(Vector2Int cell)
        {
            var footprint = RotatedFootprint(_selectedBuilding.footprintSize);
            if (!CanPlaceSelectedBuilding(cell, footprint)) return;
            if (ResourceManager.Instance != null && !ResourceManager.Instance.TrySpend(_selectedBuilding.cost)) return;

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

            // Roads (and anything else marked keepSelectedAfterPlacement) stay selected so the
            // player can lay several tiles in a row without reopening the hotbar each time.
            if (!placedData.keepSelectedAfterPlacement)
            {
                _selectedBuilding = null;
                if (_ghostInstance != null) Destroy(_ghostInstance);
                _ghostInstance = null;
                _ghostRenderers.Clear();
            }
            if (wasMandatory) _mandatoryBuildingPlaced = true;

            OnBuildingPlaced?.Invoke(placedData);
        }
    }
}
