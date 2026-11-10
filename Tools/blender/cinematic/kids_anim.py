"""The children's opening-cutscene animations (2026-11-10, after Ардан's were approved):
  * KidsPlay (shot 3): the boy plays «don't touch the ground» -- hops from ruin stone to ruin
    stone, arms out for balance; the girl cheers him on from the ground, bouncing, doll in hand.
  * KidsFlee (shot 6): the runes light up; both freeze and stare at the stones, the boy jumps down,
    and the two of them run to the peasant woman at the edge of the square.
Played out on the real ruin stones (portal.py) so feet land on their tops. Every figure of
figure.py shares Ардан's joints, so `rig_figure` is his rig at the figure's scale. Never saves.

    blender --background <Buildings-Size1-Models.blend> --python kids_anim.py -- render <out dir> [play|flee|all]
    ARDAN_SHEET=1,30,60 ... -- frames as PNGs instead of the videos
"""
import itertools
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import bpy  # noqa: E402
import ardan_anim as aa  # noqa: E402
import ardan_anim2 as a2  # noqa: E402
import portal  # noqa: E402
import townsfolk  # noqa: E402
from mathutils import Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
LEG = (0.102, 0.066)          # thigh and shin lengths before scale
STAND = sum(LEG)
PLATFORM_Z = 0.085


def rig_figure(mesh, scale, bend=None):
    """Ардан's bones at this figure's scale; `bend(Vector) -> Vector` moves the joints as the
    figure's own post-build bend did (the elder's stoop), so his limbs turn where they are."""
    data = bpy.data.armatures.new(mesh.name + "Rig")
    arm = bpy.data.objects.new(mesh.name + "Rig", data)
    mesh.users_collection[0].objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')
    for name, head, tail, parent in aa.BONES:
        b = data.edit_bones.new(name)
        bh, bt = Vector(head), Vector(tail)
        if bend:
            bh, bt = bend(bh), bend(bt)
        b.head = bh * scale
        b.tail = bt * scale
        if parent:
            b.parent = data.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT')
    arm.location = mesh.location
    mesh.location = (0, 0, 0)
    mesh.parent = arm
    mod = mesh.modifiers.new("Rig", 'ARMATURE')
    mod.object = arm
    return arm


def apply(arm, pose, scale):
    p = arm.pose.bones
    for k, v in pose.items():
        if k == "hips_loc":
            p["hips"].location = Vector(v) * scale
        else:
            aa.rot(p[k], *v)


# ---------------------------------------------------------------- poses

def crouch(c, arms_back=0.0, arms_out=0.0):
    """Knees bent by c (0..1), feet flat, the hips lowered exactly as far as the legs fold."""
    q = a2.blank()
    th, kn = -60 * c, 100 * c
    for s in ("L", "R"):
        q["thigh." + s] = [th, 0, 0]
        q["shin." + s] = [kn, 0, 0]
        q["foot." + s] = [-(th + kn), 0, 0]
    drop = STAND - (LEG[0] * math.cos(math.radians(th)) + LEG[1] * math.cos(math.radians(th + kn)))
    q["hips_loc"] = [0, -drop, 0]
    q["hips"] = [25 * c, 0, 0]
    q["chest"] = [5 * c, 0, 0]
    q["head"] = [-20 * c, 0, 0]
    for s, k in (("L", 1), ("R", -1)):
        q["upper_arm." + s] = [40 * arms_back - 20 * arms_out * 0, k * -75 * arms_out, 0]
        q["forearm." + s] = [-15 - 10 * arms_back, 0, 0]
    return q


def tuck():
    """In the air: knees up, arms flung forward and out."""
    q = a2.blank()
    for s, k in (("L", 1), ("R", -1)):
        q["thigh." + s] = [-55, 0, 0]
        q["shin." + s] = [85, 0, 0]
        q["foot." + s] = [-20, 0, 0]
        q["upper_arm." + s] = [-70, k * -40, 0]
        q["forearm." + s] = [-30, 0, 0]
    q["hips"] = [10, 0, 0]
    q["head"] = [-10, 0, 0]
    return q


