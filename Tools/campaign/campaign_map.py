"""The campaign's map (the user, 2026-11-12): a painted parchment chart of the kingdom in the loading
screen's lightly coloured look (Tools/loading/loading_maps.py: paper, stains, ink coast, hatched sea,
mountains, compass) -- Ардан's land on the left (his village, the wheat, the forest, the river with
its bridge, the town), a castle on a hill beyond, and the rest of the kingdom under a parchment fog
(chapters to come). The level points are NOT painted: the game lays its own flags over the map at
the points this script writes to Campaign/CampaignMap.json (fractions of the picture, 0..1 from the
top left), so a point can move without repainting.

    python Tools/campaign/campaign_map.py [--preview]

Writes Assets/_Project/UI/Campaign/CampaignMap_Wide.png (2400x1080) and
Assets/_Project/Campaign/CampaignMap.json; --preview also writes Design/campaign_map_preview.png
with mock flags on the points. Needs numpy, scipy, Pillow.
"""
import json, math, os, sys
import numpy as np
from PIL import Image, ImageDraw

try:
    from scipy import ndimage as ndi
except ImportError:
    # no scipy here: the two operations the painters need, in numpy (distances capped at 160 px,
    # plenty for the coast's halo and the land's tint)
    import types

    def _dilate(m):
        out = m.copy()
        out[1:, :] |= m[:-1, :]; out[:-1, :] |= m[1:, :]
        out[:, 1:] |= m[:, :-1]; out[:, :-1] |= m[:, 1:]
        return out

    def _edt(mask, cap=160):
        """Distance from each True cell to the nearest False one (city-block, capped)."""
        free = ~np.asarray(mask, bool)
        dist = np.full(free.shape, float(cap), np.float32)
        dist[free] = 0
        cover = free
        for k in range(1, cap):
            grown = _dilate(cover)
            dist[grown & ~cover] = k
            if grown.all():
                break
            cover = grown
        return dist

    def _opening(mask, iterations=1):
        m = np.asarray(mask, bool)
        for _ in range(iterations):
            m = ~_dilate(~m)
        for _ in range(iterations):
            m = _dilate(m)
        return m

    ndi = types.SimpleNamespace(distance_transform_edt=_edt, binary_opening=_opening)
    sys.modules["scipy"] = types.SimpleNamespace(ndimage=ndi)
    sys.modules["scipy.ndimage"] = ndi

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
sys.path.insert(0, os.path.join(ROOT, "Tools", "loading"))
import loading_maps as lm  # noqa: E402

W, H = 2400, 1080
S = 2
INK = np.array(lm.INK, np.float32)
RED = (150, 46, 30)

# the story's places, in pixels of the 2400x1080 picture
VILLAGE = (470, 640)
FIELD = (690, 760)
FOREST = (930, 560)
BRIDGE = (1215, 610)
TOWN = (1420, 560)
CASTLE = (1830, 360)
# the road, through them (a smooth curve is drawn through these)
ROAD = [VILLAGE, (560, 720), FIELD, (800, 690), (880, 600), FOREST, (1060, 590), BRIDGE, (1300, 590), TOWN,
        (1540, 520), (1650, 440), (1740, 400), CASTLE]
# the level points: what the game shows; name keys are the localization's
LEVELS = [
    {"id": "m1", "at": VILLAGE, "titleKey": "#campaign_level_1", "scene": "CampaignOpening", "bestKey": "m1_best"},
    {"id": "m2", "at": TOWN, "titleKey": "#campaign_level_2", "scene": "CityBuilder", "bestKey": ""},
    {"id": "m3", "at": CASTLE, "titleKey": "#campaign_level_3", "scene": "", "bestKey": ""},
]
FOG_FROM = 1960  # x where the unknown lands begin


