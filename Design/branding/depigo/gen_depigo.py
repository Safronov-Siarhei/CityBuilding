"""Generates the final Depigo Games logo kit (variant A): SVGs + PNGs via headless Edge."""
import os, subprocess, sys, time, pathlib

OUT = pathlib.Path(sys.argv[1])
TMP = pathlib.Path(sys.argv[2])
OUT.mkdir(parents=True, exist_ok=True); (OUT / "png").mkdir(exist_ok=True); TMP.mkdir(exist_ok=True)
EDGE = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"

GREEN, GOLD, BLACK, WHITE, CREAM = "#1f3a2f", "#c9a24f", "#111111", "#ffffff", "#f3ead6"

# ---------- mark (variant A) ----------
PETAL = "M0,0 C36,-38 72,-20 100,0 C72,20 36,38 0,0 Z"
VEIN = "M24,8 C42,-6 62,-10 82,-4 C62,-4 42,2 24,8 Z"
CHEV = ("M-24,-88 Q-11,-89 -6,-99 L0,-110 L6,-99 Q11,-89 24,-88 "
        "M-24,88 Q-11,89 -6,99 L0,110 L6,99 Q11,89 24,88")

def mark(color, kind="full", fill=None):
    """kind: full (>=64 px), core (no chevrons, 32-64 px / icons), tiny (no veins, <=32 px)."""
    f = fill or color
    d = PETAL + (" " + VEIN if kind != "tiny" else "")
    petals = "".join(f'<path fill-rule="evenodd" d="{d}" transform="rotate({a}) translate(7,0)"/>'
                     for a in (-45, -135, 45, 135))
    r = 10 if kind == "full" else 12
    dots = f'<circle cx="-66" r="{r}"/><circle cx="66" r="{r}"/>'
    chev = (f'<path d="{CHEV}" fill="none" stroke="{color}" stroke-width="10" '
            f'stroke-linecap="round" stroke-linejoin="round"/>') if kind == "full" else ""
    return f'<g fill="{f}">{petals}{dots}{chev}</g>'

# ---------- custom geometric wordmark (no font licence needed) ----------
def letters(spec, stroke, color, gaps):
    out, x = [], 0
    for (path, w), gap in zip(spec, gaps + [0]):
        out.append(f'<path transform="translate({x},0)" d="{path}"/>')
        x += w + gap
    g = (f'<g fill="none" stroke="{color}" stroke-width="{stroke}" stroke-linejoin="miter" '
         f'stroke-miterlimit="2" stroke-linecap="butt">{"".join(out)}</g>')
    return g, x

DEPIGO = [
    ("M8,8 H38 A42,42 0 0 1 38,92 H8 Z", 88),                 # D
    ("M70,8 H8 V92 H70 M8,50 H62", 70),                       # E
    ("M8,100 V8 H42 A21,21 0 0 1 42,50 H8", 71),               # P
    ("M8,0 V100", 16),                                        # I
    ("M82.2,23 A42,42 0 1 0 92,50 H56", 100),                 # G
    ("M92,50 A42,42 0 1 1 8,50 A42,42 0 1 1 92,50 Z", 100),   # O
]
GAMES = [
    ("M82.2,23 A42,42 0 1 0 92,50 H56", 100),
    ("M2,100 L38,6 L74,100 M17,66 H59", 76),
    ("M8,100 V8 L45,66 L82,8 V100", 90),
    ("M66,8 H8 V92 H66 M8,50 H58", 66),
    ("M72,22 C64,10 54,8 42,8 C25,8 12,15 12,29 C12,45 27,48 42,50 C59,52 74,57 74,72 C74,87 60,92 42,92 "
     "C27,92 14,87 6,76", 80),
]

def wordmark(c1, c2):
    """DEPIGO 545 wide x 100 high, GAMES under it spread to the same width."""
    dep, w = letters(DEPIGO, 16, c1, [20] * 5)
    s = 0.34
    gap = (w / s - sum(x[1] for x in GAMES)) / 4
    gam, _ = letters(GAMES, 15, c2, [gap] * 4)
    return f'{dep}<g transform="translate(0,132) scale({s})">{gam}</g>', w, 166

