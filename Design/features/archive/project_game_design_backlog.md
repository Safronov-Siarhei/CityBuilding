---
name: game-design-backlog
description: "The user's own game design decisions for CityBuilding — win condition, happiness, the building roster, the Laboratory/research spec, resources. Some shipped, some still forward-looking; each entry says which."
metadata: 
  node_type: memory
  type: project
  originSessionId: 4d3f7e47-d4f3-4463-8ed6-ac119cba57b5
  modified: 2026-08-18T05:30:20.000Z
---

Ongoing design record the user has built up across sessions since 2026-08-12. **Parts of it have since shipped** — the army, the happiness stat, decay, the processing chains, the Laboratory — and each section says so; the rest is forward-looking intent to pull from when scoping. Check [[project_current_state]] for what is actually in the build before treating anything here as unbuilt. Extend this file (don't create a duplicate) as the user adds more ideas.

**Why:** the user explicitly asked to persist the resource/food list ("сохрани этот список, в будущем будем его дополнять") and is clearly iterating on a connected set of ideas (win condition, happiness, resources) in one continuous design session — keeping them together preserves how the pieces relate to each other.

**How to apply:** read this before scoping any new gameplay feature (win conditions, combat, happiness, resources) so suggestions build on the user's stated direction instead of re-deriving from scratch or contradicting an idea they already committed to.

## Win condition: portals + Orc faction (2026-08-12)
- Each map's goal: destroy enemy portals to another world. ~5 portals per map; destroying the last one ends the map.
- **Refined 2026-08-12:** not every portal is an active threat source -- most are just destruction targets sitting passively. Only a subset (example given: 2 of the 5) actively spawn enemy squads at some interval, i.e. those specific portals are the actual origin of the periodic raids (refines/likely replaces a separate generic raid timer -- raids come from these portals specifically, not an independent clock). Confirmed intentional: active portals are meant to be harder-defended than passive ones, so destroying them first (stopping the ongoing threat) is a real tradeoff against difficulty, not a free strictly-correct move.
- Enemy faction is "Orks" (Орки): many small bases scattered across the map + one large fortified base.
- **Base assault concept (decided 2026-08-12, explicitly "take the Warcraft concept"):** each portal sits inside a base defended by enemy structures. Portals themselves have a large HP pool and take real time to bring down (not a quick strike) -- to actually make progress the player must first destroy the base's defensive buildings and kill all the enemies stationed there before the portal itself is realistically destructible. Resolves the earlier open "are bases just raid sources or do you have to clear them" question -- answer: yes, you clear them. Confirmed: base defenders hold a limited radius around their base (pure defense, not global aggro/chase) -- their goal is defending their own base, not pursuing the player's army across the map.
- Orcs raid the player periodically; raid strength/frequency scales with the player's "progression level" -- metric now defined, see "Player progression score" below.
- Known gaps flagged during discussion: game currently has zero win/lose state, zero enemy AI/units, zero player-controlled combat units (only passive building `defense` stat on Wall/Tower/Barracks/Gate). NavMesh pathfinding (already built for citizens) is reusable for enemy unit movement.
- **Defeat conditions (decided 2026-08-12):** the Town Hall is destroyed, OR the player has zero remaining citizens (population 0 means no way to recover/keep developing, a soft-lock treated as a loss).

- **First slice IMPLEMENTED 2026-08-12** (`OrcRaidManager`, `OrcUnit`, `OrcPortal`, `DefensiveBuilding`, `BuildingInstance.TryDamage`/`OnDestroyedInCombat`, `GameOverManager`, `GameOverController`), scoped down through explicit back-and-forth rather than assumed -- this is the smallest useful vertical slice, **defense-only, no win condition yet**:
  - One portal only (not ~5), fixed at a corner of the grid (`PortalCornerMarginCells` offset), not map-authored -- no base to clear, not destructible at all in this slice (no `TryDamage` path wired to it). Destroying it is explicitly deferred to the increment that adds a player army, since nothing can currently reach it offensively.
  - `OrcRaidManager` spawns a small squad of `OrcUnit`s from the portal every 90s (real time, not day-based); squad size grows slowly with `GameCalendar.CurrentDay` via `ComputeRaidSize` -- a simple stand-in for the full "player progression score" below (soldier count/level don't exist yet, so the real formula can't be built yet either).
  - `OrcUnit` paths over the existing baked NavMesh to the nearest real building (roads/bridges explicitly excluded -- an earlier draft let orcs beeline for stray road tiles instead of actual buildings) and deals damage in real time until it or the building dies. No aggro range, no target prioritization beyond nearest, no citizen casualties (orcs only ever damage buildings).
  - `BuildingInstance.defense` (Wall/Tower/Barracks/Gate) now does double duty: still HappinessManager's defense-score input, and now also each building's own attack damage against nearby orcs via the new `DefensiveBuilding` component (attached automatically at prefab-creation time for any building with `defense > 0`, same hook `ProductionBuilding` uses for `maxWorkers`).
  - Defeat condition implemented: Town Hall destroyed (via `GameOverManager` listening for `BuildingInstance.OnDestroyedInCombat`) triggers a full-screen "Поражение" panel, `Time.timeScale = 0`. The "population 0" defeat path from above is **not** implemented -- needs a citizen-death mechanic (starvation and/or combat casualties) that doesn't exist yet, and orcs deliberately don't damage citizens in this slice.
  - **Not visually verified** -- built and confirmed via clean batchmode compile + the full EditMode test suite only (99/99 green, including a new `OrcRaidManagerTests` for the pure `ComputeRaidSize` formula). No way to confirm orc pathing/attack-range feel, portal placement not landing in water or overlapping the Town Hall, or general balance actually plays right. Flagged explicitly to the user as needing a playtest pass before trusting it.
  - Next increment (not started): player army/Barracks recruitment (see below) to actually go on the offensive, then a destructible portal + real win condition, then base-clearing per the original concept above.

## Player progression score (2026-08-12)
Resolves the "exact metric" open question from the win-condition section above -- this is what raid strength/frequency scales with. Composite of:
- Number of buildings in the settlement, and their upgrade level.
- Number of citizens (population).
- Number of soldiers, and their level.
- Number of defensive structures (Wall/Tower/Barracks/Gate), and their upgrade level.
- Total resource production/output over time (changed 2026-08-12 from "current resource stockpile" -- a stockpile metric would perversely reward spending everything to zero right before a raid instead of reflecting real economic power).

New building implied: a **"лаборатория" (laboratory)** where soldier level/armor/equipment gets upgraded -- first concrete mention of how the player's (still nonexistent) army would progress. Ties directly into the still-open "player army" question (how soldiers are recruited/upkept is not yet specified, only how they're upgraded once they exist).

