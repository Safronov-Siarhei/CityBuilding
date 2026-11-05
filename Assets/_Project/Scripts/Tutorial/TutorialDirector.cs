using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.CameraControl;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.Resources;
using CityBuilder.Ships;
using CityBuilder.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.Tutorial
{
    /// <summary>
    /// The tutorial's script (Design/features/tutorial.md «The script», the user, 2026-11-05): one step
    /// at a time, each with the King's line on a panel at the top (his portrait and the words, keys
    /// #tutorial_hint_*) and a condition the game itself meets. The steps that pick a unit -- a
    /// person for the wood and the stone, the King and the soldier for the ship -- keep the camera
    /// on that unit until the player taps it, then on what it should be sent to.
    ///
    /// Built by SetupProjectHud (sprites only), the UI made here at run time; off outside the tutorial.
    /// Stage 3: the steps up to the ship reaching the orcs' island. The orc landing, the fight on
    /// their island, the portal and the reward come with stage 4 (the steps marked so).
    /// </summary>
    public class TutorialDirector : MonoBehaviour
    {
        public enum Step
        {
            TownHall, Hovel, People, WoodPick, WoodSend, StonePick, StoneSend, Barracks, Soldier,
            // stage 4: the orc landing (Raid) goes here
            Shipyard, Ship, KingPick, KingBoard, SoldierPick, SoldierBoard, Sail,
            // stage 4: landing, the guards, the tower, the portal, victory
            Done,
        }

        [SerializeField] private Transform uiRoot;
        [SerializeField] private Sprite cardSprite;
        [SerializeField] private Sprite circleSprite;
        [SerializeField] private Sprite kingPortrait;

        /// <summary>The step the player is on (for the tests and stage 4).</summary>
        public Step Current { get; private set; }

        public static TutorialDirector Instance { get; private set; }

        private const float FocusEvery = 0.3f;     // the camera re-centred on a moving unit this often
        private const float CheckEvery = 0.25f;
        private const float PeopleHintSeconds = 6f;
        private const float SailReach = 12f;       // metres from our landing beach that count as «there»

        private GameObject _panel;
        private Text _line;
        private RTSCameraController _camera;
        private CitizenSelector _citizens;
        private float _checkIn, _focusIn, _stepTime;
        private CitizenAgent _person;              // the one shown for the wood / the stone
        private ResourceNode _node;                // the tree / the boulder shown to him
        private bool _sent;                        // he took the order (then the step waits for him to finish)
        private SoldierUnit _unit;                 // the King / the soldier being boarded
        private ShipUnit _ship;

        private void Awake()
        {
            if (!TutorialMode.IsActive)
            {
                enabled = false;
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (!enabled) return;
            _camera = FindAnyObjectByType<RTSCameraController>();
            _citizens = FindAnyObjectByType<CitizenSelector>();
            BuildPanel();
            Enter(Step.TownHall);
        }

        private void Update()
        {
            _stepTime += Time.deltaTime;
            _focusIn -= Time.unscaledDeltaTime;
            if (_focusIn <= 0f)
            {
                _focusIn = FocusEvery;
                Follow();
            }
            _checkIn -= Time.deltaTime;
            if (_checkIn > 0f) return;
            _checkIn = CheckEvery;
            if (StepDone()) Enter(Current + 1);
        }

        // ------------------------------------------------------------ the steps

        private void Enter(Step step)
        {
            Current = step;
            _stepTime = 0f;
            _sent = false;
            if (step == Step.WoodPick || step == Step.StonePick)
            {
                _person = null;
                _node = null;
            }
            if (step == Step.KingPick || step == Step.SoldierPick) _unit = null;
            Say("#tutorial_hint_" + step.ToString().ToLowerInvariant());
        }

        private bool StepDone()
        {
            switch (Current)
            {
                case Step.TownHall: return BuildingInstance.TownHall != null;
                case Step.Hovel: return Standing("Hovel");
                case Step.People: return _stepTime >= PeopleHintSeconds;
                case Step.WoodPick: return Picked();
                case Step.WoodSend: return Gathered(ResourceType.Wood);
                case Step.StonePick: return Picked();
                case Step.StoneSend: return Gathered(ResourceType.Stone);
                case Step.Barracks: return Standing(BuildingIds.Barracks);
                case Step.Soldier: return FindUnit(TutorialRules.Soldier) != null;
                case Step.Shipyard: return Standing(ShipManager.ShipyardId);
                case Step.Ship: return (_ship = OurShip()) != null;
                case Step.KingPick: return Held(SoldierType.King);
                case Step.KingBoard: return Aboard();
                case Step.SoldierPick: return Held(TutorialRules.Soldier);
                case Step.SoldierBoard: return Aboard();
                case Step.Sail: return Arrived();
                default: return false;
            }
        }

        /// <summary>One of these standing and built (its construction over).</summary>
        private static bool Standing(string id)
        {
            if (!BuildingInstance.HasAny(id)) return false;
            foreach (var b in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
                if (b != null && b.Data != null && b.Data.buildingName == id && b.IsOperational) return true;
            return false;
        }

        /// <summary>The person shown has been tapped (or any free one, if the player took another).</summary>
        private bool Picked()
        {
            if (_person == null || !_person.isActiveAndEnabled) _person = AnyIdlePerson();
            var held = _citizens != null ? _citizens.Selected : null;
            if (held == null) return false;
            _person = held;
            return true;
        }

        /// <summary>He was sent to a node of this kind and has come back from it.</summary>
        private bool Gathered(ResourceType type)
        {
            if (_node == null || !_node) _node = NearestNode(type, _person != null ? _person.transform.position : Vector3.zero);
            if (_person == null) return false;
            var target = _person.GatherTarget;
            if (target != null && target.ResourceType == type) _sent = true;
            // sent, and back to idling: the tree is down / the stone is in
            return _sent && target == null && _person.IsIdle;
        }

        private bool Held(SoldierType type)
        {
            if (_unit == null || !_unit) _unit = FindUnit(type);
            var group = ArmyManager.Instance != null ? ArmyManager.Instance.SelectedGroup : null;
            if (_unit == null || group == null) return false;
            foreach (var member in group.Members)
                if (member == _unit) return true;
            return false;
        }

        private bool Aboard()
        {
            if (_ship == null || !_ship) _ship = OurShip();
            if (_unit == null) return true;          // he fell: on to the next
            return _ship != null && _ship.Aboard.Contains(_unit);
        }

        private bool Arrived()
        {
            if (_ship == null || !_ship) _ship = OurShip();
            var map = MeshMapApplier.Instance;
            if (_ship == null || map == null || !map.TryTutorialSpot("player_landing", out var landing)) return false;
            var at = _ship.transform.position;
            at.y = 0f;
            return Vector3.Distance(at, landing) <= SailReach;
        }

        // ------------------------------------------------------------ the camera

        /// <summary>The steps that show a unit keep the camera on it; once it is picked, on its target.</summary>
        private void Follow()
        {
            if (_camera == null) return;
            switch (Current)
            {
                case Step.WoodPick:
                case Step.StonePick:
                    if (_person == null || !_person.isActiveAndEnabled) _person = AnyIdlePerson();
                    if (_person != null) _camera.FocusOn(_person.transform.position);
                    break;
                case Step.WoodSend:
                case Step.StoneSend:
                    // the tree / the boulder until he is on his way, then nothing: the player watches him
                    if (!_sent && _node != null && _node) _camera.FocusOn(_node.transform.position);
                    break;
                case Step.KingPick:
                case Step.SoldierPick:
                    if (_unit == null || !_unit) _unit = FindUnit(Current == Step.KingPick ? SoldierType.King : TutorialRules.Soldier);
                    if (_unit != null && _unit.isActiveAndEnabled) _camera.FocusOn(_unit.transform.position);
                    break;
                case Step.KingBoard:
                case Step.SoldierBoard:
                    if (_ship == null || !_ship) _ship = OurShip();
                    if (_ship != null) _camera.FocusOn(_ship.transform.position);
                    break;
            }
        }

        // ------------------------------------------------------------ finding things

        private static CitizenAgent AnyIdlePerson()
        {
            var people = CitizenVisualsManager.Instance != null ? CitizenVisualsManager.Instance.AllAgents : null;
            if (people == null) return null;
            foreach (var p in people)
                if (p != null && p.isActiveAndEnabled && p.IsIdle) return p;
            return null;
        }

        private static ResourceNode NearestNode(ResourceType type, Vector3 from)
        {
            ResourceNode best = null;
            var bestDistance = float.MaxValue;
            foreach (var node in ResourceNode.All)
            {
                if (node == null || node.ResourceType != type) continue;
                var d = (node.transform.position - from).sqrMagnitude;
                if (d < bestDistance)
                {
                    best = node;
                    bestDistance = d;
                }
            }
            return best;
        }

        private static SoldierUnit FindUnit(SoldierType type)
        {
            foreach (var unit in SoldierUnit.All)
                if (unit != null && unit.Type == type && unit.CurrentHealth > 0) return unit;
            return null;
        }

        private static ShipUnit OurShip()
        {
            var ships = ShipManager.Instance;
            if (ships == null) return null;
            foreach (var ship in ships.Ships)
                if (ship != null && ship.Kind == TutorialRules.Ship) return ship;
            return null;
        }

        // ------------------------------------------------------------ the King's panel

        private void Say(string key)
        {
            if (_line == null) return;
            var text = Localization.Get(key);
            _line.text = text;
            _panel.SetActive(!string.IsNullOrEmpty(text) && text != key);
        }

        private void BuildPanel()
        {
            _panel = new GameObject("TutorialHint", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(uiRoot != null ? uiRoot : transform, false);
            var rect = (RectTransform)_panel.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -120f);
            rect.sizeDelta = new Vector2(1000f, 170f);
            var card = _panel.GetComponent<Image>();
            card.sprite = cardSprite;
            card.type = cardSprite != null && cardSprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            card.color = new Color(1f, 0.96f, 0.78f, 1f);
            card.raycastTarget = false;

            var ring = Img(_panel.transform, "Portrait", new Color(0.66f, 0.92f, 0.76f), circleSprite, new Vector2(-410f, 0f), new Vector2(140f, 140f));
            Img(ring.transform, "Face", Color.white, kingPortrait, Vector2.zero, new Vector2(126f, 126f)).preserveAspect = true;

            var go = new GameObject("Line", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(_panel.transform, false);
            _line = go.GetComponent<Text>();
            _line.font = UiTheme.Bold;
            _line.fontSize = 36;
            _line.alignment = TextAnchor.MiddleLeft;
            _line.color = new Color(0.25f, 0.14f, 0.06f);
            _line.raycastTarget = false;
            _line.horizontalOverflow = HorizontalWrapMode.Wrap;
            _line.verticalOverflow = VerticalWrapMode.Truncate;
            var lr = _line.rectTransform;
            lr.anchoredPosition = new Vector2(80f, 0f);
            lr.sizeDelta = new Vector2(780f, 150f);
        }

        private static Image Img(Transform parent, string name, Color colour, Sprite sprite, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = colour;
            image.sprite = sprite;
            image.raycastTarget = false;
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size;
            return image;
        }
    }
}
