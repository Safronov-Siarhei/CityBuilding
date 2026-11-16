"""Mission 2's last scene «Подкрепление», group 2: the cavalry bursts into a street of the burning town and rides
the orcs down (the user, 2026-11-16: «они проносятся по городу и уничтожают всех орков»). Never saves.

    blender --background <Buildings-Size1-Models.blend> --python cav_charge_anim.py -- render <out dir> [street|cut|lance|all]

The ride as group 1 (cav_ride_anim: legs only, the King ahead, sword up; lances couched). Four orcs in the street
hear them, turn and brace; as each is reached he is struck down -- the King's sword cuts the first (he spins and
drops on his side), lances throw the others back off their feet onto their backs. The rider who strikes jolts.
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
import cav_ride_anim as cr  # noqa: E402
import cavalry as cav  # noqa: E402
import elder_anim as ea  # noqa: E402
import figure as fg  # noqa: E402
import gate_talk_anim as gt  # noqa: E402
import horse  # noqa: E402
import kids_anim as ka  # noqa: E402
import king_ride  # noqa: E402
import orc_anim as oa  # noqa: E402
import orcs_cast as oc  # noqa: E402
import rider_anim as ra  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
env, ease = a2.env, a2.ease
SECONDS = 5.6
V = cr.V
START_Y = -2.0
ROWS = [[0.0], [-0.42, 0.42], [-0.62, 0.0, 0.62], [-0.42, 0.42]]
ROW_GAP = cr.ROW_GAP
LANCE_REACH, SWORD_REACH = 1.3, 0.3
STREET_HALF = 1.3
TURN_AT = 0.3                                       # the orcs hear the hooves and turn

# the orcs: (kind, x, y, struck by (row, slot), how they fall)
ORCS = [
    ("CineOrcSpear", 0.36, 2.4, (0, 0), "cut"),
    ("CineOrcTorch", -0.6, 3.6, (1, 0), "thrown"),
    ("CineOrcChampion", 0.5, 5.0, (2, 2), "thrown"),
    ("CineOrcSpear", -0.15, 6.3, (2, 1), "thrown"),
]


def slots():
    out = []
    for r, row in enumerate(ROWS):
        for s, x in enumerate(row):
            out.append((r, s, x, -r * ROW_GAP - (0.25 if r else 0.0)))
    return out


def rider_y(back, t):
    return START_Y + back + V * t


def hit_time(back, y, reach):
    return (y - reach - START_Y - back) / V


def orc_guard(kind, t):
    """Before the blow: turned away (rummaging in the ruins), then round to the riders, crouched and braced."""
    look = env(t, TURN_AT, TURN_AT + 0.5)
    q = oa.crouch(0.35 * look)
    if kind == "CineOrcTorch":
        q["upper_arm.L"] = [-130, -10, 0]
        q["forearm.L"] = [-15, 0, 0]
        q["upper_arm.R"] = [-60 * look, 8, 0]
        q["forearm.R"] = [-70, 0, 0]
    elif kind == "CineOrcSpear":
        q["upper_arm.L"] = [-35 * look, -6, -15 * look]
        q["forearm.L"] = [-75 * look, 0, 0]
        q["upper_arm.R"] = [-40 * look, 4, 0]
        q["forearm.R"] = [-50 * look, 0, 0]
    else:
        q["upper_arm.L"] = [-40 * look, -30 * look, 0]
        q["upper_arm.R"] = [-40 * look, 30 * look, 0]
        q["forearm.L"] = q["forearm.R"] = [-60 * look, 0, 0]
    return q


def orc_down(u):
    """Struck: arms flung, legs limp -- u 0..1 over the fall."""
    q = a2.blank()
    q["upper_arm.L"] = [-150 * u, -40 * u, 0]
    q["upper_arm.R"] = [-150 * u, 40 * u, 0]
    q["forearm.L"] = q["forearm.R"] = [-20 * u, 0, 0]
    q["thigh.L"] = [-25 * u, 0, 8 * u]
    q["thigh.R"] = [-10 * u, 0, -8 * u]
    q["shin.L"] = [30 * u, 0, 0]
    q["head"] = [-20 * u, 0, 0]
    return q


def orc_root(o, t, t_hit):
    """The orc's root: turning from the street's far end (+y) to face the riders (-y); after the blow thrown
    along the charge (+y) in an arc and laid on his back (or spun and dropped on his side by the King's cut)."""
    kind, x, y, _, how = o
    look = ease(env(t, TURN_AT, TURN_AT + 0.5))
    heading = math.pi * (1 - look)                     # 0 faces -y (towards the riders)
    base = Matrix.Translation((x, y, 0)) @ Matrix.Rotation(heading, 4, 'Z')
    if t < t_hit:
        return base, 0.0
    u = min(1.0, (t - t_hit) / 0.55)
    w = ease(u)
    if how == "thrown":
        fly = Vector((0.15 * w, 1.1 * w, 0.32 * math.sin(math.pi * u) + 0.07 * w))
        tilt = Matrix.Rotation(math.radians(-90 * w), 4, 'X')        # backwards, the head towards +y
        return Matrix.Translation(fly) @ base @ tilt, u
    spin = Matrix.Rotation(math.radians(-140 * w), 4, 'Z')
    side = Matrix.Rotation(math.radians(85 * ease(max(0.0, (u - 0.3) / 0.7))), 4, 'Y')
    return Matrix.Translation((0.35 * w, 0.4 * w, 0.06 * w)) @ base @ spin @ side, u


def build():
    pairs, reins = [], {}
    for r, s, x, back in slots():
        king = r == 0
        hname = "CineKingHorse" if king else ("CineWarhorse", "CineWarhorseGrey")[(r + s) % 2]
        hm = cav.make_horse(hname, obj=f"{hname}Charge{r}{s}")
        h_arm = ka.rig_figure(hm, horse.SCALE, bones=ra.HORSE_BONES)
        if king:
            rm, scale = king_ride.make(), king_ride.SCALE
        else:
            rm, scale = fg.make(f"CineCavalryCharge{r}{s}", cav.PARTS, cav.SCALE, (0, 0, 0)), cav.SCALE
        r_arm = ka.rig_figure(rm, scale)
        pairs.append({"h": h_arm, "r": r_arm, "king": king, "scale": scale, "x": x, "back": back,
                      "phase": ((r * 0.37 + s * 0.53) % 1.0), "slot": (r, s), "hname": hname, "hit": None})
    orcs = []
    for i, o in enumerate(ORCS):
        kind = o[0]
        mesh = fg.make(f"{kind}Charge{i}", oc.FIGURES[kind][0](), oc.FIGURES[kind][1], (0, 0, 0), orc=True)
        arm = ka.rig_figure(mesh, oc.FIGURES[kind][1], bones=oa.BONES)
        p = next(p for p in pairs if p["slot"] == o[3])
        t_hit = hit_time(p["back"], o[2], SWORD_REACH if p["king"] else LANCE_REACH)
        p["hit"] = t_hit
        orcs.append({"o": o, "arm": arm, "hit": t_hit, "scale": oc.FIGURES[kind][1]})
        print(f"ORC {kind} struck at {t_hit:.2f}s")
    for a in [p["h"] for p in pairs] + [p["r"] for p in pairs] + [o["arm"] for o in orcs]:
        a.animation_data_create()
        act = bpy.data.actions.new(f"{a.name}_Charge")
        act.use_fake_user = True
        a.animation_data.action = act
    sc = bpy.context.scene
    for f in range(int(SECONDS * FPS) + 1):
        t = f / FPS
        sc.frame_set(f + 1)
        for p in pairs:
            d = V * t
            h_arm, r_arm = p["h"], p["r"]
            h_arm.location = (p["x"], rider_y(p["back"], t), 0.0)
            h_arm.rotation_euler = (0, 0, ra.ROAD)
            aa.rest(h_arm)
            ra.h_apply(h_arm, cr.legs_only((d / cr.CYCLE + p["phase"]) % 1.0))
            bpy.context.view_layer.update()
            r_arm.matrix_world = gt.bone_world(h_arm, "body") @ Matrix.Translation(ra.SEAT)
            q = ra.seated(t, 1.0, 0.0)
            strike = 0.0
            if p["hit"] is not None:
                strike = env(t, p["hit"] - 0.25, p["hit"]) * (1 - env(t, p["hit"], p["hit"] + 0.35))
            if p["king"]:
                # the sword up and pumping, then a cut down to the right across the orc as he passes
                # (the cut's angles searched on the rig: the blade down and out to his right, onto the orc)
                pump = math.sin(TAU * t / 0.9) * (1 - strike)
                up, cut = (-150 + 12 * pump, 10, 0), (-30, -10, -60)
                q["upper_arm.R"] = [up[i] + (cut[i] - up[i]) * strike for i in range(3)]
                q["forearm.R"] = [-20 - 10 * pump + 10 * strike, 0, 0]
                q["chest"][2] += 15 * strike
                q["chest"][1] -= 10 * strike
            else:
                q["upper_arm.R"] = [-78 + 10 * strike, 4, 0]       # the lance jolts at the blow
                q["forearm.R"] = [-6, 0, 0]
                q["chest"][0] -= 8 * strike
            key = "king" if p["king"] else "cav"
            if key not in reins:
                aa.rest(r_arm)
                ka.apply(r_arm, q, p["scale"])
                cav.hold_reins(h_arm, r_arm, p["scale"], q)
                reins[key] = (list(q["upper_arm.L"]), list(q["forearm.L"]))
            q["upper_arm.L"], q["forearm.L"] = [list(v) for v in reins[key]]
            aa.rest(r_arm)
            ka.apply(r_arm, q, p["scale"])
        for o in orcs:
            m, u = orc_root(o["o"], t, o["hit"])
            o["arm"].matrix_world = m
            aa.rest(o["arm"])
            q = orc_down(ease(u)) if t >= o["hit"] else orc_guard(o["o"][0], t)
            ka.apply(o["arm"], q, o["scale"])
        for a in [p["h"] for p in pairs] + [p["r"] for p in pairs] + [o["arm"] for o in orcs]:
            for pb in a.pose.bones:
                pb.keyframe_insert("rotation_quaternion", frame=f + 1, group=pb.name)
                pb.keyframe_insert("location", frame=f + 1, group=pb.name)
            a.keyframe_insert("location", frame=f + 1)
            a.keyframe_insert("rotation_euler", frame=f + 1)
            a.keyframe_insert("rotation_quaternion", frame=f + 1)
    actors = []
    names = {"CineKingHorse": "CapKingHorse", "CineWarhorse": "CapWarhorse", "CineWarhorseGrey": "CapWarhorseGrey"}
    for p in pairs:
        actors += [(names[p["hname"]], "Charge", p["h"], []), ("CapKingRide" if p["king"] else "CapCavalry", "Charge", p["r"], [])]
    for o in orcs:
        actors.append((o["o"][0].replace("Cine", "Cap"), "Charge", o["arm"], []))
    return actors, [], SECONDS


def street_stage():
    """A paved street between house fronts, for the review only (the game's town is the set)."""
    def box(name, color, x0, x1, y0, y1, z0, z1):
        me = bpy.data.meshes.new(name)
        v = [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0), (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)]
        me.from_pydata(v, [], [(4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)])
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*color, 1)
        mat.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.9
        me.materials.append(mat)
        o = bpy.data.objects.new(name, me)
        bpy.context.scene.collection.objects.link(o)
    # a cobbled street (dark, so it never reads as the bridge), timber-framed fronts with red roofs, some smoke-dark
    box("ChargeStreet", (0.2, 0.18, 0.16), -STREET_HALF, STREET_HALF, -12, 14, -0.02, 0.0)
    box("ChargeGround", (0.12, 0.22, 0.08), -12, 12, -12, 14, -0.03, -0.02)
    for k in (-1, 1):
        for i in range(7):
            y0 = -8 + i * 3.0
            h = 1.2 + 0.3 * (i % 3)
            x0, x1 = sorted((k * (STREET_HALF + 0.2), k * (STREET_HALF + 2.2)))
            box(f"ChargeHouse{k}{i}", (0.55, 0.45, 0.33) if i % 2 else (0.42, 0.33, 0.25), x0, x1, y0, y0 + 2.6, 0.0, h)
            box(f"ChargeRoof{k}{i}", (0.45, 0.1, 0.07) if i % 3 else (0.16, 0.13, 0.12), x0 - 0.1, x1 + 0.1,
                y0 - 0.1, y0 + 2.7, h, h + 0.35)
    for o in bpy.context.scene.objects:
        if o.name in ("ArdanGround", "ArdanPath") or o.name.startswith("ArdanTuft"):
            o.hide_render = True


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = argv[1]
    what = argv[2] if len(argv) > 2 else "all"
    os.makedirs(out, exist_ok=True)
    actors, props, total = build()
    aa.stage(gt.keep_of(actors, props))
    street_stage()
    shots = (("street", (0.0, 2.5, 0.4), (1.0, 7.5, 1.6), 30),         # down the street at the charge
             ("cut", (0.2, 2.3, 0.5), (0.95, -0.9, 0.35), 18),            # close, from his sword side: the King cuts the first orc
             ("lance", (0.45, 4.9, 0.45), (0.7, -1.3, 0.4), 18))          # close: a lance throws the champion
    for key, at, off, lens in shots:
        if what in (key, "all"):
            ea.shoot(out, "charge_" + key, total, at, off, lens)


if __name__ == "__main__":
    main()
