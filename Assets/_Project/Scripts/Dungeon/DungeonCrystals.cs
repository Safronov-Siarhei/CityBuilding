using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Maps;
using UnityEngine;

namespace CityBuilder.Dungeon
{
    /// <summary>
    /// The upgrade crystals lying below (the user, 2026-10-31: they FLOAT in the air, so the player
    /// sees at once they are a reward; each one is guarded -- DungeonMonsters puts a band by every
    /// spot).
    /// - dungeon_crystals_per_room at every "reward" spot (the crystal rooms at the dead ends' far
    ///   ends), dungeon_crystals_lone at every "crystal" spot (a lone crystal in an empty dead end).
    /// - A hero within dungeon_crystal_pick_reach cells takes it: straight into the Laboratory's
    ///   wallet (Wallet.UpgradeCrystals, global), with a line in the feed.
    /// - It must be a SPECIAL thing (the user, 2026-11-01: «яркие, светящиеся, сочные, слегка
    ///   покачиваться ... платный контент»): the gem in its own glowing shader (Shaders/Crystal), a
    ///   breathing halo with rising sparkles turned to the camera (CrystalHalo), a glow on the floor
    ///   under it (OreGlow); it bobs, sways a little and turns slowly; taken, it flares up and melts.
    /// - A crystal taken never comes back: its id (dungeon, level, index) goes into DungeonRecord and
    ///   the town's save, like a slain boss.
    /// </summary>
    public class DungeonCrystals : MonoBehaviour
    {
        public class Crystal
        {
            public string Id;
            public Vector3 At;
            /// <summary>The height of the crystal's foot over the floor.</summary>
            public float Lift;
            public Transform Shown;
            public Transform Gem;
            /// <summary>Where its glow lies: the floor, or the top of a reward room's pedestal.</summary>
            public float GlowAt;
            public GameObject Ground;
        }

        public static DungeonCrystals Instance { get; private set; }

        /// <summary>The crystals still lying here.</summary>
        public IReadOnlyList<Crystal> Lying => _lying;

        /// <summary>Taken on this visit, by id.</summary>
        public IReadOnlyCollection<string> TakenNow => _takenNow;

        public int CrystalsTaken { get; private set; }

        private readonly List<Crystal> _lying = new List<Crystal>();
        private readonly HashSet<string> _takenNow = new HashSet<string>();
        private float _pickTimer;
        private const float PickCheckSeconds = 0.2f;
        private const float SpinDegreesPerSecond = 35f;
        private const float BobCells = 0.07f;
        private const float BobSpeed = 1.6f;
        private const float SwayDegrees = 6f;
        // the model's height (MonsterRewardCrystal is 0.7 tall): a reward spot marks the crystal's middle
        private const float GemHeight = 0.7f;
        private const float HalfHeight = GemHeight * 0.5f;
        private const float HaloSize = 1.6f;
        private const float GroundGlowSize = 1.3f;
        private const float FlareSeconds = 0.45f;
        // a reward room's pedestal (dungeon1.pedestal: 0.5 build units = 0.22 cells) and a hair over it
        private const float PedestalTop = 0.24f;

        private readonly List<(Transform shown, float age)> _flares = new List<(Transform, float)>();
        private static Mesh _quad, _flatQuad;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static string CrystalId(string dungeonId, int level, int index) => $"{dungeonId}-L{level}-c{index}";

        public void Begin(MeshMapDefinition map, string dungeonId, int level, ICollection<string> takenBefore)
        {
            var config = BalanceConfig.Instance;
            var index = 0;
            foreach (var spot in map.SpotsOf("reward")) Put(spot, config.DungeonCrystalsPerRoom, dungeonId, level, ref index, takenBefore);
            foreach (var spot in map.SpotsOf("crystal")) Put(spot, config.DungeonCrystalsLone, dungeonId, level, ref index, takenBefore);
        }

        private void Put(MapSpot spot, int count, string dungeonId, int level, ref int index, ICollection<string> takenBefore)
        {
            var ground = new Vector3(spot.position.x, 0f, spot.position.y);
            if (MeshMapApplier.Instance != null && MeshMapApplier.Instance.TryGetGroundHeight(ground, out var y)) ground.y = y;
            for (var k = 0; k < count; k++)
            {
                var id = CrystalId(dungeonId, level, index++);
                if (takenBefore != null && takenBefore.Contains(id)) continue;
                // two side by side in a room, a little apart
                var at = ground + (count > 1 ? new Vector3((k - (count - 1) * 0.5f) * 0.5f, 0f, 0f) : Vector3.zero);
                // a reward room's spot hangs over its pedestal (dungeon1.py): the crystal's middle there;
                // a lone crystal floats dungeon_crystal_float over the floor
                var lift = spot.height > 0f ? spot.height - HalfHeight : BalanceConfig.Instance.DungeonCrystalFloat;
                var crystal = new Crystal { Id = id, At = at, Lift = lift, GlowAt = spot.height > 0f ? PedestalTop : 0.03f };
                Show(crystal, k, _lying.Count);
                _lying.Add(crystal);
            }
        }

