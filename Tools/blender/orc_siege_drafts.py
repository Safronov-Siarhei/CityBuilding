"""Campaign mission 2: the ORCS' ram and siege tower (the user's second playtest, 2026-11-12: «добавь оркам
осадные башни и тараны после того как половина времени пройдёт»; the user chose new orc models) --
DRAFTS rendered for approval before anything goes into the game. Never saves the .blend.

    blender --background Buildings-Size1-Models.blend --python orc_siege_drafts.py -- <out_dir>

The kingdom's machines (siege_drafts.py, approved 2026-11-05) rebuilt rough, in the orcs' colours: dark
timber, hides, rust rags, bone horns and skulls, iron spikes; the crew are orc grunts (orcgrunt.py) at the
game's figure scale. Built in game metres (1 m = 1 cell), facing -y, standing on the origin.
  * OrcRam -- a hide-roofed gallery on six crude wheels, spikes along the eaves, a skull on the front
    gable; the log on chains ends in an iron ram's head with bone horns; two grunts inside.
  * OrcSiegeTower -- 1 x 1 cell, tapering, lashed logs and hides, skull totems and rust rags, the
    drawbridge folded up at the top deck (above a level-3 wall's walk), spikes round the top; two grunts push.
Then a lineup with an orc grunt and the mission's level-3 wall and gate for scale.
"""
import math
import os
import sys

import bpy
from mathutils import Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

# the kingdom's machine kit without its main()
_src = open(os.path.join(HERE, "siege_drafts.py"), encoding="utf-8").read()
_src = _src[:_src.rindex("\nmain()")]
exec(compile(_src, "siege_drafts.py", "exec"), globals())

import orcs as o  # noqa: E402

