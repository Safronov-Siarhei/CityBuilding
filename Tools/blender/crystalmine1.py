"""The Crystal Mine (Рудник кристаллов, the user 2026-11-14): the PAID building (~$8 in the shop), one per
sandbox map, mining the game's crystals. Footprint 2x2 (x/y -1..1), front is -y. Pure builders (no bpy),
like cemetery1; crystalmine_install.py puts them in the models file.

It is bought for real money, so it must look rich (the user): a rock hill split by great violet crystal
clusters (the game's crystal colour), a dressed stone portal trimmed in gold with a violet gem for its
keystone and royal blue banners, crystals glowing in the dark mouth, rails out to a cart heaped with
shards, an open iron-bound chest of crystals and a cutter's bench with a stone being cut.

Level 1 is made first for the user's approval; levels 2-5 come after it (the roster's way).
"""
import math

import details as d
from details import ADIT_H, ADIT_W
from kit import Builder, Facade
from mathutils import Vector

CRYSTAL, CRYSTAL_DARK, GLINT = 'roof_purple', 'roof_purple_dark', 'white'


def shard(b, base, direction, length, r, sides=6, glint=False, spin=0.0):
    """One crystal: a `sides`-sided prism from `base` along `direction`, narrowing a little, ending in a
    point; the faces alternate light and dark violet so it reads as cut. `glint` whitens one tip face."""
    base = Vector(base)
    ax = Vector(direction).normalized()
    helper = Vector((0, 0, 1)) if abs(ax.z) < 0.9 else Vector((1, 0, 0))
    u = helper.cross(ax).normalized()
    v = ax.cross(u)
    body = length * 0.72

    def ring(rad, along):
        c = base + ax * along
        return [b.bm.verts.new(c + (u * math.cos(spin + 2 * math.pi * i / sides) +
                                    v * math.sin(spin + 2 * math.pi * i / sides)) * rad) for i in range(sides)]

    lo, hi = ring(r, -0.04), ring(r * 0.86, body)
    tip = b.bm.verts.new(base + ax * length)
    for i in range(sides):
        j = (i + 1) % sides
        col = CRYSTAL if i % 2 == 0 else CRYSTAL_DARK
        b._face([lo[i], lo[j], hi[j], hi[i]], col)
        b._face([hi[i], hi[j], tip], GLINT if glint and i == 1 else col)


def cluster(b, x, y, z, scale=1.0, seed=0, main_lean=(0.0, 0.0)):
    """A clump of crystals: a tall one in the middle and smaller ones leaning out round it."""
    s = scale
    shard(b, (x, y, z), (main_lean[0], main_lean[1], 1.0), 0.62 * s, 0.085 * s, glint=True, spin=seed)
    ring = ((0.9, 0.42, 0.06), (2.6, 0.36, 0.055), (4.1, 0.3, 0.05), (5.4, 0.26, 0.045))
    for k, (a, ln, r) in enumerate(ring):
        a += seed
        dx, dy = math.cos(a), math.sin(a)
        shard(b, (x + dx * 0.05 * s, y + dy * 0.05 * s, z), (dx * 0.55, dy * 0.55, 1.0), ln * s, r * s,
              glint=(k == 0), spin=seed + k)


def loose(b, x, y, h=0.1, lean=(0.3, -0.2), spin=0.0):
    """A small shard sticking out of the ground."""
    shard(b, (x, y, 0.0), (lean[0], lean[1], 1.0), h, h * 0.28, sides=5, spin=spin)


def _arc(s, r, spring, segments=8):
    """The points of a semicircle of radius r over (s, spring), left to right."""
    return [(s - r * math.cos(math.pi * i / segments), spring + r * math.sin(math.pi * i / segments))
            for i in range(segments + 1)]


