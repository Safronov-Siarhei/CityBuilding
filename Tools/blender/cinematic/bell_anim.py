"""The priest rings the alarm (shot 12; 2026-11-10, after the orcs' were approved).

The church is built WITHOUT its bell, clapper and rope (church.build(with_bell=False)); here the
bell hangs from its yoke as its own object with a short timber lever to the front, the clapper
swings after it a beat late, and the rope runs from the lever down into the priest's fists. He
stands in the belfry facing the bell and hauls: both arms are AIMED at the rope's grip, which rises
and falls with the bell's swing, and his knees and back give with each pull.
Never saves.

    blender --background <Buildings-Size1-Models.blend> --python bell_anim.py -- render <out dir>
    ARDAN_SHEET=1,30,60 ... -- frames as PNGs instead of the video
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import bpy  # noqa: E402
import ardan_anim as aa  # noqa: E402
import ardan_anim2 as a2  # noqa: E402
import church  # noqa: E402
import figure as fg  # noqa: E402
import kids_anim as ka  # noqa: E402
import priest  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
S = fg.SET_SCALE
SECONDS = 4.4
PERIOD = 1.15            # one swing of the bell, one haul
AMPLITUDE = 24.0         # degrees, once it is going
GRAB = 0.45              # the priest has the rope in his fists
LEVER = Vector((0, -0.155, -0.03))   # from the yoke to where the rope hangs (before S)


def swing(t):
    """The bell's angle about its yoke (degrees): still, then each haul swings it higher."""
    if t < GRAB:
        return 0.0
    u = t - GRAB
    return AMPLITUDE * min(1.0, 0.35 + u / 1.4) * math.sin(TAU * u / PERIOD)


# ---------------------------------------------------------------- the pieces

def pieces(church_obj):
    bf = church.BELFRY
    pivot_pre = Vector((bf["tx"], bf["ty"], bf["top"] - 0.1))
    loc = church_obj.location + pivot_pre * S
    b = Builder()
    church.bell(b, 0, 0, bf["top"] - pivot_pre.z)
    b.rod((0, 0, 0), tuple(LEVER), 0.014, 'wood_dark')                       # the lever the rope hangs from
    b.box(-0.02, 0.02, LEVER.y - 0.012, LEVER.y + 0.012, LEVER.z - 0.02, LEVER.z + 0.012, 'iron')
    bell = fg.install_static("CineBell", b, location=tuple(loc))
    c = Builder()
    church.clapper(c, 0, 0, bf["top"] - pivot_pre.z)
    c.rod((0, 0, -0.02), (0, 0, bf["top"] - pivot_pre.z - 0.33), 0.006, 'iron')
    clap = fg.install_static("CineClapper", c, location=tuple(loc))
    r = Builder()
    r.rod((0, 0, 0), (0, 0, -1.0), 0.012 * S, 'burlap')
    rope = fg.install_static("CineBellRope", r, scale=1.0)
    return bell, clap, rope, loc


# ---------------------------------------------------------------- the priest

AIMS = ("upper_arm.L", "upper_arm.R")


def apply(arm, pose, scale):
    """Angles, then each aimed upper arm turned about its shoulder to point along its direction."""
    aims = {k: pose[k] for k in pose if k.startswith("aim.")}
    ka.apply(arm, {k: v for k, v in pose.items() if not k.startswith("aim.")}, scale)
    bpy.context.view_layer.update()
    for key, aim in aims.items():
        if aim[3] < 0.01:
            continue
        pb = arm.pose.bones[key[4:]]
        m = pb.matrix.copy()
        want = (arm.matrix_world.to_3x3().inverted() @ Vector(aim[:3])).normalized()
        now = (m.to_3x3() @ Vector((0, 1, 0))).normalized()
        q = Matrix.Identity(3).to_quaternion().slerp(now.rotation_difference(want), min(1.0, aim[3]))
        head = m.to_translation()
        pb.matrix = Matrix.Translation(head) @ q.to_matrix().to_4x4() @ Matrix.Translation(-head) @ m
        bpy.context.view_layer.update()


def lever_world(loc, t):
    return Vector(loc) + Matrix.Rotation(math.radians(swing(t)), 3, 'X') @ (LEVER * S)


