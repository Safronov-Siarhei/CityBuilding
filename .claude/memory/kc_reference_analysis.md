---
name: kc-reference-analysis
description: "Design/UI/balance findings from observing Kingdoms and Castles (2026-08-07), to inform CityBuilding feature decisions"
metadata: 
  node_type: memory
  type: project
  originSessionId: 04bdd1d4-f9be-4484-b721-4f059c90b34c
  modified: 2026-08-07T14:45:51.553Z
---

On 2026-08-07 the user played a fresh Kingdoms and Castles game (Russian localization) — first while I watched via my own periodic screenshots (synthetic input never reached the game despite correct focus/coordinates, root cause undiagnosed), then the user sent a batch of their own manual-playthrough screenshots covering much more of the UI. Findings below are concrete, observed mechanics/UI to compare against as CityBuilding grows, not guesses.

**Core loop / progression:**
- Settlement tier names change with population growth, shown as a toast ("Год 1: Поселение Онетт выросло. Теперь это Тихая гавань") — validates the tier-name branch already sketched in the GDD artifact (Хутор→Деревня→Городок→Город→Королевство).
- Housing is a hard population cap: beds == max population. When full, new "travelers" are turned away with a repeating log entry ("путники не нашли ночлега, нужно больше домов!") — constant, narratively-framed pressure to keep building housing ahead of demand, rather than a silent cap.
- New Game flow: choice between "Стандартный режим" (curated difficulty + random map) and "Творческий режим" (full sandbox: difficulty, settler/viking/resource counts, instant free building, in-editor map customization). CityBuilding's MapSelector/unseen-first-map pick is a partial analog to the "random map" part of Standard mode; a Creative-mode-style sandbox toggle (free building, tweak counts) is a plausible future addition.

**Resource economy (deeper than assumed):**
- Food is not one number — it's an aggregate of sub-categories (grain/fruit/meat/fish, each with its own storage cap, e.g. 0/50) rolled into a total.
- Clicking a resource icon opens a full **annual income/expense ledger**: itemized sources (farms/orchards/bakeries/fishing/pigs) and sinks (houses, taverns, pigs, tournament arena, theater, trade), plus **spoilage** ("Сгнило: -0") and a net total for the year. Strong transparency pattern — worth adopting once CityBuilding's resource system has enough sources/sinks to justify a breakdown view.
- Entertainment/culture buildings (tavern, theater, tournament arena) consume resources to *operate*, not just to build — ongoing upkeep tied to amenities, separate from one-time construction cost.
- Wood/resource gathering is **zone-based**, not building-centric: player drag-selects trees in "Режим рубки" (chop mode) and workers autonomously walk to marked trees. No dedicated "lumberjack building" tied to worker slots for logging specifically — architecturally different from CityBuilding's current ProductionBuilding model (fixed worker slots per building).
- Resource deposits are labeled directly on the terrain ("Железо" floating over a rock cluster) — a readability feature worth adding once our Forest/Stone TerrainType map data (MapDefinition/MapTerrainGenerator) is tied to actual resource nodes. A hovered/selected tile also shows a quality card, e.g. "Плодородная — Земля готова для земледелия" (Fertile — land ready for farming) — directly validates giving our TerrainType tiles per-tile suitability tooltips.

**Building system depth:**
- Defense/government category alone has ~17 entries: wood + stone tiers of walls and gates, castle stairs, archer tower, ballista tower, a "Greek fire" trap structure, treasury + great treasury, diplomacy hall, war hall, great hall, moat, barracks, archery school, siege workshop. CityBuilding currently has only Wall + Tower — good expansion reference.
- Industry category shows a **small/large tiered-upgrade pattern as separate buildings** rather than in-place upgrades: Quarry→Large Quarry, Charcoal Kiln→Large Charcoal Kiln, Iron Mine→Large Iron Mine, Small Warehouse→Warehouse.
- **Building prerequisites**: some buildings require another building type to already exist in the city, shown directly in the placement cost tooltip ("Требуется: Лесоруб" — Required: Lumberjack) rather than a separate tech tree.
- **Negative externality / pollution**: the Charcoal Kiln's tooltip explicitly warns its smoke "не нравится близлежащим домам" (disliked by nearby houses) — a spatial happiness penalty tied to proximity, encouraging zoning industry away from housing. CityBuilding has no happiness/proximity system yet; this is a concrete pattern to crib from when one is added.
- Buildings can be **rotated** before placement (R key or middle-mouse-drag) — CityBuilder's BuildingPlacer has no rotation yet.
- Placement UI shows small corner markers (not a full color-tinted ghost silhouette like ours), and a local grid overlay appears only within the settlement's owned territory / during active placement, not globally.
- A translucent dome/circle appears around a selected building — likely a service/influence-radius indicator.

**HUD layout (from a full manual playthrough):**
- Top-left: kingdom name (editable) + settlement tier name + speed controls (pause/1x/2x/3x) + a collapsible, scrollable narrative event log (magnifying-glass icon per entry).
- Top-right: Share button, Menu button, minimap (circular, shows camera-frustum overlay and rotate/compass toggle buttons).
- Bottom-left: population, idle-worker count, bed count, a "work priority" button, a happiness-like icon+value, and a **tax-rate slider with +/- arrows** (0% shown) — taxation is a player-adjustable dial, not fixed.
- Bottom-center: two-tier bar — building **category tabs** (icon-only row) above a **detail row** of that category's buildings (icon + label), plus a separate small **action toolbar** (chop-trees / stop-chop / mine-stone / cancel-demolish) off to the side.

**Save/Load UI:** Grid of save-slot cards, each showing a map-thumbnail screenshot, resource-icon summary, city name, Year + Population, and a per-slot delete (trash) button; one slot is a distinct "Автосохранение" (autosave) entry alongside manual saves — note CityBuilding deliberately has *no* autosave per explicit user request, so this is a documented alternative, not a suggested change.

**Main menu (fuller than first seen):** Full button list is Новая / Загрузка / Отправка королевства / Настройки / Титры / **Модификации** (Mods) / Выход (Exit) — earlier observation only caught the first five (screen-height cropping). Also has: a "Новости" panel previewing the latest content update, DLC and next-game ("Nova Roma") cross-promo panels, and a language dropdown right on the main menu.

**Pause menu:** Назад / Сохранить / Загрузка / Настройки / *(visual gap)* / Выход — the gap before Exit reads as a deliberate misclick guard on the most destructive action in the list.

**Seasons:** Visibly reskin the whole palette (snow accumulation in winter) while geometry stays identical — cheap to replicate on CityBuilding's cube-primitive buildings via material re-tinting rather than new meshes.

**How to apply:** Pull from this list when scoping future CityBuilding features (rotation, zone-based resource gathering, tier-name toasts, resource ledger/spoilage, building prerequisites, pollution/happiness proximity penalties, tiered small/large buildings, radius indicators, seasonal re-tinting) instead of re-deriving from scratch or asking the user to re-describe K&C.
