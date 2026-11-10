"""The townsfolk's opening-cutscene animations (2026-11-10, after the priest's were approved):
  * Market (shot 2): the merchant behind the fruit stall calls out, one hand waving high, the
    other cupped at his mouth, and turns to the woman with an open palm; she reaches for a fruit,
    turns it before her eyes and drops it in her basket; behind them a peasant walks by pulling a
    farm cart -- its shafts in his fists, its wheels turning as it rolls.
  * Panic (shot 11): a torchbearer orc storms in; the woman shrieks, hands to her face, and runs;
    the merchant ducks behind his counter, arms over his head; the peasant lets the shafts fall
    (the cart drops onto them) and runs; the orc shoulders the cloth stall over as he passes.
The stalls are built here one by one (village.stall) so the cloth one can topple; the cart is
built without its wheels (village.cart(wheels=False)) so they can turn. Never saves.

    blender --background <Buildings-Size1-Models.blend> --python townsfolk_anim.py -- render <out dir> [market|panic|all]
    ARDAN_SHEET=1,30,60 ... -- frames as PNGs instead of the videos
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
import bell_anim as ba  # noqa: E402
import figure as fg  # noqa: E402
import kids_anim as ka  # noqa: E402
import orc_anim as oa  # noqa: E402
import orcs_cast as oc  # noqa: E402
import townsfolk as tf  # noqa: E402
import village  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
S = fg.SET_SCALE
K = village.CART['shrink'] * S                      # the cart's own size on the set
MARKET = 6.0
PANIC = 4.0
WOMAN_AT = Vector((-0.38, -0.62, 0))
MERCHANT_AT = Vector((0.0, 0.14, 0))
FRUIT = Vector((-0.24, -0.13, 0.225)) * S           # the red fruit she takes (a stall's first heap)
LANE_Y = -1.4                                       # the peasant's way, in front of the stalls
PEASANT_FROM, PEASANT_TO = 2.0, 0.35
DROP = 0.55                                         # in the panic: he lets the shafts fall
ORC_FROM, ORC_TO = Vector((3.0, -0.4, 0)), Vector((-1.6, -0.4, 0))
ORC_WAIT = 0.3
TIP = None                                          # set in main: when the orc's shoulder meets the stall


def walk_pose(phase, stride=22.0):
    """aa.walk as data (the arms left to the caller)."""
    q = a2.blank()
    for side, off in (("L", 0.0), ("R", 0.5)):
        s = math.sin(TAU * (phase + off))
        c = math.cos(TAU * (phase + off))
        thigh = -stride * s
        knee = 4 + 50 * max(0.0, c) ** 1.4 + 5 * max(0.0, s)
        foot = -(thigh + knee) - 12 * max(0.0, s) + 25 * max(0.0, -s) ** 2 * max(0.0, c)
        q["thigh." + side] = [thigh, 0, 0]
        q["shin." + side] = [knee, 0, 0]
        q["foot." + side] = [foot, 0, 0]
    s = math.sin(TAU * phase)
    q["hips_loc"] = [0, 0.006 * math.cos(2 * TAU * phase) - 0.003, 0]
    q["hips"] = [0, 2.5 * s, 6 * s]
    q["chest"] = [8, -2 * s, -6 * s]
    q["head"] = [-4, 0, 3 * s]
    return q


def aim(q, side, frm, to, weight=1.0):
    q["aim.upper_arm." + side] = list((Vector(to) - Vector(frm)).normalized()) + [weight]


def shoulder(at, heading, side, scale, z=0.384):
    """A figure's shoulder in the world: k=+1 is its left."""
    k = 1 if side == "L" else -1
    return Vector(at) + Matrix.Rotation(heading, 3, 'Z') @ Vector((k * 0.096, 0, z)) * scale


# ---------------------------------------------------------------- the merchant

M_HEAD = 0.0                                        # he faces -y, out over his counter
MS = tf.FIGURES["CineMerchant"][1]


