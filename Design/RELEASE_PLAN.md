# Release plan: Google Play (started 2026-11-24)

The user: «предлагаю начинать создавать проект в гугл и готовить всё к релизу». Content is frozen for now
(«можно отложить добавление контента, меня больше интересует быстрее зарелизить игру»).
Status marks: [ ] not started, [~] in progress, [x] done. Who: **U** = the user (accounts, money, signing in,
legal), **C** = Claude (code, builds, texts, images in the repo).

## Where the project stands (checked 2026-11-24)
- Package `com.depigogames.citybuilding`, company «Depigo Games», productName «CityBuilding» (the menu logo says
  «CityCraft» -- the store name must be decided), version 0.1.0 / versionCode 1, IL2CPP, ARM64, min SDK 26.
- Builds are APK only, signed with the DEBUG key (no custom keystore). The release APK is ~227 MB.
- NO store SDKs in the project: no Play Billing / Unity IAP, no ads SDK, no Play Games Services.
- Crystals, campaign progress and records live in PlayerPrefs (lost on reinstall); sandbox saves are local files.

## Blockers found
1. **Size.** Google Play takes an App Bundle (AAB), not an APK, and its base download is capped at **200 MB**.
   227 MB does not fit: either slim the build (unused models/textures/audio, texture compression) or move big
   content to Play Asset Delivery packs. Measure first: which assets weigh most.
2. **Signing.** A real upload key is needed. The repo is PUBLIC: the keystore and its passwords must NEVER be
   committed -- kept outside the repo (+ a backup), passed to the build by environment variables.
3. **Closed testing rule.** A PERSONAL developer account made after Nov 2023 must run a closed test with at least
   **12 testers opted in for 14 days in a row** before production is unlocked. An ORGANIZATION account (needs a
   D-U-N-S number) is exempt. This decides the timeline.

## Steps
### A. Accounts (U)
- [ ] Google Play Console developer account (one-time 25 USD, identity verification; personal or organization).
- [ ] Create the app in Play Console: name, default language, game, free, the declarations.
- [ ] Payments profile (only if there will be paid items).

### B. Build for the store (C)
- [ ] AAB build (`BuildAndroid` option), versionCode raised on every upload.
- [ ] Release signing from a keystore outside the repo (U makes and keeps the key; C wires the build to it).
- [ ] Get under 200 MB (size report, then cuts or asset packs).
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
- [ ] Name, short description (80 chars), full description (4000), per language.
- [ ] Icon 512x512, feature graphic 1024x500, 4-8 phone screenshots (+ tablet ones).
- [ ] Privacy policy page (a public URL; can be a page in this public repo via GitHub Pages).
- [ ] Data safety form, content rating questionnaire (IARC), target audience (not children: avoids the Families
      policy), ads declaration.

### F. Launch (U)
- [ ] Internal testing track (instant, up to 100 testers) -> closed test (12+ testers x 14 days if personal
      account) -> production, possibly a staged rollout.
