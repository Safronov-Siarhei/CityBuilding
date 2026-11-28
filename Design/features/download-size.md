# Small first download (Play Asset Delivery)

**Status (2026-11-28):** STEP 2 BUILT (5 rounds the same day). AAB 1636 (14:22): install **~92 MB** (base 34.3 +
UnityDataAssetPack 58.1; was 213); two fast-follow packs: «FirstContent» 4.5 MB (the start map's level-1 dungeon,
asked for first) and «LaterContent» 118.8 MB. NOT checked on a phone (needs an install from Play internal testing);
EditMode tests NOT run after rounds 3-5 (the user: «подожди с тестами»). MUST before release (the user, 2026-11-25:
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

## Step 2 -- how the content loads today (checked 2026-11-28, AAB 1630, raw MB)
Five scenes (Splash, MainMenu, CityBuilder 7.4 MB, CampaignOpening, CampaignMap); the heavy content hangs off
ScriptableObjects under Resources, so ALL of it is in the one data pack:
- `Resources/MeshMaps/*` (MeshMapCatalog, Resources.LoadAll): Dungeon1-3 x A-D + Decor, Inv1-2, Map3-5, Mission2-3,
  Isl-*Decor. Models: Dungeon2* 56.7, Dungeon3* 50.8, Dungeon1* 39.7, Map3/4/5 ~7.
- `Resources/ArchipelagoLibrary` (every island, IslandAsset): Models/Islands 45.8.
- Campaign sets referenced straight from the CityBuilder scene (CityDefenceMode capitalSets / openingSets:
  M2CapitalSet, M2ThroneSet, M3RoadSet): Models/Cinematic 32.1, Campaign/Cast 16.8, Campaign/M3Opening 4.5.
- Stays in the install whatever we do: Buildings 43.6, UI ~60, Orcs/Soldiers/Citizens, Inv1-2.
Estimate: moving dungeons (but one level-1 variant), the islands (but the start set), campaign 2-3, Map3-5 and the
later music out = ~240 of 421 MB raw -> install about 100-115 MB (from 237). Exact after the build.

## Step 2 -- plan (proposed 2026-11-28; A, C, D done for dungeons + islands, B partly, E: size only)
A. Packages com.unity.addressables + com.unity.addressables.android (Addressable groups -> Play asset packs);
   SetupProject generates the groups: default = install-time, «Later» = fast-follow; build_android builds the
   Addressables content before the player.
B. Move the fast-follow content out of Resources into the «Later» group: dungeon map definitions (but the one
   level-1 variant), island assets (but the start set), Map3-5, Mission2-3 maps; the campaign sets become
   AssetReferences instead of scene references.
C. Runtime `ContentPacks`: starts the fast-follow download at every launch until it is complete (quietly), exposes
   Ready + progress. MeshMapCatalog / ArchipelagoLibrary look up async; the archipelago uses only the start set and
   the first game's fixed layout until Ready; a dungeon picks among the variants present; campaign level 2+ waits on
   «Загружаем… N %» / «Нужен интернет».
D. Editor + tests: Addressables in the editor read the AssetDatabase, so the tests keep working.
E. Build + phone: fast-follow only works for an install from Google Play (internal testing) or bundletool
   --local-testing; measure the install, check the background download, offline start.

## Step 2 -- built (2026-11-28, 5e27f253f)
- Packages Addressables 2.9.1 + Addressables for Android 1.1.1. `SetupProjectContent.Apply` (after CleanupTemplateAssets)
  makes the PAD init object + data builder, BuildWithPlayer, and the group `LaterContent` (FastFollow, pack separately):
  every MeshMapDefinition in `Assets/_Project/Content/LaterMaps` (label `later_map`) + `ArchipelagoLibraryLater`.
- SetupProjectMaps writes dungeons but `Dungeon1` to LaterMaps; decor to `Content/MapDecor` (out of Resources); the
  island library split: install = tutorial islands + the islands of the START map (`archipelago_start_seed` 2028, 16
  islands), the other 45 in LaterMaps/ArchipelagoLibraryLater. The cells-only preview library stays whole in Resources,
  so layout (and the map code) never depends on the pack; island meshes are found by name (`ArchipelagoLibrary.Find`).
  Setup logs an error if the start seed's level-1 dungeon is not Dungeon1.
- Runtime `Core/ContentPacks.cs`: at every launch asks Play for the pack, retries every 30 s and on resume; `Ready`,
  `Progress`, `Stalled`, `StatusText`. Not Android / editor = ready.
- Before Ready: a new sandbox game starts on the start map (`ArchipelagoMapId.NewRandom`); a typed code, a save
  (LoadGamePanel) or a dungeon not on the phone goes through `LoadingScreen.IntoGameWhenContent`: «Загружаем… N %»,
  offline «Нужен интернет, чтобы загрузить» and back to the menu after 6 s stalled.
- Measured AAB: base 34.3 + UnityDataAssetPack 88.1 = install ~122 MB; LaterContent 94.8 MB (fast-follow in the manifest).
- Tests: EditMode 460/466; the 6 failures are the sheet's balance numbers (Road upgrade, HealerHouse/TownSquare radius,
  Spearman metal, building numbers) and OrcBaseLayout's Brute guard on a rock -- none touches this work.

