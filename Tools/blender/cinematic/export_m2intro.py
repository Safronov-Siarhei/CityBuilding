"""Exports mission 2's opening cutscene, scene 1 «У ворот» (all four animation groups approved 2026-11-15), for
Unity. Never saves the .blend.

    blender --background <Buildings-Size1-Models.blend> --python export_m2intro.py -- <unity project> <group>

<group> is gate | lookout | rider | alarm (one Blender run each: the lookout's model is built two ways). It runs
that group's approved bake (gate_talk_anim / lookout_anim / rider_anim / alarm_anim .build()) and writes:

  * Models/Cinematic/Cast/<M2Figure>.fbx -- each figure with its take IN PLACE (bones only; Ардан also gets
    a Run loop for his run-up), baked into CineClips by setup like the opening's cast;
  * Models/Cinematic/M2Intro<Prop>.fbx -- the props (the captain's sword, the horn, the guards' spears);
  * Campaign/M2Intro/<group>.json -- per frame, every figure's root and every prop as a matrix in Unity axes
    (x, z, y + quaternion, the opening's unity_m), in the group's own Blender frame; CityDefenceMode.Intro
    places the frame at the north gate;
  * the figures' entries in Campaign/OpeningCast.json (clip lengths and loops).
"""
import json
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

import ardan_anim as aa  # noqa: E402
import ardan_anim2 as a2  # noqa: E402
from mathutils import Matrix  # noqa: E402

FPS = aa.FPS
# export_cast.py / export_cine.py run their whole export on import: their conversion and export settings copied
P = Matrix(((1, 0, 0), (0, 0, 1), (0, 1, 0)))


def unity_m(m):
    loc, rot, _ = m.decompose()
    q = (P @ rot.to_matrix() @ P).to_quaternion()
    return [round(loc.x, 4), round(loc.z, 4), round(loc.y, 4), round(q.x, 5), round(q.y, 5), round(q.z, 5), round(q.w, 5)]


def export(name, arm, meshes, folder):
    """A figure: as export_cast.export (generic rig, -Z forward, every action a take)."""
    arm.location = (0, 0, 0)
    arm.rotation_euler = (0, 0, 0)
    arm.animation_data.action = None
    aa.rest(arm)
    bpy.ops.object.select_all(action='DESELECT')
    arm.select_set(True)
    for m in meshes:
        m.select_set(True)
    bpy.context.view_layer.objects.active = arm
    path = os.path.join(folder, name + ".fbx")
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


def export_piece(o, folder):
    """A prop: as export_cine.export (a static mesh in its own space, the space baked)."""
    o.parent = None
    o.matrix_world = Matrix.Identity(4)
    bpy.ops.object.select_all(action='DESELECT')
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    path = os.path.join(folder, o.name + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={'MESH'},
        axis_forward='Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
        bake_space_transform=True, use_mesh_modifiers=True, mesh_smooth_type='FACE',
        add_leaf_bones=False, bake_anim=False, path_mode='STRIP', embed_textures=False,
        colors_type='LINEAR', prioritize_active_color=True)
    print("EXPORTED", path)


def group_module(name):
    if name == "gate":
        import gate_talk_anim as m
    elif name == "lookout":
        import lookout_anim as m
    elif name == "rider":
        import rider_anim as m
    elif name == "alarm":
        import alarm_anim as m
    else:
        raise SystemExit("group must be gate | lookout | rider | alarm")
    return m


def sample(actors, props, frames):
    """Per frame: each rig's root (Unity matrix) and its bones' values, each prop's matrix."""
    sc = bpy.context.scene
    roots = {id(a[2]): [] for a in actors}
    bones = {id(a[2]): [] for a in actors}
    mats = {p.name: [] for p in props}
    for f in range(frames):
        sc.frame_set(f + 1)
        for _, _, arm, _ in actors:
            roots[id(arm)] += unity_m(arm.matrix_world)
            bones[id(arm)].append({pb.name: (tuple(pb.rotation_quaternion), tuple(pb.location)) for pb in arm.pose.bones})
        for p in props:
            mats[p.name] += unity_m(p.matrix_world)
    return roots, bones, mats


