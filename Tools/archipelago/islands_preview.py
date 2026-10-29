"""Schematic island shapes for the sandbox archipelago (the user, 2026-10-29: «начать с утверждения
форм островов»). A prototype of the generator the game will run: every island is a base shape
(round, long, crescent, atoll ...) whose coastline is bent by fractal noise, so no two islands of a
kind share a shore; height then follows the kind (flat meadow, central hill, rocky ridges with
impassable mountains, a spine along a long island), and sandy BEACHES are laid on the gentle stretches
of coast -- every island gets at least one, since ships land only on beaches.

    python Tools/archipelago/islands_preview.py <out.png> [seed]

Draws a gallery: one row per kind, three seeds each, 1 px = 1 m.
"""
import math
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

WATER_DEEP = (47, 111, 176)
WATER_SHALLOW = (90, 169, 220)
SAND = (232, 211, 138)
GRASS = (124, 192, 79)
GRASS_DARK = (96, 166, 60)
FOREST = (62, 140, 63)
HILL = (167, 180, 106)
ROCK = (140, 135, 125)
ROCK_DARK = (108, 102, 94)
CLIFF = (90, 78, 64)
START = (244, 207, 90)
ORC = (178, 50, 30)
BOT = (44, 79, 168)


def value_noise(size, cell, rng):
    """Smooth noise: a random grid of `cell`-metre squares, interpolated with a smoothstep."""
    n = size // cell + 3
    grid = rng.random((n, n))
    ys, xs = np.mgrid[0:size, 0:size] / cell
    x0, y0 = xs.astype(int), ys.astype(int)
    fx, fy = xs - x0, ys - y0
    sx, sy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    a, b = grid[y0, x0], grid[y0, x0 + 1]
    c, d = grid[y0 + 1, x0], grid[y0 + 1, x0 + 1]
    return (a * (1 - sx) + b * sx) * (1 - sy) + (c * (1 - sx) + d * sx) * sy


def fbm(size, rng, base=48, octaves=4):
    total, amp, norm = np.zeros((size, size)), 1.0, 0.0
    cell = base
    for _ in range(octaves):
        total += amp * value_noise(size, max(2, cell), rng)
        norm += amp
        amp *= 0.5
        cell //= 2
    return total / norm


