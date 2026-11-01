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
            Close();
            if (BuildingFocus.Instance != null) BuildingFocus.Instance.Clear();
            if (exit) DungeonTravel.Leave();
            else DungeonTravel.Enter(id, level);
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
            if (enterButton != null)
            {
                enterButton.interactable = Shown.CanGo;
                if (_label == null) _label = enterButton.GetComponentInChildren<Text>();
                var word = Localization.Get(key + "_enter");
                if (_label != null && _label.text != word) _label.text = word;
            }
        }
    }
}
