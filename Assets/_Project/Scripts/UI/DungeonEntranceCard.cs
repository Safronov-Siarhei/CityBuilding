using CityBuilder.Core;
using CityBuilder.Dungeon;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The card of a dungeon's entrance (the user, 2026-10-31: the entrance shows its LEVEL when
    /// tapped): «Подземелье», «Уровень N», and «Войти» -- which takes the heroes down only when one
    /// of them stands at the mouth; otherwise it asks for a hero to be brought there. The way out of
    /// a dungeon (DungeonEntrance.IsExit) has the same card: «Выход», «Выйти» takes the party home.
    /// </summary>
    public class DungeonEntranceCard : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text title;
        [SerializeField] private Text body;
        [SerializeField] private Button enterButton;
        // the player's heroes, each «Доступен» by the mouth or «Далеко» (the user, 2026-11-01)
        [SerializeField] private GameObject heroRow;
        [SerializeField] private Image[] heroFaces = new Image[3];
        [SerializeField] private Text[] heroWords = new Text[3];

        private static readonly Combat.SoldierType[] HeroOrder = { Combat.SoldierType.King, Combat.SoldierType.Queen, Combat.SoldierType.Captain };

        public static DungeonEntranceCard Instance { get; private set; }

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        /// <summary>The entrance the card is open on.</summary>
        public DungeonEntrance Shown { get; private set; }

        private Text _label;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(DungeonEntrance entrance)
        {
            if (entrance == null || panelRoot == null) return;
            Shown = entrance;
            panelRoot.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            Shown = null;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        /// <summary>The «Войти» button: down with the heroes, if one is at the mouth.</summary>
        public void Enter()
        {
            if (Shown == null || !Shown.CanGo) return;
            var id = Shown.DungeonId;
            var level = Shown.Level;
            var exit = Shown.IsExit;
            var entrance = Shown;
            Close();
            if (BuildingFocus.Instance != null) BuildingFocus.Instance.Clear();
            if (exit) DungeonTravel.Leave();
            else DungeonTravel.Enter(id, level, entrance);
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
            var near = Shown.HeroNear();
            var key = Shown.IsExit ? "#dungeon_exit" : "#dungeon_entrance";
            if (title != null) title.text = Localization.Get(key + "_title");
            if (body != null)
                body.text = string.Format(Localization.Get("#dungeon_entrance_level"), Shown.Level) + "\n\n"
                    + Localization.Get(key + (near ? "_ready" : "_far"));
            RefreshHeroes();
            if (enterButton != null)
            {
                enterButton.interactable = Shown.CanGo;
                if (_label == null) _label = enterButton.GetComponentInChildren<Text>();
                var word = Localization.Get(key + "_enter");
                if (_label != null && _label.text != word) _label.text = word;
            }
        }

        /// <summary>A face per hero the player has: who goes down (by the mouth), who is too far, who is down.</summary>
        private void RefreshHeroes()
        {
            if (heroRow == null) return;
            var roster = Combat.HeroRoster.Instance;
            var show = !Shown.IsExit && roster != null && roster.Heroes.Count > 0;
            if (heroRow.activeSelf != show) heroRow.SetActive(show);
            if (!show) return;
            for (var i = 0; i < HeroOrder.Length && i < heroFaces.Length; i++)
            {
                var hero = roster.HeroOf(HeroOrder[i]);
                if (heroFaces[i] == null) continue;
                heroFaces[i].gameObject.SetActive(hero != null);
                if (hero == null) continue;
                var near = Shown.IsNear(hero);
                heroFaces[i].color = near ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f);
                if (i < heroWords.Length && heroWords[i] != null)
                    heroWords[i].text = Localization.Get(hero.IsFallen ? "#dungeon_hero_fallen" : near ? "#dungeon_hero_ready" : "#dungeon_hero_far");
            }
        }
    }
}
