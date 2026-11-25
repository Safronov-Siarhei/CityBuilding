using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

namespace CityBuilder.Core
{
    /// <summary>
    /// Google Play Billing behind <see cref="Store"/> (Unity IAP 5, the user 2026-11-25: «заняться подключением
    /// кристаллов»). Live only in an Android player; the editor and Windows keep the Store's stand-in (a purchase given
    /// at once).
    ///
    ///   - Products: every Store id, consumable (packs, sale packs, piggy levels) or not (the mine's two, freedom).
    ///     The ids in Play Console must be the same strings.
    ///   - Prices: the string Google returns, in the player's money; until it comes the Store shows its USD stand-in.
    ///   - A purchase: PurchaseProduct -> OnPurchasePending -> Store.Grant -> ConfirmPurchase. The grant waits for
    ///     <see cref="CloudSave.Ready"/> (a cloud snapshot must not wipe it) and is done once per transaction id even
    ///     when Google hands the same order again after a restart.
    ///   - A payment on its way (cash countries): OnPurchaseDeferred -> Store.MarkPending; the money comes -> pending.
    ///   - Refunds of the mine / freedom: at every start the purchases are fetched; an owned flag without an order
    ///     behind it is taken back (Store.Revoke). Only after a SUCCESSFUL fetch, never on an error.
    /// </summary>
    public static class Billing
    {
        private const string GrantedKey = "store.granted_orders";
        private const int GrantedKept = 100;

        private static StoreController _store;
        private static readonly Queue<PendingOrder> Held = new Queue<PendingOrder>();

        /// <summary>Real Google Play billing is in charge (an Android player whose store connected).</summary>
        public static bool Live { get; private set; }

        /// <summary>Google's price for a product, or null until the products came.</summary>
        public static string PriceOf(string id)
        {
            if (!Live || _store == null) return null;
            var product = _store.GetProductById(id);
            var text = product?.metadata?.localizedPriceString;
            return string.IsNullOrEmpty(text) ? null : text;
        }

        /// <summary>Starts a purchase. False when billing cannot take it now (not connected, unknown product).</summary>
        public static bool Purchase(string id)
        {
            if (!Live || _store == null) return false;
            var product = _store.GetProductById(id);
            if (product == null || !product.availableToPurchase) { Debug.LogWarning("Billing: no product " + id); return false; }
            _store.PurchaseProduct(product);
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static async void Boot()
        {
            if (Application.isEditor || Application.platform != RuntimePlatform.Android) return;
            _store = UnityIAPServices.StoreController();
            _store.OnPurchasePending += OnPending;
            _store.OnPurchaseConfirmed += OnConfirmed;
            _store.OnPurchaseFailed += failed => Debug.Log($"Billing: purchase failed {failed.FailureReason} {failed.Details}");
            _store.OnPurchaseDeferred += OnDeferred;
            _store.OnStoreConnected += OnConnected;
            _store.OnStoreDisconnected += failure => Debug.LogWarning("Billing: store disconnected: " + failure.Message);
            _store.OnProductsFetched += _ => { Live = true; Store.RaiseChanged(); };
            _store.OnProductsFetchFailed += failure => Debug.LogWarning("Billing: products not fetched: " + failure.FailureReason);
            _store.OnPurchasesFetched += OnPurchasesFetched;
            _store.OnPurchasesFetchFailed += failure => Debug.LogWarning("Billing: purchases not fetched: " + failure.Message);
            CloudSave.BecameReady += ReleaseHeld;
            // the cloud put other flags in place: compare them with Google's orders again
            CloudSave.Applied += () => { if (Live) _store.FetchPurchases(); };
            try { await _store.Connect(); }
            catch (Exception e) { Debug.LogWarning("Billing: connect failed: " + e.Message); }
        }

        private static void OnConnected()
        {
            var defs = new List<ProductDefinition>();
            foreach (var (id, consumable) in Store.Catalog())
                defs.Add(new ProductDefinition(id, consumable ? ProductType.Consumable : ProductType.NonConsumable));
            _store.FetchProducts(defs);
            _store.FetchPurchases();
        }

        private static void OnPending(PendingOrder order)
        {
            if (!CloudSave.Ready) { Held.Enqueue(order); return; }
            GrantAndConfirm(order);
        }

        private static void ReleaseHeld()
        {
            while (Held.Count > 0) GrantAndConfirm(Held.Dequeue());
        }

        private static void GrantAndConfirm(PendingOrder order)
        {
            var transaction = order.Info?.TransactionID ?? "";
            if (!WasGranted(transaction))
            {
                foreach (var item in order.CartOrdered.Items())
                {
                    var id = item.Product?.definition?.id;
                    if (!string.IsNullOrEmpty(id)) Store.Grant(id);
                }
                RememberGranted(transaction);
                PlayerPrefs.Save();
            }
            _store.ConfirmPurchase(order);
        }

        private static void OnConfirmed(Order order)
        {
            if (order is FailedOrder failed) Debug.LogWarning($"Billing: confirm failed {failed.FailureReason} {failed.Details}");
        }

        private static void OnDeferred(DeferredOrder order)
        {
            foreach (var item in order.CartOrdered.Items())
            {
                var id = item.Product?.definition?.id;
                if (!string.IsNullOrEmpty(id)) Store.MarkPending(id, true);
            }
        }

        /// <summary>A refund of a one-time product: its flag is on but Google no longer has an order for it.</summary>
        private static void OnPurchasesFetched(Orders orders)
        {
            var owned = new HashSet<string>();
            foreach (var order in orders.ConfirmedOrders.Cast<Order>().Concat(orders.PendingOrders))
                foreach (var item in order.CartOrdered.Items())
                    if (item.Product?.definition?.id != null) owned.Add(item.Product.definition.id);
            var deferred = new HashSet<string>();
            foreach (var order in orders.DeferredOrders)
                foreach (var item in order.CartOrdered.Items())
                    if (item.Product?.definition?.id != null) deferred.Add(item.Product.definition.id);

            var mine = owned.Contains(Store.Mine) || owned.Contains(Store.MineSale);
            if (Wallet.CrystalMineOwned && !mine) Store.Revoke(Store.Mine);
            if (Wallet.FreedomOwned && !owned.Contains(Store.Freedom)) Store.Revoke(Store.Freedom);
            // owned at Google but not here (a new phone without the cloud): the flag back, no second gift
            if (mine && !Wallet.CrystalMineOwned) Wallet.CrystalMineOwned = true;
            if (owned.Contains(Store.Freedom) && !Wallet.FreedomOwned) Wallet.FreedomOwned = true;
            // a pending flag whose payment is gone (cancelled at the kiosk) is cleared
            foreach (var (id, _) in Store.Catalog())
                if (Store.IsPending(id) && !deferred.Contains(id) && !owned.Contains(id)) Store.MarkPending(id, false);
        }

        private static bool WasGranted(string transaction) =>
            !string.IsNullOrEmpty(transaction) && PlayerPrefs.GetString(GrantedKey, "").Split('\n').Contains(transaction);

        private static void RememberGranted(string transaction)
        {
            if (string.IsNullOrEmpty(transaction)) return;
            var list = PlayerPrefs.GetString(GrantedKey, "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            list.Add(transaction);
            if (list.Count > GrantedKept) list.RemoveRange(0, list.Count - GrantedKept);
            Prefs.SetString(GrantedKey, string.Join("\n", list));
        }
    }
}
