# Army and soldiers

> **Status (2026-10-21):** four tiers, groups, orders, saving — shipped. Army upkeep vs tax is
> unbalanced (20 soldiers cost more coins/day than 20 citizens pay).
> **Read with:** [combat-core](combat-core.md), [orc-camps-and-main-base](orc-camps-and-main-base.md),
> [research-laboratory](research-laboratory.md).

## What the player sees
Barracks card with four tier buttons (a locked one stays tappable). A right-side army panel, one row
per group with a target-priority toggle. Selecting a group makes a world tap an ORDER (move / attack);
the other selectors stand down. A move order shows a ripple + gold flag + dot trail along the route;
gather = ring; refused = red ripple.

## Rules (decided)
- Tiers (`SoldierType`): Militia («жители с вилами», coins only, open from the start), Spearman
  (reach 2 m, + iron bar), Archer (shoots 8 m, fragile), Man-at-arms (4× militia HP, slow). Kit is paid
  AT THE BARRACKS in `recruit_ironbar`/`recruit_copperbar` (user, 2026-08-19, overriding "one workshop
  per equipment"). Kit is flat across levels; level tax is in coins.
- Recruit takes an idle citizen; disband returns them; death in battle = permanent population loss.
- Army cap `army_max_size` = 20, fixed (device reasons); upkeep is the real limiter. Unpaid → that
  soldier disbands (turning hostile = an idea, not decided). Soldiers eat like citizens.
- Command by GROUPS, never individuals; one group per unit type today (`ArmyGroup` is a real object
  so splitting later doesn't rewrite orders/UI).
- Target priority (enemies / structures) is a PERSISTENT per-group mode.
- Idle group holds ground, engages within ~7 m of its slot; no chasing, no auto-intercept.
- Auto-targeting skips shielded portals.
- `SoldierType.King` (=4) is the Invasion hero: a soldier in his own group, outside the army cap,
  stats from economy `king_*` ([invasion-mode](invasion-mode.md)). His jointed model is animated by
  `FigureAnimator` (`SoldierUnit.Struck` drives the cut).
- **In an Invasion run (user, 2026-10-23)** the Barracks hires every tier with no Laboratory, for gold
  only (the invasion tab's `recruit_coins`); nobody commands the soldiers -- no army panel --
  `Invasion/InvasionGarrison.cs` stands each type's group round the castle and sends it at the orc
  nearest the castle within `inv_soldier_guard_radius`. A hurt soldier shows a health bar (every mode);
  a killed one falls as a figure (`OrcCorpse.Leave`), a hero too (`LeaveCopy`, he rises later).

## Code
`Combat/SoldierUnit.cs` (`IsRanged`, `_breach`), `Combat/SoldierTypes.cs` (`SoldierType`,
`TargetPriority`, `SheetIdOf`), `Combat/ArmyGroup.cs` (`RestoreAttackOrder`), `Combat/ArmyManager.cs`,
`UI/ArmyPanelController.cs`, `UI/ArmyOrderInput.cs`, `UI/MoveOrderMarker.cs`.
Built in `Editor/SetupProjectPanels.cs` (`BuildArmyPanel`, `BuildArmyOrderInput`).

## Data
units tab rows `militia`, `spearman`, `archer`, `manatarms` (hp, damage, interval, range, speed,
`recruit_*`, upkeep, levels `_2/_3`, research); economy `army_max_size`.

## Tests
EditMode `ArmyBalanceTests` (relationships, not numbers), `SoldierTierTests`; PlayMode `ArmyCombatTests`,
`MoveOrderMarkerTests`.

## Traps
- Save restores the selected group by INDEX (ids come from a static counter reset by scene reload);
  attack orders as KIND + orc index, restored in a third pass after army and raids exist.
- `OrderAttack` moves the rally point — on load use `RestoreAttackOrder`, not a replay.

## Open
Splitting a type into several squads; equipment buildings; `#unit_militia_flavour` is a dead key and
the three newer tiers have no flavour keys; melee has no swing effect.
