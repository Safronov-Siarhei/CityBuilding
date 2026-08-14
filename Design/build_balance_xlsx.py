"""Builds the balance workbook the game imports from (units/economy tabs) plus a read-only
summary tab whose formulas show what the numbers actually mean."""
from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment, Border, Side
from openpyxl.utils import get_column_letter

FONT = "Arial"
INPUT_FILL = PatternFill("solid", fgColor="FFF2CC")      # cells the designer edits
DERIVED_FILL = PatternFill("solid", fgColor="EDEDED")    # computed, do not edit
HEADER_FILL = PatternFill("solid", fgColor="2F4F4F")
NOTE_FILL = PatternFill("solid", fgColor="FFFFFF")
HEADER_FONT = Font(name=FONT, bold=True, color="FFFFFF", size=11)
BODY = Font(name=FONT, size=11)
BLUE = Font(name=FONT, size=11, color="0000FF")          # hand-entered input
BLACK = Font(name=FONT, size=11)
TITLE = Font(name=FONT, bold=True, size=13)
NOTE = Font(name=FONT, size=10, italic=True, color="555555")
THIN = Side(style="thin", color="BFBFBF")
BOX = Border(left=THIN, right=THIN, top=THIN, bottom=THIN)

wb = Workbook()

# ---------------------------------------------------------------- units ----
units = wb.active
units.title = "units"

unit_cols = [
    ("id", 12, "Ключ строки. Его читает код — не переименовывать."),
    ("display_name", 16, "Как называется в интерфейсе."),
    ("max_health", 12, "Запас здоровья."),
    ("attack_damage", 14, "Урон за удар."),
    ("attack_interval_sec", 18, "Секунд между ударами."),
    ("attack_range_units", 18, "Дальность удара по юниту, м."),
    ("attack_range_structures", 22, "Дальность удара по строению, м."),
    ("walk_speed", 12, "Скорость ходьбы, м/с."),
    ("engage_radius", 14, "Радиус, в котором ищет цель без приказа, м."),
    ("recruit_coins", 14, "Цена найма в монетах (0 — нельзя нанять)."),
    ("upkeep_coins_per_day", 20, "Содержание, монет в день."),
    ("dps", 10, "ВЫЧИСЛЯЕТСЯ: урон в секунду."),
    ("ttk_vs_militia_sec", 20, "ВЫЧИСЛЯЕТСЯ: за сколько секунд убивает ополченца."),
    ("ttk_vs_orc_sec", 18, "ВЫЧИСЛЯЕТСЯ: за сколько секунд убивает орка."),
    ("coins_per_hp", 14, "ВЫЧИСЛЯЕТСЯ: цена найма за единицу здоровья."),
]

for col, (name, width, comment) in enumerate(unit_cols, start=1):
    cell = units.cell(row=1, column=col, value=name)
    cell.font = HEADER_FONT
    cell.fill = HEADER_FILL
    cell.alignment = Alignment(horizontal="center", vertical="center", wrap_text=True)
    cell.border = BOX
    units.column_dimensions[get_column_letter(col)].width = width
    units.cell(row=2, column=col).comment = None  # placeholder, comments added below

rows = [
    ["militia", "Ополчение", 12, 5, 1.1, 1.4, 2.6, 1.5, 7, 25, 1],
    ["orc", "Орк", 20, 4, 1.2, 1.4, 3, 1.3, 6, 0, 0],
]

for r, values in enumerate(rows, start=2):
    for c, value in enumerate(values, start=1):
        cell = units.cell(row=r, column=c, value=value)
        cell.font = BLUE
        cell.fill = INPUT_FILL
        cell.border = BOX
        if c in (5, 6, 7, 8):
            cell.number_format = "0.0#"

    # Derived: dps, time-to-kill each way, cost per HP. Militia is row 2, orc row 3.
    dps = units.cell(row=r, column=12, value=f"=IFERROR(D{r}/E{r},0)")
    ttk_militia = units.cell(row=r, column=13, value=f"=IFERROR(CEILING($C$2/D{r},1)*E{r},0)")
    ttk_orc = units.cell(row=r, column=14, value=f"=IFERROR(CEILING($C$3/D{r},1)*E{r},0)")
    coins_per_hp = units.cell(row=r, column=15, value=f"=IFERROR(J{r}/C{r},0)")
    for cell in (dps, ttk_militia, ttk_orc, coins_per_hp):
        cell.font = BLACK
        cell.fill = DERIVED_FILL
        cell.border = BOX
        cell.number_format = "0.00"

