"""The elder's opening-cutscene animations (2026-11-10, after the children's were approved), each
played against Ардан's approved clips so the timing can be judged together:
  * ElderTalk (shot 4): he stops Ардан with a raised hand, talks, sweeps the hand at the stones
    («Эти камни старше деревни…»), nods; when the runes wake he turns to them and draws back.
  * ElderShout (shot 13): the portal stands open behind Ардан; the elder starts, turns to him,
    shoves him by the shoulder and flings his arm out the way to run: «Беги!».
His right hand keeps the staff planted throughout. Arms that must reach a point (a shoulder, the
stones) are AIMED at it in world space rather than guessed in angles. Never saves.

    blender --background <Buildings-Size1-Models.blend> --python elder_anim.py -- render <out dir> [talk|shout|all]
    ARDAN_SHEET=1,30,60 ... -- frames as PNGs instead of the videos
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
import elder  # noqa: E402
import kids_anim as ka  # noqa: E402
import portal  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
AIM = "aim.upper_arm.L"
ELDER_AT = Vector((0.36, -0.3, 0.0))
ARDAN_SHOULDER_L = Vector((0.1, 0.0, 0.37)) * ardan.SCALE


def stoop(v):
    """elder.stoop for one point: everything above the waist leans forward 0.18 per unit up."""
    v = Vector(v)
    if v.z > 0.27:
        v.y -= (v.z - 0.27) * 0.18
    return v


# ---------------------------------------------------------------- aiming

def blank():
    q = a2.blank()
    q[AIM] = [0.0, -1.0, 0.0, 0.0]          # a world direction and how much of it (0 = not aimed)
    return q


def apply(arm, pose, scale):
    """Angles first; then the aimed upper arm is turned, about its shoulder, to point along the
    world direction (blended by the weight), and its forearm keeps its own bend on top."""
    aim = pose.get(AIM)
    ka.apply(arm, {k: v for k, v in pose.items() if k != AIM}, scale)
    if not aim or aim[3] < 0.01:
        return
    bpy.context.view_layer.update()
    pb = arm.pose.bones["upper_arm.L"]
    m = pb.matrix.copy()                                     # armature space, own rotation included
    want = (arm.matrix_world.to_3x3().inverted() @ Vector(aim[:3])).normalized()
    now = (m.to_3x3() @ Vector((0, 1, 0))).normalized()
    q = now.rotation_difference(want)
    q = Matrix.Identity(3).to_quaternion().slerp(q, min(1.0, aim[3]))
    head = m.to_translation()
    pb.matrix = Matrix.Translation(head) @ q.to_matrix().to_4x4() @ Matrix.Translation(-head) @ m
    bpy.context.view_layer.update()


def toward(frm, to):
    return list((Vector(to) - Vector(frm)).normalized())


def staff_still(q):
    """The right hand keeps the staff planted: the arm as built, the slightest sway."""
    q["upper_arm.R"] = [0, 0, 0]
    q["forearm.R"] = [0, 0, 0]
    return q


def idle(t):
    q = staff_still(blank())
    br = math.sin(TAU * t / 3.5)
    q["chest"] = [1.5 * br, 0, 0]
    q["head"] = [-2 + 1.5 * br, 0, 0]
    q["upper_arm.L"] = [3, -2, 0]
    q["forearm.L"] = [-10, 0, 0]
    return q


def talking(q, t, amount):
    """Speech: small nods of the head and a lift of the chest on the words."""
    q["head"][0] += amount * (4 * math.sin(TAU * t * 2.6) + 2 * math.sin(TAU * t * 1.1))
    q["chest"][0] -= amount * 1.5 * abs(math.sin(TAU * t * 1.3))
    return q


# ---------------------------------------------------------------- the talk (shot 4)

TALK_SECONDS = a2.AMULET_SECONDS + 0.4
STONES_TALK = Vector((0.0, -1.9, 0.25))


def facing():
    d = Vector((0, 0, 0)) - ELDER_AT
    return Vector((d.x, d.y, 0)).normalized()


def head_turn(target):
    """Degrees to turn the head (positive = to his left) to look at a world point."""
    f = facing()
    g = Vector((target[0] - ELDER_AT.x, target[1] - ELDER_AT.y, 0)).normalized()
    return max(-60.0, min(60.0, math.degrees(math.atan2(f.x * g.y - f.y * g.x, f.dot(g)))))


def shoulder():
    """His left shoulder in the world (he stands at ELDER_AT facing Ардан)."""
    f = facing()
    left = Vector((-f.y, f.x, 0))
    sh = stoop(Vector(elder_joint()))
    return ELDER_AT + left * sh.x + f * -sh.y + Vector((0, 0, sh.z)) * elder.SCALE


def elder_joint():
    return (0.096, 0.0, 0.384)


ARDAN_HEAD = Vector((0, 0, 0.5)) * ardan.SCALE


def talk_pose(t):
    q = idle(t)
    sh = shoulder()
    # 0-0.7 s: a raised hand -- «погоди-ка» -- toward Ардан's face, the forearm up
    stop = a2.env(t, 0.0, 0.6) * (1 - a2.env(t, 1.2, 1.5))
    # 1.3-2.8 s: the hand sweeps out at the stones as he speaks
    sweep = a2.env(t, 1.4, 1.9) * (1 - a2.env(t, 2.9, 3.3))
    d_stop = Vector(toward(sh, ARDAN_HEAD + Vector((0, 0, -0.05))))
    d_sweep = Vector(toward(sh, STONES_TALK))
    w = max(stop, sweep)
    d = (d_stop * stop + d_sweep * sweep)
    if d.length > 1e-6:
        q[AIM] = list(d.normalized()) + [w * 0.95]
    q["forearm.L"] = [-10 - 25 * stop - 5 * sweep, 0, 0]
    talking(q, t, a2.env(t, 0.5, 0.8) * (1 - a2.env(t, 3.1, 3.3)))
    q["head"][2] += head_turn(STONES_TALK) * 0.6 * sweep
    # 3.3 s: the runes wake -- he turns to the stones and draws back, the hand up before him
    u = a2.env(t, 3.3, 3.6)
    q["head"][2] += head_turn(STONES_TALK) * 0.9 * u
    q["head"][0] -= 8 * u
    q["chest"][0] -= 7 * u
    q["upper_arm.L"][0] -= 35 * u
    q["forearm.L"][0] -= 55 * u
    return q


# ---------------------------------------------------------------- the shout (shot 13)

HOLD = 1.0                                   # Ардан stands this long before his drop-and-run starts
SHOUT_SECONDS = HOLD + a2.DROP_SECONDS
PORTAL_AT = Vector((0.0, 2.1, 0.0))
RUN_WAY = Vector((0.4, -3.5, 0.45))


def ardan_at(t):
    return Vector((0, -a2.run_distance(max(0.0, t - HOLD)), 0))


def shout_pose(t):
    q = idle(t)
    sh = shoulder()
    portal_c = PORTAL_AT + Vector((0, 0, 0.8))
    # 0.25 s: he starts at the portal behind Ардан
    look = a2.env(t, 0.25, 0.45) * (1 - a2.env(t, 0.6, 0.85))
    q["head"][2] += head_turn(portal_c) * look
    q["head"][0] -= 12 * look
    q["chest"][0] -= 6 * look
    # 0.6-0.95 s: back to Ардан, the arm drawn back, leaning in to shout
    wind = a2.env(t, 0.6, 0.9) * (1 - a2.env(t, 0.95, 1.1))
    q["upper_arm.L"][0] += 35 * wind
    q["forearm.L"][0] -= 70 * wind
    lean = a2.env(t, 0.65, 1.1) * (1 - 0.5 * a2.env(t, 2.2, 3.0))
    q["chest"][0] += 14 * lean
    q["hips"] = [8 * lean, 0, 0]
    q["head"][0] -= 14 * lean                   # chin up: shouting
    # 0.95-1.3 s: the shove at his left shoulder; 1.3-1.8 s: the arm flung out the way to run
    push = a2.env(t, 0.95, 1.25) * (1 - a2.env(t, 1.35, 1.6))
    point = a2.env(t, 1.35, 1.75)
    d = Vector(toward(sh, ARDAN_SHOULDER_L)) * push + Vector(toward(sh, RUN_WAY)) * point
    if d.length > 1e-6:
        q[AIM] = list(d.normalized()) + [max(push, point * (1 - 0.3 * a2.env(t, 3.0, 4.0)))]
    q["forearm.L"][0] += 5 * max(push, point)
    # his eyes follow Ардан as he runs
    q["head"][2] += head_turn(ardan_at(t) + Vector((0, 0, 0.4))) * 0.7 * a2.env(t, 1.3, 1.8)
    return q


# ---------------------------------------------------------------- render

def shoot(out, name, seconds, at, off, lens):
    sc = bpy.context.scene
    cam = sc.camera
    sc.frame_start, sc.frame_end = 1, int(seconds * FPS) + 1
    at = Vector(at)
    cam.location = at + Vector(off)
    cam.data.lens = lens
    cam.rotation_euler = (at - cam.location).to_track_quat('-Z', 'Y').to_euler()
    sheet = os.environ.get("ARDAN_SHEET")
    if sheet:
        sc.render.image_settings.file_format = 'PNG'
        for f in [int(x) for x in sheet.split(",")]:
            sc.frame_set(f)
            sc.render.filepath = os.path.join(out, "%s_%03d.png" % (name, f))
            bpy.ops.render.render(write_still=True)
    else:
        st = sc.render.image_settings
        try:
            st.media_type = 'VIDEO'
        except (AttributeError, TypeError):
            pass
        st.file_format = 'FFMPEG'
        sc.render.ffmpeg.format = 'MPEG4'
        sc.render.ffmpeg.codec = 'H264'
        sc.render.ffmpeg.constant_rate_factor = 'HIGH'
        sc.render.filepath = os.path.join(out, name + ".mp4")
        bpy.ops.render.render(animation=True)
    print("RENDERED", name)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = argv[1]
    what = argv[2] if len(argv) > 2 else "all"
    os.makedirs(out, exist_ok=True)
    mesh = ardan.make(Vector((0, 0, 0)))
    arm = aa.rig(mesh)
    prop = aa.sack(arm, mesh.data.materials[0])
    gem, em, light = a2.glow(mesh, arm)
    old = elder.make((0, 0, 0))
    erig = ka.rig_figure(old, elder.SCALE, bend=stoop)
    plat, stones, sw = portal.make((0, -2.4, 0))
    keep = {mesh.name, arm.name, prop.name, gem.name, light.name, old.name, erig.name, plat.name, sw.name}
    keep |= {s.name for s in stones}
    aa.stage(keep)
    for o in bpy.context.scene.objects:
        if o.name.startswith("ArdanPath") or o.name.startswith("ArdanTuft"):
            o.hide_render = True
    track = ka.Track(elder.SCALE, 0.5)
    track.apply = apply
    if what in ("talk", "all"):
        a2.bake(arm, "CineArdan_Amulet", TALK_SECONDS, a2.amulet_pose)
        a2.key_glow(gem, em, light, TALK_SECONDS)
        track.moves = []
        track.add('pose', TALK_SECONDS, ELDER_AT, talk_pose, (0, 0, 0))
        track.bake(erig, "CineElder_Talk")
        shoot(out, "elder_talk", TALK_SECONDS, (0.1, -0.55, 0.3), (-1.55, -0.55, 0.35), 30)
    if what in ("shout", "all"):
        for c in (gem, light):
            c.animation_data_clear()
        gem.hide_render = True
        light.data.energy = 0
        plat, stones, sw = portal.make(tuple(PORTAL_AT))
        portal.set_arch(stones, sw, True)
        for o in [plat, sw] + stones:
            o.hide_render = False
        a2.bake(arm, "CineArdan_DropRun", SHOUT_SECONDS, lambda t: a2.drop_pose(max(0.0, t - HOLD)),
                root=lambda t: a2.run_distance(max(0.0, t - HOLD)))
        keep_release = a2.RELEASE
        a2.RELEASE = keep_release + HOLD          # the sack is baked on the clip's own clock
        a2.key_sack(prop, arm, SHOUT_SECONDS)
        a2.RELEASE = keep_release
        track.moves = []
        track.add('pose', SHOUT_SECONDS, ELDER_AT, shout_pose, (0, 0, 0))
        track.bake(erig, "CineElder_Shout")
        shoot(out, "elder_shout", SHOUT_SECONDS, (0.15, 0.1, 0.4), (-1.9, -1.4, 0.25), 30)


if __name__ == "__main__":
    main()