def portal(b, s, yf, depth=0.14):
    """The mine's mouth: a dressed stone face with a gold-trimmed arch OPEN into the cave behind it, a violet
    gem as the keystone, a gilded cornice, a pediment with a gold crown in relief and royal banners."""
    top = 0.96
    yb = yf + depth
    w, h = ADIT_W, ADIT_H
    r = w / 2
    spring = h - r
    # the face, built round the opening: two piers, a lintel, the spandrels over the round arch
    b.box(s - 0.42, s - r, yf, yb, 0, top, 'stone_light', bevel=0.012)
    b.box(s + r, s + 0.42, yf, yb, 0, top, 'stone_light', bevel=0.012)
    b.box(s - r, s + r, yf, yb, h, top, 'stone_light')
    back = Facade(b, '-y', yb)
    arc = _arc(s, r, spring)
    for (s0, z0), (s1, z1) in zip(arc, arc[1:]):
        corner = (s - r, h) if s1 <= s else (s + r, h)
        back.poly([corner, (s0, z0), (s1, z1)], depth, 'stone_light')
    # the cave's light on the inside of the mouth: its reveals and floor in violet
    for k in (-1, 1):
        x0, x1 = sorted((s + k * r, s + k * (r - 0.012)))
        b.box(x0, x1, yf + 0.01, yb, 0, spring, CRYSTAL_DARK)
    b.box(s - r, s + r, yf, yb, 0, 0.008, CRYSTAL_DARK)
    # the arch's mouldings: a stone ring and a gold one round the opening, standing proud of the face
    for rad, wdt, col, proud in ((r + 0.07, 0.07, 'stone_mid', 0.02), (r + 0.025, 0.035, 'gold', 0.035)):
        path = [(s - rad, 0.0), (s - rad, spring)] + _arc(s, rad, spring)[1:-1] + [(s + rad, spring), (s + rad, 0.0)]
        for (s0, z0), (s1, z1) in zip(path, path[1:]):
            b.rod((s0, yf - proud / 2, z0), (s1, yf - proud / 2, z1), wdt, col)

    b.box(s - 0.46, s + 0.46, yf - 0.03, yb + 0.02, top, top + 0.06, 'stone_mid', bevel=0.012)   # cornice
    b.box(s - 0.44, s + 0.44, yf - 0.035, yf + 0.0, top - 0.03, top, 'gold')                       # its gilt band
    d.quoins(b, s - 0.42, yf, -1, -1, 0.0, top, color='stone_mid')
    d.quoins(b, s + 0.42, yf, 1, -1, 0.0, top, color='stone_mid')
    # the pediment: a solid stone gable as deep as the face, edged in gold
    apex = top + 0.36
    Facade(b, '-y', yb).poly([(s - 0.42, top + 0.06), (s + 0.42, top + 0.06), (s, apex)], depth, 'stone_light')
    for k in (-1, 1):
        b.rod((s + k * 0.44, yf - 0.012, top + 0.06), (s, yf - 0.012, apex + 0.02), 0.03, 'gold')
    f = Facade(b, '-y', yf)
    f.poly([(s - 0.33, top + 0.09), (s + 0.33, top + 0.09), (s, apex - 0.06)], 0.012, 'stone_mid')
    # a gold crown in relief on the pediment: the royal mark of a paid building
    cz = top + 0.12
    f.rect(s - 0.09, s + 0.09, cz, cz + 0.05, 0.03, 'gold')
    for k in (-1, 0, 1):
        tip = cz + 0.12 + (0.03 if k == 0 else 0)
        f.poly([(s + k * 0.065 - 0.028, cz + 0.05), (s + k * 0.065 + 0.028, cz + 0.05), (s + k * 0.065, tip)], 0.03, 'gold')
    f.rect(s - 0.02, s + 0.02, cz + 0.012, cz + 0.038, 0.04, 'roof_red')
    # the keystone: a cut violet gem in a gold setting
    kz = h + 0.11
    f.poly([(s - 0.06, kz + 0.06), (s + 0.06, kz + 0.06), (s + 0.045, kz - 0.06), (s - 0.045, kz - 0.06)],
           0.045, 'gold')
    shard(b, (s, yf - 0.05, kz - 0.045), (0, 0, 1), 0.13, 0.04, sides=6, glint=True)
    # banners either side of the arch, blue with a gold device
    for k in (-1, 1):
        d.banner(f, s + k * 0.33, top - 0.06, 0.42, w=0.1, color='banner_blue', emblem='gold')


