---
name: feedback-build-only-on-request
description: Never build the APK on your own initiative — only when the user asks for a build in that message.
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 080acfc5-d0dc-48e1-926f-a4bcc3c65bed
  modified: 2026-08-20T08:47:55.614Z
---

The user asked (2026-08-20): **"добавь себе правило, что билды делать только когда я говорю"**.

**Why:** a build is slow, it ships an APK to their phone and to the Telegram topic, and it lands
as a notification they have to act on. Building at the end of every change turns a stream of small
fixes into a stream of half-finished APKs, and the user loses track of which one is worth
installing. They decide when a change is worth carrying to the phone, not me.

**How to apply:** finish the work the normal way — edit, run the batchmode rebuild, run the test
suites, commit and push (see [[feedback_batchmode_workflow]] and [[feedback_auto_push]]). Then
STOP and say the change is ready to build. Do not run `Tools/research/build_android.ps1` unless the
user asked for a build in that same message ("сделай билд", "собери", "скинь апк"). Finishing a fix
is not an implicit request for one.

The rest of the build workflow is unchanged once they DO ask: rewrite
`Tools/research/build_notes.txt` in Russian first, and the script sends the APK by itself (see
[[feedback_build_to_telegram]]).
