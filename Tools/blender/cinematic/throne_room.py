"""The throne room for mission 2's scene 2 «Гонец у короля» (the user, 2026-11-15): «величественный замок» -- the
King on his throne, his court presenting before him, the rider bursting in through the doors, running the carpet
to the throne's stairs and kneeling.

Built at the cutscene figures' own size (scale 1; the King stands ~0.73): the hall 3.6 wide (x), 7 long (y: the
doors at y -3.5, the throne's wall at +3.5), its walls 2.4 high; open above (dark beams across) so the day comes
in and the camera can look down. Inside:
  * a chequered stone floor; a red carpet with a gold border from the doors to the dais;
  * the dais: three steps the full width of the carpet and more, the throne on top -- a tall purple back in a gold
    frame with three finials and a crown at its head, a red cushion, gold arms on carved legs;
  * behind it a purple tapestry with the gold crown, gold fringe; two tall standing candelabra either side;
  * five columns a side (base, shaft, capital) with a gold sconce and a burning torch on each;
  * tall arched windows between the columns (pale glass in lead lines, stone frames, deep sills), purple banners
    with a gold crown hanging between them;
  * the great double door in its arch at the far end, iron-strapped, ajar for the rider.

    blender --background <Buildings-Size1-Models.blend> --python throne_room.py -- render <out_dir>
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import figure as fg  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "CineThroneRoom"
HX, Y0, Y1, H = 1.8, -3.5, 3.5, 2.4          # half width, door wall, throne wall, wall height
WALL_T = 0.14
CARPET_W = 0.56
STEP_H, STEP_D = 0.06, 0.16
DAIS_Y = 2.35                                  # the first step's front
COLS_Y = [-2.55, -1.4, -0.25, 0.9, 2.05]
COL_X = 1.25
PURPLE, PURPLE_DARK = 'roof_purple', 'roof_purple_dark'
GOLD = 'gold'
RED, RED_DARK = 'banner_red', 'roof_red_dark'
STONE, STONE_MID, STONE_DARK = 'stone_light', 'stone_mid', 'stone_dark'
GLASS = 'water'
WOOD, WOOD_DARK = 'wood_mid', 'wood_dark'
IRON = 'iron'
FIRE = 'fire'


def crown(f, s, z, w, depth):
    """A gold crown emblem on a Facade: a band and three points."""
    f.rect(s - w / 2, s + w / 2, z, z + w * 0.28, depth, GOLD)
    for k in (-1, 0, 1):
        x = s + k * w * 0.36
        f.poly([(x - w * 0.12, z + w * 0.28), (x + w * 0.12, z + w * 0.28), (x, z + w * (0.62 if k == 0 else 0.52))],
               depth, GOLD)


def floor(b):
    tile = 0.4
    nx = int(2 * HX / tile)
    ny = int((Y1 - Y0) / tile)
    for i in range(nx):
        for j in range(ny):
            x = -HX + i * tile
            y = Y0 + j * tile
            b.box(x, x + tile, y, y + tile, -0.02, 0.0, STONE if (i + j) % 2 == 0 else STONE_MID)
    # the carpet with its gold border, door to dais
    b.box(-CARPET_W / 2 - 0.03, CARPET_W / 2 + 0.03, Y0 + 0.12, DAIS_Y, 0.0, 0.006, GOLD)
    b.box(-CARPET_W / 2, CARPET_W / 2, Y0 + 0.12, DAIS_Y, 0.0, 0.008, RED)
    b.box(-0.02, 0.02, Y0 + 0.12, DAIS_Y, 0.0, 0.009, RED_DARK)


def dais(b):
    w = 1.0
    for i in range(3):
        y = DAIS_Y + i * STEP_D
        b.box(-w - 0.1 * (2 - i), w + 0.1 * (2 - i), y, Y1 - WALL_T, 0.0, STEP_H * (i + 1), STONE, bevel=0.006)
        b.box(-CARPET_W / 2, CARPET_W / 2, y - 0.004, y + STEP_D, STEP_H * (i + 1), STEP_H * (i + 1) + 0.006, RED)
        b.box(-CARPET_W / 2 - 0.02, CARPET_W / 2 + 0.02, y - 0.006, y + 0.006, STEP_H * i, STEP_H * (i + 1) + 0.006, GOLD)


def throne(b):
    z = STEP_H * 3
    y = Y1 - WALL_T - 0.5
    # the legs and the seat box, the red cushion
    for kx in (-1, 1):
        for ky in (-1, 1):
            b.box(kx * 0.2 - 0.03, kx * 0.2 + 0.03, y + ky * 0.14 - 0.03, y + ky * 0.14 + 0.03, z, z + 0.2, GOLD, bevel=0.008)
    b.box(-0.24, 0.24, y - 0.18, y + 0.18, z + 0.18, z + 0.24, GOLD, bevel=0.01)
    b.box(-0.2, 0.2, y - 0.16, y + 0.15, z + 0.24, z + 0.28, RED, bevel=0.015)
    # the arms
    for k in (-1, 1):
        b.box(k * 0.24 - 0.035, k * 0.24 + 0.035, y - 0.17, y + 0.16, z + 0.24, z + 0.4, PURPLE, bevel=0.01)
        b.box(k * 0.24 - 0.045, k * 0.24 + 0.045, y - 0.19, y + 0.17, z + 0.4, z + 0.43, GOLD, bevel=0.008)
        b.ball(k * 0.24, y - 0.19, z + 0.43, 0.03, 0.04, GOLD, sides=6)
    # the tall back: a gold frame, purple inside, three finials and a crown at its head
    yb = y + 0.17
    b.box(-0.26, 0.26, yb, yb + 0.06, z + 0.24, z + 1.12, GOLD, bevel=0.01)
    b.box(-0.21, 0.21, yb - 0.01, yb, z + 0.29, z + 1.06, PURPLE, bevel=0.006)
    for k in (-1, 0, 1):
        b.cylinder(k * 0.22, yb + 0.03, z + 1.12, z + 1.16, 0.03, GOLD, sides=8)
        b.pyramid(k * 0.22, yb + 0.03, z + 1.16, 0.022, 0.022, z + (1.3 if k == 0 else 1.24), GOLD)
    f = Facade(b, '-y', yb - 0.01)
    crown(f, 0.0, z + 0.86, 0.16, 0.012)


def walls(b):
    # the side walls, the throne's wall, the door wall either side of the door's arch
    b.box(-HX - WALL_T, -HX, Y0, Y1, -0.02, H, STONE)
    b.box(HX, HX + WALL_T, Y0, Y1, -0.02, H, STONE)
    b.box(-HX - WALL_T, HX + WALL_T, Y1 - WALL_T, Y1, -0.02, H, STONE)
    door_w, door_h = 1.0, 1.55
    b.box(-HX - WALL_T, -door_w / 2 - 0.12, Y0, Y0 + WALL_T, -0.02, H, STONE)
    b.box(door_w / 2 + 0.12, HX + WALL_T, Y0, Y0 + WALL_T, -0.02, H, STONE)
    b.box(-door_w / 2 - 0.12, door_w / 2 + 0.12, Y0, Y0 + WALL_T, door_h + 0.12, H, STONE)
    # a plinth and a cornice round the hall
    for k in (-1, 1):
        x = k * HX
        b.box(min(x, x - k * 0.04), max(x, x - k * 0.04), Y0 + WALL_T, Y1 - WALL_T, 0.0, 0.1, STONE_DARK)
        b.box(min(x, x - k * 0.06), max(x, x - k * 0.06), Y0 + WALL_T, Y1 - WALL_T, H - 0.1, H - 0.04, STONE_DARK)
    b.box(-HX, HX, Y1 - WALL_T - 0.04, Y1 - WALL_T, H - 0.1, H - 0.04, STONE_DARK)
    # the beams across the open top
    for y in (-2.9, -1.95, -0.95, 0.05, 1.05, 2.05, 3.0):
        b.box(-HX - WALL_T, HX + WALL_T, y - 0.06, y + 0.06, H - 0.02, H + 0.1, WOOD_DARK)


def door(b):
    """The great door: its stone arch, two iron-strapped leaves standing a little open."""
    door_w, door_h = 1.0, 1.55
    f = Facade(b, '+y', Y0 + WALL_T)
    f.arch(0.0, 0.0, door_w + 0.24, door_h + 0.12 + 0.12, 0.05, STONE_DARK, segments=10)
    for k in (-1, 1):
        hinge = k * door_w / 2
        open_deg = 18
        a = math.radians(open_deg) * (-k)
        w = door_w / 2
        cx = hinge - k * math.cos(a) * w / 2
        cy = Y0 + WALL_T + 0.04 + math.sin(abs(a)) * w / 2
        b.obox((cx, cy, door_h * 0.5 - 0.06), (math.cos(a), math.sin(a), 0), (-math.sin(a), math.cos(a), 0), (0, 0, 1),
               w / 2, 0.03, door_h * 0.5 - 0.06, WOOD, bevel=0.006)
        for zz in (0.25, 0.75, 1.2):
            b.obox((cx, cy + 0.032 * math.cos(a), zz), (math.cos(a), math.sin(a), 0), (-math.sin(a), math.cos(a), 0),
                   (0, 0, 1), w / 2 - 0.02, 0.004, 0.025, IRON)
        b.obox((cx + k * 0.0 - k * math.cos(a) * w * 0.35, cy + 0.04, 0.72), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.02, 0.02, 0.02, GOLD)


def windows_and_banners(b):
    for k in (-1, 1):
        f = Facade(b, '+x' if k < 0 else '-x', k * HX)
        for i in range(len(COLS_Y) - 1):
            s = (COLS_Y[i] + COLS_Y[i + 1]) / 2
            # a tall arched window: the stone frame, the pale glass, its lead lines, a deep sill
            f.arch(s, 0.75, 0.56, 1.35, 0.02, STONE_DARK, segments=8)
            f.arch(s, 0.79, 0.46, 1.25, 0.03, GLASS, segments=8)
            f.rect(s - 0.006, s + 0.006, 0.79, 1.97, 0.034, IRON)
            for z in (1.1, 1.45, 1.8):
                f.rect(s - 0.23, s + 0.23, z - 0.005, z + 0.005, 0.034, IRON)
            f.rect(s - 0.32, s + 0.32, 0.7, 0.76, 0.1, STONE_DARK)
        # the banners between: hung over the columns' line, purple with a gold crown and a gold fringe
        for y in COLS_Y:
            f.rect(y - 0.17, y + 0.17, 1.15, 2.1, 0.012, PURPLE)
            f.rect(y - 0.2, y + 0.2, 2.1, 2.14, 0.03, GOLD)
            f.poly([(y - 0.17, 1.15), (y + 0.17, 1.15), (y, 1.02)], 0.012, PURPLE)
            crown(f, y, 1.6, 0.2, 0.018)
    # the tapestry behind the throne and its fringe
    f = Facade(b, '-y', Y1 - WALL_T)
    f.rect(-0.75, 0.75, 0.45, 2.15, 0.012, PURPLE)
    f.rect(-0.8, 0.8, 2.15, 2.2, 0.03, GOLD)
    for i in range(16):
        s = -0.72 + i * 0.096
        f.rect(s - 0.02, s + 0.02, 0.4, 0.45, 0.014, GOLD)
    crown(f, 0.0, 1.62, 0.5, 0.02)
    f.rect(-0.7, 0.7, 0.55, 0.58, 0.016, GOLD)


def columns_and_torches(b):
    for k in (-1, 1):
        for y in COLS_Y:
            x = k * COL_X
            b.box(x - 0.17, x + 0.17, y - 0.17, y + 0.17, 0.0, 0.1, STONE_DARK, bevel=0.01)
            b.cylinder(x, y, 0.1, 0.16, 0.15, STONE, sides=10)
            b.cylinder(x, y, 0.16, H - 0.22, 0.12, STONE, sides=10)
            b.cylinder(x, y, H - 0.22, H - 0.14, 0.15, STONE, sides=10)
            b.box(x - 0.18, x + 0.18, y - 0.18, y + 0.18, H - 0.14, H - 0.02, STONE_DARK, bevel=0.01)
            b.cylinder(x, y, 0.95, 0.99, 0.125, GOLD, sides=10)
            # a gold sconce on the column's hall side, a burning torch in it
            tx = x - k * 0.15
            b.box(tx - 0.02, tx + 0.02, y - 0.02, y + 0.02, 1.2, 1.26, GOLD, bevel=0.005)
            b.rod((tx, y, 1.22), (tx - k * 0.03, y, 1.4), 0.028, WOOD_DARK)
            b.ball(tx - k * 0.034, y, 1.38, 0.035, 0.08, FIRE, sides=6)
    # two tall candelabra either side of the dais
    for k in (-1, 1):
        x, y = k * 0.85, Y1 - WALL_T - 0.4
        z = STEP_H * 3
        b.cylinder(x, y, z, z + 0.04, 0.09, GOLD, sides=8)
        b.cylinder(x, y, z + 0.04, z + 0.9, 0.02, GOLD, sides=6)
        b.cylinder(x, y, z + 0.9, z + 0.94, 0.1, GOLD, sides=8)
        for a in range(3):
            ang = a * 2 * math.pi / 3
            cx, cy = x + math.cos(ang) * 0.06, y + math.sin(ang) * 0.06
            b.cylinder(cx, cy, z + 0.94, z + 1.02, 0.016, 'plaster_white', sides=6)
            b.ball(cx, cy, z + 1.02, 0.012, 0.03, FIRE, sides=5)


def build():
    b = Builder()
    floor(b)
    dais(b)
    throne(b)
    walls(b)
    door(b)
    windows_and_banners(b)
    columns_and_torches(b)
    return b


def make(location=(0, 0, 0)):
    return fg.install_static(OBJ, build(), scale=1.0, location=location)


def render(out):
    """The hall with the King by his throne and the court on the carpet, from the door, from the throne and
    from the side at the dais."""
    import bpy
    from mathutils import Vector
    import king
    import courtiers
    os.makedirs(out, exist_ok=True)
    room = make()
    figs = [king.make((0.0, Y1 - WALL_T - 0.85, STEP_H * 3)),
            courtiers.make("CineAdvisor", (0.0, DAIS_Y - 0.55, 0)),
            courtiers.make("CineNoble", (-0.7, DAIS_Y - 0.9, 0)),
            courtiers.make("CineLady", (0.7, DAIS_Y - 1.0, 0))]
    figs[1].rotation_euler = (0, 0, math.radians(180))
    figs[2].rotation_euler = (0, 0, math.radians(160))
    figs[3].rotation_euler = (0, 0, math.radians(200))
    sc = bpy.context.scene
    keep = {room.name} | {f.name for f in figs}
    for o in sc.objects:
        o.hide_render = o.name not in keep
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 1400, 900
    sc.view_settings.view_transform = 'Standard'
    for name, energy, rx, rz in (("SetSun", 3.2, 35, -30), ("SetFill", 1.2, 60, 150)):
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
    for name, at, frm, lens in (("hall_from_door", (0, 1.5, 0.5), (0, Y0 + 0.3, 1.5), 24),
                                ("hall_overview", (0, 0.3, 0.3), (2.6, -4.8, 5.2), 24),
                                ("hall_from_throne", (0, -1.5, 0.6), (0.25, Y1 - 0.6, 1.4), 24),
                                ("hall_dais", (0, Y1 - 0.9, 0.7), (1.1, DAIS_Y - 1.2, 0.75), 30)):
        at, frm = Vector(at), Vector(frm)
        cam.location = frm
        cam.data.lens = lens
        cam.rotation_euler = (at - frm).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = os.path.join(out, name + ".png")
        bpy.ops.render.render(write_still=True)
        print("RENDERED", sc.render.filepath)


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if argv and argv[0] == "render":
        render(argv[1])
    else:
        make()
