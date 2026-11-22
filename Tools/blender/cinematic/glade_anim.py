"""The level-3 prologue «Пленные», group 3 «Поляна» (shot 6, the user 2026-11-22: «поляна с орками и клетки с пленными
на ней, в одной из клеток сидит старик»; the set captives_glade.py and the chief CineOrcChief approved). Never saves;
run on a COPY of the .blend (the set loads the orc scripts).

    blender --background <a COPY of Buildings-Size1-Models.blend> --python glade_anim.py -- render <out dir>
    ARDAN_SHEET=1,120,240 ... -- frames as PNGs instead of the video

8 s, ONE shot: the camera comes in along the track and pushes in on the old man's cage.
  the monsters (cine rigs; the two at the fire are the decor's own, seated):
    the CHIEF before his bone throne raises his greataxe and bellows at the cages (3.0-4.6);
    the champion beside him, hunched, breathing;
    a spearman paces along the cages' fronts; another jabs his spear at the first cage (the peasant shrinks back);
    a torchbearer walks in from the track to the fire; a guard with a torch and one with a spear stand by the cages;
    two more stand about the camp, heads turning.
  the prisoners: the peasant grips the bars and shrinks back from the jabs; the woman crouches holding the girl;
  the merchant sits with his head in his hands, the boy stands by him rubbing his eyes; the OLD MAN grips the bars,
  head bowed -- as the camera arrives (6.0) he lifts his head and looks out.
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
import captives_glade as cg  # noqa: E402
import elder  # noqa: E402
import elder_anim as ea  # noqa: E402
import figure as fg  # noqa: E402
import gate_talk_anim as gt  # noqa: E402
import kids_anim as ka  # noqa: E402
import last_stand_anim as ls  # noqa: E402
import orc_anim as oa  # noqa: E402
import orcs_cast as oc  # noqa: E402
import rider_anim as ra  # noqa: E402
import townsfolk as tf  # noqa: E402
import townsfolk_anim as tfa  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
env, ease, mix = a2.env, a2.ease, a2.mix
SECONDS = 8.0
FLOOR = 0.09 * cg.GAME_FOLK
ROAR = (3.0, 4.6)
LOOK_UP = 6.0
JABS = (1.2, 2.6, 4.2, 5.6)


def heading_to(d):
    return math.atan2(d.x, -d.y)


def toward(frm, to):
    return heading_to(Vector((to[0] - frm[0], to[1] - frm[1], 0)))


FIRE = Vector((0.0, 0.1, 0))
ELDER_DY = 0.3                       # the old man by his cage's front bars (the set's render had him at the back)
CHIEF_AT = Vector((2.2, 0.55, 0))
ELDER_CAGE = cg.CAGES[cg.ELDER_CAGE]

# the monsters: (kind, what, x, y, facing point)
ORCS = [
    ("CineOrcChief", "chief", 2.2, 0.55, (0.6, 1.8)),
    ("CineOrcChampion", "idle", 3.0, 0.1, (0.0, 0.1)),
    ("CineOrcSpear", "pace", -1.3, 0.72, None),
    ("CineOrcSpear", "jab", -1.6, 0.95, (-2.0, 1.55)),
    ("CineOrcTorch", "walk", -0.4, -2.6, (0.0, -0.3)),
    ("CineOrcTorch", "idle", 1.4, 1.0, (1.0, -1.0)),
    ("CineOrcSpear", "idle", -2.0, -0.35, (0.0, 0.1)),
    ("CineOrcSpear", "idle", 0.95, -0.95, (0.0, 0.1)),
]


def cage_point(i, dx, dy):
    """A point in cage i's own space (-y its door, toward the fire) in the world."""
    x, y, turn, _, _ = cg.CAGES[i]
    a = math.radians(turn + 180)
    lx, ly = dx, -dy
    return Vector((x + lx * math.cos(a) - ly * math.sin(a), y + lx * math.sin(a) + ly * math.cos(a), FLOOR))


