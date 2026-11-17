---
name: project-pending-2026-11-17
description: 4 queued items from the user 2026-11-17 (NOT built, user said "record, don't do now"): crystal fly-to-bar from the mine, Lab took all 5 citizens + "no workers" badge, Lab coin-shortage offer, instant build for 1 crystal.
metadata:
  type: project
---
The user asked on 2026-11-17 to RECORD these and NOT build them yet:

1. **Crystal mine pickup flies to the bar.** When the player collects crystals from the mine (CrystalMine), the crystals should fly into the crystal counter, like the existing resource fly-to-bar effect does for other resources.
2. **Laboratory staffing bug.** Placed at game start: all 5 citizens became its scientists, AND a "no workers" badge still hangs over it. Check the Lab's worker logic (auto-staffing vs scientists = research slots).
3. **Lab: not enough coins.** When an upgrade in the Laboratory lacks coins, show the existing shortage offer (coins for crystals or for an ad -- the OfferShortageWindow mechanism).
4. **Instant build for 1 crystal.** Place a building -> construction starts -> tap it -> to the LEFT of the «!» button a round button of the same style with a crystal icon and «1». The button row must be centred. For EVERY building. (User's screenshot: a building under the green net with the timer, yellow focus ring, «!» button below.)

**Why:** the user's playtest notes; they want them queued, not done now.
**How to apply:** when the user says to do them, build each in its real code path ([[feedback-do-every-item]]); price 1 crystal goes to the sheet, not C#.
