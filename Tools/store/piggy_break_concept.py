"""
Piggy bank BREAK animation (the user, 2026-11-15: shake + cracks, bursts into shards with a flash and rays, a fountain
of crystals flying into the crystal counter which rolls up, «Уровень N», the next-level pig assembles from shards).
A concept for approval, over the approved window (Tools/store/piggy_window_concept.py), ~6.5 s:

  0.0-0.6  the window dims, the full pig flies to the centre and grows
  0.6-1.9  it shakes harder and harder, cracks spread over it in three steps (a tick of light on each)
  1.9-2.1  a white flash + golden rays
  2.0-3.2  the shards fly out spinning and falling; crystals burst up in a fountain
  2.4-4.0  the crystals stream into the counter (top left), which rolls up 1 250 -> 1 550
  3.8-4.9  «Уровень 2» pops in; bronze shards fly in and assemble the next pig, a flash when it is whole
  5.1-6.0  the dim lifts, the bronze pig goes back to its place in the window: 0 / 500, «Разбить от 150»

    python Tools/store/piggy_break_concept.py <renders folder>

<renders folder> holds piggy_royal_pink_alone.png, piggy_royal_bronze_alone.png, piggy_gem_pink.png.
Writes Design/store/Piggy_Break_Concept.webp (full-colour animated WebP, 1280 x 720).
"""
import math
import os
import random
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw, ImageFilter
from menu_shop_mockup import W, H, R, INK, CREAM, text, put, crystal, font, sparkle
from piggy_window_concept import window, trim, PX

FPS = 25
DUR = 6.6
CENTRE = (960, 560)          # the pig's centre during the break
HOME = (PX, 738)             # its feet in the window (centre x, bottom y)
PIG_W = 400                  # its width in the window
BIG = 1.35                   # how much it grows in the centre
COUNTER = (95, 70)           # the player's crystal counter, top left
GAIN = 300
START = 1250
OUT = R + 'Design/store/Piggy_Break_Concept.webp'


def ease(k):
    k = min(1, max(0, k))
    return k * k * (3 - 2 * k)


def back(k):      # overshoot then settle
    k = min(1, max(0, k))
    c = 1.9
    return 1 + (c + 1) * (k - 1) ** 3 + c * (k - 1) ** 2


def shards(im, n, seed):
    """Cuts a sprite into n Voronoi pieces: [(piece, centre x, centre y)] in the sprite's own pixels."""
    rnd = random.Random(seed)
    a = np.array(im)
    h, w = a.shape[:2]
    ys, xs = np.nonzero(a[:, :, 3] > 20)
    pts = [(xs[i], ys[i]) for i in (rnd.randrange(len(xs)) for _ in range(n))]
    gy, gx = np.mgrid[0:h, 0:w]
    dist = np.stack([(gx - px) ** 2 + (gy - py) ** 2 for px, py in pts])
    owner = dist.argmin(0)
    out = []
    for i in range(n):
        m = (owner == i) & (a[:, :, 3] > 0)
        if not m.any():
            continue
        yy, xx = np.nonzero(m)
        x0, x1, y0, y1 = xx.min(), xx.max() + 1, yy.min(), yy.max() + 1
        piece = a[y0:y1, x0:x1].copy()
        piece[:, :, 3] = np.where(m[y0:y1, x0:x1], piece[:, :, 3], 0)
        # a light edge on every shard reads as broken glaze
        p = Image.fromarray(piece)
        edge = p.getchannel('A').filter(ImageFilter.FIND_EDGES).point(lambda v: 160 if v > 40 else 0)
        p.alpha_composite(Image.merge('RGBA', [Image.new('L', p.size, 255)] * 3 + [edge]))
        out.append((p, (x0 + x1) / 2, (y0 + y1) / 2))
    return out


def cracks(size, seed):
    """Three steps of crack polylines from the top (the slot) down over the body."""
    rnd = random.Random(seed)
    w, h = size
    steps = []
    starts = [(w * 0.45, h * 0.22)]
    for step in range(3):
        lines = []
        new = []
        for sx, sy in starts:
            for _ in range(2 + step):
                ang = rnd.uniform(0.3, math.pi - 0.3)
                x, y = sx, sy
                pts = [(x, y)]
                for _ in range(rnd.randint(3, 5)):
                    ang += rnd.uniform(-0.6, 0.6)
                    l = rnd.uniform(w * 0.05, w * 0.1)
                    x, y = x + math.cos(ang) * l, y + math.sin(ang) * l * 0.8
                    pts.append((x, y))
                lines.append(pts)
                new.append(pts[-1])
        steps.append(lines)
        starts = new[:4]
    return steps


def crack_layer(pig, steps, upto):
    lay = Image.new('RGBA', pig.size, 0)
    d = ImageDraw.Draw(lay)
    for lines in steps[:upto]:
        for pts in lines:
            d.line(pts, fill=(255, 250, 235, 255), width=7, joint='curve')
            d.line(pts, fill=(70, 20, 30, 255), width=3, joint='curve')
    lay.putalpha(Image.composite(lay.getchannel('A'), Image.new('L', pig.size, 0), pig.getchannel('A').point(lambda v: 255 if v > 128 else 0)))
    return lay


