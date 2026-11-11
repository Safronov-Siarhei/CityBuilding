# Monetization (direction decided 2026-11-11; the sandbox offer shop BUILT 2026-11-11)

> **Status (2026-11-11):** the user settled the direction below and asked for the offer shop («Да,
> делай»): built, see «Offer shop: how it is built». Nothing else is built (no ads SDK, no IAP).

## Offer shop: how it is built (2026-11-11)
- **Sheet:** «Наборы» (gid 2000061: bundle, name_ru/en, kind normal|bonus|starter, weight, level_from/to,
  res1..3 + amount1..3 -- a ResourceType name or `Crystals` --, price_crystals), «Наборы: уровни»
  (2000062: level, lab_from, citizens_from), both -> `Balance/bundles.csv` (stacked tables);
  «Наборы: настройки» (2000063, -> economy: shop_slot_seconds 180, shop_slot_stagger_seconds 60,
  shop_bonus_chance, shop_bonus_min_seconds, shop_notice_seconds 30, shop_starter_delay_seconds,
  shop_ad_stub_seconds); «Тексты: лавка» (2000030, #shop_*). A bundle's name is NOT a localization
  row: the importer makes `#bundle_<id>` from name_ru/name_en. All numbers are first guesses.
- **Code** (`Scripts/Shop/`): `OfferRotation` (plain rules, tested by `OfferRotationTests`): slot i
  first changes at period + i*stagger, then every period -> one change a minute; weighted pick of
  `normal` bundles of the level, never one on another slot nor the replaced one; a bonus only after
  shop_bonus_min_seconds of the session, by chance, one free bundle on screen at most; `PutStarter`
  puts the starter on the slot changing soonest. `OfferShop` (on the gameplay UI root; real time
  `Time.realtimeSinceStartupAsDouble`; off when Invasion/tutorial/dungeon): level every second,
  `Buy(slot, Free|Crystals|Ad)`, ad stand-in = wait shop_ad_stub_seconds then give, goods via
  `ResourceManager.Add` (+ `Wallet` for Crystals), a part that does not fit -> feed line
  #shop_storage_full, too few crystals -> #shop_no_crystals; starter once per account (PlayerPrefs
  `shop.starter_given`, set on claim). `OfferShopWindow` (prefab Resources/OfferShopWindow, sorting
  900, pauses timeScale, hides the interface behind, tap outside closes, window picture per season
  Resources/Shop/Window_<theme>), `OfferShopHud` (stall icon under the sack: timer, red dot when
  anything can be taken, breathing glow while a free one waits; gold notice for shop_notice_seconds
  only on a free one, tap opens), `OfferMenuStrip` (three cards in the game menu right of its entries).
- **Build:** `Editor/SetupProjectOfferShop.cs` from `Tools/store/offer_shop_art.py` (UI/Shop/*.png +
  layout.json, no words in pictures). The event feed's `LeftColumnTop` moved down one button + timer.
- **Round 2 (the user, 2026-11-11, after a look in the editor):** a red close seal on the frame's
  top-right (the tap outside still closes); the HUD dot half on the button (its centre on the rim at
  45°); the notice's countdown bar a plain rectangle (no rounded ends; `Bar.png` deleted); the
  player's crystals (count + icon) on the frame's top edge, and a crystal buy without enough shakes the
  counter and the button red with «Не хватает кристаллов» under it for 1.8 s (the feed is hidden
  behind the window, so the line is in it); no ad fill (`RewardedAds.IsReady`, a stand-in; cheat
  «Лавка: рекламы НЕТ») -> only the crystal button; a timer in its last `shop_timer_warn_seconds`
  (30) is red and pulses (the window's strips and the menu cards); a new bundle pops in (scale Y
  0.2 -> 1 with overshoot) with a cream flash (strips) / a pop (menu cards). Not compiled in batchmode
  (the user's editor was open) -- first setup will tell.
- **Waiting on the user:** which «resources received» effect (options offered 2026-11-11: icons fly
  to the HUD resource bar / to the sack; a «+N» float per good; the strip's goods bursting into the
  stamp; a counter roll on the resource bar).
- **Open:** the numbers (the user tunes the sheet); a real ad network; whether the menu cards' place
  and the window size (0.8 of the art) suit the phone -- not seen on a device yet.

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
  «Лавка»: three parchment strips -- ordinary with timer + «Смотреть» + a BLUE crystal-price button (violet melted into the crystal, the user), the gold-rimmed
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
