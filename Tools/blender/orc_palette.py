"""Writes the orcs' gloomy island palette (terrain.ORC_PALETTE) into the Unity project: the terrain
palette's layout, every cell recoloured. No .blend needed.

    blender --background --factory-startup --python orc_palette.py -- <unity project>
"""
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import terrain  # noqa: E402

project = sys.argv[sys.argv.index("--") + 1:][0]
w, h = terrain.COLS * terrain.CELL, terrain.ROWS * terrain.CELL
img = bpy.data.images.new("OrcIslandPalette", w, h, alpha=False)
px = [0.0] * (w * h * 4)
for i, (name, hx) in enumerate(terrain.TERRAIN_PALETTE):
    c, r = i % terrain.COLS, i // terrain.COLS
    rgb = kit._rgb(terrain.ORC_PALETTE.get(name, hx))
    for yy in range(terrain.CELL):
        for xx in range(terrain.CELL):
            x, y = c * terrain.CELL + xx, (terrain.ROWS - 1 - r) * terrain.CELL + yy
            k = (y * w + x) * 4
            px[k:k + 4] = [rgb[0], rgb[1], rgb[2], 1.0]
img.pixels = px
out = os.path.join(project, "Assets", "_Project", "Models", "Palette", "OrcIslandPalette.png")
img.filepath_raw = out
img.file_format = 'PNG'
img.save()
print("WROTE", out)