def svg(vb, body, w=None, h=None):
    size = f' width="{w}" height="{h}"' if w else ""
    return f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="{vb}"{size}>{body}</svg>\n'

def horizontal(c_mark, c1, c2, bg=None):
    wm, ww, wh = wordmark(c1, c2)
    W, H = 30 + 180 + 50 + ww + 30, 260
    b = f'<rect width="{W}" height="{H}" fill="{bg}"/>' if bg else ""
    b += f'<g transform="translate(120,130) scale(0.92)">{mark(c_mark)}</g>'
    b += f'<g transform="translate(260,{130 - wh / 2})">{wm}</g>'
    return svg(f"0 0 {W} {H}", b), W, H

def vertical(c_mark, c1, c2, bg=None):
    wm, ww, wh = wordmark(c1, c2)
    W, H = ww + 80, 290 + wh + 50
    b = f'<rect width="{W}" height="{H}" fill="{bg}"/>' if bg else ""
    b += f'<g transform="translate({W / 2},150)">{mark(c_mark)}</g>'
    b += f'<g transform="translate(40,300)">{wm}</g>'
    return svg(f"0 0 {W} {H}", b), W, H

GOLDGRAD = ('<defs><linearGradient id="gold" x1="0" y1="0" x2="0" y2="1">'
            '<stop offset="0" stop-color="#f3d68a"/><stop offset="1" stop-color="#c0913a"/></linearGradient></defs>')

def app_icon(kind="core"):
    scale = 2.25 if kind == "core" else 2.45
    b = GOLDGRAD + f'<rect width="512" height="512" rx="112" fill="{GREEN}"/>'
    b += f'<g transform="translate(256,256) scale({scale})">{mark("url(#gold)", kind)}</g>'
    return svg("0 0 512 512", b)

files = {}
for name, col in (("black", BLACK), ("white", WHITE), ("green", GREEN), ("gold", GOLD)):
    files[f"depigo_mark_{name}.svg"] = svg("-125 -125 250 250", mark(col))
files["depigo_mark_small_black.svg"] = svg("-100 -100 200 200", mark(BLACK, "core"))
files["depigo_mark_tiny_black.svg"] = svg("-100 -100 200 200", mark(BLACK, "tiny"))
files["depigo_logo_horizontal.svg"] = horizontal(GREEN, GREEN, GOLD)[0]
files["depigo_logo_horizontal_black.svg"] = horizontal(BLACK, BLACK, BLACK)[0]
files["depigo_logo_horizontal_white.svg"] = horizontal(WHITE, WHITE, GOLD)[0]
files["depigo_logo_vertical.svg"] = vertical(GREEN, GREEN, GOLD)[0]
files["depigo_logo_vertical_white.svg"] = vertical(WHITE, WHITE, GOLD)[0]
files["depigo_app_icon.svg"] = app_icon("core")
files["depigo_favicon.svg"] = app_icon("tiny")
for n, s in files.items():
    (OUT / n).write_text(s, encoding="utf-8")

def render(svg_text, w, h, png, transparent=True):
    src = TMP / (png + ".svg")
    src.write_text(svg_text.replace("<svg ", f'<svg width="{w}" height="{h}" ', 1), encoding="utf-8")
    args = [EDGE, "--headless", "--disable-gpu", "--hide-scrollbars", f"--window-size={w},{h}",
            f"--screenshot={OUT / 'png' / png}", src.as_uri()]
    if transparent:
        args.insert(2, "--default-background-color=00000000")
    subprocess.run(args, capture_output=True, timeout=60)

for name in ("black", "white", "green", "gold"):
    render(files[f"depigo_mark_{name}.svg"], 1024, 1024, f"mark_{name}_1024.png")
for px in (512, 192, 48):
    render(files["depigo_app_icon.svg"], px, px, f"app_icon_{px}.png")
