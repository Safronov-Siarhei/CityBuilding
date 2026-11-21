# The campaign village's ground textures (the user approved the concept 2026-11-21:
# Design/concepts/campaign_ground/compare_v1.png, made by Tools/blender/cinematic/ground_concept.py).
# Seamless tiles, written into Assets/_Project/Resources/CineGround/ and read by CineGround.shader
# (globals set by Campaign/CineGroundNoise):
#   Cobble.png  6 m tile -- rounded setts in loose rows, dark joints with moss, each stone its own shade,
#               the dome's light BAKED in (lit from the upper left), so the shader reads one texel.
#   GrassDetail.png 4 m tile -- a colour MULTIPLIER (stored /1.4, sRGB-encoded): round tufts a shade
#               lighter/darker and fine blades; the big grass zones stay the shader's own.
#   DirtDetail.png 4 m tile -- a colour MULTIPLIER (stored /2.0): soft lumps and round pale pebbles.
#   python Tools/textures/cine_ground_textures.py
import os

import numpy as np
from PIL import Image

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Project", "Resources", "CineGround")
rng = np.random.default_rng(20261121)


def hexc(h):
    return np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)], dtype=np.float64) / 255.0


def to_srgb(lin):
    lin = np.clip(lin, 0, 1)
    return np.where(lin <= 0.0031308, lin * 12.92, 1.055 * lin ** (1 / 2.4) - 0.055)


def to_lin(s):
    return np.where(s <= 0.04045, s / 12.92, ((s + 0.055) / 1.055) ** 2.4)


def smooth(t):
    return t * t * (3 - 2 * t)


def vnoise(n, cells):
    """Periodic value noise: n x n texels, `cells` lattice cells across the tile."""
    lat = rng.random((cells, cells))
    c = np.arange(n) * cells / n
    i = c.astype(int)
    t = smooth(c - i)
    i0, i1 = i % cells, (i + 1) % cells
    a = lat[np.ix_(i0, i0)] * (1 - t)[None, :] + lat[np.ix_(i0, i1)] * t[None, :]
    b = lat[np.ix_(i1, i0)] * (1 - t)[None, :] + lat[np.ix_(i1, i1)] * t[None, :]
    return a * (1 - t)[:, None] + b * t[:, None]


def fbm(n, cells, octaves):
    out, w, tot = np.zeros((n, n)), 0.5, 0.0
    for k in range(octaves):
        cc = cells << k
        if cc > n:
            break
        out += w * vnoise(n, cc)
        tot += w
        w *= 0.5
    return out / tot


def ramp(x, a, b):
    return np.clip((x - a) / (b - a), 0, 1)


def voronoi(n, gx, gy, jitter):
    """Periodic Voronoi over a gx x gy cell grid (isotropic in cell units; the tile is n x n texels).
    Returns F1 distance, distance to the nearest edge, and each texel's cell id."""
    pts = rng.random((gy, gx, 2)) - 0.5
    pts = 0.5 + pts * jitter
    ys, xs = np.mgrid[0:n, 0:n]
    u = (xs + 0.5) * gx / n
    v = (ys + 0.5) * gy / n
    cu, cv = np.floor(u).astype(int), np.floor(v).astype(int)
    best = np.full((n, n), 1e9)
    bpx = np.zeros((n, n))
    bpy = np.zeros((n, n))
    bid = np.zeros((n, n), dtype=int)
    cand = []
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            ix, iy = cu + dx, cv + dy
            p = pts[iy % gy, ix % gx]
            px, py = ix + p[..., 0], iy + p[..., 1]
            d = (px - u) ** 2 + (py - v) ** 2
            cand.append((px, py))
            take = d < best
            best = np.where(take, d, best)
            bpx = np.where(take, px, bpx)
            bpy = np.where(take, py, bpy)
            bid = np.where(take, (iy % gy) * gx + (ix % gx), bid)
    edge = np.full((n, n), 1e9)
    for px, py in cand:
        ex, ey = px - bpx, py - bpy
        ln = np.sqrt(ex * ex + ey * ey)
        ok = ln > 1e-6
        mx, my = (px + bpx) / 2 - u, (py + bpy) / 2 - v
        dist = np.where(ok, (mx * ex + my * ey) / np.where(ok, ln, 1), 1e9)
        edge = np.minimum(edge, dist)
    return np.sqrt(best), edge, bid


