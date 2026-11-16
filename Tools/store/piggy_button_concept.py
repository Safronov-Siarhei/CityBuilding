"""
Piggy bank button (the user, 2026-11-16: the ROYAL piggy chosen). The button's area is a SQUARE, the piggy in its
centre the size of the settings button, crystals floating round it and bobbing (the button is never still), the
number at the pig's feet. Full: «Полна!» right by the pig's head, the number glowing, the pig shaking a little.
Also the five levels' looks. Concepts for approval:

    python Tools/store/piggy_button_concept.py <renders folder> <modes screen photo>

<renders folder> holds piggy_royal_<pink|bronze|silver|gold|amethyst>_alone.png and piggy_gem_pink.png from
Tools/blender/store/piggy_bank.py. Writes Design/store/Piggy_Button_Concept.webp and Piggy_Levels_Concept.png.
"""
import math
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
F = R + 'Assets/_Project/Resources/Fonts/'
CREAM = (255, 244, 220)
INK = (58, 30, 14)
GOLD = (255, 212, 90)
S = 200              # the button's square, in 1080-high reference pixels
PIG_W = 132          # the piggy as wide as the settings button
LEVELS = [('pink', 'Розовая керамика', 300), ('bronze', 'Бронза', 500), ('silver', 'Серебро', 1000),
          ('gold', 'Золото', 1500), ('amethyst', 'Аметист', 3000)]
# the floating crystals: (x, y in the square, height, bob phase, bob period s)
GEMS = [(26, 44, 34, 0.0, 1.9), (174, 36, 30, 1.3, 2.3), (20, 126, 26, 2.4, 2.1), (182, 120, 28, 0.7, 1.7)]   # four (the user 2026-11-16: a 5th by the number looked out of place); the top centre is kept for «Полна!»


def font(size, name='Rubik-ExtraBold.ttf'):
    return ImageFont.truetype(F + name, size)


def trim(im, w=None, h=None):
    im = im.crop(im.getbbox())
    k = (w / im.width) if w else (h / im.height)
    return im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)


def button(pig, gem, count, t, full=False, scale=1):
    """One frame of the button at time t (s), drawn at `scale` x for smooth edges."""
    k = scale
    im = Image.new('RGBA', (S * k, S * k), 0)
    # a soft golden shine behind the pig (brighter when full)
    gl = Image.new('RGBA', im.size, 0)
    ImageDraw.Draw(gl).ellipse((30 * k, 25 * k, 170 * k, 165 * k), fill=(255, 214, 110, 150 if full else 90))
    im.alpha_composite(gl.filter(ImageFilter.GaussianBlur(18 * k)))
    for x, y, h, ph, per in GEMS:
        # crystals drift SMOOTHLY and a little chaotically (the user, 2026-11-16): two waves of unrelated
        # periods on each of up-down, side-to-side and tilt, so no two crystals move alike and nothing repeats soon
        w = 2 * math.pi * t
        dy = 4.0 * math.sin(w / per + ph) + 1.8 * math.sin(w / (per * 0.57) + ph * 1.7)
        dx = 2.2 * math.sin(w / (per * 1.43) + ph * 2.3) + 0.9 * math.sin(w / (per * 0.71) + ph * 0.6)
        tilt = 7.0 * math.sin(w / (per * 1.21) + ph) + 3.0 * math.sin(w / (per * 0.63) + ph * 0.4)
        g = trim(gem, h=h * k).rotate(tilt, resample=Image.BICUBIC, expand=True)
        im.alpha_composite(g, (round((x + dx) * k - g.width / 2), round((y + dy) * k - g.height / 2)))
    p = trim(pig, w=PIG_W * k)
    dx, rot = 0.0, 0.0
    if full:   # a SOFT wobble (the user, 2026-11-16: smoother): 0.9 s of it every 1.8 s, easing in and out
        ph = t % 1.8
        if ph < 0.9:
            fade = math.sin(math.pi * ph / 0.9) ** 2
            dx = 2.0 * fade * math.sin(2 * math.pi * 3.2 * ph)
            rot = 3.0 * fade * math.sin(2 * math.pi * 3.2 * ph + 0.6)
    if rot:
        p = p.rotate(rot, resample=Image.BICUBIC, expand=True)
    feet = 152
    im.alpha_composite(p, (round((100 + dx) * k - p.width / 2), round(feet * k - p.height)))
    d = ImageDraw.Draw(im)
    s = '{:,}'.format(count).replace(',', ' ')
    f = font(34 * k)
    if full:   # the number glows gold, pulsing
        pulse = 0.5 + 0.5 * math.sin(2 * math.pi * t / 0.9)
        glow = Image.new('RGBA', im.size, 0)
        ImageDraw.Draw(glow).text((100 * k, 170 * k), s, font=f, fill=(255, 200, 60, round(150 + 100 * pulse)), anchor='mm',
                                  stroke_width=round(10 * k), stroke_fill=(255, 190, 40, round(120 + 100 * pulse)))
        im.alpha_composite(glow.filter(ImageFilter.GaussianBlur(7 * k)))
        d = ImageDraw.Draw(im)
        d.text((100 * k, 170 * k), s, font=f, fill=GOLD, anchor='mm', stroke_width=round(4 * k), stroke_fill=INK)
        # «Полна!» centred on the button's top edge (the user, 2026-11-16)
        tx, ty = 100 * k, 16 * k
        tf = font(19 * k)
        w = d.textlength('Полна!', font=tf)
        d.rounded_rectangle((tx - w / 2 - 9 * k, ty - 15 * k, tx + w / 2 + 9 * k, ty + 15 * k), 14 * k, fill=(220, 50, 40),
                            outline=(90, 14, 10), width=round(3 * k))
        d.text((tx, ty), 'Полна!', font=tf, fill=(255, 245, 225), anchor='mm', stroke_width=round(2 * k), stroke_fill=(90, 14, 10))
    else:
        d.text((100 * k, 170 * k), s, font=f, fill=CREAM, anchor='mm', stroke_width=round(4 * k), stroke_fill=INK)
    return im if k == 1 else im.resize((S, S), Image.LANCZOS)


