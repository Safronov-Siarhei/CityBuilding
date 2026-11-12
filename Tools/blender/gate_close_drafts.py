"""Campaign mission 2: the gate that SHUTS (the user's third playtest, 2026-11-12: «делай модель закрытых ворот и
анимацию их закрытия»). DRAFTS for approval before anything goes into the game. Never saves the .blend.

    blender --background Buildings-Size1-Models.blend --python gate_close_drafts.py -- <out_dir>

The mission's gate is the level-3 Barbican (FortifiedGate1-lvl3), whose doors are only painted on its faces.
Three small pieces go over it:
  * FortifiedGate1-Passage -- a dark arch over each face's painted door: the way through, open;
  * FortifiedGate1-Leaf    -- one oak door leaf (five planks, the top following the arch, two iron bands,
    studs), its origin on the hinge; the game sets two, the second mirrored, on the town face;
  * FortifiedGate1-Grille  -- the portcullis, iron bars, on the field face.
Closing (done in code, no skeleton): the leaves swing from flat open (at right angles to the wall) shut,
the portcullis drops behind them. Frames rendered: open, closing, shut -- from the town side and the field side.
"""
import math
import os
import sys

import bpy
from mathutils import Matrix


HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

# the siege drafts' stage / shoot / mesh_of without their main()
_src = open(os.path.join(HERE, "siege_drafts.py"), encoding="utf-8").read()
_src = _src[:_src.rindex("\nmain()")]
exec(compile(_src, "siege_drafts.py", "exec"), globals())
from kit import Facade  # noqa: E402

GX = 0.38                 # the gatehouse's face, half its width (fortifiedgate1_lvl2.GX)
GATE_W, GATE_H = 0.42, 0.58
LEAF_W = GATE_W / 2 - 0.005
LEAF_T = 0.035
SPRING = GATE_H - GATE_W / 2   # where the arch starts


def passage(b):
    for side, normal in ((1, '+x'), (-1, '-x')):
        f = Facade(b, normal, side * GX)
        f.arch(0, 0.0, GATE_W + 0.02, GATE_H + 0.01, 0.012, 'stone_dark')
        f.arch(0, 0.0, GATE_W - 0.03, GATE_H - 0.02, 0.016, 'iron')


def leaf(b):
    """One leaf: hinge on the origin, the leaf along +y, its face towards +x."""
    planks = 5
    pw = LEAF_W / planks
    r = GATE_W / 2
    for i in range(planks):
        y0, y1 = i * pw, (i + 1) * pw
        mid = (y0 + y1) / 2
        dy = r - mid            # from the arch's centre line (the leaf's free edge is the gate's middle)
        top = SPRING + math.sqrt(max(0.0, r * r - dy * dy)) - 0.01
        b.box(0.0, LEAF_T, y0 + 0.002, y1 - 0.002, 0.0, top, 'wood_mid' if i % 2 == 0 else 'wood_light', bevel=0.004)
    for z in (0.1, 0.32):
        b.box(LEAF_T - 0.002, LEAF_T + 0.01, 0.0, LEAF_W, z, z + 0.035, 'iron', bevel=0.003)
        for k in range(4):
            y = 0.03 + k * (LEAF_W - 0.06) / 3
            b.box(LEAF_T + 0.008, LEAF_T + 0.018, y - 0.008, y + 0.008, z + 0.01, z + 0.026, 'iron')
    b.box(LEAF_T, LEAF_T + 0.012, 0.0, 0.025, 0.04, SPRING, 'iron')   # the hinge post's straps
    b.box(LEAF_T + 0.006, LEAF_T + 0.03, LEAF_W - 0.05, LEAF_W - 0.035, 0.2, 0.24, 'iron')   # the ring


def grille(b):
    """The portcullis in the plane x = 0 (its face towards -x), bottom on the ground, spikes down."""
    w, h = GATE_W - 0.03, GATE_H - 0.03
    bars = 6
    for i in range(bars + 1):
        y = -w / 2 + i * w / bars
        b.box(-0.012, 0.012, y - 0.01, y + 0.01, -0.03, h, 'iron')
        b.box(-0.008, 0.008, y - 0.006, y + 0.006, -0.06, -0.03, 'iron')
    for z in (0.12, 0.3, 0.48):
        b.box(-0.016, 0.016, -w / 2, w / 2, z, z + 0.02, 'iron')


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
    walls = [o for o in (bpy.data.objects.get("Fence1-lvl3-Straight"),) if o is not None]
    for i, w in enumerate(walls):
        w.parent = None
        w.matrix_world = Matrix.Translation((0, 2.0, 0)) @ Matrix.Rotation(math.radians(90), 4, 'Z')
    pas = make("FortifiedGate1-Passage", passage, mat)
    left = make("FortifiedGate1-Leaf", leaf, mat)
    right = left.copy()
    right.data = left.data
    sc.collection.objects.link(right)
    port = make("FortifiedGate1-Grille", grille, mat)
    hinge_x = GX + 0.004
    frames = (("1-open", 90.0, 0.52), ("2-closing", 40.0, 0.28), ("3-shut", 0.0, 0.0))
    for name, angle, lift in frames:
        left.matrix_world = Matrix.Translation((hinge_x, -GATE_W / 2, 0)) @ Matrix.Rotation(math.radians(-angle), 4, 'Z')
        right.matrix_world = (Matrix.Translation((hinge_x, GATE_W / 2, 0)) @ Matrix.Rotation(math.radians(angle), 4, 'Z')
                              @ Matrix.Scale(-1, 4, (0, 1, 0)))
        port.matrix_world = Matrix.Translation((-GX - 0.03, 0, lift))
        bpy.context.view_layer.update()
        objs = [gate, pas, left, right, port]
        shoot(sc, cam, cam_data, objs, os.path.join(OUT, f"gate_{name}-town.png"), yaw_deg=60, pitch_deg=22)
        shoot(sc, cam, cam_data, objs, os.path.join(OUT, f"gate_{name}-field.png"), yaw_deg=-120, pitch_deg=22)


main()
