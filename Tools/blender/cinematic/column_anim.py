"""Mission 3b «Клетка», the opening scene, group 1 «Колонна» (storyboard v3, APPROVED by the user 2026-11-23, shots
1-4): the King's column rides at a walk along the forest road (forest_road.py, approved) into the ambush clearing and
halts at the felled tree. Never saves; run on a COPY of the .blend (the cart's figures load the orc scripts).
    blender --background <a COPY of Buildings-Size1-Models.blend> --python column_anim.py -- render <out dir>
    ARDAN_SHEET=1,120,300 ... -- frames as PNGs instead of the video

One clock, SECONDS (the scene's own: the next groups go on from HALT_END):
  0-5    wide, high behind: the column winds out of the narrow road into the clearing;
  5-10   a side tracking shot along the column: lances at the slope, the riders glance at the trees;
  10-13  the cage: the monster sits still, watching the trees; it lifts its head, sniffs, rises to the bars and
         stares into the forest (it hears its own);
  13-17  the block: the vanguard's right-hand man throws up his hand -- «Стой! Дорога завалена!» (Unity's subtitle)
         -- and the column halts row by row; every head turns to the tree.
The column (the user: 20 riders + the King): 4 vanguard in two pairs, the King on his white horse beside the
standard-bearer, the cage cart (a driver on the bench, two plain-harnessed horses in the shafts, the wheels ROLL --
cage_cart's CineCageCartBare + 4 CineCageWheel*), 15 riders behind (7 pairs and one). The column rides +y.
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
import figure as fg  # noqa: E402
import forest_road as fr  # noqa: E402
import gate_talk_anim as gt  # noqa: E402
import horse  # noqa: E402
import kids_anim as ka  # noqa: E402
import king_ride  # noqa: E402
import lookout  # noqa: E402
import orc_anim as oa  # noqa: E402
import orcs_cast as oc  # noqa: E402
import rider_anim as ra  # noqa: E402
import square_anim as sq  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
env, ease, mix = a2.env, a2.ease, a2.mix
SECONDS = 17.0
V = 0.55                       # a walk, figure units a second
HALT = 13.4                    # the front row starts to rein in; each row behind 0.12 s later
HALT_LEN = 1.4                 # from the walk to a stop
HAND = (13.0, 15.8)            # the vanguard's raised hand
FRONT_STOP = fr.BLOCK_Y - 1.2  # where the front row stops

# slots: (kind, x, y back from the front row) -- kind: "cav", "king", "bearer"
LANE = 0.32
CART_BACK = 4.0                # the cart's centre behind the front row
CART_STOP_Y = FRONT_STOP - CART_BACK


def slots():
    out = [("cav", -LANE, 0.0), ("cav", LANE, 0.0), ("cav", -LANE, 0.75), ("cav", LANE, 0.75),
           ("king", -LANE, 1.7), ("bearer", LANE, 1.7)]
    back = CART_BACK + 1.3
    for i in range(8):
        for x in ((-LANE, LANE) if i < 7 else (0.0,)):
            out.append(("cav", x, back))
        back += 0.75
    return out


def row_of(back):
    return int(round(back / 0.75))


def distance(t, back):
    """How far a row has walked by t: V until its halt, then easing to a stop over HALT_LEN."""
    h = HALT + 0.12 * row_of(back)
    if t <= h:
        return V * t
    u = min(t - h, HALT_LEN)
    return V * h + V * (u - u * u / (2 * HALT_LEN))


def total(back):
    return distance(1e9, back)


def y_of(t, back):
    """A slot's y at t: it ends where the column stops (the front row FRONT_STOP)."""
    stop = FRONT_STOP - back
    return stop - total(back) + distance(t, back)


def moving(t, back):
    return min(1.0, (distance(t, back) - distance(max(0.0, t - 0.1), back)) / (0.1 * V))


# ---------------------------------------------------------------- the riders

