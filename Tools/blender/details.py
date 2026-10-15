"""The building set's STANDARD, and the details drawn to it: storeys, doors, windows, timber
framing, roofs, chimneys, banners.

Every size a player compares between two buildings lives here and nowhere else -- a door, a
window, a storey, the roof pitch, the rib on a roof. Building scripts pass a floor's BASE height
and a position; they do not invent sizes. That is what makes eight models read as one set.

Everything is measured against a citizen 0.45 m tall (UnitSize.Scale in the game, agreed
2026-10-12). 1 unit = 1 m = 1 grid cell.
"""

CITIZEN = 0.45

FOOTING = 0.10                 # stone footing under every house
STOREY = 0.56                  # floor to floor
JETTY = 0.05                   # how far a timber upper floor oversails the ground floor
G_TOP = FOOTING + STOREY       # 0.66: top of a ground floor
U_TOP = G_TOP + STOREY         # 1.22: top of an upper floor

DOOR_W, DOOR_H = 0.26, 0.50    # a citizen plus a hand
GATE_W, GATE_H = 0.40, 0.56    # the grand or double entrance
WIN_W, WIN_H, WIN_SILL = 0.20, 0.24, 0.20     # rectangular window, sill above the floor's base
ARCH_W, ARCH_H, ARCH_SILL = 0.18, 0.32, 0.16  # round-topped window in stone

PITCH = 1.0                    # roof rise per unit of run: 45 degrees on every house
ROOF = dict(overhang=0.1, t=0.045, rib_w=0.08, rib_step=0.2, rib_h=0.02,
            cap_w=0.12, cap_h=0.06, cap_lift=0.03)
BEAM_W, BEAM_D = 0.045, 0.014
WALL_BEVEL = 0.015


# -- volumes -----------------------------------------------------------------------------------

def footing(b, x0, x1, y0, y1, color='stone_mid', tufts=True):
    """The stone footing every house stands on, with a few tufts of grass growing against it, so
    the building sits in the meadow rather than on it. Tufts that would leave the plot
    (kit.PLOT, set by kit.run) are skipped."""
    b.box(x0 - 0.035, x1 + 0.035, y0 - 0.035, y1 + 0.035, 0, FOOTING, color)
    if not tufts:
        return
    e = 0.04
    spots = [(x0 - e, y0 - e, 0.045), (x1 + e, y0 - e, 0.04), (x0 - e, y1 + e, 0.04),
             (x1 + e, y1 + e, 0.045), (x0 + (x1 - x0) * 0.3, y0 - e, 0.035),
             (x1 + e, (y0 + y1) / 2, 0.035)]
    for i, (x, y, r) in enumerate(spots):
        grass_tuft(b, x, y, r, 'grass' if i % 2 else 'grass_dark')


def quoins(b, px, py, sx, sy, z0, z1, step=0.11, color='stone_mid'):
    """Corner stones up a stone wall's corner at (px, py), alternately long along x and long
    along y; sx, sy (+-1) point out of the wall. The mark of a house rebuilt in stone (level 2)."""
    z, k = z0, 0
    while z < z1 - 0.02:
        lx, ly = (0.13, 0.05) if k % 2 == 0 else (0.05, 0.13)
        xa, xb = sorted((px + sx * 0.012, px - sx * lx))
        ya, yb = sorted((py + sy * 0.012, py - sy * ly))
        b.box(xa, xb, ya, yb, z, min(z + step - 0.014, z1), color, bevel=0.006)
        z += step
        k += 1


def grass_tuft(b, x, y, r=0.04, color='grass'):
    """A small clump of grass at a wall's foot; skipped if it would poke out of the plot."""
    import kit
    plot = getattr(kit, 'PLOT', None)
    if plot is not None:
        lo, hi = plot
        if x - r < lo[0] or x + r > hi[0] or y - r < lo[1] or y + r > hi[1]:
            return
    b.blob(x, y, 0.0, r, r * 1.4, color, sides=5)


def roof(b, cx, cy, axis, half_len, half_span, ze, color='roof_orange', dark='roof_orange_dark',
         **override):
    """Standard ribbed gable roof. Returns the ridge height (slab underside)."""
    zr = ze + half_span * PITCH
    params = dict(ROOF)
    params.update(override)
    b.gable_roof(cx, cy, axis, half_len, half_span, ze, zr, color, dark, **params)
    return zr


def dormer(b, face, eave, s, ze, w=0.2, depth=0.5, h=0.44, wall='plaster', **roof_kw):
    """A gabled dormer in a roof slope over the wall facing `face` ('-y', '+y', '-x', '+x'):
    its front wall stands just behind the eave line `eave`, centred at s along it, rising from
    the eaves height ze by h, with timber edging, a cross window and its own little roof
    (roof_kw: color/dark). A level-2 house's attic made habitable."""
    from kit import Facade
    sgn = -1 if face[0] == '-' else 1
    front = eave - sgn * 0.05
    back = front - sgn * depth
    zt = ze + h
    a0, a1 = sorted((front, back))
    rc = ((front + sgn * 0.1) + back) / 2
    half = (depth + 0.1) / 2
    if face[1] == 'y':
        b.box(s - w, s + w, a0, a1, ze - 0.02, zt, wall)
        b.gable_block('y', a0, a1, s - w, s + w, zt, zt + w * PITCH, wall)
        roof(b, s, rc, 'y', half, w, zt, overhang=0.06, **roof_kw)
    else:
        b.box(a0, a1, s - w, s + w, ze - 0.02, zt, wall)
        b.gable_block('x', a0, a1, s - w, s + w, zt, zt + w * PITCH, wall)
        roof(b, rc, s, 'x', half, w, zt, overhang=0.06, **roof_kw)
    f = Facade(b, face, front)
    f.rect(s - w, s + w, ze - 0.02, ze + 0.02, BEAM_D, 'wood_dark')
    f.rect(s - w, s + w, zt - 0.04, zt, BEAM_D, 'wood_dark')
    for p in (s - w + 0.025, s + w - 0.025):
        f.rect(p - BEAM_W / 2, p + BEAM_W / 2, ze + 0.02, zt - 0.04, BEAM_D, 'wood_dark')
    cross_window(f, s, ze + 0.13 - WIN_SILL, w=0.16)
    gable_frame(f, s, w, zt, zt + w * PITCH)


def chimney(b, cx, cy, z0, z1, hw=0.09, color='stone_mid'):
    b.box(cx - hw, cx + hw, cy - hw, cy + hw, z0, z1, color)
    b.box(cx - hw - 0.025, cx + hw + 0.025, cy - hw - 0.025, cy + hw + 0.025, z1 - 0.05, z1 + 0.02,
          'stone_dark')


# -- openings ----------------------------------------------------------------------------------

# Glass is blue-grey, not a black hole: the set's windows used to be the hardest marks on the
# screen. roof_slate on a VERTICAL face gets no slate pattern (detail.py), so it reads as glass.
GLASS = 'roof_slate'
CURTAIN = 'plaster_white'
FRAME = 'wood_light'


def _glass(f, s, z0, z1, w, curtains=True, depth=0.012):
    """Pane, and white curtains tied back in its upper corners -- the softest detail per
    triangle there is."""
    f.rect(s - w / 2, s + w / 2, z0, z1, depth, GLASS)
    if curtains:
        hgt = z1 - z0
        f.poly([(s - w / 2, z1), (s - w / 2 + w * 0.34, z1), (s - w / 2, z0 + hgt * 0.32)],
               depth + 0.006, CURTAIN)
        f.poly([(s + w / 2 - w * 0.34, z1), (s + w / 2, z1), (s + w / 2, z0 + hgt * 0.32)],
               depth + 0.006, CURTAIN)


