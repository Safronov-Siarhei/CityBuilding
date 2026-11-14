using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// An orc torchbearer's errand (sandbox fires v2, the user 2026-11-14, variant A): run to the nearest
    /// burnable building not yet burning, stand at it torch_light_sec setting it alight (he can be
    /// killed meanwhile -- the fire never starts), then on to the next. With nothing left to burn he
    /// fights like any raider.
    /// </summary>
    public class TorchErrand : MonoBehaviour
    {
        private OrcUnit _orc;
        private BuildingInstance _target;
        private Vector3 _goal;
        private float _lighting = -1f;
        private float _lookIn;

        private void Start()
        {
            _orc = GetComponent<OrcUnit>();
            if (_orc != null) _orc.Steered = true;
        }

        private void Update()
        {
            if (_orc == null) return;
            if (_target == null || _target.CurrentHealth <= 0 || _target.GetComponent<BuildingFire>() != null || !FireRules.Burns(_target))
            {
                _lighting = -1f;
                _lookIn -= Time.deltaTime;
                if (_lookIn > 0f) return;
                _lookIn = 1f;
                _target = Nearest();
                if (_target == null)
                {
                    // nothing left to burn: a raider like the rest
                    _orc.Steered = false;
                    enabled = false;
                    return;
                }
                var visuals = CitizenVisualsManager.Instance;
                _goal = visuals != null ? visuals.DoorOf(_target) : _target.transform.position;
            }
            if (_lighting < 0f)
            {
                if (_orc.SteerTo(_goal)) return;
                _lighting = BalanceConfig.Instance.TorchLightSeconds;
                return;
            }
            _lighting -= Time.deltaTime;
            if (_lighting > 0f) return;
            if (FireManager.Ignite(_target, "#log_fire_torch") == null)
            {
                // it would not take (the town's first minutes, the banner's shield): he fights instead
                _orc.Steered = false;
                enabled = false;
                return;
            }
            _target = null;
            _lighting = -1f;
        }

        private BuildingInstance Nearest()
        {
            BuildingInstance best = null;
            var bestD = float.MaxValue;
            foreach (var b in BuildingInstance.All)
            {
                if (!FireRules.Burns(b) || b.GetComponent<BuildingFire>() != null || b.Data.IsGroundWork) continue;
                var d = (b.transform.position - transform.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = b; }
            }
            return best;
        }
    }
}
