using System.Collections.Generic;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// A tap on a top-bar chip (the user, 2026-11-20; concept Design/ui/Concept_ResourceInfo.png, approved): a parchment
    /// note under it. Wood, stone, coins, crystals: gained and spent over the last game day and the net (ResourceFlow
    /// measures it -- walkers carry wood, a fixed rate would lie); food: what the food buildings give a day and what the
    /// people eat, the net, how many days the stock lasts; people: free hands, free places in the houses, the mood.
    /// Built in code under the HUD's canvas; any tap closes it; it refreshes twice a second while up.
    /// </summary>
    public class ResourceInfoPopup : MonoBehaviour
    {
        public const int PeopleKey = -2;
        private const float Width = 500f;
        private const float RowHeight = 42f;
        private const int MaxRows = 6;

        private RectTransform _canvas;
        private GameObject _root;
        private RectTransform _panel;
        private Image _icon;
        private Text _title;
        private Text _store;
        private float _openFor;
        private readonly List<(Text left, Text right)> _rows = new List<(Text, Text)>();
        private Text _note;
        private int _key = int.MinValue;
        private RectTransform _chip;
        private float _refreshIn;

        public static ResourceInfoPopup Instance { get; private set; }

        /// <summary>Opens the note for this chip (a ResourceType, ResourceFlow.CrystalsKey or PeopleKey), or closes it when it is that one's already.</summary>
        public static void Toggle(Canvas canvas, int key, RectTransform chip, Sprite icon)
        {
            if (canvas == null || chip == null) return;
            if (Instance == null)
            {
                var go = new GameObject("ResourceInfoPopup", typeof(RectTransform));
                go.transform.SetParent(canvas.transform, false);
                Instance = go.AddComponent<ResourceInfoPopup>();
                Instance.Build((RectTransform)canvas.transform);
            }
            if (Instance._root.activeSelf && Instance._key == key)
            {
                Instance.Close();
                return;
            }
            Instance.Show(key, chip, icon);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Build(RectTransform canvas)
        {
            _canvas = canvas;
            var self = (RectTransform)transform;
            Stretch(self);
            _root = new GameObject("Root", typeof(RectTransform));
            _root.transform.SetParent(transform, false);
            Stretch((RectTransform)_root.transform);
            // any tap closes it (the blocker under the panel, see-through)
            var blocker = NewImage(_root.transform, "Blocker", new Color(0f, 0f, 0f, 0f));
            Stretch(blocker.rectTransform);
            blocker.rectTransform.offsetMin = new Vector2(-50f, -50f);
            blocker.rectTransform.offsetMax = new Vector2(50f, 50f);
            var button = blocker.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(Close);

            var panel = NewImage(_root.transform, "Panel", Color.white);
            panel.sprite = UnityEngine.Resources.Load<Sprite>($"{HudSkins.ResourcesFolder}/{HudSkins.Current}/panel")
                           ?? UnityEngine.Resources.Load<Sprite>($"{HudSkins.ResourcesFolder}/{HudSkin.Classic}/panel");
            if (panel.sprite != null && panel.sprite.border.sqrMagnitude > 0f) panel.type = Image.Type.Sliced;
            _panel = panel.rectTransform;
            _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0.5f, 1f);

            _icon = NewImage(_panel, "Icon", Color.white);
            _icon.preserveAspect = true;
            _icon.raycastTarget = false;
            Place(_icon.rectTransform, 30f, -26f, 46f, 46f);
            _title = NewText(_panel, "Title", 32, UiTheme.Ink, TextAnchor.MiddleLeft, UiTheme.Heavy);
            Place(_title.rectTransform, 86f, -26f, Width - 240f, 46f);
            _store = NewText(_panel, "Store", 26, UiTheme.InkSoft, TextAnchor.MiddleRight, UiTheme.Bold);
            Place(_store.rectTransform, Width - 230f, -26f, 200f, 46f);
            for (var i = 0; i < MaxRows; i++)
            {
                var y = -84f - i * RowHeight;
                var left = NewText(_panel, "Left" + i, 26, UiTheme.InkSoft, TextAnchor.MiddleLeft, UiTheme.Regular);
                Place(left.rectTransform, 30f, y, Width - 220f, RowHeight);
                var right = NewText(_panel, "Right" + i, 27, UiTheme.Ink, TextAnchor.MiddleRight, UiTheme.Heavy);
                Place(right.rectTransform, Width - 230f, y, 200f, RowHeight);
                _rows.Add((left, right));
            }
            _note = NewText(_panel, "Note", 24, UiTheme.Warning, TextAnchor.MiddleLeft, UiTheme.Bold);
            _note.horizontalOverflow = HorizontalWrapMode.Wrap;
            _root.SetActive(false);
        }

        private void Show(int key, RectTransform chip, Sprite icon)
        {
            _key = key;
            _chip = chip;
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            transform.SetAsLastSibling();
            _root.SetActive(true);
            Fill();
            _refreshIn = 0.5f;
            _openFor = 0f;
        }

        public void Close()
        {
            if (_root != null) _root.SetActive(false);
            _key = int.MinValue;
        }

        private void Update()
        {
            if (_root == null || !_root.activeSelf) return;
            // a film, a window or a pause takes the HUD: the note goes with it
            if (_chip == null || !_chip.gameObject.activeInHierarchy || Campaign.FilmSkip.FilmShowing)
            {
                Close();
                return;
            }
            // it closes by itself after ui_info_close_sec (the user, 2026-11-22: 7 s)
            _openFor += Time.unscaledDeltaTime;
            if (BalanceConfig.Instance != null && _openFor >= BalanceConfig.Instance.UiInfoCloseSeconds)
            {
                Close();
                return;
            }
            _refreshIn -= Time.unscaledDeltaTime;
            if (_refreshIn > 0f) return;
            _refreshIn = 0.5f;
            Fill();
        }

        // ---------------------------------------------------------------- the words

        private int _used;

        private void Fill()
        {
            _used = 0;
            _note.text = string.Empty;
            _note.color = UiTheme.Warning;
            var resources = ResourceManager.Instance;
            if (_key == PeopleKey) FillPeople();
            else if (_key == ResourceFlow.CrystalsKey)
            {
                _title.text = Localization.Get("#resinfo_crystals");
                _store.text = Wallet.UpgradeCrystals.ToString();
                FillFlow(ResourceFlow.CrystalsKey);
            }
            else if (_key == (int)ResourceType.Food) FillFood(resources);
            else
            {
                var type = (ResourceType)_key;
                _title.text = ResourceNames.Of(type);
                if (resources != null)
                {
                    var amount = resources.GetAmount(type);
                    var cap = resources.GetCapacity(type);
                    _store.text = $"{amount} / {cap}";
                    FillFlow(_key);
                    if (amount >= cap && cap > 0) _note.text = Localization.Get("#resinfo_full");
                }
            }
            if (string.IsNullOrEmpty(_note.text) && _key != PeopleKey && _key != (int)ResourceType.Food)
            {
                _note.text = Localization.Get("#resinfo_hint");
                _note.color = UiTheme.InkSoft;
            }
            for (var i = _used; i < _rows.Count; i++)
            {
                _rows[i].left.gameObject.SetActive(false);
                _rows[i].right.gameObject.SetActive(false);
            }
            Layout();
        }

        private void FillFlow(int key)
        {
            ResourceFlow.PerDay(key, out var gained, out var spent);
            Row(Localization.Get("#resinfo_gained"), PerDay(gained, true), gained > 0 ? UiTheme.Good : UiTheme.InkSoft);
            Row(Localization.Get("#resinfo_spent"), PerDay(-spent, true), spent > 0 ? UiTheme.Danger : UiTheme.InkSoft);
            var net = gained - spent;
            Row(Localization.Get("#resinfo_net"), PerDay(net, true), net > 0 ? UiTheme.Good : net < 0 ? UiTheme.Danger : UiTheme.InkSoft);
        }

        private void FillFood(ResourceManager resources)
        {
            _title.text = ResourceNames.Of(ResourceType.Food);
            var amount = resources != null ? resources.GetAmount(ResourceType.Food) : 0;
            _store.text = resources != null ? $"{amount} / {resources.GetCapacity(ResourceType.Food)}" : string.Empty;
            var made = 0;
            foreach (var producer in ProductionBuilding.All)
            {
                if (producer == null || producer.Instance == null || producer.Instance.IsBot || !producer.Instance.IsOperational) continue;
                if (!FoodOutput.IsProducer(producer.BuildingId) || producer.AssignedWorkers <= 0) continue;
                made += FoodOutput.PerDay(producer);
            }
            var eaters = FoodConsumptionManager.Instance;
            var mouths = eaters != null ? eaters.Mouths : 0;
            var eaten = FoodConsumptionManager.ComputeDemand(mouths);
            Row(Localization.Get("#resinfo_food_made"), PerDay(made, true), made > 0 ? UiTheme.Good : UiTheme.InkSoft);
            Row(Localization.Format("#resinfo_food_eaten", mouths), PerDay(-eaten, true), eaten > 0 ? UiTheme.Danger : UiTheme.InkSoft);
            var net = made - eaten;
            Row(Localization.Get("#resinfo_net"), PerDay(net, true), net >= 0 ? UiTheme.Good : UiTheme.Danger);
            if (net < 0)
            {
                _note.text = Localization.Format("#resinfo_lasts", Mathf.FloorToInt(amount / (float)-net));
                _note.color = UiTheme.Danger;
            }
            else
            {
                _note.text = Localization.Get("#resinfo_food_ok");
                _note.color = UiTheme.Good;
            }
        }

        private void FillPeople()
        {
            var people = CitizenManager.Instance;
            _title.text = Localization.Get("#resinfo_people");
            if (people == null) return;
            _store.text = $"{people.TotalPopulation} / {people.Capacity}";
            var free = Mathf.Max(0, people.Capacity - people.TotalPopulation);
            Row(Localization.Get("#resinfo_people_idle"), people.IdlePopulation.ToString(), people.IdlePopulation > 0 ? UiTheme.Good : UiTheme.Ink);
            Row(Localization.Get("#resinfo_people_room"), free.ToString(), free > 0 ? UiTheme.Good : UiTheme.Danger);
            // happiness is off: the sick and the soldiers instead (the user, 2026-11-22)
            var sick = Infirmary.Active && Infirmary.Instance != null ? Infirmary.Instance.Patients.Count : people.SickPopulation;
            Row(Localization.Get("#resinfo_people_sick"), sick.ToString(), sick > 0 ? UiTheme.Danger : UiTheme.Ink);
            var soldiers = Combat.ArmyManager.Instance != null ? Combat.ArmyManager.Instance.SoldierCount : 0;
            Row(Localization.Get("#resinfo_people_soldiers"), soldiers.ToString(), UiTheme.Ink);
            if (free <= 0) _note.text = Localization.Get("#resinfo_people_need_houses");
        }

        private static string PerDay(int value, bool signed)
        {
            var number = signed && value > 0 ? "+" + value : value < 0 ? "−" + (-value) : value.ToString();
            return Localization.Format("#resinfo_per_day", number);
        }

        private void Row(string left, string right, Color colour)
        {
            if (_used >= _rows.Count) return;
            var row = _rows[_used++];
            row.left.gameObject.SetActive(true);
            row.right.gameObject.SetActive(true);
            row.left.text = left;
            row.right.text = right;
            row.right.color = colour;
        }

        /// <summary>The panel's height to its rows and note; under the chip, kept inside the screen.</summary>
        private void Layout()
        {
            var noteY = -84f - _used * RowHeight - 6f;
            var hasNote = !string.IsNullOrEmpty(_note.text);
            _note.gameObject.SetActive(hasNote);
            if (hasNote) Place(_note.rectTransform, 30f, noteY, Width - 60f, 56f);
            var height = -noteY + (hasNote ? 64f : 14f);
            _panel.sizeDelta = new Vector2(Width, height);

            if (_chip == null || _canvas == null) return;
            var corners = new Vector3[4];
            _chip.GetWorldCorners(corners);
            var bottom = (corners[0] + corners[3]) * 0.5f;
            var local = (Vector2)_canvas.InverseTransformPoint(bottom);
            var half = _canvas.rect.width * 0.5f - Width * 0.5f - 12f;
            local.x = Mathf.Clamp(local.x, -half, half);
            _panel.anchoredPosition = local + new Vector2(0f, -14f);
        }

        // ---------------------------------------------------------------- the pieces

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        /// <summary>A child at (x, y) from the panel's top-left, w x h.</summary>
        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
        }

        private static Image NewImage(Transform parent, string name, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = colour;
            return image;
        }

        private static Text NewText(Transform parent, string name, int size, Color colour, TextAnchor align, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = colour;
            text.alignment = align;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
