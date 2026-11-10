"""Ардан's opening-cutscene animations, round 2 (after the shot list was agreed, 2026-11-10):
  * Amulet (shots 4-5): he stands with the sack on his shoulder, turns his head to the elder,
    looks down, lifts his left hand to the amulet; its rune flashes blue; he looks up, startled.
  * DropRun (shots 13-14): standing with the sack he flinches, lets go -- the sack slips off his
    shoulder and thumps down behind him -- and he breaks into a run.
  * Run: the run cycle the mission starts with.
Same rigid rig as ardan_anim.py (whose rig, sack and stage are reused). Never saves.

    blender --background <Buildings-Size1-Models.blend> --python ardan_anim2.py -- render <out dir> [amulet|droprun|run|all]
    ARDAN_SHEET=1,30,60 ... -- frames as PNGs instead of the videos
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
from mathutils import Euler, Matrix, Vector  # noqa: E402

FPS = aa.FPS
S = aa.S
TAU = 2 * math.pi
G = 3.3 * S            # gravity in figure units (a figure is ~1/3 of a real man)


def ease(x):
    x = min(1.0, max(0.0, x))
    return x * x * (3 - 2 * x)


def env(t, a, b):
    """0 before a, eased up to 1 at b."""
    return ease((t - a) / (b - a)) if b > a else float(t >= a)


# ---------------------------------------------------------------- poses as data

def blank():
    return {pb: [0.0, 0.0, 0.0] for pb, *_ in aa.BONES} | {"hips_loc": [0.0, 0.0, 0.0]}


def mix(a, b, w):
    return {k: [x + (y - x) * w for x, y in zip(a[k], b[k])] for k in a}


def apply(arm, pose):
    p = arm.pose.bones
    for k, v in pose.items():
        if k == "hips_loc":
            p["hips"].location = Vector(v) * S
        else:
            aa.rot(p[k], *v)


def carry_stand(t):
    """Standing with the sack: the right fist up at the shoulder on the sack's neck, the left arm
    hanging, a slow breath."""
    q = blank()
    br = math.sin(TAU * t / 3.2)
    q["chest"] = [5 + 1.2 * br, 5, 0]
    q["head"] = [-3, -2, 0]
    q["upper_arm.R"] = [-45, 12, 0]
    q["forearm.R"] = [-118, 0, 0]
    q["upper_arm.L"] = [2, -3, 0]
    q["forearm.L"] = [-12, 0, 0]
    q["hips_loc"] = [0, 0.001 * br, 0]
    return q


def run(phase):
    """The run cycle at phase 0..1 (two strides): a long reach of the thigh, the heel kicked up
    behind, the body leaning in, the elbows bent and pumping, a hop of flight on each stride."""
    q = blank()
    for side, off in (("L", 0.0), ("R", 0.5)):
        s = math.sin(TAU * (phase + off))
        c = math.cos(TAU * (phase + off))
        thigh = -42 * s - 8
        knee = 12 + 95 * max(0.0, c) ** 1.1 + 15 * max(0.0, s)
        foot = -(thigh + knee) * 0.7 - 15 * max(0.0, s)
        q["thigh." + side] = [thigh, 0, 0]
        q["shin." + side] = [knee, 0, 0]
        q["foot." + side] = [foot, 0, 0]
    s = math.sin(TAU * phase)
    q["hips_loc"] = [0, 0.012 * abs(math.cos(TAU * phase)) - 0.012, 0]
    q["hips"] = [8, 2 * s, 9 * s]
    q["chest"] = [10, -2 * s, -13 * s]
    q["head"] = [-10, 0, 5 * s]
    for side, k in (("L", 1), ("R", -1)):
        q["upper_arm." + side] = [42 * s * k - 5, -k * 6, 0]
        q["forearm." + side] = [-(75 + 20 * max(0.0, -s * k)), 0, 0]
    return q


# ---------------------------------------------------------------- the clips' timelines

AMULET_SECONDS = 4.6


def amulet_pose(t):
    """t in seconds."""
    q = carry_stand(t)
    # 0.3-0.8 s: turns his head to the elder at his left
    w = env(t, 0.3, 0.8) * (1 - env(t, 1.3, 1.7))
    q["head"][2] += 22 * w
    q["head"][0] += 4 * w
    # 1.3-1.9 s: looks down, the left hand comes up to the amulet and holds it
    d = env(t, 1.3, 1.9) * (1 - env(t, 3.3, 3.7))
    q["head"] = [q["head"][0] + 34 * d, q["head"][1], q["head"][2]]
    q["chest"][0] += 3 * d
    h = env(t, 1.4, 2.0) * (1 - env(t, 3.5, 4.0))
    # the fist on the amulet: angles fitted so the fist's end meets the stone, the elbow ahead of the chest
    q["upper_arm.L"] = [x + (y - x) * h for x, y in zip(q["upper_arm.L"], (-40, -10, -30))]
    q["forearm.L"] = [x + (y - x) * h for x, y in zip(q["forearm.L"], (-50, 0, -45))]
    # 3.3 s: startled by the stones -- the head snaps up and forward, the body draws back
    u = env(t, 3.3, 3.55)
    q["head"][0] -= 8 * u
    q["chest"][0] -= 6 * u
    q["hips_loc"][1] += 0.004 * u
    return q


def amulet_glow(t):
    """The rune's flash, 0..1: a flicker, then a full flash, fading."""
    return max(0.25 * env(t, 2.0, 2.15) * (1 - env(t, 2.2, 2.35)), env(t, 2.45, 2.7) * (1 - env(t, 3.0, 3.6)))