def plain_window(f, s, base, w=WIN_W, sill=FRAME):
    z0 = base + WIN_SILL
    z1 = z0 + WIN_H
    f.rect(s - w / 2 - 0.02, s + w / 2 + 0.02, z0 - 0.02, z1 + 0.02, 0.008, FRAME)
    _glass(f, s, z0, z1, w)
    f.rect(s - w / 2 - 0.035, s + w / 2 + 0.035, z0 - 0.035, z0 - 0.005, 0.04, sill)


def cross_window(f, s, base, w=WIN_W, sill=FRAME, bars=FRAME):
    z0 = base + WIN_SILL
    z1 = z0 + WIN_H
    f.rect(s - w / 2 - 0.02, s + w / 2 + 0.02, z0 - 0.02, z1 + 0.02, 0.008, FRAME)
    _glass(f, s, z0, z1, w, curtains=False)
    f.rect(s - 0.009, s + 0.009, z0, z1, 0.02, bars)
    f.rect(s - w / 2, s + w / 2, (z0 + z1) / 2 - 0.009, (z0 + z1) / 2 + 0.009, 0.02, bars)
    f.rect(s - w / 2 - 0.035, s + w / 2 + 0.035, z0 - 0.035, z0 - 0.005, 0.04, sill)


def shutter_window(f, s, base, w=WIN_W, shutter='wood_mid'):
    z0 = base + WIN_SILL
    z1 = z0 + WIN_H
    _glass(f, s, z0, z1, w)
    f.rect(s - w / 2 - 0.02, s + w / 2 + 0.02, z0 - 0.03, z0, 0.035, FRAME)
    f.rect(s - w / 2 - 0.09, s - w / 2 - 0.01, z0, z1, 0.02, shutter)
    f.rect(s + w / 2 + 0.01, s + w / 2 + 0.09, z0, z1, 0.02, shutter)


def arch_window(f, s, base, w=ARCH_W, h=ARCH_H, sill='stone_mid'):
    z0 = base + ARCH_SILL
    f.arch(s, z0, w + 0.04, h + 0.02, 0.008, 'stone_light')
    f.arch(s, z0, w, h, 0.012, GLASS)
    spring = z0 + h - w / 2
    f.poly([(s - w / 2, spring), (s - w / 2 + w * 0.3, spring), (s - w / 2, z0 + (spring - z0) * 0.4)],
           0.018, CURTAIN)
    f.poly([(s + w / 2 - w * 0.3, spring), (s + w / 2, spring), (s + w / 2, z0 + (spring - z0) * 0.4)],
           0.018, CURTAIN)
    f.rect(s - w / 2 - 0.03, s + w / 2 + 0.03, z0 - 0.03, z0, 0.03, sill)


def slit(f, s, z0, z1):
    f.rect(s - 0.035, s + 0.035, z0, z1, 0.01, GLASS)


def flower_box(f, s, base, w=WIN_W + 0.08, flowers='roof_red'):
    """A planter under a window, spilling over: a green mound with three blossoms on it."""
    z = base + WIN_SILL - 0.035
    f.rect(s - w / 2, s + w / 2, z - 0.05, z, 0.06, 'wood_mid')
    f.rect(s - w / 2 + 0.01, s + w / 2 - 0.01, z - 0.005, z + 0.025, 0.055, 'leaf', bevel=0.008)
    for k, c in enumerate((flowers, 'white', flowers)):
        f.blossom(s - w / 3 + k * w / 3, z + 0.02, 0.07, 0.024, c)


def climber(b, x, y, h=0.6, flowers=('roof_red', 'plaster_pink')):
    """A climbing rose against a wall at (x, y): three leafy crowns of falling size, petals
    dotted over them."""
    crowns = ((0.0, 0.0, 0.12, h * 0.48), (0.07, h * 0.4, 0.09, h * 0.38), (0.02, h * 0.7, 0.065, h * 0.3))
    for dx, z, r, hh in crowns:
        b.ball(x + dx, y, z, r, hh, 'leaf', sides=6, top='grass')
    for k, (dx, z) in enumerate(((0.09, h * 0.33), (-0.05, h * 0.22), (0.13, h * 0.6), (0.05, h * 0.88))):
        b.blob(x + dx, y - 0.08, z, 0.022, 0.035, flowers[k % len(flowers)], sides=4)


def wall_lantern(f, s, z):
    """A small lantern on a bracket beside a door."""
    f.rect(s - 0.006, s + 0.006, z + 0.06, z + 0.1, 0.07, 'wood_dark')
    f.rect(s - 0.028, s + 0.028, z - 0.03, z + 0.05, 0.075, 'window_light')
    f.rect(s - 0.035, s + 0.035, z + 0.05, z + 0.07, 0.085, 'wood_dark')


def plank_door(f, s, base=FOOTING, w=DOOR_W, h=DOOR_H, color='door', frame=FRAME):
    f.rect(s - w / 2 - 0.03, s + w / 2 + 0.03, base, base + h + 0.03, 0.015, frame)
    f.rect(s - w / 2, s + w / 2, base, base + h, 0.03, color)
    f.rect(s - 0.006, s + 0.006, base, base + h, 0.036, frame)
    f.rect(s + w / 2 - 0.06, s + w / 2 - 0.035, base + h * 0.45, base + h * 0.45 + 0.025, 0.045,
           'gold')


def arch_door(f, s, base=FOOTING, w=DOOR_W, h=DOOR_H, frame='stone_mid'):
    f.arch(s, base, w + 0.1, h + 0.06, 0.015, frame)
    f.arch(s, base, w, h, 0.03, 'door')
    f.rect(s - 0.006, s + 0.006, base, base + h - w / 2, 0.036, 'wood_dark')


def gate(f, s, base=0.0, w=GATE_W, h=GATE_H, frame='stone_mid'):
    """The grand entrance: an arched double door with planks and an iron band."""
    f.arch(s, base, w + 0.12, h + 0.08, 0.015, frame)
    f.arch(s, base, w, h, 0.03, 'door')
    for k in (-1, 1):
        f.rect(s + k * w / 6 - 0.01, s + k * w / 6 + 0.01, base, base + h - w / 2, 0.04, 'wood_dark')
    f.rect(s - w / 2, s + w / 2, base + h * 0.4, base + h * 0.4 + 0.03, 0.04, 'iron')


def step(b, s0, s1, plane_y, depth=0.14):
    """A stone step in front of a -Y facing door."""
    b.box(s0, s1, plane_y - depth, plane_y, 0, 0.06, 'stone_mid', bevel=0.008)


# -- framing -----------------------------------------------------------------------------------

def timber(f, s0, s1, z0, z1, posts, braces=()):
    """Half-timber framing on a plaster wall: sill and head beams, posts, diagonal braces.
    A brace (a, b) runs from s=a at the bottom to s=b at the top."""
    f.rect(s0, s1, z0, z0 + 0.04, BEAM_D, 'wood_dark')
    f.rect(s0, s1, z1 - 0.04, z1, BEAM_D, 'wood_dark')
    for s in posts:
        f.rect(s - BEAM_W / 2, s + BEAM_W / 2, z0 + 0.04, z1 - 0.04, BEAM_D, 'wood_dark')
    for a, b in braces:
        da = 0.03 if b > a else -0.03
        f.beam(a + da, z0 + 0.05, b - da, z1 - 0.05, BEAM_W, BEAM_D, 'wood_dark')


