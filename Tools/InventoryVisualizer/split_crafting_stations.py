"""Register crafting stations and reclassify existing items without broad XLSX export.

Prepare, author plan cells with Artifact Tool, then apply. All production writes
are hash guarded; the shared category mover maintains linked caches and choices.
"""
import argparse
import hashlib
import json
from collections import Counter
from io import BytesIO

import openpyxl
import split_equipment as migration
from category_edits import build_move
from workbook_edits import atomic_write

CATEGORY = 'Crafting stations'
STATIONS = {
    'proposal.forging.hearth': 'Complete workshop heat source for metal forging.',
    'proposal.audit.controlled_heating_firing_kiln': 'Complete stationary firing facility; power, controls and atmosphere remain unresolved.',
    'proposal.fire.firebox': 'Complete managed cooking heat source, rather than a vessel or handling tool.',
}
DEFINITION = 'Complete placed facilities that provide a crafting, firing, forging or cooking work site. Hand tools, vessels and auxiliary forming fixtures stay in Tools; fitted upgrades and unfinished components retain their own categories. Placement and recipes remain design proposals.'
TOOLS = 'Active-use harvesting, traversal, service and fabrication equipment, including hand tools, reusable process vessels and auxiliary forming fixtures. Complete crafting heat sources and facilities are Crafting stations. Transport machines remain here; category does not establish portability.'


def prepare():
    migration.OUT.mkdir(parents=True, exist_ok=True)
    snapshot = migration.SOURCE.read_bytes()
    (migration.OUT / 'before.xlsx').write_bytes(snapshot)
    book = openpyxl.load_workbook(BytesIO(snapshot), data_only=True)
    rows = list(book['Items'].values)
    live = {r[0]: r for r in rows[5:] if r[0]}
    assert STATIONS.keys() <= live.keys(), 'Expected station missing; review current roster'
    assert not any(r[3] == CATEGORY for r in live.values()), 'Already migrated'
    changes = []

    def change(sheet, address, value, template=None, formula=None):
        changes.append(dict(sheet=sheet, address=address, value=value,
                            cachedOnly=False, template=template,
                            **({'formula': formula} if formula else {})))

    categories = list(book['Categories'].values)
    taxonomy = list(book['Taxonomy'].values)
    assert not any(r[0] == CATEGORY for r in categories), 'Category already registered'
    cr = next(n for n, r in enumerate(categories, 1) if r[0] == 'Tools')
    tr = next(n for n, r in enumerate(taxonomy, 1) if r[:2] == ('Category', 'Tools'))
    change('Categories', f'C{cr}', TOOLS)
    change('Taxonomy', f'C{tr}', TOOLS)
    new_cr, new_tr = len(categories) + 1, len(taxonomy) + 1
    for i, value in enumerate([CATEGORY, 0, DEFINITION, 'Author-requested station category; organization only.', 'Primary category']):
        change('Categories', f'{migration.col(i)}{new_cr}', value, f'{migration.col(i)}{cr}',
               f'COUNTIF(Items!$D$6:$D$1005,A{new_cr})' if i == 1 else None)
    for i, value in enumerate(['Category', CATEGORY, DEFINITION]):
        change('Taxonomy', f'{migration.col(i)}{new_tr}', value, f'{migration.col(i)}{tr}')
    # Existing conceptual planning row must no longer imply that stations have
    # no catalog representation. Do not invent missing world-object item IDs.
    pr = next(n for n, r in enumerate(categories, 1) if r[0] == 'Workstations and storage')
    change('Categories', f'D{pr}', 'Planning group overlaps Crafting stations. Existing workbench, dust refinery, Ironstone sorter and camp chest are world objects; no new inventory definitions or recipes are added by classification.')
    reviewed = []
    for item in live.values():
        if item[3] in ('Tools', 'Workshop upgrades') or item[0] in STATIONS or 'kiln' in str(item[1]).lower():
            reason = STATIONS.get(item[0])
            if not reason:
                reason = ('Fitted working part, not a complete station.' if item[3] == 'Workshop upgrades' else
                          'Unfinished station component, not a complete station.' if item[3] == 'Crafting components' else
                          'Reusable tool, vessel, auxiliary fixture or noncrafting equipment; not a complete crafting facility.')
            reviewed.append(dict(id=item[0], name=item[1], before=item[3],
                                 category=CATEGORY if item[0] in STATIONS else item[3], reason=reason))
    plan = dict(sourceHash=hashlib.sha256(snapshot).hexdigest(), changes=changes,
                counts={CATEGORY: 0}, reviewed=reviewed,
                stations=[dict(id=k, name=live[k][1], previous=live[k][3]) for k in STATIONS],
                extendTables=dict(Categories=new_cr, Taxonomy=new_tr))
    (migration.OUT / 'plan.json').write_text(json.dumps(plan, indent=2), encoding='utf-8')
    book.close()
    print(f'Prepared registry and reviewed {len(reviewed)} items; 3 stations selected.')


