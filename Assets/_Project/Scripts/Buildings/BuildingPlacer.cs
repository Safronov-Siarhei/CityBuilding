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
        [SerializeField] private Color validColor = new Color(0.2f, 1f, 0.2f, 0.6f);
        [SerializeField] private Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.6f);

        /// <summary>Height of the aim point as a fraction of the screen, measured from the bottom. Above the hotbar and the confirm row, and high enough that the hand holding the phone is not over it.</summary>
        [SerializeField] private float aimScreenHeightFraction = 0.58f;

        private BuildingData _selectedBuilding;
        private GameObject _ghostInstance;
        private readonly List<Renderer> _ghostRenderers = new List<Renderer>();
        // Cached once per renderer in SelectBuilding instead of reading Renderer.materials (which
        // allocates a fresh array, instantiating unique Material copies on top of that the first
        // time) on every single Update() while the ghost is following the aim point -- placement
        // can sit open for several seconds, so that was a steady per-frame GC allocation.
        private readonly List<Material[]> _ghostMaterials = new List<Material[]>();
        private Color? _lastGhostColor;
        private bool _mandatoryBuildingPlaced;

        // 0-3, each a 90-degree step around Y. Reset to 0 on every new SelectBuilding so rotation
        // never carries over from whatever the player was placing before.
        private int _rotationSteps;

        private Vector2Int _currentCell;
        private bool _hasCurrentCell;

        // ---- line drawing (roads and fences) ----
        private bool _drawing;
        private Vector2Int _lineStart;
        private readonly List<Vector2Int> _lineCells = new List<Vector2Int>();
        private GameObject _linePreviewRoot;
        private readonly List<GameObject> _linePreviewQuads = new List<GameObject>();
        private Material _linePreviewValid;
        private Material _linePreviewInvalid;
        private readonly StringBuilder _costText = new StringBuilder();

        public bool IsPlacingMandatoryBuilding => mandatoryFirstBuilding != null && !_mandatoryBuildingPlaced;
        public bool IsSelecting => _selectedBuilding != null;
        public IReadOnlyList<BuildingData> AvailableBuildings => availableBuildings;

        /// <summary>Roads and fences: placed by drawing a line rather than one tap at a time.</summary>
        public bool IsDrawMode => _selectedBuilding != null && _selectedBuilding.keepSelectedAfterPlacement;

        /// <summary>Whether the player is on a touchscreen, which decides between the aim-and-confirm flow and the mouse's hover-and-click one.</summary>
        public bool UsesTouchAiming => TouchInputRouter.Instance != null && TouchInputRouter.Instance.LastGestureWasTouch;

        /// <summary>Where the ghost is being previewed, in screen pixels -- the HUD puts its crosshair here.</summary>
        public Vector2 AimScreenPosition => CurrentAimScreenPosition;

        /// <summary>True while a line is actually being drawn, so the HUD can swap the drawing hint for the running total.</summary>
        public bool IsDrawingLine => _drawing;

        /// <summary>Whether the confirm button should be live. Also what tints the ghost.</summary>
        public bool CanConfirm { get; private set; }

        /// <summary>Why the confirm button is dark, in the player's language -- or the running total while a line is being drawn. Empty when there is nothing to say.</summary>
        public string StatusText { get; private set; } = string.Empty;

        /// <summary>The building the player has to put down before anything else (the Town Hall). Deliberately not in AvailableBuildings -- it never appears in the hotbar -- which makes this the only way to reach it from outside.</summary>
        public BuildingData MandatoryFirstBuilding => mandatoryFirstBuilding;
        public event Action<BuildingData> OnBuildingPlaced;

        private void Start()
        {
            SubscribeToRouter();

            if (IsPlacingMandatoryBuilding)
            {
                SelectBuilding(mandatoryFirstBuilding);
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromRouter();
        }

        private void SubscribeToRouter()
        {
            var router = TouchInputRouter.Instance;
            if (router == null) return;

            router.DragStarted -= HandleDragStarted;
            router.DragMoved -= HandleDragMoved;
            router.DragEnded -= HandleDragEnded;

            router.DragStarted += HandleDragStarted;
            router.DragMoved += HandleDragMoved;
            router.DragEnded += HandleDragEnded;
        }

        private void UnsubscribeFromRouter()
        {
            var router = TouchInputRouter.Instance;
            if (router == null) return;

            router.DragStarted -= HandleDragStarted;
            router.DragMoved -= HandleDragMoved;
            router.DragEnded -= HandleDragEnded;
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

            // While drawing, the ghost tracks the finger's own cell rather than the aim point --
            // the aim point is a placement idea, and a line has no single cell to aim at.
            if (!_drawing) RefreshTargetCell();
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
            _hasCurrentCell = TryGetPlacementOrigin(CurrentAimScreenPosition, out _currentCell);
            if (_hasCurrentCell) UpdateGhost(_currentCell);
            else SetStatus(false, string.Empty);
        }

        // ---------------------------------------------------------------- gestures

        /// <summary>
        /// A confirmed tap on the world while something is selected. On touch this AIMS (or, in
        /// drawing mode, lays a single cell); on a mouse it places, which is the click the desktop
        /// flow has always had.
        /// </summary>
        public void HandleWorldTap(Vector2 screenPosition)
        {
            if (_selectedBuilding == null) return;

            if (!UsesTouchAiming)
            {
                if (TryGetPlacementOrigin(screenPosition, out var mouseCell)) TryPlace(mouseCell);
                return;
            }

            if (IsDrawMode)
            {
                if (TryGetPlacementOrigin(screenPosition, out var drawCell)) TryPlace(drawCell);
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

        private void HandleDragStarted(Vector2 screenPosition)
        {
            if (!IsDrawMode || !UsesTouchAiming) return;
            if (!TryGetPlacementOrigin(screenPosition, out var cell)) return;

            _drawing = true;
            _lineStart = cell;
            _lineCells.Clear();
            _lineCells.Add(cell);
            UpdateLinePreview();
        }

        private void HandleDragMoved(Vector2 screenPosition)
        {
            if (!_drawing) return;
            if (!TryGetPlacementOrigin(screenPosition, out var cell)) return;

            if (_selectedBuilding.isRoad) ExtendFreeformLine(cell);
            else BuildStraightLine(cell);

            UpdateLinePreview();
        }

        private void HandleDragEnded(bool completed)
        {
            if (!_drawing) return;

            _drawing = false;

            // A second finger means the player wants the camera, not this line. Throwing the whole
            // line away is deliberate: committing a half-drawn road on the way into a pinch would
            // spend resources the player never agreed to.
            if (completed) CommitLine();

            _lineCells.Clear();
            HideLinePreview();
            SetStatus(CanConfirm, string.Empty);
        }

        // ---------------------------------------------------------------- line geometry

        /// <summary>
        /// A road follows the finger. Cells between the last one and this one are filled in, since
        /// a fast drag can skip several cells between frames and a road with holes in it is not
        /// what the player drew.
        /// </summary>
        private void ExtendFreeformLine(Vector2Int cell)
        {
            if (_lineCells.Count > 0 && _lineCells[_lineCells.Count - 1] == cell) return;

            var from = _lineCells.Count > 0 ? _lineCells[_lineCells.Count - 1] : cell;
            AppendInterpolated(from, cell);
        }

        /// <summary>Bresenham-ish walk between two cells, appending everything strictly after `from`.</summary>
        private void AppendInterpolated(Vector2Int from, Vector2Int to)
        {
            var delta = to - from;
            var steps = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
            if (steps == 0) return;

            for (var i = 1; i <= steps; i++)
            {
                var t = i / (float)steps;
                var cell = new Vector2Int(
                    Mathf.RoundToInt(Mathf.Lerp(from.x, to.x, t)),
                    Mathf.RoundToInt(Mathf.Lerp(from.y, to.y, t)));

                if (_lineCells.Contains(cell)) continue;
                _lineCells.Add(cell);
            }
        }

        /// <summary>
        /// A fence runs straight from where the finger landed to one of three axes -- horizontal,
        /// vertical, or the 45-degree diagonal -- whichever the drag is closest to. Rebuilt from
        /// the anchor every frame, so the player can swing the line around before letting go.
        /// </summary>
        private void BuildStraightLine(Vector2Int cell)
        {
            var end = SnapStraightEnd(_lineStart, cell);

            _lineCells.Clear();
            _lineCells.Add(_lineStart);
            AppendInterpolated(_lineStart, end);
        }

        /// <summary>
        /// Where a fence line drawn from `start` toward `target` actually ends: snapped to the
        /// nearest of three axes -- horizontal, vertical, or the 45-degree diagonal.
        ///
        /// The 2:1 rule is what stops the line flickering between axes while the finger travels:
        /// an axis has to be at least twice as long as the other before it wins outright, and
        /// anything in between is read as a diagonal rather than as a near-tie between the two.
        ///
        /// Pure and static so it is testable without a scene, a camera or a grid.
        /// </summary>
        public static Vector2Int SnapStraightEnd(Vector2Int start, Vector2Int target)
        {
            var delta = target - start;
            var ax = Mathf.Abs(delta.x);
            var az = Mathf.Abs(delta.y);

            if (ax >= az * 2) return new Vector2Int(target.x, start.y);
            if (az >= ax * 2) return new Vector2Int(start.x, target.y);

            var length = Mathf.Min(ax, az);
            return start + new Vector2Int(Math.Sign(delta.x) * length, Math.Sign(delta.y) * length);
        }

        /// <summary>
        /// Lays the drawn line, in the order it was drawn. An invalid cell in the middle (occupied,
        /// water, off the map) is SKIPPED rather than ending the line -- a road drawn past the
        /// corner of a building should still come out the other side. Running out of resources
        /// does end it: what gets built is the prefix the player could afford, which is what the
        /// preview promised.
        /// </summary>
        private void CommitLine()
        {
            foreach (var cell in _lineCells)
            {
                var footprint = RotatedFootprint(_selectedBuilding.footprintSize);
                if (!CanPlaceSelectedBuilding(cell, footprint) || !HasRequiredBuilding()) continue;
                if (ResourceManager.Instance != null && !ResourceManager.Instance.HasEnough(_selectedBuilding.cost)) break;

                PlaceAt(cell);
            }

            // Fence shapes need no separate pass: FenceNetwork.Register already re-shapes the new
            // cell AND its neighbours, so each segment fixes the one behind it as the line grows.
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

            // Drawing takes the one-finger drag for itself; everything else leaves it to the camera.
            var router = TouchInputRouter.Instance;
            if (router != null) router.SingleDragOwner = IsDrawMode ? DragOwner.World : DragOwner.Camera;

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

            RefreshTargetCell();
        }

        public void ClearSelection()
        {
            _selectedBuilding = null;
            _hasCurrentCell = false;
            _drawing = false;
            _lineCells.Clear();
            HideLinePreview();
            SetStatus(false, string.Empty);

            HarvestRadiusOverlay.HideIfShown();
            if (_ghostInstance != null) Destroy(_ghostInstance);
            _ghostInstance = null;
            _ghostRenderers.Clear();
            _ghostMaterials.Clear();
            _lastGhostColor = null;

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

            // While drawing on a phone the ghost is in the way: the preview squares under the
            // finger already say where the line goes, and a full building model on top of them
            // hides it.
            var ghostVisible = !(IsDrawMode && UsesTouchAiming);

            if (_ghostInstance != null)
            {
                if (_ghostInstance.activeSelf != ghostVisible) _ghostInstance.SetActive(ghostVisible);
                _ghostInstance.transform.position = center;
                _ghostInstance.transform.rotation = Quaternion.Euler(0f, _rotationSteps * 90f, 0f);
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

            var color = ok ? validColor : invalidColor;

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
                var mapApplier = MeshMapApplier.Instance;
                if (mapApplier != null && _selectedBuilding.isWaterCategory) return Localization.Get("#place_blocked_water");
                if (mapApplier != null && FootprintTouchesWater(mapApplier, cell, footprint)) return Localization.Get("#place_blocked_land");
                return Localization.Get("#place_blocked_occupied");
            }

            if (!canAfford) return Localization.Format("#place_blocked_cost", MissingResourcesText());

            return string.Empty;
        }

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

            var placedData = _selectedBuilding;
            if (!PlaceAt(cell)) return;

            // Roads (and anything else marked keepSelectedAfterPlacement) stay selected so the
            // player can lay several in a row without reopening the hotbar each time.
            if (!placedData.keepSelectedAfterPlacement) ClearSelection();
            else RefreshTargetCell();
        }

        /// <summary>
        /// Spends and builds, with no opinion about what happens to the selection afterwards --
        /// that difference is what lets a drawn line of thirty road tiles reuse the exact same
        /// path as a single tap.
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

            OnBuildingPlaced?.Invoke(placedData);
            return true;
        }

        // ---------------------------------------------------------------- line preview

        /// <summary>
        /// Flat squares over the cells the line would occupy, green where it will build and red
        /// where it will not -- including cells the player cannot afford, which are marked before
        /// the finger lifts rather than silently skipped afterwards.
        /// </summary>
        private void UpdateLinePreview()
        {
            EnsureLinePreviewRoot();

            var affordable = AffordableCellCount();
            var valid = 0;

            for (var i = 0; i < _lineCells.Count; i++)
            {
                var quad = GetPreviewQuad(i);
                var grid = GridManager.Instance;
                var footprint = RotatedFootprint(_selectedBuilding.footprintSize);
                var center = grid.GetFootprintCenterWorld(_lineCells[i], footprint);

                quad.transform.position = new Vector3(center.x, grid.GroundHeight + 0.05f, center.z);
                quad.transform.localScale = Vector3.one * (grid.CellSize * 0.92f);

                var buildable = CanPlaceSelectedBuilding(_lineCells[i], footprint) && HasRequiredBuilding();
                if (buildable) valid++;

                quad.GetComponent<Renderer>().sharedMaterial =
                    buildable && valid <= affordable ? _linePreviewValid : _linePreviewInvalid;
                quad.SetActive(true);
            }

            for (var i = _lineCells.Count; i < _linePreviewQuads.Count; i++) _linePreviewQuads[i].SetActive(false);

            var willBuild = Mathf.Min(valid, affordable);
            SetStatus(willBuild > 0, willBuild > 0
                ? Localization.Format("#draw_line_summary", _selectedBuilding.LocalizedName, willBuild, TotalCostText(willBuild))
                : Localization.Format("#place_blocked_cost", MissingResourcesText()));
        }

        /// <summary>How many copies the player can actually pay for, which is what decides where the green stops.</summary>
        private int AffordableCellCount()
        {
            var manager = ResourceManager.Instance;
            if (manager == null) return _lineCells.Count;

            var affordable = int.MaxValue;
            foreach (var amount in _selectedBuilding.cost)
            {
                if (amount.amount <= 0) continue;
                affordable = Mathf.Min(affordable, manager.GetAmount(amount.type) / amount.amount);
            }
            return affordable == int.MaxValue ? _lineCells.Count : affordable;
        }

        private string TotalCostText(int count)
        {
            _costText.Clear();
            foreach (var amount in _selectedBuilding.cost)
            {
                if (amount.amount <= 0) continue;
                if (_costText.Length > 0) _costText.Append(", ");
                _costText.Append(ResourceNames.Of(amount.type)).Append(' ').Append(amount.amount * count);
            }
            return _costText.ToString();
        }

        private void EnsureLinePreviewRoot()
        {
            if (_linePreviewRoot != null) return;

            _linePreviewRoot = new GameObject("LinePreview");
            _linePreviewValid = new Material(RuntimeShaders.Unlit) { color = new Color(0.3f, 0.95f, 0.35f, 0.55f) };
            _linePreviewInvalid = new Material(RuntimeShaders.Unlit) { color = new Color(0.95f, 0.3f, 0.25f, 0.55f) };
        }

        /// <summary>Pooled: a long road drag would otherwise allocate a quad per cell per frame.</summary>
        private GameObject GetPreviewQuad(int index)
        {
            while (_linePreviewQuads.Count <= index)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "LineCell";
                Destroy(quad.GetComponent<Collider>());
                quad.transform.SetParent(_linePreviewRoot.transform, false);
                // Flat on the ground rather than standing upright like a default Quad.
                quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                _linePreviewQuads.Add(quad);
            }

            return _linePreviewQuads[index];
        }

        private void HideLinePreview()
        {
            foreach (var quad in _linePreviewQuads)
            {
                if (quad != null) quad.SetActive(false);
            }
        }
    }
}