def balance(t):
    """Standing on a stone, arms out like a tightrope walker, wobbling."""
    q = crouch(0.15, arms_out=1.0)
    w = math.sin(TAU * t * 1.3)
    q["chest"][1] += 6 * w
    q["hips"][1] -= 3 * w
    q["head"][1] -= 4 * w
    for s in ("L", "R"):
        q["upper_arm." + s][1] += 12 * w
    return q


def cheer(t):
    """Bouncing on both feet, the free right arm waving over her head, the doll hugged in the left."""
    ph = (t / 0.5) % 1.0
    c = 0.35 * (0.5 + 0.5 * math.cos(TAU * ph))
    q = crouch(c)
    q["upper_arm.R"] = [-160, 15 + 15 * math.sin(TAU * t * 2), 0]
    q["forearm.R"] = [-20, 0, 0]
    q["upper_arm.L"] = [-25, -20, -35]
    q["forearm.L"] = [-80, 0, -30]
    q["head"] = [-12, 0, 0]
    return q


def stare(t):
    """Frozen, staring: drawn back, arms pulled in."""
    q = a2.blank()
    q["chest"] = [-8, 0, 0]
    q["head"] = [-14, 0, 0]
    for s, k in (("L", 1), ("R", -1)):
        q["upper_arm." + s] = [-15, k * 5, 0]
        q["forearm." + s] = [-70, 0, k * 10]
    return q


# ---------------------------------------------------------------- a timeline of moves

class Track:
    """A figure's moves in order: ('pose', dur, at, fn(t), face) holds a spot; ('jump', dur, p0, p1);
    ('run', dur, p0, p1). Bakes the root (ground point and heading) and the pose, crossfading
    each move into the next."""

    def __init__(self, scale, run_speed):
        self.scale = scale
        self.speed = run_speed
        self.moves = []
        self.apply = apply

    def add(self, kind, dur, *args):
        self.moves.append((kind, dur) + args)
        return self

    def total(self):
        return sum(m[1] for m in self.moves)

    def _at(self, t):
        t0 = 0.0
        for i, m in enumerate(self.moves):
            if t < t0 + m[1] or i == len(self.moves) - 1:
                return i, min(t - t0, m[1]), t0
            t0 += m[1]

    @staticmethod
    def heading(d):
        return math.atan2(d.x, -d.y)

    def sample(self, i, u):
        """(root point, heading or None, pose) of move i at u seconds into it."""
        m = self.moves[i]
        kind, dur = m[0], m[1]
        if kind == 'pose':
            at, fn, face = m[2], m[3], m[4]
            return Vector(at), (self.heading(Vector(face) - Vector(at)) if face else None), fn(u)
        p0, p1 = Vector(m[2]), Vector(m[3])
        head = self.heading(p1 - p0)
        if kind == 'jump':
            x = u / dur
            if x < 0.25:                                     # gathering
                return p0, head, crouch(a2.ease(x / 0.25) * 0.7, arms_back=a2.ease(x / 0.25))
            if x < 0.75:                                     # flying
                f = (x - 0.25) / 0.5
                pos = p0.lerp(p1, f)
                pos.z = p0.z + (p1.z - p0.z) * f + 4 * 0.09 * self.scale * f * (1 - f)
                pose = a2.mix(crouch(0.7, arms_back=1.0), tuck(), a2.ease(min(1.0, f * 3)))
                return pos, head, a2.mix(pose, crouch(0.6, arms_out=0.8), a2.ease((f - 0.7) / 0.3))
            k = (x - 0.75) / 0.25                            # landing, soaking it up
            return p1, head, crouch(0.6 * (1 - a2.ease(k)) + 0.15, arms_out=0.8)
        # run: the run cycle on the distance covered, easing up to speed
        ramp = 0.35
        d = self.speed * (u * u / (2 * ramp) if u < ramp else ramp / 2 + (u - ramp))
        d = min(d, (p1 - p0).length)
        pos = p0 + (p1 - p0).normalized() * d
        pose = a2.run((d / (self.speed * a2.RUN_CYCLE)) % 1.0)
        return pos, head, a2.mix(stare(0), pose, a2.ease(u / ramp))

    def bake(self, arm, name):
        arm.animation_data_create()
        act = bpy.data.actions.new(name)
        act.use_fake_user = True
        arm.animation_data.action = act
        frames = int(self.total() * FPS) + 1
        heading = None
        for f in range(frames):
            t = f / FPS
            i, u, _ = self._at(t)
            pos, head, pose = self.sample(i, u)
            if i > 0 and u < 0.12:                           # crossfade from the move before
                pm = self.moves[i - 1]
                _, _, prev = self.sample(i - 1, pm[1])
                pose = a2.mix(prev, pose, a2.ease(u / 0.12))
            if head is not None:
                if heading is None:
                    heading = head
                diff = (head - heading + math.pi) % TAU - math.pi
                heading += diff * 0.25                       # turns, never snaps
            arm.location = pos                               # placed first: an aimed limb needs the root
            arm.rotation_euler = (0, 0, heading or 0.0)
            aa.rest(arm)
            self.apply(arm, pose, self.scale)
            for pb in arm.pose.bones:
                pb.keyframe_insert("rotation_quaternion", frame=f + 1, group=pb.name)
                pb.keyframe_insert("location", frame=f + 1, group=pb.name)
            arm.location = pos
            arm.rotation_euler = (0, 0, heading or 0.0)
            arm.keyframe_insert("location", frame=f + 1)
            arm.keyframe_insert("rotation_euler", frame=f + 1)
        return act


