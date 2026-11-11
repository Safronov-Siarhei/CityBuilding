"""Exports the opening cutscene's cast into the Unity project, rigged and with their clips
(2026-11-10, Unity part 2). Never saves the .blend.

    blender --background <Buildings-Size1-Models.blend> --python export_cast.py -- <unity project>

One FBX per figure in Assets/_Project/Models/Cinematic/Cast: the figure's mesh on its rigid rig
(each part rides one bone, as in Blender) and every clip as its own take. The clips are IN PLACE
-- the figure walks or runs on the spot and the director in Unity moves it -- except the
champion's `Leap`, whose root moves from behind the swirl to where he lands (the rig object
carries that). Each clip is baked from the same pose functions the approved Blender film used.

A figure faces Blender -Y, which is Unity +Z after the import's axis conversion.

Also writes Assets/_Project/Campaign/OpeningCast.json: each figure's clips with their length and
looping, and the walk/run speeds in metres a second that make the feet keep pace with the ground.
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
import bell_anim as ba  # noqa: E402
import elder  # noqa: E402
import elder_anim as ea  # noqa: E402
import kids_anim as ka  # noqa: E402
import orc_anim as oa  # noqa: E402
import orcs_cast as oc  # noqa: E402
import priest  # noqa: E402
import mage  # noqa: E402
import mage_anim as ma  # noqa: E402
import townsfolk as tf  # noqa: E402
import townsfolk_anim as ta  # noqa: E402
import perfs as pf  # noqa: E402
import portal  # noqa: E402
import cutscene as cs  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = 30
TAU = 2 * math.pi
WALK_CYCLE = 1.1                   # seconds for two steps
RUN_CYCLE = a2.RUN_CYCLE


# ---------------------------------------------------------------- the clips' poses

def breathe(q, t, amount=1.0):
    br = math.sin(TAU * t / 3.2)
    q["chest"][0] += 1.5 * br * amount
    q["head"][0] += -1.5 + 1.2 * br * amount
    return q


def idle(t):
    q = a2.blank()
    for s, k in (("L", 1), ("R", -1)):
        q["upper_arm." + s] = [3, -k * 3, 0]
        q["forearm." + s] = [-10, 0, 0]
    return breathe(q, t)


def walk(phase, stride=22.0, arms=True):
    q = ta.walk_pose(phase, stride)
    if arms:
        s = math.sin(TAU * phase)
        for side, k in (("L", 1), ("R", -1)):
            q["upper_arm." + side] = [24 * s * k, -k * 3, 0]
            q["forearm." + side] = [-(10 + 14 * max(0.0, -s * k)), 0, 0]
    return q


def talk(t):
    return ea.talking(idle(t), t, 1.0)


def elder_idle(t):
    return ea.idle(t)


def elder_walk(phase):
    q = walk(phase, 14.0, arms=False)
    s = math.sin(TAU * phase)
    q["upper_arm.L"] = [14 * s, -3, 0]
    q["forearm.L"] = [-14, 0, 0]
    return ea.staff_still(q)


def elder_run(phase):
    q = a2.run(phase)
    for side in ("thigh.L", "thigh.R"):
        q[side][0] *= 0.7
    q["chest"][0] += 6
    return ea.staff_still(q)


def orc_idle(t):
    q = oa.crouch(0.15)
    for s, k in (("L", 1), ("R", -1)):
        q["upper_arm." + s] = [5, k * 12, 0]
        q["forearm." + s] = [-25, 0, 0]
    return breathe(q, t, 1.6)


def orc_walk(phase):
    q = walk(phase, 18.0)
    for s, k in (("L", 1), ("R", -1)):
        q["thigh." + s][1] = k * -5
    q["chest"][0] += 10
    q["head"][0] -= 6
    return q


def leap_root(t):
    """The champion's root in Leap, relative to where he gathers behind the swirl."""
    pos, head, pose = oa.champion_move(t)
    return pos - oa.BEHIND, head, pose


# ---------------------------------------------------------------- the cast

def cyc(fn, seconds):
    """A looping clip: fn(phase 0..1)."""
    return dict(fn=lambda t: fn((t / seconds) % 1.0), seconds=seconds, loop=True)


