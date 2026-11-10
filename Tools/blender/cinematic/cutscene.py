"""The opening cutscene, rendered in Blender as a whole film first (the user, 2026-11-10: «Сначала в
Blender»), from the approved models and animations, before it is moved into Unity.

One village: a cobbled square with the ruin platform in its middle, the church on its north side,
houses round it, a market street to the south, trees. Each shot is rendered on its own (one Blender
run per shot, PNG frames) in the coordinates its animation was made in: the village is hung on an
Empty and moved under the action, rather than the action moved into the village. cutscene_edit.py
then joins the shots and lays the subtitles over them.

    blender --background <Models.blend> --python cutscene.py -- <shot> <out dir>
    shots: wide market square talk fright assembly swirl leap horde chaos bell run
    CUT_SHEET=1,40,90 -- only those frames; CUT_SMALL=1 -- 640x360 (checking)
Never saves.
"""
import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import bpy  # noqa: E402

# the file holds earlier Cine* objects (saved models); this run builds its own, so drop them first
for _o in [o for o in bpy.data.objects if o.name.startswith("Cine")]:
    bpy.data.objects.remove(_o, do_unlink=True)
PRE = {o.name for o in bpy.context.scene.objects}

import ardan  # noqa: E402
import ardan_anim as aa  # noqa: E402
import ardan_anim2 as a2  # noqa: E402
import bell_anim as ba  # noqa: E402
import church  # noqa: E402
import details as d  # noqa: E402
import elder  # noqa: E402
import elder_anim as ea  # noqa: E402
import figure as fg  # noqa: E402
import kids_anim as ka  # noqa: E402
import orc_anim as oa  # noqa: E402
import orcs_cast as oc  # noqa: E402
import portal  # noqa: E402
import townsfolk as tf  # noqa: E402
import townsfolk_anim as ta  # noqa: E402
import village  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = 30
S = fg.SET_SCALE
TAU = 2 * math.pi

# ---------------------------------------------------------------- the village (its own frame: square at 0)

CHURCH_AT = Vector((0.0, 7.4, 0))
MARKET_AT = Vector((1.0, -5.2, 0))
HOUSES = [("CineHovel", (-5.6, 0.8)), ("CineCottage", (5.8, 0.6)), ("CineManor", (-5.6, 5.4)),
          ("CineHovel", (5.6, 5.0)), ("CineCottage", (-4.4, -5.2)), ("CineHovel", (4.7, -4.6)),
          ("CineCottage", (-7.6, -1.8)), ("CineManor", (8.0, 2.4))]
FIRE_HOUSE = 5
ARDAN_AT = Vector((0.0, -2.4, 0))                  # where he talks with the elder, south of the ruins
ELDER_AT = Vector((-0.36, -2.1, 0))
FACE_NORTH = math.pi


def flat(name, colour, size, z=0.0):
    me = bpy.data.meshes.new(name)
    x, y = size
    me.from_pydata([(-x, -y, 0), (x, -y, 0), (x, y, 0), (-x, y, 0)], [], [(0, 1, 2, 3)])
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*colour, 1)
    bsdf.inputs["Roughness"].default_value = 1.0
    me.materials.append(mat)
    obj = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = (0, 0, z)
    return obj


def heading_to(frm, to):
    dx, dy = to[0] - frm[0], to[1] - frm[1]
    return math.atan2(dx, -dy)


def build_village(skip=()):
    """Everything that does not move, hung on CineVillageRoot (village coordinates)."""
    root = bpy.data.objects.new("CineVillageRoot", None)
    bpy.context.scene.collection.objects.link(root)
    objs = [flat("CineGrass", (0.16, 0.42, 0.08), (40, 40), 0.0)]
    b = Builder()
    b.cylinder(0, 0, 0.0, 0.012, 4.3, 'cobble', sides=24)
    b.box(-1.3, 1.3, -9.5, -3.9, 0.0, 0.012, 'cobble')                  # the market street
    b.box(-0.9, 0.9, 3.9, 5.6, 0.0, 0.012, 'cobble')                    # up to the church door
    objs.append(fg.install_static("CineSquare", b, scale=1.0))
    if 'church' not in skip:
        objs.append(church.make(location=tuple(CHURCH_AT)))
    made = {}
    for i, (kind, (x, y)) in enumerate(HOUSES):
        if kind not in made:
            made[kind] = village.make(kind)
            ob = made[kind]
        else:
            ob = made[kind].copy()
            bpy.context.scene.collection.objects.link(ob)
            ob.name = "%s.%d" % (kind, i)
        ob.location = (x, y, 0)
        ob.rotation_euler = (0, 0, heading_to((x, y), (0, 0)))
        objs.append(ob)
    t = Builder()
    rnd = random.Random(5)
    for i in range(46):                                                  # trees round the village
        a = rnd.uniform(0, TAU)
        r = rnd.uniform(6.8, 12.0) / S
        x, y = r * math.cos(a), r * math.sin(a)
        if -2.4 < x * S < 4.4 and y * S < -3.0:                          # keep the market street clear
            continue
        d.tree(t, x, y, r=rnd.uniform(0.18, 0.28))
    for x, y in ((-2.6, 2.9), (2.7, 3.0), (-3.0, -2.9)):
        d.bush(t, x / S, y / S, r=0.12)
    objs.append(fg.install_static("CineTrees", t))
    if 'market' not in skip:
        fruit_st, cloth, ground, bolts, fruit = ta.stall_pieces()
        body, wheels = ta.cart_pieces()
        hub = bpy.data.objects.new("CineMarketHub", None)
        bpy.context.scene.collection.objects.link(hub)
        hub.location = MARKET_AT
        for o in [fruit_st, cloth, ground, fruit, body] + [bo for bo, _ in bolts]:
            o.parent = hub
        body.location = (1.5, ta.LANE_Y - 0.3, 0)
        body.rotation_euler = (0, 0, math.pi)
        for wh, y in wheels:
            wh.parent = hub
            wh.location = (1.5, ta.LANE_Y - 0.3 - y, village.CART['z'] * ta.K)
            wh.rotation_euler = (0, 0, math.pi)
        objs.append(hub)
    for o in objs:
        if o.parent is None:
            o.parent = root
    return root


