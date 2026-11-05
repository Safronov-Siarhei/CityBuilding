using System;
using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// What a building of the player's leaves when it is destroyed in the sandbox (the user,
    /// 2026-11-05; card Design/features/ruins.md): its own model, burnt and broken off (look A,
    /// «обгоревший остов», shader CityBuilder/BuildingRuin), with rubble and ash on its plot. It keeps
    /// the plot taken; a tap opens its card (RuinCard): restore it for ruin_restore_cost_fraction of
    /// what it was worth, as a construction ruin_restore_time_fraction as long, or clear it away.
    ///
    /// Every building leaves one -- roads, fences, walls too -- but the Town Hall (its loss is the
    /// defeat) and the moat (water); nothing of the bot kingdom's and nothing in a run (Invasion).
    ///
    /// The pieces are the dying building's own meshes as they stood (its level, a fence's shape),
    /// each kept with its path in the prefab, so a save can find the same meshes again in the
    /// prefab without anything of the building's coming to life (GameSaveController).
    /// </summary>
    public class BuildingRuin : MonoBehaviour
    {
        public const string ShaderName = "CityBuilder/BuildingRuin";
        /// <summary>Assets/_Project/Resources/BuildingRuin.mat, made by SetupProject.EnsurePaletteMaterial.</summary>
        public const string MaterialResource = "BuildingRuin";
        private const string LiveShaderName = "CityBuilder/BuildingDetail";

        private static readonly Color Stone = new Color(0.56f, 0.53f, 0.49f, 1f);
        private static readonly Color Ash = new Color(0.13f, 0.12f, 0.11f, 1f);

        /// <summary>One mesh of the building as it stood: where in the prefab, and where under the ruin.</summary>
        [Serializable]
        public class Piece
        {
            public string path;
            public Vector3 position;
            public Quaternion rotation = Quaternion.identity;
            public Vector3 scale = Vector3.one;
        }

        public static readonly List<BuildingRuin> All = new List<BuildingRuin>();

        public BuildingData Data { get; private set; }
        public Vector2Int OriginCell { get; private set; }
        public int RotationSteps { get; private set; }
        public int Level { get; private set; } = 1;
        public List<Piece> Pieces { get; private set; } = new List<Piece>();
        /// <summary>The cells the ruin holds: the building's, but those it shared (a road under a gate, a mine's vein, a moat under a bridge).</summary>
        public List<Vector2Int> Cells { get; private set; } = new List<Vector2Int>();

        private static Material _baseMaterial;
        private readonly List<Material> _materials = new List<Material>(3);

        private void OnEnable() => All.Add(this);

        private void OnDisable() => All.Remove(this);

        private void OnDestroy()
        {
            foreach (var material in _materials)
            {
                if (material != null) Destroy(material);
            }
            if (_rubbleMesh != null) Destroy(_rubbleMesh);
        }

        /// <summary>Whether this building leaves ruins when it falls (see the class summary).</summary>
        public static bool LeavesRuin(BuildingInstance building) =>
            building != null && !building.IsBot && building.Data != null && building.Data.prefab != null
            && building.Data.buildingName != BuildingIds.TownHall && !building.Data.isMoat
            && !Invasion.InvasionRules.Active;

        /// <summary>The ruin of a building that has just fallen, put where it stood: before it is destroyed, while its meshes are still there to copy.</summary>
        public static BuildingRuin LeaveOf(BuildingInstance building, List<Vector2Int> cells)
        {
            var root = building.transform;
            var pieces = new List<Piece>();
            var meshes = new List<Mesh>();
            foreach (var filter in building.GetComponentsInChildren<MeshFilter>())
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || !renderer.enabled || filter.sharedMesh == null) continue;
                var material = renderer.sharedMaterial;
                // the building's own body only: not its health bar, marker, fire or scaffolding
                if (material == null || material.shader == null || material.shader.name != LiveShaderName) continue;
                var t = filter.transform;
                pieces.Add(new Piece
                {
                    path = PathOf(root, t),
                    position = root.InverseTransformPoint(t.position),
                    rotation = Quaternion.Inverse(root.rotation) * t.rotation,
                    scale = Divide(t.lossyScale, root.lossyScale)
                });
                meshes.Add(filter.sharedMesh);
            }
            var ruin = Make(building.Data, building.OriginCell, building.RotationSteps, building.Level, root.position, root.rotation, building.gameObject.layer);
            ruin.Pieces = pieces;
            ruin.Build(meshes);
            ruin.Hold(cells);
            return ruin;
        }

        /// <summary>A ruin from a save: its meshes found again in the building's prefab by their paths, nothing of the prefab coming to life.</summary>
        public static BuildingRuin Restore(BuildingData data, Vector2Int origin, int rotationSteps, int level, List<Piece> pieces, List<Vector2Int> cells)
        {
            if (data == null || data.prefab == null || GridManager.Instance == null) return null;
            var footprint = rotationSteps % 2 == 0 ? data.footprintSize : new Vector2Int(data.footprintSize.y, data.footprintSize.x);
            var center = GridManager.Instance.GetFootprintCenterWorld(origin, footprint);
            var meshes = new List<Mesh>();
            var kept = new List<Piece>();
            var prefab = data.prefab.transform;
            foreach (var piece in pieces ?? new List<Piece>())
            {
                var t = string.IsNullOrEmpty(piece.path) ? prefab : prefab.Find(piece.path);
                var filter = t != null ? t.GetComponent<MeshFilter>() : null;
                if (filter == null || filter.sharedMesh == null) continue;
                kept.Add(piece);
                meshes.Add(filter.sharedMesh);
            }
            var ruin = Make(data, origin, rotationSteps, level, center, Quaternion.Euler(0f, rotationSteps * 90f, 0f), data.prefab.layer);
            ruin.Pieces = kept;
            ruin.Build(meshes);
            ruin.Hold(cells);
            return ruin;
        }

        private static BuildingRuin Make(BuildingData data, Vector2Int origin, int rotationSteps, int level, Vector3 position, Quaternion rotation, int layer)
        {
            var go = new GameObject("Ruin-" + data.buildingName);
            go.layer = layer;
            go.transform.SetPositionAndRotation(position, rotation);
            var ruin = go.AddComponent<BuildingRuin>();
            ruin.Data = data;
            ruin.OriginCell = origin;
            ruin.RotationSteps = rotationSteps;
            ruin.Level = Mathf.Max(1, level);
            return ruin;
        }

        private void Hold(List<Vector2Int> cells)
        {
            Cells = cells ?? new List<Vector2Int>();
            var grid = GridManager.Instance;
            if (grid == null) return;
            foreach (var cell in Cells) grid.SetAreaOccupied(cell, Vector2Int.one, true);
        }

        private void Release()
        {
            var grid = GridManager.Instance;
            if (grid == null) return;
            foreach (var cell in Cells) grid.SetAreaOccupied(cell, Vector2Int.one, false);
        }

        // ---- the look ----

        private void Build(List<Mesh> meshes)
        {
            if (_baseMaterial == null) _baseMaterial = UnityEngine.Resources.Load<Material>(MaterialResource);
            var cellSize = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;
            var foot = RotationSteps % 2 == 0 ? Data.footprintSize : new Vector2Int(Data.footprintSize.y, Data.footprintSize.x);
            var size = new Vector2(foot.x * cellSize, foot.y * cellSize);
            var rng = new System.Random(OriginCell.x * 7919 + OriginCell.y * 104729 + StableHash(Data.buildingName));

            var renderers = new List<Renderer>(meshes.Count);
            var shell = Instance(Color.white, null);
            for (var i = 0; i < meshes.Count; i++)
            {
                var piece = Pieces[i];
                var go = new GameObject("Piece", typeof(MeshFilter), typeof(MeshRenderer));
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                go.transform.localPosition = piece.position;
                go.transform.localRotation = piece.rotation;
                go.transform.localScale = piece.scale;
                go.GetComponent<MeshFilter>().sharedMesh = meshes[i];
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = shell;
                renderers.Add(renderer);
            }

            // the break: a third or so of the way up, its teeth a share of a cell
            var ground = transform.position.y;
            var top = ground + 0.3f * cellSize;
            foreach (var r in renderers) top = Mathf.Max(top, r.bounds.max.y);
            var height = top - ground;
            var flat = height < 0.35f * cellSize;   // a road, a low fence: burnt, not broken
            if (shell != null)
            {
                shell.SetFloat("_RuinCut", flat ? top + 1f : ground + Mathf.Max(0.22f * cellSize, height * (0.3f + 0.12f * (float)rng.NextDouble())));
                shell.SetFloat("_RuinJag", 0.14f * cellSize);
                shell.SetFloat("_RuinSeed", (float)rng.NextDouble() * 100f);
                shell.SetFloat("_Char", 0.8f);
            }

            // rubble and ash over the plot (none on a road: it is barely more than paint)
            if (!Data.isRoad) AddRubble(size, cellSize, flat, rng);

            // a box to tap
            var box = gameObject.AddComponent<BoxCollider>();
            var boxHeight = flat ? Mathf.Max(0.3f * cellSize, height) : Mathf.Max(0.4f * cellSize, height * 0.45f);
            box.size = new Vector3(Mathf.Max(0.5f, size.x * 0.95f), boxHeight, Mathf.Max(0.5f, size.y * 0.95f));
            // the box in the ruin's own axes: the plot's X/Z are the unrotated footprint's
            if (RotationSteps % 2 == 1) box.size = new Vector3(box.size.z, box.size.y, box.size.x);
            box.center = new Vector3(0f, boxHeight * 0.5f, 0f);
        }

        private Material Instance(Color tint, Texture texture)
        {
            if (_baseMaterial == null) return null;
            var material = new Material(_baseMaterial) { name = "Ruin" };
            if (texture != null) material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", tint);
            _materials.Add(material);
            return material;
        }

        private void AddRubble(Vector2 size, float cellSize, bool flat, System.Random rng)
        {
            var stone = Instance(Stone, Texture2D.whiteTexture);
            var ash = Instance(Ash, Texture2D.whiteTexture);
            if (stone == null) return;
            stone.SetFloat("_Char", 0.5f);
            stone.SetFloat("_RuinSeed", (float)rng.NextDouble() * 100f);
            ash.SetFloat("_Char", 0f);

            // in world-aligned plot axes; the ruin's own rotation is undone below
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var stones = new List<int>();
            var ashes = new List<int>();
            var undo = Quaternion.Inverse(transform.rotation);
            float Gauss() => (float)(rng.NextDouble() + rng.NextDouble() + rng.NextDouble() - 1.5) / 1.5f;

            var area = size.x * size.y / (cellSize * cellSize);
            var count = Mathf.Clamp(Mathf.RoundToInt(area * (flat ? 3f : 7f)), 3, 40);
            for (var i = 0; i < count; i++)
            {
                var x = Mathf.Clamp(Gauss() * size.x * 0.32f, -size.x * 0.45f, size.x * 0.45f);
                var z = Mathf.Clamp(Gauss() * size.y * 0.32f, -size.y * 0.45f, size.y * 0.45f);
                var r = Mathf.Lerp(0.06f, 0.16f, (float)rng.NextDouble()) * cellSize;
                var beam = i % 4 == 3;
                var scale = beam ? new Vector3(r * 0.5f, r * 0.45f, r * 3.2f) : new Vector3(r, r * Mathf.Lerp(0.6f, 1.1f, (float)rng.NextDouble()), r * Mathf.Lerp(0.8f, 1.3f, (float)rng.NextDouble()));
                var turn = Quaternion.Euler(Mathf.Lerp(-25f, 25f, (float)rng.NextDouble()), (float)rng.NextDouble() * 360f, Mathf.Lerp(-25f, 25f, (float)rng.NextDouble()));
                Box(vertices, normals, stones, undo * new Vector3(x, scale.y * 0.3f, z), undo * turn, scale);
            }
            // ash on the ground
            Box(vertices, normals, ashes, new Vector3(0f, 0.008f * cellSize, 0f), Quaternion.identity,
                new Vector3(size.x * 0.84f, 0.012f * cellSize, size.y * 0.84f), undo);

            var mesh = new Mesh { name = "RuinRubble" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(stones, 0);
            mesh.SetTriangles(ashes, 1);
            mesh.RecalculateBounds();
            var go = new GameObject("Rubble", typeof(MeshFilter), typeof(MeshRenderer));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterials = new[] { stone, ash };
            _rubbleMesh = mesh;
        }

        private Mesh _rubbleMesh;

        private static readonly Vector3[] CubeNormals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };

        /// <summary>A box of `size` turned by `turn` at `at` (all in the ruin's space; `outer` turns the size's axes for the ash plate).</summary>
        private static void Box(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, Vector3 at, Quaternion turn, Vector3 size, Quaternion? outer = null)
        {
            var o = outer ?? Quaternion.identity;
            foreach (var n in CubeNormals)
            {
                var u = new Vector3(n.y, n.z, n.x);
                var v = Vector3.Cross(n, u);
                var start = vertices.Count;
                for (var k = 0; k < 4; k++)
                {
                    var su = (k == 1 || k == 2) ? 0.5f : -0.5f;
                    var sv = k >= 2 ? 0.5f : -0.5f;
                    var local = n * 0.5f + u * su + v * sv;
                    vertices.Add(at + o * (turn * Vector3.Scale(local, size)));
                    normals.Add(o * (turn * n));
                }
                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 3);
            }
        }

        // ---- the card's three choices ----

        /// <summary>
        /// What restoring costs: what the building was worth at its level -- its price and every
        /// upgrade to it, coins too -- times ruin_restore_cost_fraction. Rounded up.
        /// </summary>
        public List<ResourceAmount> RestoreCost()
        {
            var cost = new List<ResourceAmount>(3);
            void Add(List<ResourceAmount> amounts)
            {
                if (amounts == null) return;
                foreach (var amount in amounts)
                {
                    if (amount.amount <= 0) continue;
                    var known = cost.Find(c => c.type == amount.type);
                    if (known != null) known.amount += amount.amount;
                    else cost.Add(new ResourceAmount { type = amount.type, amount = amount.amount });
                }
            }
            Add(BuildingPlacer.CostOf(Data));
            for (var l = 2; l <= Level; l++) Add(BuildingInstance.UpgradePriceOf(Data.UpgradeCostTo(l)));
            var share = BalanceConfig.Instance != null ? BalanceConfig.Instance.RuinRestoreCostFraction : 0.5f;
            foreach (var amount in cost) amount.amount = Mathf.Max(1, Mathf.CeilToInt(amount.amount * share));
            return cost;
        }

        /// <summary>Pays and puts the building back up at its level, as a construction (ConstructionSite) a share as long as building it took.</summary>
        public BuildingInstance TryRestore()
        {
            var cost = RestoreCost();
            if (ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(cost))
            {
                EventLogManager.Instance?.Log(Localization.Format("#log_ruin_no_resources", Data.LocalizedName));
                return null;
            }
            Release();
            var building = Rebuild(Data, OriginCell, RotationSteps, Level);
            if (building != null)
            {
                var seconds = 0f;
                for (var l = 1; l <= Level; l++) seconds += Data.BuildSeconds(l);
                var share = BalanceConfig.Instance != null ? BalanceConfig.Instance.RuinRestoreTimeFraction : 0.5f;
                building.BuildOnTimer(seconds * share);
                EventLogManager.Instance?.Log(Localization.Format("#log_ruin_restored", Data.LocalizedName));
            }
            Destroy(gameObject);
            return building;
        }

        /// <summary>Clears the ruin away and frees its plot. Nothing comes back: there is nothing left to give.</summary>
        public void Clear()
        {
            Release();
            EventLogManager.Instance?.Log(Localization.Format("#log_ruin_cleared", Data.LocalizedName));
            Destroy(gameObject);
        }

        /// <summary>The building put back on its plot, as a save puts one (GameSaveController).</summary>
        private static BuildingInstance Rebuild(BuildingData data, Vector2Int cell, int rotationSteps, int level)
        {
            var grid = GridManager.Instance;
            if (grid == null || data.prefab == null) return null;
            var footprint = rotationSteps % 2 == 0 ? data.footprintSize : new Vector2Int(data.footprintSize.y, data.footprintSize.x);
            var center = grid.GetFootprintCenterWorld(cell, footprint);
            var instance = Instantiate(data.prefab, center, Quaternion.Euler(0f, rotationSteps * 90f, 0f));
            var building = instance.GetComponent<BuildingInstance>();
            if (building == null) building = instance.AddComponent<BuildingInstance>();
            building.Initialize(data, cell, rotationSteps);
            building.SetLevel(level);
            grid.SetAreaOccupied(cell, footprint, true);
            if (data.isRoad && RoadNetwork.Instance != null)
            {
                for (var x = 0; x < footprint.x; x++)
                {
                    for (var z = 0; z < footprint.y; z++) RoadNetwork.Instance.RegisterRoad(cell + new Vector2Int(x, z));
                }
            }
            return building;
        }

        // the same on every device and every run (string.GetHashCode need not be)
        private static int StableHash(string text)
        {
            var hash = 17;
            foreach (var c in text) hash = hash * 31 + c;
            return hash;
        }

        private static string PathOf(Transform root, Transform t)
        {
            if (t == root) return string.Empty;
            var path = t.name;
            for (var p = t.parent; p != null && p != root; p = p.parent) path = p.name + "/" + path;
            return path;
        }

        private static Vector3 Divide(Vector3 a, Vector3 b) =>
            new Vector3(b.x != 0f ? a.x / b.x : a.x, b.y != 0f ? a.y / b.y : a.y, b.z != 0f ? a.z / b.z : a.z);
    }
}
