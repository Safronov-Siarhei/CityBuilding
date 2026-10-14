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

    ("roof_red_dark", "A82E2B"), ("burlap", "D6B98C"),
    ("hay", "EBCB5E"), ("fire", "FF8A2A"),
    ("roof_slate", "6A7789"), ("roof_slate_dark", "475263"),
    ("roof_straw", "EDB63E"), ("roof_straw_dark", "B98128"),
]
NAMES = [n for n, _ in PALETTE]

MATERIAL_NAME = "Buildings"
PALETTE_IMAGE = "BuildingsPalette"


def _rgb(h):
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def ensure_palette(png_path):
    """(Re)paints the palette image, saves it next to the .blend and packs it in."""
    w, h = COLS * CELL, ROWS * CELL
    # Always a fresh image: repainting a PACKED one changes its buffer but the file keeps the old
    # packed bytes, so a colour added to the palette rendered black (found 2026-10-12).
    old = bpy.data.images.get(PALETTE_IMAGE)
    if old is not None:
        bpy.data.images.remove(old)
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

    def frustum(self, cx, cy, z0, z1, r0, r1, color, sides=8, rot_deg=22.5, cap=True):
        """Tapered shaft: a `sides`-gon of radius r0 at z0 narrowing to r1 at z1 (a windmill's
        tower, a furnace stack)."""
        def ring(r, z):
            return [self.bm.verts.new((cx + r * math.cos(math.radians(rot_deg + 360 * i / sides)),
                                       cy + r * math.sin(math.radians(rot_deg + 360 * i / sides)), z))
                    for i in range(sides)]
        lo, hi = ring(r0, z0), ring(r1, z1)
        for i in range(sides):
            j = (i + 1) % sides
            self._face([lo[i], lo[j], hi[j], hi[i]], color)
        if cap:
            self._face(hi, color)

    _JITTER = (0.0, 0.18, -0.12, 0.1, -0.2, 0.14, -0.06, 0.2, -0.16, 0.08, -0.1, 0.16, -0.04)

    def crag(self, cx, cy, rx, ry, h, color, top=None, sides=6, rot_deg=0.0, taper=0.55,
             lean=(0.0, 0.0), seed=0):
        """A faceted rock: an irregular `sides`-gon on the ground narrowing through a shoulder to
        a tilted crown, which can take its own colour (`top`: turf on a hill). Deterministic --
        `seed` picks the irregularity."""
        rings = []
        for k, (fz, fr) in enumerate(((0.0, 1.0), (0.55, 0.86), (1.0, taper))):
            ring = []
            for i in range(sides):
                a = math.radians(rot_deg + 360 * i / sides)
                j = 1.0 + self._JITTER[(seed + i * 3 + k) % len(self._JITTER)]
                ring.append(self.bm.verts.new((
                    cx + lean[0] * fz + rx * fr * j * math.cos(a),
                    cy + lean[1] * fz + ry * fr * j * math.sin(a),
                    h * fz * (1.0 + (0.08 * self._JITTER[(seed + i) % len(self._JITTER)]
                                     if k == 2 else 0.0)))))
            rings.append(ring)
        faces = []
        for k in range(2):
            for i in range(sides):
                j = (i + 1) % sides
                f = self._face([rings[k][i], rings[k][j], rings[k + 1][j], rings[k + 1][i]], color)
                f.normal_update()
                faces.append((f.calc_center_median().copy(), f.normal.copy()))
        self._face(rings[2], top or color)
        return faces  # (centre, outward normal) of each side face: where ore can show

    def ball(self, cx, cy, z0, r, h, color, sides=7, top=None):
        """A rounded faceted crown: a flat underside, a waist at mid height and a domed top --
        a leafy tree, not the two points of a blob. `top` colours the upper band (sunlit
        leaves)."""
        rings = [(0.55, 0.0), (0.95, 0.28), (1.0, 0.55), (0.62, 0.86)]
        vs = [[self.bm.verts.new((cx + r * f * math.cos(2 * math.pi * (i + 0.5 * (k % 2)) / sides),
                                  cy + r * f * math.sin(2 * math.pi * (i + 0.5 * (k % 2)) / sides),
                                  z0 + h * fz)) for i in range(sides)]
              for k, (f, fz) in enumerate(rings)]
        apex = self.bm.verts.new((cx, cy, z0 + h))
        self._face(list(reversed(vs[0])), color)
        for k in range(len(rings) - 1):
            col = (top or color) if k == len(rings) - 2 else color
            for i in range(sides):
                j = (i + 1) % sides
                if k % 2 == 0:
                    self._face([vs[k][i], vs[k][j], vs[k + 1][i]], col)
                    self._face([vs[k][j], vs[k + 1][j], vs[k + 1][i]], col)
                else:
                    self._face([vs[k][i], vs[k][j], vs[k + 1][j]], col)
                    self._face([vs[k][i], vs[k + 1][j], vs[k + 1][i]], col)
        for i in range(sides):
            self._face([vs[-1][i], vs[-1][(i + 1) % sides], apex], top or color)

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
                   cap_w=0.14, cap_h=0.08, cap_lift=0.03, smooth=True):
        """Two slabs meeting at a ridge along `axis`. ze is the eave height at +-half_span, zr the
        slab underside at the ridge.

        smooth (the set's look since 2026-10-14): each slope is one softly chamfered slab in
        `color` -- the tiles are painted on by the detail atlas (detail.py), not built -- with a
        thin verge board in `color_dark` along each gable end and a round ridge roll. The old
        look, a `color_dark` slab ribbed in `color` with a square cap, is smooth=False."""
        if smooth:
            self._smooth_gable(cx, cy, axis, half_len, half_span, ze, zr, color, color_dark,
                               overhang, t + rib_h * 0.5, cap_lift)
            return
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

    def _smooth_gable(self, cx, cy, axis, half_len, half_span, ze, zr, color, color_dark,
                      overhang, t, cap_lift):
        up = Vector((0, 0, 1))
        r = Vector((1, 0, 0)) if axis == 'x' else Vector((0, 1, 0))
        s = Vector((0, 1, 0)) if axis == 'x' else Vector((-1, 0, 0))
        c = Vector((cx, cy, 0))
        verge = min(0.014, half_len * 0.05)
        for side in (-1, 1):
            eave = c + s * (side * half_span) + up * ze
            ridge = c + up * zr
            d = (ridge - eave).normalized()
            length = (ridge - eave).length + overhang
            ax = r * (-side)
            n = ax.cross(d)
            mid = (eave - d * overhang + ridge) / 2
            self.obox(mid + n * (t / 2), ax, d, n, half_len, length / 2, t / 2, color,
                      bevel=min(0.012, t / 3))
            for e in (-1, 1):
                # inside the slab's own length, so the roof never grows past where it used to end
                self.obox(mid + r * (e * (half_len - verge)) + n * (t * 0.6), ax, d, n, verge,
                          length / 2, t * 0.75, color_dark)
        roll = max(0.025, min(0.055, half_span * 0.12))
        self.hcylinder(axis, (cx, cy, zr + t + cap_lift * 0.5 + roll * 0.4), half_len,
                       roll, color_dark, sides=8)

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

    def segment_ring(self, pts_out, pts_in, z0, z1, colors, bevel=0.0):
        """A wall built from one block per segment between two outlines of equal length (an
        ellipse, an octagon), colours cycling."""
        n = len(pts_out)
        for i in range(n):
            j = (i + 1) % n
            self.prism([pts_out[i], pts_out[j], pts_in[j], pts_in[i]], (0, 0, z0), (1, 0, 0),
                       (0, 1, 0), z1 - z0, colors[i % len(colors)], bevel)

    def ring_roof(self, pts_out, pts_in, z_out, z_in, colors, fascia=0.04):
        """Roof over a ring building (a round theatre): one sloped quad per segment from the outer
        outline (counter-clockwise, at z_out) to the inner one (at z_in), colours alternating, with
        a fascia along the outer eave."""
        n = len(pts_out)
        for i in range(n):
            j = (i + 1) % n
            col = colors[i % len(colors)]
            o0 = self.bm.verts.new((pts_out[i][0], pts_out[i][1], z_out))
            o1 = self.bm.verts.new((pts_out[j][0], pts_out[j][1], z_out))
            i1 = self.bm.verts.new((pts_in[j][0], pts_in[j][1], z_in))
            i0 = self.bm.verts.new((pts_in[i][0], pts_in[i][1], z_in))
            self._face([o0, o1, i1, i0], col)
            if fascia <= 0:
                continue
            l0 = self.bm.verts.new((pts_out[i][0], pts_out[i][1], z_out - fascia))
            l1 = self.bm.verts.new((pts_out[j][0], pts_out[j][1], z_out - fascia))
            self._face([l0, l1, o1, o0], colors[-1])

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

    def blossom(self, s, z, depth, r, color):
        """One flower head standing `depth` proud of the wall (a planter's bloom)."""
        p = self._p(s, z) + self.n * depth
        self.b.blob(p.x, p.y, p.z - r * 0.4, r, r * 1.5, color, sides=4)

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


