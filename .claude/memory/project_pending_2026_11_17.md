---
name: project_pending_2026_11_17
description: "Playtest list 2026-11-17 (13 items: piggy timer, hall people in invasion/tutorial, invasion auto-staff/notices/King/music/defeat window, chopping, camera on load, lab hp bar, season palette) -- status per item"
metadata:
  node_type: memory
  type: project
  originSessionId: 65820551-ddcb-4e52-a0d1-9eafc8080c74
  modified: 2026-11-17T17:12:06.062Z
---

The user's playtest of 2026-11-17 (13 items). Status after the round (all code built in one commit, not seen by the user):
1. Piggy timer h:mm:ss -- DONE (PiggyWindow.RedrawTimer).
2. Invasion: the Town Hall changed the people count -- DONE: the hall's room is added to the cap in the SANDBOX only (HousePeople.TownRoom / HallRoomCounts).
3. Invasion: a placed building gets workers by itself -- DONE (ProductionBuilding.AutoStaffed: every workplace in a run, not the Lab).
4. Invasion: battle music stays from the first wave on -- DONE (MusicPlayer).
5. Invasion: notices hung low -- DONE: EventFeedController follows the lowest SHOWN left-column button (MenuButton/WorkforceButton/InventoryButton/ShopButton).
6. King ignores a retreat order in a fight -- DONE: a PLAYER's move order (ArmyOrderInput -> OrderMoveTo(dest, breakOff: true)) makes every member Disengage (Retreat to the spot, striking nobody); bots/campaign untouched.
7. Defeat window too plain -- CONCEPT Design/ui/InvasionDefeat_A.png (Tools/ui/invasion_defeat_concept.py), sent for approval; «Новый забег» button is new (the game had only «В главное меню»).
8. Chopping/mining animation in sandbox + tutorial -- DONE: a man sent BY HAND who stopped within reach (CitizenAgent.Update Gathering branch) never called Waddle.Work; test SandboxRulesTests.ASawmillsMan_SwingsHisAxe_AtHisTree.
9. Tutorial: more people because of the hall -- same fix as 2.
10. Tutorial: warlike music from the orcs' boarding -- DONE (TutorialDirector.RaidStarted -> MusicPlayer battle track).
11. Sandbox load: camera far from the hall -- DONE: waits for the hall + loading screen, jumps to the footprint centre twice (GameSaveController).
12. Lab hp bar was an oval (stretched knob) -- DONE: plain UISprite fill.
13. Season palette not on dungeons/rocks/some trees/decor -- DONE: SeasonStyle.DressAny/DressScene (every material slot: buildings palette, summer crowns, summer terrain), called after a map is laid (+1.5 s), in Rock/Ore spawners, DungeonEntrances, BuildingInstance; Buildings-<Season>.png now also recolour the «leaf» cell (seasons.py).

**Why:** the user's list; feedback rule «do every item».
**How to apply:** next session check what the user saw; the defeat window waits for approval. Related [[project_music]], [[project_lab_redesign]].
