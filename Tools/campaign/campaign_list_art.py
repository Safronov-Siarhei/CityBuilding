"""Art for the campaign's LEVEL LIST (variant A, the user 2026-11-24: «Вариант А»): every level's preview and the
screen's backdrop (the main menu's village, blurred and dimmed), into Assets/_Project/UI/Campaign/.

Previews are FRAMES OF THE LEVELS' OWN SCENES the user picked (2026-11-24: «хочу чтобы в рамочках были кадры из
соответствующих сценок»; level 1 frame 4 of the film, level 2 frame 12, prologue 5, «Клетка» 2, «Пленные» = frame 8
of the prologue), kept in Design/concepts/campaign_list/previews/picked/<level id>.jpg (shot by the explicit play-mode
run Tests/PlayMode/CampaignPreviewShots, contact sheets by preview_sheet.py), cropped to 588x340, rounded, framed.

    python Tools/campaign/campaign_list_art.py
"""
import os
import sys

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

sys.path.insert(0, os.path.dirname(__file__))
from campaign_levels_overlay import ROOT  # noqa: E402
from preview_sheet import crop  # noqa: E402

OUT = os.path.join(ROOT, 'Assets', '_Project', 'UI', 'Campaign')
PICKED = os.path.join(ROOT, 'Design', 'concepts', 'campaign_list', 'previews', 'picked')
BACK = os.path.join(ROOT, 'Design', 'ui', 'MainMenu_VillageShot.jpg')
W, H, RADIUS = 588, 340, 22


def framed(path):
    pic = crop(Image.open(path).convert('RGB')).convert('RGBA')
    mask = Image.new('L', (W, H), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, W - 1, H - 1), RADIUS, fill=255)
    pic.putalpha(mask)
    out = Image.new('RGBA', (W + 12, H + 12), (0, 0, 0, 0))
    ImageDraw.Draw(out).rounded_rectangle((0, 0, W + 11, H + 11), RADIUS + 6, fill=(122, 84, 46), outline=(232, 190, 96), width=3)
    out.alpha_composite(pic, (6, 6))
    return out


def main():
    for f in sorted(os.listdir(PICKED)):
        lid = os.path.splitext(f)[0]
        framed(os.path.join(PICKED, f)).save(os.path.join(OUT, f"Preview_{lid}.png"))
    im = Image.open(BACK).convert('RGB').filter(ImageFilter.GaussianBlur(18))
    ImageEnhance.Brightness(im).enhance(0.5).resize((1280, 720), Image.LANCZOS).save(os.path.join(OUT, "List_Back.png"))
    print('ok')


if __name__ == '__main__':
    main()
