"""Verified category transactions; category labels never change physical item identity.

CategoriesTable rows with Grouping == 'Primary category' own the registry.
Only the explicit dependency map below follows the catalog classification.
Variants' Unity category and all gameplay/recipe semantics remain independent.
"""
import json
import re
from collections import Counter
from datetime import datetime, timezone
from io import BytesIO
from uuid import UUID
from zipfile import ZipFile
from xml.etree import ElementTree as ET
from xml.sax.saxutils import escape

import openpyxl
from workbook_edits import EditError, LOCK, NS, atomic_write, digest, master_items, parse_sheets

DEPENDENCIES = {
    "Canister concepts": [("Concept ID", "Category")],
    "Item uses": [("Item ID", "Category")],
    "Material routes": [("Item ID", "Category")],
    "Crafting browser": [("Input item ID", "Input category"), ("Output item ID", "Output category")],
}
CELL = r'<(?:\w+:)?c\b[^>]*\br="([A-Z]+\d+)"[^>]*(?:/>|>.*?</(?:\w+:)?c>)'


def registry(rows):
    """The Categories table deliberately mixes primary categories and families."""
    header = next((i for i, row in enumerate(rows) if row and row[0] == "Category"), None)
    if header is None:
        raise EditError("Category registry is missing its header.")
    names = [row[0] for row in rows[header + 1:] if len(row) >= 5 and row[4] == "Primary category" and row[0]]
    if not names or len(names) != len(set(names)) or any(not isinstance(n, str) for n in names):
        raise EditError("Category registry is empty or contains duplicate categories.")
    return sorted(names)


def parts_for(archive):
    wb = ET.fromstring(archive.read("xl/workbook.xml"))
    rels = {e.attrib["Id"]: e.attrib["Target"] for e in ET.fromstring(archive.read("xl/_rels/workbook.xml.rels"))}
    return {s.attrib["name"]: (rels[s.attrib['{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id']].lstrip('/')
            if rels[s.attrib['{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id']].startswith('/')
            else 'xl/' + rels[s.attrib['{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id']])
            for s in wb.find('s:sheets', NS)}


def patch_cells(raw, changes):
    prefix = re.search(r'<((?:\w+:)?)worksheet\b', raw).group(1)
    cells = {m.group(1): m.group() for m in re.finditer(CELL, raw, re.S)}
    for address, value in changes.items():
        old = cells.get(address)
        if old:
            opening = old[:old.index('>')].rstrip('/')
            opening = re.sub(r'\s+t="[^"]*"', '', opening)
        else:
            opening = f'<{prefix}c r="{address}"'
        formula = re.search(r'<(?:\w+:)?f\b[^>]*(?:/>|>.*?</(?:\w+:)?f>)', old or '', re.S)
        if formula:
            kind = '' if isinstance(value, (int, float)) else ' t="str"'
            cell = opening + kind + '>' + formula.group() + f'<{prefix}v>{escape(str(value))}</{prefix}v></{prefix}c>'
        elif isinstance(value, (int, float)):
            cell = opening + f'><{prefix}v>{value}</{prefix}v></{prefix}c>'
        else:
            cell = opening + f' t="inlineStr"><{prefix}is><{prefix}t xml:space="preserve">{escape(str(value))}</{prefix}t></{prefix}is></{prefix}c>'
        if old:
            raw = raw.replace(old, cell, 1)
        else:
            number = re.search(r'\d+', address).group()
            row = re.search(rf'<(?:\w+:)?row\b[^>]*\br="{number}"[^>]*>.*?</(?:\w+:)?row>', raw, re.S)
            if row:
                raw = raw.replace(row.group(), re.sub(r'</(?:\w+:)?row>$', cell + f'</{prefix}row>', row.group()), 1)
            else:
                raw = raw.replace(f'</{prefix}sheetData>', f'<{prefix}row r="{number}">{cell}</{prefix}row></{prefix}sheetData>')
    return raw