def place_root(root, square_at, turn=0.0):
    root.location = square_at
    root.rotation_euler = (0, 0, turn)


def to_clip(root, v):
    return root.matrix_world @ Vector(v)


def turn_objects(objs, about, turn):
    m = Matrix.Translation(about) @ Matrix.Rotation(turn, 4, 'Z') @ Matrix.Translation(-Vector(about))
    for o in objs:
        o.matrix_world = m @ o.matrix_world
        if "arch" in o.keys():
            arch = Matrix([o["arch"][i * 4:(i + 1) * 4] for i in range(4)])
            o["arch"] = [v for row in (m @ arch) for v in row]


# ---------------------------------------------------------------- light, camera, render

def stage():
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE'
    small = os.environ.get("CUT_SMALL")
    sc.render.resolution_x, sc.render.resolution_y = (640, 360) if small else (1280, 720)
    sc.render.fps = FPS
    sc.view_settings.view_transform = 'Standard'
    # a morning sun, low in the east and warm; a cool sky fill
    for name, energy, colour, rx, rz in (("CutSun", 4.0, (1.0, 0.9, 0.74), 62, -60),
                                          ("CutFill", 1.1, (0.75, 0.85, 1.0), 40, 140)):
        light = bpy.data.objects.new(name, bpy.data.lights.new(name, 'SUN'))
        light.data.energy = energy
        light.data.color = colour
        sc.collection.objects.link(light)
        light.rotation_euler = (math.radians(rx), 0, math.radians(rz))
    w = sc.world or bpy.data.worlds.new("W")
    sc.world = w
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.62, 0.76, 0.93, 1)
    cam = bpy.data.objects.new("CutCam", bpy.data.cameras.new("CutCam"))
    sc.collection.objects.link(cam)
    sc.camera = cam
    cam.data.clip_end = 200
    return cam


def key_cam(cam, f, at, loc, lens, shake=0.0):
    at, loc = Vector(at), Vector(loc)
    if shake:
        t = f / FPS
        loc = loc + Vector((math.sin(t * 37) * shake, math.sin(t * 29 + 1) * shake, math.sin(t * 43 + 2) * shake))
    cam.location = loc
    cam.data.lens = lens
    cam.rotation_euler = (at - loc).to_track_quat('-Z', 'Y').to_euler()
    cam.keyframe_insert("location", frame=f + 1)
    cam.keyframe_insert("rotation_euler", frame=f + 1)
    cam.data.keyframe_insert("lens", frame=f + 1)


def render(out, seconds):
    sc = bpy.context.scene
    for o in sc.objects:                             # the file's own models stay out of the film
        if o.name in PRE:
            o.hide_render = True
    frames = int(round(seconds * FPS))
    sc.frame_start, sc.frame_end = 1, frames
    os.makedirs(out, exist_ok=True)
    sc.render.image_settings.file_format = 'PNG'
    sheet = os.environ.get("CUT_SHEET")
    if sheet:
        for f in [int(x) for x in sheet.split(",")]:
            sc.frame_set(f)
            sc.render.filepath = os.path.join(out, "%04d.png" % f)
            bpy.ops.render.render(write_still=True)
    else:
        sc.render.filepath = os.path.join(out, "")
        bpy.ops.render.render(animation=True)
    print("RENDERED", out, frames)


# ---------------------------------------------------------------- the cast

def ardan_rig(sack=True, glow=False):
    mesh = ardan.make(Vector((0, 0, 0)))
    arm = aa.rig(mesh)
    out = {"arm": arm, "mesh": mesh}
    out["sack"] = aa.sack(arm, mesh.data.materials[0]) if sack else None
    if glow:
        out["glow"] = a2.glow(mesh, arm)
    return out


def elder_rig():
    return ka.rig_figure(elder.make(), elder.SCALE, bend=ea.stoop)


def hold(rig, scale, at, heading, pose_fn, seconds, name, apply=None):
    """A figure that stays on its spot, posed by pose_fn(t)."""
    tr = ka.Track(scale, 0.5)
    if apply:
        tr.apply = apply
    tr.add('fn', seconds, lambda t: (Vector(at), heading, pose_fn(t)))
    tr.bake(rig, name)


