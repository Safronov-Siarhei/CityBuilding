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
- [x] Google Play Console developer account (one-time 25 USD, identity verification; personal or organization).
- [x] Create the app in Play Console: name, default language, game, free, the declarations.
- [x] Payments profile (only if there will be paid items).

### B. Build for the store (C)
- [x] AAB build: `Tools/research/build_android.ps1 -Store` (2026-11-24, code only, NOT built yet): .aab, CB_RELEASE,
      `splitApplicationBinary` = the data in an install-time asset pack (PAD), versionCode = git commit count.
- [x] Release signing (key made + env vars set 2026-11-24): the build reads CB_KEYSTORE / CB_KEYSTORE_PASS / CB_KEY_ALIAS / CB_KEY_PASS (U makes the upload
      key OUTSIDE the repo and keeps a backup; `*.keystore`/`*.jks` are git-ignored). Play App Signing keeps the app key.
- [x] FIRST .aab BUILT 2026-11-25 (Unity Personal, versionCode 1607, 226.6 MB: base 35 MB + UnityDataAssetPack 202 MB compressed, signed with the upload key; billing library inside; Play Games off -- no app id). The build writes the keystore path + versionCode into ProjectSettings.asset: revert, never commit. Check the first .aab: base module under 200 MB, the asset pack installs with the app (internal testing track).
      Last APK build report: meshes 277 MB / textures 108 MB uncompressed of 429 MB user assets -- slimming later
      (mesh compression, dungeon reliefs ~5 MB each) would still help download size.
- [ ] **MUST before release: small first download (the user, 2026-11-25: «чем дольше скачивается -- тем меньше хочется
      играть»).** Today the install is ~237 MB download / ~268 MB on the phone (base 35 MB + install-time pack 202 MB).
      Goal ~50-70 MB at install: content to Addressables + Play Asset Delivery packs -- install-time = code, splash,
      main menu, tutorial; fast-follow (downloads by itself right after install) = sandbox maps, campaign, dungeons,
      Invasion; on-demand = rare content. A «Загружаем… N %» screen when a mode is opened before its pack arrived.
      Step 1 first: measure size per mode + mesh compression / ASTC / dungeon reliefs (cheap); step 2: the packs.
      LAYOUT DECIDED 2026-11-28 -- card Design/features/download-size.md (install: tutorial, Invasion, campaign
      level 1, archipelago start layout + 1 dungeon; everything else ONE fast-follow pack, no on-demand).
      STEP 2 BUILT 2026-11-28 (dungeons, islands, campaign 2-3, sandbox-only building levels 3-5): install ~92 MB (was 213), packs 4.5 + 119 MB.
      Left: phone check from internal testing.
- [ ] Native debug symbols for Play Console crash reports (the first upload warned, 2026-11-25): Android symbols
      export in the -Store build (`EditorUserBuildSettings.androidCreateSymbols`), upload the .zip with each release.
- [x] Target SDK 36 confirmed by Play Console on the first upload (2026-11-25).
- [x] Target API level = Google's current requirement; 64-bit only (already ARM64).
- [ ] Release flags: no cheats, no PerfProbe, no debug logs (`-release` / CB_RELEASE already exists).

### C. Must-have features before players (C, decisions U)
- [x] Cloud save of crystals + campaign progress (Play Games Saved Games) -- monetization card «BEFORE RELEASE».
- [ ] Invasion leaderboards on Play Games (day / week), decided 2026-11-17.
- [~] Payments: Google Play Billing via Unity IAP 5.5.1 (`Core/Billing.cs`, IN THE CODE 2026-11-25) behind `Store`; 14
      products (`Design/store/product_ids.txt`). U: payments profile, first AAB, create + activate the products, license
      testers -- `Design/store/PLAY_BILLING_AND_GAMES.md`.