def cave(b, s, y0, y1=None, half=0.3, high=0.68):
    """The crystal cave behind the mouth: violet walls (darker by the mouth, light at the back, the
    light of the crystals), crystals growing from the floor, the walls and the roof."""
    y1 = y1 if y1 is not None else y0 + 0.44
    t = 0.05
    mid = (y0 + y1) / 2
    for k in (-1, 1):
        x0, x1 = sorted((s + k * half, s + k * (half + 0.012)))     # the lit inner face
        b.box(x0, x1, y0, mid, 0, high, CRYSTAL_DARK)
        b.box(x0, x1, mid, y1, 0, high, CRYSTAL)
        x0, x1 = sorted((s + k * (half + 0.012), s + k * (half + t)))   # rough rock outside
        b.box(x0, x1, y0, y1 + t, 0, high + t, 'stone_dark')
    b.box(s - half - t, s + half + t, y0, y1 + t, high, high + t, 'stone_dark')
    b.box(s - half, s + half, y1, y1 + 0.012, 0, high, CRYSTAL)
    b.box(s - half - t, s + half + t, y1 + 0.012, y1 + t, 0, high + t, 'stone_dark')
    b.box(s - half, s + half, y0, y1, 0, 0.008, CRYSTAL)
    # the crystals: a clump at the back, more from the walls, two hanging from the roof
    cluster(b, s + 0.1, y0 + 0.24, 0.0, scale=0.5, seed=0.9, main_lean=(-0.1, -0.2))
    cluster(b, s - 0.12, y1 - 0.08, 0.0, scale=0.45, seed=2.2, main_lean=(0.1, -0.2))
    for k, (yy, zz, ln) in ((-1, (y1 - 0.14, 0.12, 0.2)), (1, (y1 - 0.2, 0.2, 0.17)), (-1, (mid, 0.32, 0.14)),
                            (1, (mid - 0.04, 0.06, 0.15))):
        shard(b, (s + k * half, yy, zz), (-k * 1.0, -0.2, 0.5), ln, ln * 0.25, sides=5, glint=True)
    for xx, yy, ln in ((-0.1, y1 - 0.18, 0.14), (0.14, mid, 0.11)):
        shard(b, (s + xx, yy, high), (0.1, -0.1, -1.0), ln, ln * 0.24, sides=5)
    for xx, yy in ((-0.18, mid + 0.04), (0.2, y1 - 0.06)):
        loose(b, s + xx, yy, 0.12, (0.2 * (1 if xx < 0 else -1), -0.3), xx)


def chest(b, cx, cy, angle=-12):
    """An open iron-bound chest spilling crystals."""
    b.zbox(cx, cy, 0, 0.15, 0.14, 0.1, angle, 'wood_mid', bevel=0.012)
    a = math.radians(angle)
    for t in (-0.09, 0.09):
        b.zbox(cx + t * math.cos(a), cy + t * math.sin(a), 0, 0.155, 0.016, 0.105, angle, 'gold')
    # the lid thrown back
    back = Vector((-math.sin(a), math.cos(a), 0)) * 0.1
    b.zbox(cx + back.x * 1.15, cy + back.y * 1.15, 0.13, 0.32, 0.145, 0.02, angle, 'wood_dark', bevel=0.008)
    b.zbox(cx + back.x * 1.15, cy + back.y * 1.15, 0.31, 0.33, 0.145, 0.025, angle, 'gold')
    for ox, oy, h in ((0.0, 0.0, 0.14), (-0.06, 0.02, 0.1), (0.06, -0.02, 0.11), (0.03, 0.04, 0.08)):
        shard(b, (cx + ox, cy + oy, 0.14), (ox * 3, oy * 3 - 0.2, 1.0), h, 0.035, sides=5, glint=(h > 0.13))
    b.blob(cx, cy, 0.13, 0.11, 0.05, CRYSTAL_DARK, sides=6)