def rune_glows(stones):
    """A glowing copy of each stone's rune, its own emission so they can light one by one."""
    out = []
    poses = portal.arch_poses()
    for i, (name, size, wedge, arch) in enumerate(poses):
        stone = bpy.data.objects["CinePortalStone_" + name]
        b = Builder()
        sy = size[1]
        for (p0, p1) in portal.RUNE_SHAPES[i % len(portal.RUNE_SHAPES)]:
            a, c = Vector((p0[0], 0, p0[1])), Vector((p1[0], 0, p1[1]))
            mid = (a + c) / 2
            dv = c - a
            ax = dv.normalized()
            b.obox((mid.x, -sy * 0.52 - 0.02, mid.z), ax, (0, 1, 0), ax.cross(Vector((0, 1, 0))) * -1,
                   dv.length / 2 + 0.008, 0.004, 0.011, 'water', skip=())
        me = b.to_mesh("CineRuneGlow%d" % i)
        mat = bpy.data.materials.new("CineRuneGlow%d" % i)
        mat.use_nodes = True
        nt = mat.node_tree
        nt.nodes.clear()
        em = nt.nodes.new("ShaderNodeEmission")
        em.inputs["Color"].default_value = (0.3, 0.85, 1.0, 1)
        em.inputs["Strength"].default_value = 0.0
        nt.links.new(em.outputs[0], nt.nodes.new("ShaderNodeOutputMaterial").inputs[0])
        me.materials.append(mat)
        ob = bpy.data.objects.new("CineRuneGlow%d" % i, me)
        bpy.context.scene.collection.objects.link(ob)
        ob.parent = stone
        out.append((ob, em))
    return out


def key_runes(glows, frames, start, step=0.1, strength=6.0):
    """Each rune lights in turn from `start` (seconds), flickering up; None = all lit already."""
    for f in range(frames):
        t = f / FPS
        for i, (ob, em) in enumerate(glows):
            if start is None:
                g = 1.0
            else:
                u = t - (start + step * i)
                g = 0.0 if u < 0 else min(1.0, u / 0.25) * (0.75 + 0.25 * math.sin(t * 13 + i))
            em.inputs["Strength"].default_value = strength * g
            em.inputs["Strength"].keyframe_insert("default_value", frame=f + 1)
            ob.hide_render = g < 0.02
            ob.keyframe_insert("hide_render", frame=f + 1)


def open_swirl(sw, frames, t0, dur, origin=Vector((0, 0, 0))):
    """The swirl grows from a point at the opening's heart and turns; a purple light comes up."""
    centre = Vector(origin) + Vector((0, -0.04, ka.PLATFORM_Z + portal.SPRING * 0.75))
    hub = bpy.data.objects.new("CineSwirlHub", None)
    bpy.context.scene.collection.objects.link(hub)
    hub.location = centre
    bpy.context.view_layer.update()
    sw.parent = hub
    sw.matrix_parent_inverse = hub.matrix_world.inverted()
    sw.hide_render = False
    light = bpy.data.objects.new("CinePortalLight", bpy.data.lights.new("CinePortalLight", 'POINT'))
    light.data.color = (0.7, 0.35, 1.0)
    light.data.shadow_soft_size = 0.4
    bpy.context.scene.collection.objects.link(light)
    light.location = centre + Vector((0, -0.6, -0.2))
    for f in range(frames):
        t = f / FPS
        u = 1.0 if t0 is None else max(0.0, min(1.0, (t - t0) / dur))
        k = 1 - (1 - u) ** 3 + 0.08 * math.sin(u * math.pi) if u < 1 else 1.0
        s = max(0.01, k)
        hub.scale = (s, s, s)
        hub.keyframe_insert("scale", frame=f + 1)
        light.data.energy = 300 * u + 40 * math.sin(t * 7) * u
        light.data.keyframe_insert("energy", frame=f + 1)
    return hub, light


def portal_at(origin, state, turn=0.0):
    """The stones and platform at `origin`: 'ruin', 'arch' (swirl hidden) or 'open' (swirl full)."""
    plat, stones, sw = portal.make(tuple(origin))
    if turn:
        turn_objects([plat] + stones + [sw], Vector(origin), turn)
    if state in ('arch', 'open'):
        portal.set_arch(stones, sw, True)
    sw.hide_render = state != 'open'
    return plat, stones, sw


def key_assembly(stones, frames, t0, flash):
    """The stones rise and fly into the arch: the pillars grow from the two standing bases, then the
    arch from both sides, the keystone last. Returns when the keystone lands."""
    order = []
    names = [s.name.split("_", 1)[1] for s in stones]
    for i in (1, 2, 3):
        order.append(["PillarL%d" % i, "PillarR%d" % i])
    for i in range(3):
        order.append(["Arch%d" % i, "Arch%d" % (6 - i)])
    order.append(["Arch3"])
    start = {}
    for rank, group in enumerate(order):
        for j, n in enumerate(group):
            start[n] = t0 + rank * 0.62 + j * 0.12
    fly = 1.1
    ruin = {s.name: s.matrix_world.copy() for s in stones}
    for f in range(frames):
        t = f / FPS
        for s, n in zip(stones, names):
            if n not in start:
                continue
            a = ruin[s.name]
            b = Matrix([s["arch"][i * 4:(i + 1) * 4] for i in range(4)])
            u = (t - start[n]) / fly
            if u <= 0:
                lift = max(0.0, min(1.0, (t - (start[n] - 0.5)) / 0.5))       # a shiver, then it lifts
                m = Matrix.Translation((0, 0, 0.004 * math.sin(t * 60) + 0.05 * lift * lift)) @ a
            elif u >= 1:
                m = b
            else:
                e = u * u * (3 - 2 * u)
                pa, pb = a.to_translation() + Vector((0, 0, 0.05)), b.to_translation()
                pos = pa.lerp(pb, e) + Vector((0, 0, 0.45 * math.sin(math.pi * e)))
                q = a.to_quaternion().slerp(b.to_quaternion(), e)
                m = Matrix.Translation(pos) @ q.to_matrix().to_4x4()
            s.matrix_world = m
            s.keyframe_insert("location", frame=f + 1)
            s.keyframe_insert("rotation_euler", frame=f + 1)
    end = max(start.values()) + fly
    for f in range(frames):
        t = f / FPS
        g = max(0.0, 1 - abs(t - end) / 0.35) if t > end - 0.1 else 0.0
        flash.data.energy = 300 * g
        flash.data.keyframe_insert("energy", frame=f + 1)
    return end


