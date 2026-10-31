using CityBuilder.Core;
using CityBuilder.Dungeon;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The card of a dungeon's entrance (the user, 2026-10-31: the entrance shows its LEVEL when
    /// tapped): «Подземелье», «Уровень N», and «Войти» -- which takes the heroes down only when one
    /// of them stands at the mouth; otherwise it asks for a hero to be brought there.
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
            if (Shown == null || !Shown.HeroNear() || !DungeonTravel.CanEnter) return;
            var id = Shown.DungeonId;
            Close();
            DungeonTravel.Enter(id);
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
            if (title != null) title.text = Localization.Get("#dungeon_entrance_title");
            if (body != null)
                body.text = string.Format(Localization.Get("#dungeon_entrance_level"), Shown.Level) + "\n\n"
                    + Localization.Get(near ? "#dungeon_entrance_ready" : "#dungeon_entrance_far");
            if (enterButton != null) enterButton.interactable = near && DungeonTravel.CanEnter;
        }
    }
}