for px in (32, 16):
    render(files["depigo_favicon.svg"], px, px, f"favicon_{px}.png")
for key, tag in (("depigo_logo_horizontal.svg", "horizontal"), ("depigo_logo_horizontal_white.svg", "horizontal_white"),
                 ("depigo_logo_vertical.svg", "vertical"), ("depigo_logo_vertical_white.svg", "vertical_white")):
    vb = files[key].split('viewBox="')[1].split('"')[0].split()
    W, H = float(vb[2]), float(vb[3])
    k = 1600 / W
    render(files[key], 1600, round(H * k), f"logo_{tag}_1600.png")

# ---------- overview sheet ----------
hz, hw, hh = horizontal(GREEN, GREEN, GOLD)
hzw, _, _ = horizontal(WHITE, WHITE, GOLD)
vt, vw, vh = vertical(GREEN, GREEN, GOLD)
def inner(s): return s.split(">", 1)[1].rsplit("</svg>", 1)[0]
sheet = [GOLDGRAD, '<rect width="1500" height="1000" fill="#ece7dc"/>',
         '<style>.c{fill:#fff;stroke:#d8d0c0}.t{font:600 20px Segoe UI,Arial;fill:#2b2b2b}'
         '.s{font:14px Segoe UI,Arial;fill:#7a7266}</style>']
# horizontal on white + dark
sheet += ['<rect class="c" x="20" y="20" width="960" height="300" rx="18"/>',
          f'<g transform="translate(60,45) scale({880 / hw})">{inner(hz)}</g>',
          '<rect x="20" y="340" width="960" height="300" rx="18" fill="#151515"/>',
          f'<g transform="translate(60,365) scale({880 / hw})">{inner(hzw)}</g>']
# vertical
sheet += ['<rect class="c" x="1000" y="20" width="480" height="620" rx="18"/>',
          f'<g transform="translate({1240 - vw * 0.64 / 2},70) scale(0.64)">{inner(vt)}</g>',
          '<text class="t" x="1025" y="600">Вертикальная версия</text>']
# icon ladder + small versions
sheet += ['<rect class="c" x="20" y="660" width="1460" height="320" rx="18"/>',
          '<text class="t" x="45" y="700">Иконка и мелкие размеры</text>']
x = 45
for px in (192, 96, 48):
    sheet.append(f'<g transform="translate({x},{720 + (192 - px) / 2}) scale({px / 512})">{inner(app_icon("core"))}</g>')
    sheet.append(f'<text class="s" x="{x}" y="940">{px} px</text>'); x += px + 30
for px in (32, 16):
    sheet.append(f'<g transform="translate({x},{720 + (192 - px) / 2}) scale({px / 512})">{inner(app_icon("tiny"))}</g>')
    sheet.append(f'<text class="s" x="{x - 6}" y="940">{px}</text>'); x += px + 40
x += 30
for kind, label, sz in (("full", "полный: от 64 px", 170), ("core", "без шевронов: 32–64 px", 150), ("tiny", "без прожилок: до 32 px", 150)):
    sheet.append(f'<g transform="translate({x + 90},{815}) scale({sz / 250})">{mark(BLACK, kind)}</g>')
    sheet.append(f'<text class="s" x="{x}" y="940">{label}</text>'); x += 180
for col, bg in ((GOLD, GREEN), (GREEN, CREAM)):
    sheet.append(f'<rect x="{x}" y="740" width="120" height="150" rx="12" fill="{bg}"/>')
    sheet.append(f'<g transform="translate({x + 60},815) scale(0.5)">{mark(col)}</g>'); x += 135
(OUT / "depigo_overview.svg").write_text(svg("0 0 1500 1000", "".join(sheet)), encoding="utf-8")
render(svg("0 0 1500 1000", "".join(sheet)), 1500, 1000, "overview.png", transparent=False)
print("ok", len(files), "svgs;", len(list((OUT / "png").glob("*.png"))), "pngs")
