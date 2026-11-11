# Monetization (direction decided 2026-11-11; the sandbox offer shop BUILT 2026-11-11)

> **Status (2026-11-11):** the user settled the direction below and asked for the offer shop («Да,
> делай»): built, see «Offer shop: how it is built». Nothing else is built (no ads SDK, no IAP).

## Offer shop: how it is built (2026-11-11)
- **Sheet:** «Наборы» (gid 2000061; layout since round 4 -- see below; kinds normal|bonus|starter|ad_crystals|paid_crystals; a resource is a ResourceType name or `Crystals`), «Наборы: уровни»
  (2000062: level, lab_from, citizens_from), both -> `Balance/bundles.csv` (stacked tables);
  «Наборы: настройки» (2000063, -> economy: shop_slot_seconds 180, shop_slot_stagger_seconds 45, shop_crystal_usd, shop_ad_crystals_base,
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
  only on a free one, tap opens), `OfferMenuStrip` (four cards in the game menu right of its entries).
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
  0.2 -> 1 with overshoot) with a cream flash (strips) / a pop (menu cards).
- **Received effect (the user, 2026-11-11: show where it flies IN the shop window):**
  `OfferRewardFlight` (on the window, fed by `OfferShop.Given`): a «+N» rises over each good in the
  strip, 6 icons per good arc to its place and a «+N» rises there, the place punches as they land.
  Place = the top bar's chip `Icon_<type>` (wood, stone, coins, food -- if on screen), else the sack
  `InventoryButton`; crystals -> the window's crystal counter. The HUD behind is hidden, so a copy of
  the target's picture (and the sack's glyph) fades in at its screen place and out after. Round 2 and
  this compiled and built 2026-11-11 (setup x2), not seen in play yet.
- **Round 3 (the user):** the close seal's cross one colour (no dark edge); the crystals are just the number + icon, no plate (`Counter.png` deleted), under the frame's top edge at the strips' right end; «Не хватает кристаллов» left of them.
- **Taken = inactive (the user):** a taken strip's parchment goes grey (0.62), its name, goods and timer fade to 45 %, the «Получено» stamp stays bright (`OfferShopWindow.Dim`); the menu card of a taken bundle goes grey too. Runtime code only, not built.
- **Trap (2026-11-11):** never hand-write a bare `.meta` (guid only) for a PNG: Unity imported Close/Counter
  as CUBE textures (`textureShape: 2`), no sprite -> white boxes. Copy a working PNG meta and change the
  guid; `UiTextureImport` now forces `Texture2D`, and the shop build logs any picture that is not a sprite.
- **Round 4 -- four offers (the user, 2026-11-11; built, setup x2, `OfferRotationTests` green, not seen in play):**
  places `OfferRotation.DefaultPlaces` = goods, goods, crystals for an ad, crystals for money; each
  place picks only its kind (`normal`/`ad_crystals`/`paid_crystals`), a gift and the starter only on a
  goods place. Shop stagger 45 s (4 x 45 = the 180 s period, so one change at a time -- a stagger
  over period/4 makes two slots change together). **1 crystal = $0.01** (`shop_crystal_usd`), **one ad
  = 10 crystals** (`shop_ad_crystals_base`); no ad cap, every slot reloads after the period. The
  **bonus %** is computed, a whole number (`OfferRotation.BonusPercent`: ad = crystals/base - 1, paid
  = crystals x usd / price - 1), shown as a red «+N%» badge when > 0. Crystals for money = a stand-in:
  the orange «$0.99» button shows «Покупки появятся позже» (`Pay.Money`, `OfferShop.Buy` refuses).
  Crystal strips look apart: `StripCrystal` (icy blue), `StripPaid` (blue + gold glow); wide green
  «Смотреть» (greyed with no ad fill) / wide orange price buttons. Strips 150 px high.
  **«Наборы» layout changed:** one FAMILY per row -- `bundle, kind, weight, l1_amount1..3, l1_price,
  ... l5_*, res1..3, name_ru, name_en, comment`; the same resources on every level, an empty level
  group = the family is not offered there (higher families start at level 2-4). The importer makes a
  bundle `<family>_<level>` per filled level (name key `#bundle_<family>`); crystal kinds fill level 1
  only and hold for every level (paid: l1_price = USD). 30 goods families, 3 gifts, starter, 5 ad and 5
  paid crystal offers. Crystal PRICES of goods unchanged on purpose (the user will test first).
- **«Цены ресурсов» (calc-only tab, gid 2000064, the user 2026-11-11):** each resource's worth in crystals
  per 10 pcs (Claude's starting guesses: a level-1 bundle ~ 1 ad = 10 crystals; Crystals row = 10, fixed)
  and a calculator of offers: up to 3 resources (dropdown) -> value in crystals, bonus % (vs the crystal
  price, else the $ price at B2, else one ad -- the same rule as `BonusPercent`), value in ads. Not read by
  the game. By these guesses the goods bundles' crystal prices (1-3) are ~+400..900 % cheap -- the user
  tunes either side after testing. Rejected for now: auto-checking every «Наборы» row, recipe-derived prices.
- **Open:** the numbers (the user tunes the sheet); Google Play Billing for the paid offers; a real ad network; whether the menu cards' place
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

## POSTPONED: chests with a roulette (the user's idea, 2026-11-11; agreed to put off)
- **Idea:** chests of three classes -- common (an ad or crystals), rare and legendary (crystals only);
  opening spins a roulette that gives resources, crystals with a small chance.
- **Agreed shape (Claude's proposal):** NOT a fifth strip (four already fill the window's height; five
  timers overload it; a chest always on show is no longer special) -- a chest is a RARE kind on the goods
  places 1-2, like the King's gift: weights and contents in the sheet, its class by the strip's and the
  chest's colour; contents follow the player's bundle level; the roulette is a small window over the shop
  with the existing reward flight.
- **Rules to keep:** the result is rolled first by the odds and the spin only lands on it (no «almost
  legendary» rigging); a «Шансы» button with the sheet's percentages (Google Play demands odds before a
  paid random item -- crystals are bought with money, so a crystal chest counts; Belgium/Netherlands
  restrict it, an ad-opened chest is safer); the average crystals out of a chest stay well under its
  crystal price (else chests farm crystals) -- a «средний возврат» check next to the sheet row.
- **Why postponed:** the 4-offer shop is not seen in play yet; goods' crystal prices are untested (a
  chest's price and crystal odds depend on them); no real ads/billing to judge it; the random-reward
  rules are settled once, with Google Play Billing before release. **Come back** after the user has played
  and tuned the current shop. To decide then: how often each class shows, its crystal price, what is
  inside (only resources + crystals, or later speed-ups/decorations).

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
