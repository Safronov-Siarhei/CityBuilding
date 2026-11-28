---
name: project-release-google-play
description: Google Play release prep started 2026-11-24 -- plan in Design/RELEASE_PLAN.md; app created (com.depigogames.citycraft); account predates the 12x14 closed-test rule; IAP + rewarded ads + PAD chosen.
metadata:
  node_type: memory
  type: project
  originSessionId: 65820551-ddcb-4e52-a0d1-9eafc8080c74
  modified: 2026-11-24T19:00:50.723Z
---

2026-11-24 the user froze content («быстрее зарелизить игру») and started the Google Play release.
- Plan with steps and who does them: `Design/RELEASE_PLAN.md` (keep it updated).
- Play Console app CREATED: «CityCraft», package `com.depigogames.citycraft` (project switched from ...citybuilding), game, free.
- Developer account is PERSONAL, made before Aug 2023 (the studio's first game Total Battle Ragdoll Simulator, 2023-08-20):
  the 12 testers x 14 days closed-test rule does NOT apply.
- Decisions: first release WITH crystal purchases (Play Billing) and rewarded ads; size via Play Asset Delivery
  (release build ~227 MB > 200 MB base cap); store name «CityCraft» + maybe keywords.
- Built: `build_android.ps1 -Store` (.aab, install-time asset pack, upload key from env CB_KEYSTORE/CB_KEYSTORE_PASS/
  CB_KEY_ALIAS/CB_KEY_PASS, versionCode = git commit count) -- not built yet. Upload key MADE 2026-11-24 (D:\Keys, PKCS12, env vars set+checked).
- Policy page live and themed (flag picker in plain CSS: 9-slice seams showed black on phones). Ads = UNITY ADS via
  LevelPlay, one network (the user 2026-11-24, «самую простую сеть»); AdMob is NOT available to the studio -- never
  suggest it or Google demand via another mediator. Next: the user makes the LevelPlay app (app key + rewarded unit
  id), then Claude wires it behind Shop/RewardedAds with an own EEA consent prompt.

**Why:** the user wants the fastest path to release. **How to apply:** before adding content, prefer release-plan
items; never commit keys (repo public); don't build the .aab unasked.
- ANALYTICS before release (the user 2026-11-25, «это важно»): provider not chosen (suggested Firebase Analytics +
  Crashlytics); policy + Data safety answers must be updated with it. Play Console answers: Design/store/play_console_answers.md.

2026-11-25: Play Console data safety FILLED (6 types, all via Unity Ads + purchase history); trader status «Да»; Terms of Use page docs/terms.html made (URL in Design/store/terms_url.txt), needs the user's firebase deploy. Next: gov/finance/health declarations (all «нет»), category+contacts, store listing graphics (not made).
Summary of all publishing knowledge: Design/store/PUBLISHING.md (keep it updated with each release step).

2026-11-25/26: Billing (Unity IAP 5.5.1, Core/Billing.cs) + cloud save (Core/CloudSave.cs + Prefs, Play Games plugin 2.3.0 without EDM4U) IN THE CODE. AAB 1607 then 1616 (Play Games on, app id 594846398884) built + released to internal testing. PGS set up (2 OAuth clients, Saved games ON). Phone test of 1616: DEVELOPER_ERROR = wrong SHA-1 in the Play signing client (fixed; SHA-1s in Design/store/signing_sha1.txt), then silent sign-in Canceled. Fix b3afc876f (sign-in window once per install, push ~15 s) NOT BUILT -- the user said «Позже» to the next AAB. Products not created yet (section not shown). Build needs $env:UNITY_EXE = 6000.5.4f1 + Personal licence; revert ProjectSettings.asset after a -Store build.
2026-11-28: AAB 1620 (cloud fix) released to internal testing; the user's phone test PASSED -- crystals survive uninstall/reinstall via Play Games. Next: Data safety update for PGS (user IDs, outside-account sign-in Да), products, small first download, LevelPlay, analytics.
2026-11-28 later: small first download STEP 2 BUILT (5e27f253f): Addressables + PAD, one fast-follow pack LaterContent (dungeons but Dungeon1, islands but the start map seed 2028); install ~122 MB (was 213), pack 94.8 MB; not checked on a phone (needs internal testing install). Left: campaign 2-3 to the pack (discuss), phone check. Card Design/features/download-size.md.
2026-11-28 later still: campaign 2-3 also moved to the pack (CampaignFilmKit; the opening village had pulled every Cinematic fbx in): AAB 1635 install ~110 MB, pack 109.5 MB.
Round 3 same day: sandbox-only building levels 3-5 (BuildingLevelKit), Map3-5 editor-only, film takes to the pack: AAB 1636 install ~100 MB, pack 115 MB. Sandbox music left in the install (the user). Next ideas offered: ASTC 8x8 big pictures, stronger mesh compression.
Rounds 4-5: Dungeon1 in its own first pack FirstContent (asked first), level 3-5 icons to the pack: AAB install ~92 MB. EditMode tests NOT run since round 3 (the user said wait).
Round 6: six fast-follow packs by mode (First 4.5, Sandbox 32, Campaign 15, Dungeon1-3 14/30/28), install 92.4 MB (AAB 1639); sandbox saves to the cloud (CloudSaves, file sandbox_saves); new player's first sandbox = start map, returning (sandbox.played from cloud) = map table. Tests still pending (the user wanted questions first).
Round 7: fast-follow made Play show 200 MB and block «Играть» -> packs ON-DEMAND; AAB 1644 install 96.9 MB (Google MB), the user's phone test OK. Small download DONE; margin under 99 MB ~2 MB. Next release items: ads (LevelPlay), sign-in UI, analytics, in-game privacy links.