def build_move(snapshot, item_id, destination, expected_from):
    sheets = parse_sheets(snapshot)
    choices = registry(sheets.get('Categories', []))
    if destination not in choices:
        raise EditError("Choose a registered primary category.", 400)
    master = master_items(sheets)
    ids = [r[0] for _, r in master]
    if len(ids) != len(set(ids)):
        raise EditError("Duplicate item IDs; no changes were made.")
    found = [(n, r) for n, r in master if r[0] == item_id]
    if len(found) != 1:
        raise EditError("This item is no longer in the spreadsheet.", 404)
    row_number, item = found[0]
    previous = item[3]
    if previous != expected_from:
        raise EditError("This item's category changed. Reload before moving it.")
    if previous == destination:
        return snapshot, {"itemId": item_id, "itemName": item[1], "fromCategory": previous, "toCategory": destination, "noop": True}
    if any(r[3] not in choices for _, r in master):
        raise EditError("The category registry does not cover every master item.")
    changes = {"Items": {f'D{row_number}': destination}}
    header = sheets['Items'][4]
    def set_cell(sheet, number, column, value):
        changes.setdefault(sheet, {})[f'{openpyxl.utils.get_column_letter(column + 1)}{number}'] = value
    search = list(item)
    search[3] = destination
    if 'Search terms' in header:
        # Mirrors the current AE formula; no rewriting of aliases or prose.
        terms = [search[header.index(k)] for k in ['Item ID', 'Item name', 'Category', 'Subtype', 'Item type', 'Processing stage', 'Origin family', 'Primary use', 'Material family', 'Physical form', 'Design status', 'Search aliases', 'Tier', 'Finished-item input tier', 'Crafting role']]
        set_cell('Items', row_number, header.index('Search terms'), '; '.join(str(v) if v is not None else '' for v in terms).lower())
    for sheet, mappings in DEPENDENCIES.items():
        rows = sheets.get(sheet, [])
        if len(rows) < 5:
            continue
        h = rows[4]
        for key, category in mappings:
            if key not in h or category not in h:
                continue
            ki, ci = h.index(key), h.index(category)
            for n, row in enumerate(rows[5:], 6):
                if len(row) > ki and row[ki] == item_id:
                    set_cell(sheet, n, ci, destination)
    counts = Counter(r[3] for _, r in master)
    counts[previous] -= 1
    counts[destination] += 1
    for n, row in enumerate(sheets['Categories'], 1):
        if len(row) >= 5 and row[4] == 'Primary category':
            set_cell('Categories', n, 1, counts[row[0]])
    # A contiguous validation range avoids moving Taxonomy's fixed-address groups.
    for n, row in enumerate(sheets['Categories'], 1):
        if len(row) > 6 and row[6] is not None and row[6] not in choices + ['Inventory category choices']:
            raise EditError("Categories column G is occupied; category choices cannot be installed safely.")
    changes.setdefault('Categories', {})['G5'] = 'Inventory category choices'
    for n, choice in enumerate(choices, 6):
        changes['Categories'][f'G{n}'] = choice
    with ZipFile(BytesIO(snapshot)) as archive:
        parts = parts_for(archive)
        replacements = {}
        for sheet, edits in changes.items():
            raw = patch_cells(archive.read(parts[sheet]).decode('utf-8'), edits)
            if sheet == 'Items':
                # Replace only the Category column's list validation.
                def validation(match):
                    node = match.group()
                    sqref = re.search(r'\bsqref="([^"]*)"', node)
                    if sqref and sqref.group(1) == 'D6:D1005':
                        node = re.sub(r'(<(?:\w+:)?formula1>).*?(</(?:\w+:)?formula1>)', r'\g<1>InventoryCategoryChoices\g<2>', node, flags=re.S)
                    return node
                raw = re.sub(r'<(?:\w+:)?dataValidation\b[^>]*>.*?</(?:\w+:)?dataValidation>', validation, raw, flags=re.S)
            if sheet == 'Categories':
                raw = re.sub(r'(<(?:\w+:)?dimension\b[^>]*\bref=")[^"]*', rf'\g<1>A1:G{max(len(sheets[sheet]), len(choices)+5)}', raw)
            replacements[parts[sheet]] = raw.encode('utf-8')
        wb = archive.read('xl/workbook.xml').decode('utf-8')
        prefix = re.search(r'<((?:\w+:)?)workbook\b', wb).group(1)
        name = f'<{prefix}definedName name="InventoryCategoryChoices">\'Categories\'!$G$6:$G${len(choices)+5}</{prefix}definedName>'
        if re.search(r'<(?:\w+:)?definedName\b[^>]*\bname="InventoryCategoryChoices"', wb):
            wb = re.sub(r'<(?:\w+:)?definedName\b[^>]*\bname="InventoryCategoryChoices"[^>]*>.*?</(?:\w+:)?definedName>', name, wb, flags=re.S)
        elif re.search(r'</(?:\w+:)?definedNames>', wb):
            wb = re.sub(r'</(?:\w+:)?definedNames>', lambda m: name + m.group(), wb, count=1)
        else:
            # definedNames belongs before calcPr, after externalReferences/sheets.
            wb = re.sub(r'<(?:\w+:)?calcPr\b', f'<{prefix}definedNames>{name}</{prefix}definedNames><{prefix}calcPr', wb, count=1) if re.search(r'<(?:\w+:)?calcPr\b', wb) else wb.replace(f'</{prefix}workbook>', f'<{prefix}definedNames>{name}</{prefix}definedNames></{prefix}workbook>')
        replacements['xl/workbook.xml'] = wb.encode('utf-8')
        out = BytesIO()
        with ZipFile(out, 'w') as result:
            for info in archive.infolist():
                result.writestr(info, replacements.get(info.filename, archive.read(info.filename)))
    candidate = out.getvalue()
    verify_move(snapshot, candidate, changes)
    return candidate, {"itemId": item_id, "itemName": item[1], "fromCategory": previous, "toCategory": destination, "noop": False}