def catmull(points, n=14):
    pts = [points[0]] + points + [points[-1]]
    out = []
    for i in range(1, len(pts) - 2):
        p0, p1, p2, p3 = pts[i - 1], pts[i], pts[i + 1], pts[i + 2]
        for k in range(n):
            t = k / n
            t2, t3 = t * t, t * t * t
            out.append(tuple(0.5 * ((2 * p1[j]) + (-p0[j] + p2[j]) * t + (2 * p0[j] - 5 * p1[j] + 4 * p2[j] - p3[j]) * t2
                                    + (-p0[j] + 3 * p1[j] - 3 * p2[j] + p3[j]) * t3) for j in (0, 1)))
    out.append(points[-1])
    return out


def land_mask(rng):
    """The kingdom's mainland: everything but a sea along the bottom-left and a bay at the top."""
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    n = lm.fbm(H, W, 220, rng)
    coast = 0.0 + (yy / H) * 0.9 + (1 - xx / W) * 0.55 - 0.25 + (n - 0.5) * 0.35
    land = coast < 0.82
    bay = ((xx - 1500) / 420) ** 2 + ((yy + 60) / 230) ** 2 + (lm.fbm(H, W, 120, rng) - 0.5) * 0.6 < 1
    land &= ~bay
    land[:, :12] = False
    return ndi.binary_opening(land, iterations=3)


def tree(d, x, y, s):
    d.ellipse((x - 7 * s, y - 15 * s, x + 7 * s, y - 1 * s), fill=60, outline=230, width=int(1.5 * s))
    d.line([(x, y - 1 * s), (x, y + 4 * s)], fill=230, width=int(1.5 * s))


def house(d, x, y, s, big=False):
    w, h = (9 if big else 6) * s, (7 if big else 5) * s
    d.rectangle((x - w, y - h, x + w, y), fill=40, outline=230, width=int(1.3 * s))
    d.polygon([(x - w - 2 * s, y - h), (x, y - h - 7 * s), (x + w + 2 * s, y - h)], fill=150, outline=230)


def tower(d, x, y, s):
    d.rectangle((x - 6 * s, y - 22 * s, x + 6 * s, y), fill=40, outline=235, width=int(1.6 * s))
    d.polygon([(x - 8 * s, y - 22 * s), (x, y - 34 * s), (x + 8 * s, y - 22 * s)], fill=170, outline=235)