DROP_SECONDS = 3.6
RELEASE = 0.85          # the right hand lets go of the sack
RUN_FROM, RUN_FULL = 0.95, 1.55
RUN_SPEED = 0.95 * S    # figure units a second
RUN_CYCLE = 0.72        # seconds for two strides


def drop_pose(t):
    q = carry_stand(t)
    # 0.35 s: a flinch -- head up, body back, the free left arm jerks up
    f = env(t, 0.35, 0.5) * (1 - env(t, 0.9, 1.2))
    q["head"][0] -= 12 * f
    q["chest"][0] -= 7 * f
    q["upper_arm.L"][0] -= 25 * f
    q["forearm.L"][0] -= 40 * f
    # the right arm lets the sack go and drops
    r = env(t, RELEASE, RELEASE + 0.25)
    q["upper_arm.R"] = [-45 * (1 - r) + 10 * r, 12 * (1 - r) - 4 * r, 0]
    q["forearm.R"] = [-118 * (1 - r) - 20 * r, 0, 0]
    w = env(t, RUN_FROM, RUN_FULL)
    return mix(q, run(run_phase(t)), w)


def run_distance(t):
    """Distance run by time t: the speed eases up from 0 at RUN_FROM to full at RUN_FULL."""
    if t <= RUN_FROM:
        return 0.0
    ramp = RUN_FULL - RUN_FROM
    if t <= RUN_FULL:
        x = (t - RUN_FROM) / ramp
        return RUN_SPEED * ramp * (x ** 3 - x ** 4 / 2)   # integral of the eased speed
    return RUN_SPEED * ramp * 0.5 + RUN_SPEED * (t - RUN_FULL)


def run_phase(t):
    # the cycle runs on the distance, so the feet never skate while he speeds up
    return (run_distance(t) / (RUN_SPEED * RUN_CYCLE)) % 1.0


# ---------------------------------------------------------------- baking

def bake(arm, name, seconds, pose_at, root=None):
    arm.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    for f in range(int(seconds * FPS) + 1):
        t = f / FPS
        aa.rest(arm)
        apply(arm, pose_at(t))
        for pb in arm.pose.bones:
            pb.keyframe_insert("rotation_quaternion", frame=f + 1, group=pb.name)
            pb.keyframe_insert("location", frame=f + 1, group=pb.name)
        if root:
            arm.location = (0, -root(t), 0)
            arm.keyframe_insert("location", frame=f + 1)
    return act


def glow(mesh, arm):
    """The rune's light: a small blue diamond over the rune, a soft halo, a point light on his chin."""
    mat = bpy.data.materials.new("CineAmuletGlow")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = (0.35, 0.85, 1.0, 1)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(em.outputs[0], out.inputs[0])
    bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=6, radius=1.0)
    gem = bpy.context.active_object
    gem.name = "CineAmuletGlow"
    gem.data.materials.append(mat)
    gem.parent = arm
    gem.parent_type = 'BONE'
    gem.parent_bone = "chest"
    light = bpy.data.objects.new("CineAmuletLight", bpy.data.lights.new("CineAmuletLight", 'POINT'))
    light.data.color = (0.35, 0.85, 1.0)
    light.data.energy = 0.0
    light.data.shadow_soft_size = 0.02
    bpy.context.scene.collection.objects.link(light)
    light.parent = arm
    light.parent_type = 'BONE'
    light.parent_bone = "chest"
    # bone-parented objects sit at the bone's TAIL; place them on the amulet in world terms
    bpy.context.view_layer.update()
    bone = arm.data.bones["chest"]
    tail = arm.matrix_world @ (bone.matrix_local @ Matrix.Translation((0, bone.length, 0)))
    at = Vector((0, -0.08, 0.338)) * S
    for ob, off in ((gem, Vector((0, 0, 0))), (light, Vector((0, -0.05, 0)) * S)):
        ob.matrix_parent_inverse = tail.inverted()
        ob.location = at + off
    return gem, em, light


def key_glow(gem, em, light, seconds):
    for f in range(int(seconds * FPS) + 1):
        g = amulet_glow(f / FPS)
        gem.scale = Vector((1, 0.4, 1)) * (0.004 + 0.006 * g) * S
        gem.keyframe_insert("scale", frame=f + 1)
        gem.hide_render = g < 0.02
        gem.keyframe_insert("hide_render", frame=f + 1)
        em.inputs["Strength"].default_value = 2 + 10 * g
        em.inputs["Strength"].keyframe_insert("default_value", frame=f + 1)
        light.data.energy = 0.5 * g
        light.data.keyframe_insert("energy", frame=f + 1)


