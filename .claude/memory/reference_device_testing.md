---
name: reference-device-testing
description: "How Claude drives the user's phone over adb (Pixel 6 Pro) — install, taps, screenshots, FPS from SurfaceFlinger timestats — and what does NOT work (F9 cheat, minimap jump, plain taps on the placement ghost)."
metadata:
  node_type: memory
  type: reference
  originSessionId: 979576c4-7a0c-49a0-b236-1e57adabdab9
  modified: 2026-10-16T17:56:37.236Z
---

The user enables USB debugging and lets Claude play the build on their phone (first done 2026-10-16).

- **adb**: `C:/Program Files/Unity/Hub/Editor/6000.5.4f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe`. Device seen: Pixel 6 Pro (`raven`), screen 3120x1440 landscape. Package `com.depigogames.citybuilding`; launch with `monkey -p <pkg> -c android.intent.category.LAUNCHER 1`; install `install -r "D:/Builds CB/CityBuilding.apk"`.
- **Screenshots**: `exec-out screencap -p`, scaled to 1/3 (1040x480) before reading — keeps image tokens low. A scratchpad helper `d.sh <name> tap|swipe|key|wait …` took coordinates in that 1/3 frame (not in the repo; rewrite if needed).
- **Taps**: a plain `input tap` does NOT confirm building placement (the touch schema wants a real press); use `input swipe x y x y 150` (a held tap). Panning = `swipe` with ~600 ms. Tapping the minimap does not move the camera. No pinch zoom over adb.
- **Cheats**: ResourceCheat/OrcSpawnCheat/ResearchCheat are Inspector-only and F9 needs a keyboard (`input keyevent 139` does nothing). So since 2026-10-16 (`4ec0f233`) the build carries **`Cheats/DebugPanel`** (IMGUI, button «Отладка NN» top-right at y≈150·scale): ∞ resources, raids pause, speed x1/x3/x6, FPS, per camp ◀▶ / «К лагерю» / «Ур. 1-3» (`OrcCamp.RebuildAt`, can go down) / squad of 5 of any tier outside the gate ordered at the totem then the portal. Blocks world input while open (ModalGate). Ships in release — remove or gate before a public release.
- **A Unity Android build restarts the adb daemon and the phone can drop off** (2026-10-16: after the build, `adb devices` empty and no USB PnP device at all — the user must replug / re-allow debugging).
- **FPS**: `dumpsys SurfaceFlinger --latency` returns nothing on this Android; use `dumpsys SurfaceFlinger --timestats -clear -enable`, wait N s, `--timestats -dump`, read the block for `…UnityPlayerGameActivity](BLAST)`: `totalFrames` and the `present2present` histogram. Measured 2026-10-16: town and a level-1 orc camp → 59–60 fps, 0 frames over 20 ms.
- Release build: `logcat -s Unity` is nearly empty (logs stripped), so no exceptions show there.

Related: [[feedback_build_to_telegram]], [[project_mobile_device_fleet]], [[project_orc_faction]].

**Wireless adb (2026-11-18):** a USB cable to this PC knocks out the user's Bluetooth dongle (mouse/keyboard die), so the Pixel 6 Pro is driven over Wi-Fi debugging. This PC (DESKTOP-KO1JVM5) is already paired; only `adb connect <ip>:<port>` is needed, the port from «Адреса устройств и порт» on the phone's Отладка по Wi-Fi screen (it changes each time the toggle is switched; 2026-11-18: 192.168.100.224:36955). adb = Unity's platform-tools (D:/unity/6000.5.4f1-x86_64/.../platform-tools/adb.exe). Re-pairing an already paired PC gives «protocol fault» -- not an error to chase.


**2026-11-18: three devices over Wi-Fi adb** (all paired to this PC): Pixel 6 Pro 192.168.100.224 (port changes with the toggle), Redmi Pad 192.168.100.219, Samsung A71 192.168.100.10. With several connected, set ANDROID_SERIAL. **Before EVERY tap check `dumpsys window | grep mCurrentFocus` shows citybuilding** -- the user uses the phones between my steps; blind taps twice landed in Steam Guard / a chess app. A reinstall over an APK built on the old PC fails (signature): uninstall only with the user's yes (loses saves). Profiling: dev APK (`build_android.ps1 -Development`) logs PERF lines; Core/PerfProbe toggles parts via a pushed perf.txt (see performance-mobile card).
