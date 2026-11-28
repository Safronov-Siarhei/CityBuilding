# Small first download (Play Asset Delivery)

**Status (2026-11-28):** layout DECIDED with the user, nothing built. MUST before release (the user, 2026-11-25:
«чем дольше скачивается -- тем меньше хочется играть»).

## Today (AAB 1620)
Download 237 MB = base module 35 MB (engine + code) + one install-time pack 202 MB (`splitApplicationBinary`).
Content before packing 464 MB (build report, RunLogs/android_build.log, «Used Assets ... sorted by uncompressed size»):
dungeons 152 (12 variants: Relief/Props/Ground 3-6 MB each), UI 78 (CORRECTED 2026-11-28: textures are ALREADY
ASTC -- 825 files 85 MB at ~8 bpp = 4x4, 51 files 21 MB at 6x6+, only 0.7 MB uncompressed; the importer's
textureCompression:0 in many metas is not what Android gets),
campaign + cinematics 64 (M2CapitalSet 11 MB), sandbox maps 62, buildings 45 (levels 1-5: 5.6 / 7.4 / 9.4 / 10.3 /
10.4), other 27, loading screens 15, music 14. Mesh compression is already Medium on all FBX.

## The user's rules (2026-11-28)
- The goal is a small INSTALL so the size does not scare the player; everything else downloads quietly right after.
- Every mode must start at once on the first launch:
  - Tutorial -- whole, in the install.
  - Invasion -- whole, in the install.
  - Campaign -- the first cutscene + level 1 in the install; the rest downloads while the player plays it.
  - Sandbox (archipelago is the only sandbox map) -- ONE fixed starting layout of the archipelago's islands + ONE
    variant of a level-1 dungeon (the start island's dungeon is reachable at once: only the King is needed).
- Building levels 3-5 STAY in the install (saves only ~15 MB of download, and Invasion / bot town / orc camps /
  campaign use high levels anyway).
- NO on-demand / event-triggered packs: everything not in the install is ONE fast-follow layer. Google starts it right
  after install; if it did not finish (no internet), the game resumes it at every launch, in the background.

## Layout
| Pack | Content | When |
|---|---|---|
| install-time | engine, code, UI, all buildings (all levels), units, orcs, sounds; tutorial; Invasion; campaign cutscene 1 + level 1; archipelago start layout + 1 level-1 dungeon variant | with the app |
| fast-follow | the rest of the island library, the other dungeons (all levels), the rest of the campaign, the other music | right after install / at launch |

## Behaviour while the fast-follow pack is missing
- Archipelago: a new map is built from the whole island library only when the pack is there; otherwise again from
  the start set (the player does not notice).
- Campaign: past level 1 without the pack -- «Загружаем… N %» before level 2; offline: «Нужен интернет, чтобы
  загрузить продолжение».

## Plan
1. **Step 1 -- compress what exists** (no architecture change). Textures are already ASTC 4x4: the only lever is 4x4
   -> 6x6 (~-45 MB raw), a QUALITY trade -- comparison sheets (source | 4x4 | 6x6, made with Unity's astcenc DLL
   through ctypes) sent to the user 2026-11-28; the user saw no difference. PSNR vs source (4x4 / 6x6 dB): loading
   map 53.6/44.8, sandbox frame 70.2/56.4, Freedom window 53.7/46.8, tribute scroll 60.3/54.3, Crystal Mine icon
   41.8/35.6 (>40 dB = not visible). Proposal: 6x6 for large windows/frames/loading screens, 4x4 kept for icons and
   small/text-heavy sprites. DONE 2026-11-28 (the user: «иконки зданий 4х4, остальное 6х6»): UiTextureImport v4 --
   Android ASTC 6x6 for every UI picture except UI/Icons + UI/Glyphs (4x4); 479 metas regenerated. MEASURED on AAB
   1630: textures 110.7 -> 68.0 MB raw; the data pack 192.8 -> 178.9 MB, the .aab 226.7 -> 212.8 MB = about -14 MB of
   download (~6 %). Meshes are now 69 % (291 MB raw): the size problem is the models, and step 2 (packs) is the lever. Dungeon reliefs: NOT simplified (the user
   doubts the quality; 11 of 12 variants are in the fast-follow pack anyway). Music: the tracks of the menu,
   tutorial, Invasion AND the campaign's first cutscene + level 1 are in the install (the user, 2026-11-28).
   Measure the download after it.
2. **Step 2 -- packs**: Addressables + Play Asset Delivery (com.unity.addressables.android); the fast-follow content
   moves out of Resources/scenes into Addressable groups; SetupProject generates them; mode loading becomes async;
   the start archipelago layout + dungeon variant fixed for the first game. Target install 70-100 MB (measure).

## Files
`Assets/_Project/Scripts/Editor/UiTextureImport.cs` (Android ASTC per folder). Build: `Tools/research/build_android.ps1 -Store`; size report in RunLogs/android_build.log.