def merchant_market(t):
    q = a2.blank()
    call = 1 - a2.env(t, 1.9, 2.2) * (1 - a2.env(t, 3.2, 3.5))
    mouth = MERCHANT_AT + Vector((0.0, -0.06, 0.43)) * MS
    sl, sr = shoulder(MERCHANT_AT, M_HEAD, "L", MS), shoulder(MERCHANT_AT, M_HEAD, "R", MS)
    wave = math.sin(TAU * t * 1.6)
    # calling: the right hand high and waving, the left cupped at his mouth, turning to the street
    hi = sr + Vector((-0.08 + 0.06 * wave, -0.1, 0.16))
    palm = WOMAN_AT + Vector((0.05, 0.1, 0.3))
    if call > 0.01:
        aim(q, "R", sr, hi, call)
        q["forearm.R"] = [-25 * call, 0, 0]
        aim(q, "L", sl, mouth + Vector((0.03, -0.03, -0.06)), call)
        q["forearm.L"] = [-120 * call, 0, 0]
    if call < 0.99:                                  # turning to her: an open palm, a nod
        aim(q, "R", sr, palm, 1 - call)
        q["forearm.R"] = [-30 * (1 - call), 0, 0]
    q["chest"] = [-4 * call, 0, 12 * math.sin(TAU * t / 2.4) * call - 20 * (1 - call)]
    q["head"] = [-6 + 4 * math.sin(TAU * t * 2.2) * call + 8 * (1 - call), 0, -10 * (1 - call)]
    return MERCHANT_AT, M_HEAD, q


def merchant_panic(t):
    q = a2.blank()
    look = a2.env(t, 0.3, 0.45)
    q["head"] = [-10 * look, 0, 50 * look * (1 - a2.env(t, 0.65, 0.8))]
    duck = a2.env(t, 0.6, 0.95)
    c = ka.crouch(0.9 * duck)
    for k in c:
        q[k] = c[k]
    q["chest"][0] += 25 * duck
    q["head"][0] += 20 * duck
    shake = math.sin(TAU * t * 9) * duck
    for side, k in (("L", 1), ("R", -1)):                # arms over his head, trembling
        q["upper_arm." + side] = [-150 * duck + 3 * shake, k * -15 * duck, 0]
        q["forearm." + side] = [-95 * duck, 0, k * 20 * duck]
    return MERCHANT_AT, M_HEAD, q


# ---------------------------------------------------------------- the woman

W_HEAD = math.pi                                    # she faces +y, into the stall
WS = tf.FIGURES["CinePeasantWoman"][1]


def woman_market(t):
    q = a2.blank()
    sr = shoulder(WOMAN_AT, W_HEAD, "R", WS)
    face = WOMAN_AT + Vector((0.02, 0.1, 0.4)) * WS
    reach = a2.env(t, 0.6, 1.2) * (1 - a2.env(t, 1.3, 1.6))
    look = a2.env(t, 1.35, 1.9) * (1 - a2.env(t, 3.0, 3.3))
    put = a2.env(t, 3.2, 3.6) * (1 - a2.env(t, 3.9, 4.3))
    basket = WOMAN_AT + Vector((-0.12, 0.04, 0.2)) * WS
    d = (FRUIT - sr) * reach + (face - sr) * look + (basket - sr) * put
    w = max(reach, look, put)
    if w > 0.01:
        aim(q, "R", sr, sr + d / max(w, 1e-3), w)
    q["forearm.R"] = [-10 * reach - 70 * look - 40 * put, 0, 0]
    q["chest"] = [12 * reach + 6 * put, 0, -8 * put]
    q["head"] = [18 - 8 * look + 6 * math.sin(TAU * t * 0.7) * look, 8 * math.sin(TAU * t * 0.5) * look,
                 -25 * a2.env(t, 4.3, 4.7)]
    q["head"][0] += 4 * math.sin(TAU * t * 2.0) * a2.env(t, 4.5, 4.8)    # chatting with the merchant
    return WOMAN_AT, W_HEAD, q