def rays(img, c, r, alpha, turn):
    lay = Image.new('RGBA', img.size, 0)
    d = ImageDraw.Draw(lay)
    for i in range(18):
        a = i * math.pi / 9 + turn
        d.polygon([c, (c[0] + math.cos(a - 0.07) * r, c[1] + math.sin(a - 0.07) * r),
                   (c[0] + math.cos(a + 0.07) * r, c[1] + math.sin(a + 0.07) * r)], fill=(255, 226, 140, int(alpha)))
    img.alpha_composite(lay.filter(ImageFilter.GaussianBlur(5)))


def glow(img, c, r, col, alpha):
    g = Image.new('RGBA', (r * 2, r * 2), 0)
    ImageDraw.Draw(g).ellipse((r * 0.3, r * 0.3, r * 1.7, r * 1.7), fill=col + (int(alpha),))
    img.alpha_composite(g.filter(ImageFilter.GaussianBlur(r * 0.3)), (int(c[0] - r), int(c[1] - r)))


def counter(img, value, bump):
    d = ImageDraw.Draw(img)
    s = '{:,}'.format(int(value)).replace(',', ' ')
    size = int(50 * (1 + 0.18 * bump))
    text(d, COUNTER, s, size, fill=CREAM, anchor='lm', stroke=5)
    tw = d.textlength(s, font=font(size))
    ic = crystal(int(size * 1.25))
    put(img, ic, (COUNTER[0] + tw + 14 + ic.width / 2, COUNTER[1]))


def place_pig(img, p, k, cx, cy, rot=0.0, alpha=1.0):
    """The pig sprite p at scale k centred on (cx, cy)."""
    q = p.resize((max(1, int(p.width * k)), max(1, int(p.height * k))), Image.LANCZOS)
    if rot:
        q = q.rotate(rot, resample=Image.BICUBIC, expand=True)
    if alpha < 1:
        q.putalpha(q.getchannel('A').point(lambda v: int(v * alpha)))
    img.alpha_composite(q, (int(cx - q.width / 2), int(cy - q.height / 2)))


