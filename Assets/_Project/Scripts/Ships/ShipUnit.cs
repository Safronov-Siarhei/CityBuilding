using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Maps;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Ships
{
    /// <summary>The ships (the user, 2026-10-28): what each is for, by its row in the units tab.</summary>
    public enum ShipKind { Scout = 0, Transport = 1 }

    /// <summary>A ship's numbers, read from its units-tab row (`ship_scout`, `ship_transport`).</summary>
    public static class ShipStats
    {
        public static readonly ShipKind[] All = { ShipKind.Scout, ShipKind.Transport };

        public static string SheetId(ShipKind kind) => kind == ShipKind.Scout ? "ship_scout" : "ship_transport";

        public static UnitBalance Row(ShipKind kind) => BalanceConfig.Instance != null ? BalanceConfig.Instance.Unit(SheetId(kind)) : null;

        private static UnitLevelStats Level1(ShipKind kind)
        {
            var row = Row(kind);
            return row != null && row.levels.Count > 0 ? row.levels[0] : null;
        }

        public static int MaxHealth(ShipKind kind) => Level1(kind)?.maxHealth ?? 50;
        public static float Speed(ShipKind kind) => Level1(kind)?.walkSpeed ?? 3f;
        public static int PriceCoins(ShipKind kind) => Level1(kind)?.recruitCoins ?? 0;
        /// <summary>How many it carries: citizens, soldiers and heroes alike (0 for the scout).</summary>
        public static int Capacity(ShipKind kind) => Row(kind)?.capacity ?? 0;
        /// <summary>The Shipyard's level that builds it (the user: the shipyard's level opens the ships).</summary>
        public static int ShipyardLevel(ShipKind kind) => Mathf.Max(1, Row(kind)?.shipyardLevel ?? 1);
        /// <summary>How many may sail at once; 0 = no limit (the user, 2026-10-28: unlimited for now, a column to balance later).</summary>
        public static int MaxCount(ShipKind kind) => Row(kind)?.maxCount ?? 0;
        /// <summary>How far round it the fog opens while it sails, in cells.</summary>
        public static int RevealCells(ShipKind kind) => Mathf.Max(4, Mathf.RoundToInt(Row(kind)?.engageRadius ?? 8f));
        public static string DisplayName(ShipKind kind) => Localization.Get("#unit_" + SheetId(kind));
    }

    /// <summary>
    /// One ship on the archipelago's sea: sails where it is sent over the ships' own NavMesh
    /// (ShipManager -- the sea, kept a hull's width off every shore and short of the storm), turns
    /// to its course, rides the swell, opens the fog round it. Tapped, it is selected (a gold ring
    /// on the water); a tap on the sea then sends it there (ShipInput).
    /// </summary>
    public class ShipUnit : MonoBehaviour
    {
        private const float TurnRate = 120f;     // degrees a second
        private const float ArriveDistance = 0.6f;

        public ShipKind Kind { get; private set; }
        public int Health { get; set; }
        public bool Selected { get; private set; }
        public bool IsMoving => _corners.Count > 0;

        /// <summary>
        /// Who is aboard: soldiers and heroes, put away (inactive) while they sail, kept in their
        /// groups so the army, upkeep and the save still count them. A sinking takes them with it.
        /// </summary>
        public readonly List<Combat.SoldierUnit> Aboard = new List<Combat.SoldierUnit>();

        public int Capacity => ShipStats.Capacity(Kind);
        public int FreeSeats => Mathf.Max(0, Capacity - Aboard.Count);

        /// <summary>Takes this one aboard: out of the world until the ship lands them. False when it is full.</summary>
        public bool Embark(Combat.SoldierUnit unit)
        {
            if (unit == null || FreeSeats == 0 || Aboard.Contains(unit)) return false;
            Aboard.Add(unit);
            unit.gameObject.SetActive(false);
            return true;
        }

        /// <summary>Everyone aboard back on land at this point, in their groups' rings, the groups told to hold there.</summary>
        public int Disembark(Vector3 at)
        {
            var landed = 0;
            var groups = new List<Combat.ArmyGroup>();
            foreach (var unit in Aboard)
            {
                if (unit == null) continue;
                var spot = at + Combat.ArmyGroup.FormationOffset(landed);
                unit.transform.position = ShipManager.TrySampleLand(spot, 3f, out var onLand) ? onLand : at;
                unit.gameObject.SetActive(true);
                if (unit.Group != null && !groups.Contains(unit.Group)) groups.Add(unit.Group);
                landed++;
            }
            Aboard.Clear();
            foreach (var group in groups) group.OrderMoveTo(at);
            return landed;
        }

        private readonly List<Vector3> _corners = new List<Vector3>();
        private NavMeshPath _path;
        private LineRenderer _ring;
        private float _bobPhase;

        public void Initialize(ShipKind kind, int health)
        {
            Kind = kind;
            Health = health;
            _bobPhase = Random.value * 10f;
            _ring = BuildRing();
            _ring.enabled = false;
            FogOfWarManager.Instance?.AddWalker(transform, ShipStats.RevealCells(kind));
        }

        private void OnDestroy()
        {
            FogOfWarManager.Instance?.RemoveWalker(transform);
        }

        public void SetSelected(bool selected)
        {
            Selected = selected;
            if (_ring != null) _ring.enabled = selected;
        }

        /// <summary>Sails to the nearest sea a ship can reach by the point; false when there is no way there.</summary>
        public bool SailTo(Vector3 destination)
        {
            var manager = ShipManager.Instance;
            if (manager == null || !manager.TrySampleSea(destination, 10f, out var target)) return false;
            _path ??= new NavMeshPath();
            if (!NavMesh.CalculatePath(manager.SeaPoint(transform.position), target, manager.Filter, _path) || _path.status == NavMeshPathStatus.PathInvalid)
            {
                return false;
            }
            _corners.Clear();
            var corners = _path.corners;
            for (var i = 1; i < corners.Length; i++) _corners.Add(corners[i]);
            return _corners.Count > 0;
        }

        public void Stop() => _corners.Clear();

        private void Update()
        {
            var t = Time.deltaTime;
            if (_corners.Count > 0)
            {
                var target = _corners[0];
                var to = target - transform.position;
                to.y = 0f;
                if (to.magnitude < ArriveDistance)
                {
                    _corners.RemoveAt(0);
                }
                else
                {
                    var want = Quaternion.LookRotation(to.normalized, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, want, TurnRate * t);
                    // slow while still turning, so it does not skid round a corner
                    var facing = Mathf.Clamp01(Vector3.Dot(transform.forward, to.normalized));
                    var step = ShipStats.Speed(Kind) * Mathf.Lerp(0.25f, 1f, facing) * t;
                    transform.position += transform.forward * Mathf.Min(step, to.magnitude);
                }
            }
            // the swell
            _bobPhase += t;
            var p = transform.position;
            p.y = ShipManager.WaterLevel + Mathf.Sin(_bobPhase * 1.6f) * 0.04f;
            transform.position = p;
        }

        private LineRenderer BuildRing()
        {
            var go = new GameObject("SelectionRing");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            const int segments = 32;
            var radius = Kind == ShipKind.Scout ? 1.8f : 3.0f;
            line.positionCount = segments;
            line.loop = true;
            line.useWorldSpace = false;
            line.widthMultiplier = 0.12f;
            line.material = new Material(RuntimeShaders.Unlit) { color = UI.UiTheme.Gold };
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (var i = 0; i < segments; i++)
            {
                var a = i * Mathf.PI * 2f / segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0.15f, Mathf.Sin(a) * radius));
            }
            return line;
        }
    }
}
