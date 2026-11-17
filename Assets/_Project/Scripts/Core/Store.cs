using System;
using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// What the main menu's «Магазин» sells for real money (the user, 2026-11-15; card Design/features/monetization.md
    /// «Main-menu Магазин window»), and what a purchase gives:
    ///
    ///   crystal_mine        the Crystal Mine, bought once (+menu_shop_mine_gift crystals at once);
    ///   crystal_mine_sale   the same mine, the «Подожди!» price (a product of its own: Google Play has one price a
    ///                       product). Owning EITHER is owning the mine; neither is offered again;
    ///   crystals_1..3       the crystal packs (menu_shop_pack_crystals_N);
    ///   sale_crystals_1..3  the same packs at the «Подожди!» price (menu_shop_pack_sale_usd_N), products of their own.
    ///
    /// There is NO Google Play billing yet: Buy is a stand-in that gives the product at once (a test purchase). The
    /// billing, when it comes, calls Grant on a bought product, MarkPending on a payment still on its way (cash
    /// countries) and Revoke when a purchase is taken back (a refund): the mine is locked again everywhere and its
    /// offer is back in the shop. The cheats drive Pending and Revoke until then.
    /// </summary>
    public static class Store
    {
        public const string Mine = "crystal_mine";
        public const string MineSale = "crystal_mine_sale";
        public const string PackPrefix = "crystals_";
        public const string PackSalePrefix = "sale_crystals_";
        /// <summary>«Свобода песочницы»: the sandbox's world settings, once and for good (Wallet.FreedomOwned).</summary>
        public const string Freedom = "sandbox_freedom";

        private const string PendingKey = "store.pending.";

        /// <summary>No billing yet: every Buy is a test purchase given at once.</summary>
        public static bool TestPurchases => true;

        /// <summary>A product was given (bought, or a pending payment came through).</summary>
        public static event Action<string> Granted;

        /// <summary>Anything about a product changed (given, pending, revoked): an open shop redraws.</summary>
        public static event Action Changed;

        public static string PackId(int index) => PackPrefix + (index + 1);

        public static string PackSaleId(int index) => PackSalePrefix + (index + 1);

        /// <summary>A pack's index (0-based, the sale product too), or -1 for anything else.</summary>
        public static int PackIndexOf(string id) => PackIndexOf(id, out _);

        public static int PackIndexOf(string id, out bool sale)
        {
            sale = false;
            if (id == null) return -1;
            string number;
            if (id.StartsWith(PackSalePrefix)) { sale = true; number = id.Substring(PackSalePrefix.Length); }
            else if (id.StartsWith(PackPrefix)) number = id.Substring(PackPrefix.Length);
            else return -1;
            return int.TryParse(number, out var n) && n >= 1 ? n - 1 : -1;
        }

        public static bool IsMine(string id) => id == Mine || id == MineSale;

        /// <summary>Breaking the piggy bank (PiggyBank): piggy_1..piggy_5, one product per level, each its price.</summary>
        public const string PiggyPrefix = "piggy_";

        public static string PiggyId(int level0) => PiggyPrefix + (level0 + 1);

        public static bool IsPiggy(string id) => id != null && id.StartsWith(PiggyPrefix);

        /// <summary>Crystals a product puts in the wallet: the mine's gift, a pack's crystals.</summary>
        public static int CrystalsOf(string id)
        {
            var config = BalanceConfig.Instance;
            if (config == null) return 0;
            if (IsMine(id)) return config.MenuShopMineGift;
            if (IsPiggy(id)) return PiggyBank.Stored;
            var pack = PackIndexOf(id);
            return pack >= 0 ? config.MenuShopPackCrystals(pack) : 0;
        }

        /// <summary>The stand-in price in USD (the real one will be the string Google Play returns, in the player's money).</summary>
        public static float UsdOf(string id)
        {
            var config = BalanceConfig.Instance;
            if (config == null) return 0f;
            if (id == Mine) return config.MenuShopMineUsd;
            if (id == MineSale) return config.MenuShopMineSaleUsd;
            if (id == Freedom) return config.FreedomUsd;
            if (IsPiggy(id) && int.TryParse(id.Substring(PiggyPrefix.Length), out var lv)) return BalanceConfig.Sandbox.PiggyUsd(lv - 1);
            var pack = PackIndexOf(id, out var sale);
            if (pack < 0) return 0f;
            return sale ? config.MenuShopPackSaleUsd(pack) : config.MenuShopPackUsd(pack);
        }

        /// <summary>«$7.99» -- the price as the shop shows it.</summary>
        public static string PriceText(string id) => PriceText(UsdOf(id));

        public static string PriceText(float usd) =>
            string.Format(Localization.Get("#shop_price_usd"), usd.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));

        /// <summary>A payment on its way: the card says «Ожидает оплаты» and cannot be bought twice.</summary>
        public static bool IsPending(string id) => PlayerPrefs.GetInt(PendingKey + Family(id), 0) == 1;

        /// <summary>The mine's two products share one owner flag and one pending flag.</summary>
        private static string Family(string id) => IsMine(id) ? Mine : id;

        public static bool CanBuy(string id) => !IsPending(id) && !(IsMine(id) && Wallet.CrystalMineOwned) && !(id == Freedom && Wallet.FreedomOwned)
            && (!IsPiggy(id) || (id == PiggyBank.ProductId && PiggyBank.CanBreak));

        /// <summary>Buys a product. The stand-in gives it at once and says true; the billing will answer later through Granted.</summary>
        public static bool Buy(string id)
        {
            if (!CanBuy(id)) return false;
            Grant(id);
            return true;
        }

        /// <summary>A bought product handed over: the mine unlocked, the crystals in the wallet.</summary>
        public static void Grant(string id)
        {
            SetPending(id, false);
            if (IsMine(id)) Wallet.CrystalMineOwned = true;
            if (id == Freedom) Wallet.FreedomOwned = true;
            var crystals = CrystalsOf(id);
            if (crystals > 0) Wallet.UpgradeCrystals += crystals;
            if (IsPiggy(id)) PiggyBank.OnBroken();
            Granted?.Invoke(id);
            Changed?.Invoke();
        }

        /// <summary>A payment on its way (or no longer: false).</summary>
        public static void MarkPending(string id, bool pending)
        {
            SetPending(id, pending);
            Changed?.Invoke();
        }

        /// <summary>
        /// A purchase taken back (a refund): the mine is locked again -- the offer back in the shop, every built mine
        /// stops mining and its tap opens the purchase (the user, 2026-11-15). Crystals already given stay (spent or not).
        /// </summary>
        public static void Revoke(string id)
        {
            SetPending(id, false);
            if (IsMine(id)) Wallet.CrystalMineOwned = false;
            if (id == Freedom) Wallet.FreedomOwned = false;
            Changed?.Invoke();
        }

        private static void SetPending(string id, bool pending)
        {
            PlayerPrefs.SetInt(PendingKey + Family(id), pending ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
