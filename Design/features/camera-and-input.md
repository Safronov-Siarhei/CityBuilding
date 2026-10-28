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
- The rig rides over the land (2026-10-20): each frame it samples the ground under the camera and
  along its line of sight and lifts itself (quickly up, gently down) — it used to fly through the
  Caldera's mountains. Guarded by PlayMode `CameraTerrainTests`.
- Modals block input via `ModalGate`, not time.
- SMOOTH FOLLOW (2026-10-28, the user: «камера двигается как будто не плавно»): gestures, fling,
  focus and keys move a GOAL (`_goal`, xz); each frame the rig closes on it at
  `followSharpness` 18/s (exponential, ~50 ms behind). The drag sets the goal from where the rig is
  NOW + the grab correction, so it stays exact under the lag. The fling uses `_panVelocity`, an
  average of the goal's steps over time (one uneven touch no longer decides it); no fling if the
  finger rested >0.1 s before lifting. Mouse-wheel zoom eases (`zoomSharpness` 14); a PINCH zooms
  at once (its pan needs the real distance). A move from outside (`JumpTo`, a test setting the rig)
  is adopted as the new goal (`AdoptOutsideMoves`). Not felt on a phone yet.
- A held building (BuildingFocus) is let go once the camera leaves it -- its middle off the
  screen, not while the camera is still gliding to it (`BuildingFocus.OffScreen`, the user
  2026-10-28; test `BuildingFocusTests`). The router raises `TouchedDown` on the first finger
  anywhere (world or HUD), before anything is made of it; `NotifyTouchDown` is the tests' way in.

## Code
`Input/TouchInputRouter.cs`, `Input/WorldInputDispatcher.cs`, `Camera/RTSCameraController.cs`,
`Core/ModalGate.cs`; minimap `UI/MinimapCameraUpdater.cs` (renders by hand every 0.4 s, no shadows),
`UI/MinimapCameraIndicator.cs`.

## Tests
Gesture recognition, pan and zoom have NO automated coverage (batchmode can't reach them); the
9-point checklist in `Tools/research/build_notes.txt` is the phone verification. Fence-line snap has tests.
