using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Maps;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Dungeon
{
    /// <summary>
    /// The stone spider queen in the middle of the lake (the user, 2026-11-01: «статую какого-нибудь
    /// монстра по центру озера и добавь ей эффект»; Tools/blender/dungeon1.py spider_statue): over
    /// her hangs a big crystal that turns slowly and bobs, in the reward crystals' glowing look
    /// with a wide halo. It is a REWARD (the user, 2026-11-02: «невозможно открыть статую в озере,
    /// дальность открытия не позволяет»): a hero on the lake's shore nearest it takes it --
    /// dungeon_statue_crystals upgrade crystals, once per dungeon level, saved with the crystals
    /// (DungeonCrystals.MarkTaken). Its reach runs from the statue to the nearest walkable ground,
    /// plus dungeon_crystal_pick_reach.
    /// </summary>
    public class DungeonStatue : MonoBehaviour
    {
        private const float GemScale = 2.6f;
        private const float HaloSize = 3.2f;
        private const float BobCells = 0.15f;
        private const float SpinDegreesPerSecond = 25f;
        private const float CheckSeconds = 0.4f;
        private const float FlareSeconds = 0.9f;
        // how far from the statue to look for its shore (metres)
        private const float ShoreSearch = 30f;

        private Transform _gem;
        private Vector3 _at;
        private string _id;
        private float _reach;
        private float _checkIn;
        private float _flare = -1f;

        /// <summary>The crystal over every statue of the map not taken yet.</summary>
        public static void RaiseAll(MeshMapDefinition map, Transform parent, string dungeonId, int level, ICollection<string> takenBefore)
        {
            var index = 0;
            foreach (var spot in map.SpotsOf("statue"))
            {
                var id = $"{dungeonId}-L{level}-statue{index++}";
                if ((takenBefore != null && takenBefore.Contains(id)) || DungeonRecord.CrystalsTaken.Contains(id)) continue;
                var at = new Vector3(spot.position.x, spot.height, spot.position.y);
                var go = new GameObject("StatueCrystal");
                go.transform.SetParent(parent, false);
                go.AddComponent<DungeonStatue>().Dress(at, id);
            }
        }

        private void Dress(Vector3 at, string id)
        {
            _at = at;
            _id = id;
            transform.position = at;
            // its reach: out to the shore nearest it, and a hero's pick-up reach beyond
            var config = BalanceConfig.Instance;
            var pick = config != null ? config.DungeonCrystalPickReach : 1f;
            var ground = new Vector3(at.x, 0f, at.z);
            _reach = NavMesh.SamplePosition(ground, out var shore, ShoreSearch, NavMesh.AllAreas)
                ? Flat(shore.position - ground).magnitude + pick + 0.5f
                : ShoreSearch;

            var raids = OrcRaidManager.Instance;
            var model = raids != null ? raids.RewardCrystalModel : null;
            GameObject gem;
            if (model != null) gem = Instantiate(model, transform);
            else
            {
                gem = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(gem.GetComponent<Collider>());
                gem.transform.SetParent(transform, false);
                gem.transform.localScale = new Vector3(0.25f, 0.5f, 0.25f);
            }
            gem.name = "Gem";
            gem.transform.localPosition = Vector3.zero;
            gem.transform.localScale *= GemScale;
            var look = raids != null ? raids.CrystalMaterial : null;
            foreach (var r in gem.GetComponentsInChildren<Renderer>())
            {
                if (look != null) r.sharedMaterial = look;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            _gem = gem.transform;

            var halo = raids != null ? raids.CrystalHaloMaterial : null;
            if (halo == null) return;
            var h = new GameObject("Halo", typeof(MeshFilter), typeof(MeshRenderer));
            h.transform.SetParent(transform, false);
            h.transform.localPosition = Vector3.up * 0.5f;
            h.GetComponent<MeshFilter>().sharedMesh = Quad();
            var renderer = h.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = halo;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            block.SetFloat("_Size", HaloSize);
            block.SetFloat("_Phase", 0.7f);
            renderer.SetPropertyBlock(block);
        }

        private static Mesh _quad;

        private static Mesh Quad()
        {
            if (_quad != null) return _quad;
            var h = HaloSize * 0.5f;
            _quad = new Mesh
            {
                name = "StatueHaloQuad",
                vertices = new[] { new Vector3(-h, -h, 0f), new Vector3(h, -h, 0f), new Vector3(h, h, 0f), new Vector3(-h, h, 0f) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
            };
            _quad.bounds = new Bounds(Vector3.zero, Vector3.one * HaloSize);
            return _quad;
        }

        private void Update()
        {
            var t = Time.time;
            if (_flare >= 0f)
            {
                // taken: it flares up, rises and melts away
                _flare += Time.deltaTime;
                var a = _flare / FlareSeconds;
                if (a >= 1f)
                {
                    Destroy(gameObject);
                    return;
                }
                transform.localScale = Vector3.one * ((1f + 1.5f * a) * (1f - a * a));
                transform.position += Vector3.up * (Time.deltaTime * 1.5f);
                return;
            }
            transform.position = _at + Vector3.up * (Mathf.Sin(t * 0.8f) * BobCells);
            if (_gem != null) _gem.localRotation = Quaternion.Euler(0f, t * SpinDegreesPerSecond, Mathf.Sin(t * 0.6f) * 6f);

            _checkIn -= Time.deltaTime;
            if (_checkIn > 0f) return;
            _checkIn = CheckSeconds;
            foreach (var soldier in SoldierUnit.All)
            {
                if (soldier == null || soldier.CurrentHealth <= 0) continue;
                if (Flat(soldier.transform.position - _at).magnitude > _reach) continue;
                Take();
                return;
            }
        }

        private void Take()
        {
            var worth = BalanceConfig.Instance != null ? BalanceConfig.Instance.DungeonStatueCrystals : 3;
            Wallet.UpgradeCrystals += worth;
            DungeonCrystals.Instance?.MarkTaken(_id);
            UI.ResourceGainPopups.Instance?.ShowCrystals(_at + Vector3.up * 1.5f, worth);
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Format("#log_dungeon_crystal", worth), EventSeverity.Good,
                EventGlyph.Upgrade, "Res_UpgradeCrystal", _at));
            _flare = 0f;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
