---
name: feedback-install-ceiling
description: every update keeps the store install under 99 MB; every new asset is sorted into install vs download pack when added
metadata:
  node_type: memory
  type: feedback
  originSessionId: 65820551-ddcb-4e52-a0d1-9eafc8080c74
  modified: 2026-11-28T11:30:35.661Z
---

The store install (AAB base + UnityDataAssetPack) must stay under 99 MB in every future update; the download packs
(FirstContent, LaterContent) do not count. Every new asset is decided when it is added: needed at install (tutorial,
Invasion, campaign cutscene 1 + level 1, start archipelago, menu) or into the download pack.

**Why:** the user, 2026-11-28, after the install went 213 -> 92 MB: «чем дольше скачивается -- тем меньше хочется играть».

**How to apply:** `build_android.ps1 -Store` prints the install and shouts over 99; where each kind of asset goes:
Design/features/download-size.md «Adding content». Never reference a pack asset from a scene/prefab/Resources. See
[[project-release-google-play]].