def woman_scream(t):
    q = a2.blank()
    look = a2.env(t, 0.25, 0.4)
    q["head"] = [-12 * look, 0, -60 * look]
    q["chest"] = [-10 * look, 0, -20 * look]
    face = WOMAN_AT + Matrix.Rotation(W_HEAD - 1.0 * look, 3, 'Z') @ Vector((0, -0.06, 0.43)) * WS
    hands = a2.env(t, 0.3, 0.55)
    for side, k in (("L", 1), ("R", -1)):
        sh = shoulder(WOMAN_AT, W_HEAD, side, WS)
        aim(q, side, sh, face + Vector((0, 0, -0.08)) + Matrix.Rotation(W_HEAD, 3, 'Z') @ Vector((k * 0.04, 0, 0)), hands)
        q["forearm." + side] = [-110 * hands, 0, k * 25 * hands]
    return WOMAN_AT, W_HEAD, q


# ---------------------------------------------------------------- the peasant and his cart

P_HEAD = -math.pi / 2                               # he walks -x
PS = tf.FIGURES["CinePeasant"][1]
P_SPEED = (PEASANT_FROM - PEASANT_TO) / MARKET
P_CYCLE = 1.15


def peasant_at(t):
    return Vector((PEASANT_FROM - P_SPEED * t, LANE_Y, 0))


def pulling(q):
    """His arms down at his sides, gripping the shafts, leaning into the pull."""
    for side, k in (("L", 1), ("R", -1)):
        q["upper_arm." + side] = [14, k * -6, 0]
        q["forearm." + side] = [-28, 0, 0]
    q["chest"][0] += 8
    return q


def peasant_market(t):
    q = pulling(walk_pose((P_SPEED * t / (0.42 * 1.0 * P_CYCLE * 0.62)) % 1.0, stride=20.0))
    return peasant_at(t), P_HEAD, q


def peasant_startle(t):
    """The panic's first 0.75 s: the walk dies, he looks back over his shoulder, flings the shafts."""
    at = peasant_at(MARKET) + Vector((-P_SPEED * min(t, 0.3) * (1 - min(t, 0.3) / 0.6), 0, 0))
    q = pulling(walk_pose(0.0, stride=20.0 * (1 - a2.env(t, 0.0, 0.3))))
    look = a2.env(t, 0.25, 0.45)
    q["head"][2] += 70 * look
    q["chest"][2] += 25 * look
    fling = a2.env(t, DROP, DROP + 0.12)
    for side, k in (("L", 1), ("R", -1)):
        q["upper_arm." + side] = [14 - 80 * fling, k * (-6 - 20 * fling), 0]
        q["forearm." + side] = [-28 - 30 * fling, 0, 0]
    return at, P_HEAD, q


def cart_pieces():
    body = fg.install_static("CineCartBody", village.cart(wheels=False))
    wheels = []
    for i, y in enumerate((-1, 1)):
        b = Builder()
        village.cart_wheel(b, 0, 0)
        for v in b.bm.verts:
            v.co *= village.CART['shrink']
        wheels.append((fg.install_static("CineCartWheel%d" % i, b), y * (village.CART['W'] + 0.05) * K))
    return body, wheels


