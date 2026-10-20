---
name: feedback-build-to-telegram
description: "Every Android build the user asks for must reach their Telegram automatically — the build script does it, and what to check when it does not."
metadata: 
  node_type: memory
  type: feedback
  originSessionId: c0c63e70-df66-43d5-81bc-e112016a9859
  modified: 2026-08-19T16:14:04.482Z
---

The user asked (2026-08-19) that every newly built APK go to their Telegram automatically, so a build reaches the phone that will test it without anyone moving files.

**Why:** the whole value of a build is somebody installing it. A step that depends on remembering to attach a file is a step that eventually gets skipped, and then a fix sits untested for a day.

**Before every build, rewrite `Tools/research/build_notes.txt` in RUSSIAN** — what changed in this build and what the user should look at on the phone. The send script reads that file automatically: short notes ride as the file caption, longer ones follow as their own silent message (a caption is capped at 1024 chars). The user asked for this explicitly on 2026-08-19: a bare APK in a chat says nothing about what to test.

**How to apply:** it is already automatic — `Tools/research/build_android.ps1` calls `Tools/research/send_build_to_telegram.py` after every successful build. Do NOT rely on remembering to send it by hand; if the build ran, the send ran. Still attach the APK to the conversation as well, since the user may be at the desk.

- Credentials are the same env vars `telegram_bridge.py` uses: `TELEGRAM_BOT_TOKEN`, `TELEGRAM_ALLOWED_USER_ID` (both set at User scope). Never print a token into the transcript, and never ask the user to paste one — have them set the variable themselves.
- **Builds land in the "GameBuilds" topic of the "Depigo Games" supergroup**: chat `-1003453984361`, thread `175`, both hard-coded as defaults in the script (neither is a secret; a chat id is useless without the token) and overridable via `TELEGRAM_BUILD_CHAT_ID` / `TELEGRAM_BUILD_THREAD_ID`. **Posting into a forum topic needs `message_thread_id`** or the file silently lands in the group's General topic.
- **A bot cannot post into Saved Messages ("Избранное")** — that chat is the user's own and no bot can reach it. The topic above is the closest equivalent, which is why it exists.
- **The Bot API caps uploads at 50 MB.** The APK was 32,6 MB on 2026-08-19; the script refuses and says so rather than failing halfway up.
- The bot is **@ClaudeChatSafro_bot**. Its token was revoked once (2026-08-19) and reissued; verify with `getMe` before assuming a send failure is a bug in the script.
- **A running process keeps the token it started with.** After the user reset the variable, every call still failed 401 because the shell had inherited the old value. `send_build_to_telegram.read_token` therefore reads `HKCU\Environment` FIRST and falls back to the process env — remember this trap for any other env var the user changes mid-session.

**A killed build poisons the native build cache (2026-08-20).** After a build was stopped
mid-run, the next one failed with `CreateProcess failed: The system cannot find the file
specified` from ninja, while compiling the Android GameActivity C++ layer. The cause is NOT the
code and NOT a missing NDK: the CMake cache in `D:\Fork\CityBuilding\.utmp` (Unity's own short
temp path for the native build) had the compiler recorded as `CLANG_~1.EXE`, an 8.3 short name
that `clang++.exe` does not have on this machine -- check with `cmd /c dir /x <path>`, the short
-name column is empty. **The fix is `rm -rf .utmp` and rebuild**; CMake regenerates with full
paths and the run takes about a minute. Do not go looking for compile errors -- the wrapper's
error filter prints hundreds of clang command lines and buries the one line that matters, so
grep the log for `CreateProcess failed|FAILED:|error:` instead of reading the wrapper's output.

Related: [[telegram_bridge_tool]], [[project_performance_baseline]] (which documents the build script itself).

**Windows build (added 2026-10-13):** `Tools/research/build_windows.ps1` → `BuildWindows.Run`
(Mono, Win64) → `Builds/Windows/`, zipped to `Builds/CityBuilding-Windows.zip` (40,8 MB first time,
close to the 50 MB cap) and sent to the same topic with the same `build_notes.txt`. The platform
switch dirties `Assets/Settings/PC_RPAsset.asset` (shader prefilter flags) and
`ProjectSettings.asset` (a Standalone batching entry) — revert both, they are not real changes.
