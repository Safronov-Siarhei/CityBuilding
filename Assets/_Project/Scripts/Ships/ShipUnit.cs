using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Maps;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Ships
{
    /// <summary>The ships (the user, 2026-10-28): what each is for, by its row in the units tab.</summary>
    public enum ShipKind { Scout = 0, Transport = 1, War = 2, Battleship = 3 }

    /// <summary>A ship's numbers, read from its units-tab row (`ship_scout`, `ship_transport`).</summary>
    public static class ShipStats
    {
        public static readonly ShipKind[] All = { ShipKind.Scout, ShipKind.Transport, ShipKind.War, ShipKind.Battleship };

        public static string SheetId(ShipKind kind) => kind switch
        {
            ShipKind.Scout => "ship_scout",
            ShipKind.Transport => "ship_transport",
            ShipKind.War => "ship_war",
            _ => "ship_battleship",
        };

        public static UnitBalance Row(ShipKind kind) => BalanceConfig.Instance != null ? BalanceConfig.Instance.Unit(SheetId(kind)) : null;

        private static UnitLevelStats Level1(ShipKind kind)
        {
            var row = Row(kind);
            return row != null && row.levels.Count > 0 ? row.levels[0] : null;
        }

        public static int MaxHealth(ShipKind kind) => Level1(kind)?.maxHealth ?? 50;
        public static float Speed(ShipKind kind) => Level1(kind)?.walkSpeed ?? 3f;
        public static int PriceCoins(ShipKind kind) => Level1(kind)?.recruitCoins ?? 0;
        /// <summary>A cannonball's damage (0 = unarmed: the scout, the transport).</summary>
        public static int AttackDamage(ShipKind kind) => Level1(kind)?.attackDamage ?? 0;
        public static float AttackInterval(ShipKind kind) => Mathf.Max(0.3f, Level1(kind)?.attackIntervalSeconds ?? 2f);
        /// <summary>How far its cannons reach, metres (units tab attack_range_units).</summary>
        public static float AttackRange(ShipKind kind) => Row(kind)?.attackRangeUnits ?? 10f;
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
    public class ShipUnit : MonoBehaviour, Combat.IDamageTarget
    {
        private const float TurnRate = 120f;     // degrees a second
        private const float ArriveDistance = 0.6f;
        private const float SinkSeconds = 4f;
        private const float TargetScanSeconds = 0.25f;
        // an orc camp's building is reached at its walls, not its centre
        private const float StructureReach = 2.5f;

        public ShipKind Kind { get; private set; }
        public int Health { get; set; }
        public int MaxHealth => ShipStats.MaxHealth(Kind);
        public bool Selected { get; private set; }
        public bool IsMoving => _corners.Count > 0;
        /// <summary>Going down: out of the fight, the selection and the save; gone in a few seconds.</summary>
        public bool Sinking { get; private set; }

        public Transform Transform => this != null ? transform : null;
        public bool IsAlive => this != null && Health > 0 && !Sinking;
        public bool IsStructure => true;

        /// <summary>Holed: at nothing it sinks with everyone aboard (ShipManager.Sink).</summary>
        public void TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive) return;
            Health = Mathf.Max(0, Health - amount);
            ShowHealth();
            if (Health == 0) ShipManager.Instance?.Sink(this);
        }

        /// <summary>Mended to full (ShipManager.TryRepair has been paid).</summary>
        public void Repair()
        {
            Health = MaxHealth;
            ShowHealth();
        }

        private Buildings.BuildingHealthBar _healthBar;

        /// <summary>The bar over the mast once it is hurt, as over a soldier.</summary>
        private void ShowHealth()
        {
            if (_healthBar == null)
            {
                if (Health >= MaxHealth) return;
                var top = 1.5f;
                foreach (var r in GetComponentsInChildren<Renderer>()) top = Mathf.Max(top, r.bounds.max.y - transform.position.y);
                _healthBar = Buildings.BuildingHealthBar.CreateFor(transform, top + 0.4f, Kind == ShipKind.Scout ? 1.2f : 1.8f);
            }
            _healthBar.Report(Health / (float)Mathf.Max(1, MaxHealth));
        }

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
        private float _reload;
        private float _sunkFor;

        public void Initialize(ShipKind kind, int health)
        {
            Kind = kind;
            Health = health;
            _bobPhase = Random.value * 10f;
            _ring = BuildRing();
            _ring.enabled = false;
            _reload = ShipStats.AttackInterval(kind);
            FogOfWarManager.Instance?.AddWalker(transform, ShipStats.RevealCells(kind));
            if (health < MaxHealth) ShowHealth();
        }

        /// <summary>Holed through: it heels over and goes down; nothing more is asked of it.</summary>
        public void BeginSinking()
        {
            if (Sinking) return;
            Sinking = true;
            _corners.Clear();
            SetSelected(false);
            FogOfWarManager.Instance?.RemoveWalker(transform);
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            if (_healthBar != null) Destroy(_healthBar.gameObject);
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
            if (Sinking)
            {
                // heels over and settles under the swell
                _sunkFor += t;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(-12f, transform.eulerAngles.y, 35f), 20f * t);
                transform.position += Vector3.down * (0.6f * t);
                if (_sunkFor >= SinkSeconds) Destroy(gameObject);
                return;
            }
            TickCannons(t);
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

        /// <summary>
        /// A war ship's broadside: every reload, a cannonball at the nearest enemy in reach -- an orc,
        /// an orc camp's building -- from the side turned to it. The scout and the transport carry no
        /// guns (attack_damage 0).
        /// </summary>
        private void TickCannons(float t)
        {
            var damage = ShipStats.AttackDamage(Kind);
            if (damage <= 0) return;
            _reload -= t;
            if (_reload > 0f) return;
            var target = NearestEnemy(ShipStats.AttackRange(Kind));
            if (target == null)
            {
                _reload = TargetScanSeconds;
                return;
            }
            _reload = ShipStats.AttackInterval(Kind);
            var side = Vector3.Dot(target.Transform.position - transform.position, transform.right) >= 0f ? 1f : -1f;
            var muzzle = transform.position + Vector3.up * 0.8f + transform.right * (side * (Kind == ShipKind.Battleship ? 1.1f : 0.8f));
            Combat.CombatProjectiles.Fire(Combat.ProjectileKind.Cannonball, muzzle, target, damage);
        }

        private Combat.IDamageTarget NearestEnemy(float range)
        {
            Combat.IDamageTarget best = null;
            var bestSq = range * range;
            var here = transform.position;
            foreach (var orc in Combat.OrcUnit.All)
            {
                if (orc == null || orc.CurrentHealth <= 0) continue;
                var d = FlatSq(orc.transform.position - here);
                if (d < bestSq)
                {
                    bestSq = d;
                    best = orc;
                }
            }
            if (best != null) return best;
            var reachSq = (range + StructureReach) * (range + StructureReach);
            foreach (var building in Combat.OrcCampBuilding.All)
            {
                if (building == null || building.CurrentHealth <= 0) continue;
                var d = FlatSq(building.transform.position - here);
                if (d < reachSq)
                {
                    reachSq = d;
                    best = building;
                }
            }
            return best;
        }

        private static float FlatSq(Vector3 v) => v.x * v.x + v.z * v.z;

        private LineRenderer BuildRing()
        {
            var go = new GameObject("SelectionRing");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            const int segments = 32;
            var radius = Kind == ShipKind.Scout ? 1.8f : Kind == ShipKind.Battleship ? 4.0f : 3.0f;
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
