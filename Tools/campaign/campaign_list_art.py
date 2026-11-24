"""Art for the campaign's LEVEL LIST (variant A, the user 2026-11-24: «Вариант А»): every level's preview (its
diorama from Tools/blender/campaign_dioramas.py on a sky, rounded, in a wood-and-gold frame) and the screen's
backdrop (the main menu's village, blurred and dimmed), into Assets/_Project/UI/Campaign/.

    python Tools/campaign/campaign_list_art.py <diorama dir>
"""
import os
import sys

from PIL import Image, ImageEnhance, ImageFilter

sys.path.insert(0, os.path.dirname(__file__))
from campaign_levels_overlay import ROOT  # noqa: E402
from campaign_list_concept import preview  # noqa: E402

OUT = os.path.join(ROOT, 'Assets', '_Project', 'UI', 'Campaign')
BACK = os.path.join(ROOT, 'Design', 'ui', 'MainMenu_VillageShot.jpg')
# the level's id in Campaign/CampaignMap.json -> its diorama
DIORAMAS = {"m1": "level_1", "m2": "level_2", "m3p": "prologue", "m3a": "level_3a", "m3b": "level_3b"}


def main(ddir):
    for lid, key in DIORAMAS.items():
        pic = preview(ddir, key, 588, 340, 'done', False, radius=22)
        pic.save(os.path.join(OUT, f"Preview_{lid}.png"))
    im = Image.open(BACK).convert('RGB').filter(ImageFilter.GaussianBlur(18))
    ImageEnhance.Brightness(im).enhance(0.5).resize((1280, 720), Image.LANCZOS).save(os.path.join(OUT, "List_Back.png"))
    print('ok')


if __name__ == '__main__':
    main(sys.argv[1])
