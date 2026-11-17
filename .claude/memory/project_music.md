---
name: project_music
description: Background music by mode + volume sliders (BUILT 2026-11-17, not heard): tracks per mode, quiet, fades, pauses between tracks
metadata:
  type: project
---
The user's music rules (2026-11-17), BUILT in UI/MusicPlayer.cs, not heard yet:
- menu mus_exploration; tutorial mus_the_bards_tale; sandbox mus_harvest_season + mus_market_day by turns;
  invasion mus_kings_feast, during a wave the campaign battle track (mus_mission1); campaign keeps its own music.
- Music must be QUIET background; each track swells in at the start and fades out at the end; a PAUSE between
  tracks «чтобы слух игрока отдохнул». Numbers on the sheet (music_*).
- Settings have «Музыка» and «Эффекты» sliders (GameSettings.MusicVolume/SfxVolume); every new sound source must
  multiply by one of them.

**Why:** the user's explicit request.
**How to apply:** card Design/features/combat-sound.md, section Music. Related [[project_lab_redesign]].