units.freeze_panes = "B2"
units.cell(row=5, column=1, value="Жёлтые ячейки — правишь ты. Серые считаются формулами: их можно смотреть, но менять бессмысленно.").font = NOTE
units.cell(row=6, column=1, value="Колонки id и display_name читает код. Порядок колонок не важен — импортёр ищет их по названию, лишние колонки игнорирует.").font = NOTE
units.cell(row=7, column=1, value="Добавить нового юнита = добавить строку. Но код узнает о нём только когда для него появится тип в игре.").font = NOTE

# -------------------------------------------------------------- economy ----
economy = wb.create_sheet("economy")
econ_headers = ["key", "value", "comment"]
widths = [40, 12, 70]
for col, (name, width) in enumerate(zip(econ_headers, widths), start=1):
    cell = economy.cell(row=1, column=col, value=name)
    cell.font = HEADER_FONT
    cell.fill = HEADER_FILL
    cell.alignment = Alignment(horizontal="center", vertical="center")
    cell.border = BOX
    economy.column_dimensions[get_column_letter(col)].width = width

econ_rows = [
    ("army_max_size", 20, "Общий предел армии по всем группам"),
    ("raid_interval_seconds", 90, "Через сколько секунд портал шлёт следующий набег"),
    ("raid_base_size", 2, "Размер набега на первый день"),
    ("raid_days_per_extra_raider", 3, "Каждые N дней в набеге на одного орка больше"),
    ("raid_max_size", 8, "Потолок размера набега"),
    ("portal_max_health", 320, "Запас прочности портала"),
    ("defence_attack_interval_seconds", 1, "Как часто бьёт здание с защитой (стена/башня/казармы/ворота)"),
    ("defence_attack_range_meters", 6, "Радиус обстрела здания с защитой"),
    ("day_length_seconds", 120, "Длина игрового дня в реальных секундах"),
    ("coins_per_citizen_per_day_at_max_tax", 0.5, "Монет с жителя в день при налоге 100%"),
    ("decay_per_day_at_level1", 0.02, "Ветхость в день у здания 1 уровня; делится на уровень"),
    ("decay_penalty_threshold", 0.7, "С какой ветхости начинает падать производство"),
    ("min_decay_production_multiplier", 0.5, "Во сколько раз падает производство при 100% ветхости"),
    ("repair_cost_fraction", 0.4, "Доля стоимости постройки за полный ремонт"),
    ("wood_per_tree", 5, "Дерева за одно срубленное дерево вручную"),
    ("stone_per_rock", 4, "Камня за один валун вручную"),
]

for r, (key, value, comment) in enumerate(econ_rows, start=2):
    k = economy.cell(row=r, column=1, value=key)
    v = economy.cell(row=r, column=2, value=value)
    c = economy.cell(row=r, column=3, value=comment)
    k.font = BODY
    v.font = BLUE
    v.fill = INPUT_FILL
    c.font = NOTE
    for cell in (k, v, c):
        cell.border = BOX

economy.freeze_panes = "A2"
economy.cell(row=len(econ_rows) + 3, column=1,
             value="Ключи в колонке key читает код: переименование = потерянная настройка (импорт напишет об этом ошибкой).").font = NOTE

# -------------------------------------------------------------- summary ----
s = wb.create_sheet("сводка")
s.column_dimensions["A"].width = 46
s.column_dimensions["B"].width = 16
s.column_dimensions["C"].width = 62

def title(row, text):
    cell = s.cell(row=row, column=1, value=text)
    cell.font = TITLE
    return row + 1

def line(row, label, formula, comment, fmt="0.00"):
    a = s.cell(row=row, column=1, value=label)
    b = s.cell(row=row, column=2, value=formula)
    c = s.cell(row=row, column=3, value=comment)
    a.font = BODY
    b.font = BLACK
    b.fill = DERIVED_FILL
    b.number_format = fmt
    b.border = BOX
    c.font = NOTE
    return row + 1