- [x] Cloud save of the account progress to Play Games saved games (`Core/CloudSave.cs` + `Prefs`, plugin 2.3.0 without
      EDM4U, IN THE CODE 2026-11-25). U: PGS project, credentials (SHA-1s), Saved games ON, app id into
      `Design/store/play_games_app_id.txt`. Then Data safety: user IDs + outside-account sign-in = Да.
      2026-11-26: PGS project 594846398884 set up (2 Android credentials «CityCraft»: Play signing = for new installs,
      upload key; Saved games ON; Drive API on; owner is a tester). AAB versionCode 1616 BUILT with Play Games
      (play-services-games-v2 + APP_ID checked inside). Phone test of 1616: DEVELOPER_ERROR (wrong SHA-1 in the Play
      signing OAuth client -- fixed by the user, see Design/store/signing_sha1.txt), then silent sign-in = Canceled
      (consent needed). Fix in the code b3afc876f (Google's sign-in window once per install, push ~15 s after a change,
      logs) -- AAB 1620 built 2026-11-28; PHONE TEST PASSED (the user: sign in, buy, uninstall, reinstall -- the
      crystals came back). Data safety updated 2026-11-28 (outside-account sign-in = Да, «Другое»; Personal info → User
      IDs: collected, not shared, not ephemeral, OPTIONAL, purpose App functionality). Before release: PGS properties
      (description, category, icon) + «Опубликовать» in Play Games Services → Публикация.
- [ ] CLOUD SIGN-IN UI (the user, 2026-11-28): Google's sign-in window shows once per install, so a player who declined
      has no way back. (1) After a consumable purchase (crystals_*, sale_crystals_*, piggy_*) while NOT signed in to Play
      Games: a window «not signed in -- crystals and progress live only on this phone and are lost on uninstall» with
      «Войти» / «Позже» (not for crystal_mine / sandbox_freedom: Google Play restores those anyway). (2) Main-menu
      settings: «Облачное сохранение: вкл/выкл» + «Войти». «Войти» = PlayGamesPlatform.ManuallyAuthenticate, then
      CloudSave's start read. Texts in the localization sheet, window from the skin kit. Purchases themselves never
      need the sign-in (billing uses the Play Store account).
- [ ] Advertising ID declaration (Play Console → Контент приложения): answered «Нет» 2026-11-28 -- the AAB has no
      AD_ID permission and no ads SDK yet. SWITCH TO «Да» in the same release that brings LevelPlay (its SDK adds
      com.google.android.gms.permission.AD_ID; check the AAB manifest), or Play Console blocks the release.
- [ ] Ads: Unity LevelPlay package + Unity Ads adapter, rewarded only, behind `Shop/RewardedAds` (today a stand-in),
      own consent prompt; U: LevelPlay account (Unity Cloud), Android app -> app key, rewarded ad unit id.
- [ ] IN-GAME privacy link (Google: the policy must be linked on the store page AND inside the app): a «Политика
      конфиденциальности» button in the main-menu settings opening the URL; none exists yet (checked 2026-11-25).
- [ ] SITE HOME PAGE `docs/index.html` (the user, 2026-11-26: «потом сделаем»): the root https://citycraft-game.web.app/
      is 404 today, but it is now the OAuth consent screen's «Application home page» (Google Auth Platform, project
      citycraft-game) -- make it before Google ever reviews the consent screen; then also the Play listing «Веб-сайт».
- [ ] OAuth consent screen PUBLISHED 2026-11-26 («In production», External, scopes games/games_lite/drive.appdata,
      no logo). Watch on the phone: if signing in to the cloud save shows Google's «unverified app» screen, the app
      must pass OAuth verification before release (the 100-user cap applies to unapproved sensitive scopes).
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
- [ ] Before production: send ALL saved «Контент приложения» changes (Data safety for Play Games 2026-11-28, advertising
      ID «Нет» -- or «Да» with LevelPlay, later ads/analytics answers) to review in ONE go via «Обзор публикации».
      They sit as drafts until then (the user, 2026-11-28: no point sending before the release build is final).
      Exception: if a test release gets blocked by the advertising-ID declaration, send it then.
- [ ] Internal testing track (instant, up to 100 testers) -> closed test (12+ testers x 14 days if personal
      account) -> production, possibly a staged rollout.
