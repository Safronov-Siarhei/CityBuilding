using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Combat
{
    /// <summary>
    /// The orc base round one portal -- the walled camps the user drew level by level (2026-10-16),
    /// built from OrcBaseLayout. The camp:
    ///
    /// - reserves the square its BIGGEST level needs from the moment its portal opens, so it can
    ///   grow without colliding with the town (only cells it reserved are ever built on);
    /// - builds the plan of its level and keeps its guards at their posts: each kind of guard is
    ///   topped up by its own spawner (the war tent, the brute pit, the throwers' range), one every
    ///   orc_garrison_respawn_seconds while that spawner stands -- break it and that kind is gone;
    /// - turns its standing palisade into NavMesh obstacles, one per straight run, so the wall
    ///   really blocks the way until a piece of it is broken;
    /// - grows with the player's progression (OrcBaseLayout.LevelFor), and when it grows it is
    ///   rebuilt whole -- what the player had broken comes back;
    /// - stops growing once its portal is closed. The camp outlives its portal: the towers and the
    ///   guards do not give up because the portal fell.
    ///
    /// Keyed by the portal's cell, which is what the save uses to put it back.
    /// </summary>
    public class OrcCamp : MonoBehaviour
    {
        private static readonly List<OrcCamp> _all = new List<OrcCamp>();

        /// <summary>Every camp on the map, closed portal or not.</summary>
        public static IReadOnlyList<OrcCamp> All => _all;

        /// <summary>The margin round the camp's square within which its guards still take on soldiers.</summary>
        public const float ZoneMarginMeters = 1.5f;

        private readonly List<OrcCampBuilding> _buildings = new List<OrcCampBuilding>();
        private readonly List<FallenBuilding> _fallen = new List<FallenBuilding>();
        private readonly List<GameObject> _ruins = new List<GameObject>();
        private readonly HashSet<Vector2Int> _reserved = new HashSet<Vector2Int>();
        private readonly Dictionary<OrcKind, float> _spawnTimers = new Dictionary<OrcKind, float>();
        private OrcUnit[] _slotGuards = new OrcUnit[0];
        private GameObject _wallObstacles;
        private GameObject _mountains;
        private readonly HashSet<Vector2Int> _wallCells = new HashSet<Vector2Int>();

        /// <summary>A camp building the player has broken: what it was, where, which way it faced -- all the save needs to put its ruin back.</summary>
        public readonly struct FallenBuilding
        {
            public FallenBuilding(OrcCampBuildingKind kind, Vector2Int cell, float yaw)
            {
                Kind = kind;
                Cell = cell;
                Yaw = yaw;
            }

            public OrcCampBuildingKind Kind { get; }
            public Vector2Int Cell { get; }
            public float Yaw { get; }
        }

        /// <summary>The cell of the portal this camp was built around -- its key in the save.</summary>
        public Vector2Int PortalCell { get; private set; }

        /// <summary>The portal it guards; null once the player has closed it.</summary>
        public OrcPortal Portal { get; private set; }

        public int Level { get; private set; }

        /// <summary>The highest level the ground round this portal had room for (see OrcRaidManager).</summary>
        public int MaxLevel { get; private set; } = OrcBaseLayout.MaxLevel;

        /// <summary>0 for the camp nearest the Town Hall, then outwards: further camps grow sooner.</summary>
        public int Rank { get; private set; }

        /// <summary>How the camp's frame is turned on the grid, in quarter turns (its portal's facing).</summary>
        public int Quarters { get; private set; }

        /// <summary>Its portal has been closed: it is plundered and grows no more.</summary>
        public bool Cleared { get; private set; }

        public CampLayout Layout => OrcBaseLayout.For(Level);

        public IReadOnlyList<OrcCampBuilding> Buildings => _buildings;
        public IReadOnlyList<FallenBuilding> Fallen => _fallen;

        /// <summary>The guards alive right now.</summary>
        public IReadOnlyList<OrcUnit> Guards
        {
            get
            {
                var alive = new List<OrcUnit>();
                foreach (var guard in _slotGuards)
                {
                    if (guard != null && guard.CurrentHealth > 0) alive.Add(guard);
                }
                return alive;
            }
        }

        public bool TotemStanding => Find(OrcCampBuildingKind.Totem) != null;

        /// <summary>The portal's centre as a grid corner -- the origin of the camp's frame.</summary>
        public Vector2Int Centre => PortalCell + Vector2Int.one * (OrcBaseLayout.For(MaxLevel).PortalSize / 2);

        /// <summary>The great portal's base (see OrcBaseLayout.Main): its own style, no growth, no raids, the chief.</summary>
        public bool IsMain => MaxLevel == OrcBaseLayout.MainLevel;

        public void Initialize(Vector2Int portalCell, OrcPortal portal, int quarters, int rank, int maxLevel, bool cleared = false)
        {
            PortalCell = portalCell;
            Portal = portal;
            Quarters = quarters;
            Rank = rank;
            MaxLevel = Mathf.Clamp(maxLevel, 1, OrcBaseLayout.MainLevel);
            Cleared = cleared || portal == null;
        }

        private void OnEnable()
        {
            _all.Add(this);
        }

        private void OnDisable()
        {
            _all.Remove(this);
        }

        private void OnDestroy()
        {
            if (_mountains != null) Destroy(_mountains);
            if (_wallObstacles != null) Destroy(_wallObstacles);
        }

        /// <summary>The camp that guards this portal, or null.</summary>
        public static OrcCamp Of(OrcPortal portal)
        {
            if (portal == null) return null;
            foreach (var camp in _all)
            {
                if (camp.Portal == portal) return camp;
            }
            return null;
        }

        // -- the camp's ground ------------------------------------------------------------------

        /// <summary>The cells of the ground a camp of this level covers, in world cells.</summary>
        public IEnumerable<Vector2Int> SquareCells(int level)
        {
            var bounds = OrcBaseLayout.BoundsAt(level);
            OrcBaseLayout.ToWorld(bounds.min, bounds.size, Centre, Quarters, out var min, out var size);
            for (var x = 0; x < size.x; x++)
            {
                for (var z = 0; z < size.y; z++)
                {
                    yield return min + new Vector2Int(x, z);
                }
            }
        }

        /// <summary>Takes the cells of its biggest square that are free and standable (the predicate), so the town cannot build where the camp will grow.</summary>
        public void Reserve(GridManager grid, System.Func<Vector2Int, bool> standable)
        {
            foreach (var cell in SquareCells(MaxLevel))
            {
                if (_reserved.Contains(cell)) continue;
                if (OrcRaidManager.IsScenery(cell))
                {
                    CityBuilder.Maps.RockSpawner.Instance?.ClearCell(cell);
                    CityBuilder.Maps.TreesAreaSpawner.Instance?.ClearCell(cell);
                }
                if (!grid.IsWithinBounds(cell, Vector2Int.one) || !grid.IsAreaFree(cell, Vector2Int.one)) continue;
                if (!standable(cell)) continue;
                grid.SetAreaOccupied(cell, Vector2Int.one, true);
                _reserved.Add(cell);
            }
        }

        /// <summary>Whether every cell of the plot is the camp's own ground.</summary>
        public bool Owns(Vector2Int min, Vector2Int size)
        {
            for (var x = 0; x < size.x; x++)
            {
                for (var z = 0; z < size.y; z++)
                {
                    if (!_reserved.Contains(min + new Vector2Int(x, z))) return false;
                }
            }
            return true;
        }

        /// <summary>Whether the plan of the camp's level has wall (palisade or a gate) on this cell -- how the palisade autotiles.</summary>
        public bool IsWallCell(Vector2Int cell) => _wallCells.Contains(cell);

        /// <summary>A point of the camp's frame (metres from the portal's centre) in the world.</summary>
        public Vector3 LocalToWorld(Vector2 local)
        {
            var grid = GridManager.Instance;
            var turned = OrcBaseLayout.Rotate(local, Quarters);
            var origin = grid.CellToWorld(Centre);
            return new Vector3(origin.x + turned.x * grid.CellSize, grid.GroundHeight, origin.z + turned.y * grid.CellSize);
        }

        /// <summary>Whether a world point is inside the camp's square, give or take `margin` metres.</summary>
        public bool IsInZone(Vector3 position, float margin)
        {
            var grid = GridManager.Instance;
            if (grid == null) return false;
            var origin = grid.CellToWorld(Centre);
            var offset = new Vector2((position.x - origin.x) / grid.CellSize, (position.z - origin.z) / grid.CellSize);
            var local = OrcBaseLayout.Rotate(offset, -Quarters);
            var bounds = Layout.Bounds;
            var m = margin / grid.CellSize;
            return local.x >= bounds.xMin - m && local.x <= bounds.xMax + m && local.y >= bounds.yMin - m && local.y <= bounds.yMax + m;
        }

        /// <summary>Where a raid comes out: just in front of the camp's outer gate, so it never has to break its own wall.</summary>
        public Vector3 RaidExit => LocalToWorld(new Vector2(0f, Layout.Bounds.yMin - 1.5f));

        /// <summary>A point just outside the camp's front gate, `metres` out.</summary>
        public Vector3 InFrontOfGate(float metres) => LocalToWorld(new Vector2(0f, Layout.Bounds.yMin - metres));

        // -- building and rebuilding ------------------------------------------------------------

        /// <summary>Puts up the plan of `level` with a full garrison. The caller has torn down whatever stood before.</summary>
        public void Build(int level, OrcRaidManager builder)
        {
            Level = Mathf.Clamp(level, 1, MaxLevel);
            var layout = Layout;
            _wallCells.Clear();
            foreach (var piece in layout.Pieces)
            {
                if (!OrcBaseLayout.IsWall(piece.Kind)) continue;
                OrcBaseLayout.ToWorld(piece.Min, piece.Size, Centre, Quarters, out var min, out var size);
                for (var x = 0; x < size.x; x++)
                {
                    for (var z = 0; z < size.y; z++) _wallCells.Add(min + new Vector2Int(x, z));
                }
            }
            foreach (var piece in layout.Pieces)
            {
                OrcBaseLayout.ToWorld(piece.Min, piece.Size, Centre, Quarters, out var cell, out var size);
                if (!Owns(cell, size)) continue;
                var building = builder.SpawnCampBuilding(this, piece, cell, size);
                if (building != null) _buildings.Add(building);
            }

            if (Portal != null) builder.ApplyPortalLevel(Portal, Level);
            UpdateShield();
            RebuildWalls();
            if (_mountains == null && layout.Mountains.Count > 0) _mountains = builder.SpawnMountains(this, layout.Mountains);

            for (var k = 0; k < SpawnedKinds.Length; k++)
            {
                _kindsPosted[k] = false;
                foreach (var slot in layout.Guards) _kindsPosted[k] |= slot.Kind == SpawnedKinds[k];
            }
            _slotGuards = new OrcUnit[layout.Guards.Count];
            _spawnTimers.Clear();
            for (var i = 0; i < _slotGuards.Length; i++)
            {
                _slotGuards[i] = builder.SpawnGuard(this, layout.Guards[i], LocalToWorld(layout.Guards[i].Local));
            }
        }

        /// <summary>Takes everything down -- buildings, ruins, guards, the wall's obstacles -- ready for a rebuild. The reserved ground stays the camp's.</summary>
        public void Teardown()
        {
            foreach (var building in _buildings)
            {
                if (building != null) building.Dismantle();
            }
            _buildings.Clear();
            _fallen.Clear();
            foreach (var ruin in _ruins)
            {
                if (ruin != null) Destroy(ruin);
            }
            _ruins.Clear();
            foreach (var guard in _slotGuards)
            {
                if (guard != null) Destroy(guard.gameObject);
            }
            _slotGuards = new OrcUnit[0];
            if (_wallObstacles != null) Destroy(_wallObstacles);
            _wallObstacles = null;
            if (_mountains != null) Destroy(_mountains);
            _mountains = null;
        }

        /// <summary>Grows the camp to `level`, rebuilding it whole (the user's rule: what was broken comes back).</summary>
        public void GrowTo(int level, OrcRaidManager builder)
        {
            level = Mathf.Clamp(level, 1, MaxLevel);
            if (level <= Level || Cleared || IsMain) return;
            Teardown();
            Build(level, builder);
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Format("#log_camp_grew", Level),
                EventSeverity.Danger, EventGlyph.Portal, null, LocalToWorld(Vector2.zero)));
        }

        /// <summary>Rebuilds the camp at any level, down as well as up -- the debug panel's way to try every plan on a device.</summary>
        public void RebuildAt(int level, OrcRaidManager builder)
        {
            Teardown();
            Build(level, builder);
        }

        /// <summary>The portal has been closed: the camp is plundered and stops growing.</summary>
        public void MarkCleared()
        {
            Cleared = true;
            Portal = null;
        }

        public void AdoptRuin(GameObject ruin)
        {
            if (ruin != null) _ruins.Add(ruin);
        }

        /// <summary>Puts a saved building's health back -- 0 breaks it on the spot, quietly, leaving its ruin.</summary>
        public void ApplySavedHealth(OrcCampBuildingKind kind, Vector2Int cell, int health)
        {
            foreach (var building in _buildings)
            {
                if (building == null || building.Kind != kind || building.Cell != cell) continue;
                if (health > 0)
                {
                    building.SetCurrentHealth(health);
                }
                else
                {
                    building.FallQuietly();
                }
                return;
            }
        }

        /// <summary>Called by a building of this camp as it falls.</summary>
        public void NotifyFallen(OrcCampBuilding building, bool announce)
        {
            if (!_buildings.Remove(building)) return;
            _fallen.Add(new FallenBuilding(building.Kind, building.Cell, building.transform.eulerAngles.y));
            UpdateShield();
            if (building.Kind == OrcCampBuildingKind.Palisade) RebuildWalls();

            if (announce && building.Kind == OrcCampBuildingKind.Totem && Portal != null)
            {
                EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_totem_destroyed"),
                    EventSeverity.Good, EventGlyph.Portal, null, Portal.transform.position));
            }
        }

        private void UpdateShield()
        {
            if (Portal != null) Portal.IsShielded = TotemStanding;
        }

        private OrcCampBuilding Find(OrcCampBuildingKind kind)
        {
            foreach (var building in _buildings)
            {
                if (building != null && building.CurrentHealth > 0 && building.Kind == kind) return building;
            }
            return null;
        }

        private OrcCampBuilding SpawnerOf(OrcKind kind)
        {
            foreach (var building in _buildings)
            {
                if (building == null || building.CurrentHealth <= 0) continue;
                if (OrcBaseLayout.SpawnedBy(building.Kind) == kind) return building;
            }
            return null;
        }

        // -- the wall ---------------------------------------------------------------------------

        /// <summary>
        /// One NavMesh obstacle per straight run of standing palisade rather than one per cell --
        /// a level-3 camp has well over a hundred pieces, and the carve is what costs. Rebuilt
        /// whole whenever a piece falls; that is rare, and it is the moment the way opens.
        /// Gates carry their own obstacles (they are buildings with a plot of their own).
        /// </summary>
        private void RebuildWalls()
        {
            if (_wallObstacles != null) Destroy(_wallObstacles);
            _wallObstacles = null;

            var cells = new HashSet<Vector2Int>();
            foreach (var building in _buildings)
            {
                if (building != null && building.CurrentHealth > 0 && building.Kind == OrcCampBuildingKind.Palisade) cells.Add(building.Cell);
            }
            if (cells.Count == 0) return;

            var grid = GridManager.Instance;
            _wallObstacles = new GameObject($"OrcCampWalls {PortalCell.x},{PortalCell.y}");
            var covered = new HashSet<Vector2Int>();
            foreach (var cell in cells)
            {
                foreach (var axis in new[] { Vector2Int.right, Vector2Int.up })
                {
                    // a run starts where the previous cell along this axis is not wall
                    if (cells.Contains(cell - axis) || !cells.Contains(cell + axis)) continue;
                    var length = 1;
                    while (cells.Contains(cell + axis * length)) length++;
                    for (var i = 0; i < length; i++) covered.Add(cell + axis * i);
                    AddObstacle(grid, cell, axis == Vector2Int.right ? new Vector2Int(length, 1) : new Vector2Int(1, length));
                }
            }
            foreach (var cell in cells)
            {
                if (!covered.Contains(cell)) AddObstacle(grid, cell, Vector2Int.one);
            }
        }

        private void AddObstacle(GridManager grid, Vector2Int cell, Vector2Int size)
        {
            var go = new GameObject("WallRun");
            go.transform.SetParent(_wallObstacles.transform, false);
            go.transform.position = grid.GetFootprintCenterWorld(cell, size);
            var obstacle = go.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = new Vector3(0f, 1f, 0f);
            obstacle.size = new Vector3(size.x * grid.CellSize * 0.9f, 2f, size.y * grid.CellSize * 0.9f);
            obstacle.carving = true;
        }

        // -- the garrison -----------------------------------------------------------------------

        private void Update()
        {
            if (Portal != null && Portal.CurrentHealth <= 0) Portal = null;

            var layout = Layout;
            if (_slotGuards.Length != layout.Guards.Count) return;

            var balance = BalanceConfig.Instance;
            // a plain loop over a fixed array: this runs every frame for every camp, and an
            // iterator would hand the garbage collector something each time
            for (var k = 0; k < SpawnedKinds.Length; k++)
            {
                var kind = SpawnedKinds[k];
                if (!_kindsPosted[k]) continue;
                // the chief comes back slowly: he is the base's miniboss, not one of the guard
                var respawn = kind == OrcKind.Boss ? balance.OrcChiefRespawnSeconds : balance.OrcGarrisonRespawnSeconds;
                var missing = FirstMissingSlot(layout, kind);
                if (missing < 0 || SpawnerOf(kind) == null)
                {
                    _spawnTimers[kind] = respawn;
                    continue;
                }

                _spawnTimers.TryGetValue(kind, out var timer);
                timer -= Time.deltaTime;
                if (timer > 0f)
                {
                    _spawnTimers[kind] = timer;
                    continue;
                }
                _spawnTimers[kind] = respawn;
                SpawnFromSpawner(layout, missing, SpawnerOf(kind));
                if (kind == OrcKind.Boss)
                {
                    EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_chief_returned"),
                        EventSeverity.Danger, EventGlyph.Swords, null, SpawnerOf(kind).transform.position));
                }
            }
        }

        private static readonly OrcKind[] SpawnedKinds = { OrcKind.Raider, OrcKind.Brute, OrcKind.Thrower, OrcKind.Boss };

        // Which of SpawnedKinds the current plan posts at all -- worked out once per Build.
        private readonly bool[] _kindsPosted = new bool[4];

        private int FirstMissingSlot(CampLayout layout, OrcKind kind)
        {
            for (var i = 0; i < _slotGuards.Length; i++)
            {
                if (layout.Guards[i].Kind != kind) continue;
                var guard = _slotGuards[i];
                if (guard == null || guard.CurrentHealth <= 0) return i;
            }
            return -1;
        }

        private static NavMeshPath _path;

        /// <summary>A new guard steps out of its spawner's door and walks to its post -- or, where a wall stands between the two (a spawner between the rings, a post inside), appears at the post.</summary>
        private void SpawnFromSpawner(CampLayout layout, int slotIndex, OrcCampBuilding spawner)
        {
            var builder = OrcRaidManager.Instance;
            if (builder == null) return;
            var slot = layout.Guards[slotIndex];
            var post = LocalToWorld(slot.Local);
            var door = spawner.transform.position - spawner.transform.forward * (spawner.Footprint.y * 0.5f + 0.4f);

            _path ??= new NavMeshPath();
            var walkable = NavMesh.CalculatePath(door, post, NavMesh.AllAreas, _path) && _path.status == NavMeshPathStatus.PathComplete;
            var guard = builder.SpawnGuard(this, slot, post, walkable ? door : post);
            _slotGuards[slotIndex] = guard;
        }
    }
}
