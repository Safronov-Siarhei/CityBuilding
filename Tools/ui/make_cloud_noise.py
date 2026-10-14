"""Draws the cloud noise the fog of war is made of: a seamless 256x256 texture whose red channel is
broad, soft billows and green channel finer puffs, so the cloud shader can scroll the two at
different scales and speeds and never show a seam or repeat in an obvious way.

    python make_cloud_noise.py <unity project>

Writes Assets/_Project/Shaders/CloudNoise.png. Periodic value noise summed over octaves (fBm);
every lattice wraps at the texture's edge, which is what makes it tile.
"""
import math
import os
import random
import sys

from PIL import Image

PROJECT = sys.argv[1] if len(sys.argv) > 1 else "."
OUT = os.path.join(PROJECT, "Assets", "_Project", "Shaders", "CloudNoise.png")
SIZE = 256


def smooth(t):
    return t * t * t * (t * (t * 6 - 15) + 10)


def fbm(seed, base_period, octaves, gain=0.5):
    """Value noise, `octaves` layers, the first with `base_period` lattice cells across the tile."""
    rng = random.Random(seed)
    layers = []
    period = base_period
    for _ in range(octaves):
        lattice = [[rng.random() for _ in range(period)] for _ in range(period)]
        layers.append((period, lattice))
        period *= 2

    out = [[0.0] * SIZE for _ in range(SIZE)]
    amp, total = 1.0, 0.0
    for period, lattice in layers:
        scale = period / SIZE
        for y in range(SIZE):
            fy = y * scale
            y0 = int(fy)
            ty = smooth(fy - y0)
            row0 = lattice[y0 % period]
            row1 = lattice[(y0 + 1) % period]
            line = out[y]
            for x in range(SIZE):
                fx = x * scale
                x0 = int(fx)
                tx = smooth(fx - x0)
                x1 = (x0 + 1) % period
                x0 %= period
                a = row0[x0] + (row0[x1] - row0[x0]) * tx
                b = row1[x0] + (row1[x1] - row1[x0]) * tx
                line[x] += (a + (b - a) * ty) * amp
        total += amp
        amp *= gain

    # stretch to the full 0..1 range so the shader's thresholds mean the same for both channels
    flat = [v for row in out for v in row]
    lo, hi = min(flat), max(flat)
    return [[(v - lo) / (hi - lo) for v in row] for row in out]


def main():
    billows = fbm(seed=11, base_period=4, octaves=5)
    puffs = fbm(seed=29, base_period=8, octaves=4, gain=0.55)
    img = Image.new("RGBA", (SIZE, SIZE))
    px = img.load()
    for y in range(SIZE):
        for x in range(SIZE):
            px[x, y] = (int(billows[y][x] * 255), int(puffs[y][x] * 255), 0, 255)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    img.save(OUT)
    print("CLOUD NOISE ->", OUT)


main()