def take(arm, name, poses):
    """An in-place action on `arm` from sampled bone values."""
    arm.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    arm.location = (0, 0, 0)
    arm.rotation_euler = (0, 0, 0)
    for f, pose in enumerate(poses):
        for pb in arm.pose.bones:
            q, loc = pose[pb.name]
            pb.rotation_mode = 'QUATERNION'
            pb.rotation_quaternion = q
            pb.location = loc
            pb.keyframe_insert("rotation_quaternion", frame=f, group=pb.name)
            pb.keyframe_insert("location", frame=f, group=pb.name)
    return act


def run_take(arm, scale):
    """Ардан's run-up to the gate: the run cycle, looping."""
    arm.animation_data_create()
    act = bpy.data.actions.new("Run")
    act.use_fake_user = True
    arm.animation_data.action = act
    import kids_anim as ka
    frames = max(2, round(a2.RUN_CYCLE * FPS))
    for f in range(frames + 1):
        arm.location = (0, 0, 0)
        arm.rotation_euler = (0, 0, 0)
        aa.rest(arm)
        ka.apply(arm, a2.run(f / frames), scale)
        for pb in arm.pose.bones:
            pb.keyframe_insert("rotation_quaternion", frame=f, group=pb.name)
            pb.keyframe_insert("location", frame=f, group=pb.name)
    return act


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    project, group = argv[0], argv[1]
    sc = bpy.context.scene
    sc.render.fps = FPS
    sc.render.fps_base = 1.0
    mod = group_module(group)
    actors, props, seconds = mod.build()
    frames = int(seconds * FPS) + 1
    roots, bones, mats = sample(actors, props, frames)
    for a_ in list(bpy.data.actions):
        bpy.data.actions.remove(a_)

    cast_dir = os.path.join(project, "Assets", "_Project", "Models", "Cinematic", "Cast")
    model_dir = os.path.join(project, "Assets", "_Project", "Models", "Cinematic")
    data_dir = os.path.join(project, "Assets", "_Project", "Campaign", "M2Intro")
    os.makedirs(data_dir, exist_ok=True)
    out = {"group": group, "fps": FPS, "frames": frames, "seconds": round(seconds, 4), "figures": [], "props": []}
    entries = []
    by_export = {}
    for exp, tk, arm, extra in actors:
        by_export.setdefault(exp, []).append((tk, arm, extra))
        out["figures"].append({"figure": exp, "take": tk, "root": roots[id(arm)]})
    for exp, items in by_export.items():
        arm0 = items[0][1]
        acts, clips = [], []
        for tk, arm, _ in items:
            acts.append(take(arm0, tk, bones[id(arm)]))
            clips.append({"name": tk, "seconds": round(seconds, 4), "loop": False})
        if exp == "M2Ardan":
            acts.append(run_take(arm0, 1.0))
            clips.append({"name": "Run", "seconds": round(a2.RUN_CYCLE, 4), "loop": True})
        meshes = [c for c in arm0.children if c.type == 'MESH' and c not in props] + items[0][2]
        export(exp, arm0, meshes, cast_dir)
        for a_ in acts:
            bpy.data.actions.remove(a_)
        entries.append({"name": exp, "walkSpeed": 0.42, "runSpeed": round(a2.RUN_SPEED, 4), "clips": clips})
    for p in props:
        track = mats[p.name]
        p.animation_data_clear()
        p.modifiers.clear()
        p.vertex_groups.clear()
        name = "M2Intro" + p.name.replace("Cine", "").rstrip("0123456789")   # the guards' spears share one model
        if not any(x["piece"] == name for x in out["props"]):
            p.name = name
            export_piece(p, model_dir)
        out["props"].append({"piece": name, "m": track})
    with open(os.path.join(data_dir, group + ".json"), "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, separators=(",", ":"))

    cast_path = os.path.join(project, "Assets", "_Project", "Campaign", "OpeningCast.json")
    with open(cast_path, encoding="utf-8") as f:
        cast = json.load(f)
    names = {e["name"] for e in entries}
    cast["figures"] = [x for x in cast["figures"] if x["name"] not in names] + entries
    with open(cast_path, "w", encoding="utf-8") as f:
        json.dump(cast, f, ensure_ascii=False, indent=1)
    print("EXPORT M2INTRO DONE", group, len(actors), "figures", len(props), "props", frames, "frames")


if __name__ == "__main__":       # export_m2capital.py imports its helpers
    main()
