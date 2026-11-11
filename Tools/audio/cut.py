"""Cuts a piece of a sound file into OGG (or WAV), with fades, through Blender's sequencer mixdown
(Blender carries its own ffmpeg; this machine has none). Used for the campaign film's audio.

    blender --background --factory-startup --python cut.py -- <jobs.json>

jobs.json: [{"src": path, "out": path.ogg|.wav, "start": s, "length": s (0 = to the end),
             "fadeIn": s, "fadeOut": s, "volume": 1.0, "mono": false}, ...]
"""
import json
import sys

import bpy


def cut(job):
    sc = bpy.context.scene
    fps = 100
    sc.render.fps, sc.render.fps_base = fps, 1.0
    ed = sc.sequence_editor_create()
    strips = ed.strips if hasattr(ed, "strips") else ed.sequences
    for s in list(strips):
        strips.remove(s)
    st = strips.new_sound("s", job["src"], 1, 1)
    start = job.get("start", 0.0)
    total = st.frame_final_duration / fps
    length = job.get("length", 0.0) or (total - start)
    length = min(length, total - start)
    st.frame_offset_start = int(round(start * fps))
    st.frame_start = 1 - st.frame_offset_start
    st.frame_offset_end = max(0, int(round((total - start - length) * fps)))
    vol = job.get("volume", 1.0)
    first, last = 1, 1 + int(round(length * fps))
    fi, fo = job.get("fadeIn", 0.0), job.get("fadeOut", 0.0)
    st.volume = 0.0 if fi > 0 else vol
    st.keyframe_insert("volume", frame=first)
    if fi > 0:
        st.volume = vol
        st.keyframe_insert("volume", frame=first + int(fi * fps))
    if fo > 0:
        st.volume = vol
        st.keyframe_insert("volume", frame=last - int(fo * fps))
        st.volume = 0.0
        st.keyframe_insert("volume", frame=last)
    sc.frame_start, sc.frame_end = first, last
    out = job["out"]
    ogg = out.lower().endswith(".ogg")
    bpy.ops.sound.mixdown(filepath=out, check_existing=False,
                          container='OGG' if ogg else 'WAV', codec='VORBIS' if ogg else 'PCM',
                          format='F32' if ogg else 'S16', bitrate=128 if ogg else 192,
                          mixrate=44100, accuracy=1024)
    print("CUT", out, round(length, 2), "s")


for j in json.load(open(sys.argv[sys.argv.index("--") + 1], encoding="utf-8")):
    cut(j)
