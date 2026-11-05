using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// A Workshop machine's moving parts (Tools/blender/siege_drafts.py; the user, 2026-11-05):
    /// on every blow the catapult's Arm throws over and is wound back, the ballista's Head kicks
    /// with the shot, the ram's Log swings back and drives forward. The siege tower's Bridge is
    /// let down while the tower stands docked at a wall (Docked). Parts the model lacks are skipped.
    /// </summary>
    public class SiegeMachine : MonoBehaviour
    {
        private const float ThrowSeconds = 0.18f;
        private const float RecoverSeconds = 1.6f;

        private Transform _part;
        private Quaternion _rest;
        private SoldierType _type;
        private SoldierUnit _unit;
        private float _since = 99f;

        /// <summary>The siege tower stands at a wall with its bridge down.</summary>
        public bool Docked { get; set; }

        public void Bind(Transform figure, SoldierType type)
        {
            _type = type;
            var name = type == SoldierType.Catapult ? "Arm" : type == SoldierType.Ballista ? "Head" : type == SoldierType.Ram ? "Log" : "Bridge";
            foreach (var t in figure.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.EndsWith("-" + name)) continue;
                _part = t;
                _rest = t.localRotation;
                break;
            }
            _unit = GetComponent<SoldierUnit>();
        }

        private void Start()
        {
            if (_unit == null) _unit = GetComponent<SoldierUnit>();
            if (_unit != null) _unit.Struck += OnStruck;
        }

        private void OnDestroy()
        {
            if (_unit != null) _unit.Struck -= OnStruck;
        }

        private void OnStruck() => _since = 0f;

        private void Update()
        {
            if (_part == null) return;
            if (_type == SoldierType.SiegeTower)
            {
                var target = Docked ? _rest * Quaternion.Euler(90f, 0f, 0f) : _rest;
                _part.localRotation = Quaternion.RotateTowards(_part.localRotation, target, 120f * Time.deltaTime);
                return;
            }
            if (_since > ThrowSeconds + RecoverSeconds) return;
            _since += Time.deltaTime;
            // 0 at rest, 1 at the end of the throw, then wound back
            var k = _since < ThrowSeconds ? _since / ThrowSeconds : 1f - Mathf.Clamp01((_since - ThrowSeconds) / RecoverSeconds);
            float angle;
            switch (_type)
            {
                case SoldierType.Catapult: angle = -100f * k; break;              // over the stop bar, towards the front
                case SoldierType.Ram: angle = _since < ThrowSeconds ? -28f * k : 18f * Mathf.Sin(k * Mathf.PI); break;   // drives, then swings back
                default: angle = 8f * k; break;                                  // the ballista's kick
            }
            _part.localRotation = _rest * Quaternion.Euler(angle, 0f, 0f);
        }
    }
}
