# Invasion mode («Нашествие»)

> **Status (2026-10-21):** in progress. Done: the Invasion map `Inv1` (base island halved to ~16 m),
> «В бой» starts a run, the King hero (model, jointed animation, XP/levels/respawn), the run's economy
> (6 houses × 3 people, 9 buildings, wood/stone prices, no food/tax/happiness), the King's portrait
> (top-right) + tap select + guard-the-base with no order. Next (plan in the log): orc WAVES on three
> lanes; forest monsters + boss; round orc camps; score and leaderboards.
> **The authoritative record is `Design/Invasion.md`** (Russian): the user's answers in four rounds,
> how it works now, the plan and a changelog. Read it before touching the mode and append to its log.
> **Read with:** [army-and-soldiers](army-and-soldiers.md), [maps-and-terrain](maps-and-terrain.md),
> [orc-raids-and-portals](orc-raids-and-portals.md).

## In one paragraph
Endless defence of a base island against ever-stronger orc waves, played as the King-hero. Dota-like
map: the island behind a river ring, three earth causeways (not plank bridges) and lanes out to three
round orc camps, jungles with monster camps between them, a boss lair. Simplified economy: gold
(kills, waves), wood, stone, people. Score grows faster than the wave number (10→100, 20→400, 30→900);
daily/weekly/monthly ladders; an interrupted run ends and costs points. Lose = the castle falls.
No pause, no speed-up, no autosave, no ore veins, no sandbox raids/illness/fires in a run.

## Code
`Scripts/Invasion/InvasionMode.cs` (turns sandbox systems off, places castle, towers at the
causeways, the King), `InvasionRules.cs` (the 9-building menu, wood/stone prices, upgrades also take
gold, repairs, nothing waits on the Laboratory or decays), `InvasionPeople.cs` (houses respawn a person
every 15 s while people < places; a burnt house loses its places for good);
`Combat/KingHero.cs` (XP = health of any enemy falling within `king_xp_radius`, levels, respawn at the
castle, `Guarding`: attacks orcs within `king_guard_radius` of the castle until a player's order —
`ArmyGroup.PlayerOrdered`), `UI/KingPortraitController.cs` (portrait, tap-select, guard button; built
in `SetupProjectHud.BuildKingPortrait`, face `UI/Icons/PortraitKing.png` from `render_portrait.py`), `Combat/FigureAnimator.cs` (stand/run/strike without an Animator), `SoldierType.King` (own
group, outside the army cap); entry `MainMenuController.StartInvasion` →
`GameSessionIntent.NewGameIsInvasion`. Map spots by kind: `MeshMapDefinition.SpotsOf(kind)`
(`camp`, `monsters`, `boss`, `shrine`, `bridge`).

## Data
economy `king_*`, `inv_*`; localization `#unit_king`. Blender: `Tools/blender/invasion1.py`,
`render_invasion1.py`, `heroking.py` (jointed; export routes `Hero*` to `Models/Heroes`).

## Tests
PlayMode `InvasionTests`.

## Traps
- A PNG for the HUD must sit in `UI/Icons` (UiTextureImport makes it a single sprite); elsewhere it
  imports as a multi-sprite sheet and `LoadAssetAtPath<Sprite>` returns null.
- `PlaytestCapture.ShootWithUi(At)` does not show the HUD's corner widgets correctly (the menu button
  and the King's portrait come out cut or missing) -- check corners on a device.
