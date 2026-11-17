# Monetization (direction decided 2026-11-11; the sandbox offer shop BUILT 2026-11-11)

> **Status (2026-11-11):** the user settled the direction below and asked for the offer shop («Да,
> делай»): built, see «Offer shop: how it is built». The daily login bonus is built too. No ads SDK, no IAP.
> **Rule for the whole game (the user, 2026-11-11): an amount of crystals is the NUMBER LEFT, the icon
> RIGHT** (other resources keep icon-then-number) -- see hud-and-ui.md.

## Crystal Mine -- paid building (the user's design 2026-11-14; BUILT 2026-11-14, not seen in the game)
- **Rules (the user):** bought once in the shop for ~$8 (non-consumable IAP, needs «Restore purchases» for
  iOS; the shop's offer is the NEXT task -- until then only the cheat «Рудник кристаллов куплен» in Читы ->
  economy). Sandbox only, AT MOST 1 per map (`BuildingIds.OnlyOne`). Levelled in the Laboratory, never for
  crystals (`ResearchTopic.Crystals` = 0 for it). Upgrade prices are PLACEHOLDERS the user will set: Lab
  research_coins_2..5 = 100/200/400/800, sandbox up2..5 = Монеты 100/200/400/800 + Дерево 40/80/160/320;
  build = 20 wood.
- Mines 1 crystal every crystal_mine_seconds_1..5 = 60/50/40/30/20 s into its own store, cap
  crystal_mine_cap_1..5 = 10/20/30/40/50; full = stops. Real seconds while the game runs (paused = stopped,
  the speed buttons do not hurry it; the app in background = stopped).
- Collect like Clash of Clans: a violet bubble with the crystal icon over the mine (bobs, breathes, a white
  glint flashes; faster when full) and «n/cap» under it; tap = all whole crystals to `Wallet`.
- At crystal_mine_notice_share (0.8) a feed notice «#crystal_mine_almost_full»; its tap sends the camera to
  the mine (feed) and `GuideHint` points the yellow arrow at the bubble; it repeats every
  crystal_mine_notice_repeat_sec (60) while >= 80 %.
- Not bought: the build card stands under the padlock (`BuildHints.IsLocked`); a tap says «#crystal_mine_in_shop».
- **Code:** `UI/CrystalMines.cs` (store, bubble, notice; on Managers, wired in SetupProjectCityScene),
  `Wallet.CrystalMineOwned` (PlayerPrefs), `BalanceConfig.CrystalMineSeconds/Cap/...`, save
  `GameSaveData.crystalMineStored` (one mine a map), cheat in CheatsPanelEconomy. Sheet: «Здания» row,
  «Песочница: Здания (Производство)» row, keys on «Здания и постройки», texts on «Тексты: здания и ресурсы».
- **Model:** `Tools/blender/crystalmine1.py` (pure builders, `build_level(n)`) + `crystalmine_install.py`;
  2x2, front -y. Lvl1 APPROVED (rock hill with violet clusters, gold-trimmed stone portal OPEN into a violet
  crystal cave, crown in relief on the pediment, cart, chest, cutter's bench), lvl2 APPROVED (hill 1.35,
  bigger great crystal, the map trees' own fir/round tree (not d.tree: reads as a gem), shrubs+flowers, ivy,
  hoist with a basket). Lvl3 (finials, crystal lamps, garden wall + gold-ball piers, crystal monument),
  lvl4 (violet pediment roof with gold diamonds + crest, floating shard halo), lvl5 (gilded roof, gold bands)
  SHOWN 2026-11-14, APPROVED 2026-11-15 («модели я уже смотрел, всё устраивает»); levels 2-5 still to export into the game. Hill 1.05/1.35/1.5/1.6/1.7; side/back clumps stop growing at lvl3
  and the great clump steps forward (plot edge). Tris 3134/4607/5251/5483/5707.
- **Traps:** the cave glow in the renders is a point light added only for the review (scratchpad
  glow_preview.py); the game has no light there -- the violet reveals/floor/walls carry it (a CrystalHalo glow
  at the mouth is a possible later touch). A portal thicker than ~0.14 hid the cave from the 50-degree camera.
- Open: real IAP + the shop's offer (next task), final upgrade prices (the user), notice repeat interval.

