# Ruins of the player's buildings (Руины)
Status: DESIGN (2026-11-05). The look is waiting for approval; nothing is in the game yet.

## The user's rules (2026-11-05)
- A building of the player's destroyed by enemies does not vanish. **Ruins** stay on its plot; they can be selected and have a card.
- The card has three buttons:
  - **Восстановить**, with the price above it: the building is rebuilt;
  - **Снести**: the ruins are removed for good;
  - **Закрыть**: decide later.
- The restore price = the building's full worth × `ruin_restore_cost_fraction` (0.5 to start; sheet key, sandbox tab). The full worth is its price plus every upgrade up to its level, as `BuildingInstance.RepairCostOf` counts it.
- **Ruins from EVERYTHING**, roads, fences, walls and decor included (the user overrode my "small things vanish").
- **Restoring works like construction, but faster**: scaffolding and the net, at a share of the build time.
- **Sandbox only**: the Invasion mode stays as it is.
- **The Town Hall gives no ruins**: losing it loses the game.
- The look must be thought through: the renders go to the user first ([[feedback-approve-models-first]]).

## Proposed (mine, not agreed)
- Ruins keep their cells occupied. They produce nothing, house nobody, pay no tax, and enemies do not attack them.
- Restoring brings the building back at the level it had. Demolishing refunds nothing.
- Ruins are saved with the game.
- The bot's buildings are untouched: the bot rebuilds by itself.

## Look options (render: Design/ruins_preview.png, from Tools/blender/ruins_preview.py)
- **A, burnt shell:** the building's own model, broken off a third of the way up with two tilted cuts, every palette cell swapped for a charred one (the `CHAR` map), rubble and beams inside.
- **B, rubble heap:** a generic pile on the plot with a stub of corner wall.
- Both use the buildings' palette only, so no new material is needed.

## Files
- Tools/blender/ruins_preview.py, Design/ruins_preview.png.

## Open
- The user's choice of look.
- Where the existing damaged-building repair (`repair_cost_fraction` 0.4) and the ruins' restore meet in the card.
