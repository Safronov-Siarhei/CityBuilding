"""The mage's animations for mission 1's bridge scene (the user, 2026-11-11: a short 3-4 s scene;
a violet orb brings the bridge's middle down with the orc on it; then the mage holds the bank).
Clips, each in his own frame (facing -y; his left is +x), the director turning his root:
  * Mage_Idle:  waiting by the track, the staff planted, a slow breath.
  * Mage_Wave:  «Беги в город, я их задержу» -- his head follows Ардан running past, the free LEFT
                arm flung out to his left, the way to the town, small nods on the words.
  * Mage_Cast:  he lifts the staff and strikes it down on the ground (the crystal flares), then
                thrusts the left palm forward: the orb leaves it.
  * Mage_Hold:  holding the bank: feet apart, the left palm raised at the far bank, the staff
                planted, a slow breath (loops).
The staff is part of the right forearm (the approved model), so it is lifted and struck, never
swung overhead: a raised arm would turn it crystal-down. Never saves.

    blender --background <Buildings-Size1-Models.blend> --python mage_anim.py -- render <out dir> [idle|wave|cast|hold|all]
    ARDAN_SHEET=1,20,40 ... -- frames as PNGs instead of the videos
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
import kids_anim as ka  # noqa: E402
import mage  # noqa: E402
from mathutils import Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
AT = Vector((0.0, 0.0, 0.0))
SHOULDER_L = Vector((0.096, 0.0, 0.384)) * mage.SCALE


def base(t):
    """Standing: the staff planted by his right side, a slow breath, the left arm easy."""
    q = ea.blank()
    br = math.sin(TAU * t / 3.2)
    q["chest"] = [1.2 * br, 0, 0]
    q["head"] = [-1 + 1.2 * br, 0, 0]
    q["upper_arm.L"] = [2, -3, 0]
    q["forearm.L"] = [-12, 0, 0]
    q["upper_arm.R"] = [0, 0, 0]
    q["forearm.R"] = [0, 0, 0]
    return q


IDLE_SECONDS = 3.2


def idle(t):
    return base(t)


# ---------------------------------------------------------------- the wave

WAVE_SECONDS = 2.6
TOWN_WAY = Vector((1.6, -0.5, 0.42))          # to his left, a little ahead: the road to the town


def ardan_angle(t):
    """Ардан runs past from ahead-right to behind-left: the head turns after him (deg, + = left)."""
    return -35 + 95 * a2.ease(t / WAVE_SECONDS)


def wave(t):
    q = base(t)
    # 0.2-0.6 s: the arm flung out to the town; held while he speaks; eased down at the end
    out = a2.env(t, 0.2, 0.55) * (1 - a2.env(t, 2.1, 2.6))
    d = (TOWN_WAY - SHOULDER_L).normalized()
    q[ea.AIM] = list(d) + [out * 0.95]
    q["forearm.L"][0] = -12 + 8 * out
    # a short beat of the hand on the words ("беги" -- "в город")
    q["forearm.L"][0] -= 14 * out * max(0.0, math.sin(TAU * t * 1.6))
    # the head after Ардан, the chest leaning a little into the words
    q["head"][2] += ardan_angle(t) * 0.8
    q["chest"][2] += ardan_angle(t) * 0.15
    q["chest"][0] += 5 * out
    q["head"][0] += 3.5 * math.sin(TAU * t * 2.4) * out
    return q


# ---------------------------------------------------------------- the cast

CAST_SECONDS = 2.0
STRIKE = 0.75           # the staff meets the ground
RELEASE = 1.2           # the orb leaves the palm


def cast(t):
    q = base(t)
    # 0-0.55 s: the staff lifted (the fist up), the body gathering back
    lift = a2.env(t, 0.05, 0.55) * (1 - a2.env(t, 0.6, STRIKE))
    # elbow forward, forearm back by as much: the fist rises and the staff stays upright
    q["upper_arm.R"] = [-40 * lift, 0, 0]
    q["forearm.R"] = [40 * lift, 0, 0]
    q["chest"][0] -= 8 * lift
    q["head"][0] -= 6 * lift
    # STRIKE: down hard -- a crouch on the blow
    hit = a2.env(t, 0.6, STRIKE) * (1 - a2.env(t, 1.0, 1.6))
    q["chest"][0] += 12 * hit
    q["hips_loc"] = [0, 0, -0.012 * hit]
    for k in ("L", "R"):
        q["thigh." + k] = [-14 * hit, 0, 0]
        q["shin." + k] = [26 * hit, 0, 0]
    # the left hand drawn back by the hip, then thrust at the bridge: RELEASE
    draw = a2.env(t, 0.5, 0.9) * (1 - a2.env(t, 0.95, RELEASE))
    thrust = a2.env(t, 0.95, RELEASE)
    q["upper_arm.L"] = [2 + 30 * draw - 92 * thrust, -3 - 6 * thrust, 0]
    q["forearm.L"] = [-12 - 70 * draw + 2 * thrust, 0, 0]
    q["chest"][2] += -12 * draw + 10 * thrust
    q["chest"][0] += 6 * thrust
    q["head"][0] -= 4 * thrust
    return q


# ---------------------------------------------------------------- the hold

HOLD_SECONDS = 3.2


def hold(t):
    q = base(t)
    br = math.sin(TAU * t / HOLD_SECONDS)
    # feet apart, weight down, the palm up at the far bank, the staff planted
    for k, s in (("L", 1), ("R", -1)):
        q["thigh." + k] = [-6, 0, s * 8]
        q["shin." + k] = [10, 0, 0]
    q["hips_loc"] = [0, 0, -0.006]
    q["upper_arm.L"] = [-78 + 3 * br, -8, 0]
    q["forearm.L"] = [-18 - 3 * br, 0, 0]
    q["chest"] = [4 + 1.5 * br, 0, 8]
    q["head"] = [-4 + 1.5 * br, 0, -6]
    return q


# ---------------------------------------------------------------- render

def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = argv[1]
    what = argv[2] if len(argv) > 2 else "all"
    os.makedirs(out, exist_ok=True)
    fig = mage.make((0, 0, 0))
    rig = ka.rig_figure(fig, mage.SCALE)
    aa.stage({fig.name, rig.name})
    for o in bpy.context.scene.objects:
        if o.name.startswith("ArdanPath") or o.name.startswith("ArdanTuft"):
            o.hide_render = True
    track = ka.Track(mage.SCALE, 0.5)
    track.apply = ea.apply
    clips = (("idle", "CineMage_Idle", IDLE_SECONDS, idle, (0.0, -0.1, 0.32), (1.2, -1.6, 0.35), 45),
             ("wave", "CineMage_Wave", WAVE_SECONDS, wave, (0.1, -0.1, 0.32), (0.4, -2.0, 0.4), 42),
             ("cast", "CineMage_Cast", CAST_SECONDS, cast, (0.0, -0.25, 0.3), (2.1, -0.6, 0.35), 40),
             ("hold", "CineMage_Hold", HOLD_SECONDS, hold, (0.0, -0.2, 0.32), (1.5, 1.2, 0.45), 42))
    for key, name, seconds, fn, at, off, lens in clips:
        if what not in (key, "all"):
            continue
        track.moves = []
        track.add('pose', seconds, AT, fn, (0, -1, 0))
        track.bake(rig, name)
        ea.shoot(out, "mage_" + key, seconds, at, off, lens)


if __name__ == "__main__":
    main()
