using System;
using System.Collections;
using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Research;
using CityBuilder.Citizens;
using CityBuilder.Resources;
using CityBuilder.UI;
using UnityEngine;

namespace CityBuilder.Shop
{
    /// <summary>
    /// The sandbox offer shop (the user, 2026-11-11; card Design/features/monetization.md): three
    /// bundles from the «Наборы» tab on real-time timers (<see cref="OfferRotation"/>), paid with
    /// crystals or a rewarded ad (a stand-in that waits shop_ad_stub_seconds until there is an ad
    /// network), sometimes a free «Подарок короля», and once per account the starter gift in the
    /// first sandbox game. Sandbox only: off in Invasion, the tutorial and a dungeon. Per session:
    /// a new game rolls new bundles. Lives on the gameplay scene's UI root (SetupProjectOfferShop).
    /// </summary>
    public class OfferShop : MonoBehaviour
    {
        public enum Pay { Free, Crystals, Ad, Money }

        private const string StarterGivenPref = "shop.starter_given";
        private const string CrystalsResource = "Crystals";

        public static OfferShop Instance { get; private set; }

        /// <summary>The slot number the «Не хватает ресурсов» offer goes by in AdPlayingSlot and Given.</summary>
        public const int ShortageSlot = 99;

        /// <summary>The slots changed or one was taken: redraw.</summary>
        public event Action Changed;
        /// <summary>A free bundle came up: the loud notice on the left.</summary>
        public event Action FreeArrived;
        /// <summary>A bundle was given from this slot (the window flies its goods to where they went).</summary>
        public event Action<int, BalanceConfig.OfferBundle> Given;

        public OfferRotation Rotation { get; private set; }
        /// <summary>The slot whose ad is playing, or -1.</summary>
        public int AdPlayingSlot { get; private set; } = -1;
        public double Now => Time.realtimeSinceStartupAsDouble;

        private BalanceConfig _config;
        private double _starterAt = -1;
        private float _levelCheck;
        private int _level = 1;

