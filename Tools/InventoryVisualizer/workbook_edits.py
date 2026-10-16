"""Narrow OOXML edits: retain unrelated ZIP members, row addresses and styles."""
import hashlib
import json
import os
import re
import tempfile
import threading
from xml.sax.saxutils import escape
from datetime import datetime, timezone
from io import BytesIO
from pathlib import Path
from zipfile import ZipFile
from xml.etree import ElementTree as ET

import openpyxl

LOCK = threading.RLock()
NS = {"s": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}


class EditError(Exception):
    def __init__(self, message, status=409):
        super().__init__(message)
        self.status = status


def digest(data):
    return hashlib.sha256(data).hexdigest()


def review_state(path):
    if not path.exists():
        return {"version": 1, "protectedIds": [], "reviewIds": []}
    data = json.loads(path.read_text(encoding="utf-8"))
    if data.get("version") != 1 or not isinstance(data.get("protectedIds"), list):
        raise EditError("Item protection file is invalid; no changes were made.")
    data.setdefault("reviewIds", [])
    if any(not isinstance(data[key], list) or any(not isinstance(item, str) for item in data[key])
           for key in ("protectedIds", "reviewIds")):
        raise EditError("Item review file is invalid; no changes were made.")
    return data


def atomic_write(path, data, expected=None):
    path.parent.mkdir(parents=True, exist_ok=True)
    handle, temporary = tempfile.mkstemp(prefix=path.stem + "-", suffix=path.suffix, dir=path.parent)
    try:
        with os.fdopen(handle, "wb") as output:
            output.write(data)
            output.flush()
            os.fsync(output.fileno())
        if expected is not None and digest(path.read_bytes()) != expected:
            raise EditError("Spreadsheet changed during this action. Reload and try again.")
        os.replace(temporary, path)
    finally:
        if os.path.exists(temporary):
            os.unlink(temporary)


def parse_sheets(snapshot):
    book = openpyxl.load_workbook(BytesIO(snapshot), read_only=True, data_only=True)
    try:
        return {sheet.title: list(sheet.values) for sheet in book}
    finally:
        book.close()


def master_items(sheets):
    rows = sheets["Items"]
    header = next(index for index, row in enumerate(rows) if row and row[0] == "Item ID")
    return [(index + 1, row) for index, row in enumerate(rows) if index > header and row and row[0]]


def build_deletion(snapshot, item_id):
    sheets = parse_sheets(snapshot)
    master = master_items(sheets)
    matches = [(number, row) for number, row in master if row[0] == item_id]
    if len(matches) != 1:
        raise EditError("Item no longer exists or its ID is duplicated. Reload the spreadsheet.")
    item_name = matches[0][1][1]
    recipe_ids, link_ids = set(), set()
    for sheet_name, item_column in [("Recipes", 3), ("Material routes", 3)]:
        for row in sheets.get(sheet_name, []):
            if len(row) > item_column and row[item_column] == item_id:
                recipe_ids.add(row[0] if sheet_name == "Recipes" else row[1])
    for row in sheets.get("Material routes", []):
        if len(row) > 1 and row[1] in recipe_ids:
            link_ids.add(row[0])

    clear_rows = {}
    for sheet_name, rows in sheets.items():
        for number, row in enumerate(rows, 1):
            # Exact-key records and every row of a recipe using the removed item.
            direct = any(value == item_id for value in row)
            dependent = (sheet_name in {"Recipes", "Material routes", "Crafting recipes", "Crafting browser"}
                         and any(isinstance(value, str) and value in recipe_ids | link_ids for value in row))
            if direct or dependent:
                clear_rows.setdefault(sheet_name, set()).add(number)
    with ZipFile(BytesIO(snapshot)) as archive:
        shared = []
        if "xl/sharedStrings.xml" in archive.namelist():
            shared = ["".join(node.itertext()) for node in ET.fromstring(archive.read("xl/sharedStrings.xml"))]
        workbook = ET.fromstring(archive.read("xl/workbook.xml"))
        relationships = ET.fromstring(archive.read("xl/_rels/workbook.xml.rels"))
        targets = {node.attrib["Id"]: node.attrib["Target"] for node in relationships}
        parts = {}
        for sheet in workbook.find("s:sheets", NS):
            relationship = sheet.attrib["{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id"]
            target = targets[relationship]
            parts[sheet.attrib["name"]] = target.lstrip("/") if target.startswith("/") else "xl/" + target
        replacement_parts = {}
        key_pattern = re.compile(r"(?<![\w.])" + re.escape(item_id) + r"(?![\w.])")
        for sheet_name, part in parts.items():
            raw = archive.read(part).decode("utf-8")
            removed = clear_rows.get(sheet_name, set())

            def edit_row(match):
                row_xml = match.group(0)
                number = int(re.search(r'\br="(\d+)"', row_xml).group(1))
                if number in removed:
                    # Leave styled cells, table extents and all other row addresses intact.
                    return re.sub(r"<(?:\w+:)?c\b[^>]*?(?:/>|>.*?</(?:\w+:)?c>)", blank_cell, row_xml, flags=re.S)
                # Multi-item source lists retain the other candidates.
                if item_id in row_xml or shared:
                    def edit_cell(cell_match):
                        cell = cell_match.group(0)
                        shared_index = re.search(r'<(?:\w+:)?v>(\d+)</(?:\w+:)?v>', cell)
                        if re.search(r'\bt="s"', cell) and shared_index:
                            value = shared[int(shared_index.group(1))]
                            if key_pattern.search(value):
                                opening = cell[:cell.index('>') + 1].replace('t="s"', 't="inlineStr"')
                                prefix = re.match(r'<((?:\w+:)?)c\b', cell).group(1)
                                value = key_pattern.sub("[removed item]", value)
                                return opening + f'<{prefix}is><{prefix}t xml:space="preserve">{escape(value)}</{prefix}t></{prefix}is></{prefix}c>'
                        if not key_pattern.search(cell):
                            return cell
                        if re.search(r"<(?:\w+:)?f[ >]", cell):
                            cell = cell.replace('"' + item_id + '"', '""')
                            cell = cell.replace('&quot;' + item_id + '&quot;', '&quot;&quot;')
                            cell = cell.replace('"MISSING ID"', '""')
                            # Excel will recalculate this cell on opening; do not retain its stale cache.
                            cell = re.sub(r"<(?:\w+:)?v\b[^>]*>.*?</(?:\w+:)?v>", "", cell, flags=re.S)
                        else:
                            cell = key_pattern.sub("[removed item]", cell)
                        return cell
                    row_xml = re.sub(r"<(?:\w+:)?c\b[^>]*?(?:/>|>.*?</(?:\w+:)?c>)", edit_cell, row_xml, flags=re.S)
                return row_xml

            updated = re.sub(r"<(?:\w+:)?row\b[^>]*>.*?</(?:\w+:)?row>", edit_row, raw, flags=re.S)
            if updated != raw:
                replacement_parts[part] = updated.encode("utf-8")
        # Keep native formula definitions. Excel refreshes affected summaries when opened.
        raw_workbook = archive.read("xl/workbook.xml").decode("utf-8")
        calc = re.search(r"<(?:\w+:)?calcPr\b[^>]*/>", raw_workbook)
        if calc:
            node = re.sub(r'\s(?:fullCalcOnLoad|forceFullCalc|calcMode)="[^"]*"', "", calc.group())
            node = node[:-2] + ' fullCalcOnLoad="1" forceFullCalc="1" calcMode="auto"/>'
            replacement_parts["xl/workbook.xml"] = raw_workbook.replace(calc.group(), node).encode("utf-8")
        else:
            closing = re.search(r'</((?:\w+:)?)workbook>', raw_workbook)
            prefix = closing.group(1)
            node = f'<{prefix}calcPr fullCalcOnLoad="1" forceFullCalc="1" calcMode="auto"/>'
            replacement_parts["xl/workbook.xml"] = raw_workbook.replace(closing.group(), node + closing.group()).encode("utf-8")
        output = BytesIO()
        with ZipFile(output, "w") as result:
            for info in archive.infolist():
                result.writestr(info, replacement_parts.get(info.filename, archive.read(info.filename)))
    updated = output.getvalue()
    remaining = master_items(parse_sheets(updated))
    if len(remaining) != len(master) - 1 or any(row[0] == item_id for _, row in remaining):
        raise EditError("Deletion verification failed; spreadsheet was not changed.")
    return updated, {"itemId": item_id, "itemName": item_name,
                     "clearedRows": {name: len(rows) for name, rows in clear_rows.items()}}


