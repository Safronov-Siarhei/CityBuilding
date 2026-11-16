"""Exports mission 2's scene 2 «Гонец у короля» (sets and all three animation groups approved 2026-11-15) for Unity.
Never saves the .blend.

    blender --background <Buildings-Size1-Models.blend> --python export_m2capital.py -- <unity project>

One run does it all (a figure acts in several groups -- the rider in all three -- and its FBX must carry every take):

  * Models/Cinematic/M2CapitalSet.fbx, M2ThroneSet.fbx -- the two sets, each ONE static mesh: the capital
    (capital.make: the game's level-5 models laid out, ground, moat, stairs) with its still people (the wall, gate
    and stair guards, the market's folk and stalls -- not the ones the takes move) and the throne room
    (throne_room.make) with its still court (throne_anim.extras);
  * Models/Cinematic/Cast/Cap<Figure>.fbx -- each figure with its takes IN PLACE, baked into CineClips by setup;
  * Models/Cinematic/CapProp<Name>.fbx -- the props (the cart, an apple, the advisor's scroll);
  * Campaign/M2Capital/<group>.json -- per frame each figure's root and each prop as a Unity matrix in the
    group's own Blender frame (as export_m2intro.py); Mission2Capital.json places the frames in the sets;
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
import export_m2intro as lib  # noqa: E402  (its helpers; its main() runs only as a script)

FPS = aa.FPS

# the takes' figures -> one export name each (Cap*: scene 1's M2* files keep their own takes)
EXPORT = {"M2Horse": "CapHorse", "M2Rider": "CapRider", "M2Guard": "CapGuard", "M2King": "CapKing",
          "M2Advisor": "CapAdvisor", "M2Lady": "CapLady", "M2Noble": "CapNoble", "M2Peasant": "CapPeasant",
          "M2PeasantWoman": "CapPeasantWoman", "M2Merchant": "CapMerchant", "M2Boy": "CapBoy", "M2Girl": "CapGirl"}
GROUPS = ("arrive", "street", "throne")


def group_build(name):
    if name == "arrive":
        import castle_arrive_anim as m
        actors, props, seconds = m.build()
    elif name == "street":
        import street_gallop_anim as m
        actors, props, seconds = m.build()
    else:
        import throne_anim as m
        actors, props, seconds, room = m.build()
        bpy.data.objects.remove(room, do_unlink=True)            # the set goes separately
    return actors, props, seconds


def park(objs, tag):
    """Renames a group's leftovers so the next group's make() builds new objects instead of reusing them."""
    for o in objs:
        if o is not None and o.name in bpy.data.objects:
            o.name = "Old%s %s" % (tag, o.name)
            if o.data is not None and hasattr(o.data, "name"):
                o.data.name = "Old%s %s" % (tag, o.data.name)


def join_set(name, objs):
    """The set as ONE static mesh: every piece's copy made real, joined, one palette material."""
    sc = bpy.context.scene
    # the copies' matrix_world is stale until the depsgraph runs: joined as they were, the walls, roads, gate,
    # trees and most houses landed on their models' library spots (the user, 2026-11-16: «не хватает зданий»)
    bpy.context.view_layer.update()
    real = []
    for o in objs:
        if o.type != 'MESH':
            continue
        c = o.copy()
        # the mesh as seen: the still people keep their poses (their armature applied), not the rest pose
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
    # one material: every piece draws the same palette (export_fbx's "Buildings")
    mats = [m for m in out.data.materials if m is not None]
    keep = next((m for m in mats if m.name.startswith("Buildings")), mats[0] if mats else None)
    for p in out.data.polygons:
        p.material_index = 0
    out.data.materials.clear()
    if keep is not None:
        out.data.materials.append(keep)
    bpy.ops.object.select_all(action='DESELECT')
    out.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    print("SET", name, "tris", sum(len(p.vertices) - 2 for p in out.data.polygons))
    return out


def capital_set():
    import capital
    objs = capital.make()
    keep_people = []
    for f in capital.populate():
        n = f.name
        x = f.location.x / capital.S
        y = f.location.y / capital.S
        still = (n.startswith(("Wall guard", "Gate guard", "Stair guard", "CineStalls", "CineCart"))
                 or (" crowd " in n and (x < 0 or abs(y) >= 2.3)))
        if still:
            keep_people.append(f)
    return join_set("M2CapitalSet", objs + keep_people)


def throne_set():
    import throne_room
    import throne_anim
    room = throne_room.make()
    return join_set("M2ThroneSet", [room] + throne_anim.extras())


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    project = argv[0]
    sc = bpy.context.scene
    sc.render.fps = FPS
    sc.render.fps_base = 1.0
    cast_dir = os.path.join(project, "Assets", "_Project", "Models", "Cinematic", "Cast")
    model_dir = os.path.join(project, "Assets", "_Project", "Models", "Cinematic")
    data_dir = os.path.join(project, "Assets", "_Project", "Campaign", "M2Capital")
    os.makedirs(data_dir, exist_ok=True)

    # the takes, group by group: sampled, then their objects parked out of the next group's way
    keepers = {}           # export name -> (arm, meshes, their names as built)
    takes = {}             # export name -> [(take name, bones per frame, seconds)]
    exported = set()
    for group in GROUPS:
        actors, props, seconds = group_build(group)
        frames = int(seconds * FPS) + 1
        roots, bones, mats = lib.sample(actors, props, frames)
        out = {"group": group, "fps": FPS, "frames": frames, "seconds": round(seconds, 4), "figures": [], "props": []}
        for exp, tk, arm, extra in actors:
            name = EXPORT[exp]
            out["figures"].append({"figure": name, "take": tk, "root": roots[id(arm)]})
            takes.setdefault(name, []).append((tk, bones[id(arm)], seconds))
            if name not in keepers:
                meshes = [c for c in arm.children if c.type == 'MESH'] + list(extra)
                keepers[name] = (arm, meshes, [arm.name] + [m.name for m in meshes])
        for p in props:
            piece = "CapProp" + p.name.replace("Cine", "").rstrip("0123456789")      # the apples share one model
            if piece not in exported:
                q = p.copy()
                q.data = p.data.copy()
                sc.collection.objects.link(q)
                q.animation_data_clear()
                q.modifiers.clear()
                q.vertex_groups.clear()
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

    # every figure once, with all its takes, under the names it was built with
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

    # the sets
    for make in (capital_set, throne_set):
        lib.export_piece(make(), model_dir)

    cast_path = os.path.join(project, "Assets", "_Project", "Campaign", "OpeningCast.json")
    with open(cast_path, encoding="utf-8") as f:
        cast = json.load(f)
    names = {e["name"] for e in entries}
    cast["figures"] = [x for x in cast["figures"] if x["name"] not in names] + entries
    with open(cast_path, "w", encoding="utf-8") as f:
        json.dump(cast, f, ensure_ascii=False, indent=1)
    print("EXPORT M2CAPITAL DONE", len(entries), "figures")


main()
