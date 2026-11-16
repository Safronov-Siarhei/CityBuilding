"""Mission 2's last scene «Подкрепление», group 1: the King leads the cavalry over the causeway bridge at a gallop
(the user, 2026-11-16: «по мосту несётся король и огромное количество кавалерии»). The models approved the same day
(cavalry.py, king_ride.py). Never saves.

    blender --background <Buildings-Size1-Models.blend> --python cav_ride_anim.py -- render <out dir> [wide|low|all]

The King on his white horse ahead, sword up and pumping; behind him rows of horsemen, lances couched, each pair on
its own gallop phase so the hooves don't beat in step. Heading +y (rider_anim.ROAD). The reins: cavalry.hold_reins
solved once for a horseman and once for the King (identical figures share it).
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
import cavalry as cav  # noqa: E402
import elder_anim as ea  # noqa: E402
import figure as fg  # noqa: E402
import gate_talk_anim as gt  # noqa: E402
import horse  # noqa: E402
import kids_anim as ka  # noqa: E402
import king_ride  # noqa: E402
import rider_anim as ra  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
SECONDS = 6.0
V = 2.4                                   # the gallop's speed (rider_anim's is 2.0: this is a charge)
CYCLE = ra.STRIDE * 1.15                  # ground per gallop cycle at this speed
START_Y = -8.0
ROWS = [[0.0], [-0.42, 0.42], [-0.62, 0.0, 0.62], [-0.62, 0.0, 0.62], [-0.62, 0.0, 0.62]]   # the King, then 11
ROW_GAP = 0.95
BRIDGE_HALF = 1.1                         # the causeway's half width for the review stage


def formation():
    """[(slot index, x, y offset back, is king)]."""
    out = []
    for r, row in enumerate(ROWS):
        for x in row:
            out.append((len(out), x, -r * ROW_GAP - (0.25 if r else 0.0), r == 0))
    return out


def build():
    """[(export name, take, rig, extras)], [props], seconds."""
    rnd = random.Random(7)
    pairs = []
    for i, x, back, king in formation():
        hname = "CineKingHorse" if king else ("CineWarhorse", "CineWarhorseGrey")[i % 2]
        hm = cav.make_horse(hname, obj=f"{hname}Ride{i}")
        h_arm = ka.rig_figure(hm, horse.SCALE, bones=ra.HORSE_BONES)
        if king:
            rm, scale = king_ride.make(), king_ride.SCALE
        else:
            rm, scale = fg.make(f"CineCavalryRide{i}", cav.PARTS, cav.SCALE, (0, 0, 0)), cav.SCALE
        r_arm = ka.rig_figure(rm, scale)
        pairs.append({"h": h_arm, "r": r_arm, "king": king, "scale": scale, "x": x + rnd.uniform(-0.06, 0.06),
                      "back": back + rnd.uniform(-0.12, 0.12), "phase": rnd.uniform(0, 1), "sway": rnd.uniform(0, TAU),
                      "lance": rnd.uniform(-6, 6), "hname": hname})
    reins = {}
    sc = bpy.context.scene
    for p in pairs:
        p["h"].animation_data_create()
        p["r"].animation_data_create()
        for arm, n in ((p["h"], "Horse"), (p["r"], "Rider")):
            act = bpy.data.actions.new(f"{arm.name}_Ride")
            act.use_fake_user = True
            arm.animation_data.action = act
    for f in range(int(SECONDS * FPS) + 1):
        t = f / FPS
        sc.frame_set(f + 1)
        for p in pairs:
            d = V * t
            pos = Vector((p["x"] + 0.03 * math.sin(TAU * t / 1.7 + p["sway"]), START_Y + p["back"] + d, 0.0))
            phase = (d / CYCLE + p["phase"]) % 1.0
            h_arm, r_arm = p["h"], p["r"]
            h_arm.location = pos
            h_arm.rotation_euler = (0, 0, ra.ROAD + math.radians(2.0 * math.sin(TAU * t / 2.3 + p["sway"])))
            aa.rest(h_arm)
            ra.h_apply(h_arm, ra.gait(ra.h_blank(), phase, 1.0))
            bpy.context.view_layer.update()
            r_arm.matrix_world = gt.bone_world(h_arm, "body") @ Matrix.Translation(ra.SEAT)
            q = ra.seated(t, 1.0, 0.0)
            q["chest"][0] += 3 * math.sin(TAU * phase)                 # the rider rocks with the stride
            if p["king"]:
                pump = math.sin(TAU * t / 0.9)
                q["upper_arm.R"] = [-150 + 12 * pump, 10, 0]           # the sword up, pumped: «Вперёд!»
                q["forearm.R"] = [-20 - 10 * pump, 0, 0]
                q["head"][0] -= 6
            else:
                q["upper_arm.R"] = [-78 + p["lance"], 4, 0]             # the lance couched
                q["forearm.R"] = [-6, 0, 0]
            key = "king" if p["king"] else "cav"
            if key not in reins:
                aa.rest(r_arm)
                ka.apply(r_arm, q, p["scale"])
                cav.hold_reins(h_arm, r_arm, p["scale"], q)
                reins[key] = (list(q["upper_arm.L"]), list(q["forearm.L"]))
            q["upper_arm.L"], q["forearm.L"] = [list(v) for v in reins[key]]
            aa.rest(r_arm)
            ka.apply(r_arm, q, p["scale"])
            for arm in (h_arm, r_arm):
                for pb in arm.pose.bones:
                    pb.keyframe_insert("rotation_quaternion", frame=f + 1, group=pb.name)
                    pb.keyframe_insert("location", frame=f + 1, group=pb.name)
                arm.keyframe_insert("location", frame=f + 1)
                arm.keyframe_insert("rotation_euler", frame=f + 1)
    actors = []
    for p in pairs:
        rider_name = "CapKingRide" if p["king"] else "CapCavalry"
        horse_name = {"CineKingHorse": "CapKingHorse", "CineWarhorse": "CapWarhorse", "CineWarhorseGrey": "CapWarhorseGrey"}[p["hname"]]
        actors += [(horse_name, "Ride", p["h"], []), (rider_name, "Ride", p["r"], [])]
    return actors, [], SECONDS


def bridge_stage():
    """A stone causeway along y with low parapets over water, for the review only (the game has its own)."""
    def flat(name, color, x0, x1, y0, y1, z0, z1):
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
    flat("RideWater", (0.18, 0.42, 0.62), -14, 14, -14, 14, -0.3, -0.12)
    flat("RideDeck", (0.62, 0.6, 0.56), -BRIDGE_HALF, BRIDGE_HALF, -14, 14, -0.4, 0.0)
    for k in (-1, 1):
        flat(f"RideParapet{k}", (0.52, 0.5, 0.47), k * BRIDGE_HALF - 0.08, k * BRIDGE_HALF + 0.08, -14, 14, 0.0, 0.18)
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
    bridge_stage()
    shots = (("wide", (0.0, -1.0, 0.4), (4.2, 3.2, 1.6), 30),          # from the bank ahead: they come at us
             ("low", (0.0, -2.0, 0.5), (1.35, 2.2, 0.35), 24))           # low by the parapet, the King thunders past
    for key, at, off, lens in shots:
        if what in (key, "all"):
            ea.shoot(out, "ride_" + key, total, at, off, lens)


if __name__ == "__main__":
    main()
