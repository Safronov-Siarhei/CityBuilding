using CityBuilder.Combat;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The card of a building that is not the player's -- an orc camp's tower, tent, totem, the
    /// orcs' shipyard (the user, 2026-10-31: a tapped foreign building is outlined in red like the
    /// player's own, and its «!» shows what it is). What it is, whose, its level and health, and
    /// for one that shoots its damage and reach. Read only: nothing to do with it but look.
    /// </summary>
    public class ForeignBuildingCard : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text title;
        [SerializeField] private Text body;

        public static ForeignBuildingCard Instance { get; private set; }

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private OrcCampBuilding _shown;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(OrcCampBuilding building)
        {
            if (building == null || panelRoot == null) return;
            _shown = building;
            panelRoot.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            _shown = null;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (_shown == null || _shown.CurrentHealth <= 0)
            {
                Close();
                return;
            }
            Refresh();
        }

        private void Refresh()
        {
            if (title != null) title.text = NameOf(_shown);
            if (body == null) return;
            var text = $"{Localization.Get("#foreign_owner")}: {Localization.Get("#foreign_owner_orcs")}\n"
                + $"{Localization.Get("#foreign_level")}: {_shown.Level}\n"
                + $"{Localization.Get("#foreign_health")}: {_shown.CurrentHealth} / {_shown.MaxHealth}";
            if (_shown.ShotDamage > 0)
            {
                text += $"\n{Localization.Get("#foreign_damage")}: {_shown.ShotDamage}"
                    + $"\n{Localization.Get("#foreign_range")}: {_shown.ShotRange:0.#}";
            }
            body.text = text;
        }

        /// <summary>The building's name in the player's language («Орочья сторожевая башня»...).</summary>
        public static string NameOf(OrcCampBuilding building) =>
            building == null ? string.Empty : Localization.Get("#orc_building_" + building.Kind.ToString().ToLowerInvariant());
    }
}
