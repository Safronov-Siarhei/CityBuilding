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

def footing(b, x0, x1, y0, y1, color='stone_mid'):
    b.box(x0 - 0.035, x1 + 0.035, y0 - 0.035, y1 + 0.035, 0, FOOTING, color)


def roof(b, cx, cy, axis, half_len, half_span, ze, color='roof_orange', dark='roof_orange_dark',
         **override):
    """Standard ribbed gable roof. Returns the ridge height (slab underside)."""
    zr = ze + half_span * PITCH
    params = dict(ROOF)
    params.update(override)
    b.gable_roof(cx, cy, axis, half_len, half_span, ze, zr, color, dark, **params)
    return zr


def chimney(b, cx, cy, z0, z1, hw=0.09, color='stone_mid'):
    b.box(cx - hw, cx + hw, cy - hw, cy + hw, z0, z1, color)
    b.box(cx - hw - 0.025, cx + hw + 0.025, cy - hw - 0.025, cy + hw + 0.025, z1 - 0.05, z1 + 0.02,
          'stone_dark')


# -- openings ----------------------------------------------------------------------------------

def plain_window(f, s, base, w=WIN_W, sill='wood_mid'):
    z0 = base + WIN_SILL
    f.rect(s - w / 2, s + w / 2, z0, z0 + WIN_H, 0.01, 'window')
    f.rect(s - w / 2 - 0.03, s + w / 2 + 0.03, z0 - 0.03, z0, 0.035, sill)


def cross_window(f, s, base, w=WIN_W, sill='wood_mid', bars='wood_dark'):
    z0 = base + WIN_SILL
    z1 = z0 + WIN_H
    f.rect(s - w / 2, s + w / 2, z0, z1, 0.01, 'window')
    f.rect(s - 0.011, s + 0.011, z0, z1, 0.02, bars)
    f.rect(s - w / 2, s + w / 2, (z0 + z1) / 2 - 0.01, (z0 + z1) / 2 + 0.01, 0.02, bars)
    f.rect(s - w / 2 - 0.03, s + w / 2 + 0.03, z0 - 0.03, z0, 0.035, sill)


def shutter_window(f, s, base, w=WIN_W, shutter='wood_mid'):
    z0 = base + WIN_SILL
    z1 = z0 + WIN_H
    f.rect(s - w / 2, s + w / 2, z0, z1, 0.01, 'window')
    f.rect(s - w / 2 - 0.02, s + w / 2 + 0.02, z0 - 0.03, z0, 0.035, 'wood_dark')
    f.rect(s - w / 2 - 0.09, s - w / 2 - 0.01, z0, z1, 0.02, shutter)
    f.rect(s + w / 2 + 0.01, s + w / 2 + 0.09, z0, z1, 0.02, shutter)


def arch_window(f, s, base, w=ARCH_W, h=ARCH_H, sill='stone_mid'):
    z0 = base + ARCH_SILL
    f.arch(s, z0, w, h, 0.01, 'window')
    f.rect(s - w / 2 - 0.03, s + w / 2 + 0.03, z0 - 0.03, z0, 0.03, sill)


def slit(f, s, z0, z1):
    f.rect(s - 0.035, s + 0.035, z0, z1, 0.01, 'window')


def flower_box(f, s, base, w=WIN_W + 0.08, flowers='roof_red'):
    z = base + WIN_SILL - 0.03
    f.rect(s - w / 2, s + w / 2, z - 0.05, z, 0.06, 'wood_mid')
    f.rect(s - w / 2 + 0.01, s + w / 2 - 0.01, z, z + 0.035, 0.05, flowers)


def plank_door(f, s, base=FOOTING, w=DOOR_W, h=DOOR_H, color='door', frame='wood_dark'):
    f.rect(s - w / 2 - 0.03, s + w / 2 + 0.03, base, base + h + 0.03, 0.015, frame)
    f.rect(s - w / 2, s + w / 2, base, base + h, 0.03, color)
    f.rect(s - 0.006, s + 0.006, base, base + h, 0.036, frame)
    f.rect(s + w / 2 - 0.06, s + w / 2 - 0.035, base + h * 0.45, base + h * 0.45 + 0.025, 0.045,
           'iron')


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
