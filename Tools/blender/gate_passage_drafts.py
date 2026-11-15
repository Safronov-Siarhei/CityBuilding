"""Campaign mission 2: the gatehouse with a REAL way through (the user, 2026-11-15: «мне не нравится как выглядят
ворота с внешней стороны, сейчас там должен быть виден проход в город»). DRAFTS for approval. Never saves the .blend.

    blender --background Buildings-Size1-Models.blend --python gate_passage_drafts.py -- <out_dir>

The level-3 gatehouse (FortifiedGate1-lvl3, the red-roofed barbican) is solid: its doorway is painted on, and the
game laid a dark arch over each face (FortifiedGate1-Passage). Here a copy gets a vaulted passage CUT through it
along x (the gate's way, the arch's width and height: gate_close_drafts GATE_W / GATE_H), its walls and vault in
dark stone, so the town shows through when the leaves stand open. The approved leaves and portcullis hang as before.
Frames: open and shut, from the field and from the town, a house behind the gate to show the way through.
"""
import math
import os
import sys

import bpy
from mathutils import Matrix

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

_src = open(os.path.join(HERE, "gate_close_drafts.py"), encoding="utf-8").read()
_src = _src[:_src.rindex("if len(argv) > 1")]        # its own main / export not run
exec(compile(_src, "gate_close_drafts.py", "exec"), globals())

VAULT_SEGS = 10


def cutter(b):
    """The passage's volume: straight sides to the arch's spring, a round vault over them, through and past both faces."""
    r = GATE_W / 2
    pts = [(-r, -0.02), (r, -0.02)]
    for i in range(VAULT_SEGS + 1):
        a = math.pi * i / VAULT_SEGS
        pts.append((r * math.cos(a), SPRING + r * math.sin(a)))
    pts = pts[:2] + pts[2:]
    b.prism(pts, (-1.5, 0, 0), (0, 1, 0), (0, 0, 1), 3.0, 'stone_dark', back_cap=True)   # the barbican is deeper than the old gatehouse


def floor(b):
    """Flagstones down the passage, a step lower than the faces' sills."""
    r = GATE_W / 2
    b.box(-GX - 0.02, GX + 0.02, -r, r, 0.0, 0.012, 'stone_mid')
    for i in range(6):
        x = -GX + (i + 0.5) * (2 * GX / 6)
        b.box(x - 0.004, x + 0.004, -r, r, 0.012, 0.014, 'stone_dark')


def cut(gate, mat):
    open_ = gate.copy()
    open_.data = gate.data.copy()
    open_.name = "FortifiedGate1-lvl3-Open"
    bpy.context.scene.collection.objects.link(open_)
    open_.matrix_world = Matrix.Identity(4)
    c = make("FortifiedGate1-PassageCut", cutter, mat)
    c.matrix_world = Matrix.Identity(4)
    # outward normals, merged corners: a prism's winding left it inside out and the cut ADDED the volume
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(c.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(c.data)
    bm.free()
    mod = open_.modifiers.new("Cut", 'BOOLEAN')
    mod.operation = 'DIFFERENCE'
    mod.solver = 'EXACT'
    mod.object = c
    bpy.context.view_layer.objects.active = open_
    bpy.ops.object.select_all(action='DESELECT')
    open_.select_set(True)
    bpy.ops.object.modifier_apply(modifier="Cut")
    bpy.data.objects.remove(c, do_unlink=True)
    tris = sum(len(p.vertices) - 2 for p in open_.data.polygons)
    xs = [v.co.x for v in gate.data.vertices]
    print("MODEL", open_.name, "tris", tris, "gate x %.2f..%.2f" % (min(xs), max(xs)))
    return open_


def main():
    os.makedirs(OUT, exist_ok=True)
    sc, cam, cam_data, ground = stage()
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    ground.hide_render = False
    gate = bpy.data.objects.get("FortifiedGate1-lvl3")
    if gate is None:
        print("MISSING FortifiedGate1-lvl3")
        return
    gate.parent = None
    gate.matrix_world = Matrix.Identity(4)
    # the barbican HAS a vaulted way through (x -0.45..0.45, walls at y +-0.26, vault at 0.68): only the dark
    # overlay the game laid on its faces (FortifiedGate1-Passage) hid it -- shown here as it is, no cut
    open_ = gate
    fl = make("FortifiedGate1-PassageFloor", floor, mat)
    left = make("FortifiedGate1-Leaf", leaf, mat)
    right = left.copy()
    right.data = left.data
    sc.collection.objects.link(right)
    port = make("FortifiedGate1-Grille", grille, mat)
    # a house in the town behind the gate (+x is the town side: the leaves open towards it)
    house = None
    for name in ("Cottage1-lvl1", "House1-lvl1", "Hovel1-lvl1", "Cottage1-lvl2"):
        src = bpy.data.objects.get(name)
        if src is not None:
            house = src.copy()
            house.parent = None
            sc.collection.objects.link(house)
            house.matrix_world = Matrix.Translation((3.2, 0.0, 0.0)) @ Matrix.Rotation(math.radians(90), 4, 'Z')
            break
    hinge_x = GX + 0.004
    frames = (("open", 90.0, 0.52), ("shut", 0.0, 0.0))
    for name, angle, lift in frames:
        left.matrix_world = Matrix.Translation((hinge_x, -GATE_W / 2, 0)) @ Matrix.Rotation(math.radians(-angle), 4, 'Z')
        right.matrix_world = (Matrix.Translation((hinge_x, GATE_W / 2, 0)) @ Matrix.Rotation(math.radians(angle), 4, 'Z')
                              @ Matrix.Scale(-1, 4, (0, 1, 0)))
        port.matrix_world = Matrix.Translation((-GX - 0.03, 0, lift))
        bpy.context.view_layer.update()
        objs = [open_, fl, left, right, port] + ([house] if house else [])
        shoot(sc, cam, cam_data, [open_, fl, left, right, port], os.path.join(OUT, f"passage_{name}-field.png"),
              yaw_deg=-100, pitch_deg=12)
        shoot(sc, cam, cam_data, [open_, fl, left, right, port], os.path.join(OUT, f"passage_{name}-through.png"),
              yaw_deg=-90, pitch_deg=4)
        shoot(sc, cam, cam_data, [open_, fl, left, right, port], os.path.join(OUT, f"passage_{name}-town.png"),
              yaw_deg=80, pitch_deg=12)
        for o in objs:
            o.hide_render = False


main()
