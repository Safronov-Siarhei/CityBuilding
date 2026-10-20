using System;
using System.Collections.Generic;
using System.Text;
using CityBuilder.CameraControl;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.InputControl;
using CityBuilder.Maps;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// Placement, in two flavours that exist because a finger and a mouse are not the same tool.
    ///
    /// **Mouse** keeps the flow it always had: the ghost follows the cursor, a click puts the
    /// building down. That works because a mouse HOVERS -- the player can see the preview before
    /// committing to anything.
    ///
    /// **Touch has no hover at all**, and the old code pretended otherwise: it placed on
    /// <c>press.wasPressedThisFrame</c>, so the building went down at the first pixel the finger
    /// touched, sight unseen, with the resources already spent. There was no way to aim. The phone
    /// flow instead pins the ghost to an AIM POINT fixed on screen (up in the clear area above the
    /// hotbar, where a finger cannot cover it) and lets the player drag the world underneath it
    /// until the right cell is under the ghost, then confirm. The aim point never moves: the
    /// building travels WITH the camera, so the only way to aim is to move the world, and there is
    /// no gesture that can throw the ghost somewhere the player did not mean.
    ///
    /// Roads and fences (anything <see cref="BuildingData.keepSelectedAfterPlacement"/>) get a
    /// third mode on top: the finger DRAWS a line of them, and the camera moves on two fingers for
    /// as long as that mode is open. A road follows the finger freely; a fence snaps to a straight
    /// axis, because a fence is nearly always a perimeter and a hand-drawn wobble in one cannot be
    /// taken back a section at a time.
    /// </summary>
    public class BuildingPlacer : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private RTSCameraController cameraController;
        [SerializeField] private LayerMask groundLayerMask = ~0;
        [SerializeField] private List<BuildingData> availableBuildings = new List<BuildingData>();
        [SerializeField] private BuildingData mandatoryFirstBuilding;

        /// <summary>Height of the aim point as a fraction of the screen, measured from the bottom. Above the hotbar and the confirm row, and high enough that the hand holding the phone is not over it.</summary>
        [SerializeField] private float aimScreenHeightFraction = 0.58f;

        private BuildingData _selectedBuilding;
        private GameObject _ghostInstance;
        private readonly List<Renderer> _ghostRenderers = new List<Renderer>();
        // The ghost's look: an ink drawing over a pen sketch of its plot (see PlacementPreview).
        private PlacementPreview _preview;
        // Asked per cell of the footprint every frame the ghost is up; made once, not per frame.
        private Func<Vector2Int, bool> _cellInTheWay;

        /// <summary>Tests only: the origin cell the ghost stands on, if any.</summary>
        public Vector2Int? GhostOriginForTests => _hasCurrentCell ? _currentCell : (Vector2Int?)null;

        /// <summary>Tests only: holds the ghost on this origin cell instead of following the aim point.</summary>
        public Vector2Int? PinnedOriginForTests;
        private bool _mandatoryBuildingPlaced;

        // 0-3, each a 90-degree step around Y. Reset to 0 on every new SelectBuilding so rotation
        // never carries over from whatever the player was placing before.
        private int _rotationSteps;

        private Vector2Int _currentCell;
        private bool _hasCurrentCell;

        // ---- roads and fences, one piece at a time (the user's call, 2026-10-20) ----
        // After a piece goes down the next is offered right after it, carrying on the way the
        // last two went. On a touch screen the camera glides one piece along so the aim point
        // lands on it; with a mouse the offer holds until the mouse moves.
        private bool _hasLastPiece;
        private Vector2Int _lastPiece;
        private Vector2Int _pieceStep;
        private Vector2Int? _offeredOrigin;
        private Vector2 _offeredAtMouse;
        private readonly StringBuilder _costText = new StringBuilder();

        public bool IsPlacingMandatoryBuilding => mandatoryFirstBuilding != null && !_mandatoryBuildingPlaced;
        public bool IsSelecting => _selectedBuilding != null;
        public IReadOnlyList<BuildingData> AvailableBuildings => availableBuildings;

        /// <summary>Roads and fences: placed one piece at a time like any building, but the next piece is offered straight after.</summary>
        public bool IsChainMode => _selectedBuilding != null && _selectedBuilding.keepSelectedAfterPlacement;

        /// <summary>Whether the player is on a touchscreen, which decides between the aim-and-confirm flow and the mouse's hover-and-click one.</summary>
        public bool UsesTouchAiming => TouchInputRouter.Instance != null && TouchInputRouter.Instance.LastGestureWasTouch;

        /// <summary>Where the ghost is being previewed, in screen pixels -- the HUD puts its crosshair here.</summary>
        public Vector2 AimScreenPosition => CurrentAimScreenPosition;

        /// <summary>Whether the confirm button should be live. Also what tints the ghost.</summary>
        public bool CanConfirm { get; private set; }

        /// <summary>Why the confirm button is dark, in the player's language. Empty when there is nothing to say.</summary>
        public string StatusText { get; private set; } = string.Empty;

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

            HandleKeyboard();

            if (_selectedBuilding == null) return;

            RefreshTargetCell();
        }

        private void HandleKeyboard()
        {
            var keyboard = Keyboard.current;
            var forcedSelection = IsPlacingMandatoryBuilding;

            if (!forcedSelection)
            {
                if (keyboard != null)
                {
                    for (var i = 0; i < availableBuildings.Count && i < 9; i++)
                    {
                        if (keyboard[Key.Digit1 + i].wasPressedThisFrame) SelectBuilding(availableBuildings[i]);
                    }

                    if (keyboard[Key.Escape].wasPressedThisFrame) ClearSelection();
                }

                if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) ClearSelection();
            }

            // Rotation is allowed even while placing the mandatory Town Hall (it's not a
            // cancel/switch action, so it doesn't need the forcedSelection guard above).
            if (_selectedBuilding != null && keyboard != null && keyboard[Key.R].wasPressedThisFrame) RotateSelection();
        }

        /// <summary>
        /// Where the ghost sits: a fixed point on the screen for touch, the cursor for a mouse.
        ///
        /// Recomputed rather than stored, so it stays correct if the surface ever changes size
        /// (split screen, a foldable) instead of pointing at a spot that no longer exists.
        /// </summary>
        private Vector2 CurrentAimScreenPosition
        {
            get
            {
                if (UsesTouchAiming) return FixedAimPoint;

                var mouse = Mouse.current;
                return mouse != null ? mouse.position.ReadValue() : FixedAimPoint;
            }
        }

        /// <summary>Horizontally centred, and above the middle so neither the hotbar nor the hand holding the phone covers the building being aimed.</summary>
        private Vector2 FixedAimPoint =>
            new Vector2(Screen.width * 0.5f, Screen.height * Mathf.Clamp01(aimScreenHeightFraction));

        private void RefreshTargetCell()
        {
            if (PinnedOriginForTests.HasValue)
            {
                _hasCurrentCell = true;
                _currentCell = PinnedOriginForTests.Value;
            }
            else if (OfferStillStands())
            {
                _hasCurrentCell = true;
                _currentCell = _offeredOrigin.Value;
            }
            else _hasCurrentCell = TryGetPlacementOrigin(CurrentAimScreenPosition, out _currentCell);
            if (_hasCurrentCell) UpdateGhost(_currentCell);
            else SetStatus(false, string.Empty);
        }

        // ---------------------------------------------------------------- gestures

        /// <summary>
        /// A confirmed tap on the world while something is selected. On touch a tap on the ghost
        /// confirms it; on a mouse it places, which is the click the desktop flow has always had.
        /// </summary>
        public void HandleWorldTap(Vector2 screenPosition)
        {
            if (_selectedBuilding == null) return;

            if (!UsesTouchAiming)
            {
                if (TryGetPlacementOrigin(screenPosition, out var mouseCell)) TryPlace(mouseCell);
                return;
            }

            // The aim point does NOT move. Tapping the cell the ghost already stands on confirms
            // -- that makes the ghost a second, much bigger confirm target than the button at the
            // far edge of a one-handed phone -- and a tap anywhere else does nothing at all.
            //
            // It used to re-aim, throwing the ghost to wherever the finger landed. That put the
            // building back under the thumb that was covering it and reintroduced exactly the
            // mis-taps this whole flow exists to remove: the ghost is pinned to the screen so it
            // travels WITH the camera, and the only way to move it is to move the world.
            if (_hasCurrentCell && TryGetGroundCell(screenPosition, out var tappedCell) && CoveredByGhost(tappedCell))
            {
                ConfirmPlacement();
            }
        }

        /// <summary>The confirm button. Puts the building on the cell the ghost has been previewing all along.</summary>
        public void ConfirmPlacement()
        {
            if (_selectedBuilding == null || !_hasCurrentCell) return;
            TryPlace(_currentCell);
        }

        // ---------------------------------------------------------------- selection

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

            // The camera stops gliding while anything is being placed: inertia after the finger
            // lifts would slide the aim point off the cell the player just lined up.
            if (cameraController != null) cameraController.InertiaEnabled = false;

            var router = TouchInputRouter.Instance;
            if (router != null) router.SingleDragOwner = DragOwner.Camera;

            if (_selectedBuilding.prefab == null) return;

            _ghostInstance = Instantiate(_selectedBuilding.prefab);
            _ghostRenderers.Clear();
            _ghostRenderers.AddRange(_ghostInstance.GetComponentsInChildren<Renderer>());
            foreach (var col in _ghostInstance.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }

            _preview = PlacementPreview.Attach(_ghostInstance, _ghostRenderers);

            RefreshTargetCell();
        }

        public void ClearSelection()
        {
            _selectedBuilding = null;
            _hasCurrentCell = false;
            _hasLastPiece = false;
            _offeredOrigin = null;
            SetStatus(false, string.Empty);

            HarvestRadiusOverlay.HideIfShown();
            if (_ghostInstance != null) Destroy(_ghostInstance);
            _ghostInstance = null;
            _preview = null;
            _ghostRenderers.Clear();

            if (cameraController != null) cameraController.InertiaEnabled = true;
            var router = TouchInputRouter.Instance;
            if (router != null) router.SingleDragOwner = DragOwner.Camera;
        }

        /// <summary>Rotates the current ghost/pending placement by 90 degrees -- PC 'R' key, or the mobile rotate button.</summary>
        public void RotateSelection()
        {
            if (_selectedBuilding == null) return;
            _rotationSteps = (_rotationSteps + 1) % 4;
            RefreshTargetCell();
        }

        /// <summary>Swaps X/Z for a 90 or 270 degree rotation -- a non-square footprint occupies different grid cells once turned on its side.</summary>
        private Vector2Int RotatedFootprint(Vector2Int footprint)
        {
            return _rotationSteps % 2 == 0 ? footprint : new Vector2Int(footprint.y, footprint.x);
        }

        private bool TryGetGroundCell(Vector2 screenPosition, out Vector2Int cell)
        {
            cell = default;
            if (targetCamera == null) return false;

            var ray = targetCamera.ScreenPointToRay(screenPosition);

            // The ground mesh asked directly, when there is one (see MeshMapApplier), instead of
            // a scene-wide ray that reports whatever collider it meets first. Anything standing on
            // the ground -- a tree's click box, a boulder, an authored zone volume -- is hit
            // metres above and beside the ground under the aim point, and the ghost then previews
            // a cell up to a metre off from where the player is pointing.
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

        /// <summary>
        /// The ORIGIN cell of a footprint CENTRED on this screen point.
        ///
        /// The raw grid lookup answers "which cell is under this pixel", and placement used that
        /// as the origin -- the footprint's lowest corner -- so a 5x5 Town Hall grew up and to the
        /// right of the crosshair instead of standing on it. The player aims at where the BUILDING
        /// should be, not at where its corner should be.
        /// </summary>
        private bool TryGetPlacementOrigin(Vector2 screenPosition, out Vector2Int origin)
        {
            origin = default;
            if (!TryGetGroundCell(screenPosition, out var aimCell)) return false;

            origin = OriginForCentredFootprint(aimCell, RotatedFootprint(_selectedBuilding.footprintSize));
            return true;
        }

        /// <summary>
        /// Half the footprint, back from the aimed cell. For an ODD size this puts the aimed cell
        /// exactly in the middle; for an even one the true centre falls on a cell boundary, and
        /// this rounds the building half a cell towards the origin -- there is no cell to centre on.
        ///
        /// Pure and static so it is covered by a test without a scene or a grid.
        /// </summary>
        public static Vector2Int OriginForCentredFootprint(Vector2Int aimCell, Vector2Int footprint)
        {
            return aimCell - new Vector2Int(footprint.x / 2, footprint.y / 2);
        }

        /// <summary>Whether this cell is one of the cells the ghost currently covers -- the confirm tap accepts anywhere on the building, which on a 5x5 is a far easier target than one cell.</summary>
        private bool CoveredByGhost(Vector2Int cell)
        {
            var footprint = RotatedFootprint(_selectedBuilding.footprintSize);
            var delta = cell - _currentCell;
            return delta.x >= 0 && delta.y >= 0 && delta.x < footprint.x && delta.y < footprint.y;
        }

        private void UpdateGhost(Vector2Int cell)
        {
            var footprint = RotatedFootprint(_selectedBuilding.footprintSize);
            var center = GridManager.Instance.GetFootprintCenterWorld(cell, footprint);

            const bool ghostVisible = true;

            if (_ghostInstance != null)
            {
                if (_ghostInstance.activeSelf != ghostVisible) _ghostInstance.SetActive(ghostVisible);
                if (_preview != null) _preview.SetVisible(ghostVisible);
                else
                {
                    _ghostInstance.transform.position = center;
                    _ghostInstance.transform.rotation = Quaternion.Euler(0f, _rotationSteps * 90f, 0f);
                }
            }

            // What a gatherer will be able to reach from here, laid out in cells under the ghost.
            // Level 1's radius, because that is what is about to be built -- see
            // HarvestRadiusOverlay for why this is worth showing before the decision rather than
            // after it. Costs nothing for the other 47 buildings: a zero radius hides it.
            if (ghostVisible) HarvestRadiusOverlay.ShowFor(center, _selectedBuilding.LevelStats(1).harvestRadius);

            var canPlace = CanPlaceSelectedBuilding(cell, footprint);
            var canAfford = ResourceManager.Instance == null || ResourceManager.Instance.HasEnough(_selectedBuilding.cost);
            var hasRequirement = HasRequiredBuilding();
            var ok = canPlace && canAfford && hasRequirement;

            SetStatus(ok, ok ? string.Empty : DescribeBlock(canPlace, canAfford, hasRequirement, cell, footprint));

            if (_ghostInstance == null) return;

            if (_preview == null) return;
            _cellInTheWay ??= c => !CanPlaceSelectedBuilding(c, Vector2Int.one, wholeFootprintRules: false);
            _preview.Show(GridManager.Instance.GetFootprintCenterWorld(cell, footprint), Quaternion.Euler(0f, _rotationSteps * 90f, 0f),
                cell, footprint, ok, _cellInTheWay);
        }

        private void SetStatus(bool canConfirm, string status)
        {
            CanConfirm = canConfirm;
            StatusText = status ?? string.Empty;
        }

        /// <summary>
        /// Why this cell is refused, in one short line. The old build had none of this: an
        /// unaffordable building was a red ghost and a log line that scrolled away, and "occupied"
        /// and "you cannot afford it" looked exactly alike.
        /// </summary>
        private string DescribeBlock(bool canPlace, bool canAfford, bool hasRequirement, Vector2Int cell, Vector2Int footprint)
        {
            if (!hasRequirement)
            {
                var required = _selectedBuilding.requiredBuilding;
                return Localization.Format("#place_blocked_requires", required != null ? required.LocalizedName : string.Empty);
            }

            if (!canPlace)
            {
                if (IsTownHall(_selectedBuilding) && !StartZone.Allows(cell, footprint)) return Localization.Get("#place_blocked_start_zone");
                var mapApplier = MeshMapApplier.Instance;
                if (mapApplier != null && _selectedBuilding.isWaterCategory) return Localization.Get("#place_blocked_water");
                if (mapApplier != null && FootprintTouchesWater(mapApplier, cell, footprint)) return Localization.Get("#place_blocked_land");
                return Localization.Get("#place_blocked_occupied");
            }

            if (!canAfford) return Localization.Format("#place_blocked_cost", MissingResourcesText());

            return string.Empty;
        }

        private static bool IsTownHall(BuildingData data) => data != null && data.buildingName == BuildingIds.TownHall;

        private static bool FootprintTouchesWater(MeshMapApplier mapApplier, Vector2Int cell, Vector2Int footprint)
        {
            for (var x = 0; x < footprint.x; x++)
            {
                for (var z = 0; z < footprint.y; z++)
                {
                    if (mapApplier.IsWaterCell(cell + new Vector2Int(x, z))) return true;
                }
            }
            return false;
        }

        /// <summary>Only the resources actually short, and by how much -- the full price list is already on the hotbar button.</summary>
        private string MissingResourcesText()
        {
            _costText.Clear();
            var manager = ResourceManager.Instance;

            foreach (var amount in _selectedBuilding.cost)
            {
                var have = manager != null ? manager.GetAmount(amount.type) : 0;
                var missing = amount.amount - have;
                if (missing <= 0) continue;

                if (_costText.Length > 0) _costText.Append(", ");
                _costText.Append(ResourceNames.Of(amount.type)).Append(' ').Append(missing);
            }

            return _costText.ToString();
        }

        /// <summary>
        /// GridManager.CanPlace plus a mesh-map-aware exception: a water-category building
        /// (BuildingData.isWaterCategory) may go where a normal building can't (a cell that's
        /// water-blocked), but only within the map's water-placement zone, and its footprint must
        /// be entirely water -- never touching dry Ground. A normal building is the mirror image:
        /// every footprint cell must be dry land. Water itself isn't tracked in GridManager's
        /// occupancy set (see MeshMapApplier), specifically so this exception is possible.
        /// </summary>
        private bool CanPlaceSelectedBuilding(Vector2Int cell, Vector2Int footprint, bool wholeFootprintRules = true)
        {
            if (!GridManager.Instance.IsWithinBounds(cell, footprint) || !GridManager.Instance.IsAreaFree(cell, footprint)) return false;
            // the start zone is a rule about the whole building, not about any one cell of it
            if (wholeFootprintRules && IsTownHall(_selectedBuilding) && !StartZone.Allows(cell, footprint)) return false;

            var mapApplier = MeshMapApplier.Instance;
            if (mapApplier == null) return true;

            for (var x = 0; x < footprint.x; x++)
            {
                for (var z = 0; z < footprint.y; z++)
                {
                    var c = cell + new Vector2Int(x, z);
                    // The slopes of the map's hills and mountains: scenery, never a building site.
                    if (mapApplier.IsNoBuildCell(c)) return false;
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

            var placedData = _selectedBuilding;
            if (!PlaceAt(cell)) return;

            // Roads and fences stay in hand, and the next piece is offered right after this one.
            if (!placedData.keepSelectedAfterPlacement) ClearSelection();
            else
            {
                OfferNextPiece(cell);
                RefreshTargetCell();
            }
        }

        /// <summary>
        /// Puts the next road or fence piece right after the one just laid: on in the direction of
        /// the last two, or -- for the first piece -- towards the right of the screen, the way a
        /// line is read. A touch screen glides the camera one piece along so the aim point is on
        /// it; a mouse holds the offer until it moves.
        /// </summary>
        private void OfferNextPiece(Vector2Int placed)
        {
            var footprint = RotatedFootprint(_selectedBuilding.footprintSize);
            var delta = placed - _lastPiece;
            if (_hasLastPiece && Mathf.Abs(delta.x) + Mathf.Abs(delta.y) > 0 && (delta.x == 0 || delta.y == 0)
                && Mathf.Abs(delta.x) <= footprint.x && Mathf.Abs(delta.y) <= footprint.y)
            {
                _pieceStep = new Vector2Int(Math.Sign(delta.x) * footprint.x, Math.Sign(delta.y) * footprint.y);
            }
            else
            {
                _pieceStep = ScreenRightStep(footprint);
            }
            _hasLastPiece = true;
            _lastPiece = placed;

            var next = placed + _pieceStep;
            _offeredOrigin = next;
            _offeredAtMouse = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

            if (UsesTouchAiming && cameraController != null)
            {
                var grid = GridManager.Instance;
                var shift = grid.CellToWorld(next) - grid.CellToWorld(placed);
                cameraController.FocusOn(cameraController.transform.position + shift);
            }
        }

        /// <summary>The grid step that runs most nearly to the right across the screen.</summary>
        private Vector2Int ScreenRightStep(Vector2Int footprint)
        {
            var right = targetCamera != null ? targetCamera.transform.right : Vector3.right;
            return Mathf.Abs(right.x) >= Mathf.Abs(right.z)
                ? new Vector2Int(right.x >= 0f ? footprint.x : -footprint.x, 0)
                : new Vector2Int(0, right.z >= 0f ? footprint.y : -footprint.y);
        }

        /// <summary>
        /// Whether the next piece is still where it was offered. On touch: while the camera glides
        /// there, and after that for as long as the aim point rests on it -- a pan of the player's
        /// own takes the ghost back to the aim point. With a mouse: until the mouse moves.
        /// </summary>
        private bool OfferStillStands()
        {
            if (!_offeredOrigin.HasValue) return false;
            if (UsesTouchAiming)
            {
                if (cameraController != null && cameraController.IsFocusing) return true;
                if (TryGetPlacementOrigin(CurrentAimScreenPosition, out var aim) && aim == _offeredOrigin.Value) return true;
            }
            else if (Mouse.current == null || (Mouse.current.position.ReadValue() - _offeredAtMouse).sqrMagnitude < 16f)
            {
                return true;
            }
            _offeredOrigin = null;
            return false;
        }

        /// <summary>
        /// Spends and builds, with no opinion about what happens to the selection afterwards --
        /// roads and fences stay in hand afterwards, everything else is put down.
        /// </summary>
        private bool PlaceAt(Vector2Int cell)
        {
            var footprint = RotatedFootprint(_selectedBuilding.footprintSize);

            if (ResourceManager.Instance != null && !ResourceManager.Instance.TrySpend(_selectedBuilding.cost))
            {
                // The status line above the confirm button already says what is missing; the log
                // line is what a player who tapped without looking at it will find afterwards.
                EventLogManager.Instance?.Log(Localization.Format("#log_build_no_resources", _selectedBuilding.LocalizedName));
                return false;
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

            // Roads excluded -- laying a dozen tiles in a row would otherwise flood the log out
            // of usefulness (see EventLogManager).
            if (!placedData.isRoad) EventLogManager.Instance?.Log(Localization.Format("#log_built", placedData.LocalizedName));

            if (placedData == mandatoryFirstBuilding && !_mandatoryBuildingPlaced)
            {
                _mandatoryBuildingPlaced = true;
                // The mandatory Town Hall's own reveal already ran above (Initialize runs before
                // this point) -- fog only starts covering the rest of the map from here.
                FogOfWarManager.Instance?.Activate();
            }

            // A puff of dust as it lands -- not for a road, which is barely more than paint on the ground.
            if (!placedData.isRoad)
            {
                var size = GridManager.Instance.CellSize;
                DustPuff.Spawn(center, new Vector2(footprint.x * size, footprint.y * size));
            }

            OnBuildingPlaced?.Invoke(placedData);
            return true;
        }
    }
}
