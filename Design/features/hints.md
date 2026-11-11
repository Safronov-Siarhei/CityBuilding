# Hints: «what is missing, and where» (sandbox)

> **Status (2026-11-11):** BUILT (setup x2 clean), not seen in play. The user's rules below.
> **Read with:** [research-laboratory](research-laboratory.md), [building-levels-and-decay](building-levels-and-decay.md),
> [monetization](monetization.md) (the «Не хватает ресурсов» window is the resource side of the same idea).

## Rules (the user, 2026-11-11 -- do not re-ask)
- SANDBOX ONLY (not Invasion, tutorial, dungeon): `BuildHints.Allowed`.
- The look: a yellow ARROW bobbing over the target + the target BLINKING yellow. It goes when the
  target is tapped, hidden, or after `hint_sec` (8); blink `hint_blink_sec` (0.5) -- tab «Здания и постройки».
- **A building missing** (`requiredBuilding`, e.g. Cottage needs a House): the card STAYS in the build
  menu under a PADLOCK; tapping it opens the needed building's card (its tab, scrolled to it) with the arrow.
- **A level of a building missing** (Barracks for a soldier tier, Town Hall for a hero, Shipyard for a
  ship -- the locked faces in `FocusHireBar`): the camera flies to the building, it is held as after a tap,
  its info panel opens and the arrow points at «Улучшить». The Laboratory's own level: «Улучшить
  лабораторию» in its window.
- **Not researched** (Claude's call, the user agreed): research-locked buildings now ALSO show in the
  menu under the padlock (they used to be absent); the tap opens the Laboratory's window on that row with
  the arrow on its button. An upgrade waiting on research: the panel's «нужно исследование» line is
  tappable and leads the same way. A research waiting on a bigger Laboratory: the arrow on «Улучшить
  лабораторию»; on the level before: that level's button.
- **Chains** (Claude's call): always the FIRST step the player can take now (Cottage -> House not
  researched -> no Laboratory -> the Laboratory's card). `BuildHints` walks it, max 8 steps.

## Code
`UI/GuideHint.cs` (the arrow + blink, one at a time, scrolls a target inside a ScrollRect into view; arrow
sprite `TutorialHud.ArrowSprite`), `UI/BuildHints.cs` (the chain: `IsLocked`, `LeadToBuild`,
`LeadToUpgrade`, `LeadToResearch`). Hooks: `HotbarButtonHandler.SelectThisBuilding` (locked card),
`BuildingCategoryPanel.ShowCards/ShowLock` (padlock, re-checked twice a second while open),
`FocusHireBar.Hire/Buy/BuildShip`, `BuildingInfoPanelController.ShowUpgradeResearch` (lock line, Button
added at runtime), `ResearchPanelController.StartResearch` (+ `LabUpgradeButton`, `ActionOf`),
`ResearchManager.Laboratory`. The card's padlock `LockShade` (+ `Hud_Lock` glyph) is built in
`SetupProjectGameplayUI` on every hotbar card, hidden.

## Traps
- `BuildingPlacer.SelectBuilding` still refuses a research-locked building itself; the card handler
  catches the tap before it.
- The «Не хватает ресурсов» offer is not shown for a building whose `requiredBuilding` is missing
  (it is under the padlock anyway now).

## Open
- Not played yet. Locked soldier tiers that the LAB hides (unit unlocks) get no hint (none exist now:
  every tier starts unlocked).
