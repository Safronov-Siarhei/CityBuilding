"""Mission 2's last scene «Подкрепление», groups 4 and 5 (the user, 2026-11-16: «орки уничтожают здания по всему
городу ... после этого нам показывают как оставшиеся орки убегают из города»). Never saves.

    blender --background <Buildings-Size1-Models.blend> --python orc_raze_anim.py -- render <out dir> [raze|flee|all]

  raze  4 orcs at a row of houses: two take turns hacking a door with their axes, the champion beats a wall with both
        fists (stone chunks fly at each blow), the torchbearer holds his torch up under the thatch's eave (the fire is
        Unity's). 5 s, a loop in spirit: the blows keep their beat.
  flee  6 orcs run for the town gate (+y) in a panic, looking back over their shoulders; one stumbles, sprawls and
        scrambles on; two horsemen (cavalry.py) gallop after them to the gate. 6 s.
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
import cav_ride_anim as cr  # noqa: E402
import cavalry as cav  # noqa: E402
import elder_anim as ea  # noqa: E402
import figure as fg  # noqa: E402
import gate_talk_anim as gt  # noqa: E402
import horse  # noqa: E402
import kids_anim as ka  # noqa: E402
import last_stand_anim as ls  # noqa: E402
import orc_anim as oa  # noqa: E402
import orcs_cast as oc  # noqa: E402
import rider_anim as ra  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
env, ease, mix = a2.env, a2.ease, a2.mix
WALL_Y = 1.0                                  # the houses' fronts (raze), facing -y


def orc(kind, tag):
    mesh = fg.make(f"{kind}{tag}", oc.FIGURES[kind][0](), oc.FIGURES[kind][1], (0, 0, 0), orc=True)
    return ka.rig_figure(mesh, oc.FIGURES[kind][1], bones=oa.BONES), oc.FIGURES[kind][1]


def start_action(arm, name):
    arm.animation_data_create()
    act = bpy.data.actions.new(f"{arm.name}_{name}")
    act.use_fake_user = True
    arm.animation_data.action = act


def key(arm, f):
    for pb in arm.pose.bones:
        pb.keyframe_insert("rotation_quaternion", frame=f, group=pb.name)
        pb.keyframe_insert("location", frame=f, group=pb.name)
    arm.keyframe_insert("location", frame=f)
    arm.keyframe_insert("rotation_euler", frame=f)


# ---------------------------------------------------------------- 4: razing

RAZE_SECONDS = 5.0
# (kind, x, beat period, beat offset, what)
RAZERS = [("CineOrcTorch", -0.25, 0.9, 0.0, "hack"), ("CineOrcSpear", 0.3, 0.9, 0.45, "hack"),
          ("CineOrcChampion", 2.0, 1.2, 0.2, "smash"), ("CineOrcTorch", -2.1, 0.0, 0.0, "burn")]


def beat(t, period, off):
    """The current blow's (start, blow) times for a steady beat."""
    n = math.floor((t - off) / period)
    start = off + n * period
    return start, start + period * 0.7


def raze_pose(kind, what, t, period, off):
    if what == "burn":
        q = oa.crouch(0.15)
        q["upper_arm.L"] = [-165, -15, 0]                 # the torch up under the eave
        q["forearm.L"] = [-10 + 8 * math.sin(TAU * t / 0.7), 0, 0]
        q["upper_arm.R"] = [-30, 20, 0]
        q["forearm.R"] = [-60, 0, 0]
        q["head"][0] -= 25
        q["chest"][0] -= 10
        return q
    start, blow = beat(t, period, off)
    if what == "hack" and kind == "CineOrcSpear":
        kind = "CineOrcTorch"                              # an axe-blow for both door-hackers
    q = ls.orc_attack(kind, t, start, blow)
    if kind == "CineOrcTorch" and what == "hack":
        q["upper_arm.L"], q["forearm.L"] = [-40, -20, 0], [-60, 0, 0]
    return q


def chunk_mesh(name):
    b = Builder()
    b.box(-0.05, 0.05, -0.04, 0.04, -0.035, 0.035, 'stone_mid', bevel=0.01)
    me = b.to_mesh(name)
    me.materials.append(fg.material())
    o = bpy.data.objects.new(name, me)
    fg.ensure_collection().objects.link(o)
    return o


