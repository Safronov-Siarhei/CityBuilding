---
name: project-mobile-quality
description: 2026-11-09 mobile picture-quality pass -- transparent variants kept in builds, graphics tiers, HDR off/MSAA 4x, dims past SafeArea; not seen on a phone
metadata:
  type: project
---
The user (2026-11-09): effects pretty on PC look bad on the phone; sent 9 phone screenshots. Built e8cb8803c, NOT seen on a phone yet:
- Runtime-transparent materials (keyword _SURFACE_TYPE_TRANSPARENT) likely drew OPAQUE in builds (variant stripped): fixed by Resources/ShaderVariants/Transparent*.mat written by SetupProject.KeepTransparentVariantsInBuilds. Hypothesis, unconfirmed -- if dust/zones are still hard squares on the phone, the cause is elsewhere.
- Core/GraphicsQuality.cs: tiers Auto/Low/Medium/High, settings row «Графика», runtime copy of URP asset.
- Mobile_RPAsset HDR off, MSAA 4x; screen dims StretchBleed past SafeArea.
Card: Design/features/performance-mobile.md. Related [[project-mobile-device-fleet]], [[feedback-mobile-first-optimization]].
**Why:** the user judges the game on the phone. **How to apply:** next step is the user's phone check; then maybe mobile versions of dust/zones/clouds.

2026-11-09 later: PHONE PLACEMENT LOOK = variant A (user chose of 4: A tiles / B bolder ink / C stakes / D outline): PlacementTilePreview (own colours, green/red cells), building centre rides a DOT at screen centre (aim 0.58 -> 0.5); PC keeps ink. The user's "four lines in a square" was the HUD aim marker's 4 bars. da163db95, not seen on a phone.
User then decided: look strictly by platform (isMobilePlatform), building jumps cell by cell (CoC), no tests. Open from the list: semi-transparency (7), mobile dust/clouds (8), soft shadows (9), tier numbers to sheet? (10), PC ink lift offset (11).
