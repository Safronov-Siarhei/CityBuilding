"""A whole sandbox archipelago from one seed (the user, 2026-10-29): the prototype of the in-game
generator's LAYOUT step. 500x500 m of sea with a stormy band round the edge («Бурное море», ships stop
at it); the player's start island near one side, the orcs' main island far across on the other, the
bot's island off to a flank between them, and 9-17 ordinary islands in the rest of the sea, each its
own kind and size, never closer than a strait a ship can pass (15-40 m of water between coasts).
No small orc camps for now -- only the main island (the user, 2026-10-29).

    python Tools/archipelago/archipelago_preview.py <out.png> [seed ...]

The orcs' island is never next door: its centre stands at least 55 % of the world from the start's.

Several seeds draw several maps side by side.
"""
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import islands_preview as isl  # noqa: E402

WORLD = 500               # the user, 2026-10-29: 500x500 (variant А) with smaller big islands (Б)
STORM = 24                # the stormy band round the edge, metres
STRAIT_MIN, STRAIT_MAX = 15, 40
STORM_COLOUR = (38, 52, 74)

# ordinary islands: (kind, frame size, weight)
ORDINARY = [('flat', 150, 2.0), ('hilly', 150, 2.0), ('rocky', 150, 1.5), ('long', 170, 1.5),
            ('crescent', 150, 1.0), ('atoll', 150, 0.6), ('islet', 70, 3.0)]


def footprint(land):
    """Centre offset and radius of an island's land inside its frame."""
    ys, xs = np.nonzero(land)
    cy, cx = ys.mean(), xs.mean()
    r = float(np.max(np.hypot(xs - cx, ys - cy)))
    return cx, cy, r


def layout(seed):
    """Places the islands by their real land, not a bounding circle: an island fits where its land
    stays inside the calm sea and no nearer than STRAIT_MIN to any other island's land."""
    rng = np.random.default_rng(seed)
    placed = []   # (kind, x, y, r, land, h, beach, cliffs, markers, (ox, oy))
    claimed = np.zeros((WORLD, WORLD), bool)   # land of placed islands grown by the strait
    calm = STORM + 6

    def fits(l, ox, oy):
        hgt, wid = l.shape
        if ox < 0 or oy < 0 or ox + wid > WORLD or oy + hgt > WORLD:
            return False
        ys, xs = np.nonzero(l)
        wx, wy = xs + ox, ys + oy
        if wx.min() < calm or wy.min() < calm or wx.max() >= WORLD - calm or wy.max() >= WORLD - calm:
            return False
        return not claimed[wy, wx].any()

    def claim(entry):
        nonlocal claimed
        l, (ox, oy) = entry[4], entry[9]
        ys, xs = np.nonzero(l)
        world = np.zeros((WORLD, WORLD), bool)
        world[ys + oy, xs + ox] = True
        claimed |= grow(world, STRAIT_MIN)
        placed.append(entry)

    def try_place(kind, size, s, positions):
        l, h, beach, cliffs, markers = isl.island(kind, size, s)
        cx, cy, r = footprint(l)
        for x, y in positions:
            ox, oy = int(round(x - cx)), int(round(y - cy))
            if fits(l, ox, oy):
                claim((kind, x, y, r, l, h, beach, cliffs, markers, (ox, oy)))
                return True
        return False

    # the three named islands: start on one side, orcs across, the bot on a flank between them
    side = rng.random() * math.tau
    c = WORLD / 2
    flank = math.pi / 2 * (1 if rng.random() < 0.5 else -1)
    for kind, size, turn in (('start', 130, 0.0), ('orc', 125, math.pi), ('bot', 120, flank)):
        def spots():
            for _ in range(60):
                a = side + turn + rng.normal(0, 0.25)
                d = rng.uniform(90, 200)    # pushed outward: the first that fits is taken, so try far first
                yield c + math.cos(a) * d, c + math.sin(a) * d
        for attempt in range(25):
            ordered = sorted(spots(), key=lambda p: -math.hypot(p[0] - c, p[1] - c))
            if kind == 'orc':
                sx, sy = placed[0][1], placed[0][2]
                ordered = [p for p in ordered if math.hypot(p[0] - sx, p[1] - sy) >= 0.55 * WORLD]
            if try_place(kind, size, seed * 1000 + attempt * 7 + len(placed), ordered):
                break
        else:
            raise RuntimeError(f"no room for the {kind} island (seed {seed})")

    # medium islands first, while there is sea for them, then islets in the gaps up to 12-20 in all
    total = int(rng.integers(12, 21))
    medium = [k for k in ORDINARY if k[0] != 'islet']
    weights = np.array([w for _, _, w in medium])
    want_medium = int(rng.integers(5, 10))
    tries = 0
    while len(placed) < 3 + want_medium and tries < 60:
        tries += 1
        kind, size, _ = medium[rng.choice(len(medium), p=weights / weights.sum())]
        size = int(size * rng.uniform(0.75, 1.0))      # the sea is small: medium islands vary down
        positions = [tuple(rng.uniform(calm, WORLD - calm, 2)) for _ in range(60)]
        try_place(kind, size, seed * 1000 + 500 + tries, positions)
    while len(placed) < total and tries < 160:
        tries += 1
        positions = [tuple(rng.uniform(calm, WORLD - calm, 2)) for _ in range(40)]
        try_place('islet', int(70 * rng.uniform(0.8, 1.3)), seed * 1000 + 500 + tries, positions)
    return placed


