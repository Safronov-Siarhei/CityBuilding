# «Свобода песочницы» — paid world settings

**Status (2026-11-17): BUILT, IN THE GAME, NOT SEEN BY THE USER** (setup green, MapPickerTests 4/4 incl. the purchase gate + settings carried into the world). The look is approved (see [hud-and-ui](hud-and-ui.md), SANDBOX MENU
REDESIGN: `Design/ui/SandboxHub_A3.png`, `_A3_StartPulse.gif`, `_A3_Banner.png`); the list and the rules below are
the user's answers of 2026-11-17. Related: [monetization](monetization.md), [piggy-bank](piggy-bank.md),
[save-load](save-load.md).

## What it is
A one-time purchase (~$0.99, «навсегда, для всех карт») that lets the player set the sandbox world up: on the
sandbox screen before the start, and in the game. The settings scroll is ALWAYS SHOWN OPEN with its values; a try to
change any of them without the purchase opens the purchase banner (plague/fire banner style: King + Queen, teal ribbon,
«Открыть за $0.99», «Закрыть»). Defaults = today's balance (a world with all defaults is an ordinary world).

## The list (the user, 2026-11-17)
| Group | Setting | Values | In the game? |
|---|---|---|---|
| Угрозы | Набеги орков | Выкл / Лёгкие / Обычные / Тяжёлые / Кошмар (wave strength and frequency x factor) | YES -- from the next wave on |
| Угрозы | Королевство-сосед | Всегда мир / Как обычно | YES -- «Всегда мир» during a war ends the war at once (its attackers go home, no ransom either way) |
| Угрозы | Тихое начало без набегов | 0 / 7 / 15 / 30 min (today 7) | NO -- creation only (it is spent once the game runs) |
| Бедствия | Пожары | Выкл / Вкл | YES -- off: no new fires, the burning ones burn out as now |
| Бедствия | Грозы | Выкл / Вкл | YES -- off: no new storms |
| Бедствия | Чума | Выкл / Вкл | YES -- off: no new outbreaks, the current one runs its course |
| Экономика | Стартовые запасы | Мало / Обычно / Много | NO -- creation only (they are given at the start) |
| Экономика | Скорость стройки | x1 / x2 / x4 | YES -- the timers running are re-timed by their remaining share |
| Экономика | Скорость исследований | x1 / x2 / x4 | YES -- the same for the research running |
| Мир | Туман войны | Есть / Нет | NO -- creation only (switching it off reveals the map; back on would mean nothing) |

Dropped by the user: orc camps/main base, portals, neighbour «Нет», taxes, sea caches/guardians.
In the game the settings that cannot change show their value greyed with a small lock-free note «задаётся при создании».

## Rules
- Where in the game: the same scroll in the sandbox's game menu, a button UNDER «Сохранить» (the user).
- Saved in the save (per world), so a loaded world keeps its settings; a new world starts from the defaults (or the
  last used? -- open, propose defaults).
- Piggy (the user chose A): if ANY setting is easier than the default (raids below Обычные, neighbour «Всегда мир»,
  quiet start longer than 7, a disaster off, more start goods, faster build/research, no fog), the piggy's TASKS do not
  count in that world; the piggy's per-minute filling goes on as everywhere. Harder settings give nothing extra (for now).
- THE PLAYER IS WARNED (the user, 2026-11-17: «игроку нужно предупреждение про копилку»), mock-up
  `Design/ui/SandboxHub_A3_PiggyWarning.png`: while the world is easier, the scroll's foot shows a rose plate with a
  struck-through pig «Задания копилки здесь не засчитываются» (in place of «Можно менять и в игре»), and each easier
  choice gets a small struck pig right of its bar. AGREED by the user (2026-11-17): the same line on the piggy
  window, over its tasks list, while the player is in such a world (`PiggyWindow`; text key shared with the scroll).
- The numbers (factors of each level, the quiet-start minutes, the start goods per level, the price) go to the sheet.

## Ideas parked
- Presets on top of the scroll: «Мирный» / «Классика» / «Испытание» (the user: «пока что нет, просто пометь идею»).
- A bonus for harder settings (option В) -- not chosen.

## Built (2026-11-17)
- Data: `Core/WorldSettings.cs` (Current = the running world, Draft = the menu's; saved in `GameSaveData.worldSettings`;
  `GameSessionIntent.NewGameWorld` carries Draft into a new game; MainMenuController.Start resets Current to defaults).
  Factors: economy keys `freedom_*` (tab «Время и загрузка»), `BalanceConfig.Freedom*`.
- Hooks: OrcRaidManager (off / size / interval), OrcRaidManagerPlacement + MigrationManager (quiet start), BotRelations +
  DiplomacyRules (always peace, `#world_peace_blocks`; choosing peace during a war ends it), FireManager / Thunderstorm /
  Plague, BuildingPlacer (start chain rewards x goods), ConstructionSite (player buildings only) / ResearchManager
  (speed), FogOfWarManager (no fog = DisabledCheat), PiggyTasks.CountsHere (easier world: no tasks).
- Purchase: `Store.Freedom = "sandbox_freedom"` (stand-in Grant like the crystal mine), `Wallet.FreedomOwned` (PlayerPrefs).
- UI: `UI/WorldSettingsScroll.cs` (segments, picks, struck pigs, rows greyed in game for start-only settings, warning vs
  hint, «По умолчанию»), `UI/FreedomBanner.cs` (hides the screen's front), `UI/ButtonBreath.cs` («Начать»).
  Builders: `Editor/SetupProjectSandboxHub.cs` (BuildSandboxScreen, BuildWorldScroll, BuildFreedomBanner, BuildWorldPanel).
  Art: `Tools/ui/sandbox_hub_art.py` -> `MenuArt/SandHub_*`, `Freedom_*`, `SandHub_layout.json`, row glyphs
  `SandHub_RowGlyph0..9` (silhouettes baked from the icons), piggy line `UI/Shop/Resources/Shop/Piggy/EasyWarn.png`.
- In game: game menu entry «WorldEntry» (glyph Theme_summer -- no scroll glyph yet) under «Сохранить», sandbox only
  (`GameMenuController.worldEntry`), opens `WorldPanel` (the scroll centred + round ✕) and the banner over it.
- Piggy window: the rose line `#world_warn_piggy` over the tasks in an easier sandbox world.
- The chart is rectangular now (the user, mid-build): `InkMapPainter.Paint(map, tex, aspect)` pads open sea at the
  sides; `SandboxScreenController.chartAspect` = 2 (layout point ChartAspect 820x410).
- Texts: `#world_*`, `#freedom_*` (localization + sheet tab «Тексты: интерфейс и меню»), verify_sheet clean.

## Traps / open
- The SegBar/SegPick sprites get their 9-slice border from the builder (`SlicedMenuArt`), not UiTextureImport.
- Option labels like «×2», «7 мин» are text keys, not formatted from the factors: change both when the sheet changes.
- Real billing for `sandbox_freedom` is not wired (the stand-in Store grants at once), like the other products.
- Not checked on a phone; the in-game panel and the banner were not photographed.
