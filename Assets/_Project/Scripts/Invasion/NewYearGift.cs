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
    /// <summary>A gift lying on the island (InvasionNewYear): taken by whoever of the island walks up to it, or carried off by a Thief.</summary>
    public class NewYearGift : MonoBehaviour
    {
        private const float CheckSeconds = 0.2f;

        public ResourceType Kind { get; set; }

        /// <summary>The Thief carrying it off, or null while it lies there.</summary>
        public GiftThief Carrier { get; set; }

        private float _check;
        private float _phase;

        private void Start() => _phase = Random.Range(0f, 6f);

        private void Update()
        {
            if (Carrier != null) return;
            // a slow turn and a bob, so it reads as something to go and pick up
            _phase += Time.deltaTime;
            transform.rotation = Quaternion.Euler(0f, _phase * 40f, 0f);

            _check -= Time.deltaTime;
            if (_check > 0f) return;
            _check = CheckSeconds;
            if (SomeoneOfTheIslandHere()) InvasionNewYear.Instance?.Collect(this);
        }

        private bool SomeoneOfTheIslandHere()
        {
            var reach = BalanceConfig.Instance.NyGiftPickupMeters;
            var reachSq = reach * reach;
            var at = transform.position;
            bool Near(Vector3 p) => (new Vector3(p.x - at.x, 0f, p.z - at.z)).sqrMagnitude <= reachSq;
            foreach (var soldier in SoldierUnit.All)
            {
                if (soldier != null && soldier.CurrentHealth > 0 && soldier.gameObject.activeInHierarchy && Near(soldier.transform.position)) return true;
            }
            var people = CitizenVisualsManager.Instance;
            if (people != null)
            {
                foreach (var man in people.AllAgents)
                {
                    if (man != null && man.gameObject.activeInHierarchy && Near(man.transform.position)) return true;
                }
            }
            return false;
        }
    }
}
