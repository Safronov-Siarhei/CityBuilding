"""Shared plumbing for the cutscene figures (the user, 2026-11-10: the same blocky palette style as
the King and the townsfolk, more detail; every figure on Ардан's rig so his approved animations
carry over).

A figure script gives a list of PARTS -- (bone name, build function) -- in the joint layout of
ardan.py (hips, chest, head, upper_arm/forearm/thigh/shin/foot .L/.R); `make` builds them into one
mesh in the «Кино» collection with a vertex group per part, ready for ardan_anim.rig.
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import bpy  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Vector  # noqa: E402

COLLECTION = "Кино"
PART_LAYER = "part"

# Ардан's joints (before scale), shared so every figure fits the same rig
SHOULDER, ELBOW, WRIST = (0.096, 0.0, 0.384), (0.114, -0.008, 0.318), (0.117, -0.03, 0.258)
HIP_X, HIP_Z, KNEE_Z, ANKLE_Z = 0.045, 0.2, 0.098, 0.032


def joints(k):
    return (Vector((k * SHOULDER[0], SHOULDER[1], SHOULDER[2])), Vector((k * ELBOW[0], ELBOW[1], ELBOW[2])),
            Vector((k * WRIST[0], WRIST[1], WRIST[2])))


def plain(*colours):
    """Cloth, skin, hair: none of the town's tiles, plaster or planks on them."""
    detail.OVERRIDE.update({c: detail.PLAIN for c in colours})


def limbs(parts_by_bone):
    """[(bone, fn(b))] for both sides from {bone: fn(b, k)}."""
    out = []
    for side, k in (("L", 1), ("R", -1)):
        for bone, fn in parts_by_bone.items():
            out.append((f"{bone}.{side}", (lambda f, kk: (lambda b: f(b, kk)))(fn, k)))
    return out


def lean(b, since, z0, amount):
    """Stoops what was built since `since`: everything above z0 leans forward (-y) by amount per metre."""
    b.bm.verts.ensure_lookup_table()
    for v in list(b.bm.verts)[since:]:
        if v.co.z > z0:
            v.co.y -= (v.co.z - z0) * amount


def ensure_collection():
    coll = bpy.data.collections.get(COLLECTION)
    if coll is None:
        coll = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    return coll


ORC_PALETTE = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(HERE))),
                           "Assets", "_Project", "Models", "Palette", "OrcPalette.png")


_SHARED = None


def material(orc=False):
    """The shared palette material; for orcs a copy drawing the orcs' darker palette (as the game
    does: same UVs, OrcPalette.png)."""
    # once a session: ensure_palette/ensure_atlas REMAKE their images, which would leave an earlier
    # copy (the orcs') pointing at removed images -- it rendered black (2026-11-10)
    global _SHARED
    if _SHARED is None:
        folder = os.path.dirname(bpy.data.filepath)
        img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
        mat = kit.ensure_material(img)
        atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
        detail.build_material_nodes(mat, img, atlas)
        _SHARED = (mat, img)
    mat, img = _SHARED
    if not orc:
        return mat
    orc_mat = bpy.data.materials.get("CineOrcPalette")
    if orc_mat is None:
        orc_mat = mat.copy()
        orc_mat.name = "CineOrcPalette"
        pal = bpy.data.images.load(ORC_PALETTE, check_existing=True)
        for n in orc_mat.node_tree.nodes:
            if n.type == 'TEX_IMAGE' and n.image is img:
                n.image = pal
    return orc_mat


def make(name, parts, scale, location=(0, 0, 0), post=None, orc=False):
    """Builds the parts (each vertex remembers its part), scales, and installs the object."""
    b = Builder()
    layer = b.bm.verts.layers.int.new(PART_LAYER)
    for i, (_, fn) in enumerate(parts):
        fn(b)
        for v in b.bm.verts:
            if v[layer] == 0:
                v[layer] = i + 1
    if post:
        post(b)
    for v in b.bm.verts:
        v.co *= scale
    me = b.to_mesh(name)
    me.materials.append(material(orc))
    detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
    obj = bpy.data.objects.get(name)
    if obj is None:
        obj = bpy.data.objects.new(name, me)
        ensure_collection().objects.link(obj)
    else:
        old = obj.data
        obj.data = me
        if old.users == 0:
            bpy.data.meshes.remove(old)
    obj.location = location
    obj.vertex_groups.clear()
    tags = [0] * len(me.vertices)
    me.attributes[PART_LAYER].data.foreach_get("value", tags)
    groups = [obj.vertex_groups.new(name=n) for n, _ in parts]
    for i, t in enumerate(tags):
        groups[t - 1].add([i], 1.0, 'REPLACE')
    print(f"MODEL {name} tris={sum(len(p.vertices) - 2 for p in me.polygons)}")
    return obj


def grow_part(b, parts, bone, factor, pivot):
    """Scales one part about `pivot` (a child's bigger head) before the figure is scaled."""
    layer = b.bm.verts.layers.int.get(PART_LAYER)
    index = [n for n, _ in parts].index(bone) + 1
    p = Vector(pivot)
    for v in b.bm.verts:
        if v[layer] == index:
            v.co = p + (v.co - p) * factor
