"""Mission 2's last scene «Подкрепление», group 3: the last stand on the square (the user, 2026-11-16: «в центре
города на площади капитан, ардан и пару латников дерутся из последних сил»). Never saves.

    blender --background <Buildings-Size1-Models.blend> --python last_stand_anim.py -- render <out dir> [wide|close|all]

Four back to back in the middle -- Ардан (his sword, ardan_sword's strikes), the captain (a sword too) and two
men-at-arms (the lookout's model, the approved stand-in guard, with a sword) -- in a ring of six orcs. The orcs go
in by turns: Ардан cuts one down, a man-at-arms is run through and falls on his face, the champion's smash beats the
captain to one knee and he gets up again, Ардан's backhand drops the orc going for him. Then the ring closes, weapons
up for the end -- and the HORN (the sound is Unity's): every head turns to the gate (-y).
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import bpy  # noqa: E402
import ardan  # noqa: E402
import ardan_anim as aa  # noqa: E402
import ardan_anim2 as a2  # noqa: E402
import ardan_sword as asw  # noqa: E402
import captain  # noqa: E402
import elder_anim as ea  # noqa: E402
import figure as fg  # noqa: E402
import gate_talk_anim as gt  # noqa: E402
import kids_anim as ka  # noqa: E402
import lookout  # noqa: E402
import orc_anim as oa  # noqa: E402
import orcs_cast as oc  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
env, ease, mix = a2.env, a2.ease, a2.mix
SECONDS = 7.4
HORN = 5.7                                   # the horn: every head turns to the gate
GATE = Vector((0.0, -12.0, 0.0))
R_IN, R_OUT = 0.33, 1.15                     # the defenders' ring, the orcs' ring


def heading_to(d):
    return math.atan2(d.x, -d.y)


def wrap(a):
    return (a + math.pi) % TAU - math.pi


# ---------------------------------------------------------------- the defenders

# (name, angle on the ring in degrees: where he stands and faces)
DEFENDERS = [("ardan", -90), ("captain", 0), ("guard1", 90), ("guard2", 180)]


def keyed(t, keys):
    """Pose between key poses [(time, pose)] eased."""
    if t <= keys[0][0]:
        return keys[0][1]
    for (t0, p0), (t1, p1) in zip(keys, keys[1:]):
        if t <= t1:
            return mix(p0, p1, ease((t - t0) / max(1e-4, t1 - t0)))
    return keys[-1][1]


def block():
    """The sword held up across, the blow caught on it."""
    return asw._pose(chest=(-6, 0, 0), head=(-8, 0, 0), arm_r=((-130, -35, 0), -70), arm_l=((-120, -20, 0), -80),
                     leg_l=(-14, 16, -2), leg_r=(12, 10, -20), drop=0.01)


def knee():
    """Beaten down to one knee, the sword point on the stones, the other hand on the knee."""
    q = asw._pose(chest=(22, 0, 0), head=(10, 0, 0), arm_r=((-30, 10, 0), -20), arm_l=((-40, -10, 0), -60),
                  leg_l=(-90, 90, 0), leg_r=(20, 95, -25), drop=0.13)
    return q


def defender_pose(name, t):
    if name == "ardan":
        q = keyed(t, [(0.0, asw.ready()), (1.0, asw.ready()), (1.18, asw.wind_up()), (1.35, asw.strike()),
                      (1.85, asw.ready()), (3.45, asw.ready()), (3.62, asw.wind_up2()), (3.85, asw.strike2()),
                      (4.35, asw.ready()), (4.7, asw.breath()), (5.6, asw.breath()), (5.9, asw.ready())])
    elif name == "captain":
        q = keyed(t, [(0.0, asw.ready()), (2.3, asw.ready()), (2.5, block()), (2.7, block()), (3.05, knee()),
                      (3.9, knee()), (4.5, asw.ready()), (5.2, block()), (5.6, block()), (5.9, asw.ready())])
    elif name == "guard1":
        q = keyed(t, [(0.0, asw.ready()), (0.4, block()), (0.7, asw.ready()), (2.8, asw.ready()), (3.0, asw.wind_up3()),
                      (3.2, asw.strike3()), (3.6, asw.ready()), (5.0, block()), (5.6, block()), (5.9, asw.ready())])
    else:                                                     # guard2: run through at 2.2, he falls on his face
        q = keyed(t, [(0.0, asw.ready()), (1.9, block()), (2.2, block())])
        if t > 2.2:
            u = ease(min(1.0, (t - 2.2) / 0.9))
            down = asw._pose(chest=(40, 0, 0), head=(20, 0, 0), arm_r=((-20, 30, 0), -10), arm_l=((-30, -30, 0), -20),
                             leg_l=(-10, 30, 0), leg_r=(0, 20, 0))
            q = mix(q, down, u)
    q["chest"][0] += 3.5 * math.sin(TAU * t / 1.1)            # heaving: «из последних сил»
    return q


def defender_root(name, ang, t):
    a = math.radians(ang)
    d = Vector((math.cos(a), math.sin(a), 0))
    h = heading_to(d)
    pos = d * R_IN
    if name == "captain":                                      # knocked back a step by the smash
        pos = pos - d * 0.12 * ease(env(t, 2.6, 2.9)) * (1 - ease(env(t, 3.9, 4.5)))
    if name == "guard2" and t > 2.2:
        u = ease(min(1.0, (t - 2.6) / 0.6)) if t > 2.6 else 0.0
        tilt = Matrix.Rotation(math.radians(85 * u), 4, 'X')   # forwards onto his face
        return Matrix.Translation(pos + d * 0.05 * u + Vector((0, 0, 0.05 * u))) @ Matrix.Rotation(h, 4, 'Z') @ tilt
    # at the horn they turn their heads (and a little the body) to the gate
    look = ease(env(t, HORN + 0.1, HORN + 0.6))
    turn = wrap(heading_to(GATE - pos) - h) * 0.35 * look
    return Matrix.Translation(pos) @ Matrix.Rotation(h + turn, 4, 'Z')


def head_to_gate(q, root, t, pos):
    look = ease(env(t, HORN + 0.1, HORN + 0.6))
    facing = root.to_euler().z
    delta = max(-1.2, min(1.2, wrap(heading_to(GATE - pos) - facing)))
    q["head"][2] += math.degrees(delta) * 0.7 * look
    q["chest"][2] += math.degrees(delta) * 0.25 * look


# ---------------------------------------------------------------- the orcs

# (kind, angle on the ring, attack: (start, blow), victim, killed at (s) or None, how)
ORCS = [
    ("CineOrcTorch", -100, (0.8, 1.25), "ardan", 1.35),
    ("CineOrcSpear", 170, (1.6, 2.2), "guard2", None),
    ("CineOrcChampion", 15, (2.2, 2.6), "captain", None),
    ("CineOrcTorch", -55, (3.2, 3.75), "captain", 3.85),
    ("CineOrcSpear", 100, (0.1, 0.45), "guard1", None),
    ("CineOrcTorch", 135, (4.6, 5.3), "guard1", None),
]


def orc_attack(kind, t, start, blow):
    """Circling in a crouch, then the attack: wind up from start, the blow at `blow`; afterwards back on guard."""
    q = oa.crouch(0.35)
    w = ease(env(t, start, blow - 0.12)) * (1 - ease(env(t, blow + 0.15, blow + 0.6)))
    hit = ease(env(t, blow - 0.12, blow)) * (1 - ease(env(t, blow + 0.15, blow + 0.6)))
    if kind == "CineOrcSpear":
        q["upper_arm.L"], q["forearm.L"] = [-35, -6, -15], [-75, 0, 0]
        q["upper_arm.R"] = [-40 - 10 * w - 45 * hit, 4, 0]           # the spear drawn back, then the thrust
        q["forearm.R"] = [-50 - 30 * w + 75 * hit, 0, 0]
        q["chest"][0] += 15 * hit
    elif kind == "CineOrcChampion":
        q["upper_arm.L"] = [-60 - 110 * w + 100 * hit, -20, 0]       # both fists up, then the smash
        q["upper_arm.R"] = [-60 - 110 * w + 100 * hit, 20, 0]
        q["forearm.L"] = q["forearm.R"] = [-50 + 20 * hit, 0, 0]
        q["chest"][0] += -10 * w + 30 * hit
    else:
        q["upper_arm.L"], q["forearm.L"] = [-130, -10, 0], [-15, 0, 0]  # the torch high
        q["upper_arm.R"] = [-60 - 100 * w + 110 * hit, 8, 0]          # the axe up, then down
        q["forearm.R"] = [-70 + 10 * w + 40 * hit, 0, 0]
        q["chest"][0] += 20 * hit
    return q


def orc_root(o, t):
    kind, ang, (start, blow), victim, killed = o
    a = math.radians(ang + 8 * math.sin(TAU * t / 3.0 + ang))      # circling, a little
    d = Vector((math.cos(a), math.sin(a), 0))
    lunge = 0.28 * ease(env(t, start, blow)) * (1 - ease(env(t, blow + 0.2, blow + 0.8)))
    close = 0.3 * ease(env(t, 4.4, 5.4)) * (1 - ease(env(t, HORN + 0.2, HORN + 0.8)) * 0.5)
    pos = d * (R_OUT - lunge - close)
    h = heading_to(-d)
    look = ease(env(t, HORN, HORN + 0.45))                        # the horn: they turn to the gate
    h += wrap(heading_to(GATE - pos) - h) * 0.6 * look
    base = Matrix.Translation(pos) @ Matrix.Rotation(h, 4, 'Z')
    if killed is not None and t >= killed:
        u = min(1.0, (t - killed) / 0.5)
        w = ease(u)
        fly = d * 0.35 * w + Vector((0, 0, 0.12 * math.sin(math.pi * u) + 0.07 * w))
        tilt = Matrix.Rotation(math.radians(-88 * w), 4, 'X')        # backwards, away from the blow
        return Matrix.Translation(fly) @ base @ tilt, u
    return base, 0.0


def orc_down(u):
    q = a2.blank()
    q["upper_arm.L"] = [-150 * u, -40 * u, 0]
    q["upper_arm.R"] = [-150 * u, 40 * u, 0]
    q["thigh.L"] = [-25 * u, 0, 8 * u]
    q["shin.L"] = [30 * u, 0, 0]
    q["head"] = [-20 * u, 0, 0]
    return q


# ---------------------------------------------------------------- build

def build():
    figs = []
    for name, ang in DEFENDERS:
        if name == "ardan":
            mesh = ardan.make(Vector((0, 0, 0)))
            arm, scale = aa.rig(mesh), ardan.SCALE
        elif name == "captain":
            mesh = captain.make()
            arm, scale = ka.rig_figure(mesh, captain.SCALE), captain.SCALE
        else:
            mesh = fg.make(f"CineGuardStand{name}", lookout.PARTS, lookout.SCALE, (0, 0, 0))
            arm, scale = ka.rig_figure(mesh, lookout.SCALE), lookout.SCALE
        blade = asw.sword(arm, mesh.data.materials[0])
        figs.append({"kind": "def", "name": name, "ang": ang, "arm": arm, "scale": scale, "extra": [blade]})
    for i, o in enumerate(ORCS):
        kind = o[0]
        mesh = fg.make(f"{kind}Stand{i}", oc.FIGURES[kind][0](), oc.FIGURES[kind][1], (0, 0, 0), orc=True)
        arm = ka.rig_figure(mesh, oc.FIGURES[kind][1], bones=oa.BONES)
        figs.append({"kind": "orc", "o": o, "arm": arm, "scale": oc.FIGURES[kind][1], "extra": []})
    for fgr in figs:
        fgr["arm"].animation_data_create()
        act = bpy.data.actions.new(f"{fgr['arm'].name}_Stand")
        act.use_fake_user = True
        fgr["arm"].animation_data.action = act
    sc = bpy.context.scene
    for f in range(int(SECONDS * FPS) + 1):
        t = f / FPS
        sc.frame_set(f + 1)
        for fgr in figs:
            arm = fgr["arm"]
            if fgr["kind"] == "def":
                m = defender_root(fgr["name"], fgr["ang"], t)
                q = defender_pose(fgr["name"], t)
                if fgr["name"] != "guard2":
                    head_to_gate(q, m, t, m.to_translation())
            else:
                m, u = orc_root(fgr["o"], t)
                killed = fgr["o"][4]
                if killed is not None and t >= killed:
                    q = orc_down(ease(u))
                else:
                    kind, _, (start, blow), _, _ = fgr["o"]
                    q = orc_attack(kind, t, start, blow)
                    look = ease(env(t, HORN, HORN + 0.45))
                    q["head"][0] -= 10 * look                       # heads up at the horn
            arm.matrix_world = m
            aa.rest(arm)
            ka.apply(arm, q, fgr["scale"])
            for pb in arm.pose.bones:
                pb.keyframe_insert("rotation_quaternion", frame=f + 1, group=pb.name)
                pb.keyframe_insert("location", frame=f + 1, group=pb.name)
            arm.keyframe_insert("location", frame=f + 1)
            arm.keyframe_insert("rotation_euler", frame=f + 1)
    actors = []
    for fgr in figs:
        if fgr["kind"] == "def":
            name = {"ardan": "CapArdan", "captain": "CapCaptain"}.get(fgr["name"], "CapGuardStand")
        else:
            name = fgr["o"][0].replace("Cine", "Cap")
        actors.append((name, "Stand", fgr["arm"], fgr["extra"]))
    return actors, [], SECONDS


def square_stage():
    """A paved square, house fronts round it, for the review only (the game's square is the set)."""
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
    box("StandSquare", (0.26, 0.24, 0.22), -6, 6, -6, 6, -0.02, 0.0)
    for i, (x0, x1, y0, y1) in enumerate(((-6, 6, 4, 6), (-6, -4, -6, 4), (4, 6, -6, 4))):
        box(f"StandHouse{i}", (0.45, 0.36, 0.27), x0, x1, y0, y1, 0.0, 1.5)
        box(f"StandRoof{i}", (0.42, 0.1, 0.07), x0 - 0.1, x1 + 0.1, y0 - 0.1, y1 + 0.1, 1.5, 1.85)
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
    square_stage()
    shots = (("wide", (0.0, 0.0, 0.3), (2.0, -2.6, 2.0), 30),           # the ring from above, the gate behind us
             ("close", (0.0, -0.2, 0.45), (0.55, -1.75, 0.45), 26))      # low, Ардан and the captain in front
    for key, at, off, lens in shots:
        if what in (key, "all"):
            ea.shoot(out, "stand_" + key, total, at, off, lens)


if __name__ == "__main__":
    main()
