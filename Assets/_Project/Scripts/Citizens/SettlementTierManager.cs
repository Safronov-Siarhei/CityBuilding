using System;
using UnityEngine;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// Names the settlement by population tier (K&amp;C-style: "Год 1: Поселение выросло. Теперь
    /// это Тихая гавань") and fires OnTierChanged the moment population growth crosses into a new
    /// tier -- see SettlementTierToastController for the on-screen toast. Purely presentational,
    /// no gameplay effect. Tiers only ever advance, never regress: a future population dip
    /// (starvation, raids) shouldn't flicker the settlement's name back down.
    /// </summary>
    public class SettlementTierManager : MonoBehaviour
    {
        public static SettlementTierManager Instance { get; private set; }

        // Localization keys, not names: the tier's text lives in the sheet under #tier_<code>.
        private static readonly (int minPopulation, string key)[] Tiers =
        {
            (0, "#tier_hamlet"),
            (20, "#tier_village"),
            (50, "#tier_town"),
            (100, "#tier_city"),
            (200, "#tier_kingdom"),
        };

        [SerializeField] private CitizenManager citizenManager;

        private int _currentTierIndex = -1;

        public string CurrentTierName => Core.Localization.Get(Tiers[Mathf.Max(0, _currentTierIndex)].key);

        /// <summary>Fired with the new tier's name only when population growth actually crosses into it (not on the initial silent resolve at scene load).</summary>
        public event Action<string> OnTierChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (citizenManager != null) citizenManager.OnPopulationChanged += HandlePopulationChanged;
            HandlePopulationChanged();
        }

        private void HandlePopulationChanged()
        {
            var population = citizenManager != null ? citizenManager.TotalPopulation : 0;
            var resolvedIndex = ResolveTierIndex(population);

            if (_currentTierIndex < 0)
            {
                // Establishes the starting tier (e.g. a loaded save already past Хутор) without
                // toasting -- there was no "growth" the player just watched happen.
                _currentTierIndex = resolvedIndex;
                return;
            }

            if (resolvedIndex <= _currentTierIndex) return;

            _currentTierIndex = resolvedIndex;
            OnTierChanged?.Invoke(Core.Localization.Get(Tiers[resolvedIndex].key));
        }

        /// <summary>Public so EditMode tests can cover the tier-boundary math directly.</summary>
        public static int ResolveTierIndex(int population)
        {
            var index = 0;
            for (var i = 0; i < Tiers.Length; i++)
            {
                if (population >= Tiers[i].minPopulation) index = i;
            }
            return index;
        }
    }
}
