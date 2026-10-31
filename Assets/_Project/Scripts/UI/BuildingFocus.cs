using CityBuilder.Buildings;
using CityBuilder.CameraControl;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// A building picked up with a tap (the user, 2026-10-22, every mode): the camera brings it to
    /// the middle of the screen, gold rings spread round it like a citizen's order flag and one
    /// stays round its foot, its reach shows -- the cells a yard gathers from, the circle a tower
    /// shoots in -- its health bar stays up, and a «!» button comes up at the bottom in place of
    /// the build menu. The «!» opens the building's card (BuildingInfoPanelController, or the
    /// Laboratory's own), with its upgrade and repair. A tap on open ground, picking up anything
    /// else, the building's end, or the camera taken away till the building is off the screen
    /// (the user, 2026-10-28) lets it go.
    /// </summary>
    public class BuildingFocus : MonoBehaviour
    {
        private const int RingPoints = 40;
        private const float RingMargin = 0.45f;
        private const float RippleSeconds = 0.8f;
        private static readonly Color Gold = new Color(1f, 0.85f, 0.3f, 1f);
        private static readonly Color ReachColor = new Color(1f, 0.93f, 0.6f, 1f);
        // a building that is not the player's: outlined in red (the user, 2026-10-31)
        private static readonly Color Red = new Color(0.95f, 0.18f, 0.12f, 1f);
        private static readonly Color RedReach = new Color(1f, 0.45f, 0.38f, 1f);

        [SerializeField] private BuildingInfoPanelController infoPanel;
        [SerializeField] private ResearchPanelController researchPanel;
        [SerializeField] private CitizenSelector citizenSelector;
        [SerializeField] private BuildingPlacer buildingPlacer;
        /// <summary>The «!» button at the bottom: shown while a building is held.</summary>
        [SerializeField] private GameObject detailsButton;

        public static BuildingFocus Instance { get; private set; }

        /// <summary>The building held, or null.</summary>
        public BuildingInstance Current { get; private set; }

        /// <summary>A building of another side held instead (an orc camp's), or null: red ring, its «!» opens ForeignBuildingCard.</summary>
        public OrcCampBuilding Foreign { get; private set; }

        private Transform Held => Current != null ? Current.transform : Foreign != null ? Foreign.transform : null;
        private Color _ringColour = Gold;

        private LineRenderer _ring, _reach;
        private LineRenderer[] _ripples;
        private float _rippleClock = -1f;
        private float _footRadius;
        private RTSCameraController _rig;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Select(BuildingInstance building)
        {
            if (building == null || building.Data == null)
            {
                Clear();
                return;
            }
            if ((Current != null && Current != building) || Foreign != null) Release();
            Current = building;
            _ringColour = Gold;

            var rig = FindAnyObjectByType<RTSCameraController>();
            if (rig != null) rig.FocusOn(building.transform.position);
            _rig = rig;

            EnsureBuilt();
            Paint(Gold, ReachColor);
            var footprint = building.RotatedFootprint();
            var cell = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;
            _footRadius = Mathf.Max(footprint.x, footprint.y) * cell * 0.5f * 1.2f + RingMargin;
            var at = Ground(building.transform.position);
            Circle(_ring, at, _footRadius);
            _ring.gameObject.SetActive(true);
            _rippleClock = 0f;

            // its reach: a yard's or a service's disc (the same as when placing it), a defender's circle
            var harvest = HarvestRadiusOverlay.RadiusOf(building.Data, building.Level);
            var centre = GridManager.Instance != null ? GridManager.Instance.GetFootprintCenterWorld(building.OriginCell, footprint) : at;
            if (harvest > 0) HarvestRadiusOverlay.ShowFor(centre, harvest, building.Data);
            else HarvestRadiusOverlay.HideIfShown();
            if (building.Defense > 0)
            {
                Circle(_reach, at, DefensiveBuilding.RangeOf(building));
                _reach.gameObject.SetActive(true);
            }
            else _reach.gameObject.SetActive(false);

            building.PinHealthBar(true);
            if (detailsButton != null) detailsButton.SetActive(true);
        }

        /// <summary>Holds a building of another side: a red ring round it, its reach if it shoots, the «!» for its card.</summary>
        public void SelectForeign(OrcCampBuilding building)
        {
            if (building == null)
            {
                Clear();
                return;
            }
            Release();
            Foreign = building;
            _ringColour = Red;
            var rig = FindAnyObjectByType<RTSCameraController>();
            if (rig != null) rig.FocusOn(building.transform.position);
            _rig = rig;
            EnsureBuilt();
            Paint(Red, RedReach);
            var cell = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;
            _footRadius = Mathf.Max(building.Footprint.x, building.Footprint.y) * cell * 0.5f * 1.2f + RingMargin;
            var at = Ground(building.transform.position);
            Circle(_ring, at, _footRadius);
            _ring.gameObject.SetActive(true);
            _rippleClock = 0f;
            HarvestRadiusOverlay.HideIfShown();
            if (building.ShotRange > 0f)
            {
                Circle(_reach, at, building.ShotRange);
                _reach.gameObject.SetActive(true);
            }
            else _reach.gameObject.SetActive(false);
            if (detailsButton != null) detailsButton.SetActive(true);
        }

        private void Paint(Color ring, Color reach)
        {
            _ring.material.color = ring;
            _reach.material.color = reach;
        }

        /// <summary>Lets the building go, and closes its card.</summary>
        public void Clear()
        {
            Release();
            if (infoPanel != null) infoPanel.Close();
            if (researchPanel != null) researchPanel.Close();
            if (ForeignBuildingCard.Instance != null) ForeignBuildingCard.Instance.Close();
        }

        private void Release()
        {
            if (Current != null) Current.PinHealthBar(false);
            Current = null;
            Foreign = null;
            if (_ring != null) _ring.gameObject.SetActive(false);
            if (_reach != null) _reach.gameObject.SetActive(false);
            if (_ripples != null) foreach (var ripple in _ripples) ripple.gameObject.SetActive(false);
            _rippleClock = -1f;
            HarvestRadiusOverlay.HideIfShown();
            if (detailsButton != null) detailsButton.SetActive(false);
        }

        /// <summary>The «!» is up: a building held and its card closed -- when a run's hiring buttons stand beside it (FocusHireBar).</summary>
        public bool DetailsShown => detailsButton != null && detailsButton.activeSelf;

        /// <summary>The «!»: the held building's card.</summary>
        public void OpenDetails()
        {
            if (Foreign != null)
            {
                if (ForeignBuildingCard.Instance != null) ForeignBuildingCard.Instance.Show(Foreign);
                return;
            }
            if (Current == null) return;
            if (researchPanel != null && Current.Data.buildingName == Research.ResearchManager.LaboratoryBuildingId)
            {
                researchPanel.Show(Current);
                return;
            }
            if (infoPanel != null) infoPanel.Show(Current);
        }

        private void Update()
        {
            if (Current == null && (Foreign == null || Foreign.CurrentHealth <= 0))
            {
                // destroyed while held (Unity's null): take the marks down
                if (_ring != null && _ring.gameObject.activeSelf) Release();
                return;
            }
            // anything else picked up lets the building go
            var army = ArmyManager.Instance;
            if ((army != null && army.SelectedGroup != null) || (citizenSelector != null && citizenSelector.HasSelection)
                || (buildingPlacer != null && buildingPlacer.IsSelecting))
            {
                Clear();
                return;
            }
            // the «!» steps aside while the card it opened is up, and comes back when it closes
            var cardUp = (infoPanel != null && infoPanel.IsOpen) || (researchPanel != null && researchPanel.IsOpen)
                || (ForeignBuildingCard.Instance != null && ForeignBuildingCard.Instance.IsOpen);
            if (detailsButton != null && detailsButton.activeSelf == cardUp) detailsButton.SetActive(!cardUp);
            // the camera taken away till the building is off the screen lets it go
            if (!cardUp && OffScreen(Held))
            {
                Clear();
                return;
            }
            TickRipples();
        }

        /// <summary>The building's middle is out of the camera's view -- not while the camera is still gliding to it.</summary>
        private bool OffScreen(Transform building)
        {
            if (_rig != null && _rig.IsFocusing) return false;
            var camera = Camera.main;
            if (camera == null || building == null) return false;
            var view = camera.WorldToViewportPoint(building.position);
            return view.z <= 0f || view.x < 0f || view.x > 1f || view.y < 0f || view.y > 1f;
        }

        private void TickRipples()
        {
            if (_rippleClock < 0f) return;
            _rippleClock += Time.unscaledDeltaTime;
            var at = Ground(Held.position);
            for (var i = 0; i < _ripples.Length; i++)
            {
                var t = (_rippleClock - i * 0.18f) / RippleSeconds;
                var ripple = _ripples[i];
                if (t < 0f || t >= 1f)
                {
                    ripple.gameObject.SetActive(false);
                    continue;
                }
                var spread = 1f - (1f - t) * (1f - t);
                Circle(ripple, at, _footRadius * Mathf.Lerp(0.6f, 1.8f, spread));
                var c = _ringColour;
                c.a = 0.9f * (1f - t);
                ripple.material.color = c;
                ripple.gameObject.SetActive(true);
            }
            if (_rippleClock > RippleSeconds + 0.4f) _rippleClock = -1f;
        }

        private static Vector3 Ground(Vector3 at)
        {
            var grid = GridManager.Instance;
            return new Vector3(at.x, (grid != null ? grid.GroundHeight : at.y) + 0.06f, at.z);
        }

        private static void Circle(LineRenderer line, Vector3 centre, float radius)
        {
            line.transform.position = centre;
            for (var i = 0; i < RingPoints; i++)
            {
                var a = i / (float)RingPoints * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));   // the transform lays its XY plane on the ground
            }
        }

        private void EnsureBuilt()
        {
            if (_ring != null) return;
            _ring = Line("BuildingFocusRing", 0.12f, Gold);
            _reach = Line("BuildingFocusReach", 0.06f, ReachColor);
            _ripples = new[] { Line("BuildingFocusRipple0", 0.1f, Gold), Line("BuildingFocusRipple1", 0.1f, Gold) };
        }

        private static LineRenderer Line(string name, float width, Color color)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = RingPoints;
            line.widthMultiplier = width;
            line.alignment = LineAlignment.TransformZ;
            line.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.material = MoveOrderMarker.CreateTransparentMaterial(color);
            line.gameObject.SetActive(false);
            return line;
        }
    }
}