def paint(seed=7):
    rng = np.random.default_rng(seed)
    land = land_mask(rng)
    sea_d = ndi.distance_transform_edt(~land).astype(np.float32)
    land_d = ndi.distance_transform_edt(land).astype(np.float32)
    stain = lm.fbm(H, W, 260, rng)
    grain = lm.fbm(H, W, 6, rng, 2)
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)

    img = np.empty((H, W, 3), np.float32)
    img[:] = (222, 204, 160)
    halo = np.clip(1 - (sea_d - 18) / 14.0, 0, 1)
    sea_col = lm.lerp((164, 168, 150), (192, 198, 170), halo)
    img[~land] = sea_col[~land]
    inner = np.clip(land_d / 70.0, 0, 1)
    mix = np.clip(inner * 0.9 + (lm.fbm(H, W, 90, rng) - 0.5) * 0.9, 0, 1)
    land_col = lm.lerp((222, 178, 88), (156, 160, 74), mix)
    land_col = lm.lerp(land_col, (196, 140, 64), np.clip(1 - land_d / 9.0, 0, 1) * 0.6)
    img[land] = land_col[land]

    # the wheat: a golden patch south of the village
    wheat = (((xx - FIELD[0]) / 170) ** 2 + ((yy - FIELD[1]) / 85) ** 2 + (lm.fbm(H, W, 40, rng) - 0.5) * 0.5 < 1) & land
    img[wheat] = lm.lerp(img, np.array((236, 196, 92), np.float32), np.full((H, W), 0.75, np.float32))[wheat]
    # the forest's ground, darker green
    wood = (((xx - FOREST[0]) / 230) ** 2 + ((yy - FOREST[1]) / 170) ** 2 + (lm.fbm(H, W, 70, rng) - 0.5) * 0.7 < 1) & land
    img[wood] = lm.lerp(img, np.array((118, 140, 66), np.float32), np.full((H, W), 0.55, np.float32))[wood]

    # the river: from the bay's shore at the top down to the sea, through the bridge
    river_pts = catmull([(1290, 60), (1250, 260), (1230, 430), BRIDGE, (1180, 780), (1120, 1000), (1080, 1100)], 20)
    river = Image.new("L", (W, H), 0)
    ImageDraw.Draw(river).line(river_pts, fill=255, width=22, joint="curve")
    river = np.asarray(river, np.float32) / 255.0
    img = img * (1 - river[..., None] * 0.85) + np.array((150, 170, 162), np.float32) * river[..., None] * 0.85

    img *= (0.9 + 0.14 * stain)[..., None]
    img *= (0.97 + 0.06 * grain)[..., None]

    # ---- line art
    ink = Image.new("L", (W * S, H * S), 0)
    d = ImageDraw.Draw(ink)
    sea = ~land
    brk = lm.fbm(H, W, 40, rng)
    for y in range(4, H, 9):
        x = 0
        while x < W:
            run, gap = int(rng.integers(30, 160)), int(rng.integers(6, 40))
            x2 = min(W - 1, x + run)
            seg = sea[y, x:x2] & (sea_d[y, x:x2] > 30) & (brk[y, x:x2] > 0.38)
            xs = np.flatnonzero(np.diff(np.concatenate(([0], seg.astype(np.int8), [0]))))
            for a, b in zip(xs[::2], xs[1::2]):
                if b - a > 6:
                    d.line([((x + a) * S, y * S), ((x + b) * S, y * S)], fill=110, width=S)
            x = x2 + gap
    # river banks
    for side in (-1, 1):
        bank = []
        for i in range(len(river_pts) - 1):
            (x0, y0), (x1, y1) = river_pts[i], river_pts[i + 1]
            nx, ny = -(y1 - y0), x1 - x0
            ln = math.hypot(nx, ny) or 1
            bank.append(((x0 + nx / ln * 11 * side) * S, (y0 + ny / ln * 11 * side) * S))
        d.line(bank, fill=200, width=2 * S)
    # wheat strokes
    for _ in range(420):
        x, y = int(rng.integers(FIELD[0] - 170, FIELD[0] + 170)), int(rng.integers(FIELD[1] - 85, FIELD[1] + 85))
        if 0 <= x < W and 0 <= y < H and wheat[y, x]:
            d.line([(x * S, y * S), ((x + 3) * S, (y - 7) * S)], fill=120, width=S)
    # forest trees
    for _ in range(900):
        x, y = int(rng.integers(FOREST[0] - 240, FOREST[0] + 240)), int(rng.integers(FOREST[1] - 175, FOREST[1] + 175))
        if 0 <= x < W and 0 <= y < H and wood[y, x] and river[y, x] < 0.1 and abs(x - 1000) + abs(y - 590) > 0:
            tree(d, x * S, y * S, S * rng.uniform(0.8, 1.15))
    # scattered trees elsewhere on the land
    for _ in range(260):
        x, y = int(rng.integers(40, FOG_FROM)), int(rng.integers(40, H - 40))
        if land[y, x] and land_d[y, x] > 20 and not wheat[y, x] and not wood[y, x] and river[y, x] < 0.1:
            tree(d, x * S, y * S, S * 0.85)
    # mountains: north of the castle and along the fog
    rid = 1 - np.abs(2 * lm.fbm(H, W, 160, rng, 3) - 1)
    for gy in range(0, H, 26):
        for gx in range(1500, W, 30):
            x, y = gx + int(rng.integers(-8, 9)), gy + int(rng.integers(-6, 7))
            if not (0 <= x < W and 0 <= y < H) or not land[y, x] or land_d[y, x] < 16:
                continue
            near_castle = math.hypot(x - CASTLE[0], y - CASTLE[1]) < 120
            if near_castle or (y > 470 and x < 1960) or math.hypot(x - TOWN[0], y - TOWN[1]) < 160:
                continue
            if rid[y, x] > 0.62 and rng.random() < 0.7:
                lm.mountain(d, x * S, y * S, S * rng.uniform(0.9, 1.4), rng)
    # the village: a cluster of small houses round a church
    for dx, dy in ((-40, -10), (-22, 18), (2, -26), (26, 10), (44, -18), (-8, 30), (34, 34), (-46, 22), (18, -48)):
        house(d, (VILLAGE[0] + dx) * S, (VILLAGE[1] + dy) * S, S)
    house(d, (VILLAGE[0] - 4) * S, (VILLAGE[1] + 4) * S, S, big=True)
    d.line([((VILLAGE[0] - 4) * S, (VILLAGE[1] - 18) * S), ((VILLAGE[0] - 4) * S, (VILLAGE[1] - 30) * S)], fill=230, width=2 * S)
    d.line([((VILLAGE[0] - 9) * S, (VILLAGE[1] - 26) * S), ((VILLAGE[0] + 1) * S, (VILLAGE[1] - 26) * S)], fill=230, width=2 * S)
    # the town: a wall ring with towers and roofs inside
    tx, ty = TOWN
    d.ellipse(((tx - 70) * S, (ty - 46) * S, (tx + 70) * S, (ty + 46) * S), outline=235, width=4 * S)
    for k in range(8):
        a = k * math.pi / 4
        tower(d, (tx + math.cos(a) * 70) * S, (ty + math.sin(a) * 46 + 8) * S, S * 0.9)
    for dx, dy in ((-30, -6), (-6, -16), (18, -4), (-18, 18), (10, 20), (36, 14)):
        house(d, (tx + dx) * S, (ty + dy) * S, S)
    # the castle on its hill
    cx, cy = CASTLE
    d.arc(((cx - 90) * S, (cy - 10) * S, (cx + 90) * S, (cy + 70) * S), 190, 350, fill=200, width=3 * S)
    d.rectangle(((cx - 34) * S, (cy - 22) * S, (cx + 34) * S, (cy + 8) * S), fill=40, outline=235, width=2 * S)
    for x in (-38, 38):
        tower(d, (cx + x) * S, (cy + 8) * S, S * 1.3)
    tower(d, cx * S, (cy - 18) * S, S * 1.5)
    # the bridge
    bx, by = BRIDGE
    d.rectangle(((bx - 20) * S, (by - 5) * S, (bx + 20) * S, (by + 5) * S), fill=60, outline=235, width=2 * S)
    lm.compass(d, int(W * 0.06) * S, int(H * 0.15) * S, int(H * 0.09) * S, W * S, H * S)

    # the road: a dashed red-brown line through the places
    road = catmull(ROAD, 18)
    dist = 0.0
    for i in range(len(road) - 1):
        (x0, y0), (x1, y1) = road[i], road[i + 1]
        seg = math.hypot(x1 - x0, y1 - y0)
        if int(dist / 14) % 2 == 0:
            d.line([(x0 * S, y0 * S), (x1 * S, y1 * S)], fill=255, width=4 * S)
        dist += seg

    line_art = np.asarray(ink.resize((W, H), Image.LANCZOS), np.float32) / 255.0
    coast = np.clip(1 - np.abs(sea_d - 1.0) / 2.4, 0, 1) * (~land) + np.clip(1 - land_d / 2.2, 0, 1) * land
    echo = np.clip(1 - np.abs(sea_d - 7.0) / 1.0, 0, 1) * 0.55
    line_art = np.maximum(line_art, np.maximum(coast, echo))
    # the road in red ink, the rest in brown
    road_mask = Image.new("L", (W * S, H * S), 0)
    rd = ImageDraw.Draw(road_mask)
    dist = 0.0
    for i in range(len(road) - 1):
        (x0, y0), (x1, y1) = road[i], road[i + 1]
        if int(dist / 14) % 2 == 0:
            rd.line([(x0 * S, y0 * S), (x1 * S, y1 * S)], fill=255, width=5 * S)
        dist += math.hypot(x1 - x0, y1 - y0)
    road_a = np.asarray(road_mask.resize((W, H), Image.LANCZOS), np.float32) / 255.0
    img = img * (1 - line_art[..., None]) + INK * line_art[..., None]
    img = img * (1 - road_a[..., None]) + np.array(RED, np.float32) * road_a[..., None]

    # the unknown lands: a parchment fog thickening to the right, wisps at its edge
    fog_n = lm.fbm(H, W, 120, rng)
    fog = np.clip((xx - FOG_FROM + (fog_n - 0.5) * 260) / 160.0, 0, 1)
    paper = np.array((232, 216, 176), np.float32) * (0.94 + 0.08 * stain)[..., None]
    img = img * (1 - fog[..., None] * 0.93) + paper * fog[..., None] * 0.93

    # the paper's dark edge
    edge = np.minimum(np.minimum(xx, W - 1 - xx), np.minimum(yy, H - 1 - yy))
    img *= (0.78 + 0.22 * np.clip(edge / 60.0, 0, 1))[..., None]
    return Image.fromarray(np.clip(img, 0, 255).astype(np.uint8))


