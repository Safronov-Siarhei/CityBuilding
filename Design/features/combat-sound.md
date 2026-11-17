# Combat sound (all modes)

**Status (2026-11-15):** BUILT, setup green, user not heard. The user: «звуки выстрелов для арбалетов и луков и
башен + звуки ударов мечей, чтобы бои были не такими скучными»; scope «во всех режимах».

## Rules
- One static `CombatSound` (Scripts/Combat/CombatSound.cs) for every mode: sandbox, dungeons, invasion, campaign.
- Shots: `CombatProjectiles.Launch` -> bow (archers, towers, orc archers), crossbow (Crossbowman, GnomeCrossbow),
  javelin, catapult (Cannonball). Landed arrows clink at `combat_sfx_hit_share` of hits (`ApplyDamage`).
- Blades: `SoldierUnit` melee strike and `OrcUnit.Strike(SoldierUnit)` melee -> clash.
- Phone cost: a pool of `combat_sfx_voices` 2D sources taken in turn with Play (a voice cuts its old sound, so never
  more at once), nothing off screen or past `combat_sfx_hear_range` from the screen's centre on the ground (quieter
  towards the edge), one kind no oftener than `combat_sfx_gap_sec`; pitch 0.92-1.08. No allocation per call.
- Numbers: sheet tab «Армия и отряды» (combat_sfx_*), economy.csv.

## Files
- `Audio/Resources/Combat/<kind>_N.ogg` (bow 4, crossbow 3, javelin 2, arrowhit 4, clash 11, skill 1, wood 6, stone 5; catapult NONE) -- the
  name prefix picks the kind; a new variant is just a new file. Sources in `Audio/Campaign/CREDITS.md` (all CC0).
- Cut by `Tools/audio/cut.py` (Blender's ffmpeg) from the downloads; under `Audio/` so ModelImportDefaults makes
  them mono, Vorbis, decompress-on-load.

## Skills, buildings, buttons (the user, 2026-11-15, round 2)
- Hero skills: ONE sound for every skill everywhere (skill_1 = Kenney RPG Audio drawKnife1): `HeroSkills.Use`
  (sandbox, dungeons, invasion) and `CityDefenceMode.UseSkill` (Ардан, mission 2).
- Blows on buildings: wood on a wooden one, stone on a stone one -- `BuildingInstance.IsStoneBuilt` = what it cost to
  raise to its level (build + upgrades) holds at least as much stone as wood (cached per level). Hooks: orc melee on a
  building, a soldier's melee on one (the bot's town), any projectile landing on one. wood_ = Kenney impactWood_heavy +
  impactPlank_medium, stone_ = Kenney impactMining.
  `CombatSound.StructureHit(IDamageTarget)` takes every side's buildings: the player's and the bot's
  (`BotWarTarget.Building`) by their stuff, an orc camp as wood (palisade), an orc portal as stone; anything else
  clashes / thuds. Fire damage is silent on purpose (a burning house would knock every second).
- Interface buttons: `UI/UiClickSound.cs`, one DontDestroyOnLoad object booted at the first scene: on a pointer
  release it raycasts the UI once and clicks if a live Selectable is under it (no per-button wiring; buttons are made
  in dozens of places). Clip `Audio/Resources/Ui/click_N` (Kenney UI Audio click1), volume `ui_click_volume` (sheet,
  «Армия и отряды»), ignores the listener pause (menus over a paused game).

## The user's verdicts (2026-11-15)
- catapult_1 (our sfx_whoosh) «похож на пылесос» -> removed: catapults and ship cannons are SILENT (no free real one).
- arrowhit clinks «как будто чашки бьются» -> AntumDeluge «Thwack Sounds» (dull thuds 02/03/05/06).

## Traps
- `CityBuilder.Resources` namespace exists: write `UnityEngine.Resources.LoadAll` in full.

## Open
- The user's ideas for more sounds (proposed 2026-11-15, not agreed): gate hit / break, catapult landing, deaths,
  victory/defeat stingers, wave-cleared fanfare, UI clicks, building collapse, construction hammering.

## Building sounds (2026-11-17, built, not heard by the user)
- The user: a sound when a building is put down and when its upgrade ends, every mode. `Buildings/BuildSound.cs`:
  `Placed()` from `BuildingPlacer.PlaceAt` (roads/fences per piece, at most every 0.08 s), `Upgraded()` from
  `ConstructionSite.Finish` for a player's upgrade. Clips are our own: `Tools/audio/build_sounds.py` ->
  `Audio/Resources/Build/{place,upgraded}.wav`. Volume `build_sfx_volume` (sheet «Время и загрузка»).
- Round 2 (2026-11-17): the user disliked the knock -> «place» is now a soft plop (variant B); NEW «built» (dust whoosh +
  ding-dong) when a new building finishes on its timer with BuildFinishEffect (ConstructionSite.Finish, !IsUpgrade).
  Variants to choose from: Design/audio/*.wav (`python Tools/audio/build_sounds.py variants`); the user to pick.