def fire(at, frames, n=5, seed=1):
    """A roof catching: flickering flame cones and a warm light."""
    mat = bpy.data.materials.new("CineFire")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = (1.0, 0.4, 0.05, 1)
    em.inputs["Strength"].default_value = 2.2
    nt.links.new(em.outputs[0], nt.nodes.new("ShaderNodeOutputMaterial").inputs[0])
    rnd = random.Random(seed)
    flames = []
    for i in range(n):
        bpy.ops.mesh.primitive_cone_add(vertices=5, radius1=0.18, depth=0.6)
        c = bpy.context.active_object
        c.name = "CineFlame%d" % i
        c.data.materials.append(mat)
        c.location = Vector(at) + Vector((rnd.uniform(-0.5, 0.5), rnd.uniform(-0.3, 0.3), 0.2))
        flames.append((c, rnd.uniform(0, 6), rnd.uniform(0.7, 1.3)))
    light = bpy.data.objects.new("CineFireLight", bpy.data.lights.new("CineFireLight", 'POINT'))
    light.data.color = (1.0, 0.55, 0.2)
    bpy.context.scene.collection.objects.link(light)
    light.location = Vector(at) + Vector((0, 0, 0.6))
    for f in range(frames):
        t = f / FPS
        for c, ph, sz in flames:
            k = sz * (0.8 + 0.25 * math.sin(t * 11 + ph) + 0.1 * math.sin(t * 23 + ph * 2))
            c.scale = (k * 0.9, k * 0.9, k * 1.2)
            c.keyframe_insert("scale", frame=f + 1)
        light.data.energy = 80 + 25 * math.sin(t * 13)
        light.data.keyframe_insert("energy", frame=f + 1)


# ---------------------------------------------------------------- the shots

def kids_setup(stones):
    a_, b_, c_ = ka.stepping_stones(ka.stone_tops(stones))
    boy = ka.rig_figure(tf.make("CineBoy"), tf.FIGURES["CineBoy"][1])
    girl = ka.rig_figure(tf.make("CineGirl"), tf.FIGURES["CineGirl"][1])
    mid = (a_ + b_ + c_) / 3
    inward = (Vector((0, 0, 0)) - Vector((mid.x, mid.y, 0))).normalized()
    girl_at = ka.ground(mid + inward * 0.4)
    return boy, girl, (a_, b_, c_), girl_at, mid, inward


def kids_play(boy, girl, chain, girl_at, seconds):
    a_, b_, c_ = chain
    s_b, s_g = tf.FIGURES["CineBoy"][1], tf.FIGURES["CineGirl"][1]
    sp = 0.95 * s_b * 1.1
    tb = ka.Track(s_b, sp)
    down = ka.ground(c_ + (girl_at - c_).normalized() * 0.3)
    tb.add('pose', 0.8, a_, ka.balance, b_).add('jump', 0.85, a_, b_).add('pose', 0.9, b_, ka.balance, c_)
    tb.add('jump', 0.85, b_, c_).add('pose', 0.5, c_, lambda t: ka.balance(t + 0.4), a_)
    tb.add('jump', 0.85, c_, down).add('pose', max(0.2, seconds - 4.75), down, lambda t: ka.cheer(t * 0.8), girl_at)
    tg = ka.Track(s_g, sp)
    tg.add('pose', seconds, girl_at, ka.cheer, (a_ + b_) / 2)
    tb.bake(boy, "CineBoy_Play")
    tg.bake(girl, "CineGirl_Play")


def shot_wide(out):
    root = build_village()
    plat, stones, sw = portal_at(Vector((0, 0, 0)), 'ruin')
    boy, girl, chain, girl_at, *_ = kids_setup(stones)
    seconds = 5.0
    kids_play(boy, girl, chain, girl_at, seconds)
    er = elder_rig()
    hold(er, elder.SCALE, ELDER_AT, heading_to(ELDER_AT, (0.2, -0.3)), ea.idle, seconds, "CineElder_Idle", apply=ea.apply)
    m = ka.rig_figure(tf.make("CineMerchant"), ta.MS)
    tr = ka.Track(ta.MS, 0.5)
    tr.apply = ba.apply
    tr.add('fn', seconds, lambda t: (ta.merchant_market(t)[0] + MARKET_AT, ta.M_HEAD, ta.merchant_market(t)[2]))
    tr.bake(m, "CineMerchant_Wide")
    cam = stage()
    frames = int(seconds * FPS)
    for f in range(frames):
        u = a2.ease(f / frames)
        loc = Vector((-9.0, -15.0, 8.5)).lerp(Vector((3.2, -6.6, 3.0)), u)
        at = Vector((0.0, 2.0, 1.5)).lerp(Vector((0.0, -0.6, 0.35)), u)
        key_cam(cam, f, at, loc, 28)
    render(out, seconds)


