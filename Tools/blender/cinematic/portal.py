"""The stones on the village square and the portal they become (opening cutscene, 2026-11-10).

ONE set of stones in two poses, so the cutscene can lift them from where they lie and fly them
into the arch:
  * RUIN -- what the village has always known: a round, mossy stone platform in the square's
    middle, the two bottom blocks of the old pillars still standing, the rest tumbled round it,
    half sunk, leaning, one on another; children sit on them.
  * ARCH -- the same blocks stacked into two pillars and a round arch with a keystone, the
    portal's purple swirl inside it.
The stones are dark ('stone_dark') with BLUE runes ('water') -- the very stone and rune of
Ардан's amulet. Each stone is its own object (CinePortalStoneNN) placed in the ruin pose, with
its arch pose kept in the custom property "arch" (a flattened 4x4 world matrix).
Built in the cutscene's own units (no SET_SCALE): an arch the orc champion walks out of.

    blender --background <Buildings-Size1-Models.blend> --python portal.py [-- render <out dir>]
"""
import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import bpy  # noqa: E402
import detail  # noqa: E402
import figure as fg  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Euler, Matrix, Vector  # noqa: E402

STONE, RUNE, MOSS, MOSS_DARK = 'stone_dark', 'water', 'grass', 'grass_dark'
fg.plain(STONE, RUNE, MOSS, MOSS_DARK, 'roof_purple', 'roof_purple_dark', 'white', 'cobble', 'stone_mid',
         'roof_slate', 'roof_slate_dark')

# the arch (final units): two pillars of four blocks, a semicircle of seven over them
HALF_GAP = 0.5            # inner half-width: the champion and his cleaver pass through
BLOCK = (0.3, 0.26, 0.26)  # a pillar block: across, deep, high
PILLAR_BLOCKS = 4
ARCH_BLOCKS = 7
SPRING = PILLAR_BLOCKS * BLOCK[2]
R_IN = HALF_GAP
R_OUT = HALF_GAP + BLOCK[0]
RUNE_SHAPES = [
    [((0, -0.07), (0, 0.07)), ((-0.04, 0.03), (0, 0.07)), ((0.04, 0.03), (0, 0.07))],      # an arrow up
    [((-0.04, -0.06), (0.04, 0.06)), ((0.04, -0.06), (-0.04, 0.06))],                      # a cross-stroke X
    [((0, -0.07), (0, 0.07)), ((0, 0.0), (0.05, 0.05)), ((0, -0.04), (0.05, 0.01))],        # a branch
    [((-0.05, -0.05), (0, 0.06)), ((0, 0.06), (0.05, -0.05)), ((-0.03, -0.01), (0.03, -0.01))],   # a peak
    [((0, -0.07), (0, 0.07)), ((-0.05, 0.0), (0.05, 0.0)), ((-0.03, -0.05), (0.03, -0.05))],
]


def rough(pts, rnd, cut):
    """A convex outline with every corner knocked off by a different amount: weathered, not cut."""
    out = []
    n = len(pts)
    for i in range(n):
        p, prev, nxt = Vector(pts[i]), Vector(pts[i - 1]), Vector(pts[(i + 1) % n])
        out.append(tuple(p + (prev - p).normalized() * rnd.uniform(cut * 0.4, cut)))
        out.append(tuple(p + (nxt - p).normalized() * rnd.uniform(cut * 0.4, cut)))
    return out


