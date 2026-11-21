"""Animated preview of the 'ink pen' studio splash, v2: slower, eased, the pen lifts between strokes.
Usage: python gen_splash_ink.py <out.html>   (also writes <out>.svg)"""
import math, pathlib, sys
OUT = pathlib.Path(sys.argv[1]); OUT.parent.mkdir(parents=True, exist_ok=True)

INK, PAPER = "#2a2018", "#efe6d2"
CX, CY, S = 960, 420, 1.6          # mark centre and scale on a 1920x1080 screen
TX, TY = 960 - 272, 670            # wordmark origin (545 wide)
EASE = (.45, 0, .55, 1)            # ease-in-out for strokes (CSS cubic-bezier)
SPL = " ".join(map(str, EASE))

def bez(p1, p2, s): return 3 * (1 - s) ** 2 * s * p1 + 3 * (1 - s) * s * s * p2 + s ** 3
def ease(u):
    lo, hi = 0.0, 1.0
    for _ in range(40):
        m = (lo + hi) / 2
        lo, hi = (m, hi) if bez(EASE[0], EASE[2], m) < u else (lo, m)
    return bez(EASE[1], EASE[3], (lo + hi) / 2)
def inv_ease(f):
    lo, hi = 0.0, 1.0
    for _ in range(40):
        m = (lo + hi) / 2
        lo, hi = (m, hi) if ease(m) < f else (lo, m)
    return (lo + hi) / 2

PETAL = "M0,0 C36,-38 72,-20 100,0 C72,20 36,38 0,0 Z"
VEIN = "M24,8 C42,-6 62,-10 82,-4 C62,-4 42,2 24,8 Z"
CHEV_T = "M-24,-88 Q-11,-89 -6,-99 L0,-110 L6,-99 Q11,-89 24,-88"
CHEV_B = "M-24,88 Q-11,89 -6,99 L0,110 L6,99 Q11,89 24,88"
ORDER = (-45, 45, 135, -135)

# ---------- timeline (seconds) ----------
FADE_IN = (0.0, 0.8)
PEN_IN = (0.45, 0.5)
PET_D, PET_GAP = 0.34, 0.10
T_PETAL = [(0.95 + i * (PET_D + PET_GAP), PET_D) for i in range(4)]
T_CHEV = [(2.75, 0.25), (3.14, 0.25)]
T_DOT = [(3.51, 0.12), (3.75, 0.12)]
T_FILL = (3.0, 1.2)
LET_D, LET_GAP = 0.20, 0.03
T_LET = [(4.12 + i * (LET_D + LET_GAP), LET_D) for i in range(6)]
T_GAM = [(5.58 + i * 0.08, 0.08) for i in range(5)]
T_EXIT = (5.98, 0.55)
T_FADE = (7.0, 0.6)
TOTAL = 7.6

# ---------- geometry ----------
def cubic(p0, p1, p2, p3, n=30):
    return [tuple((1 - t) ** 3 * a + 3 * (1 - t) ** 2 * t * b + 3 * (1 - t) * t * t * c + t ** 3 * d
                  for a, b, c, d in zip(p0, p1, p2, p3)) for t in (i / n for i in range(1, n + 1))]
def quad(p0, p1, p2, n=14):
    return [tuple((1 - t) ** 2 * a + 2 * (1 - t) * t * b + t * t * c for a, b, c in zip(p0, p1, p2))
            for t in (i / n for i in range(1, n + 1))]
def to_abs(p, ang=0, shift=0):
    x, y = p[0] + shift, p[1]; r = math.radians(ang)
    return (CX + S * (x * math.cos(r) - y * math.sin(r)), CY + S * (x * math.sin(r) + y * math.cos(r)))
def petal_pts(a):
    pts = [(0, 0)] + cubic((0, 0), (36, -38), (72, -20), (100, 0)) + cubic((100, 0), (72, 20), (36, 38), (0, 0))
    return [to_abs(p, a, 7) for p in pts]
