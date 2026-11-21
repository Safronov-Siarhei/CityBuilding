# Concept render of the campaign village's ground drawn with TEXTURES instead of CineGround's formulas
# (the user, 2026-11-21: «земля выглядит пикселями большими, как квадраты, это выглядит дёшево ...
# переделать на 3д модель с текстурой»). A patch like mission 1's start: a cobbled street through
# grass, a dirt lane joining it, seen from the play camera. Materials are Blender nodes here; once
# approved they are baked into tiling images for the game.
#   blender --background --python Tools/blender/cinematic/ground_concept.py -- <out.png> [eevee|cycles]
import math
import sys

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = argv[0] if argv else "ground_concept.png"
ENGINE = argv[1] if len(argv) > 1 else "eevee"

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene


def lin(h):
    """#rrggbb (sRGB, the islands' palette) -> linear RGBA for node colours."""
    c = [int(h[i:i + 2], 16) / 255 for i in (1, 3, 5)]
    return tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c) + (1.0,)


def nodes_of(mat):
    mat.use_nodes = True
    nt = mat.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Roughness"].default_value = 0.9
    nt.links.new(bsdf.outputs[0], out.inputs[0])
    return nt, bsdf


def ramp(nt, src, stops):
    r = nt.nodes.new("ShaderNodeValToRGB")
    els = r.color_ramp.elements
    els[0].position, els[0].color = stops[0]
    els[1].position, els[1].color = stops[1]
    for pos, col in stops[2:]:
        e = els.new(pos)
        e.color = col
    nt.links.new(src, r.inputs[0])
    return r


def noise(nt, vec, scale, detail=4.0, rough=0.55):
    n = nt.nodes.new("ShaderNodeTexNoise")
    n.inputs["Scale"].default_value = scale
    n.inputs["Detail"].default_value = detail
    n.inputs["Roughness"].default_value = rough
    if vec is not None:
        nt.links.new(vec, n.inputs["Vector"])
    return n


def mix(nt, a, b, fac, blend="MIX"):
    m = nt.nodes.new("ShaderNodeMix")
    m.data_type = "RGBA"
    m.blend_type = blend
    nt.links.new(fac, m.inputs[0]) if not isinstance(fac, float) else None
    if isinstance(fac, float):
        m.inputs[0].default_value = fac
    for sock, v in ((6, a), (7, b)):
        if isinstance(v, tuple):
            m.inputs[sock].default_value = v
        else:
            nt.links.new(v, m.inputs[sock])
    return m.outputs[2]


def world_xy(nt):
    tc = nt.nodes.new("ShaderNodeTexCoord")
    return tc.outputs["Object"]


# ------------------------------------------------------------------ materials

def grass_material():
    mat = bpy.data.materials.new("ConceptGrass")
    nt, bsdf = nodes_of(mat)
    p = world_xy(nt)
    # big soft zones (the islands' palette): base, darker long grass, dry meadow, light flowery patches
    big = noise(nt, p, 0.18, 4.0)
    dry = noise(nt, p, 0.07, 3.0)
    light = noise(nt, p, 0.3, 4.0)
    c = mix(nt, lin("#86C752"), lin("#71B947"), ramp(nt, big.outputs[0], [(0.45, (0, 0, 0, 1)), (0.6, (1, 1, 1, 1))]).outputs[0])
    c = mix(nt, c, lin("#C4CE68"), ramp(nt, dry.outputs[0], [(0.6, (0, 0, 0, 1)), (0.72, (0.8, 0.8, 0.8, 1))]).outputs[0])
    c = mix(nt, c, lin("#A8D867"), ramp(nt, light.outputs[0], [(0.58, (0, 0, 0, 1)), (0.7, (0.9, 0.9, 0.9, 1))]).outputs[0])
    # tufts: small round clumps a shade lighter / darker (never square cells)
    v = nt.nodes.new("ShaderNodeTexVoronoi")
    v.inputs["Scale"].default_value = 3.5
    v.inputs["Randomness"].default_value = 1.0
    nt.links.new(p, v.inputs["Vector"])
    tuft = ramp(nt, v.outputs["Distance"], [(0.0, (1, 1, 1, 1)), (0.3, (0, 0, 0, 1))])
    shade = ramp(nt, v.outputs["Color"], [(0.0, (0.78, 0.82, 0.7, 1)), (1.0, (1.12, 1.15, 1.05, 1))])
    tufted = mix(nt, c, shade.outputs[0], tuft.outputs[0], "MULTIPLY")
    # fine blades: stretched noise, a hair of light and dark
    blades = noise(nt, p, 18.0, 6.0, 0.7)
    fine = ramp(nt, blades.outputs[0], [(0.3, (0.86, 0.88, 0.82, 1)), (0.7, (1.1, 1.12, 1.06, 1))])
    col = mix(nt, tufted, fine.outputs[0], 1.0, "MULTIPLY")
    nt.links.new(col, bsdf.inputs["Base Color"])
    bump = nt.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.35
    nt.links.new(tuft.outputs[0], bump.inputs["Height"])
    nt.links.new(bump.outputs[0], bsdf.inputs["Normal"])
    return mat


