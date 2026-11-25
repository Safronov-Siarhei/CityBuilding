# Release plan: Google Play (started 2026-11-24)

Summary of everything learned so far (Russian, for quick questions): `Design/store/PUBLISHING.md`.

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
- Ads: UNITY ADS through Unity LevelPlay, one network for now (the user, 2026-11-24). AdMob was picked first, then
  dropped: AdMob is NOT AVAILABLE to the studio -- do not suggest it again, nor Google demand through another
  mediator (that needs AdMob too). The user: «самую простую сеть ... сможем поправить позже». LevelPlay lets more networks
  (AppLovin, Mintegral, Liftoff...) join later from its dashboard + adapters, without touching the game code.
  Consent: LevelPlay has no consent window of its own -- a simple in-game EEA/UK consent prompt -> SetConsent.
- Upload key made by the user 2026-11-24: PKCS12 at D:\Keys (outside the repo), alias `upload`, valid to 2054; the
  four CB_* user env vars set and checked (PKCS12: key password = store password).

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
- [x] Release signing (key made + env vars set 2026-11-24): the build reads CB_KEYSTORE / CB_KEYSTORE_PASS / CB_KEY_ALIAS / CB_KEY_PASS (U makes the upload
      key OUTSIDE the repo and keeps a backup; `*.keystore`/`*.jks` are git-ignored). Play App Signing keeps the app key.
- [~] FIRST .aab BUILT 2026-11-25 (Unity Personal, versionCode 1607, 226.6 MB: base 35 MB + UnityDataAssetPack 202 MB compressed, signed with the upload key; billing library inside; Play Games off -- no app id). The build writes the keystore path + versionCode into ProjectSettings.asset: revert, never commit. Check the first .aab: base module under 200 MB, the asset pack installs with the app (internal testing track).
      Last APK build report: meshes 277 MB / textures 108 MB uncompressed of 429 MB user assets -- slimming later
      (mesh compression, dungeon reliefs ~5 MB each) would still help download size.
- [ ] Native debug symbols for Play Console crash reports (the first upload warned, 2026-11-25): Android symbols
      export in the -Store build (`EditorUserBuildSettings.androidCreateSymbols`), upload the .zip with each release.
- [x] Target SDK 36 confirmed by Play Console on the first upload (2026-11-25).
- [ ] Target API level = Google's current requirement; 64-bit only (already ARM64).
- [ ] Release flags: no cheats, no PerfProbe, no debug logs (`-release` / CB_RELEASE already exists).

### C. Must-have features before players (C, decisions U)
- [ ] Cloud save of crystals + campaign progress (Play Games Saved Games) -- monetization card «BEFORE RELEASE».
- [ ] Invasion leaderboards on Play Games (day / week), decided 2026-11-17.
- [~] Payments: Google Play Billing via Unity IAP 5.5.1 (`Core/Billing.cs`, IN THE CODE 2026-11-25) behind `Store`; 14
      products (`Design/store/product_ids.txt`). U: payments profile, first AAB, create + activate the products, license
      testers -- `Design/store/PLAY_BILLING_AND_GAMES.md`.
- [~] Cloud save of the account progress to Play Games saved games (`Core/CloudSave.cs` + `Prefs`, plugin 2.3.0 without
      EDM4U, IN THE CODE 2026-11-25). U: PGS project, credentials (SHA-1s), Saved games ON, app id into
      `Design/store/play_games_app_id.txt`. Then Data safety: user IDs + outside-account sign-in = Да.
- [ ] Ads: Unity LevelPlay package + Unity Ads adapter, rewarded only, behind `Shop/RewardedAds` (today a stand-in),
      own consent prompt; U: LevelPlay account (Unity Cloud), Android app -> app key, rewarded ad unit id.
- [ ] IN-GAME privacy link (Google: the policy must be linked on the store page AND inside the app): a «Политика
      конфиденциальности» button in the main-menu settings opening the URL; none exists yet (checked 2026-11-25).
- [x] Terms of Use page `docs/terms.html` (EN+RU, same kit as the policy, the two pages link each other), 2026-11-25:
      licence, crystals have no money value, purchases/refunds via Google Play, rewarded ads, «as is», consumer
      rights kept. URL in `Design/store/terms_url.txt`, entered on the Play Console trader-status form. Put a
      «Условия» link next to the in-game privacy link. Trader status: «Да» (the user, 2026-11-25).
- [ ] ANALYTICS (the user, 2026-11-25: «хочу добавить аналитику в игру, это важно, просто перед релизом сделаем»):
      events for the funnel (tutorial steps, campaign levels, sandbox progress, Invasion waves, shop/ad views and
      purchases), retention. Provider not chosen; Claude's suggestion: Firebase Analytics + Crashlytics (the Firebase
      project citycraft-game exists; crash reports too), alternative GameAnalytics. When added: update the privacy
      policy, Design/store/play_console_answers.md (Data safety: app interactions, diagnostics, IDs for analytics).
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
- [x] Privacy policy PUBLISHED 2026-11-24 at https://citycraft-game.web.app/privacy-policy.html (Firebase Hosting,
      project citycraft-game on the user's working Google account; the user deploys from THEIR terminal --
      Claude's shell is sandboxed: run `firebase.cmd deploy --only hosting` from APPDATA npm in the project folder).
      Source `docs/privacy-policy.html` (EN + RU, 2026-11-24). Contact = depigogames@gmail.com (the user,
      2026-11-24: the public support address, also for the Play listing); ads named as Unity Ads (LevelPlay). Hosting: FIREBASE HOSTING (the user, 2026-11-24:
      the indie format «<project>.web.app/privacy-policy.html»): `firebase.json` serves `docs/`; the user makes the
      Firebase project on depigogames@gmail.com and runs `firebase login`; then `firebase deploy --only hosting`. Project id `citycraft-game` (.firebaserc) ->
      https://citycraft-game.web.app/privacy-policy.html
- [~] Data safety form, content rating questionnaire (IARC), target audience (not children: avoids the Families
      policy), ads declaration: answers drafted in `Design/store/play_console_answers.md` (2026-11-25), the user fills
      Play Console from it. Re-check Data safety whenever an SDK is added or removed (Play Games, more ad networks).

### F. Launch (U)
- [ ] Internal testing track (instant, up to 100 testers) -> closed test (12+ testers x 14 days if personal
      account) -> production, possibly a staged rollout.
