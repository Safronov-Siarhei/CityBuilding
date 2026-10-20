---
name: feedback-hand-off-as-file
description: "Anything the user has to copy — sheet rows, blocks of data, long snippets — must be sent as an attached FILE, never pasted into the chat."
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 080acfc5-d0dc-48e1-926f-a4bcc3c65bed
  modified: 2026-08-20T07:31:13.214Z
---

The user asked (2026-08-20), after being handed sheet rows twice as chat text: **"скинь нормально
и заведи себе такое правило, я не могу нормально копировать это"**.

**Why:** chat text is not copyable in practice. A markdown table renders as pipes and dashes, a
fenced block wraps, and tab-separated values lose their tabs entirely — which is exactly what
makes them paste into a spreadsheet as one column instead of four. The user is on a phone or in a
terminal, not in a text editor, so "here are the rows, copy them" is work they cannot do.

**How to apply:** whenever the deliverable is something the user must MOVE somewhere (rows for the
balance/localization sheet, a config block, a long command, data for another tool):

1. Write it to a real file, in the format the destination expects — `.tsv` for anything going into
   Google Sheets, UTF-8 **with BOM** so Cyrillic survives Excel too, CRLF line endings.
2. Send it with SendUserFile, `display: "attach"`.
3. Put the placement instruction in the caption — which file goes into which tab and which cell.
4. Keep the chat message to what the user has to DECIDE or KNOW, not to the data itself.

Do not also paste the same content into the chat "for convenience" — it doubles the message length
and the user still cannot use it.

Related: [[feedback_balance_row_handoff]] (what a correct sheet handoff contains — exact cell
addresses, sheet column order, inserts ordered so they do not move later rows), and
[[feedback_build_to_telegram]] (the same principle for builds: the APK goes to the phone by itself).
