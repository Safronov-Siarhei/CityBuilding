"""Invasion map 2 -- the New Year map «Новогодняя» (the user, 2026-10-25: «новогодняя карта для
нашествия, особенная, со своими нюансами»). The same structure as Invasion map 1 -- the base island
in the middle behind its river ring, three causeways, three lanes to three orc camps, three jungles
with their paths, monster camps and shrines -- because the run's rules lean on it (the user's rule:
the Invasion maps share their structure). What is its own:

- its ground drawn anew: other noise for the woods' edges, the meadows, the flowers and the beaches;
- its stone elsewhere in the jungles (each outer patch on the mirror bearing), the same amount;
- in the game: always winter (SeasonStyle), a fir forest only, falling snow, the decorated tree by
  the castle -- and the New Year rules (gifts and the thief, the blizzard, the midnight chimes and
  the Ice Shaman). See InvasionNewYear.

Not a copy of invasion1.py: it reads that file and changes these numbers, so a fix to the shared
machinery reaches both maps.

    blender --background --factory-startup --python invasion2.py -- <Inv-2.blend> [preview]
"""
import os

HERE = os.path.dirname(os.path.abspath(__file__))
SOURCE = os.path.join(HERE, "invasion1.py")

CHANGES = [
    ('PREFIX = "Inv-1"', 'PREFIX = "Inv-2"'),
    ("FLOWER_SEED, DRY_SEED = 501, 533", "FLOWER_SEED, DRY_SEED = 1201, 1233"),
    # the woods' edges, the groves' outline, the stony patches' outline
    ("jungle = clear + 1.2 * (fbm(x / 6, y / 6, 97) - 0.5)", "jungle = clear + 1.2 * (fbm(x / 6, y / 6, 1097) - 0.5)"),
    ("gr * (0.75 + 0.5 * fbm(x / 5, y / 5, 98))", "gr * (0.75 + 0.5 * fbm(x / 5, y / 5, 1098))"),
    # the meadows, the forest's heart, the lush banks, the beaches, the second shade
    ("meadow = fbm(x / 34, y / 34, 103)", "meadow = fbm(x / 34, y / 34, 1103)"),
    ("fbm(x / 7, y / 7, 109)", "fbm(x / 7, y / 7, 1109)"),
    ("fbm(x / 8, y / 8, 111)", "fbm(x / 8, y / 8, 1111)"),
    ("fbm(x / 16, y / 16, 117)", "fbm(x / 16, y / 16, 1117)"),
    ("0.6 - fbm(x / 3.2, y / 3.2, 119)", "0.6 - fbm(x / 3.2, y / 3.2, 1119)"),
    # the jungles' stone: each outer patch on the mirror bearing (the island's three stay: the houses
    # and the groves stand round them)
    ("[(*_polar(RAD(a), r), 3) for a, r in ((29.0, 80.0), (141.0, 80.0), (252.0, 37.0), (306.0, 84.0),\n"
     "                                                 (30.0, 32.0), (150.0, 32.0), (290.0, 30.0),\n"
     "                                                 (0.0, 60.0), (60.0, 60.0), (120.0, 60.0), (180.0, 60.0), (240.0, 60.0))]",
     "[(*_polar(RAD(-a), r), 3) for a, r in ((29.0, 80.0), (141.0, 80.0), (252.0, 37.0), (306.0, 84.0),\n"
     "                                                 (30.0, 32.0), (150.0, 32.0), (290.0, 30.0),\n"
     "                                                 (0.0, 60.0), (60.0, 60.0), (120.0, 60.0), (180.0, 60.0), (240.0, 60.0))]"),
    # the decor's scatter
    ("rnd = random.Random(31)", "rnd = random.Random(1231)"),
    ('print("INV1 ', 'print("INV2 '),
    ('print(f"INV1 ', 'print(f"INV2 '),
]

source = open(SOURCE, encoding="utf-8").read()
for old, new in CHANGES:
    count = source.count(old)
    assert count >= 1, f"invasion1.py no longer has: {old[:60]!r}"
    source = source.replace(old, new)
exec(compile(source, SOURCE, "exec"), {"__name__": "__main__", "__file__": SOURCE})