def build_raze():
    figs = []
    for i, (kind, x, period, off, what) in enumerate(RAZERS):
        arm, scale = orc(kind, f"Raze{i}")
        start_action(arm, "Raze")
        figs.append({"arm": arm, "scale": scale, "kind": kind, "x": x, "period": period, "off": off, "what": what})
    # the champion's blows knock stones out of the wall: 4 chunks a blow
    champ = next(f for f in figs if f["what"] == "smash")
    rnd = random.Random(5)
    chunks = []
    n = 0
    t_blow = champ["off"] + champ["period"] * 0.7
    while t_blow < RAZE_SECONDS:
        for _ in range(4):
            c = chunk_mesh(f"CineRazeChunk{n}")
            n += 1
            v = Vector((rnd.uniform(-0.8, 0.8), rnd.uniform(-1.6, -0.8), rnd.uniform(0.6, 1.4)))
            chunks.append((c, t_blow, Vector((champ["x"] + rnd.uniform(-0.15, 0.15), WALL_Y - 0.02, rnd.uniform(0.25, 0.55))),
                           v, Vector((rnd.uniform(-9, 9), rnd.uniform(-9, 9), rnd.uniform(-9, 9)))))
        t_blow += champ["period"]
    sc = bpy.context.scene
    for f in range(int(RAZE_SECONDS * FPS) + 1):
        t = f / FPS
        sc.frame_set(f + 1)
        for fr in figs:
            arm = fr["arm"]
            near = 0.45 if fr["what"] != "burn" else 0.5
            arm.matrix_world = Matrix.Translation((fr["x"], WALL_Y - near, 0)) @ Matrix.Rotation(math.pi, 4, 'Z')
            aa.rest(arm)
            ka.apply(arm, raze_pose(fr["kind"], fr["what"], t, fr["period"], fr["off"]), fr["scale"])
            key(arm, f + 1)
        for c, t0, p0, v, spin in chunks:
            if t < t0:
                c.location = p0
                c.hide_render = True
            else:
                u = t - t0
                p = p0 + v * u + Vector((0, 0, -4.9 * u * u))
                if p.z < 0.035:                                  # landed: it stays where it fell
                    tl = (v.z + math.sqrt(v.z * v.z + 4 * 4.9 * (p0.z - 0.035))) / (2 * 4.9)
                    p = p0 + v * tl + Vector((0, 0, -4.9 * tl * tl))
                    p.z = 0.035
                    u = tl
                c.location = p
                c.rotation_euler = spin * u
                c.hide_render = False
            c.keyframe_insert("location", frame=f + 1)
            c.keyframe_insert("rotation_euler", frame=f + 1)
            c.keyframe_insert("hide_render", frame=f + 1)
    actors = [(fr["kind"].replace("Cine", "Cap"), "Raze", fr["arm"], []) for fr in figs]
    return actors, [c[0] for c in chunks], RAZE_SECONDS


# ---------------------------------------------------------------- 5: the flight

FLEE_SECONDS = 6.0
RUN_V = a2.RUN_SPEED * 1.25
LANCE_REACH = 1.3
GATE_Y = 9.0
# (kind, x, y start, look-back phase, stumbles)
FLEERS = [("CineOrcSpear", -0.3, 0.0, 0.0, False), ("CineOrcTorch", 0.35, -0.5, 1.3, True),
          ("CineOrcChampion", 0.0, -1.3, 2.1, False), ("CineOrcTorch", -0.55, -1.9, 0.7, False),
          ("CineOrcSpear", 0.5, -2.4, 2.9, False), ("CineOrcTorch", -0.1, -3.0, 1.9, False)]
STUMBLE, UP_AGAIN = 2.0, 3.0


def panic_run(kind, phase, t, look):
    q = oa.brute_run(phase)
    s = math.sin(TAU * phase)
    if kind == "CineOrcSpear":
        q["upper_arm.R"] = [-20 + 40 * s, 10, 0]                     # the spear trailed, the shield flung about
        q["upper_arm.L"] = [-20 - 40 * s, -15, 0]
    elif kind == "CineOrcTorch":
        q["upper_arm.L"] = [-100 + 20 * s, -15, 0]
        q["upper_arm.R"] = [-30 + 50 * s, 10, 0]
    q["chest"][0] += 10
    q["head"][2] += 70 * look                                         # a glance back over the shoulder
    q["chest"][2] += 20 * look
    return q


