"""Joins a folder of numbered frames (the Unity opening film, CampaignPhotos.Film) into an mp4.

    blender --background --python encode_frames.py -- <frames folder> <out.mp4> [fps] [<OpeningScript.json> <audio folder>]

With the script and the audio folder, the "sounds" cue sheet the game plays (OpeningSound.cs) is laid
on the sequencer the same way -- start, volume, length, fades, loop -- and mixed into the mp4 (AAC).
"""
import json
import os
import sys

import bpy


def lay_sounds(strips, script, folder, fps, frames):
    cues = json.load(open(script, encoding="utf-8")).get("sounds", [])
    channel = 2
    for i, c in enumerate(cues):
        path = os.path.join(folder, c["clip"] + ".ogg")
        if not os.path.exists(path):
            print("MISSING SOUND", path)
            continue
        start = 1 + int(round(c["t"] * fps))
        probe = strips.new_sound("probe", path, channel, start)
        clip_len = probe.frame_final_duration
        strips.remove(probe)
        length = int(round(c.get("length", 0) * fps)) or (frames if c.get("loop") else clip_len)
        end = min(start + length, frames + 1)
        vol, fi, fo = c.get("volume", 1.0), c.get("fadeIn", 0.0), c.get("fadeOut", 0.0)
        at = start
        while at < end:
            st = strips.new_sound(f"{c['clip']}_{i}_{at}", path, channel, at)
            if st.frame_final_end > end:
                st.frame_final_end = end
            # the cue's envelope, keyed on every piece of a loop
            for f in range(at, st.frame_final_end + 1, 3):
                u = (f - start) / fps
                w = 1.0
                if fi > 0:
                    w *= min(1.0, u / fi)
                if fo > 0 and (c.get("length") or not c.get("loop")):
                    w *= max(0.0, min(1.0, (length / fps - u) / fo))
                st.volume = vol * w
                st.keyframe_insert("volume", frame=f)
            at = st.frame_final_end
            channel += 1
        print("SOUND", c["clip"], c["t"])


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
    if len(argv) > 4:
        lay_sounds(strips, argv[3], argv[4], fps, len(files))
    s = sc.render.image_settings
    try:
        s.media_type = 'VIDEO'
    except (AttributeError, TypeError):
        pass
    s.file_format = 'FFMPEG'
    sc.render.ffmpeg.format = 'MPEG4'
    sc.render.ffmpeg.codec = 'H264'
    sc.render.ffmpeg.constant_rate_factor = 'HIGH'
    if len(argv) > 4:
        sc.render.ffmpeg.audio_codec = 'AAC'
        sc.render.ffmpeg.audio_bitrate = 192
        sc.render.ffmpeg.audio_channels = 'STEREO'
        sc.render.ffmpeg.audio_mixrate = 44100
    sc.render.filepath = out
    sc.render.use_sequencer = True
    bpy.ops.render.render(animation=True)
    print("ENCODED", out, len(files), "frames")


main()
