using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// The dead and the Кладбище (the user, 2026-11-13; sandbox only, as the Infirmary):
    ///
    /// - Every villager's death (illness, hunger, an orc...) leaves a skeleton lying where he fell, a
    ///   skull over it (Drop). Each one on an island adds remains_sickness_per_body to its illness
    ///   chance (SicknessManager).
    /// - A Кладбище on the island takes them by itself, no worker: a body vanishes and the nearest
    ///   cemetery with room counts one more grave (capacity graves 20/30/40/50/60 by level). Its 20
    ///   grave places fill as it fills -- past level 1 a grave stands for a share of the dead.
    /// - Bodies over remains_notice_share of the island's residents and no room: the notice
    ///   «Постройте кладбище» (once in a while per island).
    /// A cemetery is pulled down only for crystals (BuildingInfoPanelController): otherwise it would be
    /// a free way to clear the dead.
    /// </summary>
    public class Remains : MonoBehaviour
    {
        public static Remains Instance { get; private set; }

        [SerializeField] private GameObject remainsModel;
        [SerializeField] private GameObject[] graveModels = new GameObject[0];
        [SerializeField] private Material skullMaterial;

        /// <summary>The cemetery's 20 grave places in its model's own metres (Tools/blender/cemetery1.py places(): front row first).</summary>
        private static readonly float[] PlaceX = { -0.74f, -0.42f, 0.42f, 0.74f };
        private static readonly float[] PlaceY = { -0.52f, -0.22f, 0.08f, 0.38f, 0.68f };
        public const int Places = 20;

        private const float CheckSeconds = 0.5f;
        private const float NoticeEverySeconds = 30f;
        private const float MarkSize = 0.6f;

        public class Body
        {
            public Vector3 At;
            public int Island;
            public float Yaw;
            public GameObject Figure;
        }

        private readonly List<Body> _bodies = new List<Body>();
        private readonly Dictionary<BuildingInstance, int> _graves = new Dictionary<BuildingInstance, int>();
        private readonly Dictionary<BuildingInstance, List<GameObject>> _shown = new Dictionary<BuildingInstance, List<GameObject>>();
        private readonly Dictionary<int, float> _told = new Dictionary<int, float>();
        private readonly List<BuildingInstance> _cemeteries = new List<BuildingInstance>();
        private readonly List<BuildingInstance> _gone = new List<BuildingInstance>();
        private float _checkIn;
        private static Mesh _quad;

        public static bool Active => Infirmary.Active;

        public IReadOnlyList<Body> Bodies => _bodies;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static bool IsCemetery(BuildingInstance b) => b != null && b.Data != null && b.Data.buildingName == BuildingIds.Cemetery && !b.IsBot;

        public static int CapacityOf(BuildingInstance cemetery) => cemetery != null && cemetery.Data != null ? cemetery.Data.LevelStats(cemetery.Level).graves : 0;

        public int GravesIn(BuildingInstance cemetery) => cemetery != null && _graves.TryGetValue(cemetery, out var n) ? n : 0;

        /// <summary>Bodies lying on this island.</summary>
        public int BodiesOn(int island)
        {
            var n = 0;
            foreach (var b in _bodies) if (HousePeople.Same(b.Island, island)) n++;
            return n;
        }

        /// <summary>The card's line: graves taken of the capacity.</summary>
        public static string CardLine(BuildingInstance building)
        {
            if (!Active || Instance == null || !IsCemetery(building)) return null;
            return Localization.Format("#cemetery_card", Instance.GravesIn(building), CapacityOf(building));
        }

        /// <summary>A villager died here: he lies where he fell (sandbox only).</summary>
        public void Drop(Vector3 at)
        {
            if (!Active) return;
            var body = new Body { At = at, Island = HousePeople.IslandOf(at), Yaw = Random.Range(0f, 360f) };
            _bodies.Add(body);
            Show(body);
            _checkIn = 0f;
        }

        // ---------------------------------------------------------------- the frame

        private void Update()
        {
            _checkIn -= Time.deltaTime;
            if (_checkIn > 0f) return;
            _checkIn = CheckSeconds;
            if (!Active)
            {
                if (_bodies.Count > 0) Clear();
                return;
            }

            _cemeteries.Clear();
            foreach (var b in BuildingInstance.All)
                if (IsCemetery(b) && b.IsOperational && b.CurrentHealth > 0) _cemeteries.Add(b);
            Forget();

            // the bodies go to the nearest cemetery on their island with room
            for (var i = _bodies.Count - 1; i >= 0; i--)
            {
                var body = _bodies[i];
                var cemetery = NearestWithRoom(body);
                if (cemetery == null) continue;
                _graves[cemetery] = GravesIn(cemetery) + 1;
                if (body.Figure != null) Destroy(body.Figure);
                _bodies.RemoveAt(i);
                ShowGraves(cemetery);
            }

            TellTooMany();
        }

        private BuildingInstance NearestWithRoom(Body body)
        {
            BuildingInstance best = null;
            var bestDistance = float.MaxValue;
            foreach (var c in _cemeteries)
            {
                if (!HousePeople.Same(HousePeople.IslandOf(c), body.Island)) continue;
                if (GravesIn(c) >= CapacityOf(c)) continue;
                var d = (c.transform.position - body.At).sqrMagnitude;
                if (d < bestDistance) { bestDistance = d; best = c; }
            }
            return best;
        }

        /// <summary>A cemetery pulled down takes its graves with it (they are its children).</summary>
        private void Forget()
        {
            _gone.Clear();
            foreach (var pair in _graves) if (pair.Key == null) _gone.Add(pair.Key);
            foreach (var key in _gone)
            {
                _graves.Remove(key);
                _shown.Remove(key);
            }
        }

        private void TellTooMany()
        {
            var homes = HousePeople.Instance;
            var log = EventLogManager.Instance;
            if (homes == null || log == null || _bodies.Count == 0) return;
            var share = BalanceConfig.Instance.RemainsNoticeShare;
            foreach (var body in _bodies)
            {
                var island = body.Island;
                if (_told.TryGetValue(island, out var last) && Time.time - last < NoticeEverySeconds) continue;
                var residents = homes.CitizensOn(island);
                if (BodiesOn(island) <= share * Mathf.Max(1, residents)) continue;
                _told[island] = Time.time;
                log.Post(GameEvent.At(Localization.Get("#log_remains_build_cemetery"), EventSeverity.Warning, EventGlyph.Skull,
                    BuildingIds.Cemetery, body.At));
            }
        }

        // ---------------------------------------------------------------- looks

        private void Show(Body body)
        {
            // the skeleton (Remains1); with no model yet, the skull alone marks the spot
            var go = remainsModel != null ? Instantiate(remainsModel, body.At, Quaternion.Euler(0f, body.Yaw, 0f), transform)
                : new GameObject("Remains");
            if (remainsModel == null)
            {
                go.transform.SetParent(transform, false);
                go.transform.position = body.At;
            }
            go.name = "Remains";
            body.Figure = go;
            if (skullMaterial == null) return;
            var mark = new GameObject("SkullMark", typeof(MeshFilter), typeof(MeshRenderer));
            mark.transform.SetParent(go.transform, false);
            mark.transform.position = body.At + Vector3.up * 0.7f;
            mark.GetComponent<MeshFilter>().sharedMesh = Quad();
            var renderer = mark.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = skullMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            block.SetFloat("_Size", MarkSize);
            renderer.SetPropertyBlock(block);
        }

        /// <summary>The cemetery's 20 places filled to its share of the dead (a grave a death at level 1).</summary>
        private void ShowGraves(BuildingInstance cemetery)
        {
            if (cemetery == null || graveModels.Length == 0) return;
            var capacity = Mathf.Max(1, CapacityOf(cemetery));
            var want = Mathf.Clamp(Mathf.CeilToInt(Places * GravesIn(cemetery) / (float)capacity), 0, Places);
            if (!_shown.TryGetValue(cemetery, out var graves)) _shown[cemetery] = graves = new List<GameObject>();
            while (graves.Count > want)
            {
                if (graves[graves.Count - 1] != null) Destroy(graves[graves.Count - 1]);
                graves.RemoveAt(graves.Count - 1);
            }
            if (graves.Count >= want) return;
            var grid = GridManager.Instance;
            var cell = grid != null ? grid.CellSize : 1f;
            var centre = grid != null ? grid.GetFootprintCenterWorld(cemetery.OriginCell, cemetery.RotatedFootprint()) : cemetery.transform.position;
            centre.y = cemetery.transform.position.y + 0.02f * cell;
            var turn = cemetery.transform.rotation;
            while (graves.Count < want)
            {
                var i = graves.Count;
                var local = new Vector3(PlaceX[i % PlaceX.Length], 0f, PlaceY[i / PlaceX.Length]) * cell;
                var model = graveModels[(i * 7 + 3) % graveModels.Length];
                var grave = model != null ? Instantiate(model, centre + turn * local, turn, cemetery.transform) : new GameObject("Grave");
                grave.name = "Grave";
                graves.Add(grave);
            }
        }

        private static Mesh Quad()
        {
            if (_quad != null) return _quad;
            var h = 0.5f;
            _quad = new Mesh
            {
                name = "SkullMarkQuad",
                vertices = new[] { new Vector3(-h, -h, 0f), new Vector3(h, -h, 0f), new Vector3(h, h, 0f), new Vector3(-h, h, 0f) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) },
                triangles = new[] { 0, 2, 1, 0, 3, 2 },
            };
            _quad.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
            return _quad;
        }

        private void Clear()
        {
            foreach (var b in _bodies) if (b.Figure != null) Destroy(b.Figure);
            _bodies.Clear();
        }

        // ---------------------------------------------------------------- save

        /// <summary>A save's dead and graves: bodies where they lay, each cemetery's count (after the buildings).</summary>
        public void RestoreFromSave(List<Vector3> bodies, List<KeyValuePair<BuildingInstance, int>> graves)
        {
            Clear();
            if (bodies != null)
                foreach (var at in bodies) Drop(at);
            if (graves == null) return;
            foreach (var pair in graves)
            {
                if (!IsCemetery(pair.Key)) continue;
                _graves[pair.Key] = Mathf.Max(0, pair.Value);
                ShowGraves(pair.Key);
            }
        }

        /// <summary>Every cemetery with its count, for the save.</summary>
        public void Graves(List<KeyValuePair<BuildingInstance, int>> into)
        {
            into.Clear();
            foreach (var pair in _graves) if (pair.Key != null) into.Add(pair);
        }
    }
}
