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
