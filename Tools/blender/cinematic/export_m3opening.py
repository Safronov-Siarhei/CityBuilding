"""Exports mission 3b «Клетка»'s opening scene (the forest ambush; all four animation groups approved 2026-11-23) for
Unity. Never saves the .blend; run it on a COPY of Buildings-Size1-Models.blend.

    blender --background <a COPY of Buildings-Size1-Models.blend> --python export_m3opening.py -- <unity project>

The scene plays far from the mission's map (a set of its own), before the escort begins (CityDefenceMode.Escort.cs).

  * Models/Cinematic/Cast/CapM3<Figure>.fbx -- each figure with its takes IN PLACE ("Column", "Ambush", "Melee",
    "Rally"; copies of one figure in a group get "Column1", "Column2", ...). The prefix M3 keeps them apart from the
    other films' Cap* files (one FBX holds ALL of a figure's takes);
  * Models/Cinematic/CapPropM3CageCart.fbx (the cart without wheels), CapPropM3CageWheelL/R.fbx (they roll),
    CapPropM3Javelin.fbx -- props with a matrix per frame;
  * Models/Cinematic/M3RoadSet.fbx -- the forest road (ground, the felled tree, crags, the game's trees) as ONE static
    mesh in the take's Blender frame;
  * Campaign/M3Opening/<group>.json -- per frame each figure's root and each prop as a Unity matrix, and `cam`: the
    approved review camera per frame (Blender frame: from x, y, z, at x, y, z, lens), for
    Tools/campaign/mission3b_opening_json.py;
  * the figures' entries in Campaign/OpeningCast.json.
"""
import json
import os
import re
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
GROUPS = ("column", "ambush", "melee", "rally")


def join_set(name, objs):
    """The set as ONE static mesh (as export_prologue.py: that module runs its export on import)."""
    sc = bpy.context.scene
    bpy.context.view_layer.update()
    real = []
    for o in objs:
        if o.type != 'MESH' or o.hide_render:
            continue
        c = o.copy()
        c.data = bpy.data.meshes.new_from_object(o.evaluated_get(bpy.context.evaluated_depsgraph_get()))
        c.parent = None
        c.matrix_world = o.matrix_world.copy()
        c.modifiers.clear()
        c.vertex_groups.clear()
        sc.collection.objects.link(c)
        real.append(c)
    bpy.ops.object.select_all(action='DESELECT')
    for c in real:
        c.select_set(True)
    bpy.context.view_layer.objects.active = real[0]
    bpy.ops.object.join()
    out = bpy.context.view_layer.objects.active
    out.name = name
    out.data.name = name
    mats = [m for m in out.data.materials if m is not None]
    keep = next((m for m in mats if m.name.startswith("Buildings")), mats[0] if mats else None)
    for poly in out.data.polygons:
        poly.material_index = 0
    out.data.materials.clear()
    if keep is not None:
        out.data.materials.append(keep)
    bpy.ops.object.select_all(action='DESELECT')
    out.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    print("SET", name, len(real), "pieces, tris", sum(len(poly.vertices) - 2 for poly in out.data.polygons))
    return out


def park(objs, tag):
    for o in objs:
        if o is not None and o.name in bpy.data.objects:
            o.name = "Old%s %s" % (tag, o.name)
            if o.data is not None and hasattr(o.data, "name"):
                o.data.name = "Old%s %s" % (tag, o.data.name)


def export_name(name):
    return "CapM3" + name[3:]


def prop_name(name):
    """CineCageWheelL.001 -> CapPropM3CageWheelL; every javelin one model."""
    base = re.sub(r"\.\d+$", "", name).replace("Cine", "")
    if base.startswith("Javelin"):
        return "CapPropM3Javelin"
    return "CapPropM3" + base.rstrip("0123456789")


def group_module(name):
    if name == "column":
        import column_anim as m
    elif name == "ambush":
        import ambush_anim as m
    elif name == "melee":
        import melee_anim as m
    else:
        import rally_anim as m
    return m


def cam_path(m, name, frames):
    """The group's review camera per frame (its own clock from the take's start)."""
    t0 = getattr(m, "T0", 0.0)
    col = m.column() if name == "melee" else None
    out = []
    for f in range(frames):
        T = t0 + f / FPS
        loc, at, lens = m.camera(T, col) if col is not None else m.camera(T)
        out += [round(loc.x, 3), round(loc.y, 3), round(loc.z, 3), round(at.x, 3), round(at.y, 3), round(at.z, 3), round(lens, 2)]
    return out


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    project = argv[0]
    sc = bpy.context.scene
    sc.render.fps = FPS
    sc.render.fps_base = 1.0
    cast_dir = os.path.join(project, "Assets", "_Project", "Models", "Cinematic", "Cast")
    model_dir = os.path.join(project, "Assets", "_Project", "Models", "Cinematic")
    data_dir = os.path.join(project, "Assets", "_Project", "Campaign", "M3Opening")
    os.makedirs(data_dir, exist_ok=True)

    # the set first: the forest road as one mesh
    import forest_road as fr
    fr.LAYER = 2.0          # the road's layers apart from the grass (thin ones z-fought in Unity: «дороги нет в сцене»)
    set_objs = fr.make()
    pieces = {}
    for o in set_objs:
        if o is None:
            continue
        for x in [o] + list(o.children_recursive):
            pieces[x.name] = x
    lib.export_piece(join_set("M3RoadSet", list(pieces.values())), model_dir)
    park(set_objs, "Set")
    if len(argv) > 1 and argv[1] == "set":
        print("EXPORT M3 SET DONE")
        return

    keepers, takes = {}, {}
    exported = set()
    for group in GROUPS:
        m = group_module(group)
        actors, props, seconds = m.build()
        frames = int(seconds * FPS) + 1
        roots, bones, mats = lib.sample(actors, props, frames)
        out = {"group": group, "fps": FPS, "frames": frames, "seconds": round(seconds, 4), "start": getattr(m, "T0", 0.0),
               "figures": [], "props": []}
        count = {}
        for name, tk, arm, extra in actors:
            name = export_name(name)
            n = count.get(name, 0)
            count[name] = n + 1
            take = tk if n == 0 else "%s%d" % (tk, n)
            out["figures"].append({"figure": name, "take": take, "root": roots[id(arm)]})
            takes.setdefault(name, []).append((take, bones[id(arm)], seconds))
            if name not in keepers:
                meshes = [c for c in arm.children if c.type == 'MESH'] + list(extra)
                keepers[name] = (arm, meshes, [arm.name] + [x.name for x in meshes])
        for p in props:
            piece = prop_name(p.name)
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
        out["cam"] = cam_path(m, group, frames)
        with open(os.path.join(data_dir, group + ".json"), "w", encoding="utf-8") as f:
            json.dump(out, f, ensure_ascii=False, separators=(",", ":"))
        print("GROUP", group, len(actors), "figures", len(props), "props", frames, "frames")
        park([o for o in bpy.data.objects if o.name.startswith("Cine") or o.name.endswith("Rig")], group)
        for a_ in list(bpy.data.actions):
            bpy.data.actions.remove(a_)

    mine = set()
    for arm, meshes, _ in keepers.values():
        mine.add(arm.name)
        mine |= {x.name for x in meshes}
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
    print("EXPORT M3 OPENING DONE", len(entries), "figures")


main()