def shot_market(out):
    root = build_village(skip=('market',))
    place_root(root, -MARKET_AT)
    portal_at(-MARKET_AT, 'ruin')
    seconds = ta.MARKET
    fruit_st, cloth, ground, bolts, fruit = ta.stall_pieces()
    body, wheels = ta.cart_pieces()
    rigs = {n: ka.rig_figure(tf.make(n), tf.FIGURES[n][1]) for n in ("CineMerchant", "CinePeasantWoman", "CinePeasant")}
    for n, fn in (("CineMerchant", ta.merchant_market), ("CinePeasantWoman", ta.woman_market), ("CinePeasant", ta.peasant_market)):
        tr = ka.Track(tf.FIGURES[n][1], 0.5)
        tr.apply = ba.apply
        tr.add('fn', seconds, fn)
        tr.bake(rigs[n], n + "_Market")
    frames = int(seconds * FPS) + 1
    ta.key_cart(body, wheels, rigs["CinePeasant"], frames, lambda t: 1.0)
    ta.key_fruit(fruit, rigs["CinePeasantWoman"], frames)
    cam = stage()
    for f in range(frames):
        u = f / frames
        key_cam(cam, f, Vector((0.35, -0.45, 0.32)), Vector((-0.4 + 0.5 * u, -4.0, 1.75 - 0.2 * u)), 32)
    render(out, seconds)


def shot_square(out):
    root = build_village()
    plat, stones, sw = portal_at(Vector((0, 0, 0)), 'ruin')
    seconds = 6.0
    boy, girl, chain, girl_at, *_ = kids_setup(stones)
    kids_play(boy, girl, chain, girl_at, seconds)
    er = elder_rig()
    hold(er, elder.SCALE, ELDER_AT, heading_to(ELDER_AT, (-1.2, -2.5)), ea.idle, seconds, "CineElder_Idle", apply=ea.apply)
    ar = ardan_rig()
    speed = 0.3 * ardan.SCALE
    start = Vector((-2.15, -2.5, 0))

    def walk(t):
        q = ta.walk_pose((t * 1.0 / 1.13) % 1.0, stride=22.0)
        q["chest"] = [6, 6, q["chest"][2]]
        q["head"] = [-4, -3, q["head"][2]]
        q["upper_arm.R"] = [-45, 12, 0]
        q["forearm.R"] = [-118, 0, 0]
        s = math.sin(TAU * (t / 1.13))
        q["upper_arm.L"] = [24 * s, -3, 0]
        q["forearm.L"] = [-(10 + 14 * max(0.0, -s)), 0, 0]
        return start + Vector((speed * t, 0, 0)), heading_to((0, 0), (1, 0)), q

    tr = ka.Track(ardan.SCALE, 0.5)
    tr.add('fn', seconds, walk)
    tr.bake(ar["arm"], "CineArdan_CarrySquare")
    cam = stage()
    frames = int(seconds * FPS)
    for f in range(frames):
        u = f / frames
        key_cam(cam, f, Vector((-0.6 + 0.3 * u, -1.3, 0.3)), Vector((-2.3 + 0.3 * u, -4.7, 1.0)), 30)
    render(out, seconds)


def shot_talk(out):
    """Shots 4-5 in the talk's own frame: Ардан at 0 facing -y, the ruins at (0, -2.4), the
    village turned half round so he stands south of them as in every other shot."""
    root = build_village()
    sq = Vector((0, -2.4, 0))
    place_root(root, sq, math.pi)
    plat, stones, sw = portal_at(sq, 'ruin', turn=math.pi)
    glows = rune_glows(stones)
    seconds = 6.5
    frames = int(seconds * FPS) + 1
    ar = ardan_rig(glow=True)
    a2.bake(ar["arm"], "CineArdan_Amulet", seconds, a2.amulet_pose)
    gem, em, light = ar["glow"]
    a2.key_glow(gem, em, light, seconds)
    er = elder_rig()
    tr = ka.Track(elder.SCALE, 0.5)
    tr.apply = ea.apply
    tr.add('pose', seconds, ea.ELDER_AT, ea.talk_pose, (0, 0, 0))
    tr.bake(er, "CineElder_Talk")
    key_runes(glows, frames, 3.35)
    cam = stage()
    for f in range(frames):
        t = f / FPS
        if t < 2.3:                                  # the two of them
            key_cam(cam, f, (0.12, -0.2, 0.33), Vector((0.12, -0.2, 0.33)) + Vector((-1.45, -0.7, 0.25)), 32)
        elif t < 3.4:                                # his fist on the amulet
            key_cam(cam, f, (0.0, -0.05, 0.35 * ardan.SCALE), (0.25, -0.85, 0.42), 50)
        else:                                        # over their shoulders: the runes wake, the ground shakes
            key_cam(cam, f, (0.0, -2.4, 0.3), (0.55, 0.85, 0.72), 30, shake=0.008 * min(1.0, (t - 3.4) / 0.6))
    render(out, seconds)


