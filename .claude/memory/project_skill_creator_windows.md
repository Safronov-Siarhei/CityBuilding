---
name: project-skill-creator-windows
description: "The skill-creator's description-optimizer does not work on this Windows machine — three bugs, two patched locally in a cache that plugin updates will wipe. Read before promising a triggering benchmark."
metadata: 
  node_type: memory
  type: project
  originSessionId: ee46e154-dce7-4c1a-b3aa-d80c4fcbcdd0
  modified: 2026-09-01T09:51:11.927Z
---

Tried on 2026-09-01 to run `scripts/run_loop.py` (the trigger-description optimizer) against the
`balance-sheet` skill. **It does not produce a usable signal on this machine.** Do not offer the
user a triggering benchmark as if it were available, and do not report its numbers as a verdict on
a description.

Three separate Windows problems, in the order they surface:

1. **`subprocess` cannot see `claude.cmd`.** `run_eval.py` and `improve_description.py` both call a
   bare `"claude"` in an argv list; Windows `CreateProcess` does not apply `PATHEXT`, and the npm
   install is `claude.CMD`. Every query dies with `WinError 2`. Fixed with
   `shutil.which("claude") or "claude"` — a full path to the `.CMD` launches fine.
2. **The HTML report is written in cp1251** and dies on the first `✗` glyph. Fixed by passing
   `encoding="utf-8"` to the eight `write_text` calls across `run_loop.py`,
   `generate_report.py`, `improve_description.py` and `run_eval.py`.
3. **`WinError 10038` while reading the streamed output** — NOT fixed. After 1 and 2 the loop runs
   end to end and reports `rate=0/3` for **every** query, including ones that plainly should
   trigger. So `recall=0%` there means "nothing ever ran", not "the description is bad". The
   arithmetic it prints (`precision=100%`) is vacuous for the same reason.

**Both patches live in the plugin's own cache and a plugin update will erase them:**
`C:\Users\borei\AppData\Local\Packages\Claude_pzs8sxrjxfjjc\LocalCache\Roaming\Claude\local-agent-mode-sessions\skills-plugin\<id>\<id>\skills\skill-creator\scripts\`.

Note the path trap: `C:\Users\borei\AppData\Roaming\Claude\...` resolves for Git Bash tools but
**not for Python** — this is a packaged app with redirected storage, and the `AppData\Local\Packages\…\LocalCache\Roaming\…` form is the real one. A traceback from the tool
itself is the reliable way to learn the real path.

Consequence for [[project_current_state]]: the `balance-sheet` skill's description is hand-written
and unmeasured. That is fine — it is just not benchmarked, and should not be described as if it
were.
