"""Prepare/apply the author's workshop-upgrade classification using a preserving merge.

After prepare, stage plan.json's non-cached cells into authored.xlsx with Artifact
Tool, then apply. The shared merger verifies every unchanged workbook cell.
"""
import argparse
import hashlib
import json
from collections import Counter
from io import BytesIO

import openpyxl
import split_equipment as migration

ITEM_ID = "proposal.ceramic.grinding_disc"
CATEGORY = "Workshop upgrades"
DEFINITION = "Upgrades and replaceable working parts fitted to workshop tools or stations."


def prepare():
    migration.OUT.mkdir(parents=True, exist_ok=True)
    original = migration.SOURCE.read_bytes()
    (migration.OUT / "before.xlsx").write_bytes(original)
    book = openpyxl.load_workbook(BytesIO(original), data_only=True)
    formulas = openpyxl.load_workbook(BytesIO(original), data_only=False)
    changes = []

    def change(sheet, address, value, cached=False, template=None, formula=None):
        changes.append(dict(sheet=sheet, address=address, value=value,
                            cachedOnly=cached, template=template,
                            **({"formula": formula} if formula else {})))

    rows = list(book["Items"].values)
    headers = rows[4]
    matches = [(n, row) for n, row in enumerate(rows[5:], 6) if row[0] == ITEM_ID]
    assert len(matches) == 1, "Expected exactly one ceramic abrasive disc"
    number, item = matches[0]
    assert item[3] == "Attachments", "Unexpected original category; already migrated?"
    assert not any(row[0] and row[3] == CATEGORY for row in rows[5:])
    change("Items", f"D{number}", CATEGORY)
    basis = headers.index("Classification basis")
    change("Items", f"{migration.col(basis)}{number}", (item[basis] or "") +
           " Author correction, October 15, 2026: Workshop upgrades supersedes Attachments. " + DEFINITION)
    search = headers.index("Search terms")
    change("Items", f"{migration.col(search)}{number}",
           item[search].replace("; attachments;", "; workshop upgrades;"), True)
    for sheet in book:
        if sheet.title == "Items":
            continue
        data = list(sheet.values)
        if len(data) < 5:
            continue
        header = data[4]
        for category, key in [("Category", "Item ID"), ("Input category", "Input item ID"),
                              ("Output category", "Output item ID")]:
            if category not in header or key not in header:
                continue
            ci, ki = header.index(category), header.index(key)
            for n, row in enumerate(data[5:], 6):
                if len(row) > max(ci, ki) and row[ki] == ITEM_ID:
                    address = f"{migration.col(ci)}{n}"
                    change(sheet.title, address, CATEGORY,
                           formulas[sheet.title][address].data_type == "f")
    categories = list(book["Categories"].values)
    taxonomy = list(book["Taxonomy"].values)
    assert not any(row[0] == CATEGORY for row in categories)
    cr = next(n for n, row in enumerate(categories, 1) if row[0] == "Attachments")
    tr = next(n for n, row in enumerate(taxonomy, 1) if row[:2] == ("Category", "Attachments"))
    counts = Counter(row[3] for row in rows[5:] if row[0])
    counts["Attachments"] -= 1
    counts[CATEGORY] = 1
    change("Categories", f"B{cr}", counts["Attachments"],
           formula=f"COUNTIF(Items!$D$6:$D$1005,A{cr})")
    new_cr, new_tr = len(categories) + 1, len(taxonomy) + 1
    for ci, value in enumerate([CATEGORY, 1, DEFINITION,
                                "Author-requested classification; existing item behavior retained.", "Primary category"]):
        change("Categories", f"{migration.col(ci)}{new_cr}", value,
               template=f"{migration.col(ci)}{cr}",
               formula=f"COUNTIF(Items!$D$6:$D$1005,A{new_cr})" if ci == 1 else None)
    for ci, value in enumerate(["Category", CATEGORY, DEFINITION]):
        change("Taxonomy", f"{migration.col(ci)}{new_tr}", value, template=f"{migration.col(ci)}{tr}")
    plan = dict(sourceHash=hashlib.sha256(original).hexdigest(),
                items=[dict(id=ITEM_ID, name=item[1])], counts=dict(counts), changes=changes,
                extendTables=dict(Categories=new_cr, Taxonomy=new_tr))
    (migration.OUT / "plan.json").write_text(json.dumps(plan, indent=2), encoding="utf-8")
    manifest_path = migration.ROOT / "Docs/Design/Items/EQUIPMENT_CLASSIFICATION.json"
    (migration.OUT / "manifest-before.json").write_bytes(manifest_path.read_bytes())
    book.close()
    formulas.close()
    print(f"Prepared {len(changes)} targeted cell changes")


def apply():
    manifest_path = migration.ROOT / "Docs/Design/Items/EQUIPMENT_CLASSIFICATION.json"
    original = (migration.OUT / "manifest-before.json").read_bytes()
    assert manifest_path.read_bytes() == original, "Classification manifest changed during migration"
    manifest = json.loads(original)
    matches = [item for item in manifest["items"] if item["id"] == ITEM_ID]
    assert len(matches) == 1 and matches[0]["category"] == "Attachments"
    migration.apply()
    matches[0].update(category=CATEGORY, reason=DEFINITION)
    manifest["counts"] = dict(Counter(item["category"] for item in manifest["items"]))
    plan = json.loads((migration.OUT / "plan.json").read_text(encoding="utf-8"))
    after_hash = hashlib.sha256(migration.SOURCE.read_bytes()).hexdigest()
    manifest.setdefault("revisions", []).append(dict(date="2026-10-15",
        reason="Author moved Ceramic abrasive disc into new Workshop upgrades category.",
        movedIds=[ITEM_ID], sourceHashBefore=plan["sourceHash"], sourceHashAfter=after_hash))
    manifest["sourceHashAfter"] = after_hash
    migration.atomic_write(manifest_path, (json.dumps(manifest, indent=2) + "\n").encode(),
                           hashlib.sha256(original).hexdigest())


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("action", choices=["prepare", "apply"])
    args = parser.parse_args()
    migration.OUT = migration.ROOT / "Logs/WorkshopUpgradesCategorySplit"
    prepare() if args.action == "prepare" else apply()
