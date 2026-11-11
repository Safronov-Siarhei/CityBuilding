---
name: project_bot_settlement
description: "The bot's castle (2026-11-03): the user's sketch of v1 (concentric, ~32x32 cells) and my proposed concept; waiting for the user's answers, nothing built."
metadata:
  node_type: memory
  type: project
  originSessionId: 0f587829-fe18-4bb5-a5b7-742486c4675e
  modified: 2026-11-03T07:37:49.100Z
---

On 2026-11-03 the user started designing the BOT. It lives on the archipelago's mandatory bot island ([[project_archipelago_map]]): «trade/war later».

**The user's sketch of the castle, v1** (image 135 in the chat), concentric and symmetric, about 32x32 cells:
- The centre:
  - the Town Hall at max level, roads in a cross at max level;
  - the inner fort about 10x10: 4 Fortified Towers, walls, 4 Barbicans, all at max level.
- A moat 2 cells wide all round, with 4 Drawbridges at max level (N, S, E, W).
- A ring of Squares (Площадь 2x2) used as streets, and 32 Manors in groups of 4 (2x2 blocks of 3x3).
- At the 4 corners: a 2x2 decor block (DecTree, DecBush, Flag, Well).
- The outer wall at lvl4, with towers lvl4 and a Barbican lvl4 in the middle of each side, aligned with the square streets.

**My proposal** (sent, not agreed):
- The layout is DATA: a blueprint grid with stages. The bot builds it up by a timer and follows the player's progress. It has no simulated economy.
- Its buildings are ordinary BuildingInstance with an owner = bot. Citizens are NOT simulated, only a few decorative walkers; the performance baseline says walking citizens are the whole CPU cost.
- Relations: peace means trade at its shore by ship; war means the player besieges it and loots the treasury and crystals. The orcs raid the bot too.
- Additions to the sketch: a barracks/garrison (archers on the lvl3+ walls), a market for trade, and a treasury as the war target.

Questions put to the user:
1. Friend, enemy, or both?
2. Is it built at the start, or does it grow?
3. Does it attack the player?
4. What does the player gain from it?
5. Only the archipelago, or other maps too?

## The user's answers (2026-11-03)
1. **Relations need a detailed concept of their own** (to be written).
   - First step: a new building, the **Diplomacy Hall (Зал дипломатии)**. Only once the player has built it can they have relations with the bot.
   - Three states: **Мир / Торговля / Война** (Peace / Trade / War).
   - Upgrading the Hall gives better odds of good trade deals and a smaller **contribution (контрибуция)** the player must pay the bot to end a war.
2. The bot starts **already developed**: the player should not think of attacking at once, and should see what to aim for. This replaces my "grows from small" proposal.
3. **At war the bot attacks.** It needs a **shipyard and barracks**.
   - Its base has a **garrison**: its KING walks the town, RANGED soldiers stand on the walls, MELEE soldiers walk the town.
4. The player gains **trade**, and at good relations the bot sometimes sends **GIFTS**.
5. The user asked what "other maps" meant. I explained: only the archipelago is in the picker, so the bot lives there.

Implications:
- The castle must touch the shore for the shipyard. The sketch has no shipyard or barracks yet.
- Still open: the relations meter (what raises or lowers it), how war starts, the trade mechanics, the gift frequency, the contribution formula.

## Round 2 (2026-11-03)
The user decided:
- Relations: a SCALE.
- Deals: made in a WINDOW.
- A war ends when the player pays a contribution OR razes the bot's Town Hall. With no Town Hall the bot restores nothing and its units do not respawn.
- The bot also has a Diplomacy Hall at max level.

I drew the scheme v2: `Design/bot_castle_v2.png` / `.py`. The card is `Design/features/bot.md`, where the full concept lives. READ THE CARD FIRST.

## Round 3 (2026-11-03)
The user decided:
- The Diplomacy Hall is 4x4.
- Trade, the contribution, the bot's attacks and the reward for razing its Town Hall all come LATER.
- The orcs DO attack the bot, massively but just for show. What they kill or destroy, the bot rebuilds after the raid is beaten off.

