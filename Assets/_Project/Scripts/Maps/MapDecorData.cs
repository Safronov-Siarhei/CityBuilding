using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// A map's small props -- tufts, flowers, bushes, reeds, lilies, ferns, crags -- as one list of
    /// placements over a handful of prototype meshes. Written by SetupProject from the list the
    /// Blender generator exports (Tools/blender/map3.py, Models/Map3/Map-3-Decor.csv) and drawn by
    /// MapDecorRenderer. Parallel arrays, one entry per prop: thousands of them, so no per-prop
    /// object anywhere -- not here and not in the scene.
    /// </summary>
    public class MapDecorData : ScriptableObject
    {
        [SerializeField] private Mesh[] prototypes = new Mesh[0];
        [SerializeField] private ushort[] prototypeIndex = new ushort[0];
        [SerializeField] private Vector3[] positions = new Vector3[0];
        [SerializeField] private float[] yawDegrees = new float[0];
        [SerializeField] private float[] scales = new float[0];

        public Mesh[] Prototypes => prototypes;
        public int Count => positions.Length;
        public int PrototypeOf(int i) => prototypeIndex[i];
        public Vector3 PositionOf(int i) => positions[i];
        public float YawOf(int i) => yawDegrees[i];
        public float ScaleOf(int i) => scales[i];

        /// <summary>
        /// Several maps' props in one set, each moved and turned (the archipelago: every library
        /// island's props, placed with its island). Built at run time; the prototypes are shared.
        /// </summary>
        public static MapDecorData Combine(System.Collections.Generic.List<(MapDecorData data, Vector3 at, Quaternion turn)> parts)
        {
            var meshes = new System.Collections.Generic.List<Mesh>();
            var meshIndex = new System.Collections.Generic.Dictionary<Mesh, ushort>();
            var index = new System.Collections.Generic.List<ushort>();
            var pos = new System.Collections.Generic.List<Vector3>();
            var yaw = new System.Collections.Generic.List<float>();
            var scale = new System.Collections.Generic.List<float>();
            foreach (var (data, at, turn) in parts)
            {
                var spin = turn.eulerAngles.y;
                for (var i = 0; i < data.Count; i++)
                {
                    var mesh = data.prototypes[data.prototypeIndex[i]];
                    if (!meshIndex.TryGetValue(mesh, out var k))
                    {
                        k = (ushort)meshes.Count;
                        meshes.Add(mesh);
                        meshIndex[mesh] = k;
                    }
                    index.Add(k);
                    pos.Add(at + turn * data.positions[i]);
                    yaw.Add(data.yawDegrees[i] + spin);
                    scale.Add(data.scales[i]);
                }
            }
            var combined = CreateInstance<MapDecorData>();
            combined.prototypes = meshes.ToArray();
            combined.prototypeIndex = index.ToArray();
            combined.positions = pos.ToArray();
            combined.yawDegrees = yaw.ToArray();
            combined.scales = scale.ToArray();
            return combined;
        }

#if UNITY_EDITOR
        public void EditorInitialize(Mesh[] meshes, ushort[] index, Vector3[] pos, float[] yaw, float[] scale)
        {
            prototypes = meshes;
            prototypeIndex = index;
            positions = pos;
            yawDegrees = yaw;
            scales = scale;
        }
#endif
    }
}
