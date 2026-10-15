"""Writes <id>1_lvl4.py for the 40 non-pilot buildings from one table: each row says what level
4 adds on top of level 3 (kit.borrow) and where its roofs are. Plain python, not Blender:

    python gen_lvl4.py .        (from Tools/blender; overwrites the 40 scripts)

Hovel, Cottage, Warehouse and Sawmill (the pilot) are hand-written and not in the table.
"""
import os
import sys

OUT = sys.argv[1]

CITY, STORE, FUN, DEF, PROD, FOOD = ("Город", "Склады", "Развлечения", "Оборонительные здания",
                                    "Производственные здания", "Производство еды")

# (stem, Obj prefix, Russian name, collection, footprint text, what level 4 adds, code lines)
T = [
    ("castle1", "Castle1", "Ратуша", CITY, "5x5",
     "the hall's roof and the blue cones of the four corner turrets and the clock tower's spire "
     "re-laid in glazed tiles, and a gilded crest along the hall's ridge",
     """J = d.JETTY
d.glazed(b, 0, 1.0, 'x', 1.75 + J + 0.1, 0.9, U_TOP, 'roof_blue_dark')
t_top = WALL_H + 0.4
for sx in (-1, 1):
    for sy in (-1, 1):
        d.glazed_pyramid(b, sx * 2.06, sy * 2.06, t_top + 0.06, 0.43, 0.43, t_top + 0.52,
                         'roof_blue_dark', crest=False)
bell1 = U_TOP + 0.38 + 0.44
d.glazed_pyramid(b, 0, 0.04, bell1 + 0.01, 0.52, 0.56, bell1 + 0.5, 'roof_blue_dark',
                 crest=False)"""),
    ("well1", "Well1", "Колодец", CITY, "1x1",
     "its little roof re-laid in glazed tiles with a gilded crest along the ridge",
     """d.glazed(b, 0, 0, 'x', 0.43, 0.16, 0.64, 'roof_orange_dark', overhang=0.04, t=0.04, w=0.045,
         step=0.15, row=0.12)"""),
    ("manor1", "Manor1", "Особняк", CITY, "3x3",
     "both wings' roofs re-laid in glazed tiles and gilded crests along their ridges (the side "
     "wing's stops where its ridge runs under the main roof)",
     """J = d.JETTY
d.glazed(b, 0, 0.725, 'x', 1.34 + J + 0.09, 0.625, U_TOP, 'roof_orange_dark')
d.glazed_roof(b, 0.91, -0.3575, 'y', 1.0825, 0.48, U_TOP, 'roof_orange_dark')
d.ridge_crest(b, 0.91, -0.495, 'y', 0.945, 0.48, U_TOP)"""),
    ("firebrigade1", "FireBrigade1", "Пожарная бригада", CITY, "2x2",
     "the engine house's roof and the lookout tower's pyramid re-laid in glazed tiles, and a "
     "gilded crest along the ridge",
     """d.glazed(b, -0.28, 0.16, 'y', 0.75, 0.58, G_TOP, 'roof_orange_dark')
R = L3['L2']['RAISE']
d.glazed_pyramid(b, 0.64, 0.38, 1.27 + R, 0.32, 0.32, hi[2] - 0.02 + R, 'roof_orange_dark',
                 crest=False)"""),
    ("healerhouse1", "HealerHouse1", "Дом лекаря", CITY, "2x4",
     "the long roof re-laid in glazed tiles and a gilded crest along the ridge",
     """d.glazed(b, 0, 0.78, 'y', 1.17, 0.72, G_TOP, 'roof_orange_dark')"""),
    ("laboratory1", "Laboratory1", "Лаборатория", CITY, "4x4",
     "the academy's roof and the observatory dome re-laid in glazed tiles, a gilded crest along "
     "the academy's ridge, and a gold band round the foot of the dome",
     """d.glazed(b, -0.675, 1.025, 'x', 1.275, 0.825, U_TOP, 'roof_orange_dark')
b.ring(1.2, 1.1, U_TOP + 0.6, U_TOP + 0.64, 0.6, 0.67, ['gold'], sides=8)
dome_base = U_TOP + 0.62
d.glazed_dome(b, 1.2, 1.1, dome_base, 0.6, hi[2] + 0.24 - dome_base - 0.12,
              'roof_purple_dark')"""),

    ("barn1", "Barn1", "Амбар", STORE, "1x1",
     "the roof re-laid in glazed tiles and a gilded crest along the ridge",
     """d.glazed(b, 0, 0.06, 'y', 0.41, 0.36, L_TOP, 'roof_red_dark', w=0.05, step=0.16, row=0.13)"""),
    ("granary1", "Granary1", "Кладовая", STORE, "2x1",
     "the roof and the awning re-laid in glazed tiles, gilded crests along the ridge and the "
     "awning's top",
     """d.glazed(b, 0, 0.03, 'x', 0.91, 0.31, L_TOP, 'roof_red_dark')
d.glazed_shed(b, 'x', -0.05, 0.85, -0.28, -0.45, 0.66, 0.52, 'roof_red_dark')"""),
    ("bigbarn1", "BigBarn1", "Большой амбар", STORE, "2x2",
     "the roof and the lean-to re-laid in glazed tiles, with gilded crests",
     """d.glazed(b, -0.1, 0.17, 'y', 0.76, 0.52, U_TOP, 'roof_red_dark')
d.glazed_shed(b, 'y', -0.16, 0.9, 0.42, 0.9, 0.9, 0.6, 'roof_red_dark')"""),
    ("biggranary1", "BigGranary1", "Большая кладовая", STORE, "3x2",
     "the roof and the platform's awning re-laid in glazed tiles, with gilded crests",
     """d.glazed(b, 0, 0.36, 'x', 1.39, 0.48, U_TOP, 'roof_red_dark')
d.glazed_shed(b, 'x', -1.3, 1.3, -0.12, -0.54, L_TOP - 0.06, G_TOP, 'roof_red_dark')"""),
    ("bigwarehouse1", "BigWarehouse1", "Большой склад", STORE, "3x3",
     "the roof re-laid in glazed tiles and a gilded crest along the ridge",
     """d.glazed(b, 0, 0.675, 'x', 1.44, 0.675, U_TOP, 'roof_red_dark')"""),
    ("treasury1", "Treasury1", "Сокровищница", STORE, "3x2",
     "the roof re-laid in glazed tiles and a gilded crest along the ridge",
     """d.glazed(b, 0, 0.18, 'x', 1.25, 0.6, U_TOP, 'roof_red_dark')"""),
    ("bigtreasury1", "BigTreasury1", "Большая сокровищница", STORE, "4x3",
     "the strongroom's roof and both towers' pyramids re-laid in glazed tiles, and a gilded "
     "crest along the ridge",
     """d.glazed(b, 0, 0.45, 'x', 1.3, 0.75, U_TOP, 'roof_red_dark')
R = L3['L2']['RAISE']
for sx in (-1, 1):
    d.glazed_pyramid(b, sx * 1.58, 0.05, U_TOP + 0.3 + R + 0.06, 0.41, 0.41, hi[2] + R - 0.16,
                     'roof_red_dark', crest=False)"""),

    ("tavern1", "Tavern1", "Таверна", FUN, "2x1",
     "the violet roof re-laid in glazed tiles and a gilded crest along the ridge",
     """d.glazed(b, 0, 0.04, 'x', 0.94, 0.33, L3['L2']['T_TOP'], 'roof_purple_dark')"""),
    ("church1", "Church1", "Церковь", FUN, "3x3",
     "the nave's and the chapel's roofs and the spire re-laid in glazed tiles, with gilded "
     "crests along the ridges",
     """d.glazed(b, 0, 0.4, 'y', 1.05, 0.66, U_TOP, 'roof_purple_dark')
d.glazed(b, 1.03, 0.55, 'x', 0.37, 0.3, G_TOP + 0.06, 'roof_purple_dark', w=0.05, step=0.16,
         row=0.13)
bell1 = U_TOP + 0.62 + 0.42
d.glazed_pyramid(b, 0, -0.88, bell1 + 0.05, 0.44, 0.44, hi[2] + 0.6 - 0.16, 'roof_purple_dark',
                 crest=False)"""),
    ("theater1", "Theater1", "Театр", FUN, "4x4",
     "the stage's roof and the outer slope of the ring roof re-laid in glazed tiles, and gilded "
     "crests along the stage's ridge and all round the ring's ridge",
     """d.glazed(b, 0, 0.725, 'x', 0.62, 0.375, U_TOP + 0.1, 'roof_purple_dark')
oc, T_TOP = L3['L2']['octagon'], L3['L2']['T_TOP']
r_mid = (1.78 + 1.12) / 2
d.glazed_ring(b, oc(0.0, 0.05, 1.78 + 0.12), oc(0.0, 0.05, r_mid), T_TOP, T_TOP + 0.36,
              'roof_purple_dark')
d.ring_crest(b, oc(0.0, 0.05, r_mid), T_TOP + 0.39)"""),
    ("colosseum1", "Colosseum1", "Коллизей", FUN, "4x6",
     "a gilded crest all round the rim of the velarium and six more gilded statues round the "
     "attic, so that they ring the whole arena",
     """attic = L3['L2']['T_TOP'] + 0.2
d.ring_crest(b, L3['L2']['ellipse'](1.9 - 0.15, 2.88 - 0.15), attic + 0.13, step=0.25)
import math
for a in (0, 50, 130, 180, 230, 310):   # the statues of levels 2 and 3 stand at 90 and 270
    t = math.radians(a)
    sx, sy = (1.9 - 0.15) * math.cos(t), (2.88 - 0.15) * math.sin(t)
    b.cylinder(sx, sy, attic, attic + 0.08, 0.07, 'stone_mid', sides=6)
    b.blob(sx, sy, attic + 0.08, 0.06, 0.22, 'gold', sides=6)
    b.blob(sx, sy, attic + 0.28, 0.035, 0.07, 'gold', sides=5)"""),
    ("townsquare1", "TownSquare1", "Городская площадь", FUN, "2x2",
     "a gold mosaic ring round the basin and gold finials on the four lamps",
     """b.ring(0, 0, 0.035, 0.045, 0.64, 0.7, ['gold', 'plaster_pink'], sides=16)
for sx in (-1, 1):
    for sy in (-1, 1):
        d.finial(b, sx * 0.8, sy * 0.8, 0.76, h=0.1)"""),
    ("flag1", "Flag1", "Флаг", FUN, "1x1",
     "the pole raised by another 0.3 and a gold band round the plinth",
     """b.box(-0.305, 0.305, -0.305, 0.305, 0.115, 0.14, 'gold')"""),
    ("dectree1", "DecTree1", "Декоративное дерево", FUN, "1x1",
     "a second scatter of gilded leaves through the crown and a gold finial at its top",
     """for x, y, z in ((-0.36, 0.08, 1.02), (0.2, 0.3, 1.2), (-0.22, -0.3, 1.34), (0.3, -0.1, 1.5),
                (-0.1, 0.2, 1.68)):
    b.blob(x, y, z, 0.055, 0.08, 'gold', sides=4)
d.finial(b, 0.02, 0, hi[2] - 0.04, h=0.14)"""),
    ("decbush1", "DecBush1", "Декоративный куст", FUN, "1x1",
     "a gold band round the planter's rim and gold balls on the front urns",
     """b.box(-0.446, 0.446, -0.446, 0.446, 0.08, 0.1, 'gold')
for x in (-0.37, 0.37):
    b.ball(x, -0.37, 0.3, 0.035, 0.06, 'gold', sides=6)"""),
    ("decgarden1", "DecGarden1", "Декоративный сад", FUN, "3x1",
     "gold balls on the corners of the hedge border and on the four posts of the rose arches",
     """for x in (-1.41, 1.41):
    for y in (-0.41, 0.41):
        b.ball(x, y, 0.17, 0.04, 0.07, 'gold', sides=6)
for ax in (-0.72, 0.72):
    for ay in (-0.13, 0.13):
        b.ball(ax, ay, 0.5, 0.03, 0.05, 'gold', sides=6)"""),

    ("barracks1", "Barracks1", "Казармы", DEF, "3x3",
     "the quarters' roof re-laid in glazed tiles and a gilded crest along the ridge",
     """d.glazed(b, 0, 0.77, 'x', 1.5, 0.6, U_TOP, 'roof_blue_dark')"""),
    ("gate1", "Gate1", "Ворота", DEF, "1x2",
     "the little roof over the beam and the piers' caps re-laid in glazed tiles, with a gilded "
     "crest; the palisade joins are untouched",
     """d.glazed(b, 0, 0, 'y', 0.56, 0.16, 1.08, 'roof_blue_dark', overhang=0.04, t=0.036, w=0.04,
         step=0.13, row=0.1)
for py in (-0.44, 0.44):
    d.glazed_pyramid(b, 0, py, 1.17, 0.15, 0.11, 1.34, 'roof_blue_dark', w=0.035, step=0.12,
                     row=0.09, crest=False)"""),
    ("tower1", "Tower1", "Башня", DEF, "1x1",
     "the pyramid roof re-laid in glazed tiles; the palisade joins are untouched",
     """d.glazed_pyramid(b, 0, 0, 1.67 + 0.46, 0.5, 0.5, hi[2] + 0.24 - 0.2, 'roof_blue_dark',
                 crest=False)"""),
    ("fortifiedgate1", "FortifiedGate1", "Укреплённые ворота", DEF, "2x2",
     "the four turrets' caps and the watch room's roof re-laid in glazed tiles; the wall joins "
     "are untouched",
     """for sx in (-1, 1):
    for sy in (-1, 1):
        d.glazed_pyramid(b, sx * 0.32, sy * 0.52, 1.38, 0.2, 0.2, 1.68, 'roof_blue_dark',
                         w=0.04, step=0.13, row=0.1, crest=False)
z1 = WALL_H + 0.36 + 0.42
d.glazed_pyramid(b, 0, 0, z1, 0.22, 0.31, z1 + 0.42, 'roof_blue_dark', w=0.045, step=0.14,
                 row=0.11, crest=False)"""),
    ("fortifiedtower1", "FortifiedTower1", "Укреплённая башня", DEF, "2x2",
     "the great pyramid roof re-laid in glazed tiles; the wall joins are untouched",
     """d.glazed_pyramid(b, 0, 0, U_TOP + 0.6 + STOREY + 0.06, 0.84, 0.84, hi[2] + STOREY - 0.2,
                 'roof_blue_dark', crest=False)"""),

    ("quarry1", "Quarry1", "Каменоломня", PROD, "2x2",
     "the tool lean-to re-laid in glazed tiles with a gilded crest, and a second, smaller "
     "obelisk in the yard",
     """d.glazed_shed(b, 'y', -0.36, 0.29, 0.92, 0.64, 0.62, 0.44, 'roof_slate_dark')
L3['obelisk'](b, -0.6, -0.88, h=0.56)"""),
    ("ironmine1", "IronMine1", "Железный рудник", PROD, "2x2",
     "the tool shed's roof re-laid in glazed tiles with a gilded crest",
     """d.glazed_shed(b, 'x', 0.36, 0.96, -0.14, -0.72, 0.66, 0.48, 'roof_slate_dark')"""),
    ("coppermine1", "CopperMine1", "Медный рудник", PROD, "2x2",
     "the ore store's roof re-laid in glazed tiles with a gilded crest",
     """d.glazed_shed(b, 'x', 0.51, 0.97, -0.05, -0.44, 0.64, 0.48, 'roof_slate_dark')"""),
    ("goldmine1", "GoldMine1", "Золотой рудник", PROD, "2x2",
     "the sentry box's roof re-laid in glazed tiles with a gilded crest",
     """d.glazed_shed(b, 'x', 0.46, 0.9, -0.23, -0.64, 0.82, 0.66, 'roof_slate_dark')"""),
    ("coalmine1", "CoalMine1", "Угольная шахта", PROD, "2x2",
     "the winding shed's roof re-laid in glazed tiles with a gilded crest",
     """d.glazed_shed(b, 'x', 0.13, 0.93, 0.06, -0.6, 0.72, 0.5, 'roof_slate_dark')"""),
    ("smelter1", "Smelter1", "Плавильня", PROD, "2x3",
     "the hall's roof re-laid in glazed tiles and a gilded crest along the ridge",
     """d.glazed(b, 0, 0.665, 'y', 0.775, 0.85, G_TOP, 'roof_slate_dark')"""),

    ("farm1", "Farm1", "Ферма", FOOD, "1x1",
     "a gold weathervane on a pole at the back of the field",
     """b.box(-0.32, -0.29, 0.39, 0.42, 0.035, 0.72, 'wood_dark')
d.weathervane(b, -0.305, 0.405, 0.72, h=0.2)"""),
    ("orchard1", "Orchard1", "Фруктовый сад", FOOD, "3x3",
     "the picker's hut's thatch re-laid in glazed tiles with a gilded crest, and golden apples "
     "on the middle tree of the back row",
     """d.glazed(b, -0.96, -0.91, 'x', 0.43, 0.29, G_TOP, 'roof_straw_dark', accent='roof_red',
         w=0.05, step=0.16, row=0.13)
import math
for i in range(6):
    a = math.radians(30 + 60 * i)
    b.blob(0.27 * math.cos(a), 0.95 + 0.27 * math.sin(a), 0.42 + 0.08 * (i % 3), 0.045, 0.075,
           'gold', sides=4)"""),
    ("fisherhut1", "FisherHut1", "Рыбацкая хижина", FOOD, "1x1",
     "the roof re-laid in glazed tiles and a gilded crest along the ridge",
     """d.glazed(b, -0.14, 0.14, 'x', 0.34, 0.22, G_TOP, 'roof_straw_dark', accent='roof_red',
         overhang=0.05, w=0.045, step=0.15, row=0.12)"""),
    ("pigfarm1", "PigFarm1", "Свиноферма", FOOD, "3x4",
     "the sty's roof and the shelter's lean-to re-laid in glazed tiles, with gilded crests",
     """d.glazed(b, 0, 1.4, 'x', 1.39, 0.45, L_TOP, 'roof_straw_dark', accent='roof_red')
d.glazed_shed(b, 'y', -1.9, -1.26, 1.38, 0.76, 0.58, 0.42, 'roof_straw_dark',
              accent='roof_red')"""),
    ("smokehouse1", "Smokehouse1", "Коптильня", FOOD, "1x2",
     "the roof re-laid in glazed tiles and a gilded crest along the ridge",
     """d.glazed(b, 0, 0.45, 'y', 0.52, 0.36, G_TOP, 'roof_straw_dark', accent='roof_red')"""),
    ("windmill1", "Windmill1", "Ветряк", FOOD, "2x2",
     "the thatched cap and the porch roof re-laid in glazed tiles (red diamonds, since gold "
     "vanishes on thatch), a gilded crest on the porch and a gold band round the foot of the cap",
     """d.glazed_shed(b, 'x', -0.3, 0.3, -0.2, -0.66, 0.86, 0.64, 'roof_straw_dark',
              accent='roof_red')
d.glazed_dome(b, 0.0, 0.22, 1.85 + 0.06, 0.42 + 0.1, 0.62, 'roof_straw_dark',
              accent='roof_red', w=0.05, step=0.17, row=0.13)
b.ring(0.0, 0.22, 1.85 + 0.02, 1.85 + 0.06, 0.42 + 0.05, 0.42 + 0.09, ['gold'], sides=8)"""),
    ("baker1", "Baker1", "Пекарня", FOOD, "1x2",
     "the roof re-laid in glazed tiles and a gilded crest along the ridge",
     """d.glazed(b, 0, -0.065, 'y', 0.555, 0.36, L_TOP, 'roof_straw_dark', accent='roof_red')"""),
]

