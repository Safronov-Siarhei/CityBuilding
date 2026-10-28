# Army and soldiers

> **Status (2026-10-24):** four tiers, groups, orders, saving — shipped. Every tier is now a jointed
> Blender figure in the heroes' style (stand / run / strike / limp death). Army upkeep vs tax is
> unbalanced (20 soldiers cost more coins/day than 20 citizens pay).
> **Read with:** [combat-core](combat-core.md), [orc-camps-and-main-base](orc-camps-and-main-base.md),
> [research-laboratory](research-laboratory.md).

## What the player sees
Barracks card with four tier buttons (a locked one stays tappable). A right-side army panel, one row
per group with a target-priority toggle. Selecting a group makes a world tap an ORDER (move / attack);
the other selectors stand down. A move order shows a ripple + gold flag + dot trail along the route;
gather = ring; refused = red ripple.

## Rules (decided)
- **Barracks level opens the tier (the user, 2026-10-28, «каждый уровень открывал нового солдата»):**
  units tab `barracks_level` (militia 1, spearman 2, archer 3, man-at-arms 4) -> `UnitBalance.barracksLevel`
  -> `SoldierStats.BarracksLevel`. Both modes. Since 2026-10-28 the Barracks ALONE opens a tier (every
  tier starts_unlocked=1, no unlock research; user: no double key) -- the Laboratory only raises levels 2/3. `ArmyManager.DescribeRecruitBlocker/TryRecruit(type, ..., barracksLevel)`
  (default int.MaxValue = no gate, which every older caller/test uses); the hire bar passes the held
  Barracks' level and shows a too-high tier dimmed under «Казарма ур. N» (`#army_card_barracks`, the
  castle's «Замок ур. N» band now built for both bars); a tap says `#army_need_barracks`. Test
  `TheBarracksHeld_HiresFromTheButtonsBesideTheExclamation` (L1 militia only, L2 spearman).
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
- **Looks (user, 2026-10-24: «в стиле короля, королевы и капитана», анимации «бег, стойка, удар,
  смерть»):** `Tools/blender/soldiers.py` builds SoldierMilitia (brown tunic, cap, blue sash,
  pitchfork), SoldierSpearman (blue gambeson with a gold stripe, plain kettle helmet, spear, round
  shield), SoldierArcher (green hood with a red feather, jerkin, quiver; right arm `ArmDraw`, left
  `ArmBow`), SoldierManAtArms (great helm with a blue crest, the Captain's tabard, mace, heater shield)
  → `Models/Soldiers`, wired by `SetupProjectCityScene` into `ArmyManager.soldierModels` (SoldierType
  order); a missing model falls back to the old cubes. ~0.6 m, a head under the heroes, 730–930 tris.
  `FigureAnimator`: an archer's `ArmDraw` draws and looses (`DrawAngle`) instead of the cut. Death:
  `OrcCorpse` flings any jointed figure's arms up, kicks a leg, drops the head as it topples (orcs and
  beasts with joints too). **A look per researched level** (user «продолжай» on the proposal):
  `<Name>-lvl2` adds metal (iron cap / pauldrons / mail cape / cloak and tassets), `-lvl3` a gold edge
  and a plume (`TRIMS` in soldiers.py); `soldierModels` = type*3 + level-1, a missing level wears the
  one below. A summon wears its own level; a living soldier is redressed when research raises its
  type (`SoldierUnit.RefreshStats` → `ArmyManager.Redress`, figure swapped, animator re-bound).
  Archer's bow canted 30° and thicker so it shows from the front. 730–1100 tris. Test: `InvasionTests.TheSoldiers_AreJointedFigures_AndFallLimp` (+ photos soldiers-*).
- A recruit (any scattered spawn) steps out at `ArmyManager.OpenGroundNear`: the nearest NavMesh point
  in rings round the barracks, in a run one with a complete path to the castle (2026-10-23: recruits
  stood stuck in the gap between the barracks and a house).

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

Trap (2026-10-25): `OrcsFightBack_AndAKilledSoldierCostsAPermanentCitizen` failed once in a full run (the militiaman lived past 25 s) and passed 3/3 alone -- order-dependent, not chased; since 2026-10-25 a melee soldier strikes the orc at hand while chasing (`SoldierUnit.OrcAtHand`), which may lengthen a lone soldier's fight.