def cobble_material():
    mat = bpy.data.materials.new("ConceptCobble")
    nt, bsdf = nodes_of(mat)
    p = world_xy(nt)
    # setts: a Voronoi on squashed coordinates -> rounded, slightly irregular stones in loose rows
    sq = nt.nodes.new("ShaderNodeVectorMath")
    sq.operation = "MULTIPLY"
    sq.inputs[1].default_value = (1.0, 1.45, 1.0)
    nt.links.new(p, sq.inputs[0])
    v = nt.nodes.new("ShaderNodeTexVoronoi")
    v.feature = "DISTANCE_TO_EDGE"
    v.inputs["Scale"].default_value = 4.2
    v.inputs["Randomness"].default_value = 0.45
    nt.links.new(sq.outputs[0], v.inputs["Vector"])
    vc = nt.nodes.new("ShaderNodeTexVoronoi")
    vc.inputs["Scale"].default_value = 4.2
    vc.inputs["Randomness"].default_value = 0.45
    nt.links.new(sq.outputs[0], vc.inputs["Vector"])
    # dome: 0 in the joint, rising and rounding over the stone
    dome = ramp(nt, v.outputs["Distance"], [(0.03, (0, 0, 0, 1)), (0.22, (1, 1, 1, 1))])
    # each stone its own shade, a warm one here and there
    tone = ramp(nt, vc.outputs["Color"], [(0.0, lin("#7F7B74")), (0.5, lin("#B3AC9E")), (0.8, lin("#9C968A")), (0.84, lin("#A88A6C"))])
    wear = noise(nt, p, 3.0, 3.0)
    stone = mix(nt, tone.outputs[0], lin("#D8D2C4"), ramp(nt, wear.outputs[0], [(0.55, (0, 0, 0, 1)), (0.75, (0.35, 0.35, 0.35, 1))]).outputs[0])
    joint = mix(nt, lin("#3E3A35"), lin("#6A8A3C"), ramp(nt, noise(nt, p, 1.2, 2.0).outputs[0], [(0.5, (0, 0, 0, 1)), (0.7, (0.7, 0.7, 0.7, 1))]).outputs[0])
    col = mix(nt, joint, stone, dome.outputs[0])
    nt.links.new(col, bsdf.inputs["Base Color"])
    bump = nt.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 1.0
    bump.inputs["Distance"].default_value = 0.6
    nt.links.new(dome.outputs[0], bump.inputs["Height"])
    nt.links.new(bump.outputs[0], bsdf.inputs["Normal"])
    bsdf.inputs["Roughness"].default_value = 0.75
    return mat


def dirt_material():
    mat = bpy.data.materials.new("ConceptDirt")
    nt, bsdf = nodes_of(mat)
    uv = nt.nodes.new("ShaderNodeTexCoord").outputs["UV"]
    p = world_xy(nt)
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(uv, sep.inputs[0])
    # across the lane (uv.y 0..1): two darker ruts, a grassy ridge in the middle, grass creeping at the edges
    across = sep.outputs[1]
    ruts = ramp(nt, across, [(0.14, (0, 0, 0, 1)), (0.26, (0.8, 0.8, 0.8, 1)), (0.38, (0, 0, 0, 1)), (0.62, (0, 0, 0, 1)), (0.74, (0.8, 0.8, 0.8, 1)), (0.86, (0, 0, 0, 1))])
    ridge = ramp(nt, across, [(0.43, (0, 0, 0, 1)), (0.5, (0.7, 0.7, 0.7, 1)), (0.57, (0, 0, 0, 1))])
    edge = ramp(nt, across, [(0.0, (1, 1, 1, 1)), (0.1, (0, 0, 0, 1)), (0.9, (0, 0, 0, 1)), (1.0, (1, 1, 1, 1))])
    base = mix(nt, lin("#D2AE72"), lin("#A9804E"), ramp(nt, noise(nt, p, 2.5, 6.0, 0.65).outputs[0], [(0.3, (0, 0, 0, 1)), (0.7, (1, 1, 1, 1))]).outputs[0])
    c = mix(nt, base, lin("#8F6A43"), ruts.outputs[0])
    patch = noise(nt, p, 2.5, 3.0)
    gm = mix(nt, ridge.outputs[0], edge.outputs[0], 1.0, "LIGHTEN")
    gmask = mix(nt, gm, patch.outputs[0], 1.0, "MULTIPLY")
    c = mix(nt, c, lin("#86C752"), gmask)
    # pebbles: small round light stones
    v = nt.nodes.new("ShaderNodeTexVoronoi")
    v.inputs["Scale"].default_value = 9.0
    nt.links.new(p, v.inputs["Vector"])
    peb = ramp(nt, v.outputs["Distance"], [(0.0, (1, 1, 1, 1)), (0.16, (0, 0, 0, 1))])
    pick = ramp(nt, v.outputs["Color"], [(0.7, (0, 0, 0, 1)), (0.71, (1, 1, 1, 1))])
    pm = mix(nt, peb.outputs[0], pick.outputs[0], 1.0, "MULTIPLY")
    c = mix(nt, c, lin("#C4BBA2"), pm)
    nt.links.new(c, bsdf.inputs["Base Color"])
    bump = nt.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.4
    nt.links.new(pm, bump.inputs["Height"])
    nt.links.new(bump.outputs[0], bsdf.inputs["Normal"])
    return mat


