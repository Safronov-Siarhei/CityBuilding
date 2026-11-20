---
name: project-pending-2026-11-20
description: Playtest 2026-11-20 (Editor, 12 items): 9 fixed in code (no build), 3 concepts shown and waiting for the user's choice
metadata:
  type: project
---
Fixed in code, not built/tested: loading ship on the red line; food eaten gradually over the day; people icon inside its chip (green number = free people); orc notice only on landing at a player island; lighthouse reach = own lit circle + 80 m; Crystal Mine not researchable before purchase; firefighters must really walk (fire out in 2 s); lab-under-upgrade case in the requirement chain (#research_lab_upgrading).
Concepts sent as SCREEN PNGs (Design/ui/Concept_ResourceInfo/BoosterScroll/SaveSlots.png, Tools/ui/sandbox_hud_concepts.py over the user's shot) -- the user prefers coloured screen-format concepts, not chat widgets; WAITING for the user: resource chip tap info, booster offer as an edge scroll + card, save window with current-map slots to overwrite.
Chain bug («нужна лаборатория 2» with lab 2): if it repeats with a finished lab, ask for a screenshot -- code looked right.
Also earlier idea list for a less boring sandbox (events with choices, Wonder, relics) waits for the user's pick.

CONCEPTS DECIDED 2026-11-20: 1 (resource tap info) and 3 (save slots) approved as drawn; 2 changed by the user -> badge under the resource bar, card in the screen middle. ALL THREE BUILT in code the same day (ResourceInfoPopup+ResourceFlow, BoosterHints badge/card, SaveDialogController slots), texts in the sheet; NOT compiled by setup (the user's Unity was open) -- next setup,setup must run; not seen.
