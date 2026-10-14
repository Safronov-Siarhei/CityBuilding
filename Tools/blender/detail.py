"""The soft painted detail laid over the palette's flat colours: tiles, plaster, planks, stones.

Colour still comes from the palette (UV map `UVMap`, unchanged). On top of it every face gets a
grey pattern from ONE atlas, `Buildings-Detail.png`, multiplied in so that 0.5 changes nothing:

- `Detail`  (2nd UV set, Unity uv1): where on the pattern, in pattern units. Laid in metres on
  the face's own plane -- along the ground for walls, along the slope for roofs -- so tiles run
  in rows down every roof and a pattern keeps its size whatever the face's size;
- `Pattern` (3rd UV set, Unity uv2): which pattern, as the cell index in u;
- `Shade`   (vertex colour): a soft darkening of walls towards the ground, so a house sits in the
  grass instead of floating on it. Faces looking up are left alone (a road, a field).

The pattern is picked from the face's palette colour (PATTERN_BY_COLOUR), so the 48 building
scripts need no change. The game's shader (Assets/_Project/Shaders/BuildingDetail.shader)
reads the same three things, and the atlas is copied next to the palette by export_fbx.py.
No triangles are added.
"""
import math

import bpy
from mathutils import Vector

ATLAS_FILE = "Buildings-Detail.png"
ATLAS_IMAGE = "BuildingsDetail"
CELL = 256        # pixels per pattern cell
GRID = 4          # cells per atlas row; the atlas is GRID x GRID cells

PLAIN, TILES, PLASTER, WOOD, STONE, LEAVES, SLATE, THATCH = range(8)

# how many metres one cell of each pattern covers
METRES = {PLAIN: 1.0, TILES: 0.4, PLASTER: 0.8, WOOD: 0.4, STONE: 0.5, LEAVES: 0.3,
          SLATE: 0.4, THATCH: 0.5}

SHADE_LOW = 0.84      # a wall's tone at the ground...
SHADE_HEIGHT = 0.55   # ...rising to full by this height


def pattern_of(name):
    if name.startswith("roof_slate"):
        return SLATE
    if name.startswith("roof_straw") or name == "hay":
        return THATCH
    if name.startswith("roof_"):
        return TILES
    if name.startswith("plaster"):
        return PLASTER
    if name.startswith("wood") or name in ("door", "burlap"):
        return WOOD
    if name.startswith("stone") or name == "cobble":
        return STONE
    if name in ("leaf", "grass", "grass_dark"):
        return LEAVES
    return PLAIN


# --- the atlas ------------------------------------------------------------------------------------

