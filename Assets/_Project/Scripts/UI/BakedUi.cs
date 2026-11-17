using CityBuilder.Diplomacy;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// A window built from baked art (a Tools/ui/*_game_art.py folder: PNG pieces + layout.json, placed by
    /// SetupProjectDiplomacy.BuildBaked): its pieces by their layout names, so a controller finds "Offer3Swap"
    /// without a serialized field per piece. `root` is what shows and hides.
    /// </summary>
    public class BakedUi : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private string[] names = new string[0];
        [SerializeField] private GameObject[] objects = new GameObject[0];
        [SerializeField] private ResourceIconLibrary icons;
        [SerializeField] private Sprite crystal;

        public GameObject Root => root;
        public bool IsOpen => root != null && root.activeSelf;

        public void EditorSet(GameObject windowRoot, string[] pieceNames, GameObject[] pieces, ResourceIconLibrary iconLibrary, Sprite crystalIcon)
        {
            root = windowRoot;
            names = pieceNames;
            objects = pieces;
            icons = iconLibrary;
            crystal = crystalIcon;
        }

        public GameObject Get(string name)
        {
            for (var i = 0; i < names.Length; i++)
                if (names[i] == name) return objects[i];
            return null;
        }

        public T Get<T>(string name) where T : Component
        {
            var go = Get(name);
            return go != null ? go.GetComponent<T>() : null;
        }

        public Button Button(string name) => Get<Button>(name);

        /// <summary>A tap on piece `name` (a Button the builder made) calls `action`.</summary>
        public void OnTap(string name, UnityEngine.Events.UnityAction action)
        {
            var button = Button(name);
            if (button != null) button.onClick.AddListener(action);
        }

        /// <summary>The code's own words: the sheet key the builder put on it stops re-reading over them.</summary>
        public void SetText(string name, string value)
        {
            var text = Get<Text>(name);
            if (text == null) return;
            var localized = text.GetComponent<LocalizedText>();
            if (localized != null) localized.enabled = false;
            if (text.text != value) text.text = value;
        }

        public void Show(string name, bool on)
        {
            var go = Get(name);
            if (go != null && go.activeSelf != on) go.SetActive(on);
        }

        public void Interactable(string name, bool on)
        {
            var button = Button(name);
            if (button != null && button.interactable != on) button.interactable = on;
        }

        /// <summary>An empty slot gets a resource's icon (a ResourceType name or Crystals).</summary>
        public void SetIcon(string name, string resource)
        {
            var image = Get<Image>(name);
            if (image == null) return;
            Sprite sprite = null;
            if (DiplomacyRules.IsCrystals(resource)) sprite = crystal;
            else if (icons != null && DiplomacyRules.TryParse(resource, out var type)) sprite = icons.GetIcon(type);
            image.sprite = sprite;
            image.color = sprite != null ? Color.white : Color.clear;
        }

        public void Open()
        {
            if (root == null) return;
            root.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }
    }
}
