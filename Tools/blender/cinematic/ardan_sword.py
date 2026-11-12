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


def legs(p, l=(0, 0, 0), r=(0, 0, 0)):
    for side, (th, sh, ft) in (("L", l), ("R", r)):
        aa.rot(p["thigh." + side], th)
        aa.rot(p["shin." + side], sh)
        aa.rot(p["foot." + side], ft)


def ready(p):
    legs(p, (-12, 14, -2), (10, 8, -18))
    aa.rot(p["chest"], 6, 0, 10)
    aa.rot(p["upper_arm.R"], -25, 10, 0)
    aa.rot(p["forearm.R"], -45)
    aa.rot(p["upper_arm.L"], -10, -12, 0)
    aa.rot(p["forearm.L"], -40)


def wind_up(p):
    legs(p, (-16, 18, -2), (14, 10, -24))
    aa.rot(p["hips"], 0, 0, 15)
    aa.rot(p["chest"], -4, 6, 30)
    aa.rot(p["head"], 0, 0, -20)
    aa.rot(p["upper_arm.R"], -160, -30, 0)
    aa.rot(p["forearm.R"], -70)
    aa.rot(p["upper_arm.L"], -45, -20, 0)
    aa.rot(p["forearm.L"], -50)


def strike(p):
    legs(p, (-30, 30, 0), (20, 12, -30))
    aa.rot(p["hips"], 0, 0, -15)
    aa.rot(p["chest"], 18, -6, -30)
    aa.rot(p["head"], -10, 0, 20)
    aa.rot(p["upper_arm.R"], -70, 30, 0)
    aa.rot(p["forearm.R"], -5)
    aa.rot(p["upper_arm.L"], 10, -25, 0)
    aa.rot(p["forearm.L"], -30)


def whirl(p):
    legs(p, (-20, 25, 0), (20, 25, -20))
    aa.rot(p["hips"], 0, 0, 70)
    aa.rot(p["chest"], 10, 0, 30)
    aa.rot(p["upper_arm.R"], -10, 85, 0)
    aa.rot(p["forearm.R"], -10)
    aa.rot(p["upper_arm.L"], -10, -70, 0)
    aa.rot(p["forearm.L"], -20)


def cry(p):
    legs(p, (-14, 6, 0), (14, 6, -14))
    aa.rot(p["chest"], -10, 0, 0)
    aa.rot(p["head"], -22, 0, 0)
    aa.rot(p["upper_arm.R"], -175, 10, 0)
    aa.rot(p["forearm.R"], -15)
    aa.rot(p["upper_arm.L"], -20, -35, 0)
    aa.rot(p["forearm.L"], -95)


def dash(p):
    legs(p, (-60, 55, 5), (40, 10, -30))
    p["hips"].location = (0, 0, -0.03 * S)
    aa.rot(p["chest"], 28, 0, -10)
    aa.rot(p["head"], -20, 0, 0)
    aa.rot(p["upper_arm.R"], -95, 5, 0)
    aa.rot(p["forearm.R"], 0)
    aa.rot(p["upper_arm.L"], 40, -15, 0)
    aa.rot(p["forearm.L"], -20)


def breath(p):
    legs(p, (-6, 10, 0), (6, 10, -6))
    aa.rot(p["chest"], 8, 0, 0)
    aa.rot(p["head"], 12, 0, 0)
    aa.rot(p["upper_arm.R"], 8, 8, 0)
    aa.rot(p["forearm.R"], -15)
    aa.rot(p["upper_arm.L"], -35, -10, 0)
    aa.rot(p["forearm.L"], -120, 0, 20)


POSES = [("1-Ready", ready), ("2-WindUp", wind_up), ("3-Strike", strike), ("4-Whirl", whirl),
         ("5-Cry", cry), ("6-Dash", dash), ("7-Breath", breath)]


def main():
    out = sys.argv[sys.argv.index("--") + 1:][0]
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
        aa.rest(arm)
        pose(arm.pose.bones)
        bpy.context.view_layer.update()
        for view, offset in (("", Vector((-1.25, -1.55, 0.55))), ("-side", Vector((1.9, -0.35, 0.4)))):
            cam.location = at + offset
            cam.rotation_euler = (at - cam.location).to_track_quat('-Z', 'Y').to_euler()
            sc.render.filepath = os.path.join(out, f"ardan_{name}{view}.png")
            bpy.ops.render.render(write_still=True)
        print("RENDERED", name)


main()
