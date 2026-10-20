"""Draws the main menu in each of the HUD's four looks (Classic, Soft, Glass, Dock -- the look the
player picks in the settings), one folder of sprites per look, the same file names in each.

The user's word on the first try (2026-10-20): "дёшево выглядело всё, нужно просто больше деталей
и стиля объёмным элементам". So nothing here is a flat fill. Every surface is a height field lit
from the top left (bevels, domes, grooves), carries a material (wood grain, parchment fibre, glossy
paint, smoked glass) and a few small things that make it look made rather than drawn: nails and
carved rosettes on wood, iron brackets on the dock's planks, stitching and gloss on the soft look,
gold hairlines and filigree on glass. Each look keeps the palette its HUD already has.

    python make_menu_kit.py <unity project>

Writes Assets/_Project/Resources/MenuSkins/<Look>/<role>.png plus borders.json (left, bottom,
right, top -- 9-slice, in this texture's pixels). Drawn at twice the size they show at (imported at
200 pixels per unit). The panel is drawn to TILE: its middle and edges repeat (Image.Type.Tiled),
so its noise is periodic over the tiled span and the repeats meet without a seam.

Roles: round_main (the big Play button), round (shop, settings, back), card (the mode card's frame,
a hole the picture shows through), card_footer (the card's lower part, under its title), title (the
plate a name sits on), panel (a window), button (the call to action), plaque (a small label),
row / row_selected (a list entry), tab / tab_selected, divider, vignette (over the whole screen).
"""
import json
import math
import os
import sys

import numpy as np
from PIL import Image, ImageFilter

PROJECT = sys.argv[1] if len(sys.argv) > 1 else "."
ROOT = os.path.join(PROJECT, "Assets", "_Project", "Resources", "MenuSkins")

_L = np.array([-0.55, -0.75, 0.95])
LIGHT = _L / np.linalg.norm(_L)
FLAT = LIGHT[2]
_H = LIGHT + np.array([0.0, 0.0, 1.0])
HALF = _H / np.linalg.norm(_H)


# ================================================================ maths

def hexrgb(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)])


def ramp(t, stops):
    t = np.clip(t, 0.0, 1.0)
    pos = np.array([p for p, _ in stops])
    cols = np.array([hexrgb(c) for _, c in stops])
    out = np.zeros(np.shape(t) + (3,))
    for k in range(3):
        out[..., k] = np.interp(t, pos, cols[:, k])
    return out


def solid(shape, hexcol):
    return np.broadcast_to(hexrgb(hexcol), tuple(shape) + (3,)).copy()


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def cover(d):
    """Antialiased coverage of a signed distance (positive inside)."""
    return np.clip(d + 0.5, 0.0, 1.0)


def grid(W, H):
    y, x = np.mgrid[0:H, 0:W].astype(float)
    return x, y


def rrect(x, y, box, r):
    """Signed distance to a rounded rectangle, positive inside, in pixels."""
    x0, y0, x1, y1 = box
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    hx, hy = (x1 - x0) / 2, (y1 - y0) / 2
    r = min(r, hx, hy)
    qx = np.abs(x - cx) - (hx - r)
    qy = np.abs(y - cy) - (hy - r)
    out = np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - r
    return -out


def disc(x, y, cx, cy, r):
    return r - np.hypot(x - cx, y - cy)


def periodic_noise(h, w, cy, cx, seed, py=None, px=None, oy=0, ox=0):
    rng = np.random.default_rng(seed)
    py = py or h
    px = px or w
    g = rng.random((cy, cx))
    yy = ((np.arange(h) - oy) % py) / py * cy
    xx = ((np.arange(w) - ox) % px) / px * cx
    y0 = np.floor(yy).astype(int)
    x0 = np.floor(xx).astype(int)
    fy = yy - y0
    fx = xx - x0
    fy = fy * fy * (3 - 2 * fy)
    fx = fx * fx * (3 - 2 * fx)
    y1, x1 = (y0 + 1) % cy, (x0 + 1) % cx
    y0, x0 = y0 % cy, x0 % cx
    a, b = g[np.ix_(y0, x0)], g[np.ix_(y0, x1)]
    c, d = g[np.ix_(y1, x0)], g[np.ix_(y1, x1)]
    fx, fy = fx[None, :], fy[:, None]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def fbm(h, w, cells, octaves, seed, **kw):
    total, amp, norm = np.zeros((h, w)), 1.0, 0.0
    for o in range(octaves):
        total += amp * periodic_noise(h, w, max(1, int(cells[0] * 2 ** o)), max(1, int(cells[1] * 2 ** o)), seed + 31 * o, **kw)
        norm += amp
        amp *= 0.5
    return total / norm


def blur(a, radius):
    im = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8), "L")
    return np.asarray(im.filter(ImageFilter.GaussianBlur(radius))).astype(float) / 255.0


