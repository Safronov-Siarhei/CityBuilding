---
name: feedback-auto-push
description: Push to origin immediately after every commit in the CityBuilding project
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 04bdd1d4-f9be-4484-b721-4f059c90b34c
  modified: 2026-08-07T06:06:41.243Z
---

After every git commit in this project (D:\Fork\CityBuilding), immediately run `git push origin master` — don't wait to be asked each time.

**Why:** User explicitly asked "в конце каждого выполнения сразу пуш в Fork изменения" (at the end of each execution, push immediately to Fork). The remote `origin` is already configured (`https://github.com/Safronov-Siarhei/CityBuilding.git`) — "Fork" in their phrasing refers to their GitHub repo/account (Safronov-Siarhei), matching the local folder name `D:\Fork\CityBuilding`, not a literal git remote named "Fork".

**How to apply:** This is a standing exception to the general "always confirm before pushing" rule — the user gave durable, scoped authorization for this specific project. Still only push to `origin master` (the existing remote/branch); don't add new remotes, force-push, or push to other branches without asking. If a push ever fails (e.g. diverged history, auth issue), surface it rather than force-pushing silently.

**Trap (2026-11-12):** when `git stash pop` after the pull hits a conflict (a generated prefab the other device also changed), it leaves every cleanly merged churn file STAGED; the next `git add <file> && git commit` then swept 200 generated files in. After such a pop: `git checkout HEAD -- <conflicted>`, then `git reset -q` (unstage all), then `git stash drop`. Check `git show --stat HEAD` before pushing.
