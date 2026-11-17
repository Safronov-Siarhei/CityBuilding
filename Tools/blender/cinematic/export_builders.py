"""Exports the main menu's building site (approved by the user 2026-11-17) into the Unity project.
Never saves the .blend.

    blender --background <Buildings-Size1-Models.blend> --python export_builders.py -- <unity project>

  * Models/Cinematic/CineHouseBuild, CinePlankPile, CinePlankStack, CinePlank, CineSawhorse .fbx --
    set pieces as export_cine.py exports them (Z forward, axes baked, origin at the piece's pivot).
  * Models/Cinematic/Cast/CineCarpenter, CineLabourer .fbx -- rigged as export_cast.py exports the
    film's cast, with their loops as takes posed where they stand (an aimed arm points where it did):
    P_Hammer and P_Carry, and an Idle.
  * Campaign/MenuBuilders.json -- the site in its own frame (Unity axes; the house at 0, its front to
    -z): where each piece stands, where the carpenter stands, the labourer's root along his loop and
    the carried plank's, frame by frame. UI/MenuBuildSite.cs plays it.
  * Campaign/OpeningCast.json -- the two figures' clips (looping), merged in, so the importer loops them.
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

import ardan_anim as aa  # noqa: E402
import ardan_anim2 as a2  # noqa: E402
import bell_anim as ba  # noqa: E402
import builders as bd  # noqa: E402
import builders_anim as ban  # noqa: E402
import kids_anim as ka  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = 30
TAU = 2 * math.pi
P = Matrix(((1, 0, 0), (0, 0, 1), (0, 1, 0)))


def unity_pose(m):
    loc, rot, _ = m.decompose()
    q = (P @ rot.to_matrix() @ P).to_quaternion()
    return [round(loc.x, 4), round(loc.z, 4), round(loc.y, 4)], [round(q.x, 5), round(q.y, 5), round(q.z, 5), round(q.w, 5)]


def export_piece(o, folder):
    o.parent = None
    saved = o.matrix_world.copy()
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
    o.matrix_world = saved
    print("EXPORTED", path)


def export_figure(name, arm, folder):
    arm.location = (0, 0, 0)
    arm.rotation_euler = (0, 0, 0)
    arm.animation_data.action = None
    aa.rest(arm)
    bpy.ops.object.select_all(action='DESELECT')
    arm.select_set(True)
    for m in arm.children:
        if m.type == 'MESH':
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


def idle(t):
    q = a2.blank()
    for s, k in (("L", 1), ("R", -1)):
        q["upper_arm." + s] = [3, -k * 3, 0]
        q["forearm." + s] = [-10, 0, 0]
    br = math.sin(TAU * t / 3.2)
    q["chest"][0] += 1.5 * br
    q["head"][0] += -1.5 + 1.2 * br
    return q


def bake(arm, name, scale, seconds, fn, posed):
    """A looping take: frames 0..n with the last equal to the first. `posed` -> fn(t) gives (root,
    heading, pose) and the figure is posed where it stands, only its bones keyed."""
    arm.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    frames = max(2, round(seconds * FPS))
    for f in range(frames + 1):
        t = seconds * f / frames
        arm.location = (0, 0, 0)
        arm.rotation_euler = (0, 0, 0)
        if posed:
            pos, head, pose = fn(t)
            arm.location = pos
            arm.rotation_euler = (0, 0, head or 0.0)
        else:
            pose = fn(t)
        aa.rest(arm)
        bpy.context.view_layer.update()
        ba.apply(arm, pose, scale)
        for pb in arm.pose.bones:
            pb.keyframe_insert("rotation_quaternion", frame=f, group=pb.name)
            pb.keyframe_insert("location", frame=f, group=pb.name)
    return act


def main():
    project = sys.argv[sys.argv.index("--") + 1:][0]
    models = os.path.join(project, "Assets", "_Project", "Models", "Cinematic")
    cast_folder = os.path.join(models, "Cast")
    campaign = os.path.join(project, "Assets", "_Project", "Campaign")

    # ---- the set pieces, laid out as builders_anim lays them (its frame is the site's)
    placed = {
        "CineHouseBuild": (Vector(), 0.0),
        "CinePlankPile": (ban.PILE_AT, 0.0),
        "CinePlankStack": (ban.STACK_AT, 0.0),
        "CineSawhorse": (ban.SAW_AT, math.radians(70)),
    }
    pieces = []
    for name, (at, rz) in placed.items():
        o = bd.make_set(name)
        p, r = unity_pose(Matrix.Translation(at) @ Matrix.Rotation(rz, 4, 'Z'))
        pieces.append({"id": name, "p": p, "r": r})
        export_piece(o, models)
    export_piece(bd.make_set("CinePlank"), models)

    # ---- the builders and their loops
    carp = ka.rig_figure(bd.make("CineCarpenter"), ban.CS)
    lab = ka.rig_figure(bd.make("CineLabourer"), ban.LS)
    clips = {}
    for name, arm, scale, perf, seconds, fn in (
            ("CineCarpenter", carp, ban.CS, "P_Hammer", ban.HAMMER, ban.carpenter_pose),
            ("CineLabourer", lab, ban.LS, "P_Carry", ban.CARRY, ban.labourer_pose)):
        acts = [bake(arm, "Idle", scale, 3.2, idle, False), bake(arm, perf, scale, seconds, fn, True)]
        export_figure(name, arm, cast_folder)
        clips[name] = [{"name": "Idle", "seconds": 3.2, "loop": True}, {"name": perf, "seconds": round(seconds, 4), "loop": True}]
        for a in acts:
            bpy.data.actions.remove(a)

    # ---- the labourer's root and the plank, frame by frame over his loop
    frames = max(2, round(ban.CARRY * FPS))
    root, plank = [], []
    for f in range(frames + 1):
        t = ban.CARRY * f / frames
        at, head, _, (pp, ph) = ban.labourer_at(t)
        root += [round(at.x, 4), round(at.z, 4), round(at.y, 4), round(math.degrees(math.pi - head), 2)]
        p, r = unity_pose(ban.plank_matrix(pp, ph))
        plank += p + r
    cp, cr = unity_pose(Matrix.Translation(ban.CARP_AT) @ Matrix.Rotation(ban.CARP_HEAD, 4, 'Z'))
    data = {
        "_about": "The main menu's building site (Tools/blender/cinematic/export_builders.py): the site's own frame in Unity axes, the house at 0, its front to -z. Generated; place the site itself in Campaign/MenuVillage.json.",
        "fps": FPS,
        "pieces": pieces,
        "carpenter": {"figure": "CineCarpenter", "clip": "P_Hammer", "at": cp, "yaw": round(math.degrees(math.pi - ban.CARP_HEAD), 2)},
        "labourer": {"figure": "CineLabourer", "clip": "P_Carry", "seconds": round(ban.CARRY, 4), "frames": frames + 1, "root": root},
        "plank": {"piece": "CinePlank", "frames": frames + 1, "m": plank},
    }
    with open(os.path.join(campaign, "MenuBuilders.json"), "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, separators=(",", ":"))

    # ---- their clips into the cast file, the others kept
    path = os.path.join(campaign, "OpeningCast.json")
    with open(path, encoding="utf-8") as f:
        cast = json.load(f)
    cast["figures"] = [x for x in cast["figures"] if x["name"] not in clips]
    for name, cl in clips.items():
        scale = bd.FIGURES[name][1]
        cast["figures"].append({"name": name, "walkSpeed": round(0.42 * scale, 4), "runSpeed": round(0.95 * scale, 4), "clips": cl})
    with open(path, "w", encoding="utf-8") as f:
        json.dump(cast, f, ensure_ascii=False, indent=1)
    print("EXPORT BUILDERS DONE")


main()
