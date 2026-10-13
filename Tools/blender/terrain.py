"""The map's own palette and the small decoration models scattered over it.

The buildings' palette (kit.PALETTE) is full -- every one of its 40 cells is taken, and adding a
row would shift every building's UVs. The ground needs many shades of its own anyway (five
greens, two sands, earths, rocks, three waters), so the map draws through a second palette and a
second material, "Terrain". Same rules as kit: append only, colour is a palette cell, never a
material.

The decoration models are tiny on purpose -- they are scattered by the thousand -- and drawn in
the same faceted style as the buildings: grass tufts, flowers, bushes, reeds, lily pads,
mushrooms, ferns, logs, stumps, pebbles and crags.
"""
import math

import bpy

import kit

COLS, ROWS, CELL = 8, 6, 4

TERRAIN_PALETTE = [
    ("grass_light", "A8D867"), ("grass", "86C752"), ("grass_mid", "71B947"), ("grass_lush", "5DAA41"),
    ("grass_dark", "4A963A"), ("forest_floor", "3E8034"), ("moss", "6A9A3C"), ("grass_dry", "C4CE68"),

    ("sand", "EFD68B"), ("sand_wet", "D9BB73"), ("dirt", "B68C57"), ("dirt_dark", "8F6A43"),
    ("path", "CDA86D"), ("clay", "A9714A"), ("mud", "6F5638"), ("pebble", "C4BBA2"),

    ("rock_light", "CBC6BA"), ("rock", "B2AB9E"), ("rock_mid", "99928A"), ("rock_dark", "7B756C"),
    ("rock_deep", "615C55"), ("water_shallow", "72D2DE"), ("water", "48B3E1"), ("water_deep", "3290CD"),

    ("foam", "A9E9F0"), ("leaf", "3E8C3F"), ("leaf_light", "62AF49"), ("flower_white", "FFF8E8"),
    ("flower_yellow", "FFD447"), ("flower_purple", "A97CE2"), ("flower_red", "E8574B"), ("flower_pink", "F3A5C6"),

    ("bark", "7A4E2C"), ("bark_dark", "57371F"), ("reed", "C2B45C"), ("cattail", "6B4426"),
    ("lily", "4FA94A"), ("mushroom_cap", "D9473B"), ("mushroom_stem", "F2E7D0"), ("birch_bark", "EEEBE3"),

    # a second, barely different shade of each grass, for the small patches that keep the
    # plain from reading as one flat sheet
    ("grass_b", "8ECC59"), ("grass_light_b", "B1DD71"), ("grass_mid_b", "79BE4D"), ("grass_lush_b", "65B047"),
    ("grass_dark_b", "519C40"), ("forest_floor_b", "458639"), ("grass_dry_b", "CCD472"), ("stony", "ADA285"),
]
NAMES = [n for n, _ in TERRAIN_PALETTE]
assert len(TERRAIN_PALETTE) <= COLS * ROWS

MATERIAL_NAME = "Terrain"
PALETTE_IMAGE = "TerrainPalette"


def ensure_palette(png_path):
    w, h = COLS * CELL, ROWS * CELL
    old = bpy.data.images.get(PALETTE_IMAGE)
    if old is not None:
        bpy.data.images.remove(old)
    img = bpy.data.images.new(PALETTE_IMAGE, w, h, alpha=False)
    px = [0.0] * (w * h * 4)
    for i, (_, hx) in enumerate(TERRAIN_PALETTE):
        c, r = i % COLS, i // COLS
        rgb = kit._rgb(hx)
        for yy in range(CELL):
            for xx in range(CELL):
                x, y = c * CELL + xx, (ROWS - 1 - r) * CELL + yy
                k = (y * w + x) * 4
                px[k:k + 4] = [rgb[0], rgb[1], rgb[2], 1.0]
    img.pixels = px
    img.filepath_raw = png_path
    img.file_format = 'PNG'
    img.save()
    img.pack()
    return img


def ensure_material(img):
    m = bpy.data.materials.get(MATERIAL_NAME) or bpy.data.materials.new(MATERIAL_NAME)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = img
    tex.interpolation = 'Closest'
    nt.links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
    bsdf.inputs['Roughness'].default_value = 0.9
    if 'Specular IOR Level' in bsdf.inputs:
        bsdf.inputs['Specular IOR Level'].default_value = 0.15
    nt.links.new(bsdf.outputs['BSDF'], out.inputs['Surface'])
    return m