# ---------------------------------------------------------------- the monsters

def orc_idle(t, off):
    q = oa.crouch(0.15)
    br = math.sin(TAU * (t + off) / 2.2)
    q["chest"][0] += 2 * br
    q["head"][2] += 25 * math.sin(TAU * (t + off) / 5.0)
    for s, k in (("L", 1), ("R", -1)):
        q["upper_arm." + s] = [-10, k * -8, 0]
        q["forearm." + s] = [-25, 0, 0]
    return q


def chief_pose(t):
    q = orc_idle(t, 0.4)
    q["head"][2] = 0.0
    roar = ease(env(t, ROAR[0], ROAR[0] + 0.35)) * (1 - ease(env(t, ROAR[1] - 0.3, ROAR[1])))
    q["upper_arm.R"] = [-10 - 140 * roar, 8 + 10 * roar, 0]           # the greataxe up
    q["forearm.R"] = [-25 + 15 * roar, 0, 0]
    q["upper_arm.L"] = [-10 - 40 * roar, -8 - 30 * roar, 0]           # the free fist out
    q["forearm.L"] = [-25 - 50 * roar, 0, 0]
    q["head"][0] += -28 * roar
    q["chest"][0] += -14 * roar
    return q


def pace(t):
    """Along the cages' fronts: (position, heading, pose)."""
    a, b = Vector((-1.3, 0.72, 0)), Vector((1.25, 0.72, 0))
    speed = 0.33
    span = (b - a).length
    d = speed * t
    leg = (d // span) % 2
    u = (d % span) / span
    pos = a.lerp(b, u) if leg == 0 else b.lerp(a, u)
    h = heading_to(b - a) if leg == 0 else heading_to(a - b)
    q = tfa.walk_pose((d / 0.28) % 1.0, stride=18.0)
    s = math.sin(TAU * d / 0.28)
    q["upper_arm.L"] = [-12 * s, 8, 0]
    q["upper_arm.R"] = [-30, -4, 0]                                       # the spear carried
    q["forearm.R"] = [-30, 0, 0]
    q["chest"][0] += 10
    q["head"][2] += 35 * math.sin(TAU * t / 3.0)                          # eyeing the cages
    return pos, h, q


def jab_pose(t):
    blow = min(JABS, key=lambda j: abs(t - j))
    return ls.orc_attack("CineOrcSpear", t, blow - 0.5, blow)


def walk_in(t):
    a, b = Vector((-0.4, -2.6, 0)), Vector((-0.15, -0.55, 0))
    u = min(1.0, t / 5.5)
    d = (b - a).length * u
    if u < 1.0:
        q = tfa.walk_pose((d / 0.28) % 1.0, stride=18.0)
        q["upper_arm.L"] = [-100, 8, 0]                                   # the torch carried up
        q["forearm.L"] = [-20, 0, 0]
    else:
        q = orc_idle(t, 1.1)
        q["upper_arm.L"] = [-60, 8, 0]
        q["forearm.L"] = [-40, 0, 0]                                       # the torch held to the fire
    return a.lerp(b, u), heading_to(b - a), q


# ---------------------------------------------------------------- the prisoners

def grip(q, w, shake=0.0, t=0.0):
    for s, k in (("L", 1), ("R", -1)):
        q["upper_arm." + s] = mix({"a": q["upper_arm." + s]}, {"a": [-78 + 6 * shake * math.sin(TAU * 5 * t), k * -14, 0]}, w)["a"]
        q["forearm." + s] = mix({"a": q["forearm." + s]}, {"a": [-38, 0, 0]}, w)["a"]
    return q


def prisoner_pose(name, t):
    if name == "CineElder":
        q = ra.stand(t)                                               # (ea.idle carries the staff's aim keys)
        q["upper_arm.L"] = [-58, -10, 0]                              # the left hand on a bar, the staff kept low
        q["forearm.L"] = [-48, 0, 0]
        up = ease(env(t, LOOK_UP, LOOK_UP + 0.8))
        q["head"][0] += 22 * (1 - up) - 4 * up                            # bowed, then looking out
        q["chest"][0] += 8 * (1 - up)
        return q
    if name == "CinePeasant":
        q = ra.stand(t)
        flinch = max(ease(env(t, j - 0.05, j + 0.1)) * (1 - ease(env(t, j + 0.4, j + 0.9))) for j in JABS)
        grip(q, 1 - flinch, shake=0.6, t=t)
        q["chest"][0] -= 16 * flinch
        q["head"][0] -= 10 * flinch
        q["hips_loc"] = [0, 0, 0.0]
        return q
    if name == "CinePeasantWoman":
        q = ra.stand(t)
        for s, k in (("L", 1), ("R", -1)):                                # her arms round the girl's back
            q["upper_arm." + s] = [-38, k * 12, 0]
            q["forearm." + s] = [-62, 0, k * 25]
        q["chest"][0] += 14 + 3 * math.sin(TAU * t / 1.4)
        q["head"][0] += 24                                                 # looking down at her
        return q
    if name == "CineGirl":
        q = ra.stand(t)                                                    # her face in her mother's skirt
        q["chest"][0] += 10
        q["head"][0] += 8
        for s, k in (("L", 1), ("R", -1)):
            q["upper_arm." + s] = [-55, k * 22, 0]
            q["forearm." + s] = [-40, 0, 0]
        return q
    if name == "CineMerchant":
        q = ka.crouch(0.95)                                               # sunk on the floor
        q["chest"][0] += 30
        q["head"][0] += 30
        for s, k in (("L", 1), ("R", -1)):                                # head in his hands
            q["upper_arm." + s] = [-55, k * 20, 0]
            q["forearm." + s] = [-115, 0, 0]
        q["chest"][0] += 3 * math.sin(TAU * t / 1.8)
        return q
    if name == "CineBoy":
        q = ra.stand(t)
        rub = math.sin(TAU * t * 1.3)
        for s, k in (("L", 1), ("R", -1)):                                # rubbing his eyes
            q["upper_arm." + s] = [-45, k * 18, 0]
            q["forearm." + s] = [-120 + 6 * rub * k, 0, 0]
        q["head"][0] += 12
        q["chest"][0] += 6 + 2 * abs(rub)
        return q
    return ra.stand(t)


# ---------------------------------------------------------------- the camera

ELDER_AT = cage_point(cg.ELDER_CAGE, 0.0, ELDER_DY)

CAM_FROM = (Vector((0.0, -6.0, 2.6)), Vector((0.0, 1.0, 0.3)), 28.0)


def cam_to():
    """The push's end: on the old man's face between the bars."""
    ex, ey = ELDER_CAGE[0], ELDER_CAGE[1]
    at = ELDER_AT + Vector((0, 0, 0.42))                          # his chest and face in the middle of the frame
    fwd = facing(cg.ELDER_CAGE)
    return at + fwd * 1.45 + Vector((0.04, 0.0, 0.08)), at, 42.0


PEASANT_AT = None


def camera(t):
    look = os.environ.get("GLADE_LOOK", "")
    if look.startswith("cage"):                                               # a check of one cage's prisoners
        i = int(look[4:])
        at = cage_point(i, 0, 0) + Vector((0, 0, 0.35))
        return at + facing(i) * 1.7 + Vector((0, 0, 0.35)), at, 30.0
    if os.environ.get("GLADE_LOOK") == "peasant" and PEASANT_AT is not None:   # a check of his grip, not the film
        at = PEASANT_AT + Vector((0, 0, 0.4))
        return at + facing(0) * float(os.environ.get("GLADE_SIDE", "1")) * 1.3 + Vector((0, 0, 0.1)), at, 42.0
    (p0, a0, l0), (p1, a1, l1) = CAM_FROM, cam_to()
    u = ease(env(t, 0.8, 6.6))
    return p0.lerp(p1, u), a0.lerp(a1, u), l0 + (l1 - l0) * u


def facing(i):
    """The way the people in cage i face (they are turned by the cage's `turn`; a figure faces -y): the bars they
    hold are on that side."""
    a = math.radians(cg.CAGES[i][2])
    return Vector((math.sin(a), -math.cos(a), 0.0))


GRIPPERS = {"CineElder": ("L",), "CinePeasant": ("L", "R")}
BARS = cg.GAME_FOLK * 0.37 - 0.012          # the cage's front bars from its middle (orccage1.HALF, grown)
BAR_STEP = cg.GAME_FOLK * 0.74 / 6          # orccage1.ring_of_bars: 6 gaps on the side the people face (no door)


def to_bars(p):
    """Moves a prisoner along his cage's forward line so his gripping hand(s) sit on the front bars."""
    i = p["cage"]
    centre = cage_point(i, 0.0, 0.0)
    fwd = facing(i)
    arm = p["arm"]
    for _ in range(2):
        arm.matrix_world = Matrix.Translation(p["pos"]) @ Matrix.Rotation(math.radians(p["turn"]), 4, 'Z')
        aa.rest(arm)
        ka.apply(arm, prisoner_pose(p["name"], 0.0), p["scale"])
        bpy.context.view_layer.update()
        hands = [arm.matrix_world @ arm.pose.bones["forearm." + s].tail for s in GRIPPERS[p["name"]]]
        reach = max((h - centre).dot(fwd) for h in hands)
        # and sideways, so the hand closes on a bar, not between two (the bars every BAR_STEP across that side)
        side = Vector((-fwd.y, fwd.x, 0.0))
        lat = [(h - centre).dot(side) for h in hands]
        shift = sum(round(s / BAR_STEP) * BAR_STEP - s for s in lat) / len(lat)
        p["pos"] = p["pos"] + fwd * (BARS - reach) + side * shift


# ---------------------------------------------------------------- the take

def act(arm):
    arm.animation_data_create()
    a = bpy.data.actions.new(f"{arm.name}_Glade")
    a.use_fake_user = True
    arm.animation_data.action = a


def key(arm, f):
    for pb in arm.pose.bones:
        pb.keyframe_insert("rotation_quaternion", frame=f, group=pb.name)
        pb.keyframe_insert("location", frame=f, group=pb.name)
    arm.keyframe_insert("location", frame=f)
    arm.keyframe_insert("rotation_euler", frame=f)


def build():
    props = cg.make(camp=False)
    orcs = []
    for i, (kind, what, x, y, face) in enumerate(ORCS):
        mesh = fg.make(f"{kind}Glade{i}", oc.FIGURES[kind][0](), oc.FIGURES[kind][1], (0, 0, 0), orc=True)
        orcs.append({"arm": ka.rig_figure(mesh, oc.FIGURES[kind][1], bones=oa.BONES), "scale": oc.FIGURES[kind][1],
                     "kind": kind, "what": what, "pos": Vector((x, y, 0)), "face": face, "off": 0.7 * i})
    folk = []
    for i, (x, y, turn, kind, inside) in enumerate(cg.CAGES):
        for name, dx, dy in inside:
            if name == "CineElder":
                mesh, scale = elder.make((0, 0, 0)), elder.SCALE
                arm = ka.rig_figure(mesh, scale, bend=ea.stoop)
            else:
                scale = tf.FIGURES[name][1]
                arm = ka.rig_figure(tf.make(name, (0, 0, 0)), scale)
            extra = 0.0
            if name == "CineElder":
                dy = ELDER_DY
            if name == "CinePeasantWoman":
                dx, dy = 0.0, 0.05
            if name == "CineGirl":
                dx, dy, extra = 0.0, -0.1, 180.0                          # close in front of her mother, facing her
            folk.append({"arm": arm, "scale": scale, "name": name, "pos": cage_point(i, dx, dy), "turn": turn + extra, "cage": i})
    # the ones who hold the bars stand where their hands MEET the front bars (the user: «держат воздух»)
    for p in folk:
        if p["name"] in GRIPPERS:
            to_bars(p)
            global ELDER_AT, PEASANT_AT
            if p["name"] == "CineElder":
                ELDER_AT = p["pos"]
            if p["name"] == "CinePeasant":
                PEASANT_AT = p["pos"]
            if os.environ.get("GLADE_DEBUG"):
                print("DBG", p["name"], tuple(round(v, 3) for v in p["pos"]), "centre", tuple(round(v, 3) for v in cage_point(p["cage"], 0, 0)),
                      "facing", tuple(round(v, 3) for v in facing(p["cage"])), "rotz", round(math.degrees(p["arm"].matrix_world.to_euler().z), 1))
    for o in orcs + folk:
        act(o["arm"])
    cam = bpy.data.objects.new("GladeCam", bpy.data.cameras.new("GladeCam"))
    cam.data.clip_end = 300
    bpy.context.scene.collection.objects.link(cam)
    sc = bpy.context.scene
    for f in range(int(SECONDS * FPS) + 1):
        t = f / FPS
        sc.frame_set(f + 1)
        for o in orcs:
            w = o["what"]
            pos, face = o["pos"], o["face"]
            h = toward(pos, face) if face else 0.0
            if w == "chief":
                q = chief_pose(t)
                h = toward(pos, face) if ROAR[0] - 0.4 < t < ROAR[1] + 0.4 else toward(pos, FIRE)
            elif w == "pace":
                pos, h, q = pace(t)
            elif w == "jab":
                q = jab_pose(t)
            elif w == "walk":
                pos, h, q = walk_in(t)
            else:
                q = orc_idle(t, o["off"])
            arm = o["arm"]
            arm.matrix_world = Matrix.Translation(pos) @ Matrix.Rotation(h, 4, 'Z')
            aa.rest(arm)
            ka.apply(arm, q, o["scale"])
            key(arm, f + 1)
        for p in folk:
            arm = p["arm"]
            arm.matrix_world = Matrix.Translation(p["pos"]) @ Matrix.Rotation(math.radians(p["turn"]), 4, 'Z')
            aa.rest(arm)
            ka.apply(arm, prisoner_pose(p["name"], t), p["scale"])
            key(arm, f + 1)
        loc, at, lens = camera(t)
        cam.location = loc
        cam.data.lens = lens
        cam.rotation_euler = (at - loc).to_track_quat('-Z', 'Y').to_euler()
        cam.keyframe_insert("location", frame=f + 1)
        cam.keyframe_insert("rotation_euler", frame=f + 1)
        cam.data.keyframe_insert("lens", frame=f + 1)
    actors = [(o["kind"].replace("Cine", "Cap"), "Glade", o["arm"], []) for o in orcs]
    actors += [("Cap" + p["name"].replace("Cine", ""), "Glade", p["arm"], []) for p in folk]
    return actors, props, cam


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = argv[1]
    os.makedirs(out, exist_ok=True)
    actors, props, cam = build()
    keep = gt.keep_of(actors, props)
    aa.stage(keep)
    sc = bpy.context.scene
    for o in sc.objects:                                   # the trees and decor are copies: keep all of the set
        if o.name.startswith("Glade ") or o.name == cg.OBJ:
            o.hide_render = False
        if o.name in ("ArdanGround", "ArdanPath") or o.name.startswith("ArdanTuft"):
            o.hide_render = True
    sc.camera = cam
    sc.frame_start, sc.frame_end = 1, int(SECONDS * FPS) + 1
    sheet = os.environ.get("ARDAN_SHEET")
    if sheet:
        sc.render.image_settings.file_format = 'PNG'
        for f in [int(x) for x in sheet.split(",")]:
            sc.frame_set(f)
            sc.render.filepath = os.path.join(out, "glade_%03d.png" % f)
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
        sc.render.filepath = os.path.join(out, "glade_push.mp4")
        bpy.ops.render.render(animation=True)
    print("RENDERED glade")


if __name__ == "__main__":
    main()
