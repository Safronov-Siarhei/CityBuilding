"""Shared toolkit for the building models: the colour palette, the one material, and the
primitives every building is assembled from.

Run through Blender (`blender --background <file> --python <script>`); nothing here works
outside it. 1 Blender unit = 1 grid cell, +Z is up, the model's origin is the centre of its
footprint at ground level.

Colour is never a material: every face's UVs point at the centre of one cell of a small palette
texture, so every building draws through the same single material.
"""
import math

import bmesh
import bpy
from mathutils import Vector

COLS, ROWS, CELL = 8, 5, 4

# Order is the texture layout (row by row, top to bottom). Append only: moving an entry moves
# every face already pointing at its cell.
PALETTE = [
    ("roof_blue", "3F6FD8"), ("roof_blue_dark", "2C4FA8"),
    ("roof_orange", "E36A2E"), ("roof_orange_dark", "B54A1F"),
    ("roof_purple", "7E56D9"), ("roof_purple_dark", "5B3AA8"),
    ("roof_red", "D23F3A"), ("roof_green", "4FA34A"),

    ("plaster", "F6E7B8"), ("plaster_white", "FFF6DE"),
    ("stone_light", "E2DDD0"), ("stone_mid", "B9B3A6"),
    ("stone_dark", "8C877D"), ("plaster_yellow", "F4CF5A"),
    ("plaster_pink", "EBB089"), ("sand", "EDD27E"),

    ("wood_dark", "5C3420"), ("wood_mid", "7E4A2A"),
    ("wood_light", "B07A48"), ("door", "6E3B1F"),
    ("iron", "3B3C47"), ("gold", "F5B73B"),
    ("window", "1F2638"), ("window_light", "FFD27A"),

    ("grass", "79C24F"), ("grass_dark", "53993A"),
    ("leaf", "3E8C3F"), ("cobble", "A8A194"),
    ("water", "4DB6E6"), ("banner_red", "D8393C"),
    ("banner_blue", "2F5FD0"), ("white", "FFFFFF"),
]
NAMES = [n for n, _ in PALETTE]

MATERIAL_NAME = "Buildings"
PALETTE_IMAGE = "BuildingsPalette"


def _rgb(h):
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def ensure_palette(png_path):
    """(Re)paints the palette image, saves it next to the .blend and packs it in."""
    w, h = COLS * CELL, ROWS * CELL
    img = bpy.data.images.get(PALETTE_IMAGE)
    if img is not None and tuple(img.size) != (w, h):
        bpy.data.images.remove(img)
        img = None
    if img is None:
        img = bpy.data.images.new(PALETTE_IMAGE, w, h, alpha=False)
    px = [0.0] * (w * h * 4)
    for i, (_, hx) in enumerate(PALETTE):
        c, r = i % COLS, i // COLS
        rgb = _rgb(hx)
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
    try:
        m.use_nodes = True
    except AttributeError:
        pass
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = img
    tex.interpolation = 'Closest'
    tex.location, bsdf.location, out.location = (-400, 0), (-100, 0), (250, 0)
    nt.links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
    bsdf.inputs['Roughness'].default_value = 0.85
    if 'Specular IOR Level' in bsdf.inputs:
        bsdf.inputs['Specular IOR Level'].default_value = 0.2
    nt.links.new(bsdf.outputs['BSDF'], out.inputs['Surface'])
    return m


# Box faces as corner signs in the box's own (x, y, z) frame, wound so the normal points out.
_BOX_FACES = {
    '+z': [(-1, -1, 1), (1, -1, 1), (1, 1, 1), (-1, 1, 1)],
    '-z': [(-1, -1, -1), (-1, 1, -1), (1, 1, -1), (1, -1, -1)],
    '+x': [(1, -1, -1), (1, 1, -1), (1, 1, 1), (1, -1, 1)],
    '-x': [(-1, -1, -1), (-1, -1, 1), (-1, 1, 1), (-1, 1, -1)],
    '+y': [(-1, 1, -1), (-1, 1, 1), (1, 1, 1), (1, 1, -1)],
    '-y': [(-1, -1, -1), (1, -1, -1), (1, -1, 1), (-1, -1, 1)],
}


