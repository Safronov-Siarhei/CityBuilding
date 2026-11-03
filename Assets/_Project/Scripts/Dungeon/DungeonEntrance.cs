using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.UI;
using UnityEngine;

namespace CityBuilder.Dungeon
{
    /// <summary>
    /// A dungeon's entrance on the town's map (Maps/Archipelago/DungeonEntrances): its level
    /// (1 nearest the start) and the dungeon it leads to. A tap opens its card
    /// (BuildingFocus.SelectEntrance: a ring and the «!»; the «!» opens DungeonEntranceCard).
    /// The user, 2026-11-01: a hero merely passing by gets a feed notice «Рядом обнаружено
    /// подземелье» (a tap on it selects the mouth), never the card; the card opens by itself only
    /// for a hero SENT to this mouth (a tap on it with the hero held: Await), once he is there.
    /// </summary>
    /// The dungeon's camp buildings are places of the same kind (the user, 2026-11-01: «такой
    /// принцип как при входе»): the gnomes' hire camp and the potion shop, held with a ring and
    /// the «!», their cards opening for a hero sent there.
    public class DungeonEntrance : MonoBehaviour
    {
        /// <summary>What this place is: a mouth on the town's map, a dungeon's way out, or one of its camp buildings.</summary>
        public enum Place
        {
            Mouth,
            Exit,
            HireCamp,
            Shop,
            Lever,
        }

        private const float CheckEverySeconds = 0.5f;

        private static readonly List<DungeonEntrance> _all = new List<DungeonEntrance>();
        public static IReadOnlyList<DungeonEntrance> All => _all;

        /// <summary>The mouth a hero was sent to: his arrival opens its card. Any other order of the player's clears it.</summary>
        public static DungeonEntrance Awaited;

        public int Level { get; private set; }
        public string DungeonId { get; private set; }
        public Vector2Int Cell { get; private set; }

        public Place Kind { get; private set; } = Place.Mouth;

        /// <summary>The way OUT of a dungeon (DungeonMode.Exit): its card says «Выйти» and takes the party home; passing by says nothing.</summary>
        public bool IsExit => Kind == Place.Exit;

        /// <summary>A camp building below (the hire camp, the shop), not a way anywhere.</summary>
        public bool IsCamp => Kind == Place.HireCamp || Kind == Place.Shop;

        // half its footprint, cells: where a walker stops and how near counts as «by it»
        private float _half = Maps.Archipelago.DungeonEntrances.Footprint.x * 0.5f;

        /// <summary>Half its footprint, cells (the selection ring's size).</summary>
        public float Half => _half;

        private float _timer;
        private bool _heroWasNear;

        public void Initialize(int level, string dungeonId, Vector2Int cell)
        {
            Level = level;
            DungeonId = dungeonId;
            Cell = cell;
        }

        /// <summary>Makes this the dungeon's way out.</summary>
        public void InitializeExit(string dungeonId)
        {
            Kind = Place.Exit;
            DungeonId = dungeonId;
            Level = DungeonMode.Instance != null ? DungeonMode.Instance.Level : 1;
        }

        /// <summary>Makes this one of the dungeon's camp buildings, `half` cells across from its middle.</summary>
        public void InitializeCamp(Place kind, string dungeonId, float half)
        {
            Kind = kind;
            DungeonId = dungeonId;
            _half = half;
            Level = DungeonMode.Instance != null ? DungeonMode.Instance.Level : 1;
        }

        /// <summary>Makes this one of the dungeon's levers (DungeonLevers): a hero sent to it pulls it.</summary>
        public void InitializeLever(string dungeonId, float half)
        {
            Kind = Place.Lever;
            DungeonId = dungeonId;
            _half = half;
            Level = DungeonMode.Instance != null ? DungeonMode.Instance.Level : 1;
        }

        /// <summary>Its card's button works now: a hero by it, and the game not busy going somewhere.</summary>
        public bool CanGo => HeroNear() && (IsExit || IsCamp || Kind == Place.Lever ? DungeonMode.IsActive && !LoadingScreen.Busy : DungeonTravel.CanEnter);

