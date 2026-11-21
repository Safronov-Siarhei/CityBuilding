# Studio logo and launch splash (Depigo Games)

**Status (2026-11-21):** logo kit DONE (committed 03b30172e). «Made with Unity» splash switched OFF in
SetupProject (takes effect at the next `setup`, not built yet). The studio's own splash «Чернильное перо» is
designed and previewed only — NOT in Unity yet.

## Rules the user decided
- Logo = variant A (petals + dots + chevrons), wordmark DEPIGO / GAMES drawn as strokes (no font licence).
- Splash = «Чернильное перо», chosen because it is neutral: warm paper, dark-brown ink, nothing game-specific.
- Smooth and long: v2, 7.6 s (eased strokes, pen lifts in arcs between strokes, slow ink soak, slow push-in).
- **No sound** (a synthesized scratch and CC0 Freesound recordings were both rejected: noise).
- Unity's splash removed (optional on Unity Personal since Unity 6; nothing is owed in return).

## Launch order on Android
1. Android 12+ system splash (the phone draws it: app icon in a circle on the theme background) -- cannot be
   removed in any app, only recoloured. Proposed: paper background (#efe6d2) via an Android theme resource -- not done.
2. Engine start: background = `PlayerSettings.SplashScreen.backgroundColor` (paper).
3. Studio splash scene: tap skips; the main menu loads in the background.

## Files
- `Design/branding/depigo/` -- logo kit, `gen_depigo.py`.
- `Design/branding/depigo/splash/` -- `gen_splash_ink.py` (timings at the top), `splash_ink_preview.html`.
- `Assets/_Project/Scripts/Editor/SetupProject.cs` -- `PlayerSettings.SplashScreen.show = false` next to the icons.

## Open
- Full 7.6 s every launch vs full on first launch + short (~3 s) after -- asked, not answered.
- Recolour the Android 12 system splash to paper -- proposed.
- Build the splash scene in Unity.