# the grunt's build() only: orcgrunt.py rebuilds and SAVES the .blend when imported
_g = open(os.path.join(HERE, "orcgrunt.py"), encoding="utf-8").read()
_g = _g[:_g.index("
o.ensure_collection()")]
orcgrunt = type(sys)("orcgrunt_build")
exec(compile(_g, "orcgrunt.py", "exec"), orcgrunt.__dict__)

GRUNT = 0.625           # an orc figure in the game, like the soldiers'

# the orcs' colours over the kingdom's
WOOD, WOOD_L, WOOD_D = o.TIMBER, o.TIMBER_LIGHT, o.TIMBER
IRON, ROPE, HIDE = o.METAL, o.HIDE, o.HIDE
KINGDOM, GOLD = o.RUST, o.BONE


def grunt(b, x, y, rot_deg=0.0, lean=0.0):
    """An orc grunt (orcgrunt.py) at (x, y), facing -y turned by rot_deg, tipped forward by `lean`."""
    g = orcgrunt.build(None, None)
    if lean:
        m = Matrix.Translation((0, 0, 0.2)) @ Matrix.Rotation(math.radians(lean), 4, 'X') @ Matrix.Translation((0, 0, -0.2))
        for v in g.bm.verts:
            if v.co.z > 0.2:
                v.co = m @ v.co
    tmp = bpy.data.meshes.new("GruntTmp")
    g.bm.to_mesh(tmp)
    g.bm.free()
    start = since(b)
    b.bm.from_mesh(tmp)
    bpy.data.meshes.remove(tmp)
    place(b, start, GRUNT, rot_deg, (x, y, 0))


def orc_ram(b):
    L, W = 0.72, 0.3
    R = 0.1
    z0 = R + 0.03
    for x in (-W, W):
        b.box(x - 0.04, x + 0.04, -L, L, z0 - 0.035, z0 + 0.035, WOOD_D, bevel=0.004)
        for y in (-L + 0.14, 0.0, L - 0.14):
            # crude wheels: a thick disc, no spokes
            wheel(b, x + (0.05 if x > 0 else -0.05), y, R, w=0.06, spokes=False)
    eave, ridge = 0.58, 0.88
    for x in (-W, W):
        for y in (-L + 0.04, 0.0, L - 0.04):
            o.stake(b, x, y, z0, eave + 0.05, r=0.035, color=o.TIMBER_LIGHT, point=0.06)
        b.box(x - 0.03, x + 0.03, -L, L, eave - 0.05, eave, WOOD_D)
    for k in (-1, 1):
        a = Vector((k * (W + 0.1), 0, eave - 0.04))
        c = Vector((0, 0, ridge))
        ax = a - c
        mid = (a + c) / 2
        ux = ax.normalized()
        n = ux.cross(Vector((0, 1, 0))) * -k
        # patched hides, unevenly
        for i, (y0, y1) in enumerate(((-L - 0.06, -0.25), (-0.27, 0.22), (0.2, L + 0.06))):
            b.obox(mid + n * (0.004 * i) + Vector((0, (y0 + y1) / 2, 0)), ux, (0, 1, 0), n, ax.length / 2 + 0.01 * i, (y1 - y0) / 2, 0.02,
                   o.HIDE if i != 1 else o.TIMBER)
        # spikes along the eave
        for y in (-0.6, -0.3, 0.0, 0.3, 0.6):
            o.horn(b, tuple(a + Vector((0, y, 0))), (k, 0, -0.2), 0.12, 0.018, color=o.METAL, up=0.3)
    b.box(-0.035, 0.035, -L - 0.06, L + 0.06, ridge - 0.02, ridge + 0.035, WOOD_D)
    # a skull on the front gable, rust rags
    o.skull(b, 0, -L - 0.08, eave + 0.1, s=0.07)
    o.war_banner(b, W - 0.02, L - 0.04, 1.05, w=0.16, drop=0.3)
    # the log on chains, the ram's head with bone horns
    zl = 0.34
    roof = b
    b = part("Log", (0, 0.05, eave - 0.02))
    b.hcylinder('y', (0, 0.05, zl), 0.7, 0.075, o.TIMBER_LIGHT, sides=7)
    for y in (-0.35, 0.35):
        b.hcylinder('y', (0, y, zl), 0.025, 0.082, IRON, sides=7)
        beam(b, (0, y, zl + 0.07), (0, y, eave - 0.02), 0.014, IRON)
    nose = -0.65 - 0.12
    b.box(-0.075, 0.075, nose - 0.02, nose + 0.12, zl - 0.075, zl + 0.075, IRON, bevel=0.02)
    o.skull(b, 0, nose - 0.04, zl - 0.04, s=0.08)
    for k in (-1, 1):
        o.horn(b, (k * 0.06, nose + 0.02, zl + 0.05), (k, -0.6, 0.4), 0.16, 0.026, up=1.0)
    b = roof
    for x, y in ((-0.13, 0.15), (0.13, -0.15)):
        grunt(b, x, y)


def orc_tower(b):
    DECK = 1.2                                       # above a level-3 wall's walk
    lo, hi = 0.44, 0.34
    R = 0.14
    z0 = R + 0.04
    for x in (-lo + 0.02, lo - 0.02):
        b.box(x - 0.045, x + 0.045, -lo, lo, z0 - 0.04, z0 + 0.04, WOOD_D, bevel=0.004)
        for y in (-lo + 0.12, lo - 0.12):
            wheel(b, x + (0.06 if x > 0 else -0.06), y, R, w=0.07, spokes=False)

    def half(z):
        return lo + (hi - lo) * (z - z0) / (DECK - z0)
    # corner logs, sticking out past the deck, pointed
    for sx in (-1, 1):
        for sy in (-1, 1):
            beam(b, (sx * lo * 0.97, sy * lo * 0.97, z0), (sx * hi * 0.98, sy * hi * 0.98, DECK + 0.3), 0.07, o.TIMBER_LIGHT)
    for z in (z0, 0.68, DECK):
        h = half(z)
        b.box(-h, h, -h, h, z, z + 0.04, WOOD_D)
    # the sides: rough logs across, the front hides lashed with rope, uneven
    for side in (-1, 1):
        for i in range(7):
            za = z0 + 0.04 + i * (DECK - z0) / 7
            zb = za + (DECK - z0) / 7 - 0.012
            ha, hb = half(za), half(zb)
            x = side * (ha + hb) / 2
            b.hcylinder('y', (x, 0.0, (za + zb) / 2), (ha + hb) / 2 - 0.01 + 0.02 * ((i * 7) % 3 - 1), (zb - za) / 2 + 0.006,
                        o.TIMBER if i % 2 else o.TIMBER_LIGHT, sides=6)
    for i in range(5):
        za = z0 + 0.04 + i * (DECK - z0) / 5
        zb = za + (DECK - z0) / 5 - 0.006
        ha, hb = half(za), half(zb)
        y = -(ha + hb) / 2 - 0.012
        b.obox((0.01 * (i % 2), y, (za + zb) / 2), (1, 0, 0), (0, 0, 1), (0, -1, 0), (ha + hb) / 2 - 0.01 * (i % 3), (zb - za) / 2, 0.022,
               o.HIDE if i % 2 == 0 else o.TIMBER)
        b.obox((0, y - 0.024, zb), (1, 0, 0), (0, 0, 1), (0, -1, 0), (ha + hb) / 2 + 0.005, 0.012, 0.006, ROPE)
    # skull totems on the front, rust rags hanging
    o.skull(b, -0.18, -lo - 0.05, 0.5, s=0.07)
    o.skull(b, 0.17, -half(0.9) - 0.05, 0.9, s=0.065)
    b.obox((0.0, -half(0.75) - 0.04, 0.68), (1, 0, 0), (0, 0, 1), (0, -1, 0), 0.08, 0.16, 0.008, o.RUST)
    # the drawbridge folded up at the top, chains
    bh = 0.4
    bridge = part("Bridge", (0, -hi - 0.05, DECK))
    bridge.box(-hi + 0.04, hi - 0.04, -hi - 0.075, -hi - 0.03, DECK, DECK + bh, o.TIMBER_LIGHT)
    for x in (-0.18, 0.0, 0.18):
        bridge.box(x - 0.022, x + 0.022, -hi - 0.085, -hi - 0.07, DECK + 0.02, DECK + bh - 0.02, IRON)
    o.iron_spikes(bridge, 0, -hi - 0.09, 0.001, 1, DECK + bh * 0.6, length=0.08)
    for k in (-1, 1):
        beam(b, (k * (hi - 0.06), -hi - 0.05, DECK + bh - 0.02), (k * (hi - 0.02), -hi + 0.02, DECK + bh + 0.12), 0.014, IRON)
    # a ragged parapet: stakes round the top deck, a war banner
    for sx, sy in ((-1, 0), (1, 0), (0, 1)):
        for t in (-0.26, -0.13, 0.0, 0.13, 0.26):
            x = sx * hi if sx else t
            y = t if sx else hi
            o.stake(b, x, y, DECK, DECK + 0.16 + 0.05 * ((int(t * 100) // 13) % 2), r=0.03, point=0.06)
    o.war_banner(b, hi - 0.04, hi - 0.04, DECK + 0.72, w=0.2, drop=0.34)
    # inside, a ladder; the push bar at the back and the two who push
    for k in (-1, 1):
        beam(b, (k * 0.06, 0.12, z0 + 0.04), (k * 0.06, 0.3, DECK), 0.024, o.TIMBER_LIGHT)
    for i in range(1, 8):
        t = i / 8
        b.rod((-0.06, 0.12 + 0.18 * t, z0 + 0.04 + (DECK - z0) * t), (0.06, 0.12 + 0.18 * t, z0 + 0.04 + (DECK - z0) * t), 0.016, o.TIMBER_LIGHT)
    b.hcylinder('x', (0, lo + 0.1, 0.27), lo - 0.06, 0.028, WOOD_D, sides=6)
    for x in (-0.25, 0.25):
        beam(b, (x, lo, 0.25), (x, lo + 0.12, 0.27), 0.032, WOOD_D)
        grunt(b, x, lo + 0.24, lean=16)


ORC_MACHINES = [("OrcRam", orc_ram), ("OrcSiegeTower", orc_tower)]


def orc_main():
    os.makedirs(OUT, exist_ok=True)
    sc, cam, cam_data, ground = stage()
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    ground.hide_render = False
    objs = []
    for i, (name, fn) in enumerate(ORC_MACHINES):
        obj = make(name, fn, mat)
        shoot(sc, cam, cam_data, [obj], os.path.join(OUT, f"{i + 1}-{name}.png"))
        shoot(sc, cam, cam_data, [obj], os.path.join(OUT, f"{i + 1}-{name}-back.png"), yaw_deg=145)
        objs.append(obj)
    x = 0.0
    row = []
    for obj in objs:
        obj.location = (x, 0, 0)
        row.append(obj)
        x += 1.5
    for extra, scale in (("OrcGrunt", GRUNT), ("Fence1-lvl3-Straight", 1.0), ("FortifiedGate1-lvl3", 1.0)):
        ob = bpy.data.objects.get(extra)
        if ob is None:
            print("MISSING", extra)
            continue
        ob.parent = None
        ob.matrix_world = Matrix.Translation((x - 0.3 if extra.startswith("Orc") else x, 0, 0)) @ Matrix.Scale(scale, 4)
        row.append(ob)
        x += 0.8 if extra.startswith("Orc") else 2.0
    bpy.context.view_layer.update()
    shoot(sc, cam, cam_data, row, os.path.join(OUT, "0-lineup.png"), w=1800, h=700, yaw_deg=-20, pitch_deg=28)


orc_main()
