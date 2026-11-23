"""Mission 3b «Клетка», the opening scene, group 2 «Засада» (storyboard v3, APPROVED 2026-11-23, shots 5-6): the column
stands halted at the felled tree as group 1 (column_anim.py, approved) left it. Never saves; run on a COPY of the .blend.
    blender --background <a COPY of Buildings-Size1-Models.blend> --python ambush_anim.py -- render <out dir>
    ARDAN_SHEET=1,90,200 ... -- frames as PNGs instead of the video

The scene's clock, T = 17 + t (t the take's own, 0-9 s):
  17-20  the horn in the forest (Unity's sound): the captive roars at the bars; every head snaps to the trees; the King
         throws his sword up -- «Засада! Держать клетку!» (17.6-19.6) -- and the lances come down, rider by rider;
  20-26  ~20 monsters burst out of the trees on both sides of the clearing (they crouched behind the first trunks),
         the spears' and the torches' runs (orc_anim); javelins fly out of the forest and stick in the road; the riders
         wheel their horses outward, each lane to its own side, lances couched; the driver ducks on his bench; the
         monsters pull up before the riders, crouched, weapons up -- the clash is group 3.
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
import cage_cart as cc  # noqa: E402
import cavalry as cav  # noqa: E402
import column_anim as ca  # noqa: E402
import figure as fg  # noqa: E402
import forest_road as fr  # noqa: E402
import gate_talk_anim as gt  # noqa: E402
import kids_anim as ka  # noqa: E402
import king_ride  # noqa: E402
import lookout  # noqa: E402
import orc_anim as oa  # noqa: E402
import orcs_cast as oc  # noqa: E402
import rider_anim as ra  # noqa: E402
import square_anim as sq  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
env, ease, mix = a2.env, a2.ease, a2.mix
T0 = 17.0                       # the take starts at this scene time
SECONDS = 9.0
HORN = 17.2
SHOUT = (17.6, 19.6)
LOWER = 18.6                    # the lances come down from here, 0.04 s a rider
WHEEL = 20.4                    # the horses wheel outward from here, 0.05 s a rider, 0.9 s each
BURST = 20.0                    # the first monsters break cover
ORC_V = 1.9                     # their run (Ардан's 1.52 x 1.25)


def win(t, w, rise=0.3):
    return env(t, w[0], w[0] + rise) * (1 - env(t, w[1] - rise, w[1]))


# ---------------------------------------------------------------- the column, halted

def side_of(x):
    return -1 if x < 0 else 1


def heading(T, i, x):
    """pi (up the road, +y) wheeling to face its own side: -x lane to -x (3pi/2), +x lane and the middle to +x (pi/2)."""
    u = ease(env(T, WHEEL + 0.05 * i, WHEEL + 0.05 * i + 0.9))
    return math.pi - side_of(x) * (math.pi / 2) * u


def rider_pose(T, i, kind, pos, h, reins):
    q = ra.seated(T, 0.0, 0.0)
    q["upper_arm.L"], q["forearm.L"] = [list(v) for v in reins]
    rnd = random.Random(i * 13 + 5)
    snap = env(T, HORN + 0.15 + 0.02 * i, HORN + 0.45 + 0.02 * i)
    # first the head snaps to the trees on his side (searching), then it follows the monster coming at him
    side = side_of(pos.x)
    target = Vector((side * 5.5, pos.y + rnd.uniform(-2.0, 2.0) + 1.5 * math.sin(T * 1.3 + i), 0.5))
    q["head"][2] += sq.look_deg(pos, h, target) * 0.9 * snap
    q["chest"][2] += sq.look_deg(pos, h, target) * 0.25 * snap
    if kind == "king":
        up = ease(env(T, HORN + 0.3, HORN + 0.7))
        ready = ease(env(T, SHOUT[1], SHOUT[1] + 0.6))
        q["upper_arm.R"] = [(-12 - 138 * up) * (1 - ready) - 95 * ready, 10, 0]
        q["forearm.R"] = [(-24 + 4 * up) * (1 - ready) - 15 * ready, 0, 0]
        # the shout: the head turns to one side and the other, the chest up
        shout = win(T, SHOUT, 0.3)
        q["head"][2] += 35 * math.sin(TAU * (T - SHOUT[0]) / 1.6) * shout
        q["head"][0] -= 10 * shout
        q["chest"][0] -= 6 * shout
    else:
        down = ease(env(T, LOWER + 0.04 * i, LOWER + 0.04 * i + 0.5))
        sq.lance_up(q, 1 - down)
        couched = {"a": [-78 + rnd.uniform(-5, 5), 4, 0]}, {"a": [-6, 0, 0]}
        q["upper_arm.R"] = mix({"a": q["upper_arm.R"]}, couched[0], down)["a"]
        q["forearm.R"] = mix({"a": q["forearm.R"]}, couched[1], down)["a"]
        q["chest"][0] += 8 * down
    return q


def horse_pose(T, i, turning):
    """Stamping in place, the head tossing at the horn; stepping round while it wheels."""
    q = sq.horse_walk_pose(T * 1.6 + 0.37 * i, T * 0.25 + 0.6 * turning, max(0.25, turning))
    toss = env(T, HORN + 0.1, HORN + 0.4) * (1 - env(T, HORN + 1.4, HORN + 2.2))
    q["neck"][0] -= 14 * toss
    q["head"][0] -= 10 * toss + 6 * math.sin(TAU * T / 0.55) * toss
    return q


# ---------------------------------------------------------------- the monsters

KINDS = ["CineOrcSpear"] * 12 + ["CineOrcTorch"] * 8


def monsters():
    """20 attackers: (kind, side, start, target, break-cover time). Each makes for a rider's flank; they come out of the
    first trees (the clearing's rim + its overhang, x ~ ±5.2-6.0)."""
    rnd = random.Random(31)
    riders = [(x, ca.FRONT_STOP - back) for _, x, back in ca.slots()]
    out = []
    kinds = KINDS[:]
    rnd.shuffle(kinds)
    for i, kind in enumerate(kinds):
        side = -1 if i % 2 else 1
        lane = [r for r in riders if side_of(r[0]) == side or r[0] == 0.0]
        rx, ry = lane[(i // 2) * len(lane) // 10 % len(lane)]
        ty = ry + rnd.uniform(-0.2, 0.2)
        if abs(ty - ca.CART_STOP_Y) < 0.7:                 # not into the cart
            ty += 0.8
        target = Vector((rx + side * rnd.uniform(0.75, 1.05), ty, 0.0))
        start = Vector((side * rnd.uniform(5.3, 6.1), ty + rnd.uniform(-1.4, 1.4), 0.0))
        out.append((kind, side, start, target, BURST + rnd.uniform(0.0, 1.5)))
    return out


def orc_state(T, m):
    """(root matrix, pose) of a monster at T: crouched in cover, the run, the pull-up braced before the rider."""
    kind, side, start, target, go = m
    way = target - start
    dist = way.length
    h = math.atan2(way.x, -way.y)
    run = max(0.0, T - go) * ORC_V
    if T < go:
        q = oa.crouch(0.55)
        q["head"][0] -= 10
        pos = start
    elif run < dist:
        pos = start + way.normalized() * run
        phase = (run / (a2.RUN_SPEED * a2.RUN_CYCLE)) % 1.0
        q = oa.spear_run(phase) if kind == "CineOrcSpear" else oa.torch_run(phase)
        rise = ease(env(T, go, go + 0.25))                  # up out of the crouch
        q = mix(oa.crouch(0.55), q, rise) if rise < 1 else q
    else:
        pos = target
        at = go + dist / ORC_V
        brace = ease(env(T, at, at + 0.3))
        q = mix(oa.spear_run(0.25) if kind == "CineOrcSpear" else oa.torch_run(0.25), braced(kind, T), brace)
    return Matrix.Translation(pos) @ Matrix.Rotation(h, 4, 'Z'), q


def braced(kind, T):
    q = oa.crouch(0.38)
    s = math.sin(TAU * T / 0.9)
    if kind == "CineOrcTorch":
        q["upper_arm.L"] = [-130 + 8 * s, -10, 0]          # the torch high, the axe drawn back to strike
        q["forearm.L"] = [-15, 0, 0]
        q["upper_arm.R"] = [-120 + 10 * s, 20, 0]
        q["forearm.R"] = [-60, 0, 0]
    else:
        q["upper_arm.L"] = [-45, -6, -15]                   # the shield up, the spear levelled at the rider
        q["forearm.L"] = [-75, 0, 0]
        q["upper_arm.R"] = [-62 + 6 * s, 4, 0]
        q["forearm.R"] = [-20, 0, 0]
    q["head"][0] -= 12
    return q


# ---------------------------------------------------------------- the javelins

JAVELINS = [  # (side, launch y, land (x, y), launch time)
    (1, 2.0, (0.55, 2.2), 20.5), (-1, -1.0, (-0.6, -1.3), 20.8), (1, -4.0, (0.7, -4.6), 21.0),
    (-1, 3.6, (-0.55, 3.0), 21.2), (1, 0.3, (0.65, -0.2), 21.45), (-1, -5.2, (-0.8, -5.6), 21.7),
]
FLY = 0.75


def javelin_mesh(name):
    b = Builder()
    L = 0.62
    b.rod((0, 0, 0), (0, L, 0), 0.016, 'wood_mid')
    b.rod((0, L, 0), (0, L + 0.07, 0), 0.03, 'iron')
    b.rod((0, 0, 0), (0, 0.06, 0), 0.025, 'wood_dark')
    return fg.install_static(name, b, scale=1.0)


def javelin_pos(j, u):
    side, ly, land, at = j
    p0 = Vector((side * 5.6, ly, 0.9))
    p1 = Vector((land[0], land[1], 0.08))
    return p0.lerp(p1, u) + Vector((0, 0, 1.3 * math.sin(math.pi * u) * (1 - u * 0.15)))


def javelin_matrix(T, j):
    """Hidden under the ground before its throw; an arc from the trees, the point leading along the arc; stuck in the
    road at its landing slant after."""
    at = j[3]
    if T < at:
        return Matrix.Translation((0, 0, -10))
    u = min(1.0, (T - at) / FLY)
    pos = javelin_pos(j, u)
    v = min(u, 0.98)
    d = (javelin_pos(j, v + 0.02) - javelin_pos(j, v)).normalized()
    rot = Vector((0, 1, 0)).rotation_difference(d).to_matrix().to_4x4()
    return Matrix.Translation(pos - d * 0.62) @ rot          # the mesh runs butt (origin) -> point (+y)


# ---------------------------------------------------------------- the take

def build():
    pairs = []
    for i, (kind, x, back) in enumerate(ca.slots()):
        if kind == "king":
            p = sq.pair(f"Amb{i}", "CineKingHorse", king_ride.make(), king_ride.SCALE)
        else:
            hn = ("CineWarhorse", "CineWarhorseGrey")[i % 3 == 1]
            p = sq.pair(f"Amb{i}", hn, fg.make(f"CineCavalryAmb{i}", cav.PARTS, cav.SCALE, (0, 0, 0)), cav.SCALE)
            p["hname"] = hn
        # where group 1 left it
        p.update(kind=kind, x=x + random.Random(i).uniform(-0.04, 0.04),
                 y=ca.FRONT_STOP - (back + random.Random(i + 50).uniform(-0.08, 0.08)))
        pairs.append(p)
    cart = cc.make("CineCageCartBare")
    wheels = [cc.make("CineCageWheel" + ("L" if x < 0 else "R")) for x in (-1, 1, -1, 1)]
    shaft = [ca.plain_horse(f"AmbCartHorse{i}") | {"x": x} for i, x in enumerate((-0.16, 0.16))]
    kind = "CineOrcCaptive"
    mesh = fg.make(kind + "Amb", oc.FIGURES[kind][0](), oc.FIGURES[kind][1], (0, 0, 0), orc=True)
    captive = {"arm": ka.rig_figure(mesh, oc.FIGURES[kind][1], bones=oa.BONES), "scale": oc.FIGURES[kind][1]}
    dm = fg.make("CineGuardAmbDriver", lookout.PARTS, lookout.SCALE, (0, 0, 0))
    driver = {"arm": ka.rig_figure(dm, lookout.SCALE), "scale": lookout.SCALE}
    orcs = []
    for i, m in enumerate(monsters()):
        k = m[0]
        om = fg.make(f"{k}Amb{i}", oc.FIGURES[k][0](), oc.FIGURES[k][1], (0, 0, 0), orc=True)
        orcs.append({"m": m, "arm": ka.rig_figure(om, oc.FIGURES[k][1], bones=oa.BONES), "scale": oc.FIGURES[k][1]})
    javs = [javelin_mesh(f"CineJavelin{i}") for i in range(len(JAVELINS))]
    for p in pairs:
        sq.act(p["h"], "Ambush")
        sq.act(p["r"], "Ambush")
    for p in shaft:
        sq.act(p["h"], "Ambush")
    for a in [captive["arm"], driver["arm"]] + [o["arm"] for o in orcs]:
        sq.act(a, "Ambush")
    sc = bpy.context.scene
    reins = {}
    drop = ca.hips_drop(driver["arm"], ca.driver_pose(0.0), driver["scale"])
    bench = cc.SCALE * (cc.BED_Z + 0.235)
    seat_at = Vector((0.0, cc.SCALE * (-cc.HALF_L - 0.13), bench - drop + 0.01))
    cm = Matrix.Translation((0.0, ca.CART_STOP_Y, 0.0)) @ Matrix.Rotation(math.pi, 4, 'Z')
    roll = ca.total(ca.CART_BACK) / (cc.WHEEL_R * cc.SCALE)            # the wheels as group 1 left them
    for f in range(int(SECONDS * FPS) + 1):
        T = T0 + f / FPS
        sc.frame_set(f + 1)
        for i, p in enumerate(pairs):
            pos = Vector((p["x"], p["y"], 0.0))
            h = heading(T, i, p["x"])
            turning = min(1.0, abs(h - heading(T - 0.1, i, p["x"])) / 0.12)
            sq.seat(p, pos, h, horse_pose(T, i, turning))
            key = "king" if p["kind"] == "king" else "cav"
            if key not in reins:
                reins[key] = sq.solve_reins(p, ra.seated(T, 0.0, 0.0))
            aa.rest(p["r"])
            ka.apply(p["r"], rider_pose(T, i, p["kind"], pos, h, reins[key]), p["scale"])
        cart.matrix_world = cm
        for w, (sx, sy) in zip(wheels, ((-1, 0), (1, 0), (-1, 1), (1, 1))):
            hub = Vector((sx * cc.WHEEL_X, cc.WHEEL_YS[sy], cc.WHEEL_R)) * cc.SCALE
            w.matrix_world = cm @ Matrix.Translation(hub) @ Matrix.Rotation(roll, 4, 'X')
        for k, p in enumerate(shaft):
            p["h"].location = cm @ Vector((p["x"], -cc.SCALE * (cc.HALF_L + 0.66), 0.0))
            p["h"].rotation_euler = (0, 0, math.pi)
            aa.rest(p["h"])
            ra.h_apply(p["h"], horse_pose(T, 7 + k, 0.0))
        arm = driver["arm"]
        arm.matrix_world = cm @ Matrix.Translation(seat_at)
        aa.rest(arm)
        ka.apply(arm, driver_pose(T), driver["scale"])
        arm = captive["arm"]
        arm.matrix_world = cm @ Matrix.Translation((0.0, 0.1, cc.SCALE * cc.BED_Z)) @ Matrix.Rotation(-math.pi / 2, 4, 'Z')
        aa.rest(arm)
        ka.apply(arm, captive_pose(T), captive["scale"])
        for o in orcs:
            m, q = orc_state(T, o["m"])
            o["arm"].matrix_world = m
            aa.rest(o["arm"])
            ka.apply(o["arm"], q, o["scale"])
        for j, jv in zip(JAVELINS, javs):
            jv.matrix_world = javelin_matrix(T, j)
            jv.keyframe_insert("location", frame=f + 1)
            jv.keyframe_insert("rotation_euler", frame=f + 1)
        for ob in [cart] + wheels:
            ob.keyframe_insert("location", frame=f + 1)
            ob.keyframe_insert("rotation_euler", frame=f + 1)
        for p in pairs:
            sq.key(p["h"], f + 1)
            sq.key(p["r"], f + 1)
        for a in [p["h"] for p in shaft] + [captive["arm"], driver["arm"]] + [o["arm"] for o in orcs]:
            sq.key(a, f + 1)
    names = {"CineKingHorse": "CapKingHorse", "CineWarhorse": "CapWarhorse", "CineWarhorseGrey": "CapWarhorseGrey"}
    actors = []
    for p in pairs:
        if p["kind"] == "king":
            actors += [("CapKingHorse", "Ambush", p["h"], []), ("CapKingRide", "Ambush", p["r"], [])]
        else:
            actors += [(names[p["hname"]], "Ambush", p["h"], []), ("CapCavalry", "Ambush", p["r"], [])]
    actors += [("CapCartHorse", "Ambush", p["h"], []) for p in shaft]
    actors += [("CapGuardStand", "Ambush", driver["arm"], []), ("CapOrcCaptive", "Ambush", captive["arm"], [])]
    actors += [(o["m"][0].replace("CineOrc", "CapOrc"), "Ambush", o["arm"], []) for o in orcs]
    return actors, [cart] + wheels + javs, SECONDS


def driver_pose(T):
    """He ducks at the burst: down over his knees, the arms over his head."""
    q = ca.driver_pose(0.0)
    duck = ease(env(T, BURST + 0.6, BURST + 1.1))
    q["chest"][0] += 40 * duck
    q["head"][0] += 20 * duck
    for s, k in (("L", 1), ("R", -1)):
        q["upper_arm." + s] = mix({"a": q["upper_arm." + s]}, {"a": [-150, -k * 30, 0]}, duck)["a"]
        q["forearm." + s] = mix({"a": q["forearm." + s]}, {"a": [-100, 0, 0]}, duck)["a"]
    q["head"][2] += 40 * math.sin(TAU * T / 1.3) * (1 - duck) * env(T, HORN, HORN + 0.4)
    return q


def captive_pose(T):
    """At the bars as group 1 left it; it roars at the horn, then shakes the bars and roars on at the burst."""
    q = ca.captive_pose(T0 - 0.01)
    roar = win(T, (HORN + 0.1, HORN + 1.6), 0.25) + win(T, (21.0, 22.6), 0.25) + win(T, (24.2, 25.6), 0.25)
    q["head"][0] -= 35 * roar
    q["chest"][0] -= 12 * roar
    q["head"][2] -= 55 * env(T, HORN, HORN + 0.3)                     # from the men to the forest's sounds
    shake = env(T, BURST + 0.3, BURST + 0.5)
    s = math.sin(TAU * 4.0 * T) * shake
    for side in ("L", "R"):
        q["upper_arm." + side][0] += 10 * s
        q["forearm." + side][0] -= 8 * s
    return q


# ---------------------------------------------------------------- the review: shots 5-6

def camera(T):
    king = Vector((-ca.LANE, ca.FRONT_STOP - 1.7, 0.0))
    if T < 20.0:
        u = (T - T0) / 3.0
        return king + Vector((2.1 - 0.2 * u, 1.3, 1.25)), king + Vector((0.0, 0.0, 0.75)), 30 + 4 * u
    if T < 23.0:
        u = (T - 20.0) / 3.0
        return Vector((0.4, -10.0 + 0.8 * u, 5.8 - 0.4 * u)), Vector((0.0, -0.5, 0.2)), 24
    u = (T - 23.0) / 3.0
    return Vector((-2.7 + 0.2 * u, -3.8, 0.85)), Vector((-0.8, -0.6, 0.45)), 28


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = argv[1]
    os.makedirs(out, exist_ok=True)
    set_objs = fr.make()
    actors, props, total_s = build()
    keep = gt.keep_of(actors, props) | {o.name for o in set_objs}
    aa.stage(keep)
    sc = bpy.context.scene
    for o in sc.objects:
        if o.name in ("ArdanGround", "ArdanPath") or o.name.startswith("ArdanTuft"):
            o.hide_render = True
    cam = bpy.data.objects.new("AmbCam", bpy.data.cameras.new("AmbCam"))
    cam.data.clip_end = 300
    sc.collection.objects.link(cam)
    sc.camera = cam
    for f in range(int(total_s * FPS) + 1):
        loc, at, lens = camera(T0 + f / FPS)
        cam.location = loc
        cam.rotation_euler = (at - loc).to_track_quat('-Z', 'Y').to_euler()
        cam.data.lens = lens
        cam.keyframe_insert("location", frame=f + 1)
        cam.keyframe_insert("rotation_euler", frame=f + 1)
        cam.data.keyframe_insert("lens", frame=f + 1)
    sc.frame_start, sc.frame_end = 1, int(total_s * FPS) + 1
    sheet = os.environ.get("ARDAN_SHEET")
    if sheet:
        sc.render.image_settings.file_format = 'PNG'
        for f in [int(x) for x in sheet.split(",")]:
            sc.frame_set(f)
            sc.render.filepath = os.path.join(out, "ambush_%03d.png" % f)
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
        sc.render.filepath = os.path.join(out, "ambush.mp4")
        bpy.ops.render.render(animation=True)
    print("RENDERED ambush")


if __name__ == "__main__":
    main()
