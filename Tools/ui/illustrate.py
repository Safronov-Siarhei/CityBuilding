"""Turns the loading screen's photographs from the game into pictures that look drawn (the user,
2026-10-25: «прикольный загрузочный экран, который нарисован»):

- an Invasion scene (playtest_shots/loading/LoadRaw_<theme>.png, MenuArtBake.Bake_TheLoadingPictures) is
  flattened into a few tones per colour, given an ink outline where the tones meet, laid on warm
  paper and darkened at the corners -> UI/Loading/Load_<theme>.png;
- a sandbox map's ink-on-parchment chart (LoadInk_<map>.png, already drawn by the game's own
  parchment fog) is cut to the square of the map -> UI/Loading/Ink_<map>.png.

    python illustrate.py <unity project>
"""
import glob
import os
import sys

import numpy as np
from PIL import Image, ImageFilter

PROJECT = sys.argv[1] if len(sys.argv) > 1 else "."
DIR = os.path.join(PROJECT, "Assets", "_Project", "UI", "Loading")
RAW = os.path.join(PROJECT, "playtest_shots", "loading")   # MenuArtBake's raw shots, not in git
INK = np.array([52, 30, 14], dtype=float) / 255.0


def paper(w, h, seed=7):
    """Warm paper: a soft blotchy grain, values near 1 so it tints by multiplying."""
    rng = np.random.default_rng(seed)
    grain = rng.normal(0.0, 1.0, (h // 4 + 1, w // 4 + 1))
    img = Image.fromarray(((grain - grain.min()) / (np.ptp(grain) + 1e-6) * 255).astype(np.uint8))
    img = img.resize((w, h), Image.BICUBIC).filter(ImageFilter.GaussianBlur(3))
    g = np.asarray(img, dtype=float) / 255.0
    fine = rng.normal(0.0, 1.0, (h, w)) * 0.025
    base = np.stack([np.full((h, w), 1.0), np.full((h, w), 0.965), np.full((h, w), 0.9)], axis=-1)
    return np.clip(base * (0.93 + 0.07 * g[..., None]) + fine[..., None], 0, 1)


def illustrate(src, dst):
    img = Image.open(src).convert("RGB")
    w, h = img.size
    # flatten: smooth the tones into patches (a wide palette, so every colour keeps its hue --
    # 40 colours turned the King's purple and the blue roofs brown)
    flat = img.filter(ImageFilter.MedianFilter(5)).filter(ImageFilter.MedianFilter(5))
    flat = flat.quantize(colors=200, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGB")
    flat = flat.filter(ImageFilter.ModeFilter(5))
    a = np.asarray(flat, dtype=float) / 255.0
    # the ink: where the photograph's shapes meet (its own edges, sharper than the flattened ones)
    gray = img.convert("L").filter(ImageFilter.GaussianBlur(1.0))
    edges = np.asarray(gray.filter(ImageFilter.FIND_EDGES), dtype=float) / 255.0
    ink = np.clip((edges - 0.05) * 7.0, 0, 1)
    ink = np.asarray(Image.fromarray((ink * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(0.6)), dtype=float) / 255.0 * 0.9
    a = a * (1 - ink[..., None]) + INK * ink[..., None]
    # warm it a little, lay it on paper
    a = np.clip(a * 1.06, 0, 1) * np.array([1.03, 1.0, 0.94])
    a = a * paper(w, h)
    # darker corners
    yy, xx = np.mgrid[0:h, 0:w]
    d = np.sqrt(((xx - w / 2) / (w / 2)) ** 2 + ((yy - h / 2) / (h / 2)) ** 2)
    a = a * (1 - 0.28 * np.clip(d - 0.55, 0, 1)[..., None])
    Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8)).save(dst)
    print("ILLUSTRATED", dst)


def crop_chart(src, dst):
    """The chart is photographed square from above in a 16:10 frame: the map is the middle square."""
    img = Image.open(src).convert("RGB")
    w, h = img.size
    side = int(h * 1.02)
    left = (w - side) // 2
    img.crop((left, 0, left + side, h)).save(dst)
    print("CHART", dst)


def quill(dst, size=256):
    """The pen that draws the chart: a cream feather on a dark shaft, its nib at the bottom left
    (the sprite's pivot is set there, so it writes where it stands)."""
    from PIL import ImageDraw
    S = size * 4
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    import math
    ang = math.radians(50)
    ux, uy = math.cos(ang), -math.sin(ang)          # along the shaft, up and right
    px, py = -uy, ux
    nib = (S * 0.08, S * 0.92)

    def at(t, s):
        return (nib[0] + ux * t * S + px * s * S, nib[1] + uy * t * S + py * s * S)

    # the vane: a long leaf either side of the shaft's upper part
    vane = [at(0.28, 0)] + [at(0.28 + 0.9 * k / 12, 0.11 * math.sin(math.pi * k / 12) + 0.02) for k in range(13)] \
        + [at(1.18, 0)] + [at(0.28 + 0.9 * k / 12, -0.08 * math.sin(math.pi * k / 12)) for k in range(12, -1, -1)]
    d.polygon(vane, fill=(246, 236, 214, 255), outline=(110, 80, 50, 255))
    for k in range(3, 12, 2):
        a = at(0.28 + 0.9 * k / 12, 0)
        b = at(0.28 + 0.9 * k / 12 + 0.05, 0.09 * math.sin(math.pi * k / 12))
        d.line([a, b], fill=(190, 170, 140, 255), width=S // 120)
    d.line([at(0.04, 0), at(1.15, 0)], fill=(70, 45, 25, 255), width=S // 40)
    d.polygon([nib, at(0.08, 0.025), at(0.08, -0.025)], fill=(40, 30, 25, 255))
    img.resize((size, size), Image.LANCZOS).save(dst)
    print("QUILL", dst)


def main():
    quill(os.path.join(DIR, "Quill.png"))
    for src in sorted(glob.glob(os.path.join(RAW, "LoadRaw_*.png"))):
        theme = os.path.basename(src)[len("LoadRaw_"):-4]
        illustrate(src, os.path.join(DIR, f"Load_{theme}.png"))
    for src in sorted(glob.glob(os.path.join(RAW, "LoadInk_*.png"))):
        map_id = os.path.basename(src)[len("LoadInk_"):-4]
        crop_chart(src, os.path.join(DIR, f"Ink_{map_id}.png"))


main()