## Main-menu «Магазин» window + the mine's offer (AGREED 2026-11-15, the user; BUILT 2026-11-15, not seen, untested)
**Built (2026-11-15):** `Core/Store.cs` (products crystal_mine / crystal_mine_sale / crystals_1..3; NO billing yet --
`Buy` grants at once as a test purchase; `Grant` / `MarkPending` / `Revoke` are the billing's future entry points, the
cheats drive pending + refund); `UI/MenuShopWindow.cs` (runtime-built overlay canvas 950: blurred screen capture,
shop list, «Подожди!» exit offer, «Покупка успешна!», in-game mine window `ShowMine()` that pauses + blocks ModalGate);
`MainMenuController.OpenShop` -> `ShowShop(StartNewGame)` (the old ShopDialog removed from SetupProjectMainMenuScreens);
`CrystalMines` (no growth / no bubble when not owned, tap opens ShowMine, place hint after Ratusha+Lighthouse on every
map, collect hint until a real tap: PlayerPrefs `hint.crystal_mine_collected`); `BuildingFocus.Select` + `BuildHints`
open ShowMine for a locked mine; `Wallet.CrystalMineChanged`. Art: `Tools/store/menu_shop_art.py` ->
`UI/Shop/Resources/Shop/MenuShop/`. Numbers: economy `menu_shop_*` (sheet «Наборы: настройки»), texts `#shop_*`,
`#crystal_mine_place/_collect` («Тексты: лавка»); mine lvl 1 free (buildings cost_wood 0, sandbox cost emptied).
Traps: inside CityBuilder.* write `UnityEngine.Resources.Load` (a CityBuilder.Resources namespace exists); exit offer
timing `menu_shop_exit_offer_hours` (0 = every exit) remembered in PlayerPrefs `menushop.exit_shown_utc`.
Open: real Google Play billing + prices from the store; cloud save; mine lvl 2-5 models export.
Mockups: `Design/store/MenuShop_Mockup.png`, `MenuShop_ExitOffer.png` (script `Tools/store/menu_shop_mockup.py`, v3).
- **Window:** the menu scene stays behind, BLURRED; title «Магазин» top centre, crystal balance top right; bundles in
  ONE list scrolling down (no tabs for now -- «дополнительные вкладки добавим потом»); «Назад» bottom centre.
- **First four bundles:** the Crystal Mine (wide card on top: render, ribbon «НАВСЕГДА», a green chip «Только для
  Песочницы» with the castle medallion off `UI/MenuArt/Banner_Sandbox.png`, «до 180 в час», «+300 сразу», **$7.99, NO
  discount in the shop**); three crystal packs in a row: Мешок 200 $0.99, Ларец 1 200 $4.99 (+20 %, «ПОПУЛЯРНОЕ»),
  Сокровищница 3 000 $9.99 (+50 %, «ВЫГОДНЕЕ ВСЕГО»). Pack art = UNMINED crystals growing out of a rock, bigger per
  pack (the user rejected a pile of crystal icons); card bands name / art / amount / price never overlap; ribbons
  stretch to their words. Prices/amounts are placeholders.
- **«Назад» -> «Подожди!»** (light title): the shop's interface HIDDEN (only the blurred scene), the SAME mine bundle
  full size with $7.99 struck out, **$5.59 (−30 %)** sticker; «Всё равно выйти» under the window. No timer. A
  separate Google Play product (one price per product); owning EITHER product = owning the mine. Not shown once owned.
- **After buying (either product):** «Покупка успешна» window (+300 crystals fly to the balance), the mine card
  leaves the shop, the exit offer is never shown again. The mine is unlocked in EVERY sandbox save; its LEVEL 1 is
  FREE to build. Pending payment (cash countries): the card says «Ожидает оплаты», no second purchase.
- **Hints in the sandbox:** after a purchase, on opening ANY sandbox map: Town Hall -> Lighthouse -> then the game
  offers to place the Crystal Mine (yellow arrow to its build card; it needs NO residents) -- on EVERY map where no
  mine stands yet; after its first crystal, a hint that it can be collected (arrow at the bubble) -- ONCE PER ACCOUNT,
  but «done» only when the player really TAPPED the bubble and collected: until then it keeps showing (any map).
