using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// A Tower or Fortified Tower of level 3+ standing against a walk wall becomes the stair up
    /// (the user, 2026-11-02): its level model gives way to the wall tower of its level
    /// (Tools/blender/walls_walk.py: WallTower / WallFortTower), with a doorway onto the walk only
    /// on the cells where a wall really meets it. Away from such a wall it looks as it always did.
    ///
    /// Every piece is a hidden child built into the prefab by SetupProject -- the bodies of levels
    /// 3..5 and a doorway for each slot (a side of the 1x1 tower; each cell of a side of the 2x2)
    /// -- so a change only switches children on and off. WallWalk calls RefreshAll after a rebuild.
    /// </summary>
    public class WallTowerLook : MonoBehaviour
    {
        // levels 3..5
        [SerializeField] private GameObject[] bodies = new GameObject[0];
        // per level, `slots` doorways in slot order: side N, E, S, W, each side's cells west->east / south->north
        [SerializeField] private GameObject[] doors = new GameObject[0];
        [SerializeField] private int slotsPerSide = 1;

        private static readonly List<WallTowerLook> All = new List<WallTowerLook>();
        private BuildingInstance _building;
        private BuildingLevelAppearance _appearance;

        public void Wire(GameObject[] levelBodies, GameObject[] levelDoors, int cellsPerSide)
        {
            bodies = levelBodies;
            doors = levelDoors;
            slotsPerSide = cellsPerSide;
        }

        private void OnEnable() => All.Add(this);

        // a wall levelled or lost: the walk is rebuilt here, at the latest, and every tower refreshed
        private void LateUpdate() => WallWalk.Ensure();
        private void OnDisable() => All.Remove(this);

        public static void RefreshAll()
        {
            for (var i = 0; i < All.Count; i++) All[i].Refresh();
        }

        /// <summary>Whether it shows its wall look now (for the tests).</summary>
        public bool OnTheWall { get; private set; }

        public void Refresh()
        {
            if (_building == null) _building = GetComponent<BuildingInstance>();
            if (_appearance == null) _appearance = GetComponent<BuildingLevelAppearance>();
            if (_building == null) return;
            var joins = WallWalk.JoinsOf(_building);
            var level = Mathf.Clamp(_building.Level, 3, 5);
            OnTheWall = joins != null && bodies.Length > 0;
            _appearance?.SetHidden(OnTheWall);
            for (var i = 0; i < bodies.Length; i++)
                if (bodies[i] != null) bodies[i].SetActive(OnTheWall && i == level - 3);
            var perLevel = slotsPerSide * 4;
            for (var i = 0; i < doors.Length; i++)
                if (doors[i] != null) doors[i].SetActive(false);
            if (!OnTheWall) return;
            var grid = Grid.GridManager.Instance;
            if (grid == null) return;
            foreach (var wall in joins)
            {
                // where the wall cell lies from the tower, in the tower's own (placed) frame
                var local = transform.InverseTransformPoint(grid.GetFootprintCenterWorld(wall, Vector2Int.one));
                int side;
                float along;
                if (Mathf.Abs(local.z) >= Mathf.Abs(local.x))
                {
                    side = local.z > 0f ? 0 : 2;
                    along = local.x;
                }
                else
                {
                    side = local.x > 0f ? 1 : 3;
                    along = local.z;
                }
                var cell = slotsPerSide > 1 && along > 0f ? 1 : 0;
                var i = (level - 3) * perLevel + side * slotsPerSide + cell;
                if (i < doors.Length && doors[i] != null) doors[i].SetActive(true);
            }
        }
    }
}
