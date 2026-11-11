---
name: project-pending-2026-10-25
description: Four fixes the user queued on 2026-10-25 evening (menu field floating, info dialog overlap, barracks tiers per level, resource yield bubbles not hiding) — not started.
metadata:
  type: project
---
Queued by the user 2026-10-25 (they said "write OK, I'll start it later"); NOT started:
1. Main menu live background: the farm field (MenuBackground "ploughmen" vignette) hangs in the air — ground under it / its height.
2. Mode «!» info dialog must hide the window behind it (sandbox/invasion screen showed through: two «Песочница» title plates stacked) — dim/hide the screen or make the dialog opaque over it.
3. Barracks in a run: all soldier tiers open at level 1 → each Barracks level should open the next tier (L1 militia, L2 spearman, L3 archer, L4 man-at-arms?) — confirm mapping, numbers in the invasion tab; FocusHireBar shows only open tiers.
4. Tapping a wood/stone node shows yield bubbles («5» over nodes, NodeYieldBubble); opening a building card after that leaves them up — any other tap must hide them.
Also: the user rebuilds the project themselves after the Discord link commit (899559df). [[project-menu-ui]]


Update 2026-10-28: #4 (yield bubbles) DONE -- the badge goes at the next touch anywhere (TouchInputRouter.TouchedDown) or under a dialog. #1-#3 still open.
Update 2026-10-28 (later): #2 (info dialog overlap) DONE -- a window over the menu fades the screen behind. Left: #1 menu field floating, #3 barracks tier per level (mapping L1 militia, L2 spearman, L3 archer, L4 man-at-arms awaits the user).
Update 2026-10-28 (later): #3 barracks DONE (user said «делай казарму» = mapping accepted). Only #1 menu field floating left.
