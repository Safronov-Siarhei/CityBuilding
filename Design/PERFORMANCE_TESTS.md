# Performance test log

Every performance measurement of the game, newest first, for whoever takes over the optimisation.
Each entry: what was measured, on what, how, the numbers, what was changed because of them (with the commit).
The rules and budgets that came out of these live in `Design/features/performance-mobile.md`; this file is the
raw record. **Add every new measurement here** (the user, 2026-11-21).

## How to measure on a device (the method used since 2026-11-20)

1. **Development APK**: `$env:UNITY_EXE='D:\unity\6000.5.4f1-x86_64\Editor\Unity.exe'; ./Tools/research/build_android.ps1 -Development`
   (~2-5 min). The APK is `Builds/Android/CityBuilding-dev.apk` (also copied to `D:\Builds CB`).
   **Never pipe the script into `Select-Object -First N`**: PowerShell stops it before the copy (two "fixed" builds
   were never installed that way, 2026-11-20). Install from `Builds/Android/` when in doubt.
2. **adb over Wi-Fi**: adb is `D:/unity/6000.5.4f1-x86_64/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe`.
   The device's port changes every time «Отладка по Wi-Fi» is toggled; take it from that screen's main page
   («IP-адрес и порт»), `adb connect ip:port`. Set `ANDROID_SERIAL`. On MIUI (Redmi) an install pops an
   «install via USB?» window on the device: someone must tap «Установить» within ~1 min.
