"""Exports the level-3 prologue «Пленные» (all three animation groups approved 2026-11-22/23) for Unity.
Never saves the .blend; run it on a COPY (captives_glade loads model scripts whose main() saves).

    blender --background <a COPY of Buildings-Size1-Models.blend> --python export_prologue.py -- <unity project>

The square plays in mission 2's own town (no set); the cage cart stands in its street; the glade is a far set.

  * Models/Cinematic/Cast/CapL3<Figure>.fbx -- each figure with its takes IN PLACE ("Cage", "Square", "Glade"; copies
    of one figure in a group get "Square1", "Square2", ...). The prefix L3 keeps them apart from mission 2's Cap*
    files: one FBX holds ALL of a figure's takes, so a shared name would wipe mission 2's;
  * Models/Cinematic/CapPropL3CageCart.fbx, CapPropL3CageHorse.fbx -- the cart (it rocks) and its two horses;
  * Models/Cinematic/L3GladeSet.fbx (ground, trees, stones), L3GladeOrcSet.fbx (the camp and the cages: the game
    draws Models/Orcs in the orcs' palette) and L3GladeIsleSet.fbx (dead trees, skull stakes: the swamp palette) --
    one static mesh each, in the glade's Blender frame; ModelImportDefaults gives each its palette by name;
  * Campaign/L3Prologue/<group>.json -- per frame each figure's root and each prop as a Unity matrix (export_m2help);
    glade.json also carries the push-in camera (`cam`, Blender frame) for Tools/campaign/prologue_json.py;
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
GROUPS = ("cage", "square", "glade")
ISLE = ("OrcDeadTree", "OrcSkullStake")          # ModelImportDefaults.OrcIslandModel: the swamp palette in the game


def export_name(name, arm):
    """CapX -> CapL3X; the square's horses all come as CapWarhorse: the grey ones by their mesh."""
    if name == "CapWarhorse" and any("Grey" in c.name for c in arm.children):
        name = "CapWarhorseGrey"
    return "CapL3" + name[3:]


def join_set(name, objs):
    """The set as ONE static mesh (export_m2help.join_set; that module runs its export on import)."""
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


def glade_sets(objs, model_dir):
    """The glade's pieces split by the palette the game draws them in, each joined and exported."""
    base, orc, isle = [], [], []
    for o in objs:
        src = o.name[len("Glade "):] if o.name.startswith("Glade ") else o.name
        if src.startswith(ISLE):
            isle.append(o)
        elif src.startswith("Orc") or src.startswith("cage"):
            orc.append(o)
        else:
            base.append(o)
    for name, part in (("L3GladeSet", base), ("L3GladeOrcSet", orc), ("L3GladeIsleSet", isle)):
        if part:
            lib.export_piece(join_set(name, part), model_dir)


def glade_cam():
    import glade_anim as g
    (p0, a0, l0), (p1, a1, l1) = g.CAM_FROM, g.cam_to()
    r = lambda v: [round(v.x, 3), round(v.y, 3), round(v.z, 3)]       # noqa: E731
    return {"t0": 0.8, "t1": 6.6, "from": r(p0), "at": r(a0), "lens": l0, "from2": r(p1), "at2": r(a1), "lens2": l1}


def group_build(name):
    if name == "cage":
        import cage_anim as m
        return m.build()
    if name == "square":
        import square_anim as m
        return m.build()
    import glade_anim as m
    actors, props, cam = m.build()
    return actors, props, m.SECONDS


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    project = argv[0]
    sc = bpy.context.scene
    sc.render.fps = FPS
    sc.render.fps_base = 1.0
    cast_dir = os.path.join(project, "Assets", "_Project", "Models", "Cinematic", "Cast")
    model_dir = os.path.join(project, "Assets", "_Project", "Models", "Cinematic")
    data_dir = os.path.join(project, "Assets", "_Project", "Campaign", "L3Prologue")
    os.makedirs(data_dir, exist_ok=True)

    keepers, takes = {}, {}
    exported = set()
    for group in GROUPS:
        actors, props, seconds = group_build(group)
        frames = int(seconds * FPS) + 1
        moving = [] if group == "glade" else props            # the glade's props are its set: static, joined
        roots, bones, mats = lib.sample(actors, moving, frames)
        out = {"group": group, "fps": FPS, "frames": frames, "seconds": round(seconds, 4), "figures": [], "props": []}
        count = {}
        for name, tk, arm, extra in actors:
            name = export_name(name, arm)
            n = count.get(name, 0)
            count[name] = n + 1
            take = tk if n == 0 else "%s%d" % (tk, n)
            out["figures"].append({"figure": name, "take": take, "root": roots[id(arm)]})
            takes.setdefault(name, []).append((take, bones[id(arm)], seconds))
            if name not in keepers:
                meshes = [c for c in arm.children if c.type == 'MESH'] + list(extra)
                keepers[name] = (arm, meshes, [arm.name] + [m.name for m in meshes])
        for p in moving:
            piece = "CapPropL3" + p.name.replace("Cine", "").rstrip("0123456789")      # the two horses share one model
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
        if group == "glade":
            glade_sets(props, model_dir)
            out["cam"] = glade_cam()
        with open(os.path.join(data_dir, group + ".json"), "w", encoding="utf-8") as f:
            json.dump(out, f, ensure_ascii=False, separators=(",", ":"))
        print("GROUP", group, len(actors), "figures", len(moving), "props", frames, "frames")
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
    print("EXPORT PROLOGUE DONE", len(entries), "figures")


main()