- **Locked mine card in the sandbox (the user's item 5):** a tap opens the shop's mine offer ON SCREEN in the game:
  the game PAUSED, the HUD hidden, «Закрыть» bottom centre. Same bundle and price as the shop's.
- **Refund (Google returns the money / the purchase is revoked):** the offer is back in the shop, the mine locked
  again in the sandbox; in EVERY save a built mine STOPS mining, and a tap on the building opens the purchase window.
- **Cloud save:** built when the Google Play page/game are set up, before the build goes for review. Until then
  the +300 crystals are lost on reinstall (the mine itself Google restores).

## Player ID + personal reward codes (DECIDED 2026-11-15, the user; NOT BUILT)
For prizes outside the game (a contest winner gets e.g. 1000 crystals) and, later, compensations.
- **The player's ID = their Google Play (Play Games) account**, not a local random id: the same on every device
  and after a reinstall. Comes with the Google Play setup + cloud save, before the build goes for review.
  The ID is shown in the settings with a copy button, so the player can send it to us.
- **Rewards go by personal ACCOUNT CODES:** we generate a code bound to ONE account ID and ONE reward
  (product + amount), signed with our private key (kept OUTSIDE the public repo); the player types it in a
  «Ввести код» field; the game checks the signature and that the ID is theirs, gives the reward, remembers the
  code as used (in the cloud save, so it cannot be redeemed twice after a reinstall). Someone else's code does
  not work; a code cannot be forged. Only the PUBLIC key is in the game.
- Not a «one code for everyone» promo code (Google Play restricts those): a personal gift to a named account.
  Check the contest rules against Google Play policy before running a contest.
- Later, optional: a server mailbox (gifts pushed to an ID, no typing) on the same ID -- not decided.
- Open: where the code generator lives (a local script with the private key, outside the repo); code format
  (short enough to type: base32 of id-hash + reward + signature); which rewards a code may carry.

**Shop list redone (the user, 2026-11-15):** the three packs are WIDE cards like «Подожди!»'s (`PackOfferCard(.., sale:
false)`: full price, bonus sticker +20 % / +50 %, ribbon over the crystals), one under another below the mine; the
list scrolls to the Сокровищница. Mockup `Design/store/MenuShop_WideCards.png` (`exit_pack_offers_mockup.py`).
Then (same day): the shop's pack cards 40 % LOWER (200 vs 330, grounds `PackCardBgShort1-3`) so the mine + almost
two packs fit one screen; every card's words start on ONE line (`TextX`, 540 from the card's left edge, mine and
packs, shop and «Подожди!»); «НАВСЕГДА» centred over the mine. Mockup `Design/store/MenuShop_Mockup.png`.

## Campaign: «Поддержи разработку» banner (the user's idea 2026-11-15; v2 look APPROVED; BUILT 2026-11-15 as UI/SupportOfferWindow, NOT YET CALLED by the campaign, not seen)
The campaign has no ads and no paywall (the user: no ads, never break the story; levels for crystals would put
players off the story). Instead, after a WON level a banner with Ардан: «Поддержи разработку», a few in-game
purchases at a discount (crystals only for now: Мешок −10 %, Ларец −15 %, Сокровищница −20 % -- the same sale
products as «Подожди!», no new Google Play products), a big «Продолжить» under it (never a tiny cross).
Mockup `Design/store/SupportBanner_Mockup.png` (`Tools/store/support_banner_mockup.py <Ardan png>`; Ардан rendered
from CineArdan by `Tools/blender/cinematic/render_ardan_banner.py` -> `Design/store/shop_art/Ardan_render.png`).
v1 (plain beige panel) REJECTED by the user: too simple next to the plague / fire banners. **v2** (APPROVED 2026-11-15)
is built on the plague banner's structure (`Tools/ui/plague_banner_art.py`): wood frame (no corner studs), a festive
field (crystals in 3 depths on royal purple, golden rays, confetti), a purple ribbon «Поддержи разработку», Ардан
left + the campaign mage right on warm glows, a parchment column with 2 lines, 3 compact pack rows (no «Популярное»/«Выгоднее всего» flags; struck old
price, % sticker, green price) and the word «Продолжить». `Design/store/SupportBanner_v2_Mockup.png` by
`Tools/store/support_banner_v2_mockup.py <Ardan png> <mage png>`; the mage render `Design/store/shop_art/Mage_render.png`
(render_ardan_banner.py with mage.make()/CineMage, camera x -0.9).
- RULE (the user, 2026-11-15): shown after EVERY won level from the 2nd on (never after the 1st), replays too;
  `support_banner_from_level` = 2 in economy («Наборы: настройки» row 91).
- The user's look after mission 2 (2026-11-16): the window sat low -> the layout is lifted so the Window piece's middle is the screen's (`_lift`, from layout.json's Window rect); every other root canvas is OFF while it shows (`_hidden`, back in OnDestroy). Not built/seen yet.
- BUILT: `Assets/_Project/Scripts/UI/SupportOfferWindow.cs` -- `SupportOfferWindow.ShowAfterWin(levelNumber, onDone)`
  (1-based level; not due -> onDone at once). Runtime overlay canvas (order 960), pauses time + ModalGate; pieces
  Window + Row1-3 and layout.json in `Assets/_Project/UI/Shop/Resources/Shop/Support/` written by
  `Tools/store/support_banner_v2_mockup.py` (Window keeps 2048 on Android: UiTextureImport skips `/Support/`);
  words/prices/buttons in code; packs = `Store.PackSaleId(p)` with % from `menu_shop_pack_sale_off_pct_*` (0 hides
  a row). A purchase: lines 1-2 become `#support_thanks` / `#support_got`, rows stay. Texts `#support_*`
  («Тексты: лавка» rows 57-62). Cheat «Баннер поддержки» (Экономика tab). `BackCurrent()` for a back key.
