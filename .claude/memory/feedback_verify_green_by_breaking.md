---
name: verify-green-by-breaking
description: "A test that has never failed is not evidence — break the thing it covers, watch the right test go red with the right message, then restore. Earned repeatedly in this project."
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 9160e728-cf3c-43c7-bcdb-bd50065a7f56
  modified: 2026-08-18T13:53:38.427Z
---

Before reporting a suite green as evidence a feature works, **deliberately break the mechanism and re-run.** The right test must go red, with a message that names the real symptom. Then restore and re-run clean. Keep a `.cp` of the file first so the restore is exact.

**Why:** in this project a green suite has repeatedly meant nothing, and each time the test looked completely reasonable. The failures were never in the assertion — they were in the world the test set up.

**How to apply:** break one mechanism at a time, and check WHICH test fails, not just that something did. If the wrong one fails, the tests are entangled or the one you meant is vacuous.

## The times it paid, and what each one looked like

- **Save/load (2026-08-18)**: commenting out `RestoreArmy` failed on "Expected: 2, But was: 0". The only reason to believe the green.
- **Migration**: removing the arrive/leave calls failed three behaviour tests with their own messages ("Nobody left, so this test proved nothing").
- **The harvest radius**: deleting the radius filter and the suite STAYED GREEN. The fixture had no Town Hall, so `CitizenVisualsManager` resolved no town centre, so no citizens existed, so nobody gathered anything — and "the far boulder was untouched" was true for the emptiest of reasons. Fixed by placing a Castle in setup and asserting `AllAgents.Count > 0` before relying on a worker. The map's own 140 boulders also had to be hidden for the test, or the worker just picks a nearer real rock and the filter is never exercised.
- **The chopping progress bar**: three tests green, and the bar was invisible on a real screen — it sat two metres beside the trunk because the node's local +Y is sideways on a rotated FBX. The tests built their node from a bare `new GameObject()`: no rotation, no scale, **the one shape the bug cannot appear in**.

## The shape of the mistake, every time

The assertion was fine. The SETUP was degenerate — an empty world, an unrotated transform, a unit scale, nobody employed — so the assertion passed on nothing. When writing a test, ask what would make it pass vacuously, and put that in the fixture: a citizen that must exist, a rotation that must be wrong, a competitor that must be cleared out of the way.

Related: [[feedback_batchmode_workflow]] for running the suites, and never trust a results XML whose `start-time` is not fresh — a stale green from an earlier run reads identically to a real one.