def stone_mesh(name, size, rune, wedge=None, moss=False, seed=0):
    """One old block centred on its origin, its rune on its front (-y) face; `wedge` = (inner, outer)
    half-widths for an arch block (narrower inside). Weathered: every corner knocked off unevenly,
    the front and back halves worn differently, so its edges step and no face is a clean box."""
    b = Builder()
    sx, sy, sz = size
    rnd = random.Random(seed * 31 + 5)
    if wedge:
        wi, wo = wedge
        outline = [(-wi, -sz / 2), (wi, -sz / 2), (wo, sz / 2), (-wo, sz / 2)]
    else:
        outline = [(-sx / 2, -sz / 2), (sx / 2, -sz / 2), (sx / 2, sz / 2), (-sx / 2, sz / 2)]
    # one body lofted from a worn front outline to a differently worn, slightly smaller back one:
    # the sides lean and twist a little, like a stone weathered for centuries, with no seam
    front = rough(outline, rnd, 0.07)
    shrink = rnd.uniform(0.88, 0.95)
    back = [(x * shrink + rnd.uniform(-0.012, 0.012), z * shrink + rnd.uniform(-0.012, 0.012))
            for x, z in rough(outline, rnd, 0.09)]
    bm = b.bm
    fv = [bm.verts.new((x, -sy * 0.52, z)) for x, z in front]
    bv = [bm.verts.new((x, sy * 0.48, z)) for x, z in back]
    faces = [b._face(fv, STONE), b._face(list(reversed(bv)), STONE)]
    if faces[0].normal.y > 0:
        for f in faces:
            f.normal_flip()
    n = len(fv)
    for i in range(n):
        j = (i + 1) % n
        faces.append(b._face([fv[j], fv[i], bv[i], bv[j]], STONE))
    if faces[-1].calc_center_median().length and faces[-1].normal.dot(faces[-1].calc_center_median()) < 0:
        for f in faces[2:]:
            f.normal_flip()
    b._bevel(faces, 0.01)
    # a softened rim round the front face: the face sits a hair proud of the worn edge
    face = [(x * 0.86, z * 0.86) for x, z in rough(outline, rnd, 0.05)]
    b.prism(face, (0, -sy * 0.52 + 0.001, 0), (1, 0, 0), (0, 0, 1), 0.012, STONE, bevel=0.006, back_cap=True)
    # the rune, cut in blue
    for (a, c) in RUNE_SHAPES[rune % len(RUNE_SHAPES)]:
        a, c = Vector((a[0], 0, a[1])), Vector((c[0], 0, c[1]))
        mid = (a + c) / 2
        d = c - a
        ax = d.normalized()
        b.obox((mid.x, -sy * 0.52 - 0.013, mid.z), ax, (0, 1, 0), ax.cross(Vector((0, 1, 0))) * -1,
               d.length / 2 + 0.006, 0.006, 0.008, RUNE, skip=())
    me = b.to_mesh(name)
    return me


def arch_poses():
    """(name, size, wedge, arch world matrix) for every stone, pillars bottom-up then the arch."""
    out = []
    for k, side in ((-1, "L"), (1, "R")):
        x = k * (HALF_GAP + BLOCK[0] / 2)
        for i in range(PILLAR_BLOCKS):
            z = BLOCK[2] * (i + 0.5)
            sway = Matrix.Rotation(random.Random(i * 5 + k).uniform(-0.035, 0.035), 4, 'Z')   # laid by hand, not by rule
            out.append((f"Pillar{side}{i}", BLOCK, None, Matrix.Translation((x, 0, z)) @ sway))
    for i in range(ARCH_BLOCKS):
        a0 = math.pi * i / ARCH_BLOCKS
        a1 = math.pi * (i + 1) / ARCH_BLOCKS
        am = (a0 + a1) / 2
        rm = (R_IN + R_OUT) / 2
        half = (a1 - a0) / 2
        wi, wo = R_IN * math.tan(half) - 0.006, R_OUT * math.tan(half) - 0.006
        centre = Vector((-rm * math.cos(am), 0, SPRING + rm * math.sin(am)))
        # the block's local +z points outward from the arch's centre
        rot = Euler((0, -(math.pi / 2 - am), 0)).to_matrix().to_4x4()
        rot = Matrix.Rotation(-(math.pi / 2 - am), 4, 'Y')
        rot = Matrix.Rotation(am - math.pi / 2, 4, 'Y')
        size = (wo * 2, BLOCK[1], BLOCK[0])
        out.append((f"Arch{i}", size, (wi, wo), Matrix.Translation(centre) @ rot))
    return out


PLACED = []


def ruin_pose(i, name, size, arch):
    """Where a stone lies in the ruin: the two bottom pillar blocks stand in place; the others
    tumbled outward round the platform, half sunk, tipped, one leaning on another."""
    if name in ("PillarL0", "PillarR0"):
        PLACED.append(arch.to_translation() * Vector((1, 1, 0)))
        return arch @ Matrix.Translation((0, 0, -0.04))                     # sunk a little, still standing
    rnd = random.Random(i * 7 + 3)
    a = (i * 2.399 + 0.6) % (2 * math.pi)                                    # golden-angle spread round the ring
    if abs(math.sin(a)) < 0.25 and math.cos(a) > 0:                          # keep the front of the gap clear for the children
        a += 0.6
    for _ in range(60):                                                      # no stone inside another
        aa = a + rnd.uniform(-0.3, 0.3)
        r = rnd.uniform(0.6, 1.15)
        spot = Vector((r * math.cos(aa), r * math.sin(aa), 0))
        if all((spot - q).length > 0.36 for q in PLACED):
            break
    a = aa
    PLACED.append(spot)
    face_out = Matrix.Rotation(a + math.pi / 2, 4, 'Z')                      # the rune face (-y) turned outward
    kind = i % 3
    if kind == 0:                                                            # still upright, leaning
        lie = Matrix.Rotation(rnd.uniform(-0.25, 0.25), 4, 'X') @ Matrix.Rotation(rnd.uniform(-0.2, 0.2), 4, 'Y')
    elif kind == 1:                                                          # tipped on its side
        lie = Matrix.Rotation(math.pi / 2 + rnd.uniform(-0.15, 0.15), 4, 'Y')
    else:                                                                    # fallen on its back, rune to the sky
        lie = Matrix.Rotation(-math.pi / 2 + rnd.uniform(-0.25, 0.1), 4, 'X')
    rot = face_out @ Matrix.Rotation(rnd.uniform(-0.3, 0.3), 4, 'Z') @ lie
    hx, hy, hz = (s / 2 for s in size)
    low = min((rot @ Vector((x, y, z))).z for x in (-hx, hx) for y in (-hy, hy) for z in (-hz, hz))
    sink = rnd.uniform(0.03, 0.06)
    return Matrix.Translation((r * math.cos(a), r * math.sin(a), 0.085 - low - sink)) @ rot


