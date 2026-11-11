---
name: feedback-everything-in-repo
description: "The user (2026-11-11): ALL project knowledge must live in the repo (fork) so work survives a deleted Claude folder or a switch to another AI -- memory notes synced to .claude/memory with Tools/memory_sync.py, rules in Design/WORKING_RULES.md, AGENTS.md for other AIs."
metadata:
  type: feedback
---

The user: «нужно чтобы абсолютно вся информация была доступна на форке» (in case the Claude folder is deleted or unreachable, or another AI continues the work).

**Why:** the local memory folder had 81 notes while the repo held a stale 21-note snapshot from 2026-10-21; one note existed only in the repo.

**How to apply:**
- After writing or changing any memory note: `python Tools/memory_sync.py` (two-way, newer wins) and commit `.claude/memory/` with the task. Careful: git checkouts give repo files fresh mtimes -- when in doubt use `--to-repo` (local is authoritative while working here).
- A new standing rule from the user also goes into `Design/WORKING_RULES.md` (and CLAUDE.md if short).
- Parked ideas get a row in `Design/features/ideas.md`.
- Never commit secrets: the repo is public (Telegram token stays in the env var).
- Not in git by decision pending: the .blend files (~227 MB on the user's Google Drive) -- see [[reference_machine_paths]].