def island(kind, size, seed):
    """Returns (land mask, height 0..1, beach mask, markers) for one island of a kind."""
    rng = np.random.default_rng(seed)
    c = size / 2
    ys, xs = np.mgrid[0:size, 0:size].astype(float)
    angle0 = rng.random() * math.tau
    # the coast is bent twice: a slow wobble and a fine fringe (bays, capes, spits)
    warp = ((fbm(size, rng, base=size // 3) - 0.5) * 0.75 + (fbm(size, rng, base=size // 7) - 0.5) * 0.45
            + (fbm(size, rng, base=max(4, size // 18)) - 0.5) * 0.3)
    dx, dy = xs - c, ys - c
    ca, sa = math.cos(angle0), math.sin(angle0)
    u, v = dx * ca + dy * sa, -dx * sa + dy * ca   # the island's own axes, turned at random
    markers = []

    if kind in ('start', 'flat', 'hilly', 'rocky', 'orc', 'bot'):
        r = {'start': 0.33, 'flat': 0.33, 'hilly': 0.34, 'rocky': 0.32, 'orc': 0.38, 'bot': 0.33}[kind] * size
        squash = 1.0 + rng.random() * 0.35
        d = np.sqrt((u / squash) ** 2 + (v * squash * 0.9) ** 2) / r
    elif kind == 'long':
        d = np.sqrt((u / (0.46 * size)) ** 2 + (v / (0.14 * size)) ** 2)
        # a gentle bend, so a long island is not a sausage
        d = np.sqrt((u / (0.46 * size)) ** 2 + ((v - (u / size) ** 2 * size * 0.5) / (0.14 * size)) ** 2)
    elif kind == 'crescent':
        r0 = 0.32 * size
        ring = np.abs(np.sqrt(u ** 2 + v ** 2) - r0) / (0.11 * size)
        bite = np.clip((u / (0.4 * size)) + 0.35, 0, None)   # the open side of the moon
        d = ring + bite * 1.4
    elif kind == 'atoll':
        r0 = 0.34 * size
        thick = 0.06 + fbm(size, rng, base=size // 4) * 0.06     # the ring fattens and thins
        d = np.abs(np.sqrt(u ** 2 + v ** 2) - r0) / (thick * size)
        # one or two channels into the lagoon
        ang = np.arctan2(v, u)
        for _ in range(int(rng.integers(1, 3))):
            a = rng.random() * math.tau
            d = d + np.exp(-(((ang - a + math.pi) % math.tau - math.pi) / 0.12) ** 2) * 2.0
    elif kind == 'islet':
        d = np.sqrt(u ** 2 + v ** 2) / (0.16 * size)
    else:
        raise ValueError(kind)

    shore = d + warp            # < 1 is land
    land = shore < 1.0
    # keep the biggest piece only (a fringe can cut off specks of land)
    land = biggest(land)
    inland = np.clip(1.0 - shore, 0, 1)   # 0 at the coast, rising inland

    h = inland * 0.5
    detail = fbm(size, rng, base=size // 6)
    if kind in ('flat', 'start', 'bot', 'islet', 'atoll'):
        h = inland * 0.25 + detail * 0.08
    elif kind == 'hilly':
        h = inland ** 0.8 * 0.75 + detail * 0.2
    elif kind in ('rocky', 'orc'):
        ridges = 1 - np.abs(fbm(size, rng, base=size // 4) - 0.5) * 2
        h = inland ** 0.6 * 0.55 + ridges ** 3 * inland * 0.6
    elif kind == 'long':
        spine = np.exp(-((v - (u / size) ** 2 * size * 0.5) / (0.06 * size)) ** 2)
        h = spine * inland * 1.1 + detail * 0.15
    elif kind == 'crescent':
        h = inland * 0.5 + detail * 0.2
    h = np.where(land, np.clip(h, 0, 1), 0)

    beach = beaches(land, h, rng, kind)
    # cliffs: the coast that is not beach where high ground comes down to the sea
    high = h > 0.28
    for _ in range(9):
        high = high | np.roll(high, 1, 0) | np.roll(high, -1, 0) | np.roll(high, 1, 1) | np.roll(high, -1, 1)
    cliffs = coast_cells(land) & ~beach & high
    # two cells deep, so a cliff reads on the schematic
    inner = cliffs | np.roll(cliffs, 1, 0) | np.roll(cliffs, -1, 0) | np.roll(cliffs, 1, 1) | np.roll(cliffs, -1, 1)
    cliffs = inner & land & ~beach
    if kind == 'start':
        # the zone must fit: shrink it step by step on a narrow island rather than leave none
        spot, r = None, 0.13 * size
        while spot is None and r > 8:
            spot = flat_spot(land, h, beach, size, rng, radius=r)
            r *= 0.85
        markers.append(('start', spot))
    if kind == 'orc':
        markers.append(('orc', flat_spot(land, h, beach, size, rng, radius=0.10 * size, near_coast=True)))
    if kind == 'bot':
        markers.append(('bot', flat_spot(land, h, beach, size, rng, radius=0.10 * size)))
    return land, h, beach, cliffs, markers


def biggest(mask):
    """The largest 4-connected piece of a mask (a flood fill; islands are small enough)."""
    size = mask.shape[0]
    label = np.zeros(mask.shape, int)
    best, best_n, current = 0, 0, 0
    for y0 in range(size):
        for x0 in range(size):
            if not mask[y0, x0] or label[y0, x0]:
                continue
            current += 1
            stack, n = [(y0, x0)], 0
            label[y0, x0] = current
            while stack:
                y, x = stack.pop()
                n += 1
                for yy, xx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
                    if 0 <= yy < size and 0 <= xx < size and mask[yy, xx] and not label[yy, xx]:
                        label[yy, xx] = current
                        stack.append((yy, xx))
            if n > best_n:
                best, best_n = current, n
    return label == best


def coast_cells(land):
    pad = np.pad(land, 1)
    water_near = ~pad[:-2, 1:-1] | ~pad[2:, 1:-1] | ~pad[1:-1, :-2] | ~pad[1:-1, 2:]
    return land & water_near


def beaches(land, h, rng, kind):
    """Sand on 1-3 arcs of gentle coast, 3-6 m deep. Rocky islands get one, meadows up to three."""
    size = land.shape[0]
    coast = coast_cells(land)
    ys, xs = np.nonzero(coast)
    c = size / 2
    ang = np.arctan2(ys - c, xs - c)
    count = {'rocky': 1, 'orc': 2, 'islet': 1, 'atoll': 2}.get(kind, int(rng.integers(2, 4)))
    beach = np.zeros_like(land)
    for _ in range(count):
        centre = rng.random() * math.tau
        width = 0.35 + rng.random() * 0.45    # radians of coast
        arc = np.abs((ang - centre + math.pi) % math.tau - math.pi) < width / 2
        depth = int(3 + rng.integers(0, 4))
        for y, x in zip(ys[arc], xs[arc]):
            if h[y, x] > 0.5:
                continue                     # no sand under a cliff
            beach[max(0, y - depth):y + depth + 1, max(0, x - depth):x + depth + 1] = True
    beach &= land
    # sand only near the shore: distance to water within the depth
    near = np.zeros_like(land)
    w = ~land
    for _ in range(6):
        w = w | np.roll(w, 1, 0) | np.roll(w, -1, 0) | np.roll(w, 1, 1) | np.roll(w, -1, 1)
        near |= w & land
    return beach & near & (h < 0.45)


def flat_spot(land, h, beach, size, rng, radius, near_coast=False):
    """The flattest open patch big enough for a zone: start zone, orc base, bot town."""
    best, best_score = None, -1e9
    for _ in range(400):
        x, y = rng.integers(0, size, 2)
        if not land[y, x]:
            continue
        r = int(radius)
        y0, y1, x0, x1 = max(0, y - r), min(size, y + r), max(0, x - r), min(size, x + r)
        patch = land[y0:y1, x0:x1]
        r2 = r + 6         # the zone stands clear of the shore
        if y - r2 < 0 or x - r2 < 0 or y + r2 >= size or x + r2 >= size:
            continue
        if not land[y - r2:y + r2, x - r2:x + r2].all() and not near_coast:
            continue
        if near_coast and patch.mean() < 0.97:
            continue
        score = -h[y0:y1, x0:x1].std() * 5 - h[y, x]
        if near_coast:
            score -= min_dist_to_water(land, x, y) / size
        if score > best_score:
            best, best_score = (int(x), int(y), r), score
    return best


def min_dist_to_water(land, x, y):
    ys, xs = np.nonzero(~land)
    return float(np.min(np.hypot(xs - x, ys - y))) if len(xs) else 0.0


def paint(land, h, beach, cliffs, markers, rng_seed):
    size = land.shape[0]
    rng = np.random.default_rng(rng_seed + 99)
    img = np.zeros((size, size, 3), np.uint8)
    # water: shallow near the coast
    w = ~land
    shallow = np.zeros_like(land)
    grow = land.copy()
    for _ in range(7):
        grow = grow | np.roll(grow, 1, 0) | np.roll(grow, -1, 0) | np.roll(grow, 1, 1) | np.roll(grow, -1, 1)
    shallow = grow & w
    img[w] = WATER_DEEP
    img[shallow] = WATER_SHALLOW
    forest = fbm(size, rng, base=size // 7) > 0.56
    img[land] = GRASS
    img[land & (h > 0.18)] = GRASS_DARK
    img[land & forest & (h < 0.55)] = FOREST
    img[land & (h > 0.45)] = HILL
    img[land & (h > 0.62)] = ROCK
    img[land & (h > 0.78)] = ROCK_DARK
    img[beach] = SAND
    # cliffs: steep coast that is not beach
    coast = coast_cells(land)
    img[cliffs] = CLIFF
    im = Image.fromarray(img)
    draw = ImageDraw.Draw(im)
    for kind, spot in markers:
        if spot is None:
            continue
        x, y, r = spot
        colour = {'start': START, 'orc': ORC, 'bot': BOT}[kind]
        draw.ellipse([x - r, y - r, x + r, y + r], outline=colour, width=3)
        if kind == 'orc':
            draw.rectangle([x - 6, y - 6, x + 6, y + 6], fill=ORC)
        if kind == 'bot':
            draw.polygon([(x, y - 8), (x + 8, y + 6), (x - 8, y + 6)], fill=BOT)
    return im


KINDS = [
    ('start', 'Стартовый остров (жёлтая зона под Ратушу)', 200),
    ('orc', 'Остров главной базы орков (красный круг)', 200),
    ('bot', 'Остров поселения бота (синий круг)', 170),
    ('flat', 'Равнинный', 150),
    ('hilly', 'Холмистый', 150),
    ('rocky', 'Скалистый (горы непроходимы)', 150),
    ('long', 'Вытянутый (хребет по оси)', 170),
    ('crescent', 'Полумесяц (бухта)', 150),
    ('atoll', 'Атолл (кольцо с лагуной)', 150),
    ('islet', 'Островок', 70),
]


def main():
    out = sys.argv[1]
    seed = int(sys.argv[2]) if len(sys.argv) > 2 else 7
    variants = 3
    font = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 20)
    small = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 15)
    cell_w = 220
    rows = []
    for k, (kind, caption, size) in enumerate(KINDS):
        tiles = []
        for i in range(variants):
            s = seed * 1000 + k * 10 + i
            land, h, beach, cliffs, markers = island(kind, size, s)
            tiles.append((paint(land, h, beach, cliffs, markers, s), int(land.sum())))
        rows.append((caption, tiles))

    label_h, pad = 30, 10
    width = pad + variants * (cell_w + pad)
    height = pad + sum(label_h + max(t.size[1] for t, _ in tiles) + 24 + pad for _, tiles in rows) + 70
    sheet = Image.new("RGB", (width, height), (242, 232, 205))
    draw = ImageDraw.Draw(sheet)
    y = pad
    for caption, tiles in rows:
        draw.text((pad, y), caption, fill=(59, 36, 20), font=font)
        y += label_h
        tall = max(t.size[1] for t, _ in tiles)
        for i, (tile, area) in enumerate(tiles):
            x = pad + i * (cell_w + pad)
            frame = Image.new("RGB", (cell_w, tall), WATER_DEEP)
            frame.paste(tile, ((cell_w - tile.size[0]) // 2, (tall - tile.size[1]) // 2))
            sheet.paste(frame, (x, y))
            draw.text((x, y + tall + 2), f"суша ≈ {area:,} м²".replace(",", " "), fill=(122, 85, 50), font=small)
        y += tall + 24 + pad
    legend = [(SAND, 'пляж (высадка)'), (CLIFF, 'обрыв'), (GRASS, 'равнина'), (FOREST, 'лес'),
              (HILL, 'холм'), (ROCK, 'гора'), (WATER_SHALLOW, 'мелководье')]
    x = pad
    for colour, name in legend:
        draw.rectangle([x, y + 8, x + 18, y + 26], fill=colour, outline=(59, 36, 20))
        draw.text((x + 24, y + 8), name, fill=(59, 36, 20), font=small)
        x += 24 + int(draw.textlength(name, font=small)) + 18
    draw.text((pad, y + 36), "1 пиксель = 1 метр. Каждый столбец — свой сид: берег у каждого острова свой.", fill=(122, 85, 50), font=small)
    sheet.save(out)
    print("saved", out, sheet.size)


if __name__ == '__main__':
    main()