def uv_of(color):
    i = NAMES.index(color)
    c, r = i % COLS, i // COLS
    return ((c + 0.5) / COLS, 1.0 - (r + 0.5) / ROWS)


class TerrainBuilder(kit.Builder):
    """kit.Builder drawing through the terrain palette."""

    def _uv(self, color):
        return uv_of(color)

    def poly(self, pts, color):
        vs = [self.bm.verts.new(p) for p in pts]
        return self._face(vs, color)


# -- decoration models -------------------------------------------------------------------------
#
# Each returns a TerrainBuilder with the model standing on z = 0 around the origin. `v` picks a
# variant (colour or shape), so one kind can have several prototypes.


def _blade(b, x, y, h, w, lean, ang, color):
    """One blade: a thin triangle leaning away from its root, drawn from both sides."""
    a = math.radians(ang)
    dx, dy = math.cos(a), math.sin(a)
    px, py = -dy * w, dx * w
    tip = (x + dx * lean, y + dy * lean, h)
    b.poly([(x - px, y - py, 0), (x + px, y + py, 0), tip], color)
    b.poly([(x + px, y + py, 0), (x - px, y - py, 0), tip], color)


def _diamond(b, x, y, z, r, color):
    """A flat four-pointed head, seen from above -- a flower at this size needs no more."""
    b.poly([(x + r, y, z), (x, y + r, z), (x - r, y, z), (x, y - r, z)], color)


# Sizes are set against the game's own scale: a citizen is 0.45 m tall and a grown tree 1-2 m,
# so a tuft is ankle-high, a bush comes to a citizen's chest and a log is shorter than a tree.

def tuft(v):
    b = TerrainBuilder()
    cols = [("grass_lush", "grass_dark"), ("grass_mid", "grass_lush"), ("grass_dry", "grass_mid")][v % 3]
    for k in range(4):
        ang = k * 90 + 25 * v
        r = 0.025 + 0.015 * (k % 2)
        _blade(b, r * math.cos(math.radians(ang)), r * math.sin(math.radians(ang)),
               0.15 + 0.05 * ((k * 7 + v) % 3) / 2, 0.02, 0.06, ang, cols[k % 2])
    return b


def flowers(v):
    b = TerrainBuilder()
    head = ["flower_white", "flower_yellow", "flower_purple", "flower_pink", "flower_red"][v % 5]
    spots = [(0.0, 0.0), (0.08, 0.035), (-0.06, 0.06), (0.03, -0.075), (-0.085, -0.03), (0.1, -0.065)]
    for k, (x, y) in enumerate(spots):
        h = 0.07 + 0.025 * ((k + v) % 3)
        _blade(b, x, y, h, 0.008, 0.0, 90 * k, "grass_lush")
        _diamond(b, x, y, h, 0.026, head)
    return b


def bush(v):
    b = TerrainBuilder()
    r = [0.2, 0.26, 0.17][v % 3]
    b.ball(0.0, 0.0, 0.0, r, r * 1.3, "leaf", sides=6, top="leaf_light")
    if v % 3 == 1:
        b.ball(r * 0.75, -r * 0.3, 0.0, r * 0.6, r * 0.85, "leaf", sides=5, top="leaf_light")
    if v % 3 == 2:   # berries
        for k in range(3):
            a = math.radians(40 + 100 * k)
            _diamond(b, r * 0.8 * math.cos(a), r * 0.8 * math.sin(a), r * 0.75, 0.025, "flower_red")
    return b


def reeds(v):
    b = TerrainBuilder()
    for k in range(5):
        ang = k * 72 + 13 * v
        rr = 0.02 + 0.025 * (k % 3)
        x, y = rr * math.cos(math.radians(ang)), rr * math.sin(math.radians(ang))
        _blade(b, x, y, 0.28 + 0.12 * ((k + v) % 3) / 2, 0.012, 0.035, ang, "reed" if k % 2 else "grass_mid")
    for k in range(1 + v % 2):
        x, y = 0.025 * k, 0.02 * ((k + v) % 2)
        h = 0.36 + 0.05 * k
        _blade(b, x, y, h - 0.05, 0.006, 0.0, 30, "reed")
        b.zbox(x, y, h - 0.05, h + 0.03, 0.016, 0.016, 0, "cattail")
    return b


