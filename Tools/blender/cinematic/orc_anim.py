"""The orcs' opening-cutscene animations (2026-11-10, after the elder's were approved):
  * ChampionLeap (shot 9): a beat of silence before the open portal, then the champion bursts out
    of the swirl in one leap, lands in a deep crouch with a fist on the stones -- the platform
    cracks, grit flies -- rises slowly and roars, arms flung wide; then turns his head to the village.
  * HordeRun (shot 10): the torchbearer (torch high, axe swinging) and the spearman (shield up,
    spear forward) run out of the swirl after him and split off into the village.
The orcs are built on their own joints (orcs_cast: wider, lower), so the rig is theirs; the moves
are the shared Track of kids_anim. Never saves.

    blender --background <Buildings-Size1-Models.blend> --python orc_anim.py -- render <out dir> [leap|horde|all]
    ARDAN_SHEET=1,30,60 ... -- frames as PNGs instead of the videos
"""
import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import bpy  # noqa: E402
import ardan_anim as aa  # noqa: E402
import ardan_anim2 as a2  # noqa: E402
import figure as fg  # noqa: E402
import kids_anim as ka  # noqa: E402
import orcs_cast as oc  # noqa: E402
import portal  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
PZ = ka.PLATFORM_Z
LEG = (oc.HIP_Z - oc.KNEE_Z, oc.KNEE_Z - oc.ANKLE_Z)
STAND = sum(LEG)

BONES = [
    ("hips", (0, 0, oc.HIP_Z), (0, 0, 0.25), None),
    ("chest", (0, 0, 0.25), (0, 0, 0.36), "hips"),
    ("head", (0, 0, 0.36), (0, 0, 0.5), "chest"),
]
for _side, _k in (("L", 1), ("R", -1)):
    _sh, _el, _wr = oc.joints(_k)
    BONES += [
        (f"upper_arm.{_side}", tuple(_sh), tuple(_el), "chest"),
        (f"forearm.{_side}", tuple(_el), tuple(_wr + Vector((0, 0, -0.05))), f"upper_arm.{_side}"),
        (f"thigh.{_side}", (_k * oc.HIP_X, 0, oc.HIP_Z), (_k * oc.HIP_X, 0, oc.KNEE_Z), "hips"),
        (f"shin.{_side}", (_k * oc.HIP_X, 0, oc.KNEE_Z), (_k * oc.HIP_X, 0, oc.ANKLE_Z), f"thigh.{_side}"),
        (f"foot.{_side}", (_k * oc.HIP_X, 0, oc.ANKLE_Z), (_k * oc.HIP_X, -0.06, 0.015), f"shin.{_side}"),
    ]


def crouch(c):
    """An orc's crouch: knees out and bent by c, feet flat, the hips lowered as far as the legs fold."""
    q = a2.blank()
    th, kn = -55 * c, 95 * c
    for s, k in (("L", 1), ("R", -1)):
        q["thigh." + s] = [th, k * -8 * c, 0]
        q["shin." + s] = [kn, 0, 0]
        q["foot." + s] = [-(th + kn), 0, 0]
    drop = STAND - (LEG[0] * math.cos(math.radians(th)) + LEG[1] * math.cos(math.radians(th + kn)))
    q["hips_loc"] = [0, -drop, 0]
    q["hips"] = [20 * c, 0, 0]
    q["chest"] = [12 * c, 0, 0]
    q["head"] = [-15 * c, 0, 0]
    return q


def brute_run(phase):
    """A heavier run than Ардан's: a shorter, wider stride, the body hunched forward."""
    q = a2.run(phase)
    for s, k in (("L", 1), ("R", -1)):
        q["thigh." + s][0] *= 0.8
        q["thigh." + s][1] = k * -5
        q["shin." + s][0] *= 0.85
    q["chest"][0] += 8
    q["head"][0] -= 6
    return q


def torch_run(phase):
    q = brute_run(phase)
    s = math.sin(TAU * phase)
    q["upper_arm.L"] = [-130 + 10 * s, -10, 0]          # the torch held high, bobbing
    q["forearm.L"] = [-15, 0, 0]
    q["upper_arm.R"] = [-30 + 45 * s, 8, 0]             # the axe swinging
    q["forearm.R"] = [-55 - 20 * max(0.0, s), 0, 0]
    return q


def spear_run(phase):
    q = brute_run(phase)
    s = math.sin(TAU * phase)
    q["upper_arm.L"] = [-35 + 4 * s, -6, -15]            # the shield up before him
    q["forearm.L"] = [-75, 0, 0]
    q["upper_arm.R"] = [-30 + 6 * s, 4, 0]              # the spear carried forward
    q["forearm.R"] = [-50, 0, 0]
    return q


