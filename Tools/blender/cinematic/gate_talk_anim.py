"""Mission 2's opening cutscene, group 1 (the user, 2026-11-15: «Ардан и капитан»): Ардан and the captain of
the guard at the north gate, played together on one clock so the timing is judged as one take.

Ардан (facing +y, the gate behind him) runs in and stops before the captain, bends with his hands on his knees
to get his breath, tells (points back the way he came, flings his arms wide: «огромные твари»), turns at the
lookout's shout to look up at the wall, turns back; then takes the sword the captain holds out and looks at it.

The captain (facing -y) waits with his left hand at his belt, lifts a calming palm («Отдышись, парень»),
listens frowning with his hand at his chin, looks up at the wall at the shout, nods; draws the spare sword
from his right hip and holds it out hilt first; lets go and stands.

The SWORD is Ардан's approved one (ardan_sword.sword): a prop baked as world keys (as the sack is) -- in the
captain's scabbard on his hips, drawn by his right hand, held out by the blade below the guard, and from the
grab on in Ардан's right fist as in mission 2. Clips: CineArdan_Gate, CineCaptain_Gate (one take each; split
into loops later for Unity if wanted). Never saves.

    blender --background <Buildings-Size1-Models.blend> --python gate_talk_anim.py -- render <out dir> [side|front|all]
    ARDAN_SHEET=1,60,120 ... -- frames as PNGs instead of the videos
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
import ardan_sword as asw  # noqa: E402
import captain  # noqa: E402
import elder_anim as ea  # noqa: E402
import kids_anim as ka  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
S, SC = ardan.SCALE, captain.SCALE
env, ease = a2.env, a2.ease

CAPTAIN_AT = Vector((0.0, 0.42, 0.0))     # facing -y, towards the gate
STOP_AT = Vector((0.0, 0.0, 0.0))         # where Ардан pulls up, facing +y
RUN_FROM = Vector((0.0, -1.7, 0.0))       # in through the gate behind him

# the take's beats (seconds)
RUN_END = 2.1          # he stops
PANT_END = 3.8         # straightens
TELL_END = 8.0
SHOUT = 8.1            # the lookout's shout: both look up at the wall
BACK = 9.7             # back to each other
DRAW = 10.3            # the captain's hand on the hilt
OUT = 11.0             # the sword clear and held out
GRAB = 12.0            # Ардан's fist on the grip
LET_GO = 12.35
SECONDS = 14.2


# ---------------------------------------------------------------- Ардан

def a_place(t):
    """(root point, heading) -- the run in easing to a stop, then a turn to the wall and back."""
    dist = (STOP_AT - RUN_FROM).length
    v = a2.RUN_SPEED
    brake = 0.55                                    # the last metres eased off
    run_t = RUN_END - brake
    d_full = v * run_t
    if t < run_t:
        d = v * t
    elif t < RUN_END:
        x = (t - run_t) / brake
        d = d_full + v * brake * (x - x * x / 2)
    else:
        d = d_full + v * brake * 0.5
    pos = RUN_FROM + (STOP_AT - RUN_FROM).normalized() * min(d, dist)
    if t >= RUN_END:
        pos = STOP_AT.copy()
    heading = math.pi                               # facing +y
    turn = env(t, SHOUT + 0.15, SHOUT + 0.75) * (1 - env(t, BACK, BACK + 0.6))
    return pos, heading - math.radians(150) * turn


def a_run_distance(t):
    p, _ = a_place(t)
    return (p - RUN_FROM).length


def a_stand(t):
    q = a2.blank()
    br = math.sin(TAU * t / 2.4)
    q["chest"] = [1.5 * br, 0, 0]
    q["head"] = [-1 + 1.0 * br, 0, 0]
    q["upper_arm.L"] = [2, -4, 0]
    q["upper_arm.R"] = [2, 4, 0]
    q["forearm.L"] = q["forearm.R"] = [-10, 0, 0]
    return q


def a_pant(t):
    """Bent at the hips, hands on his knees, heaving; the head up at the captain."""
    q = a2.blank()
    heave = math.sin(TAU * t * 1.7)
    q["hips"] = [28, 0, 0]
    q["chest"] = [22 + 5 * heave, 0, 0]
    q["head"] = [-48 - 4 * heave, 0, 0]
    for k, s in (("L", 1), ("R", -1)):
        q["thigh." + k] = [-46, 0, 0]
        q["shin." + k] = [30, 0, 0]
        q["foot." + k] = [-12, 0, 0]
        q["upper_arm." + k] = [-22 + 3 * heave, -s * 6, 0]
        q["forearm." + k] = [-8, 0, 0]
    q["hips_loc"] = [0, -0.022, 0]
    return q


def a_tell(t):
    """Talking fast: points back the way he came, then both arms flung wide and up -- «огромные твари»."""
    q = ea.talking(a_stand(t), t, 1.0)
    point = env(t, 4.1, 4.5) * (1 - env(t, 5.4, 5.8))
    q["upper_arm.R"] = [2 + 58 * point, 4 + 6 * point, 0]
    q["forearm.R"] = [-10 + 4 * point, 0, 0]
    q["chest"][2] -= 22 * point
    q["head"][2] -= 30 * point
    wide = env(t, 5.9, 6.35) * (1 - env(t, 7.4, 7.9))
    beat = 6 * math.sin(TAU * t * 2.0) * wide
    q["upper_arm.L"] = [2 - 55 * wide + beat, -4 - 70 * wide, 0]
    q["upper_arm.R"][0] -= 55 * wide - beat
    q["upper_arm.R"][1] += 70 * wide
    q["forearm.L"] = [-10 - 25 * wide, 0, 0]
    q["forearm.R"][0] -= 25 * wide
    q["chest"][0] -= 6 * wide
    q["head"][0] -= 6 * wide
    return q


def a_look_up(t):
    q = a_stand(t)
    up = env(t, SHOUT + 0.2, SHOUT + 0.7) * (1 - env(t, BACK, BACK + 0.5))
    q["head"][0] -= 30 * up
    q["chest"][0] -= 4 * up
    return q


def a_take(t):
    """He reaches for the hilt held out to him, takes it and raises it upright before his face -- a salute --
    looks up the blade, then nods to the captain."""
    reach = env(t, GRAB - 0.75, GRAB) * (1 - env(t, GRAB + 0.3, GRAB + 0.9))
    q = a_stand(t)
    q["upper_arm.R"] = [2 - 52 * reach, 4 + 2 * reach, 0]
    q["forearm.R"] = [-10 - 22 * reach, 0, 0]
    q["head"][0] += 10 * reach
    up = env(t, GRAB + 0.3, GRAB + 1.0)
    q["upper_arm.R"][0] -= 42 * up
    q["forearm.R"][0] -= 78 * up                     # the forearm up: the cocked blade stands upright
    q["upper_arm.R"][1] -= 10 * up                   # the fist in to his middle
    q["head"][0] -= 8 * up * (1 - env(t, 13.3, 13.5))
    q["head"][0] += 12 * math.sin(math.pi * min(1.0, max(0.0, (t - 13.5) / 0.45)))   # the nod
    q["chest"][0] -= 3 * up
    return q


def a_pose(t):
    if t < RUN_END:
        q = a2.run((a_run_distance(t) / (a2.RUN_SPEED * a2.RUN_CYCLE)) % 1.0)
        return a2.mix(q, a_pant(0.0), env(t, RUN_END - 0.3, RUN_END))
    if t < PANT_END:
        return a2.mix(a_pant(t), a_tell(t), env(t, PANT_END - 0.45, PANT_END))
    if t < TELL_END:
        return a_tell(t)
    if t < BACK + 0.6:
        return a2.mix(a_tell(TELL_END), a_look_up(t), env(t, TELL_END, TELL_END + 0.3))
    return a_take(t)


# ---------------------------------------------------------------- the captain

def c_idle(t):
    q = a2.blank()
    br = math.sin(TAU * t / 3.0)
    q["chest"] = [1.2 * br, 0, 0]
    q["head"] = [-2 + 1.0 * br, 0, 0]
    q["upper_arm.L"] = [8, -16, 0]           # the left hand on his belt
    q["forearm.L"] = [-62, 0, 18]
    q["upper_arm.R"] = [0, 3, 0]
    q["forearm.R"] = [-8, 0, 0]
    return q


def c_pose(t):
    q = c_idle(t)
    # the calming palm, low and forward, patting the air: «Отдышись, парень»
    calm = env(t, 2.5, 2.85) * (1 - env(t, 3.7, 4.0))
    q["upper_arm.L"] = [8 - 52 * calm + 6 * math.sin(TAU * t * 2.2) * calm, -16 + 12 * calm, 0]
    q["forearm.L"] = [-62 + 40 * calm, 0, 18 * (1 - calm)]
    ea.talking(q, t, calm)
    # listening, frowning: the hand at his chin, a slow nod; the head back a little at «огромные»
    chin = env(t, 4.2, 4.7) * (1 - env(t, SHOUT, SHOUT + 0.35))
    q["upper_arm.L"][0] -= 30 * chin
    q["upper_arm.L"][1] += 24 * chin
    q["forearm.L"] = [q["forearm.L"][0] - 70 * chin, 0, q["forearm.L"][2] * (1 - chin)]
    q["head"][0] += (6 + 3 * math.sin(TAU * t * 0.7)) * chin - 8 * env(t, 6.0, 6.4) * (1 - env(t, 7.2, 7.6))
    q["head"][2] += 6 * env(t, 6.0, 6.4) * (1 - env(t, 7.2, 7.6))
    # the shout: up at the wall behind Ардан
    up = env(t, SHOUT + 0.1, SHOUT + 0.5) * (1 - env(t, BACK - 0.3, BACK + 0.2))
    q["head"][0] -= 26 * up
    q["chest"][0] -= 5 * up
    # a decisive nod back at Ардан
    q["head"][0] += 10 * math.sin(math.pi * min(1.0, max(0.0, (t - BACK) / 0.5)))
    # the draw: the right hand back to the hilt at his hip, the sword up and out, held forward by the blade
    to_hilt = env(t, DRAW - 0.5, DRAW) * (1 - env(t, DRAW + 0.05, OUT))
    out = env(t, DRAW + 0.05, OUT) * (1 - env(t, LET_GO + 0.1, LET_GO + 0.8))
    q["upper_arm.R"] = [0 + 18 * to_hilt - 48 * out, 3 + 4 * to_hilt + 6 * out, 0]
    q["forearm.R"] = [-8 - 62 * to_hilt - 34 * out, 0, -20 * to_hilt]
    q["chest"][2] -= 10 * to_hilt
    q["head"][0] += 8 * to_hilt + 6 * out
    return q


# ---------------------------------------------------------------- the sword prop

def frame(origin, x, blade):
    """A 4x4 frame: x across the guard, y along the blade, z = x × y, at the grip's centre."""
    blade = Vector(blade).normalized()
    x = (Vector(x) - blade * Vector(x).dot(blade)).normalized()
    z = x.cross(blade)
    m = Matrix.Identity(4)
    for i in range(3):
        m[i][0], m[i][1], m[i][2], m[i][3] = x[i], blade[i], z[i], origin[i]
    return m


