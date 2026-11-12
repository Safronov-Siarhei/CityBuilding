using System;
using System.Collections;
using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Shop
{
    /// <summary>
    /// The shop's «Бустеры» tab (the user, 2026-11-12; card monetization.md): booster_slots places, no
    /// booster twice at once; each for an ad OR crystals, the same booster either way. A taken place
    /// recharges for booster_slot_recharge_sec (real time) and comes back with another booster -- one
    /// not in the other places and not the one just taken. Per session, like the offers: a new game deals
    /// new boosters. Also runs the running boosters' clocks (Boosters.Tick, the game's own time). Lives
    /// beside the OfferShop on the gameplay scene's UI root (SetupProjectOfferShop).
    /// </summary>
    public class BoosterShop : MonoBehaviour
    {
        public class Slot
        {
            public string Id;
            public bool Taken;
            /// <summary>Real time (Now) the taken place comes back.</summary>
            public double ReadyAt;
        }

        public static BoosterShop Instance { get; private set; }

        /// <summary>A place changed or was taken: redraw.</summary>
        public event Action Changed;

        public readonly List<Slot> Slots = new List<Slot>();
        /// <summary>The place whose ad is playing, or -1.</summary>
        public int AdPlayingSlot { get; private set; } = -1;
        public double Now => Time.realtimeSinceStartupAsDouble;

        private System.Random _random;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Boosters.SaveLeft();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Boosters.SaveLeft();
        }

        private void Start()
        {
            _random = new System.Random(Environment.TickCount);
            var count = Mathf.Min(BalanceConfig.Sandbox.BoosterSlots, Boosters.All.Count);
            for (var i = 0; i < count; i++) Slots.Add(new Slot { Id = Pick(null) });
            Changed?.Invoke();
        }

        private void Update()
        {
            if (!OfferShop.Allowed) return;
            // the running boosters' time: the game's own clock, so a pause stops it (the user, 2026-11-12)
            Boosters.Tick(Time.deltaTime);
            var now = Now;
            var changed = false;
            foreach (var slot in Slots)
            {
                if (!slot.Taken || now < slot.ReadyAt) continue;
                slot.Id = Pick(slot.Id);
                slot.Taken = false;
                changed = true;
            }
            if (changed) Changed?.Invoke();
        }

        /// <summary>A booster none of the places shows, and not `previous`; the least bad one if all are out.</summary>
        private string Pick(string previous)
        {
            var free = new List<string>();
            foreach (var def in Boosters.All)
            {
                if (def.id == previous) continue;
                var shown = false;
                foreach (var slot in Slots)
                    if (slot.Id == def.id) shown = true;
                if (!shown) free.Add(def.id);
            }
            if (free.Count == 0) return previous;
            return free[_random.Next(free.Count)];
        }

        /// <summary>The place's booster into the stock, paid this way. False if it cannot be (taken, too few crystals, no ad, an ad playing).</summary>
        public bool Buy(int index, OfferShop.Pay pay, bool useAtOnce = false)
        {
            if (index < 0 || index >= Slots.Count || AdPlayingSlot >= 0) return false;
            var slot = Slots[index];
            var def = Boosters.Def(slot.Id);
            if (def == null || slot.Taken) return false;
            switch (pay)
            {
                case OfferShop.Pay.Crystals:
                    if (Wallet.UpgradeCrystals < def.priceCrystals) return false;
                    Wallet.UpgradeCrystals -= def.priceCrystals;
                    Give(index, useAtOnce);
                    return true;
                case OfferShop.Pay.Ad:
                    if (!def.ad || !RewardedAds.IsReady) return false;
                    StartCoroutine(PlayAd(index, slot.Id, useAtOnce));
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>The place showing this booster, or -1 (the hints' «есть в Лавке»).</summary>
        public int SlotOf(string id)
        {
            for (var i = 0; i < Slots.Count; i++)
                if (Slots[i].Id == id && !Slots[i].Taken) return i;
            return -1;
        }

        private IEnumerator PlayAd(int index, string id, bool useAtOnce)
        {
            AdPlayingSlot = index;
            Changed?.Invoke();
            yield return new WaitForSecondsRealtime(BalanceConfig.Sandbox.ShopAdStubSeconds);
            AdPlayingSlot = -1;
            // the place may have changed meanwhile: the booster watched for is what is given
            if (Slots[index].Id == id && !Slots[index].Taken) Give(index, useAtOnce);
            else
            {
                Boosters.AddStock(id, 1);
                if (useAtOnce) Boosters.Use(id);
            }
            Changed?.Invoke();
        }

        private void Give(int index, bool useAtOnce)
        {
            var slot = Slots[index];
            slot.Taken = true;
            slot.ReadyAt = Now + BalanceConfig.Sandbox.BoosterSlotRechargeSeconds;
            Boosters.AddStock(slot.Id, 1);
            if (useAtOnce) Boosters.Use(slot.Id);
            Changed?.Invoke();
        }
    }
}