def cutter_bench(b, cx, cy):
    """A gem cutter's bench: a heavy table, a raw crystal in a clamp, a grinding wheel, cut stones."""
    x0, x1, y0, y1, h = cx - 0.2, cx + 0.2, cy - 0.1, cy + 0.1, 0.2
    b.box(x0, x1, y0, y1, h - 0.03, h, 'wood_light', bevel=0.008)
    for px in (x0 + 0.03, x1 - 0.03):
        for py in (y0 + 0.03, y1 - 0.03):
            b.box(px - 0.018, px + 0.018, py - 0.018, py + 0.018, 0, h - 0.03, 'wood_dark')
    shard(b, (cx - 0.08, cy + 0.01, h), (-0.25, 0.1, 1.0), 0.22, 0.05, glint=True)        # the raw stone
    b.box(cx - 0.13, cx - 0.03, cy - 0.04, cy + 0.06, h, h + 0.03, 'iron')                 # its clamp
    b.hcylinder('y', (cx + 0.1, cy, h + 0.07), 0.015, 0.065, 'stone_mid', sides=10)        # grinding wheel
    b.box(cx + 0.08, cx + 0.12, cy - 0.03, cy + 0.03, h, h + 0.04, 'wood_dark')
    for k, (ox, oy) in enumerate(((0.02, -0.06), (0.06, -0.07), (0.15, -0.06))):          # cut gems
        b.pyramid(cx + ox, cy + oy, h, 0.018, 0.018, h + 0.03, CRYSTAL if k % 2 else GLINT, sides=6)
    b.zbox(cx + 0.27, cy - 0.02, 0, 0.13, 0.05, 0.05, 0, 'wood_mid', bevel=0.006)          # the cutter's stool


HILL_H = {1: 1.05, 2: 1.35, 3: 1.5, 4: 1.6, 5: 1.7}   # the crystal mountain grows with the level (the user)


def ivy(b, x, y, z0, z1, side):
    """Ivy falling down a corner of the portal: leafy clumps getting smaller downwards, a few blooms."""
    n = 5
    for i in range(n):
        t = i / (n - 1)
        z = z1 - (z1 - z0) * t
        r = 0.07 - 0.03 * t
        b.ball(x + side * 0.02 * (i % 2), y, z - r, r, r * 1.6, 'leaf', sides=6, top='grass')
        if i % 2 == 1:
            b.blob(x + side * 0.03, y - 0.05, z - r * 0.6, 0.02, 0.03, 'plaster_pink', sides=4)


def _cone(b, x, y, z0, r, apex, color, sides=6, rot=0.0):
    """A conifer tier, as trees1.cone (the game's trees): a cone closed underneath."""
    ring = [b.bm.verts.new((x + r * math.cos(math.radians(rot + 360 * i / sides)),
                            y + r * math.sin(math.radians(rot + 360 * i / sides)), z0)) for i in range(sides)]
    top = b.bm.verts.new((x, y, apex))
    for i in range(sides):
        b._face([ring[i], ring[(i + 1) % sides], top], color)
    b._face(list(reversed(ring)), color)


def small_fir(b, x, y, z, s=1.0):
    """The map's young fir (trees1 Tree9) at scale s, standing at height z."""
    b.cylinder(x, y, z, z + 0.14 * s, 0.035 * s, 'wood_dark', sides=6, rot_deg=0)
    _cone(b, x, y, z + 0.1 * s, 0.24 * s, z + 0.52 * s, 'leaf')
    _cone(b, x, y, z + 0.36 * s, 0.16 * s, z + 0.8 * s, 'grass_dark', rot=30)


def small_round(b, x, y, z, s=1.0):
    """The map's young round tree (trees1 Tree1) at scale s, standing at height z."""
    b.cylinder(x, y, z, z + 0.36 * s, 0.045 * s, 'wood_mid', sides=6, rot_deg=0)
    b.ball(x, y, z + 0.3 * s, 0.3 * s, 0.7 * s, 'leaf', sides=7, top='grass_dark')


def shrub(b, x, y, r=0.1):
    """A low bush in the map trees' look: a leafy ball with a darker crown."""
    b.ball(x, y, 0.0, r, r * 1.5, 'leaf', sides=7, top='grass_dark')


