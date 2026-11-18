---
name: project-sandbox-balance-2026-11-18
description: Sandbox building tabs filled on the user's price/time pattern, instant build priced by time, one mandatory worker for Tax House/Diplomacy/Cemetery, ore icon concepts waiting
metadata:
  type: project
---
2026-11-18, the user's balance request:
- The user hand-filled «Песочница: Здания (Город/Склады/Производство)»; Claude filled Еда/Оборона/Водные/Декор on the same pattern (rules in Design/features/sandbox-run-systems.md) -- the user may retune them.
- Upgrade gains (max_workers, hp, defense, radius, beds, graves, happiness...) moved to the right end of every tab.
- Instant build: 1 crystal per started 30 s of the REMAINING build (instant_build_step_sec). Remaining, not total -- say so if the user meant total.
- Tax House, Diplomacy Hall, Cemetery: 1 mandatory worker (ProductionBuilding.Manned). Needs setup x2 by the user (Unity was open).
- Ore icons: look D (solid chunk) chosen, iron red facets, copper dark-copper facets -- IN THE GAME (UI/Icons re-rendered).
- Android module installed by hand 2026-11-18 (NDK r27c + SDK pieces from modules.json); Unity must be restarted to see it.
Related: [[project-balance-pipeline]], [[feedback-approve-models-first]]
- Ore heaps/cart loads on the 4 mines, Smelter, Warehouse, BigWarehouse (all 5 levels): solid chunks, iron red, copper dark-copper -- approved and FBX re-exported 2026-11-18.
