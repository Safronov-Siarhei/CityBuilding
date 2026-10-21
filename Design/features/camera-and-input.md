# Camera and touch input

> **Status (2026-10-21):** touch controls rebuilt 2026-08-20 after the user called them unplayable.
> The full gesture × target → outcome schema is **`Design/touch_controls.md`** (Russian, 25 KB) —
> read its relevant section rather than re-deriving; keep it updated when behaviour changes.
> **Open since 2026-08-30, user never answered:** raise the zoom floor from 5 m to 8 m (at 5 m the
> camera is under the rooflines).
> **Read with:** [building-placement](building-placement.md), [army-and-soldiers](army-and-soldiers.md).

## Rules
- Act on the finger's LIFT/gesture, never on `press.wasPressedThisFrame` (the old bug: four systems
  each acted the frame the finger landed).
- One router owns touches (`TouchInputRouter`, `DragOwner`, phases) and dispatches taps
  (`WorldInputDispatcher`); modes Browse / CitizenOrder / ArmyCommand / Placement / Draw.
- Camera: pitch 50°, FOV 60°, zoom min/opening/max = 5 / 15 / 25 m (opening lives in SetupProject as
  the camera's local z; must stay within min/max). Ground strip ≈ 1.97 × distance. World-anchored pan,
  centroid zoom. `RTSCameraController.FocusOn` glides to a point (event feed taps).
- Modals block input via `ModalGate`, not time.

## Code
`Input/TouchInputRouter.cs`, `Input/WorldInputDispatcher.cs`, `Camera/RTSCameraController.cs`,
`Core/ModalGate.cs`; minimap `UI/MinimapCameraUpdater.cs` (renders by hand every 0.4 s, no shadows),
`UI/MinimapCameraIndicator.cs`.

## Tests
Gesture recognition, pan and zoom have NO automated coverage (batchmode can't reach them); the
9-point checklist in `Tools/research/build_notes.txt` is the phone verification. Fence-line snap has tests.