def bone_world(arm, bone):
    """The bone's pose carried onto points given in the figure's rest space."""
    return arm.matrix_world @ arm.pose.bones[bone].matrix @ arm.data.bones[bone].matrix_local.inverted()


def key_sword(sword, arm_a, arm_c):
    sc = bpy.context.scene
    sword.modifiers.clear()
    sword.parent = None
    sword.matrix_world = Matrix.Identity(4)
    _, _, wr = ardan._joints(-1)
    grip0 = Vector((wr.x, wr.y, wr.z - 0.034)) * S
    blade0 = Vector((0, -1, -1)).normalized()                  # ardan_sword's cocked wrist
    f0_inv = frame(grip0, (1, 0, 0), blade0).inverted()
    u = captain.SCABBARD_DIR
    mouth = captain.SCABBARD_MOUTH * SC
    sheath = frame(mouth - u * 0.042 * S, (1, 0, 0), u)        # the guard at the scabbard's mouth
    fist_c = Vector((-0.117, -0.03, 0.224)) * SC
    offer_blade = Vector((0.0, 0.95, -0.3)).normalized()       # back towards the captain, hilt to Ардан
    held_back = 0.065 * S                                      # he holds it by the blade below the guard
    for f in range(int(SECONDS * FPS) + 1):
        t = f / FPS
        sc.frame_set(f + 1)
        in_sheath = bone_world(arm_c, "hips") @ sheath @ f0_inv
        fist = bone_world(arm_c, "forearm.R") @ fist_c
        held = frame(fist - offer_blade * held_back, (1, 0, 0), offer_blade) @ f0_inv
        ardan_m = bone_world(arm_a, "forearm.R")
        if t < DRAW:
            m = in_sheath
        elif t < OUT:
            # drawn up out of the scabbard as the hand rises, turning to be held out
            m = blend(in_sheath, held, ease((t - DRAW) / (OUT - DRAW)))
        elif t < GRAB:
            m = held
        else:
            m = blend(held, ardan_m, ease((t - GRAB) / 0.3))
        sword.matrix_world = m
        sword.keyframe_insert("location", frame=f + 1)
        sword.keyframe_insert("rotation_euler", frame=f + 1)
        sword.keyframe_insert("scale", frame=f + 1)


