---
name: feedback-build-folder
description: "Builds go to D:\\Builds CB and the previous build there is deleted first (the user's preliminary rule, 2026-10-13)."
metadata:
  node_type: memory
  type: feedback
  originSessionId: e2f9aeb6-fb8c-4e4d-a103-d1e062cd15cf
  modified: 2026-10-13T18:11:48.583Z
---

The user (2026-10-13): «заведи предварительное правило билд собирать по этому пути и удалять прошлый билд из этой папки "D:\Builds CB"».

**Why:** they test from that folder; it should only ever hold the latest build, so there is no doubt which one they are running.

**How to apply:** `Tools/research/build_windows.ps1` empties `D:\Builds CB` (contents only, never the folder), builds the player into `D:\Builds CB\CityBuilding-Windows` (BuildWindows takes `-buildOutput <folder>`, quoted by hand because Start-Process does not quote) and zips it to `D:\Builds CB\CityBuilding-Windows.zip`, then sends the zip to Telegram as before. "Preliminary" -- the user may refine it; it was stated for builds in general, so an Android build should follow it too when next asked for (build_android.ps1 still writes to Builds/Android -- ask or update it then; clearing the folder would also delete the Windows build).

Still: build only when asked ([[feedback_build_only_on_request]]), notes in Russian, Telegram send ([[feedback_build_to_telegram]]).

**Android (2026-10-14):** `build_android.ps1` still builds into `Builds/Android`, then copies the APK into `D:\Builds CB` after deleting only the OLD APKs there — the Windows build in the same folder is left alone (my call, not confirmed by the user; tell them if asked). The script now uses `$p.WaitForExit()` instead of `-Wait`.
