using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Combat
{
    /// <summary>
    /// A townsman let out of an orc cage (the user's idea, 2026-10-19). He walks to the Town Hall
    /// over the NavMesh and becomes a citizen there -- or, when the town has no free housing,
    /// waits by the hall until it has (the user's rule: he does not turn back, nor move in
    /// homeless). On the way he is fair game: an orc with no soldier to fight goes for him (see
    /// OrcUnit). The player's own army never attacks him -- he is not an IDamageTarget.
    ///
    /// Saved (position and health) and put back on load.
    /// </summary>
    public class FreedPrisoner : MonoBehaviour
    {
        private static readonly List<FreedPrisoner> _all = new List<FreedPrisoner>();

        /// <summary>Every freed prisoner still on his way or waiting at the hall.</summary>
        public static IReadOnlyList<FreedPrisoner> All => _all;

        private const float ReplanSeconds = 2f;
        private const float RoomCheckSeconds = 1f;
        // How far past the Town Hall's footprint edge counts as having arrived.
        private const float ArrivalMarginMeters = 1.5f;

        private static Material[] _clothes;
        private static Material _skin;
        // Posted once per spell of waiting, not once per prisoner -- a cage of five arriving at a
        // full town would otherwise fill the feed.
        private static bool _toldNoRoom;

        private readonly NavRoute _route = new NavRoute();
        private float _replanTimer;
        private float _roomTimer;
        private float _walkSpeed;

        public int CurrentHealth { get; private set; }
        public int MaxHealth { get; private set; }

        /// <summary>Standing by the Town Hall, waiting for a free home.</summary>
        public bool Waiting { get; private set; }

        /// <summary>Lets a prisoner out at `position` (a broken cage's door) -- or puts a saved one back, on the health he had.</summary>
        public static FreedPrisoner Spawn(Vector3 position, int health = -1)
        {
            EnsureMaterials();
            _hallCheckedAt = float.NegativeInfinity;
            var root = new GameObject("FreedPrisoner");
            root.transform.position = position;
            root.transform.localScale = Vector3.one * UnitSize.Scale;
            var clothes = _clothes[Random.Range(0, _clothes.Length)];
            AddCube(root.transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.3f, 0.5f, 0.3f), clothes);
            AddCube(root.transform, new Vector3(0f, 0.61f, 0f), new Vector3(0.22f, 0.22f, 0.22f), _skin);
            var prisoner = root.AddComponent<FreedPrisoner>();
            var balance = BalanceConfig.Instance;
            prisoner.MaxHealth = Mathf.Max(1, balance.PrisonerHealth);
            prisoner.CurrentHealth = health > 0 ? Mathf.Min(health, prisoner.MaxHealth) : prisoner.MaxHealth;
            prisoner._walkSpeed = balance.PrisonerWalkSpeed;
            return prisoner;
        }

        private void OnEnable()
        {
            _all.Add(this);
        }

        private void OnDisable()
        {
            _all.Remove(this);
            if (Waiting && !AnyoneWaiting()) _toldNoRoom = false;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            if (CurrentHealth > 0) return;
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_prisoner_killed"),
                EventSeverity.Danger, EventGlyph.Skull, null, transform.position));
            Destroy(gameObject);
        }

        private void Update()
        {
            if (CurrentHealth <= 0) return;
            if (!TryFindTownHall(out var hall, out var halfSize))
            {
                Waiting = false;
                return;
            }

            var toHall = hall - transform.position;
            toHall.y = 0f;
            if (toHall.magnitude <= halfSize + ArrivalMarginMeters)
            {
                Arrive();
                return;
            }

            Waiting = false;
            _replanTimer -= Time.deltaTime;
            if (_replanTimer <= 0f || _route.IsFinished)
            {
                _replanTimer = ReplanSeconds;
                _route.SetDestination(transform.position, hall, straightLineFallback: false);
            }
            if (!_route.TryGetDirection(transform.position, out var direction)) return;
            transform.position += direction * _walkSpeed * Time.deltaTime;
            var grid = GridManager.Instance;
            if (grid != null)
            {
                var p = transform.position;
                p.y = grid.GroundHeight;
                transform.position = p;
            }
            transform.rotation = Quaternion.LookRotation(direction);
        }

        /// <summary>At the hall: a free home makes him a citizen, otherwise he waits for one.</summary>
        private void Arrive()
        {
            _roomTimer -= Time.deltaTime;
            if (Waiting && _roomTimer > 0f) return;
            _roomTimer = RoomCheckSeconds;

            var citizens = CitizenManager.Instance;
            if (citizens != null && citizens.FreeSpace > 0)
            {
                citizens.AddCitizens(1);
                Waiting = false;
                Destroy(gameObject);
                return;
            }

            if (!Waiting && !_toldNoRoom)
            {
                _toldNoRoom = true;
                EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_prisoners_no_room"),
                    EventSeverity.Warning, EventGlyph.Alert, null, transform.position));
            }
            Waiting = true;
        }

        private static bool AnyoneWaiting()
        {
            foreach (var prisoner in _all)
            {
                if (prisoner != null && prisoner.Waiting) return true;
            }
            return false;
        }

        // The hall does not move: one scene scan a second, shared by every prisoner, and none at
        // all while nobody has been let out.
        private static float _hallCheckedAt = float.NegativeInfinity;
        private static bool _hallFound;
        private static Vector3 _hall;
        private static float _hallHalfSize;

        private static bool TryFindTownHall(out Vector3 position, out float halfSize)
        {
            if (Time.time - _hallCheckedAt >= 1f || Time.time < _hallCheckedAt)
            {
                _hallCheckedAt = Time.time;
                _hallFound = false;
                var grid = GridManager.Instance;
                var any = BuildingInstance.HasAny(BuildingIds.TownHall);
                foreach (var instance in any ? FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None) : new BuildingInstance[0])
                {
                    if (instance.Data == null || instance.Data.buildingName != BuildingIds.TownHall) continue;
                    var footprint = instance.RotatedFootprint();
                    _hall = grid != null ? grid.GetFootprintCenterWorld(instance.OriginCell, footprint) : instance.transform.position;
                    _hallHalfSize = Mathf.Max(footprint.x, footprint.y) * (grid != null ? grid.CellSize : 1f) * 0.5f;
                    _hallFound = true;
                    break;
                }
            }
            position = _hall;
            halfSize = _hallHalfSize;
            return _hallFound;
        }

        private static void EnsureMaterials()
        {
            if (_clothes != null) return;
            var colors = new[]
            {
                new Color(0.72f, 0.26f, 0.24f), new Color(0.24f, 0.42f, 0.68f), new Color(0.32f, 0.55f, 0.28f),
                new Color(0.62f, 0.5f, 0.28f), new Color(0.45f, 0.42f, 0.48f),
            };
            _clothes = new Material[colors.Length];
            for (var i = 0; i < colors.Length; i++) _clothes[i] = new Material(RuntimeShaders.Lit) { color = colors[i] };
            _skin = new Material(RuntimeShaders.Lit) { color = new Color(0.85f, 0.68f, 0.55f) };
        }

        private static void AddCube(Transform parent, Vector3 localPosition, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<BoxCollider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