def lily(v):
    b = TerrainBuilder()
    r = 0.13 + 0.03 * (v % 2)
    pts = []
    for k in range(6):
        a = math.radians(30 + k * 300 / 5)          # a notch cut out of the disc
        pts.append((r * math.cos(a), r * math.sin(a), 0.01))
    b.poly([(0, 0, 0.01)] + pts, "lily")
    if v % 2 == 0:
        _diamond(b, 0.03, 0.0, 0.03, 0.035, "flower_pink")
        _diamond(b, 0.03, 0.0, 0.035, 0.012, "flower_yellow")
    return b


def mushrooms(v):
    b = TerrainBuilder()
    cap = "mushroom_cap" if v % 2 == 0 else "dirt"
    for x, y, s in [(0.0, 0.0, 1.0), (0.06, 0.03, 0.7), (-0.04, 0.05, 0.55)][: 2 + v % 2]:
        b.cylinder(x, y, 0.0, 0.05 * s, 0.012 * s, "mushroom_stem", sides=4)
        b.pyramid(x, y, 0.045 * s, 0.035 * s, 0.035 * s, 0.085 * s, cap, sides=5, rot_deg=0)
    return b


def fern(v):
    b = TerrainBuilder()
    for k in range(5):
        a = math.radians(k * 72 + 20 * v)
        dx, dy = math.cos(a), math.sin(a)
        px, py = -dy * 0.03, dx * 0.03
        tip = (dx * 0.2, dy * 0.2, 0.05)
        col = "grass_dark" if k % 2 else "leaf"
        b.poly([(0, 0, 0.01), (dx * 0.1 + px, dy * 0.1 + py, 0.12), tip, (dx * 0.1 - px, dy * 0.1 - py, 0.12)], col)
        b.poly([(0, 0, 0.01), (dx * 0.1 - px, dy * 0.1 - py, 0.12), tip, (dx * 0.1 + px, dy * 0.1 + py, 0.12)], col)
    return b


def log(v):
    b = TerrainBuilder()
    length = 0.7 + 0.2 * (v % 2)
    b.hcylinder('x', (0.0, 0.0, 0.07), length / 2, 0.07, "bark" if v % 2 == 0 else "bark_dark", sides=6)
    _diamond(b, length * 0.2, 0.0, 0.142, 0.04, "moss")
    return b


def stump(v):
    b = TerrainBuilder()
    b.cylinder(0.0, 0.0, 0.0, 0.13, 0.1, "bark", sides=6)
    b.cylinder(0.0, 0.0, 0.13, 0.135, 0.08, "dirt", sides=6)
    if v % 2:
        _diamond(b, 0.1, 0.05, 0.03, 0.03, "mushroom_cap")
    return b


def pebbles(v):
    b = TerrainBuilder()
    for k, (x, y, r) in enumerate([(0.0, 0.0, 0.06), (0.1, 0.04, 0.04), (-0.07, 0.08, 0.035)]):
        b.crag(x, y, r, r * 0.8, r * 0.8, "pebble" if (k + v) % 2 else "rock", sides=4, seed=k + v)
    return b


def crag(v):
    """A decorative boulder for the slopes of hills and mountains: bigger than the stone the
    player gathers, and never on ground where it could be mistaken for one."""
    b = TerrainBuilder()
    s = [1.0, 1.5, 0.7, 2.1][v % 4]
    b.crag(0.0, 0.0, 0.7 * s, 0.55 * s, 0.75 * s, ["rock", "rock_mid", "rock_light", "rock_mid"][v % 4],
           top="moss" if v % 2 == 0 else None, sides=6, seed=v * 3, lean=(0.05 * s, 0.03 * s))
    if v % 4 == 1:
        b.crag(0.6 * s, -0.3 * s, 0.35 * s, 0.3 * s, 0.4 * s, "rock_dark", sides=5, seed=v + 5)
    return b


DECOR = {
    "tuft": (tuft, 3), "flowers": (flowers, 5), "bush": (bush, 3), "reeds": (reeds, 2),
    "lily": (lily, 2), "mushrooms": (mushrooms, 2), "fern": (fern, 2), "log": (log, 2),
    "stump": (stump, 2), "pebbles": (pebbles, 2), "crag": (crag, 4),
}


def build_prototypes(material, collection):
    """One mesh per (kind, variant); returns {kind: [mesh, ...]} and prints their triangle counts."""
    out = {}
    for kind, (fn, variants) in DECOR.items():
        meshes = []
        for v in range(variants):
            b = fn(v)
            tris = b.triangles()
            me = b.to_mesh(f"Decor_{kind}_{v}")
            me.materials.append(material)
            meshes.append(me)
            print(f"DECOR {kind}_{v} tris={tris}")
        out[kind] = meshes
    return out
