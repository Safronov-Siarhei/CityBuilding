"""Ардан's cutscene rig and the first two animations, after the user approved the model (2026-11-10):
a walk, and a walk with a sack over his right shoulder (the opening shot crosses the square so).

The rig is rigid, like the game's jointed figures: every part of the model sits wholly on one
bone (ardan.PARTS -> vertex groups), so the blocks turn at the joints and never bend.

    blender --background <Buildings-Size1-Models.blend> --python ardan_anim.py -- render <out dir> [walk|carry|all]
    ARDAN_SHEET=1,5,9 ... -- frames as PNGs instead of the videos (a contact sheet for checking)

Never saves (the model file is saved only by `ardan.py -- save`).
"""
import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import bpy  # noqa: E402
import ardan  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Euler, Quaternion, Vector  # noqa: E402

FPS = 30
S = ardan.SCALE

# (bone, head, tail, parent) before SCALE; the parts in ardan.PARTS carry the same names
BONES = [
    ("hips", (0, 0, 0.2), (0, 0, 0.27), None),
    ("chest", (0, 0, 0.27), (0, 0, 0.39), "hips"),
    ("head", (0, 0, 0.39), (0, 0, 0.54), "chest"),
]
for _side, _k in (("L", 1), ("R", -1)):
    _sh, _el, _wr = ardan._joints(_k)
    BONES += [
        (f"upper_arm.{_side}", tuple(_sh), tuple(_el), "chest"),
        (f"forearm.{_side}", tuple(_el), tuple(_wr + Vector((0, 0, -0.04))), f"upper_arm.{_side}"),
        (f"thigh.{_side}", (_k * 0.045, 0, 0.2), (_k * 0.045, 0, 0.098), "hips"),
        (f"shin.{_side}", (_k * 0.045, 0, 0.098), (_k * 0.045, 0, 0.032), f"thigh.{_side}"),
        (f"foot.{_side}", (_k * 0.045, 0, 0.032), (_k * 0.045, -0.05, 0.012), f"shin.{_side}"),
    ]


def rig(mesh):
    data = bpy.data.armatures.new(ardan.OBJ + "Rig")
    arm = bpy.data.objects.new(ardan.OBJ + "Rig", data)
    mesh.users_collection[0].objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')
    for name, head, tail, parent in BONES:
        b = data.edit_bones.new(name)
        b.head = Vector(head) * S
        b.tail = Vector(tail) * S
        b.roll = 0.0
        if parent:
            b.parent = data.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT')
    mesh.parent = arm
    mod = mesh.modifiers.new("Rig", 'ARMATURE')
    mod.object = arm
    return arm


# ---------------------------------------------------------------- posing

def rot(pb, rx=0.0, ry=0.0, rz=0.0):
    """Turns a bone by degrees about the rig's axes at rest -- X pitches (a leg or arm: negative
    swings it forward; the body: positive leans it forward), Y rolls it sideways (positive moves
    a hanging limb towards -X, his right), Z turns it -- stored in the bone's own frame."""
    r = Euler((math.radians(rx), math.radians(ry), math.radians(rz)), 'XYZ').to_matrix()
    m = pb.bone.matrix_local.to_3x3()
    pb.rotation_mode = 'QUATERNION'
    pb.rotation_quaternion = (m.inverted() @ r @ m).to_quaternion()


def rest(arm):
    for pb in arm.pose.bones:
        pb.rotation_mode = 'QUATERNION'
        pb.rotation_quaternion = Quaternion()
        pb.location = (0, 0, 0)