def platform():
    """The round stone platform in the square's middle: a cracked ring of slabs, a low step, moss."""
    b = Builder()
    b.cylinder(0, 0, 0.0, 0.05, 1.45, 'stone_mid', sides=16)
    b.cylinder(0, 0, 0.05, 0.08, 1.2, 'cobble', sides=16)
    rnd = random.Random(11)
    for ring, (r0, r1, n) in enumerate(((0.36, 0.7, 8), (0.7, 1.18, 14))):  # slab joints: two rings of slabs
        for i in range(n):
            a = 2 * math.pi * (i + 0.5 * ring) / n
            b.rod((r0 * math.cos(a), r0 * math.sin(a), 0.081), (r1 * math.cos(a), r1 * math.sin(a), 0.081), 0.006, 'roof_slate')
    for i in range(32):
        a0, a1 = 2 * math.pi * i / 32, 2 * math.pi * (i + 1) / 32
        b.rod((0.7 * math.cos(a0), 0.7 * math.sin(a0), 0.081), (0.7 * math.cos(a1), 0.7 * math.sin(a1), 0.081), 0.006, 'roof_slate')
    b.cylinder(0, 0, 0.08, 0.083, 0.36, 'roof_slate', sides=12)
    b.cylinder(0, 0, 0.083, 0.085, 0.33, 'cobble', sides=12)
    for i in range(12):                                                       # rubble: chips knocked off the stones
        a = rnd.uniform(0, 2 * math.pi)
        r = rnd.uniform(0.5, 1.35)
        h = rnd.uniform(0.025, 0.05)
        pts = [(h * math.cos(t), h * math.sin(t)) for t in (2 * math.pi * k / 4 + rnd.uniform(-0.3, 0.3) for k in range(4))]
        rot = rnd.uniform(0, math.pi)
        b.prism(rough(pts, rnd, h * 0.4), (r * math.cos(a), r * math.sin(a), 0.08),
                (math.cos(rot), math.sin(rot), 0), (-math.sin(rot), math.cos(rot), 0), rnd.uniform(0.02, 0.04),
                STONE if i % 3 else 'stone_mid', bevel=0.004, back_cap=True)
    for i in range(5):                                                        # short zigzag cracks, spread round
        a = 2 * math.pi * i / 5 + rnd.uniform(-0.3, 0.3)
        r = rnd.uniform(0.45, 1.0)
        p0 = Vector((r * math.cos(a), r * math.sin(a), 0.082))
        for k in range(3):
            step = Vector((math.cos(a + (0.5 if k % 2 else -0.5)), math.sin(a + (0.5 if k % 2 else -0.5)), 0)) * 0.06
            b.rod(tuple(p0), tuple(p0 + step), 0.005, 'roof_slate_dark')
            p0 = p0 + step
    return b


def swirl():
    """The portal's surface: rings from dark purple at the rim to white at the heart, a twist of
    arms -- the game's portal colours (orcs.PORTAL)."""
    b = Builder()
    cz = (SPRING + R_IN) * 0.5
    rd = R_IN * 0.99                                                          # the opening's own outline: arch on two jambs
    door = [(rd * math.cos(t), SPRING + rd * math.sin(t)) for t in (math.pi * i / 16 for i in range(17))]
    door += [(-rd, 0.02), (rd, 0.02)]
    b.prism(door, (0, -0.02, 0), (1, 0, 0), (0, 0, 1), 0.01, 'roof_purple_dark', back_cap=True)
    for j, (rx, c) in enumerate(((0.4, 'roof_purple'), (0.27, 'roof_purple_dark'), (0.16, 'roof_purple'), (0.07, 'white'))):
        pts = [(rx * math.cos(t), cz + rx * 1.55 * math.sin(t)) for t in (2 * math.pi * i / 20 for i in range(20))]
        b.prism(pts, (0, -0.026 - 0.004 * j, 0), (1, 0, 0), (0, 0, 1), 0.004, c, back_cap=True)
    for k in range(5):                                                         # swirling arms
        for s in range(10):
            t = s / 10
            a = k * 2 * math.pi / 5 + t * 3.0
            r = R_IN * (0.12 + 0.68 * t)
            z = cz + r * math.sin(a) * 1.55
            b.obox((r * math.cos(a), -0.05, z), (math.cos(a + 1.3), 0, math.sin(a + 1.3)), (0, 1, 0),
                   (math.sin(a + 1.3), 0, -math.cos(a + 1.3)), 0.03, 0.004, 0.008, 'white' if s < 3 else 'roof_purple')
    return b


