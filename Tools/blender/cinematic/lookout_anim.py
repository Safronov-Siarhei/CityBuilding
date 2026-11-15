"""Mission 2's opening cutscene, group 2 (the user, 2026-11-15: «продолжаем сцену»): the lookout on the wall.
One take, CineLookout_Alarm (9.4 s), facing -y (the field and the forest ahead; his left is +x):

  0.0-2.6  he watches, the spear planted, his head sweeping slowly along the forest edge;
  2.6      he sees: stock-still, leaning in;
  2.9-5.0  he flings the left arm at the forest (aimed, ea.AIM) and shouts «Враг! Из леса идут!» -- the chest
           pumping on the words, a stamp of the foot;
  5.0-6.1  the hand drops to the horn at his hip, takes it and lifts it to his mouth;
  6.1-8.6  he blows: the chest swells, the head goes back, the bell up and out to his left;
  8.6-9.4  the horn lowered a little, still in his hand, looking out.

The HORN is a prop (lookout.make_horn) baked as world keys: on his hips until he takes it, then at his mouth
(the mouthpiece at his lips, the bell up-forward-left, following his head). Never saves.

    blender --background <Buildings-Size1-Models.blend> --python lookout_anim.py -- render <out dir> [front|side|all]
    ARDAN_SHEET=1,60,120 ... -- frames as PNGs instead of the videos
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
import elder_anim as ea  # noqa: E402
import gate_talk_anim as gt  # noqa: E402
import kids_anim as ka  # noqa: E402
import lookout  # noqa: E402
from mathutils import Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
SL = lookout.SCALE
env, ease = a2.env, a2.ease

SEES = 2.6
POINT = 2.9
POINT_END = 5.0
TAKE = 5.45            # his fist on the horn
AT_MOUTH = 6.1
BLOW_END = 8.6
SECONDS = 9.4

FOREST = Vector((0.55, -1.0, -0.2)).normalized()           # out over the parapet, ahead and to his left
SHOULDER_L = Vector((0.096, 0.0, 0.384)) * SL


def base(t):
    """Standing at the parapet, the spear planted at his right, breathing."""
    q = a2.blank()
    br = math.sin(TAU * t / 2.8)
    q["chest"] = [1.2 * br, 0, 0]
    q["head"] = [-2 + 1.0 * br, 0, 0]
    q["upper_arm.L"] = [2, -4, 0]
    q["forearm.L"] = [-10, 0, 0]
    q["upper_arm.R"] = [0, 0, 0]
    q["forearm.R"] = [0, 0, 0]
    q[ea.AIM] = [0.0, -1.0, 0.0, 0.0]
    return q


def pose(t):
    q = base(t)
    # watching: the head sweeps the forest edge, slowly, left to right and back
    watch = 1 - env(t, SEES - 0.2, SEES)
    q["head"][2] += 28 * math.sin(TAU * t / 3.4) * watch
    q["head"][0] += 4 * watch
    # he sees: still, leaning in, the head turned to the forest
    seen = env(t, SEES - 0.15, SEES + 0.1)
    q["head"][2] += 16 * seen * (1 - env(t, AT_MOUTH - 0.4, AT_MOUTH))
    q["chest"][0] += 9 * seen * (1 - env(t, POINT, POINT + 0.3))
    # the point and the shout: the left arm flung at the forest, the chest pumping on the words
    point = env(t, POINT, POINT + 0.3) * (1 - env(t, POINT_END - 0.2, POINT_END + 0.2))
    q[ea.AIM] = list(FOREST) + [0.95 * point]
    q["forearm.L"][0] = -10 + 8 * point
    words = point * max(0.0, math.sin(TAU * (t - POINT) * 1.8))
    q["chest"][0] -= 7 * words
    q["head"][0] -= 10 * point + 6 * words
    q["chest"][2] += 10 * point
    # a stamp forward with the left foot on «Враг!»
    stamp = env(t, POINT, POINT + 0.25) * (1 - env(t, POINT_END, POINT_END + 0.4))
    q["thigh.L"] = [-16 * stamp, 0, 0]
    q["shin.L"] = [14 * stamp, 0, 0]
    q["foot.L"] = [2 * stamp, 0, 0]
    q["thigh.R"] = [6 * stamp, 0, 0]
    q["foot.R"] = [-6 * stamp, 0, 0]
    # the hand down to the horn at his hip, then up to his mouth: elbow out and up, the fist at his lips
    lift = env(t, TAKE, AT_MOUTH) * (1 - env(t, BLOW_END, BLOW_END + 0.6))
    low = 1 - env(t, BLOW_END, BLOW_END + 0.6)
    q["upper_arm.L"] = [2 - 75 * lift + 25 * (1 - low) * env(t, BLOW_END, SECONDS), -4 - 38 * lift, 0]
    q["forearm.L"] = [q["forearm.L"][0] - 105 * lift, 0, 0]
    # the blow: the chest swells, the head back, a long note with a shake
    blow = env(t, AT_MOUTH, AT_MOUTH + 0.35) * (1 - env(t, BLOW_END - 0.2, BLOW_END + 0.2))
    q["chest"][0] -= 9 * blow
    q["head"][0] -= 14 * blow + 1.5 * math.sin(TAU * t * 7) * blow
    q["head"][2] += 10 * blow
    q["hips_loc"] = [0, 0.004 * blow, 0]
    return q


# ---------------------------------------------------------------- the horn prop

def key_horn(horn, arm):
    sc = bpy.context.scene
    horn.parent = None
    mouthpiece = Vector(lookout.HORN_MOUTHPIECE) * SL
    bell = Vector(lookout.HORN_PTS[0]) * SL
    f0_inv = gt.frame(mouthpiece, (1, 0, 0), bell - mouthpiece).inverted()
    lips = Vector((0.0, -0.05, 0.425 + 0.014)) * SL
    for f in range(int(SECONDS * FPS) + 1):
        t = f / FPS
        sc.frame_set(f + 1)
        on_hip = gt.bone_world(arm, "hips")
        head = gt.bone_world(arm, "head")
        # the rest pose's forward (-y), up and left (+x) carried through the head's pose
        rot = head.to_3x3()
        fwd = (rot @ Vector((0, -1, 0))).normalized()
        up = (rot @ Vector((0, 0, 1))).normalized()
        left = (rot @ Vector((1, 0, 0))).normalized()
        bell_dir = (fwd * 0.55 + up * 0.32 + left * 0.8).normalized()
        at_mouth = gt.frame(head @ lips, left, bell_dir) @ f0_inv
        w = ease((t - TAKE) / (AT_MOUTH - TAKE)) if t >= TAKE else 0.0
        horn.matrix_world = gt.blend(on_hip, at_mouth, w) if w < 1.0 else at_mouth
        if t > BLOW_END:
            # lowered a little: the bell eased down towards his chest, still in his hand
            low = ease((t - BLOW_END) / 0.6)
            down = gt.frame(head @ lips + fwd * 0.03 * SL - up * 0.07 * SL * low, left,
                            (fwd * 0.6 + up * (0.32 - 0.8 * low) + left * 0.8).normalized()) @ f0_inv
            horn.matrix_world = gt.blend(at_mouth, down, low)
        horn.keyframe_insert("location", frame=f + 1)
        horn.keyframe_insert("rotation_euler", frame=f + 1)
        horn.keyframe_insert("scale", frame=f + 1)


def build():
    """The take baked: [(export name, take, rig, extra meshes)], [props], seconds (export_m2intro.py)."""
    fig = lookout.make((0, 0, 0))
    arm = ka.rig_figure(fig, SL)
    horn = lookout.make_horn()
    track = ka.Track(SL, 0.5)
    track.apply = ea.apply
    track.moves = []
    track.add('pose', SECONDS, Vector((0, 0, 0)), pose, (0, -1, 0))
    track.bake(arm, "CineLookout_Alarm")
    key_horn(horn, arm)
    return [("M2Lookout", "Alarm", arm, [])], [horn], SECONDS


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = argv[1]
    what = argv[2] if len(argv) > 2 else "all"
    os.makedirs(out, exist_ok=True)
    actors, props, _ = build()
    aa.stage(gt.keep_of(actors, props))
    for o in bpy.context.scene.objects:
        if o.name.startswith("ArdanTuft") or o.name.startswith("ArdanPath"):
            o.hide_render = True
    shots = (("front", (0.0, 0.0, 0.32), (0.75, -1.3, 0.12), 36),
             ("side", (0.0, 0.0, 0.32), (1.5, 0.25, 0.2), 38))
    for key, at, off, lens in shots:
        if what in (key, "all"):
            ea.shoot(out, "lookout_" + key, SECONDS, at, off, lens)


if __name__ == "__main__":
    main()
