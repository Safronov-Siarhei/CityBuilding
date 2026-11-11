# Monetization (decided direction 2026-11-11 — nothing built)

> **Status (2026-11-11):** the user settled the direction below. Still don't build anything until the user asks.

## Decided (the user, 2026-11-11)
- **Market: the whole world** → Google Play Billing is the main till; Russia is a side case (see «Store
  payments and regions»).
- **Goal at launch: as many players and reviews as possible**, money second → free to play, a light
  touch: nothing that blocks, nags or slows a free player.
- **Ads ONLY by the player's choice** (rewarded: a doubled reward, the offer icon). No banners, no
  interstitials -- for now («пока»).
- **Invasion will have shared leaderboards** → nothing bought may make an Invasion run stronger. Checked
  2026-11-11: crystals are spent only in the sandbox (research lvl 3+, `DiplomacyWindow`); the Invasion
  hub only SHOWS them. Keep it so: any future Invasion purchase must be looks only.

## Proposed order (Claude, 2026-11-11; the user agreed to the frame, items not yet confirmed one by one)
1. Rewarded ads by choice. 2. «Королевский набор» -- a one-off non-consumable (rewards without watching,
   starting crystals, an exclusive look). 3. Looks: HUD styles, castle looks, map themes. 4. Crystal packs
   -- ONLY after a cloud save: `Wallet` lives in PlayerPrefs (lost on reinstall, easy to edit), bought
   currency there means refunds and complaints; non-consumables Google restores by itself, so they come first.
5. Later, by release data: campaign chapters as paid add-ons or a season pass.
Never: energy, timers lengthened to sell skips, power for money in Invasion. Prices: by country tests, no
guessed numbers. Ads need a consent window (EU); declaring a child audience tightens ad rules a lot.

## Earlier ideas (2026-10-14/15)
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
