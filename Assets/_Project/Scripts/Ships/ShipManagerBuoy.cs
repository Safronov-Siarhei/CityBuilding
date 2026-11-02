using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Ships
{
    /// <summary>
    /// The marker of a ship's move order (the user, 2026-11-02: «буй», a flag in the water looked
    /// poor): a buoy (Tools/blender/orderbuoy.py) set on the water where the ship will stop, bobbing
    /// on the swell; it sinks away once the ship is there or sent elsewhere. One buoy -- the latest
    /// order's.
    /// </summary>
    public partial class ShipManager
    {
        [SerializeField] private GameObject orderBuoyModel;

        private const float BuoyDropSeconds = 0.35f;
        private const float BuoySinkSeconds = 0.6f;
        private const float BuoyBobMetres = 0.06f;
        private const float BuoyTiltDegrees = 6f;

        private Transform _buoy;
        private ShipUnit _buoyShip;
        private Vector3 _buoyAt;
        private float _buoyAge;
        private float _buoySinking = -1f;

        /// <summary>Sets the buoy where this ship's order ends.</summary>
        public void ShowBuoy(ShipUnit ship)
        {
            if (ship == null || orderBuoyModel == null || !ship.IsMoving) return;
            if (_buoy == null)
            {
                _buoy = Instantiate(orderBuoyModel, transform).transform;
                _buoy.name = "OrderBuoy";
                foreach (var r in _buoy.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            }
            _buoyShip = ship;
            _buoyAt = new Vector3(ship.Destination.x, WaterLevel, ship.Destination.z);
            _buoyAge = 0f;
            _buoySinking = -1f;
            _buoy.gameObject.SetActive(true);
        }

        private void TickBuoy()
        {
            if (_buoy == null || !_buoy.gameObject.activeSelf) return;
            var t = Time.deltaTime;
            _buoyAge += t;
            // the ship there, sunk, or sent somewhere else: the buoy goes down
            if (_buoySinking < 0f && (_buoyShip == null || !_buoyShip.IsAlive || !_buoyShip.IsMoving
                || (_buoyShip.Destination - _buoyAt).sqrMagnitude > 1f))
            {
                _buoySinking = 0f;
            }
            var drop = 0f;
            if (_buoySinking >= 0f)
            {
                _buoySinking += t;
                if (_buoySinking >= BuoySinkSeconds)
                {
                    _buoy.gameObject.SetActive(false);
                    return;
                }
                drop = -0.9f * (_buoySinking / BuoySinkSeconds);
            }
            else if (_buoyAge < BuoyDropSeconds)
            {
                // set down from a little above, with a splash of a bounce
                var k = _buoyAge / BuoyDropSeconds;
                drop = 0.6f * (1f - k) * (1f - k);
            }
            var bob = Mathf.Sin(_buoyAge * 2.1f) * BuoyBobMetres;
            _buoy.position = _buoyAt + Vector3.up * (bob + drop);
            _buoy.rotation = Quaternion.Euler(Mathf.Sin(_buoyAge * 1.7f) * BuoyTiltDegrees, _buoyAge * 8f, Mathf.Cos(_buoyAge * 1.3f) * BuoyTiltDegrees);
        }
    }
}
