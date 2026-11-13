"""Concepts for the sandbox Лечебница (the user, 2026-11-13): beds instead of a radius. A tapped
healer shows six bed slots at the bottom (locked ones read «Ур. N») and the «!» seventh; an empty
slot is an empty bed, a taken one the patient's portrait, one being treated the portrait in a 60 s
ring. Over the world: the sickness mark over a sick villager walking to it, the two notices, the
no-worker mark. Real HUD sprites, 1920x1080 reference sizes.

    python Tools/ui/healer_concepts.py  ->  Design/ui/Healer_Concepts.png
"""
import os
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

import smelter_bar_concepts as sb

ROOT = sb.ROOT
OUT = os.path.join(ROOT, "Design/ui/Healer_Concepts.png")
W, H = sb.W, 560
BTN, GAP = 128, 22
CREAM = (255, 246, 225)
INK = (70, 42, 20)

# level 3 healer: 4 beds, 2 healers; 2 under treatment, 1 waiting in bed, 1 empty, 2 locked
SLOTS = [("PortraitVillagerPeasant2", .70), ("PortraitVillagerWoman1", .25),
         ("PortraitVillagerElder1", None), (None, None), ("lock", 4), ("lock", 5)]


def ground(building="HealerHouse_L3"):
    im = Image.new("RGBA", (W, H))
    d = ImageDraw.Draw(im)
    for y in range(H):
        k = y / H
        d.line([(0, y), (W, y)], fill=(int(96 - 30 * k), int(132 - 40 * k), int(70 - 22 * k), 255))
    for i in range(160):
        x, y = (i * 397) % W, (i * 211) % H
        d.ellipse([x, y, x + 40, y + 18], fill=(70, 104, 52, 255))
    return im.filter(ImageFilter.GaussianBlur(6))