3. **Numbers**: the dev build logs a `PERF` line to logcat every 3 s (`adb logcat -s Unity | grep PERF`): fps, cpu
   main/render, gpu (FrameTimingManager), draws/tris, GC, BehaviourUpdate (= all scripts' Update), LateBehaviourUpdate,
   Physics, what is on the map (orcs, soldiers, people). Frame pacing from SurfaceFlinger:
   `dumpsys SurfaceFlinger --timestats -clear -enable`, wait, `--timestats -dump`, read the game layer's
   `present2present` histogram (frames over 20/34/50 ms).
4. **Commands** (`Core/PerfProbe`): push a one-line text file to
   `/sdcard/Android/data/com.depigogames.citybuilding/files/perf.txt`; the game reads it every second when its text
   changes (push a different line each time; delete the file when done -- it is re-read on the next start).
   | line | does |
   |---|---|
   | `town 100 220` | 100 buildings round the castle, 220 people, staffed (`Core/PerfTown.Build`) |
   | `battle 120 120` | a field battle in front of the camera: 120 soldiers (2/3 men-at-arms, 1/3 archers) v 120 orcs, both x30 health; `battle 0 0` clears |
   | `capsule off` / `on` | citizens walk without / with their CharacterController |
   | `kinds` | every script kind with an Update, by count, into `perf_out.txt` |
   | `off <Type>` / `on <Type>` | switch every script (or collider) of that type off / back on -- read the saving in the PERF line |
   | `army 60` | 60 player soldiers (level-3 stats) at the hall (`PerfTown.Army`) |
   | `wave 25` | send Invasion wave 25 now (`InvasionWaves.SendWave`) |
   | `marks 8` | `Core/PerfMarks` timers inside the army's Update for 8 s, ms per frame into `perf_out.txt` |
   | `post/shadows/particles/skinned/figures/ui/sky off`, `shader <name> off`, `scale 0.5`, `msaa 1`, `census` | GPU-side switches and a renderer census (2026-11-18) |
5. **Editor**: `PerformanceProfileTests` (PlayMode) grows a town in stages and switches script kinds off in turn.
   **It understates phone costs badly** (the capsule cost showed 0 there and 25 fps on the tablet) -- trust the device.

## Devices

| device | chip / GPU | RAM | screen | note |
|---|---|---|---|---|
| Redmi Pad 10.6 (2022), 22081283G | Helio G99 (2x A76 2.2 GHz + 6x A55 2.0), Mali-G57 MC2 | 4 GB | 2000x1200, 60 Hz | Android 13; the weak GPU of the fleet, the reference for the limits below; quality tier Low (auto) |
| Pixel 6 Pro | Tensor G1, Mali-G78 MP20 | 12 GB | 3120x1440, 120 Hz | strong |
| Samsung A71 | SD 730, Adreno 618 | 6 GB | 2400x1080 | |
| Redmi 12 | Helio G88, Mali-G52 MC2 | 4 GB | | the floor, not measured yet |

---

## 2026-11-21 -- campaign levels (Redmi Pad)

**Mission 1 «Бегство»** (scene CampaignOpening, its own ground/darkness shaders, camera «Cutscene Camera»):

| part | fps | cpu / gpu ms | scripts ms | tris | frames > 34 ms |
|---|---|---|---|---|---|
| opening film, village by day | 31-32 | 14-15 / 29-30 | 1.8-2.0 | 260-330k | 0.5 % |
| film, portal + burning village | 28-30 | 15-23 / 33-36 | 3-8 | 380-690k | 4-17 % |
| play, Ардан in the burning village | **27-28** | 16-20 / **35-36** | 2-2.7 | 170k | 17 % |

**GPU-bound at only 170k triangles** -- the cost is per PIXEL, not per object. Switches at the play start (`bu.sh`
means, gpu ms):

| switch | fps | gpu |
|---|---|---|
| as shipped | 28 | 35.5 |
| `shader CineGround off` (the film's ground: grass, cobbles, dirt, field quads) | **40** | **24.4** (-11) |
| `post off` | 35 | 28.6 (-6) |
| `dark off` (DungeonDark, the full-screen darkness quad) | 34 | 29.3 (-6) |
| `scale 0.5` | 45 | 19.3 |
| `shadows off`, `particles off`, `sky off`, `shader BuildingDetail off` | 28 | ~35 (nothing) |

Why: `CineGround.shader` -- the grass pass alone samples the noise texture 4 times + hashes per pixel, with soft
main-light shadows and per-pixel fog, over a full-screen 800 m quad; the road/field ribbons lie on top of the grass
(overdraw: census 2.6 screens). `DungeonDark` does 9 mask taps + 2 noise + a depth read on every pixel, alpha-blended
over the whole screen even where the darkness is open. Not fixed yet (2026-11-21) -- candidates: draw the ribbons
before the grass (early-Z kills the grass under them), bake the grass's 4 noise layers into one texture, hard
shadows on the ground; the darkness at half resolution or with fewer taps; post off on Low.
(The sandbox looks heavier but uses the cheap island ground -- that is why it runs faster with more on screen.)

**Main menu** (village behind the modes carousel): 33-35 fps on the on-screen counter (not measured further).

**Mission 2 «Город»** (scene CityBuilder, map Mission2; reached with the campaign map's dev cheat «пройти все»;
Ардан left standing, the waves run on their own), 40 s samples:

| part | fps | cpu / gpu ms | scripts ms | on the map | frames > 34 ms |
|---|---|---|---|---|---|
| opening scene | 47-49 | 12.5 / 18 | 4.9 | 42 soldiers, 542k tris | 0.2 % |
| waves 1-4 (minutes 1-5) | 37-38 | 16-17 / 25-26 | 6-9.3 | 3-20 orcs, 30-42 soldiers, ~510k tris | 2-6 % |
| wave 5 at the broken outer gate | **32** | 26 / 28 | **16-17.5** | 24 orcs, 30 soldiers | 9 % |

`marks 8` at wave 5: **Orc.Target 8.2 ms a frame** (17.7 calls a frame = ~460 us each), soldiers 1.1 ms in all.
BUG: an orc on a siege road (`OrcUnit.WalkTheLane`) with no building to go for re-ran `AcquireTarget` EVERY frame,
and that runs `WithinADetour` -- a NavMesh path -- for the nearest building. The same `_target == null ||` pattern was
in the plain march and in `Raid`. Fix: `TimeToRetarget`/`Retargeted` -- a fallen target re-picks at once, nothing to
go for looks again in 0.5 s (jittered), a held one every 5 s. AFTER (same mission, 9 x 40 s to wave 5):

| | fps | cpu ms | scripts ms | Orc.Target | all orcs' Update |
|---|---|---|---|---|---|
| before | 32-38 | 16-26 | 6-17.5 | 8.2 ms | 10.1 ms |
| after | 36-38 (one dip to 30.5, gpu 31) | 13-17 | 4.3-10.9 | **0.22 ms** | **0.96 ms** |

Mission 2 is now GPU-bound at ~25 ms (2000x1200, Low): the next lever there is the GPU, as in mission 1.

## 2026-11-21 -- Invasion with a developed player (Redmi Pad)

Inv1 summer run; `town 40 60` (40 buildings, 60 people), `army 60` (60 level-3 soldiers, new command), then
`wave 25` and `wave 30` (new command: send wave N now):

| state | fps | cpu / gpu ms | scripts ms | note |
|---|---|---|---|---|
| run start | 42-43 | 10.4 / 21.1 | 3.9 | 154k tris, 85 orcs on the map |
| + town 40/60 + army 60 | 54-55 | 15.5 / 16.5 | 7.0 | |
| wave 25 (30 orcs) marching in | 45-48 | 18.2 / -- | 11.8 | 115 orcs, 61 soldiers, 60 people; 14 frames > 34 ms, 9 > 50 ms in 40 s |
| waves 25 + 30 stacked | 47-48 | -- | 11.6 | 135 orcs |
| as soldiers and people died | 52 -> 60 | -- | 2.9 | |

Never under 44 fps; the late waves are far below the field-battle limit (100 v 100 = 40 fps). The 60 level-3 soldiers
lost and the castle fell, so a longer late-wave fight was not captured.

## 2026-11-21 -- battle limits (Redmi Pad)

The user's question: how big a fight before fps falls under 30. Answer used for design: **no fight bigger than
120 v 120** (the user, 2026-11-21).

**Field battle, empty archipelago sandbox, default camera** (`battle N N`):

| per side | fps | cpu / gpu ms | scripts ms | frames > 34 ms |
|---|---|---|---|---|
| 20 v 20 | 48-55 | 14 / 15-19 | 5.5 | 0 |
| 40 v 40 | 54-55 | 15 / 15 | 6.2 | 0 |
| 60 v 60 | 45 | 17 / 19 | 8.3 | 0 |
| 80 v 80 | 41-42 | 19 / 22 | 10.9 | 1 % |
| 100 v 100 | 40-42 | 20 / 22 | 12.9 | 4 % |
| 120 v 120 | 35-37 | 23 / 25 | 15.6 | 8 % |
| 150 v 150 | 30 | 31 / 26 | 21.8 | 22 % (6 % > 50 ms) |

**Same battle inside a big town** (`town 100 220`, ~210 people walking, fight among the houses, orcs burning them),
BEFORE the crowd fix:

| per side | fps | cpu / gpu ms | scripts ms | frames > 34 ms |
|---|---|---|---|---|
| town alone | 46 | 16 / 19 | 6.5 | 2 % |
| 20 v 20 | 43 | 18 / 20 | 7.8 | 0.4 % |
| 40 v 40 | 41 | 19 / 21 | 9.5 | 1 % |
| 60 v 60 | 39 | 20 / 22 | 11.3 | 4 % |
| 80 v 80 | 37 | 23 / 24 | 13.6 | 11 % |
| 100 v 100 | 36 | 25 / 24 | 16.0 | 13 % |
| 120 v 120 | 27 | 36 / 27 | 24.9 | 36 % |
| 150 v 150 | 20 | 51 / 31 | 38.1 | 70 % |

Scripts grew faster than the fighters past 100. `marks` at 120 v 120: Orc.FightSoldier 8.8 ms (its chase),
Orc.GuardWalk 3.8 ms, soldier capsule Move 2.1 ms -- **CharacterController.Move in a packed crowd costs 60-100 us a
step** (a few us in the open). Fix: `Core/CrowdPause` -- a walker that got under a quarter of its step for 0.4 s stops
pushing for ~0.4 s (909d4b659). AFTER, same town save (188 people):

| per side | fps | scripts ms |
|---|---|---|
| town alone | 43 | 7.3 |
| 100 v 100 | 38 | 8.9 |
| 120 v 120 | 30 | 21.6 |
| 150 v 150 | 29 (no frames > 50 ms) | 24.1 |

Left: soldiers WALKING through a crowd (107 capsule Moves a frame at 58 us = 6.2 ms). Lever not pulled (look
decision): soldiers and orcs without the capsule + a cheap separation push.

## 2026-11-20 night -- the empty town's script cost (Redmi Pad)

An empty archipelago sandbox (5 people) spent **11.2 ms in scripts**, cpu 17.5 ms, 52 fps. Switching kinds off one by
one (`off <Type>`): the bot kingdom's 50 SoldierUnits ~7 ms, the 152 orc camp buildings' removal ~5 ms (they shrink
the soldiers' enemy list), orcs ~1 ms, everything else ~0. `marks`: **Soldier.Wall 6.8 ms** -- the bot's archers
posted on their walls ran a search over ~200 enemies EVERY frame (a no-target-every-frame bug).

| fix | commit | scripts | cpu |
|---|---|---|---|
| before | | 11.2 ms | 17.5 ms |
| wall archers search on a timer; orc towers wait when nobody is in reach | c53823fd7 | 6.2 ms | 13.9 ms |
| the bot garrison no longer mans before the player's castle stands (a new game had all 50 under the fog) | d265cdd6a | **4.0 ms** | 11.7 ms |

GPU then ~14.5 ms at 2000x1200 Low -- the tablet is GPU-bound in an empty town.

## 2026-11-20 evening -- citizens' CharacterController (Redmi Pad)

`town 100 220` then `capsule on/off`, alternated, 25-40 s each, archipelago sandbox:

| town | capsule ON | capsule OFF |
|---|---|---|
| 220 people | 41 fps, scripts 18.3 ms, physics 1.1 ms | **50 fps**, scripts 13.4 ms, physics 0.7 ms |
| 300 people | 24-34 fps, scripts 33 ms, physics 2.0 ms | **50 fps**, scripts 13 ms, physics 0.7 ms |

(Right after 220 people spawned with capsules: 20 fps, scripts 40 ms -- capsules pushing apart.) Without the capsule the
citizens cost almost nothing. -> `CitizenAgent.SimpleWalk` ON in every mode, the capsule off from Awake (44918c779).
The same comparison in the editor test had shown NO difference.

## 2026-11-20 -- army scripts in the editor (PerformanceProfileTests)

Idle orcs and soldiers searched for a target every frame (the 0.4 s timer only ran while they had one); camp guards
walked on the spot when they could not reach their post. Fixed: timer + jitter, immediate re-pick on a fallen target or
a new attack order; guards settle at the post (2ee2ac051). Editor, 11 soldiers + 8 orcs fighting: +0.33 -> +0.20 ms,
soldier 17-24 -> 3.5-6.5 us/frame, orc 4.4-5.3 -> 1.9-2.3 us, GC 3 982 -> 1 982 B/frame.

## 2026-11-18 -- GPU on three phones

See `performance-mobile.md` (top of the card): the film/menu ground shader's per-pixel noise baked into a texture
(menu 16 -> 31-36 fps on the Redmi Pad, mission 1 on the Pixel 21-27 -> 57-60), no MSAA on Adreno, Vulkan
pre-transform off (A71 drew in 45 % of the screen), film on phones without DOF/bloom, capped shadow reach.

## 2026-08 / 2026-08-31 -- the first baseline (editor)

Walking citizens were the CPU cost (17 walkers +0.72 ms), CharacterController.Move ~8 us vs 0.45 us for a transform
write, terrain taken off the collision matrix (halved Move). Draws: bare map 1 010, +30 buildings 1 146, +60 citizens
1 266. Details in `performance-mobile.md`.
