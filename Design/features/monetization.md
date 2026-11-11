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

## BEFORE RELEASE (the user, 2026-11-11: «запомни»)
Players' crystals must survive a reinstall. Today `Wallet.UpgradeCrystals` is PlayerPrefs only, so a
reinstall or a new phone wipes it. Needs a cloud save (e.g. Google Play Games Saved Games) or our own server.

## Sandbox offer bundles (the user's concept, 2026-11-11; discussed, not built)
- In the sandbox's menu (the round top-left button, pauses the game) THREE long bundle offers, each on
  a 3-minute timer, starts 1 minute apart so they change one by one; a new one is random from the sheet
  and never the same as the other two on screen.
- Contents like wood+stone, coins, crystals, iron+coal; their OWN sheet tab, ~50 bundles, the user fills it.
- Bundles have LEVELS so the amounts fit the player's progress.
- Paid with crystals OR a rewarded ad (the player chooses); sometimes a FREE bonus bundle (small).
- On a change, a special notification on the left for 30 s (must stand out from the usual feed).
- Look: like the rate-us window (parchment, ribbon); to be discussed.
- Decided with the user (2026-11-11, round 2):
  - The loud 30-s notification only for a FREE bonus (and maybe a rare rich bundle); ordinary changes
    are quiet (timer + dot). Early in the FIRST sandbox game: a free bundle + the notification, so the
    player learns the shop exists (once per account).
  - No ad cap needed (the player chooses): each bundle can be taken ONCE per its 3-minute period, then
    «Получено» until it changes.
  - Bundle level = laboratory level + number of citizens (a levels table on the sheet: level, lab from,
    citizens from; the highest row met wins; no lab = lab level 0).
  - A «crystals» bundle = crystals + another resource, sold for crystals: the crystals inside are fewer
    than the price, so the buy costs fewer crystals net.
  - Timers in REAL time, kept per session only: re-entering the game/mode rerolls the bundles (the user:
    we lose nothing). Exception: the free bonus must not be farmable by re-entering (it appears after
    some minutes of play, not on entry; the starter one once per account).
  - Its own place: a small shop icon in the left HUD column (under the sack) with a timer, glowing when a
    bonus is up; the notification opens it. The same three offers ALSO shown in the sandbox menu.
  - Ads are a stand-in for now (no ad network yet).
- Mock-up v1 (2026-11-11, awaiting the user's verdict): `Design/store/OfferShop_Mockup.png` (window
  «Лавка»: three parchment strips -- ordinary with timer + «Смотреть» + crystal price, the gold-rimmed
  free «Подарок короля», a taken one with a «Получено» stamp) and `OfferShop_Hud.png` (stall icon in the
  left column with a timer and a red dot, glowing; gold notification with a 30-s bar). Drawn by
  `Tools/store/offer_shop_mockup.py`. Then: the sheet tabs and the code.

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
