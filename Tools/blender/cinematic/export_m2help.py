"""Exports mission 2's last scene «Подкрепление» (all five animation groups approved 2026-11-16) for Unity.
Never saves the .blend.

    blender --background <Buildings-Size1-Models.blend> --python export_m2help.py -- <unity project>

The scene plays in the mission's own town, so there is no set: only the takes.

  * Models/Cinematic/Cast/Cap<Figure>.fbx -- each figure with its takes IN PLACE (the cavalry rides in three groups,
    the horses too, the orcs in four); a group with several copies of one figure gives each copy its own take
    ("Ride", "Ride1", "Ride2", ...: the riders' strides are out of step);
  * Models/Cinematic/CapPropRazeChunk.fbx -- the stone the champion knocks out of a wall;
  * Campaign/M2Help/<group>.json -- per frame each figure's root and each prop as a Unity matrix in the group's own
    Blender frame (as export_m2capital.py); a prop not yet in play has a zero scale; Mission2Help.json places them;
  * the figures' entries in Campaign/OpeningCast.json.
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
import export_m2intro as lib  # noqa: E402

FPS = aa.FPS
GROUPS = ("ride", "charge", "stand", "raze", "flee")


def group_build(name):
    if name == "ride":
        import cav_ride_anim as m
        return m.build()
    if name == "charge":
        import cav_charge_anim as m
        return m.build()
    if name == "stand":
        import last_stand_anim as m
        return m.build()
    import orc_raze_anim as m
    return m.build_raze() if name == "raze" else m.build_flee()


def park(objs, tag):
    """Renames a group's leftovers so the next group's make() builds new objects instead of reusing them
    (export_m2capital.park; that module runs its export on import)."""
    for o in objs:
        if o is not None and o.name in bpy.data.objects:
            o.name = "Old%s %s" % (tag, o.name)
            if o.data is not None and hasattr(o.data, "name"):
                o.data.name = "Old%s %s" % (tag, o.data.name)


def sample(actors, props, frames):
    """lib.sample, but a prop hidden on a frame (a stone not yet knocked out) gets a zero scale."""
    from mathutils import Matrix
    sc = bpy.context.scene
    roots = {id(a[2]): [] for a in actors}
    bones = {id(a[2]): [] for a in actors}
    mats = {p.name: [] for p in props}
    zero = Matrix.Diagonal((0, 0, 0, 1))
    for f in range(frames):
        sc.frame_set(f + 1)
        for _, _, arm, _ in actors:
            roots[id(arm)] += lib.unity_m(arm.matrix_world)
            bones[id(arm)].append({pb.name: (tuple(pb.rotation_quaternion), tuple(pb.location)) for pb in arm.pose.bones})
        for p in props:
            m = p.matrix_world
            if p.hide_render:
                m = m @ zero
            mats[p.name] += lib.unity_m(m)
    return roots, bones, mats


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    project = argv[0]
    sc = bpy.context.scene
    sc.render.fps = FPS
    sc.render.fps_base = 1.0
    cast_dir = os.path.join(project, "Assets", "_Project", "Models", "Cinematic", "Cast")
    model_dir = os.path.join(project, "Assets", "_Project", "Models", "Cinematic")
    data_dir = os.path.join(project, "Assets", "_Project", "Campaign", "M2Help")
    os.makedirs(data_dir, exist_ok=True)

    keepers = {}           # export name -> (arm, meshes, their names as built)
    takes = {}             # export name -> [(take name, bones per frame, seconds)]
    exported = set()
    for group in GROUPS:
        actors, props, seconds = group_build(group)
        frames = int(seconds * FPS) + 1
        roots, bones, mats = sample(actors, props, frames)
        out = {"group": group, "fps": FPS, "frames": frames, "seconds": round(seconds, 4), "figures": [], "props": []}
        count = {}
        for name, tk, arm, extra in actors:
            n = count.get(name, 0)
            count[name] = n + 1
            take = tk if n == 0 else "%s%d" % (tk, n)
            out["figures"].append({"figure": name, "take": take, "root": roots[id(arm)]})
            takes.setdefault(name, []).append((take, bones[id(arm)], seconds))
            if name not in keepers:
                meshes = [c for c in arm.children if c.type == 'MESH'] + list(extra)
                keepers[name] = (arm, meshes, [arm.name] + [m.name for m in meshes])
        for p in props:
            piece = "CapProp" + p.name.replace("Cine", "").rstrip("0123456789")      # the stones share one model
            if piece not in exported:
                q = p.copy()
                q.data = p.data.copy()
                sc.collection.objects.link(q)
                q.animation_data_clear()
                q.modifiers.clear()
                q.vertex_groups.clear()
                q.hide_render = False
                q.name = piece
                lib.export_piece(q, model_dir)
                bpy.data.objects.remove(q, do_unlink=True)
                exported.add(piece)
            out["props"].append({"piece": piece, "m": mats[p.name]})
        with open(os.path.join(data_dir, group + ".json"), "w", encoding="utf-8") as f:
            json.dump(out, f, ensure_ascii=False, separators=(",", ":"))
        print("GROUP", group, len(actors), "figures", len(props), "props", frames, "frames")
        park([o for o in bpy.data.objects if o.name.startswith("Cine") or o.name.endswith("Rig")], group)
        for a_ in list(bpy.data.actions):
            bpy.data.actions.remove(a_)

    mine = set()
    for arm, meshes, _ in keepers.values():
        mine.add(arm.name)
        mine |= {m.name for m in meshes}
    for o in list(bpy.data.objects):
        if o.name.startswith("Old") and o.name not in mine:
            bpy.data.objects.remove(o, do_unlink=True)
    entries = []
    for name, (arm, meshes, built) in keepers.items():
        for o, n in zip([arm] + meshes, built):
            o.name = n
        acts, clips = [], []
        for tk, poses, seconds in takes[name]:
            acts.append(lib.take(arm, tk, poses))
            clips.append({"name": tk, "seconds": round(seconds, 4), "loop": False})
        lib.export(name, arm, meshes, cast_dir)
        for a_ in acts:
            bpy.data.actions.remove(a_)
        entries.append({"name": name, "walkSpeed": 0.42, "runSpeed": round(a2.RUN_SPEED, 4), "clips": clips})
        park([arm] + meshes, "Done")
        print("FIGURE", name, len(clips), "takes")

    cast_path = os.path.join(project, "Assets", "_Project", "Campaign", "OpeningCast.json")
    with open(cast_path, encoding="utf-8") as f:
        cast = json.load(f)
    names = {e["name"] for e in entries}
    cast["figures"] = [x for x in cast["figures"] if x["name"] not in names] + entries
    with open(cast_path, "w", encoding="utf-8") as f:
        json.dump(cast, f, ensure_ascii=False, indent=1)
    print("EXPORT M2HELP DONE", len(entries), "figures")


main()