def chev_pts(sg):
    pts = [(-24, 88 * sg)] + quad((-24, 88 * sg), (-11, 89 * sg), (-6, 99 * sg)) + [(-3, 104.5 * sg), (0, 110 * sg), (3, 104.5 * sg), (6, 99 * sg)] \
        + quad((6, 99 * sg), (11, 89 * sg), (24, 88 * sg))
    return [to_abs(p) for p in pts]

# ---------- pen keyframes (time, x, y) ----------
keys = []
def stroke(pts, t0, dur):
    """pen follows the stroke with the same easing as the ink's dash animation"""
    L = [0.0]
    for a, b in zip(pts, pts[1:]): L.append(L[-1] + math.dist(a, b))
    run = [(t0 + dur * inv_ease(l / L[-1]), *p) for p, l in zip(pts, L)]
    keys.extend(run)
def travel(p, q, t0, dur, lift=60, n=8):
    """pen lifts off the paper in a soft arc between strokes"""
    mx, my = (p[0] + q[0]) / 2, min(p[1], q[1]) - lift
    for i in range(1, n):
        u = i / n; e = ease(u)
        x = (1 - e) ** 2 * p[0] + 2 * (1 - e) * e * mx + e * e * q[0]
        y = (1 - e) ** 2 * p[1] + 2 * (1 - e) * e * my + e * e * q[1]
        keys.append((t0 + dur * u, x, y))

start = (1560, 70)
keys.append((0.0, *start)); keys.append((PEN_IN[0], *start))
travel(start, petal_pts(ORDER[0])[0], *PEN_IN, lift=0)
prev_end = None
for i, (a, (t0, d)) in enumerate(zip(ORDER, T_PETAL)):
    pts = petal_pts(a)
    if prev_end: travel(prev_end, pts[0], prev_end_t, t0 - prev_end_t, lift=25)
    stroke(pts, t0, d); prev_end, prev_end_t = pts[-1], t0 + d
for sg, (t0, d) in zip((-1, 1), T_CHEV):
    pts = chev_pts(sg); travel(prev_end, pts[0], prev_end_t, t0 - prev_end_t)
    stroke(pts, t0, d); prev_end, prev_end_t = pts[-1], t0 + d
for x, (t0, d) in zip((-66, 66), T_DOT):
    p = to_abs((x, 0)); travel(prev_end, (p[0], p[1] - 14), prev_end_t, t0 - prev_end_t)
    keys += [(t0, p[0], p[1] - 14), (t0 + d * .45, *p), (t0 + d, p[0], p[1] - 18)]
    prev_end, prev_end_t = (p[0], p[1] - 18), t0 + d
DEP_W = [(0, 88), (108, 70), (198, 71), (289, 16), (325, 100), (445, 100)]
for (x0, w), (t0, d) in zip(DEP_W, T_LET):
    g = [(TX + x0, TY + 6), (TX + x0 + w * .3, TY + 94), (TX + x0 + w * .75, TY + 12), (TX + x0 + w, TY + 60)]
    travel(prev_end, g[0], prev_end_t, t0 - prev_end_t, lift=20, n=4)
    stroke(g, t0, d); prev_end, prev_end_t = g[-1], t0 + d
for i, (t0, d) in enumerate(T_GAM):
    x = TX + i * 545 / 4.4
    g = [(x, TY + 140), (x + 30, TY + 162)]
    travel(prev_end, g[0], prev_end_t, t0 - prev_end_t, lift=10, n=3)
    stroke(g, t0, d); prev_end, prev_end_t = g[-1], t0 + d
travel(prev_end, (1720, -80), *T_EXIT, lift=0, n=12)
keys.append((T_EXIT[0] + T_EXIT[1], 1720, -80)); keys.append((TOTAL, 1720, -80))
keys.sort(key=lambda k: k[0])
ded = []
for k in keys:
    if ded and k[0] <= ded[-1][0] + 1e-4: k = (ded[-1][0] + 1e-4, k[1], k[2])
    ded.append(k)
key_times = ";".join(f"{min(k[0] / TOTAL, 1):.5f}" for k in ded)
key_vals = ";".join(f"{k[1]:.1f},{k[2]:.1f}" for k in ded)

