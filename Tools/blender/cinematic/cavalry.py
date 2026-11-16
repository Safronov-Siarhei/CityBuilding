"""The King's cavalry for mission 2's last scene «Подкрепление» (the user, 2026-11-16: «по мосту несётся король и
огромное количество кавалерии, они проносятся по городу и уничтожают всех орков»). Renders go to the user for
approval before Unity sees them.

  CineCavalry   a heavy horseman on Ардан's rig (figure.py), same style as the rider and the captain: a steel
                bascinet with its visor up and a blue plume, a mail aventail, a steel breastplate under a blue
                surcoat with the capital's gold cross, steel pauldrons, vambraces and greaves, mail sleeves and
                thighs; a LANCE with a blue pennon in the right hand, a blue kite shield with the gold cross on the
                left forearm, a sword at the left hip.
  CineWarhorse  the game horse (horse.py) as a destrier: dark bay or grey, a blue caparison with a gold hem and
                the gold cross on each flank.
  CineKingHorse the King's white horse, a red caparison with gold.

    blender --background <Buildings-Size1-Models.blend> --python cavalry.py -- render <out dir>
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import figure as fg  # noqa: E402
import horse  # noqa: E402
from mathutils import Vector  # noqa: E402

OBJ = "CineCavalry"
SCALE = 1.0

SKIN = 'plaster_pink'
BLUE, BLUE_DARK = 'banner_blue', 'roof_blue_dark'
STEEL, STEEL_DARK, MAIL = 'stone_light', 'iron', 'stone_mid'
LEATHER, BOOT, SOLE = 'wood_dark', 'door', 'iron'
WOOD = 'wood_light'
GOLD = 'gold'
HAIR = 'wood_mid'
EYE, PUPIL = 'white', 'window'
RED, RED_DARK = 'banner_red', 'roof_red_dark'
WHITE = 'plaster_white'
fg.plain(SKIN, BLUE, BLUE_DARK, STEEL, STEEL_DARK, MAIL, LEATHER, BOOT, SOLE, WOOD, GOLD, HAIR, EYE, PUPIL,
         RED, RED_DARK, WHITE)


# ---------------------------------------------------------------- the horseman

def foot(b, k):
    x = k * fg.HIP_X
    b.box(x - 0.028, x + 0.028, -0.058, 0.028, 0.0, 0.012, SOLE, bevel=0.004)
    b.box(x - 0.027, x + 0.027, -0.056, 0.027, 0.012, 0.038, STEEL, bevel=0.009)        # the sabaton


def shin(b, k):
    """Steel greaves, a knee cop at the top."""
    x = k * fg.HIP_X
    b.box(x - 0.027, x + 0.027, -0.028, 0.026, 0.03, 0.1, STEEL, bevel=0.009)
    b.ball(x, -0.012, 0.102, 0.03, 0.03, STEEL_DARK, sides=6)


def thigh(b, k):
    """Mail on the thighs, a steel plate over the front."""
    x = k * fg.HIP_X
    b.box(x - 0.025, x + 0.025, -0.025, 0.025, 0.098, 0.2, MAIL, bevel=0.008)
    b.box(x - 0.022, x + 0.022, -0.029, -0.02, 0.11, 0.19, STEEL, bevel=0.004)


def waist(b):
    """The surcoat's split skirt (for the saddle) with a gold hem, the belt, the sword on the left hip (+x)."""
    for k in (-1, 1):
        b.obox((k * 0.042, 0, 0.21), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.044, 0.078, 0.052, BLUE, bevel=0.012)
        b.box(k * 0.042 - 0.044, k * 0.042 + 0.044, -0.079, 0.079, 0.156, 0.166, GOLD)
    b.frustum(0, 0, 0.22, 0.262, 0.086, 0.08, MAIL, sides=10)
    b.cylinder(0, 0, 0.25, 0.268, 0.083, LEATHER, sides=10)
    b.box(-0.014, 0.014, -0.089, -0.081, 0.249, 0.27, GOLD, bevel=0.003)
    b.rod((0.09, -0.012, 0.25), (0.112, 0.04, 0.1), 0.018, LEATHER)                      # the scabbard
    b.rod((0.09, -0.012, 0.25), (0.083, -0.028, 0.31), 0.011, LEATHER)                   # the grip
    b.rod((0.106, -0.006, 0.256), (0.07, -0.02, 0.25), 0.011, STEEL)                     # the guard
    b.ball(0.082, -0.03, 0.312, 0.009, 0.014, GOLD, sides=6)