def verify_move(before, after, changes):
    """Verify untouched bytes and changed values/formulas/styles without full exports."""
    try:
        with ZipFile(BytesIO(before)) as original, ZipFile(BytesIO(after)) as candidate:
            assert original.namelist() == candidate.namelist(), 'archive members'
            assert candidate.testzip() is None, 'ZIP integrity'
            parts = parts_for(original)
            allowed_parts = {parts[sheet] for sheet in changes} | {'xl/workbook.xml'}
            for member in original.namelist():
                if member not in allowed_parts:
                    assert original.read(member) == candidate.read(member), member
            for sheet, edits in changes.items():
                old_raw = original.read(parts[sheet]).decode('utf-8')
                new_raw = candidate.read(parts[sheet]).decode('utf-8')
                ET.fromstring(new_raw)
                old = {m.group(1): m.group() for m in re.finditer(CELL, old_raw, re.S)}
                new = {m.group(1): m.group() for m in re.finditer(CELL, new_raw, re.S)}
                assert set(new) == set(old) | set(edits), (sheet, 'cell addresses')
                for address, value in old.items():
                    if address not in edits:
                        assert new[address] == value, (sheet, address, 'unchanged cell')
                for address, expected in edits.items():
                    def node(raw):
                        prefixes = set(re.findall(r'<(?:/)?(\w+):', raw))
                        namespaces = ''.join(f' xmlns:{p}="{NS["s"]}"' for p in prefixes)
                        return ET.fromstring(f'<root xmlns="{NS["s"]}"{namespaces}>' + raw + '</root>')[0]
                    cell = node(new[address])
                    if address in old:
                        previous = node(old[address])
                        assert {k:v for k,v in previous.attrib.items() if k!='t'} == {k:v for k,v in cell.attrib.items() if k!='t'}, (sheet,address,'style/attributes')
                        old_formula = re.search(r'<(?:\w+:)?f\b[^>]*(?:/>|>.*?</(?:\w+:)?f>)', old[address], re.S)
                        new_formula = re.search(r'<(?:\w+:)?f\b[^>]*(?:/>|>.*?</(?:\w+:)?f>)', new[address], re.S)
                        assert (old_formula.group() if old_formula else None) == (new_formula.group() if new_formula else None), (sheet,address,'formula')
                    actual = (''.join(cell.find('s:is', NS).itertext()) if cell.attrib.get('t') == 'inlineStr'
                              else cell.find('s:v', NS).text)
                    assert actual == str(expected), (sheet,address,'value/cache')
            wb = ET.fromstring(candidate.read('xl/workbook.xml'))
            assert any(n.attrib.get('name') == 'InventoryCategoryChoices' for n in wb.findall('s:definedNames/s:definedName', NS))
            items = ET.fromstring(candidate.read(parts['Items']))
            assert any(n.attrib.get('sqref') == 'D6:D1005' and n.find('s:formula1', NS).text == 'InventoryCategoryChoices' for n in items.findall('s:dataValidations/s:dataValidation', NS))
    except (AssertionError, ET.ParseError, AttributeError) as error:
        raise EditError(f"Category verification failed; no workbook changes were saved: {error}") from error


