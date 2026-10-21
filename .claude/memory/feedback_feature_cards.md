---
name: feedback-feature-cards
description: "The user had the project split into feature cards (2026-10-21) so a discussion reads only the relevant feature, not the whole project; read the index then one card, and update cards after every task."
metadata:
  node_type: memory
  type: feedback
  originSessionId: 00ec56af-41af-4e20-aa76-c5f36bc84d70
  modified: 2026-10-21T16:17:54.645Z
---

The project is split into feature cards in the repo: `Design/features/README.md` is the index (card,
what it covers, Russian keywords), one `.md` per feature, ~24 of them. Asked by the user on
2026-10-21: «разделим проект на фичи, чтобы при каждом обсуждении не приходилось читать весь проект».

**Why:** the user opens a new chat per task to save tokens; reading the old 58 KB current-state note
plus code every time defeated that. Cards are in git, so they also survive a move to another device.

**How to apply:**
- At the start of a task: read the index, then only the matching card(s), then only the code files
  the card lists. Don't open `archive/` unless a card points there.
- At the end of every task (part of [[feedback-save-context-every-task]]): update the touched cards —
  status line with the date, new user decisions, new files/tests, traps, open items — commit them with
  the work. A new feature = a new card + an index row. Keep [[project-current-state]] short: only
  "latest + waiting on the user", details go to cards.
- Cards are written in English (cheaper to read); Russian keywords in the index for matching the
  user's words.
