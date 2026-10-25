using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// The game's heroes -- the King, the Queen, the Captain (Hero) -- one of each at most, in
    /// either mode (the user, 2026-10-25: the sandbox has them too). They are bought in the castle,
    /// the Town Hall standing (BuildingInstance.TownHall): each for &lt;id&gt;_price coins from the
    /// castle's &lt;id&gt;_castle_level on -- the sandbox tab's numbers, where the King has a price too
    /// ("все только за монеты"); a run gives its King for nothing at the start (InvasionMode).
    /// They stand before the castle, guard it when let go of, and come back to it after a fall.
    /// </summary>
    public class HeroRoster : MonoBehaviour
    {
        /// <summary>Why a hero can or cannot be bought now.</summary>
        public enum Offer { Bought, Owned, CastleTooLow, NotEnoughMoney, NoSuchHero }

        /// <summary>What heroes cost: coins, in both modes.</summary>
        public const ResourceType Money = ResourceType.Coins;

        /// <summary>Where the first hero stands and comes back to: in front of the castle, towards the south.</summary>
        private static readonly Vector3 HomeOffset = new Vector3(0f, 0f, -4.5f);

        public static HeroRoster Instance { get; private set; }

        private readonly List<Hero> _heroes = new List<Hero>();

        /// <summary>The heroes there are, in the order they came.</summary>
        public IReadOnlyList<Hero> Heroes => _heroes;

        /// <summary>A hero joined (the portraits listen).</summary>
        public event Action HeroesChanged;

        /// <summary>Where they are bought and come back to: the Town Hall standing, or null.</summary>
        public static BuildingInstance Castle => BuildingInstance.TownHall;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>The hero of this kind if there is one, else null.</summary>
        public Hero HeroOf(SoldierType type)
        {
            foreach (var hero in _heroes)
            {
                if (hero != null && hero.Type == type) return hero;
            }
            return null;
        }

        /// <summary>Whether this hero can be bought now: one of each, a castle high enough, the coins.</summary>
        public Offer CanBuy(SoldierType type)
        {
            var balance = BalanceConfig.Instance.Hero(SoldierStats.SheetIdOf(type));
            if (balance == null || !SoldierStats.IsHero(type)) return Offer.NoSuchHero;
            if (HeroOf(type) != null) return Offer.Owned;
            var castle = Castle;
            if (castle == null || castle.Level < balance.castleLevel) return Offer.CastleTooLow;
            var money = ResourceManager.Instance;
            if (balance.price > 0 && (money == null || !money.HasEnough(PriceOf(balance)))) return Offer.NotEnoughMoney;
            return Offer.Bought;
        }

        /// <summary>Buys the hero if it can be bought: pays, and he steps out before the castle. The answer says why not.</summary>
        public Offer Buy(SoldierType type)
        {
            var offer = CanBuy(type);
            if (offer != Offer.Bought) return offer;
            var balance = BalanceConfig.Instance.Hero(SoldierStats.SheetIdOf(type));
            if (balance.price > 0 && !ResourceManager.Instance.TrySpend(PriceOf(balance))) return Offer.NotEnoughMoney;
            Spawn(type);
            return Offer.Bought;
        }

        private static List<ResourceAmount> PriceOf(HeroBalance balance) =>
            new List<ResourceAmount> { new ResourceAmount { type = Money, amount = balance.price } };

        /// <summary>A hero for nothing, before the castle -- a run's King at the start.</summary>
        public Hero Spawn(SoldierType type) => SpawnAt(type, null);

        /// <summary>A hero a save puts back where he stood, as he was.</summary>
        public Hero Restore(SoldierType type, Vector3 position, int level, int experience, int health, bool fallen, float respawnRemaining)
        {
            if (!SoldierStats.IsHero(type) || HeroOf(type) != null) return null;
            var hero = SpawnAt(type, position);
            if (hero != null) hero.Restore(level, experience, health, fallen, respawnRemaining);
            return hero;
        }

        private Hero SpawnAt(SoldierType type, Vector3? position)
        {
            if (ArmyManager.Instance == null) return null;
            var home = HomeFor(_heroes.Count);
            var hero = ArmyManager.Instance.SpawnHero(type, position ?? home);
            if (hero == null) return null;
            hero.Home = home;
            hero.GuardCentre = CastleCentre;
            _castleSeen = Castle;
            _heroes.Add(hero);
            HeroesChanged?.Invoke();
            return hero;
        }

        private static Vector3 CastleCentre => Castle != null ? Castle.transform.position : Vector3.zero;

        /// <summary>Where the index-th hero stands: before the castle, the second and the third beside the first, not on him.</summary>
        private static Vector3 HomeFor(int index)
        {
            var home = CastleCentre + HomeOffset;
            if (GridManager.Instance != null) home.y = GridManager.Instance.GroundHeight;
            if (index > 0) home += new Vector3(index % 2 == 1 ? -1.4f : 1.4f, 0f, 0f) * ((index + 1) / 2);
            return home;
        }

        private BuildingInstance _castleSeen;

        private void Update()
        {
            // a save puts the heroes back before the castle may be there, and a castle can be
            // rebuilt elsewhere: follow it
            var castle = Castle;
            if (castle == _castleSeen || castle == null) return;
            _castleSeen = castle;
            for (var i = 0; i < _heroes.Count; i++)
            {
                if (_heroes[i] == null) continue;
                _heroes[i].Home = HomeFor(i);
                _heroes[i].GuardCentre = castle.transform.position;
            }
        }
    }
}
