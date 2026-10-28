using System.Collections.Generic;
using System.IO;
using CityBuilder.Buildings;
using CityBuilder.CameraControl;
using CityBuilder.Cheats;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Resources;
using CityBuilder.Saving;
using CityBuilder.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// Procedural building prefabs (used where no FBX exists: water buildings, fallbacks), the fence
    /// and its pieces, and CombineParts, which bakes cube parts into one mesh.
    /// </summary>
    public static partial class SetupProject
    {
        /// <summary>
        /// A fence segment. Unlike every other building, its prefab carries several models -- the
        /// straight run, the corner, the T and the crossroads -- and FenceAppearance shows whichever one matches the
        /// neighbouring segments (see FenceShape). Nothing about which model is visible is decided
        /// here or saved: it is derived from the cells around it every time the line changes, so a
        /// destroyed segment turns its neighbours back into dead ends for free.
        ///
        /// Two deliberate differences from CreateBuildingData: the collider spans the WHOLE cell
        /// with no BuildingInset, because segments have to touch to read as one fence, and each
        /// model hangs inside a wrapper so FenceAppearance can turn the wrapper without disturbing
        /// whatever local rotation the FBX brought with it.
        /// </summary>
        private static BuildingData CreateFenceBuildingData(string id, Material trimMaterial)
        {
            var root = new GameObject(id);
            root.AddComponent<BuildingInstance>();
            var appearance = root.AddComponent<FenceAppearance>();

            // One wood material for both models, made here rather than inside each of them.
            var woodMaterial = CreateLitMaterial("Building_Fence_Wood", new Color(0.5f, 0.36f, 0.22f));
            var straight = CreateFenceModel("Straight", root.transform, trimMaterial, woodMaterial, isCorner: false, height: out var modelHeight);
            var corner = CreateFenceModel("Corner", root.transform, trimMaterial, woodMaterial, isCorner: true, height: out _);
            // The junction pieces exist only as Blender models; without them a T or a crossroads
            // shows the straight run (FenceShape.ResolveWithoutJunctions).
            var tee = CreateFenceJunction("T", root.transform);
            var cross = CreateFenceJunction("Cross", root.transform);

            // Each variant is baked on its own. The PAIR has to stay two objects for
            // FenceAppearance to switch and turn between them, but the ten posts, caps and rails
            // inside each one are decoration nothing addresses -- and a fence is placed in runs of
            // dozens, so ten draws per segment is one of the largest bills on the map.
            CombineParts(straight, $"{id}_Straight");
            CombineParts(corner, $"{id}_Corner");
            if (tee != null) CombineParts(tee, $"{id}_T");
            if (cross != null) CombineParts(cross, $"{id}_Cross");

            appearance.SetModels(straight, corner, tee, cross);

            // From level 2 the fence is stone (FortifiedFence1, the user 2026-10-23) and levels 3-5
            // grow it (Fence1-lvl3..5, the user 2026-10-28), each shape baked on its own like the
            // wood and hidden until an upgrade asks for it.
            var shapes = new[] { "Straight", "Corner", "T", "Cross" };
            var levels = new GameObject[4 * FenceAppearance.Shapes];
            for (var level = 2; level <= 5; level++)
            for (var i = 0; i < shapes.Length; i++)
            {
                var piece = level == 2
                    ? CreateFenceJunction(shapes[i], root.transform, "FortifiedFence1", "Stone" + shapes[i])
                    : CreateFenceJunction(shapes[i], root.transform, "Fence1", $"Lvl{level}{shapes[i]}", level);
                if (piece == null) continue;
                CombineParts(piece, level == 2 ? $"{id}_Stone{shapes[i]}" : $"{id}_Lvl{level}{shapes[i]}");
                piece.SetActive(false);
                levels[(level - 2) * FenceAppearance.Shapes + i] = piece;
            }
            appearance.SetLevelModels(levels);
            // The straight run is what a lone segment shows; FenceAppearance swaps them the moment
            // the piece learns about its neighbours.
            if (corner != null) corner.SetActive(false);
            if (tee != null) tee.SetActive(false);
            if (cross != null) cross.SetActive(false);

            var height = modelHeight > 0.01f ? modelHeight : 1.2f;
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(CellSize, height, CellSize);
            collider.center = new Vector3(0f, height * 0.5f, 0f);

            // The two variants were baked individually above; merging at the ROOT would fuse them
            // into one object and lose the switch entirely.
            var prefab = SavePrefab(root, id, combineParts: false);

            var data = ScriptableObject.CreateInstance<BuildingData>();
            data.buildingName = id;
            data.prefab = prefab;
            data.footprintSize = Vector2Int.one;
            ApplyBalance(data, Balance(id));
            data.connectsToFences = true;
            // Laid out tile by tile like a road, so the hotbar selection survives each placement.
            data.keepSelectedAfterPlacement = true;

            Directory.CreateDirectory(BuildingDataFolder);
            var dataPath = $"{BuildingDataFolder}/{id}.asset";
            DeleteIfExists(dataPath);
            AssetDatabase.CreateAsset(data, dataPath);
            return data;
        }

        /// <summary>
        /// A junction piece (`Fence1-lvl1-T.fbx`, `-Cross.fbx`) in its wrapper, or null when the model
        /// is not there -- unlike the straight run and the corner, these have no built fallback. The T
        /// is exported joining north, south and east (FenceShape's base orientation).
        /// </summary>
        private static GameObject CreateFenceJunction(string wrapperName, Transform parent, string model = "Fence1", string childName = null, int level = 1)
        {
            var authored = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsBuildingsFolder}/{model}-lvl{level}-{wrapperName}.fbx");
            if (authored == null) return null;

            var wrapper = new GameObject(childName ?? wrapperName);
            wrapper.transform.SetParent(parent, false);
            var piece = Object.Instantiate(authored, Vector3.zero, authored.transform.rotation, wrapper.transform);
            piece.name = authored.name;
            FitModelToGround(piece, $"Fence {wrapperName}");
            return wrapper;
        }

        /// <summary>
        /// One fence segment, built rather than imported: posts and two rails, laid along Z for a
        /// straight run and turned into the south-east elbow for a corner. FenceAppearance shows
        /// whichever of the two matches the neighbours and rotates the wrapper to face them, so the
        /// geometry below only ever has to describe ONE orientation of each -- see FenceShape, and
        /// FenceShapeTests, which fails deliberately if either is drawn facing another way.
        /// </summary>
        private static GameObject CreateFenceModel(string wrapperName, Transform parent, Material trimMaterial, Material woodMaterial, bool isCorner, out float height)
        {
            const float postSide = 0.14f;
            const float postHeight = 0.95f;
            const float railThickness = 0.07f;
            var half = CellSize * 0.5f;

            var wrapper = new GameObject(wrapperName);
            wrapper.transform.SetParent(parent, false);

            // The Blender-made piece wins when it is there (`Fence1-lvl1-Straight.fbx`,
            // `-Corner.fbx`), exported in exactly this orientation: straight along Z, corner
            // joining south and east. The T and Cross pieces are CreateFenceJunction's.
            var authored = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsBuildingsFolder}/Fence1-lvl1-{wrapperName}.fbx");
            if (authored != null)
            {
                var piece = Object.Instantiate(authored, Vector3.zero, authored.transform.rotation, wrapper.transform);
                piece.name = authored.name;
                height = FitModelToGround(piece, $"Fence {wrapperName}").y;
                return wrapper;
            }

            void Post(string partName, float x, float z)
            {
                AddCubePart(wrapper.transform, partName, new Vector3(x, postHeight * 0.5f, z), new Vector3(postSide, postHeight, postSide), woodMaterial);
                AddCubePart(wrapper.transform, partName + "Cap", new Vector3(x, postHeight + 0.03f, z), new Vector3(postSide * 1.3f, 0.06f, postSide * 1.3f), trimMaterial);
            }

            // A rail from the centre of the cell out to one edge, so a straight run is two of them
            // back to back and a corner is two at right angles. Length half a cell either way, which
            // is what makes neighbouring segments meet exactly on the cell border.
            void Rail(string partName, float y, Vector3 direction)
            {
                var centre = direction * (half * 0.5f);
                var size = direction.z != 0f
                    ? new Vector3(railThickness, railThickness * 1.6f, half)
                    : new Vector3(half, railThickness * 1.6f, railThickness);
                AddCubePart(wrapper.transform, partName, new Vector3(centre.x, y, centre.z), size, woodMaterial);
            }

            Post("PostCentre", 0f, 0f);

            if (isCorner)
            {
                // South and east, matching the corner the fence network expects.
                Post("PostSouth", 0f, -half);
                Post("PostEast", half, 0f);
                Rail("RailSouthHigh", postHeight * 0.74f, Vector3.back);
                Rail("RailSouthLow", postHeight * 0.36f, Vector3.back);
                Rail("RailEastHigh", postHeight * 0.74f, Vector3.right);
                Rail("RailEastLow", postHeight * 0.36f, Vector3.right);
            }
            else
            {
                Post("PostSouth", 0f, -half);
                Post("PostNorth", 0f, half);
                Rail("RailSouthHigh", postHeight * 0.74f, Vector3.back);
                Rail("RailSouthLow", postHeight * 0.36f, Vector3.back);
                Rail("RailNorthHigh", postHeight * 0.74f, Vector3.forward);
                Rail("RailNorthLow", postHeight * 0.36f, Vector3.forward);
            }

            height = postHeight + 0.06f;
            return wrapper;
        }

        /// <summary>
        /// Procedurally assembles a dwelling/workshop from cube primitives: a stepped plinth,
        /// corner posts, a framed door with a step, a window count derived from wall width
        /// (placed on both the front and back faces), a banded three-tier roof with a ridge cap,
        /// and an optional chimney with a cap. The part count and placement all come from
        /// footprint/height, not hand-picked positions, so the same generator produces a small
        /// House and a wider Lumberjack alike.
        /// </summary>
        private static GameObject CreateHutPrefab(string name, Vector2Int footprint, float height, Color wallColor, Color roofColor, int maxWorkers, bool hasChimney, Material trimMaterial, Material doorMaterial, Material windowMaterial, BuildingMotif motif = BuildingMotif.None)
        {
            var sizeX = footprint.x * CellSize - BuildingInset;
            var sizeZ = footprint.y * CellSize - BuildingInset;

            var root = new GameObject(name);
            root.AddComponent<BuildingInstance>();
            if (maxWorkers > 0) root.AddComponent<ProductionBuilding>();

            var wallMaterial = CreateLitMaterial($"Building_{name}_Walls", wallColor);
            var roofMaterial = CreateLitMaterial($"Building_{name}_Roof", roofColor);
            var roofShadeMaterial = CreateLitMaterial($"Building_{name}_RoofShade", Shade(roofColor, 0.8f));

            var y = AddSteppedPlinth(root.transform, sizeX, sizeZ, height * 0.1f, trimMaterial, wallMaterial);

            var wallHeight = height * 0.42f;
            AddCubePart(root.transform, "Walls", new Vector3(0f, y + wallHeight * 0.5f, 0f), new Vector3(sizeX, wallHeight, sizeZ), wallMaterial);
            AddCornerPosts(root.transform, sizeX, sizeZ, y, wallHeight, trimMaterial);

            var doorWidth = Mathf.Clamp(sizeX * 0.24f, 0.36f, 0.6f);
            var doorHeight = wallHeight * 0.62f;
            AddFramedOpening(root.transform, "Door", 0f, y + doorHeight * 0.5f, -sizeZ * 0.5f, -1f, doorWidth, doorHeight, trimMaterial, doorMaterial);
            AddCubePart(root.transform, "Doorstep", new Vector3(0f, y - 0.03f, -sizeZ * 0.5f - 0.22f), new Vector3(doorWidth + 0.3f, 0.06f, 0.3f), trimMaterial);

            // Window count derived from wall width — wider buildings procedurally get more,
            // mirrored on the front and back faces.
            var windowCount = Mathf.Clamp(Mathf.FloorToInt(sizeX / 1.3f), 0, 2);
            var windowY = y + wallHeight * 0.7f;
            for (var i = 0; i < windowCount; i++)
            {
                var t = (i + 1f) / (windowCount + 1f);
                var wx = Mathf.Lerp(-sizeX * 0.5f + 0.35f, sizeX * 0.5f - 0.35f, t);
                if (Mathf.Abs(wx) < doorWidth * 0.7f) continue; // skip over the door
                AddFramedOpening(root.transform, $"WindowFront{i}", wx, windowY, -sizeZ * 0.5f, -1f, 0.22f, 0.22f, trimMaterial, windowMaterial);
                AddFramedOpening(root.transform, $"WindowBack{i}", wx, windowY, sizeZ * 0.5f, 1f, 0.22f, 0.22f, trimMaterial, windowMaterial);
            }

            y += wallHeight;

            AddCubePart(root.transform, "Fascia", new Vector3(0f, y + 0.03f, 0f), new Vector3(sizeX * 1.05f, 0.06f, sizeZ * 1.05f), trimMaterial);
            y += 0.06f;

            y = AddShingleRoof(root.transform, sizeX, sizeZ, y, height - y, 3, roofMaterial, roofShadeMaterial, trimMaterial);

            if (hasChimney)
            {
                var chimneyHeight = height * 0.2f;
                var chimneySize = Mathf.Min(sizeX, sizeZ) * 0.15f;
                var chimneyBase = Mathf.Max(0f, y - height * 0.3f);
                AddCubePart(root.transform, "Chimney", new Vector3(sizeX * 0.28f, chimneyBase + chimneyHeight * 0.5f, sizeZ * 0.28f), new Vector3(chimneySize, chimneyHeight, chimneySize), wallMaterial);
                AddCubePart(root.transform, "ChimneyCap", new Vector3(sizeX * 0.28f, chimneyBase + chimneyHeight + 0.03f, sizeZ * 0.28f), new Vector3(chimneySize * 1.4f, 0.06f, chimneySize * 1.4f), trimMaterial);
                y = Mathf.Max(y, chimneyBase + chimneyHeight + 0.06f);
            }

            y = AddMotif(root.transform, motif, name, sizeX, sizeZ, y, height, wallMaterial, roofMaterial, trimMaterial);

            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(sizeX * 1.1f, y, sizeZ * 1.1f);
            collider.center = new Vector3(0f, y * 0.5f, 0f);

            return SavePrefab(root, name);
        }

        /// <summary>
        /// Procedurally assembles a wall segment or tower: a stepped plinth, wall block(s) with
        /// arrow slits, and a crenellation ring generated by looping around the footprint's
        /// perimeter (a merlon count derived from size, not four hand-typed corner positions).
        /// Towers get two stacked, slightly inset wall tiers plus a lookout cap instead of one
        /// solid block, reading as noticeably taller and more fortified than a plain wall.
        /// </summary>
        private static GameObject CreateFortificationPrefab(string name, Vector2Int footprint, float height, Color wallColor, Color roofColor, Material trimMaterial, bool isTower, BuildingMotif motif = BuildingMotif.None)
        {
            var sizeX = footprint.x * CellSize - BuildingInset;
            var sizeZ = footprint.y * CellSize - BuildingInset;

            var root = new GameObject(name);
            root.AddComponent<BuildingInstance>();

            var wallMaterial = CreateLitMaterial($"Building_{name}_Walls", wallColor);
            var shadeMaterial = CreateLitMaterial($"Building_{name}_Shade", Shade(wallColor, 0.85f));
            var roofMaterial = CreateLitMaterial($"Building_{name}_Roof", roofColor);

            var y = AddSteppedPlinth(root.transform, sizeX, sizeZ, height * 0.08f, trimMaterial, wallMaterial);

            if (isTower)
            {
                var tier1Height = height * 0.34f;
                AddCubePart(root.transform, "WallsLower", new Vector3(0f, y + tier1Height * 0.5f, 0f), new Vector3(sizeX, tier1Height, sizeZ), wallMaterial);
                AddArrowSlits(root.transform, "SlitLower", sizeX, sizeZ, y + tier1Height * 0.6f, trimMaterial);
                y += tier1Height;

                const float inset = 0.9f;
                var tier2Height = height * 0.28f;
                AddCubePart(root.transform, "WallsUpper", new Vector3(0f, y + tier2Height * 0.5f, 0f), new Vector3(sizeX * inset, tier2Height, sizeZ * inset), shadeMaterial);
                AddArrowSlits(root.transform, "SlitUpper", sizeX * inset, sizeZ * inset, y + tier2Height * 0.6f, trimMaterial);
                y += tier2Height;

                var merlonHeight = height * 0.12f;
                AddCrenellationRing(root.transform, "Merlon", sizeX * inset, sizeZ * inset, y + merlonHeight * 0.5f, merlonHeight, trimMaterial);
                y += merlonHeight;

                var capHeight = height * 0.14f;
                var capSize = Mathf.Min(sizeX, sizeZ) * 0.5f;
                AddCubePart(root.transform, "Lookout", new Vector3(0f, y + capHeight * 0.5f, 0f), new Vector3(capSize, capHeight, capSize), wallMaterial);
                y += capHeight;
            }
            else
            {
                var wallHeight = height * 0.66f;
                AddCubePart(root.transform, "Walls", new Vector3(0f, y + wallHeight * 0.5f, 0f), new Vector3(sizeX, wallHeight, sizeZ), wallMaterial);
                AddArrowSlits(root.transform, "Slit", sizeX, sizeZ, y + wallHeight * 0.62f, trimMaterial);
                y += wallHeight;

                var merlonHeight = height * 0.15f;
                AddCrenellationRing(root.transform, "Merlon", sizeX, sizeZ, y + merlonHeight * 0.5f, merlonHeight, trimMaterial);
                y += merlonHeight;
            }

            y = AddMotif(root.transform, motif, name, sizeX, sizeZ, y, height, wallMaterial, roofMaterial, trimMaterial);

            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(sizeX * 1.1f, y, sizeZ * 1.1f);
            collider.center = new Vector3(0f, y * 0.5f, 0f);

            return SavePrefab(root, name);
        }

        /// <summary>
        /// The shell for everything that is not a building with walls: a well, a flag, a garden, a
        /// gate you walk through, an arena with an open floor. It lays a paved base the size of the
        /// footprint and leaves the rest to the motif, which is the whole object here rather than
        /// an ornament on top of one.
        ///
        /// The base is deliberately thin and NOT a trigger, unlike a road: these are things a
        /// player taps and a citizen walks around, not surfaces they walk over.
        /// </summary>
        private static GameObject CreatePlotPrefab(string name, Vector2Int footprint, float height, Color baseColor, Color accentColor, int maxWorkers, Material trimMaterial, BuildingMotif motif)
        {
            var sizeX = footprint.x * CellSize - BuildingInset;
            var sizeZ = footprint.y * CellSize - BuildingInset;

            var root = new GameObject(name);
            root.AddComponent<BuildingInstance>();
            if (maxWorkers > 0) root.AddComponent<ProductionBuilding>();

            var baseMaterial = CreateLitMaterial($"Building_{name}_Base", baseColor);
            var accentMaterial = CreateLitMaterial($"Building_{name}_Accent", accentColor);

            const float paving = 0.08f;
            AddCubePart(root.transform, "Paving", new Vector3(0f, paving * 0.5f, 0f), new Vector3(sizeX, paving, sizeZ), baseMaterial);
            AddCubePart(root.transform, "PavingEdge", new Vector3(0f, paving * 0.5f + 0.01f, 0f), new Vector3(sizeX * 0.86f, paving, sizeZ * 0.86f), accentMaterial);

            var y = AddMotif(root.transform, motif, name, sizeX, sizeZ, paving, height, baseMaterial, accentMaterial, trimMaterial);
            // Never flatter than a step: a collider a few centimetres tall is a building nobody can
            // tap, and half of these are one cell across to begin with.
            y = Mathf.Max(y, 0.4f);

            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(sizeX * 1.05f, y, sizeZ * 1.05f);
            collider.center = new Vector3(0f, y * 0.5f, 0f);

            return SavePrefab(root, name);
        }

        /// <summary>
        /// A flat, thin paved tile with a dashed centerline stripe -- players lay these one at a
        /// time (footprint 1x1) to build a continuous road; see RoadNetwork/CitizenAgent for how
        /// citizens detect and speed up on them. Its collider is a trigger, unlike every other
        /// building's solid BoxCollider: CharacterController ignores triggers for movement
        /// blocking, so citizens walk straight over a road (staying grounded on the terrain
        /// collider beneath) instead of treating it as an obstacle, while the trigger still lets
        /// BuildingPlacer/BuildingSelector's raycasts detect it for ground-picking and clicking.
        /// </summary>
        private static GameObject CreateRoadPrefab(string name, Color roadColor, Color stripeColor)
        {
            var sizeX = CellSize - 0.04f;
            var sizeZ = CellSize - 0.04f;
            const float thickness = 0.05f;

            var root = new GameObject(name);
            root.AddComponent<BuildingInstance>();

            var roadMaterial = CreateLitMaterial($"Building_{name}_Surface", roadColor);
            var stripeMaterial = CreateLitMaterial($"Building_{name}_Stripe", stripeColor);

            AddCubePart(root.transform, "Surface", new Vector3(0f, thickness * 0.5f, 0f), new Vector3(sizeX, thickness, sizeZ), roadMaterial);
            AddCubePart(root.transform, "StripeNorth", new Vector3(0f, thickness + 0.01f, sizeZ * 0.27f), new Vector3(sizeX * 0.12f, 0.02f, sizeZ * 0.34f), stripeMaterial);
            AddCubePart(root.transform, "StripeSouth", new Vector3(0f, thickness + 0.01f, -sizeZ * 0.27f), new Vector3(sizeX * 0.12f, 0.02f, sizeZ * 0.34f), stripeMaterial);

            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(sizeX, thickness, sizeZ);
            collider.center = new Vector3(0f, thickness * 0.5f, 0f);
            collider.isTrigger = true;

            return SavePrefab(root, name);
        }

        /// <summary>
        /// A wooden deck on short corner posts (a "built on stilts over the water" read), used for
        /// every isWaterCategory production building -- see CreateWaterBuildingData. addWheel adds
        /// a mill paddle wheel (a cross of two thin blades, not a true circle -- matching the
        /// no-circles style everywhere else); addCrates adds a couple of cargo props for a dock.
        /// </summary>
        private static GameObject CreateWaterBuildingPrefab(string name, Vector2Int footprint, Color deckColor, Color accentColor, bool addWheel, bool addCrates)
        {
            var sizeX = footprint.x * CellSize - BuildingInset;
            var sizeZ = footprint.y * CellSize - BuildingInset;
            const float deckThickness = 0.14f;
            const float postDepth = 0.4f;

            var root = new GameObject(name);
            root.AddComponent<BuildingInstance>();

            var deckMaterial = CreateLitMaterial($"Building_{name}_Deck", deckColor);
            var accentMaterial = CreateLitMaterial($"Building_{name}_Accent", accentColor);

            AddCubePart(root.transform, "Deck", new Vector3(0f, deckThickness * 0.5f, 0f), new Vector3(sizeX, deckThickness, sizeZ), deckMaterial);

            var halfX = sizeX * 0.5f - 0.15f;
            var halfZ = sizeZ * 0.5f - 0.15f;
            var postIndex = 0;
            for (var sx = -1; sx <= 1; sx += 2)
            {
                for (var sz = -1; sz <= 1; sz += 2)
                {
                    AddCubePart(root.transform, $"Post{postIndex}", new Vector3(sx * halfX, -postDepth * 0.5f, sz * halfZ), new Vector3(0.12f, postDepth, 0.12f), accentMaterial);
                    postIndex++;
                }
            }

            if (addWheel)
            {
                var postX = sizeX * 0.5f + 0.12f;
                AddCubePart(root.transform, "WheelPost", new Vector3(postX, 0.8f, 0f), new Vector3(0.14f, 1.6f, 0.14f), accentMaterial);
                AddCubePart(root.transform, "BladeVertical", new Vector3(postX, 0.9f, 0f), new Vector3(0.08f, 1.3f, 0.16f), deckMaterial);
                AddCubePart(root.transform, "BladeHorizontal", new Vector3(postX, 0.9f, 0f), new Vector3(0.08f, 0.16f, 1.3f), deckMaterial);
            }

            if (addCrates)
            {
                AddCubePart(root.transform, "CrateA", new Vector3(sizeX * 0.22f, deckThickness + 0.15f, sizeZ * 0.2f), new Vector3(0.3f, 0.3f, 0.3f), accentMaterial);
                AddCubePart(root.transform, "CrateB", new Vector3(-sizeX * 0.18f, deckThickness + 0.12f, -sizeZ * 0.25f), new Vector3(0.24f, 0.24f, 0.24f), accentMaterial);
            }

            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(sizeX, deckThickness + postDepth, sizeZ);
            collider.center = new Vector3(0f, (deckThickness - postDepth) * 0.5f, 0f);

            return SavePrefab(root, name);
        }

        /// <summary>
        /// A water-only production building (BuildingData.isWaterCategory) -- placeable exclusively
        /// within a mesh map's water-placement zone, and never touching dry Ground (see
        /// BuildingPlacer.CanPlaceSelectedBuilding). Mirrors CreateBuildingData's data-assignment
        /// tail but drives CreateWaterBuildingPrefab instead of the land-building style switch.
        /// </summary>
        private static BuildingData CreateWaterBuildingData(
            string id, Vector2Int footprint, Color deckColor, Color accentColor,
            bool addWheel = false, bool addCrates = false)
        {
            var prefab = CreateWaterBuildingPrefab(id, footprint, deckColor, accentColor, addWheel, addCrates);

            var data = ScriptableObject.CreateInstance<BuildingData>();
            data.buildingName = id;
            data.prefab = prefab;
            data.footprintSize = footprint;
            ApplyBalance(data, Balance(id));
            data.isWaterCategory = true;

            Directory.CreateDirectory(BuildingDataFolder);
            var dataPath = $"{BuildingDataFolder}/{id}.asset";
            DeleteIfExists(dataPath);
            AssetDatabase.CreateAsset(data, dataPath);
            return data;
        }

        /// <summary>Two-tier foundation (a wide dark base course, then a narrower top course).</summary>
        private static float AddSteppedPlinth(Transform parent, float sizeX, float sizeZ, float totalHeight, Material baseMaterial, Material topMaterial)
        {
            var baseHeight = totalHeight * 0.55f;
            var topHeight = totalHeight * 0.45f;
            AddCubePart(parent, "PlinthBase", new Vector3(0f, baseHeight * 0.5f, 0f), new Vector3(sizeX * 1.1f, baseHeight, sizeZ * 1.1f), baseMaterial);
            AddCubePart(parent, "PlinthTop", new Vector3(0f, baseHeight + topHeight * 0.5f, 0f), new Vector3(sizeX * 1.03f, topHeight, sizeZ * 1.03f), topMaterial);
            return totalHeight;
        }

        /// <summary>Four thin trim-colored pillars at the wall corners, looped over corner sign combinations.</summary>
        private static void AddCornerPosts(Transform parent, float sizeX, float sizeZ, float y, float postHeight, Material material)
        {
            const float thickness = 0.12f;
            var halfX = sizeX * 0.5f - thickness * 0.5f;
            var halfZ = sizeZ * 0.5f - thickness * 0.5f;
            var index = 0;
            for (var sx = -1; sx <= 1; sx += 2)
            {
                for (var sz = -1; sz <= 1; sz += 2)
                {
                    AddCubePart(parent, $"CornerPost{index}", new Vector3(sx * halfX, y + postHeight * 0.5f, sz * halfZ), new Vector3(thickness, postHeight, thickness), material);
                    index++;
                }
            }
        }

        /// <summary>A door or window as two layers — a slightly larger dark frame behind a smaller inset pane — instead of one flat cube.</summary>
        private static void AddFramedOpening(Transform parent, string name, float x, float y, float zFace, float outwardSign, float width, float height, Material frameMaterial, Material paneMaterial)
        {
            AddCubePart(parent, $"{name}Frame", new Vector3(x, y, zFace + outwardSign * 0.01f), new Vector3(width + 0.08f, height + 0.08f, 0.04f), frameMaterial);
            AddCubePart(parent, $"{name}Pane", new Vector3(x, y, zFace + outwardSign * 0.05f), new Vector3(width, height, 0.05f), paneMaterial ?? frameMaterial);
        }

        /// <summary>Thin vertical arrow-slit accents on the front face, count derived from wall width.</summary>
        private static void AddArrowSlits(Transform parent, string prefix, float sizeX, float sizeZ, float y, Material material)
        {
            var count = Mathf.Clamp(Mathf.RoundToInt(sizeX / 1.3f), 1, 2);
            for (var i = 0; i < count; i++)
            {
                var t = (i + 1f) / (count + 1f);
                var x = Mathf.Lerp(-sizeX * 0.5f + 0.3f, sizeX * 0.5f - 0.3f, t);
                AddCubePart(parent, $"{prefix}{i}", new Vector3(x, y, -sizeZ * 0.5f - 0.02f), new Vector3(0.08f, 0.32f, 0.04f), material);
            }
        }

        /// <summary>
        /// A crenellation (battlement) ring: merlons spaced along all four footprint edges,
        /// with the count per edge derived from size — not four hand-typed corner positions.
        /// </summary>
        private static void AddCrenellationRing(Transform parent, string prefix, float sizeX, float sizeZ, float y, float merlonHeight, Material material)
        {
            const float thickness = 0.22f;
            var halfX = sizeX * 0.5f - thickness * 0.5f;
            var halfZ = sizeZ * 0.5f - thickness * 0.5f;
            var perSide = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(sizeX, sizeZ) / 1.4f), 2, 4);

            var index = 0;
            for (var i = 0; i < perSide; i++)
            {
                var t = perSide == 1 ? 0f : Mathf.Lerp(-0.82f, 0.82f, (float)i / (perSide - 1));
                AddCubePart(parent, $"{prefix}{index++}", new Vector3(t * halfX, y, -halfZ), new Vector3(thickness, merlonHeight, thickness), material);
                AddCubePart(parent, $"{prefix}{index++}", new Vector3(t * halfX, y, halfZ), new Vector3(thickness, merlonHeight, thickness), material);
                AddCubePart(parent, $"{prefix}{index++}", new Vector3(-halfX, y, t * halfZ), new Vector3(thickness, merlonHeight, thickness), material);
                AddCubePart(parent, $"{prefix}{index++}", new Vector3(halfX, y, t * halfZ), new Vector3(thickness, merlonHeight, thickness), material);
            }
        }

        /// <summary>
        /// N stepped, shrinking roof tiers with alternating shade (a cheap banded/"shingle"
        /// look) topped with a thin ridge cap, returning the y after the ridge.
        /// </summary>
        private static float AddShingleRoof(Transform parent, float sizeX, float sizeZ, float yStart, float heightBudget, int tiers, Material roofMaterial, Material roofShadeMaterial, Material trimMaterial)
        {
            var y = yStart;
            var scale = 1f;
            for (var tier = 0; tier < tiers; tier++)
            {
                var tierHeight = (heightBudget / tiers) * (1f - tier * 0.1f);
                scale *= Mathf.Lerp(0.92f, 0.6f, tiers <= 1 ? 0f : (float)tier / (tiers - 1));
                var material = tier % 2 == 0 ? roofMaterial : roofShadeMaterial;
                AddCubePart(parent, $"RoofTier{tier}", new Vector3(0f, y + tierHeight * 0.5f, 0f), new Vector3(sizeX * scale, tierHeight, sizeZ * scale), material);
                y += tierHeight;
            }

            AddCubePart(parent, "RoofRidge", new Vector3(0f, y + 0.03f, 0f), new Vector3(sizeX * scale * 0.9f, 0.06f, 0.12f), trimMaterial);
            y += 0.06f;

            return y;
        }

        private static Color Shade(Color color, float factor)
        {
            return new Color(Mathf.Clamp01(color.r * factor), Mathf.Clamp01(color.g * factor), Mathf.Clamp01(color.b * factor), color.a);
        }

        private static GameObject AddCubePart(Transform parent, string partName, Vector3 localPosition, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = partName;
            Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>
        /// Bakes a procedurally assembled building's cube and sphere parts into ONE mesh on the
        /// root, with one submesh per material, and deletes the part objects.
        ///
        /// **This is the biggest single draw-call saving in the game, and it was found by
        /// measuring rather than by reading the code.** RenderCostTests counted the live scene on
        /// 2026-08-31: thirty placed buildings added 488 forward draws and 488 shadow draws, about
        /// 16 of each per building, against a note in the performance baseline that guessed "4-5
        /// parts per building". A hut is a stepped plinth, four corner posts, a framed door, a
        /// doorstep, up to four framed windows, a fascia, three roof tiers, a ridge, a chimney and
        /// its cap -- twenty to thirty separate renderers, every one of them its own draw call on
        /// a phone, per building, twice over once shadows are counted.
        ///
        /// They can all be merged because none of them is ever addressed on its own: the parts
        /// carry no colliders (AddCubePart strips them), nothing looks a part up by name, and
        /// every system that touches a building's appearance -- the ghost preview, the fire tint,
        /// the health bar -- already works through GetComponentsInChildren&lt;Renderer&gt; and an
        /// ARRAY of materials, so one renderer with six submeshes is the same to them as six
        /// renderers.
        ///
        /// What it does NOT do is reduce the material count: the submeshes are still drawn once
        /// each. A building of six materials is six draws instead of twenty-five, and the way to
        /// get from six to one is fewer materials, not more merging.
        ///
        /// The merged mesh has to be saved as its own asset. A prefab does not embed mesh data --
        /// saving one that points at a mesh living only in memory writes a broken reference, and
        /// the building comes back invisible.
        /// </summary>
        private static void CombineParts(GameObject root, string name)
        {
            if (root == null) return;

            // Only children that are pure decoration are swallowed. A child carrying anything
            // else -- a collider, a script, a light -- is something the game addresses on its own,
            // and merging its mesh away would leave that behind pointing at nothing. Nothing
            // generated today is in that position; the check is here so that adding such a child
            // later degrades to "this one is not merged" instead of to a silent breakage.
            var mergeable = new List<Transform>();
            for (var i = 0; i < root.transform.childCount; i++)
            {
                var child = root.transform.GetChild(i);
                if (IsPureMeshPart(child)) mergeable.Add(child);
            }

            var filters = new List<MeshFilter>();
            foreach (var child in mergeable) filters.AddRange(child.GetComponentsInChildren<MeshFilter>(true));

            // One part is already one draw; merging it would only copy the mesh into a second
            // asset. This is the normal answer for an authored single-mesh FBX.
            if (filters.Count <= 1) return;

            // Grouped by material, and the ORDER those materials were first seen is kept: it
            // becomes the submesh order, which has to line up with the renderer's material array.
            var order = new List<Material>();
            var byMaterial = new Dictionary<Material, List<CombineInstance>>();
            var worldToLocal = root.transform.worldToLocalMatrix;
            var vertexCount = 0;

            foreach (var filter in filters)
            {
                var mesh = filter.sharedMesh;
                var renderer = filter.GetComponent<MeshRenderer>();
                if (mesh == null || renderer == null) continue;

                var material = renderer.sharedMaterial;
                if (material == null) continue;

                if (!byMaterial.TryGetValue(material, out var group))
                {
                    group = new List<CombineInstance>();
                    byMaterial[material] = group;
                    order.Add(material);
                }

                // Into the ROOT's space, not the world's: the prefab is instantiated anywhere on
                // the map, and its parts are positioned relative to it.
                var toRoot = worldToLocal * filter.transform.localToWorldMatrix;
                for (var sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    group.Add(new CombineInstance { mesh = mesh, subMeshIndex = sub, transform = toRoot });
                }
                vertexCount += mesh.vertexCount;
            }

            if (order.Count == 0) return;

            // Two passes, because one CombineMeshes call cannot do both jobs. The first merges
            // each material's parts into a single sub-mesh-less blob (mergeSubMeshes: true); the
            // second lays those blobs side by side as separate submeshes (mergeSubMeshes: false),
            // which is what lets one renderer keep one material per submesh.
            var perMaterial = new CombineInstance[order.Count];
            for (var i = 0; i < order.Count; i++)
            {
                var merged = new Mesh { name = $"{name}_part{i}" };
                if (vertexCount > 65000) merged.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                merged.CombineMeshes(byMaterial[order[i]].ToArray(), true, true);
                perMaterial[i] = new CombineInstance { mesh = merged, subMeshIndex = 0, transform = Matrix4x4.identity };
            }

            var combined = new Mesh { name = name };
            if (vertexCount > 65000) combined.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            combined.CombineMeshes(perMaterial, false, false);
            combined.RecalculateBounds();

            // The per-material blobs were scaffolding. Left alive they leak into the editor's
            // memory for the rest of the run, one set per building.
            foreach (var instance in perMaterial) Object.DestroyImmediate(instance.mesh);

            // Normals are deliberately NOT recalculated. The parts are boxes whose normals are
            // already correct under the axis-aligned scales AddCubePart applies, and
            // RecalculateNormals would average the ones that now share a vertex position across
            // two different parts -- turning a crisp 90-degree corner into a smeared one.

            Directory.CreateDirectory(GeneratedMeshFolder);
            var meshPath = $"{GeneratedMeshFolder}/{name}.asset";
            DeleteIfExists(meshPath);
            AssetDatabase.CreateAsset(combined, meshPath);

            foreach (var child in mergeable) Object.DestroyImmediate(child.gameObject);

            root.AddComponent<MeshFilter>().sharedMesh = combined;
            root.AddComponent<MeshRenderer>().sharedMaterials = order.ToArray();
        }

        /// <summary>
        /// True when this subtree is nothing but geometry -- every object in it carries only a
        /// Transform, a MeshFilter and a MeshRenderer. Those are the parts CombineParts is allowed
        /// to bake away, because deleting them deletes nothing the game refers to.
        /// </summary>
        private static bool IsPureMeshPart(Transform child)
        {
            foreach (var component in child.GetComponentsInChildren<Component>(true))
            {
                if (component is Transform || component is MeshFilter || component is MeshRenderer) continue;
                return false;
            }
            return true;
        }
    }
}
