using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Dungeon
{
    /// <summary>The dungeon shop's potions, in the shop's and the bar's order.</summary>
    public enum PotionKind
    {
        Healing = 0,
        Rage = 1,
        Speed = 2,
        Defence = 3,
    }

    /// <summary>
    /// The potions bought at the dungeon's shop (the user, 2026-11-01): NOT drunk on purchase --
    /// each one bought is an icon under the heroes' portraits (UI/DungeonPotionBar) and a tap on it
    /// drinks it, for the whole party (the heroes and the gnomes) for the sheet's seconds: healing
    /// gives back a share of everyone's health over its time, rage strikes harder and faster, speed
    /// walks faster, defence takes a share of every blow. They work only below: the ones not drunk
    /// wait in the town's save for the next visit (DungeonRecord.Potions) and never reach the
    /// town's inventory.
    /// </summary>
    public class DungeonPotions : MonoBehaviour
    {
        public const int Kinds = 4;
        private const float HealTickSeconds = 0.5f;

        public static DungeonPotions Instance { get; private set; }

        private readonly int[] _carried = new int[Kinds];
        private float _healLeft;
        private float _healTick;
        private readonly List<SoldierUnit> _party = new List<SoldierUnit>();

        /// <summary>A potion was bought or drunk: the bar redraws.</summary>
        public event System.Action Changed;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>The potions the heroes left below last time.</summary>
        public void Begin(IList<int> carried)
        {
            for (var i = 0; i < Kinds; i++) _carried[i] = carried != null && i < carried.Count ? Mathf.Max(0, carried[i]) : 0;
            Changed?.Invoke();
        }

        public int Count(PotionKind kind) => _carried[(int)kind];

        /// <summary>The counts by kind, as the town's save keeps them.</summary>
        public List<int> Carried() => new List<int>(_carried);

        public static int Price(PotionKind kind) => BalanceConfig.Instance != null ? BalanceConfig.Instance.PotionCoins((int)kind) : 30;
        public static float Seconds(PotionKind kind) => BalanceConfig.Instance != null ? BalanceConfig.Instance.PotionSeconds((int)kind) : 10f;
        public static float Power(PotionKind kind) => BalanceConfig.Instance != null ? BalanceConfig.Instance.PotionPower((int)kind) : 1f;

        public bool CanBuy(PotionKind kind) => DungeonMode.Instance != null && DungeonMode.Instance.CanPay(Price(kind));

        /// <summary>One bought: paid for and into the bag, not drunk.</summary>
        public bool Buy(PotionKind kind)
        {
            if (!CanBuy(kind) || !DungeonMode.Instance.TrySpendCoins(Price(kind))) return false;
            _carried[(int)kind]++;
            Changed?.Invoke();
            return true;
        }

        /// <summary>One drunk: it works on everyone in the party now, for its seconds.</summary>
        public bool Drink(PotionKind kind)
        {
            if (_carried[(int)kind] <= 0) return false;
            _carried[(int)kind]--;
            var seconds = Seconds(kind);
            var power = Power(kind);
            switch (kind)
            {
                case PotionKind.Healing:
                    _healLeft = seconds;
                    _healTick = 0f;
                    break;
                default:
                    foreach (var unit in Party())
                    {
                        if (kind == PotionKind.Rage) unit.Enrage(power, seconds);
                        else if (kind == PotionKind.Speed) unit.Haste(power, seconds);
                        else unit.Ward(power, seconds);
                    }
                    break;
            }
            EventLogManager.Instance?.Post(GameEvent.Nowhere(
                string.Format(Localization.Get("#log_potion_drunk"), Localization.Get("#potion_" + kind.ToString().ToLowerInvariant()), Mathf.RoundToInt(seconds)),
                EventSeverity.Good, EventGlyph.Star, null));
            Changed?.Invoke();
            return true;
        }

        /// <summary>The healing potion's time left (0 when none works).</summary>
        public float HealingLeft => _healLeft;

        /// <summary>The heroes standing and the gnomes alive.</summary>
        private List<SoldierUnit> Party()
        {
            _party.Clear();
            if (HeroRoster.Instance != null)
                foreach (var hero in HeroRoster.Instance.Heroes)
                    if (hero != null && !hero.IsFallen && hero.Unit != null) _party.Add(hero.Unit);
            if (DungeonHirelings.Instance != null)
                foreach (var gnome in DungeonHirelings.Instance.Members)
                    if (gnome != null && gnome.CurrentHealth > 0) _party.Add(gnome);
            return _party;
        }

        private void Update()
        {
            if (_healLeft <= 0f) return;
            _healTick -= Time.deltaTime;
            _healLeft -= Time.deltaTime;
            if (_healTick > 0f) return;
            _healTick = HealTickSeconds;
            // the share of health over the potion's whole time, a slice every tick
            var share = Power(PotionKind.Healing) * HealTickSeconds / Mathf.Max(0.1f, Seconds(PotionKind.Healing));
            foreach (var unit in Party()) unit.Heal(Mathf.Max(1, Mathf.RoundToInt(unit.MaxHealth * share)));
        }
    }
}
