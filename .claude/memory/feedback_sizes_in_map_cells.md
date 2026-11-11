---
name: feedback-sizes-in-map-cells
description: "When discussing sizes with the user, talk in MAP CELLS (the game's units), never metres"
metadata:
  node_type: memory
  type: feedback
  originSessionId: 0f587829-fe18-4bb5-a5b7-742486c4675e
  modified: 2026-10-31T11:35:18.668Z
---

Talk about every size in MAP format: cells, the game's world unit. Never use metres. The user said
this on 2026-10-31: «в следующий раз при обсуждении размеров мы разговариваем в формате карты».

**Why:** I built the dungeon and its camp in "metres" for a 1.8 m person. In the game a hero is
0.6-0.8 cells tall: HeroKing 0.79, HeroQueen 0.62, HeroCaptain 0.81. A level-1 building is 1 x 1-2
cells and 1.2-1.5 tall. So everything came out about 2.3x too big, and «200 x 200» meant
different things to the two of us.

**How to apply:**
- Say «N клеток», compared with a hero's height (about 0.8) and a building (1-2 cells).
- Build new Blender content directly in cells, or shrink it with SCALE = 0.44, as `dungeon1.py` and
  `dungeon_camp.py` do.
- Check new models against the heroes before showing them. Use `render_preview.py` with
  `with=HeroKing...`.

Related: [[project-dungeon-entrance]], [[feedback-dungeon-style]].
