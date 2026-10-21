# Feature cards — read this first, then ONLY the cards the task touches

The project is ~50k lines of C# plus Blender/Python tools and a balance sheet. Nobody (human or
Claude) should read all of it to discuss one mechanic. Every feature has a card here: what the
player sees, the rules the user already decided (do not re-ask them), the code/data/tests that
implement it, the traps already paid for, and what is open.

**How to use (for Claude):**
1. Find the feature in the table below (match by keywords). Read that card, plus the cards in its
   «Read with» line only if the task crosses into them.
2. Open code files only from the card's «Code» list, and only the parts you need.
3. When the work is done, UPDATE THE CARD (status line, rules, files, open items). A card that lies
   is worse than none. A new feature gets a new card and a row in this table.
4. Cards hold durable knowledge. Day-by-day history lives in git log and `archive/`.

Shared plumbing almost every feature touches: [balance-and-localization](balance-and-localization.md)
(numbers/text never in C#) and [build-and-test](build-and-test.md) (SetupProject generates the whole
game; batchmode loop). Read those two only when changing data or running Unity.

| Card | Covers | Keywords |
|---|---|---|
| **Economy** | | |
| [resources-and-storage](resources-and-storage.md) | resource types, storage families, capacity, coins, taxes, lifetime produced | ресурсы, склад, вместимость, налог, монеты |
| [production-chains](production-chains.md) | workshops, recipes, grain→flour→bread, ore→bars, minting, recipe picker | производство, рецепт, плавильня, пекарня, чеканка |
| [gathering](gathering.md) | trees/rocks, harvest radius, gatherer walk, manual tap-to-gather, regrowth | лес, камень, сбор, радиус, лесопилка |
| [food-and-hunger](food-and-hunger.md) | eating, diet, starvation deaths, food variety | еда, голод, рацион |
| **Population** | | |
| [citizens-and-workforce](citizens-and-workforce.md) | headcount, workers, workforce panel, citizen agents, tiers | жители, рабочие, назначить |
| [happiness-and-migration](happiness-and-migration.md) | 6-factor happiness, arrivals/departures | довольство, миграция |
| [hazards-fire-sickness](hazards-fire-sickness.md) | fire from decay, brigade, illness, well, healer | пожар, болезнь, колодец, лекарь |
| **Buildings** | | |
| [buildings-catalog](buildings-catalog.md) | the 49-building roster, categories, ids, BuildingData, model contract | здания, ростер, категории |
| [building-placement](building-placement.md) | placing, ghost looks, rotation, fences autotile, roads, demolition, no-build, start zone | установка, призрак, забор, дорога, снос |
| [building-levels-and-decay](building-levels-and-decay.md) | levels 1–5, upgrade rules, level models, decay, repair | уровни, улучшение, ветхость, ремонт |
| [research-laboratory](research-laboratory.md) | tech tree, scientists, lab levels gate, research window | лаборатория, исследования |
| **War** | | |
| [army-and-soldiers](army-and-soldiers.md) | soldier tiers, groups, orders, recruitment, move-order flag | армия, солдаты, казарма, приказ |
| [combat-core](combat-core.md) | damage targets, towers, projectiles, NavRoute, wall breaching | бой, башня, стрелы, урон |
| [orc-raids-and-portals](orc-raids-and-portals.md) | raids, progression score, portals, orc kinds, win/lose | орки, набег, портал, победа, поражение |
| [orc-camps-and-main-base](orc-camps-and-main-base.md) | walled camps L1–3, spawners, totems, cages, treasury, main base, boss | лагерь, база орков, вождь, клетки, сокровищница |
| **World** | | |
| [maps-and-terrain](maps-and-terrain.md) | map pipeline, Maps 1–5, picker, decor, fords/bridges, orc spots, Vikings/dungeons plans | карта, рельеф, мост, брод, выбор карты |
| [fog-of-war](fog-of-war.md) | exploration, parchment/cloud looks | туман, разведка |
| [camera-and-input](camera-and-input.md) | RTS camera, touch gestures, input routing | камера, тач, жесты, управление |
| **Interface & meta** | | |
| [hud-and-ui](hud-and-ui.md) | HUD layout, skins/looks, event feed, panels, menus, settings | интерфейс, HUD, лента событий, меню |
| [save-load](save-load.md) | save format, autosave, restore order | сохранение, автосейв |
| [monetization](monetization.md) | ideas only: rewarded ads, paid HUD looks, promo codes | монетизация, реклама, покупки |
| **Pipelines** | | |
| [balance-and-localization](balance-and-localization.md) | Google Sheet → CSV → BalanceConfig; localization keys | баланс, таблица, локализация |
| [art-pipeline](art-pipeline.md) | Blender models, standard, levels, export, icons, UI sprites | Blender, модели, иконки |
| [performance-mobile](performance-mobile.md) | measured CPU/draw budget, triangle budget, device fleet | производительность, FPS, draw calls |
| [build-and-test](build-and-test.md) | SetupProject, batchmode loop, tests, APK/Windows builds, cheats | сборка, тесты, APK, читы |

`archive/` holds the long chronological notes these cards were distilled from (2026-10-21). Read an
archive file only when a card points at it for a specific detail.