def startled(ar, er, seconds):
    """Ардан and the elder where they stood, frozen at the end of the talk."""
    hold(ar["arm"], ardan.SCALE, ARDAN_AT, FACE_NORTH, lambda t: a2.amulet_pose(6.0 + 0 * t), seconds, "CineArdan_Hold")
    hold(er, elder.SCALE, ELDER_AT, heading_to(ELDER_AT, ARDAN_AT), lambda t: ea.talk_pose(6.0), seconds,
         "CineElder_Hold", apply=ea.apply)


def shot_fright(out):
    root = build_village()
    plat, stones, sw = portal_at(Vector((0, 0, 0)), 'ruin')
    glows = rune_glows(stones)
    boy, girl, chain, girl_at, mid, inward = kids_setup(stones)
    a_, b_, c_ = chain
    s_b, s_g = tf.FIGURES["CineBoy"][1], tf.FIGURES["CineGirl"][1]
    sp = 0.95 * s_b * 1.1
    woman_at = Vector((0.75, -1.3, ka.PLATFORM_Z))
    woman = ka.rig_figure(tf.make("CinePeasantWoman"), ta.WS)
    hold(woman, ta.WS, woman_at, heading_to(woman_at, c_), lambda t: ta.woman_scream(min(t, 0.0) + 0.0)[2], 3.3, "CineWoman_Wait",
         apply=ba.apply)
    away = woman_at + Vector((0.12, 0.25, 0))
    centre = Vector((0, 0, ka.PLATFORM_Z))
    tb = ka.Track(s_b, sp)
    tb.add('pose', 0.6, c_, ka.balance, b_).add('pose', 0.7, c_, ka.stare, centre).add('jump', 0.8, c_, ka.ground(c_ + (away - c_).normalized() * 0.3))
    tb.add('run', 1.2, ka.ground(c_ + (away - c_).normalized() * 0.3), away)
    tg = ka.Track(s_g, sp)
    tg.add('pose', 0.6, girl_at, ka.cheer, c_).add('pose', 0.9, girl_at, ka.stare, centre).add('run', 1.8, girl_at, away + Vector((-0.15, 0.05, 0)))
    tb.bake(boy, "CineBoy_Flee")
    tg.bake(girl, "CineGirl_Flee")
    seconds = 3.3
    ar, er = ardan_rig(), elder_rig()
    startled(ar, er, seconds)
    frames = int(seconds * FPS) + 1
    key_runes(glows, frames, None)
    cam = stage()
    at = Vector((mid.x, mid.y, 0.25)) + inward * 0.2
    side = Vector((-inward.y, inward.x, 0))
    for f in range(frames):
        key_cam(cam, f, at, at + inward * 1.7 + side * 1.5 + Vector((0, 0, 0.6)), 30, shake=0.006)
    render(out, seconds)


def shot_assembly(out):
    root = build_village()
    plat, stones, sw = portal_at(Vector((0, 0, 0)), 'ruin')
    glows = rune_glows(stones)
    seconds = 6.0
    frames = int(seconds * FPS) + 1
    ar, er = ardan_rig(), elder_rig()
    startled(ar, er, seconds)
    flash = bpy.data.objects.new("CineFlash", bpy.data.lights.new("CineFlash", 'POINT'))
    flash.data.color = (0.6, 0.9, 1.0)
    bpy.context.scene.collection.objects.link(flash)
    flash.location = (0, -0.4, portal.SPRING + portal.R_OUT)
    key_assembly(stones, frames, 0.4, flash)
    key_runes(glows, frames, None)
    cam = stage()
    for f in range(frames):
        u = f / frames
        key_cam(cam, f, Vector((0, 0, 0.55 + 0.35 * u)), Vector((1.7, -4.5, 0.45)), 30, shake=0.004)
    render(out, seconds)


def shot_swirl(out):
    root = build_village()
    plat, stones, sw = portal_at(Vector((0, 0, 0)), 'arch')
    glows = rune_glows(stones)
    seconds = 3.0
    frames = int(seconds * FPS) + 1
    ar, er = ardan_rig(), elder_rig()
    startled(ar, er, seconds)
    open_swirl(sw, frames, 0.15, 1.2)
    key_runes(glows, frames, None)
    cam = stage()
    for f in range(frames):
        t = f / FPS
        if t < 1.5:
            key_cam(cam, f, (0, 0, 0.8), (0.5, -3.3, 0.55), 32)
        else:                                        # their faces in its light
            key_cam(cam, f, (-0.15, -2.25, 0.4), (0.3, -1.05, 0.5), 38)
    render(out, seconds)


def orc_rigs():
    figs = {}
    for name in ("CineOrcChampion", "CineOrcTorch", "CineOrcSpear"):
        figs[name] = ka.rig_figure(oc.make(name), oc.FIGURES[name][1], bones=oa.BONES)
    return figs


