"""
The loading screen's art (the user, 2026-11-10): full-screen painted maps of islands in the sea, the
lightly coloured look, one template per screen shape so nothing is stretched (Wide 20:9 for phones,
Tall 4:3 for tablets; LoadingScreen picks by the screen and covers it, cropping only sea at the
edges), the bottom kept as open sea for the course; and the course's pieces as UI sprites: the
dashed course, the sailed (red) course, the anchor, the cross, the flat ink galleon.

    python Tools/loading/loading_maps.py            (writes into Assets/_Project/UI/Loading)

Needs numpy, scipy, Pillow. The islands are invented, nothing to do with a mode's real map.
"""
import math, random, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont
from scipy import ndimage as ndi

FONT = 'D:/Fork/CityBuilding/Assets/_Project/Resources/Fonts/Rubik-Medium.ttf'
INK = (66, 40, 20)


def noise(h, w, cell, rng):
    gh, gw = max(2, h // cell + 2), max(2, w // cell + 2)
    g = Image.fromarray((rng.random((gh, gw)) * 255).astype(np.uint8))
    return np.asarray(g.resize((w + cell * 2, h + cell * 2), Image.BICUBIC), dtype=np.float32)[cell:cell + h, cell:cell + w] / 255.0


def fbm(h, w, cell, rng, octaves=5):
    out = np.zeros((h, w), np.float32)
    amp, tot = 1.0, 0.0
    for _ in range(octaves):
        out += amp * noise(h, w, max(2, int(cell)), rng)
        tot += amp
        amp *= 0.5
        cell /= 2
    return out / tot


def islands(h, w, rng, bar):
    """A land mask: a few big lands and islets, kept off the edges and the loading strip."""
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    warp_x = (fbm(h, w, 300, rng) - 0.5) * 140
    warp_y = (fbm(h, w, 300, rng) - 0.5) * 140
    warp_x += (fbm(h, w, 70, rng) - 0.5) * 70
    warp_y += (fbm(h, w, 70, rng) - 0.5) * 70
    X, Y = xx + warp_x, yy + warp_y
    field = np.full((h, w), -1.0, np.float32)
    lands = []
    usable_h = h - bar
    tries = 0
    want = int(rng.integers(6, 9))
    while len(lands) < want and tries < 600:
        tries += 1
        r = rng.uniform(0.07, 0.13) * h * (1.7 if len(lands) < 2 else 1.0)
        cx, cy = rng.uniform(0.08, 0.92) * w, rng.uniform(0.14, 0.86) * usable_h
        if any(math.hypot(cx - a, cy - b) < (r + rr) * 1.7 for a, b, rr in lands):
            continue
        lands.append((cx, cy, r))
    for cx, cy, r in lands:
        sx, sy = rng.uniform(0.7, 1.4), rng.uniform(0.7, 1.5)
        ang = rng.uniform(0, math.pi)
        dx, dy = X - cx, Y - cy
        u = (dx * math.cos(ang) + dy * math.sin(ang)) / (r * sx)
        v = (-dx * math.sin(ang) + dy * math.cos(ang)) / (r * sy)
        field = np.maximum(field, 1.0 - np.sqrt(u * u + v * v))
        # islets round it
        for _ in range(rng.integers(2, 6)):
            a = rng.uniform(0, 2 * math.pi)
            d = r * rng.uniform(1.15, 1.6)
            ir = r * rng.uniform(0.08, 0.2)
            ix, iy = cx + math.cos(a) * d * sx, cy + math.sin(a) * d * sy
            field = np.maximum(field, 1.0 - np.hypot(X - ix, Y - iy) / ir)
    field += (fbm(h, w, 120, rng) - 0.5) * 0.4 + (fbm(h, w, 36, rng, 3) - 0.5) * 0.35
    # off the frame and the strip
    edge = np.minimum.reduce([xx, w - 1 - xx, yy, (h - bar) - yy])
    # the compass rose's corner stays sea
    field -= np.clip(1 - np.hypot(xx - w * 0.07, yy - h * 0.12) / (h * 0.2), 0, 1) * 2.0
    field -= np.clip(1 - edge / 70.0, 0, 1) * 1.5
    land = field > 0
    land = ndi.binary_opening(land, iterations=2)
    lab, n = ndi.label(land)
    sizes = ndi.sum(land, lab, range(1, n + 1))
    for i, s in enumerate(sizes):
        if s < 120:
            land[lab == i + 1] = False
    return land


def lerp(a, b, t):
    t = t[..., None] if isinstance(t, np.ndarray) else t
    return np.asarray(a, np.float32) * (1 - t) + np.asarray(b, np.float32) * t


def paint(w, h, seed, coloured, tip, strip=True):
    rng = np.random.default_rng(seed)
    bar = int(h * 0.16)
    land = islands(h, w, rng, bar)
    sea_d = ndi.distance_transform_edt(~land).astype(np.float32)
    land_d = ndi.distance_transform_edt(land).astype(np.float32)
    stain = fbm(h, w, 260, rng)
    grain = fbm(h, w, 6, rng, 2)

    # ---- the paper and the sea
    paper = np.array((228, 210, 166) if not coloured else (222, 204, 160), np.float32)
    img = np.empty((h, w, 3), np.float32)
    img[:] = paper
    sea_tone = (206, 194, 160) if not coloured else (164, 168, 150)
    shallow_tone = (226, 212, 176) if not coloured else (192, 198, 170)
    sea = ~land
    halo = np.clip(1 - (sea_d - 18) / 14.0, 0, 1)
    sea_col = lerp(sea_tone, shallow_tone, halo)
    img[sea] = sea_col[sea]

    # ---- the land
    if coloured:
        inner = np.clip(land_d / 70.0, 0, 1)
        mix = np.clip(inner * 0.9 + (fbm(h, w, 90, rng) - 0.5) * 0.9, 0, 1)
        land_col = lerp((222, 178, 88), (156, 160, 74), mix)
        land_col = lerp(land_col, (196, 140, 64), np.clip(1 - land_d / 9.0, 0, 1) * 0.6)
    else:
        land_col = lerp((240, 226, 186), (226, 206, 160), np.clip(1 - land_d / 12.0, 0, 1))
    img[land] = land_col[land]

    # stains and age over everything
    img *= (0.9 + 0.14 * stain)[..., None]
    img *= (0.97 + 0.06 * grain)[..., None]

    # ---- line art on a 2x layer: hatching, coast, mountains, compass, ship
    S = 2
    ink = Image.new('L', (w * S, h * S), 0)
    d = ImageDraw.Draw(ink)
    brk = fbm(h, w, 40, rng)
    # sea hatching: horizontal strokes broken by noise, not on the shallows
    step = 9
    for y in range(4, h - bar + 30, step):
        x = 0
        while x < w:
            run = int(rng.integers(30, 160))
            gap = int(rng.integers(6, 40))
            x2 = min(w - 1, x + run)
            ys = y + int(rng.integers(-1, 2))
            seg = sea[ys % h, x:x2] & (sea_d[ys % h, x:x2] > 30) & (brk[ys % h, x:x2] > 0.38)
            # draw the contiguous stretches of the segment
            xs = np.flatnonzero(np.diff(np.concatenate(([0], seg.astype(np.int8), [0]))))
            for a, b in zip(xs[::2], xs[1::2]):
                if b - a > 6:
                    d.line([((x + a) * S, ys * S), ((x + b) * S, ys * S)], fill=110, width=S)
            x = x2 + gap
    # the shallow's outer line, dotted
    ring = (sea_d > 29) & (sea_d < 31)
    ys, xs = np.nonzero(ring)
    keep = (xs + ys) % 7 < 3
    for x, y in zip(xs[keep], ys[keep]):
        d.point((x * S, y * S), fill=150)
    ink_arr = np.asarray(ink, np.float32) / 255.0

    # coast: a heavy line + a thin echo offshore
    coast = np.clip(1 - np.abs(sea_d - 1.0) / 2.4, 0, 1) * land.__invert__() + np.clip(1 - land_d / 2.2, 0, 1) * land
    echo = np.clip(1 - np.abs(sea_d - 7.0) / 1.0, 0, 1) * 0.55
    big = lambda a: np.kron(a, np.ones((S, S), np.float32))

    # mountains and hills on the high ground
    rid = 1 - np.abs(2 * fbm(h, w, 160, rng, 3) - 1)
    elev = rid * np.clip(land_d / 45.0, 0, 1)
    draw2 = ImageDraw.Draw(ink)
    for gy in range(0, h, 26):
        for gx in range(0, w, 30):
            x = gx + int(rng.integers(-8, 9)); y = gy + int(rng.integers(-6, 7))
            if not (0 <= x < w and 0 <= y < h) or not land[y, x] or land_d[y, x] < 16:
                continue
            e = elev[y, x]
            if e > 0.8 and rng.random() < 0.8:
                mountain(draw2, x * S, y * S, S * rng.uniform(0.9, 1.4), rng)
            elif e > 0.62 and rng.random() < 0.25:
                hill(draw2, x * S, y * S, S)
    # lakes
    for _ in range(int(rng.integers(2, 5))):
        ys, xs = np.nonzero(land_d > 40)
        if len(xs) == 0:
            break
        k = int(rng.integers(len(xs)))
        r = rng.uniform(6, 13) * S
        cx, cy = xs[k] * S, ys[k] * S
        draw2.ellipse((cx - r * 1.4, cy - r, cx + r * 1.4, cy + r), outline=230, width=S * 2)
    compass(draw2, int(w * 0.07) * S, int(h * 0.12) * S, int(h * 0.09) * S, w * S, h * S)

    top, bottom = h - bar + 26, h - 26
    if strip:
        # the loading strip: two rope lines, waves between, the ship at ~40 %
        top, bottom = h - bar + 26, h - 26
        mid = (top + bottom) // 2
        left, right = int(w * 0.08), int(w * 0.92)
        for yy_ in (top, bottom):
            draw2.line([(left * S, yy_ * S), (right * S, yy_ * S)], fill=235, width=3 * S)
            draw2.line([(left * S, (yy_ + 5) * S), (right * S, (yy_ + 5) * S)], fill=120, width=S)
        for x in range(left + 10, right - 20, 34):
            y = mid + 16 + int(rng.integers(-2, 3))
            draw2.arc(((x) * S, (y - 6) * S, (x + 22) * S, (y + 6) * S), 200, 340, fill=170, width=S * 2)
        ship_x = left + (right - left) * 0.42
        # its wake behind it
        for k in range(1, 9):
            x = ship_x - 40 - k * 26
            draw2.arc(((x) * S, (mid + 12) * S, (x + 20) * S, (mid + 22) * S), 200, 340, fill=200, width=S * 2)
        ship(draw2, ship_x * S, (mid + 18) * S, (bottom - top) * 1.15 * S)


    line_art = np.asarray(ink.resize((w, h), Image.LANCZOS), np.float32) / 255.0
    line_art = np.maximum(line_art, np.maximum(coast, echo))
    ink_col = np.array(INK, np.float32)
    img = img * (1 - line_art[..., None]) + ink_col * line_art[..., None]

    # the strip's sea, a soft band under the ropes
    band = np.zeros((h, w), np.float32)
    if strip:
        band[top + 6:bottom, int(w * 0.08):int(w * 0.92)] = 0.12
    img = img * (1 - band[..., None]) + np.array((120, 90, 50), np.float32) * band[..., None]

    # worn, darker, slightly burnt edge
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    e = np.minimum.reduce([xx, w - 1 - xx, yy, h - 1 - yy])
    wob = fbm(h, w, 80, rng) * 60
    t = np.clip(1 - (e - wob * 0.3) / (90 + wob), 0, 1) ** 1.6
    img = img * (1 - 0.75 * t[..., None]) + np.array((110, 74, 38), np.float32) * (0.75 * t[..., None])
    burnt = (e < 10 + wob * 0.25)
    img[burnt] = img[burnt] * 0.45

    out = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8))
    if tip:
        dd = ImageDraw.Draw(out)
        dd.text((w // 2, h - bar - 22), tip, font=ImageFont.truetype(FONT, int(h * 0.03)), fill=INK, anchor='mm')
    return out


def mountain(d, x, y, s, rng):
    hgt, half = 16 * s, 14 * s
    peak = (x + rng.uniform(-3, 3) * s, y - hgt)
    d.line([(x - half, y), peak, (x + half, y)], fill=235, width=int(2 * s))
    # hatching on the shaded east slope
    for k in range(1, 5):
        t = k / 5
        px, py = peak[0] + (half * 0.9) * t, peak[1] + hgt * t
        d.line([(px, py), (px - 5 * s, py + 2 * s)], fill=170, width=max(1, int(s)))
    if rng.random() < 0.6:
        x2 = x + half * 1.1
        d.line([(x2 - half * 0.6, y), (x2, y - hgt * 0.6), (x2 + half * 0.6, y)], fill=210, width=int(1.6 * s))


def hill(d, x, y, s):
    d.arc((x - 10 * s, y - 7 * s, x + 10 * s, y + 7 * s), 200, 340, fill=170, width=int(1.5 * s))


def compass(d, cx, cy, r, w, h):
    for rr, wd, f in ((r, 4, 190), (r * 0.82, 2, 150), (r * 1.25, 2, 90)):
        d.ellipse((cx - rr, cy - rr, cx + rr, cy + rr), outline=f, width=wd)
    # faint rhumb lines across the sea
    for k in range(16):
        a = k * math.pi / 8
        d.line([(cx, cy), (cx + math.cos(a) * w * 1.2, cy + math.sin(a) * w * 1.2)], fill=40, width=2)
    for k in range(8):
        a = k * math.pi / 4 - math.pi / 2
        ln = r * (0.95 if k % 2 == 0 else 0.55)
        tip = (cx + math.cos(a) * ln, cy + math.sin(a) * ln)
        l = (cx + math.cos(a + 1.75) * r * 0.13, cy + math.sin(a + 1.75) * r * 0.13)
        rr_ = (cx + math.cos(a - 1.75) * r * 0.13, cy + math.sin(a - 1.75) * r * 0.13)
        d.polygon([tip, l, (cx, cy)], fill=230 if k % 2 == 0 else 160)
        d.polygon([tip, (cx, cy), rr_], outline=230, fill=60)


def ship(d, x, y, size):
    """A flat ink ship: a curved hull, two masts, bellied sails, a pennant."""
    s = size / 100.0
    hull = [(x - 48 * s, y - 14 * s), (x + 50 * s, y - 14 * s), (x + 62 * s, y - 26 * s), (x + 40 * s, y + 4 * s), (x - 38 * s, y + 4 * s), (x - 56 * s, y - 22 * s)]
    d.polygon(hull, fill=60, outline=235)
    d.line(hull + [hull[0]], fill=235, width=int(4 * s))
    for k in range(3):
        d.line([(x - 40 * s, y - 6 * s + k * 0), (x + 44 * s, y - 6 * s)], fill=235, width=int(2 * s))
    for mx, mh in ((x - 12 * s, 78), (x + 22 * s, 62)):
        d.line([(mx, y - 14 * s), (mx, y - mh * s)], fill=235, width=int(4 * s))
        top, bot = y - (mh - 8) * s, y - 22 * s
        sail = [(mx - 24 * s, top), (mx + 22 * s, top), (mx + 28 * s, (top + bot) / 2), (mx + 20 * s, bot), (mx - 22 * s, bot), (mx - 16 * s, (top + bot) / 2)]
        d.polygon(sail, fill=25, outline=235)
        d.line(sail + [sail[0]], fill=235, width=int(3 * s))
        d.line([(mx - 18 * s, (top + bot) / 2), (mx + 24 * s, (top + bot) / 2)], fill=150, width=int(2 * s))
    d.polygon([(x - 12 * s, y - 78 * s), (x + 8 * s, y - 73 * s), (x - 12 * s, y - 68 * s)], fill=235)
    d.line([(x + 50 * s, y - 14 * s), (x + 74 * s, y - 40 * s)], fill=235, width=int(3 * s))


# ---------------------------------------------------------------- the strip's pieces (UI sprites)
SHIP_INK = (66, 40, 20, 255)
SHIP_PAPER = (242, 228, 192, 255)
SHIP_HULL = (128, 86, 48, 255)
ROUTE_RED = (150, 46, 30, 255)


def bez(p0, p1, p2, n=16):
    return [((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0],
             (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]) for t in (i / n for i in range(n + 1))]


def galleon(d, x, y, size):
    """A flat ink galleon, waterline at y, facing right: curved hull with planks, gun ports and a
    stern castle, three masts with bellied square sails, rigging and red pennants."""
    s = size / 100.0
    INK_, PAPER_, HULL_ = SHIP_INK, SHIP_PAPER, SHIP_HULL
    P = lambda a, b: (x + a * s, y + b * s)
    bow, stern = P(66, -22), P(-60, -30)
    hull = bez(stern, P(-50, 8), P(-20, 8)) + bez(P(-20, 8), P(40, 10), bow)[1:] + [P(52, -14), P(-50, -14), P(-58, -36), P(-42, -36), P(-42, -26)]
    d.polygon(hull, fill=HULL_, outline=INK_)
    d.line(hull + [hull[0]], fill=INK_, width=int(3 * s))
    for k in (0, 1):
        yy = -6 + k * 7
        d.line(bez(P(-50, yy - 4), P(0, yy + 5), P(54, yy - 9)), fill=INK_, width=max(2, int(1.4 * s)))
    for k in range(5):
        cx = -30 + k * 17
        d.ellipse((x + (cx - 2.2) * s, y - 11.5 * s, x + (cx + 2.2) * s, y - 7.5 * s), fill=INK_)
    d.line([P(52, -18), P(88, -40)], fill=INK_, width=int(3 * s))
    for mx, mh in ((-34, 78), (0, 96), (32, 80)):
        d.line([P(mx, -14), P(mx, -mh)], fill=INK_, width=int(3.5 * s))
        for top, bot, half in ((-mh + 8, -mh * 0.58, 15 if mh < 90 else 18), (-mh * 0.55, -22, 19 if mh < 90 else 23)):
            l0, r0 = P(mx - half, top), P(mx + half, top)
            l1, r1 = P(mx - half - 3, bot), P(mx + half + 3, bot)
            sail = [l0, r0] + bez(r0, P(mx + half + 10, (top + bot) / 2), r1)[1:] + [l1] + bez(l1, P(mx - half + 6, (top + bot) / 2), l0)[1:]
            d.polygon(sail, fill=PAPER_, outline=INK_)
            d.line(sail + [sail[0]], fill=INK_, width=int(2.4 * s))
            d.line(bez(P(mx - half + 2, top * 0.4 + bot * 0.6), P(mx + 4, (top + bot) / 2 + 4), P(mx + half + 4, top * 0.4 + bot * 0.6)), fill=INK_, width=max(1, int(1.2 * s)))
            d.line([P(mx - half - 4, top), P(mx + half + 4, top)], fill=INK_, width=int(2.6 * s))
        d.polygon([P(mx, -mh), P(mx + 16, -mh + 3), P(mx + 10, -mh + 5), P(mx + 16, -mh + 7), P(mx, -mh + 8)], fill=(170, 60, 40, 255), outline=INK_)
    d.line([P(88, -40), P(32, -80)], fill=INK_, width=max(1, int(1.2 * s)))
    d.line([P(-60, -30), P(-34, -78)], fill=INK_, width=max(1, int(1.2 * s)))


def sprite(w, h, draw, S=3):
    img = Image.new('RGBA', (w * S, h * S), (0, 0, 0, 0))
    draw(ImageDraw.Draw(img), S)
    return img.resize((w, h), Image.LANCZOS)


# the course's shape, shared with LoadingScreen.cs: y = mid - AMP * sin(2 pi u), in a strip ROUTE_W x ROUTE_H
ROUTE_W, ROUTE_H, ROUTE_AMP = 1920, 80, 16


def route(dashed):
    def draw(d, S):
        pts = [(ROUTE_W * i / 400 * S, (ROUTE_H / 2 - ROUTE_AMP * math.sin(i / 400 * 2 * math.pi)) * S) for i in range(401)]
        for i in range(400):
            if dashed and (i // 5) % 2:
                continue
            d.line([pts[i], pts[i + 1]], fill=SHIP_INK if dashed else ROUTE_RED, width=(3 if dashed else 6) * S)
    return sprite(ROUTE_W, ROUTE_H, draw)


def anchor_sprite():
    def draw(d, S):
        c, r, w = 48 * S, 34 * S, 5 * S
        d.ellipse((c - r * 0.25, c - r, c + r * 0.25, c - r * 0.5), outline=SHIP_INK, width=w)
        d.line([(c, c - r * 0.5), (c, c + r * 0.75)], fill=SHIP_INK, width=w)
        d.line([(c - r * 0.42, c - r * 0.2), (c + r * 0.42, c - r * 0.2)], fill=SHIP_INK, width=w)
        d.arc((c - r * 0.75, c - r * 0.25, c + r * 0.75, c + r * 0.8), 15, 165, fill=SHIP_INK, width=w)
    return sprite(96, 96, draw)


def cross_sprite():
    def draw(d, S):
        c, r = 48 * S, 24 * S
        for k in (-1, 1):
            d.line([(c - r, c - r * k), (c + r, c + r * k)], fill=ROUTE_RED, width=8 * S)
    return sprite(96, 96, draw)


def ship_sprite():
    # 340 x 240, waterline 30 px above the bottom at x 160: the UI pivot is (160/340, 30/240)
    return sprite(340, 240, lambda d, S: galleon(d, 160 * S, 210 * S, 190 * S))


if __name__ == '__main__':
    import os
    root = sys.argv[1] if len(sys.argv) > 1 else 'Assets/_Project/UI/Loading'
    maps = os.path.join(root, 'Resources', 'LoadingMaps')
    os.makedirs(maps, exist_ok=True)
    # the user's pick (2026-11-10): the lightly coloured map; a template per screen shape, never stretched
    templates = (('Wide', (2400, 1080)), ('Tall', (2048, 1536)))
    seeds = (11, 23, 37)
    for name, (w, h) in templates:
        for k, seed in enumerate(seeds, 1):
            paint(w, h, seed, True, None, strip=False).save(os.path.join(maps, f'Map_{name}_{k}.png'), optimize=True)
            print('map', name, k)
    route(True).save(os.path.join(root, 'Route_Dashed.png'))
    route(False).save(os.path.join(root, 'Route_Sailed.png'))
    anchor_sprite().save(os.path.join(root, 'Route_Anchor.png'))
    cross_sprite().save(os.path.join(root, 'Route_Cross.png'))
    ship_sprite().save(os.path.join(root, 'Ship.png'))
    print('ok')