def chest(b):
    """The breastplate under a blue surcoat with the gold cross, pauldrons, the mail aventail at the neck."""
    b.obox((0, 0, 0.33), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.082, 0.058, 0.064, STEEL, bevel=0.024)
    # the surcoat: front and back panels over the plate, open at the sides
    for y, s in ((-0.06, -1), (0.058, 1)):
        b.box(-0.062, 0.062, y - 0.006, y + 0.006, 0.255, 0.385, BLUE)
    b.box(-0.011, 0.011, -0.068, -0.064, 0.28, 0.375, GOLD)                              # the cross, upright
    b.box(-0.045, 0.045, -0.068, -0.064, 0.33, 0.352, GOLD)                              # and its bar
    b.box(-0.064, 0.064, -0.068, 0.066, 0.255, 0.265, GOLD)                              # the belt line hem
    for k in (-1, 1):                                                                     # the pauldrons
        b.ball(k * 0.088, 0.0, 0.372, 0.044, 0.04, STEEL, sides=8)
        b.ball(k * 0.092, 0.0, 0.352, 0.042, 0.022, STEEL_DARK, sides=8)
    b.cylinder(0, 0, 0.37, 0.41, 0.052, MAIL, sides=8)                                   # the aventail


def upper_arm(b, k):
    sh, el, _ = fg.joints(k)
    b.rod(sh, el, 0.042, MAIL)
    b.rod(sh + (el - sh) * 0.45, el, 0.046, STEEL)                                       # the rerebrace


def forearm(b, k):
    """Vambraces and gauntlets; the LANCE in the right hand (forward in the rest pose), the kite SHIELD on the left."""
    _, el, wr = fg.joints(k)
    d = (wr - el).normalized()
    b.rod(el, wr, 0.043, STEEL)
    b.ball(el.x, el.y, el.z, 0.03, 0.03, STEEL_DARK, sides=6)                            # the couter
    b.ball(wr.x, wr.y, wr.z - 0.03, 0.022, 0.034, STEEL_DARK, sides=6)                   # the gauntlet
    hand = wr - d * -0.03
    if k < 0:
        # the lance through the fist: grip behind, a vamplate before the hand, the shaft, the point, the pennon
        # along the arm: the charge holds it out ahead with the arm (a pose turns it with the forearm)
        tail, tip = hand - d * 0.3, hand + d * 1.25
        b.rod(tail, tip, 0.02, WOOD)
        b.rod(hand + d * 0.05, hand + d * 0.09, 0.05, STEEL)
        b.rod(tip, tip + d * 0.09, 0.018, STEEL)
        side = d.cross(Vector((1, 0, 0))).normalized()
        b.prism([(0.0, 0.0), (0.2, 0.012), (0.2, 0.05), (0.0, 0.07)],
                tuple(tip - d * 0.22), d, side, 0.004, BLUE, back_cap=True)
    else:
        # the kite shield strapped to the outside of the left forearm: blue, gold rim and cross
        c = el + (wr - el) * 0.5 + Vector((0.05, 0, 0))
        pts = [(-0.07, 0.07), (0.07, 0.07), (0.07, -0.02), (0.0, -0.13), (-0.07, -0.02)]
        b.prism(pts, (c.x, c.y, c.z), (0, 1, 0), (0, 0, 1), 0.012, BLUE, back_cap=True)
        b.prism([(-0.075, 0.075), (0.075, 0.075), (0.075, 0.064), (-0.075, 0.064)], (c.x - 0.002, c.y, c.z),
                (0, 1, 0), (0, 0, 1), 0.016, GOLD, back_cap=True)
        b.box(c.x + 0.012, c.x + 0.016, c.y - 0.008, c.y + 0.008, c.z - 0.085, c.z + 0.055, GOLD)
        b.box(c.x + 0.012, c.x + 0.016, c.y - 0.045, c.y + 0.045, c.z + 0.006, c.z + 0.022, GOLD)


