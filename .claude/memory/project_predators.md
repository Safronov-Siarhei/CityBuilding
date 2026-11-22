---
name: project-predators
description: "Sandbox predators (2026-11-22, in discussion) -- wolves/bears spawn at random spots on 3 cave islands, attack citizens and stores (stock lost in proportion to hp damage), retreat to a cave; nothing built"
metadata:
  node_type: memory
  type: project
  originSessionId: 65820551-ddcb-4e52-a0d1-9eafc8080c74
  modified: 2026-11-22T07:24:29.133Z
---

The user's idea 2026-11-22, decided so far:
- Predators spawn at RANDOM places on an island (no den to spawn from -- the threat must not come from one spot).
- At game start 3 islands are picked: the player's main island + 2 random (never bot/orc). Only they get a small
  animal CAVE, the sign that predators live there; the animals go back to the cave after a successful hunt.
- Store attack: stock lost in proportion to the hp damage dealt (half the hp = half the stock), lost for good (eaten),
  NOT carried and recoverable -- the bear must be a real threat. A bear that has dealt half a store's hp walks to the cave.
- Kill reward: some food always; hides only if a HUNTER killed it (wolf +2, bear +5). Hunters get real hp + damage:
  a bear can kill a hunter, a hunter has a chance to kill a bear.
- Bear and wolf need attack animations; models go as renders for approval first ([[feedback-approve-models-first]]).
All 15 answered -> the card; concepts approved; MODEL RENDERS sent 2026-11-22 (Tools/blender/predators.py, render_predators.py), waiting for approval. Related: [[project-hunting]].

**Why:** a real threat in the sandbox's peaceful time; ties into hunting (hides), cemetery/plague (bodies).
**How to apply:** do not re-ask the decided points; card Design/features/predators.md once agreed.
