"""The builders at work on the house under construction (main menu concept 1, stage 2, the user
2026-11-17), as loops for the menu's village:
  * CineCarpenter_Hammer (4.8 s): on the scaffold's deck, facing the front wall's top plate -- five
    blows on a nail held in his left fingers, a look at the work, a fresh nail from the pouch at his hip.
  * CineLabourer_Carry (10.4 s): squats at the plank pile, swings a plank onto his right shoulder,
    carries it to the stack at the scaffold's foot, sets it down, walks back.
Both loops end in their first pose. Renders the set and the cast for the user's approval (before
Unity sees anything): stills of the house, a lineup with Ардан and the peasant for scale, the loop as
a video. Never saves.

    blender --background <Buildings-Size1-Models.blend> --python builders_anim.py -- render <out dir> [stills|cast|work|all]
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import bpy  # noqa: E402
import ardan  # noqa: E402
import ardan_anim as aa  # noqa: E402
import ardan_anim2 as a2  # noqa: E402
import bell_anim as ba  # noqa: E402
import builders as bd  # noqa: E402
import figure as fg  # noqa: E402
import kids_anim as ka  # noqa: E402
import townsfolk as tf  # noqa: E402
import townsfolk_anim as tfa  # noqa: E402
import village  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
S = fg.SET_SCALE

# ---------------------------------------------------------------- the layout (set units, the house at the origin, its front at -y)

HS = bd.HS
DECK_TOP = (bd.DECK + 0.006) * HS
CARP_AT = Vector((0.0, (bd.SCAF_Y[1] - 0.04) * HS, DECK_TOP))      # mid-bay, between the poles
CARP_HEAD = math.pi                                  # facing +y, the wall
NAIL = Vector((CARP_AT.x + 0.035, -bd.HY * HS - 0.04, bd.EAVE * HS))
PILE_AT = Vector((2.35, -1.95, 0.0))
STACK_AT = Vector((0.65, -1.95, 0.0))       # on the pile's line: he walks straight, both at his right
SAW_AT = Vector((1.75, 0.55, 0.0))
PILE_TOP = PILE_AT + Vector((0, 0, (0.05 + 5 * 0.016) * S + 0.012))
STACK_TOP = STACK_AT + Vector((0, 0, (0.025 + 2 * 0.016) * S + 0.012))
A = PILE_AT + Vector((0.0, -0.3, 0.0))               # where he stands to pick
B = STACK_AT + Vector((0.0, -0.3, 0.0))              # and to set down
CS = bd.FIGURES["CineCarpenter"][1]
LS = bd.FIGURES["CineLabourer"][1]
WALK_SPEED = 0.42                                    # the cast's own walk (OpeningCast.json)
HAMMER = 4.8
WALK = (A - B).length / WALK_SPEED
T_PICK, T_TURN, T_PUT = 1.6, 0.5, 1.4
CARRY = T_PICK + WALK + T_PUT + T_TURN + WALK + T_TURN


def shoulder(at, heading, side, scale, z=0.384):
    return tfa.shoulder(at, heading, side, scale, z)


def aim(q, side, frm, to, weight=1.0):
    tfa.aim(q, side, frm, to, weight)


def stand():
    q = a2.blank()
    q["chest"] = [2, 0, 0]
    return q


# ---------------------------------------------------------------- the carpenter

def hammer_blow(u):
    """One blow, u 0..1: the hammer up and back, then down on the nail (impact at 0.62), a small bounce."""
    if u < 0.5:
        return a2.ease(u / 0.5)                     # 0 on the nail .. 1 raised
    if u < 0.62:
        return 1 - ((u - 0.5) / 0.12) ** 2         # the fast drop
    return 0.06 * math.sin(math.pi * (u - 0.62) / 0.38)


def carpenter_pose(t):
    q = stand()
    t %= HAMMER
    sr = shoulder(CARP_AT, CARP_HEAD, "R", CS)
    sl = shoulder(CARP_AT, CARP_HEAD, "L", CS)
    blows = 5
    hit_end = blows * 0.5
    rest = a2.env(t, hit_end, hit_end + 0.25) * (1 - a2.env(t, HAMMER - 0.35, HAMMER - 0.05))
    pouch = a2.env(t, hit_end + 1.0, hit_end + 1.35) * (1 - a2.env(t, hit_end + 1.8, hit_end + 2.15))
    # the right arm: raised = the hand high over his shoulder, the forearm folded back; down = on the nail
    r = hammer_blow((t % 0.5) / 0.5) if t < hit_end else 0.0
    up = sr + Vector((-0.02, 0.02, 0.17)) * CS
    on = NAIL + Vector((0.0, -0.08, 0.06)) * CS
    hand = on.lerp(up, r)
    low = sr + Vector((-0.03, 0.06, -0.2)) * CS      # resting: the hammer down at his side
    hand = hand.lerp(low, rest)
    aim(q, "R", sr, hand)
    q["forearm.R"] = [-30 - 85 * r - 10 * (1 - rest), 0, 0]
    # the left hand holds the nail; at rest it goes to the pouch at the left hip for another
    hold = NAIL + Vector((0.035, -0.04, -0.02)) * CS
    hip = CARP_AT + Matrix.Rotation(CARP_HEAD, 3, 'Z') @ Vector((0.09, -0.02, 0.2)) * CS
    lh = hold.lerp(hip, pouch)
    aim(q, "L", sl, lh)
    q["forearm.L"] = [-35 * (1 - pouch) - 15 * pouch, 0, 0]
    # the body leans into each blow, the head watches the nail, then looks the work over
    q["chest"] = [6 + 6 * (1 - r) * (1 - rest), 0, -4 * r * (1 - rest)]
    look = math.sin(TAU * (t - hit_end) / 1.2) * rest * (1 - pouch)
    q["head"] = [10 * (1 - rest) + 18 * pouch, 0, 18 * look]
    q["hips"] = [0, 0, 3 * r * (1 - rest)]
    return CARP_AT, CARP_HEAD, q


# ---------------------------------------------------------------- the labourer

def heading_to(d):
    return math.atan2(d.x, -d.y)


H_GO = heading_to(B - A)                             # towards the stack, the pile and the stack at his right
H_BACK = heading_to(A - B)
H_PLANK = H_GO                                       # the plank lies along x on the pile, the stack and his shoulder


def bend(c):
    """Squat and lean to reach the planks low beside him."""
    q = ka.crouch(0.75 * c)
    q["chest"][0] += 30 * c
    q["chest"][2] -= 15 * c                          # turned a little to his right, to the planks
    q["head"][0] += 10 * c
    return q


def carry_arms(q, at, heading, load):
    """The right hand over the plank on his shoulder (load 0..1 blends from free)."""
    sr = shoulder(at, heading, "R", LS)
    fwd = Matrix.Rotation(heading, 3, 'Z') @ Vector((0, -1, 0))
    side = Matrix.Rotation(heading, 3, 'Z') @ Vector((-1, 0, 0))
    if load > 0.01:
        aim(q, "R", sr, sr + side * 0.07 * LS + fwd * 0.05 * LS + Vector((0, 0, 0.1)) * LS, load)
        q["forearm.R"] = [-120 * load, 0, 0]


def plank_on_shoulder(at, heading):
    sr = shoulder(at, heading, "R", LS)
    side = Matrix.Rotation(heading, 3, 'Z') @ Vector((-1, 0, 0))
    return sr + side * 0.012 + Vector((0, 0, 0.03 * LS))


def walk_q(dist, load):
    q = tfa.walk_pose((dist / (WALK_SPEED * 1.25)) % 1.0, stride=20.0)
    s = math.sin(TAU * (dist / (WALK_SPEED * 1.25)))
    q["upper_arm.L"] = [-18 * s, 0, 0]
    if load < 0.01:
        q["upper_arm.R"] = [18 * s, 0, 0]
    return q


def reach(q, at, heading, target, w, sink=0.0):
    for side in ("L", "R"):
        s = shoulder(at, heading, side, LS) + Vector((0, 0, -sink * LS))
        fwd = Matrix.Rotation(heading, 3, 'Z') @ Vector((0, -1, 0))
        aim(q, side, s, target + fwd * (0.12 if side == "L" else -0.12) + Vector((0, 0, 0.05)), w)


def turn(h0, h1, u):
    return h0 + ((h1 - h0 + math.pi) % TAU - math.pi) * a2.ease(u)


def labourer_at(t):
    """(root, heading, pose, plank (point, heading)) at t into the loop."""
    t %= CARRY
    t1 = T_PICK
    t2 = t1 + WALK
    t3 = t2 + T_PUT
    t4 = t3 + T_TURN
    t5 = t4 + WALK
    if t < t1:                                       # pick: down, grip, up onto the shoulder
        c = a2.env(t, 0.0, 0.6) * (1 - a2.env(t, 0.85, 1.4))
        lift = a2.env(t, 0.75, 1.45)
        q = bend(c)
        reach(q, A, H_GO, PILE_TOP, c * (1 - lift), 0.12 * c)
        carry_arms(q, A, H_GO, lift)
        p = PILE_TOP.lerp(plank_on_shoulder(A, H_GO), a2.ease(lift))
        return A, H_GO, q, (p, H_PLANK)
    if t < t2:                                       # carry it over
        d = (t - t1) * WALK_SPEED
        at = A + (B - A).normalized() * d
        q = walk_q(d, 1)
        carry_arms(q, at, H_GO, 1)
        return at, H_GO, q, (plank_on_shoulder(at, H_GO), H_PLANK)
    if t < t3:                                       # set it down on the stack at his right
        u = t - t2
        c = a2.env(u, 0.25, 0.8) * (1 - a2.env(u, 1.0, 1.4))
        down = a2.env(u, 0.15, 0.8)
        q = bend(c)
        carry_arms(q, B, H_GO, 1 - down)
        reach(q, B, H_GO, STACK_TOP, down * (1 - a2.env(u, 0.9, 1.3)), 0.12 * c)
        p = plank_on_shoulder(B, H_GO).lerp(STACK_TOP, a2.ease(down))
        return B, H_GO, q, (p, H_PLANK)
    if t < t4:                                       # turn round
        return B, turn(H_GO, H_BACK, (t - t3) / T_TURN), stand(), (STACK_TOP, H_PLANK)
    if t < t5:                                       # walk back, hands free
        d = (t - t4) * WALK_SPEED
        at = B + (A - B).normalized() * d
        return at, H_BACK, walk_q(d, 0), (STACK_TOP, H_PLANK)
    return A, turn(H_BACK, H_GO, (t - t5) / T_TURN), stand(), (STACK_TOP, H_PLANK)


def labourer_pose(t):
    at, h, q, _ = labourer_at(t)
    return at, h, q


# ---------------------------------------------------------------- the scene and the renders

def plank_matrix(point, heading):
    """The carried plank lies along his heading (it is built along x)."""
    return Matrix.Translation(point) @ Matrix.Rotation(heading - math.pi / 2, 4, 'Z')


def build_scene():
    objs = {}
    objs["house"] = bd.make_set("CineHouseBuild", (0, 0, 0))
    objs["pile"] = bd.make_set("CinePlankPile", tuple(PILE_AT))
    objs["stack"] = bd.make_set("CinePlankStack", tuple(STACK_AT))
    objs["saw"] = bd.make_set("CineSawhorse", tuple(SAW_AT))
    objs["saw"].rotation_euler = (0, 0, math.radians(70))
    objs["plank"] = bd.make_set("CinePlank")
    objs["cottage"] = village.make("CineCottage", (-4.3, 0.6, 0))
    carp = ka.rig_figure(bd.make("CineCarpenter"), CS)
    lab = ka.rig_figure(bd.make("CineLabourer"), LS)
    return objs, carp, lab


def track(scale):
    tr = ka.Track(scale, 0.95 * scale * 1.05)
    tr.apply = ba.apply
    return tr


def bake_all(carp, lab, plank, seconds):
    tr = track(CS)
    tr.add('fn', seconds, carpenter_pose)
    tr.bake(carp, "CineCarpenter_Hammer")
    tr = track(LS)
    tr.add('fn', seconds, labourer_pose)
    tr.bake(lab, "CineLabourer_Carry")
    for f in range(int(seconds * FPS) + 1):
        _, _, _, (p, h) = labourer_at(f / FPS)
        plank.matrix_world = plank_matrix(p, h)
        plank.keyframe_insert("location", frame=f + 1)
        plank.keyframe_insert("rotation_euler", frame=f + 1)


def still(out, name, at, off, lens, frame=1):
    sc = bpy.context.scene
    cam = sc.camera
    sc.frame_set(frame)
    at = Vector(at)
    cam.location = at + Vector(off)
    cam.data.lens = lens
    cam.rotation_euler = (at - cam.location).to_track_quat('-Z', 'Y').to_euler()
    sc.render.image_settings.file_format = 'PNG'
    sc.render.filepath = os.path.join(out, name + ".png")
    bpy.ops.render.render(write_still=True)
    print("RENDERED", name)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = argv[1]
    what = argv[2] if len(argv) > 2 else "all"
    os.makedirs(out, exist_ok=True)
    objs, carp, lab = build_scene()
    keep = {o.name for o in objs.values()}
    extra = []
    if what in ("cast", "all"):
        extra.append(ardan.make(Vector((0, 0, 0))))
        extra.append(tf.make("CinePeasant"))
    for r in (carp, lab):
        keep |= {r.name} | {c.name for c in r.children}
    keep |= {o.name for o in extra}
    aa.stage(keep)
    sc = bpy.context.scene
    sc.render.resolution_x, sc.render.resolution_y = 1600, 900
    for o in sc.objects:
        if o.name.startswith("ArdanPath") or o.name.startswith("ArdanTuft"):
            o.hide_render = True
    for o in extra:
        o.hide_render = True
    seconds = 2 * CARRY if what in ("work", "all") else 6.0
    bake_all(carp, lab, objs["plank"], seconds)
    sc.frame_start, sc.frame_end = 1, int(seconds * FPS) + 1
    if what in ("stills", "all"):
        mid = Vector((0.6, -0.7, 0.8))
        still(out, "house_front", mid, (3.4, -6.8, 2.9), 40, frame=int(3.5 * FPS))
        still(out, "house_back", mid, (-4.4, 5.6, 3.0), 40, frame=int(3.5 * FPS))
        still(out, "house_scaffold_close", CARP_AT + Vector((0, 0, 0.3)), (0.6, -2.3, 0.8), 42, frame=int(0.3 * FPS))
        still(out, "house_with_cottage", Vector((-1.6, -0.4, 0.8)), (2.2, -9.6, 3.6), 38, frame=int(3.5 * FPS))
    if what in ("cast", "all"):
        # the lineup: the two builders by Ардан and the peasant, on the ground for scale
        carp.animation_data.action = None
        lab.animation_data.action = None
        for o in (objs["house"], objs["pile"], objs["stack"], objs["saw"], objs["plank"], objs["cottage"]):
            o.hide_render = True
        for i, r in enumerate((carp, lab)):
            aa.rest(r)
            r.location = (-0.35 + 0.35 * i, 0, 0)
            r.rotation_euler = (0, 0, 0)
        extra[0].location = (0.35, 0.0, 0)
        extra[1].location = (0.7, 0.0, 0)
        for o in extra:
            o.hide_render = False
        bpy.context.view_layer.update()
        still(out, "cast_front", (0.17, 0, 0.27), (0, -3.2, 0.5), 50)
        still(out, "cast_back", (0.17, 0, 0.27), (0, 3.2, 0.6), 50)
        still(out, "cast_threequarter", (0.17, 0, 0.27), (-1.9, -2.5, 0.7), 50)
        still(out, "cast_faces", (-0.17, 0, 0.45), (0.0, -1.2, 0.08), 60)
        for o in extra:
            o.hide_render = True
        for o in (objs["house"], objs["pile"], objs["stack"], objs["saw"], objs["plank"], objs["cottage"]):
            o.hide_render = False
        bake_all(carp, lab, objs["plank"], seconds)
    if what in ("work", "all"):
        tfa.shoot(out, "builders_work", seconds, (1.0, -1.1, 0.7), (2.0, -5.8, 2.0), 38)
        sc.frame_start, sc.frame_end = 1, int(HAMMER * 2 * FPS) + 1
        tfa.shoot(out, "carpenter_close", HAMMER * 2, CARP_AT + Vector((0, 0.05, 0.3)), (0.6, -2.3, 0.8), 42)


if __name__ == "__main__":
    main()
