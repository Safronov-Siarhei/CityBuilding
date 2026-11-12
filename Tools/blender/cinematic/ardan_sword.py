"""Ардан with a SWORD for campaign mission 2 (the user's second playtest, 2026-11-12: «почему мы играем за
Латника?» -- the user chose mission 1's Ардан with a sword and attack/skill animations). DRAFT for approval:
the model with the sword, and the key poses of what he will do in the mission. Never saves.

    blender --background <Buildings-Size1-Models.blend> --python ardan_sword.py -- <out dir>

The sword is a prop on the right forearm's bone (rigid, like the sack in ardan_anim.py): a plain
arming sword -- a steel blade, a brass crossguard and pommel, a leather grip -- nothing grander, he is
a village lad who has just picked it up. The poses (each a still; the animations go between them):
  1 Ready      -- the stance he stands and walks in, the sword low in front;
  2 WindUp     -- the strike drawn back over the right shoulder;
  3 Strike     -- the blow come down across (the plain attack is 2 -> 3 -> 1);
  4 Whirl      -- «Круговой удар»: the sword out at arm's length, spinning;
  5 Cry        -- «Боевой клич»: the sword raised high, the head back, shouting;
  6 Dash       -- «Натиск»: the lunge, the sword out in front;
  7 Breath     -- «Второе дыхание»: the sword point down, a hand on the chest.
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import bpy  # noqa: E402
import ardan  # noqa: E402
import ardan_anim as aa  # noqa: E402
import ardan_anim2 as a2  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Vector  # noqa: E402

S = ardan.SCALE
STEEL, STEEL_DARK = 'stone_light', 'iron'
detail.OVERRIDE.update({c: detail.PLAIN for c in (STEEL, STEEL_DARK)})


def sword(arm, mat):
    """The sword in his right fist, the blade forward out of it (at rest, the arm hanging, it points ahead)."""
    _, _, wr = ardan._joints(-1)
    x, y, z = wr.x, wr.y, wr.z - 0.034
    b = Builder()
    b.box(x - 0.008, x + 0.008, y - 0.03, y + 0.03, z - 0.008, z + 0.008, ardan.LACE, bevel=0.003)          # grip
    b.ball(x, y + 0.04, z, 0.013, 0.013, ardan.BRASS, sides=6)                                               # pommel
    b.box(x - 0.045, x + 0.045, y - 0.042, y - 0.032, z - 0.009, z + 0.009, ardan.BRASS, bevel=0.003)       # crossguard
    b.box(x - 0.011, x + 0.011, y - 0.25, y - 0.042, z - 0.004, z + 0.004, STEEL)                           # blade
    b.box(x - 0.003, x + 0.003, y - 0.24, y - 0.042, z - 0.006, z + 0.006, STEEL_DARK)                      # fuller
    b.prism([(-0.011, 0.0), (0.011, 0.0), (0.0, -0.028)], (x, y - 0.25, z), (1, 0, 0), (0, 1, 0), 0.008, STEEL,
            back_cap=True)                                                                                    # point
    # the blade between forward and along the arm (a cocked wrist): rigid on the forearm, it points
    # forward when the arm is out in front, down when it hangs
    from mathutils import Matrix
    tilt = Matrix.Translation((x, y, z)) @ Matrix.Rotation(math.radians(45), 4, 'X') @ Matrix.Translation((-x, -y, -z))
    for v in b.bm.verts:
        v.co = (tilt @ v.co) * S
    me = b.to_mesh("CineArdanSword")
    me.materials.append(mat)
    detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
    obj = bpy.data.objects.new("CineArdanSword", me)
    arm.users_collection[0].objects.link(obj)
    obj.vertex_groups.new(name="forearm.R").add(list(range(len(me.vertices))), 1.0, 'REPLACE')
    obj.parent = arm
    mod = obj.modifiers.new("Rig", 'ARMATURE')
    mod.object = arm
    print(f"MODEL CineArdanSword tris={sum(len(p.vertices) - 2 for p in me.polygons)}")
    return obj


def _pose(hips=(0, 0, 0), chest=(0, 0, 0), head=(0, 0, 0), arm_r=((0, 0, 0), 0), arm_l=((0, 0, 0), 0),
          leg_l=(0, 0, 0), leg_r=(0, 0, 0), drop=0.0):
    """A pose as ardan_anim2's dict of (rx, ry, rz) per bone: easy to mix (a2.mix) into clips."""
    q = a2.blank()
    q["hips"] = list(hips)
    q["chest"] = list(chest)
    q["head"] = list(head)
    q["upper_arm.R"], q["forearm.R"] = list(arm_r[0]), [arm_r[1], 0, 0]
    q["upper_arm.L"], q["forearm.L"] = list(arm_l[0]), [arm_l[1], 0, 0]
    for side, (th, sh, ft) in (("L", leg_l), ("R", leg_r)):
        q["thigh." + side], q["shin." + side], q["foot." + side] = [th, 0, 0], [sh, 0, 0], [ft, 0, 0]
    q["hips_loc"] = [0, -drop, 0]
    return q


