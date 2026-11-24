"""SUPERSEDED 2026-11-24: the user rejected the scroll («выбивается из основного стиля игры»); the level LIST
(campaign_list_art.py) replaced it and its Scroll_* art was deleted. Kept for the record.

The campaign map as the approved SCROLL (the user, 2026-11-24: «Подходит» to scroll v2, «Да, встраивай»).

Bakes the art the game's CampaignMapScreen lays out at run time, and writes every level's place into
Assets/_Project/Campaign/CampaignMap.json (its other fields kept):

  UI/Campaign/Scroll_Paper_<n>.png   the long parchment in tiles (<= 2048 px: the texture limit), with the dashed
                                     road, the ink doodles, the compass rose and the next chapter's wax seal baked in;
                                     the scenes' places are left blank (a closed level shows blank paper)
  UI/Campaign/Scroll_<id>_Color.png  a level DONE: its scene in watercolour + ink, alpha = how much it covers the paper
  UI/Campaign/Scroll_<id>_Ink.png    a level OPEN: its scene in ink only
  UI/Campaign/Scroll_Rod.png, Scroll_Curl.png, Scroll_Table.png   the rolled ends, the paper's shade by them, the desk

    python Tools/campaign/campaign_scroll.py <diorama dir>

<diorama dir> holds Tools/blender/campaign_dioramas.py's `flat` renders (campaign_<key>_flat[_ink].png).
"""
import json
import math
import os
import random
import sys

from PIL import Image, ImageChops, ImageDraw, ImageFilter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import campaign_levels_overlay as kit  # noqa: E402

ROOT = kit.ROOT
OUT = os.path.join(ROOT, 'Assets', '_Project', 'UI', 'Campaign')
MAP = os.path.join(ROOT, 'Assets', '_Project', 'Campaign', 'CampaignMap.json')
PW, PH = 3600, 1000          # the whole paper
TILES = 2                    # 1800 px each

# level id -> (diorama key, scene centre x, y, width) in paper pixels
SCENES = {"m1": ("level_1", 380, 560, 560), "m2": ("level_2", 900, 580, 560), "m3p": ("prologue", 1420, 560, 560),
          "m3a": ("level_3a", 2060, 300, 480), "m3b": ("level_3b", 2060, 760, 480)}
SEAL = (2600, 640)           # the next chapter


def road_lines():
    main = [(x, 600 + 30 * math.sin(x / 260.0)) for x in range(0, 1740, 4)]
    fy = main[-1][1]
    up = [(1740 + t * 6, fy - (t * 6) ** 1.2 * 0.36) for t in range(50)]
    dn = [(1740 + t * 6, fy + (t * 6) ** 1.2 * 0.24) for t in range(50)]
    east = [(2300 + t * 6, 790 - t * 3.0) for t in range(40)]
    return [main, up, dn, east]


