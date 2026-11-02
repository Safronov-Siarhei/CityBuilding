"""The Tax House (the user, 2026-11-02: «Налоговый дом» in the Город tab -- each level raises what a
citizen pays a day; its levels are bought with crystals). 2 x 2 cells, front to -y:
a stone ground floor with an arched door and a counter window, a plastered upper floor, a gold coin
on a bracket over the door, stacks of coins and a strongbox by the steps.

- lvl1: a timber upper floor under straw; one coin sign.
- lvl2: plastered upper floor under red tiles, a chimney, a second coin stack.
- lvl3: a side wing with the counting room's bay window, a weather vane.
- lvl4: a slate roof, a corner tower with a blue cone, a banner.
- lvl5: gilded ridge, gold cone, a big coin on the gable.

    blender --background Buildings-Size1-Models.blend --python taxhouse.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Vector  # noqa: E402

COLLECTION = "Налоговый дом"


def coin(b, x, y, z, r, face=-1):
    """A coin standing up, its face to `face` y."""
    b.hcylinder('y', (x, y, z), 0.025, r, 'gold', sides=10)
    b.hcylinder('y', (x, y + face * 0.02, z), 0.012, r * 0.6, 'sand', sides=8)


def stack(b, x, y, n):
    for k in range(n):
        b.cylinder(x, y, 0.06 + k * 0.035, 0.09 + k * 0.035, 0.07, 'gold', sides=8)


def tax_house(level):
    b = Builder()
    # the plinth and the stone ground floor
    b.box(-0.95, 0.95, -0.95, 0.95, 0.0, 0.08, 'cobble')
    b.box(-0.75, 0.75, -0.6, 0.65, 0.08, 1.0, 'stone_mid', bevel=0.02)
    for x in (-0.75, 0.75):                                           # corner quoins
        for k in range(4):
            b.box(x - 0.05, x + 0.05, -0.65, -0.55, 0.1 + k * 0.23, 0.26 + k * 0.23, 'stone_light')
    b.box(-0.2, 0.2, -0.63, -0.6, 0.08, 0.7, 'door')                   # the door
    b.cylinder(0, -0.615, 0.62, 0.68, 0.2, 'stone_light', sides=8)
    b.box(0.35, 0.62, -0.63, -0.6, 0.4, 0.7, 'window')                 # the counter window
    b.box(0.32, 0.65, -0.72, -0.6, 0.36, 0.4, 'wood_dark')              # its sill-counter
    b.box(-0.62, -0.35, -0.63, -0.6, 0.4, 0.7, 'window')
    b.box(-0.3, 0.3, -0.9, -0.62, 0.08, 0.14, 'stone_light')            # the step
    # the upper floor
    upper = 'wood_light' if level == 1 else 'plaster'
    b.box(-0.8, 0.8, -0.65, 0.7, 1.0, 1.7, upper, bevel=0.015)
    if level == 1:
        for x in (-0.8, -0.3, 0.3, 0.8):
            b.box(x - 0.04, x + 0.04, -0.68, -0.64, 1.0, 1.7, 'wood_dark')
    else:
        b.box(-0.82, 0.82, -0.68, 0.72, 0.98, 1.04, 'wood_dark')        # the jetty beam
    for x in (-0.45, 0.45):
        b.box(x - 0.14, x + 0.14, -0.68, -0.65, 1.2, 1.5, 'window_light')
        b.box(x - 0.17, x + 0.17, -0.7, -0.65, 1.17, 1.2, 'wood_dark')
    # the roof, its ridge along x
    roof, roof_dark = {1: ('roof_straw', 'roof_straw_dark'), 2: ('roof_red', 'roof_red_dark'),
                       3: ('roof_red', 'roof_red_dark'), 4: ('roof_slate', 'roof_slate_dark'),
                       5: ('roof_slate', 'gold')}[level]
    b.gable_roof(0, 0.025, 'x', 0.85, 0.72, 1.7, 2.35, roof, roof_dark)
    if level >= 5:
        b.box(-0.9, 0.9, -0.04, 0.09, 2.38, 2.46, 'gold')                # gilded ridge
    # the coin on its bracket over the door
    b.rod((0, -0.66, 1.08), (0, -0.92, 1.08), 0.03, 'iron')
    coin(b, 0, -0.92, 0.9, 0.15)
    # coins and the strongbox by the steps
    stack(b, 0.55, -0.82, 3)
    b.box(-0.7, -0.4, -0.88, -0.66, 0.08, 0.28, 'wood_dark', bevel=0.01)
    b.box(-0.71, -0.39, -0.89, -0.65, 0.18, 0.21, 'iron')
    b.box(-0.58, -0.52, -0.9, -0.88, 0.14, 0.22, 'gold')
    if level >= 2:
        stack(b, 0.7, -0.75, 5)
        b.box(0.45, 0.65, 0.2, 0.4, 1.9, 2.75, 'stone_dark')              # the chimney
        b.box(0.43, 0.67, 0.18, 0.42, 2.75, 2.8, 'stone_light')
    if level >= 3:
        # the side wing with a bay window on the counting room
        b.box(0.75, 0.95, -0.4, 0.5, 0.08, 1.2, 'stone_mid')
        b.box(0.93, 1.0, -0.15, 0.25, 0.4, 0.9, 'window_light')
        b.box(0.73, 1.0, -0.45, 0.55, 1.2, 1.28, 'wood_dark')
        b.cylinder(-0.6, 0.0, 2.3, 2.75, 0.015, 'iron', sides=4)       # the vane
        b.prism([(0, 2.6), (0.2, 2.6), (0.14, 2.66), (0.2, 2.72), (0, 2.72)], (-0.6, 0, 0), (1, 0, 0), (0, 0, 1),
                0.01, 'iron', back_cap=True)
    if level >= 4:
        # the corner tower with a cone and a banner
        b.cylinder(-0.75, -0.55, 0.08, 2.2, 0.24, 'stone_light', sides=8)
        b.box(-0.8, -0.7, -0.8, -0.78, 1.5, 1.75, 'window_light')
        cone = 'gold' if level >= 5 else 'roof_blue'
        b.pyramid(-0.75, -0.55, 2.2, 0.3, 0.3, 2.9, cone, sides=8)
        b.cylinder(-0.75, -0.55, 2.85, 3.15, 0.012, 'iron', sides=4)
        b.prism([(0, 2.95), (0.22, 2.95), (0.22, 3.12), (0, 3.12)], (-0.75, -0.55, 0), (1, 0, 0), (0, 0, 1),
                0.01, 'banner_blue', back_cap=True)
    if level >= 5:
        coin(b, 0, -0.74, 2.0, 0.17)                                     # the big coin on the gable
    return b


MODELS = {f"TaxHouse1-lvl{v}": (lambda v=v: tax_house(v)) for v in range(1, 6)}


def main():
    coll = bpy.data.collections.get(COLLECTION)
    if coll is None:
        coll = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    for i, (name, build) in enumerate(MODELS.items()):
        me = build().to_mesh(name)
        me.materials.append(mat)
        detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
        obj = bpy.data.objects.get(name)
        if obj is None:
            obj = bpy.data.objects.new(name, me)
            coll.objects.link(obj)
        else:
            old = obj.data
            obj.data = me
            if old.users == 0:
                bpy.data.meshes.remove(old)
        obj.location = Vector((60.0 + i * 4.0, -52.0, 0.0))
        print(f"MODEL {name}: tris={sum(len(p.vertices) - 2 for p in obj.data.polygons)}")
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


main()
