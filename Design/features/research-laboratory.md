# Research and the Laboratory

> **Status (2026-10-21):** shipped to the user's full spec (2026-08-17). The tree is "a price list,
> not a tree" (audit): ~8 600 coins, every L1 topic 60 s; re-price only after a playtest.
> **Read with:** [building-levels-and-decay](building-levels-and-decay.md), [army-and-soldiers](army-and-soldiers.md).

## Rules (the user's spec — do not re-ask)
- Tapping the Laboratory opens the RESEARCH WINDOW itself (not the building card); scientists +/-,
  Улучшить, Починить live in its header. Two tabs: buildings, soldiers.
- Cost in coins, taken at start; time in real seconds (≥ 60 s). One research at a time. Cancel
  refunds 70 %.
- Scientists: up to 10; each AFTER THE FIRST cuts 5 s; zero = research stands still (not lost);
  removing one adds its 5 s back.
- Lab level gates: L1 opens buildings and soldier types; level N permits level N of anything. Lab's
  own levels are NOT gated.
- Lose the Lab → nothing researched is lost; an interrupted research refunds in full and resets.
- Two kinds of building gate: built first + levels researched later, or researched before building.
- 18 buildings open from minute one: Ратуша, Колодец, Лачуга, Дорога, Лаборатория, Склад, Кладовая,
  Городская площадь, Декор. дерево, Декор. куст, Флаг, Забор, Ворота, Казарма, Лесопилка, Каменоломня,
  Фруктовый сад, Рыбацкая хижина. Мост and Сокровищница are closed.
- Soldiers: Казарма and Militia L1 free; levels 2/3 researched; a researched level upgrades soldiers
  already recruited. Research replaces equipment buildings for now.
- The list: only what's researchable now, grouped by the seven categories, completed rows greyed with a tick.

## Code
`Research/ResearchManager.cs`, `Research/ResearchCatalog.cs` (category order, skips orc rows),
`Research/ResearchTopic.cs` (`RequiredLabLevel`, `ResearchKind`), `UI/ResearchPanelController.cs`,
`Cheats/ResearchCheat.cs`. Built in `Editor/SetupProjectPanels.cs` (`BuildResearchPanel`).

## Data
buildings/units: `research_coins(_N)`, `research_sec(_N)`, `starts_unlocked`; economy
`research_seconds_per_worker`, `research_free_workers`, `research_cancel_refund_percent`, `research_min_seconds`.

## Tests
EditMode `ResearchTests`; PlayMode `ResearchGateTests`.

## Traps
`#research_empty_lab_level` is effectively unreachable now (kept as a safety net). A locked soldier
tier stays tappable on purpose — that's how a player learns what the Lab is for.
