"""Joins a folder of numbered frames (the Unity opening film, CampaignPhotos.Film) into an mp4.

    blender --background --python encode_frames.py -- <frames folder> <out.mp4> [fps]
"""
import os
import sys

import bpy


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    src, out = argv[0], argv[1]
    fps = int(argv[2]) if len(argv) > 2 else 30
    files = sorted(f for f in os.listdir(src) if f.lower().endswith((".jpg", ".png")))
    sc = bpy.context.scene
    sc.render.fps = fps
    sc.render.resolution_x, sc.render.resolution_y = 1280, 720
    sc.render.resolution_percentage = 100
    ed = sc.sequence_editor_create()
    strips = ed.strips if hasattr(ed, "strips") else ed.sequences
    st = strips.new_image("film", os.path.join(src, files[0]), channel=1, frame_start=1)
    for f in files[1:]:
        st.elements.append(f)
    sc.frame_start, sc.frame_end = 1, len(files)
    s = sc.render.image_settings
    try:
        s.media_type = 'VIDEO'
    except (AttributeError, TypeError):
        pass
    s.file_format = 'FFMPEG'
    sc.render.ffmpeg.format = 'MPEG4'
    sc.render.ffmpeg.codec = 'H264'
    sc.render.ffmpeg.constant_rate_factor = 'HIGH'
    sc.render.filepath = out
    sc.render.use_sequencer = True
    bpy.ops.render.render(animation=True)
    print("ENCODED", out, len(files), "frames")


main()
