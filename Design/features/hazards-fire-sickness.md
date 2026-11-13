# Hazards: fire and sickness

> **Status (2026-11-13):** sandbox healer v2 (beds) BUILT, not seen, untested. **Earlier (2026-10-21):** shipped 2026-08-19, numbers first-pass and NOT balanced (first playtest:
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

## Plague + Cemetery -- DECIDED 2026-11-13 (the user), NOT BUILT (cemetery model: level 1 render first, then levels)
Plague (sandbox, per island, never spreads to other islands):
- Starts when illness killed 10% of an island's residents within 5 minutes.
- While it lasts the island's houses make NO new residents; the rest of the harm is the deaths themselves.
- Hints: «build infirmaries» (pointing at it); short of resources -> the existing offer to get them;
  illness killed 40% of the island -> the game PAUSES and a banner offers to end the plague for a
  rewarded ad or crystals.
- Look over the island: a green haze on the ground + circling ravens.
- How it ends besides the banner: OPEN (proposed: T minutes with no illness death).
Cemetery (new building, sandbox):
- Every death (any cause) leaves a skeleton lying on the ground with a skull icon over it; each body
  on an island raises its illness chance by +2%.
- With a cemetery on the island, bodies vanish by themselves into it (no worker) and graves appear on it.
- Capacity 20, levels via the Laboratory 20/30/40/50/60; the player picks: a new cemetery or an upgrade.
- Demolishing a cemetery costs 50 crystals (else it would be a free way to clear bodies).
- Bodies over 10% of the island's residents -> notice + hint «build a cemetery to avoid plague».
- Model: headstones and a fence round the edges; level 1 render first, then the other levels.
  Level 1 drafted 2026-11-13: Tools/blender/cemetery1.py (2x2 plot, picket fence, gate, path, memorial
  cross, 20 grave places 4x5 filling front-first; base 1514 tris, ~39 a grave), review
  Design/buildings/Cemetery_L1_review.png -- APPROVED 2026-11-13. Levels 2-5
  (stone footing + cobbles + lamps / chapel with gilded cross / glazed / gilded roof) in
  Design/buildings/Cemetery_levels_review.png -- AWAIT APPROVAL. 20 places at every level: past lvl 1 a grave = a share of the dead.

## Code
`Buildings/FireManager.cs`, `Buildings/BuildingFire.cs`, `Citizens/SicknessManager.cs`,
`Buildings/BuildingInstance.cs` (`TryDamage`).

## Data
economy: `fire_*`, `sickness_*`, `heal_per_healer_per_day`; buildings `service_radius(_N)`.

## Tests
EditMode `SicknessAndFireTests`; PlayMode `HazardTests`.