def _patterns():
    """Every pattern as a CELL x CELL array of grey values around 0.5, each one tiling seamlessly."""
    import numpy as np
    rng = np.random.default_rng(7)
    n = CELL
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float64)

    def wave_noise(freqs, terms=10):
        out = np.zeros((n, n))
        for _ in range(terms):
            fx, fy = rng.integers(-freqs, freqs + 1, 2)
            if fx == 0 and fy == 0:
                continue
            out += np.sin(2 * np.pi * (fx * xx + fy * yy) / n + rng.uniform(0, 2 * np.pi)) / math.hypot(fx, fy)
        return out / (np.abs(out).max() + 1e-9)

    def blur(a, sigma):
        f = np.fft.fftfreq(n)
        gx, gy = np.meshgrid(f, f)
        return np.real(np.fft.ifft2(np.fft.fft2(a) * np.exp(-2 * (np.pi * sigma) ** 2 * (gx ** 2 + gy ** 2))))

    def tiles(scallop=True):
        rows, per = 4, 4
        h, w = n // rows, n // per
        r = (yy // h).astype(int)
        xs = (xx + np.where(r % 2 == 1, w / 2, 0)) % n
        col = (xs // w).astype(int)
        lx = ((xs % w) - w / 2) / (w / 2)
        ly = (yy % h) / h
        edge = 0.78 + 0.2 * (1 - lx ** 2) if scallop else np.full_like(lx, 0.9)
        jitter = rng.uniform(-0.03, 0.03, (rows, per))[r, col]
        inside = 0.46 + 0.1 * (ly / edge) + jitter
        inside = inside + np.where(edge - ly < 0.07, 0.035, 0.0) - np.where(np.abs(lx) > 0.93, 0.06, 0.0)
        a = np.where(ly < edge, inside, 0.37)
        return blur(a, 1.3) + 0.015 * wave_noise(12)

    def plaster():
        return 0.5 + 0.05 * wave_noise(3) + 0.02 * wave_noise(14, 20)

    def wood():
        h = n // 4
        plank = (yy // h).astype(int)
        tone = rng.uniform(-0.035, 0.035, 4)[plank]
        grain = 0.03 * np.sin(2 * np.pi * (yy / 9.0 + 1.6 * wave_noise(4)))
        seam = np.where((yy % h) < 3, -0.1, 0.0)
        return blur(0.5 + tone + grain + seam, 0.8)

    def stone():
        # dressed stone in courses: soft-edged blocks, each its own shade, half-bond rows
        rows, per = 4, 2
        h, w = n // rows, n // per
        r = (yy // h).astype(int)
        xs = (xx + np.where(r % 2 == 1, w / 2, 0)) % n
        col = (xs // w).astype(int)
        ex = np.minimum(xs % w, w - xs % w) / 10.0
        ey = np.minimum(yy % h, h - yy % h) / 8.0
        body = np.clip(np.minimum(ex, ey), 0, 1)
        tone = rng.uniform(-0.035, 0.035, (rows, per))[r, col]
        return blur(0.43 + 0.09 * np.sqrt(body) + tone + 0.015 * wave_noise(10), 1.4)

    def leaves():
        return 0.5 + 0.07 * wave_noise(6, 16)

    def thatch():
        # straw: fine vertical strands in layered bands, each band's lower edge a little darker
        band = (yy % (n // 3)) / (n // 3)
        strands = 0.035 * np.sin(2 * np.pi * (xx / 5.0 + 0.8 * wave_noise(5)))
        return blur(0.5 + strands + 0.06 * band - 0.05 + 0.02 * wave_noise(10), 0.7)

    def plain():
        return 0.5 + 0.01 * wave_noise(8)

    return {PLAIN: plain(), TILES: tiles(), PLASTER: plaster(), WOOD: wood(), STONE: stone(),
            LEAVES: leaves(), SLATE: tiles(scallop=False), THATCH: thatch()}


def ensure_atlas(png_path):
    """(Re)paints the atlas image, saves it as `png_path` and packs it, like the palette."""
    import numpy as np
    size = CELL * GRID
    atlas = np.full((size, size), 0.5)
    for idx, cell in _patterns().items():
        c, r = idx % GRID, idx // GRID
        atlas[r * CELL:(r + 1) * CELL, c * CELL:(c + 1) * CELL] = np.clip(cell, 0, 1)
    old = bpy.data.images.get(ATLAS_IMAGE)
    if old is not None:
        bpy.data.images.remove(old)
    img = bpy.data.images.new(ATLAS_IMAGE, size, size, alpha=False)
    px = np.ones((size, size, 4))
    px[..., 0] = px[..., 1] = px[..., 2] = atlas[::-1]   # Blender stores the bottom row first
    img.pixels = px.ravel()
    img.filepath_raw = png_path
    img.file_format = 'PNG'
    img.save()
    img.pack()
    img.colorspace_settings.name = 'Non-Color'
    return img


# --- per mesh -------------------------------------------------------------------------------------

def add_layers(me, names, cols, rows):
    """Writes the Detail and Pattern UV sets and the Shade colour onto a finished model's mesh.
    `names` is the palette's colour list, `cols`/`rows` its layout -- the face's colour is read
    back from where its UVMap points."""
    pal = me.uv_layers["UVMap"]
    for layer in ("Detail", "Pattern"):
        old = me.uv_layers.get(layer)
        if old is not None:
            me.uv_layers.remove(old)
    det = me.uv_layers.new(name="Detail")
    pat = me.uv_layers.new(name="Pattern")
    me.uv_layers.active = pal
    old = me.color_attributes.get("Shade")
    if old is not None:
        me.color_attributes.remove(old)
    shade = me.color_attributes.new("Shade", 'FLOAT_COLOR', 'CORNER')
    pal, det, pat = me.uv_layers["UVMap"], me.uv_layers["Detail"], me.uv_layers["Pattern"]
    up = Vector((0, 0, 1))
    for poly in me.polygons:
        u, v = pal.data[poly.loop_indices[0]].uv
        c = min(max(int(u * cols), 0), cols - 1)
        r = min(max(int((1 - v) * rows), 0), rows - 1)
        nrm = poly.normal
        idx = pattern_of(names[r * cols + c])
        if idx in (TILES, SLATE, THATCH) and abs(nrm.z) < 0.2:
            # a roof colour on an upright face is not a roof: glass (details.GLASS), a barn's
            # painted boards, a banner -- flat colour, no tiles
            idx = PLAIN
        if abs(nrm.z) > 0.95:
            ua, va = Vector((1, 0, 0)), Vector((0, 1, 0))
        else:
            ua = Vector((-nrm.y, nrm.x, 0)).normalized()
            va = (up - nrm * nrm.dot(up)).normalized()
        size = METRES[idx]
        looks_up = nrm.z > 0.7
        for li in poly.loop_indices:
            p = me.vertices[me.loops[li].vertex_index].co
            det.data[li].uv = (p.dot(ua) / size, p.dot(va) / size)
            pat.data[li].uv = (idx + 0.5, 0.5)
            if looks_up:
                g = 1.0
            else:
                k = min(1.0, max(0.0, p.z / SHADE_HEIGHT))
                g = SHADE_LOW + (1 - SHADE_LOW) * (k * k * (3 - 2 * k))
            shade.data[li].color = (g, g, g, 1.0)
    me.color_attributes.active_color = shade


def build_material_nodes(m, palette_img, atlas_img):
    """Palette x (detail x 2) x shade -- the same sum BuildingDetail.shader does in the game."""
    nt = m.node_tree
    nt.nodes.clear()
    new, link = nt.nodes.new, nt.links.new

    def op(kind, a, b=None):
        node = new("ShaderNodeMath")
        node.operation = kind
        for i, x in enumerate((a, b)):
            if x is None:
                continue
            if isinstance(x, (int, float)):
                node.inputs[i].default_value = x
            else:
                link(x, node.inputs[i])
        return node.outputs[0]

    uv0 = new("ShaderNodeUVMap"); uv0.uv_map = "UVMap"
    pal = new("ShaderNodeTexImage"); pal.image = palette_img; pal.interpolation = 'Closest'
    link(uv0.outputs[0], pal.inputs[0])

    uvd = new("ShaderNodeUVMap"); uvd.uv_map = "Detail"
    sd = new("ShaderNodeSeparateXYZ"); link(uvd.outputs[0], sd.inputs[0])
    uvp = new("ShaderNodeUVMap"); uvp.uv_map = "Pattern"
    sp = new("ShaderNodeSeparateXYZ"); link(uvp.outputs[0], sp.inputs[0])
    idx = op('FLOOR', sp.outputs[0])
    col = op('MODULO', idx, float(GRID))
    row = op('FLOOR', op('DIVIDE', idx, float(GRID)))
    u = op('DIVIDE', op('ADD', col, op('FRACT', sd.outputs[0])), float(GRID))
    v = op('SUBTRACT', 1.0, op('DIVIDE', op('SUBTRACT', op('ADD', row, 1.0), op('FRACT', sd.outputs[1])), float(GRID)))
    cxyz = new("ShaderNodeCombineXYZ"); link(u, cxyz.inputs[0]); link(v, cxyz.inputs[1])
    det = new("ShaderNodeTexImage"); det.image = atlas_img; det.interpolation = 'Linear'
    link(cxyz.outputs[0], det.inputs[0])

    two = new("ShaderNodeVectorMath"); two.operation = 'SCALE'; two.inputs[3].default_value = 2.0
    link(det.outputs[0], two.inputs[0])
    shade = new("ShaderNodeVertexColor"); shade.layer_name = "Shade"
    mul1 = new("ShaderNodeVectorMath"); mul1.operation = 'MULTIPLY'
    link(pal.outputs[0], mul1.inputs[0]); link(two.outputs[0], mul1.inputs[1])
    mul2 = new("ShaderNodeVectorMath"); mul2.operation = 'MULTIPLY'
    link(mul1.outputs[0], mul2.inputs[0]); link(shade.outputs[0], mul2.inputs[1])

    bsdf = new("ShaderNodeBsdfPrincipled")
    bsdf.inputs['Roughness'].default_value = 0.85
    if 'Specular IOR Level' in bsdf.inputs:
        bsdf.inputs['Specular IOR Level'].default_value = 0.2
    link(mul2.outputs[0], bsdf.inputs['Base Color'])
    out = new("ShaderNodeOutputMaterial")
    link(bsdf.outputs[0], out.inputs[0])