def rider_pose(t, i, kind, pos, reins):
    q = ra.seated(t, 0.0, 0.0)
    q["upper_arm.L"], q["forearm.L"] = [list(v) for v in reins]
    rnd = random.Random(i * 7 + 1)
    ph, per = rnd.uniform(0, TAU), rnd.uniform(3.0, 5.0)
    alarm = env(t, HAND[0] + 0.2 + 0.03 * i, HAND[0] + 0.8 + 0.03 * i)
    # glancing at the trees on the way, then every head to the block ahead
    look = 28 * math.sin(TAU * t / per + ph) * (1 - alarm)
    to_block = sq.look_deg(pos, math.pi, Vector((0.0, fr.BLOCK_Y, 0))) * alarm
    q["head"][2] += look + 0.8 * to_block
    q["chest"][2] += 0.2 * to_block
    q["head"][0] -= 6 * alarm
    if kind == "king":
        # the sword low by the knee, the blade down; at the halt he straightens and lifts it a little
        q["upper_arm.R"] = [-12 - 18 * alarm, 10, 0]
        q["forearm.R"] = [-24 - 20 * alarm, 0, 0]
        q["chest"][0] -= 4 * alarm
    else:
        sq.lance_up(q, 1.0)                          # lances upright on the march, the pennons over the column
    if i == 1:                                        # the vanguard's right-hand man: the hand up, «Стой!»
        up = win(t, HAND, 0.35)
        q["upper_arm.L"] = mix({"a": q["upper_arm.L"]}, {"a": [-160, -12, 0]}, up)["a"]
        q["forearm.L"] = mix({"a": q["forearm.L"]}, {"a": [-12, 0, 0]}, up)["a"]
        q["head"][0] -= 8 * up
    return q


def win(t, w, rise=0.3):
    return env(t, w[0], w[0] + rise) * (1 - env(t, w[1] - rise, w[1]))


# ---------------------------------------------------------------- the cart

def cart_matrix(t):
    y = y_of(t, CART_BACK)
    return Matrix.Translation((0.0, y, 0.0)) @ Matrix.Rotation(math.pi, 4, 'Z')


def captive_pose(t):
    """Sitting low, watching the trees; at 10.3 the head comes up, sniffs; it rises to the bars (gripping them) and
    stares into the forest; at the halt it grins round at the men (the head swings to them, the chest swells)."""
    rise = ease(env(t, 11.4, 12.3))
    q = oa.crouch(0.55 - 0.35 * rise)
    lift = ease(env(t, 10.3, 10.8))
    sniff = env(t, 10.6, 10.8) * (1 - env(t, 11.3, 11.5))
    q["head"][0] += 14 - 30 * lift + 7 * math.sin(TAU * 5.0 * t) * sniff
    q["head"][2] += 25 * math.sin(TAU * t / 6.0) * (1 - lift) - 30 * ease(env(t, 11.2, 11.9))
    grip = ease(env(t, 11.6, 12.2))
    for s, k in (("L", 1), ("R", -1)):
        q["upper_arm." + s] = [-15 * (1 - grip) - 80 * grip, k * (-8 - 14 * grip), 0]
        q["forearm." + s] = [-40 * (1 - grip) - 30 * grip, 0, 0]
    grin = ease(env(t, 14.2, 14.8))
    q["head"][2] += 55 * grin
    q["chest"][0] -= 10 * grin
    return q


# ---------------------------------------------------------------- the take

def plain_horse(name):
    keep = horse.BLUE, horse.GOLD
    horse.BLUE = horse.GOLD = horse.TACK                 # the cart horses: a plain leather browband (the user)
    try:
        hm = cav.make_horse("CineWarhorse", obj=name)
    finally:
        horse.BLUE, horse.GOLD = keep
    return {"h": ka.rig_figure(hm, horse.SCALE, bones=ra.HORSE_BONES)}


def hips_drop(arm, q, scale):
    """The figure's hips height over its root in pose q (to sit it on a bench)."""
    arm.matrix_world = Matrix()
    aa.rest(arm)
    ka.apply(arm, q, scale)
    bpy.context.view_layer.update()
    return (arm.matrix_world @ arm.pose.bones["hips"].head).z