def operation_id(value):
    try:
        if str(UUID(value)) != value:
            raise ValueError()
    except (ValueError, TypeError, AttributeError):
        raise EditError("A valid category operation ID is required.", 400)
    return value


def operation_status(workbook, backups, op_id):
    operation_id(op_id)
    with LOCK:
        done = backups / f'category-{op_id}.committed.json'
        pending = backups / f'category-{op_id}.prepared.json'
        if done.exists():
            return json.loads(done.read_text(encoding='utf-8'))
        if not pending.exists():
            return {"status": "not-found", "operationId": op_id}
        receipt = json.loads(pending.read_text(encoding='utf-8'))
        current = digest(workbook.read_bytes())
        if current == receipt['resultHash']:
            receipt['status'] = 'committed'
            atomic_write(done, json.dumps(receipt, indent=2).encode('utf-8'))
        else:
            receipt['status'] = 'not-applied' if current == receipt['sourceHash'] else 'uncertain'
        return receipt


def move(workbook, backups, body):
    op_id = operation_id(body.get('operationId'))
    with LOCK:
        existing = operation_status(workbook, backups, op_id)
        if existing['status'] != 'not-found':
            if any(existing.get(k) != body.get(k) for k in ['itemId', 'fromCategory', 'toCategory', 'sourceHash', 'undoOf']):
                raise EditError("Operation ID was already used for a different move.")
        if existing['status'] not in {'not-found', 'not-applied'}:
            if existing['status'] != 'committed':
                raise EditError("Previous save outcome is uncertain. Reload and inspect the item before another move.")
            return existing
        snapshot = workbook.read_bytes()
        if digest(snapshot) != body.get('sourceHash'):
            raise EditError("Spreadsheet changed. Reload before moving this item.")
        if body.get('undoOf'):
            original = operation_status(workbook, backups, body['undoOf'])
            if (original['status'] != 'committed' or original.get('noop') or original.get('undoOf')
                    or original.get('itemId') != body.get('itemId') or original.get('toCategory') != body.get('fromCategory')
                    or original.get('fromCategory') != body.get('toCategory')):
                raise EditError("This move can no longer be undone. Reload and inspect the current category.")
        candidate, receipt = build_move(snapshot, body.get('itemId'), body.get('toCategory'), body.get('fromCategory'))
        receipt.update(operationId=op_id, undoOf=body.get('undoOf'), sourceHash=digest(snapshot), resultHash=digest(candidate),
                       changedAt=datetime.now(timezone.utc).isoformat(), status='prepared')
        backups.mkdir(parents=True, exist_ok=True)
        backup = backups / (digest(snapshot) + '.xlsx')
        if not backup.exists():
            backup.write_bytes(snapshot)
        prepared = backups / f'category-{op_id}.prepared.json'
        if prepared.exists():
            original_receipt = json.loads(prepared.read_text(encoding='utf-8'))
            if original_receipt['resultHash'] != receipt['resultHash']:
                raise EditError("The prepared operation no longer matches this workbook.")
            receipt = original_receipt
        else:
            atomic_write(prepared, json.dumps(receipt, indent=2).encode('utf-8'))
        if candidate != snapshot:
            atomic_write(workbook, candidate, receipt['sourceHash'])
        receipt['status'] = 'committed'
        try:
            atomic_write(backups / f'category-{op_id}.committed.json', json.dumps(receipt, indent=2).encode('utf-8'))
        except OSError:
            pass  # Prepared record + result hash reconcile a lost response/journal write.
        return receipt