def clip(fn, seconds, loop=False):
    return dict(fn=fn, seconds=seconds, loop=loop)


def human_clips(walk_fn=walk, run_fn=a2.run, idle_fn=idle):
    return {
        "Idle": clip(idle_fn, 3.2, True),
        "Walk": cyc(walk_fn, WALK_CYCLE),
        "Run": cyc(run_fn, RUN_CYCLE),
        "Stare": clip(lambda t: breathe(ka.stare(t), t, 0.5), 3.2, True),
        "Talk": clip(talk, 3.0, True),
    }


def kid_clips():
    c = human_clips()
    c["Cheer"] = clip(ka.cheer, 1.0, True)
    c["Balance"] = clip(ka.balance, 1.0 / 1.3 * 2, True)
    c["Crouch"] = clip(lambda t: ka.crouch(0.6, arms_back=1.0), 0.5, True)
    return c


def townsfolk_clips(name):
    c = human_clips()
    if name == "CinePeasantWoman":
        c["Run"] = cyc(pf.skirt_run, RUN_CYCLE)
        c["Scream"] = clip(lambda t: ta.woman_scream(t)[2], 0.85)
    if name == "CinePeasant":
        c["Startle"] = clip(lambda t: ta.peasant_startle(t)[2], 0.75)
    if name == "CineMerchant":
        c["Panic"] = clip(lambda t: ta.merchant_panic(t)[2], 1.5)
    return c


def orc_clips(name):
    run = {"CineOrcChampion": oa.brute_run, "CineOrcTorch": oa.torch_run, "CineOrcSpear": oa.spear_run}[name]
    c = {
        "Idle": clip(orc_idle, 3.2, True),
        "Walk": cyc(orc_walk, WALK_CYCLE * 1.1),
        "Run": cyc(run, RUN_CYCLE * 1.1),
    }
    return c


def cast():
    """(name, rig, scale, apply, clips, extra meshes)."""
    out = []
    mesh = ardan.make(Vector((0, 0, 0)))
    arm = aa.rig(mesh)
    sack = aa.sack(arm, mesh.data.materials[0])
    c = human_clips()
    c["WalkSack"] = dict(fn=None, seconds=WALK_CYCLE, loop=True, direct=lambda a, t: aa.walk(a, (t / WALK_CYCLE) % 1.0, carry=True))
    c["Amulet"] = clip(a2.amulet_pose, a2.AMULET_SECONDS)
    c["Drop"] = clip(a2.drop_pose, a2.DROP_SECONDS)
    out.append(("CineArdan", arm, ardan.SCALE, ka.apply, c, [sack]))

    arm = ka.rig_figure(elder.make(), elder.SCALE, bend=ea.stoop)
    c = human_clips(elder_walk, elder_run, elder_idle)
    c["Talk"] = clip(lambda t: ea.talking(ea.idle(t), t, 1.0), 3.0, True)
    out.append(("CineElder", arm, elder.SCALE, ea.apply, c, []))

    arm = ka.rig_figure(priest.make((0, 0, 0)), priest.SCALE)
    out.append(("CinePriest", arm, priest.SCALE, ka.apply, human_clips(), []))

    # the mage at the bridge (mission 1, approved 2026-11-11): his own idle and the scene's three clips
    arm = ka.rig_figure(mage.make((0, 0, 0)), mage.SCALE)
    c = human_clips(idle_fn=ma.idle)
    c["Wave"] = clip(ma.wave, ma.WAVE_SECONDS)
    c["Cast"] = clip(ma.cast, ma.CAST_SECONDS)
    c["Hold"] = clip(ma.hold, ma.HOLD_SECONDS, True)
    out.append(("CineMage", arm, mage.SCALE, ea.apply, c, []))

    for n in ("CineMerchant", "CinePeasant", "CinePeasantWoman"):
        arm = ka.rig_figure(tf.make(n), tf.FIGURES[n][1])
        out.append((n, arm, tf.FIGURES[n][1], ba.apply, townsfolk_clips(n), []))
    for n in ("CineBoy", "CineGirl"):
        arm = ka.rig_figure(tf.make(n), tf.FIGURES[n][1])
        out.append((n, arm, tf.FIGURES[n][1], ka.apply, kid_clips(), []))
    for n in oc.FIGURES:
        arm = ka.rig_figure(oc.make(n), oc.FIGURES[n][1], bones=oa.BONES)
        out.append((n, arm, oc.FIGURES[n][1], ka.apply, orc_clips(n), []))
    return out