def hoist(b, x, y, z):
    """A timber hoist on a crag: an A-frame, a jib reaching out over the yard, a rope and a basket of
    crystals half way up (the mine's riches are hauled out over the top as well as through the mouth)."""
    for k in (-1, 1):
        b.rod((x + k * 0.1, y + 0.08, z), (x, y + 0.02, z + 0.5), 0.035, 'wood_mid')
    b.rod((x - 0.07, y + 0.06, z + 0.16), (x + 0.07, y + 0.06, z + 0.16), 0.03, 'wood_dark')
    b.rod((x, y + 0.04, z + 0.48), (x + 0.02, y - 0.4, z + 0.52), 0.035, 'wood_light')     # the jib
    b.hcylinder('x', (x, y + 0.06, z + 0.12), 0.07, 0.04, 'wood_dark', sides=8)           # the winch drum
    bx, by = x + 0.02, y - 0.38
    b.rod((bx, by, z + 0.5), (bx, by, z - 0.08), 0.012, 'burlap')                        # the rope
    bz = z - 0.2
    b.frustum(bx, by, bz, bz + 0.1, 0.055, 0.07, 'wood_light', sides=8)
    b.ring(bx, by, bz + 0.085, bz + 0.1, 0.06, 0.075, ('iron',), sides=8)
    for k, (ox, oy) in enumerate(((0.0, 0.0), (0.03, -0.02), (-0.03, 0.02))):
        shard(b, (bx + ox, by + oy, bz + 0.08), (ox * 6, oy * 6, 1.0), 0.09 - 0.015 * k, 0.025, sides=5, glint=(k == 0))


PEDIMENT_TOP, PEDIMENT_RISE = 0.96 + 0.06, 0.3   # where portal() puts its pediment's base and apex


def pediment_roof(b, s, yf, depth, color, diamonds=None, crest=False):
    """Levels 4-5: the pediment gets a roof of its own pitch -- two slabs from the cornice to the apex, `color`
    (violet, then gold), with a lattice of `diamonds` (gold) and a gilded crest along the ridge."""
    import math
    z0, apex = PEDIMENT_TOP - 0.01, PEDIMENT_TOP + PEDIMENT_RISE + 0.02
    half = 0.47
    y0, y1 = yf - 0.05, yf + depth + 0.04
    for k in (-1, 1):
        lo = Vector((s + k * half, (y0 + y1) / 2, z0))
        hi = Vector((s, (y0 + y1) / 2, apex))
        ax = (hi - lo).normalized()
        ay = Vector((0, 1, 0))
        az = ax.cross(ay)
        if az.z < 0:
            az = -az
        length = (hi - lo).length
        b.obox((lo + hi) / 2 + az * 0.015, ax, ay, az, length / 2, (y1 - y0) / 2, 0.015, color, bevel=0.004)
        if diamonds:
            for i in range(1, 4):
                for j in range(3):
                    c = lo + ax * (length * i / 4.2) + ay * ((y1 - y0) * (j + 0.5) / 3 - (y1 - y0) / 2) + az * 0.032
                    if (i + j) % 2:
                        continue
                    w = 0.035
                    pts = [c - ax * w, c - ay * w, c + ax * w, c + ay * w]
                    verts = [b.bm.verts.new(q) for q in pts]
                    f = b._face(verts, diamonds)
                    f.normal_update()
                    if f.normal.dot(az) < 0:
                        f.normal_flip()
    if crest:
        y = y0 + 0.02
        while y < y1 - 0.01:
            b.pyramid(s, y, apex + 0.02, 0.014, 0.014, apex + 0.09, 'gold')
            y += 0.06


def monument(b, x, y):
    """Level 3: a great cut crystal on a stone plinth ringed in gold -- the mine's own monument."""
    b.box(x - 0.1, x + 0.1, y - 0.1, y + 0.1, 0, 0.05, 'stone_mid', bevel=0.006)
    b.box(x - 0.07, x + 0.07, y - 0.07, y + 0.07, 0.05, 0.22, 'stone_light', bevel=0.006)
    b.box(x - 0.085, x + 0.085, y - 0.085, y + 0.085, 0.22, 0.25, 'gold')
    shard(b, (x, y, 0.25), (0, 0, 1), 0.3, 0.06, sides=6, glint=True)
    for k in range(3):
        a = k * 2.094
        shard(b, (x + 0.04 * math.cos(a), y + 0.04 * math.sin(a), 0.25), (math.cos(a), math.sin(a), 1.4), 0.14, 0.03, sides=5)


