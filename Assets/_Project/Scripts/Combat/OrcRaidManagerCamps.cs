using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// The walled camps and the main base: models by kind and level, building, growing, guards,
    /// plunder and ruins.
    /// </summary>
    public partial class OrcRaidManager
    {
        // ---------------------------------------------------------------------------------------
        // Orc camps: the walled bases the user drew level by level (2026-10-16). OrcBaseLayout
        // holds the plans, OrcCamp runs a camp; what is here is building the pieces, since the
        // models live on this component.

        private static GameObject Pick(GameObject[] byLevel, int level)
        {
            if (byLevel == null || byLevel.Length == 0) return null;
            return byLevel[Mathf.Clamp(level, 1, byLevel.Length) - 1];
        }

        private GameObject ModelOf(OrcCampBuildingKind kind, int level) => level >= OrcBaseLayout.MainLevel ? MainModelOf(kind) : kind switch
        {
            OrcCampBuildingKind.Watchtower => Pick(watchtowerModels, level),
            OrcCampBuildingKind.WarTent => warTentModel,
            OrcCampBuildingKind.Totem => Pick(totemModels, level),
            OrcCampBuildingKind.Gate => Pick(gateModels, level),
            OrcCampBuildingKind.GateTower => gateTowerModel,
            OrcCampBuildingKind.Storehouse => Pick(storehouseModels, level),
            OrcCampBuildingKind.BrutePit => brutePitModel,
            OrcCampBuildingKind.ThrowerRange => throwerRangeModel,
            OrcCampBuildingKind.Cage => Pick(cageModels, level),
            _ => null,
        };

        private GameObject MainModelOf(OrcCampBuildingKind kind) => kind switch
        {
            OrcCampBuildingKind.Watchtower => mainTowerModel,
            OrcCampBuildingKind.WarTent => mainBarracksModel,
            OrcCampBuildingKind.Totem => mainTotemModel,
            OrcCampBuildingKind.Gate => mainGateModel,
            OrcCampBuildingKind.GateTower => mainGateModel,
            OrcCampBuildingKind.Storehouse => mainStorehouseModel,
            OrcCampBuildingKind.BrutePit => mainBrutePitModel,
            OrcCampBuildingKind.ThrowerRange => mainThrowerRangeModel,
            OrcCampBuildingKind.ChiefHouse => chiefHouseModel,
            OrcCampBuildingKind.Cage => mainCageModel,
            OrcCampBuildingKind.Treasury => mainTreasuryModel,
            _ => null,
        };

        private GameObject RuinOf(OrcCampBuildingKind kind, int level) => level >= OrcBaseLayout.MainLevel ? kind switch
        {
            OrcCampBuildingKind.Watchtower => mainTowerRuinModel,
            OrcCampBuildingKind.WarTent => mainBarracksRuinModel,
            OrcCampBuildingKind.Totem => mainTotemRuinModel,
            OrcCampBuildingKind.Gate => mainGateRuinModel,
            OrcCampBuildingKind.GateTower => mainGateRuinModel,
            OrcCampBuildingKind.Storehouse => mainStorehouseRuinModel,
            OrcCampBuildingKind.BrutePit => mainBrutePitRuinModel,
            OrcCampBuildingKind.ThrowerRange => mainThrowerRangeRuinModel,
            OrcCampBuildingKind.ChiefHouse => chiefHouseRuinModel,
            OrcCampBuildingKind.Cage => mainCageRuinModel,
            OrcCampBuildingKind.Treasury => mainTreasuryRuinModel,
            _ => null,
        } : RuinOf(kind);

        private GameObject RuinOf(OrcCampBuildingKind kind) => kind switch
        {
            OrcCampBuildingKind.Watchtower => watchtowerRuinModel,
            OrcCampBuildingKind.WarTent => warTentRuinModel,
            OrcCampBuildingKind.Totem => totemRuinModel,
            OrcCampBuildingKind.Gate => gateRuinModel,
            OrcCampBuildingKind.GateTower => gateTowerRuinModel,
            OrcCampBuildingKind.Storehouse => storehouseRuinModel,
            OrcCampBuildingKind.BrutePit => brutePitRuinModel,
            OrcCampBuildingKind.ThrowerRange => throwerRangeRuinModel,
            OrcCampBuildingKind.Cage => cageRuinModel,
            _ => null,
        };

        /// <summary>How tall the tap target of a camp building is.</summary>
        private static float ClickHeight(OrcCampBuildingKind kind) => kind switch
        {
            OrcCampBuildingKind.Watchtower => 3f,
            OrcCampBuildingKind.GateTower => 2.6f,
            OrcCampBuildingKind.Totem => 1.6f,
            OrcCampBuildingKind.Palisade => 1f,
            OrcCampBuildingKind.Gate => 1.4f,
            _ => 1.2f,
        };

        /// <param name="checkGround">False for a camp coming back from a save: its ground was checked when it went up, and on load the map's colliders may not be ready to be asked again.</param>
        private OrcCamp CreateCamp(Vector2Int portalCell, OrcPortal portal, int quarters, int rank, int maxLevel, bool cleared,
            bool checkGround = true)
        {
            var grid = GridManager.Instance;
            var root = new GameObject($"OrcCamp {portalCell.x},{portalCell.y}");
            root.transform.position = grid.GetFootprintCenterWorld(portalCell, PortalFootprint);
            var camp = root.AddComponent<OrcCamp>();
            camp.Initialize(portalCell, portal, quarters, rank, maxLevel, cleared);
            var mapApplier = MeshMapApplier.Instance;
            // without the raycasts, water is still known from the map's own data
            camp.Reserve(grid, cell => checkGround
                ? IsSolidGround(grid, mapApplier, cell, Vector2Int.one)
                : mapApplier == null || !mapApplier.IsWaterCell(cell));
            return camp;
        }

        /// <summary>Opens a fresh camp round a portal, at level 1, with a full garrison.</summary>
        private OrcCamp BuildCamp(OrcPortal portal, int rank, int maxLevel)
        {
            var quarters = OrcBaseLayout.QuarterTurns(portal.transform.eulerAngles.y);
            var camp = CreateCamp(portal.Cell, portal, quarters, rank, maxLevel, cleared: false);
            camp.Build(maxLevel == OrcBaseLayout.MainLevel ? OrcBaseLayout.MainLevel : 1, this);
            return camp;
        }

        /// <summary>
        /// The mountains the main base is backed into: per block of cells, one NavMesh obstacle
        /// (nothing walks through) and rock blocks laid over it, turned and alternated so the
        /// ridge does not read as a stamp. The cells are the camp's reserved ground, so nothing
        /// is ever built on them either.
        /// </summary>
        public GameObject SpawnMountains(OrcCamp camp, IReadOnlyList<RectInt> rects)
        {
            var grid = GridManager.Instance;
            var root = new GameObject($"OrcMountains {camp.PortalCell.x},{camp.PortalCell.y}");
            var n = 0;
            foreach (var rect in rects)
            {
                OrcBaseLayout.ToWorld(rect.min, rect.size, camp.Centre, camp.Quarters, out var min, out var size);
                var block = new GameObject("MountainBlock");
                block.transform.SetParent(root.transform, false);
                block.transform.position = grid.GetFootprintCenterWorld(min, size);
                var obstacle = block.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
                obstacle.center = new Vector3(0f, 1.5f, 0f);
                obstacle.size = new Vector3(size.x * grid.CellSize, 3f, size.y * grid.CellSize);
                obstacle.carving = true;
                // the beast's skeleton stands instead of the rock blocks (DressSkeleton, below)
                if (HasSkeleton) continue;
                if (mountainModels == null || mountainModels.Length == 0) continue;
                // rock blocks every three cells across the rect, the outer ones sunk a little
                for (var x = 1.5f; x < size.x; x += 3f)
                {
                    for (var z = 1.5f; z < size.y; z += 3f)
                    {
                        var model = mountainModels[(n * 7 + (int)x + (int)z) % mountainModels.Length];
                        var at = grid.CellToWorld(min) + new Vector3(x * grid.CellSize, grid.GroundHeight, z * grid.CellSize);
                        var rock = Instantiate(model, at, Quaternion.Euler(0f, 90f * ((n * 3 + (int)x) % 4), 0f), root.transform);
                        rock.name = "Mountain";
                        n++;
                    }
                }
            }
            if (HasSkeleton) DressSkeleton(camp, root);
            return root;
        }

        private bool HasSkeleton => ribModels != null && ribModels.Length > 0 && ribModels[0] != null && vertebraModel != null;

        /// <summary>
        /// The main base's heart (the user's concept, 2026-11-06: "C as the heart"): a giant beast's
        /// skeleton where the mountains were -- its ribs rising out of the side blocks and arching
        /// over the base, its fallen spine across the back block, dead trees between the bones --
        /// and its skull lying in front of the outer gate, the way in through its jaws. The blocks'
        /// obstacles stay exactly as they were: the base is as closed as before. Crows circle over it
        /// all; skull stakes line the way in. Everything is a child of `root`, gone with it.
        /// </summary>
        private void DressSkeleton(OrcCamp camp, GameObject root)
        {
            var yaw = camp.Quarters * 90f;
            var applier = MeshMapApplier.Instance;
            GameObject Put(GameObject model, Vector2 local, float turn, float scale, string name)
            {
                if (model == null) return null;
                var at = camp.LocalToWorld(local);
                if (applier != null && applier.TryGetGroundHeight(at, out var h)) at.y = h;
                var go = Instantiate(model, at, Quaternion.Euler(0f, yaw + turn, 0f), root.transform);
                go.name = name;
                if (!Mathf.Approximately(scale, 1f)) go.transform.localScale = Vector3.one * scale;
                return go;
            }
            GameObject Rib(int k) => ribModels[Mathf.Min(k, ribModels.Length - 1)];
            GameObject Tree(int k) => deadTreeModels != null && deadTreeModels.Length > 0 ? deadTreeModels[Mathf.Abs(k) % deadTreeModels.Length] : null;

            // the ribs: their feet in the strips beside the chief's quarter and the sanctum, outside
            // their walls, arching in over them (living base A, 2026-11-06; one snapped each side)
            var ys = new[] { -16f, -11f, -6f, -1f, 4f };
            for (var i = 0; i < ys.Length; i++)
            {
                var size = 1f - 0.02f * Mathf.Abs(ys[i] + 6f);
                Put(Rib(i == 3 ? 1 : 0), new Vector2(-16.5f, ys[i]), 0f, size, "Rib");
                Put(Rib(i == 1 ? 1 : 0), new Vector2(16.5f, ys[i]), 180f, size, "Rib");
            }
            // between the ribs: vertebrae fallen off the spine and dead trees
            for (var y = -17f; y <= 4f; y += 3.8f)
            {
                var k = (int)(y + 20f);
                foreach (var side in new[] { -1f, 1f })
                {
                    Put(vertebraModel, new Vector2(side * 18.2f, y + 1.9f), 37f * k * side, 0.9f, "Vertebra");
                    Put(Tree(k + (side > 0 ? 1 : 0)), new Vector2(side * 15.3f, y + 0.6f), 53f * k, 1f, "DeadTree");
                }
            }
            // the spine: fallen behind the sanctum's back wall, big in the middle, smaller to the ends
            for (var x = -17.5f; x <= 17.6f; x += 2.5f)
            {
                var size = 1.6f - 0.6f * Mathf.Abs(x) / 17.5f;
                Put(vertebraModel, new Vector2(x, 10f), 90f + (x > 0f ? 0f : 180f), size, "Spine");
            }
            for (var x = -16f; x <= 16.1f; x += 4f)
                Put(Tree((int)(x + 40f)), new Vector2(x + 1.2f, 12.5f), 41f * x, 1.1f, "DeadTree");

            // the skull before the outer gate, where the ground in front lets it lie
            var gateLine = camp.Layout.Bounds.yMin;
            if (skullGateModel != null && SkullGroundFree(camp, gateLine))
            {
                Put(skullGateModel, new Vector2(0f, gateLine - 6.5f), 0f, 1f, "SkullGate");
                // its two halves are solid; the way between the jaws is open
                foreach (var side in new[] { -1f, 1f })
                {
                    var half = new GameObject("SkullObstacle");
                    half.transform.SetParent(root.transform, false);
                    half.transform.SetPositionAndRotation(camp.LocalToWorld(new Vector2(side * 3.2f, gateLine - 6.5f)), Quaternion.Euler(0f, yaw, 0f));
                    var obstacle = half.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                    obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
                    obstacle.center = new Vector3(0f, 1.5f, 0f);
                    obstacle.size = new Vector3(3.6f, 3f, 12.5f);
                    obstacle.carving = true;
                }
                // skull stakes along the way in
                var grid = GridManager.Instance;
                for (var k = 0; k < 6; k++)
                {
                    foreach (var side in new[] { -1f, 1f })
                    {
                        var at = new Vector2(side * (3.3f + 0.4f * (k % 2)), gateLine - 14.5f - k * 2.6f);
                        if (applier != null && grid != null && applier.IsWaterCell(grid.WorldToCell(camp.LocalToWorld(at)))) continue;
                        Put(skullStakeModel, at, 70f * k * side, 1.2f, "SkullStake");
                    }
                }
            }

            // crows circling over the skull and over the base
            if (crowModel != null)
            {
                var flock = root.AddComponent<CrowFlock>();
                flock.Initialize(crowModel, camp.LocalToWorld(new Vector2(0f, gateLine - 6.5f)), camp.LocalToWorld(new Vector2(0f, -12f)));
            }
            SceneryShadows.DisableCasting(root);
        }

        /// <summary>Whether the skull's ground in front of the gate (10 x 13 cells) is the camp's to lie on: land, in the grid, nothing standing there. Takes it if so.</summary>
        private static bool SkullGroundFree(OrcCamp camp, int gateLine)
        {
            var grid = GridManager.Instance;
            var applier = MeshMapApplier.Instance;
            if (grid == null) return false;
            OrcBaseLayout.ToWorld(new Vector2Int(-5, gateLine - 13), new Vector2Int(10, 13), camp.Centre, camp.Quarters, out var min, out var size);
            for (var x = 0; x < size.x; x++)
            {
                for (var z = 0; z < size.y; z++)
                {
                    var cell = min + new Vector2Int(x, z);
                    if (!grid.IsWithinBounds(cell, Vector2Int.one) || !grid.IsAreaFree(cell, Vector2Int.one)) return false;
                    if (applier != null && applier.IsWaterCell(cell)) return false;
                }
            }
            grid.SetAreaOccupied(min, size, true);
            return true;
        }

        /// <summary>
        /// Grows every camp still guarding a portal to the level the player's progression calls
        /// for (OrcBaseLayout.LevelFor). Called with the score the raid clock has just read, so it
        /// costs nothing extra. A camp only ever grows, and a grown camp is rebuilt whole.
        /// </summary>
        public void GrowCamps(int score)
        {
            var balance = BalanceConfig.Instance;
            // a copy: growing a camp rebuilds it, and the registry must not change under the loop
            foreach (var camp in new List<OrcCamp>(OrcCamp.All))
            {
                if (camp == null || camp.Cleared || camp.Portal == null) continue;
                var level = OrcBaseLayout.LevelFor(score, camp.Rank, balance.OrcCampLevel2Score, balance.OrcCampLevel3Score,
                    balance.OrcCampLevelScorePerRank, camp.MaxLevel);
                if (level > camp.Level) camp.GrowTo(level, this);
            }
        }

        /// <summary>Gives a portal the health and the look of its camp's level -- a camp that grows is rebuilt whole, its portal too.</summary>
        public void ApplyPortalLevel(OrcPortal portal, int level)
        {
            if (portal == null) return;
            if (level >= OrcBaseLayout.MainLevel)
            {
                // The great portal: one look of its own, never the next wave's source.
                portal.SetLevel(level, BalanceConfig.Instance.MainPortalHealth);
                if (mainPortalModel == null) return;
                var look = Instantiate(mainPortalModel, portal.transform, false);
                look.name = "PortalActive";
                portal.SetLooks(look, null);
                OrcPortalFx.Dress(look, null, true);
                return;
            }
            portal.SetLevel(level, BalanceConfig.Instance.PortalHealthAt(level));
            var active = Pick(portalActiveModels, level);
            var passive = Pick(portalPassiveModels, level);
            if (active == null || passive == null) return;
            var activeLook = Instantiate(active, portal.transform, false);
            var passiveLook = Instantiate(passive, portal.transform, false);
            activeLook.name = "PortalActive";
            passiveLook.name = "PortalPassive";
            portal.SetLooks(activeLook, passiveLook);
            OrcPortalFx.Dress(activeLook, passiveLook, false);
        }

        /// <summary>One building of a camp's plan on its plot. The camp has already checked the plot is its own ground.</summary>
        public OrcCampBuilding SpawnCampBuilding(OrcCamp camp, CampPiece piece, Vector2Int cell, Vector2Int footprint)
        {
            EnsureMaterials();
            var grid = GridManager.Instance;
            var baseYaw = camp.Quarters * 90f;
            var yaw = baseYaw;
            GameObject model;
            if (piece.Kind == OrcCampBuildingKind.Palisade)
            {
                // The palisade autotiles like the town's fence: which of the four pieces, turned
                // how far, follows from which neighbours are wall (gates count).
                FenceShape.Resolve(camp.IsWallCell(cell + Vector2Int.up), camp.IsWallCell(cell + Vector2Int.right),
                    camp.IsWallCell(cell + Vector2Int.down), camp.IsWallCell(cell + Vector2Int.left),
                    out var variant, out var steps);
                if (piece.Level >= OrcBaseLayout.MainLevel)
                {
                    model = mainWallModels != null && mainWallModels.Length == 4 ? mainWallModels[(int)variant] : null;
                }
                else
                {
                    model = palisadeModels != null && palisadeModels.Length >= piece.Level * 4
                        ? palisadeModels[(piece.Level - 1) * 4 + (int)variant]
                        : null;
                }
                yaw = steps * 90f;
            }
            else
            {
                model = ModelOf(piece.Kind, piece.Level);
                // A gate's model runs its wall along its own Z with the outside on -X; in the camp
                // the wall runs across the front, the outside towards the town.
                if (piece.Kind == OrcCampBuildingKind.Gate || piece.Kind == OrcCampBuildingKind.GateTower) yaw = baseYaw - 90f;
            }

            var root = new GameObject($"OrcCamp{piece.Kind} {cell.x},{cell.y}");
            root.transform.SetPositionAndRotation(grid.GetFootprintCenterWorld(cell, footprint), Quaternion.Euler(0f, yaw, 0f));
            var building = root.AddComponent<OrcCampBuilding>();
            building.Initialize(piece.Kind, piece.Level, camp, cell, footprint, RuinOf(piece.Kind, piece.Level));

            if (model != null)
            {
                Instantiate(model, root.transform, false).name = "Model";
            }
            else
            {
                AddPortalPart(root.transform, new Vector3(0f, 0.6f, 0f), new Vector3(footprint.x * 0.8f, 1.2f, footprint.y * 0.8f));
            }

            // Tappable, so a group can be sent at it (ArmyOrderInput), and a trigger so it never
            // becomes a wall units wedge against; the NavMesh carve is what routes them round it.
            // The palisade's carve is its camp's, one obstacle per straight run (OrcCamp).
            var height = ClickHeight(piece.Kind);
            var local = Quaternion.Inverse(root.transform.rotation) * new Vector3(footprint.x, 0f, footprint.y);
            var size = new Vector3(Mathf.Abs(local.x), 0f, Mathf.Abs(local.z));
            var click = root.AddComponent<BoxCollider>();
            click.isTrigger = true;
            click.center = new Vector3(0f, height / 2f, 0f);
            click.size = new Vector3(size.x * 0.95f, height, size.z * 0.95f);
            if (piece.Kind != OrcCampBuildingKind.Palisade)
            {
                var obstacle = root.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
                obstacle.size = new Vector3(size.x * 0.9f, 2f, size.z * 0.9f);
                obstacle.center = new Vector3(0f, 1f, 0f);
                obstacle.carving = true;
            }
            return building;
        }

        // ---------------------------------------------------------------------------------------
        // The Invasion mode's round camps (InvasionOrcCamp): the same buildings, laid round two
        // circles rather than on a grid plan, belonging to no OrcCamp.

        /// <summary>
        /// One building of a round camp, standing at `at` turned `yaw` degrees, `size` metres across
        /// (x along its own X, y along its Z). A Palisade is one section of the ring wall. Breakable
        /// like any camp building, leaving its ruin; carved out of the NavMesh so it is a real wall.
        /// </summary>
        public OrcCampBuilding SpawnLooseCampBuilding(OrcCampBuildingKind kind, int level, Vector3 at, float yaw, Vector2 size)
        {
            EnsureMaterials();
            var wall = kind == OrcCampBuildingKind.Palisade;
            var model = wall ? ringWallModel : ModelOf(kind, level);
            var ruin = wall ? ringWallRuinModel : RuinOf(kind, level);
            var grid = GridManager.Instance;
            var cell = grid != null ? grid.WorldToCell(at) : Vector2Int.zero;
            var footprint = new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(size.x)), Mathf.Max(1, Mathf.RoundToInt(size.y)));

            var root = new GameObject($"OrcRingCamp{kind}");
            root.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            var building = root.AddComponent<OrcCampBuilding>();
            building.Initialize(kind, level, null, cell, footprint, ruin);
            if (model != null) Instantiate(model, root.transform, false).name = "Model";
            else AddPortalPart(root.transform, new Vector3(0f, 0.6f, 0f), new Vector3(size.x * 0.8f, 1.2f, size.y * 0.8f));

            var height = ClickHeight(kind);
            var click = root.AddComponent<BoxCollider>();
            click.isTrigger = true;
            click.center = new Vector3(0f, height / 2f, 0f);
            click.size = new Vector3(size.x * 0.95f, height, size.y * 0.95f);
            var obstacle = root.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
            obstacle.size = new Vector3(size.x * (wall ? 1f : 0.9f), 2f, size.y * 0.9f);
            obstacle.center = new Vector3(0f, 1f, 0f);
            obstacle.carving = true;
            return building;
        }

        /// <summary>A round camp's open gateway: scenery, nothing to break, nothing in the way.</summary>
        public GameObject SpawnRingGate(Vector3 at, float yaw)
        {
            var gate = ringGateModel != null ? Instantiate(ringGateModel, at, Quaternion.Euler(0f, yaw, 0f)) : new GameObject();
            gate.name = "OrcRingGate";
            gate.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            return gate;
        }

        /// <summary>A round camp's portal: the level-3 portal's burning look and nothing else -- it cannot be closed (the user's rule).</summary>
        public GameObject SpawnPortalLook(Vector3 at, float yaw)
        {
            var model = Pick(portalActiveModels, 3);
            var portal = model != null ? Instantiate(model, at, Quaternion.Euler(0f, yaw, 0f)) : new GameObject();
            portal.name = "OrcRingPortal";
            portal.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            return portal;
        }

        /// <summary>A round camp's guard: holds `post`, fights soldiers within `radius` of it.</summary>
        public OrcUnit SpawnRingGuard(Vector3 post, int level, OrcKind kind, float radius)
        {
            EnsureMaterials();
            var unit = SpawnOrc(post, level, kind, scatter: false);
            unit.SetGuardPost(null, post, radius);
            return unit;
        }

        /// <summary>One guard of a camp: a `slot.Kind` orc of `slot.Level`, standing at `spawnAt` and holding `post`.</summary>
        public OrcUnit SpawnGuard(OrcCamp camp, GuardSlot slot, Vector3 post, Vector3? spawnAt = null)
        {
            EnsureMaterials();
            var unit = SpawnOrc(spawnAt ?? post, slot.Level, slot.Kind, scatter: false);
            unit.SetGuardPost(camp, post);
            return unit;
        }

        /// <summary>
        /// The camp round a closed portal is plundered: the player takes the loot (more for a
        /// bigger camp) and the orcs need time to regroup -- the next raid is put back by the
        /// breather. Both are the user's answer (2026-10-16) to what closing a portal buys.
        /// </summary>
        private void PlunderCamp(OrcPortal portal)
        {
            var camp = OrcCamp.Of(portal);
            if (camp == null || camp.Cleared) return;
            var level = Mathf.Max(1, camp.Level);
            camp.MarkCleared();

            var balance = BalanceConfig.Instance;
            var coins = balance.OrcCampLootCoins * level;
            var wood = balance.OrcCampLootWood * level;
            var stone = balance.OrcCampLootStone * level;
            var resources = CityBuilder.Resources.ResourceManager.Instance;
            if (resources != null)
            {
                resources.Add(CityBuilder.Resources.ResourceType.Coins, coins);
                resources.Add(CityBuilder.Resources.ResourceType.Wood, wood);
                resources.Add(CityBuilder.Resources.ResourceType.Stone, stone);
            }
            _raidTimer += balance.OrcCampBreatherSeconds;
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Format("#log_camp_plundered", coins, wood, stone),
                EventSeverity.Good, EventGlyph.Portal, null, portal.transform.position));
        }

        /// <summary>The closed portal's ruin, on the plot the portal stood on. Scenery only: no collider, nothing to attack.</summary>
        private void SpawnRuin(Vector2Int cell, Vector3 position, Quaternion rotation, bool main = false)
        {
            var model = main ? mainPortalRuinModel : portalClosedModel;
            if (model == null) return;
            var ruin = Instantiate(model, position, rotation);
            ruin.name = $"OrcPortalRuin {cell.x},{cell.y}";
        }
    }
}
