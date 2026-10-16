"""Move the author's eight marked garments from Accessories to Clothing."""
import hashlib
import json
from collections import Counter
from io import BytesIO

import openpyxl
import split_equipment as migration

IDS = {
    "equipment.cloak", "equipment.jacket", "proposal.fashion.hat",
    "proposal.fashion.husk_garment", "proposal.fashion.fur_garment",
    "proposal.fashion.hide_garment", "proposal.collection.mica_brim_sun_visor",
    "proposal.gear.simple_wraps",
}
CLOTHING = "Ordinary worn garments, fashion clothing, wraps, hats and visors. Protective armor stays in Armor; eyewear stays in Accessories. No new slots or effects."
ACCESSORIES = "Eyewear, carried personal reserves, carriers and supporting utility gear; ordinary garments are classified as Clothing. No new slots or effects."


def main():
    migration.OUT = migration.ROOT / "Logs/ClothingCategorySplit"
    migration.OUT.mkdir(parents=True, exist_ok=True)
    original = migration.SOURCE.read_bytes()
    (migration.OUT / "before.xlsx").write_bytes(original)
    values = openpyxl.load_workbook(BytesIO(original), read_only=False, data_only=True)
    authored = openpyxl.load_workbook(BytesIO(original), read_only=False, data_only=False)
    changes, moved = [], []

    def change(sheet, address, value, cached=False, template=None, formula=None):
        changes.append({"sheet": sheet, "address": address, "value": value,
                        "cachedOnly": cached, "template": template, **({"formula": formula} if formula else {})})
        if not cached:
            authored[sheet][address] = "=" + formula if formula else value

    rows = list(values["Items"].values)
    headers = rows[4]
    for number, row in enumerate(rows[5:], 6):
        if not row or row[0] not in IDS:
            continue
        assert row[3] == "Accessories", f"Unexpected category for {row[0]}"
        moved.append({"id": row[0], "name": row[1]})
        change("Items", f"D{number}", "Clothing")
        basis = headers.index("Classification basis")
        change("Items", f"{migration.col(basis)}{number}", (row[basis] or "") + " Author correction, October 15: Clothing supersedes Accessories. " + CLOTHING)
        search = headers.index("Search terms")
        change("Items", f"{migration.col(search)}{number}", row[search].replace("; accessories;", "; clothing;"), True)
    assert {item['id'] for item in moved} == IDS, "Missing marked garment"
    for sheet in values:
        if sheet.title == "Items":
            continue
        data = list(sheet.values)
        if len(data) < 5:
            continue
        header = data[4]
        for category, key in [("Category", "Item ID"), ("Input category", "Input item ID"), ("Output category", "Output item ID")]:
            if category not in header or key not in header:
                continue
            ci, ki = header.index(category), header.index(key)
            for number, row in enumerate(data[5:], 6):
                if len(row) > max(ci, ki) and row[ki] in IDS:
                    address = f"{migration.col(ci)}{number}"
                    change(sheet.title, address, "Clothing", authored[sheet.title][address].data_type == 'f')
    category_rows = list(values['Categories'].values)
    taxonomy_rows = list(values['Taxonomy'].values)
    cr = next(i + 1 for i, row in enumerate(category_rows) if row and row[0] == 'Accessories')
    tr = next(i + 1 for i, row in enumerate(taxonomy_rows) if len(row) > 1 and row[:2] == ('Category', 'Accessories'))
    remaining = sum(1 for row in rows[5:] if row and len(row) > 3 and row[0] and row[3] == 'Accessories') - len(IDS)
    change('Categories', f'B{cr}', remaining, formula=f'COUNTIF(Items!$D$6:$D$1005,A{cr})')
    change('Categories', f'C{cr}', ACCESSORIES)
    change('Taxonomy', f'C{tr}', ACCESSORIES)
    new_cr, new_tr = len(category_rows) + 1, len(taxonomy_rows) + 1
    for index, value in enumerate(['Clothing', len(IDS), CLOTHING, 'Author-requested clothing category; organization only.', 'Primary category']):
        change('Categories', f'{migration.col(index)}{new_cr}', value, template=f'{migration.col(index)}{cr}',
               formula=f'COUNTIF(Items!$D$6:$D$1005,A{new_cr})' if index == 1 else None)
    for index, value in enumerate(['Category', 'Clothing', CLOTHING]):
        change('Taxonomy', f'{migration.col(index)}{new_tr}', value, template=f'{migration.col(index)}{tr}')
    plan = {'sourceHash': hashlib.sha256(original).hexdigest(), 'items': moved,
            'counts': {'Clothing': len(IDS), 'Accessories': remaining}, 'changes': changes,
            'extendTables': {'Categories': new_cr, 'Taxonomy': new_tr}}
    (migration.OUT / 'plan.json').write_text(json.dumps(plan, indent=2), encoding='utf-8')
    # Full-workbook Artifact Tool import exhausted its heap on the preceding slice.
    # Use the same verified fallback authoring plus targeted preserving merge.
    authored.save(migration.OUT / 'authored.xlsx')
    authored.close(); values.close()
    migration.apply()
    manifest_path = migration.ROOT / 'Docs/Design/Items/EQUIPMENT_CLASSIFICATION.json'
    manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
    for item in manifest['items']:
        if item['id'] in IDS:
            item.update(category='Clothing', reason=CLOTHING)
        elif item['category'] == 'Accessories':
            item['reason'] = ACCESSORIES
    manifest['counts'] = dict(Counter(item['category'] for item in manifest['items']))
    after_hash = hashlib.sha256(migration.SOURCE.read_bytes()).hexdigest()
    manifest.setdefault('revisions', []).append({'date': '2026-10-15', 'reason': 'Author marked eight garments to move from Accessories into Clothing.',
                                                'movedIds': sorted(IDS), 'sourceHashBefore': plan['sourceHash'], 'sourceHashAfter': after_hash})
    manifest['sourceHashAfter'] = after_hash
    manifest_path.write_bytes((json.dumps(manifest, indent=2) + '\n').encode('utf-8'))
    print('Clothing correction complete:', plan['counts'])


if __name__ == '__main__':
    main()