# ---------------------------------------------------------------- the champion

BEHIND = Vector((0, 0.45, PZ))           # out of sight behind the swirl
LAND = Vector((0, -0.95, PZ))
FLY_FROM, FLY_TO = 0.55, 1.05            # seconds
IMPACT = FLY_TO
LEAP_SECONDS = 4.6
VILLAGE = Vector((-2.0, -2.5, 0))


def champion_move(t):
    """(root, heading, pose) at t seconds."""
    head_to = math.atan2(LAND.x - BEHIND.x, -(LAND.y - BEHIND.y))
    if t < FLY_FROM:                                       # gathering, hidden behind the swirl
        return BEHIND, head_to, crouch(0.8 * a2.env(t, 0.1, FLY_FROM))
    if t < FLY_TO:                                         # the leap
        f = (t - FLY_FROM) / (FLY_TO - FLY_FROM)
        pos = BEHIND.lerp(LAND, f)
        pos.z = PZ + 4 * 0.32 * f * (1 - f)
        q = a2.blank()
        for s, k in (("L", 1), ("R", -1)):
            q["thigh." + s] = [-45 - 25 * f, k * -10, 0]
            q["shin." + s] = [70 - 30 * f, 0, 0]
            q["foot." + s] = [-15, 0, 0]
        q["upper_arm.R"] = [40 - 120 * f, 10, 0]           # the cleaver arm comes over
        q["forearm.R"] = [-40, 0, 0]
        q["upper_arm.L"] = [-90 + 60 * f, -25, 0]
        q["forearm.L"] = [-20, 0, 0]
        q["chest"] = [10 + 15 * f, 0, 0]
        q["head"] = [-20, 0, 0]
        return pos, head_to, q
    q = crouch(1.0)                                        # the landing: deep, a fist on the stones
    q["chest"][0] += 22
    q["head"][0] -= 25
    q["upper_arm.L"] = [-55, -15, 0]
    q["forearm.L"] = [-5, 0, 0]
    q["upper_arm.R"] = [25, 15, 0]
    q["forearm.R"] = [-30, 0, 0]
    land = a2.mix(q, q, 0)
    if t < IMPACT + 0.12:                                  # the impact soaks into the knees
        return LAND, head_to, a2.mix(champion_move(FLY_TO - 1e-4)[2], land, a2.ease((t - IMPACT) / 0.12))
    rise = a2.env(t, IMPACT + 0.7, IMPACT + 1.4)           # he rises, slow and heavy
    stand = crouch(0.25)
    stand["upper_arm.L"] = [5, -12, 0]
    stand["upper_arm.R"] = [5, 12, 0]
    stand["forearm.L"] = [-25, 0, 0]
    stand["forearm.R"] = [-25, 0, 0]
    q = a2.mix(land, stand, rise)
    roar = a2.env(t, IMPACT + 1.5, IMPACT + 1.8) * (1 - a2.env(t, IMPACT + 2.9, IMPACT + 3.2))
    if roar > 0:
        shake = math.sin(TAU * t * 11) * roar
        r = crouch(0.3)
        r["chest"] = [-18 + 1.5 * shake, 0, 0]
        r["head"] = [-32 + 3 * shake, 0, 2 * shake]
        for s, k in (("L", 1), ("R", -1)):
            r["upper_arm." + s] = [-35, k * -70, 0]
            r["forearm." + s] = [-60 + 4 * shake, 0, k * 15]
        q = a2.mix(q, r, roar)
    # then he turns his head to the village
    look = a2.env(t, IMPACT + 3.1, IMPACT + 3.5)
    q["head"][2] += 35 * look
    q["chest"][2] += 10 * look
    return LAND, head_to, q