def halo(b, x, y, z, r=0.32):
    """Level 4: shards floating in a ring round the great crystal -- the mountain's magic."""
    for k in range(7):
        a = k * 2 * math.pi / 7
        zz = z + 0.06 * math.sin(a * 2)
        shard(b, (x + r * math.cos(a), y + r * math.sin(a), zz - 0.05), (0.2 * math.cos(a), 0.2 * math.sin(a), 1.0),
              0.12, 0.03, sides=5, glint=(k % 2 == 0))
        b.pyramid(x + r * math.cos(a), y + r * math.sin(a), zz - 0.05, 0.026, 0.026, zz - 0.11, CRYSTAL_DARK, sides=5)


def build_level(level):
    b = Builder()
    h = HILL_H[level]
    k = h / HILL_H[1]
    # the yard: cobbles in front of the mouth
    d.paving(b, -0.62, 0.62, -0.98, -0.2)
    # the rock, split by crystal; it grows with the level
    d.rock_hill(b, -0.97, 0.97, 0.12, 0.97, h, ore=CRYSTAL)
    d.rock(b, -0.62, 0.05, 0.3, 0.26, 0.62, 'stone_dark', top='grass_dark', seed=11, rot=8)
    d.rock(b, 0.66, 0.08, 0.28, 0.25, 0.56, 'stone_mid', top='grass', seed=13, rot=-12)
    big = 1.55 if level == 1 else 1.55 + 0.3 * (level - 1) ** 0.5
    gy = 0.56 - 0.05 * (level - 1)   # the great clump steps forward as it grows, its fan stays on the plot
    cluster(b, 0.02, gy, 0.86 * k, scale=big, seed=0.3, main_lean=(0.05, 0.12))   # the great clump on top
    cluster(b, -0.66, 0.34, 0.55 * k, scale=1.0 + 0.1 * (min(level, 3) - 1), seed=1.7, main_lean=(-0.35, 0.1))
    grown = min(level, 3) - 1    # the side and back clumps stop growing at level 3: the plot's edge is near
    cluster(b, 0.7, 0.4, 0.6 * k, scale=1.1 + 0.1 * grown, seed=2.9, main_lean=(0.35 - 0.06 * grown, 0.05))
    cluster(b, -0.36, 0.76, 0.7 * k, scale=0.75 + 0.1 * grown, seed=4.2, main_lean=(-0.2, 0.3 - 0.08 * grown))
    cluster(b, 0.4, 0.78, 0.74 * k, scale=0.7 + 0.1 * grown, seed=5.1, main_lean=(0.2, 0.3 - 0.08 * grown))
    if level >= 2:
        # level 2: more crystal breaking out of the higher mountain, and green on it
        cluster(b, -0.22, 0.42, 0.72 * k, scale=0.7, seed=6.3, main_lean=(-0.3, -0.25))
        cluster(b, 0.3, 0.4, 0.74 * k, scale=0.65, seed=7.1, main_lean=(0.3, -0.25))
        small_round(b, -0.6, 0.57, 0.7 * h - 0.04, s=0.6)     # on the crags' turf, the map's own trees
        small_fir(b, 0.6, 0.62, 0.78 * h - 0.04, s=0.65)
        for x, y, z in ((-0.84, 0.05, 0.0), (0.84, 0.0, 0.0), (-0.84, 0.5, 0.0), (0.84, 0.48, 0.0)):
            shrub(b, x, y, r=0.1)
            d.flowers(b, x + 0.05, y - 0.1, z, colors=('roof_purple', 'white', 'plaster_pink'), r=0.04)
        hoist(b, 0.78, 0.14, 0.5)

    s, yf = 0.0, -0.22
    portal(b, s, yf)
    cave(b, s, yf + 0.14)
    for k in (-1, 1):
        d.lamp(b, s + k * 0.52, yf - 0.08, h=0.58)
    d.rails(b, s, -0.98, yf + 0.36)
    # the cart, heaped with shards instead of ore
    cy = -0.62
    b.zbox(s, cy, 0.07, 0.2, 0.11, 0.14, 0, 'wood_mid', bevel=0.01)
    b.zbox(s, cy, 0.13, 0.15, 0.125, 0.155, 0, 'gold')
    for k in (-1, 1):
        b.hcylinder('x', (s, cy + k * 0.08, 0.06), 0.1, 0.045, 'iron', sides=6)
    b.blob(s, cy, 0.18, 0.1, 0.05, CRYSTAL_DARK, sides=6)
    for ox, oy, h, lean in ((0.0, 0.0, 0.17, (0.0, -0.2)), (-0.05, 0.05, 0.12, (-0.5, 0.3)),
                            (0.05, -0.05, 0.13, (0.5, -0.3)), (0.04, 0.07, 0.1, (0.3, 0.5))):
        shard(b, (s + ox, cy + oy, 0.18), (lean[0], lean[1], 1.0), h, 0.035, sides=5, glint=(h > 0.15))

    chest(b, 0.6, -0.62)
    cutter_bench(b, -0.6, -0.6)
    d.pickaxe(b, -0.36, -0.3, lean_to_y=-0.24)
    # loose shards in the grass
    for x, y, h, lean, sp in ((-0.86, -0.86, 0.16, (0.3, -0.3), 0.2), (0.86, -0.9, 0.14, (-0.3, -0.2), 1.1),
                              (-0.3, -0.9, 0.11, (0.2, -0.4), 2.0), (0.88, -0.3, 0.17, (0.4, 0.0), 0.7),
                              (-0.88, -0.22, 0.15, (-0.4, -0.1), 1.6)):
        if level >= 3 and x < -0.85 and y > -0.3:
            continue   # the monument stands there
        loose(b, x, y, h, lean, sp)
    for x, y in ((-0.88, -0.5), (0.3, -0.92), (0.9, -0.62)):
        d.grass_tuft(b, x, y)
    if level >= 3:
        # level 3, crowned: gold finials on the pediment, crystal lamps, a low wall with gold-ball piers
        # along the yard's front, the crystal monument
        for x, z in ((s, PEDIMENT_TOP + PEDIMENT_RISE + (0.1 if level >= 4 else 0.0)), (s - 0.44, PEDIMENT_TOP), (s + 0.44, PEDIMENT_TOP)):
            d.finial(b, x, yf + 0.07, z, h=0.2 if x == s else 0.14)
        for side in (-1, 1):
            shard(b, (s + side * 0.52, yf - 0.08, 0.73), (0, 0, 1), 0.12, 0.035, sides=6, glint=True)
        d.garden_wall(b, [(-0.9, -0.9), (-0.2, -0.9)])
        d.garden_wall(b, [(0.2, -0.9), (0.9, -0.9)])
        for x in (-0.2, 0.2):
            d.pier(b, x, -0.9, h=0.26)
        monument(b, -0.86, -0.27)
    if level >= 4:
        pediment_roof(b, s, yf, 0.14, 'gold' if level >= 5 else CRYSTAL, diamonds=None if level >= 5 else 'gold', crest=True)
        halo(b, 0.02, gy, 0.86 * h / HILL_H[1] + 0.62 * big * 0.55)
    if level >= 5:
        # level 5: the roof gilded, gold bands round the great crystals' roots
        kh = h / HILL_H[1]
        for x, y, z, r in ((0.02, gy, 0.86 * kh, 0.17 * big / 1.55), (-0.66, 0.34, 0.55 * kh, 0.11), (0.7, 0.4, 0.6 * kh, 0.12)):
            b.ring(x, y, z - 0.02, z + 0.05, r * 0.9, r * 1.15, ('gold',), sides=8)
    if level >= 2:
        # level 2: ivy down the portal, flower beds by the rails, moss on the yard's edge
        for side in (-1, 1):
            ivy(b, side * 0.43, -0.24, 0.35, 0.95, side)
            d.flowers(b, side * 0.28, -0.36, colors=('roof_purple', 'plaster_yellow', 'white'), r=0.045)
        for x, y in ((-0.66, -0.94), (0.66, -0.95), (-0.62, -0.25), (0.62, -0.25)):
            d.grass_tuft(b, x, y, r=0.05, color='grass_dark')
    return b


# what export puts in the game (levels 2-5 after the user approves level 1)
GAME_MODELS = {f"CrystalMine1-lvl{n}": (lambda n=n: build_level(n)) for n in (1, 2, 3, 4, 5)}