## Army recruitment via Barracks (2026-08-12)
Resolves the "how do soldiers appear" question left open above.
- Barracks recruits soldiers of varying level/equipment; higher tiers must first be unlocked in the Laboratory before they're recruitable there.
- Recruitment cost = coins (see currency note below) + the soldier's finished equipment, each produced in its own dedicated building, one building per equipment type (confirmed 2026-08-12 -- e.g. a separate Bowyer for bows, separate Armory for iron armor, not a shared multi-output building) -- e.g. an iron-armored soldier needs iron armor + a weapon + coins; an archer needs a bow + coins. Cost scales with the soldier's power/tier.
- Army has a maximum size cap: **fixed and constant, 20 soldiers** (decided 2026-08-12, chosen partly for device/performance reasons). Deliberately not scaled by economy -- the design intent is that the player chooses how many of those 20 slots to actually fill, since upkeep (coins + food) strains the economy; a weak economy can't sustain a full 20 for long. This is the intended difficulty lever, not the cap itself.
- Soldiers consume food from the settlement's supply (on top of citizens) and require daily-or-weekly pay/upkeep (довольствие) -- exact cadence not decided, ties to the still-open day/week calendar system (see Happiness model below).
- **Upkeep payment (decided 2026-08-12):** coins are auto-paid from the player's treasury each cycle. If the treasury can't cover it, that soldier is simply removed (auto-disbanded) -- no penalty beyond losing the unit. **Future idea, not committed yet:** an unpaid soldier could instead turn hostile and start attacking nearby buildings/people like an Orc, rather than just vanishing -- flagged as a possible later addition, not decided.
- **New currency: "монеты" (coins)**, distinct from Gold/gold bars (золотые слитки). Coins come from taxes, or from a dedicated building that exchanges gold bars for coins. Recruitment and upkeep are paid in coins, not raw Gold -- the resource list below should eventually gain a coins entry separate from Gold.

