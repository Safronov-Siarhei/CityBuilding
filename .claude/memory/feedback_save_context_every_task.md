---
name: feedback-save-context-every-task
description: "After every finished task (committed/pushed or reported), update project memory as if the chat closes right now — the user starts new chats often to save tokens and should never have to say \"сохрани контекст\"."
metadata:
  node_type: memory
  type: feedback
  originSessionId: fa46be31-b22f-47d3-8959-b7b091dbe342
  modified: 2026-10-21T16:17:57.833Z
---

At the end of every completed piece of work — a commit+push, a finished batch, a reported result — update the memory files right away, as if the user will close this chat and open a new one next: current state (what exists, what is committed, what is NOT done/exported/verified), what waits on the user, the traps found, how to pick up. Since 2026-10-21 the "topic note" is the FEATURE CARD in `Design/features/` ([[feedback-feature-cards]]): update the touched cards and commit them with the work; keep [[project-current-state]] to "latest + waiting on the user"; prune what went stale.

**Why:** the user switches to a new chat for each new task on purpose — a long chat re-sends its whole context on every request, so it costs more tokens per task (they corrected me when I said chat length did not matter; compaction only prevents overflow, it does not save tokens). Until 2026-10-15 they ended every chat with «сохрани контекст»; they asked for this to happen without being asked.

**How to apply:** treat the memory update as part of wrapping up, before the closing message — not something to wait for. Write it so a cold session can continue from MEMORY.md alone. If the user still says «сохрани контекст», do a final consolidation pass (stale facts, missing traps), but there should be little left to add. Do not suggest staying in one long chat to "save effort".