def debris(mat):
    """The cracks his landing leaves and the grit it throws: separate objects keyed in at impact."""
    sc = bpy.context.scene
    rnd = random.Random(9)
    objs = []
    b = Builder()
    for i in range(9):                                      # cracks out from where he lands
        a = TAU * i / 9 + rnd.uniform(-0.25, 0.25)
        p = Vector((0, 0, 0))
        for k in range(3):
            step = Vector((math.cos(a + rnd.uniform(-0.5, 0.5)), math.sin(a + rnd.uniform(-0.5, 0.5)), 0)) * rnd.uniform(0.05, 0.09)
            b.rod(tuple(p + Vector((0, 0, 0.004))), tuple(p + step + Vector((0, 0, 0.004))), 0.007, 'roof_slate_dark')
            p = p + step
    me = b.to_mesh("CineLandingCracks")
    me.materials.append(mat)
    cr = bpy.data.objects.new("CineLandingCracks", me)
    sc.collection.objects.link(cr)
    cr.location = LAND
    f0 = int(IMPACT * FPS) + 1
    cr.scale = (0.01, 0.01, 1)
    cr.keyframe_insert("scale", frame=f0)
    cr.scale = (1, 1, 1)
    cr.keyframe_insert("scale", frame=f0 + 4)
    objs.append(cr)
    for i in range(14):                                     # grit: chips thrown out low, falling back
        g = Builder()
        h = rnd.uniform(0.012, 0.022)
        g.box(-h, h, -h, h, -h, h, 'stone_mid' if i % 2 else 'stone_dark', bevel=0.003)
        gm = g.to_mesh("CineGrit%d" % i)
        gm.materials.append(mat)
        ob = bpy.data.objects.new("CineGrit%d" % i, gm)
        sc.collection.objects.link(ob)
        a = rnd.uniform(0, TAU)
        v = Vector((math.cos(a), math.sin(a), 0)) * rnd.uniform(0.5, 1.0) + Vector((0, 0, rnd.uniform(0.6, 1.1)))
        spin = Vector((rnd.uniform(-9, 9), rnd.uniform(-9, 9), rnd.uniform(-9, 9)))
        for f in range(f0 - 1, f0 + 30):
            t = max(0.0, (f - f0) / FPS)
            pos = LAND + Vector((0, 0, 0.02)) + v * t + Vector((0, 0, -0.5 * 3.3 * t * t))
            if pos.z < PZ:
                pos.z = PZ
            ob.location = pos
            ob.rotation_euler = spin * t
            ob.scale = (1, 1, 1) if f >= f0 and f < f0 + 26 else (0, 0, 0)
            ob.keyframe_insert("location", frame=f)
            ob.keyframe_insert("rotation_euler", frame=f)
            ob.keyframe_insert("scale", frame=f)
        objs.append(ob)
    return objs


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
    plat, stones, sw = portal.make()
    portal.set_arch(stones, sw, True)
    figs = {}
    for name in ("CineOrcChampion", "CineOrcTorch", "CineOrcSpear"):
        mesh = oc.make(name)
        figs[name] = ka.rig_figure(mesh, oc.FIGURES[name][1], bones=BONES)
    grit = debris(fg.material())
    keep = {plat.name, sw.name} | {s.name for s in stones} | {o.name for o in grit}
    for r in figs.values():
        keep |= {r.name} | {c.name for c in r.children}
    aa.stage(keep)
    for o in bpy.context.scene.objects:
        if o.name.startswith("ArdanPath") or o.name.startswith("ArdanTuft"):
            o.hide_render = True
    champ = ka.Track(1.3, 0.9)
    champ.add('fn', LEAP_SECONDS, champion_move)
    champ.bake(figs["CineOrcChampion"], "CineOrcChampion_Leap")
    if what in ("leap", "all"):
        for n in ("CineOrcTorch", "CineOrcSpear"):
            figs[n].location = (0, 3, -2)                 # off stage
        shoot(out, "orc_leap", LEAP_SECONDS, (0, -0.55, 0.5), (0.85, -2.3, -0.15), 32)
    if what in ("horde", "all"):
        speed = 0.95
        # each path crosses the arch's line 0.2 off its middle, clear of the pillars
        runs = (("CineOrcTorch", torch_run, Vector((0.0, 0.5, PZ)), Vector((-1.0, -1.5, PZ)), 0.0),
                ("CineOrcSpear", spear_run, Vector((0.0, 0.5, PZ)), Vector((1.0, -1.5, PZ)), 0.35))
        for n, fn, p0, p1, wait in runs:
            tr = ka.Track(1.0, speed)
            tr.run_pose = fn
            if wait:
                tr.add('pose', wait, p0, lambda t: crouch(0), (p1.x, p1.y, 0))
            tr.add('run', 2.6, p0, p1)
            tr.bake(figs[n], n + "_Run")
        for o in grit:                                   # the cracks are already there, the grit long settled
            o.animation_data_clear()
            o.scale = (1, 1, 1) if o.name == "CineLandingCracks" else (0, 0, 0)
        # the champion stands roaring in front as they pour past him: his clip from 1.9 s on
        champ.moves = []
        champ.add('fn', 3.0, lambda t: champion_move(t + IMPACT + 1.5))
        champ.bake(figs["CineOrcChampion"], "CineOrcChampion_Roar")
        shoot(out, "orc_horde", 3.0, (0, -0.6, 0.4), (1.2, -2.5, 0.25), 28)


if __name__ == "__main__":
    main()
