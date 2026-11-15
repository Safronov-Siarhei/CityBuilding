"""Campaign mission 2: the tower door's marker as SPARKS AT THE THRESHOLD instead of the flat yellow disc (the user,
2026-11-15: «вместо жёлтых кругов сделаем какой-то эффект, сейчас это выглядит дёшево» -> picked «Искры у порога»).
DRAFT for approval. Never saves the .blend.

    blender --background Buildings-Size1-Models.blend --python tower_door_fx_drafts.py -- <out_dir>

The look (one frame of it): a thin ring of short gold dashes on the ground before the door (radius 0.32 cells, the
step that takes Ардан up), slowly turning and breathing in the game, and small gold sparks rising from inside it,
fading as they climb. 1 Blender unit = 1 cell; the door faces -y (the town side, Unity's back).
"""
import math
import os
import random
import sys

import bpy
from mathutils import Matrix

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
_src = open(os.path.join(HERE, "siege_drafts.py"), encoding="utf-8").read()
_src = _src[:_src.rindex("\nmain()")]
exec(compile(_src, "siege_drafts.py", "exec"), globals())

RADIUS = 0.32
DOOR_OUT = 0.72
DASHES = 12


def glow(name, rgb, strength):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = (*rgb, 1)
    em.inputs["Strength"].default_value = strength
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(em.outputs[0], out.inputs[0])
    return m


def ring(at):
    objs = []
    mat = glow("DoorRing", (1.0, 0.68, 0.08), 1.1)
    for i in range(DASHES):
        a = 2 * math.pi * i / DASHES
        bpy.ops.mesh.primitive_cube_add(size=1)
        d = bpy.context.object
        d.name = f"DoorDash{i}"
        d.scale = (RADIUS * 0.34, RADIUS * 0.07, 0.004)
        d.rotation_euler = (0, 0, a + math.pi / 2)
        d.location = (at[0] + RADIUS * math.cos(a), at[1] + RADIUS * math.sin(a), 0.006)
        d.data.materials.append(mat)
        d.visible_shadow = False
        objs.append(d)
    return objs


def sparks(at):
    objs = []
    rnd = random.Random(7)
    tones = (((1.0, 0.82, 0.3), 1.3), ((1.0, 0.66, 0.12), 1.1), ((0.95, 0.5, 0.06), 0.9))
    hot = [glow(f"Spark{i}", c, k) for i, (c, k) in enumerate(tones)]
    for i in range(18):
        r = RADIUS * 0.8 * math.sqrt(rnd.random())
        # (sizes below scale with the ring)
        a = rnd.random() * 2 * math.pi
        h = rnd.random() ** 1.3 * RADIUS * 3.0
        size = RADIUS * 0.09 * (1.0 - 0.6 * h / (RADIUS * 3.0))
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=size)
        s = bpy.context.object
        s.name = f"DoorSpark{i}"
        s.location = (at[0] + r * math.cos(a), at[1] + r * math.sin(a), 0.03 + h)
        s.data.materials.append(hot[min(2, int(h / RADIUS))])
        s.visible_shadow = False
        objs.append(s)
    return objs


def main():
    os.makedirs(OUT, exist_ok=True)
    sc, cam, cam_data, ground = stage()
    ground.hide_render = False
    tower = None
    for name in ("FortifiedTower1-lvl3", "FortifiedTower1-lvl2", "FortifiedTower1"):
        tower = bpy.data.objects.get(name)
        if tower is not None:
            break
    if tower is None:
        print("MISSING tower")
        return
    tower.parent = None
    tower.matrix_world = Matrix.Identity(4)
    # the game's sizes are in the tower's cell: its width is one cell, the door circle 0.32 of it (WallFight.cs)
    global RADIUS
    from mathutils import Vector
    pts = [part.matrix_world @ Vector(c) for part in [tower] + list(tower.children_recursive) if part.type == 'MESH'
           for c in part.bound_box]
    width = max(p.x for p in pts) - min(p.x for p in pts)
    face = min(p.y for p in pts)
    RADIUS = 0.13 * width   # the game's circle is ~1.7 door widths across (the user's screenshot)
    at = (0.0, face - 0.12 - RADIUS * 0.8)
    print("TOWER width %.2f face y %.2f radius %.2f" % (width, face, RADIUS))
    fx = ring(at) + sparks(at)
    bpy.context.view_layer.update()
    shoot(sc, cam, cam_data, [tower] + fx, os.path.join(OUT, "door_fx-game.png"), yaw_deg=0, pitch_deg=50)
    shoot(sc, cam, cam_data, fx + [tower], os.path.join(OUT, "door_fx-side.png"), yaw_deg=-35, pitch_deg=22)
    # close on the threshold: an unseen box over the doorway frames the door with the sparks
    bpy.ops.mesh.primitive_cube_add(size=1, location=(at[0], at[1] + RADIUS, RADIUS * 1.6))
    frame = bpy.context.object
    frame.scale = (RADIUS * 3.4, RADIUS * 3.0, RADIUS * 3.2)
    frame.visible_camera = False
    frame.visible_shadow = False
    for part in [tower] + list(tower.children_recursive):
        part.hide_render = False   # shoot() hides what it framed once done
    shoot(sc, cam, cam_data, fx + [frame], os.path.join(OUT, "door_fx-close.png"), yaw_deg=-20, pitch_deg=35)


main()
