using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Invasion
{
    /// <summary>The Thief's errand: to the nearest free gift, then home to its portal with it. Killed on the way, it drops it.</summary>
    public class GiftThief : MonoBehaviour
    {
        private InvasionNewYear _rules;
        private OrcUnit _orc;
        private Vector3 _home;
        private NewYearGift _target;
        private NewYearGift _carried;
        private bool _gotHome;

        public NewYearGift Carried => _carried;

        public void Begin(InvasionNewYear rules, Vector3 home)
        {
            _rules = rules;
            _home = home;
            _orc = GetComponent<OrcUnit>();
            if (_orc != null) _orc.Steered = true;
        }

        private void Update()
        {
            if (_orc == null || _rules == null) return;
            if (_carried == null)
            {
                if (_target == null || _target.Carrier != null) _target = _rules.NearestFreeGift(transform.position);
                if (_target == null)
                {
                    GoHome();
                    return;
                }
                if (!_orc.SteerTo(_target.transform.position))
                {
                    _carried = _target;
                    _carried.Carrier = this;
                    _target = null;
                }
                return;
            }
            GoHome();
        }

        private void LateUpdate()
        {
            // the gift rides on its back -- not as its child, or the corpse would take it down
            if (_carried != null) _carried.transform.position = transform.position + Vector3.up * 0.55f + transform.forward * -0.15f;
        }

        private void GoHome()
        {
            if (_orc.SteerTo(_home)) return;
            _gotHome = true;
            if (_carried != null) _rules.Lose(_carried);
            _carried = null;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_gotHome || _carried == null) return;
            // killed on the way: the gift falls where he did, free for the taking
            var at = transform.position;
            if (NavMesh.SamplePosition(at, out var hit, 2f, NavMesh.AllAreas)) at = hit.position;
            _carried.transform.position = at;
            _carried.Carrier = null;
        }
    }
}