class FaceAt(Facade):
    """A Facade on a wall facing any horizontal direction: the face of a polygonal tower, a
    round theatre, an arena. `s` is measured from the face's centre along the wall."""

    def __init__(self, builder, cx, cy, angle_deg):
        self.b = builder
        a = math.radians(angle_deg)
        self.n = Vector((math.cos(a), math.sin(a), 0))
        self.up = Vector((0, 0, 1))
        self.a = self.up.cross(self.n)
        self.sa = 1
        self.w = self.a
        self.origin = Vector((cx, cy, 0))


def polygon_faces(points):
    """For a convex outline given counter-clockwise, yields (cx, cy, angle_deg, width) for each
    edge: its midpoint, the direction it faces, and its length -- ready for FaceAt."""
    for i in range(len(points)):
        x0, y0 = points[i]
        x1, y1 = points[(i + 1) % len(points)]
        ex, ey = x1 - x0, y1 - y0
        yield ((x0 + x1) / 2, (y0 + y1) / 2, math.degrees(math.atan2(-ex, ey)),
               math.hypot(ex, ey))


# -- scene plumbing ---------------------------------------------------------------------------
#
# The user's placeholder file is the authority on sizes. Each model script reads its box from
# there on every run, so an updated scene is picked up without anyone copying numbers over.