def key_sack(prop, arm, seconds):
    """The sack rides the chest until RELEASE, then falls off behind his right shoulder, tumbles,
    thumps down and settles -- baked as world keys (its skin to the chest is dropped)."""
    sc = bpy.context.scene
    prop.modifiers.clear()
    prop.parent = None
    me = prop.data
    chest = arm.data.bones["chest"]
    # its mesh was built in rest pose: carry it by the chest's pose matrix
    rest_inv = chest.matrix_local.inverted()
    mats = []
    for f in range(int(seconds * FPS) + 1):
        sc.frame_set(f + 1)
        mats.append(arm.matrix_world @ arm.pose.bones["chest"].matrix @ rest_inv)
    k0 = int(RELEASE * FPS)
    base = mats[k0]
    centre = Vector((-0.095, 0.06, 0.36)) * S
    vel = Vector((-0.25, 0.35, 0.25)) * S
    spin = Vector((2.5, -1.5, 0.8))
    pos0 = base @ centre
    z_floor = 0.055 * S
    landed = None
    for f in range(len(mats)):
        if f <= k0:
            m = mats[f]
        else:
            t = (f - k0) / FPS
            if landed is None:
                pos = pos0 + vel * t + Vector((0, 0, -0.5 * G * t * t))
                ang = spin * t
                if pos.z <= z_floor:
                    landed = (t, pos.copy(), ang.copy())
            if landed is not None:
                lt, lp, la = landed
                dt = t - lt
                k = 1 - math.exp(-dt * 9)
                pos = lp + Vector((vel.x, vel.y, 0)) * 0.12 * k
                pos.z = z_floor + 0.02 * S * math.exp(-dt * 10) * abs(math.sin(dt * 26))
                ang = la + Vector((1.2, -0.4, 0.3)) * k
            rot = Euler(ang).to_matrix().to_4x4()
            m = Matrix.Translation(pos) @ rot @ Matrix.Translation(-pos0) @ base
        prop.matrix_world = m
        prop.keyframe_insert("location", frame=f + 1)
        prop.keyframe_insert("rotation_euler", frame=f + 1)
        prop.keyframe_insert("scale", frame=f + 1)
    return me


def render(out, name, seconds, cam_at, cam_off, lens, follow=None):
    sc = bpy.context.scene
    frames = int(seconds * FPS) + 1
    sc.frame_start, sc.frame_end = 1, frames
    cam = sc.camera
    cam.data.lens = lens
    for f in range(frames):
        y = -follow(f / FPS) if follow else 0.0
        at = Vector(cam_at) + Vector((0, y, 0))
        cam.location = at + Vector(cam_off)
        cam.rotation_euler = (at - cam.location).to_track_quat('-Z', 'Y').to_euler()
        cam.keyframe_insert("location", frame=f + 1)
        cam.keyframe_insert("rotation_euler", frame=f + 1)
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
    cam.animation_data_clear()
    print("RENDERED", name)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    what = argv[2] if len(argv) > 2 else "all"
    out = argv[1]
    os.makedirs(out, exist_ok=True)
    mesh = ardan.make(Vector((0, 0, 0)))
    arm = aa.rig(mesh)
    prop = aa.sack(arm, mesh.data.materials[0])
    gem, em, light = glow(mesh, arm)
    aa.stage({mesh.name, arm.name, prop.name, gem.name, light.name})
    H = 0.3 * S
    if what in ("amulet", "all"):
        bake(arm, "CineArdan_Amulet", AMULET_SECONDS, amulet_pose)
        key_glow(gem, em, light, AMULET_SECONDS)
        # in front of him, a little to his left (the elder's side), at chest height
        render(out, "ardan_amulet", AMULET_SECONDS, (0, 0, H + 0.03 * S), (0.45 * S, -1.25 * S, 0.06 * S), 50)
    if what in ("droprun", "all"):
        for c in (gem, light):
            c.animation_data_clear()
        gem.hide_render = True
        light.data.energy = 0
        bake(arm, "CineArdan_DropRun", DROP_SECONDS, drop_pose, root=run_distance)
        key_sack(prop, arm, DROP_SECONDS)
        # side-on, the camera trailing him a little, so the sack falls in frame and he runs out of it
        render(out, "ardan_droprun", DROP_SECONDS, (0, -0.25, H), (-1.7, -1.0, 0.3),
               35, follow=lambda t: 0.6 * run_distance(t))
    if what in ("run", "all"):
        prop.hide_render = True
        gem.hide_render = True
        arm.animation_data_clear()
        bake(arm, "CineArdan_Run", 3.0, lambda t: run((t / RUN_CYCLE) % 1.0), root=lambda t: RUN_SPEED * t)
        render(out, "ardan_run", 3.0, (0, -0.05, H), (1.9, -1.5, 0.35), 50, follow=lambda t: RUN_SPEED * t)


if __name__ == "__main__":
    main()