def build_flee():
    figs = []
    for i, (kind, x, y0, lp, stumble) in enumerate(FLEERS):
        arm, scale = orc(kind, f"Flee{i}")
        start_action(arm, "Flee")
        figs.append({"arm": arm, "scale": scale, "kind": kind, "x": x, "y0": y0, "lp": lp, "stumble": stumble})
    riders = []
    for i, x in enumerate((-0.35, 0.4)):
        hname = ("CineWarhorse", "CineWarhorseGrey")[i]
        h_arm = ka.rig_figure(cav.make_horse(hname, obj=f"{hname}Chase{i}"), horse.SCALE, bones=ra.HORSE_BONES)
        r_arm = ka.rig_figure(fg.make(f"CineCavalryChase{i}", cav.PARTS, cav.SCALE, (0, 0, 0)), cav.SCALE)
        start_action(h_arm, "Chase")
        start_action(r_arm, "Chase")
        riders.append({"h": h_arm, "r": r_arm, "hname": hname, "x": x, "back": -5.2 - 0.8 * i, "phase": 0.3 * i,
                       "dist": 0.0, "struck": None})
    reins = None
    sc = bpy.context.scene
    dt = 1.0 / FPS
    for fr in figs:
        fr["hit"] = None
    for f in range(int(FLEE_SECONDS * FPS) + 1):
        t = f / FPS
        sc.frame_set(f + 1)
        # the riders gallop on until their blow, then rein back to a canter: the rest get away through the gate
        # (the user, 2026-11-16: «кавалерия проносится по оркам, но орки продолжают бежать» -- now whoever a rider
        # reaches is ridden down, nobody is passed through)
        for p in riders:
            v = cr.V * 1.05
            if p["struck"] is not None:
                v = cr.V * 1.05 + (1.1 - cr.V * 1.05) * ease(min(1.0, (t - p["struck"]) / 1.0))
            if f:
                p["dist"] += v * dt
            ry = p["back"] + p["dist"]
            for fr in figs:
                if fr["hit"] is not None:
                    continue
                oy = fr["y0"] + fr.get("d", 0.0)
                if abs(fr["x"] - p["x"]) < 0.4 and ry + LANCE_REACH >= oy and ry < oy + 0.3:
                    fr["hit"] = t
                    fr["hit_y"] = oy
                    if p["struck"] is None:
                        p["struck"] = t
        for fr in figs:
            arm = fr["arm"]
            if fr["hit"] is not None:
                # ridden down from behind: thrown forwards onto his face, sliding a little
                u = min(1.0, (t - fr["hit"]) / 0.5)
                w = ease(u)
                m = (Matrix.Translation((fr["x"], fr["hit_y"] + 0.7 * w, 0.1 * math.sin(math.pi * u) + 0.06 * w))
                     @ Matrix.Rotation(math.pi, 4, 'Z') @ Matrix.Rotation(math.radians(86 * w), 4, 'X'))
                arm.matrix_world = m
                aa.rest(arm)
                ka.apply(arm, ls.orc_down(w), fr["scale"])
                key(arm, f + 1)
                continue
            d = RUN_V * t
            if fr["stumble"]:
                # the sprawl: down at STUMBLE, up again by UP_AGAIN, slower after
                lost = 0.0 if t < STUMBLE else min(t - STUMBLE, UP_AGAIN - STUMBLE) * RUN_V * 0.85
                d -= lost
            fr["d"] = d
            y = fr["y0"] + d
            look = max(0.0, math.sin(TAU * (t + fr["lp"]) / 2.6)) ** 3
            q = panic_run(fr["kind"], (d / (a2.RUN_SPEED * a2.RUN_CYCLE)) % 1.0, t, look)
            m = Matrix.Translation((fr["x"], y, 0)) @ Matrix.Rotation(math.pi, 4, 'Z')
            if fr["stumble"] and STUMBLE <= t < UP_AGAIN + 0.3:
                down = ease(env(t, STUMBLE, STUMBLE + 0.3)) * (1 - ease(env(t, UP_AGAIN - 0.5, UP_AGAIN + 0.2)))
                m = m @ Matrix.Translation((0, 0, 0.06 * down)) @ Matrix.Rotation(math.radians(78 * down), 4, 'X')
                sprawl = a2.blank()
                sprawl["upper_arm.L"] = [-120, -30, 0]
                sprawl["upper_arm.R"] = [-120, 30, 0]
                sprawl["thigh.L"] = [20, 0, 0]
                sprawl["head"] = [-30, 0, 0]
                q = mix(q, sprawl, down)
            arm.matrix_world = m
            aa.rest(arm)
            ka.apply(arm, q, fr["scale"])
            key(arm, f + 1)
        for p in riders:
            h_arm, r_arm = p["h"], p["r"]
            dist = p["dist"]
            h_arm.location = (p["x"], p["back"] + dist, 0)
            h_arm.rotation_euler = (0, 0, ra.ROAD)
            aa.rest(h_arm)
            ra.h_apply(h_arm, cr.legs_only((dist / cr.CYCLE + p["phase"]) % 1.0))
            bpy.context.view_layer.update()
            r_arm.matrix_world = gt.bone_world(h_arm, "body") @ Matrix.Translation(ra.SEAT)
            q = ra.seated(t, 1.0, 0.0)
            jolt = 0.0 if p["struck"] is None else env(t, p["struck"] - 0.15, p["struck"]) * (1 - env(t, p["struck"], p["struck"] + 0.35))
            q["upper_arm.R"] = [-78 + 10 * jolt, 4, 0]
            q["forearm.R"] = [-6, 0, 0]
            if reins is None:
                aa.rest(r_arm)
                ka.apply(r_arm, q, cav.SCALE)
                cav.hold_reins(h_arm, r_arm, cav.SCALE, q)
                reins = (list(q["upper_arm.L"]), list(q["forearm.L"]))
            q["upper_arm.L"], q["forearm.L"] = [list(v) for v in reins]
            aa.rest(r_arm)
            ka.apply(r_arm, q, cav.SCALE)
            key(h_arm, f + 1)
            key(r_arm, f + 1)
    names = {"CineWarhorse": "CapWarhorse", "CineWarhorseGrey": "CapWarhorseGrey"}
    actors = [(fr["kind"].replace("Cine", "Cap"), "Flee", fr["arm"], []) for fr in figs]
    for p in riders:
        actors += [(names[p["hname"]], "Chase", p["h"], []), ("CapCavalry", "Chase", p["r"], [])]
    return actors, [], FLEE_SECONDS


