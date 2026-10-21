# Save and load

> **Status (2026-10-21):** saves cover town, army (incl. selection and attack orders), hunger,
> hazards, raids, portals, orc camps, prisoners, rocks. Autosave every 4 min real time + on
> pause/quit. Old saves keep loading through legacy fields.
> **Read with:** the card of whatever system you are adding state to.

## Rules
- Named saves via the save dialog; ONE autosave slot `_autosave` (240 s, the user's choice, plus
  `OnApplicationPause/Quit`); the autosave never becomes the "current" save. ~10 ms per save for an
  18 KB town in the editor (move the write off-thread if it hitches on a phone).
- Home button = autosave + leave, no confirm.
- **Every new piece of state must be added to `SaveData` AND restored**, and both paths verified by
  breaking them. Prefer ELAPSED over remaining where the remainder depends on post-load state (fires).
- Renaming a save field drops it from every existing save — keep old names (`recentStarvationDeaths`).
- Ids from static counters are not stable across a scene reload — save INDEXES.
- Restore order matters: `RestoreArmy` and `RestoreRaids` return what they built; `RestoreArmyOrders`
  pairs them afterwards. Orc camps rebuild from plan, then apply saved health (0 = ruin, `FallQuietly`);
  guards are NOT saved (spawners re-muster). A cage/treasury restored broken pays nothing.
- Not saved on purpose: which portal is ACTIVE (re-picked), shots in flight, ore veins (dealt from
  the map id), citizen names (spawn order).
- A save made on a deleted map (Maps 1–2) is listed greyed out «map removed» (`#load_map_removed`).
- No autosave in an Invasion run (an interrupted run simply ends).

## Code
`Saving/GameSaveController.cs` (capture + `Restore*`), `Saving/SaveData.cs` (`GameSaveData`,
`version`, `ArmyAttackTargetKind`, portal/camp/prisoner entries), `Saving/SaveSystem.cs` (files,
`SanitizeName`), `Saving/SaveDialogController.cs`, `Saving/GameSessionIntent.cs` (new game map id /
load slot across the scene change), `UI/LoadGamePanelController.cs`.

## Tests
EditMode `SaveDataShapeTests`, `SaveSystemSanitizeNameTests`; PlayMode `SaveRoundTripTests`, `AutosaveTests`.
