---
name: feedback-do-every-item
description: The user's playtest lists must be done item by item and each fix checked where it actually runs -- they were angry when fixes silently did not apply
metadata:
  type: feedback
---
Do EVERY item of a playtest list, and check each fix lands in the code path that actually runs.
**Why:** 2026-11-06 the user wrote «ПОЧЕМУ ТЫ ДЕЛАЕШЬ НЕ ВСЁ ЧТО Я ТЕБЕ ГОВОРЮ??????» -- the ship price fix had been
inserted into the gnomes' function (first string match), and "ship comes to the troops" only covered one case.
**How to apply:** after scripted string-replace edits, grep that each lands in the intended method (not the first
match); for behaviour asks, cover the case the user showed on the screenshot, not only the general one; end with a
point-by-point list against the user's items. See [[project-tutorial]].