# ---------------------------------------------------------------- baking and export

def bake(arm, name, scale, apply, spec):
    arm.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    frames = max(2, round(spec["seconds"] * FPS))
    for f in range(frames + 1):
        t = f / FPS if not spec["loop"] else spec["seconds"] * f / frames
        arm.location = (0, 0, 0)
        arm.rotation_euler = (0, 0, 0)
        aa.rest(arm)
        if spec.get("direct"):
            spec["direct"](arm, t)
        elif spec.get("root"):
            pos, head, pose = spec["fn"](t)
            arm.location = pos
            arm.rotation_euler = (0, 0, head or 0.0)
            apply(arm, pose, scale)
        else:
            apply(arm, spec["fn"](t), scale)
        for pb in arm.pose.bones:
            pb.keyframe_insert("rotation_quaternion", frame=f, group=pb.name)
            pb.keyframe_insert("location", frame=f, group=pb.name)
        if spec.get("root"):
            arm.keyframe_insert("location", frame=f)
            arm.keyframe_insert("rotation_euler", frame=f)
    return act


# ---------------------------------------------------------------- the performances

P = Matrix(((1, 0, 0), (0, 0, 1), (0, 1, 0)))


def unity_m(m):
    loc, rot, _ = m.decompose()
    q = (P @ rot.to_matrix() @ P).to_quaternion()
    return [round(loc.x, 4), round(loc.z, 4), round(loc.y, 4), round(q.x, 5), round(q.y, 5), round(q.z, 5), round(q.w, 5)]


def unity_p(v):
    return [round(v.x, 4), round(v.z, 4), round(v.y, 4)]


def samples(perf):
    """The performance sampled once, in order (a Track's heading turns as it goes)."""
    if getattr(perf, "_samples", None) is None:
        perf._samples = []
        for f in range(perf.frames()):
            pos, head, pose = perf.fn(f / FPS)
            perf._samples.append((Vector(pos), head or 0.0, {k: list(v) for k, v in pose.items()}))
    return perf._samples


def root_track(perf):
    fmap, turn = pf.FRAMES[perf.frame]
    out = []
    for pos, head, _ in samples(perf):
        w = fmap(pos)
        out += [round(w.x, 4), round(w.z, 4), round(w.y, 4), round(math.degrees(math.pi - (head + turn)), 2)]
    return out


def bake_perf(arm, perf, scale, apply, keyed_root=False, start=0):
    """The performance's pose as a take, posed where it stands in its shot (so an aimed arm points
    where it did); the root keyed too only for sampling the set pieces that follow it."""
    arm.animation_data_create()
    act = bpy.data.actions.new(perf.name)
    act.use_fake_user = True
    arm.animation_data.action = act
    for f, (pos, head, pose) in enumerate(samples(perf)):
        arm.location = pos
        arm.rotation_euler = (0, 0, head)
        aa.rest(arm)
        bpy.context.view_layer.update()
        (perf.apply or apply)(arm, pose, scale)
        for pb in arm.pose.bones:
            pb.keyframe_insert("rotation_quaternion", frame=f + start, group=pb.name)
            pb.keyframe_insert("location", frame=f + start, group=pb.name)
        if keyed_root:
            arm.keyframe_insert("location", frame=f + start)
            arm.keyframe_insert("rotation_euler", frame=f + start)
    return act


def sample_objects(objs, frames, fmap, prefix, tracks):
    sc = bpy.context.scene
    mats = {o.name: [] for o in objs}
    shift = Matrix.Translation(fmap(Vector()))
    for f in range(frames):
        sc.frame_set(f + 1)
        for o in objs:
            mats[o.name] += unity_m(shift @ o.matrix_world)
    for o in objs:
        tracks["objects"].append({"name": prefix + "/" + o.name.split(".")[0], "piece": o.name.split(".")[0],
                                  "frames": frames, "m": mats[o.name]})