def install(name, me, matrix):
    obj = bpy.data.objects.get(name)
    if obj is None:
        obj = bpy.data.objects.new(name, me)
        fg.ensure_collection().objects.link(obj)
    else:
        old = obj.data
        obj.data = me
        if old.users == 0:
            bpy.data.meshes.remove(old)
    obj.matrix_world = matrix
    return obj


def make(origin=(0, 0, 0)):
    """The platform, every stone in its ruin pose (arch pose in its "arch" property), the swirl
    hidden; returns (platform, stones, swirl)."""
    o = Matrix.Translation(origin)
    mat = fg.material()
    plat_b = platform()
    plat = fg.install_static("CinePortalSquare", plat_b, scale=1.0, location=origin)
    stones = []
    PLACED.clear()
    for i, (name, size, wedge, arch) in enumerate(arch_poses()):
        me = stone_mesh("CinePortalStone_" + name, size, i, wedge, moss=True, seed=i)
        me.materials.append(mat)
        detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
        obj = install("CinePortalStone_" + name, me, o @ ruin_pose(i, name, size, arch))
        obj["arch"] = [v for row in (o @ arch @ Matrix.Translation((0, 0, 0.08))) for v in row]
        stones.append(obj)
    sw = fg.install_static("CinePortalSwirl", swirl(), scale=1.0, location=(origin[0], origin[1], origin[2] + 0.08))
    sw.hide_render = True
    print(f"MODEL CinePortal: {len(stones)} stones")
    return plat, stones, sw


def set_arch(stones, sw, on):
    for s in stones:
        if on:
            s["ruin"] = [v for row in s.matrix_world for v in row]
            s.matrix_world = Matrix([s["arch"][i * 4:(i + 1) * 4] for i in range(4)])
        elif "ruin" in s:
            s.matrix_world = Matrix([s["ruin"][i * 4:(i + 1) * 4] for i in range(4)])
    sw.hide_render = not on


def render(out):
    import townsfolk
    import elder
    import orcs_cast
    import ardan
    os.makedirs(out, exist_ok=True)
    plat, stones, sw = make()
    kids = [townsfolk.make("CineBoy", (0.05, -0.2, 0.08)), townsfolk.make("CineGirl", (-0.2, -0.75, 0.08))]
    old = elder.make((0.9, -1.2, 0))
    hero = ardan.make(Vector((-1.1, -1.8, 0)))
    champ = orcs_cast.make("CineOrcChampion", (0.0, -0.55, 0.08))
    sc = bpy.context.scene
    keep = {plat.name, sw.name, old.name, hero.name, champ.name} | {s.name for s in stones} | {k.name for k in kids}
    for ob in sc.objects:
        ob.hide_render = ob.name not in keep
    sw.hide_render = True
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 1400, 900
    sc.view_settings.view_transform = 'Standard'
    for name, energy, rx, rz in (("SetSun", 3.5, 50, -25), ("SetFill", 1.0, 70, 150)):
        light = bpy.data.objects.new(name, bpy.data.lights.new(name, 'SUN'))
        light.data.energy = energy
        sc.collection.objects.link(light)
        light.rotation_euler = (math.radians(rx), 0, math.radians(rz))
    w = sc.world or bpy.data.worlds.new("W")
    sc.world = w
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.8, 0.82, 0.88, 1)
    cam = bpy.data.objects.new("SetCam", bpy.data.cameras.new("SetCam"))
    sc.collection.objects.link(cam)
    sc.camera = cam

    def shot(name, at, off, lens):
        at = Vector(at)
        cam.location = at + Vector(off)
        cam.data.lens = lens
        cam.rotation_euler = (at - cam.location).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = os.path.join(out, name + ".png")
        bpy.ops.render.render(write_still=True)

    champ.hide_render = True
    shot("ruin", (0, 0, 0.3), (2.4, -4.2, 2.4), 40)
    shot("ruin_stone", (0.5, -0.0, 0.25), (0.9, -1.4, 0.5), 50)
    set_arch(stones, sw, True)
    for k in kids:
        k.hide_render = True
    old.hide_render = True
    champ.hide_render = False
    shot("portal", (0, 0, 0.9), (2.0, -4.6, 1.4), 40)
    shot("portal_front", (0, 0, 0.9), (0.0, -4.4, 0.6), 40)


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if argv and argv[0] == "render":
        render(argv[1])
    else:
        make()