## Happiness/довольство model (2026-08-12)
Factors, as specified by the user (not yet weighted/balanced):
1. Diversity of food types consumed (more types = higher) — **IMPLEMENTED 2026-08-17 (`c7f640f`)**, see the food-consumption entry below.
2. Recent deaths (more deaths in preceding days = happiness penalty) — **IMPLEMENTED 2026-08-17 (`c7f640f`)**, starvation is the death mechanic.
3. Entertainment buildings — each contributes its own happiness value. Fits the existing `BuildingData`/`BuildingCategory` pattern directly (new category).
4. Defense level — soldiers + defensive buildings + their upgrade level. `BuildingInstance` already has `defense` and `Level` for Wall/Tower/Barracks/Gate; "soldiers" as actual units don't exist yet.
5. Tax rate (lower tax = higher happiness) — no tax system exists yet; ties naturally to a Gold income mechanic.
   - **Economic side IMPLEMENTED 2026-08-12** (`TaxManager`, `TaxRateController`, `ResourceType.Coins`): player-adjustable 0-100% rate via a +/- stepped control (left HUD, above the event log), collected as the new Coins currency once per GameCalendar day, proportional to population (0.5 coins/citizen/day at 100% tax, first-pass/tunable). Coins are a separate ResourceType from Gold now, with their own icon (a stacked-squares "coins" pictogram, distinct from the Gold bar icon). The happiness link itself (lower tax = higher happiness) is NOT wired up -- there's no happiness stat yet, only the tax/coin economy this factor will eventually read from. The "exchange Gold for Coins at a dedicated building" alternate coin source from the Army section below is also NOT implemented -- taxes are currently the only coin source. Both deferred as follow-ups, not unilaterally designed.
6. Work days per game week (fewer = higher happiness) — user explicitly flagged that production/other systems must scale with this too. Requires a day/week calendar.
7. Building decay/ветхость (more buildings in critical decay = penalty) — `BuildingInstance.Decay` field already exists (0..1) but nothing advances it over time yet; this is the missing piece.
   - **Mechanics decided 2026-08-12:** decay accrues purely from building age (time-based, no maintenance concept), and the accrual rate is slower the higher the building's upgrade `Level` -- a level-3 building ages more slowly than the same building at level 1. Above 70% decay, the building's productivity/performance drops. At 100% decay the building is destroyed outright and must be rebuilt at close to full original cost, same as initial construction. Player can pay resources to reduce decay via an action in the building's own info panel (renovate/repair) -- a sibling to the existing upgrade button in `BuildingInfoPanelController`.
   - **IMPLEMENTED 2026-08-12** (`GameCalendar`, `BuildingInstance`, `ProductionBuilding`, `BuildingInfoPanelController`, `SetupProject`, save data): day counter ticks every 120s real time; decay/day = 2%/Level; >70% decay linearly drops production to 50% floor at 100%; at 100% the building is destroyed and its cell freed; repair costs 40% of the base placement cost and resets decay to 0, button next to Upgrade in the info panel; a small floating orange marker appears above a building while decay is in the penalty band. **Scoping deviation from the literal spec, decided unilaterally -- flag to the user if it matters:** the Town Hall is excluded from decay-triggered destruction (roads/bridges too). Reasoning: Town Hall loss is already a defeat condition, but no actual game-over/defeat state exists yet -- decay silently deleting the mandatory building with zero win/lose handling behind it would be a broken experience, not the intended one. Revisit this exclusion once defeat conditions are actually implemented. All the specific numbers above (day length, 2%/day, 40% repair cost, 50% floor) are first-pass and freely tunable, not something the user specified precisely.

**Cross-cutting dependency noted during discussion:** a day/calendar system (with an explicit day counter, later possibly day/night) is a prerequisite for factors 2, 6, and 7 simultaneously — worth building before those three rather than separately. Food consumption is a separate prerequisite unlocking factor 1 (and feeds factor 2 via starvation deaths).

