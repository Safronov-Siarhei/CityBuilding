# Pre-release checklist

What must be checked before the game ships. Add a line when the user says «перед релизом»; strike it when done.

- [ ] **Storage caps vs prices** (the user, 2026-11-17): after the storage rework no price may exceed what the
      stores can hold -- Tax House 150 wood / 200 stone, Colosseum 90/80, Big Treasury 110 stone, Big Warehouse 80 /
      Big Granary 70 wood, Manor 55 wood, Moat 25 of a 20-cap resource, research up to 800 coins. The user re-balances
      after the review of every building's logic. Card: features/resources-and-storage.md.
- [ ] **Localization fit**: every language's texts fit their boxes (memory feedback_localization_fit_before_release).
- [ ] **Return notifications** (the user, 2026-11-21: «необходимое перед релизом в Google Play»). Nothing exists yet
      (no package, no code). Proposed, not agreed in detail: LOCAL notifications first (Unity Mobile Notifications,
      no server) for the daily bonus, a full piggy bank, «не заходил» 1/3/7 days, news with a reward; at most one a
      day, none 22:00-9:00, back off after 3 ignored, stop after 14 days; our own «сообщать, когда…?» window before
      Android 13's permission prompt at a good moment (not the first launch); a toggle per kind in the settings; a tap
      opens what it is about; texts in the localization sheet (2-3 per reason, the game's voice); the small status-bar
      icon = the white Depigo flower silhouette. «Склады полны / стройка готова» only if the town lives offline.
      Server pushes (Firebase) later, with the Invasion leaderboards. Open: offline progress? voice (herald or villagers)?
- [ ] **Tutorial offer**: how the tutorial is offered to the player; the collapse effect in all modes (features/tutorial.md).
