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
    /// The orcs (stage 4): after the soldier is hired an orc barge lands tutorial_raid_orcs orcs (just
    /// «Отбейте набег»); their island (portal, level-1 tower, one guard) is laid by OrcRaidManager when
    /// the Town Hall stands. Every orc hits at tutorial_orc_damage_mult. Breaking the portal wins the
    /// map (GameOverManager: it is the only one); the win pays tutorial_reward_crystals once.
    /// </summary>
    public class TutorialDirector : MonoBehaviour
    {
        public enum Step
        {
            TownHall, Hovel, People, WoodPick, WoodSend, StonePick, StoneSend, Barracks, Soldier,
            Raid,
            Shipyard, Ship, KingPick, KingBoard, SoldierPick, SoldierBoard, Sail,
            Land, Guard, Tower, Portal,
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
        private const float SailReach = 12f;       // metres from our landing beach that count as «there»
        private const float BoardingGiveUp = 30f;  // the raid's orcs not at their barge by then: aboard anyway
        private const float WanderEvery = 9f;      // the King strolls to a new spot this often
        private const float WanderReach = 14f;     // that far from the Town Hall at most

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
        private ShipUnit _barge;                   // the orcs' raid
        private float _raidAt = -1f;
        private readonly HashSet<OrcUnit> _softened = new HashSet<OrcUnit>();
        private readonly List<OrcUnit> _walkers = new List<OrcUnit>();   // the raid's orcs on their way from the portal to the barge
        private Vector3 _walkBeach;
        private TutorialHud _hud;
        private BuildingCategoryPanel _buildMenu;
        private BuildingPlacer _placer;
        private float _normalDistance = -1f;       // the camera's distance before it zoomed in on a person
        private float _wanderIn;
        private bool _portalSet;                   // the portal's health set to the tutorial's few seconds
        private FocusHireBar[] _hireBars;
        private const string RewardedKey = "tutorial.rewarded";

        // Not in Awake: TutorialMode is decided in MeshMapApplier.Awake, and the order of two objects'
        // Awakes is not fixed -- a new HUD object put before this one turned the tutorial off (2026-11-05).
        // Every Awake has run by the first Start.

        private void OnDestroy()
        {
            if (GameOverManager.Instance != null) GameOverManager.Instance.OnGameEnded -= OnGameEnded;
            if (Instance == this) Instance = null;
            _hud?.Dispose();
            TutorialRules.Wanted = null;
            TutorialRules.Selectable = null;
            TutorialRules.MayLand = false;
        }

        private void Start()
        {
            if (!TutorialMode.IsActive)
            {
                enabled = false;
                return;
            }
            Instance = this;
            _camera = FindAnyObjectByType<RTSCameraController>();
            _citizens = FindAnyObjectByType<CitizenSelector>();
            _buildMenu = FindAnyObjectByType<BuildingCategoryPanel>();
            _placer = FindAnyObjectByType<BuildingPlacer>();
            _hireBars = FindObjectsByType<FocusHireBar>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            BuildPanel();
            _hud = new TutorialHud(uiRoot != null ? uiRoot : transform, circleSprite);
            _hud.ApplyHidden(false);
            Enter(Step.TownHall);
            if (GameOverManager.Instance != null) GameOverManager.Instance.OnGameEnded += OnGameEnded;
        }

        /// <summary>The portal broken: the tutorial is won -- its crystals, once (the menu tag stays for the replays).</summary>
        private void OnGameEnded(bool victory)
        {
            if (!victory) return;
            Enter(Step.Done);
            // nothing behind the victory but the King's line and the panel (the user, 2026-11-06)
            _hud.HideAllBut("GameOverPanel", "TutorialHint");
            _hud.Tick(null, false, null, Vector3.zero, 0f);
            if (PlayerPrefs.GetInt(RewardedKey, 0) != 0) return;
            PlayerPrefs.SetInt(RewardedKey, 1);
            Wallet.Crystals += BalanceConfig.Instance.TutorialRewardCrystals;
        }

        private void Update()
        {
            _stepTime += Time.deltaTime;
            WalkToBarge();
            ShowTheWay();
            _focusIn -= Time.unscaledDeltaTime;
            if (_focusIn <= 0f)
            {
                _focusIn = FocusEvery;
                Follow();
            }
            _checkIn -= Time.deltaTime;
            if (_checkIn > 0f) return;
            _checkIn = CheckEvery;
            _hud.ApplyHidden(BuildingInstance.TownHall != null);
            MoorBarge();
            Wander();
            Soften();
            Drive();
            ShortenPortal();
            if (Current != Step.Done && StepDone()) Enter(Current + 1);
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
            if (step == Step.Raid) LaunchRaid();
            TutorialRules.Wanted = WantedAt(step);
            TutorialRules.Selectable = SelectableAt(step);
            TutorialRules.MayLand = step >= Step.Land;
            // a group still held from before (the King came out selected in the raid) is let go: the step shows whom to tap
            if ((step == Step.KingPick || step == Step.SoldierPick) && ArmyManager.Instance != null) ArmyManager.Instance.SelectGroup(null);
            // the first person shown close up (the user: zoomed in 50%, easy to tap) ...
            if (step == Step.WoodPick && _camera != null && _normalDistance < 0f)
            {
                _normalDistance = _camera.GoalDistance;
                _camera.ZoomTo(_normalDistance * BalanceConfig.Instance.TutorialPersonZoom);
            }
            // ... and once the stone is in, the usual height over the Town Hall: most of the island in view
            if (step == Step.Barracks && _camera != null)
            {
                if (_normalDistance > 0f) _camera.ZoomTo(_normalDistance);
                if (BuildingInstance.TownHall != null) _camera.FocusOn(BuildingInstance.TownHall.transform.position);
            }
            Say("#tutorial_hint_" + step.ToString().ToLowerInvariant());
        }

        private bool StepDone()
        {
            switch (Current)
            {
                case Step.TownHall: return BuildingInstance.TownHall != null;
                case Step.Hovel: return Standing("Hovel");
                case Step.People: return _stepTime >= BalanceConfig.Instance.TutorialPeopleHintSeconds;
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
                case Step.Raid: return RaidBeaten();
                case Step.Sail: return Arrived();
                case Step.Land: return _ship == null || !_ship || _ship.Aboard.Count == 0;
                case Step.Guard: return !AnyOrcAlive();
                case Step.Tower: return !AnyTowerStanding();
                case Step.Portal: return OrcPortal.All.Count == 0;
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

        // ------------------------------------------------------------ the orcs

        /// <summary>
        /// The orcs' barge waits by their island from the moment it is laid (the user, 2026-11-05: it
        /// stands there; the orcs come out of the portal and board it when the raid starts).
        /// </summary>
        private void MoorBarge()
        {
            if (_barge != null || _raidAt >= 0f || OrcPortal.All.Count == 0 || ShipManager.Instance == null) return;
            var map = MeshMapApplier.Instance;
            var towards = map != null && map.TryTutorialSpot("orc_landing", out var landing) ? landing : Vector3.zero;
            _barge = ShipManager.Instance.MoorTutorialBarge(OrcPortal.All[0].transform.position, towards);
        }

        /// <summary>The raid: tutorial_raid_orcs orcs step out of the portal and walk to the barge (WalkToBarge).</summary>
        private void LaunchRaid()
        {
            _raidAt = Time.time;
            var orcs = OrcRaidManager.Instance;
            if (orcs == null || OrcPortal.All.Count == 0) return;
            var portal = OrcPortal.All[0].transform.position;
            if (_barge == null || !_barge || !ShipManager.TryBargeBeach(_barge, out _walkBeach))
            {
                // no barge (no sea by their island): the raid lands as it used to, at our beach
                if (MeshMapApplier.Instance != null && MeshMapApplier.Instance.TryTutorialSpot("orc_landing", out var beach))
                    orcs.SpawnLanding(beach, BalanceConfig.Instance.TutorialRaidOrcs, 1);
                return;
            }
            var toBeach = _walkBeach - portal;
            toBeach.y = 0f;
            var step = toBeach.sqrMagnitude > 0.01f ? toBeach.normalized * 3f : Vector3.forward * 3f;
            for (var i = 0; i < BalanceConfig.Instance.TutorialRaidOrcs; i++)
            {
                var orc = orcs.SpawnOrcOfKind(portal + step + Vector3.right * (i - 0.5f), 1, OrcKind.Raider);
                if (orc == null) continue;
                orc.Steered = true;
                _softened.Add(orc);
                _walkers.Add(orc);
            }
        }

        /// <summary>Every frame while they walk: on to the barge's beach; all there (or too long) -- aboard, and it sails.</summary>
        private void WalkToBarge()
        {
            if (_walkers.Count == 0) return;
            var waiting = false;
            foreach (var orc in _walkers)
                if (orc != null && orc.SteerTo(_walkBeach)) waiting = true;
            if (waiting && Time.time - _raidAt < BoardingGiveUp) return;
            var count = 0;
            foreach (var orc in _walkers)
            {
                if (orc == null) continue;
                count++;
                Destroy(orc.gameObject);
            }
            _walkers.Clear();
            var map = MeshMapApplier.Instance;
            if (count > 0 && _barge != null && _barge && map != null && map.TryTutorialSpot("orc_landing", out var landing))
                ShipManager.Instance.SailTutorialBarge(_barge, landing, count);
        }

        /// <summary>The barge has landed its orcs and every one of them is down (a barge that never sailed: no raid to wait for).</summary>
        private bool RaidBeaten()
        {
            if (_raidAt < 0f) return true;
            if (_walkers.Count > 0) return false;
            if (_barge != null && _barge && _barge.OrcCargo > 0) return false;
            foreach (var orc in OrcUnit.All)
                if (orc != null && orc.CurrentHealth > 0 && !orc.IsGuard) return false;
            return Time.time - _raidAt > 3f;
        }

        private static bool AnyOrcAlive()
        {
            foreach (var orc in OrcUnit.All)
                if (orc != null && orc.CurrentHealth > 0) return true;
            return false;
        }

        private static bool AnyTowerStanding()
        {
            foreach (var b in OrcCampBuilding.All)
                if (b != null && b.Kind == OrcCampBuildingKind.Watchtower && b.CurrentHealth > 0) return true;
            return false;
        }

        /// <summary>Every orc of the tutorial hits softly: visible, never deadly (the user, 2026-11-05).</summary>
        private void Soften()
        {
            var mult = BalanceConfig.Instance.TutorialOrcDamageMult;
            foreach (var orc in OrcUnit.All)
            {
                if (orc == null || _softened.Contains(orc)) continue;
                _softened.Add(orc);
                // the guard was softened when it was laid; tougher, they last more than one shot (the user)
                if (!orc.IsGuard) orc.Toughen(BalanceConfig.Instance.TutorialOrcHealthMult, mult);
            }
        }

        /// <summary>
        /// Whose group a tap may pick on this step (the user, 2026-11-06): nobody's while the town is built and
        /// the raid beaten off -- it all runs on its own --, the King's and then the soldier's at the boarding,
        /// anyone's once they have landed.
        /// </summary>
        private static System.Func<ArmyGroup, bool> SelectableAt(Step step)
        {
            switch (step)
            {
                case Step.KingPick:
                case Step.KingBoard:
                    return g => Holds(g, SoldierType.King);
                case Step.SoldierPick:
                case Step.SoldierBoard:
                    return g => Holds(g, TutorialRules.Soldier);
                default:
                    return step >= Step.Land ? g => true : (System.Func<ArmyGroup, bool>)null;
            }
        }

        private static bool Holds(ArmyGroup group, SoldierType type)
        {
            if (group == null) return false;
            foreach (var member in group.Members)
                if (member != null && member.Type == type) return true;
            return false;
        }

        // ------------------------------------------------------------ the fight runs itself

        /// <summary>
        /// The soldier and the King fight on their own (the user, 2026-11-06): the soldier walks the island's
        /// round and runs at a landing (the squad's «Охранять город») until he is picked for the ship; landed
        /// on the orcs' island, both go for the orcs, then the tower, then the portal.
        /// </summary>
        private void Drive()
        {
            var army = ArmyManager.Instance;
            if (army == null) return;
            var soldier = FindUnit(TutorialRules.Soldier);
            var squad = soldier != null ? soldier.Group : null;
            if (squad != null && squad.IsSquad)
            {
                var behaviour = Current < Step.SoldierPick ? SquadBehaviour.Guard : SquadBehaviour.Hold;
                if (squad.Behaviour != behaviour) army.SetBehaviour(squad, behaviour);
            }
            if (Current < Step.Land || Current >= Step.Done) return;
            Assault(soldier);
            Assault(FindUnit(SoldierType.King));
        }

        /// <summary>A unit ashore on the orcs' island with nothing to hit: at the next foe there, in the order the user gave.</summary>
        private static void Assault(SoldierUnit unit)
        {
            if (unit == null || !unit.isActiveAndEnabled || unit.Group == null) return;
            var group = unit.Group;
            if (group.AttackTarget != null && group.AttackTarget.IsAlive) return;
            var map = MeshMapApplier.Instance;
            var from = unit.transform.position;
            var tower = AnyTowerStanding();
            var foe = ArmyManager.NearestFoe(from, t => (map == null || map.SameIsland(from, t.Transform.position))
                && !(tower && t is OrcPortal), false);
            if (foe != null) group.OrderAttack(foe);
        }

        /// <summary>The tutorial's portal falls to the King alone in tutorial_portal_sec (the user, 2026-11-06: ~7 s, it took ages).</summary>
        private void ShortenPortal()
        {
            if (_portalSet || OrcPortal.All.Count == 0) return;
            _portalSet = true;
            var king = SoldierStats.Stats(SoldierType.King);
            var perSecond = king.attackDamage / Mathf.Max(0.1f, king.attackIntervalSeconds);
            var portal = OrcPortal.All[0];
            portal.SetLevel(portal.Level, Mathf.Max(1, Mathf.CeilToInt(perSecond * BalanceConfig.Instance.TutorialPortalSeconds)));
        }

        // ------------------------------------------------------------ showing the way

        /// <summary>The building the step asks for: its card unlocked, the arrow on it.</summary>
        private static string WantedAt(Step step)
        {
            switch (step)
            {
                case Step.TownHall: return BuildingIds.TownHall;
                case Step.Hovel: return "Hovel";
                case Step.Barracks: return BuildingIds.Barracks;
                case Step.Shipyard: return ShipManager.ShipyardId;
                default: return null;
            }
        }

        /// <summary>Every frame: the blinking build button, the arrow over the card, the gold ring on what to tap.</summary>
        private void ShowTheWay()
        {
            // a build step only until its building is down: then no blinking, no arrow, and the menu shut --
            // it is being built (the user, 2026-11-06: the arrow kept pointing at the Лачуга under construction)
            var buildStep = Current == Step.Hovel || Current == Step.Barracks || Current == Step.Shipyard;
            var building = buildStep && !BuildingInstance.HasAny(TutorialRules.Wanted);
            var placing = _placer != null && _placer.IsSelecting;
            var open = _buildMenu != null && _buildMenu.IsOpen;
            if (buildStep && !building && open && !placing) _buildMenu.Close();
            var tab = _buildMenu != null ? _buildMenu.FirstTab : null;
            var card = building && open && !placing && _buildMenu != null ? _buildMenu.CardOf(TutorialRules.Wanted) : null;
            // the Barracks / the Shipyard held: the arrow on its hiring button (the user: nothing said where to hire)
            if (card == null && (Current == Step.Soldier || Current == Step.Ship))
                card = HireButton(Current == Step.Soldier ? FocusHireBar.Kind.Barracks : FocusHireBar.Kind.Shipyard);
            Highlight(out var at, out var radius);
            _hud.Tick(tab, building && !open && !placing, card, at, radius);
        }

        private RectTransform HireButton(FocusHireBar.Kind kind)
        {
            if (_hireBars == null) return null;
            foreach (var bar in _hireBars)
                if (bar != null && bar.BarKind == kind) return bar.FirstShownButton;
            return null;
        }

        /// <summary>What the player should tap now, in the world: a ring round it (radius 0: nothing).</summary>
        private void Highlight(out Vector3 at, out float radius)
        {
            at = Vector3.zero;
            radius = 0f;
            switch (Current)
            {
                case Step.WoodPick:
                case Step.StonePick:
                    if (_person != null && _person.isActiveAndEnabled) Ring(_person.transform.position, 0.7f, out at, out radius);
                    break;
                case Step.WoodSend:
                case Step.StoneSend:
                    if (!_sent && _node != null && _node) Ring(_node.transform.position, 1.1f, out at, out radius);
                    break;
                case Step.Soldier:
                    RingUnlessHeld(Built(BuildingIds.Barracks), out at, out radius);
                    break;
                case Step.Ship:
                    RingUnlessHeld(Built(ShipManager.ShipyardId), out at, out radius);
                    break;
                case Step.KingPick:
                case Step.SoldierPick:
                    if (_unit != null && _unit && _unit.isActiveAndEnabled) Ring(_unit.transform.position, 0.8f, out at, out radius);
                    break;
                case Step.KingBoard:
                case Step.SoldierBoard:
                    if (_ship != null && _ship) Ring(_ship.transform.position, 3f, out at, out radius);
                    break;
                case Step.Shipyard:
                    // where the Shipyard goes, while it is being placed (the user, 2026-11-06: hard to find the spot)
                    if (_placer != null && _placer.IsSelecting && !BuildingInstance.HasAny(ShipManager.ShipyardId)
                        && MeshMapApplier.Instance != null && MeshMapApplier.Instance.TryTutorialSpot("shipyard", out var yard))
                        Ring(yard, 2.6f, out at, out radius);
                    break;
                case Step.Sail:
                    // where to send the ship with the troops aboard (the user, 2026-11-06)
                    if (MeshMapApplier.Instance != null && MeshMapApplier.Instance.TryTutorialSpot("player_landing", out var landing))
                        Ring(landing, 4f, out at, out radius);
                    break;
            }
        }

        private static void Ring(Vector3 position, float size, out Vector3 at, out float radius)
        {
            at = position;
            radius = size;
        }

        /// <summary>A building the player must tap (the Barracks to hire, the Shipyard to buy): ringed until he holds it.</summary>
        private static void RingUnlessHeld(BuildingInstance building, out Vector3 at, out float radius)
        {
            at = Vector3.zero;
            radius = 0f;
            if (building == null || Holding(building)) return;
            var grid = Grid.GridManager.Instance;
            var cell = grid != null ? grid.CellSize : 1f;
            var size = building.RotatedFootprint();
            at = grid != null ? grid.GetFootprintCenterWorld(building.OriginCell, size) : building.transform.position;
            at.y = building.transform.position.y;
            radius = Mathf.Max(size.x, size.y) * cell * 0.65f;
        }

        private static bool Holding(BuildingInstance building) => BuildingFocus.Instance != null && BuildingFocus.Instance.Current == building;

        /// <summary>The built (working) one of these, or null.</summary>
        private static BuildingInstance Built(string id)
        {
            if (!BuildingInstance.HasAny(id)) return null;
            foreach (var b in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
                if (b != null && b.Data != null && b.Data.buildingName == id && b.IsOperational) return b;
            return null;
        }

        /// <summary>
        /// The King strolls about the island while the player builds (the user, 2026-11-05: he walked
        /// to its edge and stood there) -- until the steps that put him in the player's hands.
        /// </summary>
        private void Wander()
        {
            if (Current >= Step.KingPick || BuildingInstance.TownHall == null) return;
            _wanderIn -= CheckEvery;
            if (_wanderIn > 0f) return;
            _wanderIn = WanderEvery;
            var king = FindUnit(SoldierType.King);
            if (king == null || king.Group == null || !king.isActiveAndEnabled) return;
            if (ArmyManager.Instance != null && ArmyManager.Instance.SelectedGroup == king.Group) return;
            // not in a fight: a stroll order called him off the raid
            if (Current == Step.Raid || (king.Group.AttackTarget != null && king.Group.AttackTarget.IsAlive)) return;
            var hall = BuildingInstance.TownHall.transform.position;
            var offset = Random.insideUnitCircle * WanderReach;
            if (ShipManager.TrySampleLand(hall + new Vector3(offset.x, 0f, offset.y), 3f, out var spot)) king.Group.OrderMoveTo(spot);
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
                case Step.Raid:
                    // the orcs' barge on its way (the user, 2026-11-06), until it has put them ashore
                    if (_barge != null && _barge && (_walkers.Count > 0 || _barge.OrcCargo > 0)) _camera.FocusOn(_barge.transform.position);
                    break;
                case Step.Sail:
                    // the ship at first, the marked spot ahead of it in view
                    if (_ship == null || !_ship) _ship = OurShip();
                    if (_ship != null && _stepTime < 3f) _camera.FocusOn(_ship.transform.position);
                    break;
                case Step.Soldier:
                case Step.Ship:
                    // the Barracks to hire at, the Shipyard to buy at: on it until the player taps it
                    var place = Built(Current == Step.Soldier ? BuildingIds.Barracks : ShipManager.ShipyardId);
                    if (place != null && !Holding(place)) _camera.FocusOn(place.transform.position);
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
            _line.text = Emphasise(text);
            _panel.SetActive(!string.IsNullOrEmpty(text) && text != key);
        }

        private const string KeywordColour = "#B5400C";

        /// <summary>
        /// The words between asterisks in the sheet («Постройте *Казарму*») in their own colour (the user,
        /// 2026-11-06: the key words should stand out). The sheet holds the stars, the colour stays here.
        /// </summary>
        public static string Emphasise(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('*') < 0) return text;
            var sb = new System.Text.StringBuilder(text.Length + 32);
            var open = false;
            foreach (var c in text)
            {
                if (c != '*')
                {
                    sb.Append(c);
                    continue;
                }
                sb.Append(open ? "</color>" : "<color=" + KeywordColour + ">");
                open = !open;
            }
            if (open) sb.Append("</color>");
            return sb.ToString();
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
            // the face kept inside its round frame (the crown stuck out of it, the user 2026-11-05)
            ring.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            Img(ring.transform, "Face", Color.white, kingPortrait, Vector2.zero, new Vector2(126f, 126f)).preserveAspect = true;

            var go = new GameObject("Line", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(_panel.transform, false);
            _line = go.GetComponent<Text>();
            _line.font = UiTheme.Bold;
            _line.fontSize = 36;
            _line.alignment = TextAnchor.MiddleLeft;
            _line.color = new Color(0.25f, 0.14f, 0.06f);
            _line.raycastTarget = false;
            _line.supportRichText = true;
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