def save(rgb_srgb, name):
    os.makedirs(OUT, exist_ok=True)
    im = Image.fromarray((np.clip(rgb_srgb, 0, 1) * 255 + 0.5).astype(np.uint8), "RGB")
    im.save(os.path.join(OUT, name))
    print("written", name, im.size)


def cobble(n=1536, tile=6.0):
    gx, gy = 25, 36                                  # ~0.24 x 0.17 m setts (the concept's 4.2 / 1.45 squash)
    f1, edge, cid = voronoi(n, gx, gy, 0.45)
    rnd = rng.random(gx * gy)[cid]
    stops = [(0.0, hexc("#7F7B74")), (0.5, hexc("#B3AC9E")), (0.88, hexc("#9C968A")), (0.93, hexc("#A88A6C")), (1.0, hexc("#A88A6C"))]
    stone = np.zeros((n, n, 3))
    for (a, ca), (b, cb) in zip(stops, stops[1:]):
        m = (rnd >= a) & (rnd <= b)
        t = ((rnd - a) / max(b - a, 1e-6))[..., None]
        stone = np.where(m[..., None], ca * (1 - t) + cb * t, stone)
    wear = ramp(fbm(n, int(3 * tile), 4), 0.55, 0.75)[..., None] * 0.35
    stone = stone * (1 - wear) + hexc("#D8D2C4") * wear
    moss = ramp(fbm(n, int(1.2 * tile), 3), 0.5, 0.7)[..., None] * 0.7
    joint = hexc("#3E3A35") * (1 - moss) + hexc("#6A8A3C") * moss
    dome = smooth(ramp(edge, 0.025, 0.15))
    # the dome's light: height -> normal -> lit from the upper left (the play camera looks north)
    h = dome * 0.035
    texel = tile / n
    gx_, gy_ = np.gradient(h, texel)
    nrm = np.stack([-gx_, gy_, np.ones_like(h)], -1)
    nrm /= np.linalg.norm(nrm, axis=-1, keepdims=True)
    light = np.array([-0.45, 0.55, 0.7])
    light /= np.linalg.norm(light)
    shade = (0.62 + 0.5 * np.clip(nrm @ light, 0, 1))[..., None]
    col = (joint * (1 - dome[..., None]) + stone * dome[..., None]) * shade
    save(col, "Cobble.png")


def grass(n=512, tile=4.0):
    f1, _, cid = voronoi(n, 14, 14, 1.0)
    rnd = rng.random(14 * 14)[cid][..., None]
    tuft = ramp(-f1, -0.3, 0.0)[..., None]
    shade = np.array([0.80, 0.84, 0.74]) * (1 - rnd) + np.array([1.12, 1.15, 1.05]) * rnd
    mult = 1 * (1 - tuft) + shade * tuft
    blades = ramp(fbm(n, int(18 * tile), 3), 0.3, 0.7)[..., None]
    mult *= np.array([0.88, 0.9, 0.85]) * (1 - blades) + np.array([1.1, 1.12, 1.06]) * blades
    save(to_srgb(mult / 1.4), "GrassDetail.png")


def dirt(n=512, tile=4.0):
    lump = ramp(fbm(n, int(2.5 * tile), 5), 0.3, 0.7)[..., None]
    mult = np.array([1.08, 1.08, 1.08]) * (1 - lump) + np.array([0.86, 0.84, 0.82]) * lump
    f1, _, cid = voronoi(n, 36, 36, 1.0)
    pick = (rng.random(36 * 36)[cid] > 0.7)[..., None]
    peb = ramp(-f1, -0.16, -0.02)[..., None] * pick
    pale = to_lin(hexc("#C4BBA2")) / to_lin(hexc("#C19A62"))     # the pebble over the lane's mid colour
    mult = mult * (1 - peb) + pale * peb
    save(to_srgb(mult / 2.0), "DirtDetail.png")


cobble()
grass()
dirt()