SIZE_COLLECTION = "Габариты"
SOURCE_FILE = "Buildings-Size1.blend"
PLOT = None  # the current model's placeholder box (lo, hi), set by run()


def _size_collection():
    coll = bpy.data.collections.get(SIZE_COLLECTION)
    if coll is None:
        coll = bpy.data.collections.new(SIZE_COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    return coll


def refresh_placeholder(obj_name, source_name=None):
    """Loads the placeholder (`source_name`, default `obj_name`) from the user's source file and
    makes it the wireframe `<obj_name>-size` reference here. Returns (location, min_xyz, max_xyz)
    of the box in local space."""
    import os
    source = os.path.join(os.path.dirname(bpy.data.filepath), SOURCE_FILE)
    wanted = source_name or obj_name
    with bpy.data.libraries.load(source, link=False) as (data_from, data_to):
        if wanted not in data_from.objects:
            raise RuntimeError(f"{wanted} is not in {source}")
        data_to.objects = [wanted]
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


def install_model(obj_name, builder, material, location, collection=None):
    obj = bpy.data.objects.get(obj_name)
    if obj is None:  # a piece the user's file has no placeholder of its own for (a fence's T)
        obj = bpy.data.objects.new(obj_name, bpy.data.meshes.new(obj_name + "-empty"))
        coll = bpy.data.collections.get(collection) if collection else None
        (coll or bpy.context.scene.collection).objects.link(obj)
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


def run(obj_name, build, source_name=None, offset=(0, 0, 0), collection=None, save=True):
    """The whole per-building pipeline: palette, material, size box, model, check, save.
    `build(lo, hi)` gets the placeholder box and returns a Builder. A model without a placeholder
    of its own borrows `source_name`'s box and stands `offset` away from it."""
    import os
    import detail
    img = ensure_palette(os.path.join(os.path.dirname(bpy.data.filepath), "Buildings-Palette.png"))
    mat = ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(os.path.dirname(bpy.data.filepath), detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    location, lo, hi = refresh_placeholder(obj_name, source_name)
    if lo[2] > 1e-4:
        print(f"NOTE: the {obj_name} box starts {lo[2]:.3f} above the ground; modelling from 0")
        lo = (lo[0], lo[1], 0.0)
    global PLOT
    PLOT = (lo, hi)  # what shared details (a grass tuft) must stay inside -- see details.grass_tuft
    location = location + Vector(offset)
    ref = bpy.data.objects.get(obj_name + "-size")
    if ref is not None:
        ref.location = location
    obj = install_model(obj_name, build(lo, hi), mat, location, collection)
    detail.add_layers(obj.data, NAMES, COLS, ROWS)
    report_bounds(obj, lo, hi)
    file_display_settings()
    if save:
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
