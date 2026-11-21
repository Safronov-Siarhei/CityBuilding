# Studio logo and launch splash (Depigo Games)

**Status (2026-11-21):** logo kit DONE (committed 03b30172e). «Made with Unity» splash OFF. The studio splash
«Чернильное перо» is BUILT INTO UNITY as the build's first scene `Splash.unity` (setup run; not seen in the
game, not on a phone).

## Rules the user decided
- Logo = variant A (petals + dots + chevrons), wordmark DEPIGO / GAMES drawn as strokes (no font licence).
- Splash = «Чернильное перо», chosen because it is neutral: warm paper, dark-brown ink, nothing game-specific.
- Smooth and long: v2, 7.6 s (eased strokes, pen lifts in arcs between strokes, slow ink soak, slow push-in).
- **No sound** (a synthesized scratch and CC0 Freesound recordings were both rejected: noise).
- Unity's splash removed (optional on Unity Personal since Unity 6; nothing is owed in return).

## Launch order on Android
1. Android 12+ system splash (the phone draws it: app icon in a circle on the theme background) -- cannot be
   removed in any app, only recoloured. Recoloured to paper (user 2026-11-21): Unity 6 itself uses
   `PlayerSettings.SplashScreen.backgroundColor` to cover the Android 12+ automatic splash (Unity 6.3 docs), so the
   one line in SetupProject does it -- no Android theme resource needed. NOT checked on a phone yet (icon may or may not show).
2. Engine start: background = `PlayerSettings.SplashScreen.backgroundColor` (paper).
3. `Splash.unity` (build index 0): `SplashInk` plays the timeline and `LoadSceneAsync("MainMenu")` underneath with
   activation held; at the end (or after a tap + a 0.35 s fade to black) the menu is activated. If the menu is not
   loaded when the splash would fade out, the finished logo stays on the paper until it is.

## How it is made (one source of truth)
- `Design/branding/depigo/splash/gen_splash_ink.py` -- the timeline, shapes and the HTML preview.
- `export_splash_unity.py` (same folder) runs it and writes `Assets/_Project/UI/Splash/`: `splash_ink.json`
  (stroke polylines in 1920x1080 px y-down, dots, soak, pen keyframes, easing curves), `paper.png`, `fill.png`,
  `pen.png`. **Change the splash there and re-export; nothing in C# is tuned by hand.**
- Runtime: `Scripts/UI/SplashInk.cs` (player), `InkStrokeGraphic.cs` (strokes + dots as one UI mesh, rebuilt only
  while drawing), `SplashInkData.cs` (json + cubic-bezier easing). Shader `Shaders/InkSoak.shader` (UI; ragged
  radial reveal of fill.png), material `Materials/SplashInkSoak.mat` (referenced by the scene -> in every build).
- Editor: `SetupProjectSplash.cs` builds the scene; `SetupProject.Run` puts it FIRST in the build list.
- The scene also carries `FrameRateController` (60 fps from the first frame).

## Traps
- Editor Play from MainMenu skips the splash (it is only the build's first scene); open `Splash.unity` to see it.
- `paper.png` is 1.9 MB of grain (PNG can't compress noise); imported at max 2048, no mips.

## Open
- Full 7.6 s every launch vs full on first launch + short (~3 s) after -- asked, not answered.
- Check on a phone: the first screen is paper (and whether the app icon still shows), the splash is smooth at 60 fps.