**Composite Happiness stat IMPLEMENTED 2026-08-12** (`HappinessManager`, `HappinessController`, unprompted follow-up session -- proceeded autonomously since the factors below were already decided/implemented pieces, not new design): a 0-100 `HappinessPercent` that's a plain average of the three factor scores that already have a real system behind them -- factor 5 tax rate (`100 - TaxRatePercent`), factor 7 decay (`100 * (1 - average Decay across buildings where BuildingInstance.DecaysOverTime)`), and factor 4 defense (total `Data.defense * Level` across all buildings vs. population, scaled against a first-pass target of 0.5 defense/citizen for 100). Recomputes on `GameCalendar.OnDayPassed` and `TaxManager.OnTaxRateChanged`. Shown as a HUD readout panel (right side, mirroring EventLog's panel on the left) with the percentage plus the three factor scores broken out, since the model is still first-pass/tunable. Also logs an EventLog notice once when Happiness drops to ≤30% and once when it recovers to ≥70% (hysteresis, no spam). Factors 1/2/3/6 (food diversity, deaths, entertainment, work week) are still NOT wired in -- none of their prerequisite systems exist yet, per the "Pending topics" note above. Happiness itself has no downstream gameplay consequence yet (nothing reads `HappinessPercent` to affect migration/production/anything else) -- this is only the stat, not its effects.

See also [[kc_reference_analysis]] for the Kingdoms & Castles reference points (tax slider, entertainment upkeep, pollution/happiness proximity penalty) this model is partly inspired by.

## Full building taxonomy (2026-08-15, the user's diagrams)

~48 buildings across six categories (Город, Склады, Развлечения, Оборонительные, Производственные, Производство еды) plus a seventh, **Водные**, deliberately left as a stub — "требует отдельной долгой проработки", don't touch it.

Decisions the user gave explicitly, do not re-ask:

- **Tiers are separate buildings, not levels.** Забор and Укреплённый забор are two buildings; the tower line is **Башня and Укреплённая башня — two** (Большая башня was in the first diagram and the user cut it, 2026-08-16; it never existed in code or the sheet, only `Tower` does). **Each of them additionally has levels 1/2/3.** (I read the diagram the other way round first and was corrected.)
- **Every level changes stats AND model.** Models per level come later; when a level's model is missing, show the nearest level below it — "чтобы не ломалась игра".
- **Per-level stats get their own sheet columns**, hand-editable. Implemented as `<column>_2` / `<column>_3` with inheritance — see [[project_balance_pipeline]].
- **Model naming: `<Id>1-lvl<N>`** (`Castle1-lvl1`, `Fence1-lvl2`), where the digit after the name is the building variant. Files that shape themselves to neighbours add a part suffix: `Fence1-lvl1-Straight.fbx`.
- Дом = Лачуга (`Hovel1`). Колодец is coming but its rules are unwritten: tied to fire fighting, happiness/drought, and food production speed.
- Resources with no building in the diagram (песок, глина, шкуры) stay ideas, ignored for now.
- **Saves are ignored for now** — renaming ids will break old saves and that is accepted.
- **Стена and Хижина охотника were removed** from the game (`d9c7cc6`); Tower's prerequisite moved to the Fence.

