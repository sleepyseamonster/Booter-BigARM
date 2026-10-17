"""Catalog renames preserve stable IDs, linked labels and unrelated workbook bytes."""
import re
import json
from datetime import datetime, timezone
from io import BytesIO
from zipfile import ZipFile

from openpyxl.utils import get_column_letter
import category_edits
from workbook_edits import EditError, LOCK, atomic_write, digest, master_items, parse_sheets

NAME_LINKS = {
    'Item uses': [('Item ID', 'Item name')],
    'Material routes': [('Item ID', 'Item name')],
    'Tier review': [('Item ID', 'Item name')],
    'Craft audit': [('Item ID', 'Item name')],
    'Crafting browser': [('Input item ID', 'Input name'), ('Output item ID', 'Output name')],
    'Item relationships': [('Parent item ID', 'Parent item'), ('Related item ID', 'Related item')],
}
SEARCH_FIELDS = ['Item ID', 'Item name', 'Category', 'Subtype', 'Item type', 'Processing stage',
                 'Origin family', 'Primary use', 'Material family', 'Physical form', 'Design status',
                 'Search aliases', 'Tier', 'Finished-item input tier', 'Crafting role']


def normalized_name(destination):
    if (not isinstance(destination, str) or not destination.strip() or len(destination.strip()) > 200
            or any(not (0x20 <= ord(c) <= 0xD7FF or 0xE000 <= ord(c) <= 0xFFFD
                        or 0x10000 <= ord(c) <= 0x10FFFF) for c in destination)):
        raise EditError('Enter an item name of 1–200 characters without line breaks or control characters.', 400)
    return destination.strip()


def build_name(snapshot, item_id, destination, expected_from):
    destination = normalized_name(destination)
    sheets = parse_sheets(snapshot)
    master = master_items(sheets)
    if len({row[0] for _, row in master}) != len(master):
        raise EditError('Duplicate item IDs; no changes were made.')
    found = [(n, row) for n, row in master if row[0] == item_id]
    if len(found) != 1:
        raise EditError('This item no longer exists. Reload the spreadsheet.', 404)
    number, item = found[0]
    headers = sheets['Items'][4]
    old = item[headers.index('Item name')]
    if old != expected_from:
        raise EditError('This item’s name changed. Reload before saving.')
    receipt = dict(itemId=item_id, itemName=destination, fromName=old, toName=destination, noop=old == destination)
    if old == destination:
        return snapshot, receipt
    changes = {}

    def set_cell(sheet, row_number, field, value):
        h = sheets[sheet][4]
        if field in h:
            column = h.index(field)
            changes.setdefault(sheet, {})[f'{get_column_letter(column + 1)}{row_number}'] = value

    set_cell('Items', number, 'Item name', destination)
    updated = dict(zip(headers, item))
    updated['Item name'] = destination
    # Preserve former catalog names as searchable aliases, as in the catalog audits.
    updated['Search aliases'] = '; '.join(dict.fromkeys(v for v in [updated.get('Search aliases'), old] if v))
    set_cell('Items', number, 'Search aliases', updated['Search aliases'])
    set_cell('Items', number, 'Search terms', '; '.join(str(updated.get(k) or '') for k in SEARCH_FIELDS).lower())
    for sheet, mappings in NAME_LINKS.items():
        if sheet not in sheets or len(sheets[sheet]) < 5:
            continue
        h = sheets[sheet][4]
        for key, label in mappings:
            if key not in h or label not in h:
                continue
            for n, row in enumerate(sheets[sheet][5:], 6):
                if row[h.index(key)] == item_id:
                    set_cell(sheet, n, label, destination)
    result = BytesIO()
    with ZipFile(BytesIO(snapshot)) as archive, ZipFile(result, 'w') as output:
        parts = category_edits.parts_for(archive)
        replacements = {}
        for sheet, edits in changes.items():
            raw = archive.read(parts[sheet]).decode('utf-8')
            if sheet == 'Items':
                address = f'{get_column_letter(headers.index("Item name") + 1)}{number}'
                cell = next((m.group() for m in re.finditer(category_edits.CELL, raw, re.S) if m.group(1) == address), '')
                if re.search(r'<(?:\w+:)?f\b', cell):
                    raise EditError('The item name is calculated by a formula. Edit the spreadsheet formula first.')
            replacements[parts[sheet]] = category_edits.patch_cells(raw, edits).encode('utf-8')
        for info in archive.infolist():
            output.writestr(info, replacements.get(info.filename, archive.read(info.filename)))
    candidate = result.getvalue()
    category_edits.verify_move(snapshot, candidate, changes, category_contract=False)
    receipt['changedCells'] = {sheet: len(edits) for sheet, edits in changes.items()}
    return candidate, receipt


def operation_status(workbook, backups, operation_id):
    return category_edits.operation_status(workbook, backups / 'Name', operation_id)


def update(workbook, backups, body):
    body = {**body, 'toName': normalized_name(body.get('toName'))}
    operation_id = category_edits.operation_id(body.get('operationId'))
    with LOCK:
        existing = operation_status(workbook, backups, operation_id)
        if existing['status'] != 'not-found':
            if any(existing.get(key) != body.get(key) for key in ['itemId', 'fromName', 'toName', 'sourceHash']):
                raise EditError('Operation ID already belongs to another name edit.')
            if existing['status'] == 'committed':
                return existing
            if existing['status'] != 'not-applied':
                raise EditError('Previous save is uncertain. Reload and inspect the name.')
        snapshot = workbook.read_bytes()
        if digest(snapshot) != body.get('sourceHash'):
            raise EditError('Spreadsheet changed. Reload before saving this name.')
        candidate, receipt = build_name(snapshot, body.get('itemId'), body.get('toName'), body.get('fromName'))
        receipt.update(operationId=operation_id, sourceHash=digest(snapshot), resultHash=digest(candidate),
                       changedAt=datetime.now(timezone.utc).isoformat(), status='prepared')
        directory = backups / 'Name'
        directory.mkdir(parents=True, exist_ok=True)
        backup = directory / (digest(snapshot) + '.xlsx')
        if not backup.exists():
            backup.write_bytes(snapshot)
        prepared = directory / f'category-{operation_id}.prepared.json'
        if prepared.exists():
            prior = json.loads(prepared.read_text(encoding='utf-8'))
            if prior['resultHash'] != receipt['resultHash']:
                raise EditError('Prepared name edit no longer matches the workbook.')
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
