"""The main menu language picker's pictures (concept v2 approved 2026-11-14, Design/ui/LanguagePicker_Concept.png):
one flag per language, the chevron and the tick, plus the language list SetupProjectMainMenu reads.

    python Tools/ui/language_flags.py  ->  Assets/_Project/UI/Icons/Flags/Flag_<code>.png, Chevron.png, Tick.png, languages.json

The list holds the languages whose own name the game's font (Rubik: Latin + Cyrillic) can write.
Japanese, Korean and Chinese wait for a CJK font (the user, 2026-11-14: translations last).
A language without a column on the sheet shows the English text until it gets one.
"""
import json
import os
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import language_picker_concept as concept  # noqa: E402

OUT = concept.R + 'Assets/_Project/UI/Icons/Flags/'
# (code, flag, the language's own name), in the list's order
LANGS = [(c, f, n) for c, f, n in concept.LANGS if c not in ('ja', 'ko', 'zh')]


def chevron():
    S = 4
    im = Image.new('RGBA', (64 * S, 64 * S), 0)
    d = ImageDraw.Draw(im)
    d.polygon([(8 * S, 20 * S), (56 * S, 20 * S), (32 * S, 48 * S)], fill=(84, 50, 24))
    return im.resize((64, 64), Image.LANCZOS)


def tick():
    S = 4
    im = Image.new('RGBA', (64 * S, 64 * S), 0)
    ImageDraw.Draw(im).line((10 * S, 34 * S, 26 * S, 50 * S, 54 * S, 16 * S), fill=(46, 120, 30), width=10 * S, joint='curve')
    return im.resize((64, 64), Image.LANCZOS)


def main():
    os.makedirs(OUT, exist_ok=True)
    for code, f, _ in LANGS:
        concept.flag(f, 96, 64).save(OUT + f'Flag_{code}.png')
    chevron().save(OUT + 'Chevron.png')
    tick().save(OUT + 'Tick.png')
    with open(OUT + 'languages.json', 'w', encoding='utf-8') as fh:
        json.dump({'languages': [{'code': c, 'name': n} for c, _, n in LANGS]}, fh, ensure_ascii=False, indent=1)
    print(OUT, len(LANGS))


if __name__ == '__main__':
    main()
