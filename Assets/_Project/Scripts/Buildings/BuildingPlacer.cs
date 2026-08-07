using System;
using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Grid;
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

            var footprint = _selectedBuilding.footprintSize;
            var center = GridManager.Instance.GetFootprintCenterWorld(cell, footprint);
            _ghostInstance.transform.position = center;

            var canPlace = GridManager.Instance.CanPlace(cell, footprint);
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

        private void TryPlace(Vector2Int cell)
        {
            var footprint = _selectedBuilding.footprintSize;
            if (!GridManager.Instance.CanPlace(cell, footprint)) return;
            if (ResourceManager.Instance != null && !ResourceManager.Instance.TrySpend(_selectedBuilding.cost)) return;

            var center = GridManager.Instance.GetFootprintCenterWorld(cell, footprint);
            var instance = Instantiate(_selectedBuilding.prefab, center, Quaternion.identity);

            var buildingInstance = instance.GetComponent<BuildingInstance>();
            if (buildingInstance == null) buildingInstance = instance.AddComponent<BuildingInstance>();
            buildingInstance.Initialize(_selectedBuilding, cell);

            GridManager.Instance.SetAreaOccupied(cell, footprint, true);

            var placedData = _selectedBuilding;
            var wasMandatory = _selectedBuilding == mandatoryFirstBuilding;
            _selectedBuilding = null;
            if (_ghostInstance != null) Destroy(_ghostInstance);
            _ghostInstance = null;
            _ghostRenderers.Clear();
            if (wasMandatory) _mandatoryBuildingPlaced = true;

            OnBuildingPlaced?.Invoke(placedData);
        }
    }
}