# ---------------------------------------------------------------- the stones' tops

def stone_tops(stones):
    """(name, top point, flat radius) per ruin stone: where a child's feet can land."""
    out = []
    for s in stones:
        vs = [s.matrix_world @ v.co for v in s.data.vertices]
        top = max(v.z for v in vs)
        flat = [v for v in vs if v.z > top - 0.012]
        c = sum(flat, Vector()) / len(flat)
        r = min(max((Vector((v.x, v.y, 0)) - Vector((c.x, c.y, 0))).length for v in flat), 0.2)
        out.append((s.name, Vector((c.x, c.y, top)), r))
    return out


def stepping_stones(tops, n=3):
    """A chain of n flat stone tops on the near (-y) side, each hop 0.25-0.6 long."""
    flat = [t for t in tops if t[2] > 0.06 and t[1].z < 0.36]         # about a child's chest above the platform: a scramble, not a wall
    best = None
    for chain in itertools.permutations(flat, n):
        hops = [(chain[i + 1][1] - chain[i][1]).length for i in range(n - 1)]
        if not all(0.25 < h < 0.7 for h in hops):
            continue
        score = sum(c[1].y for c in chain) + 0.3 * sum(abs(h - 0.4) for h in hops)
        if best is None or score < best[0]:
            best = (score, chain)
    return [c[1] for c in best[1]]


def ground(p):
    return Vector((p.x, p.y, PLATFORM_Z))


# ---------------------------------------------------------------- render