def render(seed, scale=2):
    placed = layout(seed)
    land = np.zeros((WORLD, WORLD), bool)
    world_img = None
    tiles = []
    for kind, x, y, r, l, h, beach, cliffs, markers, (ox, oy) in placed:
        tile = isl.paint(l, h, beach, cliffs, markers, seed)
        tiles.append((tile, l, ox, oy))
    img = Image.new("RGB", (WORLD, WORLD), isl.WATER_DEEP)
    for tile, l, ox, oy in tiles:  # noqa
        mask = Image.fromarray(np.where(grow(l, 7), 255, 0).astype(np.uint8))
        img.paste(tile, (ox, oy), mask)
    # the storm band, with a ragged inner edge
    a = np.array(img)
    rng = np.random.default_rng(seed + 5)
    edge = isl.fbm(WORLD, rng, base=20) * 10
    ys, xs = np.mgrid[0:WORLD, 0:WORLD]
    dist = np.minimum(np.minimum(xs, ys), np.minimum(WORLD - 1 - xs, WORLD - 1 - ys))
    storm = dist < STORM - 5 + edge
    a[storm] = STORM_COLOUR
    foam = (dist >= STORM - 5 + edge) & (dist < STORM - 3 + edge)
    a[foam] = (200, 214, 230)
    img = Image.fromarray(a).resize((WORLD * scale, WORLD * scale), Image.NEAREST)
    return img, placed


def grow(mask, n):
    out = mask.copy()
    for _ in range(n):
        out = out | np.roll(out, 1, 0) | np.roll(out, -1, 0) | np.roll(out, 1, 1) | np.roll(out, -1, 1)
    return out


def main():
    out = sys.argv[1]
    seeds = [int(s) for s in sys.argv[2:]] or [1, 2]
    font = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 22)
    small = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 16)
    maps = []
    for seed in seeds:
        img, placed = render(seed)
        kinds = {}
        for p in placed:
            kinds[p[0]] = kinds.get(p[0], 0) + 1
        maps.append((seed, img, len(placed), kinds))
    w = sum(m[1].size[0] for m in maps) + 20 * (len(maps) + 1)
    h = maps[0][1].size[1] + 90
    sheet = Image.new("RGB", (w, h), (242, 232, 205))
    draw = ImageDraw.Draw(sheet)
    x = 20
    names = {'flat': 'равн.', 'hilly': 'холм.', 'rocky': 'скал.', 'long': 'вытян.', 'crescent': 'полумес.',
             'atoll': 'атолл', 'islet': 'островок'}
    for seed, img, n, kinds in maps:
        draw.text((x, 8), f"Сид {seed}: островов {n}", fill=(59, 36, 20), font=font)
        sheet.paste(img, (x, 40))
        rest = ", ".join(f"{names[k]} {v}" for k, v in kinds.items() if k in names)
        draw.text((x, 46 + img.size[1]), "старт · орки · бот + " + rest, fill=(122, 85, 50), font=small)
        x += img.size[0] + 20
    sheet.save(out)
    print("saved", out, sheet.size)


if __name__ == '__main__':
    main()