# ------------------------------------------------------------------ geometry

def plane(name, size, mat, z=0.0):
    bpy.ops.mesh.primitive_plane_add(size=size, location=(0, 0, z))
    o = bpy.context.object
    o.name = name
    o.data.materials.append(mat)
    return o


def ribbon(name, pts, width, mat, z):
    """A flat strip along a polyline; uv = (metres along, 0..1 across)."""
    import bmesh
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.new()
    left, right, along = [], [], [0.0]
    for i, a in enumerate(pts):
        a = Vector(a)
        d = (Vector(pts[min(i + 1, len(pts) - 1)]) - Vector(pts[max(i - 1, 0)])).normalized()
        n = Vector((-d.y, d.x)) * width * 0.5
        left.append(bm.verts.new((a.x + n.x, a.y + n.y, z)))
        right.append(bm.verts.new((a.x - n.x, a.y - n.y, z)))
        if i:
            along.append(along[-1] + (Vector(pts[i]) - Vector(pts[i - 1])).length)
    for i in range(len(pts) - 1):
        f = bm.faces.new((left[i], right[i], right[i + 1], left[i + 1]))
        for loop, (u, v) in zip(f.loops, ((along[i], 0), (along[i], 1), (along[i + 1], 1), (along[i + 1], 0))):
            loop[uvl].uv = (u, v)
    bm.to_mesh(me)
    o = bpy.data.objects.new(name, me)
    scene.collection.objects.link(o)
    o.data.materials.append(mat)
    return o


def curve(a, b, c, n=40):
    out = []
    for i in range(n + 1):
        t = i / n
        out.append(((1 - t) ** 2 * a[0] + 2 * (1 - t) * t * b[0] + t * t * c[0], (1 - t) ** 2 * a[1] + 2 * (1 - t) * t * b[1] + t * t * c[1]))
    return out


plane("Grass", 120, grass_material())
ribbon("Street", curve((2.5, -30), (-3.5, 0), (2.5, 30)), 2.6, cobble_material(), 0.01)
ribbon("Lane", curve((1.2, -2), (6, 3), (14, 10)), 1.9, dirt_material(), 0.005)

# a few markers for scale (the hero ~0.8 of a cell, ~1.8 m): plain boxes like the play props
for x, y, s in ((-4.5, 3.0, 1.2), (5.5, -4.0, 0.9), (-3.0, -6.0, 0.7)):
    bpy.ops.mesh.primitive_cube_add(size=s, location=(x, y, s / 2))
    m = bpy.data.materials.new("Prop")
    m.use_nodes = True
    m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = lin("#B5733F")
    bpy.context.object.data.materials.append(m)

# ------------------------------------------------------------------ light and camera (the play view)

sun = bpy.data.lights.new("Sun", "SUN")
sun.energy = 3.2
sun.color = (1.0, 0.96, 0.88)
sun.angle = math.radians(2)
so = bpy.data.objects.new("Sun", sun)
so.rotation_euler = (math.radians(50), math.radians(10), math.radians(35))
scene.collection.objects.link(so)
world = bpy.data.worlds.new("Sky")
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.55, 0.7, 0.95, 1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.9
scene.world = world

cam = bpy.data.cameras.new("Cam")
cam.lens = 35
co = bpy.data.objects.new("Cam", cam)
co.location = (0, -11.5, 16.5)
co.rotation_euler = (math.radians(35), 0, 0)
scene.collection.objects.link(co)
scene.camera = co

scene.render.resolution_x = 2000
scene.render.resolution_y = 1200
scene.view_settings.view_transform = "Standard"
if ENGINE == "cycles":
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 48
    scene.cycles.use_denoising = True
else:
    scene.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items.keys() else "BLENDER_EEVEE"
scene.render.filepath = OUT
bpy.ops.render.render(write_still=True)
print("written", OUT)
