"""Exports Ардан WITH THE SWORD (approved 2026-11-12, «Утверждаю, ставь») for campaign mission 2, where he
replaces the Латник. Never saves the .blend.

    blender --background <Buildings-Size1-Models.blend> --python export_ardan_sword.py -- <unity project>

Writes Assets/_Project/Models/Cinematic/Cast/CineArdanSword.fbx (the figure, the sword on his right
forearm, every clip a take) and adds/replaces the CineArdanSword entry in Campaign/OpeningCast.json,
which gives each clip's length and looping (read by the importer and by CineFigure).

The clips go between ardan_sword.py's approved poses:
  Idle   -- the ready stance, breathing (loop);
  Run    -- the run's legs, the sword held ready (loop, a2.RUN_CYCLE);
  Attack -- ready -> wind-up -> strike -> ready; Attack2 the backhand, Attack3 the overhead (in turn in the game);
  Whirl  -- «Круговой удар»: the whirl pose spun round once;
  Cry    -- «Боевой клич»: the sword up, shouting;
  Dash   -- «Натиск»: the lunge;
  Breath -- «Второе дыхание»: the hand on the chest.
"""
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import bpy  # noqa: E402

for _o in [o for o in bpy.data.objects if o.name.startswith("Cine")]:
    bpy.data.objects.remove(_o, do_unlink=True)
for _a in list(bpy.data.actions):
    bpy.data.actions.remove(_a)

import ardan  # noqa: E402
import ardan_anim as aa  # noqa: E402
import ardan_anim2 as a2  # noqa: E402
import ardan_sword as sw  # noqa: E402
import kids_anim as ka  # noqa: E402
from mathutils import Vector  # noqa: E402

FPS = 30
TAU = 2 * math.pi
NAME = "CineArdanSword"


def ease(x):
    x = min(1.0, max(0.0, x))
    return x * x * (3 - 2 * x)


def breathe(q, t, amount=1.0):
    br = math.sin(TAU * t / 3.2)
    q["chest"][0] += 1.5 * br * amount
    q["head"][0] += -1.5 + 1.2 * br * amount
    return q


def through(t, keys):
    """Pose at time t along keys [(time, pose_fn)], eased between neighbours."""
    if t <= keys[0][0]:
        return keys[0][1]()
    for (t0, a), (t1, b) in zip(keys, keys[1:]):
        if t <= t1:
            return a2.mix(a(), b(), ease((t - t0) / max(1e-6, t1 - t0)))
    return keys[-1][1]()


def idle(t):
    return breathe(sw.ready(), t)


def run(phase):
    q = a2.run(phase)
    q["upper_arm.R"] = [-35, 10, 0]
    q["forearm.R"] = [-60, 0, 0]
    return q


def attack(t):
    return through(t, [(0.0, sw.ready), (0.2, sw.wind_up), (0.32, sw.strike), (0.55, sw.ready)])


def attack2(t):
    """The backhand (approved 2026-11-12 with the three-strike sheet)."""
    return through(t, [(0.0, sw.ready), (0.2, sw.wind_up2), (0.32, sw.strike2), (0.55, sw.ready)])


def attack3(t):
    """The overhead blow, a touch slower."""
    return through(t, [(0.0, sw.ready), (0.24, sw.wind_up3), (0.36, sw.strike3), (0.6, sw.ready)])


def whirl(t, seconds=0.6):
    spin = sw.whirl()
    spin["hips"][2] += 360 * ease(t / seconds)
    w = min(ease(t / 0.08), ease((seconds - t) / 0.08))
    return a2.mix(sw.ready(), spin, w) if w < 1 else spin


def held(pose, seconds, rise, fall, wobble=None):
    def fn(t):
        w = min(ease(t / rise), ease((seconds - t) / fall))
        p = pose()
        if wobble:
            wobble(p, t)
        return a2.mix(sw.ready(), p, w)
    return fn


def cry_shake(p, t):
    p["upper_arm.R"][0] += 6 * math.sin(TAU * t * 5)
    p["head"][0] += 3 * math.sin(TAU * t * 7)


