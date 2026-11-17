# News in the main menu (Новости)

> **Status (2026-11-17): CONCEPTS drawn (A/B/C + the button), the user picks. NOT BUILT.**

## Rules (the user, 2026-11-17)
- A separate «Новости» button in the main menu; inside, every news item with its date, newest first; a tap opens
  its text; optional building icons and an optional reward.
- No setup/code per news item: the build takes every row of a config tab and lays the news out in date order.
- The window OPENS BY ITSELF on entering the game once the player has entered at least 3 times (and there is unread
  news); before that only a red dot on the button.
- Rewards: sandbox boosters and crystals.
- The look: concepts first, details discussed after.
- Button icon: variant 4 «Письмо с печатью» (the user, 2026-11-17).

## Proposed implementation (my proposal 2026-11-17, agreed in outline)
- Sheet tab «Новости» -> `Balance/news.csv`, one row = one item: `id, date (YYYY-MM-DD), title_ru, title_en,
  text_ru, text_en, icons (building ids / res:<id>), reward (crystal:5 / booster:<id>:1), picture (optional)`.
  The importer turns the texts into localization keys itself; a generic window lists whatever rows there are.
- Read state, entry count, claimed rewards: PlayerPrefs for now (player ID later).
- Stage B (later): fetch the published tab at start (short timeout, cached), so news ships without a build;
  future-dated rows hidden until their day; rewards from the fetched copy capped or disallowed (a reply can be forged).

## Concepts (Tools/ui/news_concepts.py -> Design/ui/)
- `News_Button.png` -- a round wood button with a scroll, third after settings and language, red dot + «Новости».
- `News_A_Scroll.png` A «Свиток»: dated tabs on the left, the open item on parchment right (text, icons, reward).
- `News_B_Board.png` B «Доска объявлений»: cork board with a pinned note per item; a tap opens it.
- `News_C_Book.png` C «Летопись»: an open leather book, contents left, the item right.

- `News_Icons.png` -- 8 glyph variants for the button (Tools/ui/news_icon_concepts.py): scroll, scroll with «!», newspaper, sealed letter, herald horn, bell, chronicle book, notice board.

## Open
- Which concept; a picture per item or not; whether the in-game Reference/notes feed the news text.
