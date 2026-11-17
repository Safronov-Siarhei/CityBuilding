"""The diplomacy windows and banners FOR THE GAME (the user approved the concepts 2026-11-17, «добавляй в игру»).
The concepts (diplomacy_concepts.py, diplomacy_banner_art.py) cut into pieces: the still art baked, every word left
to the game (localized), every number and resource icon filled in by the code. One folder per screen under
Assets/_Project/UI/Diplomacy/, each with its PNGs and layout.json; SetupProjectDiplomacy.BuildBaked places them.

    python Tools/ui/diplomacy_game_art.py <folder with the banner figures (see diplomacy_banner_art.py)>

layout.json: {"reference": [1920, 1080], "items": [...]} in drawing order. An item is
  kind "p" -- a picture: sprite (a PNG in the folder; "" = an empty slot the code fills), rect, button, parent;
  kind "t" -- a text: key ("" = the code writes it), size, font x/b/m (heavy/bold/regular), color, rim, align c/l/r, rot;
  kind "g" -- an empty group (to show/hide what is parented to it).
rect = [x0, y0, x1, y1] in the 1920x1080 reference, y down. A parented item's rect is still in the reference.
Also writes UI/Glyphs/Hud_Diplomacy.png (the treaty, the user's pick C) and UI/Icons/PortraitKingNeighbour.png.
"""
import json
import os
import sys

from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import diplomacy_concepts as dc  # noqa: E402
from diplomacy_concepts import W, H, INK, CREAM, RED, GREEN, GREY, SHOP, ICONS, GLYPHS, load, panel, card, button, grey, put, res, chest  # noqa: E402
from diplomacy_icon_concepts import treaty  # noqa: E402
import diplomacy_banner_art as ba  # noqa: E402  (reads the figures folder from argv[1])

ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(ROOT, "Assets/_Project/UI/Diplomacy")


def hexc(c):
    return "%02x%02x%02x" % tuple(c[:3])


class Screen:
    """A screen's still background (one picture, 'Window') plus the items over it."""

    def __init__(self, name):
        self.name = name
        self.bg = Image.new("RGBA", (W, H))
        self.items = []
        self.sprites = {}

    def sprite(self, sprite, im):
        if sprite not in self.sprites:
            self.sprites[sprite] = im
        return self.sprites[sprite]

    def pic(self, name, sprite, im, cx, cy, button=False, parent=""):
        """A picture centred on (cx, cy); im = None reuses the sprite saved before."""
        im = self.sprite(sprite, im)
        x0, y0 = int(round(cx - im.width / 2)), int(round(cy - im.height / 2))
        self.items.append({"kind": "p", "name": name, "sprite": sprite, "rect": [x0, y0, x0 + im.width, y0 + im.height],
                           "button": button, "parent": parent})

    def slot(self, name, rect, parent=""):
        self.items.append({"kind": "p", "name": name, "sprite": "", "rect": [int(v) for v in rect], "button": False, "parent": parent})

    def group(self, name):
        self.items.append({"kind": "g", "name": name, "sprite": "", "rect": [0, 0, W, H], "button": False, "parent": ""})

    def text(self, name, cx, cy, w, h, key, size, font="x", color=INK, rim=None, align="c", rot=0, parent=""):
        self.items.append({"kind": "t", "name": name, "rect": [int(cx - w / 2), int(cy - h / 2), int(cx + w / 2), int(cy + h / 2)],
                           "key": key, "size": size, "font": font, "color": hexc(color), "rim": hexc(rim) if rim else "",
                           "align": align, "rot": rot, "parent": parent})

    def save(self):
        folder = os.path.join(OUT, self.name)
        os.makedirs(folder, exist_ok=True)
        bb = self.bg.getbbox()
        self.bg.crop(bb).save(os.path.join(folder, "Window.png"))
        items = [{"kind": "p", "name": "Window", "sprite": "Window", "rect": list(bb), "button": False, "parent": ""}] + self.items
        for sprite, im in self.sprites.items():
            im.save(os.path.join(folder, sprite + ".png"))
        for it in items:   # JsonUtility wants every field on every item
            for k, v in (("sprite", ""), ("button", False), ("parent", ""), ("key", ""), ("size", 0), ("font", "x"),
                         ("color", "ffffff"), ("rim", ""), ("align", "c"), ("rot", 0)):
                it.setdefault(k, v)
        with open(os.path.join(folder, "layout.json"), "w", encoding="utf-8") as f:
            json.dump({"reference": [W, H], "items": items}, f, indent=1, ensure_ascii=False)
        print(self.name, len(self.sprites) + 1, "pictures,", len(items), "items")