# ---------------------------------------------------------------- review stages

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


def hide_default_ground():
    for o in bpy.context.scene.objects:
        if o.name in ("ArdanGround", "ArdanPath") or o.name.startswith("ArdanTuft"):
            o.hide_render = True


def raze_stage():
    box("RazeStreet", (0.2, 0.18, 0.16), -8, 8, -6, WALL_Y, -0.02, 0.0)
    for i, x0 in enumerate((-3.2, -1.0, 1.2)):
        box(f"RazeHouse{i}", (0.55, 0.45, 0.33) if i % 2 else (0.45, 0.36, 0.27), x0, x0 + 2.0, WALL_Y, WALL_Y + 2.0, 0.0, 1.3)
        box(f"RazeRoof{i}", (0.62, 0.5, 0.22), x0 - 0.15, x0 + 2.15, WALL_Y - 0.25, WALL_Y + 2.1, 1.3, 1.7)  # thatch
    box("RazeDoor", (0.3, 0.17, 0.08), -0.25, 0.25, WALL_Y - 0.03, WALL_Y, 0.0, 0.65)
    hide_default_ground()


def flee_stage():
    box("FleeStreet", (0.2, 0.18, 0.16), -1.3, 1.3, -8, GATE_Y + 6, -0.02, 0.0)
    box("FleeGround", (0.12, 0.22, 0.08), -12, 12, -8, GATE_Y + 12, -0.03, -0.02)
    for k in (-1, 1):
        for i in range(5):
            y0 = -7 + i * 3.0
            x0, x1 = sorted((k * 1.5, k * 3.5))
            box(f"FleeHouse{k}{i}", (0.5, 0.4, 0.3), x0, x1, y0, y0 + 2.6, 0.0, 1.3)
            box(f"FleeRoof{k}{i}", (0.45, 0.1, 0.07), x0 - 0.1, x1 + 0.1, y0 - 0.1, y0 + 2.7, 1.3, 1.65)
        x0, x1 = sorted((k * 1.0, k * 6.0))
        box(f"FleeWall{k}", (0.5, 0.5, 0.48), x0, x1, GATE_Y, GATE_Y + 0.6, 0.0, 1.6)        # the town wall, the gate gap
        box(f"FleeTower{k}", (0.55, 0.55, 0.52), k * 1.0 - 0.5, k * 1.0 + 0.5, GATE_Y - 0.2, GATE_Y + 0.8, 0.0, 2.3)
    hide_default_ground()


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = argv[1]
    what = argv[2] if len(argv) > 2 else "all"
    os.makedirs(out, exist_ok=True)
    if what in ("raze", "all"):
        actors, props, total = build_raze()
        aa.stage(gt.keep_of(actors, props))
        raze_stage()
        ea.shoot(out, "raze_wide", total, (0.0, WALL_Y, 0.5), (1.4, -4.6, 1.4), 26)
    if what == "flee":
        actors, props, total = build_flee()
        aa.stage(gt.keep_of(actors, props))
        flee_stage()
        for key_, at, off, lens in (("flee_back", (0.0, 3.0, 0.4), (0.8, -7.5, 1.8), 28),   # behind them: to the gate
                                    ("flee_front", (0.0, 1.0, 0.4), (0.9, 6.2, 0.9), 24)):  # from the gate: faces of fear
            ea.shoot(out, key_, total, at, off, lens)


if __name__ == "__main__":
    main()
