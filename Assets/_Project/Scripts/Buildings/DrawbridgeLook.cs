using CityBuilder.Combat;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// The Drawbridge's two leaves (Tools/blender/crossings.py, Drawbridge-Leaf-lvl1..5), one per
    /// cell, each on a pivot at its outer hinge: they rise while an orc is within
    /// `drawbridge_raise_radius_cells` and come down when none is. The look only -- the way itself is
    /// the Gate NavMesh area (allies pass, enemies break it), whatever the leaves are doing.
    /// </summary>
    public class DrawbridgeLook : MonoBehaviour
    {
        // per level 1..5, two pivots: (level - 1) * 2 + side
        [SerializeField] private Transform[] pivots = new Transform[0];
        [SerializeField] private Quaternion[] rest = new Quaternion[0];

        private const float CheckSeconds = 0.25f;
        private BuildingInstance _building;
        private float _raised;
        private float _checkIn;
        private bool _wantUp;
        private int _shownLevel = -1;

        public void Wire(Transform[] levelPivots, Quaternion[] restRotations)
        {
            pivots = levelPivots;
            rest = restRotations;
        }

        private void Update()
        {
            if (_building == null) _building = GetComponent<BuildingInstance>();
            var level = _building != null ? Mathf.Clamp(_building.Level, 1, 5) : 1;
            if (level != _shownLevel)
            {
                _shownLevel = level;
                for (var i = 0; i < pivots.Length; i++)
                    if (pivots[i] != null) pivots[i].gameObject.SetActive(i / 2 == level - 1);
            }

            _checkIn -= Time.deltaTime;
            if (_checkIn <= 0f)
            {
                _checkIn = CheckSeconds;
                _wantUp = EnemyNear();
            }
            var config = BalanceConfig.Instance;
            var seconds = config != null ? config.DrawbridgeRaiseSeconds : 1f;
            var target = _wantUp ? 1f : 0f;
            if (Mathf.Approximately(_raised, target)) return;
            _raised = Mathf.MoveTowards(_raised, target, Time.deltaTime / Mathf.Max(0.05f, seconds));
            var angle = (config != null ? config.DrawbridgeRaiseDegrees : 70f) * Mathf.SmoothStep(0f, 1f, _raised);
            for (var side = 0; side < 2; side++)
            {
                var i = (level - 1) * 2 + side;
                if (i < pivots.Length && pivots[i] != null) pivots[i].localRotation = rest[i] * Quaternion.Euler(-angle, 0f, 0f);
            }
        }

        private bool EnemyNear()
        {
            var config = BalanceConfig.Instance;
            var radius = config != null ? config.DrawbridgeRaiseRadiusCells : 0f;
            var sq = radius * radius;
            var at = transform.position;
            var orcs = OrcUnit.All;
            for (var i = 0; i < orcs.Count; i++)
            {
                var orc = orcs[i];
                if (orc == null) continue;
                var d = orc.transform.position - at;
                d.y = 0f;
                if (d.sqrMagnitude <= sq) return true;
            }
            return false;
        }
    }
}