def walk(arm, phase, stride=28.0, carry=False):
    """The walk at phase 0..1 (two steps): legs swing from the hip, the knee folds as the foot
    passes under him, the foot rolls heel to toe; the arms swing against the legs; the hips bob
    twice and turn with the stride; the head stays level."""
    p = arm.pose.bones
    tau = 2 * math.pi
    for side, off in (("L", 0.0), ("R", 0.5)):
        s = math.sin(tau * (phase + off))
        c = math.cos(tau * (phase + off))
        thigh = -stride * s
        knee = 4 + 50 * max(0.0, c) ** 1.4 + 5 * max(0.0, s)
        foot = -(thigh + knee) - 12 * max(0.0, s) + 25 * max(0.0, -s) ** 2 * max(0.0, c)
        rot(p["thigh." + side], thigh)
        rot(p["shin." + side], knee)
        rot(p["foot." + side], foot)
    s = math.sin(tau * phase)
    p["hips"].location = (0, (0.006 * math.cos(2 * tau * phase) - 0.003) * S, 0)
    rot(p["hips"], 0, 2.5 * s, 6 * s)
    rot(p["chest"], 6 if carry else 4, (6 if carry else 0) - 2 * s, -8 * s)
    rot(p["head"], -4, -3 if carry else 0, 3 * s)
    for side, k in ((("L", 1),) if carry else (("L", 1), ("R", -1))):
        rot(p["upper_arm." + side], 24 * s * k, -k * 3, 0)
        rot(p["forearm." + side], -(10 + 14 * max(0.0, -s * k)))
    if carry:
        # the right elbow down and forward, the fist up at the shoulder on the sack's neck
        rot(p["upper_arm.R"], -45, 12, 0)
        rot(p["forearm.R"], -118, 0, 0)


def sack(arm, mat):
    """The sack he carries (a prop), resting on his right shoulder; rides the chest bone."""
    b = Builder()
    cx, cy, cz = -0.095, 0.06, 0.34
    b.ball(cx, cy, cz, 0.064, 0.11, 'burlap', sides=7)
    b.cylinder(cx + 0.006, cy, cz + 0.1, cz + 0.13, 0.02, 'burlap', sides=6)
    b.cylinder(cx + 0.006, cy, cz + 0.105, cz + 0.115, 0.023, 'wood_dark', sides=6)
    b.ball(cx + 0.008, cy, cz + 0.128, 0.024, 0.022, 'burlap', sides=6)
    pivot = Vector((cx, cy, cz + 0.05))
    tilt = Euler((0.85, -0.3, 0.0)).to_matrix()
    for v in b.bm.verts:
        v.co = (pivot + tilt @ (v.co - pivot)) * S
    me = b.to_mesh("CineArdanSack")
    me.materials.append(mat)
    detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
    obj = bpy.data.objects.new("CineArdanSack", me)
    arm.users_collection[0].objects.link(obj)
    obj.vertex_groups.new(name="chest").add(list(range(len(me.vertices))), 1.0, 'REPLACE')
    obj.parent = arm
    mod = obj.modifiers.new("Rig", 'ARMATURE')
    mod.object = arm
    return obj


def bake_action(arm, name, frames, pose_at):
    arm.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    for f in range(frames + 1):
        rest(arm)
        pose_at(f / frames)
        for pb in arm.pose.bones:
            pb.keyframe_insert("rotation_quaternion", frame=f, group=pb.name)
            pb.keyframe_insert("location", frame=f, group=pb.name)
    return act


def curves(act):
    try:
        return list(act.fcurves)
    except Exception:
        out = []
        for layer in act.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    out += list(bag.fcurves)
        return out


# ---------------------------------------------------------------- the stage

def stage(keep):
    sc = bpy.context.scene
    for o in sc.objects:
        o.hide_render = o.name not in keep
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 1280, 720
    sc.render.fps = FPS
    sc.view_settings.view_transform = 'Standard'
    for name, energy, rx, rz in (("ArdanSun", 3.5, 50, -25), ("ArdanFill", 1.0, 70, 150)):
        light = bpy.data.objects.new(name, bpy.data.lights.new(name, 'SUN'))
        light.data.energy = energy
        sc.collection.objects.link(light)
        light.rotation_euler = (math.radians(rx), 0, math.radians(rz))
    w = sc.world or bpy.data.worlds.new("W")
    sc.world = w
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.72, 0.95, 1)

    def flat(name, color, size, loc, z):
        me = bpy.data.meshes.new(name)
        x, y = size
        me.from_pydata([(-x, -y, 0), (x, -y, 0), (x, y, 0), (-x, y, 0)], [], [(0, 1, 2, 3)])
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*color, 1)
        mat.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0
        me.materials.append(mat)
        obj = bpy.data.objects.new(name, me)
        sc.collection.objects.link(obj)
        obj.location = (loc[0], loc[1], z)
        return obj

    flat("ArdanGround", (0.12, 0.36, 0.06), (12, 20), (0, -8), 0.0)
    flat("ArdanPath", (0.55, 0.42, 0.22), (0.22, 20), (0, -8), 0.001)
    # tufts and pebbles to see the ground go by
    rnd = random.Random(3)
    tm = bpy.data.materials.new("ArdanTuft")
    tm.use_nodes = True
    tm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.08, 0.32, 0.05, 1)
    bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=0.02, depth=0.05)
    tuft = bpy.context.active_object
    tuft.data.materials.append(tm)
    for i in range(700):
        x = rnd.uniform(-3, 3)
        if abs(x) < 0.26:
            continue
        t = bpy.data.objects.new("ArdanTuft%d" % i, tuft.data)
        sc.collection.objects.link(t)
        t.location = (x, rnd.uniform(-20, 2), 0.02)
        t.rotation_euler = (0, 0, rnd.uniform(0, 6))
    bpy.data.objects.remove(tuft)
    cam = bpy.data.objects.new("ArdanCam", bpy.data.cameras.new("ArdanCam"))
    sc.collection.objects.link(cam)
    sc.camera = cam
    return cam