- HOOKED 2026-11-15: mission 2 (`CityDefenceHud`: «К карте» after a win -> the banner -> the map;
  `CityDefenceMode.LevelNumber` = 2). Every NEW level must do the same: the rule is in campaign.md and WORKING_RULES. Later maybe an exclusive «Набор покровителя» (Ардан's look; new model -> renders first).

## «Подожди!» exit offers: the pool (DECIDED 2026-11-15, mockups APPROVED; BUILT 2026-11-15, not seen, untested)
**Built:** `MenuShopWindow.ExitOfferDue/PickExitOffer/ShowWait/PackOfferCard` (card ground `PackCardBg1-3` from
`menu_shop_art.py`, the ribbon centred over the crystals as the user asked); `Store` products `sale_crystals_1..3`
(prefix chosen so it never starts with `crystals_`), `Store.PackIndexOf`. PlayerPrefs: `menushop.visits`,
`menushop.exit_shown_utc`, `menushop.exit_last`. Keys: `menu_shop_exit_offer_hours` 24, `menu_shop_exit_free_visits` 1,
`menu_shop_exit_mine_chance` 0.5, `menu_shop_pack_sale_usd_1..3`, `menu_shop_pack_sale_off_pct_1..3` (0 = pack out of
the pool); text `#shop_wait_text_pack`.
Mockups for approval: `Design/store/ExitOffer_Pack1-3.png`, `ExitOffer_Packs_Sheet.png` (script
`Tools/store/exit_pack_offers_mockup.py`): the same window as the mine's, one pack at a discount, card colour per pack.
- NOT on the first shop visit (the player first sees the normal prices); from the SECOND visit on.
- At most ONE «Подожди!» a DAY for all offers together.
- The pool: the mine −30 % ($5.59) while not owned; Мешок 200 −10 % ($0.89), Ларец 1 200 −15 % ($4.24),
  Сокровищница 3 000 −20 % ($7.99). While the mine is not owned it comes up in ~half of the shows, the packs share
  the rest; once owned only the packs. The same offer never twice in a row.
- No «Подожди!» when the player bought something during that shop visit.
- Pack discounts stay smaller than the mine's (10-20 %) so the mine stays the best deal.
- Every number in the sheet (discounts, prices, the mine's weight, hours between shows, first-visit rule).
- Each discounted pack = a Google Play product of its own (3 more, 8 in all). PRICES: the real price is set ONLY
  in Google Play Console (per product, per country); once billing is in, the game shows the price string Google
  returns and the sheet's USD numbers become stand-ins for the editor/tests only. The sticker % is computed from
  the two Google prices (base vs discounted) or kept in the sheet -- to settle with the billing.
- Later (with billing): pick the offer for the player (bought a Мешок -> the Ларец comes up more).

## Boosters: how they are built (2026-11-12, IN THE CODE, user not seen, untested)
- **Sheet:** «Песочница (Лавка): Бустеры» (gid 2000065) -> `Balance/boosters.csv`: a `booster` table
  (name_ru/en, desc_ru/en with `{0}`, effect, duration_sec, price_crystals, ad) and a `key` table
  (booster_slots, booster_slot_recharge_sec, every hint_* number, hint_raid_near_cells 30). The importer
  (`ReadBoosters`) writes `BalanceConfig.Boosters` + merges the keys into the sandbox economy; names are
  `#booster_<id>` / `#booster_desc_<id>`. Window words in «Тексты: лавка» (#booster_*, #shop_tab_*, #inventory_*).
- **Code** (`Scripts/Shop/`): `Boosters` (static: stock `booster.stock.<id>` + time left `booster.left.<id>`
  in PlayerPrefs, one per account; Tick by the game's clock, sandbox only; `Use` = at once (Ремонт ->
  `BuildingInstance.RepairFree`, Новосёлы -> AddCitizens) or +duration; effect helpers asked by
  CitizenAgent (chop/dig speed), FoodOutput.PerDay, TaxManager, ConstructionSite, ResearchManager,
  SoldierUnit.StrikeDamage, BuildingInstance.TryDamage, SoldierTypes.RecruitCost). `BoosterShop` (beside
  OfferShop: 4 places, no duplicates, 15-min real-time recharge, ad stub or crystals). `OfferShopWindow`
  got the bottom tabs «Предложения | Бустеры» and a booster look of each strip (`ShowBoosters()` opens on
  it). `InventoryPanelController`: resources 4 to a row, booster tiles 6 to a row (owned or running, gold
  frame + time), the card grows; tap -> window beside it «Включить»/«Продлить». `BoosterHud` (chips under
  the resource bar, ring of time left, tap -> Запасы), `DayOnMinimap` (sandbox: «День» on the minimap
  foot; Invasion untouched), `BoosterAuras` (violet spiral ribbons, `Shaders/BoosterStream`, material
  `Materials/BoosterStream.mat`; `BuildingInstance.All` registry added for it), `BoosterHints` (banner
  above the build menu; triggers and pacing as agreed below; Вербовка offered from FocusHireBar.ShowSoldiers).
- **Art:** `Tools/store/booster_art.py` -> `UI/Shop/Resources/Shop/Boosters/Booster_<id>.png` (192 px,
  glow baked) + TabOn/TabOff, BoosterRing/Disc, BoosterTile(On).
- **Traps:** booster time runs only in a sandbox game (OfferShop.Allowed); the shop window pauses
  timeScale, so the clocks stop while it is open; prices are empty in the sheet (the user sets them).
- **Open:** nobody has seen it in the game yet; prices; the real ads SDK.

## Offer shop: how it is built (2026-11-11)
- **Sheet:** «Песочница (Лавка): Предложения» (was «Наборы», gid 2000061; layout since round 4 -- see below; kinds normal|bonus|starter|ad_crystals|paid_crystals; a resource is a ResourceType name or `Crystals`), «Наборы: уровни»
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
- **Ad limit (the user, 2026-11-11: never give bundles for nothing):** «Наборы» has a formula column
  `lN_ads` after each `lN_price` = the level's worth in ads by «Цены ресурсов» (sum of amount x price per
  10 / 10 / its B3). Beside it `lN_worth` = the level's worth in crystals (compare with lN_price; NOT read by the game). Then `lN_bonus` = round((worth/price - 1) x 100) %, green/red text. All three formula columns (worth, bonus, ads) are painted green (the user); the user also moved kind/weight after name_ru -- formulas follow a dragged column on their own. The CSV export carries the computed number; the importer reads it into
  `OfferBundle.ads`. A goods bundle with ads > `shop_ad_max_ads` (2, «Наборы: настройки») is crystals only
  (`OfferRotation.AdAllowed`; `OfferShop.Buy` refuses Pay.Ad): ONE wide blue button with the price and a
  crystal (`ButtonBlueWide`, `SlotView.buyWide`) -- the same wide button now also stands for «no ad to
  show». Tested in `OfferRotationTests.AGoodsBundleOverTheAdLimitIsForCrystalsOnly`. The user moved
  res1..3 and name_ru before the level groups (the importer reads by header name; `bundle` must stay
  column A, `level` on «Наборы: уровни»; headers must not be renamed).
- **Trap (2026-11-11):** `ResourceType` still lists Grain, Flour, Bread, Meat (kept for old saves) but
  NOTHING makes or uses them since 2026-11-02 (food-and-hunger) -- never put them in a bundle or a price
  list (Population is not a resource either). Six round-4 families did (farmer, hunter, miller, baker,
  feast, granary): replaced by supplies (Food+Coins, L1), convoy (Wood+Stone+Coins, L1), woodcoal
  (Wood+Coal, L2), stoneworks (Stone+Iron, L2), prospector (Gold+Coins, L2), miner (Iron+CopperOre, L3).
  bundles.csv pulled; BalanceConfig/LocalizationConfig rebuild with the next code build.
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

## «Не хватает ресурсов» (the user, 2026-11-11; BUILT, setup x2, `OfferRotationTests` green, not seen in play)
- **Rule:** in the sandbox, PICKING a building card the player cannot afford (`BuildingPlacer.SelectBuilding`)
  puts up the «Не хватает ресурсов» window over the placement; buying closes it and the player goes on
  placing (the building is NOT placed for them -- they may not have chosen the spot). Closing without buying
  leaves the usual red «not enough» placement. Buildings only for now (army/heroes/research/ships later).
  No shop outside the sandbox (Invasion = leaderboards, tutorial); the dungeon (coin-only offers) maybe later.
- **The offer:** exactly the shortfall per resource (up to 4), at its worth by
  `shop_price10_<ResourceType>` -- rows on «Наборы: настройки» that are FORMULAS reading «Цены ресурсов»
  (the user: export the prices, not the calculator below them) -> economy.csv -> `BalanceConfig.ShopPrice10`.
  Whole crystals rounded up, at least 1, no discount (`OfferRotation.ShortageBundle`, kind `shortage`); worth
  over shop_ad_max_ads ads -> crystals only (wide blue button), else ad + crystals. The goods go in PAST the
  storehouse ceiling (`ResourceManager.AddOverCapacity`; the user: paid goods are never lost; production then
  adds nothing until the stock drops under the ceiling).
- **Code:** `OfferShop.ShortageFor(cost)` / `BuyShortage(bundle, pay)` (slot number `OfferShop.ShortageSlot` =
  99 in AdPlayingSlot/Given); `OfferShopWindow.ShowShortage(bundle)` = the same component in `shortage` mode
  (prefab `Resources/OfferShortageWindow`, built by `BuildOfferShopWindow(true)` from layout.json's `s_*`
  rects, picture `Resources/Shop/WindowShort_<theme>` from offer_shop_art.py), one strip, no timer, closes
  1.4 s after the goods fly. Texts `#shop_shortage_title`, `#shop_shortage_name`.
- **Upgrade too (the user's playtest, 2026-11-11):** «Улучшить» in the building panel
  (`BuildingInfoPanelController.Upgrade`) that fails for lack of resources (researched, not being built)
  puts up the same window with the upgrade's shortfall; after buying the player presses Upgrade again.
  The button drops its selected (pressed) look after the click.
- **Trap:** a building that WAITS on another (`requiredBuilding`, «Нужен: Коттедж») gets NO shortage offer
  on picking -- the goods would not let it stand (the playtest: bought, still red). Gate `HasRequiredBuilding()`.
- **Art:** the short window's ribbon is wider than the shop's (`SRX0/SRX1` = shop ribbon ± 150 px in
  offer_shop_art.py) -- «Не хватает ресурсов» did not fit. A strip has FOUR good places (a price, so a
  shortfall, can ask for four resources; bundles use three).
- **Wide blue button:** price + crystal are centred as a group at runtime (`OfferShopWindow.CentreWidePrice`, on a price change) -- the layout's right-aligned price left them right of centre.
- **Still open:** the silver level disc over an upgraded LIGHTHOUSE (`BuildingInstance.UpdateLevelMarker`;
  it has a model for level 1 only): models for levels 2-5 or hide the disc -- the user said wait.

## Crystal icon redesign (the user, 2026-11-11; variant 4 IN THE GAME)
- **Chosen: 4 (the brighter cluster on gold)**, copied over `UI/Icons/Res_UpgradeCrystal.png` (name kept;
  every crystal in the game -- counters, shop, buttons, lab, dungeon -- reads that one file). All five
  candidates + the old icon + the comparison sheet are kept OUTSIDE Assets in `Design/art/crystal_icons/`
  (not in the build) for a later swap: copy another `Crystal_<n>.png` over the same file.
  `render_icons.py` no longer draws Res_UpgradeCrystal (it would put the old cluster back).
- The crystals are the game's main currency and the icon (`UI/Icons/Res_UpgradeCrystal.png`, a pale
  violet cluster) is lost in the counters. 12 SVG sketches shown; the user picked 1 (cut violet stone),
  3 (ice-blue brilliant), 4 (brighter cluster on gold), 5 (blue stone in a gold medal), 9 (winged) to
  see rendered. `Tools/blender/crystal_icons.py -- <out> 256` renders them stand-alone (own materials:
  the building palette is full). Next: the chosen one replaces Res_UpgradeCrystal (and the shop's
  crystal sprites / OfferShop art if they draw their own).

## Daily login bonus (BUILT 2026-11-11, concept approved; not yet seen on a phone)
- **Rules (the user):** a 10-day calendar of crystals 10-10-10-10-50-10-10-10-10-99 (sheet «Наборы:
  настройки»: daily_bonus_1..10), then it starts again; a MISSED day starts the chain from day 1.
  «×2» (daily_bonus_ad_mult) for a rewarded ad. Closing the window any other way (seal, tap outside)
  still gives the plain reward -- the bonus is never lost by a misclick. The day turns at local midnight;
  clock cheating tolerated until the cloud save.
- **Logic:** `UI/DailyBonus.cs` (static; PlayerPrefs daily.last_claim / daily.streak; Due, TodayIndex,
  IndexFor -- testable --, Reward, Claim(doubled), ResetToday for the cheat «Ежедневный бонус: снова»).
  Days 5 and 10 note `daily_big` for the rate-us window. Reads BalanceConfig.Sandbox (not the run's).
- **Window:** `UI/DailyBonusWindow.cs`, prefab `Resources/DailyBonusWindow.prefab` from
  `Editor/SetupProjectDailyBonus.cs` (after BuildOfferShopWindow). The offer shop's frame/picture per
  season/close seal/buttons (UI/Shop) + its own pieces from `Tools/store/daily_bonus_art.py` ->
  `UI/DailyBonus/` (Card_taken/today/ahead/big/big_today, Pill, Tick, layout.json, all in the 1920x1080
  reference). The concept: `Tools/store/daily_bonus_mockup.py` -> `Design/store/DailyBonus_Mockup.png`
  (the art script imports its `tile(bare=True)`, so the two never drift). Today's card gets its tick
  for 0.6 s, then the window goes. No ad ready (RewardedAds.IsReady false): only «Забрать», centred.
  The ad is the shop's stand-in (shop_ad_stub_seconds).
- **When:** RateUsDirector puts it up in the MAIN MENU when DailyBonus.Due and calm for rateus_delay_sec
  (no other window, no loading); the rate-us window waits while it is open.
- **Texts:** «Тексты: лавка» #daily_title, #daily_day ({0}), #daily_line, #daily_take, #daily_double (×{0}).
- **Look (the user):** cards with a gold bevelled rim, a ribbon «День N» (NO gloss on it), rays for today,
  royal blue big days; the amount on a SOLID dark pill (a translucent one let the green background through).
- **Trap:** PIL ImageDraw on an RGBA picture REPLACES alpha -- translucent shapes go through `over()`.

## Boosters (the user, 2026-11-11; being discussed, concepts shown, nothing built)
- **Decided:** bought in the shop's new tab «Бустеры» (tabs «Предложения» | «Бустеры»); kept in the
  «Запасы» window (no tab: «Ресурсы», a gap, «Бустеры» with captions), tapping a booster there opens its
  activation window; SANDBOX ONLY, not tied to a map, one stock for the account (like crystals -- so a
  cloud save before selling); a building sped up by a booster must SHOW it (a visual effect on it).
- **Proposed, awaiting the user:** time counts only during play; the same booster again adds time (not
  strength); a shorter version for an ad, N per day; starter set Урожай, Ремесло, Зодчий, Учёный, Боевой
  дух, «Готово!» (instant finish); active boosters as a row of medallions with a time ring under the top
  bar (tap -> «Запасы»); a hint at the right moment (raid -> «Боевой дух», only if in stock, 8 s).
  Building look A (gold ground ring + rising motes + badge) or B (gold outline glow + badge).
- **Shop v2 (the user):** the whole shop window TALLER, the tabs «Предложения» | «Бустеры» at the BOTTOM centre,
  boosters as long strips like the offers (more than four scroll). The ad button reads «▶ Смотреть» (the
  user did not get «▶ 5 мин»); the strip says «За кристаллы — 15 мин, за рекламу — 5 мин».
- **Round 3 (the user, shop v2 look approved):** the SAME time for crystals and for an ad (no shorter ad
  version); boosters get their OWN sheet tab «Песочница (Лавка): Бустеры» (the bundles tab renamed
  «Песочница (Лавка): Предложения», gid 2000061 unchanged -- the pull goes by gid); FOUR booster slots
  like the offers, each with a 15-min recharge after it is taken; a booster lasts 5 min (to discuss).
- **Round 4 (the user, the list, in progress):** 12+ boosters; the 4 slots change every 15 min, never
  two of the same; EACH booster its own icon. Tiers: wood / stone / ore boosters speed up gathering by
  sawmill/quarry/mine workers AND by ordinary citizens, in tiers ×1.2 / ×1.5 / ×2; the food booster adds
  +1 / +2 / +3 to a food building's output EACH production cycle (all food buildings alike).
  Tiers are separate boosters in the pool: tier I for an ad OR crystals, tiers II and III for crystals
  only. One icon per booster kind; the tier is written ON the icon's corner («×1.2», «+2») -- not by
  rim colour. Gathering boosters speed up only the ACTION on the resource (chopping, quarrying,
  digging), never walking.
  Taxes (the user, 2026-11-12): a «Казна» booster, tax ×1.2 / ×1.5 / ×2, lasts 10 MIN (tax comes once per
  120-s game day, so 5 min is 2-3 payouts); priced like the other boosters' tiers. So the duration is a
  per-booster sheet column (default 5 min).
  The rest (the user, 2026-11-12): Зодчий -- 3 min of INSTANT building; Учёный -- 3 min of instant
  research; Боевой дух -- as proposed (+50% damage to the army and heroes), 3 min; Стойкость -- buildings'
  HP +20% / +50% / +100%; Вербовка -- hiring at HALF the price; Ремонт -- repairs every damaged
  building at once; Новосёлы -- +10 citizens at once. Ремесло (workshops) and «Готово!» not named
  (asked). Open: tiers / durations of these, Новосёлы vs housing.
- **Round 5 -- NO TIERS (the user, 2026-11-12):** 12 boosters, one strength each, every one for an ad OR
  crystals, same time either way: Лесоруб / Каменотёс / Рудокоп ×1.5 (the gathering action only);
  Изобилие +1 food per cycle; Казна tax ×1.2, 10 min; Зодчий 3 min instant building; Учёный 3 min instant
  research; Боевой дух +50% damage, 3 min; Стойкость buildings' HP +25%; Вербовка hiring at half price;
  Ремонт repairs all; Новосёлы +10 citizens. No daily ad cap: the slots' 15-min recharge already limits
  it (4 per 15 min); crystals are for those who do not want an ad.
  Новосёлы = +20 citizens (the user, 2026-11-12).
- **OPEN -- Рудокоп NOT built (the user, 2026-11-12):** the mines' mechanics are not thought through yet and
  their work is in question, so the ore booster waits until the mines are settled. Its intent so far:
  ×1.5 to the ore-digging action of mine workers and citizens, never walking. 11 boosters are built first.
- **Inventory (the user's idea, 2026-11-12):** «Запасы» lists only the boosters the player HAS (stock > 0
  or running); resources: ALL of them, as now (the user: else a player may never learn they exist).
  Concept v2 `Booster_Inventory.png`: resources, a gap, the owned boosters (running one gold with its
  time), tapping one opens its window («Включить», or «Продлить» while it runs). v3: compact tiles, SIX a
  row (icon, ×N, name; time instead of the name while running), the effect only in the window, so all 11
  fit two rows (`Booster_Inventory_Full.png`).
- **Decided 2026-11-12:** booster time runs ONLY while playing; «Продлить» adds one more full duration;
  prices: the user sets them in the sheet; Новосёлы may exceed the houses' places (the user expects a
  housing shortage to send people away -- BUILT 2026-11-12, see sandbox-run-systems.md).
- **Sheet tab «Песочница (Лавка): Бустеры»** (gid 2000065, target `boosters`, repo `Balance/boosters.csv`,
  2026-11-12): a `booster` table (effect, duration_sec, price_crystals empty, ad) + a `key` table (slots,
  recharge, every hint number). Not read by the game yet (no importer): implementation to come.
- **Icons v1 (2026-11-12, FOR APPROVAL, not in the game):** `Tools/blender/booster_icons.py` (stand-alone,
  low-poly sign on a gold-rimmed enamel medallion) -> `Design/store/booster_icons/Booster_<id>.png`, sheet
  `Design/store/Booster_Icons.png`.
  v1 REJECTED (the user: round icons do not look like the game). **v2 (`bare` mode):** the sign alone from the
  resource icons' angle + a VIOLET glow added afterwards (variant Б chosen 2026-11-12) -> `Design/store/booster_icons_v2/`,
  sheet `Booster_Icons_v2.png`; Новосёлы redone with the game's own villager FBX (Peasant1 + Woman2, palette
  texture), `Booster_Settlers_v2.png`. The glow is not in the PNGs yet (PIL in the contact-sheet script).
  **APPROVED 2026-11-12** (icons v2 + the inventory with them, `Booster_Inventory(_Full).png`). Design phase of the
  boosters is complete; next: implementation (importer for the `boosters` tab, shop tab, «Запасы», HUD row, stream, hints).
