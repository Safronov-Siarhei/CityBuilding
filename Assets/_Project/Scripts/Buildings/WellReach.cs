using System.Collections.Generic;
using CityBuilder.Grid;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// The Колодец's one reach (the user, 2026-11-17): the same circle keeps the houses inside it
    /// from falling ill and counts as water for the buildings inside it against fire. It is the
    /// well's service_radius at its level (8/10/12/14/16 cells), measured from the well's footprint
    /// centre to the other building's footprint centre -- the circle the player sees is the rule.
    /// The well does nothing for food.
    /// </summary>
    public static class WellReach
    {
        public const string WellName = "Well";

        public static bool IsWell(BuildingData data) => data != null && data.buildingName == WellName;

        /// <summary>The middle of a building's turned footprint, on the ground.</summary>
        public static Vector3 CentreOf(BuildingInstance b)
        {
            var grid = GridManager.Instance;
            return grid != null ? grid.GetFootprintCenterWorld(b.OriginCell, b.RotatedFootprint()) : b.transform.position;
        }

        /// <summary>Whether a point lies inside a well's circle (flat distance, metres).</summary>
        public static bool Covers(Vector3 wellCentre, float radiusMetres, Vector3 point)
        {
            var dx = point.x - wellCentre.x;
            var dz = point.z - wellCentre.z;
            return dx * dx + dz * dz <= radiusMetres * radiusMetres;
        }

        /// <summary>Whether `b` stands inside this operational well's circle.</summary>
        public static bool Covers(BuildingInstance well, BuildingInstance b) =>
            well != null && b != null && well.ServiceRadius > 0 && Covers(CentreOf(well), well.ServiceRadius, CentreOf(b));

        /// <summary>
        /// What the well does something for: a house (sickness), or, where fires are on, a building
        /// that can burn. What the yellow outlines mark.
        /// </summary>
        public static bool Affected(BuildingInstance b)
        {
            if (b == null || b.Data == null || b.IsBot || IsWell(b.Data)) return false;
            if (BuildingInstance.IsHouse(b.Data)) return true;
            return FireRules.Active && FireRules.Burns(b);
        }
    }

    /// <summary>
    /// What placing or holding a Колодец shows (the user, 2026-11-17): its circle, a thin pale-gold
    /// line like a tower's reach, and a yellow frame on the ground round every building inside it
    /// that the well does something for. Pooled lines; the outlines are re-found only when the
    /// circle has moved or half a second has passed, so a ghost following the finger costs nothing
    /// per frame but setting a position.
    /// </summary>
    public class WellReachOverlay : MonoBehaviour
    {
        private const int RingPoints = 64;
        private const int MaxOutlines = 80;
        private const float RefreshSeconds = 0.5f;
        private const float Lift = 0.06f;
        private static readonly Color RingColor = new Color(1f, 0.93f, 0.6f, 1f);
        private static readonly Color OutlineColor = new Color(1f, 0.85f, 0.15f, 1f);

        private static WellReachOverlay _instance;

        private LineRenderer _ring;
        private Material _outlineMaterial;
        private readonly List<LineRenderer> _outlines = new List<LineRenderer>();
        private int _shown;
        private float _ringRadius = -1f;
        private Vector3 _foundAt = new Vector3(float.MaxValue, 0f, 0f);
        private float _foundRadius = -1f;
        private float _foundTime = -10f;

        private static WellReachOverlay Instance
        {
            get
            {
                // a scene load destroys the object while the static still points at it
                if (_instance != null) return _instance;
                var go = new GameObject("WellReachOverlay");
                _instance = go.AddComponent<WellReachOverlay>();
                _instance.Build();
                return _instance;
            }
        }

        /// <summary>Shows the circle at `centre` and frames what it covers. A radius of zero hides it.</summary>
        public static void Show(Vector3 centre, int radiusMetres)
        {
            if (radiusMetres <= 0)
            {
                HideIfShown();
                return;
            }
            Instance.ShowAt(centre, radiusMetres);
        }

        /// <summary>Hides it, without creating one just to hide it.</summary>
        public static void HideIfShown()
        {
            if (_instance == null || !_instance.gameObject.activeSelf) return;
            _instance.gameObject.SetActive(false);
            _instance._foundTime = -10f;
        }

        private void ShowAt(Vector3 centre, float radius)
        {
            var grid = GridManager.Instance;
            var ground = grid != null ? grid.GroundHeight : centre.y;
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            _ring.transform.position = new Vector3(centre.x, ground + Lift, centre.z);
            if (!Mathf.Approximately(radius, _ringRadius))
            {
                _ringRadius = radius;
                for (var i = 0; i < RingPoints; i++)
                {
                    var a = i / (float)RingPoints * Mathf.PI * 2f;
                    _ring.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
                }
            }

            var flat = new Vector3(centre.x, 0f, centre.z);
            if ((flat - _foundAt).sqrMagnitude < 0.25f && Mathf.Approximately(radius, _foundRadius)
                && Time.unscaledTime - _foundTime < RefreshSeconds) return;
            _foundAt = flat;
            _foundRadius = radius;
            _foundTime = Time.unscaledTime;
            FrameCovered(centre, radius, grid);
        }

        private void FrameCovered(Vector3 centre, float radius, GridManager grid)
        {
            var cell = grid != null ? grid.CellSize : 1f;
            var shown = 0;
            foreach (var b in BuildingInstance.All)
            {
                if (shown >= MaxOutlines) break;
                if (!WellReach.Affected(b)) continue;
                var at = WellReach.CentreOf(b);
                if (!WellReach.Covers(centre, radius, at)) continue;
                var size = b.RotatedFootprint();
                var hx = Mathf.Max(1, size.x) * cell * 0.5f + 0.1f;
                var hz = Mathf.Max(1, size.y) * cell * 0.5f + 0.1f;
                var line = Outline(shown++);
                line.transform.position = new Vector3(at.x, b.transform.position.y + Lift, at.z);
                line.SetPosition(0, new Vector3(-hx, -hz, 0f));
                line.SetPosition(1, new Vector3(hx, -hz, 0f));
                line.SetPosition(2, new Vector3(hx, hz, 0f));
                line.SetPosition(3, new Vector3(-hx, hz, 0f));
                if (!line.gameObject.activeSelf) line.gameObject.SetActive(true);
            }
            for (var i = shown; i < _shown; i++) _outlines[i].gameObject.SetActive(false);
            _shown = shown;
        }

        private LineRenderer Outline(int index)
        {
            while (_outlines.Count <= index)
            {
                var line = Line("WellReachOutline", 4, 0.12f, _outlineMaterial);
                line.numCornerVertices = 0;
                _outlines.Add(line);
            }
            return _outlines[index];
        }

        private void Build()
        {
            _ring = Line("WellReachRing", RingPoints, 0.08f, UI.MoveOrderMarker.CreateTransparentMaterial(RingColor));
            _ring.gameObject.SetActive(true);
            _outlineMaterial = UI.MoveOrderMarker.CreateTransparentMaterial(OutlineColor);
        }

        private LineRenderer Line(string name, int points, float width, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = points;
            line.widthMultiplier = width;
            line.alignment = LineAlignment.TransformZ;
            line.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // its XY plane on the ground
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = material;
            go.SetActive(false);
            return line;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
