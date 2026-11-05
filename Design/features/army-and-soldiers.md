# Army and soldiers

> **Status (2026-10-24):** four tiers, groups, orders, saving — shipped. Every tier is now a jointed
> Blender figure in the heroes' style (stand / run / strike / limp death). Army upkeep in coins is GONE
> (2026-11-05): a soldier is paid once, at hiring.
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
- Army cap `army_max_size` = **80 since 2026-11-05** (the user: «убери лимит в 20, пока что поставь 80»; economy AND invasion tabs; was 20 for device reasons -- watch the frame time on the phone with a full army; a full group stands in 5 rings, 4.5 cells out). **NO UPKEEP in coins since 2026-11-05 (the user: «один раз
  нанял и больше не тратишься», both modes):** `ArmyManager.UpkeepCharged = false` makes DailyUpkeep 0 and ChargeDailyUpkeep
  a no-op; the army line reads «Армия: N/20» (`#army_summary_run`). The units tab's `upkeep_coins_per_day*` columns and
  `#army_summary` / `#log_disbanded` are unused now (kept for switching back). Soldiers still EAT like citizens (food).
  Test: `ArmyCombatTests.NoUpkeep_AnEmptyTreasuryKeepsTheArmy` (replaced UnpayableUpkeep_Disbands...).
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

## Roster widening (DESIGN, the user 2026-11-05)
**Decided by the user:**
- **Roles and counters**, not a ladder of strength: every unit has a job and a weakness.
- **New units:** Арбалетчик, Всадник/рыцарь, Монах-лекарь, Катапульта/баллиста.
- **Hired from the Barracks plus new buildings:** infantry from the Barracks, cavalry from a new Конюшня, siege from a new Мастерская, the healer from the Church.
- **The bot is a full mirror:** the same units as the player, in the bot's colours (straw capes). Its `botwaves` and garrison get the new ids.

