using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Ships
{
    /// <summary>
    /// The orcs at sea (the user, 2026-10-28, phase 4b): ONE shipyard, in the water by their main
    /// base, that fires like a tower and is raised again a while after it is broken; out of it, on
    /// the raids' clock (OrcRaidManager), a fleet -- transports with the raid aboard and war ships
    /// to guard them -- sails to the beach nearest the player's Town Hall, puts the orcs ashore and
    /// goes home. The war ships turn on any of the player's ships that comes near.
    /// </summary>
    public partial class ShipManager
    {
        private const float OrcThinkSeconds = 0.5f;
        private const float OrcArriveMeters = 3f;
        private const float OrcHomeMeters = 8f;
        private const float EscortHuntMeters = 25f;
        private const float EscortSpread = 6f;

        [SerializeField] private GameObject orcShipyardModel;

        private OrcCampBuilding _orcShipyard;
        private float _orcShipyardRebuildIn;
        private int _orcShipyardSavedHealth;
        private float _orcThinkIn;
        private int _nextOrcRaid = 1;

        /// <summary>The orcs' shipyard while it stands, else null.</summary>
        public OrcCampBuilding OrcShipyard => _orcShipyard != null && _orcShipyard.CurrentHealth > 0 ? _orcShipyard : null;

        /// <summary>Seconds until the orcs raise their broken shipyard again; 0 while it stands (or before it was ever raised).</summary>
        public float OrcShipyardRebuildIn => _orcShipyardRebuildIn;

        private void OnEnable() => OrcCampBuilding.OnDestroyed += OnOrcBuildingFallen;
        private void OnDisable() => OrcCampBuilding.OnDestroyed -= OnOrcBuildingFallen;

        private void OnOrcBuildingFallen(OrcCampBuilding building)
        {
            if (building == null || building != _orcShipyard) return;
            _orcShipyard = null;
            _orcShipyardRebuildIn = Mathf.Max(1f, BalanceConfig.Instance != null ? BalanceConfig.Instance.OrcShipyardRebuildSeconds : 300f);
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_orc_shipyard_destroyed"), EventSeverity.Good, EventGlyph.Swords, null, building.transform.position));
        }

        /// <summary>A save's shipyard: its health (0 = none standing) and the wait to rebuild; applied when it is (re)raised.</summary>
        public void RestoreOrcShipyard(int health, float rebuildIn)
        {
            _orcShipyardRebuildIn = Mathf.Max(0f, rebuildIn);
            _orcShipyardSavedHealth = health;
            if (_orcShipyard == null) return;
            if (health > 0) _orcShipyard.SetCurrentHealth(health);
            else if (rebuildIn > 0f)
            {
                _orcShipyard.Dismantle();
                _orcShipyard = null;
            }
        }

        /// <summary>The nearest ship of one side within reach of a point, or null.</summary>
        public ShipUnit NearestShip(Vector3 at, float range, bool orc)
        {
            ShipUnit best = null;
            var bestSq = range * range;
            foreach (var ship in _ships)
            {
                if (ship == null || ship.IsOrc != orc || !ship.IsAlive) continue;
                var d = ship.transform.position - at;
                var sq = d.x * d.x + d.z * d.z;
                if (sq >= bestSq) continue;
                bestSq = sq;
                best = ship;
            }
            return best;
        }

        private static OrcCamp MainCamp()
        {
            foreach (var camp in OrcCamp.All)
            {
                if (camp != null && camp.IsMain) return camp;
            }
            return null;
        }

        private void TickOrcs()
        {
            if (!HasSea) return;
            if (_orcShipyardRebuildIn > 0f)
            {
                _orcShipyardRebuildIn -= Time.deltaTime;
                if (_orcShipyardRebuildIn <= 0f)
                {
                    _orcShipyardRebuildIn = 0f;
                    _orcShipyardSavedHealth = 0;
                    if (RaiseOrcShipyard() != null)
                        EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_orc_shipyard_rebuilt"), EventSeverity.Warning, EventGlyph.Alert, null, _orcShipyard.transform.position));
                }
            }
            else if (_orcShipyard == null) RaiseOrcShipyard();

            _orcThinkIn -= Time.deltaTime;
            if (_orcThinkIn > 0f) return;
            _orcThinkIn = OrcThinkSeconds;
            for (var i = _ships.Count - 1; i >= 0; i--)
            {
                var ship = _ships[i];
                if (ship != null && ship.IsOrc && ship.IsAlive) Steer(ship);
            }
        }

        /// <summary>
        /// Raises the shipyard on the sea nearest the main base's centre -- not while the base is
        /// cleared. Null when there is no main base (yet) or no sea by it.
        /// </summary>
        public OrcCampBuilding RaiseOrcShipyard()
        {
            if (_orcShipyard != null) return _orcShipyard;
            var camp = MainCamp();
            var grid = Grid.GridManager.Instance;
            if (camp == null || camp.Cleared || grid == null) return null;
            var centre = grid.GetFootprintCenterWorld(camp.Centre, Vector2Int.one);
            if (!TryNearestSea(centre, 120f, out var at)) return null;

            var root = new GameObject("OrcShipyard");
            var away = at - centre;
            away.y = 0f;
            root.transform.SetPositionAndRotation(new Vector3(at.x, WaterLevel + 0.1f, at.z),
                Quaternion.LookRotation(away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward));
            if (orcShipyardModel != null)
            {
                // the slipway runs out to sea: its stem end (the model's -y, the game's +z after the turn) faces away from the base
                var model = Instantiate(orcShipyardModel, root.transform);
                model.name = "Model";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * orcShipyardModel.transform.rotation;
            }
            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(3f, 2.4f, 4f);
            box.center = new Vector3(0f, 0.9f, 0f);
            var building = root.AddComponent<OrcCampBuilding>();
            building.Initialize(OrcCampBuildingKind.Shipyard, OrcBaseLayout.MainLevel, null, grid.WorldToCell(at), new Vector2Int(3, 4), null);
            if (_orcShipyardSavedHealth > 0) building.SetCurrentHealth(_orcShipyardSavedHealth);
            _orcShipyardSavedHealth = 0;
            _orcShipyard = building;
            return building;
        }

        /// <summary>The sea a ship can reach nearest a point on land, searched outwards in rings.</summary>
        private bool TryNearestSea(Vector3 from, float maxRadius, out Vector3 point)
        {
            point = default;
            if (TrySampleSea(from, 2f, out point)) return true;
            for (var r = 4f; r <= maxRadius; r += 3f)
            {
                var steps = Mathf.Clamp(Mathf.RoundToInt(r * 0.8f), 12, 64);
                var best = float.MaxValue;
                var found = false;
                for (var k = 0; k < steps; k++)
                {
                    var a = k * Mathf.PI * 2f / steps;
                    var probe = from + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                    if (!TrySampleSea(probe, 1.5f, out var p)) continue;
                    var d = (p - from).sqrMagnitude;
                    if (d >= best) continue;
                    best = d;
                    point = p;
                    found = true;
                }
                if (found) return true;
            }
            return false;
        }

        /// <summary>Sea by a beach, nearest the player's Town Hall: where a fleet puts its orcs ashore.</summary>
        public bool TryFindOrcLanding(out Vector3 sea)
        {
            sea = default;
            var hall = BuildingInstance.TownHall;
            if (hall == null) return false;
            var from = hall.transform.position;
            for (var r = 8f; r <= 220f; r += 4f)
            {
                var steps = Mathf.Clamp(Mathf.RoundToInt(r * 0.5f), 12, 48);
                for (var k = 0; k < steps; k++)
                {
                    var a = k * Mathf.PI * 2f / steps;
                    var probe = from + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                    if (TrySampleSea(probe, 1.5f, out var p) && TryFindLandingAt(p, out _))
                    {
                        sea = p;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// A raid by sea: `size` orcs of `level` in as many transports as they need, with
        /// orc_fleet_escorts war ships, out of the shipyard for the beach nearest the Town Hall.
        /// False (nothing sails) while the shipyard is broken or there is nowhere to land.
        /// </summary>
        public bool LaunchOrcFleet(int size, int level)
        {
            var shipyard = OrcShipyard;
            if (shipyard == null || size <= 0 || !TryFindOrcLanding(out var landing)) return false;
            if (!TryNearestSea(shipyard.transform.position, 20f, out var home)) return false;

            var raid = _nextOrcRaid++;
            var seats = Mathf.Max(1, ShipStats.Capacity(ShipKind.OrcTransport));
            var facing = Quaternion.LookRotation((landing - home).sqrMagnitude > 0.01f ? (landing - home).normalized : Vector3.forward);
            ShipUnit first = null;
            var n = 0;
            for (var left = size; left > 0; left -= seats, n++)
            {
                var ship = SpawnOrcShip(ShipKind.OrcTransport, home, n, facing, raid, Mathf.Min(seats, left), level, landing);
                if (first == null) first = ship;
            }
            var escorts = BalanceConfig.Instance != null ? BalanceConfig.Instance.OrcFleetEscorts : 1;
            for (var k = 0; k < escorts; k++, n++)
            {
                var side = (k % 2 == 0 ? 1f : -1f) * EscortSpread * (1 + k / 2);
                SpawnOrcShip(ShipKind.OrcWar, home, n, facing, raid, 0, level, landing + facing * Vector3.right * side);
            }
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Format("#log_orc_fleet", size), EventSeverity.Danger, EventGlyph.Swords, null,
                home, first != null ? first.transform : null));
            return true;
        }

        private ShipUnit SpawnOrcShip(ShipKind kind, Vector3 home, int index, Quaternion facing, int raid, int cargo, int level, Vector3 destination)
        {
            var at = home + facing * new Vector3((index % 3 - 1) * 3.5f, 0f, -(index / 3) * 5f);
            if (!TrySampleSea(at, 6f, out at)) at = home;
            var ship = Spawn(kind, at, facing, ShipStats.MaxHealth(kind));
            ship.OrcRaid = raid;
            ship.OrcCargo = cargo;
            ship.OrcLevel = level;
            ship.OrcDestination = TrySampleSea(destination, 8f, out var d) ? d : destination;
            ship.SailTo(ship.OrcDestination);
            return ship;
        }

        /// <summary>An orc ship's next move: out to its beach, the orcs ashore, home again; a war ship hunts the player's ships on the way.</summary>
        private void Steer(ShipUnit ship)
        {
            var here = ship.transform.position;
            if (ship.OrcHomeward)
            {
                var shipyard = OrcShipyard;
                var home = shipyard != null && TryNearestSea(shipyard.transform.position, 20f, out var h) ? h : ship.OrcDestination;
                if (Flat(here - home) <= OrcHomeMeters)
                {
                    Remove(ship);
                    return;
                }
                if (!ship.IsMoving && !ship.SailTo(home)) Remove(ship);
                return;
            }

            if (ship.Kind == ShipKind.OrcWar)
            {
                var prey = NearestShip(here, EscortHuntMeters, orc: false);
                if (prey != null)
                {
                    ship.SailTo(prey.transform.position);
                    return;
                }
                if (!RaidStillOutbound(ship.OrcRaid))
                {
                    ship.OrcHomeward = true;
                    return;
                }
            }

            if (Flat(here - ship.OrcDestination) > OrcArriveMeters)
            {
                if (!ship.IsMoving && !ship.SailTo(ship.OrcDestination)) ship.OrcHomeward = true;
                return;
            }
            if (ship.Kind != ShipKind.OrcTransport) return;    // an escort waits off the beach

            ship.Stop();
            if (ship.OrcCargo > 0 && TryFindLandingAt(here, out var beach))
            {
                var leader = OrcRaidManager.Instance != null ? OrcRaidManager.Instance.SpawnLanding(beach, ship.OrcCargo, ship.OrcLevel) : null;
                EventLogManager.Instance?.Post(GameEvent.At(Localization.Format("#log_orc_landing", ship.OrcCargo), EventSeverity.Danger, EventGlyph.Swords, null,
                    beach, leader != null ? leader.transform : null));
            }
            ship.OrcCargo = 0;
            ship.OrcHomeward = true;
        }

        private bool RaidStillOutbound(int raid)
        {
            foreach (var other in _ships)
            {
                if (other != null && other.IsAlive && other.Kind == ShipKind.OrcTransport && other.OrcRaid == raid && !other.OrcHomeward) return true;
            }
            return false;
        }
    }
}
