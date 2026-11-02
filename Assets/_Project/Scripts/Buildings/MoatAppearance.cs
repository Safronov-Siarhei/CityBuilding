using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// A moat cell's banks (Tools/blender/moat.py, Moat-Bank): one per side, shown where no moat
    /// lies beside it, so a laid line reads as one channel. Built hidden into the prefab by
    /// SetupProject; turned in world space, so the building's own placement turn changes nothing.
    /// </summary>
    public class MoatAppearance : MonoBehaviour
    {
        // sides N, E, S, W
        [SerializeField] private GameObject[] banks = new GameObject[0];
        // the bank's import turn (the model is drawn on the south side)
        [SerializeField] private Quaternion modelRotation = Quaternion.identity;

        // the turn that brings the south bank to each side: N, E, S, W
        private static readonly float[] Yaw = { 180f, -90f, 0f, 90f };
        private static readonly Vector2Int[] Sides = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        public void Wire(GameObject[] sideBanks, Quaternion rotation)
        {
            banks = sideBanks;
            modelRotation = rotation;
        }

        public void Refresh(Vector2Int cell)
        {
            for (var i = 0; i < banks.Length && i < Sides.Length; i++)
            {
                if (banks[i] == null) continue;
                banks[i].transform.rotation = Quaternion.Euler(0f, Yaw[i], 0f) * modelRotation;
                banks[i].SetActive(!Moat.Has(cell + Sides[i]));
            }
        }
    }
}
