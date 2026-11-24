"""Art for the themed privacy-policy page (docs/privacy-policy.html, Firebase Hosting citycraft-game): the game's own
pieces, made small for phones. The user, 2026-11-24: «сделать страницу политики более красивой и тематической»,
«учти что её могут открывать на телефонах».

    python Tools/store/privacy_page_assets.py
"""
import os
import shutil

from PIL import Image, ImageEnhance, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'docs', 'assets')
SKIN = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'MenuSkins', 'Classic')
FONTS = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'Fonts')


def main():
    os.makedirs(OUT, exist_ok=True)
    # the main menu's village, blurred and dimmed: the menu's own buttons melt away
    bg = Image.open(os.path.join(ROOT, 'Design', 'ui', 'MainMenu_VillageShot.jpg')).convert('RGB')
    bg = bg.resize((1280, 720), Image.LANCZOS).filter(ImageFilter.GaussianBlur(9))
    ImageEnhance.Brightness(bg).enhance(0.62).save(os.path.join(OUT, 'bg.jpg'), quality=72, optimize=True, progressive=True)
    logo = Image.open(os.path.join(ROOT, 'Assets', '_Project', 'UI', 'MenuArt', 'Logo_CityCraft.png')).convert('RGBA')
    logo.resize((600, round(logo.height * 600 / logo.width)), Image.LANCZOS).save(os.path.join(OUT, 'logo.png'), optimize=True)
    icon = Image.open(os.path.join(ROOT, 'Assets', '_Project', 'UI', 'AppIcon.png')).convert('RGB')
    icon.resize((180, 180), Image.LANCZOS).save(os.path.join(OUT, 'icon.png'), optimize=True)
    icon.resize((48, 48), Image.LANCZOS).save(os.path.join(OUT, 'favicon.png'), optimize=True)
    for piece in ('panel', 'button', 'title'):
        shutil.copy(os.path.join(SKIN, piece + '.png'), os.path.join(OUT, piece + '.png'))
    # the main menu's language picker (UI/Icons/Flags, Tools/ui/language_flags.py): only the languages the page has
    flags = os.path.join(ROOT, 'Assets', '_Project', 'UI', 'Icons', 'Flags')
    for f in ('Flag_en', 'Flag_ru', 'Tick', 'Chevron'):
        shutil.copy(os.path.join(flags, f + '.png'), os.path.join(OUT, f + '.png'))
    for font in ('Rubik-Medium.ttf', 'Rubik-Bold.ttf', 'Rubik-OFL.txt'):
        shutil.copy(os.path.join(FONTS, font), os.path.join(OUT, font))
    total = sum(os.path.getsize(os.path.join(OUT, f)) for f in os.listdir(OUT))
    print(f'ok, {total // 1024} KB')


if __name__ == '__main__':
    main()