def clip(out, name, arm, act, speed, seconds, side, cam):
    sc = bpy.context.scene
    arm.animation_data.action = act
    if hasattr(arm.animation_data, "action_slot") and len(act.slots):
        arm.animation_data.action_slot = act.slots[0]
    for fc in curves(act):
        if not any(m.type == 'CYCLES' for m in fc.modifiers):
            fc.modifiers.new('CYCLES')
    frames = int(seconds * FPS)
    sc.frame_start, sc.frame_end = 1, frames
    travel = speed * (frames - 1) / FPS
    rider = bpy.data.objects.new("ArdanRider", None)
    sc.collection.objects.link(rider)
    # the figure walks along -Y; the rig object is moved by keyframes on a separate action
    for ob in (arm, rider):
        ob.location = (0, 0, 0)
    arm.location = (0, 0, 0)
    drv = arm.driver_add("location", 1).driver
    drv.type = 'SCRIPTED'
    drv.expression = f"-{speed / FPS} * (frame - 1)"
    rdrv = rider.driver_add("location", 1).driver
    rdrv.type = 'SCRIPTED'
    rdrv.expression = drv.expression
    cam.parent = rider
    at = Vector((0, -0.05, 0.3 * S))
    cam.location = at + Vector((side * 1.9, -1.5, 0.35))
    cam.data.lens = 50
    cam.rotation_euler = (at - cam.location).to_track_quat('-Z', 'Y').to_euler()
    sheet = os.environ.get("ARDAN_SHEET")
    if sheet:
        sc.render.image_settings.file_format = 'PNG'
        for f in [int(x) for x in sheet.split(",")]:
            sc.frame_set(f)
            sc.render.filepath = os.path.join(out, "%s_%03d.png" % (name, f))
            bpy.ops.render.render(write_still=True)
    else:
        s = sc.render.image_settings
        try:
            s.media_type = 'VIDEO'
        except (AttributeError, TypeError):
            pass
        s.file_format = 'FFMPEG'
        sc.render.ffmpeg.format = 'MPEG4'
        sc.render.ffmpeg.codec = 'H264'
        sc.render.ffmpeg.constant_rate_factor = 'HIGH'
        sc.render.filepath = os.path.join(out, name + ".mp4")
        bpy.ops.render.render(animation=True)
    print("RENDERED", name, "travel", round(travel, 2))
    arm.driver_remove("location", 1)
    cam.parent = None
    bpy.data.objects.remove(rider)
    arm.location = (0, 0, 0)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    mesh = ardan.make(Vector((0, 0, 0)))
    arm = rig(mesh)
    walk_act = bake_action(arm, "CineArdan_Walk", 30, lambda t: walk(arm, t))
    carry_act = bake_action(arm, "CineArdan_CarrySack", 34, lambda t: walk(arm, t, stride=22.0, carry=True))
    prop = sack(arm, mesh.data.materials[0])
    if argv[0] != "render":
        return
    out = argv[1]
    what = argv[2] if len(argv) > 2 else "all"
    os.makedirs(out, exist_ok=True)
    cam = stage({mesh.name, arm.name, prop.name})
    # metres a second: a stride of two steps a second, as long as his legs reach
    if what in ("walk", "all"):
        prop.hide_render = True
        clip(out, "ardan_walk", arm, walk_act, 0.42 * S, 4.0, 1, cam)
    if what in ("carry", "all"):
        prop.hide_render = False
        clip(out, "ardan_carry", arm, carry_act, 0.3 * S, 4.0, -1, cam)


if __name__ == "__main__":
    main()