def main():
    src, photo = sys.argv[1], sys.argv[2]
    gem = Image.open(os.path.join(src, 'piggy_gem_pink.png')).convert('RGBA')
    pig = Image.open(os.path.join(src, 'piggy_royal_pink_alone.png')).convert('RGBA')
    # the animation over the real corner of the modes screen, under its settings button (size check)
    shot = Image.open(photo).convert('RGBA')
    base = shot.crop((1880, 0, 2400, 520))   # the top-right corner, the settings button at its top
    frames = []
    fps = 30
    CX = 415   # under the settings button
    for i in range(fps * 6):
        t = i / fps
        full = t >= 3.0
        fr = base.copy()
        b = button(pig, gem, 1000 if full else 245, t, full, 4)
        fr.alpha_composite(b, (CX - S // 2, 300 - S // 2))
        d = ImageDraw.Draw(fr)
        # the square: the button's tap area
        for x in range(CX - S // 2, CX + S // 2, 14):
            d.line((x, 300 - S // 2, x + 7, 300 - S // 2), fill=(255, 255, 255, 160), width=2)
            d.line((x, 300 + S // 2, x + 7, 300 + S // 2), fill=(255, 255, 255, 160), width=2)
        for y in range(300 - S // 2, 300 + S // 2, 14):
            d.line((CX - S // 2, y, CX - S // 2, y + 7), fill=(255, 255, 255, 160), width=2)
            d.line((CX + S // 2, y, CX + S // 2, y + 7), fill=(255, 255, 255, 160), width=2)
        d.rounded_rectangle((20, 440, 480, 505), 18, fill=(30, 22, 16, 200))
        d.text((250, 472), 'Копилка полна' if full else 'Копится', font=font(26), fill=CREAM, anchor='mm')
        frames.append(fr.convert('RGB'))
    # full-colour animated WebP: a GIF's 256 colours band the pig's smooth shading into visible lines (the user saw them)
    frames[0].save(R + 'Design/store/Piggy_Button_Concept.webp', save_all=True, append_images=frames[1:],
                   duration=1000 // fps, loop=0, quality=92, method=4)
    # the five levels, as buttons
    sheet = Image.new('RGBA', (5 * 300 + 40, 430), (52, 40, 30, 255))
    d = ImageDraw.Draw(sheet)
    for j, (colour, name, cap) in enumerate(LEVELS):
        pg = Image.open(os.path.join(src, 'piggy_royal_%s_alone.png' % colour)).convert('RGBA')
        b = button(pg, gem, cap, 0.3, False, 2).resize((260, 260), Image.LANCZOS)
        x = 20 + j * 300
        sheet.alpha_composite(b, (x + 20, 70))
        d.text((x + 150, 34), 'Уровень %d' % (j + 1), font=font(32), fill=(250, 236, 200), anchor='mm')
        d.text((x + 150, 360), name, font=font(28), fill=(250, 236, 200), anchor='mm')
        d.text((x + 150, 398), 'вмещает %s' % '{:,}'.format(cap).replace(',', ' '), font=font(22, 'Rubik-Medium.ttf'), fill=(220, 200, 160), anchor='mm')
    sheet.convert('RGB').save(R + 'Design/store/Piggy_Levels_Concept.png', optimize=True)
    print('written Piggy_Button_Concept.webp, Piggy_Levels_Concept.png')


if __name__ == '__main__':
    main()