BROWN = (60, 36, 16)


def btn(kind, w, h, icon=None, icon_h=None):
    """A button picture; an icon baked at its left. Returns (image, label box offset from the centre)."""
    b = button(kind, w, h) if kind != "greyblue" else grey(button("blue", w, h), .85)
    if icon is not None:
        ih = icon_h or int(h * .62)
        ic = icon.resize((int(icon.width * ih / icon.height), ih), Image.LANCZOS)
        b.alpha_composite(ic, (int(h * .32), int(h / 2 - ic.height / 2 - 4)))
    return b


def label_on(scr, name, key, cx, cy, w, h, has_icon, size=36, rim=BROWN, parent=""):
    shift = h * .36 if has_icon else 0
    scr.text(name, cx + shift, cy - 4, w - 2 * shift - 30, h - 16, key, size, "x", CREAM, rim, parent=parent)


# ---------------------------------------------------------------- the Diplomacy window (peace and war in one)
def main_window():
    s = Screen("Window")
    pw, ph = 1120, 770
    px, py = (W - pw) // 2, 180
    cx = W // 2
    s.bg.alpha_composite(panel(pw, ph), (px, py))
    st = dc.nine(os.path.join(SHOP, "Badge.png"), (640, 92), (40, 20, 40, 20))
    put(s.bg, st, cx, py + 4)
    s.text("Title", cx, py, 600, 70, "#diplomacy_title", 44, "x", CREAM, (70, 40, 16))
    s.pic("Close", "Close", load(SHOP, "Close", (96, 96)), px + pw - 30, py + 30, button=True)
    # the kings
    put(s.bg, dc.king_badge(150, False), px + 140, py + 145)
    put(s.bg, dc.king_badge(150, True), px + pw - 140, py + 145)
    s.text("You", px + 140, py + 238, 200, 40, "#diplomacy_you", 30)
    s.text("Neighbour", px + pw - 140, py + 238, 200, 40, "#diplomacy_neighbour", 30)
    s.text("Caption", cx, py + 90, 680, 44, "#diplomacy_relations_caption", 34, "b")
    s.text("Value", cx, py + 168, 300, 80, "", 64, "x", RED, CREAM)
    s.text("PerDay", cx, py + 222, 400, 34, "#diplomacy_per_day", 26, "b", GREY)
    # the scale: band and marks baked, the needle moved by the code along ScaleSpan
    x0, y, w = px + 150, py + 290, pw - 300
    band_layer = Image.new("RGBA", (W, H))
    d = ImageDraw.Draw(band_layer)
    hgt = 34
    band = Image.new("RGBA", (w, hgt))
    bd = ImageDraw.Draw(band)
    for i in range(w):
        k = i / (w - 1)
        col = (int(190 - 120 * k), int(60 + 100 * k), int(44 + 10 * k), 255) if k < 0.5 else (int(130 - 70 * (k - .5) * 2), int(150 + 20 * (k - .5) * 2), 60, 255)
        bd.line([(i, 0), (i, hgt)], fill=col)
    m = Image.new("L", (w, hgt))
    ImageDraw.Draw(m).rounded_rectangle([0, 0, w - 1, hgt - 1], hgt // 2, fill=255)
    band_layer.paste(band, (x0, y), m)
    d.rounded_rectangle([x0, y, x0 + w, y + hgt], hgt // 2, outline=INK, width=4)

    def at(v):
        return x0 + (v + 100) / 200 * w
    for v, lab in [(-50, "−50"), (0, "0"), (30, "+30"), (60, "+60")]:
        d.line([(at(v), y - 6), (at(v), y + hgt + 6)], fill=INK, width=3)
        dc.text(d, (at(v), y + hgt + 26), lab, 24, INK, "Bold")
    s.bg.alpha_composite(band_layer)
    s.text("ScaleDanger", at(-75), y - 22, 200, 30, "#diplomacy_scale_danger", 22, "b", RED)
    s.text("ScaleTrade", at(45), y - 22, 160, 30, "#diplomacy_scale_trade", 22, "b", GREEN)
    s.text("ScaleGifts", at(80), y - 22, 160, 30, "#diplomacy_scale_gifts", 22, "b", GREEN)
    s.slot("ScaleSpan", [x0, y, x0 + w, y + hgt])
    needle = Image.new("RGBA", (40, 60))
    nd = ImageDraw.Draw(needle)
    nd.polygon([(2, 2), (38, 2), (20, 26)], fill=CREAM, outline=INK)
    nd.line([(20, 18), (20, 52)], fill=CREAM, width=5)
    s.pic("Needle", "Needle", needle, at(0), y + 12)
    # the state
    put(s.bg, card(360, 70, selected=True), cx, py + 405)
    s.text("State", cx, py + 402, 340, 50, "", 34)
    # peace: the three actions
    s.group("AtPeace")
    yb = py + 540
    for name, kind, icon, x, key in (("Gift", "gold", res("coins", 64), cx - 340, "#diplomacy_gift"),
                                     ("Trade", "blue", load(SHOP, "Stall", (64, 64)), cx, "#diplomacy_trade"),
                                     ("TradeLocked", "greyblue", load(GLYPHS, "Hud_Lock", (60, 60)), cx, "#diplomacy_trade"),
                                     ("War", "orange", load(GLYPHS, "Swords", (60, 60)), cx + 340, "#diplomacy_war")):
        s.pic(name, name, btn(kind, 300, 96, icon), x, yb, button=True, parent="AtPeace")
        label_on(s, name + "Label", key, x, yb, 300, 96, True, parent=name)
    s.text("GiftSub", cx - 340, yb + 70, 320, 34, "#diplomacy_gift_sub", 26, "b", parent="AtPeace")
    s.text("TradeSub", cx, yb + 70, 320, 34, "", 26, "b", parent="AtPeace")
    s.text("WarSub", cx + 340, yb + 70, 320, 34, "#diplomacy_war_sub", 26, "b", parent="AtPeace")
    s.text("Help1", cx, py + ph - 100, 1000, 36, "#diplomacy_help_1", 28, "m", parent="AtPeace")
    s.text("Help2", cx, py + ph - 62, 1000, 36, "#diplomacy_help_2", 28, "m", RED, parent="AtPeace")
    # war: the wave, peace for coins / crystals
    s.group("AtWar")
    s.text("Wave", cx, py + 470, 1000, 40, "", 30, "b", RED, parent="AtWar")
    s.pic("PeaceCoins", "PeaceCoins", btn("green", 440, 96, res("coins", 64)), cx - 260, yb, button=True, parent="AtWar")
    label_on(s, "PeaceCoinsLabel", "", cx - 260, yb, 440, 96, True, parent="PeaceCoins")
    s.pic("PeaceCrystals", "PeaceCrystals", btn("blue", 440, 96, res("crystal", 64)), cx + 260, yb, button=True, parent="AtWar")
    label_on(s, "PeaceCrystalsLabel", "", cx + 260, yb, 440, 96, True, parent="PeaceCrystals")
    s.text("PeaceCoinsSub", cx - 260, yb + 70, 460, 34, "", 26, "b", parent="AtWar")
    s.text("PeaceCrystalsSub", cx + 260, yb + 70, 460, 34, "#diplomacy_peace_crystals_sub", 26, "b", parent="AtWar")
    s.text("WarHelp1", cx, py + ph - 100, 1000, 36, "#diplomacy_war_help_1", 28, "m", parent="AtWar")
    s.text("WarHelp2", cx, py + ph - 62, 1000, 36, "#diplomacy_war_help_2", 28, "m", parent="AtWar")
    # a short note (not enough coins...) over the help lines for a few seconds
    s.text("Note", cx, py + ph - 80, 1000, 40, "", 30, "b", RED)
    s.save()


# ---------------------------------------------------------------- the gift to the neighbour
def gift_window():
    s = Screen("Gift")
    pw, ph = 1220, 640
    px, py = (W - pw) // 2, 230
    cx = W // 2
    s.bg.alpha_composite(panel(pw, ph), (px, py))
    put(s.bg, dc.nine(os.path.join(SHOP, "Badge.png"), (640, 92), (40, 20, 40, 20)), cx, py + 4)
    s.text("Title", cx, py, 600, 70, "#diplomacy_gift_title", 44, "x", CREAM, (70, 40, 16))
    s.pic("Close", "Close", load(SHOP, "Close", (96, 96)), px + pw - 30, py + 30, button=True)
    s.text("Once", cx, py + 110, 1100, 44, "#diplomacy_gift_once", 32, "b")
    for i, (name, stack) in enumerate((("Small", 1), ("Medium", 2), ("Large", 3))):
        x = cx + (i - 1) * 370
        cw, ch = 330, 420
        s.bg.alpha_composite(card(cw, ch, selected=i == 1), (x - cw // 2, py + 160))
        for k in range(stack):
            put(s.bg, res("coins", 120 - 10 * k), x - (stack - 1) * 30 + k * 60, py + 320 - k * 6)
        s.text(name + "Name", x, py + 210, 300, 50, "#diplomacy_gift_" + name.lower(), 40)
        s.text(name + "Plus", x, py + 410, 320, 40, "", 30, "b", GREEN)
        s.pic(name + "Buy", "Buy", btn("gold", 260, 90, res("coins", 60)), x, py + 510, button=True)
        label_on(s, name + "Price", "", x, py + 510, 260, 90, True, parent=name + "Buy")
    # sent today: a veil with the time till tomorrow
    s.group("Sent")
    veil = Image.new("RGBA", (pw - 80, 440), (245, 226, 186, 236))
    s.pic("Veil", "Veil", veil, cx, py + 150 + 220, parent="Sent")
    s.pic("Timer", "Timer", load(SHOP, "Timer", (300, 100)), cx, py + 330, parent="Sent")
    s.text("TimerText", cx + 20, py + 326, 200, 60, "", 44, "x", CREAM, INK, parent="Sent")
    s.text("SentText", cx, py + 420, 1000, 46, "#diplomacy_gift_sent", 34, parent="Sent")
    s.save()


# ---------------------------------------------------------------- the trade with the neighbour
def trade_window():
    s = Screen("Trade")
    pw, ph, py = dc.TRADE_PW, dc.TRADE_PH, dc.TRADE_PY
    px = (W - pw) // 2
    cx = W // 2
    s.bg.alpha_composite(panel(pw, ph), (px, py))
    put(s.bg, dc.nine(os.path.join(SHOP, "Badge.png"), (640, 92), (40, 20, 40, 20)), cx, py + 4)
    s.text("Title", cx, py, 600, 70, "#diplomacy_trade_title", 44, "x", CREAM, (70, 40, 16))
    s.pic("Close", "Close", load(SHOP, "Close", (96, 96)), px + pw - 30, py + 30, button=True)
    put(s.bg, card(330, 70, True), px + 230, py + 110)
    s.text("Deals", px + 230, py + 107, 310, 50, "", 32)
    put(s.bg, load(SHOP, "Timer", (260, 86)), px + pw - 230, py + 110)
    s.text("TimerText", px + pw - 210, py + 106, 180, 50, "", 38, "x", CREAM, INK)
    s.text("NewIn", px + pw - 520, py + 108, 200, 40, "#diplomacy_trade_new_in", 28, "b", align="r")
    cw, ch = 300, 300
    plain, rare = card(cw, ch), card(cw, ch, selected=True)
    arrow = Image.new("RGBA", (30, 30))
    ImageDraw.Draw(arrow).polygon([(2, 1), (28, 15), (2, 29)], fill=INK)
    stamp = dc.bought_stamp(250, -14, word=None)
    veil = Image.new("RGBA", (cw - 20, ch - 20), (214, 200, 170, 242))
    vd = ImageDraw.Draw(veil)
    veil.alpha_composite(load(GLYPHS, "Hud_Lock", (70, 70)), ((cw - 20) // 2 - 35, 85))
    bought_veil = Image.new("RGBA", (cw - 20, ch - 20), (240, 226, 190, 120))
    for i in range(10):
        col, row = i % 5, i // 5
        x = px + 170 + col * 340
        top = py + 180 + row * 330
        g = "Offer%d" % i
        s.group(g)
        s.pic(g + "Card", "Card", plain, x, top + ch / 2, parent=g)
        s.pic(g + "CardRare", "CardRare", rare, x, top + ch / 2, parent=g)
        s.text(g + "Rare", x, top + 26, 260, 30, "#diplomacy_trade_rare", 22, "x", (150, 70, 170), parent=g)
        s.slot(g + "Give", [x - 72 - 42, top + 50, x - 72 + 42, top + 134], parent=g)
        s.slot(g + "Get", [x + 72 - 42, top + 50, x + 72 + 42, top + 134], parent=g)
        s.pic(g + "Arrow", "Arrow", arrow, x, top + 94, parent=g)
        s.text(g + "GiveN", x - 72, top + 152, 140, 40, "", 32, parent=g)
        s.text(g + "GetN", x + 72, top + 152, 140, 40, "", 32, "x", GREEN, parent=g)
        s.text(g + "GiveL", x - 72, top + 186, 140, 30, "#diplomacy_trade_give", 22, "b", GREY, parent=g)
        s.text(g + "GetL", x + 72, top + 186, 140, 30, "#diplomacy_trade_get", 22, "b", GREY, parent=g)
        s.pic(g + "Swap", "Swap", button("green", 230, 78), x, top + 244, button=True, parent=g)
        s.text(g + "SwapL", x, top + 240, 210, 60, "#diplomacy_trade_swap", 34, "x", CREAM, BROWN, parent=g + "Swap")
        s.pic(g + "Bought", "BoughtVeil", bought_veil, x, top + ch / 2, parent=g)
        s.pic(g + "Stamp", "Stamp", stamp, x, top + ch / 2 + 6, parent=g + "Bought")
        s.text(g + "StampL", x, top + ch / 2 + 8, 210, 56, "#diplomacy_trade_bought", 34, "x", (247, 232, 196), rot=-14, parent=g + "Bought")
        s.pic(g + "Locked", "LockedVeil", veil, x, top + ch / 2, parent=g)
        s.text(g + "LockedL", x, top + 210, 260, 36, "#diplomacy_trade_locked", 28, "x", GREY, parent=g + "Locked")
    y = py + ph - 64
    s.text("Refresh", cx - 420, y, 600, 40, "#diplomacy_trade_refresh", 30, "b")
    s.pic("Ad", "Ad", btn("blue", 260, 84, load(SHOP, "Play", (56, 56))), cx, y, button=True)
    label_on(s, "AdLabel", "#diplomacy_trade_ad", cx, y, 260, 84, True, parent="Ad")
    s.pic("Crystals", "Crystals", btn("gold", 260, 84, res("crystal", 56)), cx + 300, y, button=True)
    label_on(s, "CrystalsLabel", "", cx + 300, y, 260, 84, True, parent="Crystals")
    s.save()


# ---------------------------------------------------------------- the neighbour's hall razed
def razed_window():
    s = Screen("Razed")
    pw, ph = 900, 560
    px, py = (W - pw) // 2, 240
    cx = W // 2
    s.bg.alpha_composite(panel(pw, ph), (px, py))
    put(s.bg, dc.nine(os.path.join(SHOP, "Badge.png"), (640, 92), (40, 20, 40, 20)), cx, py + 4)
    s.text("Title", cx, py, 600, 70, "#diplomacy_razed_title", 40, "x", CREAM, (70, 40, 16))
    put(s.bg, load(SHOP, "Glow", (380, 380)), cx, py + 210)
    put(s.bg, res("crystal", 200), cx, py + 210)
    s.text("Amount", cx, py + 350, 300, 80, "", 60, "x", GREEN, CREAM)
    s.text("Text", cx, py + 410, 840, 44, "#diplomacy_razed_text", 30, "b")
    s.pic("Take", "Take", button("green", 340, 90), cx, py + ph - 60, button=True)
    s.text("TakeL", cx, py + ph - 64, 320, 60, "#diplomacy_take", 36, "x", CREAM, BROWN, parent="Take")
    s.save()


# ---------------------------------------------------------------- the four banners (the plague/fire build)
def banner_base(name, field, model, glow, tall=600, recolour=False):
    s = Screen(name)
    img = ba.window(field(ba.IX1 - ba.IX0, ba.IY1 - ba.IY0))
    img = ba.figure(img, ba.FIG + "figure_%s_24.png" % model, tall, 320, glow, recolour)
    img = ba.figure(img, ba.FIG + "figure_%s_-24.png" % model, tall, 1600, glow, recolour)
    s.bg = img
    return s


def ribbon(s, colours, title_key):
    img = ba.ribbon(Image.new("RGBA", (W, H)), "", *colours)
    s.bg.alpha_composite(img)
    out = colours[3]
    s.text("Title", ba.CX, (ba.Y0 - 82 + ba.IY0 + 66) / 2 + 6, 900, 100, title_key, 72, "x", (255, 240, 200), out)


def bbutton(s, name, kind, box, key, icon=None, tail=False):
    col, light, out = ba.BTN[kind]
    x0, y0, x1, y1 = box
    im = Image.new("RGBA", (int(x1 - x0), int(y1 - y0) + 12))
    d = ImageDraw.Draw(im)
    w, h = im.width, int(y1 - y0)
    d.rounded_rectangle((0, 11, w - 1, h + 11), 40, fill=out)
    d.rounded_rectangle((0, 0, w - 1, h), 40, fill=col, outline=out, width=7)
    d.rounded_rectangle((11, 10, w - 12, h / 2), 30, fill=light)
    if icon is not None:
        im.alpha_composite(icon, (40, int(h / 2 - icon.height / 2)))
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    s.pic(name, name, im, cx, cy + 6, button=True)
    shift = (icon.width / 2 + 10) if icon is not None else 0
    s.text(name + "Label", cx + shift - (34 if tail else 0), cy, (x1 - x0) - 2 * shift - 80, h - 20, key, 40, "x", (255, 255, 255), out, parent=name)
    if tail:
        s.text(name + "Tail", x1 - 70, cy, 90, h - 20, "", 38, "x", (255, 250, 200), out, align="r", parent=name)


def tile(s, name, cx, cy, size=170):
    d = ImageDraw.Draw(s.bg)
    d.rounded_rectangle((cx - size / 2, cy - size / 2, cx + size / 2, cy + size / 2), 26, fill=(236, 214, 166), outline=(150, 96, 52), width=5)
    s.slot(name + "Icon", [cx - 48, cy - size / 2 + 18, cx + 48, cy - size / 2 + 114])
    s.text(name + "N", cx, cy + size / 2 - 28, size - 10, 44, "", 36, "x", (70, 150, 50), (255, 250, 230))


def help_banner():
    s = banner_base("BannerHelp", ba.help_field, "VillagerCraftsman1", (255, 230, 160))
    ribbon(s, ((44, 104, 176), (18, 50, 98), (96, 160, 222), (14, 38, 80)), "#diplomacy_help_title")
    s.text("Body", ba.CX, 410, 560, 120, "", 38, "m", ba.INK)
    tile(s, "Ask", ba.CX, 575, 180)
    bbutton(s, "Send", "green", (ba.CX - 270, 710, ba.CX + 270, 820), "#diplomacy_help_send", tail=True)
    bbutton(s, "Refuse", "grey", (ba.CX - 220, 860, ba.CX + 220, 950), "#diplomacy_help_refuse", tail=True)
    s.save()


def gift_banner():
    s = banner_base("BannerGift", ba.gift_field, "HeroKing", (255, 240, 170), recolour=True)
    ribbon(s, ((220, 160, 40), (140, 92, 14), (252, 214, 110), (100, 60, 8)), "#diplomacy_botgift_title")
    s.text("Body", ba.CX, 410, 560, 60, "#diplomacy_botgift_text", 38, "m", ba.INK)
    for i in range(3):
        s.group("Item%d" % i)
    for i, x in enumerate((ba.CX - 180, ba.CX, ba.CX + 180)):
        d = ImageDraw.Draw(s.bg)
        n = "Item%d" % i
        # the tile drawn as a picture so an unused one can hide
        t = Image.new("RGBA", (170, 170))
        ImageDraw.Draw(t).rounded_rectangle((0, 0, 169, 169), 26, fill=(236, 214, 166), outline=(150, 96, 52), width=5)
        s.pic(n + "Tile", "Tile", t, x, 620, parent=n)
        s.slot(n + "Icon", [x - 44, 620 - 85 + 18, x + 44, 620 - 85 + 106], parent=n)
        s.text(n + "N", x, 620 + 85 - 28, 160, 44, "", 36, "x", (70, 150, 50), (255, 250, 230), parent=n)
    bbutton(s, "Take", "green", (ba.CX - 250, 790, ba.CX + 250, 900), "#diplomacy_take")
    s.save()


def edge_banner():
    s = banner_base("BannerEdge", ba.edge_field, "SoldierSpearman-lvl2", (255, 160, 80), 720)
    ribbon(s, ((214, 104, 34), (130, 54, 12), (248, 160, 90), (96, 36, 6)), "#diplomacy_edge_title")
    CX = ba.CX
    d = ImageDraw.Draw(s.bg)
    y = 445
    crack = [(CX - 70, y - 6), (CX - 40, y + 10), (CX - 18, y - 14), (CX + 6, y + 12), (CX + 30, y - 10), (CX + 70, y + 6)]
    d.line(crack, fill=(60, 30, 16), width=16)
    d.line(crack, fill=(196, 52, 44), width=8)
    for side in (-1, 1):
        b = dc.king_badge(170, side > 0)
        s.bg.alpha_composite(b, (int(CX + side * 165 - b.width / 2), int(y - b.height / 2)))
    d = ImageDraw.Draw(s.bg)
    d.rounded_rectangle((CX - 62, y + 40, CX + 62, y + 104), 30, fill=(150, 46, 36), outline=(60, 30, 16), width=5)
    s.text("Value", CX, y + 72, 120, 56, "", 46, "x", (255, 240, 200), (50, 26, 10))
    x0, x1, sy = int(CX) - 240, int(CX) + 240, 652
    band = Image.new("RGBA", (x1 - x0, 34))
    bd = ImageDraw.Draw(band)
    for i in range(band.width):
        k = i / (band.width - 1)
        bd.line((i, 0, i, 34), fill=(int(196 * (1 - k) + 70 * k), int(56 * (1 - k) + 170 * k), 52, 255))
    m = Image.new("L", band.size)
    ImageDraw.Draw(m).rounded_rectangle((0, 0, band.width - 1, 33), 17, fill=255)
    s.bg.paste(band, (x0, sy), m)
    d = ImageDraw.Draw(s.bg)
    d.rounded_rectangle((x0 - 3, sy - 3, x1 + 3, sy + 37), 20, outline=(60, 36, 16), width=5)
    s.slot("ScaleSpan", [x0, sy, x1, sy + 34])
    wx = x0 + 50 / 200 * (x1 - x0)
    d.line((wx, sy + 2, wx, sy + 32), fill=(60, 36, 16), width=4)
    sw = Image.open(os.path.join(GLYPHS, "Swords.png")).convert("RGBA")
    sw = sw.crop(sw.getbbox()).resize((44, 44), Image.LANCZOS)
    d.ellipse((wx - 30, sy - 72, wx + 30, sy - 12), fill=(178, 48, 38), outline=(60, 20, 10), width=4)
    s.bg.alpha_composite(sw, (int(wx - 22), int(sy - 64)))
    needle = Image.new("RGBA", (34, 26))
    ImageDraw.Draw(needle).polygon([(1, 1), (33, 1), (17, 25)], fill=(255, 246, 220), outline=(60, 36, 16))
    s.pic("Needle", "Needle", needle, wx, sy + 4)
    s.text("Body", CX, 728, 560, 50, "#diplomacy_edge_text", 36, "m", (150, 40, 30))
    bbutton(s, "Gift", "gold", (CX - 250, 778, CX + 250, 884), "#diplomacy_gift", ba.icon("Res_Coins", 64))
    s.text("Later", CX, 942, 400, 60, "#diplomacy_later", 44, "b", (120, 106, 58))
    s.items[-1]["button"] = True
    s.save()


def war_banner():
    s = banner_base("BannerWar", ba.war_field, "SoldierKnight-lvl2", (255, 110, 80), 640)
    ribbon(s, ((178, 40, 34), (100, 16, 12), (226, 96, 80), (70, 10, 8)), "#diplomacy_warbanner_title")
    CX = ba.CX
    d = ImageDraw.Draw(s.bg)
    d.ellipse((CX - 80, 340, CX + 80, 500), fill=(178, 48, 38), outline=(60, 20, 10), width=6)
    sw = Image.open(os.path.join(GLYPHS, "Swords.png")).convert("RGBA")
    sw = sw.crop(sw.getbbox())
    sw = sw.resize((110, round(sw.height * 110 / sw.width)), Image.LANCZOS)
    s.bg.alpha_composite(sw, (int(CX - sw.width / 2), int(420 - sw.height / 2)))
    s.text("Body", CX, 560, 560, 50, "#diplomacy_warbanner_text", 34, "m", ba.INK)
    s.text("Timer", CX, 615, 300, 70, "", 64, "x", (178, 40, 34), (255, 245, 220))
    bbutton(s, "ToHall", "blue", (CX - 250, 690, CX + 250, 800), "#diplomacy_to_hall")
    s.text("Ok", CX, 880, 400, 60, "#diplomacy_ok", 44, "b", (120, 106, 58))
    s.items[-1]["button"] = True
    s.save()


def glyph_and_portrait():
    g = treaty().resize((256, 256), Image.LANCZOS)
    g.save(os.path.join(ROOT, "Assets/_Project/UI/Glyphs/Hud_Diplomacy.png"))
    k = load(ICONS, "PortraitKing")
    k = ba.neighbour_colours(k.transpose(Image.FLIP_LEFT_RIGHT))
    k.save(os.path.join(ROOT, "Assets/_Project/UI/Icons/PortraitKingNeighbour.png"))


if __name__ == "__main__":
    main_window()
    gift_window()
    trade_window()
    razed_window()
    help_banner()
    gift_banner()
    edge_banner()
    war_banner()
    glyph_and_portrait()
