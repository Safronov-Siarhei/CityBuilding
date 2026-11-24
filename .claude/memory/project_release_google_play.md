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
- Policy page live and themed (flag picker in plain CSS: 9-slice seams showed black on phones). Ads = Google AdMob
  MEDIATION hub + UMP (the user 2026-11-24); next: the user makes the AdMob app + rewarded unit, then Claude wires
  the plugin behind Shop/RewardedAds.

**Why:** the user wants the fastest path to release. **How to apply:** before adding content, prefer release-plan
items; never commit keys (repo public); don't build the .aab unasked.