def key_cart(body, wheels, prig, frames, holding):
    """The cart follows the fists that hold its shafts (or drops onto them when let go)."""
    sc = bpy.context.scene
    c = village.CART
    axle_z = c['z'] * K
    end = Vector(((c['L'] + 0.5) * K, 0, 0.02 * K))                  # a shaft's end, cart-local
    rel = end - Vector((0, 0, axle_z))
    R = rel.length
    alpha = math.atan2(rel.z, rel.x)
    yaw = math.pi                                                    # its shafts point at him (-x)
    spin = 0.0
    last = None
    for f in range(frames):
        sc.frame_set(f + 1)
        t = f / FPS
        fists = [prig.matrix_world @ prig.pose.bones["forearm." + s].tail for s in ("L", "R")]
        F = (fists[0] + fists[1]) / 2
        hold = holding(t)
        h = (F.z - axle_z) * hold + rel.z * (1 - hold)
        h += 0.02 * math.exp(-max(0.0, t - 0.1) * 12) * abs(math.sin(t * 30)) * (1 - hold) if hold < 1 else 0
        phi = alpha - math.asin(max(-1.0, min(1.0, h / R)))
        xr = R * math.cos(alpha - phi)
        if last is None or hold > 0.5:
            axle = Vector((F.x - math.cos(yaw) * xr, LANE_Y, axle_z))
            if last is not None and hold <= 0.5:
                axle = last
        else:
            axle = last
        if last is not None:
            spin += (axle - last).length / (c['r'] * K)
        last = axle
        m = Matrix.Translation(axle) @ Matrix.Rotation(yaw, 4, 'Z') @ Matrix.Rotation(phi, 4, 'Y') @ Matrix.Translation((0, 0, -axle_z))
        body.matrix_world = m
        for path in ("location", "rotation_euler"):
            body.keyframe_insert(path, frame=f + 1)
        for wh, y in wheels:
            wh.matrix_world = Matrix.Translation(axle) @ Matrix.Rotation(yaw, 4, 'Z') @ Matrix.Translation((0, y, 0)) @ Matrix.Rotation(spin, 4, 'Y')
            for path in ("location", "rotation_euler"):
                wh.keyframe_insert(path, frame=f + 1)


# ---------------------------------------------------------------- the stalls

def stall_pieces():
    b = Builder()
    village.stall(b, 0.0, 0.0, ('roof_red', 'plaster_white'), 'fruit')
    fruit_st = fg.install_static("CineStallFruit", b)
    c = Builder()
    village.stall(c, 0.0, -0.2, ('roof_blue', 'plaster_yellow'), 'cloth')     # its back edge on the origin: it topples about it
    cloth = fg.install_static("CineStallCloth", c, location=(0.85 * S, (0.1 + 0.2) * S, 0))
    f = Builder()
    f.ball(0, 0, 0, 0.018, 0.032, 'roof_red', sides=6)
    fruit = fg.install_static("CineFruit", f, location=tuple(FRUIT))
    return fruit_st, cloth, fruit


def key_fruit(fruit, wrig, frames):
    """On the counter; in her right fist from 1.3 s; in the basket (her left fist) from 3.85 s."""
    sc = bpy.context.scene
    for f in range(frames):
        t = f / FPS
        sc.frame_set(f + 1)
        if t < 1.3:
            p = FRUIT
        else:
            bone = "forearm.R" if t < 3.85 else "forearm.L"
            p = wrig.matrix_world @ wrig.pose.bones[bone].tail + Vector((0, 0, 0.012 if t >= 3.85 else -0.005))
        fruit.location = p
        fruit.keyframe_insert("location", frame=f + 1)


