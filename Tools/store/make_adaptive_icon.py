"""The Android ADAPTIVE icon layer from Assets/_Project/UI/AppIcon.png (the user, 2026-11-18: the icon sat as a
small square inside the launcher's white circle -- it must fill the circle).

An adaptive icon is a 108dp layer of which the launcher shows only the middle 72dp (a circle on most phones).
The art is laid at 76 % of the layer and its edge pixels are stretched out to the border, so the visible circle
is covered edge to edge and still shows ~88 % of the picture.

    python Tools/store/make_adaptive_icon.py
"""
import os

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, "Assets", "_Project", "UI", "AppIcon.png")
DST = os.path.join(ROOT, "Assets", "_Project", "UI", "AppIconAdaptive.png")
SIZE = 432
ART = 0.76

src = Image.open(SRC).convert("RGB")
inner = int(SIZE * ART)
art = src.resize((inner, inner), Image.LANCZOS)
pad = (SIZE - inner) // 2
out = Image.new("RGB", (SIZE, SIZE))
# edge-replicate: the border bands are the art's outer rows/columns stretched outwards
out.paste(art.crop((0, 0, inner, 1)).resize((inner, pad)), (pad, 0))
out.paste(art.crop((0, inner - 1, inner, inner)).resize((inner, SIZE - pad - inner)), (pad, pad + inner))
out.paste(art.crop((0, 0, 1, inner)).resize((pad, inner)), (0, pad))
out.paste(art.crop((inner - 1, 0, inner, inner)).resize((SIZE - pad - inner, inner)), (pad + inner, pad))
for (x, y), px in (((0, 0), (0, 0)), ((pad + inner, 0), (inner - 1, 0)), ((0, pad + inner), (0, inner - 1)),
                   ((pad + inner, pad + inner), (inner - 1, inner - 1))):
    out.paste(Image.new("RGB", (SIZE - inner - pad if x else pad, SIZE - inner - pad if y else pad), art.getpixel(px)), (x, y))
out.paste(art, (pad, pad))
out.save(DST)
print("wrote", DST)
