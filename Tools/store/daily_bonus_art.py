"""The daily login bonus window's own art (the user approved the concept on 2026-11-11:
Design/store/DailyBonus_Mockup.png, drawn by daily_bonus_mockup.py).

    python -I Tools/store/daily_bonus_art.py

Writes Assets/_Project/UI/DailyBonus/: the day cards without words or crystals (Card_taken, Card_today,
Card_ahead, Card_big, Card_big_today), the amount pill (Pill), the taken tick (Tick), and layout.json:
every rect in the offer shop's 1920 x 1080 reference (y down), so SetupProjectDailyBonus places the
pieces where the concept has them. The window's frame, picture, title ribbon, close seal and buttons
are the offer shop's own (UI/Shop). Crystal amounts: the number LEFT, the icon RIGHT.
"""
import json
import os
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import daily_bonus_mockup as m  # noqa: E402

OUT = m.R + 'Assets/_Project/UI/DailyBonus/'
PAD = 30            # the card picture's margin round the card (its shadow, today's glow, the ribbon's top)
GAP = 26
ROWS = ((338, 566), (594, 822))
PILL = (160, 60)


def card(name, state, big):
    layout = json.load(open(m.SHOP + 'layout.json', encoding='utf-8'))
    sx0, sx1 = layout['strips'][0][0], layout['strips'][0][2]
    w = int((sx1 - sx0 - 4 * GAP) / 5)
    h = ROWS[0][1] - ROWS[0][0]
    img = Image.new('RGBA', (w + 2 * PAD, h + 2 * PAD), (0, 0, 0, 0))
    m.tile(img, (PAD, PAD, PAD + w, PAD + h), 0, 0, state, big, bare=True)
    img.save(OUT + name + '.png')
    return w, h


def pill():
    S = 2
    w, h = PILL[0] * S, PILL[1] * S
    img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    m.over(img, lambda g: g.rounded_rectangle((0, 0, w - 1, h - 1), h // 2, fill=(62, 36, 16, 255),
                                              outline=(176, 124, 52), width=3 * S))
    m.over(img, lambda g: g.rounded_rectangle((6 * S, 5 * S, w - 6 * S, h * 0.45), h // 3, fill=(255, 255, 255, 22)))
    img.resize(PILL, Image.LANCZOS).save(OUT + 'Pill.png')


def tick():
    S = 2
    br = 42 * S
    size = 2 * br + 12 * S
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    c = size / 2 - 2 * S
    m.over(img, lambda g: g.ellipse((c - br, c - br + 4 * S, c + br, c + br + 4 * S), fill=(20, 40, 10, 120)))
    d = ImageDraw.Draw(img)
    d.ellipse((c - br, c - br, c + br, c + br), fill=(80, 176, 64), outline=(255, 255, 255), width=5 * S)
    d.line([(c - 20 * S, c + 2 * S), (c - 5 * S, c + 17 * S), (c + 22 * S, c - 14 * S)], fill='white',
           width=11 * S, joint='curve')
    img.resize((size // S, size // S), Image.LANCZOS).save(OUT + 'Tick.png')
    return size // S


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, state, big in (('Card_taken', 'taken', False), ('Card_today', 'today', False), ('Card_ahead', 'ahead', False),
                             ('Card_big', 'ahead', True), ('Card_big_today', 'today', True)):
        w, h = card(name, state, big)
    pill()
    t = tick()

    shop = json.load(open(m.SHOP + 'layout.json', encoding='utf-8'))
    sx0, sx1 = shop['strips'][0][0], shop['strips'][0][2]
    step = (sx1 - sx0 - 4 * GAP) / 5 + GAP
    cards = [[round(sx0 + k * step), y0, round(sx0 + k * step) + w, y1] for (y0, y1) in ROWS for k in range(5)]

    def rect(cx, cy, rw, rh):
        return [round(cx - rw / 2), round(cy - rh / 2), round(cx + rw / 2), round(cy + rh / 2)]

    cy = h * 0.41
    px0, py0 = w / 2 - PILL[0] / 2, h - 42 - PILL[1] / 2
    s0, sy0, s1, sy1 = shop['strips'][3]
    by = (sy0 + sy1) / 2 + 36
    mid = (s0 + s1) / 2
    take = [round(mid - 476 - 30), round(by - 54), round(mid - 30), round(by + 55)]
    double = [round(mid + 30), round(by - 54), round(mid + 476 + 30), round(by + 55)]

    def on(b, x0, x1, half):
        return [b[0] + x0, round(by - 4 - half), b[0] + x1, round(by - 4 + half)]

    layout = {
        'reference': shop['reference'],
        'pad': PAD,
        # one key per card: Unity's JsonUtility reads no list of lists
        **{f'cards_{i}': c for i, c in enumerate(cards)},
        # card-relative (0,0 = the card's top-left, without the picture's margin)
        'card_crystal': rect(w / 2, cy, 98, 98),
        'card_crystal_big': rect(w / 2, cy, 122, 122),
        'card_tick': rect(w / 2, cy + 2, t, t),
        'card_day': rect(w / 2, -16 + 24, 156, 46),
        'card_pill': [round(px0), round(py0), round(px0 + PILL[0]), round(py0 + PILL[1])],
        'card_pill_text': [round(px0 + 8), round(py0), round(px0 + PILL[0] - 70), round(py0 + PILL[1])],
        'card_pill_crystal': [round(px0 + PILL[0] - 66), round(py0 + 4), round(px0 + PILL[0] - 14), round(py0 + 56)],
        'line': [sx0, 838, sx1, 886],
        'take': take,
        'double': double,
        # button-relative pieces in screen rects: «Забрать», the amount right-aligned, the crystal after it
        'take_label': on(take, 40, 320, 40),
        'take_amount': on(take, 300, 385, 40),
        'take_crystal': [take[0] + 390, round(by - 34), take[0] + 454, round(by + 30)],
        'double_play': [double[0] + 40, round(by - 34), double[0] + 104, round(by + 30)],
        'double_label': on(double, 120, 260, 40),
        'double_amount': on(double, 280, 385, 40),
        'double_crystal': [double[0] + 390, round(by - 34), double[0] + 454, round(by + 30)],
    }
    with open(OUT + 'layout.json', 'w', encoding='utf-8') as f:
        json.dump(layout, f, indent=1)
    print('ok', OUT, 'card', w, h)


if __name__ == '__main__':
    main()