CALL = {"flag1": "b = L3['build'](lo, (hi[0], hi[1], hi[2] + 0.3))   # the pole rises 0.3 again"}

TEMPLATE = '''"""{obj}-lvl4 -- the {en} ({ru}) at level 4.

Footprint {fp} (level 1's). Level 3 ({stem}_lvl3.build) with {what}. Level 4's mark across the set is the glazed roof: a
lattice of the category's dark shade and gold diamonds over the tiles. Models only -- the game
stops at level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "{obj}-lvl4"
L3 = kit.borrow("{stem}_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    {call}
{body}
    return b


kit.run(OBJ, build, source_name="{obj}-lvl1", offset=(LVL4_DX, 0, 0),
        collection="{coll}")
'''

EN = {"castle1": "Town Hall", "well1": "Well", "manor1": "Manor", "firebrigade1": "Fire Brigade",
      "healerhouse1": "Healer's House", "laboratory1": "Laboratory", "barn1": "Barn",
      "granary1": "Larder", "bigbarn1": "Big Barn", "biggranary1": "Big Larder",
      "bigwarehouse1": "Big Warehouse", "treasury1": "Treasury", "bigtreasury1": "Big Treasury",
      "tavern1": "Tavern", "church1": "Church", "theater1": "Theatre", "colosseum1": "Colosseum",
      "townsquare1": "Town Square", "flag1": "Flag", "dectree1": "Decorative Tree",
      "decbush1": "Decorative Bush", "decgarden1": "Decorative Garden", "barracks1": "Barracks",
      "gate1": "Gate", "tower1": "Tower", "fortifiedgate1": "Fortified Gate",
      "fortifiedtower1": "Fortified Tower", "quarry1": "Quarry", "ironmine1": "Iron Mine",
      "coppermine1": "Copper Mine", "goldmine1": "Gold Mine", "coalmine1": "Coal Mine",
      "smelter1": "Smelter", "farm1": "Farm", "orchard1": "Orchard", "fisherhut1": "Fisher's Hut",
      "pigfarm1": "Pig Farm", "smokehouse1": "Smokehouse", "windmill1": "Windmill",
      "baker1": "Bakery"}

import textwrap  # noqa: E402

for stem, obj, ru, coll, fp, what, code in T:
    body = textwrap.indent(code, "    ")
    txt = TEMPLATE.format(obj=obj, en=EN[stem], ru=ru, fp=fp, stem=stem,
                          what=what, coll=coll,
                          call=CALL.get(stem, "b = L3['build'](lo, hi)"), body=body)
    # rewrap the docstring's "Level 3 (...) with ..." paragraph to the repo's width
    head, rest = txt.split("\n\nFootprint ", 1)
    para, tail = rest.split('\n"""', 1)
    para = "Footprint " + para
    para = "\n".join(textwrap.wrap(" ".join(para.split()), 96, break_on_hyphens=False))
    txt = head + "\n\n" + para + '\n"""' + tail
    with open(os.path.join(OUT, stem + "_lvl4.py"), "w", encoding="utf-8", newline="\n") as fh:
        fh.write(txt)
print(len(T), "written")
