"""Exports the «Чернильное перо» splash for Unity from the SAME generator as the preview, so the game and the
preview can't drift apart:
  Assets/_Project/UI/Splash/splash_ink.json  -- strokes (polylines in 1920x1080 screen px, y down), dots, ink
                                                soak, pen keyframes, timings and easing curves
  Assets/_Project/UI/Splash/fill.png          -- the four petals filled (white, alpha), 1024 px, for the ink soak
  Assets/_Project/UI/Splash/pen.png           -- the quill (nib at the pivot written into the json)
Usage: python export_splash_unity.py            (needs Edge for the PNGs)"""
import json, math, pathlib, re, runpy, subprocess, sys

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parents[3]
DST = REPO / "Assets/_Project/UI/Splash"
DST.mkdir(parents=True, exist_ok=True)
EDGE = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"

sys.argv = ["gen", str(HERE / "splash_ink_preview.html")]
g = runpy.run_path(str(HERE / "gen_splash_ink.py"), run_name="__main__")
CX, CY, S, TX, TY = g["CX"], g["CY"], g["S"], g["TX"], g["TY"]

# ---------- SVG path -> polylines (absolute M H V L C Q A Z only, as the letters use) ----------
def arc_pts(x1, y1, rx, ry, fa, fs, x2, y2, n=40):
    xp, yp = (x1 - x2) / 2, (y1 - y2) / 2
    lam = xp * xp / (rx * rx) + yp * yp / (ry * ry)
    if lam > 1: rx, ry = rx * math.sqrt(lam), ry * math.sqrt(lam)
    num = rx * rx * ry * ry - rx * rx * yp * yp - ry * ry * xp * xp
    den = rx * rx * yp * yp + ry * ry * xp * xp
    coef = (-1 if fa == fs else 1) * math.sqrt(max(0.0, num / den))
    cxp, cyp = coef * rx * yp / ry, -coef * ry * xp / rx
    cx, cy = cxp + (x1 + x2) / 2, cyp + (y1 + y2) / 2
    def ang(ux, uy, vx, vy):
        return math.atan2(ux * vy - uy * vx, ux * vx + uy * vy)
    t1 = ang(1, 0, (xp - cxp) / rx, (yp - cyp) / ry)
    dt = ang((xp - cxp) / rx, (yp - cyp) / ry, (-xp - cxp) / rx, (-yp - cyp) / ry)
    if not fs and dt > 0: dt -= 2 * math.pi
    if fs and dt < 0: dt += 2 * math.pi
    k = max(8, int(abs(dt) / (2 * math.pi) * n * 2))
    return [(cx + rx * math.cos(t1 + dt * i / k), cy + ry * math.sin(t1 + dt * i / k)) for i in range(1, k + 1)]

def parse(d):
    toks = re.findall(r"[MHVLCQAZ]|-?\d*\.?\d+", d)
    polys, cur, i, pos, start, cmd = [], None, 0, (0, 0), (0, 0), None
    def num():
        nonlocal i; i += 1; return float(toks[i - 1])
    while i < len(toks):
        if re.match(r"[A-Z]", toks[i]): cmd = toks[i]; i += 1
        if cmd == "M":
            pos = (num(), num()); start = pos; cur = [pos]; polys.append(cur); cmd = "L"; continue
        if cmd == "H": pos = (num(), pos[1]); cur.append(pos)
        elif cmd == "V": pos = (pos[0], num()); cur.append(pos)
        elif cmd == "L": pos = (num(), num()); cur.append(pos)
        elif cmd == "C":
            p1, p2, p3 = (num(), num()), (num(), num()), (num(), num())
            cur += g["cubic"](pos, p1, p2, p3); pos = p3
        elif cmd == "Q":
            p1, p2 = (num(), num()), (num(), num())
            cur += g["quad"](pos, p1, p2); pos = p2
        elif cmd == "A":
            rx, ry, _rot, fa, fs, x, y = (num() for _ in range(7))
            cur += arc_pts(pos[0], pos[1], rx, ry, int(fa), int(fs), x, y); pos = (x, y)
        elif cmd == "Z":
            cur.append(start); pos = start; cmd = None
    return polys

def flat(pts): return [round(v, 2) for p in pts for v in p]

strokes = []
def add(polys, t, width, join, cap):
    strokes.append({"polys": [{"p": flat(p)} for p in polys], "t0": t[0], "dur": t[1],
                    "width": round(width, 3), "round": join == "round", "roundCaps": cap == "round"})

for a, t in zip(g["ORDER"], g["T_PETAL"]):
    add([g["petal_pts"](a)], t, 2.2 * S, "round", "round")
for sg, t in zip((-1, 1), g["T_CHEV"]):
    add([g["chev_pts"](sg)], t, 10 * S, "round", "round")
