"""The village church for the opening cutscene (2026-11-10): the service is held in it, and the
priest rings the alarm from its belfry.

The game's Church1 (church1.py: a stone nave under a violet roof, lancet windows between
buttresses, a rose window, a front bell tower with a gold bell and a violet spire with a gold
cross) with more detail for the close shots:
  * quoins up every corner, a plinth band, stepped buttresses;
  * stained glass in the lancets (blue, red and gold panes in lead lines);
  * a rose window with spokes; a ridge of small crests along the roof;
  * a tall OPEN belfry the priest can stand in: arches on four sides, a floor, the gold bell on
    a timber yoke, its rope hanging to the floor;
  * three stone steps to an iron-strapped double door, lanterns either side, a notice board,
    a bench, flower beds and a sand path.
Drawn to the game's standard, then grown by figure.SET_SCALE for the cutscene figures.

    blender --background <Buildings-Size1-Models.blend> --python church.py
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import details as d  # noqa: E402
import figure as fg  # noqa: E402
from details import FOOTING, G_TOP, U_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "CineChurch"
LANCET_H = 0.62
STAINED = ('roof_blue', 'roof_red', 'plaster_yellow')


def stained_lancet(f, s, z0, w=d.ARCH_W, h=LANCET_H):
    """A lancet in a light stone frame: dark lead, three coloured panes up it, a cusp at the top."""
    f.arch(s, z0 - 0.02, w + 0.05, h + 0.04, 0.008, 'stone_light')
    f.arch(s, z0, w, h, 0.012, 'window')
    pane_h = (h - w / 2) / 3
    for i, colour in enumerate(STAINED):
        z = z0 + 0.015 + i * pane_h
        f.rect(s - w / 2 + 0.015, s + w / 2 - 0.015, z, z + pane_h - 0.02, 0.016, colour)
    f.disc(s, z0 + h - w / 2 + 0.005, w / 2 - 0.02, 0.016, 'plaster_yellow', sides=6)
    f.rect(s - w / 2 - 0.035, s + w / 2 + 0.035, z0 - 0.045, z0 - 0.02, 0.035, 'stone_mid')


BELFRY = {}          # the belfry's centre, floor and top (before SET_SCALE), filled by build()


def bell(b, tx, ty, top):
    b.frustum(tx, ty, top - 0.27, top - 0.13, 0.12, 0.065, 'gold', sides=10)
    b.cylinder(tx, ty, top - 0.3, top - 0.27, 0.13, 'gold', sides=10)
    b.dome(tx, ty, top - 0.135, 0.067, 0.05, ['gold'], sides=10)


def clapper(b, tx, ty, top):
    b.ball(tx, ty, top - 0.33, 0.025, 0.05, 'iron', sides=6)


def build(with_bell=True):
    """The church; with_bell=False leaves the bell, clapper and rope out (bell_anim swings them)."""
    b = Builder()
    # ---- the nave
    x0, x1, y0, y1 = -0.66, 0.66, -0.55, 1.35
    hs = (x1 - x0) / 2
    zr = U_TOP + hs * d.PITCH
    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, U_TOP, 'stone_light', bevel=0.02)
    b.box(x0 - 0.02, x1 + 0.02, y0 - 0.02, y1 + 0.02, FOOTING, FOOTING + 0.08, 'stone_mid', bevel=0.01)   # plinth
    b.gable_block('y', y0, y1, x0, x1, U_TOP, zr, 'stone_light')
    # the roof starts at the bell tower's back wall: run to the usual overhang it went 0.17 into the
    # tower, its ridge level with the belfry floor, and showed inside the belfry (the user, 2026-11-11)
    r0, r1 = -0.88 + 0.4, y1 + 0.1
    d.roof(b, 0, (r0 + r1) / 2, 'y', (r1 - r0) / 2, hs, U_TOP, **d.PURPLE)
    for y in [y0 + 0.15 + i * 0.25 for i in range(8)]:
        b.box(-0.03, 0.03, y - 0.03, y + 0.03, zr + 0.04, zr + 0.1, 'roof_purple_dark', bevel=0.01)       # ridge crests
    for px, sx in ((x0, -1), (x1, 1)):
        for py, sy in ((y0, -1), (y1, 1)):
            d.quoins(b, px, py, sx, sy, FOOTING + 0.08, U_TOP)
    for nm, plane, sx in (('-x', x0, -1), ('+x', x1, 1)):
        f = Facade(b, nm, plane)
        for s in (-0.15, 0.45, 1.05):
            stained_lancet(f, s, FOOTING + 0.25)
        for s in (0.15, 0.75):  # stepped buttresses between the windows
            bx0, bx1 = (x1, x1 + 0.12) if sx > 0 else (x0 - 0.12, x0)
            cx0, cx1 = (x1, x1 + 0.07) if sx > 0 else (x0 - 0.07, x0)
            b.box(bx0, bx1, s - 0.055, s + 0.055, 0, G_TOP, 'stone_mid', bevel=0.008)
            b.box(cx0, cx1, s - 0.05, s + 0.05, G_TOP, U_TOP - 0.08, 'stone_mid', bevel=0.008)
            b.box(bx0, bx1, s - 0.06, s + 0.06, G_TOP - 0.02, G_TOP + 0.02, 'stone_light')
    # the back gable: a rose window with spokes, a lancet under it
    back = Facade(b, '+y', y1)
    rz = U_TOP + 0.22
    back.disc(0, rz, 0.21, 0.015, 'stone_mid', sides=12)
    back.disc(0, rz, 0.17, 0.022, 'window', sides=12)
    for i in range(6):
        import math
        a = i * math.pi / 3
        back.beam(-0.16 * math.cos(a), rz - 0.16 * math.sin(a), 0.16 * math.cos(a), rz + 0.16 * math.sin(a), 0.016, 0.03, 'stone_light')
    for i, colour in enumerate(('roof_blue', 'roof_red') * 3):
        import math
        a = i * math.pi / 3 + math.pi / 6
        back.disc(0.1 * math.cos(a), rz + 0.1 * math.sin(a), 0.04, 0.026, colour, sides=6)
    back.disc(0, rz, 0.05, 0.035, 'gold')
    stained_lancet(back, 0, FOOTING + 0.25)

    # ---- the bell tower on the front: an open belfry tall enough for the priest
    tx, ty, th = 0.0, -0.88, 0.4
    stone_top = U_TOP + 0.8      # the belfry floor clears the nave's ridge (it showed through the openings)
    bell0, bell1 = stone_top + 0.08, stone_top + 0.5
    d.footing(b, tx - th, tx + th, ty - th, ty + th)
    b.box(tx - th, tx + th, ty - th, ty + th, FOOTING, stone_top, 'stone_light', bevel=0.025)
    for px, sx in ((tx - th, -1), (tx + th, 1)):
        for py, sy in ((ty - th, -1), (ty + th, 1)):
            d.quoins(b, px, py, sx, sy, FOOTING, stone_top)
    b.box(tx - th - 0.04, tx + th + 0.04, ty - th - 0.04, ty + th + 0.04, G_TOP - 0.03, G_TOP + 0.03, 'stone_mid')
    b.box(tx - th - 0.05, tx + th + 0.05, ty - th - 0.05, ty + th + 0.05, stone_top - 0.04, bell0, 'stone_mid', bevel=0.015)
    # the belfry: four corner piers and arches, a floor, a dark inside
    for sx in (-1, 1):
        x_a, x_b = sorted((tx + sx * th, tx + sx * (th - 0.09)))
        for sy in (-1, 1):
            y_a, y_b = sorted((ty + sy * th, ty + sy * (th - 0.09)))
            b.box(x_a, x_b, y_a, y_b, bell0, bell1, 'stone_light', bevel=0.008)
    for nm, plane, s in (('-y', ty - th, tx), ('+y', ty + th, tx), ('-x', tx - th, ty), ('+x', tx + th, ty)):
        f = Facade(b, nm, plane)
        f.rect(s - th + 0.09, s + th - 0.09, bell1 - 0.08, bell1, 0.02, 'stone_light')       # the lintel over the opening
        f.rect(s - th + 0.09, s + th - 0.09, bell0, bell0 + 0.06, 0.02, 'stone_light')        # a parapet to lean on
    b.box(tx - th + 0.09, tx + th - 0.09, ty - th + 0.09, ty + th - 0.09, bell0, bell0 + 0.02, 'wood_mid')   # floor
    # (no dark back wall: in the Unity film it read as a black slab beside the bell -- the user, 2026-11-11)
    # the yoke and the bell, its rope down to the floor
    b.box(tx - th + 0.09, tx + th - 0.09, ty - 0.025, ty + 0.025, bell1 - 0.12, bell1 - 0.08, 'wood_dark')
    BELFRY.update(tx=tx, ty=ty, floor=bell0, top=bell1)
    if with_bell:
        bell(b, tx, ty, bell1)
        clapper(b, tx, ty, bell1)
        b.rod((tx + 0.08, ty, bell1 - 0.18), (tx + 0.1, ty - 0.05, bell0 + 0.02), 0.012, 'burlap')   # the rope
    b.box(tx - th - 0.02, tx + th + 0.02, ty - th - 0.02, ty + th + 0.02, bell1, bell1 + 0.06, 'stone_mid', bevel=0.01)
    top = bell1 + 0.06 + 1.0
    cross = top - 0.16
    b.pyramid(tx, ty, bell1 + 0.06, th + 0.04, th + 0.04, cross, 'roof_purple')
    for sx in (-1, 1):                                                                   # little gables at the spire's foot
        b.pyramid(tx + sx * (th - 0.05), ty, bell1 + 0.06, 0.06, 0.06, bell1 + 0.22, 'roof_purple_dark')
    b.box(tx - 0.015, tx + 0.015, ty - 0.015, ty + 0.015, cross - 0.05, top, 'gold')
    b.box(tx - 0.06, tx + 0.06, ty - 0.015, ty + 0.015, top - 0.08, top - 0.05, 'gold')
    b.ball(tx, ty, cross - 0.06, 0.03, 0.04, 'gold', sides=6)

    # ---- the front: steps, the strapped double door, lanterns, a round window over it
    f = Facade(b, '-y', ty - th)
    d.gate(f, tx, FOOTING + 0.08, w=0.32, h=0.54)
    for z in (0.22, 0.42):                                                              # strap hinges
        for k in (-1, 1):
            f.rect(tx + k * 0.16 - k * 0.07, tx + k * 0.16, FOOTING + 0.08 + z, FOOTING + 0.08 + z + 0.025, 0.045, 'iron')
    f.rect(tx - 0.03, tx - 0.012, FOOTING + 0.3, FOOTING + 0.33, 0.05, 'gold')           # door rings
    f.rect(tx + 0.012, tx + 0.03, FOOTING + 0.3, FOOTING + 0.33, 0.05, 'gold')
    for k in (-1, 1):
        d.wall_lantern(f, tx + k * 0.27, FOOTING + 0.48)
    f.disc(tx, U_TOP - 0.15, 0.13, 0.015, 'stone_mid')
    f.disc(tx, U_TOP - 0.15, 0.1, 0.025, 'roof_purple')
    f.disc(tx, U_TOP - 0.15, 0.04, 0.032, 'gold')
    d.arch_window(f, tx, U_TOP + 0.05, w=0.14)
    for nm, plane in (('-x', tx - th), ('+x', tx + th)):
        stained_lancet(Facade(b, nm, plane), ty, G_TOP + 0.05, w=0.12, h=0.36)
    for i, (w, dz) in enumerate(((0.36, 0.0), (0.32, 0.03), (0.28, 0.06))):
        yy = ty - th - 0.2 + i * 0.065
        b.box(tx - w, tx + w, yy, ty - th, 0, FOOTING + dz - 0.0, 'stone_mid', bevel=0.006)

    # ---- the churchyard: path, notice board, bench, flower beds, trees
    b.box(-0.22, 0.22, -1.75, ty - th - 0.2, 0, 0.012, 'cobble')
    b.box(-0.6, -0.56, -1.55, -1.51, 0, 0.42, 'wood_dark')                              # the notice board
    b.box(-0.4, -0.36, -1.55, -1.51, 0, 0.42, 'wood_dark')
    b.box(-0.62, -0.34, -1.56, -1.5, 0.22, 0.42, 'wood_mid', bevel=0.005)
    for i, (x, z) in enumerate(((-0.55, 0.36), (-0.44, 0.3), (-0.5, 0.26))):
        b.box(x - 0.035, x + 0.035, -1.565, -1.558, z - 0.04, z + 0.03, 'plaster_white' if i != 1 else 'plaster_yellow')
    d.bench(b, 0.35, 0.65, -1.55, -1.45)
    for x in (-0.55, 0.55):
        b.box(x - 0.14, x + 0.14, -1.42, -1.3, 0, 0.03, 'wood_mid')
        d.flowers(b, x, -1.36)
    d.tree(b, -1.0, -1.05)
    d.tree(b, 1.0, -0.9, r=0.2)
    for x, y in ((-0.95, 0.3), (0.98, 1.0), (-0.9, 1.2)):
        d.bush(b, x, y, r=0.1)
    return b


def make(location=(0, 0, 0), with_bell=True):
    return fg.install_static(OBJ, build(with_bell), location=location)


def render(out):
    """The church with Ардан and the priest at its door for scale; the belfry and the door close."""
    import math
    import bpy
    from mathutils import Vector
    import ardan
    import priest
    os.makedirs(out, exist_ok=True)
    church = make()
    hero = ardan.make(Vector((0.35, -2.25, 0)))
    pr = priest.make((-0.25, -2.2, 0))
    sc = bpy.context.scene
    keep = {church.name, hero.name, pr.name}
    for o in sc.objects:
        o.hide_render = o.name not in keep
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 1200, 1200
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
    S = fg.SET_SCALE
    for name, at, off, lens in (("church_34", (0, -0.3, 1.9), (6.5, -8.5, 3.0), 50),
                                ("church_front", (0, -1.0, 1.9), (0, -10.0, 0.6), 50),
                                ("church_back", (0, 0.5, 1.4), (-5.5, 7.0, 3.0), 50),
                                ("church_belfry", (0, -0.88 * S, (1.84 + 0.3) * S), (1.0, -2.2, 0.3), 60),
                                ("church_door", (0, -2.0, 0.55), (0.9, -2.6, 0.35), 45)):
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
        make()
