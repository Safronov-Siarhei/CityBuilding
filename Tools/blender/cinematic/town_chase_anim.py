"""Mission 2's last scene «Подкрепление», groups 6 and 7 (the user, 2026-11-16, on the scene in Unity: «город не похож на
атакованный орками ... нет орков, которые гоняются за мирными жителями, нет трупов на лицах»). Never saves.

    blender --background <Buildings-Size1-Models.blend> --python town_chase_anim.py -- render <out dir> [chase|dead|all]

  chase  4 townsfolk run down a street (+y) in panic -- looking back, hands up, hands on the head -- with 3 orcs at
         their heels (axe and torch up, spear forward); the spearman catches the slow merchant, runs him through from
         behind, he falls on his face; the orc stands over him a moment and runs on. 4.5 s.
  dead   the dead lying in the streets, face down (still): a man-at-arms with his sword, a peasant, a woman, an orc.
         One arrangement Unity lays in several streets, turned. 1 s, nothing moves.
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import bpy  # noqa: E402
import alarm_anim as al  # noqa: E402
import ardan_anim as aa  # noqa: E402
import ardan_anim2 as a2  # noqa: E402
import ardan_sword as asw  # noqa: E402
import elder_anim as ea  # noqa: E402
import figure as fg  # noqa: E402
import gate_talk_anim as gt  # noqa: E402
import kids_anim as ka  # noqa: E402
import lookout  # noqa: E402
import orc_anim as oa  # noqa: E402
import orc_raze_anim as orz  # noqa: E402
import townsfolk as tf  # noqa: E402
from mathutils import Matrix  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
env, ease, mix = a2.env, a2.ease, a2.mix
CHASE_SECONDS = 4.5
DEAD_SECONDS = 1.0
TURN = Matrix.Rotation(math.pi, 4, 'Z')        # the figures face -y: turned to run +y

# (figure, x, y start, speed factor, run dressing)
FOLK = [("CinePeasant", -0.45, 1.3, 1.05, lambda q: al.look_back(q)),
        ("CinePeasantWoman", 0.4, 0.9, 0.92, al.hands_up),
        ("CineBoy", -0.05, 1.9, 0.95, lambda q: al.look_back(q, 0.6)),
        ("CineMerchant", 0.3, -0.9, 0.72, al.hands_on_head)]
# (orc, x, y start, speed factor, run pose)
ORCS = [("CineOrcSpear", 0.3, -2.1, 1.2, oa.spear_run),
        ("CineOrcTorch", -0.35, -2.7, 1.1, oa.torch_run),
        ("CineOrcChampion", 0.05, -3.6, 1.05, oa.brute_run)]
CATCH_GAP = 0.6                 # the spear's reach from the orc's middle to the merchant's
STAND_OVER = 1.0                # the orc's moment over the body before he runs on


def phase_of(d, scale):
    return (d / (a2.RUN_SPEED * scale * a2.RUN_CYCLE)) % 1.0


def thrust(u):
    """The spear driven forward, two-handed, then pulled back."""
    q = oa.brute_run(0.0)
    s = math.sin(math.pi * min(1.0, u * 1.6))
    q["chest"][0] += 12 * s
    q["upper_arm.R"] = [-30 - 55 * s, 4, 0]
    q["forearm.R"] = [-50 + 15 * s, 0, 0]
    q["upper_arm.L"] = [-35, -6, -15]
    q["forearm.L"] = [-75, 0, 0]
    for side in ("L", "R"):
        q["thigh." + side] = [-12 if side == "L" else 6, 0, 0]
        q["shin." + side] = [14, 0, 0]
    return q


def fall_on_face(u):
    """A man thrown on his face: arms flung forward, a leg drawn up (the root tips him over)."""
    q = a2.blank()
    q["upper_arm.L"] = [-155 * u, -35 * u, 0]
    q["upper_arm.R"] = [-120 * u, 45 * u, 0]
    q["forearm.R"] = [-30 * u, 0, 0]
    q["thigh.L"] = [-30 * u, 0, 6 * u]
    q["shin.L"] = [40 * u, 0, 0]
    q["head"] = [-25 * u, 0, 0]
    return q


def build_chase():
    figs = []
    for name, x, y0, k, dress in FOLK:
        scale = tf.FIGURES[name][1]
        arm = ka.rig_figure(tf.make(name, (0, 0, 0)), scale)
        orz.start_action(arm, "Chase")
        figs.append({"arm": arm, "name": name, "scale": scale, "x": x, "y": y0, "v": a2.RUN_SPEED * scale * k,
                     "dress": dress, "folk": True, "d": 0.0, "down": None})
    for kind, x, y0, k, pose in ORCS:
        arm, scale = orz.orc(kind, "Chase")
        orz.start_action(arm, "Chase")
        figs.append({"arm": arm, "name": kind, "scale": scale, "x": x, "y": y0, "v": a2.RUN_SPEED * scale * k,
                     "pose": pose, "folk": False, "d": 0.0, "caught": None})
    merchant = next(f for f in figs if f["name"] == "CineMerchant")
    spear = next(f for f in figs if f["name"] == "CineOrcSpear")
    sc = bpy.context.scene
    dt = 1.0 / FPS
    for f in range(int(CHASE_SECONDS * FPS) + 1):
        t = f / FPS
        sc.frame_set(f + 1)
        # the spearman reaches the merchant: he stops and thrusts, the merchant goes down on his face
        if spear["caught"] is None and spear["y"] + spear["d"] + CATCH_GAP >= merchant["y"] + merchant["d"]:
            spear["caught"] = t
            merchant["down"] = t + 0.18
        for fr in figs:
            arm = fr["arm"]
            if fr["folk"]:
                if fr["down"] is not None and t >= fr["down"]:
                    u = min(1.0, (t - fr["down"]) / 0.45)
                    w = ease(u)
                    y = fr["y"] + fr["d"] + 0.35 * w
                    arm.matrix_world = (Matrix.Translation((fr["x"], y, 0.1 * math.sin(math.pi * u) + 0.05 * w)) @ TURN
                                        @ Matrix.Rotation(math.radians(86 * w), 4, 'X'))
                    aa.rest(arm)
                    ka.apply(arm, fall_on_face(w), fr["scale"])
                    orz.key(arm, f + 1)
                    continue
                if f:
                    fr["d"] += fr["v"] * dt
                q = fr["dress"](a2.run(phase_of(fr["d"], fr["scale"])))
            else:
                c = fr.get("caught")
                if c is not None and t >= c and t < c + STAND_OVER:
                    q = thrust((t - c) / STAND_OVER)
                else:
                    if f:
                        fr["d"] += fr["v"] * dt
                    q = fr["pose"](phase_of(fr["d"], fr["scale"]))
                    q["head"][0] -= 8                                        # eyes on the prey
            arm.matrix_world = Matrix.Translation((fr["x"], fr["y"] + fr["d"], 0)) @ TURN
            aa.rest(arm)
            ka.apply(arm, q, fr["scale"])
            orz.key(arm, f + 1)
    print("CAUGHT at", spear["caught"], "merchant y", merchant["y"] + merchant["d"], "orc y", spear["y"] + spear["d"])
    actors = []
    for fr in figs:
        name = ("CapFolk" + fr["name"][4:]) if fr["folk"] else fr["name"].replace("Cine", "Cap")
        actors.append((name, "Chase", fr["arm"], []))
    return actors, [], CHASE_SECONDS


# (who, x, y, heading deg, pose): face down, each its own sprawl
DEAD = [("guard", 0.0, 0.0, 30, (1.0, 0.0)), ("CinePeasant", 1.1, 0.7, -70, (0.8, 1.0)),
        ("CinePeasantWoman", -1.0, 0.9, 150, (1.0, 0.5)), ("CineOrcSpear", 0.6, -1.0, 200, (0.9, 0.0))]


def dead_pose(variant):
    """Lying on his face: v[0] how far the arms are flung, v[1] one arm under the body instead."""
    a, under = variant
    q = fall_on_face(a)
    if under:
        q["upper_arm.L"] = [-10 * under, 20 * under, 0]
        q["forearm.L"] = [-90 * under, 0, 0]
    q["head"] = [-15, 0, 35]                                     # the cheek on the ground
    return q


def build_dead():
    figs = []
    for i, (who, x, y, h, v) in enumerate(DEAD):
        if who == "guard":
            mesh = fg.make("CineGuardDead", lookout.PARTS, lookout.SCALE, (0, 0, 0))
            arm, scale = ka.rig_figure(mesh, lookout.SCALE), lookout.SCALE
            extra = [asw.sword(arm, mesh.data.materials[0])]
            name = "CapGuardStand"
        elif who.startswith("CineOrc"):
            arm, scale = orz.orc(who, "Dead")
            extra = []
            name = who.replace("Cine", "Cap")
        else:
            scale = tf.FIGURES[who][1]
            arm = ka.rig_figure(tf.make(who, (0, 0, 0)), scale)
            extra = []
            name = "CapFolk" + who[4:]
        orz.start_action(arm, "Dead")
        figs.append((name, arm, scale, x, y, h, v, extra))
    sc = bpy.context.scene
    for f in range(int(DEAD_SECONDS * FPS) + 1):
        sc.frame_set(f + 1)
        for name, arm, scale, x, y, h, v, extra in figs:
            arm.matrix_world = (Matrix.Translation((x, y, 0.05 * scale)) @ Matrix.Rotation(math.radians(h), 4, 'Z')
                                @ Matrix.Rotation(math.radians(88), 4, 'X'))
            aa.rest(arm)
            ka.apply(arm, dead_pose(v), scale)
            orz.key(arm, f + 1)
    return [(name, "Dead", arm, extra) for name, arm, scale, x, y, h, v, extra in figs], [], DEAD_SECONDS


def lane_stage():
    """A town lane between house fronts, for the review only (the game's town is the set)."""
    import cav_charge_anim as cc
    cc.street_stage()


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = argv[1]
    what = argv[2] if len(argv) > 2 else "all"
    os.makedirs(out, exist_ok=True)
    if what in ("chase", "all"):
        actors, props, total = build_chase()
        aa.stage(gt.keep_of(actors, props))
        lane_stage()
        for key_, at, off, lens in (("chase_front", (0.0, 1.5, 0.35), (0.9, 5.5, 0.9), 26),     # they run at us
                                    ("chase_side", (0.3, 0.2, 0.3), (1.0, 2.3, 0.85), 26)):     # the catch, from the side
            ea.shoot(out, key_, total, at, off, lens)
    if what == "dead":
        actors, props, total = build_dead()
        aa.stage(gt.keep_of(actors, props))
        lane_stage()
        for key_, at, off, lens in (("dead_wide", (0.0, 0.0, 0.1), (2.2, -3.0, 2.0), 28),
                                    ("dead_close", (0.2, 0.2, 0.1), (0.9, -1.4, 0.7), 24)):
            ea.shoot(out, key_, total, at, off, lens)


if __name__ == "__main__":
    main()
