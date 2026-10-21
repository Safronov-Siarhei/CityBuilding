---
name: project-fog-clouds
description: Fog of war has two looks (2026-10-14) — clouds (4c829b0a) and a parchment map (3dc0f9db), switched in main-menu settings; how each is built, what is still open.
metadata:
  type: project
---

The user called the old fog (black lid hovering 5 m up, 1 m staircase edge, mountains poking through, instant pop) "грубо, дёшево" and picked variant Б (clouds) from three mockups (А soft shadow, Б clouds, В parchment map) on 2026-10-14. They said explicitly to KEEP the exploration rules as they are for now (buildings reveal permanently, citizens temporarily) — do not make citizen reveal permanent without asking.

**Built:** `FogOfWarManager` keeps the reveal bookkeeping; drawing = `Shaders/FogClouds.shader` on a blanket mesh (20 m chunks, 2 m lattice, `Clearance` 3.5 m above max terrain within 2 nodes, heights from `MeshMapApplier.CollectTerrainMeshes` vertices). Mask R8 one texel/cell, eased over `RevealSeconds` 0.8, sampled where the VIEW RAY meets the ground (parallax fix). Noise `Shaders/CloudNoise.png` from `Tools/ui/make_cloud_noise.py`, imported linear/repeat by `ShaderTextureImport`. Material `Materials/FogClouds.mat` made in `SetupProjectFog.cs` — the scene reference is what puts the shader in the build. Chunks with no fog within 8 cells are switched off.

**Tests:** `FogTests` (Map 3) — easing, no cloud over explored town, nothing pokes through, photos fog-*.png; all three verified by breaking.

**Open / watch:** not seen on a phone yet (transparent overdraw when zoomed out over lots of fog); at the 5 m min zoom over hills the camera can sit below the blanket; grass tufts show through the thin edge. Related: [[project_map3]], [[feedback_mobile_first_optimization]].

**Parchment look added (2026-10-14, 3dc0f9db)** at the user's request, with a switch in the MAIN MENU settings (the only settings card; GameSettings.FogStyle in PlayerPrefs, OnChanged → FogOfWarManager.ApplyStyle swaps chunk materials). `Shaders/FogParchment.shader` reads a chart texture (R water from IsWaterCell, G height by rasterising terrain triangles /32 m, B forest from Wood ResourceNodes, A stone) built once in `BuildChart` when parchment is first shown. Ink lines via fwidth iso-lines. Note: the chart shows rivers/hills/forests under the fog — a known hint to the player, the user chose it knowingly. Tests: FogTests parchment test, SettingsPanelTests, GameSettingsTests (EditMode) — broken-verified.


**Parchment is the DEFAULT since 2026-10-14** (user: "облака не очень смотрятся"). `GameSettings.DefaultFogStyle = Parchment`; the PlayerPrefs key was bumped to `settings.fog_style.v2` so everyone who had tried the switch starts on parchment once. Clouds stay as the alternative in settings.