def main():
    ddir = sys.argv[1]
    random.seed(7)
    paper = kit.parchment(PW, PH)
    placed = []
    keep = Image.new('L', (PW, PH), 0)
    for lid, (key, cx, cy, w) in SCENES.items():
        col, lines = kit.load(ddir, key, "_flat")
        col, lines = kit.fit(col, w), kit.fit(lines.convert('RGB'), w).convert('L')
        x0, y0 = cx - w // 2, cy - col.height // 2
        fm = kit.fade_mask(col.size, sum(map(ord, key)))
        placed.append((lid, key, x0, y0, col, lines, fm))
        ImageDraw.Draw(keep).rectangle((x0 + 20, y0 - 40, x0 + w - 20, y0 + col.height + 90), fill=255)
    sx, sy = SEAL
    ImageDraw.Draw(keep).ellipse((sx - 140, sy - 140, sx + 140, sy + 140), fill=255)
    ImageDraw.Draw(keep).ellipse((1000, 30, 1220, 250), fill=255)       # the compass rose
    # the road: dashed, never over a scene
    road = Image.new('L', (PW, PH), 0)
    rd = ImageDraw.Draw(road)
    for line in road_lines():
        for i in range(0, len(line) - 2, 3):
            rd.line((line[i], line[i + 1]), fill=255, width=5)
    for lid, key, x0, y0, col, lines, fm in placed:
        hole = Image.new('L', (PW, PH), 0)
        hole.paste(fm.point(lambda v: 255 if v > 60 else 0), (x0, y0))
        road = ImageChops.subtract(road, hole)
    kit.put_ink(paper, road, (0, 0), (120, 86, 50))
    kit.doodles(paper, ImageChops.lighter(keep, road.filter(ImageFilter.MaxFilter(31))), compass=(1110, 140), tries=1400, right=80)
    # the next chapter: a wax seal at the road's end
    pd = ImageDraw.Draw(paper)
    blob = [(sx + math.cos(a / 30 * 6.283) * (78 + 8 * math.sin(a * 2.1)), sy + math.sin(a / 30 * 6.283) * (78 + 8 * math.sin(a * 2.1)))
            for a in range(30)]
    pd.polygon(blob, fill=(150, 28, 28), outline=(96, 14, 14))
    pd.ellipse((sx - 52, sy - 52, sx + 52, sy + 52), outline=(110, 18, 18), width=4)
    kit.text_c(pd, (sx, sy), "II", 52, fill=(236, 190, 160))

    os.makedirs(OUT, exist_ok=True)
    tw = PW // TILES
    for n in range(TILES):
        paper.crop((n * tw, 0, (n + 1) * tw, PH)).save(os.path.join(OUT, f'Scroll_Paper_{n}.png'))
    # each scene twice: done (watercolour + ink) and open (ink only), cut to its own rectangle
    places = {}
    for lid, key, x0, y0, col, lines, fm in placed:
        region = paper.crop((x0, y0, x0 + col.width, y0 + col.height))
        a = ImageChops.multiply(fm, col.getchannel('A'))
        washed = kit.wash(col, fm, region)               # the paper with the wash in it
        ink = kit.ink_layer(lines, fm)
        rgb = Image.composite(Image.new('RGB', col.size, (52, 34, 18)), washed, ink)
        done = rgb.convert('RGBA')
        done.putalpha(ImageChops.lighter(a, ink))
        done.save(os.path.join(OUT, f'Scroll_{lid}_Color.png'))
        opened = Image.new('RGBA', col.size, (52, 34, 18, 0))
        opened.putalpha(ink)
        opened.save(os.path.join(OUT, f'Scroll_{lid}_Ink.png'))
        w, h = col.size
        places[lid] = dict(sx=round(x0 / PW, 4), sy=round(y0 / PH, 4), sw=round(w / PW, 4), sh=round(h / PH, 4),
                           x=round((x0 + w * 0.17) / PW, 4), y=round((y0 + h * 0.16) / PH, 4),
                           nx=round((x0 + w * 0.5) / PW, 4), ny=round(min(PH - 40, y0 + h - 6) / PH, 4))
    # the rolled end, the paper's shade beside it, the desk
    rw, rh = 90, PH + 80
    rod = Image.new('RGBA', (rw, rh), (0, 0, 0, 0))
    d = ImageDraw.Draw(rod)
    for x in range(rw):
        k = math.sin(x / (rw - 1) * math.pi)
        d.line((x, 40, x, rh - 40), fill=tuple(int(c * (0.55 + 0.45 * k)) for c in (226, 200, 150)) + (255,))
    for y0 in (0, rh - 52):
        d.rounded_rectangle((10, y0, rw - 10, y0 + 52), 16, fill=(96, 58, 28, 255), outline=(52, 30, 14, 255), width=4)
    rod.save(os.path.join(OUT, 'Scroll_Rod.png'))
    curl = Image.new('RGBA', (64, 8), (90, 58, 28, 0))
    for x in range(64):
        for y in range(8):
            curl.putpixel((x, y), (90, 58, 28, int(150 * (1 - x / 63) ** 2)))
    curl.save(os.path.join(OUT, 'Scroll_Curl.png'))
    wood = Image.effect_noise((256, 1024), 40).resize((1024, 1024)).filter(ImageFilter.GaussianBlur(2))
    wood = ImageChops.multiply(Image.merge('RGB', [wood.point(lambda v: 150 + v // 3)] * 3), Image.new('RGB', (1024, 1024), (92, 56, 30)))
    wd = ImageDraw.Draw(wood)
    for y in range(0, 1024, 170):
        wd.line((0, y, 1024, y), fill=(52, 30, 14), width=4)
    wood.save(os.path.join(OUT, 'Scroll_Table.png'))

    with open(MAP, encoding='utf-8') as f:
        data = json.load(f)
    data["picture"] = "Scroll_Paper"
    data["paperAspect"] = round(PW / PH, 4)
    data["comment"] = ("The campaign map = a parchment SCROLL (Tools/campaign/campaign_scroll.py writes the places): "
                       "sx, sy, sw, sh = the level's scene on the paper (fractions from its top left); x, y = its medal; "
                       "nx, ny = its name plaque; a done level shows its scene in colour, an open one in ink, a closed one "
                       "blank paper. titleKey = the level's name; scene = the level's scene (empty: not built yet, "
                       "«Скоро»); bestKey = its best time in PlayerPrefs; after = opens once that level is done (empty: "
                       "the one before it); label = the medal's number (empty: its place); film = a story scene.")
    for lv in data["levels"]:
        lv.update(places.get(lv["id"], {}))
    with open(MAP, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=1)
    print('scroll baked:', TILES, 'tiles,', len(places), 'scenes')


if __name__ == '__main__':
    main()