def held(c, cx, top, size=280, name="HealerHouse_L3"):
    b = sb.sprite(name, (size, size))
    glow = Image.new("RGBA", (size + 40, size + 40))
    glow.paste((255, 214, 90, 150), (20, 20), b.split()[3])
    glow = glow.filter(ImageFilter.GaussianBlur(9))
    c.alpha_composite(glow, (cx - size // 2 - 20, top - 20))
    c.alpha_composite(b, (cx - size // 2, top))


def bed(size):
    """An empty bed seen from above-front: wooden frame, white pillow, folded blanket."""
    im = Image.new("RGBA", (size, size))
    d = ImageDraw.Draw(im)
    s = size / 100
    d.rounded_rectangle([14 * s, 20 * s, 86 * s, 84 * s], 8 * s, fill=(122, 76, 40, 255), outline=(70, 42, 20, 255), width=int(3 * s))
    d.rounded_rectangle([20 * s, 26 * s, 80 * s, 80 * s], 5 * s, fill=(236, 226, 204, 255))
    d.rounded_rectangle([26 * s, 30 * s, 74 * s, 44 * s], 6 * s, fill=(255, 255, 255, 255), outline=(190, 176, 150, 255), width=int(2 * s))
    d.rectangle([20 * s, 54 * s, 80 * s, 80 * s], fill=(176, 64, 52, 255))
    d.line([(20 * s, 54 * s), (80 * s, 54 * s)], fill=(236, 200, 160, 255), width=int(4 * s))
    d.rectangle([12 * s, 84 * s, 20 * s, 92 * s], fill=(70, 42, 20, 255))
    d.rectangle([80 * s, 84 * s, 88 * s, 92 * s], fill=(70, 42, 20, 255))
    return im


def lock(size):
    im = Image.new("RGBA", (size, size))
    d = ImageDraw.Draw(im)
    s = size / 100
    d.arc([30 * s, 14 * s, 70 * s, 58 * s], 180, 360, fill=(230, 214, 170, 255), width=int(9 * s))
    d.line([(34.5 * s, 36 * s), (34.5 * s, 46 * s)], fill=(230, 214, 170, 255), width=int(9 * s))
    d.line([(65.5 * s, 36 * s), (65.5 * s, 46 * s)], fill=(230, 214, 170, 255), width=int(9 * s))
    d.rounded_rectangle([22 * s, 44 * s, 78 * s, 86 * s], 8 * s, fill=(214, 170, 70, 255), outline=(110, 74, 30, 255), width=int(3 * s))
    d.ellipse([45 * s, 56 * s, 55 * s, 66 * s], fill=(110, 74, 30, 255))
    d.rectangle([48 * s, 62 * s, 52 * s, 76 * s], fill=(110, 74, 30, 255))
    return im


def portrait_disc(name, size):
    """A villager portrait cut to a disc (as the hire bar's people)."""
    p = sb.sprite(name, (size, size))
    m = Image.new("L", (size, size))
    ImageDraw.Draw(m).ellipse([0, 0, size - 1, size - 1], fill=255)
    out = Image.new("RGBA", (size, size))
    out.paste(p, (0, 0), m)
    return out


def sick_badge(size=44):
    """The sickness mark: a green disc, a white drop with a fever line."""
    t = Image.new("RGBA", (size, size))
    d = ImageDraw.Draw(t)
    d.ellipse([0, 0, size - 1, size - 1], fill=(92, 160, 60, 255), outline=(250, 236, 190, 255), width=max(2, size // 12))
    s = size / 44
    d.ellipse([14 * s, 18 * s, 30 * s, 34 * s], fill=(255, 255, 255, 255))
    d.polygon([(15 * s, 24 * s), (22 * s, 8 * s), (29 * s, 24 * s)], fill=(255, 255, 255, 255))
    return t


def slot(kind, arg):
    b = sb.button(BTN, main=False, dim=kind == "lock")
    inner = int(BTN * .74)
    ox, oy = (b.width - inner) // 2, (b.height - inner) // 2 - 2
    if kind == "lock":
        b.alpha_composite(lock(int(BTN * .5)), ((b.width - int(BTN * .5)) // 2, 12))
        d = ImageDraw.Draw(b)
        t = f"Ур. {arg}"
        f = sb.font(26)
        d.text(((b.width - d.textlength(t, font=f)) / 2, b.height - 50), t, font=f, fill=CREAM)
        return b
    if kind is None:
        b.alpha_composite(bed(inner), (ox, oy))
        return b
    b.alpha_composite(portrait_disc(kind, inner), (ox, oy))
    if arg is not None:  # under treatment: the 60 s ring
        r = Image.new("RGBA", b.size)
        rd = ImageDraw.Draw(r)
        box = [ox - 6, oy - 6, ox + inner + 6, oy + inner + 6]
        rd.arc(box, 0, 360, fill=(40, 28, 18, 220), width=11)
        rd.arc(box, -90, -90 + 360 * arg, fill=(110, 210, 90, 255), width=11)
        b.alpha_composite(r)
    else:  # lying in bed, waiting for a free healer: a small bed badge
        b.alpha_composite(bed(46), (b.width - 50, b.height - 52))
    return b


def row_x(count):
    total = count * BTN + (count - 1) * GAP
    return [W / 2 - total / 2 + BTN / 2 + i * (BTN + GAP) for i in range(count)]


def label(c, x, y, text, size=22):
    d = ImageDraw.Draw(c)
    f = sb.font(size, "Bold")
    w = d.textlength(text, font=f)
    p = sb.plaque(int(w + 80), size + 22)
    c.alpha_composite(p, (int(x - p.width / 2), int(y)))
    d.text((x - w / 2, y + 9), text, font=f, fill=INK)


def panel_bar():
    c = ground()
    held(c, W // 2, 30)
    xs = row_x(7)
    cy = H - 60 - 80
    for i, (kind, arg) in enumerate(SLOTS):
        sb.put_centre(c, slot("lock" if kind == "lock" else kind, arg), xs[i], cy)
    sb.mark_button(c, xs[6], cy)
    names = ["42 с", "15 с", "ждёт", "пусто", "закрыто", "закрыто", "окно"]
    for x, n in zip(xs, names):
        label(c, x, cy + 70, n, 20)
    caption(c, "1", "Лечебница ур. 3: 4 койки, 2 лекаря")
    return c


def villager(c, cx, foot, scale=1.0):
    """A plain standing villager silhouette (body, head), a stand-in for the 3D figure."""
    d = ImageDraw.Draw(c)
    s = scale
    d.rounded_rectangle([cx - 22 * s, foot - 90 * s, cx + 22 * s, foot - 20 * s], 14 * s, fill=(150, 120, 90, 255), outline=(70, 42, 20, 255), width=3)
    d.rectangle([cx - 16 * s, foot - 22 * s, cx - 4 * s, foot], fill=(90, 70, 50, 255))
    d.rectangle([cx + 4 * s, foot - 22 * s, cx + 16 * s, foot], fill=(90, 70, 50, 255))
    d.ellipse([cx - 18 * s, foot - 128 * s, cx + 18 * s, foot - 92 * s], fill=(200, 214, 150, 255), outline=(70, 42, 20, 255), width=3)


def caption(canvas, letter, text):
    d = ImageDraw.Draw(canvas)
    f = sb.font(28, "Bold")
    p = sb.plaque(int(110 + d.textlength(text, font=f)), 64)
    canvas.alpha_composite(p, (30, 24))
    d.text((56, 36), letter, font=sb.font(36), fill=(150, 40, 30))
    d.text((92, 40), text, font=f, fill=INK)


def notice(c, x, y, text, icon):
    d = ImageDraw.Draw(c)
    f = sb.font(26, "Bold")
    w = d.textlength(text, font=f)
    p = sb.plaque(int(w + 110), 66)
    c.alpha_composite(p, (int(x), int(y)))
    c.alpha_composite(icon, (int(x + 18), int(y + 9)))
    d.text((x + 76, y + 18), text, font=f, fill=INK)


def panel_world():
    c = ground()
    # left: a sick villager walking to the healer, the mark over his head, a 30 s ring round it
    villager(c, 330, 380, 1.3)
    mark = sb.sprite("NoWorker", (96, 96))
    disc = Image.new("RGBA", mark.size)
    dd = ImageDraw.Draw(disc)
    dd.ellipse([4, 4, 91, 91], fill=(250, 240, 214, 255), outline=(110, 74, 30, 255), width=5)
    disc.alpha_composite(sick_badge(64), (16, 16))
    ring = ImageDraw.Draw(disc)
    ring.arc([0, 0, 95, 95], -90, -90 + 360 * .6, fill=(214, 34, 40, 255), width=7)
    sb.put_centre(c, disc, 330, 160)
    for k in range(4):  # footsteps towards the healer
        ImageDraw.Draw(c).ellipse([420 + k * 70, 400 - k * 18, 444 + k * 70, 412 - k * 18], fill=(60, 90, 44, 255))
    label(c, 330, 420, "больной идёт в лечебницу", 22)
    label(c, 330, 466, "кольцо = 30 с до смерти без койки", 20)

    # middle: the healer with no healer assigned -- the worker mark over it
    held(c, 960, 120, 240, "HealerHouse")
    sb.put_centre(c, sb.sprite("NoWorker", (96, 96)), 960, 92)
    label(c, 960, 380, "лекаря нет: лечебница не принимает", 22)

    # right: the two notices
    notice(c, 1250, 120, "На острове не хватает мест в лечебнице", sick_badge(48))
    notice(c, 1250, 220, "Назначьте рабочего лечебницы", sb.sprite("NoWorker", (48, 48)))
    caption(c, "2", "В мире: значки и уведомления")
    return c


def panel_card():
    c = Image.new("RGBA", (W, 420), (30, 22, 16, 255))
    card = sb.nine("card", (980, 360), (34, 34, 34, 34))
    c.alpha_composite(card, (W // 2 - 490, 40))
    d = ImageDraw.Draw(c)
    t = "Лечебница · ур. 3"
    d.text((W // 2 - d.textlength(t, font=sb.font(40)) / 2, 62), t, font=sb.font(40), fill=INK)
    rows = [(bed(56), "Койки", "3 / 4"), (portrait_disc("PortraitVillagerPeasant1", 56), "Лекари", "2 / 2"),
            (sick_badge(56), "Лечение", "60 с"), (lock(56), "Ждёт места", "30 с")]
    x0 = W // 2 - 440
    for i, (icon, name, value) in enumerate(rows):
        x = x0 + (i % 2) * 450
        y = 140 + (i // 2) * 110
        d.rounded_rectangle([x, y, x + 430, y + 90], 16, fill=(52, 36, 24, 235))
        c.alpha_composite(icon, (x + 16, y + 17))
        d.text((x + 88, y + 26), name, font=sb.font(32, "Bold"), fill=CREAM)
        d.text((x + 410 - d.textlength(value, font=sb.font(34)), y + 24), value, font=sb.font(34), fill=(255, 214, 90))
    caption(c, "3", "Окно здания (кнопка «!»)")
    return c


def main():
    panels = [panel_bar(), panel_world(), panel_card()]
    total = sum(p.height for p in panels) + 12 * (len(panels) - 1)
    sheet = Image.new("RGBA", (W, total), (30, 22, 16, 255))
    y = 0
    for p in panels:
        sheet.alpha_composite(p, (0, y))
        y += p.height + 12
    sheet.convert("RGB").save(OUT, optimize=True)
    print(OUT)


if __name__ == "__main__":
    main()
