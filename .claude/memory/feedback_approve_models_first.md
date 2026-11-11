---
name: feedback-approve-models-first
description: "New models (monsters, buildings, props) go to the user as renders for approval BEFORE they are wired into the game, even when the user says \"потом всё вместе посмотрим\"."
metadata:
  node_type: memory
  type: feedback
  originSessionId: 65820551-ddcb-4e52-a0d1-9eafc8080c74
  modified: 2026-11-04T14:13:56.796Z
---

Before putting any new model into the game, send the user renders and wait for approval.

**Why:** 2026-11-04 the user asked «почему ты сначала не утвердил модели монстров у меня?». I had read «добавим ресурсы и монстров, а потом всё вместе посмотрим» as permission to skip the approval step for the 8 level-3 monsters (monsters3.py). For level 2, the models were approved first, and that was the right way.

**How to apply:** a request to "add monsters/buildings" means: model them, render previews, send them, and stop. Wire them into the game only after the user approves. "Посмотрим вместе" covers the mechanics, never the look of new models. See [[project-dungeon-entrance]].
