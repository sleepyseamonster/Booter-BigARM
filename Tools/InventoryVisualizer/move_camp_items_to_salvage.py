"""Move the full live Camp items category to Salvage and retire its selector."""
import json
import re
import shutil
from collections import Counter
from io import BytesIO
from zipfile import ZipFile

import openpyxl
import move_reviewed_tools as batch
from category_edits import CELL, parts_for, registry
from workbook_edits import atomic_write, digest, parse_sheets

OUT = batch.migration.ROOT / 'Logs/CampItemsToSalvage20261016'
AUDIT = batch.migration.ROOT / 'Docs/Design/Items/CAMP_ITEMS_SALVAGE_CLASSIFICATION_2026-10-16.json'
REASON = 'Author direction October 16: all Camp items are Salvage; retire the Camp items section. Item identity, condition, tiers, purposes and recipe semantics remain unchanged.'
_prepare = batch.prepare


def prepare():
    _prepare()
    plan = json.loads((OUT / 'plan.json').read_text(encoding='utf-8'))
    original = (OUT / 'before.xlsx').read_bytes()
    book = openpyxl.load_workbook(BytesIO(original), data_only=False)
    changes = {(c['sheet'], c['address']): c for c in plan['changes']}

    def change(sheet, address, value):
        changes[sheet, address] = dict(sheet=sheet, address=address, value=value,
                                       cachedOnly=False, template=None)

    retired = 'Retired primary category: all entries moved to Salvage by author direction October 16, 2026. Historical label only.'
    for n, row in enumerate(book['Categories'].values, 1):
        if row[0] == 'Camp items':
            change('Categories', f'C{n}', retired)
            change('Categories', f'D{n}', REASON)
            change('Categories', f'E{n}', 'Legacy category')
        elif row[0] == 'Salvage':
            change('Categories', f'C{n}', (row[2] or '') + ' Includes all former Camp items as salvage.')
        elif row[0] == 'Crafting stations':
            change('Categories', f'C{n}', 'Non-cooking crafting, firing and forging facilities. The former Camp items Cooking station is classified as Salvage; recipe semantics remain unchanged.')
    for n, row in enumerate(book['Taxonomy'].values, 1):
        if row[:2] == ('Category', 'Camp items'):
            change('Taxonomy', f'C{n}', retired)
        elif row[:2] == ('Category', 'Crafting stations'):
            change('Taxonomy', f'C{n}', 'Non-cooking crafting, firing and forging facilities. Cooking station is classified as Salvage by author direction.')
    choices = [c for c in registry(parse_sheets(original)['Categories']) if c != 'Camp items']
    previous = book.defined_names['InventoryCategoryChoices'].attr_text
    assert previous == f"'Categories'!$G$6:$G${len(choices)+6}"
    assert [book['Categories'][f'G{n}'].value for n in range(6, len(choices)+7)] == sorted(choices + ['Camp items'])
    for n, value in enumerate(choices + [' '], 6):
        change('Categories', f'G{n}', value)
    plan.update(changes=list(changes.values()), previousChoices=previous,
                categoryChoices=f"'Categories'!$G$6:$G${len(choices)+5}",
                retiredChoiceCell=f'G{len(choices)+6}')
    book.close()
    (OUT / 'plan.json').write_text(json.dumps(plan, indent=2), encoding='utf-8')
    shutil.copyfile(batch.migration.ROOT / 'Tools/InventoryVisualizer/stage_category_split.mjs', OUT / 'stage.mjs')


def apply():
    plan = json.loads((OUT / 'plan.json').read_text(encoding='utf-8'))
    original = (OUT / 'before.xlsx').read_bytes()
    assert digest(batch.REVIEW.read_bytes()) == plan['reviewHash']
    source = batch.migration.SOURCE
    candidate_path = OUT / 'candidate.xlsx'
    candidate_path.write_bytes(original)
    batch.migration.SOURCE = candidate_path
    batch.migration.OUT = OUT
    try:
        batch.migration.apply()
    finally:
        batch.migration.SOURCE = source
    staged = candidate_path.read_bytes()
    output = BytesIO()
    with ZipFile(BytesIO(staged)) as archive, ZipFile(output, 'w') as result:
        category_part = parts_for(archive)['Categories']
        for info in archive.infolist():
            raw = archive.read(info.filename)
            if info.filename == 'xl/workbook.xml':
                assert raw.count(plan['previousChoices'].encode()) == 1
                raw = raw.replace(plan['previousChoices'].encode(), plan['categoryChoices'].encode())
            if info.filename == category_part:
                raw = re.sub(CELL, lambda m: '' if m.group(1) == plan['retiredChoiceCell'] else m.group(), raw.decode(), flags=re.S).encode()
            result.writestr(info, raw)
    candidate = output.getvalue()
    before, after = parse_sheets(original), parse_sheets(candidate)
    selected = {i['id'] for i in plan['items']}
    old_items = [r for r in before['Items'][5:] if r[0]]
    new_items = [r for r in after['Items'][5:] if r[0]]
    assert [r[0] for r in old_items] == [r[0] for r in new_items]
    assert all(r[3] == 'Salvage' for r in new_items if r[0] in selected)
    assert not any(r[3] == 'Camp items' for r in new_items)
    assert 'Camp items' not in registry(after['Categories'])
    counts = Counter(r[3] for r in new_items)
    assert all(r[1] == counts[r[0]] for r in after['Categories'] if len(r)>4 and r[4]=='Primary category')
    check = openpyxl.load_workbook(BytesIO(candidate), data_only=False)
    assert check.defined_names['InventoryCategoryChoices'].attr_text == plan['categoryChoices']
    assert check['Categories'][plan['retiredChoiceCell']].value is None
    check.close()
    assert not AUDIT.exists()
    assert digest(batch.REVIEW.read_bytes()) == plan['reviewHash']
    batch.guard.release_guard()
    atomic_write(source, candidate, plan['sourceHash'])
    audit = dict(date='2026-10-16', reason=REASON, items=plan['items'], retiredCategory='Camp items',
                 counts=dict(counts), sourceHashBefore=plan['sourceHash'], sourceHashAfter=digest(candidate),
                 scope='Catalog-only classification. IDs, item properties, recipes and review/art links retained. Deterministic world identity, streaming/unload/reload, generated-object identity, authored placement and persisted runtime deltas are not applicable: no Unity runtime or world data changes.')
    atomic_write(AUDIT, (json.dumps(audit, indent=2)+'\n').encode())
    candidate_path.write_bytes(candidate)
    print(f"Verified {len(selected)} moves; Salvage {counts['Salvage']}; Camp items retired; total {len(new_items)}.")


if __name__ == '__main__':
    batch.OUT = OUT
    batch.AUDIT = AUDIT
    batch.REASON = REASON
    batch.SOURCE_CATEGORY = 'Camp items'
    batch.TARGET_CATEGORY = 'Salvage'
    batch.SELECTED = {r[0] for r in parse_sheets(batch.migration.SOURCE.read_bytes())['Items'][5:] if r[0] and r[3]=='Camp items'}
    assert batch.SELECTED, 'No Camp items remain; do not replay this migration.'
    batch.guard.OUT = OUT
    batch.guard.prepare = prepare
    batch.guard.apply = apply
    batch.guard.run()
    batch.update_manifest()