def head(b):
    """A young bearded face under a bascinet with the visor up and a blue plume."""
    z = 0.425
    b.box(-0.041, 0.041, -0.043, 0.04, z - 0.002, z + 0.085, SKIN, bevel=0.018)
    for k in (-1, 1):
        b.box(k * 0.018 - 0.009, k * 0.018 + 0.009, -0.0465, -0.0425, z + 0.044, z + 0.055, EYE)
        b.box(k * 0.016 - 0.0045, k * 0.016 + 0.0045, -0.0475, -0.045, z + 0.045, z + 0.054, PUPIL)
        b.box(k * 0.019 - 0.012, k * 0.019 + 0.012, -0.048, -0.042, z + 0.059, z + 0.066, HAIR, bevel=0.002)
    b.box(-0.008, 0.008, -0.056, -0.043, z + 0.028, z + 0.05, SKIN, bevel=0.004)          # the nose
    b.box(-0.04, 0.04, -0.047, -0.012, z - 0.012, z + 0.02, HAIR, bevel=0.01)             # the beard
    b.box(-0.011, 0.011, -0.0495, -0.0465, z + 0.01, z + 0.014, RED_DARK)
    # the bascinet: a pointed steel skull over the ears and the back of the head, the raised visor's brow
    b.box(-0.05, 0.05, -0.03, 0.05, z + 0.0, z + 0.07, STEEL, bevel=0.016)               # cheeks and nape
    b.frustum(0, 0.004, z + 0.066, z + 0.13, 0.054, 0.016, STEEL, sides=8)
    b.frustum(0, 0.004, z + 0.13, z + 0.15, 0.016, 0.003, STEEL, sides=8)
    b.obox((0, -0.048, z + 0.104), (1, 0, 0), (0, 0.5, 1), (0, -1, 0.5), 0.046, 0.016, 0.006, STEEL, bevel=0.004)  # the visor, up
    for k in (-1, 1):
        b.ball(k * 0.052, -0.02, z + 0.07, 0.009, 0.012, GOLD, sides=6)                  # the visor's pivots
    # the plume: a fan of blue up and back from the crest
    for i in range(4):
        a = math.radians(20 + i * 14)
        p0 = Vector((0, 0.01, z + 0.14))
        p1 = p0 + Vector((0, math.sin(a) * 0.09, math.cos(a) * 0.08))
        b.rod(p0, p1, 0.022 - i * 0.003, BLUE if i % 2 == 0 else BLUE_DARK)


PARTS = [("hips", waist), ("chest", chest), ("head", head)] + fg.limbs(
    {"thigh": thigh, "shin": shin, "foot": foot, "upper_arm": upper_arm, "forearm": forearm})


def make(location=(0, 0, 0)):
    return fg.make(OBJ, PARTS, SCALE, location)


# ---------------------------------------------------------------- the horses

def _caparison(cloth, trim, cross):
    """A cloth over the horse from the withers to the croup, falling to the hocks, a gold hem; the cross on each
    flank. In the horse's rest space (horse.py): the barrel x ±0.084, y ±0.2, z 0.25-0.42."""
    def build(b):
        horse.body(b)
        for k in (-1, 1):
            x = k * 0.1
            b.box(x - 0.006, x + 0.006, -0.22, 0.25, 0.2, 0.4, cloth)                     # the flank's drape
            b.box(x - 0.008, x + 0.008, -0.225, 0.255, 0.19, 0.205, trim)                # its hem
            if cross:
                b.box(x + k * 0.006 - 0.003, x + k * 0.006 + 0.003, 0.02, 0.04, 0.24, 0.37, trim)
                b.box(x + k * 0.006 - 0.003, x + k * 0.006 + 0.003, -0.025, 0.085, 0.31, 0.33, trim)
        b.obox((0, 0.02, 0.418), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.106, 0.2, 0.008, cloth, bevel=0.004)  # over the back
        b.obox((0, -0.24, 0.3), (1, 0, 0), (0, 0.35, 1), (0, -1, 0.35), 0.1, 0.1, 0.008, cloth, bevel=0.004)  # the chest
        b.box(-0.1, 0.1, -0.29, -0.27, 0.19, 0.205, trim)
        b.obox((0, 0.27, 0.33), (1, 0, 0), (0, -0.3, 1), (0, 1, 0.3), 0.1, 0.085, 0.008, cloth, bevel=0.004)  # the rump
        # the saddle back on top of the cloth
        b.obox((0, -0.005, 0.432), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.058, 0.06, 0.012, horse.TACK, bevel=0.008)
        b.obox((0, 0.06, 0.455), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.05, 0.012, 0.022, horse.TACK, bevel=0.006)
    return build


