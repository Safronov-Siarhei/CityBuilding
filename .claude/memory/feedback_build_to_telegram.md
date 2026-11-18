---
name: feedback-build-to-telegram
description: "SUPERSEDED 2026-11-18: builds are NOT sent to Telegram any more; the APK goes to D:\Builds CB and onto the phone over (Wi-Fi) adb."
metadata:
  type: feedback
---

The user (2026-11-18): «исключи отправку в телеграмм, она больше не нужна». The Telegram send was removed from
Tools/research/build_android.ps1 and build_windows.ps1 (send_build_to_telegram.py stays in the repo unused).

**Why:** the APK is 227 MB (Telegram's 50 MB limit made it fail every time) and the phone is now reached by adb over Wi-Fi.
**How to apply:** after a build the user asked for, install it with `adb install -r "D:/Builds CB/CityBuilding.apk"` (see [[reference-device-testing]]); never resurrect the Telegram step.