for d, (x0, _), t in zip(g["DEPIGO"], g["DEP_W"], g["T_LET"]):
    add([[(TX + x0 + x, TY + y) for x, y in p] for p in parse(d)], t, 16, "miter", "butt")
gap = (545 / 0.34 - sum(w for _, w in g["GAMES"])) / 4
gx = 0
for (d, w), t in zip(g["GAMES"], g["T_GAM"]):
    add([[(TX + (gx + x) * .34, TY + 132 + y * .34) for x, y in p] for p in parse(d)], t, 15 * .34, "miter", "butt")
    gx += w + gap

dots = []
for x, (t0, d) in zip((-66, 66), g["T_DOT"]):
    cx, cy = g["to_abs"]((x, 0))
    dots.append({"x": round(cx, 2), "y": round(cy, 2), "r": 10 * S, "t0": round(t0 + d * .4, 3), "dur": 0.35})

# the fill texture covers a square round the mark's centre, just big enough for the petals
ext = max(max(abs(px - CX), abs(py - CY)) for a in g["ORDER"] for px, py in g["petal_pts"](a))
side = math.ceil(2 * ext + 40)

PEN_BOX = (-6, -222, 116, 236)     # pen svg units (= screen px): x, y, w, h; nib at (0, 0)
data = {
    "width": 1920, "height": 1080, "total": g["TOTAL"],
    "ink": g["INK"], "paper": g["PAPER"],
    # the preview's <radialGradient id="vig"> and <filter id="grain"> as numbers for the SplashPaper shader
    "paperLook": {"vignette": "#6b4f2a", "cx": .5, "cy": .46, "r": .75, "inner": .55, "vignetteAlpha": .42,
                  "grain": "#59452b", "grainAlpha": .09, "grainFreq": .9},
    "fadeIn": list(g["FADE_IN"]), "fadeOut": list(g["T_FADE"]), "fadeEase": [.4, 0, .6, 1], "skipFade": 0.35,
    "zoomFrom": 0.965, "zoomEase": [.25, .1, .25, 1],
    "strokeEase": list(g["EASE"]),
    "strokes": strokes, "dots": dots, "dotScale": [0, 1.25, 1], "dotKeys": [0, .6, 1],
    "fill": {"x": CX, "y": CY, "size": side, "t0": g["T_FILL"][0], "dur": g["T_FILL"][1],
             "radius": 260, "edge": 10, "noise": 38, "noiseScale": 0.035, "ease": [.15, .6, .35, 1]},
    "pen": {"w": PEN_BOX[2], "h": PEN_BOX[3], "pivotX": -PEN_BOX[0] / PEN_BOX[2],
            "pivotY": (PEN_BOX[1] + PEN_BOX[3]) / PEN_BOX[3],
            "t": [round(k[0], 4) for k in g["ded"]], "x": [round(k[1], 1) for k in g["ded"]],
            "y": [round(k[2], 1) for k in g["ded"]]},
}
(DST / "splash_ink.json").write_text(json.dumps(data, separators=(",", ":")), encoding="utf-8")

# ---------- textures ----------
def render(svg, w, h, name, transparent=True):
    src = HERE / f"_render_{name}.svg"
    src.write_text(svg, encoding="utf-8")
    args = [EDGE, "--headless", "--disable-gpu", "--hide-scrollbars", f"--window-size={w},{h}",
            f"--screenshot={DST / name}", src.as_uri()]
    if transparent: args.insert(2, "--default-background-color=00000000")
    subprocess.run(args, capture_output=True, timeout=60)
    src.unlink()

defs = g["svg"].split("<defs>")[1].split("</defs>")[0]
# the paper is NOT a picture any more (a stretched 1920x1080 PNG looked soft on the tablet, 2026-11-21): the
# shader SplashPaper draws it per screen pixel from the numbers in data["paperLook"]
TEX = 1024
petals = "".join(f'<path fill-rule="evenodd" d="{g["PETAL"]} {g["VEIN"]}" transform="rotate({a}) translate(7,0)"/>' for a in g["ORDER"])
render(f'<svg xmlns="http://www.w3.org/2000/svg" width="{TEX}" height="{TEX}" viewBox="{CX - side / 2} {CY - side / 2} {side} {side}">'
       f'<g fill="#fff" transform="translate({CX},{CY}) scale({S})">{petals}</g></svg>', TEX, TEX, "fill.png")
render(f'<svg xmlns="http://www.w3.org/2000/svg" width="{PEN_BOX[2] * 2}" height="{PEN_BOX[3] * 2}" '
       f'viewBox="{" ".join(map(str, PEN_BOX))}">{g["PEN"]}</svg>', PEN_BOX[2] * 2, PEN_BOX[3] * 2, "pen.png")
print("exported", len(strokes), "strokes,", len(data["pen"]["t"]), "pen keys, fill side", side, "->", DST)
