# Hazards: fire and sickness

> **Status (2026-11-13):** sandbox healer v2 (beds) BUILT; PLAGUE (+ 7-min shield after the banner) + CEMETERY + the dead BUILT (setup green), not seen, untested; lying skeleton Remains1 approved and in. **Earlier (2026-10-21):** shipped 2026-08-19, numbers first-pass and NOT balanced (first playtest:
> 3 dead of illness by day 7 — clearly harsh).
> **Read with:** [building-levels-and-decay](building-levels-and-decay.md), [citizens-and-workforce](citizens-and-workforce.md).

## Rules (decided)
- **Fire chance IS decay** (user's call): a maintained town never burns; repair is fire insurance.
  Spread ignores the neighbour's decay (gaps between districts matter).
- An unattended fire burns `fire_burn_seconds`, eats a chunk of the building and stops. Each
  firefighter in reach (Пожарная бригада `service_radius`) subtracts seconds, floored — recomputed
  from the CURRENT crew. Fire damage goes through `BuildingInstance.TryDamage` (Town Hall lost to fire = defeat).
- **Fires are diced; illness is worked out**: illness chance = base + hungry + no well in reach.
  Sick people leave their jobs. The untreated streak counts days the BEDS WERE NOT CLEARED
  (one healer must not immunise a town of forty). Дом лекаря heals `heal_per_healer_per_day`.
- Well, Healer, Brigade share one per-level `service_radius` column (Well 14/18/24, Healer 16/20/26, Brigade 18/24/30).
- No sickness while the town settles in (easy start, 2026-10-20). Fires and illness are off in Invasion.
- Saved: sick count, untreated streak, every fire with ELAPSED time (remaining depends on the crew after load).

## Sandbox healer v2 -- BUILT 2026-11-13 (concepts Design/ui/Healer_Concepts.png APPROVED), not seen by the user
The user's rules, SANDBOX ONLY (other modes keep the radius healer above):
- Illness chance unchanged (daily roll). A sick resident leaves his job (another is hired), pays no tax,
  and WALKS to the nearest healer on his island with a free bed, a sickness mark over his head.
- No radius: beds. Beds by level 2/3/4/5/6; healers (workers) 1/2/2/3/3. Several healers on an island: beds add up, nearest free one.
- A free bed is TAKEN THE MOMENT he falls ill (he walks there meanwhile): his death timer stops and
  treatment counts from then (he is on the healer's list). In a bed = never dies. Without a bed a sick resident lives 30 s (the walk counts); a bed freed in time
  goes to whoever fell ill FIRST. No healer on the island = he dies.
- Treatment 60 s per patient, in queue order; as many at once as healers assigned.
- A healer with no worker takes nobody (no-worker mark + notice «Назначьте рабочего лечебницы»);
  beds short: notice «На острове не хватает мест в лечебнице».
- Bottom bar on tap: 6 bed slots (empty = a plus / portrait / portrait in a 60 s ring; locked «Ур. N»), no captions under them; sickness mark = a GREEN SKULL + «!» 7th. No bar over the building.
- Numbers on the sheet as per-level columns (beds, workers, 60 s, 30 s).
- Workers: general auto-assignment, BUT the player's manual count has priority for EVERY building
  (lowered 3->1: auto never gives more than 1; set to 0: no-worker mark stays, auto never staffs it).

**How it is built (2026-11-13):**
- `Citizens/Infirmary.cs` (on the managers, `Infirmary.Active` = not Invasion / tutorial / campaign town):
  patients follow `CitizenManager.SickPopulation` (Reconcile), each one a villager figure taken by
  `CitizenVisualsManager.TakeForPatient` (idle first, else off a job -- the place stays wanted); beds
  each frame oldest first to the nearest healer on his island that `Takes` (built, a healer in it,
  beds > 0); in bed the figure walks to `DoorOf` and is hidden; cured -> `ReturnFromPatient`; dead ->
  `KillAgent` (+ HealSick, RecordDeaths, #log_plague_deaths). A healer that loses its last worker
  lets its patients out (back to waiting, fresh 30 s).
- SicknessManager.PassDay: in the sandbox only FallIll; no daily Heal / untreated streak.
- HousePeople.IdleOn and LayOffIslandExcess subtract the island's sick; TaxManager taxes HealthyPopulation.
- Mark: `UI/Icons/SickMark.png` (Tools/ui/sick_mark_icon.py) on `Materials/SickMark.mat`
  (IconBillboard, new `_Ring` property: the red ring left, -1 none).
- Bottom bar: `FocusHireBar.Kind.Healer`, 6 buttons (faces = Hud_Plus / patient portrait / Hud_Lock with
  «Ур. N» band), `rings` = UI/Icons/HealRing.png filled radially.
- Card line `#healer_card` (Infirmary.CardLine via FoodLine); notices `#log_infirmary_full`, `#log_infirmary_no_worker` (once per 20 s per island).
- Data: buildings `beds..beds_5` (HealerHouse 2..6); sandbox tab «Город» `max_workers..max_workers_5` for
  all its 12 rows (HealerHouse 1/2/2/3/3) -- a blank level-1 cell there would wipe the base value;
  economy `infirmary_treat_sec` 60, `infirmary_wait_sec` 30.
- Workers: in the sandbox EVERY workplace is AutoStaffed (lab, healer, brigade too); the player's own
  count caps it (WorkersCapped); emptied by hand = no-worker mark stays, nobody sent.
- SAVED (2026-11-13): `GameSaveData.patients` (PatientEntry: home cell, bed cell, waited, treated,
  order, inside) -> `Infirmary.RestoreFromSave` after the buildings; each gets a figure of his own house
  back (`TakeForPatient(home)`), inside ones hidden at the door. An old save (no list) rebuilds them
  from the sick count with fresh clocks.
- NO RADIUS in the sandbox: «Песочница: Здания (Город)» has `service_radius..service_radius_5` for its
  12 rows -- HealerHouse 0, Well/FireBrigade copy the base (keep them in step with «Здания»).
- Trap: a patient's figure on another island than its healer never walks (no healer there = he dies, by the rules).

## Plague + Cemetery -- DECIDED 2026-11-13 (the user), BUILT 2026-11-13 (not seen, untested)
Plague (sandbox, per island, never spreads to other islands) -- `Citizens/Plague.cs`:
- Starts when illness killed `plague_death_share` 10% of an island's people within `plague_window_sec`
  300 (Infirmary.Die -> Plague.RecordIllnessDeath).
- While it lasts the island's houses make NO new residents (HousePeople.Update); the rest of the harm
  is the deaths themselves. Notice #log_plague_start «стройте лечебницы»; short of resources -> the
  existing shop offer when placing.
- `plague_banner_share` 40% of the island dead since the outbreak -> the game PAUSES and
  `UI/PlagueBanner` offers to end it: rewarded ad (stand-in: ShopAdStubSeconds) or `plague_end_crystals` 30.
- Ends by itself after `plague_calm_sec` 180 with no illness death on the island (my proposal, not
  objected), or by the banner. Saved (OutbreakEntry).
- SHIELD (the user, 2026-11-13): ended BY THE BANNER (ad or crystals) -> no plague breaks out on ANY
  island for `plague_shield_sec` 420 (7 min, game time; illness deaths meanwhile are not counted);
  notice #log_plague_shield; saved (SaveData.plagueShield). Plague.EndByBanner / ShieldLeft.
- Look: SwampHaze patches (PlagueHaze.mat) over the island's buildings + a CrowFlock (OrcRaidManager.CrowModel).
- BANNER art v2 APPROVED 2026-11-13 (v1, a plain card, was «дёшево»): the Rate window's build --
  wood window over a field of green skulls (3 depths, varied size/turn), the dungeon skeleton
  (MonsterSkeleton, helmet raised so the skull no longer pokes through) at each side, red ribbon
  «Чума!», parchment, green ad button with a play mark, crystals button = ONLY the price + the
  crystal icon (the user: no words). Art: `Tools/ui/plague_banner_art.py <figures folder>` ->
  `Assets/_Project/UI/Plague/*.png + layout.json` + mock-up `Design/ui/Plague_Banner.png`; figures
  `render_icons_3d.py -- figure_skeleton_l|figure_skeleton_r`; prefab part `SetupProjectPanels.BuildPlagueBanner`
  (scale 0.6). `#plague_banner_crystals` is no longer shown (the label is the number).
Cemetery + the dead -- `Citizens/Remains.cs`:
- Every death (any cause) leaves a body where the figure fell (CitizenManager.KillCitizens(n, bodiesLaid)
  -> BodiesOwed -> CitizenVisualsManager drops it; KillAgent drops its own) with a skull mark; each body
  on an island adds `remains_sickness_per_body` +2% to its illness (SicknessManager.FallIllByIsland,
  the sick then taken from that island: Infirmary.Expect).
- A Кладбище (building `Cemetery`, 2x2) on the island takes them by itself, no worker; capacity
  `graves` 20/30/40/50/60 by level (Lab); its 20 grave places fill to its share (Grave1-stone/light/cross).
- Demolishing costs `cemetery_demolish_crystals` 50 (BuildingInfoPanelController).
- Bodies over `remains_notice_share` 10% of the island's residents -> notice #log_remains_build_cemetery.
- Models: Tools/blender/cemetery1.py (levels 1-5 APPROVED), installed by cemetery_install.py.
  Remains1 (lying skeleton, 386 tris) APPROVED 2026-11-13 and exported (Models/Buildings/Remains1.fbx).
- Bodies and graves saved (SaveData bodies / GraveEntry).

## Sandbox fires v2 -- DECIDED 2026-11-14 (the user), NOT BUILT
Why: decay is OFF since 2026-10-25, and fire chance WAS decay -> no fire has started anywhere since.
SANDBOX ONLY (other modes: no fires at all).
- Chance: random, every burnable building, `fire_chance_per_day` (~0.5%/game day), x2 when no water
  within 8 CELLS (a well, the sea, a river) -- the user: 4 cells would force wells everywhere.
- No fires and no illness the first 10 minutes of a run (was 7 for illness).
- Spread ONLY to a burnable building TOUCHING it (no gap); a 1-cell gap stops it. Every 10 s of
  burning, 30% per touching neighbour.
- Never burn: Road, Square, TownSquare, Moat, Bridge, Well, Castle (town hall), FortifiedTower
  (Бастион), FortifiedGate (Барбакан), Lighthouse, Cemetery, Quarry, the mines. Fence, Gate and Tower
  burn only at levels 1-2 (stone from 3). EVERYTHING ELSE burns, Flag and decor (tree, bush, garden) too.
- Burning: 1.5% of max hp per second (~65 s to nothing untended); a fire STRENGTH 0..100 grows
  +2/s, damage scales with it; each bucket takes 25 off; 0 = out. Destroyed -> ruins (existing rule).
- Orc raids: 1-2 TORCHBEARERS per raid, only from the player's Town Hall level 2: run to the nearest
  burnable building, 3 s to light it (killable meanwhile), then the next (variant A). Model to approve.
- THUNDERSTORM (all maps): every 8-12 min a dark cloud with rain crosses part of the map; every
  15-30 s a 25% strike on the tallest burnable building under it = fire; fires under rain burn half
  as fast. Later maybe a «Громоотвод» research.
- Firefighters: take a bucket, run to the nearest water (well / river / sea), fill 1 s, run to the
  fire, pour; repeat until out; then home.
- Residents: a wandering resident sometimes goes into a burnable building for 5-15 s (hidden); a fire
  starting while he is in kills him; the body lies on the nearest free cell that no building covers --
  a road, Square or TownSquare cell is fine (into Remains / cemetery).
- BANNER like the plague's: fire destroyed ~15% of the player's buildings within 5 min -> pause,
  ad or crystals put out every fire (+ a no-fire shield like the plague's 7 min). CONCEPT FIRST.
- Fire Brigade = variant A (like the Infirmary): no radius, the whole island; the nearest brigade with a
  free firefighter answers; none on the island = nobody puts it out. Levels: firefighters 1/2/2/3/3;
  lvl2 big bucket (40 instead of 25); lvl3 a water barrel at the station (first trip skips the water
  run); lvl4 runners +25% speed; lvl5 prevention -50% fire chance on the island. No worker = mark +
  «Назначьте пожарного». Fire mark over a burning building, notice «Пожар! Постройте пожарную бригаду».

## Code
`Buildings/FireManager.cs`, `Buildings/BuildingFire.cs`, `Citizens/SicknessManager.cs`, `Citizens/Plague.cs`,
`Citizens/Remains.cs`, `UI/PlagueBanner.cs`,
`Buildings/BuildingInstance.cs` (`TryDamage`).

## Data
economy: `fire_*`, `sickness_*`, `heal_per_healer_per_day`; buildings `service_radius(_N)`.

## Tests
EditMode `SicknessAndFireTests`; PlayMode `HazardTests`.
