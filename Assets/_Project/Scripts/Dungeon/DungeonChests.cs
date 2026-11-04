using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Dungeon
{
    /// <summary>
    /// Chests in the dead ends a crystal does not take (the user, 2026-11-04: «размести в тупиках
    /// сундуки с ресурсами, только не много ... утешительный приз игроку, сундукам нужно добавить
    /// какой-то блеск»): one at every "chest" spot of the map, the archipelago caches' chest model
    /// with a gold glint over it (the crystals' halo, gold). A hero within dungeon_chest_reach opens
    /// it: the lid swings up and it pays a little of ONE thing -- wood, stone, gold or coins, in turn
    /// from chest to chest (dungeon_chest_*, times the dungeon's level). Not guarded and not saved:
    /// they are full again on the next visit, like the monsters.
    /// </summary>
    public class DungeonChests : MonoBehaviour
    {
        public class Chest
        {
            public Vector3 At;
            public Transform Root;
            public Transform Lid;
            public Quaternion LidRest;
            public float LidSign = 1f;
            public float Opening = -1f;
            public GameObject Glint;
            public bool Open => Opening >= 0f;
        }

        public static DungeonChests Instance { get; private set; }

        public IReadOnlyList<Chest> Chests => _chests;

        private readonly List<Chest> _chests = new List<Chest>();
        private int _level = 1;
        private float _checkTimer;
        private const float CheckSeconds = 0.2f;
        private const float LidOpenDegrees = 105f;
        private const float LidOpenSeconds = 0.8f;
        private const float GlintSize = 1.2f;
        private const float GlintLift = 0.35f;
        private static readonly Color GlintColour = new Color(1f, 0.82f, 0.3f, 1f);
        private static Mesh _quad;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Begin(MeshMapDefinition map, int level)
        {
            _level = Mathf.Clamp(level, 1, 3);
            var model = MeshMapApplier.Instance != null ? MeshMapApplier.Instance.CacheChestModel : null;
            foreach (var spot in map.SpotsOf("chest")) Put(spot, model);
        }

        private void Put(MapSpot spot, GameObject model)
        {
            var at = new Vector3(spot.position.x, 0f, spot.position.y);
            if (MeshMapApplier.Instance != null && MeshMapApplier.Instance.TryGetGroundHeight(at, out var y)) at.y = y;
            if (UnityEngine.AI.NavMesh.SamplePosition(at, out var hit, 2f, UnityEngine.AI.NavMesh.AllAreas)) at = hit.position;
            var chest = new Chest { At = at };
            var root = new GameObject($"DungeonChest{_chests.Count}").transform;
            root.SetParent(transform, false);
            root.position = at;
            // its front to the camera, which never turns
            var cam = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
            cam.y = 0f;
            if (cam.sqrMagnitude > 0.01f) root.rotation = Quaternion.LookRotation(cam.normalized);
            chest.Root = root;
            if (model != null)
            {
                var c = Instantiate(model, root);
                c.name = "Chest";
                c.transform.localPosition = Vector3.zero;
                c.transform.localRotation = model.transform.localRotation;
                SceneryShadows.DisableCasting(c);
                foreach (var t in c.GetComponentsInChildren<Transform>(true))
                    if (t.name.EndsWith("Lid")) chest.Lid = t;
                if (chest.Lid != null)
                {
                    chest.LidRest = chest.Lid.localRotation;
                    // which way round the hinge lifts the lid's front: the import's axes decide (as TideCaches)
                    var lid = chest.Lid.GetComponent<Renderer>();
                    if (lid != null)
                    {
                        var rest = lid.bounds.center.y;
                        chest.Lid.localRotation = chest.LidRest * Quaternion.Euler(60f, 0f, 0f);
                        chest.LidSign = lid.bounds.center.y >= rest ? 1f : -1f;
                        chest.Lid.localRotation = chest.LidRest;
                    }
                }
            }
            chest.Glint = Glint(root);
            _chests.Add(chest);
        }

        /// <summary>A gold glint over the chest: the crystals' breathing halo with its sparkles, in gold.</summary>
        private GameObject Glint(Transform root)
        {
            var raids = OrcRaidManager.Instance;
            var halo = raids != null ? raids.CrystalHaloMaterial : null;
            if (halo == null) return null;
            var h = new GameObject("Glint", typeof(MeshFilter), typeof(MeshRenderer));
            h.transform.SetParent(root, false);
            h.transform.localPosition = Vector3.up * GlintLift;
            h.GetComponent<MeshFilter>().sharedMesh = Quad();
            var renderer = h.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = halo;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            block.SetFloat("_Size", GlintSize);
            block.SetFloat("_Phase", _chests.Count * 2.3f);
            block.SetColor("_BaseColor", GlintColour);
            block.SetColor("_SparkColor", new Color(1f, 0.95f, 0.7f, 1f));
            renderer.SetPropertyBlock(block);
            return h;
        }

        private static Mesh Quad()
        {
            if (_quad != null) return _quad;
            var h = GlintSize * 0.5f;
            _quad = new Mesh
            {
                name = "ChestGlintQuad",
                vertices = new[] { new Vector3(-h, -h, 0f), new Vector3(h, -h, 0f), new Vector3(h, h, 0f), new Vector3(-h, h, 0f) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
            };
            _quad.bounds = new Bounds(Vector3.zero, Vector3.one * GlintSize);
            return _quad;
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            foreach (var chest in _chests)
            {
                if (!chest.Open || chest.Opening >= 1f) continue;
                chest.Opening = Mathf.Min(1f, chest.Opening + dt / LidOpenSeconds);
                if (chest.Lid != null)
                    chest.Lid.localRotation = chest.LidRest * Quaternion.Euler(LidOpenDegrees * chest.LidSign * Mathf.SmoothStep(0f, 1f, chest.Opening), 0f, 0f);
            }
            _checkTimer -= dt;
            if (_checkTimer > 0f) return;
            _checkTimer = CheckSeconds;
            OpenNear();
        }

        /// <summary>Any hero within reach opens the chests there. Public for the tests.</summary>
        public void OpenNear()
        {
            var reach = BalanceConfig.Instance.DungeonChestReach;
            for (var i = 0; i < _chests.Count; i++)
            {
                var chest = _chests[i];
                if (chest.Open) continue;
                foreach (var soldier in SoldierUnit.All)
                {
                    if (soldier == null || soldier.CurrentHealth <= 0) continue;
                    var d = soldier.transform.position - chest.At;
                    d.y = 0f;
                    if (d.magnitude > reach) continue;
                    OpenChest(chest, i);
                    break;
                }
            }
        }

        private void OpenChest(Chest chest, int index)
        {
            chest.Opening = 0f;
            if (chest.Glint != null) Destroy(chest.Glint);
            var c = BalanceConfig.Instance;
            var popups = UI.ResourceGainPopups.Instance;
            var at = chest.At + Vector3.up * 0.8f;
            // one thing a chest, in turn: wood, stone, gold, coins
            switch (index % 4)
            {
                case 0: Pay(ResourceType.Wood, c.DungeonChestWood * _level, at, popups); break;
                case 1: Pay(ResourceType.Stone, c.DungeonChestStone * _level, at, popups); break;
                case 2: Pay(ResourceType.Gold, c.DungeonChestGold * _level, at, popups); break;
                default:
                    var coins = c.DungeonChestCoins * _level;
                    ResourceManager.Instance?.Add(HeroRoster.Money, coins);
                    popups?.ShowCoins(chest.At, coins);
                    break;
            }
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_dungeon_chest"), EventSeverity.Good,
                EventGlyph.Star, null, chest.At));
        }

        /// <summary>A resource for the town, carried up with the bosses' (DungeonTravel.Leave reads DungeonMonsters.ResourcesEarned).</summary>
        private static void Pay(ResourceType type, int amount, Vector3 at, UI.ResourceGainPopups popups)
        {
            if (amount <= 0) return;
            var monsters = DungeonMonsters.Instance;
            if (monsters != null)
                monsters.ResourcesEarned[type] = (monsters.ResourcesEarned.TryGetValue(type, out var had) ? had : 0) + amount;
            popups?.Show(at, amount, type);
        }
    }
}
