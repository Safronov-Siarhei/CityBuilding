---
name: project_pending_2026_10_31
description: "The user's playtest notes of 2026-10-31 evening, to do when asked (\"просто запомни, ничего не делай\") -- fertile land tap, dungeon entrance facing, storm edge look."
metadata:
  node_type: memory
  type: project
  originSessionId: 0f587829-fe18-4bb5-a5b7-742486c4675e
  modified: 2026-10-31T14:08:12.178Z
---

ALL THREE DONE 2026-10-31 (pushed 7a884a38) after the user said «делаем»; the storm was redone without asking which style. Kept for history:

1. **Fertile land tap.** Tapping fertile land lights up the patch's EDGES and posts «Обнаружена плодородная земля» in the event feed. The deposit tap works the same way: `WorldInputDispatcher.TryReportDeposit`.
2. **Dungeon entrances face the player's CAMERA**, so they read clearly. Today `DungeonEntrances.Build` turns the mouth to the start island.
   - Also seen in the user's screenshot: an entrance stood on SAND with a green circle round it.
   - The spot check uses `world.Beach`, and library islands carry no beach flags; the ground colour (UV) must be checked instead, as `FertileLand.KeepGrass` does.
3. **Storm edge.** The user called the whitecaps "some white diamonds that hardly look like a storm" (`StormEdge.Whitecap`).
   - Also visible in the screenshot: a straight grey-blue band along the calm/storm line. That is the surf ring lying under the dark tint.
   - The sea stacks read as scattered spikes.
   - The edge needs a real storm look: waves or foam crests, darker gradient, maybe clouds or mist. Ask the user which before redoing it.

See [[project_archipelago_map]], [[project_dungeon_entrance]].
