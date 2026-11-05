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
        private Buildings.BuildingInstance _bot;
        // a tapped fighter, any side (the user, 2026-11-05: «нажатие на любого воина должно показывать информацию»)
        private SoldierUnit _soldier;
        private OrcUnit _orc;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>A soldier of any side's: the player's, a hero, the bot kingdom's garrison.</summary>
        public void Show(SoldierUnit soldier)
        {
            if (soldier == null || panelRoot == null) return;
            _shown = null;
            _bot = null;
            _orc = null;
            _soldier = soldier;
            panelRoot.SetActive(true);
            Refresh();
        }

        /// <summary>An enemy fighter: an orc, a monster, the bot kingdom's soldier at war.</summary>
        public void Show(OrcUnit orc)
        {
            if (orc == null || panelRoot == null) return;
            _shown = null;
            _bot = null;
            _soldier = null;
            _orc = orc;
            panelRoot.SetActive(true);
            Refresh();
        }

        public void Show(OrcCampBuilding building)
        {
            if (building == null || panelRoot == null) return;
            _soldier = null;
            _orc = null;
            _bot = null;
            _shown = building;
            panelRoot.SetActive(true);
            Refresh();
        }

        /// <summary>A building of the bot kingdom (BuildingInstance.IsBot): whose, its level and health.</summary>
        public void Show(Buildings.BuildingInstance building)
        {
            if (building == null || panelRoot == null) return;
            _shown = null;
            _soldier = null;
            _orc = null;
            _bot = building;
            panelRoot.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            _shown = null;
            _bot = null;
            _soldier = null;
            _orc = null;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (_soldier != null || _orc != null)
            {
                // the fighter fell or is gone: the card with him
                if ((_soldier != null && (_soldier.CurrentHealth <= 0 || !_soldier.enabled)) || (_orc != null && _orc.CurrentHealth <= 0)
                    || (_soldier == null && _orc == null))
                {
                    Close();
                    return;
                }
                Refresh();
                return;
            }
            if (_bot != null)
            {
                Refresh();
                return;
            }
            if (_shown == null || _shown.CurrentHealth <= 0)
            {
                Close();
                return;
            }
            Refresh();
        }

        private void Refresh()
        {
            if (_soldier != null)
            {
                var hero = _soldier.GetComponent<Hero>();
                var level = hero != null ? hero.Level : Mathf.Max(1, _soldier.LookLevel);
                var owner = Bot.BotGarrison.Owns(_soldier) ? "#foreign_owner_bot" : "#foreign_owner_player";
                if (title != null) title.text = SoldierStats.DisplayName(_soldier.Type);
                if (body != null)
                    body.text = $"{Localization.Get("#foreign_owner")}: {Localization.Get(owner)}\n"
                        + $"{Localization.Get("#foreign_level")}: {level}\n"
                        + $"{Localization.Get("#unit_card_health")}: {_soldier.CurrentHealth} / {_soldier.MaxHealth}\n"
                        + $"{Localization.Get("#foreign_damage")}: {_soldier.AttackDamage}";
                return;
            }
            if (_orc != null)
            {
                var id = OrcUnit.SheetIdOf(_orc.Kind);
                var owner = _orc.BotSide ? "#foreign_owner_bot" : OrcUnit.IsMonster(_orc.Kind) ? "#foreign_owner_monsters" : "#foreign_owner_orcs";
                if (title != null) title.text = OrcName(_orc);
                if (body != null)
                    body.text = $"{Localization.Get("#foreign_owner")}: {Localization.Get(owner)}\n"
                        + $"{Localization.Get("#foreign_level")}: {_orc.Level}\n"
                        + $"{Localization.Get("#unit_card_health")}: {_orc.CurrentHealth} / {_orc.MaxHealth}\n"
                        + $"{Localization.Get("#foreign_damage")}: {_orc.AttackDamage}";
                return;
            }
            if (_bot != null)
            {
                if (title != null) title.text = _bot.Data != null ? _bot.Data.LocalizedName : string.Empty;
                if (body != null)
                    body.text = $"{Localization.Get("#foreign_owner")}: {Localization.Get("#foreign_owner_bot")}\n"
                        + $"{Localization.Get("#foreign_level")}: {_bot.Level}\n"
                        + $"{Localization.Get("#foreign_health")}: {_bot.CurrentHealth} / {_bot.MaxHealth}";
                return;
            }
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

        /// <summary>An enemy fighter's name: its monster row's, else its units row's, else its id.</summary>
        public static string OrcName(OrcUnit orc)
        {
            var id = OrcUnit.SheetIdOf(orc.Kind);
            var unit = BalanceConfig.Instance != null ? BalanceConfig.Instance.Unit(id) : null;
            var fallback = unit != null && !string.IsNullOrEmpty(unit.displayName) ? unit.displayName : id;
            return Localization.GetOrDefault("#monster_" + id, Localization.GetOrDefault("#unit_" + id, fallback));
        }

        /// <summary>The building's name in the player's language («Орочья сторожевая башня»...).</summary>
        public static string NameOf(OrcCampBuilding building) =>
            building == null ? string.Empty : Localization.Get("#orc_building_" + building.Kind.ToString().ToLowerInvariant());
    }
}