- **Town HUD (2026-11-12):** the active boosters' row (medallion + time ring) under the resource bar, where
  «День N» was; «День N» moves onto the minimap's ring foot (the user's idea) -- SANDBOX ONLY; Invasion and
  the campaign keep their HUD as it is (their «Волна N» on top, the run clock on the ring foot). Building look APPROVED 2026-11-12: a VIOLET «magic» wind stream winding up round every
  boosted building (one colour for all; the user's idea after a well-known town builder) + the booster's
  badge over the roof; to build as one ribbon mesh with a scrolling texture (one draw, no particles).
- **Hints at the right moment (the user, 2026-11-12)** -- shown only when the booster is IN STOCK (case A,
  «Включить»); triggers:
  - Боевой дух: orcs or the bot's army LANDED on the player's Town Hall island (not merely sailing);
  - Ремонт: damaged buildings after a fight; Стойкость: enemies coming; Вербовка: barracks/hire opened;
    Зодчий: a long build/upgrade started; Учёный: a long research started;
  - Новосёлы: 10+ citizens left and the houses have room for them (not refilling yet); or free places;
  - Казна: coins below 50;
  - Лесоруб / Каменотёс / Изобилие: that resource below 5 AND working extraction buildings with 3+
    workers in all;
  - frequency (agreed): fades by itself in 8 s (a bar on its foot) or a swipe; one hint on screen; the
    same hint once per 10 min; none in the first 7 min; ignored 3 times in a row -> quiet till next session;
    all numbers in the sheet.
  - Case B (agreed 2026-11-12): not in stock but in one of the shop's 4 slots right now -> the same hint
    with «▶ Смотреть» (ad, then it is switched on at once; the slot goes to its 15-min recharge). Any
    trigger, but its OWN rarer limit: at most one shop offer per 20 min (sheet). No crystals on the hint. Concept `Booster_Hints.png`.
- **Concepts:** `Tools/store/booster_mockup.py` -> `Design/store/Booster_Shop.png`, `Booster_Inventory.png`,
  `Booster_Hud.png` (icons are placeholder medallions; real ones rendered for approval).

## POSTPONED indefinitely: play only online (the user's idea, 2026-11-11)
- Idea: check the connection and refuse to play offline (so offline players do not dodge the ads), and
  sell a bundle that lifts it. Claude advised against: ads are rewarded only (forcing online adds no
  views), «requires internet» is a top 1-star complaint for a single-player game, a paid unlock reads as
  ransom. Softer alternative offered: shop / ads / daily bonus need the net (grey «Нет сети»), the game
  itself does not. The user: note it, implementation put off indefinitely.

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

## Crystals fly from the mine (2026-11-17, the user) -- BUILT, not seen
- A tap on the mine's bubble: the crystals fly into the top bar's crystal chip (`Icon_Crystals`) -- the shop's
  `OfferRewardFlight`, now also `OfferRewardFlight.FromWorld(icon, resource, amount, fromRect)` on its own overlay
  canvas (sorting 40) into the LIVE HUD. Hooked in `CrystalMines` (bubble onClick).

## Instant build for crystals (2026-11-17, the user) -- BUILT, not seen
- A held building of the player's under construction (new or upgrade): a round button with a crystal and «1» LEFT of
  the «!» in the focus row (the row's layout centres both). Tap: pays `instant_build_crystals` (economy, 1; sheet
  «Здания и постройки» row 45) and `ConstructionSite.Finish()`; short of crystals -> `OfferShopWindow.Show()`.
  Every building; hidden in the tutorial and for bot buildings. `UI/InstantBuildBar.cs`, built by
  `SetupProjectHud.BuildInstantBuildButton`.
