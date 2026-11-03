using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// A moat cell's banks (Tools/blender/moat.py, Moat-Bank): one per side with no moat beside it,
    /// so a laid line reads as one channel. Drawn as ONE mesh -- the banks a cell wants, combined --
    /// shared by every cell with the same sides (16 at most, made once), so a cell costs two draws
    /// (these banks and its water, the shore band along the same sides) however many sides it shows (mobile first: four bank
    /// renderers a cell measured at six draws). Laid in world axes, whatever the cell's own turn.
    /// </summary>
    public class MoatAppearance : MonoBehaviour
    {
        [SerializeField] private MeshFilter banks;
        // the water: a flat cell-sized quad whose UV1.x says which sides are banks (Shaders/MoatWater.shader
        // draws the shore band along those), one mesh per side-mask shared by every cell
        [SerializeField] private MeshFilter water;
        [SerializeField] private Mesh bankMesh;
        // the bank's mesh as the model stands (its import turn and scale), drawn on the south side
        [SerializeField] private Matrix4x4 bankMatrix = Matrix4x4.identity;

        // the turn that brings the south bank to each side: N, E, S, W
        private static readonly float[] Yaw = { 180f, -90f, 0f, 90f };
        private static readonly Vector2Int[] Sides = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        private static readonly Dictionary<int, Mesh> Shapes = new Dictionary<int, Mesh>();
        private static readonly Dictionary<int, Mesh> Waters = new Dictionary<int, Mesh>();

        public void Wire(MeshFilter combined, Mesh bank, Matrix4x4 matrix, MeshFilter waterQuad)
        {
            banks = combined;
            water = waterQuad;
            bankMesh = bank;
            bankMatrix = matrix;
        }

        public void Refresh(Vector2Int cell)
        {
            if (banks == null || bankMesh == null) return;
            var mask = 0;
            for (var i = 0; i < Sides.Length; i++)
                if (!Moat.Has(cell + Sides[i])) mask |= 1 << i;
            banks.transform.rotation = Quaternion.identity;
            var renderer = banks.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.enabled = mask != 0;
            banks.sharedMesh = mask != 0 ? Shape(mask) : null;
            if (water != null)
            {
                water.transform.rotation = Quaternion.identity;
                water.sharedMesh = Water(mask);
            }
        }

        private static Mesh Water(int mask)
        {
            if (Waters.TryGetValue(mask, out var mesh) && mesh != null) return mesh;
            mesh = new Mesh { name = $"MoatWater{mask}" };
            mesh.SetVertices(new List<Vector3> { new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f) });
            mesh.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) });
            var m = new Vector2(mask, 0f);
            mesh.SetUVs(1, new List<Vector2> { m, m, m, m });
            mesh.SetNormals(new List<Vector3> { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
            mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            mesh.RecalculateBounds();
            Waters[mask] = mesh;
            return mesh;
        }

        private Mesh Shape(int mask)
        {
            if (Shapes.TryGetValue(mask, out var mesh) && mesh != null) return mesh;
            var parts = new List<CombineInstance>(4);
            for (var i = 0; i < Yaw.Length; i++)
            {
                if ((mask & (1 << i)) == 0) continue;
                parts.Add(new CombineInstance { mesh = bankMesh, transform = Matrix4x4.Rotate(Quaternion.Euler(0f, Yaw[i], 0f)) * bankMatrix });
            }
            mesh = new Mesh { name = $"MoatBanks{mask}" };
            mesh.CombineMeshes(parts.ToArray(), true, true);
            Shapes[mask] = mesh;
            return mesh;
        }
    }
}