- Campaign 2-3 (2026-11-28, the user: «Кампанию в докачку»): the CAUSE was the opening village (main menu + the first
  cutscene) taking EVERY Models/Cinematic fbx as set pieces and every Cast figure into its cast -- now it skips
  `CampaignFilmKit.IsLater` names (Cap*, M2*, M3*, L3*). Missions 2-3's figures, clips, props and sets moved from
  CityDefenceMode's serialized fields into `Campaign/CampaignFilmKit` (Content/LaterMaps/CampaignFilmKit.asset, address
  `CampaignFilmKit`), read in CityDefenceMode.Awake (UseFilmKit). Mission2/Mission3 maps -> LaterMaps. Begin/BeginEscort
  wait for the pack (IntoGameWhenContent). Music stays in the install: the first cutscene plays the same tracks.
  Measured AAB 1635: install 122 -> 110 MB, pack 94.8 -> 109.5 MB (~2 MB shared pieces counted in both).

- Round 3 (2026-11-28, the user's idea): levels 3-5 of the buildings only the sandbox raises that high go to the
  pack: `Buildings/BuildingLevelKit` (Content/LaterMaps/BuildingLevelKit.asset, address `BuildingLevelKit`, 135
  models = 45 buildings x 3); the prefab keeps the level's HEIGHT and no model, BuildingLevelAppearance.FillFromKit
  makes it (fitted transform from setup, season-dressed) when the level is shown; until the pack is here the level
  shows the model below. Kept in the install: the Invasion tab's 12 buildings (read from invasion.csv by
  SetupProjectBuildings.KeepsHighLevels); the tutorial raises nothing past 2; campaign 1 uses raw fbx. The bot's
  village is the same buildings, nothing kept for it (the user: unseen in the first minutes). Orc camps stay (~1.5 MB,
  not worth it). Map3-5 (no player picks them) -> `Content/EditorMaps` (MeshMapCatalog.EditorOnlyFolder, editor/tests
  only, never built). Missions 2-3's scripts + takes (json) -> CampaignFilmKit. Sandbox music: NOT moved (the user:
  leave it for now; only 2 tracks are the sandbox's, ~6 MB). Measured: install 110 -> 100 MB.
  Trap: a level made from the kit appears AFTER placement -- anything caching a building's renderers at spawn misses
  it (SeasonStyle is re-applied in FillFromKit).

- Round 4-5 (2026-11-28): NO dungeon in the install (the user: «вынести подземелье 1 в докачку и поставить ей первый
  приоритет»): Dungeon1 -> `Content/FirstMaps` (MeshMapCatalog.FirstFolder, label `first_map`), its own Addressables
  group / Play pack `FirstContent` (ContentPacks.FirstPackName); ContentPacks asks for it FIRST, the big one after it
  lands (one at a time); `FirstReady` / `FirstProgress` / `FirstStatusText`; DungeonTravel waits for the first pack only
  (`IntoGameWhenContent(..., firstPackOnly)`); MeshMapCatalog loads the first pack's maps alone when only it is here.
  Level 3-5 PICTURES of the same 45 buildings -> `UI/LevelIconKit` (135; BuildingInfoCard, Laboratory, RunBuildingCard,
  EventIconLibrary look there on a miss, else the level below). Bot town: `BuildingLevelAppearance.ModelMade` -> the
  bot's palette for a model made later. Measured: 100 -> 92.4 MB. Next if needed: ASTC 8x8 big pictures (~6-8 MB),
  stronger mesh compression (~4-6 MB), sandbox music (~6 MB, the user: not now).

## Adding content (the user's rules, 2026-11-28)
- The store install (base + UnityDataAssetPack) stays UNDER 99 MB in every update; `build_android.ps1 -Store` prints
  it per pack and shouts over the ceiling.
- Every new asset is decided when it is added: install (the tutorial, Invasion, campaign cutscene 1 + level 1, the
  start archipelago + its islands, the menu and its music) or the download pack (everything else). Where it goes:
  - a map / dungeon definition -> `Content/LaterMaps` (or `FirstMaps` for what the first minutes may need);
  - a building's levels 3-5 -> automatic (BuildingLevelKit + LevelIconKit) unless the building is in invasion.csv;
  - a campaign film piece of mission 2+ -> CampaignFilmKit, file name Cap*/M2*/M3*/L3* (campaign.md trap);
  - anything else -> a ScriptableObject kit in `Content/LaterMaps` added to SetupProjectContent's list, loaded with
    Addressables when `ContentPacks.Ready`, with a fallback (or a wait on the loading screen) until then.
- Never reference a pack asset from a scene, a prefab, Resources or an install-time ScriptableObject: Unity then copies
  it into the install (that is how the menu village carried all of campaign 2-3).

### Traps
- The pack arrives only for an install FROM PLAY (internal testing) or bundletool --local-testing; a sideloaded AAB
  reports Unknown, treated as «here» (everything is then in the APK set anyway).
- Moving an asset out of Resources: SetupProject deletes the old copy -- an old Resources path left behind doubles the size.
- The sheet tabs are now thematic Russian names; the old English tabs are «СТАРЫЙ … (не править)».
- After a -Store build revert ProjectSettings.asset (git checkout).

### Open
- Later music (sandbox, campaign 2+) to the pack.
- Phone check from internal testing: install size, quiet download, the offline loading screen.

## Files
`Assets/_Project/Scripts/Editor/UiTextureImport.cs` (Android ASTC per folder); `Editor/SetupProjectContent.cs`, `Editor/SetupProjectMaps.cs`
(CreateArchipelagoLibrary, dungeon/decor folders); `Core/ContentPacks.cs`; `Maps/MeshMapCatalog.cs`; `Maps/Archipelago/ArchipelagoLibrary.cs`,
`ArchipelagoGenerator.cs`, `ArchipelagoMapId.cs`; `UI/LoadingScreen.cs` (IntoGameWhenContent); `Dungeon/DungeonTravel.cs`;
`UI/SandboxScreenController.cs`, `UI/LoadGamePanelController.cs`. Build: `Tools/research/build_android.ps1 -Store`; size report in RunLogs/android_build.log.
