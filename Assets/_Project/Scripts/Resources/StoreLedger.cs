using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Resources
{
    /// <summary>
    /// The sandbox's stores (the user, 2026-11-17; card Design/features/resources-and-storage.md «Storage rework»).
    /// Each Склад, Кладовая and Сокровищница (and their Big ones) holds ITS OWN amounts, up to a cap per resource
    /// and level from the storage tab (storage.csv); the caps of all stores add up, and that sum is the town's
    /// ceiling (ResourceManager.GetCapacity). No store, no room: the Town Hall holds nothing of its own.
    ///
    /// ResourceManager's totals stay the one truth everything reads; this only says WHERE the total lies. Every
    /// change of a total comes through <see cref="Sync"/>: a gain goes into the stores nearest where it was made
    /// (<see cref="ResourceManager.AddProduced(ResourceType,int,Vector3)"/>; the Town Hall when nobody says), as
    /// far as they have room; what does not fit (a purchase, a gift, a reward may overfill) lies «in the Town
    /// Hall» -- the overflow -- and is never lost. A price is paid from the overflow first, then from the
    /// fullest store.
    ///
    /// A store burnt down loses what lay in it (a red «-N» over it); its ruin keeps half, rounded up, for the
    /// restore. A store demolished by hand moves its goods into the others where they fit; the rest is lost.
    /// A store under construction holds nothing; one under an upgrade keeps its old level's caps (the level
    /// only comes when the work is done). One shared ledger for the whole archipelago.
    /// </summary>
    public static class StoreLedger
    {
        private static readonly int Types = Enum.GetValues(typeof(ResourceType)).Length;

        private static readonly List<BuildingInstance> Stores = new List<BuildingInstance>();
        private static readonly Dictionary<BuildingInstance, int[]> Stock = new Dictionary<BuildingInstance, int[]>();
        private static int[] _overflow = new int[Types];
        private static readonly int[] Capacity = new int[Types];
        private static bool _capacityDirty = true;

        // building id -> [resource, level 0..4]
        private static Dictionary<string, int[,]> _caps;
        private static BalanceConfig _capsFrom;
        private static readonly bool[] Tracked = new bool[Types];

        /// <summary>The sandbox (not a run, the campaign, the tutorial or a dungeon).</summary>
        public static bool Sandbox =>
            PiggyTasks.InSandbox && !Dungeon.DungeonMode.IsActive
            && (ResourceManager.Instance == null || !ResourceManager.Instance.UnlimitedStorage);

        /// <summary>
        /// The stores hold the goods: the sandbox, once the town has had a store (the start chain gives the first one
        /// at once). Before that -- and in a scene that never gets one, as the tests' -- the old family pools stand.
        /// Losing every store later does not switch it off.
        /// </summary>
        public static bool Active => _everHadStore && Sandbox;

        private static bool _everHadStore;

        /// <summary>A new scene: nothing stands yet.</summary>
        public static void Reset()
        {
            Stores.Clear();
            Stock.Clear();
            _overflow = new int[Types];
            _capacityDirty = true;
            _caps = null;
            _everHadStore = false;
        }

        // ---------------------------------------------------------------- caps

        private static void EnsureCaps()
        {
            var config = BalanceConfig.Instance;
            if (_caps != null && _capsFrom == config) return;
            _capsFrom = config;
            _caps = new Dictionary<string, int[,]>();
            Array.Clear(Tracked, 0, Tracked.Length);
            if (config == null) return;
            foreach (var row in config.StoreCapRows)
            {
                if (row == null || string.IsNullOrEmpty(row.building)) continue;
                if (!_caps.TryGetValue(row.building, out var table)) _caps[row.building] = table = new int[Types, 5];
                foreach (var type in row.resources)
                {
                    var t = (int)type;
                    if (t < 0 || t >= Types) continue;
                    Tracked[t] = true;
                    for (var l = 0; l < 5; l++) table[t, l] = row.caps != null && l < row.caps.Length ? row.caps[l] : 0;
                }
            }
        }

        /// <summary>Whether this building is one of the stores (it has a row in the storage tab).</summary>
        public static bool IsStore(BuildingData data)
        {
            if (data == null) return false;
            EnsureCaps();
            return _caps.ContainsKey(data.buildingName);
        }

        /// <summary>Whether the stores keep this resource at all (crystals, people and the like are not kept).</summary>
        public static bool Keeps(ResourceType type)
        {
            EnsureCaps();
            var t = (int)type;
            return t >= 0 && t < Types && Tracked[t];
        }

        /// <summary>What one store of this kind holds of `type` at `level` (1-5).</summary>
        public static int CapOf(string buildingId, ResourceType type, int level)
        {
            EnsureCaps();
            if (string.IsNullOrEmpty(buildingId) || !_caps.TryGetValue(buildingId, out var table)) return 0;
            return table[(int)type, Mathf.Clamp(level, 1, 5) - 1];
        }

        /// <summary>What this store holds of `type` now: nothing while it is being built, its standing level's while it is upgraded.</summary>
        public static int CapOf(BuildingInstance store, ResourceType type)
        {
            if (store == null || store.Data == null || !store.StorageReady) return 0;
            return CapOf(store.Data.buildingName, type, store.Level);
        }

        /// <summary>The town's ceiling for `type`: every store's cap added up.</summary>
        public static int CapacityOf(ResourceType type)
        {
            if (_capacityDirty)
            {
                _capacityDirty = false;
                Array.Clear(Capacity, 0, Capacity.Length);
                for (var i = Stores.Count - 1; i >= 0; i--)
                {
                    var store = Stores[i];
                    if (store == null) { Stores.RemoveAt(i); continue; }
                    for (var t = 0; t < Types; t++) Capacity[t] += CapOf(store, (ResourceType)t);
                }
            }
            return Capacity[(int)type];
        }

        /// <summary>A store built, finished, upgraded or lost: the ceiling is counted again.</summary>
        public static void MarkCapacityDirty() => _capacityDirty = true;

        // ---------------------------------------------------------------- stores

        /// <summary>The player's stores standing (and being built) on the map.</summary>
        public static IReadOnlyList<BuildingInstance> All => Stores;

        public static void Register(BuildingInstance store)
        {
            if (store == null || store.IsBot || !IsStore(store.Data) || Stock.ContainsKey(store)) return;
            Stores.Add(store);
            Stock[store] = new int[Types];
            _capacityDirty = true;
            if (_everHadStore) return;
            // the first store: from now on the stores hold everything -- what the town has is laid into it (the rest
            // lies in the Town Hall); a load then puts each store's own goods back over this (Load, Restored)
            _everHadStore = true;
            var resources = ResourceManager.Instance;
            if (resources == null || !Sandbox) return;
            for (var t = 0; t < Types; t++) Sync((ResourceType)t, resources.GetAmount((ResourceType)t), null);
        }

        /// <summary>
        /// A store gone without its own goodbye (OnStoreLost / OnStoreDemolished): whatever still lay in it goes
        /// to the overflow, so a total is never silently wrong.
        /// </summary>
        public static void Unregister(BuildingInstance store)
        {
            if (store == null || !Stock.TryGetValue(store, out var stock)) return;
            for (var t = 0; t < Types; t++) _overflow[t] += stock[t];
            Stock.Remove(store);
            Stores.Remove(store);
            _capacityDirty = true;
        }

        /// <summary>What lies in this store of `type`.</summary>
        public static int AmountIn(BuildingInstance store, ResourceType type) =>
            store != null && Stock.TryGetValue(store, out var stock) ? stock[(int)type] : 0;

        /// <summary>What lies «in the Town Hall» above the stores' room.</summary>
        public static int Overflow(ResourceType type) => _overflow[(int)type];

        // ---------------------------------------------------------------- the totals

        private static int Placed(int t)
        {
            var sum = _overflow[t];
            foreach (var pair in Stock) sum += pair.Value[t];
            return sum;
        }

        /// <summary>
        /// The total of `type` became `total`: the difference is laid into the stores nearest `at` (or the Town
        /// Hall) as far as they have room, the rest into the overflow -- or, for a loss, taken from the overflow
        /// first and then from the fullest store.
        /// </summary>
        public static void Sync(ResourceType type, int total, Vector3? at)
        {
            if (!Keeps(type)) return;
            var t = (int)type;
            var delta = total - Placed(t);
            if (delta > 0) Lay(t, delta, at);
            else if (delta < 0) Take(t, -delta);
        }

        private static readonly List<BuildingInstance> ByDistance = new List<BuildingInstance>();

        private static void Lay(int t, int amount, Vector3? at)
        {
            var from = at ?? (BuildingInstance.TownHall != null ? BuildingInstance.TownHall.transform.position : Vector3.zero);
            ByDistance.Clear();
            foreach (var store in Stores)
                if (store != null && CapOf(store, (ResourceType)t) > Stock[store][t]) ByDistance.Add(store);
            ByDistance.Sort((a, b) => (a.transform.position - from).sqrMagnitude.CompareTo((b.transform.position - from).sqrMagnitude));
            foreach (var store in ByDistance)
            {
                if (amount <= 0) break;
                var stock = Stock[store];
                var room = CapOf(store, (ResourceType)t) - stock[t];
                var put = Mathf.Min(room, amount);
                stock[t] += put;
                amount -= put;
            }
            if (amount > 0) _overflow[t] += amount;
            ByDistance.Clear();
        }

        private static void Take(int t, int amount)
        {
            var fromOverflow = Mathf.Min(_overflow[t], amount);
            _overflow[t] -= fromOverflow;
            amount -= fromOverflow;
            while (amount > 0)
            {
                BuildingInstance fullest = null;
                var most = 0;
                foreach (var pair in Stock)
                    if (pair.Value[t] > most) { most = pair.Value[t]; fullest = pair.Key; }
                if (fullest == null) break;
                var take = Mathf.Min(most, amount);
                Stock[fullest][t] -= take;
                amount -= take;
            }
        }

        // ---------------------------------------------------------------- losing a store

        /// <summary>
        /// The store burnt down: its goods are lost (a red «-N» for each) and the totals drop. Returns half of it,
        /// rounded up in the player's favour, for its ruin to give back when it is restored.
        /// </summary>
        public static List<ResourceAmount> OnStoreLost(BuildingInstance store)
        {
            var keep = new List<ResourceAmount>();
            if (store == null || !Stock.TryGetValue(store, out var stock)) return keep;
            var resources = ResourceManager.Instance;
            var at = store.transform.position;
            for (var t = 0; t < Types; t++)
            {
                var lost = stock[t];
                if (lost <= 0) continue;
                stock[t] = 0;
                keep.Add(new ResourceAmount { type = (ResourceType)t, amount = (lost + 1) / 2 });
                UI.ResourceGainPopups.Instance?.ShowLoss(at, lost, (ResourceType)t);
                // the goods are already out of the ledger: the total follows without touching any other store
                if (resources != null) resources.DropKept((ResourceType)t, lost);
            }
            Unregister(store);
            return keep;
        }

        /// <summary>
        /// A predator's blow (Maps/Predators, the user 2026-11-22): the store's `type` follows its health -- it keeps
        /// `share` (its health after the blow over before it) of what lay in it, the rest eaten for good (a red «-N»).
        /// Returns how much went.
        /// </summary>
        public static int Eat(BuildingInstance store, ResourceType type, float share)
        {
            if (store == null || !Stock.TryGetValue(store, out var stock)) return 0;
            var t = (int)type;
            var lost = stock[t] - Mathf.FloorToInt(stock[t] * Mathf.Clamp01(share));
            if (lost <= 0) return 0;
            stock[t] -= lost;
            UI.ResourceGainPopups.Instance?.ShowLoss(store.transform.position, lost, type);
            ResourceManager.Instance?.DropKept(type, lost);
            return lost;
        }

        /// <summary>What a hand demolition of this store would lose: what the other stores have no room for.</summary>
        public static List<ResourceAmount> WouldLose(BuildingInstance store)
        {
            var lose = new List<ResourceAmount>();
            if (store == null || !Stock.TryGetValue(store, out var stock)) return lose;
            for (var t = 0; t < Types; t++)
            {
                if (stock[t] <= 0) continue;
                var room = 0;
                foreach (var other in Stores)
                    if (other != null && other != store) room += Mathf.Max(0, CapOf(other, (ResourceType)t) - Stock[other][t]);
                var lost = stock[t] - room;
                if (lost > 0) lose.Add(new ResourceAmount { type = (ResourceType)t, amount = lost });
            }
            return lose;
        }

        /// <summary>The store demolished by hand: its goods move into the other stores where they fit, the rest is lost.</summary>
        public static void OnStoreDemolished(BuildingInstance store)
        {
            if (store == null || !Stock.TryGetValue(store, out var stock)) return;
            var resources = ResourceManager.Instance;
            var at = store.transform.position;
            var moving = (int[])stock.Clone();
            Array.Clear(stock, 0, stock.Length);
            Stock.Remove(store);
            Stores.Remove(store);
            _capacityDirty = true;
            for (var t = 0; t < Types; t++)
            {
                var amount = moving[t];
                if (amount <= 0) continue;
                // into the others, nearest first, as far as they have room
                ByDistance.Clear();
                foreach (var other in Stores)
                    if (other != null && CapOf(other, (ResourceType)t) > Stock[other][t]) ByDistance.Add(other);
                ByDistance.Sort((a, b) => (a.transform.position - at).sqrMagnitude.CompareTo((b.transform.position - at).sqrMagnitude));
                foreach (var other in ByDistance)
                {
                    if (amount <= 0) break;
                    var put = Mathf.Min(CapOf(other, (ResourceType)t) - Stock[other][t], amount);
                    Stock[other][t] += put;
                    amount -= put;
                }
                ByDistance.Clear();
                if (amount <= 0) continue;
                UI.ResourceGainPopups.Instance?.ShowLoss(at, amount, (ResourceType)t);
                if (resources != null) resources.DropKept((ResourceType)t, amount);
            }
        }

        // ---------------------------------------------------------------- save / load

        /// <summary>What lies in this store, for the save: (type, amount) pairs, nothing for an empty one.</summary>
        public static void Save(BuildingInstance store, List<ResourceAmount> into)
        {
            if (store == null || into == null || !Stock.TryGetValue(store, out var stock)) return;
            for (var t = 0; t < Types; t++)
                if (stock[t] > 0) into.Add(new ResourceAmount { type = (ResourceType)t, amount = stock[t] });
        }

        public static void SaveOverflow(List<ResourceAmount> into)
        {
            if (into == null) return;
            for (var t = 0; t < Types; t++)
                if (_overflow[t] > 0) into.Add(new ResourceAmount { type = (ResourceType)t, amount = _overflow[t] });
        }

        /// <summary>A store put back from a save: its goods as they lay (the totals are squared up in <see cref="Restored"/>).</summary>
        public static void Load(BuildingInstance store, List<ResourceAmount> stock)
        {
            if (store == null || stock == null || !Stock.TryGetValue(store, out var into)) return;
            Array.Clear(into, 0, into.Length);
            foreach (var a in stock)
                if (a != null && (int)a.type >= 0 && (int)a.type < Types) into[(int)a.type] = Mathf.Max(0, a.amount);
        }

        /// <summary>
        /// After a load. A save with the stores' goods: the overflow as it was, then the totals squared up. An older
        /// save (only totals): everything is laid into the stores nearest the Town Hall, the rest lies in it.
        /// </summary>
        public static void Restored(bool saved, List<ResourceAmount> overflow)
        {
            var resources = ResourceManager.Instance;
            if (resources == null) return;
            _overflow = new int[Types];
            if (saved && overflow != null)
                foreach (var a in overflow)
                    if (a != null && (int)a.type >= 0 && (int)a.type < Types) _overflow[(int)a.type] = Mathf.Max(0, a.amount);
            if (!saved) foreach (var pair in Stock) Array.Clear(pair.Value, 0, pair.Value.Length);
            _capacityDirty = true;
            for (var t = 0; t < Types; t++) Sync((ResourceType)t, resources.GetAmount((ResourceType)t), null);
        }
    }
}