def shot_leap(out):
    root = build_village()
    plat, stones, sw = portal_at(Vector((0, 0, 0)), 'arch')
    glows = rune_glows(stones)
    seconds = oa.LEAP_SECONDS
    frames = int(seconds * FPS) + 1
    open_swirl(sw, frames, None, 1.0)
    key_runes(glows, frames, None)
    ar, er = ardan_rig(), elder_rig()
    startled(ar, er, seconds)
    figs = orc_rigs()
    for n in ("CineOrcTorch", "CineOrcSpear"):
        figs[n].location = (0, 3, -3)
    oa.debris(fg.material())
    champ = ka.Track(1.3, 0.9)
    champ.add('fn', seconds, oa.champion_move)
    champ.bake(figs["CineOrcChampion"], "CineOrcChampion_Leap")
    cam = stage()
    for f in range(frames):
        t = f / FPS
        shake = 0.02 * max(0.0, 1 - abs(t - oa.IMPACT) / 0.3) + 0.006 * (1 if 2.5 < t < 4.1 else 0)
        key_cam(cam, f, (0, -0.55, 0.5), (0.85, -2.85, 0.35), 32, shake=shake)
    render(out, seconds)


def shot_horde(out):
    root = build_village()
    plat, stones, sw = portal_at(Vector((0, 0, 0)), 'arch')
    glows = rune_glows(stones)
    seconds = 3.0
    frames = int(seconds * FPS) + 1
    open_swirl(sw, frames, None, 1.0)
    key_runes(glows, frames, None)
    ar, er = ardan_rig(), elder_rig()
    startled(ar, er, seconds)
    figs = orc_rigs()
    grit = oa.debris(fg.material())
    for o in grit:
        o.animation_data_clear()
        o.scale = (1, 1, 1) if o.name == "CineLandingCracks" else (0, 0, 0)
    for n, fn, p0, p1, wait in (("CineOrcTorch", oa.torch_run, Vector((0.0, 0.5, ka.PLATFORM_Z)), Vector((-1.0, -1.5, ka.PLATFORM_Z)), 0.0),
                                ("CineOrcSpear", oa.spear_run, Vector((0.0, 0.5, ka.PLATFORM_Z)), Vector((1.0, -1.5, ka.PLATFORM_Z)), 0.35)):
        tr = ka.Track(1.0, 0.95)
        tr.run_pose = fn
        if wait:
            tr.add('pose', wait, p0, lambda t: oa.crouch(0), (p1.x, p1.y, 0))
        tr.add('run', seconds - wait, p0, p1)
        tr.bake(figs[n], n + "_Run")
    champ = ka.Track(1.3, 0.9)
    champ.add('fn', seconds, lambda t: oa.champion_move(t + oa.IMPACT + 1.5))
    champ.bake(figs["CineOrcChampion"], "CineOrcChampion_Roar")
    cam = stage()
    for f in range(frames):
        key_cam(cam, f, (0, -0.6, 0.4), (1.2, -3.1, 0.65), 28)
    render(out, seconds)


def shot_chaos(out):
    root = build_village(skip=('market',))
    place_root(root, -MARKET_AT)
    plat, stones, sw = portal_at(-MARKET_AT, 'arch')
    seconds = ta.PANIC
    frames = int(seconds * FPS) + 1
    open_swirl(sw, frames, None, 1.0, origin=-MARKET_AT)
    bpy.context.view_layer.update()
    house = bpy.data.objects["CineHovel.%d" % FIRE_HOUSE]                     # the hovel by the market
    corners = [house.matrix_world @ Vector(c) for c in house.bound_box]
    mid = sum(corners, Vector()) / 8
    fire(Vector((mid.x, mid.y, max(c.z for c in corners) - 0.5)), frames, n=7)   # along its ridge
    fruit_st, cloth, ground, bolts, fruit = ta.stall_pieces()
    body, wheels = ta.cart_pieces()
    rigs = {n: ka.rig_figure(tf.make(n), tf.FIGURES[n][1]) for n in ("CineMerchant", "CinePeasantWoman", "CinePeasant")}
    orc = ka.rig_figure(oc.make("CineOrcTorch"), 1.0, bones=oa.BONES)
    sp = 0.95 * 1.05
    ta.TIP = ta.orc_time_at_x(ta.CLOTH_AT.x - 0.05, sp)

    def track(scale):
        tr = ka.Track(scale, 0.95 * scale * 1.05)
        tr.apply = ba.apply
        return tr
    tr = track(ta.MS)
    tr.add('fn', seconds, ta.merchant_panic)
    tr.bake(rigs["CineMerchant"], "CineMerchant_Panic")
    tr = track(ta.WS)
    tr.add('fn', 0.85, ta.woman_scream).add('run', seconds - 0.85, ta.WOMAN_AT, Vector((-2.4, -1.6, 0)))
    tr.bake(rigs["CinePeasantWoman"], "CinePeasantWoman_Panic")
    tr = track(ta.PS)
    start = ta.peasant_at(ta.MARKET) + Vector((-ta.P_SPEED * 0.15, 0, 0))
    tr.add('fn', 0.75, ta.peasant_startle).add('run', seconds - 0.75, start, start + Vector((-3.2, 0.3, 0)))
    tr.bake(rigs["CinePeasant"], "CinePeasant_Panic")
    tr = track(1.0)
    tr.add('fn', seconds, lambda t: ta.orc_move(t, sp))
    tr.bake(orc, "CineOrcTorch_Raid")
    ta.key_cart(body, wheels, rigs["CinePeasant"], frames, lambda t: 1.0 - a2.env(t, ta.DROP, ta.DROP + 0.12))
    sc = bpy.context.scene
    for f in range(frames):
        sc.frame_set(f + 1)
        fruit.location = rigs["CinePeasantWoman"].matrix_world @ rigs["CinePeasantWoman"].pose.bones["forearm.L"].tail + Vector((0, 0, 0.012))
        fruit.keyframe_insert("location", frame=f + 1)
    ta.key_topple(cloth, bolts, frames)
    cam = stage()
    for f in range(frames):
        key_cam(cam, f, Vector((0.7, -0.2, 0.6)), Vector((-0.3, -4.2, 1.45)), 26, shake=0.004)
    render(out, seconds)


