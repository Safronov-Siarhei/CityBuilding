using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The inventory (the user, 2026-10-25): a round button on the left edge, under the citizens
    /// button, opens a card with every resource the settlement can hold -- its picture, its name and
    /// how much there is (not the room: a storehouse's room is shared by its whole group, so a
    /// per-row «/ 300» would read as wrong) -- the food chain's middle links (wheat,
    /// flour, pork) included, which the resource bar along the top no longer shows: up there is
    /// only Food, the one food counter. A resource the town has none of is dimmed, not hidden, so
    /// the card does not jump about as things come and go.
    ///
    /// The rows are built by SetupProject (one per resource in `types`); this only fills them, on
    /// opening and whenever an amount changes while the card is up.
    /// </summary>
    public class InventoryPanelController : MonoBehaviour
    {
        private static readonly Color Dimmed = new Color(1f, 1f, 1f, 0.35f);

        // the card is parchment (the theme pass): white words vanished into it (the user,
        // 2026-10-31) -- the words are dark ink, an empty store's paler
        private static readonly Color Ink = new Color(0.27f, 0.16f, 0.07f, 1f);
        private static readonly Color InkDimmed = new Color(0.27f, 0.16f, 0.07f, 0.4f);

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private ResourceType[] types = new ResourceType[0];
        [SerializeField] private Image[] icons = new Image[0];
        [SerializeField] private Text[] names = new Text[0];
        [SerializeField] private Text[] amounts = new Text[0];
        [SerializeField] private ResourceIconLibrary iconLibrary;
        [SerializeField] private HudHider hud;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        /// <summary>The resources the card lists, in its order. For the tests.</summary>
        public ResourceType[] Types => types;

        // The controller sits on the card itself, so it listens exactly while the card is up.
        private void OnEnable()
        {
            if (ResourceManager.Instance != null) ResourceManager.Instance.OnResourceChanged += HandleResourceChanged;
        }

        private void OnDisable()
        {
            if (ResourceManager.Instance != null) ResourceManager.Instance.OnResourceChanged -= HandleResourceChanged;
        }

        public void OpenPanel()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            ModalGate.SetBlocked(true);
            // the buttons behind the card go while it is open (the user's rule, 2026-10-14)
            if (hud != null) hud.Hide(this);
            Refresh();
        }

        public void ClosePanel()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            ModalGate.SetBlocked(false);
            if (hud != null) hud.Show(this);
        }

        private void HandleResourceChanged(ResourceType type, int amount)
        {
            if (IsOpen) Refresh();
        }

        /// <summary>Every row: picture, name, amount. Cheap -- a dozen texts, and only while the card is up.</summary>
        public void Refresh()
        {
            var resources = ResourceManager.Instance;
            if (resources == null || !IsOpen) return;
            for (var i = 0; i < types.Length; i++)
            {
                var type = types[i];
                var amount = resources.GetAmount(type);
                var tint = amount > 0 ? Color.white : Dimmed;
                if (i < icons.Length && icons[i] != null)
                {
                    if (icons[i].sprite == null && iconLibrary != null) icons[i].sprite = iconLibrary.GetIcon(type);
                    icons[i].color = tint;
                }
                if (i < names.Length && names[i] != null)
                {
                    names[i].text = Capitalized(ResourceNames.Of(type));
                    names[i].color = amount > 0 ? Ink : InkDimmed;
                }
                if (i < amounts.Length && amounts[i] != null)
                {
                    amounts[i].text = amount.ToString();
                    amounts[i].color = amount > 0 ? Ink : InkDimmed;
                }
            }
        }

        private static string Capitalized(string name) =>
            string.IsNullOrEmpty(name) ? name : char.ToUpperInvariant(name[0]) + name.Substring(1);
    }
}