def apply():
    source = migration.SOURCE
    original = (migration.OUT / 'before.xlsx').read_bytes()
    plan = json.loads((migration.OUT / 'plan.json').read_text(encoding='utf-8'))
    assert hashlib.sha256(source.read_bytes()).hexdigest() == plan['sourceHash'], 'Workbook changed; prepare again'
    audit_path = migration.ROOT / 'Docs/Design/Items/CRAFTING_STATION_CLASSIFICATION.json'
    assert not audit_path.exists(), 'Audit already exists'
    # Construct and verify the complete transaction locally before replacing
    # production once. The shared mover verifies every untouched cell and part.
    candidate_path = migration.OUT / 'candidate.xlsx'
    candidate_path.write_bytes(original)
    migration.SOURCE = candidate_path
    try:
        migration.apply()
    finally:
        migration.SOURCE = source
    candidate = candidate_path.read_bytes()
    for station in plan['stations']:
        candidate, _ = build_move(candidate, station['id'], CATEGORY, station['previous'])
    before = openpyxl.load_workbook(BytesIO(original), data_only=False)
    after = openpyxl.load_workbook(BytesIO(candidate), data_only=False)
    assert [r[0] for r in list(before['Items'].values)[5:]] == [r[0] for r in list(after['Items'].values)[5:]]
    assert {c.coordinate: c.value for row in before['Items'] for c in row if c.data_type == 'f'} == {c.coordinate: c.value for row in after['Items'] for c in row if c.data_type == 'f'}
    for sheet in before:
        for row in sheet:
            for cell in row:
                assert cell.style_id == after[sheet.title][cell.coordinate].style_id, (sheet.title, cell.coordinate)
    before.close(); after.close()
    atomic_write(source, candidate, plan['sourceHash'])
    audit = dict(date='2026-10-15', definition=DEFINITION,
                 sourceHashBefore=plan['sourceHash'], sourceHashAfter=hashlib.sha256(candidate).hexdigest(),
                 reviewedItems=plan['reviewed'], movedIds=list(STATIONS),
                 boundary='Catalog organization only. No world identity, streaming, generated objects, gameplay saves, station behavior or recipes changed.')
    atomic_write(audit_path, (json.dumps(audit, indent=2) + '\n').encode())
    # Keep the earlier equipment decision manifest current for its two moves.
    manifest_path = migration.ROOT / 'Docs/Design/Items/EQUIPMENT_CLASSIFICATION.json'
    old = manifest_path.read_bytes()
    manifest = json.loads(old)
    for item in manifest['items']:
        if item['id'] in STATIONS:
            item.update(category=CATEGORY, reason=STATIONS[item['id']])
    manifest['counts'] = dict(Counter(i['category'] for i in manifest['items']))
    manifest['sourceHashAfter'] = audit['sourceHashAfter']
    manifest.setdefault('revisions', []).append(dict(date='2026-10-15', reason='Separate complete crafting facilities from reusable tools.', movedIds=list(STATIONS), sourceHashBefore=plan['sourceHash'], sourceHashAfter=audit['sourceHashAfter']))
    atomic_write(manifest_path, (json.dumps(manifest, indent=2) + '\n').encode(), hashlib.sha256(old).hexdigest())
    (migration.OUT / 'receipt.json').write_text(json.dumps(audit, indent=2), encoding='utf-8')
    print('Verified and applied 3 crafting stations; original item IDs, formulas and styles preserved.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('action', choices=['prepare', 'apply'])
    args = parser.parse_args()
    migration.OUT = migration.ROOT / 'Logs/CraftingStationsCategorySplit'
    prepare() if args.action == 'prepare' else apply()