# ---------- ink ----------
def draw(d, t, extra=""):
    t0, dur = t
    return (f'<path d="{d}" pathLength="1" stroke-dasharray="1 1" stroke-dashoffset="1" {extra}>'
            f'<animate attributeName="stroke-dashoffset" from="1" to="0" begin="{t0:.3f}s" dur="{dur:.3f}s" '
            f'calcMode="spline" keyTimes="0;1" keySplines="{SPL}" fill="freeze"/></path>')
mark_outline = "".join(draw(PETAL, t, f'transform="rotate({a}) translate(7,0)"') for a, t in zip(ORDER, T_PETAL))
chevrons = draw(CHEV_T, T_CHEV[0]) + draw(CHEV_B, T_CHEV[1])
dots = "".join(f'<circle cx="{x}" r="0"><animate attributeName="r" values="0;12.5;10" keyTimes="0;.6;1" '
               f'calcMode="spline" keySplines=".2 0 .4 1;.4 0 .6 1" begin="{t0 + d * .4:.3f}s" dur="0.35s" fill="freeze"/></circle>'
               for x, (t0, d) in zip((-66, 66), T_DOT))
fill = "".join(f'<path fill-rule="evenodd" d="{PETAL} {VEIN}" transform="rotate({a}) translate(7,0)"/>' for a in ORDER)
DEPIGO = ["M8,50 V8 H38 A42,42 0 0 1 38,92 H8 Z", "M70,8 H8 V92 H70 M8,50 H62", "M8,100 V8 H42 A21,21 0 0 1 42,50 H8",
          "M8,0 V100", "M82.2,23 A42,42 0 1 0 92,50 H56", "M92,50 A42,42 0 1 1 8,50 A42,42 0 1 1 92,50 Z"]
GAMES = [("M82.2,23 A42,42 0 1 0 92,50 H56", 100), ("M2,100 L38,6 L74,100 M17,66 H59", 76),
         ("M8,100 V8 L45,66 L82,8 V100", 90), ("M66,8 H8 V92 H66 M8,50 H58", 66),
         ("M72,22 C64,10 54,8 42,8 C25,8 12,15 12,29 C12,45 27,48 42,50 C59,52 74,57 74,72 C74,87 60,92 42,92 C27,92 14,87 6,76", 80)]
dep = "".join(draw(d, t, f'transform="translate({x},0)"') for d, (x, _), t in zip(DEPIGO, DEP_W, T_LET))
gap = (545 / 0.34 - sum(w for _, w in GAMES)) / 4
gx, gam = 0, ""
for (d, w), t in zip(GAMES, T_GAM):
    gam += draw(d, t, f'transform="translate({gx},0)"'); gx += w + gap

PEN = ('<g transform="translate(14,10)" opacity=".18" fill="#000">'
       '<path d="M0,0 L10,-70 L18,-68 Z"/><path d="M14,-69 C10,-150 70,-200 95,-215 C80,-160 55,-110 18,-68 Z"/></g>'
       f'<path d="M0,0 L9,-70 L18,-68 Z" fill="{INK}"/>'
       '<path d="M13,-69 C9,-150 70,-200 95,-215 C80,-160 55,-110 18,-68 Z" fill="#f7f1e4" stroke="#9c8a6a" stroke-width="2"/>'
       '<path d="M15,-69 C30,-120 60,-170 92,-212" stroke="#9c8a6a" stroke-width="2" fill="none"/>')

