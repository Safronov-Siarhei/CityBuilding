using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// The worker-shortage mark (the user, 2026-11-02): a villager's face with a red «!» standing over
    /// every built workplace that wants more hands than it has (ProductionBuilding.ShortOfWorkers) --
    /// a new building when there is nobody free, one whose man was killed. It goes once the free
    /// people fill the place (CitizenManager.RefillWantedJobs). One quad per building, made the first
    /// time it is wanted; the shader turns it to the camera (Shaders/IconBillboard).
    /// </summary>
    public class WorkerShortageMarks : MonoBehaviour
    {
        [SerializeField] private Material material;
        // the resource-shortage mark (the user, 2026-11-13): an ore heap with the red «!» (UI/Icons/NoResource.png)
        [SerializeField] private Material resourceMaterial;

        private const float CheckSeconds = 0.5f;
        private const float MarkSize = 1.1f;
        private const float MarkLift = 0.7f;

        private readonly Dictionary<ProductionBuilding, GameObject> _marks = new Dictionary<ProductionBuilding, GameObject>();
        private readonly Dictionary<ProductionBuilding, GameObject> _resourceMarks = new Dictionary<ProductionBuilding, GameObject>();
        private readonly List<ProductionBuilding> _gone = new List<ProductionBuilding>();
        private float _checkIn;
        private static Mesh _quad;

        private void Update()
        {
            _checkIn -= Time.unscaledDeltaTime;
            if (_checkIn > 0f || (material == null && resourceMaterial == null)) return;
            _checkIn = CheckSeconds;
            foreach (var building in ProductionBuilding.All)
            {
                if (building == null) continue;
                var noHands = building.ShortOfWorkers;
                Toggle(_marks, building, noHands, material);
                // the hands first: a place with nobody in it is not short of ore yet
                Toggle(_resourceMarks, building, !noHands && building.ShortOfResources, resourceMaterial);
            }
            // a building pulled down takes its mark with it (a child); forget it
            Forget(_marks);
            Forget(_resourceMarks);
        }

        private void Toggle(Dictionary<ProductionBuilding, GameObject> marks, ProductionBuilding building, bool show, Material with)
        {
            marks.TryGetValue(building, out var mark);
            if (!show || with == null)
            {
                if (mark != null && mark.activeSelf) mark.SetActive(false);
                return;
            }
            if (mark == null) marks[building] = Make(building, with);
            else if (!mark.activeSelf)
            {
                Place(mark.transform, building);
                mark.SetActive(true);
            }
        }

        private void Forget(Dictionary<ProductionBuilding, GameObject> marks)
        {
            _gone.Clear();
            foreach (var pair in marks)
                if (pair.Key == null) _gone.Add(pair.Key);
            foreach (var key in _gone) marks.Remove(key);
        }

        /// <summary>Whether the resource-shortage mark is up over this building -- for the tests.</summary>
        public bool ShowsShortage(ProductionBuilding building) => building != null && _resourceMarks.TryGetValue(building, out var mark) && mark != null && mark.activeSelf;

        /// <summary>Whether the mark is up over this building -- for the tests.</summary>
        public bool Shows(ProductionBuilding building) => building != null && _marks.TryGetValue(building, out var mark) && mark != null && mark.activeSelf;

        private GameObject Make(ProductionBuilding building, Material with)
        {
            var mark = new GameObject(with == resourceMaterial ? "NoResourceMark" : "NoWorkerMark", typeof(MeshFilter), typeof(MeshRenderer));
            mark.transform.SetParent(building.transform, false);
            mark.GetComponent<MeshFilter>().sharedMesh = Quad();
            var renderer = mark.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = with;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            block.SetFloat("_Size", MarkSize);
            renderer.SetPropertyBlock(block);
            Place(mark.transform, building);
            return mark;
        }

        /// <summary>Over the building's top (its level model may have grown since).</summary>
        private static void Place(Transform mark, ProductionBuilding building)
        {
            var top = building.transform.position.y;
            foreach (var r in building.GetComponentsInChildren<MeshRenderer>())
                if (r.transform != mark) top = Mathf.Max(top, r.bounds.max.y);
            var p = building.transform.position;
            mark.position = new Vector3(p.x, top + MarkLift, p.z);
        }

        /// <summary>The quad: the shader turns it to the camera, so its bounds hold it whichever way.</summary>
        private static Mesh Quad()
        {
            if (_quad != null) return _quad;
            var h = 0.5f;
            _quad = new Mesh
            {
                name = "NoWorkerMarkQuad",
                vertices = new[] { new Vector3(-h, -h, 0f), new Vector3(h, -h, 0f), new Vector3(h, h, 0f), new Vector3(-h, h, 0f) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) },
                triangles = new[] { 0, 2, 1, 0, 3, 2 },
            };
            _quad.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
            return _quad;
        }
    }
}
