# Monetization (ideas only — nothing built)

> **Status (2026-10-21):** ideas from 2026-10-14/15. Don't build anything until the user asks.
> **Read with:** [hud-and-ui](hud-and-ui.md), [resources-and-storage](resources-and-storage.md).

1. **Rewarded-ad offer icon:** from time to time an icon pops up in a HUD corner offering resources
   for watching an ad; offers rotate. Open: frequency, lifetime, which corner (ask — the HUD is the
   user's sketch), worth vs the economy (resources are tight on purpose), SDK (Unity Ads / LevelPlay /
   AdMob). Amounts go in the balance sheet.
2. **Paid HUD looks** (Soft/Glass/Dock beyond the free Classic) via Unity IAP non-consumables, a lock
   in settings, «Restore purchases». Open: Android only or iOS too; real purchase tests need a Google
   Play merchant account.
3. **Promo codes for a 5-star review:** answered as FORBIDDEN (Google Play ratings policy, Apple
   guidelines, FTC 2024 / EU consumer law). Proposed instead: codes for Discord/Telegram, beta testers,
   creators, referrals; ask for reviews with the in-app review API after a win, never paid. Unanswered.

Related, undecided: the sandbox lacks goals — a campaign of scenario levels with stars (tiers as
milestones) was proposed.

## Store payments and regions (researched 2026-11-11, the user asked; to settle before release)
Not checked against the live Play Console -- re-read Google's pages before acting.
- **Regional prices.** Google Play allows a price per country in local currency for every in-app product
  (~170 countries), recommended local prices, pricing templates for many products, and price experiments
  (A/B by country). No Steam-like purchasing-power matrix: we build our own grid (e.g. from Steam's
  regional recommendations / PPP). In the game, never hard-code a price: show the string Play returns
  (formatted price in the player's currency); regional OFFERS = different products/bundles per
  country/currency (e.g. smaller cheaper packs), read from the billing answer. Needs Google Play Billing
  (Unity IAP or the Play Billing plugin).
- **Russia.** Google Play billing is suspended for users in Russia (March 2022). Since 2022-08-02 Google
  lifted the requirement to use Play Billing for users in Russia (help article 11950272), so a
  third-party payment for Russian players inside the Play build is allowed -- other countries still
  must use Play Billing. Since 2024-12-26 developers paid by Google to a RUSSIAN bank account cannot
  monetise on Play (does not touch third-party payments, but matters for where Google pays us).
  Open, for a lawyer/accountant: which entity can take rubles (ЮKassa/CloudPayments etc. want a
  Russian entity), sanctions law of our company's country, taxes. Alternative: a separate RuStore
  build with its own payments; rewarded ads as a fallback income there.
