# «Свобода песочницы» — paid world settings

**Status (2026-11-17): DECIDED, NOT BUILT; the piggy warning mock-up awaits the user.** The look is approved (see [hud-and-ui](hud-and-ui.md), SANDBOX MENU
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
  choice gets a small struck pig right of its bar. Proposed too (not drawn): the same line on the piggy window's tasks
  column inside such a world. Not approved yet.
- The numbers (factors of each level, the quiet-start minutes, the start goods per level, the price) go to the sheet.

## Ideas parked
- Presets on top of the scroll: «Мирный» / «Классика» / «Испытание» (the user: «пока что нет, просто пометь идею»).
- A bonus for harder settings (option В) -- not chosen.

## Not built yet: everything
Screen (A3 look) -> `SetupProjectMainMenuScreens`; the banner (art from a script like plague_banner_art.py); a
`WorldSettings` save block + the systems reading it (raids, bot war, quiet start, fires/storms/plague, start goods,
build/research timers, fog); Store product `sandbox_freedom`; the piggy-task gate; the in-game scroll under «Сохранить».