def gable_frame(f, sc, half_span, ze, zr):
    """King post and two struts on a gable end."""
    f.rect(sc - BEAM_W / 2, sc + BEAM_W / 2, ze, zr - 0.06, BEAM_D, 'wood_dark')
    k = (zr - ze) * 0.55
    f.beam(sc - half_span * 0.55, ze + 0.02, sc - 0.03, ze + k, BEAM_W, BEAM_D, 'wood_dark')
    f.beam(sc + half_span * 0.55, ze + 0.02, sc + 0.03, ze + k, BEAM_W, BEAM_D, 'wood_dark')


# -- props ---------------------------------------------------------------------------------------

def banner(f, s, top, length, w=0.14, color='banner_red', emblem='gold'):
    f.rect(s - w / 2 - 0.02, s + w / 2 + 0.02, top - 0.03, top, 0.035, 'wood_dark')
    f.poly([(s - w / 2, top - 0.03), (s + w / 2, top - 0.03), (s + w / 2, top - length),
            (s, top - length - 0.05), (s - w / 2, top - length)], 0.02, color)
    mid = top - 0.03 - length / 2
    f.rect(s - 0.03, s + 0.03, mid - 0.04, mid + 0.04, 0.026, emblem)


def bucket(b, x, y, z0, r=0.045, color='wood_light'):
    b.cylinder(x, y, z0, z0 + r * 1.5, r, color)
    b.cylinder(x, y, z0 + r * 0.6, z0 + r * 0.9, r * 1.08, 'iron')


def barrel(b, x, y, z0=0.0, r=0.08, h=0.2, top='wood_light'):
    b.cylinder(x, y, z0, z0 + h, r, 'wood_mid')
    b.cylinder(x, y, z0 + h * 0.62, z0 + h * 0.72, r * 1.05, 'iron')
    b.cylinder(x, y, z0 + h, z0 + h + 0.005, r * 0.9, top)


def crate(b, x, y, size=0.18, angle=0.0, z0=0.0):
    b.zbox(x, y, z0, z0 + size, size / 2, size / 2, angle, 'wood_light', bevel=0.01)


def bench(b, x0, x1, y0, y1, z=0.1):
    b.box(x0, x1, y0, y1, z - 0.025, z, 'wood_light')
    for x in (x0 + 0.03, x1 - 0.03):
        b.box(x - 0.015, x + 0.015, y0 + 0.01, y1 - 0.01, 0, z - 0.025, 'wood_dark')


def bush(b, x, y, z0=0.0, r=0.12, color='leaf'):
    b.blob(x, y, z0, r, r * 1.8, color)


def tree(b, x, y, z0=0.0, r=0.22):
    b.cylinder(x, y, z0, z0 + 0.16, 0.045, 'wood_mid', sides=6)
    b.blob(x, y, z0 + 0.12, r, r * 2.1, 'leaf', sides=6)


# -- fortification ----------------------------------------------------------------------------

MERLON_L, MERLON_STEP, MERLON_H = 0.14, 0.28, 0.1
WALL_H = 0.62                  # curtain wall: a citizen can just look over the battlements


