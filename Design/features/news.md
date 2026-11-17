# News in the main menu (Новости)

> **Status (2026-11-17): BUILT (stage A: news ship with the build), not seen by the user.**

## Rules (the user, 2026-11-17)
- A «Новости» button in the main menu: the letter with the wax seal (icon variant 4). Since 2026-11-17 (concept
  Design/ui/MenuCorner_Concept.png, approved) it stands SMALL (94) under «Магазин», right of the settings, in a
  SafeAreaFitter holder «UnderShop»; the top-left corner keeps only the language button. Inside: the newest items.
- No code/setup per item: the build takes every row of the sheet tab «Новости» and orders them.
- NO DATES: items carry a NUMBER (a build goes to review and may not ship on a planned day). Bigger = newer.
- The window lists the 10 newest (`news_shown_count`).
- The window OPENS BY ITSELF on entering the menu only for a player with at least 6 hours of total play
  (`news_auto_open_hours`) and only when something is unread; once a launch. A new player gets only the red dot.
  (Replaces the earlier «from the 3rd entry» rule.)
- Read = the item was seen open once. The red dot stays while a reward is not claimed.
- Rewards: sandbox boosters and crystals, claimed once per item.
- No pictures per item: text + the game's building/resource icons.
- Formatting from the sheet by tags; the text scrolls when long. Comments on every header cell of the tab.
- The texts are the LAST columns of the tab; further languages are added to the right (title_xx, text_xx).
- Window look: concept A «Свиток» + the wax seals (my idea, accepted): an unread item has a whole seal, an opened
  one a broken seal (it pops as it breaks).

## The sheet tab «Новости» (gid 2000078, `Balance/news.csv`, `sheet_tabs.csv` target `news`)
`number, icons, reward, comment, title_ru, text_ru, title_en, text_en[, title_xx, text_xx ...]`
- `icons`: building ids (as on «Здания») or `res:<Name>` (UI/Icons/Res_<Name>.png), split by space/comma.
- `reward`: `crystal:5; booster:architect:1` (booster ids from «Песочница (Лавка): Бустеры»).
- Texts: `\n` = new line; tags `<b>`, `<i>`, `<color=#hex>`, `<size=N>` (normal text is 32).
- Keys: `news_shown_count`, `news_auto_open_hours` (economy, tab «Время и загрузка»); window words
  `#news_title/_reward/_claim/_claimed/_empty` («Тексты: интерфейс и меню»).
- Two example items (1 tutorial-in-sandbox + 5 crystals, 2 building move + Architect booster) -- the user edits them.

## Code
- `Editor/BalanceImporter.ReadNews` -> `BalanceConfig.News` (NewsItem: number, icon sprites resolved at setup,
  rewards); texts -> localization keys `#news_<n>_title` / `#news_<n>_text` (every `title_*`/`text_*` column; a
  language the localization tab lacks is dropped; an empty cell falls back to en).
- `Core/PlayTime` -- total play seconds outside the main menu, PlayerPrefs `playtime.seconds`, flushed every 30 s,
  on pause and quit.
- `UI/News` -- state: read/claimed (PlayerPrefs `news.read.<n>`, `news.claimed.<n>`), AnyDot, Claim (crystals into
  Wallet, boosters into Boosters.AddStock), ShouldOpenByItself.
- `UI/NewsWindow` -- built in code from the menu skin (MenuSkins/Classic panel/title/row/card/divider) +
  `Resources/News` (SealWhole, SealBroken, Close) + `Shop/MenuShop` (Crystal, ButtonGreen, Sticker).
- `UI/NewsButton` -- the menu button: dot, auto-open (0.8 s after the menu, never over another menu window).
- `Editor/SetupProjectMainMenuScreens.BuildNewsButton`; art `Tools/ui/news_art.py` (Hud_News glyph, seals, close).
- Concepts: `Tools/ui/news_concepts.py` (A/B/C), `Tools/ui/news_icon_concepts.py` (8 icons) -> `Design/ui/News_*.png`.

## Traps
- Never renumber a shipped item: the device remembers read/claimed by the number.
- The play-time counter starts with the build that has it: earlier players start from 0 hours.

## Open
- Stage B (news over the internet without a build) -- the user chose «with the build» for now.
- Not seen by the user; no test (a test only if a rule breaks).