def key_topple(cloth, frames):
    for f in range(frames):
        t = f / FPS
        x = a2.ease(min(1.0, max(0.0, (t - TIP) / 0.5)))
        ang = -82 * x * x
        if t > TIP + 0.5:
            ang += 6 * math.exp(-(t - TIP - 0.5) * 9) * abs(math.sin((t - TIP - 0.5) * 25))
        cloth.rotation_euler = (math.radians(ang), 0, 0)
        cloth.keyframe_insert("rotation_euler", frame=f + 1)


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
    global TIP
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = argv[1]
    what = argv[2] if len(argv) > 2 else "all"
    os.makedirs(out, exist_ok=True)
    fruit_st, cloth, fruit = stall_pieces()
    body, wheels = cart_pieces()
    rigs = {}
    for name in ("CineMerchant", "CinePeasantWoman", "CinePeasant"):
        rigs[name] = ka.rig_figure(tf.make(name), tf.FIGURES[name][1])
    orc = ka.rig_figure(oc.make("CineOrcTorch"), 1.0, bones=oa.BONES)
    keep = {fruit_st.name, cloth.name, fruit.name, body.name, orc.name} | {w.name for w, _ in wheels}
    for r in list(rigs.values()) + [orc]:
        keep |= {r.name} | {c.name for c in r.children}
    aa.stage(keep)
    for o in bpy.context.scene.objects:
        if o.name.startswith("ArdanPath") or o.name.startswith("ArdanTuft"):
            o.hide_render = True
    view = ((0.35, -0.45, 0.3), (-0.7, -3.6, 1.5), 32)

    def track(scale, apply=ba.apply):
        tr = ka.Track(scale, 0.95 * scale * 1.05)
        tr.apply = apply
        return tr

    if what in ("market", "all"):
        orc.location = (0, 6, -3)
        for name, fn in (("CineMerchant", merchant_market), ("CinePeasantWoman", woman_market), ("CinePeasant", peasant_market)):
            tr = track(tf.FIGURES[name][1])
            tr.add('fn', MARKET, fn)
            tr.bake(rigs[name], name + "_Market")
        frames = int(MARKET * FPS) + 1
        key_cart(body, wheels, rigs["CinePeasant"], frames, lambda t: 1.0)
        key_fruit(fruit, rigs["CinePeasantWoman"], frames)
        shoot(out, "town_market", MARKET, *view)
    if what in ("panic", "all"):
        for o in (fruit, body, cloth) + tuple(w for w, _ in wheels):
            o.animation_data_clear()
        sp = 0.95 * 1.05
        dist = (ORC_FROM.x - (0.85 * S + 0.25))
        TIP = ORC_WAIT + dist / sp + 0.35 / 2
        tr = track(tf.FIGURES["CineMerchant"][1])
        tr.add('fn', PANIC, merchant_panic)
        tr.bake(rigs["CineMerchant"], "CineMerchant_Panic")
        tr = track(tf.FIGURES["CinePeasantWoman"][1])
        tr.add('fn', 0.85, woman_scream).add('run', PANIC - 0.85, WOMAN_AT, Vector((-2.4, -1.6, 0)))
        tr.bake(rigs["CinePeasantWoman"], "CinePeasantWoman_Panic")
        tr = track(tf.FIGURES["CinePeasant"][1])
        start = peasant_at(MARKET) + Vector((-P_SPEED * 0.15, 0, 0))
        tr.add('fn', 0.75, peasant_startle).add('run', PANIC - 0.75, start, start + Vector((-3.2, 0.3, 0)))
        tr.bake(rigs["CinePeasant"], "CinePeasant_Panic")
        tr = track(1.0, apply=ka.apply)
        tr.run_pose = oa.torch_run
        tr.add('pose', ORC_WAIT, ORC_FROM, lambda t: oa.crouch(0), tuple(ORC_TO))
        tr.add('run', PANIC - ORC_WAIT, ORC_FROM, ORC_TO)
        tr.bake(orc, "CineOrcTorch_Raid")
        frames = int(PANIC * FPS) + 1
        key_cart(body, wheels, rigs["CinePeasant"], frames, lambda t: 1.0 - a2.env(t, DROP, DROP + 0.12))
        # the fruit stays in her basket
        sc = bpy.context.scene
        for f in range(frames):
            sc.frame_set(f + 1)
            fruit.location = rigs["CinePeasantWoman"].matrix_world @ rigs["CinePeasantWoman"].pose.bones["forearm.L"].tail + Vector((0, 0, 0.012))
            fruit.keyframe_insert("location", frame=f + 1)
        key_topple(cloth, frames)
        shoot(out, "town_panic", PANIC, *view)


if __name__ == "__main__":
    main()