def make_priest_move(loc, stand, rope_len):
    shoulders = {k: stand + Vector((k * 0.096 * priest.SCALE * -1, 0, 0.384 * priest.SCALE)) for k in (1, -1)}

    def grip(t):
        return lever_world(loc, t) - Vector((0, 0, rope_len))

    def move(t):
        q = a2.blank()
        g = grip(t)
        hold = a2.env(t, 0.05, GRAB)
        low = 0.0
        if t >= GRAB:
            # how far down the haul is: 0 with the rope high, 1 at the bottom of the pull
            low = max(0.0, min(1.0, (stand.z + 0.5 * priest.SCALE - g.z) / (0.3 * priest.SCALE)))
        c = ka.crouch(0.28 * low)
        for k in c:
            q[k] = c[k]
        q["chest"] = [6 + 14 * low, 0, 0]
        q["head"] = [-14 - 6 * low, 0, 0]
        for side, k in (("L", 1), ("R", -1)):
            # facing +y, his left (L) is toward -x
            sh = stand + Vector((-k * 0.096, 0, 0.384)) * priest.SCALE
            fist = g + Vector((-k * 0.012, 0, k * 0.03))
            q["aim.upper_arm." + side] = list((fist - sh).normalized()) + [hold]
            q["forearm." + side] = [-12, 0, 0]
        return stand, math.pi, q

    return move, grip


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = argv[1]
    os.makedirs(out, exist_ok=True)
    ch = church.make(with_bell=False)
    bell, clap, rope, loc = pieces(ch)
    bf = church.BELFRY
    floor = (bf["floor"] + 0.02) * S
    stand = Vector((bf["tx"] * S + 0.03, bf["ty"] * S - 0.36, floor))
    grip_z = floor + 0.47 * priest.SCALE
    rope_len = (loc.z + (LEVER.z * S)) - grip_z
    pr = priest.make((0, 0, 0))
    prig = ka.rig_figure(pr, priest.SCALE)
    move, grip = make_priest_move(loc, stand, rope_len)
    track = ka.Track(priest.SCALE, 0.5)
    track.apply = apply
    track.add('fn', SECONDS, move)
    track.bake(prig, "CinePriest_RingBell")
    sc = bpy.context.scene
    frames = int(SECONDS * FPS) + 1
    for f in range(frames):
        t = f / FPS
        bell.rotation_euler = (math.radians(swing(t)), 0, 0)
        bell.keyframe_insert("rotation_euler", frame=f + 1)
        clap.rotation_euler = (math.radians(swing(max(0.0, t - 0.1))), 0, 0)
        clap.keyframe_insert("rotation_euler", frame=f + 1)
    # the rope: from the lever to between his fists, keyed after his pose is baked
    for f in range(frames):
        sc.frame_set(f + 1)
        t = f / FPS
        top = lever_world(loc, t)
        fists = [prig.matrix_world @ prig.pose.bones["forearm." + s].tail for s in ("L", "R")]
        end = (fists[0] + fists[1]) / 2 if t >= GRAB else grip(t)
        d = end - top
        rope.location = top
        rope.rotation_euler = Vector((0, 0, -1)).rotation_difference(d.normalized()).to_euler()
        rope.scale = (1, 1, d.length)
        for path in ("location", "rotation_euler", "scale"):
            rope.keyframe_insert(path, frame=f + 1)
    keep = {ch.name, bell.name, clap.name, rope.name, prig.name, pr.name}
    aa.stage(keep)
    for o in sc.objects:
        if o.name.startswith("ArdanPath") or o.name.startswith("ArdanTuft"):
            o.hide_render = True
    sc.frame_start, sc.frame_end = 1, frames
    cam = sc.camera
    at = Vector((bf["tx"] * S, bf["ty"] * S - 0.15, floor + 0.38))
    cam.location = at + Vector((2.6, -0.25, 0.1))                          # through the side arch
    cam.data.lens = 38
    cam.rotation_euler = (at - cam.location).to_track_quat('-Z', 'Y').to_euler()
    sheet = os.environ.get("ARDAN_SHEET")
    if sheet:
        sc.render.image_settings.file_format = 'PNG'
        for f in [int(x) for x in sheet.split(",")]:
            sc.frame_set(f)
            sc.render.filepath = os.path.join(out, "priest_bell_%03d.png" % f)
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
        sc.render.filepath = os.path.join(out, "priest_bell.mp4")
        bpy.ops.render.render(animation=True)
    print("RENDERED priest_bell")


if __name__ == "__main__":
    main()
