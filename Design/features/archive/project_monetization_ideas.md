---
name: project-monetization-ideas
description: "The user's monetization ideas for CityBuilding (2026-10-14): rotating rewarded-ad offer icon for resources, paid HUD looks via IAP. Ideas only — nothing built yet."
metadata:
  node_type: memory
  type: project
  originSessionId: 44eb0858-0eb9-49b7-8631-b0eada6c380b
  modified: 2026-10-15T18:58:10.687Z
---

The user's monetization ideas, as of 2026-10-14. None of them is built yet.

1. **Rewarded-ad offer icon (2026-10-14).** From time to time an icon with a good offer pops up in a corner of the HUD. Tapping it offers resources in exchange for watching an ad. The offers rotate: different resources and amounts each time.
   - Open questions, not yet discussed: how often it appears, how long it stays, which corner (the HUD layout in [[project_ui_skin]] is the user's sketch, so ask where it goes), what an offer is worth relative to the economy ([[project_balance_audit]]; resources are tight on purpose), and the ad SDK (Unity Ads / LevelPlay / AdMob).
   - The amounts should live in the balance sheet ([[project_balance_pipeline]]), not in C#.
2. **Paid HUD looks (Classic/Soft/Glass/Dock)** via Unity IAP non-consumables, plus "Restore purchases". Open: Android only or iOS too; real purchase tests need a Google Play merchant account. See [[project_ui_skin]] round 4.

3. **Promo codes for a 5-star review (2026-10-15, idea only).** A limited batch (~1000) of codes for ad removal or extra content. A player gets a code by sending proof that they left a 5-star review. The user accepts the lost revenue as a cost of promoting the game.
   - My answer the same day: the goal is good, but this mechanism is **forbidden**. Google Play's ratings policy bans incentivising ratings. Apple's guidelines ban manipulating reviews. Tying the reward to 5 stars specifically makes it worse, and in the US (FTC 2024 rule) and the EU it is also a consumer-law issue. The risk is removed reviews, rating penalties and, at worst, the app pulled. A rating built this way also lies to the store's algorithm about retention.
   - Proposed instead, using the same pool of codes: hand them out for joining Discord or Telegram, to beta testers and content creators, at launch or as a referral reward. Ask for reviews with an in-app review prompt (Play In-App Review API / SKStoreReviewController), timed after a win and never paid for. The user has not answered yet.

**Why:** this is a mobile-first game ([[feedback_mobile_first_optimization]]), and the user is planning its revenue around rewarded ads and cosmetics.

**How to apply:** when either idea comes up, start from these notes. Don't build anything until the user asks.

Related discussion the same day: the user feels the sandbox lacks goals. A campaign of scenario levels with stars was proposed, with tiers brought back as milestones in the sandbox. That is still being discussed; nothing is decided.
