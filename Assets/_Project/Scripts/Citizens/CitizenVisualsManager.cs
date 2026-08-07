using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// Spawns/despawns a simple cube-built citizen model per population point and lets each one
    /// wander ambiently near the town center (see CitizenAgent). Purely visual — population
    /// itself stays owned by CitizenManager; this just mirrors it. No task assignment yet.
    /// </summary>
    public class CitizenVisualsManager : MonoBehaviour
    {
        [SerializeField] private CitizenManager citizenManager;
        [SerializeField] private GridManager gridManager;

        private static readonly Color[] ClothingColors =
        {
            new Color(0.72f, 0.26f, 0.24f),
            new Color(0.24f, 0.42f, 0.68f),
            new Color(0.32f, 0.55f, 0.28f),
            new Color(0.62f, 0.5f, 0.28f),
            new Color(0.45f, 0.42f, 0.48f),
        };
        private static readonly Color SkinColor = new Color(0.85f, 0.68f, 0.55f);

        private readonly List<GameObject> _agents = new List<GameObject>();
        private Material[] _clothingMaterials;
        private Material _skinMaterial;
        private Vector3? _townCenter;

        private void Start()
        {
            if (citizenManager != null)
            {
                citizenManager.OnPopulationChanged += SyncVisualCount;
            }
            SyncVisualCount();
        }

        private void SyncVisualCount()
        {
            if (citizenManager == null) return;

            var target = citizenManager.TotalPopulation;

            while (_agents.Count < target)
            {
                var agent = SpawnAgent();
                if (agent == null) break; // town center not resolvable yet (Town Hall not placed)
                _agents.Add(agent);
            }

            while (_agents.Count > target)
            {
                var last = _agents[_agents.Count - 1];
                _agents.RemoveAt(_agents.Count - 1);
                if (last != null) Destroy(last);
            }
        }

        private GameObject SpawnAgent()
        {
            var center = ResolveTownCenter();
            if (center == null) return null;

            EnsureMaterials();

            var root = new GameObject("Citizen");
            root.transform.SetParent(transform, false);

            var clothing = _clothingMaterials[Random.Range(0, _clothingMaterials.Length)];
            AddCubePart(root.transform, "Body", new Vector3(0f, 0.25f, 0f), new Vector3(0.3f, 0.5f, 0.3f), clothing);
            AddCubePart(root.transform, "Head", new Vector3(0f, 0.61f, 0f), new Vector3(0.22f, 0.22f, 0.22f), _skinMaterial);

            var agent = root.AddComponent<CitizenAgent>();
            agent.Initialize(center.Value);

            return root;
        }

        private Vector3? ResolveTownCenter()
        {
            if (_townCenter.HasValue) return _townCenter;
            if (gridManager == null) return null;

            foreach (var instance in FindObjectsByType<BuildingInstance>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (instance.Data != null && instance.Data.buildingName == "TownHall")
                {
                    _townCenter = gridManager.GetFootprintCenterWorld(instance.OriginCell, instance.Data.footprintSize);
                    return _townCenter;
                }
            }

            return null;
        }

        private void EnsureMaterials()
        {
            if (_clothingMaterials != null) return;

            _clothingMaterials = new Material[ClothingColors.Length];
            for (var i = 0; i < ClothingColors.Length; i++)
            {
                _clothingMaterials[i] = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = ClothingColors[i] };
            }
            _skinMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = SkinColor };
        }

        private static void AddCubePart(Transform parent, string partName, Vector3 localPosition, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = partName;
            Destroy(go.GetComponent<BoxCollider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
