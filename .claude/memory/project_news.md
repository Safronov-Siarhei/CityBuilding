---
name: project-news
description: Main-menu «Новости» (2026-11-17) -- rules decided, 3 window concepts drawn (A scroll / B board / C book), not built
metadata:
  type: project
---
The user wants news in the main menu fed by a config tab, no setup per item. Decided: the window opens by itself
from the player's 3rd entry (before that a red dot only); rewards = sandbox boosters + crystals; button icon = 4 «Письмо с печатью»; picture per item
undecided. Concepts in Design/ui/News_*.png (Tools/ui/news_concepts.py); card Design/features/news.md.
**Why:** players returning after an update should see what changed without the dev shipping UI per news.
**How to apply:** wait for the user's pick of A/B/C before building; keep the news.csv format the card proposes.