def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = argv[1]
    what = argv[2] if len(argv) > 2 else "all"
    os.makedirs(out, exist_ok=True)
    plat, stones, sw = portal.make()
    tops = stone_tops(stones)
    print("TOPS", [(n[-8:], tuple(round(x, 2) for x in p), round(r, 2)) for n, p, r in tops])
    a, b, c = stepping_stones(tops)
    print("STONES", [tuple(round(x, 2) for x in p) for p in (a, b, c)])
    centre = Vector((0, 0, PLATFORM_Z))
    woman_at = Vector((0.75, -1.3, PLATFORM_Z))
    woman = townsfolk.make("CinePeasantWoman", tuple(woman_at))
    woman.rotation_euler = (0, 0, math.pi * 0.85)
    boy_s, girl_s = townsfolk.FIGURES["CineBoy"][1], townsfolk.FIGURES["CineGirl"][1]
    boy = rig_figure(townsfolk.make("CineBoy", tuple(a)), boy_s)
    girl = rig_figure(townsfolk.make("CineGirl", (0, 0, 0)), girl_s)
    keep = {plat.name, woman.name, boy.name, girl.name} | {s.name for s in stones} | {o.name for o in boy.children} | {o.name for o in girl.children}
    cam = aa.stage(keep)
    for o in bpy.context.scene.objects:
        if o.name.startswith("ArdanPath") or o.name.startswith("ArdanTuft"):
            o.hide_render = True
    sc = bpy.context.scene
    sp = 0.95 * boy_s * 1.1
    # the girl stands inside the ring, between the stones and the centre, facing the boy
    mid = (a + b + c) / 3
    inward = (Vector((0, 0, 0)) - Vector((mid.x, mid.y, 0))).normalized()
    girl_at = ground(mid + inward * 0.4)
    clips = []
    if what in ("play", "all"):
        tb = Track(boy_s, sp)
        tb.add('pose', 0.8, a, balance, b).add('jump', 0.85, a, b).add('pose', 0.9, b, balance, c)
        tb.add('jump', 0.85, b, c).add('pose', 0.5, c, lambda t: balance(t + 0.4), a)
        tb.add('jump', 0.85, c, ground(c + (girl_at - c).normalized() * 0.3)).add('pose', 1.0, ground(c + (girl_at - c).normalized() * 0.3), lambda t: cheer(t * 0.8), girl_at)
        tg = Track(girl_s, sp)
        tg.add('pose', tb.total(), girl_at, cheer, (a + b) / 2)
        clips.append(("kids_play", tb, tg, "CineBoy_Play", "CineGirl_Play"))
    if what in ("flee", "all"):
        away = woman_at + Vector((0.12, 0.25, 0))
        tb = Track(boy_s, sp)
        tb.add('pose', 0.6, c, balance, b).add('pose', 0.7, c, stare, centre).add('jump', 0.8, c, ground(c + (away - c).normalized() * 0.3))
        tb.add('run', 1.6, ground(c + (away - c).normalized() * 0.3), away)
        tg = Track(girl_s, sp)
        tg.add('pose', 0.6, girl_at, cheer, c).add('pose', 0.9, girl_at, stare, centre)
        tg.add('run', 2.2, girl_at, away + Vector((-0.15, 0.05, 0)))
        clips.append(("kids_flee", tb, tg, "CineBoy_Flee", "CineGirl_Flee"))
    for name, tb, tg, nb, ng in clips:
        tb.bake(boy, nb)
        tg.bake(girl, ng)
        seconds = max(tb.total(), tg.total())
        frames = int(seconds * FPS) + 1
        sc.frame_start, sc.frame_end = 1, frames
        # from inside the ring, past the girl, onto the stones the boy hops along
        at = Vector((mid.x, mid.y, 0.25)) + inward * 0.2
        side = Vector((-inward.y, inward.x, 0))
        cam.location = at + inward * 1.7 + side * 1.5 + Vector((0, 0, 0.6))
        cam.data.lens = 38
        cam.rotation_euler = (at - cam.location).to_track_quat('-Z', 'Y').to_euler()
        sheet = os.environ.get("ARDAN_SHEET")
        if sheet:
            sc.render.image_settings.file_format = 'PNG'
            for f in [int(x) for x in sheet.split(",")]:
                sc.frame_set(min(f, frames))
                sc.render.filepath = os.path.join(out, "%s_%03d.png" % (name, f))
                bpy.ops.render.render(write_still=True)
        else:
            st = sc.render.image_settings
            try:
                st.media_type = 'VIDEO'
            except (AttributeError, TypeError):
                pass
            st.file_format = 'FFMPEG'
            sc.render.ffmpeg.format = 'MPEG4'
            sc.render.ffmpeg.codec = 'H264'
            sc.render.ffmpeg.constant_rate_factor = 'HIGH'
            sc.render.filepath = os.path.join(out, name + ".mp4")
            bpy.ops.render.render(animation=True)
        print("RENDERED", name)


if __name__ == "__main__":
    main()