        /// <summary>The shop works here: a sandbox game, not a run, the tutorial or a dungeon.</summary>
        public static bool Allowed =>
            !Invasion.InvasionMode.IsActive && !Tutorial.TutorialMode.IsActive && !Dungeon.DungeonMode.IsActive && !Campaign.CityDefenceMode.IsActive;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _config = BalanceConfig.Sandbox;
            Rotation = new OfferRotation(_config.OfferBundles, OfferRotation.DefaultPlaces, _config.ShopSlotSeconds, _config.ShopSlotStaggerSeconds,
                _config.ShopBonusChance, _config.ShopBonusMinSeconds, Environment.TickCount);
            _level = CurrentLevel();
            Rotation.Start(Now, _level);
            if (PlayerPrefs.GetInt(StarterGivenPref, 0) == 0) _starterAt = Now + _config.ShopStarterDelaySeconds;
            Changed?.Invoke();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Rotation == null || !Allowed) return;
            _levelCheck -= Time.unscaledDeltaTime;
            if (_levelCheck <= 0f)
            {
                _levelCheck = 1f;
                _level = CurrentLevel();
            }
            var now = Now;
            var changed = Rotation.Tick(now, _level, out var bonus);
            if (_starterAt >= 0 && now >= _starterAt && Rotation.PutStarter(now, _level))
            {
                _starterAt = -1;
                changed = true;
                bonus = true;
            }
            if (changed) Changed?.Invoke();
            if (bonus) FreeArrived?.Invoke();
        }

        private static int CurrentLevel()
        {
            var lab = ResearchManager.Instance != null ? ResearchManager.Instance.LabLevel : 0;
            var citizens = CitizenManager.Instance != null ? CitizenManager.Instance.TotalPopulation : 0;
            return OfferRotation.LevelFor(BalanceConfig.Sandbox.OfferLevels, lab, citizens);
        }

        /// <summary>Takes the slot's bundle the chosen way. False if it cannot be (taken, no crystals, an ad already playing).</summary>
        public bool Buy(int slot, Pay pay)
        {
            if (Rotation == null || slot < 0 || slot >= Rotation.SlotCount) return false;
            var s = Rotation.Slots[slot];
            if (s.Bundle == null || s.Taken || AdPlayingSlot >= 0) return false;
            if (s.IsFree != (pay == Pay.Free)) return false;
            // crystals for money: no store yet (the user, 2026-11-11: a stand-in) -- the window says so
            if (s.IsPaid || pay == Pay.Money) return false;
            if (s.IsAdOnly && pay != Pay.Ad) return false;
            switch (pay)
            {
                case Pay.Crystals:
                    if (Wallet.UpgradeCrystals < s.Bundle.priceCrystals)
                    {
                        Feed("#shop_no_crystals");
                        return false;
                    }
                    Wallet.UpgradeCrystals -= s.Bundle.priceCrystals;
                    Give(s, slot);
                    return true;
                case Pay.Ad:
                    if (!RewardedAds.IsReady) return false;
                    // a goods bundle worth more than shop_ad_max_ads ads: crystals only
                    if (!s.IsAdOnly && !OfferRotation.AdAllowed(s.Bundle, _config.ShopAdMaxAds)) return false;
                    StartCoroutine(PlayAd(slot, s.Bundle));
                    return true;
                default:
                    if (s.Bundle.kind == OfferRotation.KindStarter)
                    {
                        PlayerPrefs.SetInt(StarterGivenPref, 1);
                        PlayerPrefs.Save();
                    }
                    Give(s, slot);
                    return true;
            }
        }

        /// <summary>
        /// The «Не хватает ресурсов» offer for this price, or null if nothing is missing (or the shop is
        /// off here): the shortfall of each resource, priced by shop_price10_* (the user, 2026-11-11).
        /// </summary>
        public BalanceConfig.OfferBundle ShortageFor(IEnumerable<ResourceAmount> cost)
        {
            if (cost == null || !Allowed || ResourceManager.Instance == null) return null;
            var list = new List<ResourceAmount>(cost);
            if (ResourceManager.Instance.HasEnough(list)) return null;
            var missing = new List<BalanceConfig.OfferGood>();
            foreach (var c in list)
            {
                var lack = c.amount - ResourceManager.Instance.GetAmount(c.type);
                if (lack <= 0) continue;
                var found = missing.Find(g => g.resource == c.type.ToString());
                if (found != null) found.amount += lack;
                else missing.Add(new BalanceConfig.OfferGood { resource = c.type.ToString(), amount = lack });
            }
            if (missing.Count == 0) return null;
            var config = _config ?? BalanceConfig.Sandbox;
            return OfferRotation.ShortageBundle(missing,
                r => Enum.TryParse<ResourceType>(r, out var t) ? config.ShopPrice10(t) : 0f, config.ShopAdCrystalsBase);
        }

        /// <summary>Takes the «Не хватает ресурсов» offer: crystals, or an ad while it is worth at most shop_ad_max_ads ads. The goods go in past the storehouse ceiling.</summary>
        public bool BuyShortage(BalanceConfig.OfferBundle bundle, Pay pay)
        {
            if (bundle == null || bundle.kind != OfferRotation.KindShortage || AdPlayingSlot >= 0) return false;
            var config = _config ?? BalanceConfig.Sandbox;
            switch (pay)
            {
                case Pay.Crystals:
                    if (Wallet.UpgradeCrystals < bundle.priceCrystals)
                    {
                        Feed("#shop_no_crystals");
                        return false;
                    }
                    Wallet.UpgradeCrystals -= bundle.priceCrystals;
                    GiveShortage(bundle);
                    return true;
                case Pay.Ad:
                    if (!RewardedAds.IsReady || !OfferRotation.AdAllowed(bundle, config.ShopAdMaxAds)) return false;
                    StartCoroutine(PlayShortageAd(bundle));
                    return true;
                default:
                    return false;
            }
        }

        private IEnumerator PlayShortageAd(BalanceConfig.OfferBundle bundle)
        {
            AdPlayingSlot = ShortageSlot;
            Changed?.Invoke();
            yield return new WaitForSecondsRealtime((_config ?? BalanceConfig.Sandbox).ShopAdStubSeconds);
            AdPlayingSlot = -1;
            GiveShortage(bundle);
        }

        private void GiveShortage(BalanceConfig.OfferBundle bundle)
        {
            foreach (var good in bundle.goods)
                if (Enum.TryParse<ResourceType>(good.resource, out var type) && ResourceManager.Instance != null)
                    ResourceManager.Instance.AddOverCapacity(type, good.amount);
            Given?.Invoke(ShortageSlot, bundle);
            Changed?.Invoke();
        }

        /// <summary>The rewarded ad's stand-in: a few real seconds, then the reward (the slot may have changed meanwhile -- the bundle watched is what is given).</summary>
        private IEnumerator PlayAd(int slot, BalanceConfig.OfferBundle bundle)
        {
            AdPlayingSlot = slot;
            Changed?.Invoke();
            yield return new WaitForSecondsRealtime(_config.ShopAdStubSeconds);
            AdPlayingSlot = -1;
            var s = Rotation.Slots[slot];
            if (s.Bundle == bundle) Give(s, slot);
            else Give(new OfferSlot { Bundle = bundle }, slot);
            Changed?.Invoke();
        }

        private void Give(OfferSlot slot, int index)
        {
            slot.Taken = true;
            var lost = false;
            foreach (var good in slot.Bundle.goods)
            {
                if (good.resource == CrystalsResource)
                {
                    Wallet.UpgradeCrystals += good.amount;
                    continue;
                }
                if (!Enum.TryParse<ResourceType>(good.resource, out var type) || ResourceManager.Instance == null) continue;
                if (ResourceManager.Instance.Add(type, good.amount) < good.amount) lost = true;
            }
            if (lost) Feed("#shop_storage_full");
            Given?.Invoke(index, slot.Bundle);
            Changed?.Invoke();
        }

        private static void Feed(string key)
        {
            if (EventLogManager.Instance != null) EventLogManager.Instance.Log(Localization.Get(key));
        }
    }
}