def blend(m0, m1, w):
    l0, r0, _ = m0.decompose()
    l1, r1, _ = m1.decompose()
    return Matrix.Translation(l0.lerp(l1, w)) @ r0.slerp(r1, w).to_matrix().to_4x4()


# ---------------------------------------------------------------- baking and render

def bake(arm, name, scale, place, pose):
    arm.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    for f in range(int(SECONDS * FPS) + 1):
        t = f / FPS
        pos, heading = place(t)
        arm.location = pos
        arm.rotation_euler = (0, 0, heading)
        aa.rest(arm)
        ka.apply(arm, pose(t), scale)
        for pb in arm.pose.bones:
            pb.keyframe_insert("rotation_quaternion", frame=f + 1, group=pb.name)
            pb.keyframe_insert("location", frame=f + 1, group=pb.name)
        arm.keyframe_insert("location", frame=f + 1)
        arm.keyframe_insert("rotation_euler", frame=f + 1)
    return act


def keep_of(actors, props):
    """What a review render shows: the rigs, their meshes, the props."""
    keep = {p.name for p in props}
    for _, _, arm, extra in actors:
        keep |= {arm.name} | {c.name for c in arm.children} | {m.name for m in extra}
    return keep


def build():
    """The take baked: [(export name, take, rig, extra meshes)], [props], seconds (export_m2intro.py)."""
    mesh = ardan.make(Vector((0, 0, 0)))
    arm_a = aa.rig(mesh)
    sword = asw.sword(arm_a, mesh.data.materials[0])
    cap = captain.make((0, 0, 0))
    arm_c = ka.rig_figure(cap, SC)
    bake(arm_a, "CineArdan_Gate", S, a_place, a_pose)
    bake(arm_c, "CineCaptain_Gate", SC, lambda t: (CAPTAIN_AT, 0.0), c_pose)
    key_sword(sword, arm_a, arm_c)
    return [("M2Ardan", "Gate", arm_a, []), ("M2Captain", "Gate", arm_c, [])], [sword], SECONDS


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = argv[1]
    what = argv[2] if len(argv) > 2 else "all"
    os.makedirs(out, exist_ok=True)
    actors, props, _ = build()
    aa.stage(keep_of(actors, props))
    for o in bpy.context.scene.objects:
        if o.name.startswith("ArdanTuft"):
            o.hide_render = True
    shots = (("side", (0.0, -0.35, 0.28), (-2.3, -0.2, 0.45), 32),
             ("front", (0.0, 0.2, 0.3), (1.35, -0.25, 0.22), 36))
    for key, at, off, lens in shots:
        if what in (key, "all"):
            ea.shoot(out, "gate_talk_" + key, SECONDS, at, off, lens)


if __name__ == "__main__":
    main()