# where the rider's left fist holds the reins: over the pommel, a hand's breadth up and forward (the horse's rest
# space). The user, 2026-11-16: «они странно держат упряжки, нужно сделать точнее» -- the reins ran to the pommel
# while the hands were elsewhere; now they run from the bit to this point and the left hand is put on it.
REIN_HAND = Vector((0.0, -0.13, 0.52))


def _neck(b):
    """horse.neck with the reins gathered up into the rider's left fist instead of lying on the pommel."""
    nb, pl = horse.NECK_BASE, horse.POLL
    b.rod((nb[0], nb[1], nb[2] - 0.04), (pl[0], pl[1] + 0.02, pl[2] - 0.03), 0.115, horse.COAT)
    b.rod((nb[0], nb[1] + 0.03, nb[2] + 0.02), (pl[0], pl[1] + 0.035, pl[2] + 0.005), 0.08, horse.COAT)
    b.rod((0, nb[1] + 0.06, nb[2] + 0.045), (0, pl[1] + 0.04, pl[2] + 0.035), 0.032, horse.COAT_DARK)
    b.rod((0.022, nb[1] + 0.06, nb[2] + 0.02), (0.018, pl[1] + 0.045, pl[2] + 0.005), 0.028, horse.COAT_DARK)
    bit = horse._at(0.2, -0.004)
    for k in (-1, 1):
        b.rod((k * 0.038, bit.y, bit.z), (REIN_HAND.x + k * 0.012, REIN_HAND.y, REIN_HAND.z), 0.007, horse.TACK)


def hold_reins(h_arm, r_arm, scale, q):
    """The left arm's angles that put the left fist on the reins (a coarse-to-fine search over the shoulder's
    pitch and swing and the elbow): writes them into q and returns q."""
    import bpy
    import gate_talk_anim as gt
    import kids_anim as ka
    import ardan_anim as aa
    target = gt.bone_world(h_arm, "neck") @ REIN_HAND

    def miss(a, b_, c):
        q["upper_arm.L"] = [a, 0, b_]
        q["forearm.L"] = [c, 0, 0]
        aa.rest(r_arm)
        ka.apply(r_arm, q, scale)
        bpy.context.view_layer.update()
        pb = r_arm.pose.bones["forearm.L"]
        return ((r_arm.matrix_world @ pb.tail) - target).length

    best = (1e9, 0, 0, 0)
    for step, span in ((10, None), (3, 12), (1, 4)):
        if span is None:
            grid = [(a, b_, c) for a in range(-90, 31, step) for b_ in range(-40, 41, step) for c in range(-120, 1, step)]
        else:
            _, a0, b0, c0 = best
            grid = [(a, b_, c) for a in range(a0 - span, a0 + span + 1, step)
                    for b_ in range(b0 - span, b0 + span + 1, step) for c in range(c0 - span, c0 + span + 1, step)]
        for a, b_, c in grid:
            m = miss(a, b_, c)
            if m < best[0]:
                best = (m, a, b_, c)
    m, a, b_, c = best
    q["upper_arm.L"] = [a, 0, b_]
    q["forearm.L"] = [c, 0, 0]
    print(f"REINS miss {m:.3f} upper_arm.L {[a, 0, b_]} forearm.L {[c, 0, 0]}")
    aa.rest(r_arm)
    ka.apply(r_arm, q, scale)
    return q


HORSES = {
    # name: coat, coat dark, cloth, trim, cross
    "CineWarhorse": ('wood_dark', 'door', BLUE, GOLD, True),
    "CineWarhorseGrey": ('stone_mid', 'iron', BLUE, GOLD, True),
    "CineKingHorse": (WHITE, 'stone_light', RED, GOLD, False),
}


def make_horse(name, location=(0, 0, 0), obj=None):
    """One of HORSES; `obj` names the object when a scene needs several of one kind (fg.make reuses by name)."""
    coat, dark, cloth, trim, cross = HORSES[name]
    keep = horse.COAT, horse.COAT_DARK
    horse.COAT, horse.COAT_DARK = coat, dark
    fg.plain(coat, dark)
    parts = [(n, _caparison(cloth, trim, cross) if n == "body" else _neck if n == "neck" else f) for n, f in horse.PARTS]
    try:
        return fg.make(obj or name, parts, horse.SCALE, location)
    finally:
        horse.COAT, horse.COAT_DARK = keep


