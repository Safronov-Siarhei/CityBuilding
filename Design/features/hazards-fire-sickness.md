# Hazards: fire and sickness

> **Status (2026-11-13):** sandbox healer v2 (beds) DECIDED, concepts sent, not built. **Earlier (2026-10-21):** shipped 2026-08-19, numbers first-pass and NOT balanced (first playtest:
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

## Sandbox healer v2 -- DECIDED 2026-11-13, NOT BUILT (concepts Design/ui/Healer_Concepts.png await approval)
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

## Code
`Buildings/FireManager.cs`, `Buildings/BuildingFire.cs`, `Citizens/SicknessManager.cs`,
`Buildings/BuildingInstance.cs` (`TryDamage`).

## Data
economy: `fire_*`, `sickness_*`, `heal_per_healer_per_day`; buildings `service_radius(_N)`.

## Tests
EditMode `SicknessAndFireTests`; PlayMode `HazardTests`.
