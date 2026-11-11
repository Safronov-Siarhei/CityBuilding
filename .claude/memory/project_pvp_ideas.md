---
name: project-pvp-ideas
description: "PvP for CityBuilding (2026-10-20): real-time duel judged too costly for now; the user LIKES option A, asynchronous raids on other players' town snapshots (Clash of Clans style), but wants to think more. Nothing decided or built."
metadata:
  type: project
---

**Asked 2026-10-20:** how realistic is a duel — two players in one lobby, goal: destroy the other's main building.

**My assessment (given to the user):** real-time duel = months, near-rewrite of the core. 27 single-instance managers (ResourceManager, CitizenManager, ArmyManager, ResearchManager, FogOfWarManager, GameOverManager, GameCalendar...) assume one player; lockstep sync is impractical (Unity physics, pathfinding, Random in 13 files); host-authoritative means heavy state traffic for hundreds of moving units on mobile; plus lobby/relay services (Unity Lobby+Relay or Photon, paid at scale), reconnects, anti-cheat, a symmetric duel map, army-vs-player-buildings combat, per-player fog, PvP balance, 25+ min sessions with no pause, and an empty matchmaker without an audience. Recommended NOT before launch/audience.

Cheaper options offered: **A** async raids on town snapshots; **B** race — both play their own copy of the same seeded map, first to destroy the orc main base wins, only progress/score synced; **C** leaderboards / weekly challenges.

**USER: «запиши вариант А, он прикольный, но надо ещё подумать».** Option A as pitched: a player's town is uploaded to a server as a snapshot; another player attacks that snapshot with their army while the defence (towers, walls) is run by AI; the owner later gets a report "you were raided, here is the result". No real-time netcode, no waiting for an opponent; strong retention loop (build defences, take revenge). Fits what exists: army-vs-fortified-base combat already works against orc camps.

Open questions to raise when it comes back: what a raid steals/costs (resources? trophies/rating?), protection after being raided (shield timer), matchmaking by town strength, which buildings count as defence, how the snapshot is stored (layout + levels + garrison), server choice (Unity Cloud Save/Cloud Code, PlayFab, Firebase, Nakama), cheating (client-side results need server validation), offline revenge flow, and how it coexists with the orc campaign (separate mode vs. the same town).

**How to apply:** do not build anything until the user decides; when PvP or retention comes up, start from here. Related: [[project_monetization_ideas]] (retention ideas list), [[project_orc_faction]] (base-assault combat to reuse).