        private void Show(Crystal c, int k, int n)
        {
            var raids = OrcRaidManager.Instance;
            var root = new GameObject("RewardCrystal").transform;
            root.SetParent(transform, false);
            root.position = c.At + Vector3.up * c.Lift;

            // the gem, in its own glowing shader
            var model = raids != null ? raids.RewardCrystalModel : null;
            GameObject gem;
            if (model != null) gem = Instantiate(model, root);
            else
            {
                gem = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(gem.GetComponent<Collider>());
                gem.transform.SetParent(root, false);
                gem.transform.localScale = new Vector3(0.25f, GemHeight, 0.25f);
            }
            gem.name = "Gem";
            gem.transform.localPosition = Vector3.zero;
            var look = raids != null ? raids.CrystalMaterial : null;
            foreach (var r in gem.GetComponentsInChildren<Renderer>())
            {
                if (look != null) r.sharedMaterial = look;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            c.Gem = gem.transform;

            // the halo with its sparkles, round the gem's middle
            var halo = raids != null ? raids.CrystalHaloMaterial : null;
            if (halo != null)
            {
                var h = new GameObject("Halo", typeof(MeshFilter), typeof(MeshRenderer));
                h.transform.SetParent(root, false);
                h.transform.localPosition = Vector3.up * HalfHeight;
                h.GetComponent<MeshFilter>().sharedMesh = Quad();
                var renderer = h.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = halo;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                var block = new MaterialPropertyBlock();
                block.SetFloat("_Size", HaloSize);
                block.SetFloat("_Phase", n * 1.7f + k);
                renderer.SetPropertyBlock(block);
            }

            // the glow on the floor (or the pedestal) under it -- it does not bob
            var ground = raids != null ? raids.CrystalGroundMaterial : null;
            if (ground != null)
            {
                var g = new GameObject("GroundGlow", typeof(MeshFilter), typeof(MeshRenderer));
                g.transform.SetParent(transform, false);
                g.transform.position = c.At + Vector3.up * c.GlowAt;
                g.GetComponent<MeshFilter>().sharedMesh = FlatQuad();
                var renderer = g.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = ground;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                c.Ground = g;
            }
            c.Shown = root;
        }

        /// <summary>The halo's quad: the shader turns it to the camera and sizes it; the bounds are made big enough for that.</summary>
        private static Mesh Quad()
        {
            if (_quad != null) return _quad;
            var h = HaloSize * 0.5f;
            _quad = new Mesh
            {
                name = "CrystalHaloQuad",
                vertices = new[] { new Vector3(-h, -h, 0f), new Vector3(h, -h, 0f), new Vector3(h, h, 0f), new Vector3(-h, h, 0f) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
            };
            _quad.bounds = new Bounds(Vector3.zero, Vector3.one * HaloSize);
            return _quad;
        }

        private static Mesh FlatQuad()
        {
            if (_flatQuad != null) return _flatQuad;
            var h = GroundGlowSize * 0.5f;
            _flatQuad = new Mesh
            {
                name = "CrystalGroundQuad",
                vertices = new[] { new Vector3(-h, 0f, -h), new Vector3(h, 0f, -h), new Vector3(h, 0f, h), new Vector3(-h, 0f, h) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) },
                triangles = new[] { 0, 2, 1, 0, 3, 2 },
            };
            _flatQuad.RecalculateBounds();
            return _flatQuad;
        }

        private void Update()
        {
            var t = Time.time;
            for (var i = 0; i < _lying.Count; i++)
            {
                var c = _lying[i];
                if (c.Shown == null) continue;
                // a slow bob, a little sway, a slow turn -- each crystal out of step with the next
                var p = c.At;
                p.y += c.Lift + Mathf.Sin(t * BobSpeed + i) * BobCells;
                c.Shown.position = p;
                if (c.Gem != null)
                    c.Gem.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.1f + i) * SwayDegrees, t * SpinDegreesPerSecond + i * 40f,
                        Mathf.Cos(t * 0.9f + i * 1.3f) * SwayDegrees);
            }
            // taken: it flares up, rises and melts away
            for (var i = _flares.Count - 1; i >= 0; i--)
            {
                var (shown, age) = _flares[i];
                age += Time.deltaTime;
                if (shown == null || age >= FlareSeconds)
                {
                    if (shown != null) Destroy(shown.gameObject);
                    _flares.RemoveAt(i);
                    continue;
                }
                var a = age / FlareSeconds;
                shown.localScale = Vector3.one * ((1f + 1.5f * a) * (1f - a * a));
                shown.position += Vector3.up * (Time.deltaTime * 1.5f);
                _flares[i] = (shown, age);
            }
            _pickTimer -= Time.deltaTime;
            if (_pickTimer > 0f) return;
            _pickTimer = PickCheckSeconds;
            PickUp();
        }

        /// <summary>Any hero within reach takes the crystals there. Public for the tests.</summary>
        public void PickUp()
        {
            var reach = BalanceConfig.Instance.DungeonCrystalPickReach;
            for (var i = _lying.Count - 1; i >= 0; i--)
            {
                var c = _lying[i];
                foreach (var soldier in SoldierUnit.All)
                {
                    if (soldier == null || soldier.CurrentHealth <= 0) continue;
                    var d = soldier.transform.position - c.At;
                    d.y = 0f;
                    if (d.magnitude > reach) continue;
                    Take(i);
                    break;
                }
            }
        }

        private void Take(int i)
        {
            var c = _lying[i];
            _lying.RemoveAt(i);
            if (c.Shown != null) _flares.Add((c.Shown, 0f));
            if (c.Ground != null) Destroy(c.Ground);
            _takenNow.Add(c.Id);
            DungeonRecord.CrystalsTaken.Add(c.Id);
            CrystalsTaken++;
            Wallet.UpgradeCrystals += 1;
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Format("#log_dungeon_crystal", 1), EventSeverity.Good,
                EventGlyph.Upgrade, "Res_UpgradeCrystal", c.At));   // the crystal itself on the notice (the user, 2026-11-01)
        }
    }
}