def preview(img):
    """The map with mock level flags (the game draws its own) for the user's approval."""
    from PIL import ImageFont
    font = lambda size: ImageFont.truetype(os.path.join(ROOT, "Assets", "_Project", "Resources", "Fonts", "Rubik-Bold.ttf"), size)
    out = img.convert("RGBA")
    d = ImageDraw.Draw(out)
    for i, lv in enumerate(LEVELS):
        x, y = lv["at"]
        state = ("done", "open", "locked")[i] if i < 3 else "locked"
        col = {"done": (224, 168, 58), "open": (196, 52, 40), "locked": (140, 132, 120)}[state]
        d.ellipse((x - 34, y - 4, x + 34, y + 14), fill=(60, 36, 18, 120))
        d.line([(x, y + 6), (x, y - 70)], fill=(63, 34, 16), width=6)
        d.polygon([(x + 2, y - 70), (x + 60, y - 56), (x + 2, y - 40)], fill=col, outline=(63, 34, 16))
        d.ellipse((x - 26, y - 106, x + 26, y - 54), fill=(243, 226, 184), outline=(63, 34, 16), width=5)
        d.text((x, y - 80), str(i + 1), fill=(63, 34, 16), font=font(34), anchor="mm")
    # the «Играть» button bottom centre, as it lights once a point is picked
    d.rounded_rectangle((W / 2 - 160, H - 140, W / 2 + 160, H - 50), radius=40, fill=(224, 168, 58), outline=(63, 34, 16), width=6)
    d.text((W / 2, H - 95), "Играть", fill=(63, 34, 16), font=font(46), anchor="mm")
    return out


def main():
    img = paint()
    pic = os.path.join(ROOT, "Assets", "_Project", "UI", "Campaign")
    os.makedirs(pic, exist_ok=True)
    img.save(os.path.join(pic, "CampaignMap_Wide.png"), optimize=True)
    data = {
        "comment": "The campaign map's level points (Tools/campaign/campaign_map.py writes this): x, y as fractions of the picture from its top left; titleKey = the level's name; the game draws a flag on each; scene = the level's scene (empty: not built yet, «Скоро»); bestKey = its best time in PlayerPrefs (empty: none). A level opens once the one before it is done.",
        "picture": "CampaignMap_Wide",
        "levels": [{"id": lv["id"], "x": round(lv["at"][0] / W, 4), "y": round(lv["at"][1] / H, 4), "titleKey": lv["titleKey"], "scene": lv["scene"], "bestKey": lv["bestKey"]} for lv in LEVELS],
    }
    with open(os.path.join(ROOT, "Assets", "_Project", "Campaign", "CampaignMap.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump(data, f, ensure_ascii=False, indent=1)
    if "--preview" in sys.argv:
        preview(img).save(os.path.join(ROOT, "Design", "campaign_map_preview.png"))
    print("ok")


if __name__ == "__main__":
    main()