def shot_bell(out):
    root = build_village(skip=('church',))
    place_root(root, -CHURCH_AT)
    portal_at(-CHURCH_AT, 'open')
    seconds = 3.4
    ch = church.make(with_bell=False)
    bell, clap, rope, loc = ba.pieces(ch)
    bf = church.BELFRY
    floor = (bf["floor"] + 0.02) * S
    stand = Vector((bf["tx"] * S + 0.03, bf["ty"] * S - 0.36, floor))
    grip_z = floor + 0.47 * ba.priest.SCALE
    rope_len = (loc.z + (ba.LEVER.z * S)) - grip_z
    pr = ka.rig_figure(ba.priest.make((0, 0, 0)), ba.priest.SCALE)
    move, grip = ba.make_priest_move(loc, stand, rope_len)
    tr = ka.Track(ba.priest.SCALE, 0.5)
    tr.apply = ba.apply
    tr.add('fn', seconds, move)
    tr.bake(pr, "CinePriest_RingBell")
    frames = int(seconds * FPS) + 1
    sc = bpy.context.scene
    for f in range(frames):
        t = f / FPS
        bell.rotation_euler = (math.radians(ba.swing(t)), 0, 0)
        bell.keyframe_insert("rotation_euler", frame=f + 1)
        clap.rotation_euler = (math.radians(ba.swing(max(0.0, t - 0.1))), 0, 0)
        clap.keyframe_insert("rotation_euler", frame=f + 1)
    for f in range(frames):
        sc.frame_set(f + 1)
        t = f / FPS
        top = ba.lever_world(loc, t)
        fists = [pr.matrix_world @ pr.pose.bones["forearm." + s].tail for s in ("L", "R")]
        end = (fists[0] + fists[1]) / 2 if t >= ba.GRAB else grip(t)
        dv = end - top
        rope.location = top
        rope.rotation_euler = Vector((0, 0, -1)).rotation_difference(dv.normalized()).to_euler()
        rope.scale = (1, 1, dv.length)
        for path in ("location", "rotation_euler", "scale"):
            rope.keyframe_insert(path, frame=f + 1)
    cam = stage()
    at = Vector((bf["tx"] * S, bf["ty"] * S - 0.15, floor + 0.38))
    for f in range(frames):
        u = f / frames
        key_cam(cam, f, at, at + Vector((2.6 - 0.4 * u, -0.25 - 0.6 * u, 0.1)), 36)
    render(out, seconds)


def shot_run(out):
    """Shots 13-14 in the shout's frame: Ардан at 0, the open portal at (0, 2.1) behind him."""
    root = build_village()
    sq = ea.PORTAL_AT
    place_root(root, sq)
    plat, stones, sw = portal_at(sq, 'arch')
    glows = rune_glows(stones)
    seconds = ea.SHOUT_SECONDS
    frames = int(seconds * FPS) + 1
    open_swirl(sw, frames, None, 1.0, origin=sq)
    key_runes(glows, frames, None)
    ar = ardan_rig()
    a2.bake(ar["arm"], "CineArdan_DropRun", seconds, lambda t: a2.drop_pose(max(0.0, t - ea.HOLD)),
            root=lambda t: a2.run_distance(max(0.0, t - ea.HOLD)))
    keep_release = a2.RELEASE
    a2.RELEASE = keep_release + ea.HOLD
    a2.key_sack(ar["sack"], ar["arm"], seconds)
    a2.RELEASE = keep_release
    er = elder_rig()
    tr = ka.Track(elder.SCALE, 0.5)
    tr.apply = ea.apply
    tr.add('pose', seconds, ea.ELDER_AT, ea.shout_pose, (0, 0, 0))
    tr.bake(er, "CineElder_Shout")
    figs = orc_rigs()
    for n in ("CineOrcTorch", "CineOrcSpear"):
        figs[n].location = (0, 6, -3)
    land = sq + oa.LAND
    hold(figs["CineOrcChampion"], 1.3, land, 0.0, lambda t: oa.champion_move(oa.LEAP_SECONDS + 0.4 + t * 0)[2], seconds, "CineOrcChampion_Stand")
    cam = stage()
    for f in range(frames):
        key_cam(cam, f, (0.15, 0.1, 0.4), (-1.9, -1.4, 0.25), 30)
    render(out, seconds)


SHOTS = {"wide": shot_wide, "market": shot_market, "square": shot_square, "talk": shot_talk,
         "fright": shot_fright, "assembly": shot_assembly, "swirl": shot_swirl, "leap": shot_leap,
         "horde": shot_horde, "chaos": shot_chaos, "bell": shot_bell, "run": shot_run}


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:]
    SHOTS[argv[0]](argv[1])