def breath_rise(p, t):
    p["chest"][0] += 4 * math.sin(TAU * t / 1.2)


CLIPS = {
    "Idle": dict(fn=idle, seconds=3.2, loop=True),
    "Run": dict(fn=lambda t: run((t / a2.RUN_CYCLE) % 1.0), seconds=a2.RUN_CYCLE, loop=True),
    "Attack": dict(fn=attack, seconds=0.55, loop=False),
    "Attack2": dict(fn=attack2, seconds=0.55, loop=False),
    "Attack3": dict(fn=attack3, seconds=0.6, loop=False),
    "Whirl": dict(fn=whirl, seconds=0.6, loop=False),
    "Cry": dict(fn=held(sw.cry, 1.0, 0.2, 0.2, cry_shake), seconds=1.0, loop=False),
    "Dash": dict(fn=held(sw.dash, 0.45, 0.12, 0.15), seconds=0.45, loop=False),
    "Breath": dict(fn=held(sw.breath, 1.2, 0.25, 0.25, breath_rise), seconds=1.2, loop=False),
}


def bake(arm, name, spec):
    arm.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    frames = max(2, round(spec["seconds"] * FPS))
    for f in range(frames + 1):
        t = f / FPS if not spec["loop"] else spec["seconds"] * f / frames
        t = min(t, spec["seconds"])
        aa.rest(arm)
        ka.apply(arm, spec["fn"](t), ardan.SCALE)
        for pb in arm.pose.bones:
            pb.keyframe_insert("rotation_quaternion", frame=f, group=pb.name)
            pb.keyframe_insert("location", frame=f, group=pb.name)
    return act


def export(arm, meshes, folder):
    arm.location = (0, 0, 0)
    arm.rotation_euler = (0, 0, 0)
    arm.animation_data.action = None
    aa.rest(arm)
    bpy.ops.object.select_all(action='DESELECT')
    arm.select_set(True)
    for m in meshes:
        m.select_set(True)
    bpy.context.view_layer.objects.active = arm
    path = os.path.join(folder, NAME + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={'ARMATURE', 'MESH'},
        axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
        bake_space_transform=False, use_mesh_modifiers=False, mesh_smooth_type='FACE',
        add_leaf_bones=False, primary_bone_axis='Y', secondary_bone_axis='X',
        use_armature_deform_only=False, armature_nodetype='NULL',
        bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=True, bake_anim_force_startend_keying=True,
        bake_anim_step=1.0, bake_anim_simplify_factor=0.0,
        path_mode='STRIP', embed_textures=False, colors_type='LINEAR', prioritize_active_color=True)
    print("EXPORTED", path)


def main():
    project = sys.argv[sys.argv.index("--") + 1:][0]
    bpy.context.scene.render.fps = FPS
    bpy.context.scene.render.fps_base = 1.0
    folder = os.path.join(project, "Assets", "_Project", "Models", "Cinematic", "Cast")
    mesh = ardan.make(Vector((0, 0, 0)))
    arm = aa.rig(mesh)
    blade = sw.sword(arm, mesh.data.materials[0])
    for cname, spec in CLIPS.items():
        bake(arm, cname, spec)
    meshes = [c for c in arm.children if c.type == 'MESH'] + ([blade] if blade not in arm.children else [])
    export(arm, meshes, folder)

    path = os.path.join(project, "Assets", "_Project", "Campaign", "OpeningCast.json")
    with open(path, encoding="utf-8") as f:
        data = json.load(f)
    entry = {
        "name": NAME,
        "walkSpeed": round(0.42 * ardan.SCALE, 4),
        "runSpeed": round(0.95 * ardan.SCALE, 4),
        "clips": [{"name": c, "seconds": round(s["seconds"], 4), "loop": s["loop"]} for c, s in CLIPS.items()],
    }
    data["figures"] = [x for x in data["figures"] if x["name"] != NAME] + [entry]
    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=1)
    print("EXPORT ARDAN SWORD DONE", len(CLIPS), "clips")


main()