def build():
    pairs = []
    for i, (kind, x, back) in enumerate(slots()):
        if kind == "king":
            p = sq.pair(f"Col{i}", "CineKingHorse", king_ride.make(), king_ride.SCALE)
        else:
            hn = ("CineWarhorse", "CineWarhorseGrey")[i % 3 == 1]
            p = sq.pair(f"Col{i}", hn, fg.make(f"CineCavalryCol{i}", cav.PARTS, cav.SCALE, (0, 0, 0)), cav.SCALE)
            p["hname"] = hn
        p.update(kind=kind, x=x + random.Random(i).uniform(-0.04, 0.04), back=back + random.Random(i + 50).uniform(-0.08, 0.08),
                 phase=random.Random(i + 90).uniform(0, 1))
        pairs.append(p)
    cart = cc.make("CineCageCartBare")
    wheels = [cc.make("CineCageWheel" + ("L" if x < 0 else "R")) for x in (-1, 1, -1, 1)]
    for w, n in zip(wheels, ("FL", "FR", "BL", "BR")):
        w.name = "CineCageWheel" + n
    shaft = [plain_horse(f"ColCartHorse{i}") | {"x": x} for i, x in enumerate((-0.16, 0.16))]
    kind = "CineOrcCaptive"
    mesh = fg.make(kind + "Col", oc.FIGURES[kind][0](), oc.FIGURES[kind][1], (0, 0, 0), orc=True)
    captive = {"arm": ka.rig_figure(mesh, oc.FIGURES[kind][1], bones=oa.BONES), "scale": oc.FIGURES[kind][1]}
    dm = fg.make("CineGuardColDriver", lookout.PARTS, lookout.SCALE, (0, 0, 0))
    driver = {"arm": ka.rig_figure(dm, lookout.SCALE), "scale": lookout.SCALE}
    for p in pairs:
        sq.act(p["h"], "Column")
        sq.act(p["r"], "Column")
    for p in shaft:
        sq.act(p["h"], "Column")
    for a in (captive["arm"], driver["arm"]):
        sq.act(a, "Column")
    sc = bpy.context.scene
    reins = {}
    dq0 = driver_pose(0.0)
    drop = hips_drop(driver["arm"], dq0, driver["scale"])
    bench = cc.SCALE * (cc.BED_Z + 0.235)
    seat_at = Vector((0.0, cc.SCALE * (-cc.HALF_L - 0.13), bench - drop + 0.01))     # the bench, the cart's frame
    for f in range(int(SECONDS * FPS) + 1):
        t = f / FPS
        sc.frame_set(f + 1)
        for i, p in enumerate(pairs):
            pos = Vector((p["x"], y_of(t, p["back"]), 0.0))
            mv = moving(t, p["back"])
            sq.seat(p, pos, math.pi, sq.horse_walk_pose(t + 0.37 * i, distance(t, p["back"]) + p["phase"] * sq.WALK_STRIDE, mv))
            key = "king" if p["kind"] == "king" else "cav"
            if key not in reins:
                reins[key] = sq.solve_reins(p, ra.seated(t, 0.0, 0.0))
            q = rider_pose(t, i, p["kind"], pos, reins[key])
            aa.rest(p["r"])
            ka.apply(p["r"], q, p["scale"])
        # the cart, its wheels rolling with the distance, the shaft horses, the driver, the monster
        cm = cart_matrix(t)
        d = distance(t, CART_BACK)
        cart.matrix_world = cm
        cart.keyframe_insert("location", frame=f + 1)
        cart.keyframe_insert("rotation_euler", frame=f + 1)
        roll = d / (cc.WHEEL_R * cc.SCALE)
        for w, (sx, sy) in zip(wheels, ((-1, 0), (1, 0), (-1, 1), (1, 1))):
            hub = Vector((sx * cc.WHEEL_X, cc.WHEEL_YS[sy], cc.WHEEL_R)) * cc.SCALE
            w.matrix_world = cm @ Matrix.Translation(hub) @ Matrix.Rotation(roll, 4, 'X')
            w.keyframe_insert("location", frame=f + 1)
            w.keyframe_insert("rotation_euler", frame=f + 1)
        for k, p in enumerate(shaft):
            hp = cm @ Vector((p["x"], -cc.SCALE * (cc.HALF_L + 0.66), 0.0))
            p["h"].location = hp
            p["h"].rotation_euler = (0, 0, math.pi)
            aa.rest(p["h"])
            ra.h_apply(p["h"], sq.horse_walk_pose(t + 0.6 * k, d + 0.2 * k, moving(t, CART_BACK)))
            sq.key(p["h"], f + 1)
        arm = driver["arm"]
        arm.matrix_world = cm @ Matrix.Translation(seat_at)
        aa.rest(arm)
        ka.apply(arm, driver_pose(t), driver["scale"])
        sq.key(arm, f + 1)
        if f == 180 and os.environ.get("COL_DEBUG"):
            bpy.context.view_layer.update()
            print("DRIVER", tuple(arm.matrix_world.translation), "hips", tuple(arm.matrix_world @ arm.pose.bones["hips"].head),
                  "drop", drop, "bench", bench, "kids", [c.name for c in arm.children], "hidden", [c.hide_render for c in arm.children])
        arm = captive["arm"]
        arm.matrix_world = cm @ Matrix.Translation((0.0, 0.1, cc.SCALE * cc.BED_Z)) @ Matrix.Rotation(-math.pi / 2, 4, 'Z')
        aa.rest(arm)
        ka.apply(arm, captive_pose(t), captive["scale"])
        sq.key(arm, f + 1)
        for p in pairs:
            sq.key(p["h"], f + 1)
            sq.key(p["r"], f + 1)
    names = {"CineKingHorse": "CapKingHorse", "CineWarhorse": "CapWarhorse", "CineWarhorseGrey": "CapWarhorseGrey"}
    actors = []
    for p in pairs:
        if p["kind"] == "king":
            actors += [("CapKingHorse", "Column", p["h"], []), ("CapKingRide", "Column", p["r"], [])]
        else:
            actors += [(names[p["hname"]], "Column", p["h"], []), ("CapCavalry", "Column", p["r"], [])]
    actors += [("CapCartHorse", "Column", p["h"], []) for p in shaft]
    actors += [("CapGuardStand", "Column", driver["arm"], []), ("CapOrcCaptive", "Column", captive["arm"], [])]
    return actors, [cart] + wheels, SECONDS


