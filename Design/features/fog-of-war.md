# Fog of war

> **Status (2026-10-21):** two looks on one blanket — PARCHMENT (default since 2026-10-14) and
> clouds — switched in the main-menu settings. Exploration rules deliberately unchanged (user).
> **Read with:** [maps-and-terrain](maps-and-terrain.md), [performance-mobile](performance-mobile.md).

## Rules
- Buildings reveal around them (`reveal radius` per building). The fog is chunked (100 chunks), out of
  the shadow pass (it used to cast — found only by `SceneryShadowTests`).
- Parchment: `Shaders/FogParchment.shader` reads a chart texture built once (`BuildChart`: R water,
  G height, B forest, A stone) with ink iso-lines. The chart hints rivers/hills/forests under the
  fog — the user chose that knowingly.
- `GameSettings.FogStyle`, PlayerPrefs key `settings.fog_style.v2` (bumped so everyone lands on parchment once).

## Code
`Maps/FogOfWarManager.cs` (`ApplyStyle`, chunks, chart), `Core/GameSettings.cs`,
`Shaders/FogParchment.shader` + clouds shader, `Tools/ui/make_cloud_noise.py`. SetupProject: `SetupProjectFog.cs`.

## Tests
PlayMode `FogTests` (Map 3; photos `fog-*.png`), `SettingsPanelTests`; EditMode `GameSettingsTests`.

## Watch
Transparent overdraw when zoomed out over lots of fog; at 5 m min zoom over hills the camera can sit
under the blanket; grass tufts show through the thin edge.
