using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// Where portals and camps go: the search round the Town Hall, the map's own spots, the main
    /// base's spot, ground tests, and building a portal.
    /// </summary>
    public partial class OrcRaidManager
    {
        /// <summary>
        /// Opens the map's portals: one per slot of PortalPlacement's template, each on solid
        /// ground as near its slot as the map allows. "Solid" is MeshMapApplier.IsGroundAt, i.e.
        /// actually standing on the map's Ground mesh at the flat playable height -- which rules
        /// out the lake and the decorative cliffs, either of which would strand every spawned orc
        /// off the NavMesh and send them walking through the world in a straight line.
        ///
        /// A slot with no standable ground anywhere near it is SKIPPED rather than fatal: a map
        /// whose outermost ring falls in the water should lose that one portal, not all of them.
        /// Losing every slot is the error, because then the map has no objective at all.
        /// </summary>
        private void TrySpawnPortalsNearTownHall()
        {
            var grid = GridManager.Instance;
            if (grid == null) return;

            if (!TryFindTownHallPosition(out var townHallPosition))
            {
                // HasAny said one exists, so this means it was destroyed within the same frame.
                // Nothing to anchor to; try again next frame.
                return;
            }

            // Random starting bearing so repeat playthroughs on the same map don't always put the
            // portals in the exact same places relative to the base.
            var template = PortalPlacement.BuildTemplate(Random.Range(0f, Mathf.PI * 2f));
            var opened = 0;

            var maxBearingDeviation = PortalPlacement.MaxBearingDeviationRadians(template.Count);
            // The first slot's own distance is the nearest any portal may end up, however the
            // search has to move it -- that is what the sheet's portal_distance_from_town_hall
            // means, and the template puts every other slot further out again.
            var nearestAllowed = template[0].DistanceMeters;

            var designed = MeshMapApplier.Instance != null ? MeshMapApplier.Instance.CurrentMap : null;
            if (designed != null && (designed.HasMainBaseSpot || designed.CampSpots.Length > 0))
            {
                // A designed map says where the orcs stand: its rivers and mountains are laid out
                // round those spots, and a search round the Town Hall could put a camp anywhere.
                opened = OpenAtMapSpots(grid, townHallPosition, designed);
            }
            // The main base first, and furthest: its ground is the biggest thing on the map, and the
            // camps fit round it far more easily than it fits round them.
            else if (TryFindMainBaseSpot(grid, townHallPosition, out var mainCell))
            {
                var main = SpawnPortal(grid, mainCell, isMain: true);
                MainPortalCell = mainCell;
                BuildCamp(main, template.Count, OrcBaseLayout.MainLevel);
            }
            else
            {
                Debug.LogWarning("OrcRaidManager: no room for the main base on this map -- closing every camp's portal wins instead.");
            }

            foreach (var slot in designed != null && (designed.HasMainBaseSpot || designed.CampSpots.Length > 0)
                         ? new List<PortalPlacement.Slot>() : template)
            {
                // The biggest camp that fits round the slot: the square it will grow into is
                // reserved now, so a smaller one is only taken where the ground has no room.
                var found = false;
                var cell = default(Vector2Int);
                var maxLevel = OrcBaseLayout.MaxLevel;
                for (; maxLevel >= 1 && !found; maxLevel--)
                {
                    found = TryFindPortalSpot(grid, townHallPosition, slot, maxBearingDeviation, nearestAllowed, maxLevel, out cell);
                    if (found) break;
                }
                if (!found)
                {
                    Debug.LogWarning($"OrcRaidManager: no valid ground for the portal slot at {slot.DistanceMeters:0.#} m " +
                                     $"from the Town Hall at {townHallPosition} (all water/cliff/occupied). That portal is skipped.");
                    continue;
                }

                // The template is ordered nearest first, so the order they open in is the rank.
                var portal = SpawnPortal(grid, cell);
                BuildCamp(portal, opened, maxLevel);
                opened++;
            }

            PickNextSource();

            // Set before the early return below, and whatever happened: the map geometry won't
            // change, so re-running this every frame would just repeat the same failure forever.
            _portalsPlaced = true;

            if (opened == 0)
            {
                Debug.LogError($"OrcRaidManager: not one of the {template.Count} portal slots around the Town Hall at {townHallPosition} " +
                               "found valid ground. No portals will exist, no raids will happen, and the map cannot be won this session.");
                return;
            }

            // Only starts counting once the portals actually exist, so the player isn't raided
            // from nowhere during the time it takes them to place the Town Hall.
            //
            // The full interval rather than the score's, deliberately: the portals open on the
            // frame the Town Hall goes down, when the score IS a Town Hall and five settlers, and
            // asking PlayerProgression for that would buy a scan over the buildings to be told so.
            //
            // And never before the town has settled in (the user, 2026-10-20: an easy start) --
            // the same seven minutes migration and sickness wait out.
            _raidTimer = Mathf.Max(BalanceConfig.Instance.RaidIntervalSeconds, BalanceConfig.Instance.SettlingInSeconds);
            // The nearest portal is the one the camera goes to: the closest threat to the town.
            EventLogManager.Instance?.Post(GameEvent.At(opened == 1
                    ? Localization.Get("#log_portal_opened")
                    : Localization.Format("#log_portals_opened", opened),
                EventSeverity.Danger, EventGlyph.Portal, null, NearestPortal(townHallPosition)));
        }

        /// <summary>
        /// Opens the portals on a designed map's own spots: the main base on its spot with its gate
        /// to the south (the map's cirque opens that way), then the camps, nearest the Town Hall
        /// first (that is their rank), each the biggest that fits within a few metres of its spot.
        /// Returns how many camps opened.
        /// </summary>
        private int OpenAtMapSpots(GridManager grid, Vector3 townHall, MeshMapDefinition map)
        {
            var mapApplier = MeshMapApplier.Instance;
            var camps = new List<(Vector2 spot, int level)>();
            for (var i = 0; i < map.CampSpots.Length; i++)
            {
                camps.Add((map.CampSpots[i], i < map.CampLevels.Length ? map.CampLevels[i] : OrcBaseLayout.MaxLevel));
            }
            camps.Sort((a, b) => (new Vector3(a.spot.x, 0f, a.spot.y) - townHall).sqrMagnitude
                .CompareTo((new Vector3(b.spot.x, 0f, b.spot.y) - townHall).sqrMagnitude));

            if (map.HasMainBaseSpot)
            {
                var spot = new Vector3(map.MainBaseSpot.x, 0f, map.MainBaseSpot.y);
                var half = MainPortalFootprint / 2;
                if (TryFindNear(grid, spot, half, cell => grid.IsWithinBounds(cell, MainPortalFootprint) &&
                        grid.IsAreaFree(cell, MainPortalFootprint) &&
                        CampSquareFits(grid, mapApplier, cell, OrcBaseLayout.MainLevel, townHall, quartersOverride: 0), out var mainCell))
                {
                    var main = SpawnPortal(grid, mainCell, isMain: true, rotation: Quaternion.identity);
                    MainPortalCell = mainCell;
                    BuildCamp(main, camps.Count, OrcBaseLayout.MainLevel);
                }
                else
                {
                    Debug.LogWarning($"OrcRaidManager: the main base does not fit on the map's spot {map.MainBaseSpot} -- closing every camp's portal wins instead.");
                }
            }

            var opened = 0;
            foreach (var (camp, designedLevel) in camps)
            {
                var spot = new Vector3(camp.x, 0f, camp.y);
                var found = false;
                var cell = default(Vector2Int);
                // the level the map made its ground for; lower only if that ground has been spoilt
                var maxLevel = Mathf.Clamp(designedLevel, 1, OrcBaseLayout.MaxLevel);
                for (; maxLevel >= 1; maxLevel--)
                {
                    var level = maxLevel;
                    found = TryFindNear(grid, spot, PortalFootprint / 2, c => grid.IsWithinBounds(c, PortalFootprint) &&
                        grid.IsAreaFree(c, PortalFootprint) && IsSolidGround(grid, mapApplier, c) &&
                        CampSquareFits(grid, mapApplier, c, level, townHall), out cell);
                    if (found) break;
                }
                if (!found)
                {
                    Debug.LogWarning($"OrcRaidManager: no ground for the camp on the map's spot {camp}. That camp is skipped.");
                    continue;
                }
                if (maxLevel < designedLevel)
                {
                    Debug.LogWarning($"OrcRaidManager: the camp on the map's spot {camp} has room for level {maxLevel} only, not the {designedLevel} its map was made for.");
                }
                var portal = SpawnPortal(grid, cell);
                BuildCamp(portal, opened, maxLevel);
                opened++;
            }
            return opened;
        }

        /// <summary>The plot nearest a spot (its centre on the spot first, then outwards in rings a metre apart) that passes `fits`.</summary>
        private static bool TryFindNear(GridManager grid, Vector3 spot, Vector2Int halfPlot, System.Func<Vector2Int, bool> fits, out Vector2Int cell)
        {
            var centre = grid.WorldToCell(spot) - halfPlot;
            var reach = Mathf.CeilToInt(SpotSearchMeters / grid.CellSize);
            for (var ring = 0; ring <= reach; ring++)
            {
                for (var dx = -ring; dx <= ring; dx++)
                {
                    for (var dz = -ring; dz <= ring; dz++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != ring) continue;
                        var candidate = centre + new Vector2Int(dx, dz);
                        if (!fits(candidate)) continue;
                        cell = candidate;
                        return true;
                    }
                }
            }
            cell = default;
            return false;
        }

        private static Vector3 NearestPortal(Vector3 from)
        {
            var nearest = from;
            var best = float.MaxValue;
            foreach (var portal in OrcPortal.All)
            {
                var distance = (portal.transform.position - from).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                nearest = portal.transform.position;
            }
            return nearest;
        }

        private static bool TryFindTownHallPosition(out Vector3 position)
        {
            foreach (var instance in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                if (instance.Data != null && instance.Data.buildingName == TownHallBuildingName)
                {
                    position = instance.transform.position;
                    return true;
                }
            }

            position = default;
            return false;
        }

        /// <summary>
        /// The nearest standable cell to one template slot: its own bearing at its own distance
        /// first, then the same bearing walked in and out to either side, and only then a bearing
        /// nudged off it -- never by more than the cap the spread depends on.
        ///
        /// BEARING outside, radius inside, and the nesting is the whole point. The other way round
        /// -- sweeping every bearing before nudging the radius by four metres -- is what a first
        /// cut of this did, and it would rather send a portal a hundred and eighty degrees around
        /// the settlement than move it four metres. On the real map that produced two portals four
        /// degrees apart (PortalOpeningTests caught it), which is exactly the crowding the spread
        /// exists to prevent. A portal's bearing is what the player plans their walls around; its
        /// exact distance is not, so the distance is what gives way.
        /// </summary>
        private static bool TryFindPortalSpot(GridManager grid, Vector3 townHallPosition, PortalPlacement.Slot slot,
            float maxBearingDeviationRadians, float nearestAllowedMeters, int campLevel, out Vector2Int cell)
        {
            var mapApplier = MeshMapApplier.Instance;

            foreach (var angle in PortalPlacement.BearingsToTry(slot.AngleRadians, PortalDirectionSamples, maxBearingDeviationRadians))
            {
                foreach (var radius in RadiiToTry(slot.DistanceMeters, nearestAllowedMeters))
                {
                    var candidate = townHallPosition + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    var candidateCell = grid.WorldToCell(candidate);

                    if (!grid.IsWithinBounds(candidateCell, PortalFootprint) || !grid.IsAreaFree(candidateCell, PortalFootprint)) continue;
                    if (!IsSolidGround(grid, mapApplier, candidateCell)) continue;
                    if (!CampSquareFits(grid, mapApplier, candidateCell, campLevel, townHallPosition)) continue;

                    cell = candidateCell;
                    return true;
                }
            }

            cell = default;
            return false;
        }

        /// <summary>Whether the whole square a camp of `level` covers round a portal on `cell` is on the map, free and standable.</summary>
        private static bool CampSquareFits(GridManager grid, MeshMapApplier mapApplier, Vector2Int cell, int level, Vector3 townHall,
            int? quartersOverride = null)
        {
            var layout = OrcBaseLayout.For(level);
            var plot = Vector2Int.one * layout.PortalSize;
            var position = grid.GetFootprintCenterWorld(cell, plot);
            var quarters = quartersOverride ?? OrcBaseLayout.QuarterTurns(FacingFrom(position, townHall).eulerAngles.y);
            var bounds = layout.Bounds;
            OrcBaseLayout.ToWorld(bounds.min, bounds.size, cell + plot / 2, quarters, out var min, out var size);
            if (!grid.IsWithinBounds(min, size)) return false;
            // cheap occupancy first, then the ground under each cell. A boulder or a tree does
            // not count against the square: the camp clears them when it goes up.
            for (var x = 0; x < size.x; x++)
            {
                for (var z = 0; z < size.y; z++)
                {
                    var c = min + new Vector2Int(x, z);
                    if (!grid.IsAreaFree(c, Vector2Int.one) && !IsScenery(c)) return false;
                }
            }
            return IsSolidGround(grid, mapApplier, min, size);
        }

        /// <summary>
        /// Where the main base goes: as far from the Town Hall as its ground fits, looking round
        /// the compass at each distance -- the furthest spot first, because the user's main base
        /// is the end of the map, not a neighbour. Its square is about 38x32 cells, so this runs
        /// the full check only where the cheap one (occupancy) passes.
        /// </summary>
        private static bool TryFindMainBaseSpot(GridManager grid, Vector3 townHall, out Vector2Int cell)
        {
            var mapApplier = MeshMapApplier.Instance;
            var start = Random.Range(0f, Mathf.PI * 2f);
            for (var radius = 88f; radius >= 30f; radius -= 3f)
            {
                for (var i = 0; i < 36; i++)
                {
                    var angle = start + Mathf.PI * 2f * i / 36f;
                    var centre = townHall + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    var candidate = grid.WorldToCell(centre) - Vector2Int.one * 2;
                    if (!grid.IsWithinBounds(candidate, MainPortalFootprint) || !grid.IsAreaFree(candidate, MainPortalFootprint)) continue;
                    if (!CampSquareFits(grid, mapApplier, candidate, OrcBaseLayout.MainLevel, townHall)) continue;
                    cell = candidate;
                    return true;
                }
            }
            cell = default;
            return false;
        }

        /// <summary>A boulder or a tree on the cell -- scenery an orc camp clears for itself.</summary>
        public static bool IsScenery(Vector2Int cell) =>
            (RockSpawner.Instance != null && RockSpawner.Instance.Has(cell)) ||
            (TreesAreaSpawner.Instance != null && TreesAreaSpawner.Instance.Has(cell));

        /// <summary>Every cell of a portal's plot is dry, flat playable ground -- one cell of lake under a corner of the platform would leave the arch hanging over the water.</summary>
        private static bool IsSolidGround(GridManager grid, MeshMapApplier mapApplier, Vector2Int cell)
        {
            return IsSolidGround(grid, mapApplier, cell, PortalFootprint);
        }

        private static bool IsSolidGround(GridManager grid, MeshMapApplier mapApplier, Vector2Int cell, Vector2Int footprint)
        {
            if (mapApplier == null) return true;
            for (var dx = 0; dx < footprint.x; dx++)
            {
                for (var dy = 0; dy < footprint.y; dy++)
                {
                    var c = cell + new Vector2Int(dx, dy);
                    if (mapApplier.IsWaterCell(c)) return false;
                    if (!mapApplier.IsGroundAt(grid.GetFootprintCenterWorld(c, Vector2Int.one))) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// The distances to try along one bearing, nearest the slot's own first: it, then a step
        /// out, a step in, two steps out, two steps in, and so on.
        ///
        /// The inward walk stops at the template's FIRST slot -- the distance the sheet says a
        /// portal may not be nearer than. Letting it run to PortalPlacement's absolute floor
        /// instead put a portal eight metres from the Town Hall on a real map, inside the
        /// settlement and past every wall, which is precisely what that sheet number exists to
        /// prevent. Outward has no such limit: the grid bounds check turns back anything that
        /// leaves the map, and a portal further out costs the design nothing.
        /// </summary>
        private static IEnumerable<float> RadiiToTry(float slotDistanceMeters, float nearestAllowedMeters)
        {
            yield return slotDistanceMeters;

            for (var delta = PortalRadiusStepMeters; delta <= PortalMaxRadiusDeltaMeters; delta += PortalRadiusStepMeters)
            {
                yield return slotDistanceMeters + delta;

                var inward = slotDistanceMeters - delta;
                if (inward >= nearestAllowedMeters) yield return inward;
            }
        }

        /// <summary>Builds one portal on a cell already known to be standable, and hands it back so the caller can set the health a save left it on.</summary>
        /// <param name="rotation">A fixed facing (a designed map's main base faces south); otherwise the portal faces the Town Hall.</param>
        private OrcPortal SpawnPortal(GridManager grid, Vector2Int cell, bool isMain = false, Quaternion? rotation = null)
        {
            var footprint = isMain ? MainPortalFootprint : PortalFootprint;
            var position = grid.GetFootprintCenterWorld(cell, footprint);

            EnsureMaterials();

            var root = new GameObject(isMain ? "OrcMainPortal" : "OrcPortal");
            root.transform.SetPositionAndRotation(position, rotation ?? FacingTownHall(position));
            var portal = root.AddComponent<OrcPortal>();
            portal.SetCell(cell);
            portal.IsMain = isMain;

            if (isMain ? mainPortalModel != null : Pick(portalActiveModels, 1) != null && Pick(portalPassiveModels, 1) != null)
            {
                // Both looks are built once and switched (see OrcPortal.ShowActive); its camp
                // swaps them for its level's (ApplyPortalLevel).
                ApplyPortalLevel(portal, isMain ? OrcBaseLayout.MainLevel : 1);
            }
            else
            {
                // The cube arch the models replaced: two posts and a lintel.
                AddPortalPart(root.transform, new Vector3(-0.6f, 1.5f, 0f), new Vector3(0.4f, 3f, 0.4f));
                AddPortalPart(root.transform, new Vector3(0.6f, 1.5f, 0f), new Vector3(0.4f, 3f, 0.4f));
                AddPortalPart(root.transform, new Vector3(0f, 3.1f, 0f), new Vector3(1.6f, 0.4f, 0.4f));
            }

            // One click target covering the whole arch (the parts' own colliders are stripped), so
            // the player can tap the portal to order an assault on it -- see ArmyOrderInput. A
            // TRIGGER, like every other click affordance in this project: it must not become a
            // wall that raiding orcs and marching soldiers get wedged against.
            var clickCollider = root.AddComponent<BoxCollider>();
            clickCollider.isTrigger = true;
            clickCollider.center = new Vector3(0f, isMain ? 1.8f : 1.2f, 0f);
            clickCollider.size = isMain ? new Vector3(3.6f, 3.6f, 1.8f) : new Vector3(1.9f, 2.4f, 1.2f);

            // Occupies its cell so the player can't drop a building on top of the arch -- and, with
            // several portals, so the search for the next slot cannot pick a cell one of them is
            // already standing on.
            grid.SetAreaOccupied(cell, footprint, true);
            // Testing convenience (see PortalFogRevealRadiusCells) -- the player can see where
            // raids come from instead of having them arrive out of unexplored fog.
            FogOfWarManager.Instance?.RevealPermanent(cell, PortalFogRevealRadiusCells);

            return portal;
        }

        /// <summary>The standing portals, nearest the Town Hall first -- the order a camp's rank follows.</summary>
        private static List<OrcPortal> PortalsByDistance()
        {
            var portals = new List<OrcPortal>(OrcPortal.All);
            if (TryFindTownHallPosition(out var townHall))
            {
                portals.Sort((a, b) => (a.transform.position - townHall).sqrMagnitude
                    .CompareTo((b.transform.position - townHall).sqrMagnitude));
            }
            return portals;
        }

        /// <summary>
        /// A portal turns its front -- the side the whirlpool and the steps face -- towards the
        /// Town Hall, snapped to a quarter turn so the square platform keeps to its plot.
        /// </summary>
        private static Quaternion FacingTownHall(Vector3 position)
        {
            if (!TryFindTownHallPosition(out var townHall)) return Quaternion.identity;
            return FacingFrom(position, townHall);
        }

        private static Quaternion FacingFrom(Vector3 position, Vector3 townHall)
        {
            var toTown = townHall - position;
            toTown.y = 0f;
            if (toTown.sqrMagnitude < 0.01f) return Quaternion.identity;
            // The model's front is its -Z, so its +Z points away from the town.
            var yaw = Mathf.Atan2(-toTown.x, -toTown.z) * Mathf.Rad2Deg;
            return Quaternion.Euler(0f, Mathf.Round(yaw / 90f) * 90f, 0f);
        }

        private void AddPortalPart(Transform parent, Vector3 localPosition, Vector3 size)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = "PortalPart";
            Destroy(part.GetComponent<BoxCollider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = _portalMaterial;
        }
    }
}
