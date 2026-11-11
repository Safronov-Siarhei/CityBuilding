---
name: reference-machine-paths
description: "Where the tools live on the user's current machine (since 2026-10-20): project on G:, Blender from Steam, Unity on D:."
metadata:
  node_type: memory
  type: reference
  originSessionId: 0f587829-fe18-4bb5-a5b7-742486c4675e
  modified: 2026-10-21T07:02:34.008Z
---

Since 2026-10-20 the user works on a new machine:
- Project: G:\Fork\CityBuilding (memory folder G--Fork-CityBuilding). Scripts and old notes still say D:/Fork/CityBuilding -- use G: instead.
- Blender 5.2.2: G:\SteamLibrary\steamapps\common\Blender\blender.exe (Steam install, not on PATH).
- Unity 6000.5.4f1: D:\unity\6000.5.4f1\Editor\Unity.exe (also 6000.1.4f1, 6000.3.5f2 there).
- The .blend files live on the user's Google Drive, synced across their devices: H:\Другие компьютеры\Ноутбук\blender projects\Buildings3D (Buildings-Size1.blend, Buildings-Size1-Models.blend, maps). The user (2026-10-21): "меняй сразу на гугл драйве" -- run Blender on the H: files directly, no local copies. Claude MAY now edit Buildings-Size1.blend placeholders there too (Gate1 is 1x1, FortifiedGate1 1x3 since 2026-10-21; previous versions kept as .blend1). Export: `blender --background "<H:>/Buildings-Size1-Models.blend" --python export_fbx.py -- G:/Fork/CityBuilding <Names>`.

See [[project_current_state]], [[feedback_batchmode_workflow]].
- Push works since 2026-10-20 10:00: the user re-signed git in via Git Credential Manager (the old cached login SAFRONOV277 got 403). If a push hangs, a "Connect to GitHub" GCM window is waiting behind other windows.
- Claude in Chrome works here after the user signed the extension in with the app's account (2026-10-20). Localization tab: data ends at row 266+ (append below the last; ctrl+Down from A2 finds it); read cells via #t-name-box and #t-formula-bar-input.
- Tools/research/run.ps1 -Steps setup,setup,editmode,playmode [-Filter X] [-Graphics] finds Unity itself; warm runs: setup ~30-45 s, EditMode ~10 s, PlayMode ~4 min.
- PlayMode flakes (2026-10-20): Map1-based tests fail one at a time in full runs (a different one each run: StorageAndUpgradeTests.Photograph_TheBuildingLevels, AuthoredModelTests.Photograph_TheAuthoredBuilding, PrisonerCageTests.AnOrcWithNoSoldierToFightKillsAPrisoner) and pass alone -- order-dependent, not caused by the map work.
- Since 2026-11-11 a copy of that Blender folder is in the repo at `Blender/` (Tools/blend_sync.py keeps it in step; .blend1 backups skipped).