**Proposed (mine, not agreed yet):**
- Арбалетчик opens at Barracks level 5 (it opens nothing today). Slow reload, pierces armour (bonus vs man-at-arms / brutes), +range from walls.
- Всадник: fast, a heavy first blow after a run-up; the Spearman gets a bonus against it (the spearman's real role).
- Монах: no attack, heals nearby soldiers over time.
- Катапульта: slow, long range, hits buildings/walls; walks no walls, crosses no moat.
- Big units (cavalry, siege) take 2 of the army slots.
- Models: jointed figures in the soldiers' style, 3 looks per level; a horse rig is the costliest part. Renders go to the user first.

**Мастерская (the user, 2026-11-05): four machines** -- катапульта, баллиста, осадная башня, таран.
- **A machine is ONE unit with its crew in the model** (user picked): takes 2 citizens and 2-3 army slots; cheap on the phone.
- My roles (not agreed): катапульта = slow, long, area, a dead zone close in, vs buildings/towers/crowds;
  баллиста = one heavy bolt, longer than an archer, vs big targets (man-at-arms, brutes, knights, machines, ships near shore);
  таран = slow, roofed (arrows barely hurt it), weak to melee.
- **The ram damages ONLY gates** (user, 2026-11-05): zero to every other building, wall, unit.
- **Осадная башня:** rolls to a chosen wall piece and docks; while docked it is a temporary stair (a node in `WallWalk`),
  fighters climb it one by one; it has health and can be broken or burnt. **Works against ANY fence** (user): on a lvl1-2
  fence (not walkable today) it throws a gangway over and the fighters climb across to the other side.
- **Walls for all but cavalry** (user): every soldier and melee hero may walk the walls (today only ranged).
  Mine: enemies on a wall do not pass through each other -- they meet and fight in the narrow way.
- **Getting down inside** by the defender's stair towers; **where there is none, they JUMP DOWN and lose some health** (user).
- **Orcs get no siege and no wall walking for now** (user); the bot, as a full mirror, gets the machines and storms the
  player's walls (machines ride transports, taking more room than a footman).

**Machine drafts (2026-11-05, APPROVED by the user the same day):** `Tools/blender/siege_drafts.py -- <out_dir>` builds and
renders SiegeCatapult / SiegeBallista / SiegeTower / SiegeRam in GAME metres with their crew (the soldiers' figure at 0.625,
blue gambeson, leather cap) plus a lineup with a spearman, a lvl-5 wall and a lvl-1 fence; never saves. Sizes: catapult
0.94x1.27x0.86 (2.5k tris), ballista 0.88x1.27x0.60 (2.2k), tower 1.06x1.20x1.97 with the deck at 1.35 (2.7k), ram
0.78x1.61x0.89 (2.6k). The bot needs no models of its own: its units draw through BotPalette.mat, which turns roof_blue/banner_blue (the crew, pennants) to straw. Not yet: splitting into animated parts (catapult arm, ballista string+bolt, ram log, tower drawbridge), install into the .blend, export, the Workshop building's model.

**Workshop building draft (2026-11-05, APPROVED by the user the same day, doors shut):** `Tools/blender/workshop1.py` builds
Workshop1-lvl1 (3x3 like the Barracks, borrowing its box -- no placeholder of its own; stands 6 m in -y of it in the Models
.blend): a tall half-timbered carpenters' hall gable-on with its great doors SHUT (the user: no black hole; a wicket door with a lit window), blue Defence roof, lean-to timber store, forge
chimney, yard with a treadwheel crane, a catapult half built on trestles, stone shot. 3065 tris (over the 1000-1500 services
budget; the crane wheel and the trestle catapult are the place to trim). Levels 2-5 (2026-11-05, APPROVED by the user): `workshop1_lvl2..5.py` (kit.borrow chain like the Barracks):
lvl2 quoins, a louvre on the ridge, a dormer in the left slope, a rail fence open in the middle, a stack of wheels (4453 tris);
lvl3 gold finials on the gable ends, a weathervane on the louvre, a stone gateway with blue caps and a banner, a paved way (4916);
lvl4 glazed roof + glazed lean-to (5248); lvl5 gilded roofs (5248). Each level 3.6 m +X of the one before, 6 m -y of the Barracks.
Heavy for the budget: trim (quoins, wheels, the crane) if the phone needs it.

**New fighters' drafts (2026-11-05, APPROVED by the user after fixes: knight's pennant along the lance, monk's sun on the scapular, hood off the brows, scapular lying on the habit):** `Tools/blender/soldiers_new_drafts.py -- <out_dir>`
(borrows soldiers.py's helpers by exec without its main(); never saves) builds jointed SoldierCrossbow (kettle hat with a wide
brim, breastplate over the blue gambeson, blue pavise on the back, crossbow at the hip; 960 tris), SoldierMonk (white habit,
blue scapular and hood, gold sun on the chest and on the staff, a lit lantern; 1034) and SoldierKnight (the man-at-arms seated
on a white horse in a blue caparison with gold crosses, steel chanfron, couched lance with a pennant, feather plume; parts
HorseHead + LegFL/FR/BL/BR for the gallop; 1566), and a lineup with the spearman, archer and man-at-arms.
Level 2-3 looks (2026-11-05, APPROVED by the user; TRIMS in the same script, `<Name>-lvl2/-lvl3`): crossbowman +pauldrons,
gorget, mail aventail, steel vambrace, steel pavise rim / +gold brim, red plume, gold pavise bands, gold bow tips (1142/1232);
monk +blue shoulder mantle (shoulders only, the sun stays seen), silver chain, red book, steel lantern / +gold circlet, gold
scapular edges and hem, a gold halo ring behind the staff's sun (1206/1434); knight +rider's cape and tassets, steel caparison
edge, crinet plates on the neck / +gold helm band, gold shield rim, broad gold hem, gold horn on the chanfron (1688/1728).

**Stable building lvl1 APPROVED by the user (2026-11-05):** `Tools/blender/stable1.py` builds Stable1-lvl1 (3x3,
borrows the Barracks' box, stands 12 m in -y of it): a long stable with a timber hayloft under the blue Defence roof, blue
double doors with white X braces, four stalls with horses looking out over the half doors (white, chestnut, bay, grey; blue
halters; no dark holes), a hay door, a gold horseshoe sign; yard with a quintain, a lance rack, trough, hay bales, saddle on
the rail fence. Fix 2026-11-05 (user): open loose boxes under a blue lean-to along the front (partitions, half doors, straw), horses standing inside with closed boxes (no missing faces); 3145 tris; approved after this fix. Levels 2-5 not made yet.

**Numbers AGREED by the user (2026-11-05)** -- go into the sheet when the code is built (no columns yet):
- Workshop: 40 wood, 30 stone, 10 iron; health 220, levels by the Barracks' formula. Opens: lvl1 Ram, lvl2 Ballista,
  lvl3 Catapult, lvl4 Siege tower, lvl5 nothing new (look only).
- Level 1 machines (hp / damage / every s / range cells / speed / army slots / crew citizens / coins + kit / upkeep a day -- MOOT since upkeep was removed the same day):
  - Ram: 160 / 60 to GATES only / 3 / touch / 0.7 / 2 / 2 / 120 + 20 wood, 2 iron bars / 4. Arrows do 25% to it.
  - Ballista: 40 / 30 / 3.5 / 11 / 0.8 / 2 / 2 / 140 + 1 iron bar, 1 copper bar / 5. x1.5 vs big targets (man-at-arms, brute,
    chief, machines, ships), x0.3 vs buildings.
  - Catapult: 60 / 25 splash r 1.2 cells / 5 / 14, min 4 / 0.6 / 3 / 2 / 180 + 30 wood, 2 iron bars / 6. x2 vs buildings.
  - Siege tower: 300 / -- / -- / -- / 0.5 / 3 / 2 / 150 + 60 wood / 5. Arrows do 50%; climb one by one, wall_climb_sec each.
- Jump down from a wall with no stair inside: loses 30% of max health.
- Machine levels 2-3 by Laboratory research like soldiers: hp and damage about x1.45 a level; research 250 / 450 coins.
- New sheet columns: units `army_slots` (soldiers 1), `crew`, damage multipliers vs gates / buildings / big targets;
  economy: wall jump damage share, arrow-damage shares for the ram and the tower. The bot (full mirror) uses the same numbers.

**Open questions:** levels and prices of Конюшня / Мастерская; the healer's numbers; the bot's unit counts per wave; whether orcs get answers too.

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