r = 1
r = title(r, "Дуэль: ополченец против орка")
r = line(r, "Орк убивает ополченца за, сек", "=units!M3", "Меньшее число выигрывает дуэль.")
r = line(r, "Ополченец убивает орка за, сек", "=units!N2", "")
r = line(r, "Кто выигрывает 1 на 1", '=IF(units!M3<units!N2,"орк","ополченец")', "По задумке — орк.", "General")
r = line(r, "Сколько ополченцев нужно, чтобы успеть", "=CEILING(units!N2/units!M3,1)", "Столько бойцов убивают орка быстрее, чем он одного из них.")
r += 1

r = title(r, "Экономика армии")
r = line(r, "Содержание одного бойца, мон./день", "=units!K2", "")
r = line(r, "Содержание полной армии, мон./день", "=units!K2*INDEX(economy!B:B,MATCH(\"army_max_size\",economy!A:A,0))", "При полном кэпе.")
r = line(r, "Налоговый доход при 20 жителях, мон./день", "=20*INDEX(economy!B:B,MATCH(\"coins_per_citizen_per_day_at_max_tax\",economy!A:A,0))", "При налоге 100%.")
r = line(r, "Хватает ли налога на полную армию", '=IF(B10>=B9,"да","нет")', "Если нет — армию придётся держать меньше кэпа или поднимать экономику.", "General")
r = line(r, "Найм полной армии стоит, монет", "=units!J2*INDEX(economy!B:B,MATCH(\"army_max_size\",economy!A:A,0))", "Разовая трата.")
r += 1

r = title(r, "Штурм портала")
r = line(r, "Прочность портала", "=INDEX(economy!B:B,MATCH(\"portal_max_health\",economy!A:A,0))", "")
r = line(r, "Урон группы из 5 ополченцев, в секунду", "=5*units!L2", "")
r = line(r, "5 ополченцев ломают портал за, сек", "=IFERROR(B15/B16,0)", "Без учёта того, что их будут бить.")
r = line(r, "10 ополченцев ломают портал за, сек", "=IFERROR(B15/(10*units!L2),0)", "")
r += 1

r = title(r, "Набеги")
r = line(r, "Набег на 1-й день, орков", "=INDEX(economy!B:B,MATCH(\"raid_base_size\",economy!A:A,0))", "", "0")
r = line(r, "Набег на 10-й день, орков", "=MIN(INDEX(economy!B:B,MATCH(\"raid_max_size\",economy!A:A,0)),INDEX(economy!B:B,MATCH(\"raid_base_size\",economy!A:A,0))+INT(9/INDEX(economy!B:B,MATCH(\"raid_days_per_extra_raider\",economy!A:A,0))))", "", "0")
r = line(r, "Набег на 30-й день, орков", "=MIN(INDEX(economy!B:B,MATCH(\"raid_max_size\",economy!A:A,0)),INDEX(economy!B:B,MATCH(\"raid_base_size\",economy!A:A,0))+INT(29/INDEX(economy!B:B,MATCH(\"raid_days_per_extra_raider\",economy!A:A,0))))", "", "0")
r = line(r, "Набегов за игровой день", "=INDEX(economy!B:B,MATCH(\"day_length_seconds\",economy!A:A,0))/INDEX(economy!B:B,MATCH(\"raid_interval_seconds\",economy!A:A,0))", "Сколько волн приходит за один день календаря.")
r += 1

r = title(r, "Износ зданий")
r = line(r, "Дней до полного износа, 1 уровень", "=1/INDEX(economy!B:B,MATCH(\"decay_per_day_at_level1\",economy!A:A,0))", "После этого здание разрушается.", "0.0")
r = line(r, "Дней до полного износа, 3 уровень", "=3/INDEX(economy!B:B,MATCH(\"decay_per_day_at_level1\",economy!A:A,0))", "Уровень замедляет износ.", "0.0")
r = line(r, "Реальных минут до полного износа, 1 ур.", "=B27*INDEX(economy!B:B,MATCH(\"day_length_seconds\",economy!A:A,0))/60", "", "0.0")

r += 1
note = s.cell(row=r, column=1, value="Эта вкладка только считает — правь units и economy, числа здесь пересчитаются сами.")
note.font = NOTE

wb.save(r"C:\Users\borei\AppData\Local\Temp\claude\D--Fork-CityBuilding\eb49c351-aa8b-4a23-97fd-0aa9d97ab685\scratchpad\CityBuilding_Balance.xlsx")
print("saved")
