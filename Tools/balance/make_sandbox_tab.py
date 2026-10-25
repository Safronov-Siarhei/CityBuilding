"""Builds Assets/_Project/Balance/sandbox.csv: the sandbox's building, people and hero numbers.

    python -I make_sandbox_tab.py <repo root>
"""
import csv
import io
import os
import sys

root = sys.argv[1]
bal = os.path.join(root, 'Assets', '_Project', 'Balance')

with open(os.path.join(bal, 'buildings.csv'), encoding='utf-8') as f:
    brows = list(csv.reader(f))
head = brows[0]
buildings = [dict(zip(head, r)) for r in brows[1:] if r and r[0] and r[0][0].isalpha() and r[0] == r[0].strip() and ' ' not in r[0]]

with open(os.path.join(bal, 'invasion.csv'), encoding='utf-8') as f:
    irows = list(csv.reader(f))
ki = [i for i, r in enumerate(irows) if r and r[0] == 'key'][0]
inv_keys = [(r[0], r[1], r[2] if len(r) > 2 else '') for r in irows[ki + 1:] if r and r[0]]

MATS = ['wood', 'stone', 'iron', 'coal', 'gold']
COLS = (['building', 'display_name']
        + ['cost_' + m for m in MATS + ['ironbar']]
        + [f'up{n}_{m}' for n in range(2, 6) for m in MATS]
        + ['build_sec'] + [f'build_sec_{n}' for n in range(2, 6)]
        + ['housing_capacity'] + [f'housing_capacity_{n}' for n in range(2, 6)]
        + ['comment'])


def num(v):
    try:
        return float(str(v).replace(',', '.')) if str(v).strip() else 0.0
    except ValueError:
        return 0.0


def secs(total, base, per):
    return max(1, int(round(base + per * total)))


COMMENTS = {
    'Castle': 'Ратуша: ставится сразу в начале игры (build_sec не действует); жителей не даёт -- живут в ней только первые поселенцы',
    'Hovel': 'Дом: даёт жителей до своей вместимости, по одному каждые person_respawn_sec',
    'Cottage': 'Дом: даёт жителей до своей вместимости, по одному каждые person_respawn_sec',
    'Manor': 'Дом: даёт жителей до своей вместимости, по одному каждые person_respawn_sec',
    'Road': 'Кладётся по клетке: стройка 1 сек',
}

out_rows = []
for b in buildings:
    bid = b['id']
    row = {'building': bid, 'display_name': b.get('display_name', '')}
    for m in MATS + ['ironbar']:
        row['cost_' + m] = b.get('cost_' + m, '')
    for n in range(2, 6):
        for m in MATS:
            row[f'up{n}_{m}'] = b.get(f'up{n}_{m}', '')
    total1 = sum(num(b.get('cost_' + m)) for m in MATS + ['ironbar'])
    row['build_sec'] = 1 if bid == 'Road' else (0 if bid == 'Castle' else secs(total1, 4, 0.25))
    for n in range(2, 6):
        tot = sum(num(b.get(f'up{n}_{m}')) for m in MATS)
        row[f'build_sec_{n}'] = secs(tot, 6, 0.15)
    for n, col in enumerate(['housing_capacity'] + [f'housing_capacity_{k}' for k in range(2, 6)]):
        row[col] = 0 if bid == 'Castle' else b.get(col, '')
    row['comment'] = COMMENTS.get(bid, '')
    out_rows.append(row)

HERO_PRICES = [
    ('king_price', 100, 'Песочница, Король: цена в Ратуше, монет (в песочнице все герои покупаются)'),
    ('king_castle_level', 1, 'Песочница, Король: с какого уровня Ратуши можно купить'),
]
OVERRIDE = {'queen_price': 300, 'captain_price': 500}

keys = [
    ('person_respawn_sec', 30, 'Песочница: через сколько секунд дом (Лачуга, Коттедж, Особняк) даёт следующего жителя, пока жителей меньше, чем мест в домах. Ратуша жителей не даёт'),
    ('repair_cost_fraction', '0,4', 'Песочница: ремонт с нуля стоит такую долю цены здания (постройка + оплаченные улучшения, всё кроме монет); ремонт стоит столько x доля потерянной прочности'),
    ('minimap_radius', 40, 'Песочница: сколько метров вокруг взгляда камеры показывает миникарта (половина её ширины)'),
] + HERO_PRICES
hero_prefixes = ('king_', 'queen_', 'captain_', 'hero_', 'skill_')
for k, v, c in inv_keys:
    if not k.startswith(hero_prefixes):
        continue
    if k in OVERRIDE:
        v = OVERRIDE[k]
        c = c.replace('замке, золота', 'Ратуше, монет')
    c = c.replace('Нашествие, как в песочнице:', 'Песочница:').replace('Нашествие', 'Песочница').replace('замка', 'Ратуши').replace('в замке', 'в Ратуше')
    keys.append((k, v, c))

NOTES = [
    'Песочница: стройка, жители и герои на одном листе. Таблица зданий кладётся поверх листа buildings: столбцы, что есть здесь, действуют в песочнице вместо тех, что на buildings; остальное (здоровье, рабочие, склады...) берётся с buildings.',
    'cost_* -- цена постройки, upN_* -- цена улучшения до уровня N (пусто = 0). build_sec -- секунды стройки, build_sec_N -- секунды улучшения до уровня N (подобраны формулой от цены, правь как нужно). housing_capacity -- сколько жителей даёт дом на уровне (пусто = как уровнем ниже).',
    'Ключи ниже заменяют такие же ключи листов economy и invasion в песочнице. Залить в игру: Unity -> CityBuilder -> Balance -> Pull From Google Sheets, затем SetupProject.',
]

buf = io.StringIO()
w = csv.writer(buf, lineterminator='\r\n')
for n in NOTES:
    w.writerow([n])
w.writerow([])
w.writerow(COLS)
for r in out_rows:
    w.writerow([r.get(c, '') for c in COLS])
w.writerow([])
w.writerow(['key', 'value', 'comment'])
for k, v, c in keys:
    w.writerow([k, v, c])
with open(os.path.join(bal, 'sandbox.csv'), 'w', encoding='utf-8', newline='') as f:
    f.write(buf.getvalue())
print(len(out_rows), 'buildings', len(keys), 'keys')