def merlons(b, axis, a0, a1, c0, c1, z, color='stone_light'):
    """A row of merlons along `axis` from a0 to a1, on a wall top spanning c0..c1 across it."""
    n = int((a1 - a0 - MERLON_L) // MERLON_STEP) + 1
    first = (a0 + a1) / 2 - (n - 1) * MERLON_STEP / 2
    for i in range(n):
        a = first + i * MERLON_STEP
        if axis == 'x':
            b.box(a - MERLON_L / 2, a + MERLON_L / 2, c0, c1, z, z + MERLON_H, color)
        else:
            b.box(c0, c1, a - MERLON_L / 2, a + MERLON_L / 2, z, z + MERLON_H, color)


# -- storage -------------------------------------------------------------------------------------

RED = dict(color='roof_red', dark='roof_red_dark')   # the Storage category's roofs


def barn_door(f, s, base=FOOTING, w=GATE_W, h=DOOR_H, color='roof_red'):
    """Double plank door braced with a white X on each leaf -- the barn's own door."""
    f.rect(s - w / 2 - 0.03, s + w / 2 + 0.03, base, base + h + 0.03, 0.015, 'wood_dark')
    for k in (-1, 1):
        c = s + k * w / 4
        f.rect(c - w / 4 + 0.005, c + w / 4 - 0.005, base, base + h, 0.03, color)
        f.beam(c - w / 4 + 0.02, base + 0.02, c + w / 4 - 0.02, base + h - 0.02, 0.025, 0.04,
               'plaster_white')
        f.beam(c - w / 4 + 0.02, base + h - 0.02, c + w / 4 - 0.02, base + 0.02, 0.025, 0.04,
               'plaster_white')


def barred_window(f, s, base, w=WIN_W):
    z0 = base + WIN_SILL
    f.rect(s - w / 2, s + w / 2, z0, z0 + WIN_H, 0.01, GLASS)
    for k in (-1, 0, 1):
        f.rect(s + k * w / 3.2 - 0.008, s + k * w / 3.2 + 0.008, z0, z0 + WIN_H, 0.02, 'iron')
    f.rect(s - w / 2 - 0.03, s + w / 2 + 0.03, z0 - 0.03, z0, 0.03, 'stone_mid')


def sack(b, x, y, z0=0.0, r=0.07):
    b.blob(x, y, z0, r, r * 2.1, 'burlap', sides=5)


def log_pile(b, x, y, length=0.4, rows=(3, 2, 1), r=0.05, axis='x'):
    """Logs stacked in a pyramid, lying along `axis`, centred on (x, y)."""
    for level, count in enumerate(rows):
        z = r + level * r * 1.7
        for i in range(count):
            off = (i - (count - 1) / 2) * r * 2.05
            c = (x, y + off, z) if axis == 'x' else (x + off, y, z)
            b.hcylinder(axis, c, length / 2, r, 'wood_light' if (i + level) % 2 else 'wood_mid',
                        sides=5)


def stone_pile(b, x, y, size=0.13):
    """Cut stone blocks, two rows and one on top."""
    for i, (dx, dy, dz, a) in enumerate(((-0.07, -0.05, 0, 4), (0.08, -0.04, 0, -6),
                                         (0.0, 0.09, 0, 10), (0.01, 0.0, size, 20))):
        b.zbox(x + dx, y + dy, dz, dz + size, size / 2 + 0.01, size / 2 - 0.01, a,
               'stone_light' if i % 2 else 'stone_mid', bevel=0.01)


def ore_heap(b, x, y, r=0.18, color='iron', second='stone_dark'):
    b.blob(x, y, 0, r, r * 1.2, color, sides=6)
    b.blob(x + r * 0.5, y - r * 0.3, 0, r * 0.55, r * 0.8, second, sides=5)


def cart(b, x, y, load=None):
    """Hand cart along x with two wheels and shafts; `load` is drawn by the caller on top."""
    b.box(x - 0.2, x + 0.2, y - 0.12, y + 0.12, 0.1, 0.14, 'wood_mid')
    for sy in (-1, 1):
        b.box(x - 0.2, x + 0.2, y + sy * 0.12 - 0.015, y + sy * 0.12 + 0.015, 0.14, 0.22, 'wood_light')
        b.hcylinder('y', (x, y + sy * 0.15, 0.1), 0.02, 0.1, 'wood_dark', sides=8)
    b.rod((x + 0.2, y - 0.08, 0.12), (x + 0.48, y - 0.08, 0.05), 0.025, 'wood_dark')
    b.rod((x + 0.2, y + 0.08, 0.12), (x + 0.48, y + 0.08, 0.05), 0.025, 'wood_dark')


def brazier(b, x, y):
    b.box(x - 0.02, x + 0.02, y - 0.02, y + 0.02, 0, 0.3, 'iron')
    b.cylinder(x, y, 0.3, 0.36, 0.07, 'iron', sides=6)
    b.blob(x, y, 0.33, 0.05, 0.12, 'fire', sides=4)


def hay_loft(f, s, base, w=0.22, h=0.24):
    """Loft hatch in a gable with hay spilling out and a hoist beam over it."""
    f.rect(s - w / 2 - 0.025, s + w / 2 + 0.025, base, base + h + 0.025, 0.012, 'wood_dark')
    f.rect(s - w / 2, s + w / 2, base, base + h, 0.02, 'window')
    f.rect(s - w / 2 + 0.02, s + w / 2 - 0.02, base, base + 0.07, 0.05, 'hay')
    f.rect(s - 0.02, s + 0.02, base + h + 0.04, base + h + 0.08, 0.16, 'wood_dark')


LOFT = 0.28                    # a storage building's half storey over the doors (hay, goods)
L_TOP = G_TOP + LOFT           # 0.94: wall top of a one-and-a-half-storey store


def shed_roof(b, axis, a0, a1, wall, out, z_wall, z_out, color='roof_red', dark='roof_red_dark'):
    """Lean-to roof: runs along `axis` from a0 to a1, falls from the wall line `wall` (at height
    z_wall) to the outer edge `out` (at z_out). One smooth slab in `color` like every other roof in
    the set (the tiles come from the detail atlas), with a darker edge board along the eave."""
    from mathutils import Vector
    if axis == 'x':
        p0, p1 = Vector((0, wall, z_wall)), Vector((0, out, z_out))
        r = Vector((1, 0, 0))
    else:
        p0, p1 = Vector((wall, 0, z_wall)), Vector((out, 0, z_out))
        r = Vector((0, 1, 0))
    dvec = p1 - p0
    dd = dvec.normalized()
    ax = r if r.cross(dd).z > 0 else -r
    n = ax.cross(dd)
    mid = (p0 + p1) / 2 + r * ((a0 + a1) / 2)
    half_len, half_w = (a1 - a0) / 2, dvec.length / 2 + 0.05
    t = ROOF['t'] * 0.8 + ROOF['rib_h'] * 0.5
    b.obox(mid + n * (t / 2), ax, dd, n, half_len, half_w, t / 2, color, bevel=0.01)
    edge = (p1 - p0).normalized() * (half_w - 0.012)
    b.obox(mid + edge + n * (t * 0.5), ax, dd, n, half_len + 0.01, 0.014, t * 0.7, dark)


PURPLE = dict(color='roof_purple', dark='roof_purple_dark')   # Entertainment's roofs


def lit_window(f, s, base, w=WIN_W):
    """A window with warm light behind it -- a tavern, a theatre in the evening."""
    z0 = base + WIN_SILL
    z1 = z0 + WIN_H
    f.rect(s - w / 2, s + w / 2, z0, z1, 0.01, 'window_light')
    f.rect(s - 0.011, s + 0.011, z0, z1, 0.02, 'wood_dark')
    f.rect(s - w / 2, s + w / 2, (z0 + z1) / 2 - 0.01, (z0 + z1) / 2 + 0.01, 0.02, 'wood_dark')
    f.rect(s - w / 2 - 0.03, s + w / 2 + 0.03, z0 - 0.03, z0, 0.035, 'wood_mid')


def flowers(b, x, y, z0=0.0, colors=('roof_red', 'plaster_yellow', 'roof_purple', 'white'), r=0.05):
    """A clump of flowers: a leafy mound with coloured heads."""
    import math
    b.blob(x, y, z0, r * 1.6, r * 1.4, 'grass_dark', sides=5)
    for i, c in enumerate(colors[:3]):
        a = 2.1 * i
        b.blob(x + r * 0.8 * math.cos(a), y + r * 0.8 * math.sin(a), z0 + r * 0.9, r * 0.6, r * 0.8,
               c, sides=4)


def weathervane(b, x, y, z, h=0.24):
    """An iron pole on a ridge with a gold ball and an arrow pointing along x -- the mark of a
    building that has prospered (level 2)."""
    b.box(x - 0.008, x + 0.008, y - 0.008, y + 0.008, z, z + h, 'iron')
    b.cylinder(x, y, z + h * 0.42, z + h * 0.52, 0.024, 'gold', sides=6)
    za = z + h * 0.82
    b.rod((x - 0.1, y, za), (x + 0.1, y, za), 0.012, 'iron')
    b.prism([(x + 0.1, za - 0.03), (x + 0.15, za), (x + 0.1, za + 0.03)], (0, y + 0.005, 0),
            (1, 0, 0), (0, 0, 1), 0.01, 'iron', back_cap=True)
    b.prism([(x - 0.14, za - 0.045), (x - 0.08, za - 0.015), (x - 0.08, za + 0.015),
             (x - 0.14, za + 0.045)], (0, y + 0.005, 0), (1, 0, 0), (0, 0, 1), 0.01, 'gold',
            back_cap=True)


def fountain(b, x, y, z0=0.0, r=0.3):
    """A round stone basin of water with a spout on a column: the centre of a richer garden or
    courtyard (level 2)."""
    b.cylinder(x, y, z0, z0 + 0.1, r, 'stone_light', sides=8, bevel=0.01)
    b.cylinder(x, y, z0 + 0.1, z0 + 0.105, r * 0.86, 'water', sides=8)
    b.cylinder(x, y, z0 + 0.1, z0 + 0.29, r * 0.17, 'stone_mid', sides=6)
    b.cylinder(x, y, z0 + 0.29, z0 + 0.32, r * 0.4, 'stone_light', sides=8)
    b.blob(x, y, z0 + 0.31, r * 0.17, 0.12, 'water', sides=5)


def lamp(b, x, y, h=0.62):
    b.box(x - 0.018, x + 0.018, y - 0.018, y + 0.018, 0, h, 'iron')
    b.box(x - 0.045, x + 0.045, y - 0.045, y + 0.045, h, h + 0.09, 'window_light')
    b.pyramid(x, y, h + 0.09, 0.06, 0.06, h + 0.15, 'iron')


# -- defensive lines -------------------------------------------------------------------------------
# A fence piece fills its cell edge to edge so neighbours meet without a gap; its gate and tower
# must carry the same height and thickness so the join has no step.

PALISADE_H = 0.72              # wooden tier: taller than a citizen, points included
PALISADE_LOG = 0.09
STONE_WALL_T = 0.30            # stone tier: the same WALL_H as the Town Hall's curtain wall
FENCE_MERLON_STEP = 0.25       # divides a cell, so the rhythm runs on across joins
DEFENCE = dict(color='roof_blue', dark='roof_blue_dark')   # Defence shares the castle's blue


def palisade_arm(b, ox, oy, dx, dy, t0=0.0, t1=0.5):
    """Pointed logs from t0 to t1 out of (ox, oy) along (dx, dy), on the 0.1 grid every piece
    shares (logs at 0.05, 0.15 ...), with two binding beams through them."""
    t = 0.05
    i = 0
    while t < t1 - 1e-6:
        if t > t0:
            x, y = ox + dx * t, oy + dy * t
            h = PALISADE_LOG / 2
            col = 'wood_light' if i % 2 else 'wood_mid'
            b.box(x - h, x + h, y - h, y + h, 0, PALISADE_H - 0.08, col, skip=('-z', '+z'))
            b.pyramid(x, y, PALISADE_H - 0.08, h, h, PALISADE_H, col)
        t += 0.1
        i += 1
    for z in (0.2, 0.5):
        xa, ya, xb, yb = ox + dx * t0, oy + dy * t0, ox + dx * t1, oy + dy * t1
        w = PALISADE_LOG / 2 + 0.012
        b.box(min(xa, xb) - (w if dx == 0 else 0), max(xa, xb) + (w if dx == 0 else 0),
              min(ya, yb) - (w if dy == 0 else 0), max(ya, yb) + (w if dy == 0 else 0),
              z, z + 0.05, 'wood_dark', skip=())


def stone_wall_arm(b, ox, oy, dx, dy, t0, t1):
    """Stone curtain wall from t0 to t1 along (dx, dy): plinth, wall, merlons on the shared grid,
    an arrow slit on each face."""
    hw = STONE_WALL_T / 2

    def span(a, c, pad):
        xa, ya, xb, yb = ox + dx * a, oy + dy * a, ox + dx * c, oy + dy * c
        return (min(xa, xb) - (pad if dx == 0 else 0), max(xa, xb) + (pad if dx == 0 else 0),
                min(ya, yb) - (pad if dy == 0 else 0), max(ya, yb) + (pad if dy == 0 else 0))

    x0, x1, y0, y1 = span(t0, t1, hw + 0.03)
    b.box(x0, x1, y0, y1, 0, FOOTING, 'stone_mid')
    x0, x1, y0, y1 = span(t0, t1, hw)
    b.box(x0, x1, y0, y1, FOOTING, WALL_H, 'stone_light')
    t = FENCE_MERLON_STEP / 2
    while t < t1 - 1e-6:
        if t >= t0 - 1e-6:
            x0, x1, y0, y1 = span(t - MERLON_L / 2, t + MERLON_L / 2, hw)
            b.box(x0, x1, y0, y1, WALL_H, WALL_H + MERLON_H, 'stone_light')
        t += FENCE_MERLON_STEP
    ts = (t0 + t1) / 2 if t1 - t0 > 0.3 else None
    if ts is not None:
        for side in (-1, 1):
            if dx == 0:
                cx, cy = ox + side * (hw + 0.004), oy + dy * ts
                b.box(cx - 0.004, cx + 0.004, cy - 0.03, cy + 0.03, 0.3, 0.5, 'window', skip=())
            else:
                cx, cy = ox + dx * ts, oy + side * (hw + 0.004)
                b.box(cx - 0.03, cx + 0.03, cy - 0.004, cy + 0.004, 0.3, 0.5, 'window', skip=())


# -- production ----------------------------------------------------------------------------------

SLATE = dict(color='roof_slate', dark='roof_slate_dark')   # Production's roofs
ADIT_W, ADIT_H = GATE_W, GATE_H    # a mine mouth is a gate a laden cart goes through


def rock(b, x, y, rx, ry, h, color='stone_mid', top=None, seed=0, rot=0.0, taper=0.55,
         ore=None, veins=0):
    """One crag of living rock; `top='grass'` gives it a turfed crown. With `ore`, `veins`
    crystals of that colour show on the faces turned most towards the front (-y) and the camera."""
    faces = b.crag(x, y, rx, ry, h, color, top=top, sides=6, rot_deg=rot, taper=taper, seed=seed)
    if ore and veins:
        front = sorted(faces, key=lambda cn: cn[1].y - 0.4 * cn[1].z)
        for i, (c, n) in enumerate(front[:veins]):
            r = 0.065 if i % 2 == 0 else 0.05
            ore_vein(b, c.x + n.x * 0.01, c.y + n.y * 0.01, c.z, ore, r)
            if i % 2 == 0:
                ore_vein(b, c.x + n.x * 0.01 + 0.06 * n.y, c.y + n.y * 0.01 - 0.06 * n.x,
                         c.z + 0.07, ore, r * 0.6)


def rock_hill(b, x0, x1, y0, y1, h, ore=None):
    """A hill of crags filling x0..x1, y0..y1 and peaking at h, turf on the high crowns, `ore`
    showing in the faces. The front (-y) is left low on purpose: that is where a mine opens or
    a quarry is cut."""
    w, dp = x1 - x0, y1 - y0
    pieces = (  # x, y, radius x, radius y as fractions of the area; height as a fraction of h
        (0.50, 0.62, 0.30, 0.34, 1.00, 'stone_mid', 'grass', 0, 10, 2),
        (0.19, 0.50, 0.17, 0.30, 0.70, 'stone_dark', 'grass_dark', 3, -15, 3),
        (0.81, 0.52, 0.17, 0.30, 0.78, 'stone_dark', 'grass', 5, 20, 3),
        (0.30, 0.82, 0.22, 0.15, 0.84, 'stone_light', None, 7, 5, 1),
        (0.72, 0.82, 0.22, 0.15, 0.90, 'stone_mid', 'grass_dark', 9, -8, 1),
    )
    for fx, fy, frx, fry, fh, col, top, seed, rot, veins in pieces:
        rock(b, x0 + fx * w, y0 + fy * dp, frx * w, fry * dp, fh * h, col, top, seed, rot,
             ore=ore, veins=veins)


def ore_vein(b, x, y, z, color, r=0.05):
    """A crystal of ore showing in the rock: the mine's resource, read from the game camera."""
    b.blob(x, y, z - r * 0.9, r, r * 1.8, color, sides=4)


def adit(b, s, yf, roof_depth=0.26, w=ADIT_W, h=ADIT_H, beam='wood_dark'):
    """A mine mouth in a rock face at y=yf facing -y: a dark opening in a timber frame under a
    small slate lean-to. Returns the height of the lintel."""
    from kit import Facade
    b.box(s - w / 2 - 0.07, s + w / 2 + 0.07, yf, yf + 0.3, 0, h + 0.14, 'stone_dark', bevel=0.02)
    f = Facade(b, '-y', yf)
    f.rect(s - w / 2, s + w / 2, 0, h, 0.01, 'window')
    for k in (-1, 1):
        x = s + k * (w / 2 + 0.03)
        b.box(x - 0.035, x + 0.035, yf - 0.075, yf + 0.005, 0, h + 0.04, beam)
    b.box(s - w / 2 - 0.08, s + w / 2 + 0.08, yf - 0.08, yf + 0.005, h + 0.0, h + 0.07, beam)
    shed_roof(b, 'x', s - w / 2 - 0.12, s + w / 2 + 0.12, yf + 0.02, yf - roof_depth, h + 0.2,
              h + 0.07, **SLATE)
    return h + 0.07


def rails(b, s, y0, y1, gauge=0.16):
    """A cart track along y from y0 to y1 (y0 < y1): sleepers and two iron rails."""
    y = y0 + 0.05
    while y < y1 - 0.02:
        b.box(s - gauge / 2 - 0.05, s + gauge / 2 + 0.05, y - 0.025, y + 0.025, 0, 0.02, 'wood_mid')
        y += 0.12
    for k in (-1, 1):
        x = s + k * gauge / 2
        b.box(x - 0.012, x + 0.012, y0, y1, 0.02, 0.04, 'iron')


def minecart(b, x, y, load, angle=0.0, gauge=0.16):
    """An ore cart on the track, heaped with `load` (the ore's colour)."""
    b.zbox(x, y, 0.07, 0.2, gauge / 2 + 0.03, 0.14, angle, 'wood_mid', bevel=0.01)
    b.zbox(x, y, 0.13, 0.15, gauge / 2 + 0.045, 0.155, angle, 'iron')
    for k in (-1, 1):
        b.hcylinder('x', (x, y + k * 0.08, 0.06), gauge / 2 + 0.02, 0.045, 'iron', sides=6)
    b.blob(x, y, 0.17, 0.11, 0.12, load, sides=5)


def pickaxe(b, x, y, lean_to_y=None):
    """A pick standing against a wall: handle up to (x, lean_to_y), iron head on top."""
    top_y = y + 0.08 if lean_to_y is None else lean_to_y
    b.rod((x, y, 0.01), (x, top_y, 0.34), 0.022, 'wood_light')
    b.rod((x - 0.09, top_y - 0.01, 0.33), (x + 0.09, top_y - 0.01, 0.35), 0.026, 'iron')


# -- food ----------------------------------------------------------------------------------------

STRAW = dict(color='roof_straw', dark='roof_straw_dark')   # Food's roofs: thatch


def rail_fence(b, pts, h=0.26, post_step=0.34, closed=False, color='wood_mid', post='wood_dark'):
    """A low farm fence of posts and two rails along a polyline of (x, y) points."""
    from mathutils import Vector
    pts = list(pts) + ([pts[0]] if closed else [])
    for i in range(len(pts) - 1):
        a, c = Vector((*pts[i], 0)), Vector((*pts[i + 1], 0))
        n = max(1, round((c - a).length / post_step))
        for k in range(n + (1 if i == len(pts) - 2 and not closed else 0)):
            p = a + (c - a) * (k / n)
            b.box(p.x - 0.022, p.x + 0.022, p.y - 0.022, p.y + 0.022, 0, h, post)
        for z in (h * 0.45, h * 0.85):
            b.rod((a.x, a.y, z), (c.x, c.y, z), 0.022, color)


def hay_bale(b, x, y, angle=0.0, z0=0.0, size=0.2):
    b.zbox(x, y, z0, z0 + size * 0.75, size * 0.7, size / 2, angle, 'hay', bevel=0.02)
    b.zbox(x, y, z0, z0 + size * 0.76, size * 0.72 - 0.06, size / 2 + 0.005, angle, 'roof_straw_dark')
    b.zbox(x, y, z0, z0 + size * 0.765, size * 0.72 - 0.08, size / 2 + 0.006, angle, 'hay')


def basket(b, x, y, fill, r=0.065, z0=0.0):
    """A wicker basket heaped with fruit, fish or loaves (`fill` is their colour)."""
    b.cylinder(x, y, z0, z0 + r * 1.1, r, 'wood_light', sides=6)
    b.cylinder(x, y, z0 + r * 0.7, z0 + r * 0.85, r * 1.06, 'wood_mid', sides=6)
    b.blob(x, y, z0 + r * 0.9, r * 0.85, r * 0.7, fill, sides=5)


def trough(b, x0, x1, y, w=0.12, h=0.1):
    b.box(x0, x1, y - w / 2, y + w / 2, 0, h, 'wood_mid', bevel=0.008)
    b.box(x0 + 0.02, x1 - 0.02, y - w / 2 + 0.02, y + w / 2 - 0.02, h - 0.03, h + 0.002, 'water')


# -- level 3: the building at its grandest ----------------------------------------------------------
# Level 2 is the same house grown richer; level 3 keeps the silhouette and crowns it: gold on the
# ridges and spires, a turret or tower where the plot allows, paved yards and low stone walls.

def finial(b, x, y, z, h=0.16):
    """A gilded spike with a ball, on a ridge end or a cone's tip -- level 3's gold."""
    b.cylinder(x, y, z, z + h * 0.35, 0.016, 'gold', sides=6)
    b.ball(x, y, z + h * 0.3, 0.036, h * 0.42, 'gold', sides=6)
    b.pyramid(x, y, z + h * 0.68, 0.012, 0.012, z + h, 'gold')


def turret(b, x, y, z0, z1, r, wall='stone_light', roof='roof_orange', spire=None, face='-y',
           band='stone_mid'):
    """A round turret: an 8-sided shaft from z0 to z1 under a corbelled band, a tall cone roof
    and a gold finial, with a slit window facing `face`. Returns the cone's apex."""
    b.cylinder(x, y, z0, z1, r, wall, sides=8, bevel=0.008)
    b.cylinder(x, y, z1 - 0.06, z1, r + 0.03, band, sides=8)
    apex = z1 + (spire if spire is not None else r * 2.6)
    b.pyramid(x, y, z1 - 0.01, r + 0.07, r + 0.07, apex, roof, sides=8, rot_deg=22.5)
    finial(b, x, y, apex - 0.02)
    a = r * 0.924   # an octagon's flat face, rotated 22.5 so one faces each axis
    sx, sy = {'-y': (0, -1), '+y': (0, 1), '-x': (-1, 0), '+x': (1, 0)}[face]
    zw = (z0 + z1) / 2
    if sx:
        b.box(x + sx * a - 0.008, x + sx * a + 0.008, y - 0.03, y + 0.03, zw - 0.09, zw + 0.09,
              GLASS)
    else:
        b.box(x - 0.03, x + 0.03, y + sy * a - 0.008, y + sy * a + 0.008, zw - 0.09, zw + 0.09,
              GLASS)
    return apex


def paving(b, x0, x1, y0, y1, z=0.014, color='cobble'):
    """Flagstones over a path or yard: the town has paved the doorstep (level 3)."""
    b.box(x0, x1, y0, y1, 0, z, color)


def garden_wall(b, pts, h=0.17, t=0.07, color='stone_light', cap='stone_mid'):
    """A low stone wall with a coping along an axis-aligned polyline `pts` -- level 3's answer
    to the rail and picket fences."""
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        xa, xb = sorted((ax, bx))
        ya, yb = sorted((ay, by))
        b.box(xa - t / 2, xb + t / 2, ya - t / 2, yb + t / 2, 0, h, color, bevel=0.006)
        b.box(xa - t / 2 - 0.012, xb + t / 2 + 0.012, ya - t / 2 - 0.012, yb + t / 2 + 0.012,
              h, h + 0.03, cap, bevel=0.006)


def pier(b, x, y, h=0.3, w=0.06, color='stone_light'):
    """A square gate pier with a gold ball on its cap."""
    b.box(x - w, x + w, y - w, y + w, 0, h, color, bevel=0.008)
    b.box(x - w - 0.015, x + w + 0.015, y - w - 0.015, y + w + 0.015, h, h + 0.03, 'stone_mid')
    b.ball(x, y, h + 0.03, 0.04, 0.07, 'gold', sides=6)


def pennant(b, x, y, z, h=0.32, color='banner_red', length=0.2):
    """An iron pole with a long swallow-tailed pennant flying along +x, for a spire or turret."""
    b.box(x - 0.009, x + 0.009, y - 0.009, y + 0.009, z, z + h, 'iron')
    zt = z + h - 0.02
    b.prism([(x, zt - 0.09), (x + length, zt - 0.06), (x + length * 0.82, zt - 0.035),
             (x + length, zt - 0.01), (x, zt)], (0, y + 0.005, 0), (1, 0, 0), (0, 0, 1), 0.012,
            color, back_cap=True)
    b.ball(x, y, z + h, 0.022, 0.04, 'gold', sides=6)


def cupola(b, x, y, z0, r=0.13, h=0.24, roof='roof_orange', wall='stone_light'):
    """A glazed octagonal lantern on a ridge: a base sitting into the roof from z0, glass all
    round under a cornice, a cone roof and a gold finial. Returns the apex."""
    b.cylinder(x, y, z0 - 0.08, z0 + 0.04, r + 0.02, wall, sides=8)
    b.cylinder(x, y, z0 + 0.04, z0 + h, r, GLASS, sides=8)
    for i in range(8):
        import math
        a = math.radians(22.5 + 45 * i)
        px, py = x + r * math.cos(a), y + r * math.sin(a)
        b.box(px - 0.012, px + 0.012, py - 0.012, py + 0.012, z0 + 0.04, z0 + h, 'wood_light')
    b.cylinder(x, y, z0 + h, z0 + h + 0.03, r + 0.035, wall, sides=8)
    apex = z0 + h + 0.03 + r * 1.8
    b.pyramid(x, y, z0 + h + 0.03, r + 0.05, r + 0.05, apex, roof, sides=8, rot_deg=22.5)
    finial(b, x, y, apex - 0.02, h=0.13)
    return apex


def statue(b, x, y, z0=0.0, scale=1.0, plinth='stone_light'):
    """A gilded figure with a raised arm on a stepped stone plinth -- a town that can afford a
    monument (level 3)."""
    s = scale
    b.box(x - 0.13 * s, x + 0.13 * s, y - 0.13 * s, y + 0.13 * s, z0, z0 + 0.06 * s, 'stone_mid',
          bevel=0.006)
    b.box(x - 0.09 * s, x + 0.09 * s, y - 0.09 * s, y + 0.09 * s, z0 + 0.06 * s, z0 + 0.3 * s,
          plinth, bevel=0.006)
    b.box(x - 0.11 * s, x + 0.11 * s, y - 0.11 * s, y + 0.11 * s, z0 + 0.3 * s, z0 + 0.33 * s,
          'stone_mid')
    zb = z0 + 0.33 * s
    b.frustum(x, y, zb, zb + 0.2 * s, 0.06 * s, 0.035 * s, 'gold', sides=6)
    b.ball(x, y, zb + 0.19 * s, 0.035 * s, 0.07 * s, 'gold', sides=6)
    b.rod((x + 0.03 * s, y, zb + 0.16 * s), (x + 0.08 * s, y, zb + 0.3 * s), 0.022 * s, 'gold')
    b.rod((x - 0.03 * s, y, zb + 0.15 * s), (x - 0.06 * s, y, zb + 0.06 * s), 0.022 * s, 'gold')


def armillary(b, x, y, z0=0.0, r=0.16):
    """A gold armillary sphere -- three crossed rings on an axis -- on a stone column."""
    import math
    b.cylinder(x, y, z0, z0 + 0.05, 0.14, 'stone_mid', sides=8)
    b.cylinder(x, y, z0 + 0.05, z0 + 0.32, 0.055, 'stone_light', sides=8)
    zc = z0 + 0.32 + r + 0.02
    n = 10
    for plane in ('xz', 'yz', 'xy'):
        pts = []
        for i in range(n):
            a = 2 * math.pi * i / n
            u, v = r * math.cos(a), r * math.sin(a)
            pts.append((x + u, y, zc + v) if plane == 'xz' else
                       (x, y + u, zc + v) if plane == 'yz' else (x + u, y + v, zc))
        for i in range(n):
            b.rod(pts[i], pts[(i + 1) % n], 0.018, 'gold')
    b.rod((x, y, z0 + 0.32), (x + 0.06, y, zc + r + 0.06), 0.014, 'iron')
    b.ball(x, y, zc - 0.035, 0.035, 0.07, 'gold', sides=6)


def louvre(b, x, y, z0, half=0.11, h=0.3, roof='roof_red'):
    """A louvred vent turret on a store's ridge: a plank box with slatted openings on all four
    sides under a pyramid roof and a gold finial. z0 sits just under the ridge. Returns the apex."""
    from kit import Facade
    z1 = z0 + h
    b.box(x - half, x + half, y - half, y + half, z0, z1, 'wood_light', bevel=0.008)
    for nm, plane, s in (('-y', y - half, x), ('+y', y + half, x), ('-x', x - half, y),
                         ('+x', x + half, y)):
        f = Facade(b, nm, plane)
        f.rect(s - half + 0.04, s + half - 0.04, z0 + 0.1, z1 - 0.03, 0.006, 'window')
        for k in range(4):
            z = z0 + 0.115 + k * 0.032
            f.rect(s - half + 0.04, s + half - 0.04, z, z + 0.014, 0.016, 'wood_dark')
    b.box(x - half - 0.02, x + half + 0.02, y - half - 0.02, y + half + 0.02, z1, z1 + 0.025,
          'wood_dark')
    apex = z1 + 0.2
    b.pyramid(x, y, z1 + 0.025, half + 0.04, half + 0.04, apex, roof)
    finial(b, x, y, apex - 0.01)
    return apex


def bunting(b, p0, p1, sag=0.08, step=0.09, colors=('banner_red', 'plaster_yellow', 'roof_purple',
                                                      'white', 'banner_blue')):
    """A string of little triangular flags sagging between two points p0, p1 (x, y, z) -- the
    festival look of an entertainment building at level 3."""
    import math
    dx, dy = p1[0] - p0[0], p1[1] - p0[1]
    length = math.hypot(dx, dy)
    ux, uy = dx / length, dy / length

    def at(t):
        return (p0[0] + dx * t, p0[1] + dy * t,
                p0[2] + (p1[2] - p0[2]) * t - sag * 4 * t * (1 - t))
    segs = 6
    for k in range(segs):
        b.rod(at(k / segs), at((k + 1) / segs), 0.008, 'iron')
    n = max(2, int(length / step))
    for i in range(1, n):
        t = i / n
        x, y, z = at(t)
        b.prism([(-0.025, 0.0), (0.025, 0.0), (0.0, -0.065)], (x, y, z - 0.004), (ux, uy, 0),
                (0, 0, 1), 0.006, colors[i % len(colors)], back_cap=True)


# -- level 4: the glazed roof ------------------------------------------------------------------------
# Level 3 crowned the building in gold; level 4 re-lays its roof in glazed tiles: a lattice of
# diamonds in the category's dark shade and gold over the base tiles, and a gilded crest along the
# ridge. The roof is what the game's camera sees most, so this is the tier you read from afar.

def _slopes(cx, cy, axis, half_len, half_span, ze, overhang=None, t=None):
    """The top surface of each slope of a standard d.roof: (origin at the eave's middle,
    unit along the ridge, unit up the slope, unit normal, slope length)."""
    from mathutils import Vector
    overhang = ROOF['overhang'] if overhang is None else overhang
    t = (ROOF['t'] + ROOF['rib_h'] * 0.5) if t is None else t
    zr = ze + half_span * PITCH
    up = Vector((0, 0, 1))
    r = Vector((1, 0, 0)) if axis == 'x' else Vector((0, 1, 0))
    s = Vector((0, 1, 0)) if axis == 'x' else Vector((-1, 0, 0))
    c = Vector((cx, cy, 0))
    out = []
    for side in (-1, 1):
        eave = c + s * (side * half_span) + up * ze
        ridge = c + up * zr
        dvec = (ridge - eave).normalized()
        ax = r * (-side)
        n = ax.cross(dvec)
        start = eave - dvec * overhang + n * (t + 0.004)
        out.append((start, r, dvec, n, (ridge - eave).length + overhang))
    return out


def glazed_roof(b, cx, cy, axis, half_len, half_span, ze, dark, accent='gold', w=0.075,
                step=0.22, row=0.17, overhang=None, t=None):
    """Lays a lattice of flat diamonds over both slopes of a standard d.roof (same arguments):
    rows alternate `dark` and `accent`, every other row offset by half a step. Two triangles a
    diamond; skips any that would hang over a verge or the eave."""
    for start, r, dvec, n, length in _slopes(cx, cy, axis, half_len, half_span, ze, overhang, t):
        k = 0
        v = row * 0.75
        while v < length - w - 0.06:
            off = (step / 2) if k % 2 else 0.0
            col = dark if k % 2 == 0 else accent
            u = -half_len + 0.03 + w + off
            while u < half_len - 0.03 - w:
                cpt = start + r * u + dvec * v
                pts = [cpt - r * w, cpt - dvec * w * 1.3, cpt + r * w, cpt + dvec * w * 1.3]
                verts = [b.bm.verts.new(p) for p in pts]
                f = b._face(verts, col)
                f.normal_update()
                if f.normal.dot(n) < 0:
                    f.normal_flip()
                u += step
            v += row
            k += 1


def ridge_crest(b, cx, cy, axis, half_len, half_span, ze, step=0.14, h=0.07, t=None):
    """A gilded crest along the ridge roll of a standard d.roof: small gold spikes with a ball
    between each pair."""
    zr = ze + half_span * PITCH
    t = (ROOF['t'] + ROOF['rib_h'] * 0.5) if t is None else t
    roll = max(0.025, min(0.055, half_span * 0.12))
    z = zr + t + ROOF['cap_lift'] * 0.5 + roll * 1.3
    a = -half_len + step / 2
    while a < half_len - step / 2 + 1e-6:
        x, y = (cx + a, cy) if axis == 'x' else (cx, cy + a)
        b.pyramid(x, y, z - 0.01, 0.014, 0.014, z + h, 'gold')
        a += step


def glazed(b, cx, cy, axis, half_len, half_span, ze, dark, **kw):
    """Level 4's whole roof treatment: the diamond lattice and the gilded ridge crest."""
    glazed_roof(b, cx, cy, axis, half_len, half_span, ze, dark, **kw)
    ridge_crest(b, cx, cy, axis, half_len, half_span, ze, t=kw.get('t'))


def _diamond(b, c, r, dvec, n, w, col):
    verts = [b.bm.verts.new(p) for p in (c - r * w, c - dvec * w * 1.3, c + r * w, c + dvec * w * 1.3)]
    f = b._face(verts, col)
    f.normal_update()
    if f.normal.dot(n) < 0:
        f.normal_flip()


def glazed_pyramid(b, cx, cy, z0, hx, hy, apex, dark, accent='gold', w=0.055, step=0.18,
                   row=0.15, crest=True):
    """The level-4 lattice on the four faces of a square b.pyramid roof (same cx, cy, z0, hx,
    hy, apex), rows narrowing towards the apex, and a gold finial on its tip."""
    from mathutils import Vector
    corners = [Vector((cx - hx, cy - hy, z0)), Vector((cx + hx, cy - hy, z0)),
               Vector((cx + hx, cy + hy, z0)), Vector((cx - hx, cy + hy, z0))]
    top = Vector((cx, cy, apex))
    for i in range(4):
        a, bb = corners[i], corners[(i + 1) % 4]
        base_mid = (a + bb) / 2
        dvec = (top - base_mid).normalized()
        r = (bb - a).normalized()
        n = r.cross(dvec).normalized()
        if n.dot(base_mid - Vector((cx, cy, z0))) < 0:
            n = -n
        height = (top - base_mid).length
        k, v = 0, row * 0.7
        while v < height - w * 2.5:
            t = v / height
            half = (bb - a).length / 2 * (1 - t) - w - 0.03
            col = dark if k % 2 == 0 else accent
            off = step / 2 if k % 2 else 0.0
            u = -half + off
            while u <= half + 1e-6:
                c = base_mid + dvec * v + r * u + n * 0.006
                _diamond(b, c, r, dvec, n, w, col)
                u += step
            v += row
            k += 1
    if crest:
        finial(b, cx, cy, apex - 0.02, h=0.12)


def glazed_shed(b, axis, a0, a1, wall, out, z_wall, z_out, dark, accent='gold', w=0.055,
                step=0.18, row=0.15):
    """The level-4 lattice over a d.shed_roof (same first seven arguments), and a row of gold
    spikes along its high edge."""
    from mathutils import Vector
    if axis == 'x':
        p0, p1, r = Vector((0, wall, z_wall)), Vector((0, out, z_out)), Vector((1, 0, 0))
    else:
        p0, p1, r = Vector((wall, 0, z_wall)), Vector((out, 0, z_out)), Vector((0, 1, 0))
    dvec = (p1 - p0).normalized()
    ax = r if r.cross(dvec).z > 0 else -r
    n = ax.cross(dvec)
    t = ROOF['t'] * 0.8 + ROOF['rib_h'] * 0.5
    half_len = (a1 - a0) / 2
    mid_a = (a0 + a1) / 2
    length = (p1 - p0).length
    hi, lo_ = (p0, p1) if p0.z >= p1.z else (p1, p0)
    down = (lo_ - hi).normalized()
    k, v = 0, row * 0.6
    while v < length - w:
        col = dark if k % 2 == 0 else accent
        off = step / 2 if k % 2 else 0.0
        u = -half_len + 0.03 + w + off
        while u < half_len - 0.03 - w:
            c = hi + down * v + r * (mid_a + u) + n * (t + 0.004)
            _diamond(b, c, r, -down, n, w, col)
            u += step
        v += row
        k += 1
    u = -half_len + 0.07
    while u < half_len - 0.05:
        p = hi + r * (mid_a + u) + n * t
        b.pyramid(p.x, p.y, p.z - 0.01, 0.012, 0.012, p.z + 0.06, 'gold')
        u += 0.14


def ring_crest(b, pts, z, step=0.2, h=0.08):
    """Gold spikes along a closed outline `pts` at height z -- a ring roof's or a rim's crest."""
    import math
    for (ax, ay), (bx, by) in zip(pts, pts[1:] + pts[:1]):
        seg = math.hypot(bx - ax, by - ay)
        n = max(1, int(seg / step))
        for i in range(n):
            t = i / n
            x, y = ax + (bx - ax) * t, ay + (by - ay) * t
            b.pyramid(x, y, z - 0.01, 0.014, 0.014, z + h, 'gold')
