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
- `Audio/Resources/Combat/<kind>_N.ogg` (bow 4, crossbow 3, javelin 2, catapult 1, arrowhit 3, clash 11) -- the
  name prefix picks the kind; a new variant is just a new file. Sources in `Audio/Campaign/CREDITS.md` (all CC0).
- Cut by `Tools/audio/cut.py` (Blender's ffmpeg) from the downloads; under `Audio/` so ModelImportDefaults makes
  them mono, Vorbis, decompress-on-load.

## Traps
- `CityBuilder.Resources` namespace exists: write `UnityEngine.Resources.LoadAll` in full.

## Open
- The user's ideas for more sounds (proposed 2026-11-15, not agreed): gate hit / break, catapult landing, deaths,
  victory/defeat stingers, wave-cleared fanfare, UI clicks, building collapse, construction hammering.