# ---------------------------------------------------------------- the review

def render(out):
    import bpy
    from mathutils import Matrix, Vector
    import ardan_anim as aa
    import gate_talk_anim as gt
    import kids_anim as ka
    import king_ride
    import rider_anim as ra
    os.makedirs(out, exist_ok=True)
    pairs = []
    for i, (hname, who) in enumerate((("CineKingHorse", "king"), ("CineWarhorse", "cav"), ("CineWarhorseGrey", "cav2"))):
        hm = make_horse(hname)
        h_arm = ka.rig_figure(hm, horse.SCALE, bones=ra.HORSE_BONES)
        if who == "king":
            rm, scale = king_ride.make(), king_ride.SCALE
        else:
            rm = make() if who == "cav" else fg.make("CineCavalry2", PARTS, SCALE, (0, 0, 0))
            scale = SCALE
        r_arm = ka.rig_figure(rm, scale)
        pairs.append((h_arm, r_arm, who, scale, i))
    sc = bpy.context.scene
    keep = set()
    for h_arm, r_arm, who, scale, i in pairs:
        x, y = (0.0, 0.0) if who == "king" else ((-0.55, 0.75) if who == "cav" else (0.55, 0.85))
        h_arm.location = (x, y, 0)
        h_arm.rotation_euler = (0, 0, 0)
        aa.rest(h_arm)
        ra.h_apply(h_arm, ra.gait(ra.h_blank(), 0.15 + 0.3 * i, 1.0))
        bpy.context.view_layer.update()
        r_arm.matrix_world = gt.bone_world(h_arm, "body") @ Matrix.Translation(ra.SEAT)
        aa.rest(r_arm)
        q = ra.seated(0.0, 1.0, 0.0)
        if who == "king":                                   # the sword up: «Вперёд!»
            q["upper_arm.R"] = [-150, 10, 0]
            q["forearm.R"] = [-20, 0, 0]
        else:                                               # the lance lowered for the charge, the shield up
            q["upper_arm.R"] = [-78, 4, 0]
            q["forearm.R"] = [-6, 0, 0]
        ka.apply(r_arm, q, scale)
        hold_reins(h_arm, r_arm, scale, q)
        for o in (h_arm, r_arm):
            keep.add(o.name)
            keep |= {c.name for c in o.children}
    for o in sc.objects:
        if o.type in ('MESH', 'CURVE', 'EMPTY', 'ARMATURE'):
            o.hide_render = o.name not in keep
    ground = bpy.data.objects.new("CavGround", bpy.data.meshes.new("CavGround"))
    ground.data.from_pydata([(-6, -6, 0), (6, -6, 0), (6, 6, 0), (-6, 6, 0)], [], [(0, 1, 2, 3)])
    mat = bpy.data.materials.new("CavGrass")
    mat.diffuse_color = (0.35, 0.6, 0.25, 1)
    mat.use_nodes = True
    mat.node_tree.nodes["Principled BSDF"].inputs[0].default_value = (0.35, 0.6, 0.25, 1)
    ground.data.materials.append(mat)
    sc.collection.objects.link(ground)
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 1600, 900
    sc.view_settings.view_transform = 'Standard'
    for name, energy, rx, rz in (("CavSun", 3.0, 40, -40), ("CavFill", 0.9, 60, 150)):
        light = bpy.data.objects.new(name, bpy.data.lights.new(name, 'SUN'))
        light.data.energy = energy
        sc.collection.objects.link(light)
        light.rotation_euler = (math.radians(rx), 0, math.radians(rz))
    w = sc.world or bpy.data.worlds.new("W")
    sc.world = w
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.72, 1.0, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 0.8
    cam = bpy.data.objects.new("CavCam", bpy.data.cameras.new("CavCam"))
    sc.collection.objects.link(cam)
    sc.camera = cam
    for name, at, frm, lens in (("cavalry_front", (0, 0.3, 0.55), (1.6, -3.0, 1.0), 40),
                                ("cavalry_side", (0, 0.4, 0.55), (3.4, 0.4, 0.9), 40),
                                ("cavalry_king", (0, 0.0, 0.75), (0.9, -1.3, 0.95), 45),
                                ("cavalry_rider", (-0.55, 0.75, 0.7), (-1.6, -0.5, 0.95), 45)):
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