svg = f'''<svg id="s" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1920 1080" preserveAspectRatio="xMidYMid slice">
<defs>
  <filter id="grain"><feTurbulence type="fractalNoise" baseFrequency=".9" numOctaves="2" seed="3"/>
    <feColorMatrix values="0 0 0 0 .35  0 0 0 0 .27  0 0 0 0 .17  0 0 0 .09 0"/></filter>
  <radialGradient id="vig" cx=".5" cy=".46" r=".75"><stop offset=".55" stop-color="#6b4f2a" stop-opacity="0"/>
    <stop offset="1" stop-color="#6b4f2a" stop-opacity=".42"/></radialGradient>
  <filter id="bleed"><feTurbulence type="fractalNoise" baseFrequency=".035" numOctaves="2" seed="7"/>
    <feDisplacementMap in="SourceGraphic" scale="38"/></filter>
  <mask id="soak"><circle cx="{CX}" cy="{CY}" r="0" fill="#fff" filter="url(#bleed)">
    <animate attributeName="r" from="0" to="260" begin="{T_FILL[0]}s" dur="{T_FILL[1]}s" calcMode="spline"
      keyTimes="0;1" keySplines=".15 .6 .35 1" fill="freeze"/></circle></mask>
</defs>
<rect width="1920" height="1080" fill="{PAPER}"/>
<rect width="1920" height="1080" filter="url(#grain)"/>
<rect width="1920" height="1080" fill="url(#vig)"/>
<g id="cam" transform="translate(960,540)"><g transform="scale(0.965)">
  <animateTransform attributeName="transform" type="scale" from="0.965" to="1" begin="0s" dur="{T_FADE[0]}s"
    calcMode="spline" keyTimes="0;1" keySplines=".25 .1 .25 1" fill="freeze"/>
  <g transform="translate(-960,-540)">
    <g fill="none" stroke="{INK}" stroke-linecap="round" stroke-linejoin="round">
      <g transform="translate({CX},{CY}) scale({S})" stroke-width="2.2">{mark_outline}</g>
      <g transform="translate({CX},{CY}) scale({S})" stroke-width="10">{chevrons}</g>
    </g>
    <g fill="{INK}" mask="url(#soak)"><g transform="translate({CX},{CY}) scale({S})">{fill}</g></g>
    <g fill="{INK}" transform="translate({CX},{CY}) scale({S})">{dots}</g>
    <g fill="none" stroke="{INK}" stroke-linejoin="miter" stroke-miterlimit="2">
      <g transform="translate({TX},{TY})" stroke-width="16">{dep}</g>
      <g transform="translate({TX},{TY + 132}) scale(.34)" stroke-width="15">{gam}</g>
    </g>
    <g>{PEN}<animateMotion dur="{TOTAL}s" fill="freeze" calcMode="linear" keyTimes="{key_times}" values="{key_vals}"/></g>
  </g>
</g></g>
<rect width="1920" height="1080" fill="#000" opacity="1">
  <animate attributeName="opacity" from="1" to="0" begin="{FADE_IN[0]}s" dur="{FADE_IN[1]}s" calcMode="spline" keyTimes="0;1" keySplines=".4 0 .6 1" fill="freeze"/>
  <animate attributeName="opacity" from="0" to="1" begin="{T_FADE[0]}s" dur="{T_FADE[1]}s" calcMode="spline" keyTimes="0;1" keySplines=".4 0 .6 1" fill="freeze"/>
</rect>
</svg>'''

html = f'''<!DOCTYPE html><html lang="ru"><head><meta charset="utf-8"><title>Depigo Games — заставка «Чернильное перо»</title>
<style>html,body{{margin:0;height:100%;background:#000;overflow:hidden}}
#wrap{{position:absolute;inset:0;display:flex;align-items:center;justify-content:center}}
svg{{width:100vw;height:56.25vw;max-height:100vh;max-width:177.78vh}}
#ui{{position:fixed;right:14px;bottom:12px;font:14px Segoe UI,Arial;color:#bbb}}
button{{font:14px Segoe UI,Arial;padding:6px 12px;margin-left:6px;border-radius:6px;border:1px solid #666;background:#222;color:#eee;cursor:pointer}}</style>
</head><body><div id="wrap">{svg}</div>
<div id="ui"><label><input type="checkbox" id="loop" checked> по кругу</label><button onclick="play()">▶ ещё раз</button></div>
<script>
const s=document.getElementById('s');
function play(){{s.setCurrentTime(0);s.unpauseAnimations();}}
setInterval(()=>{{if(document.getElementById('loop').checked&&s.getCurrentTime()>{TOTAL}+1.0)play();}},200);
</script></body></html>'''
OUT.write_text(html, encoding="utf-8")
OUT.with_suffix(".svg").write_text(svg, encoding="utf-8")
print("ok", len(ded), "pen keys, total", TOTAL, "s")
