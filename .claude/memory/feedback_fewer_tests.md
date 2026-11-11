---
name: feedback-fewer-tests
description: "STRICT since 2026-10-29: the user says test runs still eat the day (only 2 big tasks/day). One related-test run at the END of a task; no photo tests for UI tweaks; no re-runs after small fixes. (first raised 2026-10-21) — write new tests only for bug fixes and gameplay rules, run only related tests, full suite only for shared code or every few tasks."
metadata:
  node_type: memory
  type: feedback
  originSessionId: 0f587829-fe18-4bb5-a5b7-742486c4675e
  modified: 2026-10-21T06:02:21.776Z
---

The user (2026-10-21): "можешь уменьшить количество тестов на каждую задачу? на мой взгляд это нецелесообразное большое количество". Every small change was costing ~10 minutes: setup twice, the full EditMode+PlayMode suite (~680 tests), a new test, and a deliberate break-and-rerun.

Proposed rule (proposed, waiting for their yes; follow it in the meantime):
- New tests only for bug fixes and gameplay rules (economy, gathering, raids). UI and look tweaks (sizes, names, labels) get a screenshot, not a test.
- Run only the tests related to the task (`-Filter`), 1–2 minutes.
- Full suite only when shared code changed (citizens, resources, SetupProject) or once every few tasks before a push.
- Break-to-verify ([[feedback_verify_green_by_breaking]]) only for bug-fix regression tests.
- `setup` twice only when a CSV changed. A C#-only change needs one setup.

**Why:** time per task, and the user's tokens/attention.
**How to apply:** default to the light path; say in one line which tests ran.

**2026-10-29, the user again (stricter):** «ты всё ещё делаешь слишком много тестов... гоняешь бесполезные тесты и тратишь токены... в день получается сделать только две обширные задачи». What went wrong: a test/photo run after every small tweak (plaque width, crop, a typo), several setup+playmode cycles per feature. NOW:
- Write the whole task, then ONE run of the related tests at the end (setup once; twice only for CSV changes). Never re-run after a size/text/colour tweak -- just rebuild once and commit.
- No new photo/screenshot tests for UI; the user checks UI in the game. Don't look at screenshots to iterate on layout.
- New tests only for gameplay rules and bugs; keep them few.
- Full suite only when shared code changed, in the background while working on.

**2026-11-04, the user (FINAL, now in CLAUDE.md):** «на мелкие правки не вижу смысла проводить тесты ... после общего числа внушительных правок прогонять общий тест ... собирать проект тоже только после достаточного числа правок, иначе проверок будет больше чем правок». So: small edit = no build, no test; build after a batch of edits / at a task's end; ONE general test run after a substantial body of changes (feature or several tasks), in the background. The rule text lives in CLAUDE.md -- follow that.

**2026-11-04 later:** «зачем тебе делать отдельную сборку при правках в таблице» -- a CSV change rides along with the next code build (one setup); the double setup is ONLY for Editor-code changes (the old "twice for CSV" was wrong).