class Builder:
    """Accumulates one building's geometry into a single bmesh."""

    def __init__(self):
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")

    def _uv(self, color):
        i = NAMES.index(color)
        c, r = i % COLS, i // COLS
        return ((c + 0.5) / COLS, 1.0 - (r + 0.5) / ROWS)

    def _face(self, verts, color):
        f = self.bm.faces.new(verts)
        uv = self._uv(color)
        for loop in f.loops:
            loop[self.uv].uv = uv
        return f

    def _bevel(self, faces, amount):
        if amount <= 0:
            return
        edges = {e for f in faces for e in f.edges}
        edges = [e for e in edges if len(e.link_faces) == 2]
        if edges:
            bmesh.ops.bevel(self.bm, geom=edges, offset=amount, offset_type='OFFSET',
                            segments=1, profile=0.5, affect='EDGES', clamp_overlap=True)

    # -- primitives -------------------------------------------------------------------------

    def obox(self, center, ax, ay, az, hx, hy, hz, color, bevel=0.0, skip=('-z',)):
        """Box in an arbitrary right-handed frame (az = ax x ay). `skip` drops hidden faces."""
        c = Vector(center)
        ax, ay, az = (Vector(a).normalized() for a in (ax, ay, az))
        v = {}
        for sx in (-1, 1):
            for sy in (-1, 1):
                for sz in (-1, 1):
                    v[(sx, sy, sz)] = self.bm.verts.new(c + ax * (hx * sx) + ay * (hy * sy) + az * (hz * sz))
        faces = [self._face([v[k] for k in ks], color)
                 for name, ks in _BOX_FACES.items() if name not in skip]
        used = {vv for f in faces for vv in f.verts}
        for vv in list(v.values()):
            if vv not in used:
                self.bm.verts.remove(vv)
        self._bevel(faces, bevel)

    def box(self, x0, x1, y0, y1, z0, z1, color, bevel=0.0, skip=('-z',)):
        self.obox(((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), (1, 0, 0), (0, 1, 0), (0, 0, 1),
                  abs(x1 - x0) / 2, abs(y1 - y0) / 2, abs(z1 - z0) / 2, color, bevel, skip)

    def zbox(self, cx, cy, z0, z1, hx, hy, angle_deg, color, bevel=0.0):
        """Box standing on the ground, turned about Z."""
        a = math.radians(angle_deg)
        ax = (math.cos(a), math.sin(a), 0)
        ay = (-math.sin(a), math.cos(a), 0)
        self.obox((cx, cy, (z0 + z1) / 2), ax, ay, (0, 0, 1), hx, hy, (z1 - z0) / 2, color, bevel)

    def prism(self, pts, origin, u, v, depth, color, bevel=0.0, back_cap=False):
        """Convex polygon `pts` drawn in the (u, v) plane at `origin`, extruded along u x v."""
        o, u, v = Vector(origin), Vector(u), Vector(v)
        n = u.cross(v).normalized()
        area = sum(pts[i][0] * pts[(i + 1) % len(pts)][1] - pts[(i + 1) % len(pts)][0] * pts[i][1]
                   for i in range(len(pts)))
        if area < 0:
            pts = list(reversed(pts))
        bot = [self.bm.verts.new(o + u * p[0] + v * p[1]) for p in pts]
        top = [self.bm.verts.new(o + u * p[0] + v * p[1] + n * depth) for p in pts]
        faces = [self._face(top, color)]
        if back_cap:
            faces.append(self._face(list(reversed(bot)), color))
        for i in range(len(pts)):
            j = (i + 1) % len(pts)
            faces.append(self._face([bot[i], bot[j], top[j], top[i]], color))
        self._bevel(faces, bevel)

    def cylinder(self, cx, cy, z0, z1, r, color, sides=8, bevel=0.0, rot_deg=22.5):
        pts = [(cx + r * math.cos(math.radians(rot_deg + 360 * i / sides)),
                cy + r * math.sin(math.radians(rot_deg + 360 * i / sides))) for i in range(sides)]
        self.prism(pts, (0, 0, z0), (1, 0, 0), (0, 1, 0), z1 - z0, color, bevel)

    def pyramid(self, cx, cy, z0, hx, hy, apex_z, color, sides=4, rot_deg=45.0):
        """Pointed roof. Four sides means a square base with half-sizes hx, hy."""
        if sides == 4:
            base = [(cx - hx, cy - hy), (cx + hx, cy - hy), (cx + hx, cy + hy), (cx - hx, cy + hy)]
        else:
            base = [(cx + hx * math.cos(math.radians(rot_deg + 360 * i / sides)),
                     cy + hy * math.sin(math.radians(rot_deg + 360 * i / sides))) for i in range(sides)]
        bv = [self.bm.verts.new((x, y, z0)) for x, y in base]
        apex = self.bm.verts.new((cx, cy, apex_z))
        for i in range(len(bv)):
            self._face([bv[i], bv[(i + 1) % len(bv)], apex], color)

    def blob(self, cx, cy, z0, r, h, color, sides=6):
        """Faceted bush/tree crown: a ring with a point above and a point below."""
        ring = [self.bm.verts.new((cx + r * math.cos(2 * math.pi * i / sides),
                                   cy + r * math.sin(2 * math.pi * i / sides), z0 + h * 0.4))
                for i in range(sides)]
        top = self.bm.verts.new((cx, cy, z0 + h))
        bot = self.bm.verts.new((cx, cy, z0))
        for i in range(sides):
            j = (i + 1) % sides
            self._face([ring[i], ring[j], top], color)
            self._face([ring[j], ring[i], bot], color)

    def gable_roof(self, cx, cy, axis, half_len, half_span, ze, zr, color, color_dark,
                   overhang=0.1, t=0.05, rib_w=0.07, rib_step=0.16, rib_h=0.025,
                   cap_w=0.14, cap_h=0.08, cap_lift=0.03):
        """Two slabs in `color_dark` meeting at a ridge along `axis`, ribbed in `color`, with a
        ridge cap. ze is the eave height at +-half_span, zr the slab underside at the ridge."""
        up = Vector((0, 0, 1))
        r = Vector((1, 0, 0)) if axis == 'x' else Vector((0, 1, 0))
        s = Vector((0, 1, 0)) if axis == 'x' else Vector((-1, 0, 0))
        c = Vector((cx, cy, 0))
        for side in (-1, 1):
            eave = c + s * (side * half_span) + up * ze
            ridge = c + up * zr
            d = (ridge - eave).normalized()
            length = (ridge - eave).length + overhang
            ax = r * (-side)
            n = ax.cross(d)
            mid = (eave - d * overhang + ridge) / 2
            self.obox(mid + n * (t / 2), ax, d, n, half_len, length / 2, t / 2, color_dark)
            x = -half_len + rib_w
            while x < half_len - rib_w * 0.7:
                self.obox(mid + n * (t + rib_h / 2) + r * x, ax, d, n, rib_w / 2, length / 2,
                          rib_h / 2, color)
                x += rib_step
        self.obox(c + up * (zr + cap_lift + cap_h / 2), r, s, up, half_len, cap_w / 2, cap_h / 2,
                  color_dark, bevel=min(0.01, cap_h / 4))

    def ring(self, cx, cy, z0, z1, r_in, r_out, colors, sides=8, rot_deg=22.5, bevel=0.0):
        """Wall of a round shaft built from `sides` blocks, colours cycling through `colors`."""
        for i in range(sides):
            a0 = math.radians(rot_deg + 360 * i / sides)
            a1 = math.radians(rot_deg + 360 * (i + 1) / sides)
            pts = [(cx + r_out * math.cos(a0), cy + r_out * math.sin(a0)),
                   (cx + r_out * math.cos(a1), cy + r_out * math.sin(a1)),
                   (cx + r_in * math.cos(a1), cy + r_in * math.sin(a1)),
                   (cx + r_in * math.cos(a0), cy + r_in * math.sin(a0))]
            self.prism(pts, (0, 0, z0), (1, 0, 0), (0, 1, 0), z1 - z0, colors[i % len(colors)],
                       bevel)

    def hcylinder(self, axis, c, half_len, r, color, sides=6):
        """Lying cylinder (axle, log) along x or y, centred on c."""
        c = Vector(c)
        ring = [(r * math.cos(2 * math.pi * i / sides), r * math.sin(2 * math.pi * i / sides))
                for i in range(sides)]
        if axis == 'x':
            pts = [(c.y + a, c.z + b) for a, b in ring]
            self.prism(pts, (c.x - half_len, 0, 0), (0, 1, 0), (0, 0, 1), 2 * half_len, color,
                       back_cap=True)
        else:
            pts = [(-c.x + a, c.z + b) for a, b in ring]
            self.prism(pts, (0, c.y - half_len, 0), (-1, 0, 0), (0, 0, 1), 2 * half_len, color,
                       back_cap=True)

    def gable_block(self, axis, a0, a1, s0, s1, ze, zr, color):
        """Attic triangle under a gable roof: ridge along `axis` from a0 to a1, spanning s0..s1
        across it, from the eaves at ze up to the ridge at zr."""
        sc = (s0 + s1) / 2
        if axis == 'x':
            self.prism([(s0, ze), (s1, ze), (sc, zr)], (a0, 0, 0), (0, 1, 0), (0, 0, 1), a1 - a0,
                       color, back_cap=True)
        else:
            self.prism([(-s0, ze), (-s1, ze), (-sc, zr)], (0, a0, 0), (-1, 0, 0), (0, 0, 1),
                       a1 - a0, color, back_cap=True)

    def dome(self, cx, cy, z0, r, h, colors, sides=8, rot_deg=22.5):
        """Faceted dome: three rings narrowing to a point, colours alternating by segment."""
        rings = [(r, 0.0), (r * 0.82, 0.45), (r * 0.45, 0.82)]
        vs = []
        for rr, hh in rings:
            vs.append([self.bm.verts.new((cx + rr * math.cos(math.radians(rot_deg + 360 * i / sides)),
                                          cy + rr * math.sin(math.radians(rot_deg + 360 * i / sides)),
                                          z0 + h * hh)) for i in range(sides)])
        apex = self.bm.verts.new((cx, cy, z0 + h))
        for i in range(sides):
            j = (i + 1) % sides
            col = colors[i % len(colors)]
            for k in range(len(rings) - 1):
                self._face([vs[k][i], vs[k][j], vs[k + 1][j], vs[k + 1][i]], col)
            self._face([vs[-1][i], vs[-1][j], apex], col)

    def rod(self, p0, p1, w, color):
        """Square beam between two points (ladder rails, poles, a telescope)."""
        p0, p1 = Vector(p0), Vector(p1)
        d = p1 - p0
        ax = d.normalized()
        helper = Vector((0, 0, 1)) if abs(ax.z) < 0.9 else Vector((1, 0, 0))
        ay = helper.cross(ax).normalized()
        az = ax.cross(ay)
        self.obox((p0 + p1) / 2, ax, ay, az, d.length / 2, w / 2, w / 2, color, skip=())

    # -- output -----------------------------------------------------------------------------

    def triangles(self):
        return sum(len(f.verts) - 2 for f in self.bm.faces)

    def to_mesh(self, name):
        for f in self.bm.faces:
            f.smooth = False
        me = bpy.data.meshes.new(name)
        self.bm.to_mesh(me)
        self.bm.free()
        return me


_FACADE_AXES = {
    # outward normal -> (horizontal axis a with a x up = normal, normal)
    '-y': ((1, 0, 0), (0, -1, 0)),
    '+y': ((-1, 0, 0), (0, 1, 0)),
    '+x': ((0, 1, 0), (1, 0, 0)),
    '-x': ((0, -1, 0), (-1, 0, 0)),
}


class Facade:
    """Draws flat details onto one vertical wall plane.

    `s` is always the WORLD coordinate along the wall (x for a ±y wall, y for a ±x wall), `z`
    is height; `depth` is how far the detail stands proud of the wall.
    """

    def __init__(self, builder, normal, plane):
        a, n = _FACADE_AXES[normal]
        self.b = builder
        self.a, self.n, self.up = Vector(a), Vector(n), Vector((0, 0, 1))
        self.sa = sum(a)  # +1 or -1: converts a world coordinate into the a-axis coordinate
        self.w = self.a * self.sa  # world axis the s coordinate runs along
        self.origin = self.n * (plane * sum(n))

    def _p(self, s, z):
        return self.origin + self.w * s + self.up * z

    def rect(self, s0, s1, z0, z1, depth, color, bevel=0.0):
        c = self._p((s0 + s1) / 2, (z0 + z1) / 2) + self.n * (depth / 2)
        self.b.obox(c, self.a, self.up, self.n, abs(s1 - s0) / 2, abs(z1 - z0) / 2, depth / 2,
                    color, bevel)

    def beam(self, s0, z0, s1, z1, width, depth, color):
        p0, p1 = self._p(s0, z0), self._p(s1, z1)
        d = p1 - p0
        ax = d.normalized()
        ay = self.n.cross(ax)
        c = (p0 + p1) / 2 + self.n * (depth / 2)
        self.b.obox(c, ax, ay, self.n, d.length / 2, width / 2, depth / 2, color)

    def poly(self, pts, depth, color, bevel=0.0):
        """Convex outline given as (s, z) world pairs."""
        local = [(s * self.sa, z) for s, z in pts]
        self.b.prism(local, self.origin, self.a, self.up, depth, color, bevel)

    def arch(self, s, z0, w, h, depth, color, segments=6, bevel=0.0):
        """Round-topped opening: straight sides, semicircle on top. h is the total height."""
        r = w / 2
        spring = z0 + h - r
        pts = [(s - r, z0), (s + r, z0)]
        for i in range(segments + 1):
            t = math.pi * i / segments
            pts.append((s + r * math.cos(t), spring + r * math.sin(t)))
        self.poly(pts, depth, color, bevel)

    def disc(self, s, z, r, depth, color, sides=8):
        pts = [(s + r * math.cos(math.radians(22.5 + 360 * i / sides)),
                z + r * math.sin(math.radians(22.5 + 360 * i / sides))) for i in range(sides)]
        self.poly(pts, depth, color)


# -- scene plumbing ---------------------------------------------------------------------------
#
# The user's placeholder file is the authority on sizes. Each model script reads its box from
# there on every run, so an updated scene is picked up without anyone copying numbers over.

SIZE_COLLECTION = "Габариты"
SOURCE_FILE = "Buildings-Size1.blend"


def _size_collection():
    coll = bpy.data.collections.get(SIZE_COLLECTION)
    if coll is None:
        coll = bpy.data.collections.new(SIZE_COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    return coll


def refresh_placeholder(obj_name):
    """Loads `obj_name` from the user's source file and makes it the wireframe `<name>-size`
    reference here. Returns (location, min_xyz, max_xyz) of the box in local space."""
    import os
    source = os.path.join(os.path.dirname(bpy.data.filepath), SOURCE_FILE)
    with bpy.data.libraries.load(source, link=False) as (data_from, data_to):
        if obj_name not in data_from.objects:
            raise RuntimeError(f"{obj_name} is not in {source}")
        data_to.objects = [obj_name]
    src = data_to.objects[0]
    ref_name = obj_name + "-size"
    old = bpy.data.objects.get(ref_name)
    if old is not None:
        old_mesh = old.data
        bpy.data.objects.remove(old)
        if old_mesh.users == 0:
            bpy.data.meshes.remove(old_mesh)
    src.name = ref_name
    src.data.name = ref_name
    src.display_type = 'WIRE'
    src.hide_render = True
    _size_collection().objects.link(src)
    vs = [Vector((v.co.x * src.scale.x, v.co.y * src.scale.y, v.co.z * src.scale.z))
          for v in src.data.vertices]
    lo = tuple(min(v[i] for v in vs) for i in range(3))
    hi = tuple(max(v[i] for v in vs) for i in range(3))
    return src.location.copy(), lo, hi


def install_model(obj_name, builder, material, location):
    obj = bpy.data.objects[obj_name]
    old = obj.data
    me = builder.to_mesh(obj_name)
    me.materials.append(material)
    obj.data = me
    obj.location = location
    if old.users == 0:
        bpy.data.meshes.remove(old)
    return obj


def report_bounds(obj, lo, hi):
    xs = [v.co.x for v in obj.data.vertices]
    ys = [v.co.y for v in obj.data.vertices]
    zs = [v.co.z for v in obj.data.vertices]
    tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    got_lo, got_hi = (min(xs), min(ys), min(zs)), (max(xs), max(ys), max(zs))
    print(f"MODEL {obj.name}: tris={tris} verts={len(xs)} "
          f"x[{got_lo[0]:.3f},{got_hi[0]:.3f}] y[{got_lo[1]:.3f},{got_hi[1]:.3f}] "
          f"z[{got_lo[2]:.3f},{got_hi[2]:.3f}]")
    ok = all(got_lo[i] >= lo[i] - 1e-4 and got_hi[i] <= hi[i] + 1e-4 for i in range(3))
    print(("FITS" if ok else "DOES NOT FIT") + f" box lo={tuple(round(x, 3) for x in lo)} "
          f"hi={tuple(round(x, 3) for x in hi)}")


def run(obj_name, build):
    """The whole per-building pipeline: palette, material, size box, model, check, save.
    `build(lo, hi)` gets the placeholder box and returns a Builder."""
    import os
    img = ensure_palette(os.path.join(os.path.dirname(bpy.data.filepath), "Buildings-Palette.png"))
    mat = ensure_material(img)
    location, lo, hi = refresh_placeholder(obj_name)
    if lo[2] > 1e-4:
        print(f"NOTE: the {obj_name} box starts {lo[2]:.3f} above the ground; modelling from 0")
        lo = (lo[0], lo[1], 0.0)
    obj = install_model(obj_name, build(lo, hi), mat, location)
    report_bounds(obj, lo, hi)
    file_display_settings()
    bpy.ops.wm.save_mainfile()


def file_display_settings():
    """Standard view transform (what Unity's linear pipeline shows) and textured solid view."""
    sc = bpy.context.scene
    sc.view_settings.view_transform = 'Standard'
    sc.view_settings.look = 'None'
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == 'VIEW_3D':
                for space in area.spaces:
                    if space.type == 'VIEW_3D':
                        space.shading.color_type = 'TEXTURE'