def scenes(rigs, perf_by, stones, sw, tracks):
    """Plays the performances that move set pieces and samples those pieces."""
    sc = bpy.context.scene
    keyed = []

    def play(fig, name):
        arm, scale, apply = rigs[fig]
        keyed.append((arm, bake_perf(arm, perf_by[(fig, name)], scale, apply, keyed_root=True, start=1)))
        return arm

    def clear():
        for arm, act in keyed:
            arm.animation_data.action = None
            bpy.data.actions.remove(act)
        keyed.clear()

    # the market: the cart behind the peasant, the fruit the woman takes
    fruit_st, cloth, ground, bolts, fruit = ta.stall_pieces()
    body, wheels = ta.cart_pieces()
    frames = int(ta.MARKET * FPS) + 1
    play("CineMerchant", "P_Market")
    wrig = play("CinePeasantWoman", "P_Market")
    prig = play("CinePeasant", "P_Market")
    ta.key_cart(body, wheels, prig, frames, lambda t: 1.0)
    ta.key_fruit(fruit, wrig, frames)
    sample_objects([body] + [w for w, _ in wheels] + [fruit], frames, pf.frame_M, "market", tracks)
    clear()

    # the chaos: the cart dropped, the fruit in her basket, the stall wrenched over, its bolts rolling
    frames = int(ta.PANIC * FPS) + 1
    play("CineMerchant", "P_Panic")
    wrig = play("CinePeasantWoman", "P_Chaos")
    prig = play("CinePeasant", "P_Chaos")
    play("CineOrcTorch", "P_Raid")
    ta.key_cart(body, wheels, prig, frames, lambda t: 1.0 - a2.env(t, ta.DROP, ta.DROP + 0.12))
    for f in range(frames):
        sc.frame_set(f + 1)
        fruit.location = wrig.matrix_world @ wrig.pose.bones["forearm.L"].tail + Vector((0, 0, 0.012))
        fruit.keyframe_insert("location", frame=f + 1)
    ta.key_topple(cloth, bolts, frames)
    sample_objects([body] + [w for w, _ in wheels] + [fruit, cloth] + [b for b, _ in bolts], frames, pf.frame_M, "chaos", tracks)
    clear()

    # the bell: its swing, the clapper's, and the rope from the lever to the priest's fists
    move, grip, loc, _ = pf.bell_stage()
    frames = int(3.4 * FPS) + 1
    prig = play("CinePriest", "P_Ring")
    top, bottom, bell, clap = [], [], [], []
    for f in range(frames):
        sc.frame_set(f + 1)
        t = f / FPS
        lt = ba.lever_world(loc, t)
        fists = [prig.matrix_world @ prig.pose.bones["forearm." + s].tail for s in ("L", "R")]
        end = (fists[0] + fists[1]) / 2 if t >= ba.GRAB else grip(t)
        top += unity_p(pf.frame_B(lt))
        bottom += unity_p(pf.frame_B(end))
        bell.append(round(ba.swing(t), 3))
        clap.append(round(ba.swing(max(0.0, t - 0.1)), 3))
    tracks["points"] += [{"name": "bell/ropeTop", "p": top}, {"name": "bell/ropeBottom", "p": bottom}]
    tracks["values"] += [{"name": "bell/bell", "v": bell}, {"name": "bell/clapper", "v": clap}]
    clear()

    # the talk: the amulet's glow, the runes waking one by one
    frames = int(6.5 * FPS) + 1
    glow, runes = [], []
    for f in range(frames):
        t = f / FPS
        glow.append(round(a2.amulet_glow(t), 4))
        g = 0.0
        for i in range(len(stones)):
            u = t - (3.35 + 0.1 * i)
            g += 0.0 if u < 0 else min(1.0, u / 0.25) * (0.75 + 0.25 * math.sin(t * 13 + i))
        runes.append(round(g / len(stones), 4))
    tracks["values"] += [{"name": "talk/amulet", "v": glow}, {"name": "talk/runes", "v": runes}]

    # the assembly, then the swirl opening in the arch
    flash = bpy.data.objects.new("CineFlash", bpy.data.lights.new("CineFlash", 'POINT'))
    sc.collection.objects.link(flash)
    frames = int(6.0 * FPS) + 1
    cs.key_assembly(stones, frames, 0.4, flash)
    sample_objects(stones, frames, pf.frame_V, "assembly", tracks)
    energy = []
    for f in range(frames):
        sc.frame_set(f + 1)
        energy.append(round(flash.data.energy, 2))
    tracks["values"].append({"name": "assembly/flash", "v": energy})
    for s_ in stones:
        s_.animation_data_clear()
    portal.set_arch(stones, sw, True)
    frames = int(3.0 * FPS) + 1
    hub, light = cs.open_swirl(sw, frames, 0.15, 1.2)
    sample_objects([sw], frames, pf.frame_V, "swirl", tracks)
    energy = []
    for f in range(frames):
        sc.frame_set(f + 1)
        energy.append(round(light.data.energy, 2))
    tracks["values"].append({"name": "swirl/light", "v": energy})
    tracks["points"].append({"name": "swirl/light", "p": unity_p(light.location)})