def blank_cell(match):
    cell = match.group(0)
    opening = re.match(r"<((?:\w+:)?c)\b([^>]*?)(?:/?>)", cell)
    attributes = re.sub(r'\s+t="[^"]*"', "", opening.group(2))
    return "<" + opening.group(1) + attributes.rstrip("/") + "/>"


def mutate(workbook_path, state_path, backup_dir, action, item_id, expected_hash, protected=None, reviewed=None):
    if action not in {"protect", "review", "delete"}:
        raise EditError("Unknown item action.", 400)
    with LOCK:
        snapshot = workbook_path.read_bytes()
        if digest(snapshot) != expected_hash:
            raise EditError("Spreadsheet changed. Reload it before changing this item.")
        data = review_state(state_path)
        kept = set(data["protectedIds"])
        flagged = set(data["reviewIds"])
        if not any(row[0] == item_id for _, row in master_items(parse_sheets(snapshot))):
            raise EditError("This item is no longer in the spreadsheet.", 404)
        if action in {"protect", "review"}:
            value = protected if action == "protect" else reviewed
            if not isinstance(value, bool):
                raise EditError("Keep/review state must be true or false.", 400)
            if action == "protect":
                if value and item_id in flagged:
                    raise EditError("Unmark this item for review before keeping it.")
                kept.add(item_id) if value else kept.discard(item_id)
            else:
                flagged.add(item_id) if value else flagged.discard(item_id)
                if value:
                    kept.discard(item_id)
            data["protectedIds"] = sorted(kept)
            data["reviewIds"] = sorted(flagged)
            atomic_write(state_path, (json.dumps(data, indent=2) + "\n").encode("utf-8"))
            return {"protectedIds": data["protectedIds"], "reviewIds": data["reviewIds"]}
        if item_id in kept:
            raise EditError("This item is kept. Uncheck its green circle before deleting it.")
        updated, receipt = build_deletion(snapshot, item_id)
        backup_dir.mkdir(parents=True, exist_ok=True)
        backup = backup_dir / (digest(snapshot) + ".xlsx")
        if not backup.exists():
            backup.write_bytes(snapshot)
        receipt["deletedAt"] = datetime.now(timezone.utc).isoformat()
        receipt["resultHash"] = digest(updated)
        atomic_write(workbook_path, updated, expected_hash)
        try:
            (backup_dir / (digest(updated) + ".json")).write_text(json.dumps(receipt, indent=2), encoding="utf-8")
        except OSError:
            pass  # The workbook is committed; a receipt failure must not report deletion failure.
        return receipt
