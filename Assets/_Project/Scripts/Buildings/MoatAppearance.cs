using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// A moat cell's banks (Tools/blender/moat.py, Moat-Bank): one per side with no moat beside it,
    /// so a laid line reads as one channel. Drawn as ONE mesh -- the banks a cell wants, combined --
    /// shared by every cell with the same sides (16 at most, made once), so a cell costs two draws
    /// (these banks and its living water) however many sides it shows (mobile first: four bank
    /// renderers a cell measured at six draws). Laid in world axes, whatever the cell's own turn.
    /// </summary>
    public class MoatAppearance : MonoBehaviour
    {
        [SerializeField] private MeshFilter banks;
        [SerializeField] private Mesh bankMesh;
        // the bank's mesh as the model stands (its import turn and scale), drawn on the south side
        [SerializeField] private Matrix4x4 bankMatrix = Matrix4x4.identity;

        // the turn that brings the south bank to each side: N, E, S, W
        private static readonly float[] Yaw = { 180f, -90f, 0f, 90f };
        private static readonly Vector2Int[] Sides = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        private static readonly Dictionary<int, Mesh> Shapes = new Dictionary<int, Mesh>();

        public void Wire(MeshFilter combined, Mesh bank, Matrix4x4 matrix)
        {
            banks = combined;
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