def driver_pose(t):
    """On the bench: seated, the fists forward on the reins; at the halt he hauls them back and leans back."""
    q = ra.seated(t, 0.0, 0.0)
    haul = win(t, (HALT + 0.4, HALT + 2.2), 0.4)
    for s, k in (("L", 1), ("R", -1)):
        q["thigh." + s] = [-80, 0, k * 10]
        q["shin." + s] = [80, 0, 0]
        q["upper_arm." + s] = [-40 + 22 * haul, -k * 8, 0]
        q["forearm." + s] = [-50 - 25 * haul, 0, 0]
    q["chest"][0] += 8 - 14 * haul
    q["head"][2] += 20 * math.sin(TAU * t / 4.7) * (1 - env(t, HAND[0], HAND[0] + 0.6))
    return q


# ---------------------------------------------------------------- the review: the cuts of shots 1-4

def camera(t):
    """(location, look-at, lens) of the cut playing at t."""
    cy = y_of(t, CART_BACK)
    if t < 5.0:
        u = t / 5.0
        return Vector((6.5 - 1.0 * u, -15.0 + 2.5 * u, 7.0 - 0.8 * u)), Vector((0.0, -5.0 + 2.6 * u, 0.0)), 26
    if t < 10.0:
        return Vector((2.5, cy - 1.6, 0.75)), Vector((0.0, cy + 0.4, 0.45)), 28
    if t < 13.0:
        u = (t - 10.0) / 3.0
        return Vector((1.15, cy - 0.5 + 0.15 * u, 0.62)), Vector((0.0, cy + 0.1, 0.55)), 34 + 6 * u
    return Vector((1.35, fr.BLOCK_Y - 5.6, 1.45)), Vector((-0.1, fr.BLOCK_Y - 0.6, 0.35)), 28


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
    cam = bpy.data.objects.new("ColCam", bpy.data.cameras.new("ColCam"))
    cam.data.clip_end = 300
    sc.collection.objects.link(cam)
    sc.camera = cam
    for f in range(int(total_s * FPS) + 1):
        loc, at, lens = camera(f / FPS)
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
            sc.render.filepath = os.path.join(out, "column_%03d.png" % f)
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
        sc.render.filepath = os.path.join(out, "column.mp4")
        bpy.ops.render.render(animation=True)
    print("RENDERED column")


if __name__ == "__main__":
    main()
