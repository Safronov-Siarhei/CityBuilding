using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// The Barbican's end walls joining the walls beside them (the user, 2026-10-25, «как сейчас
    /// реализован забор»): a fence that meets one of its end cells from the side gets a short stone
    /// arm from that cell's middle to it (Tools/blender/fortifiedgate1_arm.py), so the line turns a
    /// corner there instead of butting into the side of the curtain wall. Along the Barbican's own
    /// axis its wall already runs to the cell's edge, and the middle cell is the way through.
    ///
    /// The four arms are children of the prefab (SetupProject), in its own frame: each end cell at
    /// local z = ±1, an arm to local ±x of it. FenceNetwork tells this when a neighbour comes or goes.
    /// </summary>
    public class GateArms : MonoBehaviour
    {
        /// <summary>Order: (+z end, +x), (+z end, -x), (-z end, +x), (-z end, -x).</summary>
        [SerializeField] private GameObject[] arms = new GameObject[4];

        private static readonly Vector3[] Ends = { Vector3.forward, Vector3.forward, Vector3.back, Vector3.back };
        private static readonly Vector3[] Sides = { Vector3.right, Vector3.left, Vector3.right, Vector3.left };

        public void SetArms(GameObject[] models) => arms = models;

        /// <summary>The cells this listens for: its two end cells.</summary>
        public Vector2Int EndCell(int end)
        {
            var grid = GridManager.Instance;
            var at = transform.TransformPoint(end == 0 ? Vector3.forward : Vector3.back);
            return grid != null ? grid.WorldToCell(at) : Vector2Int.zero;
        }

        /// <summary>Each arm shown when a fence (or anything in the line) stands in the cell it points at.</summary>
        public void Refresh(FenceNetwork network)
        {
            var grid = GridManager.Instance;
            if (grid == null || network == null || arms == null) return;
            for (var i = 0; i < arms.Length && i < Ends.Length; i++)
            {
                if (arms[i] == null) continue;
                var end = transform.TransformPoint(Ends[i]);
                var beside = transform.TransformPoint(Ends[i] + Sides[i]);
                var join = network.Connects(grid.WorldToCell(beside)) && grid.WorldToCell(beside) != grid.WorldToCell(end);
                if (arms[i].activeSelf != join) arms[i].SetActive(join);
            }
        }
    }
}
