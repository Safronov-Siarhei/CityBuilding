using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Grid
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [SerializeField] private float cellSize = 2f;
        [SerializeField] private Vector3 originWorldPosition = Vector3.zero;
        [SerializeField] private Vector2Int gridSize = new Vector2Int(30, 30);
        [SerializeField] private float groundHeight = 1f;

        private readonly HashSet<Vector2Int> _occupiedCells = new HashSet<Vector2Int>();

        public float CellSize => cellSize;
        public Vector2Int GridSize => gridSize;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public Vector2Int WorldToCell(Vector3 worldPosition)
        {
            var local = worldPosition - originWorldPosition;
            var x = Mathf.FloorToInt(local.x / cellSize);
            var z = Mathf.FloorToInt(local.z / cellSize);
            return new Vector2Int(x, z);
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            return originWorldPosition + new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);
        }

        public Vector3 GetFootprintCenterWorld(Vector2Int originCell, Vector2Int footprintSize)
        {
            var corner = CellToWorld(originCell);
            return new Vector3(
                corner.x + footprintSize.x * cellSize * 0.5f,
                groundHeight,
                corner.z + footprintSize.y * cellSize * 0.5f);
        }

        public bool IsWithinBounds(Vector2Int originCell, Vector2Int footprintSize)
        {
            if (originCell.x < 0 || originCell.y < 0) return false;
            return originCell.x + footprintSize.x <= gridSize.x && originCell.y + footprintSize.y <= gridSize.y;
        }

        public bool IsAreaFree(Vector2Int originCell, Vector2Int footprintSize)
        {
            for (var x = 0; x < footprintSize.x; x++)
            {
                for (var z = 0; z < footprintSize.y; z++)
                {
                    if (_occupiedCells.Contains(originCell + new Vector2Int(x, z))) return false;
                }
            }
            return true;
        }

        public bool CanPlace(Vector2Int originCell, Vector2Int footprintSize)
        {
            return IsWithinBounds(originCell, footprintSize) && IsAreaFree(originCell, footprintSize);
        }

        public void SetAreaOccupied(Vector2Int originCell, Vector2Int footprintSize, bool occupied)
        {
            for (var x = 0; x < footprintSize.x; x++)
            {
                for (var z = 0; z < footprintSize.y; z++)
                {
                    var cell = originCell + new Vector2Int(x, z);
                    if (occupied) _occupiedCells.Add(cell);
                    else _occupiedCells.Remove(cell);
                }
            }
        }
    }
}
