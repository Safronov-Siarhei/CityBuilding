# Release plan: Google Play (started 2026-11-24)

The user: «предлагаю начинать создавать проект в гугл и готовить всё к релизу». Content is frozen for now
(«можно отложить добавление контента, меня больше интересует быстрее зарелизить игру»).
Status marks: [ ] not started, [~] in progress, [x] done. Who: **U** = the user (accounts, money, signing in,
legal), **C** = Claude (code, builds, texts, images in the repo).

## Where the project stands (checked 2026-11-24)
- Package `com.depigogames.citycraft` (the Play Console app, created 2026-11-24; was ...citybuilding), company «Depigo Games», productName «CityBuilding» (the menu logo says
  «CityCraft» -- the store name must be decided), version 0.1.0 / versionCode 1, IL2CPP, ARM64, min SDK 26.
- Builds are APK only, signed with the DEBUG key (no custom keystore). The release APK is ~227 MB.
- NO store SDKs in the project: no Play Billing / Unity IAP, no ads SDK, no Play Games Services.
- Crystals, campaign progress and records live in PlayerPrefs (lost on reinstall); sandbox saves are local files.

## Decisions (the user, 2026-11-24)
- The Google Play developer account ALREADY EXISTS: PERSONAL, made before Aug 2023 (the studio's first game,
  Total Battle Ragdoll Simulator, came out 2023-08-20) -- older than Nov 13 2023, so the 12 testers x 14 days
  closed-test rule does NOT apply: internal testing -> production directly.
- The Play Console app is created (2026-11-24): «CityCraft», package `com.depigogames.citycraft`, game, free.
- Store name: «CityCraft» is the main part; keywords may be added (e.g. «CityCraft: ...», title cap 30 chars).
- First release WITH crystal purchases (Play Billing) AND rewarded ads.
- Size: Play Asset Delivery (not slimming first).

## Blockers found
1. **Size.** Google Play takes an App Bundle (AAB), not an APK, and its base download is capped at **200 MB**.
   227 MB does not fit: either slim the build (unused models/textures/audio, texture compression) or move big
   content to Play Asset Delivery packs. Measure first: which assets weigh most.
2. **Signing.** A real upload key is needed. The repo is PUBLIC: the keystore and its passwords must NEVER be
   committed -- kept outside the repo (+ a backup), passed to the build by environment variables.
3. ~~Closed testing rule~~ (not for this account, see Decisions). A PERSONAL developer account made after Nov 2023 must run a closed test with at least
   **12 testers opted in for 14 days in a row** before production is unlocked. An ORGANIZATION account (needs a
   D-U-N-S number) is exempt. This decides the timeline.

## Steps
### A. Accounts (U)
- [ ] Google Play Console developer account (one-time 25 USD, identity verification; personal or organization).
- [ ] Create the app in Play Console: name, default language, game, free, the declarations.
- [ ] Payments profile (only if there will be paid items).

### B. Build for the store (C)
- [x] AAB build: `Tools/research/build_android.ps1 -Store` (2026-11-24, code only, NOT built yet): .aab, CB_RELEASE,
      `splitApplicationBinary` = the data in an install-time asset pack (PAD), versionCode = git commit count.
- [~] Release signing: the build reads CB_KEYSTORE / CB_KEYSTORE_PASS / CB_KEY_ALIAS / CB_KEY_PASS (U makes the upload
      key OUTSIDE the repo and keeps a backup; `*.keystore`/`*.jks` are git-ignored). Play App Signing keeps the app key.
- [ ] Check the first .aab: base module under 200 MB, the asset pack installs with the app (internal testing track).
      Last APK build report: meshes 277 MB / textures 108 MB uncompressed of 429 MB user assets -- slimming later
      (mesh compression, dungeon reliefs ~5 MB each) would still help download size.
- [ ] Target API level = Google's current requirement; 64-bit only (already ARM64).
- [ ] Release flags: no cheats, no PerfProbe, no debug logs (`-release` / CB_RELEASE already exists).

### C. Must-have features before players (C, decisions U)
- [ ] Cloud save of crystals + campaign progress (Play Games Saved Games) -- monetization card «BEFORE RELEASE».
- [ ] Invasion leaderboards on Play Games (day / week), decided 2026-11-17.
- [ ] Payments: Google Play Billing (Unity IAP) for crystals / offers -- only if paid items are in the first release.
- [ ] Ads: mediation SDK + consent (GDPR/UMP) -- only if ads are in the first release.
- [ ] Old-save check: real saves kept as fixtures, loaded by a test every run (see save-load card).

### D. Quality pass (U plays, C fixes)
- [ ] One general test run (EditMode), fix what fails.
- [ ] A release AAB/APK on the phones of the fleet: sandbox, campaign to the prologue, Invasion -- end to end.
- [ ] Performance on the weakest phone.
- [ ] Translations last: en ru de fr es pt it pl tr, then a check that every text fits its box.

### E. Store listing (C drafts, U approves and uploads)
- [~] Name, short description (80 chars), full description (4000), EN + RU drafts in `Design/store/listing.md`
      (2026-11-24), waiting for the user's pick and edits; other languages with the game's translations.
- [ ] Icon 512x512, feature graphic 1024x500, 4-8 phone screenshots (+ tablet ones).
- [~] Privacy policy: `docs/privacy-policy.html` (EN + RU, 2026-11-24). Contact = depigogames@gmail.com (the user,
      2026-11-24: the public support address, also for the Play listing); placeholder [AD NETWORK] until the SDK is chosen. Hosting: FIREBASE HOSTING (the user, 2026-11-24:
      the indie format «<project>.web.app/privacy-policy.html»): `firebase.json` serves `docs/`; the user makes the
      Firebase project on depigogames@gmail.com and runs `firebase login`; then `firebase deploy --only hosting`.
- [ ] Data safety form, content rating questionnaire (IARC), target audience (not children: avoids the Families
      policy), ads declaration.

### F. Launch (U)
- [ ] Internal testing track (instant, up to 100 testers) -> closed test (12+ testers x 14 days if personal
      account) -> production, possibly a staged rollout.
