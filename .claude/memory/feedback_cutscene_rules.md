---
name: feedback-cutscene-rules
description: every cutscene starts only after the loading screen is gone, and shows no interface but «Пропустить»
metadata:
  type: feedback
---

A cutscene (film, in-mission scene) must START only once the loading screen has gone -- its clock waits at 0 while
`LoadingScreen.Busy` -- and while it plays NOTHING of the interface may show except the «Пропустить» button
(no HUD, hints, joystick, skill buttons, hp bars, cheats button). After it, everything it took (camera place on
the rig, HUD, music) comes back exactly as it was.

**Why:** the user, 2026-11-15, on mission 2's opening scene: «осталась проблема как в первой сцене которую мы
делали, запиши себе правило, сцена должна начинаться после загрузочного экрана; в сцене не должно быть интерфейса
кроме кнопки Пропустить» -- the same mistake had happened with the opening film.

**How to apply:** hold the scene's time while LoadingScreen.Busy; sweep root canvases EVERY frame (HUD parts are
built late), FilmSkip.FilmShowing hides IMGUI too; save and restore the camera's local place. See
[[project-campaign]].
