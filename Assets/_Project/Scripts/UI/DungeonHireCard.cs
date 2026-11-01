using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Dungeon;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The card of the dungeon's hire camp (the user, 2026-11-01): the three gnomes
    /// (DungeonHirelings.Offered), each with his health, damage and a «Нанять: N» button, and the
    /// party's count «N/5». A button works only with a hero at the camp, room in the party and the
    /// coins on the bar.
    /// </summary>
    public class DungeonHireCard : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text title;
        [SerializeField] private Text count;
        [SerializeField] private Text[] names = new Text[3];
        [SerializeField] private Text[] stats = new Text[3];
        [SerializeField] private Button[] hireButtons = new Button[3];

        public static DungeonHireCard Instance { get; private set; }

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        /// <summary>The camp the card is open on.</summary>
        public DungeonEntrance Shown { get; private set; }

        private Text[] _labels;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(DungeonEntrance camp)
        {
            if (camp == null || panelRoot == null) return;
            Shown = camp;
            panelRoot.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            Shown = null;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        /// <summary>A «Нанять» button: the gnome at `index` in DungeonHirelings.Offered.</summary>
        public void Hire(int index)
        {
            var party = DungeonHirelings.Instance;
            if (Shown == null || party == null || !Shown.HeroNear() || index < 0 || index >= DungeonHirelings.Offered.Length) return;
            party.Hire(DungeonHirelings.Offered[index]);
            Refresh();
        }

        private void Update()
        {
            if (IsOpen) Refresh();
        }

        private void Refresh()
        {
            if (Shown == null)
            {
                Close();
                return;
            }
            var party = DungeonHirelings.Instance;
            var near = Shown.HeroNear();
            if (title != null) title.text = Localization.Get("#dungeon_hire_title");
            if (count != null)
                count.text = near
                    ? string.Format(Localization.Get("#dungeon_hire_count"), party != null ? party.Count : 0, DungeonHirelings.Max)
                    : Localization.Get("#dungeon_hire_far");
            _labels ??= new Text[hireButtons.Length];
            for (var i = 0; i < DungeonHirelings.Offered.Length && i < hireButtons.Length; i++)
            {
                var type = DungeonHirelings.Offered[i];
                var row = SoldierStats.StatsAt(type, 1);
                if (i < names.Length && names[i] != null) names[i].text = SoldierStats.DisplayName(type);
                if (i < stats.Length && stats[i] != null) stats[i].text = string.Format(Localization.Get("#dungeon_hire_stats"), row.maxHealth, row.attackDamage);
                var button = hireButtons[i];
                if (button == null) continue;
                button.interactable = near && party != null && party.CanHire(type);
                if (_labels[i] == null) _labels[i] = button.GetComponentInChildren<Text>();
                var word = string.Format(Localization.Get("#dungeon_hire_buy"), DungeonHirelings.Price(type));
                if (_labels[i] != null && _labels[i].text != word) _labels[i].text = word;
            }
        }
    }
}