BUILT:
- the Diplomacy Hall, with models 1-5;
- the relations skeleton and its window (ed05568f);
- the BASE EDITOR artifact https://claude.ai/artifact/AuMAkdzSMwuHrQxof6i8Sg, file `Tools/base_editor/base_editor.html`, the user asked for it.

The next steps are in the card `Design/features/bot.md`, section «Next».

## Round 4 (2026-11-03): skeleton BUILT, not seen in game
- The castle stands on the bot island from the blueprint: the islands were regenerated with a pad.
- Built: the garrison (King, archers, patrols), the orc raids on the bot, the rebuild after a raid, a razed hall ending the war.
- Details are in the card `Design/features/bot.md`.
- Left (user: later): war attacks by the bot, trade, contribution, gifts, the razing reward.

## Round 5 (2026-11-03), commit 01ebbab4
- The bot's roofs are NEUTRAL STRAW (#D9BC74/#A6874A; turquoise first, the user swapped it the same day); its King wears a straw cape (BotKingPalette, DressKing). User: the player must see they are not theirs.
  - BotPalette.mat recolours all roof cells, the gold and the banners.
  - BotKingdom.Dress applies it.
  - The user has seen only the test photo.
- The base editor has an ORC side (the Королевство/Орки switch): orc camp buildings, guard posts, and the main base as its example. Nothing in the game reads orc blueprints yet. Artifact v3 is at the same URL.
- **WAR BUILT 2026-11-04 (3656ed550), not seen in game, bot test green.** User answers:
  - attack = landing + shelling together;
  - strength grows over time;
  - ransom in coins, or crystals when coins are short;
  - the garrison defends the castle.
  - The waves are in the user's sheet tab `botwaves` (wave/unit/count/level, gid 1680852949). Transports are counted automatically.
  - Details and traps are in card bot.md: no player Town Hall means no raid; BotSide and orcs do not fight each other; the manifest is not saved.
- **2026-11-05 (cf2b2cdf9), built, untested:**
  - Each bot building razed by the army lowers the ransom (5 coins each, never below 50).
  - Each new war starts its raids 2 waves further on (war 2 starts at wave 3).
  - botwaves has 20 waves.
  - Open question for later: widen the barracks' unit list for the player and the bot.
  - The sheet was synced on 2026-11-05 (economy rows 598-600, botwaves 40 rows); verify_sheet reports 0 differences.
- **Town templates (2026-11-05), user decisions:**
  - each seed gets a random template;
  - a living town with districts and levels, stronger towards the centre, razed stage by stage;
  - razing changes only the ransom, and the ransom falls BY LEVEL (still to build);
  - the island grows to about 55×55.
  - Template A «Кольцевой город» draft v1 was sent to the user (Design/bot_town_a.py/.json/.png, bd6957b77), awaiting review. Then B, C and D on the same pattern. See bot.md «Town templates».
  - Lab moved to the shore (user). B «Приморский», C «Город кварталов» and D «Цитадель и посад» drafted 2026-11-05 (714e9b23a, Design/bot_town_common.py + bot_town_b/c/d.py) and sent for review.
  - Next after approval:
    - bigger bot island;
    - template picked per seed;
    - anchor on the town centre (B's hall is off-centre);
    - ransom by level;
    - build.
- **Town templates BUILT 2026-11-05**, built clean with no tests (the user said: tests later, as one general run):
  - one template per seed;
  - anchored on the town centre;
  - garrison routes come from the template;
  - 6 bot islands regenerated with the new BOT_PAD;
  - the ransom falls per level.
  - Not seen in game. The bot test numbers may need adjusting in the general run.
2026-11-06: bot buildings leave ruins; bot rebuilds them (construction timer) only after every 3rd orc raid (bot_rebuild_every_raids, sheet synced). Camera smoother+slower (follow 10, fling cap 22). Built, not seen.