def ready():
    return _pose(chest=(6, 0, 10), arm_r=((-25, 10, 0), -45), arm_l=((-10, -12, 0), -40),
                 leg_l=(-12, 14, -2), leg_r=(10, 8, -18), drop=0.004)


def wind_up():
    return _pose(hips=(0, 0, 15), chest=(-4, 6, 30), head=(0, 0, -20), arm_r=((-160, -30, 0), -70),
                 arm_l=((-45, -20, 0), -50), leg_l=(-16, 18, -2), leg_r=(14, 10, -24), drop=0.006)


def strike():
    return _pose(hips=(0, 0, -15), chest=(18, -6, -30), head=(-10, 0, 20), arm_r=((-70, 30, 0), -5),
                 arm_l=((10, -25, 0), -30), leg_l=(-30, 30, 0), leg_r=(20, 12, -30), drop=0.012)


def whirl():
    return _pose(hips=(0, 0, 70), chest=(10, 0, 30), arm_r=((-10, 85, 0), -10), arm_l=((-10, -70, 0), -20),
                 leg_l=(-20, 25, 0), leg_r=(20, 25, -20), drop=0.01)


def cry():
    return _pose(chest=(-10, 0, 0), head=(-22, 0, 0), arm_r=((-175, 10, 0), -15), arm_l=((-20, -35, 0), -95),
                 leg_l=(-14, 6, 0), leg_r=(14, 6, -14))


def dash():
    return _pose(chest=(28, 0, -10), head=(-20, 0, 0), arm_r=((-95, 5, 0), 0), arm_l=((40, -15, 0), -20),
                 leg_l=(-60, 55, 5), leg_r=(40, 10, -30), drop=0.03)


def breath():
    q = _pose(chest=(8, 0, 0), head=(12, 0, 0), arm_r=((8, 8, 0), -15), arm_l=((-35, -10, 0), -120),
              leg_l=(-6, 10, 0), leg_r=(6, 10, -6))
    q["forearm.L"] = [-120, 0, 20]
    return q


def wind_up2():
    """Strike 2 (the user's third playtest: «атаки ардана скучные»): the backhand -- the blade drawn across the
    chest to the left shoulder, the body turned away."""
    return _pose(hips=(0, 0, -20), chest=(-2, -6, -38), head=(0, 0, 25), arm_r=((-85, -75, 0), -105),
                 arm_l=((-20, -10, 0), -60), leg_l=(-14, 16, -2), leg_r=(12, 10, -22), drop=0.006)


def strike2():
    """...and the backhand cut out to the right, the arm straight."""
    return _pose(hips=(0, 0, 18), chest=(12, 6, 34), head=(-6, 0, -22), arm_r=((-80, 75, 0), -8),
                 arm_l=((15, -30, 0), -35), leg_l=(-26, 26, 0), leg_r=(18, 12, -28), drop=0.012)


def wind_up3():
    """Strike 3: the overhead -- the sword raised straight up and back."""
    return _pose(chest=(-14, 0, 6), head=(-8, 0, 0), arm_r=((-178, 5, 0), -75), arm_l=((-150, -10, 0), -80),
                 leg_l=(-10, 12, -2), leg_r=(10, 8, -16), drop=0.002)


def strike3():
    """...and brought straight down in front, a deep lunge."""
    return _pose(hips=(8, 0, 0), chest=(30, 0, 0), head=(-18, 0, 0), arm_r=((-55, 5, 0), 0),
                 arm_l=((-40, -10, 0), -20), leg_l=(-45, 45, 0), leg_r=(28, 14, -34), drop=0.03)


POSES = [("1-Ready", ready), ("2-WindUp", wind_up), ("3-Strike", strike), ("4-Whirl", whirl),
         ("5-Cry", cry), ("6-Dash", dash), ("7-Breath", breath),
         ("8-WindUp2", wind_up2), ("9-Strike2", strike2), ("10-WindUp3", wind_up3), ("11-Strike3", strike3)]


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    out = args[0]
    only = set(args[1].split(",")) if len(args) > 1 else None   # e.g. "2-WindUp,3-Strike": just these poses
    os.makedirs(out, exist_ok=True)
    mesh = ardan.make(Vector((0, 0, 0)))
    arm = aa.rig(mesh)
    blade = sword(arm, mesh.data.materials[0])
    cam = aa.stage({mesh.name, arm.name, blade.name})
    sc = bpy.context.scene
    sc.render.resolution_x, sc.render.resolution_y = 720, 720
    sc.render.image_settings.file_format = 'PNG'
    at = Vector((0, 0, 0.28 * S))
    cam.data.lens = 60
    for name, pose in POSES:
        if only and name not in only:
            continue
        aa.rest(arm)
        a2.apply(arm, pose())
        bpy.context.view_layer.update()
        for view, offset in (("", Vector((-1.25, -1.55, 0.55))), ("-side", Vector((1.9, -0.35, 0.4)))):
            cam.location = at + offset
            cam.rotation_euler = (at - cam.location).to_track_quat('-Z', 'Y').to_euler()
            sc.render.filepath = os.path.join(out, f"ardan_{name}{view}.png")
            bpy.ops.render.render(write_still=True)
        print("RENDERED", name)


if __name__ == "__main__":
    main()
