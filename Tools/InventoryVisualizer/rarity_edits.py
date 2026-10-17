"""Guarded rarity assignments; change only the selected Items.Rarity cell."""
import json
import re
from datetime import datetime, timezone
from io import BytesIO
from zipfile import ZipFile

from openpyxl.utils import get_column_letter
import category_edits
from workbook_edits import EditError, LOCK, atomic_write, digest, master_items, parse_sheets

RARITIES = ['Junk', 'Common', 'Uncommon', 'Rare', 'Epic', 'Legendary']


def build_rarity(snapshot, item_id, destination, expected_from):
    if destination not in ['', *RARITIES]:
        raise EditError('Choose a listed item rarity.', 400)
    sheets = parse_sheets(snapshot)
    master = master_items(sheets)
    if len({row[0] for _, row in master}) != len(master):
        raise EditError('Duplicate item IDs; no changes were made.')
    found = [(number, row) for number, row in master if row[0] == item_id]
    if len(found) != 1:
        raise EditError('This item no longer exists. Reload the spreadsheet.', 404)
    number, item = found[0]
    headers = sheets['Items'][4]
    if 'Rarity' not in headers:
        raise EditError('The master spreadsheet has no Rarity field.')
    column = headers.index('Rarity')
    old = item[column] or ''
    if old != expected_from:
        raise EditError('This item’s rarity changed. Reload before saving.')
    receipt = dict(itemId=item_id, itemName=item[1], fromRarity=old,
                   toRarity=destination, noop=old == destination)
    if old == destination:
        return snapshot, receipt
    changes = {'Items': {f'{get_column_letter(column + 1)}{number}': destination}}
    result = BytesIO()
    with ZipFile(BytesIO(snapshot)) as archive, ZipFile(result, 'w') as output:
        part = category_edits.parts_for(archive)['Items']
        raw = archive.read(part).decode('utf-8')
        # A cache-only write would be replaced when Excel recalculates a formula.
        cell = next((m.group() for m in re.finditer(category_edits.CELL, raw, re.S)
                     if m.group(1) in changes['Items']), '')
        if re.search(r'<(?:\w+:)?f\b', cell):
            raise EditError('Rarity is calculated by a formula. Edit the spreadsheet formula first.')
        updated = category_edits.patch_cells(raw, changes['Items']).encode('utf-8')
        for info in archive.infolist():
            output.writestr(info, updated if info.filename == part else archive.read(info.filename))
    candidate = result.getvalue()
    category_edits.verify_move(snapshot, candidate, changes, category_contract=False)
    receipt['changedCells'] = {'Items': 1}
    return candidate, receipt


def operation_status(workbook, backups, operation_id):
    return category_edits.operation_status(workbook, backups / 'Rarity', operation_id)


def update(workbook, backups, body):
    operation_id = category_edits.operation_id(body.get('operationId'))
    with LOCK:
        existing = operation_status(workbook, backups, operation_id)
        if existing['status'] != 'not-found':
            if any(existing.get(key) != body.get(key) for key in ['itemId', 'fromRarity', 'toRarity', 'sourceHash']):
                raise EditError('Operation ID already belongs to another rarity edit.')
            if existing['status'] == 'committed':
                return existing
            if existing['status'] != 'not-applied':
                raise EditError('Previous save is uncertain. Reload and inspect the rarity.')
        snapshot = workbook.read_bytes()
        if digest(snapshot) != body.get('sourceHash'):
            raise EditError('Spreadsheet changed. Reload before saving this rarity.')
        candidate, receipt = build_rarity(snapshot, body.get('itemId'), body.get('toRarity'), body.get('fromRarity'))
        receipt.update(operationId=operation_id, sourceHash=digest(snapshot), resultHash=digest(candidate),
                       changedAt=datetime.now(timezone.utc).isoformat(), status='prepared')
        directory = backups / 'Rarity'
        directory.mkdir(parents=True, exist_ok=True)
        backup = directory / (digest(snapshot) + '.xlsx')
        if not backup.exists():
            backup.write_bytes(snapshot)
        prepared = directory / f'category-{operation_id}.prepared.json'
        if prepared.exists():
            prior = json.loads(prepared.read_text(encoding='utf-8'))
            if prior['resultHash'] != receipt['resultHash']:
                raise EditError('Prepared rarity edit no longer matches the workbook.')
            receipt = prior
        else:
            atomic_write(prepared, json.dumps(receipt, indent=2).encode('utf-8'))
        if candidate != snapshot:
            atomic_write(workbook, candidate, receipt['sourceHash'])
        receipt['status'] = 'committed'
        try:
            atomic_write(directory / f'category-{operation_id}.committed.json', json.dumps(receipt, indent=2).encode('utf-8'))
        except OSError:
            pass  # The prepared result hash reconciles an interrupted journal write.
        return receipt