        /// <summary>This place's card: the hire camp's, the shop's, or the way in/out's.</summary>
        public void OpenCard()
        {
            switch (Kind)
            {
                case Place.HireCamp:
                    // held like the Barracks: the gnomes' portraits with their prices along the
                    // bottom (FocusHireBar) and nothing else -- no «!», no card (the user, 2026-11-02)
                    var focus = BuildingFocus.Instance;
                    if (focus != null && focus.Entrance != this)
                    {
                        Combat.ArmyManager.Instance?.SelectGroup(null);
                        focus.SelectEntrance(this);
                    }
                    break;
                case Place.Shop:
                    if (DungeonShopCard.Instance != null) DungeonShopCard.Instance.Show(this);
                    break;
                case Place.Lever:
                    // no card: a hero by it pulls it; else the player is told to bring one
                    if (DungeonLevers.Instance != null) DungeonLevers.Instance.Use(this);
                    break;
                default:
                    if (DungeonEntranceCard.Instance != null) DungeonEntranceCard.Instance.Show(this);
                    break;
            }
        }

        /// <summary>Any place's card is up.</summary>
        public static bool AnyCardOpen =>
            (DungeonEntranceCard.Instance != null && DungeonEntranceCard.Instance.IsOpen)
            || (DungeonShopCard.Instance != null && DungeonShopCard.Instance.IsOpen);

        /// <summary>Every place's card down.</summary>
        public static void CloseCards()
        {
            if (DungeonEntranceCard.Instance != null) DungeonEntranceCard.Instance.Close();
            if (DungeonShopCard.Instance != null) DungeonShopCard.Instance.Close();
        }

        private void OnEnable() => _all.Add(this);

        private void OnDisable()
        {
            _all.Remove(this);
            if (Awaited == this) Awaited = null;
        }

        /// <summary>A hero is on his way here: his arrival -- or his being here already -- opens the card.</summary>
        public void Await()
        {
            Awaited = this;
            _heroWasNear = false;
            _timer = 0f;
        }

        /// <summary>Where a walker coming from `from` stops: in front of the mouth on his side, well inside the reach.</summary>
        public Vector3 ApproachFrom(Vector3 from)
        {
            var away = from - transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;
            var stop = transform.position + away.normalized * (_half + 0.7f);
            return UnityEngine.AI.NavMesh.SamplePosition(stop, out var hit, 4f, UnityEngine.AI.NavMesh.AllAreas) ? hit.position : stop;
        }

        /// <summary>A living hero within dungeon_enter_reach_cells of the entrance (its footprint's edge, not its middle).</summary>
        public bool HeroNear()
        {
            var roster = HeroRoster.Instance;
            if (roster == null) return false;
            foreach (var hero in roster.Heroes)
                if (IsNear(hero)) return true;
            return false;
        }

        /// <summary>This hero stands and is within reach: he goes down with the party (the user, 2026-11-01: only those by the mouth do).</summary>
        public bool IsNear(Hero hero)
        {
            if (hero == null || hero.IsFallen || hero.Unit == null) return false;
            var balance = BalanceConfig.Instance;
            var reach = (balance != null ? balance.DungeonEnterReach : 4f) + _half;
            var d = hero.Unit.transform.position - transform.position;
            d.y = 0f;
            return d.sqrMagnitude <= reach * reach;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = CheckEverySeconds;
            var near = HeroNear();
            if (near && !_heroWasNear)
            {
                if (Awaited == this)
                {
                    Awaited = null;
                    if (CanGo && !AnyCardOpen) OpenCard();
                }
                else if (Kind == Place.Mouth)
                {
                    var entrance = this;
                    var notice = GameEvent.At(Localization.Get("#log_dungeon_near"), EventSeverity.Good, EventGlyph.Star, null, transform.position);
                    notice.Tapped = () =>
                    {
                        if (entrance == null) return;
                        // the hero let go first, or holding him would let the mouth go at once
                        if (ArmyManager.Instance != null) ArmyManager.Instance.SelectGroup(null);
                        if (BuildingFocus.Instance != null) BuildingFocus.Instance.SelectEntrance(entrance);
                    };
                    EventLogManager.Instance?.Post(notice);
                }
            }
            _heroWasNear = near;
        }

        /// <summary>The entrance under a screen point, or null.</summary>
        public static DungeonEntrance At(Vector2 screenPosition, Camera camera)
        {
            if (camera == null || _all.Count == 0) return null;
            var ray = camera.ScreenPointToRay(screenPosition);
            foreach (var entrance in _all)
            {
                var box = entrance != null ? entrance.GetComponent<Collider>() : null;
                if (box != null && box.Raycast(ray, out _, 1000f)) return entrance;
            }
            return null;
        }
    }
}
