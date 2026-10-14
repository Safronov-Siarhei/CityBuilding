"""Prints every finished model's triangle count, one `TRIS <name> <count>` line each. Never saves.

    blender --background Buildings-Size1-Models.blend --python report_tris.py

For comparing a set-wide change (a shared detail in details.py) against the budget in
project memory: housing 500-1000, services 1000-1500, landmarks 3000-4000, fence pieces <= 300.
"""
import bpy

for o in sorted(bpy.data.objects, key=lambda x: x.name):
    if o.type != 'MESH' or o.name.endswith("-size"):
        continue
    if any(c.name == "Габариты" for c in o.users_collection):
        continue
    if not o.data.materials or o.data.materials[0] is None or o.data.materials[0].name != "Buildings":
        continue
    print("TRIS", o.name, sum(len(p.vertices) - 2 for p in o.data.polygons))