**The definitive roster, re-sent 2026-08-16 with every model name spelled out** (id = the model's base name, `<Id>1-lvl1..3`). Treat this as authoritative over anything above:

- **Город**: Ратуша `Castle1` ⚠, Колодец `Well1`, Лачуга `Hovel1`, Дорога `Road1`, Коттедж `Cottage1`, Особняк `Manor1`, Пожарная бригада `FireBrigade1`, Дом лекаря `HealerHouse1`, Лаборатория `Laboratory1`
- **Склады**: Склад `Warehouse1`, **Кладовая `Granary1`** ⚠, Сокровищница `Treasury1`, Амбар `Barn1`, and a Большой/Большая version of each (`Big…1`)
- **Развлечения**: Городская площадь `TownSquare1`, Таверна `Tavern1`, Театр `Theater1`, Церковь `Church1`, Коллизей `Colosseum1`, Декоративный сад `DecGarden1`, Декоративное дерево `DecTree1`, Декоративный куст `DecBush1`, Флаг `Flag1`
- **Оборонительные**: Забор `Fence1`, Ворота `Gate1`, Башня `Tower1`, Казарма `Barracks1`, Укреплённый забор `FortifiedFence1`, Укреплённые ворота `FortifiedGate1`, Укреплённая башня `FortifiedTower1`
- **Производственные**: Лесопилка `Sawmill1`, Каменоломня `Quarry1`, Железный рудник `IronMine1`, Золотой рудник `GoldMine1`, Медный рудник `CopperMine1`, Угольный рудник `CoalMine1`, Плавильня `Smelter1`
- **Производство еды**: Фруктовый сад `Orchard1`, Ферма `Farm1`, Рыбацкая хижина `FisherHut1`, Свиноферма `PigFarm1`, Коптильня `Smokehouse1`, Ветряк `Windmill1`, Пекарня `Baker1`
- **Водные**: still five empty boxes. Мост/Пристань/Водяная мельница live in the game today and belong here.

⚠ Two traps this diagram cleared up, both of which I had got wrong: **Ратуша's model is `Castle1`** (there is no separate "Замок" building — I had invented one), and **`Granary1` is the Кладовая**, a fourth storehouse, not a grain silo.

**The category list is these seven**, and they are not the enum the code had (Housing/Food/Production/Infrastructure/Military/Decor). Housing, roads, the well, the healer, the fire brigade and the laboratory are all one category, Город.

Systems the taxonomy needs that DO NOT EXIST yet: storage capacity (8 storage buildings are meaningless while resources are unlimited), fire, illness/healing, processing chains that CONSUME a resource (Плавильня, Ветряк, Пекарня), water as a resource.

Scale to keep in mind: 48 buildings x 3 levels is ~144 models, more for the connecting pieces. Suggested to the user (not yet agreed): build one vertical slice at a time, e.g. Ферма → Ветряк → Пекарня, which would be the first real processing chain.

## Fence / wall line (2026-08-14, first models authored by the user)

The user is now producing 3D models themselves and started with the fence. Their stated plan:

- **Three levels, a model per level:** 1 wooden, 2 thin stone, 3 thick stone. Level 1 is built (`f136c72`).
- Segments **autotile**: a piece shapes itself to its neighbours. Implemented for the two models they authored (straight + corner) via `FenceShape`/`FenceNetwork`/`FenceAppearance`.
- The fence is an **obstacle** -- enemies must break it to get through. Works because every non-road building carves the NavMesh; the missing half (a blocked `OrcUnit` walked a straight line through walls) was fixed in the same commit.
- **Gates and towers will join the line later**, both still to be modelled. The hook exists: set `BuildingData.connectsToFences` and they register as connection points without changing their own model.

**Still open, asked but not yet answered:** are levels 1/2/3 the existing upgrade path (`BuildingInstance.TryUpgrade`, which today only bumps a number and never swaps the model or the stats) or three separate hotbar buildings? Swapping model+stats per level is the "planned for later" noted in `BuildingData`, and it is the prerequisite for levels 2 and 3 either way.

**Model authoring contract** (rewritten 2026-08-15, `8775160`): put FBX in `Assets/_Project/Models/Buildings/` named **`<Id>1-lvl1.fbx`** and the build picks it up BY NAME — no code change, procedural placeholder disappears. One cell is 1 m; footprint x is along X, y along Z. Geometry offset from the origin is tolerated (the build re-centres from renderer bounds and stands the model on the ground) and the **collider height is measured from the model**, so a stand-in of the wrong height is safe — the user is deliberately sending short stubs with final names. Nothing is scaled: a model wider than its plot gets a build warning and still hangs over its neighbours. Fence parts add a suffix (`Fence1-lvl1-Straight.fbx`); the straight run lies along Z, the corner joins south+east, and a model turned differently fails `FenceShapeTests` deliberately. Covered by `AuthoredModelTests` (click box matches the drawn model) and `RotatedFootprintTests`.

**Levels 2/3 ARE wired as of 2026-08-21** (`d4812570`): drop `<Id>1-lvl2.fbx` / `<Id>1-lvl3.fbx` in the same folder and the build hangs them in the prefab, the upgrade swaps them, and the click box follows the new height. The footprint stays level 1's whatever the later model measures. A level with no file keeps showing the level below it -- which is every building in the game today. The fence is the exception: it uses `FenceAppearance` for its straight/corner pair, and nobody has decided whether its three levels are an upgrade or three hotbar buildings.

## The first processing chain, spelled out (2026-08-16)

The user's own words, the first concrete chain of the deferred "цепочки переработки" topic:

**пшеница** (производит Ферма, хранится в Амбаре) → **мука** (производит Ветряк, хранится в Кладовой) → **хлеб** (производит Пекарня, хранится в Кладовой)

Note the split: wheat gets its own storehouse, flour and bread share the Кладовая with everything else edible.

**IMPLEMENTED 2026-08-17 (`c3c7314`), 211 EditMode + 34 PlayMode green.** Consumption is a general mechanic, not a bakery special case: `BuildingData.consumesResource` + `BuildingLevelStats.consumptionPerWorkerPerTick`, authored in the buildings tab as `consumes` / `consumption_per_tick(_2/_3)`. **A zero consumption is what marks a building as making its resource out of nothing** — there is no "None" in `ResourceType`, so nothing else could carry that flag. `ProductionBuilding.SuppliedWorkers` (pure, tested) runs as many worker stations as there is input for, so a half-supplied mill works at half strength. A converter **refuses the tick when its output store is full** rather than burning input (a plain producer still just wastes the tick), and latches one event-log line when starved.

First-pass numbers, all tunable in the sheet: Ферма 3 пшеница/tick/worker; Ветряк 2 пшеницы → 1 мука, 2/3/4 workers, requires Farm; Пекарня 1 мука → 2 хлеб, 2/3/4 workers, requires Windmill. `ResourceType` gained `Flour` and `Bread` (both → the Food storage group).

**Two side effects worth knowing:** the Ферма no longer produces generic `Food` at all — Рыбацкая хижина is the only source left until the meat/fruit chains exist. And the top HUD bar, which showed only timber/ore/money, now carries пшеница/мука/хлеб/еда too (widened 1300 → 1800); **the widened bar has never been seen on screen** — PlayMode screenshots capture an off-screen camera, which does not render a Screen Space Overlay canvas.

## Food consumption IMPLEMENTED 2026-08-17 (`c7f640f`)

`FoodConsumptionManager` + `ResourceDiet` + `CitizenManager.KillCitizens`. 227 EditMode / 34 PlayMode green. Decisions taken without asking (all reversible, numbers in the economy tab):

- **Everyone eats, citizens and soldiers** (`food_per_mouth_per_day` 0.5) — the soldier half was already the design's own decision in the army section.
- **A grace period before deaths** (`hungry_days_before_deaths` 2): first hungry day is an event-log warning only. Deaths are then proportional to the missing rations, not a flat toll. **This grace period is invented, not specified** — the user never said how harsh starvation should be.
- **Served round-robin across every edible store**, not largest-first, because the variety happiness score reads what was actually eaten.
- **`ResourceDiet.Edible` is the diet, deliberately not the storage group** — flour lives in the Кладовая but is not food; eating it would drain the bakery's input. New food types go in that one list.
- Happiness is now an average of **five** factors (tax, decay, defence, food variety, recent deaths); the panel's breakdown is two lines and the panel grew 110 → 130. Still missing: entertainment (3) and the work week (6).
- **Second defeat condition finally armed**: population 0 ends the map. Guarded by `_hadCitizens` because population is legitimately 0 before the Town Hall is placed.

`food_variety_target` is 2 because only two edible resources exist (хлеб, еда). **It must rise as food types are added**, or a full score stays trivially reachable — there is a test asserting the target never exceeds `ResourceDiet.Edible.Count`, but nothing forces it upward.

## Citizen roster panel -- IMPLEMENTED 2026-08-17 (`3d30352`) as a per-building list

A dedicated tab/panel listing **every citizen and the job they hold**, where the player can reassign people between production buildings from one place — "переназначить жителей на необходимые в данный момент производства".

What this collides with in the code as it stands: **citizens have no identity at all**. `CitizenManager` is a headcount pair (`_totalPopulation` / `_assignedPopulation`), and each `ProductionBuilding` keeps its own `AssignedWorkers` integer; the walking figures are pooled visuals owned by `CitizenVisualsManager`, not persistent people. Reassignment today is only the +/- buttons inside one building's info panel, so moving somebody from the sawmill to the bakery means opening two panels and knowing which building to rob.

**The user was shown both options and chose the per-building list** (individual named citizens was the alternative, and would have meant giving citizens identity plus a new save format). Opened by a `Жители` button in the top row at x -520, y 465 — the gap between Меню and the settlement-status text. `WorkforcePanelController` + `SetupProject.BuildWorkforcePanel`; 4 PlayMode tests, 38 green.

Rows are alphabetical (scene order reshuffles between opens), say what each workshop makes and eats, warm-tint an unstaffed building, and grey `+` out when nobody is idle. **Tapping +/- refreshes the numbers in place rather than repopulating** — per the mobile-first note. The list does rebuild when a row's building has gone (decay destroys buildings while the panel is open; `ModalGate` blocks input, not time).

**Trap for anyone testing UI lists here:** `Destroy()` is deferred to end of frame, so counting `listContent.childCount` right after repopulating counts last time's rows too. That is why the controller exposes `RowCount` off its own list — a PlayMode test failed on exactly this first.

## Metals chain + recipe selection IMPLEMENTED 2026-08-17 (`f0ebd2f`)

238 EditMode / 38 PlayMode green. The user's decisions this session, do not re-ask:

- **The Пристань and the Водяная мельница produce NOTHING** ("это бред" — a dock mining gold, a mill growing food). They stay in the catalogue as placeholders until the Водные are designed.
- **Existing `Iron` and `Gold` are the ORE**, not the metal. New resources: `CopperOre`, `IronBar`, `CopperBar`, `GoldBar`. Ore stays a raw good in the Склад; bars go in the Сокровищница. The enum names `Iron`/`Gold` were deliberately NOT renamed — the whole sheet spells them that way; `ResourceNames` says "железная руда".
- **The player picks the metal in the Плавильня's own card.** That is what recipes-as-a-list exist for; see [[project_balance_pipeline]] for the data shape.

Decided by me, flag if wrong: 2 ore + 1 coal → 1 bar (**this is what finally gives coal a consumer** — it had been mined and spent on nothing since the beginning); Укреплённая башня / Укреплённые ворота / Лаборатория now cost `IronBar` instead of raw iron, because otherwise bars would be a fresh dead end.

**The resource bar now shows only resources the settlement has met**, and keeps showing them once seen. Fifteen fixed chips did not fit; a chip that vanishes when you spend the last unit is worse than a zero.

**Still owed by the user (asked 2026-08-17, unanswered):** the FOOD half — Фруктовый сад (фрукты), Свиноферма (мясо), Коптильня (мясо → копчёности), whether Рыбацкая хижина switches to рыба and the generic `Food` retires, and what the bars are spent on beyond those three buildings (the design's equipment buildings do not exist).

## Laboratory / research — the user's full spec (2026-08-17, asked and answered in detail)

Their own words, gathered over six rounds of questions. **Do not re-ask any of this.**

- **The window**: tapping the Laboratory opens the research window ITSELF, not the ordinary building card (they picked option (б) explicitly) — so the scientists' +/-, Улучшить and Починить live in that window's header. Two tabs: Изучение зданий, Изучение солдат.
- **Cost is coins. Time is real seconds**, "например, 1 минуту здание до 2 уровня". All researches are ≥ 60 s.
- **Scientists**: up to 10. Each one **after the first** cuts 5 s off the total; with **zero the research is impossible** (it stands still, it is not lost). Removing one mid-research adds its 5 s back, adding one takes 5 s off — the user spelled this out both ways.
- **One research at a time.** Coins are taken at the start. **Cancelling returns 70 %.**
- **Laboratory levels gate what may be researched**: level 1 only OPENS buildings and soldier types, level 2 permits second levels of anything, level 3 permits third ones. Its own levels are bought with resources and are deliberately NOT gated (confirmed).
- **Lose the Laboratory and nothing researched is lost** — rebuild it and everything works; already-researched levels can still be upgraded meanwhile. A research interrupted by its loss **refunds the coins in full and resets**.
- **Two kinds of building gate**: some are built first and their levels researched later; others must be researched before they can be built at all. lvl2 is always required before lvl3.
- **The eighteen buildings open from the first minute** (everything else is closed — the user said so explicitly): Ратуша, Колодец, Лачуга, Дорога, Лаборатория, Склад, Кладовая, Городская площадь, Декоративное дерево, Декоративный куст, Флаг, Забор, Ворота, Казарма, Лесопилка, Каменоломня, Фруктовый сад, Рыбацкая хижина. **Мост is therefore closed** — flagged to them as a possible early-game wall; they kept it closed.
- **Сокровищница is closed**, and they asked for "немного ресурсов в ратуше" until it is opened — that already exists as `base_capacity_valuables` (300), no new mechanic needed, only a number to tune.
- **Soldiers**: Казарма and Ополчение are available from the start (level 1 free); **3 levels**, levels 2 and 3 researched at a Laboratory of that same level. A researched level raises stats **for soldiers already recruited**. All stats grow (the specific numbers are still to be discussed). Recruitment cost and upkeep both rise with the level; **food consumption does not**.
- **Research replaces the equipment buildings for now** (Лучная мастерская, Оружейная etc. from the Army section stay a later increment).
- **The window's list is compact**: only what can be researched right now, grouped by the seven categories, with already-researched rows kept in place greyed out with a tick.

## Pending topics -- user will describe later, ask them
- **The rest of the processing chains** (ore → metal, meat → smoked, fruit → wine): the grain chain above is decided, the others are not. Don't assume them.
- **Entertainment buildings** (what specific buildings, what happiness values) and **seasons** (cosmetic only, or gameplay effects like less food from farms in winter) -- explicitly left open 2026-08-12 ("вопрос досуга и сезонов оставляем открытым, в будущем обсудим, напомни"). Bring these up again in a later session/turn instead of assuming a design.

## Resource & food types (2026-08-12, extend this list over time)
Player's current wishlist for what the game's resource economy should eventually cover — not all implemented yet. As of 2026-08-12, ResourceType enum only has: Wood, Stone, Food, Gold, Population, Iron, Coal.

- **Ресурсы:** дерево, камень, уголь, железная руда, золотая руда, медная руда, песок, глина, вода, шкуры
- **Еда:** фрукты, мясо, хлеб, овощи, рыба, вино
- **Валюта:** золото/золотые слитки (сырьё) отдельно от монет (см. Army recruitment выше) — монеты как отдельная трата-валюта, не то же самое, что золото.

## Army slice IMPLEMENTED 2026-08-13 (`SoldierUnit`, `ArmyGroup`, `ArmyManager`, `SoldierStats`, `NavRoute`, `ArmyPanelController`, `ArmyOrderInput`, destructible `OrcPortal`)

First playable win condition. Design questions answered by the user this session (these were open before, don't re-ask):

- **Population coupling:** recruiting takes one IDLE citizen out of the town (`CitizenManager.TryTakeIdleCitizen`), disbanding returns them (`ReturnCitizen`), and a soldier KILLED in battle is a permanent population loss. The asymmetry is deliberate: losing a fight must cost more than changing your mind.
- **First soldier tier:** "жители с вилами" (Militia) -- no armour, low HP (12), decent damage (5), coins ONLY to recruit (25), 1 coin/day upkeep. Explicitly NOT gated behind the equipment buildings/Laboratory, which stay a later increment.
- **Command model:** groups, never individual units. One group per unit TYPE (user picked the "по типу, но с делением позже" option after confirming it looks identical to a plain per-type model for the player -- `ArmyGroup` is a real object with an id and member list so splitting a type into two squads later doesn't rewrite orders/UI). Right-side panel lists one row per group.
- **Target priority** (бить врагов / ломать строения) is a PERSISTENT per-group mode toggled on the group's row, not a one-shot order.
- **Idle behaviour:** a group with no order holds its ground and only engages within ~7m of its formation slot. No chasing across the map, no auto-intercepting raids.
- **Victory:** one portal on the map today; destroying the last portal wins (`GameOverManager.IsVictory`, shared end-of-map panel re-titled per outcome). Portal has 320 HP and a trigger click collider.
- Orcs now fight back: `OrcUnit` prefers any soldier within 6m over the building it was walking to, and returns to the building when none is near.

**Known gaps, deliberately deferred (flag before building on top of them):** the army is NOT saved/loaded (soldiers vanish on load and their citizens do not come back -- the save's population was already reduced by recruitment); soldiers don't eat food (no food-consumption system exists at all); no equipment buildings / Laboratory / higher tiers; no orc base around the portal (the design's "clear the base first" step); splitting one type into several squads is unimplemented.
