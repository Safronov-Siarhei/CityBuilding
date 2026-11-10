"""Joins the opening cutscene's rendered shots (cutscene.py, PNG frames per shot) into one film in
Blender's sequencer and lays the subtitles over it, at the bottom as the user asked.

    blender --background --python cutscene_edit.py -- <frames dir> <film.mp4>
"""
import os
import sys

import bpy

ORDER = ["wide", "market", "square", "talk", "fright", "assembly", "swirl", "leap", "horde", "chaos", "bell", "run"]
# (shot, from s, to s, who, line) -- times within that shot
SUBTITLES = [
    ("talk", 0.5, 2.3, "Старик", "Эти камни старше деревни…"),
    ("run", 0.9, 2.4, "Старик", "Беги!"),
]
FPS = 30


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    src, out = argv[0], argv[1]
    sc = bpy.context.scene
    sc.render.fps = FPS
    sc.render.resolution_x, sc.render.resolution_y = 1280, 720
    ed = sc.sequence_editor_create()
    strips = ed.strips if hasattr(ed, "strips") else ed.sequences
    start = {}
    frame = 1
    for name in ORDER:
        folder = os.path.join(src, name)
        files = sorted(f for f in os.listdir(folder) if f.endswith(".png")) if os.path.isdir(folder) else []
        if not files:
            continue
        st = strips.new_image(name, os.path.join(folder, files[0]), channel=1, frame_start=frame)
        for f in files[1:]:
            st.elements.append(f)
        start[name] = frame
        frame += len(files)
    sc.frame_start, sc.frame_end = 1, frame - 1
    for shot, a, b, who, line in SUBTITLES:
        if shot not in start:
            continue
        f0 = start[shot] + int(a * FPS)
        t = strips.new_effect("sub_%s_%d" % (shot, f0), 'TEXT', channel=2, frame_start=f0,
                              length=int((b - a) * FPS)) if hasattr(strips, "new_effect") else None
        t.text = "%s: %s" % (who, line)
        t.font_size = 40
        t.use_shadow = True
        t.location = (0.5, 0.07)
        t.anchor_x, t.anchor_y = 'CENTER', 'BOTTOM'
        t.blend_type = 'ALPHA_OVER'
    still = os.environ.get("EDIT_STILL")             # check one frame (a subtitle) as a PNG
    if still:
        sc.render.use_sequencer = True
        sc.render.image_settings.file_format = 'PNG'
        sc.frame_set(int(still))
        sc.render.filepath = out
        bpy.ops.render.render(write_still=True)
        return
    st = sc.render.image_settings
    try:
        st.media_type = 'VIDEO'
    except (AttributeError, TypeError):
        pass
    st.file_format = 'FFMPEG'
    sc.render.ffmpeg.format = 'MPEG4'
    sc.render.ffmpeg.codec = 'H264'
    sc.render.ffmpeg.constant_rate_factor = 'HIGH'
    sc.render.filepath = out
    sc.render.use_sequencer = True
    bpy.ops.render.render(animation=True)
    print("FILM", out, frame - 1, "frames", {k: round((v - 1) / FPS, 1) for k, v in start.items()})


main()
