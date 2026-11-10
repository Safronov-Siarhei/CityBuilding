"""The village's houses, market stalls and cart for the opening cutscene (2026-11-10).

The houses ARE the game's (hovel1.py, cottage1.py, manor1.py, loaded with kit.borrow so nothing of
theirs is rebuilt or saved) with the life of a lived-in village added round them for the close
shots: washing on a line, a broom, hens, a shop sign, crates and sacks, a rain barrel, a cat.
The stalls and the cart are new, in the same kit. Everything is grown by figure.SET_SCALE.

    blender --background <Buildings-Size1-Models.blend> --python village.py [-- render <out dir>]
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import details as d  # noqa: E402
import figure as fg  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402

CLOTH = ('plaster_white', 'roof_blue', 'roof_red', 'plaster_yellow')
fg.plain('burlap', 'plaster_white', 'roof_red', 'roof_blue', 'plaster_yellow', 'roof_straw', 'roof_straw_dark',
         'leaf', 'grass', 'white', 'banner_red', 'banner_blue', 'wood_light', 'wood_mid', 'wood_dark')


def game_house(script, obj, braces=True):
    """The game's own house (its build at its placeholder box) and that box. `braces=False` drops
    the half-timber diagonals (2026-11-10: the manor's ran across its windows)."""
    ns = kit.borrow(script)
    _, lo, hi = kit.refresh_placeholder(obj)
    real = d.timber
    if not braces:
        d.timber = lambda f, s0, s1, z0, z1, posts, braces=(): real(f, s0, s1, z0, z1, posts, ())
    try:
        return ns['build'](lo, hi), lo, hi
    finally:
        d.timber = real


# ---------------------------------------------------------------- lived-in details

def washing_line(b, x0, x1, y, h=0.42):
    for x in (x0, x1):
        b.box(x - 0.012, x + 0.012, y - 0.012, y + 0.012, 0, h, 'wood_mid')
    b.rod((x0, y, h - 0.02), (x1, y, h - 0.02), 0.005, 'burlap')
    n = 4
    for i in range(n):
        x = x0 + (x1 - x0) * (i + 0.6) / (n + 0.4)
        c = CLOTH[i % len(CLOTH)]
        b.box(x - 0.045, x + 0.045, y - 0.004, y + 0.004, h - 0.15 + 0.02 * (i % 2), h - 0.02, c)


def broom(b, x, y, lean=0.25):
    b.rod((x, y, 0.02), (x + lean * 0.3, y, 0.36), 0.012, 'wood_mid')
    b.box(x - 0.03, x + 0.03, y - 0.015, y + 0.015, 0.0, 0.07, 'roof_straw', bevel=0.005)


def hen(b, x, y, facing=1):
    b.ball(x, y, 0.0, 0.03, 0.05, 'plaster_white', sides=5)
    b.box(x + facing * 0.022 - 0.012, x + facing * 0.022 + 0.012, y - 0.01, y + 0.01, 0.04, 0.065, 'plaster_white', bevel=0.004)
    b.box(x + facing * 0.034 - 0.004, x + facing * 0.034 + 0.004, y - 0.004, y + 0.004, 0.065, 0.075, 'roof_red')
    b.box(x + facing * 0.04 - 0.004, x + facing * 0.04 + 0.006, y - 0.003, y + 0.003, 0.05, 0.055, 'plaster_yellow')


def cat(b, x, y, z0):
    b.box(x - 0.035, x + 0.035, y - 0.02, y + 0.02, z0, z0 + 0.035, 'roof_orange', bevel=0.01)
    b.box(x + 0.025, x + 0.05, y - 0.018, y + 0.018, z0 + 0.025, z0 + 0.06, 'roof_orange', bevel=0.008)
    for k in (-1, 1):
        b.pyramid(x + 0.038, y + k * 0.01, z0 + 0.058, 0.006, 0.006, z0 + 0.075, 'roof_orange')
    b.rod((x - 0.035, y, z0 + 0.01), (x - 0.07, y - 0.02, z0 + 0.005), 0.012, 'roof_orange_dark')


def sack(b, x, y, z0=0.0, r=0.055, h=0.11):
    """A filled grain sack: a rounded body, a gathered neck tied with string."""
    b.ball(x, y, z0, r, h, 'burlap', sides=7)
    b.cylinder(x, y, z0 + h - 0.008, z0 + h + 0.02, r * 0.35, 'burlap', sides=6)
    b.cylinder(x, y, z0 + h + 0.004, z0 + h + 0.012, r * 0.4, 'wood_dark', sides=6)
    b.ball(x, y, z0 + h + 0.018, r * 0.42, 0.02, 'burlap', sides=6)


def sacks(b, x, y, n=3):
    """Two sacks standing side by side, a third lying on the ground in front of them."""
    sack(b, x, y)
    sack(b, x + 0.11, y + 0.01)
    if n > 2:
        b.hcylinder('x', (x + 0.055, y - 0.09, 0.04), 0.08, 0.04, 'burlap', sides=7)
        b.box(x + 0.13, x + 0.145, y - 0.13, y - 0.05, 0.0, 0.08, 'wood_dark')


def rain_barrel(b, x, y):
    d.barrel(b, x, y, r=0.07, h=0.18, top='water')


def shop_sign(b, x, y, z, emblem='roof_straw'):
    """An iron bracket out from the wall (+x) with a hanging board and a painted loaf."""
    b.rod((x, y, z), (x + 0.2, y, z), 0.012, 'iron')
    b.rod((x, y, z - 0.08), (x + 0.12, y, z), 0.008, 'iron')
    b.box(x + 0.06, x + 0.2, y - 0.01, y + 0.01, z - 0.15, z - 0.02, 'wood_light', bevel=0.004)
    b.ball(x + 0.13, y - 0.012, z - 0.12, 0.035, 0.05, emblem, sides=6)


def flower_pots(b, x, y):
    for i, c in enumerate(('roof_red', 'plaster_yellow', 'roof_purple')):
        px = x + i * 0.07
        b.frustum(px, y, 0.0, 0.05, 0.022, 0.028, 'roof_orange_dark', sides=6)
        b.blob(px, y, 0.05, 0.03, 0.05, 'leaf', sides=5)
        b.blob(px, y - 0.01, 0.07, 0.014, 0.022, c, sides=4)


# ---------------------------------------------------------------- the houses

def hovel():
    b, lo, hi = game_house('hovel1.py', 'Hovel1-lvl1')
    washing_line(b, lo[0] + 0.1, lo[0] + 0.85, lo[1] - 0.25)
    broom(b, hi[0] - 0.25, lo[1] + 0.12)
    for i, (x, y) in enumerate(((0.2, -0.95), (0.35, -1.05), (0.05, -1.12))):
        hen(b, x, y, 1 if i % 2 else -1)
    return b


def cottage():
    b, lo, hi = game_house('cottage1.py', 'Cottage1-lvl1')
    shop_sign(b, hi[0] - 0.12, lo[1] + 0.25, 0.72)
    sacks(b, hi[0] - 0.05, lo[1] - 0.12)
    rain_barrel(b, lo[0] + 0.05, lo[1] + 0.1)
    flower_pots(b, lo[0] + 0.2, lo[1] - 0.12)
    cat(b, lo[0] + 0.35, lo[1] - 0.05, 0.0)
    return b


def manor():
    b, lo, hi = game_house('manor1.py', 'Manor1-lvl1', braces=False)
    for i, (x, y) in enumerate(((hi[0] - 0.12, lo[1] + 0.15), (hi[0] - 0.3, lo[1] + 0.12))):
        d.barrel(b, x, y)
    d.crate(b, hi[0] - 0.12, lo[1] + 0.38, angle=15)
    d.crate(b, hi[0] - 0.12, lo[1] + 0.38, angle=40, size=0.13, z0=0.18)
    return b


# ---------------------------------------------------------------- the market

STALL = dict(w=0.3, dep=0.2, C=0.2)


def stall_goods(b, cx, cy, goods, only=None):
    """The goods on a stall's counter; `only` = the index of one heap/bolt (a toppled stall throws
    them one by one)."""
    w, dep, C = STALL['w'], STALL['dep'], STALL['C']
    rnd = __import__('random').Random(hash(goods) & 0xffff)
    for i in range(8):
        x = cx - w + 0.06 + (i % 4) * (2 * w - 0.12) / 3
        y = cy - dep + 0.07 + (i // 4) * 0.1
        if goods == 'fruit':
            colour = ('roof_red', 'plaster_yellow', 'roof_green', 'roof_orange')[i % 4]
            jit = [rnd.uniform(-0.01, 0.01) for _ in range(3)]
            if only is not None and i != only:
                continue
            b.frustum(x, y, C, C + 0.03, 0.045, 0.05, 'wood_light', sides=6)
            for j in range(3):
                b.ball(x + (j - 1) * 0.022, y + jit[j], C + 0.025, 0.017, 0.03, colour, sides=5)
        else:
            if only is not None and i != only:
                continue
            colour = ('roof_blue', 'roof_purple', 'banner_red', 'plaster_white', 'roof_green')[i % 5]
            b.hcylinder('y', (x, y, C + 0.028), 0.04, 0.028, colour, sides=6)                          # bolts of cloth


def stall(b, cx, cy, stripe=('roof_red', 'plaster_white'), goods='fruit', parts=('frame', 'goods', 'ground')):
    """A market stall facing -y: four posts, a counter, a striped sloping awning with a scalloped
    fringe, goods on the counter and baskets and crates round it. `parts` picks pieces (a toppled
    stall: the frame tips, the goods fly, what stands on the ground stays)."""
    w, dep = STALL['w'], STALL['dep']
    C = STALL['C']                       # the counter top: a cutscene figure's waist once grown
    if 'goods' in parts:
        stall_goods(b, cx, cy, goods)
    if 'ground' in parts:
        for x in (cx - w - 0.1, cx + w + 0.1):
            b.frustum(x, cy - dep - 0.05, 0.0, 0.08, 0.05, 0.065, 'wood_light', sides=8)
            b.ball(x, cy - dep - 0.05, 0.06, 0.05, 0.05, 'roof_red' if goods == 'fruit' else 'leaf', sides=6)
        d.crate(b, cx + w - 0.05, cy + dep + 0.12, angle=10, size=0.14)
        d.crate(b, cx + w - 0.2, cy + dep + 0.12, angle=-5, size=0.12)
    if 'frame' not in parts:
        return
    for sx in (-1, 1):
        for sy in (-1, 1):
            h = 0.52 if sy < 0 else 0.6
            b.box(cx + sx * w - 0.015, cx + sx * w + 0.015, cy + sy * dep - 0.015, cy + sy * dep + 0.015, 0, h, 'wood_mid')
    b.box(cx - w, cx + w, cy - dep, cy + dep * 0.3, C - 0.04, C, 'wood_light', bevel=0.006)          # the counter, waist-high
    b.box(cx - w + 0.02, cx + w - 0.02, cy - dep + 0.01, cy - dep + 0.03, 0.0, C - 0.04, 'wood_mid')      # its front boards
    # the awning: strips from the back (high) to the front (low), alternating colours
    n = 8
    for i in range(n):
        x0 = cx - w - 0.04 + i * (2 * w + 0.08) / n
        x1 = x0 + (2 * w + 0.08) / n
        c = stripe[i % 2]
        b.obox(((x0 + x1) / 2, cy, 0.575), (1, 0, 0), (0, 1, 0.3), (0, -0.3, 1), (x1 - x0) / 2, dep + 0.08, 0.008, c)
        b.box(x0 + 0.004, x1 - 0.004, cy - dep - 0.075, cy - dep - 0.062, 0.48, 0.525, c)                # the fringe


def stalls():
    b = Builder()
    stall(b, 0.0, 0.0, ('roof_red', 'plaster_white'), 'fruit')
    stall(b, 0.85, 0.1, ('roof_blue', 'plaster_yellow'), 'cloth')
    return b


CART = dict(L=0.42, W=0.2, z=0.2, r=0.2, shrink=0.65)   # before the 0.65 and SET_SCALE


def cart_wheel(b, y, z):
    """One spoked wheel at (0, y, z): rim, eight spokes, an iron hub."""
    r = CART['r']
    pts = [(r * math.cos(2 * math.pi * i / 16), z + r * math.sin(2 * math.pi * i / 16)) for i in range(17)]
    for (x0_, z0_), (x1_, z1_) in zip(pts, pts[1:]):
        b.rod((x0_, y, z0_), (x1_, y, z1_), 0.028, 'wood_dark')
    for i in range(8):
        a = math.pi * i / 4
        b.rod((0, y, z), (r * math.cos(a), y, z + r * math.sin(a)), 0.014, 'wood_mid')
    b.hcylinder('y', (0, y, z), 0.03, 0.04, 'iron', sides=8)


def cart(wheels=True):
    """A farm cart: two big spoked wheels, a plank bed with rails, shafts resting on the ground,
    a load of hay and sacks. wheels=False leaves the wheels off (townsfolk_anim turns them)."""
    b = Builder()
    L, W, z = CART['L'], CART['W'], CART['z']
    b.box(-L, L, -W, W, z, z + 0.035, 'wood_mid', bevel=0.005)
    for sy in (-1, 1):
        b.box(-L, L, sy * W - 0.012, sy * W + 0.012, z + 0.035, z + 0.13, 'wood_light')
        for x in (-L + 0.02, 0.0, L - 0.02):
            b.box(x - 0.012, x + 0.012, sy * W - 0.016, sy * W + 0.016, z, z + 0.14, 'wood_dark')
        if wheels:
            cart_wheel(b, sy * (W + 0.05), z)
    b.hcylinder('y', (0, 0, z), W + 0.06, 0.016, 'wood_dark', sides=6)                             # the axle
    for sy in (-1, 1):
        b.rod((L, sy * 0.1, z + 0.03), (L + 0.5, sy * 0.1, 0.02), 0.026, 'wood_dark')                 # shafts
    b.rod((L + 0.4, -0.1, 0.06), (L + 0.4, 0.1, 0.06), 0.02, 'wood_dark')
    # the load
    b.ball(-0.1, 0.0, z + 0.03, 0.26, 0.22, 'roof_straw', sides=8, top='plaster_yellow')
    sack(b, 0.22, -0.08, z0=z + 0.035, r=0.07, h=0.13)
    sack(b, 0.25, 0.08, z0=z + 0.035, r=0.07, h=0.13)
    b.rod((-0.3, -0.12, z + 0.2), (0.3, 0.12, z + 0.06), 0.012, 'burlap')                          # a rope over it
    for v in b.bm.verts:                     # drawn large; a wheel comes to a man's chest once grown
        v.co *= CART['shrink']
    return b


SETS = {
    "CineHovel": hovel,
    "CineCottage": cottage,
    "CineManor": manor,
    "CineStalls": stalls,
    "CineCart": cart,
}


def make(name, location=(0, 0, 0)):
    return fg.install_static(name, SETS[name](), location=location)


def render(out):
    """A little street: the three houses, the stalls in front, the cart; Ардан and the merchant
    among them for scale. Then each piece closer."""
    import bpy
    from mathutils import Vector
    import ardan
    import townsfolk
    os.makedirs(out, exist_ok=True)
    S = fg.SET_SCALE
    placed = {
        "CineHovel": (-3.6, 1.0, 0), "CineCottage": (0.0, 1.0, 0), "CineManor": (4.2, 1.6, 0),
        "CineStalls": (-0.6, -2.0, 0), "CineCart": (3.3, -2.0, 0),
    }
    objs = [make(n, loc) for n, loc in placed.items()]
    objs.append(ardan.make(Vector((1.6, -2.6, 0))))
    objs.append(townsfolk.make("CineMerchant", (-1.3, -2.75, 0)))
    sc = bpy.context.scene
    keep = {o.name for o in objs}
    for o in sc.objects:
        o.hide_render = o.name not in keep
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 1600, 900
    sc.view_settings.view_transform = 'Standard'
    for name, energy, rx, rz in (("SetSun", 3.5, 50, -25), ("SetFill", 1.0, 70, 150)):
        light = bpy.data.objects.new(name, bpy.data.lights.new(name, 'SUN'))
        light.data.energy = energy
        sc.collection.objects.link(light)
        light.rotation_euler = (math.radians(rx), 0, math.radians(rz))
    w = sc.world or bpy.data.worlds.new("W")
    sc.world = w
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.8, 0.82, 0.88, 1)
    cam = bpy.data.objects.new("SetCam", bpy.data.cameras.new("SetCam"))
    sc.collection.objects.link(cam)
    sc.camera = cam
    shots = [("street", (0.3, 0.0, 1.0), (5.0, -11.0, 5.5), 40)]
    for n, (x, y, _) in placed.items():
        shots.append((n.replace("Cine", "").lower(), (x, y, 0.7), (2.6, -4.2, 1.8), 40))
    for name, at, off, lens in shots:
        at = Vector(at)
        cam.location = at + Vector(off)
        cam.data.lens = lens
        cam.rotation_euler = (at - cam.location).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = os.path.join(out, name + ".png")
        bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if argv and argv[0] == "render":
        render(argv[1])
    else:
        for n in SETS:
            make(n)