def lit(base, height, strength=1.0, spec=0.0, gloss=36, light_amount=1.0):
    """The base colour lit by a height field: brighter where it faces the light, darker away."""
    gy, gx = np.gradient(height)
    n = np.stack([-gx * strength, -gy * strength, np.ones_like(height)], axis=-1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    diff = (n * LIGHT).sum(-1)
    mult = 1.0 + light_amount * 1.6 * (diff - FLAT)
    s = np.clip((n * HALF).sum(-1), 0, 1) ** gloss * spec
    return np.clip(base * mult[..., None] + s[..., None], 0, 1)


class Sprite:
    """Straight-alpha RGBA canvas with 'over' compositing."""

    def __init__(self, W, H):
        self.W, self.H = W, H
        self.rgb = np.zeros((H, W, 3))
        self.a = np.zeros((H, W))
        self.x, self.y = grid(W, H)

    def over(self, rgb, a):
        a = np.clip(a, 0, 1)
        out = a + self.a * (1 - a)
        safe = np.where(out > 1e-6, out, 1.0)
        self.rgb = (rgb * a[..., None] + self.rgb * (self.a * (1 - a))[..., None]) / safe[..., None]
        self.a = out

    def shadow_hard(self, d_fn, dy, col, alpha=1.0):
        d = d_fn(self.x, self.y - dy)
        self.over(solid((self.H, self.W), col), cover(d) * alpha)

    def shadow_soft(self, d_fn, dy, radius, alpha):
        d = d_fn(self.x, self.y - dy)
        self.over(solid((self.H, self.W), "#1e1206"), blur(cover(d), radius) * alpha)

    def save(self, skin, name, border=None):
        img = np.dstack([np.clip(self.rgb, 0, 1), np.clip(self.a, 0, 1)[..., None]])
        Image.fromarray((img * 255 + 0.5).astype(np.uint8), "RGBA").save(os.path.join(skin["dir"], name + ".png"))
        skin["borders"][name] = border or [0, 0, 0, 0]


# ================================================================ materials

def wood_grain(W, H, along_x, seed, period=None, scale=1.0):
    if along_x:
        g = fbm(H, W, (int(10 * scale), 1), 4, seed, px=period) * 0.6 + fbm(H, W, (int(36 * scale), 3), 3, seed + 5, px=period) * 0.4
    else:
        g = fbm(H, W, (1, int(10 * scale)), 4, seed, py=period) * 0.6 + fbm(H, W, (3, int(36 * scale)), 3, seed + 5, py=period) * 0.4
    streak = np.sin(g * 30) * 0.5 + 0.5
    return np.clip(0.3 + 0.45 * g + 0.25 * streak, 0, 1)


def end_grain(x, y, cx, cy, seed):
    """Rings of a sawn log, wobbling a little -- the face of a round wooden button."""
    H, W = x.shape
    r = np.hypot(x - cx, y - cy)
    wob = fbm(H, W, (5, 5), 3, seed) * 14
    rings = np.sin((r + wob) / 5.5) * 0.5 + 0.5
    return np.clip(0.35 + 0.35 * rings ** 2 + (fbm(H, W, (30, 30), 2, seed + 3) - 0.5) * 0.25, 0, 1)


def parchment(W, H, seed, period=None, offset=0):
    kw = dict(py=period, px=period, oy=offset, ox=offset)
    f1 = fbm(H, W, (4, 4), 5, seed, **kw)
    f2 = fbm(H, W, (48, 8), 2, seed + 9, **kw)
    fibre = fbm(H, W, (3, 80), 2, seed + 4, **kw)
    spots = np.clip((fbm(H, W, (7, 7), 3, seed + 21, **kw) - 0.62) * 4, 0, 1)
    return np.clip(0.64 + (f1 - 0.5) * 0.32 + (f2 - 0.5) * 0.08 + (fibre - 0.5) * 0.1 - spots * 0.12, 0, 1)


WOOD = [(0.0, "#1f0f06"), (0.3, "#47260f"), (0.55, "#6b3d1b"), (0.8, "#8c5627"), (1.0, "#ad7237")]
WOOD_LIGHT = [(0.0, "#3a1f0c"), (0.4, "#6e4220"), (0.7, "#93602f"), (1.0, "#bc8546")]
WALNUT = [(0.0, "#120904"), (0.4, "#2c180b"), (0.75, "#47290f"), (1.0, "#64401f")]
PARCH = [(0.0, "#7e5c30"), (0.3, "#c2a06a"), (0.6, "#e3cb97"), (0.85, "#f2e1b5"), (1.0, "#fbf0d0")]
GOLD = [(0.0, "#3d2306"), (0.3, "#87561a"), (0.55, "#c99232"), (0.78, "#efc566"), (1.0, "#fff2c4")]
IRON = [(0.0, "#141414"), (0.4, "#33302c"), (0.7, "#57524b"), (1.0, "#8c857a")]
CREAM = [(0.0, "#d9ccb4"), (0.5, "#f3ead8"), (1.0, "#fffdf6")]
ORANGE = [(0.0, "#a8480a"), (0.45, "#ee8424"), (0.8, "#ffae4a"), (1.0, "#ffd58f")]
BLUE = [(0.0, "#1b5f8c"), (0.45, "#2f95c9"), (0.8, "#5cc7e8"), (1.0, "#a6e6f6")]


# ================================================================ details

def nails(sp, h, base, points, r, palette=IRON, dome=4.0):
    """Round-headed nails: a dome in the height field, iron in the colour, a dark ring round each."""
    for cx, cy in points:
        d = disc(sp.x, sp.y, cx, cy, r)
        inside = d > 0
        h += np.where(inside, np.sqrt(np.clip(1 - ((r - d) / r) ** 2, 0, 1)) * dome, 0)
        tone = 0.55 + 0.25 * np.clip((sp.y - cy) / r * -1, -1, 1)
        base[:] = np.where(inside[..., None], ramp(np.full(d.shape, 0.6) * 0 + tone, palette), base)
        ring = (d > -1.6) & (d <= 0.6)
        base[:] = np.where(ring[..., None], base * 0.35, base)


def rosette(sp, h, cx, cy, R, petals=8, depth=4.0):
    """A carved rosette: petals raised round a boss -- relief only, coloured by the wood under it."""
    dx, dy = sp.x - cx, sp.y - cy
    r = np.hypot(dx, dy) / R
    ang = np.arctan2(dy, dx)
    petal = np.clip(np.cos(ang * petals / 2) ** 2 * (1 - r) * 1.6 - 0.1, 0, 1) * (r < 1)
    boss = np.clip(1 - (r / 0.3) ** 2, 0, 1)
    ring = np.exp(-((r - 0.92) / 0.05) ** 2) * (r < 1.2)
    h += (petal * depth + boss * depth * 1.3 + ring * depth * 0.6)


def groove(h, d, at, width=1.3, depth=2.0):
    """An engraved line following a shape's edge, `at` pixels in from it."""
    h -= np.exp(-((d - at) / width) ** 2) * depth


def bead(h, d, at, width=2.2, height=2.0):
    h += np.exp(-((d - at) / width) ** 2) * height


def stitch(sp, d, at, every=12, on=7, col="#ffffff", alpha=0.85):
    """A dashed line following the edge, `at` px in -- stitching on cloth or a soft panel."""
    pos = (sp.x + sp.y)
    dash = ((pos % every) < on)
    line = np.exp(-((d - at) / 1.1) ** 2)
    sp.over(solid((sp.H, sp.W), col), line * dash * alpha)


def filigree(sp, cx, cy, sx, sy, size, col="#f0c46a", alpha=0.95):
    """A gold hairline corner ornament: an L with a curl at each end and a dot in the bend."""
    u = (sp.x - cx) * sx
    v = (sp.y - cy) * sy
    m = np.zeros_like(u)
    line = 1.15
    m = np.maximum(m, np.exp(-((v) / line) ** 2) * (u > 0) * (u < size))
    m = np.maximum(m, np.exp(-((u) / line) ** 2) * (v > 0) * (v < size))
    for a, b in ((size, 0), (0, size)):
        rr = np.hypot(u - a - (0 if a == 0 else -6), v - b - (0 if b == 0 else -6) + (6 if b == 0 else 0))
        m = np.maximum(m, np.exp(-((rr - 6) / line) ** 2) * ((u - a) * (1 if a else 0) + (v - b) * (1 if b else 0) > -6))
    dot = np.exp(-(((u - 12) ** 2 + (v - 12) ** 2) / 8))
    m = np.maximum(m, dot)
    sp.over(solid((sp.H, sp.W), col), m * alpha)


# ================================================================ shapes per look

def look_dir(skin):
    path = os.path.join(ROOT, skin)
    if os.path.isdir(path):
        for f in os.listdir(path):
            if f.endswith(".png") or f == "borders.json":
                os.remove(os.path.join(path, f))
    os.makedirs(path, exist_ok=True)
    return {"name": skin, "dir": path, "borders": {}}


def finish(skin):
    with open(os.path.join(skin["dir"], "borders.json"), "w", encoding="utf-8") as f:
        json.dump(skin["borders"], f, indent=1, sort_keys=True)
    print("MENU SKIN", skin["name"], len(skin["borders"]), "sprites")


# ---------------------------------------------------------------- wood (Classic and Dock)

def wood_frame(sp, d, frame_w, seed, period=None, plank=False):
    """Wood `frame_w` wide inside the shape `d`: grain along each side, a moulding of two grooves and a bead."""
    W, H = sp.W, sp.H
    x, y = sp.x, sp.y
    near_h = np.minimum(y, H - 1 - y) < np.minimum(x, W - 1 - x)
    g = np.where(near_h, wood_grain(W, H, True, seed, period), wood_grain(W, H, False, seed + 3, period))
    base = ramp(0.1 + g * 0.65, WOOD)
    h = np.zeros((H, W))
    t = np.clip(d / frame_w, 0, 1)
    h += np.sin(t * math.pi) ** 0.5 * 5
    groove(h, d, frame_w * 0.3, 1.4, 2.2)
    bead(h, d, frame_w * 0.62, 2.0, 2.2)
    groove(h, d, frame_w * 0.82, 1.1, 1.6)
    h += (g - 0.5) * 1.2
    if plank:
        # the dock's boards: seams across the frame every so often
        seam_x = (np.abs(((x + 40) % 120) - 60) < 1.4) & near_h
        seam_y = (np.abs(((y + 40) % 120) - 60) < 1.4) & ~near_h
        h -= (seam_x | seam_y) * 2.5
        base = np.where((seam_x | seam_y)[..., None], base * 0.45, base)
    return base, h


def classic_round_main(skin, S=480, dock=False):
    sp = Sprite(S, S)
    c = S / 2
    R = S * 0.42
    d_out = lambda x, y: disc(x, y, c, c - 6, R)
    sp.shadow_soft(d_out, 10, 10, 0.45)
    sp.shadow_hard(d_out, 12, "#2f1a0c")
    d = d_out(sp.x, sp.y)
    # wooden ring
    base = ramp(0.15 + end_grain(sp.x, sp.y, c, c - 6, 4) * 0.6, WOOD)
    h = np.zeros((S, S))
    ring_w = R * 0.26
    t = np.clip(d / ring_w, 0, 1)
    h += np.sin(t * math.pi) ** 0.6 * 7
    groove(h, d, ring_w * 0.5, 1.3, 2.0)
    # the gold face, domed, with rays engraved from its middle
    face = d > ring_w
    fr = (R - ring_w)
    rr = np.hypot(sp.x - c, sp.y - c + 6) / fr
    ang = np.arctan2(sp.y - c + 6, sp.x - c)
    rays = (np.cos(ang * 24) * 0.5 + 0.5) ** 6 * (rr > 0.55) * (rr < 0.92)
    dome = np.sqrt(np.clip(1 - rr ** 2, 0, 1)) * 16
    fh = dome - rays * 1.8
    groove(fh, d - ring_w, 7, 1.2, 2.0)
    gbase = ramp(np.full((S, S), 0.62) + (fbm(S, S, (10, 10), 3, 9) - 0.5) * 0.08, GOLD)
    base = np.where(face[..., None], gbase, base)
    h = np.where(face, fh + 7, h)
    # brass nails round the ring
    pts = [(c + math.cos(a) * (R - ring_w * 0.5), c - 6 + math.sin(a) * (R - ring_w * 0.5)) for a in np.linspace(0, 2 * math.pi, 12, endpoint=False)]
    nails(sp, h, base, pts, 7.5, palette=GOLD if not dock else IRON)
    rgb = lit(base, h, 1.0, spec=np.where(face, 0.55, 0.12) if False else 0.35, gloss=30)
    sp.over(solid((S, S), "#1c0e05"), cover(d + 3) * (d < 1.5))
    sp.over(rgb, cover(d))
    gloss = np.exp(-(((sp.x - c + 30) / (fr * 0.55)) ** 2 + ((sp.y - c + 6 + fr * 0.45) / (fr * 0.22)) ** 2)) * 0.35 * face
    sp.over(solid((S, S), "#fffbe8"), gloss)
    sp.save(skin, "round_main")


def classic_round(skin, S=300, dock=False):
    sp = Sprite(S, S)
    c = S / 2
    R = S * 0.42
    d_out = lambda x, y: disc(x, y, c, c - 5, R)
    sp.shadow_soft(d_out, 8, 7, 0.4)
    sp.shadow_hard(d_out, 9, "#2f1a0c")
    d = d_out(sp.x, sp.y)
    base = ramp(0.12 + end_grain(sp.x, sp.y, c, c - 5, 13) * 0.6, WOOD_LIGHT if not dock else WOOD)
    h = np.zeros((S, S))
    rim = R * 0.16
    h += np.sin(np.clip(d / rim, 0, 1) * math.pi / 2) * 6
    groove(h, d, rim, 1.2, 2.4)
    bead(h, d, rim + 5, 1.6, 1.4)
    pts = [(c + math.cos(a) * (R - rim * 0.5), c - 5 + math.sin(a) * (R - rim * 0.5)) for a in np.linspace(math.pi / 8, 2 * math.pi + math.pi / 8, 8, endpoint=False)]
    nails(sp, h, base, pts, 5.0, palette=GOLD if not dock else IRON, dome=3)
    rgb = lit(base, h, 1.0, spec=0.18, gloss=24)
    sp.over(solid((S, S), "#1c0e05"), cover(d + 3) * (d < 1.5))
    sp.over(rgb, cover(d))
    sp.save(skin, "round")


def classic_card(skin, W=440, H=660, border=120, dock=False):
    sp = Sprite(W, H)
    m = 10
    r = 10 if dock else 26
    box = (m, m, W - 1 - m, H - 1 - m - 8)
    d_fn = lambda x, y: rrect(x, y, box, r)
    sp.shadow_soft(d_fn, 8, 8, 0.45)
    sp.shadow_hard(d_fn, 8, "#2f1a0c")
    d = d_fn(sp.x, sp.y)
    fw = 46
    base, h = wood_frame(sp, d, fw, 21, plank=dock)
    # a gold lip inside the wood
    lip = (d >= fw) & (d < fw + 6)
    base = np.where(lip[..., None], ramp(np.full((H, W), 0.6), GOLD), base)
    h = np.where(lip, 3 + np.sin(np.clip((d - fw) / 6, 0, 1) * math.pi) * 2.5, h)
    corners = ((m + fw * 0.5 + 4, m + fw * 0.5 + 4), (W - 1 - m - fw * 0.5 - 4, m + fw * 0.5 + 4),
               (m + fw * 0.5 + 4, H - 1 - m - 8 - fw * 0.5 - 4), (W - 1 - m - fw * 0.5 - 4, H - 1 - m - 8 - fw * 0.5 - 4))
    if dock:
        # iron brackets over each corner, riveted
        for (cx, cy), (sx, sy) in zip(corners, ((1, 1), (-1, 1), (1, -1), (-1, -1))):
            u = (sp.x - (cx - sx * (fw * 0.5 + 4))) * sx
            v = (sp.y - (cy - sy * (fw * 0.5 + 4))) * sy
            arm = fw + 8
            br = ((u >= 0) & (v >= 0) & (u < 104) & (v < arm)) | ((u >= 0) & (v >= 0) & (v < 104) & (u < arm))
            base = np.where(br[..., None], ramp(0.45 + (fbm(H, W, (20, 20), 3, 3) - 0.5) * 0.3, IRON), base)
            dd = np.minimum(np.minimum(u, v), np.where(u < arm, 104 - v, 104 - u))
            dd = np.minimum(dd, np.where((u >= arm), arm - v, np.where(v >= arm, arm - u, 99)))
            h = np.where(br, 6 + np.clip(dd / 3, 0, 1) * 2, h)
            nails(sp, h, base, [(cx, cy), (cx + sx * 66, cy - sy * 2), (cx - sx * 2, cy + sy * 66)], 5.5, palette=IRON, dome=3)
    else:
        for cx, cy in corners:
            rosette(sp, h, cx, cy, fw * 0.48, petals=8, depth=3.2)
        mids = [(W / 2, m + fw * 0.5), (W / 2, H - 1 - m - 8 - fw * 0.5), (m + fw * 0.5, (H - 8) / 2), (W - 1 - m - fw * 0.5, (H - 8) / 2)]
        nails(sp, h, base, mids, 6.0, palette=GOLD, dome=3)
    rgb = lit(base, h, 1.0, spec=0.22, gloss=26)
    frame = (d >= 0) & (d < fw + 6)
    sp.a = np.where(d >= fw + 6, 0.0, sp.a)          # the window is clear: the picture shows, not the frame's shadow
    sp.over(solid((H, W), "#1c0e05"), cover(d + 3) * (d < 1.5))
    sp.over(rgb, cover(d) * frame)
    # the hairline shadow the frame throws on the picture
    inner = d - (fw + 6)
    sp.over(solid((H, W), "#000000"), np.exp(-np.clip(inner, 0, None) / 6) * (inner >= 0) * 0.55)
    sp.save(skin, "card", [border, border + 8, border, border])


def parchment_fill(sp, d, inset, seed, burn=48, period=None, offset=0):
    """Parchment inside a shape, its edge darkened as if singed, and a faint ink border ruled on it."""
    t = parchment(sp.W, sp.H, seed, period, offset)
    dd = d - inset
    t = t - smooth(burn, 0, dd) * 0.38 * (0.7 + 0.6 * fbm(sp.H, sp.W, (5, 5), 3, seed + 2, py=period, px=period, oy=offset, ox=offset))
    base = ramp(t, PARCH)
    rule = np.exp(-((dd - 22) / 1.0) ** 2) * 0.35 + np.exp(-((dd - 27) / 0.8) ** 2) * 0.2
    base = base * (1 - rule[..., None] * 0.6)
    return base


def classic_footer(skin, W=300, H=220, border=60):
    sp = Sprite(W, H)
    d = rrect(sp.x, sp.y, (0, 0, W - 1, H - 1), 0) + 40
    base = parchment_fill(sp, d, 0, 41, burn=30)
    sp.over(base, np.ones((H, W)))
    sp.save(skin, "card_footer", [border, border, border, border])


def classic_panel(skin, S=512, border=128, dock=False):
    sp = Sprite(S, S)
    m = 12
    r = 10 if dock else 30
    box = (m, m, S - 1 - m, S - 1 - m - 8)
    d_fn = lambda x, y: rrect(x, y, box, r)
    sp.shadow_soft(d_fn, 10, 10, 0.5)
    sp.shadow_hard(d_fn, 10, "#2f1a0c")
    d = d_fn(sp.x, sp.y)
    fw = 40
    period = S - 2 * border
    base, h = wood_frame(sp, d, fw, 51, period=period, plank=dock)
    lip = (d >= fw) & (d < fw + 5)
    base = np.where(lip[..., None], ramp(np.full((S, S), 0.6), GOLD), base)
    h = np.where(lip, 3 + np.sin(np.clip((d - fw) / 5, 0, 1) * math.pi) * 2, h)
    pfill = parchment_fill(sp, d, fw + 5, 61, period=period, offset=border)
    inside = d >= fw + 5
    base = np.where(inside[..., None], pfill, base)
    h = np.where(inside, (fbm(S, S, (60, 60), 2, 71, py=period, px=period, oy=border, ox=border) - 0.5) * 0.8, h)
    cpos = [(m + fw * 0.5 + 6, m + fw * 0.5 + 6), (S - 1 - m - fw * 0.5 - 6, m + fw * 0.5 + 6),
            (m + fw * 0.5 + 6, S - 1 - m - 8 - fw * 0.5 - 6), (S - 1 - m - fw * 0.5 - 6, S - 1 - m - 8 - fw * 0.5 - 6)]
    if dock:
        nails(sp, h, base, cpos, 7.0, palette=IRON, dome=3.5)
    else:
        for cx, cy in cpos:
            rosette(sp, h, cx, cy, fw * 0.5, petals=8, depth=3.0)
    rgb = lit(base, h, 1.0, spec=0.18, gloss=26)
    sp.over(solid((S, S), "#1c0e05"), cover(d + 3) * (d < 1.5))
    sp.over(rgb, cover(d))
    inner = d - (fw + 5)
    sp.over(solid((S, S), "#2a1606"), np.exp(-np.clip(inner, 0, None) / 4) * (inner >= 0) * 0.5)
    sp.save(skin, "panel", [border, border + 8, border, border])


def plate(skin, name, W, H, r, border, fill, rim, seed, dock=False, nail_ends=False, shadow=8, rim_w=7, rim_palette=GOLD, gloss_top=0.0):
    """A plate with a raised rim: buttons, plaques, tabs, rows, the title board."""
    sp = Sprite(W, H)
    m = shadow + 2
    box = (m, m, W - 1 - m, H - 1 - m - shadow)
    d_fn = lambda x, y: rrect(x, y, box, r)
    sp.shadow_soft(d_fn, 5, 5, 0.35)
    sp.shadow_hard(d_fn, shadow, "#2f1a0c")
    d = d_fn(sp.x, sp.y)
    if fill is WOOD or fill is WALNUT or fill is WOOD_LIGHT:
        g = wood_grain(W, H, True, seed, scale=0.6)
        fbase = ramp(0.12 + g * 0.6, fill)
        fh = (g - 0.5) * 1.5
    elif fill is PARCH:
        fbase = ramp(parchment(W, H, seed) - smooth(18, 0, d - rim_w) * 0.2, PARCH)
        fh = np.zeros((H, W))
    else:
        v = 1 - (sp.y - m) / (H - 2 * m)
        fbase = ramp(0.35 + 0.4 * v + (fbm(H, W, (3, 10), 3, seed) - 0.5) * 0.12, fill)
        fh = np.zeros((H, W))
    h = fh + np.clip((d - rim_w) / 10, 0, 1) * 3
    rimm = d < rim_w
    t = np.clip(d / rim_w, 0, 1)
    h = np.where(rimm, np.sin(t * math.pi) * 3.5, h)
    base = np.where(rimm[..., None], ramp(np.full((H, W), 0.6), rim_palette), fbase)
    groove(h, d, rim_w + 1, 0.9, 1.5)
    if nail_ends:
        cy = (box[1] + box[3]) / 2
        nails(sp, h, base, [(box[0] + rim_w + 16, cy), (box[2] - rim_w - 16, cy)], 6.5, palette=IRON if dock else GOLD, dome=3)
    rgb = lit(base, h, 1.0, spec=0.25, gloss=28)
    if gloss_top:
        gl = np.exp(-((sp.y - (box[1] + rim_w + 8)) / (H * 0.1)) ** 2) * (d > rim_w) * gloss_top
        rgb = np.clip(rgb + gl[..., None], 0, 1)
    sp.over(solid((H, W), "#1c0e05"), cover(d + 2.5) * (d < 1.5))
    sp.over(rgb, cover(d))
    sp.save(skin, name, [border, border + shadow, border, border])


def divider_ink(skin, col="#4a2c14", W=640, H=64, dots=True):
    sp = Sprite(W, H)
    c = H / 2
    u = np.abs(sp.x - W / 2)
    taper = np.clip(1 - u / (W / 2 - 24), 0, 1)
    line = np.exp(-((sp.y - c) / (0.5 + 2.0 * taper)) ** 2) * (u < W / 2 - 24)
    diamond = np.clip(1 - (np.abs(sp.x - W / 2) / 18 + np.abs(sp.y - c) / 13), 0, 1) * 4
    a = np.clip(line + diamond, 0, 1)
    if dots:
        for s in (-1, 1):
            for k, rr in ((40, 4.5), (62, 3.2), (W / 2 - 18, 4.0)):
                a = np.maximum(a, np.clip(rr - np.hypot(sp.x - (W / 2 + s * k), sp.y - c), 0, 1))
    sp.over(solid((H, W), col), a)
    sp.save(skin, "divider")


def vignette(skin, col="#120a04", strength=0.85, W=480, H=270):
    sp = Sprite(W, H)
    r = np.hypot((sp.x - W / 2) / (W / 2) * 0.9, (sp.y - H / 2) / (H / 2))
    sp.over(solid((H, W), col), smooth(0.55, 1.4, r) * strength)
    sp.save(skin, "vignette")


def wood_look(name, dock):
    s = look_dir(name)
    classic_round_main(s, dock=dock)
    classic_round(s, dock=dock)
    classic_card(s, dock=dock)
    classic_footer(s)
    classic_panel(s, dock=dock)
    r = 14 if dock else 40
    plate(s, "button", 380, 150, r, 64, GOLD, None, 3, dock=dock, rim_palette=WOOD, rim_w=9, gloss_top=0.18)
    plate(s, "title", 560, 140, 12 if dock else 22, 70, WOOD_LIGHT, None, 5, dock=dock, nail_ends=True, rim_palette=WOOD, rim_w=8)
    plate(s, "plaque", 300, 120, r, 52, WALNUT, None, 7, dock=dock, rim_w=6)
    plate(s, "row", 320, 140, 12 if dock else 26, 44, PARCH, None, 9, dock=dock, rim_w=5, rim_palette=WOOD_LIGHT, shadow=5)
    plate(s, "row_selected", 320, 140, 12 if dock else 26, 44, PARCH, None, 11, dock=dock, rim_w=8, shadow=5)
    plate(s, "tab", 280, 120, 12 if dock else 40, 52, WOOD_LIGHT, None, 13, dock=dock, rim_w=6, rim_palette=WOOD)
    plate(s, "tab_selected", 280, 120, 12 if dock else 40, 52, PARCH, None, 15, dock=dock, rim_w=8)
    divider_ink(s)
    vignette(s)
    finish(s)


# ---------------------------------------------------------------- Soft

def glossy(sp, d, palette, rim_w=8, rim_col="#ffffff", rays=0, seed=1):
    """Glossy paint: a vertical gradient, a soft inner shade at the bottom, a white rim, a gloss crescent on top."""
    H, W = sp.H, sp.W
    top = sp.y[np.where(d > 0)].min() if (d > 0).any() else 0
    bot = sp.y[np.where(d > 0)].max() if (d > 0).any() else H
    v = 1 - (sp.y - top) / max(1, bot - top)
    t = 0.3 + 0.55 * v
    if rays:
        cx = W / 2
        cy = (top + bot) / 2
        ang = np.arctan2(sp.y - cy, sp.x - cx)
        t = t + (np.cos(ang * rays) * 0.5 + 0.5) ** 3 * 0.06
    t = t + (fbm(H, W, (6, 6), 3, seed) - 0.5) * 0.05
    base = ramp(t, palette)
    inner = d - rim_w
    base = base * (1 - 0.25 * np.exp(-np.clip(inner, 0, None) / 10)[..., None] * smooth(0.4, 0.0, v)[..., None])
    rim = (d >= 0) & (d < rim_w)
    base = np.where(rim[..., None], solid((H, W), rim_col) * (0.92 + 0.08 * v[..., None]), base)
    sp.over(base, cover(d))
    crescent = np.clip(inner, 0, None)
    gl = smooth(0.62, 0.95, v) * (inner > 3) * 0.45 * smooth(0, 12, crescent)
    sp.over(solid((H, W), "#ffffff"), gl)
    line = np.exp(-((d - rim_w - 3) / 1.0) ** 2) * 0.35
    sp.over(solid((H, W), "#ffffff"), line)


def soft_round(skin, name, S, palette, square_r):
    sp = Sprite(S, S)
    m = 26
    box = (m, m - 6, S - 1 - m, S - 1 - m - 6)
    d_fn = (lambda x, y: rrect(x, y, box, square_r)) if square_r else (lambda x, y: disc(x, y, S / 2, S / 2 - 6, S / 2 - m))
    sp.shadow_soft(d_fn, 10, 12, 0.35)
    d = d_fn(sp.x, sp.y)
    glossy(sp, d, palette, rim_w=10, rays=16, seed=S)
    # a ring of small dots inside the rim -- the detail on a soft button
    inner = d - 22
    n = 28
    cx, cy = S / 2, S / 2 - 6
    ang = np.arctan2(sp.y - cy, sp.x - cx)
    dots = (np.cos(ang * n) > 0.6) * np.exp(-(inner / 2.2) ** 2)
    sp.over(solid((S, S), "#ffffff"), dots * 0.35)
    sp.save(skin, name)


def soft_card(skin, W=440, H=660, border=120):
    sp = Sprite(W, H)
    m = 22
    box = (m, m - 6, W - 1 - m, H - 1 - m - 6)
    d_fn = lambda x, y: rrect(x, y, box, 40)
    sp.shadow_soft(d_fn, 12, 14, 0.4)
    d = d_fn(sp.x, sp.y)
    fw = 30
    sp.a = np.where(d >= fw, 0.0, sp.a)
    v = 1 - sp.y / H
    base = ramp(0.55 + 0.35 * v + (fbm(H, W, (5, 5), 3, 3) - 0.5) * 0.06, CREAM)
    frame = (d >= 0) & (d < fw)
    sp.over(base, cover(d) * frame)
    # an orange band along the top edge, and a stitched line inside the frame
    band = (sp.y < box[1] + fw) & frame
    sp.over(ramp(np.full((H, W), 0.55), ORANGE), band * 0.9)
    stitch(sp, d, fw * 0.5, every=14, on=8, col="#e9a050", alpha=0.75)
    for cx, cy in ((box[0] + fw / 2, box[1] + fw / 2), (box[2] - fw / 2, box[1] + fw / 2),
                   (box[0] + fw / 2, box[3] - fw / 2), (box[2] - fw / 2, box[3] - fw / 2)):
        dd = disc(sp.x, sp.y, cx, cy, 8)
        sp.over(solid((H, W), "#ffffff"), cover(dd))
        sp.over(ramp(np.full((H, W), 0.6), ORANGE), cover(dd - 3))
    inner = d - fw
    sp.over(solid((H, W), "#5a3a20"), np.exp(-np.clip(inner, 0, None) / 7) * (inner >= 0) * 0.4)
    sp.save(skin, "card", [border, border + 6, border, border])


def soft_footer(skin, W=300, H=220, border=60):
    sp = Sprite(W, H)
    base = ramp(0.75 + (fbm(H, W, (4, 4), 3, 5) - 0.5) * 0.05, CREAM)
    dots = ((sp.x % 18 - 9) ** 2 + (sp.y % 18 - 9) ** 2) < 4
    base = base * (1 - dots[..., None] * 0.05)
    sp.over(base, np.ones((H, W)))
    sp.save(skin, "card_footer", [border, border, border, border])


def soft_panel(skin, S=512, border=128):
    sp = Sprite(S, S)
    m = 24
    box = (m, m - 6, S - 1 - m, S - 1 - m - 6)
    d_fn = lambda x, y: rrect(x, y, box, 52)
    sp.shadow_soft(d_fn, 12, 14, 0.4)
    d = d_fn(sp.x, sp.y)
    period = S - 2 * border
    base = ramp(0.72 + (fbm(S, S, (4, 4), 3, 7, py=period, px=period, oy=border, ox=border) - 0.5) * 0.05, CREAM)
    dots = (((sp.x - border) % 16 - 8) ** 2 + ((sp.y - border) % 16 - 8) ** 2) < 3.2
    base = base * (1 - dots[..., None] * 0.045)
    edge = d < 10
    base = np.where(edge[..., None], solid((S, S), "#ffffff"), base)
    sp.over(base, cover(d))
    stitch(sp, d, 24, every=14, on=8, col="#e3b47a", alpha=0.7)
    sp.save(skin, "panel", [border, border + 6, border, border])


def soft_plate(skin, name, W, H, r, border, palette, rim="#ffffff", rim_w=8, dots=False):
    sp = Sprite(W, H)
    m = 18
    box = (m, m - 4, W - 1 - m, H - 1 - m - 4)
    d_fn = lambda x, y: rrect(x, y, box, r)
    sp.shadow_soft(d_fn, 7, 8, 0.32)
    d = d_fn(sp.x, sp.y)
    glossy(sp, d, palette, rim_w=rim_w, rim_col=rim, seed=W + H)
    if dots:
        stitch(sp, d, rim_w + 9, every=12, on=6, col="#ffffff", alpha=0.55)
    sp.save(skin, name, [border, border, border, border])


def soft_look():
    s = look_dir("Soft")
    soft_round(s, "round_main", 480, ORANGE, 120)
    soft_round(s, "round", 300, BLUE, 80)
    soft_card(s)
    soft_footer(s)
    soft_panel(s)
    soft_plate(s, "button", 380, 150, 50, 66, ORANGE, dots=True)
    soft_plate(s, "title", 560, 140, 50, 70, ORANGE, dots=True)
    soft_plate(s, "plaque", 300, 120, 46, 54, CREAM, rim="#ffffff")
    soft_plate(s, "row", 320, 140, 30, 48, [(0, "#efe7d8"), (1, "#ffffff")], rim="#eadfca", rim_w=4)
    soft_plate(s, "row_selected", 320, 140, 30, 48, [(0, "#ffe3c0"), (1, "#fff6ea")], rim="#f08a24", rim_w=6)
    soft_plate(s, "tab", 280, 120, 46, 54, CREAM, rim="#ffffff")
    soft_plate(s, "tab_selected", 280, 120, 46, 54, ORANGE)
    divider_ink(s, col="#e58a2e")
    vignette(s, col="#2a1c10", strength=0.55)
    finish(s)


# ---------------------------------------------------------------- Glass

GOLD_LINE = "#f0c46a"


def glass_fill(sp, d, alpha, tint="#1c120a", seed=1, sheen=True):
    """Smoked glass: dark, see-through, frosted a little, with a sheen across its top."""
    H, W = sp.H, sp.W
    frost = fbm(H, W, (32, 32), 2, seed) - 0.5
    v = sp.y / H
    base = solid((H, W), tint) * (1.0 + frost[..., None] * 0.25 + (0.25 * (1 - v) * sheen)[..., None])
    sp.over(base, cover(d) * (alpha + frost * 0.04))
    if sheen:
        gleam = smooth(0.0, 0.5, 1 - v) * np.exp(-((sp.x - sp.y * 0.6 - W * 0.25) / (W * 0.12)) ** 2) * 0.08
        sp.over(solid((H, W), "#ffffff"), gleam * cover(d))


def hairline(sp, d, at, col=GOLD_LINE, alpha=0.9, width=1.1):
    sp.over(solid((sp.H, sp.W), col), np.exp(-((d - at) / width) ** 2) * alpha)


def glass_round(skin, name, S, main):
    sp = Sprite(S, S)
    c = S / 2
    R = S * 0.42
    d_fn = lambda x, y: disc(x, y, c, c - 4, R)
    sp.shadow_soft(d_fn, 8, 10, 0.35)
    d = d_fn(sp.x, sp.y)
    glass_fill(sp, d, 0.72, seed=S)
    hairline(sp, d, 2.5, alpha=0.95, width=1.4)
    hairline(sp, d, 12, alpha=0.5)
    # a dial of ticks between the two rings
    ang = np.arctan2(sp.y - c + 4, sp.x - c)
    n = 48 if main else 32
    ticks = (np.cos(ang * n) > 0.93) * (d > 4.5) * (d < 10)
    long = (np.cos(ang * n / 4) > 0.985) * (d > 4) * (d < 11)
    sp.over(solid((S, S), GOLD_LINE), np.clip(ticks * 0.6 + long * 0.9, 0, 1))
    # the reflection: an arc of light over the top
    arc = np.exp(-((np.hypot(sp.x - c, sp.y - c + 4) - R * 0.78) / (R * 0.05)) ** 2) * smooth(0.2, 0.9, -(sp.y - c) / R) * 0.18
    sp.over(solid((S, S), "#ffffff"), arc * (d > 0))
    sp.save(skin, name)


def glass_framed(skin, name, W, H, r, border, alpha, hole=False, filigree_size=0, tint="#1c120a", tiled=False):
    sp = Sprite(W, H)
    m = 18
    box = (m, m - 4, W - 1 - m, H - 1 - m - 4)
    d_fn = lambda x, y: rrect(x, y, box, r)
    sp.shadow_soft(d_fn, 8, 10, 0.35)
    d = d_fn(sp.x, sp.y)
    if hole:
        fw = 28
        sp.a = np.where(d >= fw, 0.0, sp.a)
        dd = np.where(d < fw, d, -1)
        glass_fill(sp, dd, alpha, tint=tint, seed=W)
        hairline(sp, d, fw, alpha=0.9)
        sp.over(solid((H, W), "#000000"), np.exp(-np.clip(d - fw, 0, None) / 6) * (d >= fw) * 0.45)
    else:
        # a tiled panel repeats its middle, so it gets no gleam (a gleam repeated is a checkerboard)
        # and frost whose period is the tiled span
        glass_fill(sp, d, alpha, tint=tint, seed=W, sheen=not tiled)
    hairline(sp, d, 2.5, alpha=0.95, width=1.3)
    hairline(sp, d, 9, alpha=0.4)
    if filigree_size:
        o = 16
        for cx, cy, sx, sy in ((box[0] + o, box[1] + o, 1, 1), (box[2] - o, box[1] + o, -1, 1), (box[0] + o, box[3] - o, 1, -1), (box[2] - o, box[3] - o, -1, -1)):
            filigree(sp, cx, cy, sx, sy, filigree_size)
    sp.save(skin, name, [border, border, border, border])


def glass_look():
    s = look_dir("Glass")
    glass_round(s, "round_main", 480, True)
    glass_round(s, "round", 300, False)
    glass_framed(s, "card", 440, 660, 30, 120, 0.82, hole=True, filigree_size=54)
    sp = Sprite(300, 220)
    glass_fill(sp, np.full((220, 300), 50.0), 0.84, seed=3)
    sp.save(s, "card_footer", [60, 60, 60, 60])
    glass_framed(s, "panel", 512, 512, 36, 128, 0.84, filigree_size=70, tiled=True)
    glass_framed(s, "button", 380, 150, 52, 66, 0.82, tint="#6a4612")
    glass_framed(s, "title", 560, 140, 20, 70, 0.8, filigree_size=26)
    glass_framed(s, "plaque", 300, 120, 44, 54, 0.7)
    glass_framed(s, "row", 320, 140, 22, 48, 0.35, tint="#3a2a1a")
    glass_framed(s, "row_selected", 320, 140, 22, 48, 0.6, tint="#7a5420")
    glass_framed(s, "tab", 280, 120, 44, 54, 0.45)
    glass_framed(s, "tab_selected", 280, 120, 44, 54, 0.7, tint="#7a5420")
    divider_ink(s, col=GOLD_LINE)
    vignette(s, col="#05030a", strength=0.75)
    finish(s)


def main():
    os.makedirs(ROOT, exist_ok=True)
    wood_look("Classic", dock=False)
    wood_look("Dock", dock=True)
    soft_look()
    glass_look()


main()
