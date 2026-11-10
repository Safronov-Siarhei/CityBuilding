"""Exports the opening cutscene's set pieces into the Unity project (2026-11-10, the user: the film
moves to Unity). Never saves the .blend.

    blender --background <Buildings-Size1-Models.blend> --python export_cine.py -- <unity project>

One FBX per piece in Assets/_Project/Models/Cinematic, each with its origin at its own pivot (the
object's placement dropped, as export_fbx does), and one placement file,
Assets/_Project/Campaign/OpeningSet.json, saying where each piece stands in the VILLAGE's frame
(the square's centre at 0, as cutscene.py lays it out). The ruin stones carry two placements: as
they lie, and where the arch puts them. Positions and rotations are already in Unity's axes:
Blender (x, y, z) is Unity (x, z, y) (see export_fbx.py), so a rotation R becomes P R P with P the
y/z swap.
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

import church  # noqa: E402
import figure as fg  # noqa: E402
import portal  # noqa: E402
import townsfolk_anim as ta  # noqa: E402
import bell_anim as ba  # noqa: E402
import cutscene as cs  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

S = fg.SET_SCALE
CHURCH_AT = Vector((0.0, 7.4, 0))        # cutscene.py's village
MARKET_AT = Vector((1.0, -5.2, 0))
P = Matrix(((1, 0, 0), (0, 0, 1), (0, 1, 0)))


def unity(m):
    """A Blender world matrix as Unity position + rotation (x, y, z, w) + scale."""
    loc, rot, scale = m.decompose()
    r = P @ rot.to_matrix() @ P
    q = r.to_quaternion()
    return {"p": [round(loc.x, 5), round(loc.z, 5), round(loc.y, 5)],
            "r": [round(q.x, 6), round(q.y, 6), round(q.z, 6), round(q.w, 6)],
            "s": [round(scale.x, 5), round(scale.z, 5), round(scale.y, 5)]}


def double_sided(o):
    """Every face twice, the copy turned round: the game's building shader culls back faces, and
    the church has faces modelled facing in (the belfry's walls showed holes in Unity, 2026-11-10)."""
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(o.data)
    dup = bmesh.ops.duplicate(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:])
    bmesh.ops.reverse_faces(bm, faces=[g for g in dup["geom"] if isinstance(g, bmesh.types.BMFace)])
    bm.to_mesh(o.data)
    bm.free()


def export(o, folder):
    saved = o.matrix_world.copy()
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
    o.matrix_world = saved
    print("EXPORTED", path)


def main():
    project = sys.argv[sys.argv.index("--") + 1:][0]
    folder = os.path.join(project, "Assets", "_Project", "Models", "Cinematic")
    os.makedirs(folder, exist_ok=True)
    pieces = {}
    stones = {}
    village = Matrix.Identity(4)

    # the church without its bell (the bell, its clapper hang at the yoke and can swing)
    ch = church.make(location=tuple(CHURCH_AT), with_bell=False)
    bell, clap, rope, loc = ba.pieces(ch)
    bpy.context.view_layer.update()            # matrix_world lags a set location until this
    pieces[ch.name] = ch.matrix_world.copy()
    pieces[bell.name] = bell.matrix_world.copy()
    pieces[clap.name] = clap.matrix_world.copy()
    bpy.data.objects.remove(rope, do_unlink=True)

    # the ruins on the square: the platform, the swirl and the fifteen stones
    plat, sts, sw = portal.make((0, 0, 0))
    bpy.context.view_layer.update()
    pieces[plat.name] = plat.matrix_world.copy()
    pieces[sw.name] = sw.matrix_world.copy()
    for s in sts:
        arch = Matrix([s["arch"][i * 4:(i + 1) * 4] for i in range(4)])
        stones[s.name] = {"ruin": unity(s.matrix_world), "arch": unity(arch)}

    # the market: the fruit stall, the cloth stall (frame, what stands on the ground, its bolts), the cart
    fruit_st, cloth, ground, bolts, fruit = ta.stall_pieces()
    body, wheels = ta.cart_pieces()
    bpy.context.view_layer.update()
    hub = Matrix.Translation(MARKET_AT)
    for o in [fruit_st, cloth, ground, fruit] + [b for b, _ in bolts]:
        pieces[o.name] = hub @ o.matrix_world
    cart_at = hub @ Matrix.Translation((1.5, ta.LANE_Y - 0.3, 0)) @ Matrix.Rotation(math.pi, 4, 'Z')
    pieces[body.name] = cart_at
    for i, (wh, y) in enumerate(wheels):
        pieces[wh.name] = cart_at @ Matrix.Translation((0, y, 0.2 * ta.K))

    out = {"pieces": [], "stones": []}
    double_sided(ch)
    for name, m in pieces.items():
        export(bpy.data.objects[name], folder)
        out["pieces"].append(dict(name=name, **unity(village @ m)))
    # each stone's rune as its own glowing insert, in the stone's own frame (Unity lights them in turn)
    glows = cs.rune_glows(sts)
    for s, (g, _) in zip(sts, glows):
        g.name = "CineRuneGlow_" + s.name.split("_", 1)[1]
        g.data.name = g.name
    for name, poses in stones.items():
        export(bpy.data.objects[name], folder)
        glow = "CineRuneGlow_" + name.split("_", 1)[1]
        export(bpy.data.objects[glow], folder)
        out["stones"].append(dict(name=name, glow=glow, **poses))
    data = os.path.join(project, "Assets", "_Project", "Campaign")
    os.makedirs(data, exist_ok=True)
    with open(os.path.join(data, "OpeningSet.json"), "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, indent=1)
    print("EXPORT CINE DONE", len(out["pieces"]), "pieces", len(out["stones"]), "stones")


main()