def main():
    src = sys.argv[1]
    pink = trim(Image.open(os.path.join(src, 'piggy_royal_pink_alone.png')).convert('RGBA'), w=PIG_W)
    bronze = trim(Image.open(os.path.join(src, 'piggy_royal_bronze_alone.png')).convert('RGBA'), w=PIG_W)
    gem = Image.open(os.path.join(src, 'piggy_gem_pink.png')).convert('RGBA')
    base1 = window(pink, gem, 300, 300, True, live=False)
    base2 = window(bronze, gem, 0, 500, False, live=False, price='$1.49')
    home1 = (HOME[0], HOME[1] - pink.height / 2)
    home2 = (HOME[0], HOME[1] - bronze.height / 2)
    steps = cracks(pink.size, 5)
    out_pieces = shards(pink, 22, 7)
    in_pieces = shards(bronze, 22, 9)
    rnd = random.Random(3)
    fly = []
    for p, sx, sy in out_pieces:
        ox, oy = sx - pink.width / 2, sy - pink.height / 2
        a = math.atan2(oy - 30, ox)
        v = rnd.uniform(700, 1200)
        fly.append((p, ox * BIG, oy * BIG, math.cos(a) * v, math.sin(a) * v - 500, rnd.uniform(-540, 540)))
    come = []
    for p, sx, sy in in_pieces:
        ox, oy = sx - bronze.width / 2, sy - bronze.height / 2
        a = rnd.uniform(0, 2 * math.pi)
        come.append((p, ox * BIG, oy * BIG, math.cos(a) * 900, math.sin(a) * 700, rnd.uniform(-400, 400), rnd.uniform(0, 0.35)))
    gems = []
    for i in range(30):
        gems.append((rnd.uniform(-math.pi * 0.85, -math.pi * 0.15), rnd.uniform(600, 1000), 2.0 + i * 0.02,
                     2.4 + i * 0.05, rnd.randint(46, 70), rnd.uniform(-30, 30)))
    small = trim(gem, h=60)
    frames = []
    for i in range(int(FPS * DUR)):
        t = i / FPS
        img = (base1 if t < 5.1 else base2).copy()
        # the dim
        dim = ease(t / 0.5) * (1 - ease((t - 5.1) / 0.5))
        if dim > 0:
            img.alpha_composite(Image.new('RGBA', (W, H), (12, 6, 24, int(205 * dim))))
        # the counter appears with the fountain and rolls up as crystals land
        landed = sum(1 for g in gems if t >= g[3] + 0.55)
        if 1.9 < t < 5.6:
            last = max([g[3] + 0.55 for g in gems if t >= g[3] + 0.55] or [-9])
            counter(img, START + GAIN * landed / len(gems), max(0, 1 - (t - last) / 0.15))
        if t < 1.95:
            # fly to the centre and grow, then shake harder and harder with cracks
            k = ease(t / 0.6)
            cx = home1[0] + (CENTRE[0] - home1[0]) * k
            cy = home1[1] + (CENTRE[1] - home1[1]) * k
            s = 1 + (BIG - 1) * k
            glow(img, (cx, cy), 380, (255, 214, 110), 110 + 60 * k)
            shake = ease((t - 0.6) / 1.3)
            dx = 14 * shake * math.sin(2 * math.pi * 11 * t)
            rot = 6 * shake * math.sin(2 * math.pi * 9 * t + 1)
            p = pink
            upto = 0 if t < 0.8 else 1 if t < 1.2 else 2 if t < 1.6 else 3
            if upto:
                p = p.copy()
                p.alpha_composite(crack_layer(pink, steps, upto))
            place_pig(img, p, s, cx + dx, cy, rot)
            for tick in (0.8, 1.2, 1.6):   # a tick of light on each new crack
                if tick <= t < tick + 0.12:
                    glow(img, (cx, cy - 60), 160, (255, 250, 230), 200 * (1 - (t - tick) / 0.12))
        if 1.9 <= t < 2.6:   # the flash and rays
            k = (t - 1.9) / 0.7
            rays(img, CENTRE, 900, 150 * (1 - k), t * 0.6)
            img.alpha_composite(Image.new('RGBA', (W, H), (255, 252, 240, int(255 * max(0, 1 - k * 2.5)))))
        if 1.95 <= t < 3.4:  # shards fly out spinning and falling
            tt = t - 1.95
            for p, ox, oy, vx, vy, spin in fly:
                x = CENTRE[0] + ox + vx * tt
                y = CENTRE[1] + oy + vy * tt + 1500 * tt * tt
                place_pig(img, p, BIG, x, y, spin * tt, max(0, 1 - tt / 1.4))
        for ang, v, t0, t1, h, tilt in gems:   # the fountain, then the stream to the counter
            if t0 <= t < t1 + 0.55:
                if t < t1:
                    tt = t - t0
                    x = CENTRE[0] + math.cos(ang) * v * tt
                    y = CENTRE[1] + math.sin(ang) * v * tt + 900 * tt * tt
                    sc = 1
                else:
                    tt = t - t0
                    fx = CENTRE[0] + math.cos(ang) * v * (t1 - t0)
                    fy = CENTRE[1] + math.sin(ang) * v * (t1 - t0) + 900 * (t1 - t0) ** 2
                    k = ease((t - t1) / 0.55)
                    x = fx + (COUNTER[0] + 160 - fx) * k
                    y = fy + (COUNTER[1] - fy) * k - 120 * math.sin(math.pi * k)
                    sc = 1 - 0.5 * k
                q = trim(small, h=int(h * sc)).rotate(tilt + t * 200, resample=Image.BICUBIC, expand=True)
                img.alpha_composite(q, (int(x - q.width / 2), int(y - q.height / 2)))
        if 3.8 <= t < 5.1:
            # «Уровень 2» and the bronze pig assembling from shards
            k = back((t - 3.8) / 0.4)
            d = ImageDraw.Draw(img)
            if t < 4.95:
                size = max(1, int(84 * k))
                text(d, (CENTRE[0], 205), 'Уровень 2', size, fill=(255, 214, 120), stroke=6)
            built = 4.75
            if t < built:
                for p, ox, oy, vx, vy, spin, delay in come:
                    kk = ease((t - 3.95 - delay) / 0.55)
                    if kk <= 0:
                        continue
                    x = CENTRE[0] + ox + vx * (1 - kk)
                    y = CENTRE[1] + oy + vy * (1 - kk)
                    place_pig(img, p, BIG, x, y, spin * (1 - kk), kk)
            else:
                glow(img, CENTRE, 420, (255, 210, 140), 160 * max(0, 1 - (t - built) / 0.3))
                place_pig(img, bronze, BIG * (1 + 0.08 * math.sin(min(1, (t - built) / 0.3) * math.pi)), *CENTRE)
                if t < built + 0.15:
                    img.alpha_composite(Image.new('RGBA', (W, H), (255, 250, 230, int(160 * (1 - (t - built) / 0.15)))))
        if t >= 5.1:   # back to its place in the window
            k = ease((t - 5.1) / 0.6)
            cx = CENTRE[0] + (home2[0] - CENTRE[0]) * k
            cy = CENTRE[1] + (home2[1] - CENTRE[1]) * k
            place_pig(img, bronze, BIG + (1 - BIG) * k, cx, cy)
            if t < 5.6:
                d = ImageDraw.Draw(img)
                counter(img, START + GAIN, 0)
        if 4.9 <= t < 5.4:
            d = ImageDraw.Draw(img)
            for j in range(6):
                a = j * math.pi / 3 + t * 3
                sparkle(d, CENTRE[0] + math.cos(a) * 300, CENTRE[1] + math.sin(a) * 200, 18)
        frames.append(img.convert('RGB').resize((1280, 720), Image.LANCZOS))
    hold = [frames[-1]] * int(FPS * 0.8)
    frames[0].save(OUT, save_all=True, append_images=frames[1:] + hold, duration=1000 // FPS, loop=0, quality=88, method=4)
    print('written', os.path.basename(OUT), len(frames) + len(hold), 'frames')


if __name__ == '__main__':
    main()
