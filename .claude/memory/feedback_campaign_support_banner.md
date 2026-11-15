---
name: feedback_campaign_support_banner
description: every new campaign level must show the «Поддержи разработку» banner after a win (from level 2 on) -- call SupportOfferWindow.ShowAfterWin
metadata:
  type: feedback
---
Every campaign level, when won, shows the «Поддержи разработку» banner (from the 2nd level on, never after the 1st, replays too).
When setting up a new level: a `LevelNumber` const on its mode and `SupportOfferWindow.ShowAfterWin(LevelNumber, next)` on the win screen's way out instead of leaving directly. Mission 2 hooked in CityDefenceHud.

**Why:** the user, 2026-11-15: the campaign has no ads/paywall; this banner is its only monetization, and the campaign is set up from another device, so the rule must live in the repo (campaign.md RULE box, WORKING_RULES).
**How to apply:** check it on every new campaign level; see [[project_monetization_ideas]] and Design/features/campaign.md.
