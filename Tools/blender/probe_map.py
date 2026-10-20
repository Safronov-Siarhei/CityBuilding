"""Probes a map generator's layout without Blender: an ASCII map, a PNG height/zone map, and
checks for every orc spot (water/relief cells in its square) and the start zone.

    python probe_map.py <mapN.py> <out.png>
"""
import math
import sys
import types

path, out_png = sys.argv[1], sys.argv[2]
src = open(path, encoding="utf-8").read()
src = src[:src.index("# -- the surface ---")]
for mod in ("bmesh", "bpy", "kit", "terrain"):
    sys.modules[mod] = types.ModuleType(mod)
sys.modules["terrain"].TerrainBuilder = object
mu = types.ModuleType("mathutils")
mu.Vector = tuple
sys.modules["mathutils"] = mu
sys.argv = ["x", "--", "C:/tmp/probe.blend"]
g = {"__file__": path, "__name__": "probe"}
exec(compile(src, path, "exec"), g)

N, HALF = g["N"], g["HALF"]
H = [[None] * N for _ in range(N)]
for j in range(N):
    for i in range(N):
        x, y = i - HALF + 0.5, j - HALF + 0.5
        H[j][i] = g["height_at"](x, y)

def cell(x, y):
    return H[int(y + HALF)][int(x + HALF)]

def check(name, cx, cy, half):
    water = relief = 0
    for j in range(int(cy - half), int(cy + half)):
        for i in range(int(cx - half), int(cx + half)):
            z, sd = cell(i + 0.5, j + 0.5)
            if sd < 0:
                water += 1
            elif abs(z) > 0.02:
                relief += 1
    print(f"{name:10s} at ({cx:6.1f},{cy:6.1f}) half {half:4.1f}: water {water}, relief/slope {relief}")

for x, y, level in g["CAMPS"]:
    check(f"camp L{level}", x, y, g["CAMP_HALF"][level])
mx, my = g["MAIN_BASE"]
x0, x1, y0, y1 = (-19, 19, -23, 5)
water = relief = 0
for j in range(int(my + y0), int(my + y1)):
    for i in range(int(mx + x0), int(mx + x1)):
        z, sd = cell(i + 0.5, j + 0.5)
        water += sd < 0
        relief += sd >= 0 and abs(z) > 0.02
print(f"main base  footprint: water {water}, relief/slope {relief}")
for x, y in g["START_ZONE"]:
    z, sd = cell(x, y)
    dmin = min(math.hypot(x - cx, y - cy) for cx, cy, _ in g["CAMPS"] + [(mx, my, 0)])
    print(f"zone corner ({x},{y}): z={z:.2f} sd={sd:.1f} nearest orc {dmin:.0f} m")
for name, pts in (("fords", g["FORDS"]), ("bridges", g["BRIDGES"])):
    print(name, [(round(a, 1), round(b, 1)) for a, b in pts])

land = sum(1 for row in H for z, sd in row if sd > 0 and abs(z) < 1e-6)
water = sum(1 for row in H for z, sd in row if sd < 0)
print(f"flat land {100 * land / N / N:.0f}%  water {100 * water / N / N:.0f}%")

# ascii, 2 m per char
for j in range(N - 1, -1, -4):
    line = ""
    for i in range(0, N, 2):
        z, sd = H[j][i]
        x, y = i - HALF, j - HALF
        ch = "~" if sd < 0 else ("^" if z > 6 else "n" if z > 1.5 else "-" if abs(z) > 0.02 else ".")
        for cx, cy, lv in g["CAMPS"]:
            if abs(x - cx) <= g["CAMP_HALF"][lv] and abs(y - cy) <= g["CAMP_HALF"][lv]:
                ch = str(lv)
        if abs(x - mx) <= 19 and my - 23 <= y <= my + 5:
            ch = "M"
        line += ch
    print(line)

try:
    from PIL import Image
    img = Image.new("RGB", (N, N))
    for j in range(N):
        for i in range(N):
            z, sd = H[j][i]
            x, y = i - HALF + 0.5, j - HALF + 0.5
            if sd < 0:
                c = (40, 90, 170) if sd < -3 else (70, 150, 200)
            elif abs(z) > 0.02:
                v = int(min(255, 120 + z * 8))
                c = (v, v - 20, v - 40) if z > 0.3 else (200, 190, 140)
            elif g["ford_at"](x, y) > 0:
                c = (190, 180, 150)
            elif g["in_forest"](x, y):
                c = (40, 110, 50)
            elif g["outcrop"](x, y):
                c = (150, 150, 140)
            else:
                c = (110, 170, 80)
            img.putpixel((i, N - 1 - j), c)
    img = img.resize((N * 4, N * 4), Image.NEAREST)
    img.save(out_png)
    print("png", out_png)
except ImportError:
    print("no PIL")
