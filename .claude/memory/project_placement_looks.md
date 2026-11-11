---
name: project-placement-looks
description: How a building is shown while being placed -- the pen-and-ink look the user chose on 2026-10-20 after rejecting the flat green/red tint; what was tried and dropped.
metadata:
  type: project
---

**IN THE GAME since 2026-10-20 (246f451a, 810e77dd), for EVERY building and for road/fence lines.**
The user found the old flat green/red tint «дёшево». Seven looks were built and photographed; the user rejected own-colours+gold plate, plain ink drawing with square frame/cell boxes («слишком просто»), parchment sheet + ink blots, and architect's corner marks + watercolour wash, and CHOSE «набросок от руки + штриховка картографа» plus three extras: the pen draws it in, only the clashing part turns red, a shadow. All other looks and the tint were deleted.

How it works:
- `PlacementPreview` (Scripts/Buildings) on the ghost: swaps every renderer to shader `CityBuilder/PlacementGhost` (Shaders/PlacementGhost.shader: ink drawing -- paper faces, half-metre ink grid, inked silhouette; depth pre-pass so it is see-through without inner faces; `_Valid`, per-cell `_BlockMask`/`_BlockRect`/`_UseMask` so only the part over blocked cells is red, `_RevealY` clips both passes for the draw-in). Registered in RuntimeShaders.PlacementGhost + Always Included.
- Ground = `SketchMarks.Build(..., reveal)`: per side a firm + light wandering pen stroke running past corners (seeded by footprint so it does not crawl), dotted cell lines, red hatching texture on blocked cells, soft shadow. Pen draws in 0.45 s on pick-up (drawing rises behind an ink line after 0.12 s), 0.22 s re-sketch per new cell; hop 0.14 s; DustPuff on landing (not for roads/draw mode). Static reused buffers; BuildingPlacer caches the per-cell delegate (`_cellInTheWay`).
- Lines: `SketchMarks.BuildLine` -- blue square per piece, hatched red = ground refuses, red outline = beyond what is affordable. One mesh, replaces the old pooled green/red quads.
- Helpers in `Ink.cs` (Hash, Smooth, Make transparent unlit).
- Test hooks: BuildingPlacer.PinnedOriginForTests, PreviewLineForTests. Tests: PlacementPreviewTests (Map5 meadow (-6,-12) and lake shore (5,2); Tavern is research-gated so the biggest pickable is the Warehouse). Photos with -Graphics: placement-pen-0..5, placement-free/-blocked/-dust/-road-line.png.
- No sound: the game has no audio system (a pen-scratch sound was liked as an idea).

**Roads and fences piece by piece (2026-10-20, user's call):** no more drag-to-draw lines. `BuildingPlacer.IsChainMode` (= keepSelectedAfterPlacement) pieces are placed on the tick like any building and stay in hand; `OfferNextPiece` puts the next right after the last, continuing the direction of the last two (first time: the grid step most to the screen's right); touch -> `cameraController.FocusOn(rig + step)` glides one piece so the aim point lands on it, offer held while IsFocusing / aim == offer; mouse -> held until the mouse moves (or no mouse at all). ✕ ends. Removed: drag handlers + router subscription, line preview + SketchMarks.BuildLine, line geometry incl. SnapStraightEnd + FenceLineSnapTests, #draw_hint/#draw_line_summary (also deleted from the sheet; verify_sheet clean). PlacementHudController: ✓ always places, aim marker for every placement, rotate hidden for chain pieces. Test hooks: GhostOriginForTests. Photo placement-road-chain.png.
