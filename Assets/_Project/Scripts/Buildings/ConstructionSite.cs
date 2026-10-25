using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// A building going up, or going up a level, on a timer (the user, 2026-10-22 for an Invasion
    /// run, 2026-10-25 for the sandbox too; no speeding up): while the site stands the building
    /// does not work at all -- a tower does not shoot, a yard's workers wait at it, a barracks hires
    /// nobody, a house makes no one, nothing is upgraded again -- and a hammer with the time left
    /// stands over it (UI.ConstructionLabels). When the time is up the site finishes (an upgrade's
    /// level is applied only then) and removes itself.
    ///
    /// The seconds are the sheet's build_sec columns, per building and level (BuildingData.BuildSeconds).
    /// </summary>
    public class ConstructionSite : MonoBehaviour
    {
        private static readonly List<ConstructionSite> _all = new List<ConstructionSite>();

        /// <summary>Every site standing, for the hammer labels.</summary>
        public static IReadOnlyList<ConstructionSite> All => _all;

        /// <summary>Raised when a building is done -- built, or its new level in place -- and works again.</summary>
        public static event Action<BuildingInstance> Finished;

        public BuildingInstance Building { get; private set; }
        public float Total { get; private set; }
        public float Remaining { get; private set; }

        /// <summary>Going up a level (rather than being built).</summary>
        public bool IsUpgrade { get; private set; }

        public float Progress => Total > 0f ? 1f - Remaining / Total : 1f;

        private Action _onDone;

        /// <summary>
        /// Puts the building under construction for `seconds`; `onDone` runs as it finishes (an
        /// upgrade's level-up). `total`, when larger, is the whole time -- a save putting a half-built
        /// one back.
        /// </summary>
        public static ConstructionSite Begin(BuildingInstance building, float seconds, bool upgrade, Action onDone = null, float total = 0f)
        {
            if (building == null) return null;
            if (seconds <= 0f)
            {
                onDone?.Invoke();
                return null;
            }
            var site = building.gameObject.AddComponent<ConstructionSite>();
            site.Building = building;
            site.Remaining = seconds;
            site.Total = Mathf.Max(seconds, total);
            site.IsUpgrade = upgrade;
            site._onDone = onDone;
            building.Site = site;
            return site;
        }

        private void OnEnable() => _all.Add(this);

        private void OnDisable() => _all.Remove(this);

        private void Update()
        {
            Remaining -= Time.deltaTime;
            if (Remaining > 0f) return;
            Finish();
        }

        /// <summary>Done now. Public for the tests, which do not wait out the clock.</summary>
        public void Finish()
        {
            Remaining = 0f;
            var done = _onDone;
            _onDone = null;
            // gone first, so the building counts as working when its listeners hear of it
            _all.Remove(this);
            if (Building != null && Building.Site == this) Building.Site = null;
            enabled = false;
            Destroy(this);
            done?.Invoke();
            Finished?.Invoke(Building);
        }
    }
}
