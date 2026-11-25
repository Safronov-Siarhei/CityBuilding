# Save and load

> **2026-11-18:** a save name keeps letters of ANY alphabet (a Russian name came out empty and the save was silently never written -- the user's «сохранение не сохранилось»); an empty name saves under the date-time. Test `SaveSystemSanitizeNameTests.KeepsCyrillic_ARussianNameIsAName`.

> **Status (2026-10-21):** saves cover town, army (incl. selection and attack orders), hunger,
> hazards, raids, portals, orc camps, prisoners, rocks. Autosave every 4 min real time + on
> pause/quit. Old saves keep loading through legacy fields.
> **Read with:** the card of whatever system you are adding state to.

## Rules
- Named saves via the save dialog; ONE AUTOSAVE PER MAP (the user, 2026-11-17; it was one shared `_autosave` and a
  game on another map overwrote it): slot `GameSaveController.AutosaveSlot(mapId)` = `_autosave_<mapId>` (a new game on
  the same map replaces it); a MANUAL save on a map deletes that map's autosave (it is newer); the list shows the
  autosaves first, newest first, as «Автосохранение — <map code>» (`#save_autosave_map`, code = `ArchipelagoMapId.Code`)
  + the date; the old shared `_autosave` is moved onto its map's slot when the list opens (`MigrateSharedAutosave`,
  `SaveSystem.Replace` keeps the newer). `IsAutosave(slot)` covers both. Test `AutosaveTests` (incl. the manual-save
  rule). 240 s, the user's choice, plus `OnApplicationPause/Quit`; the autosave never becomes the "current" save. ~10 ms per save for an
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
- Delete a save (the user, 2026-11-17): a red bin (glyph Hud_Trash, make_hud_glyphs.py) at each row's right in the
  sandbox's saves window; a small «Удалить сохранение «X»? Его нельзя будет вернуть.» card (Отмена / Удалить) ->
  `SaveSystem.Delete`, list redrawn. LoadGamePanelController (trashGlyph, deleteConfirm...), built in BuildSavesDialog.

- 2026-11-17: a resumed save looks at the Town Hall's footprint CENTRE once the hall exists and the loading screen is gone (waits up to 20 s), and again 0.6 s later (`GameSaveController.LookAtTownHallAfterLoad`) -- it gave up at frame 3 and the save opened far away.

**2026-11-20 (the user approved Design/ui/Concept_SaveSlots.png):** «Сохранить игру» lists THIS map's saves (by `GameSaveData.mapId` = `GameSaveController.CurrentMapId`, now public; autosaves never), newest first, in a scrolling column (`SetupProjectGameplayUI.BuildSaveUI`: card 900x800, `Slots` ScrollRect + RectMask2D, `Warning`, confirm = a dynamic-label button). A row tap puts its name in the field; a typed name that exists = overwrite: row lit (`UiTheme.RowSelected`), button «Перезаписать» (`#save_overwrite`), red `#save_overwrite_warning`; any other name = new save. Rows built at runtime in `SaveDialogController` (`#save_row_stats` day/people, local time). Each listed save is parsed once per opening (few files). Built, not compiled by setup yet (the user's Unity was open).

**2026-11-25 cloud save (account progress, not map files):** PlayerPrefs progress goes to Google Play Games saved games
through `Core/CloudSave.cs`; every runtime PlayerPrefs write must use `CityBuilder.Core.Prefs.Set*` (key list for the
snapshot). Map saves (`SaveSystem` files) stay on the device for now. Details: monetization.md «Google Play billing +
cloud save».