def export(name, arm, meshes, folder):
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


def main():
    project = sys.argv[sys.argv.index("--") + 1:][0]
    bpy.context.scene.render.fps = FPS                  # the file's own 24 stretched every take by 30/24
    bpy.context.scene.render.fps_base = 1.0
    folder = os.path.join(project, "Assets", "_Project", "Models", "Cinematic", "Cast")
    os.makedirs(folder, exist_ok=True)
    data = {"figures": []}
    members = cast()
    rigs = {name: (arm, scale, apply) for name, arm, scale, apply, _, _ in members}
    plat, stones, sw = portal.make((0, 0, 0))
    bpy.context.view_layer.update()
    perf_list = pf.build(stones)
    move, _, _, _ = pf.bell_stage()
    perf_list.append(pf.Perf("CinePriest", "P_Ring", 3.4, move, "B", ba.apply))
    perf_by = {(p.figure, p.name): p for p in perf_list}
    tracks = {"fps": FPS, "figures": [], "objects": [], "values": [], "points": [],
              "meta": {"kidsPlay": pf.KIDS_PLAY, "sackRelease": a2.RELEASE + ea.HOLD,
                       "orcFrom": unity_p(pf.frame_M(ta.ORC_FROM)), "tip": round(ta.TIP, 3)}}
    scenes(rigs, perf_by, stones, sw, tracks)
    # the set pieces' own keys would ride into every figure's FBX as takes of its rig (they resolve
    # on any object): gone before the figures are baked and exported
    for a_ in list(bpy.data.actions):
        bpy.data.actions.remove(a_)
    for p in perf_list:
        tracks["figures"].append({"name": p.figure + "/" + p.name, "frames": p.frames(), "root": root_track(p)})

    for name, arm, scale, apply, clips, extra in members:
        meshes = [c for c in arm.children if c.type == 'MESH'] + [m for m in extra if m not in arm.children]
        acts = []
        for cname, spec in clips.items():
            acts.append(bake(arm, cname, scale, apply, spec))
        for p in [p for p in perf_list if p.figure == name]:
            acts.append(bake_perf(arm, p, scale, apply))
            clips[p.name] = dict(seconds=p.seconds, loop=False)
        export(name, arm, meshes, folder)
        data["figures"].append({
            "name": name,
            "walkSpeed": round(0.42 * scale, 4),
            "runSpeed": round(0.95 * scale, 4),
            "clips": [{"name": c, "seconds": round(s["seconds"], 4), "loop": s["loop"]} for c, s in clips.items()],
        })
        for a in acts:                                  # the next figure's export takes only its own
            bpy.data.actions.remove(a)
    out = os.path.join(project, "Assets", "_Project", "Campaign", "OpeningCast.json")
    with open(out, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=1)
    with open(os.path.join(project, "Assets", "_Project", "Campaign", "OpeningTracks.json"), "w", encoding="utf-8") as f:
        json.dump(tracks, f, ensure_ascii=False, separators=(",", ":"))
    print("EXPORT CAST DONE", len(data["figures"]), "figures", len(perf_list), "performances")


main()
