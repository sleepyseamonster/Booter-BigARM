"""Prepare/reconcile a reviewed equipment taxonomy change without broad XLSX serialization."""
import argparse
import hashlib
import json
import re
from collections import Counter
from io import BytesIO
from pathlib import Path
from xml.etree import ElementTree as ET
from xml.sax.saxutils import escape
from zipfile import ZipFile

import openpyxl
from workbook_edits import atomic_write

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Docs/Design/Items/MASTER_ITEM_DATABASE.xlsx"
OUT = ROOT / "Logs/EquipmentCategorySplit"
DEFINITIONS = {
    "Armor": "Complete protective wear, shields and guards. Classification does not assign protection effects.",
    "Tools": "Equipment actively used for harvesting, traversal, processing, fabrication or operating machinery; includes workshop fixtures and transport machines, not a portability claim.",
    "Accessories": "General clothing, eyewear, carried personal reserves, carriers and supporting utility gear; no new equipment slots or effects.",
    "Attachments": "Fitted mounts, cargo rigging, inserts, backing, liners and replaceable add-ons that serve another piece of equipment.",
}
# Every exception is deliberate. Remaining equipment IDs are explicitly enumerated as Tools below.
GROUPS = {
    "Armor": """proposal.gear.mail_shirt proposal.gear.metal_armor_set proposal.gear.chest_armor
proposal.gear.helmet proposal.gear.leg_armor proposal.gear.boots proposal.gear.gloves proposal.gear.shield
proposal.legger.belly_pan proposal.legger.shell_guard proposal.legger.iron_guard proposal.legger.joint_guard
proposal.legger.nanocarbon_panel proposal.legger.ballistic_guard proposal.legger.edge_guard proposal.legger.instrument_guard
author.electrical.insulated_suit author.electrical.insulated_gloves author.electrical.insulated_boots
proposal.collection.husk_fiber_face_wrap proposal.collection.chainmail_shoulder_mantle proposal.collection.shell_rib_gaiters
proposal.collection.pressed_husk_apron proposal.collection.round_iron_buckler proposal.collection.riveted_steel_round_shield
proposal.collection.broad_chitin_shield proposal.collection.shell_rib_oval_shield proposal.collection.tall_iron_pavise
proposal.collection.folding_wing_frame_screen_shield proposal.collection.ceramic_faced_shield proposal.collection.layered_husk_shield
proposal.collection.steel_rimmed_shell_shield proposal.collection.hook_edged_catch_shield proposal.collection.elbow_strapped_work_shield
proposal.collection.knee_braced_ground_shield proposal.collection.segmented_tower_shield proposal.collection.mica_slit_observation_shield
proposal.collection.grit_shedding_curved_shield proposal.collection.insulated_handle_shield proposal.collection.ceremonial_inlay_shield
proposal.collection.magnet_clamp_utility_shield proposal.collection.replaceable_scale_shield proposal.collection.ancient_plasteel_guard_shield""".split(),
    "Accessories": """equipment.algae_canister equipment.cloak equipment.jacket proposal.gear.grounding_kit
proposal.gear.backpack proposal.fashion.special_spectacles proposal.fashion.eyeglasses proposal.fashion.hat
proposal.fashion.husk_garment proposal.fashion.fur_garment proposal.fashion.hide_garment proposal.gear.simple_wraps
proposal.fire.glow_holder author.communication.crude_two_way_radio proposal.collection.mica_brim_sun_visor""".split(),
    "Attachments": """proposal.gear.special_plates proposal.canister.harness proposal.ceramic.armor_insert
proposal.ceramic.grinding_disc proposal.legger.sling proposal.legger.cradle proposal.legger.ceramic_backing
proposal.legger.pannier_frame proposal.legger.sample_rack proposal.legger.ore_bin proposal.legger.board_bracket
proposal.legger.spares_case proposal.legger.seam_guard proposal.legger.impact_cap proposal.legger.fragment_liner
proposal.legger.impact_lattice proposal.collection.minimal_hide_tool_belt""".split(),
    "Tools": """equipment.digging_tool tool.micro_dust_harvester equipment.grappling_hook proposal.gear.magnets
proposal.canister.inspection_gauge proposal.ceramic.crucible proposal.ceramic.casting_mold proposal.ceramic.mortar
proposal.ceramic.pestle proposal.ceramic.firing_tray proposal.fire.striker proposal.fire.cooking_pot proposal.fire.cooking_slab
proposal.fire.lidded_pan proposal.fire.rack proposal.fire.pot_stand proposal.fire.tongs proposal.fire.firebox
proposal.wind.turbine proposal.wind.hand_sail proposal.wind.dirtboard proposal.legger.grease_applicator
proposal.legger.service_pick proposal.legger.bearing_puller proposal.legger.alignment_gauge
author.electrical.magnetic_disruptor author.electrical.magnetic_inhibitor author.electrical.magnetic_enhancer
author.electrical.static_enhancer author.electrical.static_disruptor author.forging.hammer author.forging.anvil
proposal.forging.tongs proposal.forging.sledge proposal.forging.hot_cut proposal.forging.punch proposal.forging.drift
proposal.forging.swage_block proposal.forging.file proposal.forging.hearth author.city_transport.carriage author.city_transport.car
proposal.lore.shell_scraper proposal.collection.hand_cranked_metal_drill proposal.collection.husk_sheet_cutting_shears
proposal.collection.cable_grip_canyon_ascender proposal.collection.braked_canyon_descent_trolley proposal.collection.shell_runner_cargo_sled
proposal.audit.metalworking_hand_saw proposal.audit.workshop_winding_mandrel proposal.audit.food_preparation_screen
proposal.audit.metal_glassworking_blowpipe""".split(),
}


def col(index):
    return openpyxl.utils.get_column_letter(index + 1)


def prepare():
    OUT.mkdir(parents=True, exist_ok=True)
    snapshot = SOURCE.read_bytes()
    (OUT / "before.xlsx").write_bytes(snapshot)
    cached = openpyxl.load_workbook(BytesIO(snapshot), read_only=True, data_only=True)
    formulas = openpyxl.load_workbook(BytesIO(snapshot), read_only=False, data_only=False)
    mapping = {key: group for group, keys in GROUPS.items() for key in keys}
    assert len(mapping) == sum(map(len, GROUPS.values())), "Duplicate classification IDs"
    changes, items = [], []

    def change(sheet, address, value, cached_only=False, template=None):
        changes.append({"sheet": sheet, "address": address, "value": value,
                        "cachedOnly": cached_only, "template": template})

    master_rows = list(cached["Items"].values)
    headers = master_rows[4]
    live_ids = set()
    for number, row in enumerate(master_rows[5:], 6):
        if not row or not row[0] or row[3] != "Equipment":
            continue
        key, item_name = row[:2]
        assert key in mapping, f"Unreviewed equipment: {key}"
        live_ids.add(key)
        group = mapping[key]
        items.append({"id": key, "name": item_name, "category": group, "reason": DEFINITIONS[group]})
        change("Items", f"D{number}", group)
        basis_column = headers.index("Classification basis")
        basis = (row[basis_column] or "") + " October 15 equipment split: " + group + ". " + DEFINITIONS[group]
        change("Items", f"{col(basis_column)}{number}", basis)
        search_column = headers.index("Search terms")
        if row[search_column]:
            change("Items", f"{col(search_column)}{number}", row[search_column].replace("; equipment;", "; " + group.lower() + ";"), True)
    mapping = {key: mapping[key] for key in live_ids}
    assert live_ids, "No Equipment items left to migrate"
    # Keep linked formula definitions; update only the category caches they expose.
    for sheet in cached:
        if sheet.title == "Items":
            continue
        rows = list(sheet.values)
        if len(rows) < 5:
            continue
        header = rows[4]
        for category_field, id_field in [("Category", "Item ID"), ("Input category", "Input item ID"), ("Output category", "Output item ID")]:
            if category_field not in header or id_field not in header:
                continue
            ci, ki = header.index(category_field), header.index(id_field)
            for number, row in enumerate(rows[5:], 6):
                if len(row) > max(ci, ki) and row[ki] in mapping:
                    address = f"{col(ci)}{number}"
                    is_formula = formulas[sheet.title][address].data_type == "f"
                    change(sheet.title, address, mapping[row[ki]], is_formula)
    category_rows = list(cached["Categories"].values)
    taxonomy_rows = list(cached["Taxonomy"].values)
    category_equipment = next(i + 1 for i, row in enumerate(category_rows) if row and row[0] == "Equipment")
    taxonomy_equipment = next(i + 1 for i, row in enumerate(taxonomy_rows) if len(row) > 1 and row[0] == "Category" and row[1] == "Equipment")
    counts = Counter(item["category"] for item in items)
    for index, group in enumerate(DEFINITIONS):
        cr = category_equipment if index == 0 else len(category_rows) + index
        tr = taxonomy_equipment if index == 0 else len(taxonomy_rows) + index
        for ci, value in enumerate([group, counts[group], DEFINITIONS[group], "Organization only; retained design status, tiers, slots and behavior.", "Primary category"]):
            change("Categories", f"{col(ci)}{cr}", value, template=f"{col(ci)}{category_equipment}")
        # Count formula is retained as a live Excel formula with a verified cache.
        changes[-4]["formula"] = f'COUNTIF(Items!$D$6:$D$1005,A{cr})'
        for ci, value in enumerate(["Category", group, DEFINITIONS[group]]):
            change("Taxonomy", f"{col(ci)}{tr}", value, template=f"{col(ci)}{taxonomy_equipment}")
    cached.close()
    formulas.close()
    plan = {"sourceHash": hashlib.sha256(snapshot).hexdigest(), "items": items,
            "counts": dict(counts), "changes": changes,
            "extendTables": {"Categories": len(category_rows) + 3, "Taxonomy": len(taxonomy_rows) + 3}}
    (OUT / "plan.json").write_text(json.dumps(plan, indent=2), encoding="utf-8")
    print(json.dumps({"counts": plan["counts"], "changes": len(changes), "sourceHash": plan["sourceHash"]}))


def apply():
    plan = json.loads((OUT / "plan.json").read_text(encoding="utf-8"))
    snapshot = (OUT / "before.xlsx").read_bytes()
    assert hashlib.sha256(SOURCE.read_bytes()).hexdigest() == plan["sourceHash"], "Workbook changed during classification"
    # Staged authoring values are checked before a preserving merge into the original ZIP.
    authored = openpyxl.load_workbook(OUT / "authored.xlsx", read_only=False, data_only=False)
    for change in plan["changes"]:
        if not change["cachedOnly"] and not change.get("formula"):
            assert authored[change["sheet"]][change["address"]].value == change["value"], change
    authored.close()
    ns = {"s": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
    with ZipFile(BytesIO(snapshot)) as archive:
        wb = ET.fromstring(archive.read("xl/workbook.xml"))
        rels = {el.attrib["Id"]: el.attrib["Target"] for el in ET.fromstring(archive.read("xl/_rels/workbook.xml.rels"))}
        parts = {}
        for sheet in wb.find("s:sheets", ns):
            target = rels[sheet.attrib['{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id']]
            parts[sheet.attrib["name"]] = target.lstrip("/") if target.startswith("/") else "xl/" + target
        replacements = {}
        for sheet_name, part in parts.items():
            selected = [c for c in plan["changes"] if c["sheet"] == sheet_name]
            if not selected:
                continue
            raw = archive.read(part).decode("utf-8")
            prefix = re.search(r'<((?:\w+:)?)worksheet\b', raw).group(1)
            original_cells = {m.group(1): m.group(0) for m in re.finditer(r'<(?:\w+:)?c\b[^>]*\br="([A-Z]+\d+)"[^>]*(?:/>|>.*?</(?:\w+:)?c>)', raw, re.S)}
            for change in selected:
                address = change["address"]
                old = original_cells.get(address)
                template = original_cells.get(change.get("template")) or old or f'<{prefix}c r="{address}"/>'
                if change["cachedOnly"]:
                    assert old and re.search(r'<(?:\w+:)?f\b', old), change
                    opening = old[:old.index('>') + 1]
                    opening = re.sub(r'\s+t="[^"]*"', '', opening[:-1]) + ' t="str">'
                    formula = re.search(r'<(?:\w+:)?f\b[^>]*(?:/>|>.*?</(?:\w+:)?f>)', old, re.S).group()
                    cell = opening + formula + f'<{prefix}v>{escape(str(change["value"]))}</{prefix}v></{prefix}c>'
                else:
                    opening = template[:template.index('>') + 1].rstrip('/>')
                    opening = re.sub(r'\br="[A-Z]+\d+"', f'r="{address}"', opening)
                    opening = re.sub(r'\s+t="[^"]*"', '', opening)
                    if "formula" in change:
                        cell = opening + f'><{prefix}f>{escape(change["formula"])}</{prefix}f><{prefix}v>{change["value"]}</{prefix}v></{prefix}c>'
                    else:
                        cell = opening + f' t="inlineStr"><{prefix}is><{prefix}t xml:space="preserve">{escape(str(change["value"]))}</{prefix}t></{prefix}is></{prefix}c>'
                if old:
                    raw = raw.replace(old, cell, 1)
                else:
                    number = re.search(r'\d+', address).group()
                    row = re.search(rf'<(?:\w+:)?row\b[^>]*\br="{number}"[^>]*>.*?</(?:\w+:)?row>', raw, re.S)
                    if row:
                        updated_row = re.sub(r'</(?:\w+:)?row>$', cell + f'</{prefix}row>', row.group())
                        raw = raw.replace(row.group(), updated_row, 1)
                    else:
                        raw = raw.replace(f'</{prefix}sheetData>', f'<{prefix}row r="{number}">{cell}</{prefix}row></{prefix}sheetData>')
            replacements[part] = raw.encode("utf-8")
        for member in archive.namelist():
            if member.startswith("xl/tables/") and member.endswith('.xml'):
                raw = archive.read(member).decode("utf-8")
                table = ET.fromstring(raw)
                # Match sheet's table relationship target to avoid touching another table.
                for sheet_name, last_row in plan["extendTables"].items():
                    part = parts[sheet_name]
                    rel_path = str(Path(part).parent / "_rels" / (Path(part).name + ".rels")).replace('\\', '/')
                    if rel_path in archive.namelist() and Path(member).name in archive.read(rel_path).decode():
                        old_ref = table.attrib['ref']
                        new_ref = re.sub(r'\d+$', str(last_row), old_ref)
                        raw = raw.replace(f'ref="{old_ref}"', f'ref="{new_ref}"')
                        replacements[member] = raw.encode("utf-8")
        output = BytesIO()
        with ZipFile(output, 'w') as result:
            for info in archive.infolist():
                result.writestr(info, replacements.get(info.filename, archive.read(info.filename)))
    candidate = output.getvalue()
    verify(candidate, snapshot, plan)
    atomic_write(SOURCE, candidate, plan["sourceHash"])
    (OUT / "receipt.json").write_text(json.dumps({"beforeHash": plan["sourceHash"], "afterHash": hashlib.sha256(candidate).hexdigest(), "counts": plan["counts"], "changedParts": sorted(replacements)}, indent=2), encoding="utf-8")
    print('Verified and applied', plan['counts'])


def verify(candidate, original, plan):
    before = openpyxl.load_workbook(BytesIO(original), read_only=True, data_only=False)
    after = openpyxl.load_workbook(BytesIO(candidate), read_only=True, data_only=False)
    cached = openpyxl.load_workbook(BytesIO(candidate), read_only=False, data_only=True)
    allowed = {(c['sheet'], c['address']): c for c in plan['changes'] if not c['cachedOnly']}
    assert before.sheetnames == after.sheetnames
    for sheet in before:
        ar = list(after[sheet.title].values)
        for ri, row in enumerate(sheet.values, 1):
            for ci, value in enumerate(row):
                address = f'{col(ci)}{ri}'
                change = allowed.get((sheet.title, address))
                actual = ar[ri-1][ci] if ri <= len(ar) and ci < len(ar[ri-1]) else None
                expected = ('=' + change['formula'] if 'formula' in change else change['value']) if change else value
                assert actual == expected, (sheet.title, address, value, actual, expected)
    for change in plan['changes']:
        if change['cachedOnly']:
            assert cached[change['sheet']][change['address']].value == change['value'], change
    ids_before = [row[0] for row in list(before['Items'].values)[5:] if row and row[0]]
    ids_after = [row[0] for row in list(after['Items'].values)[5:] if row and row[0]]
    assert ids_after == ids_before, 'Item identities/row order changed'
    assert all(row[3] != 'Equipment' for row in list(cached['Items'].values)[5:] if row and row[0])
    before.close(); after.close(); cached.close()


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('action', choices=['prepare', 'apply'])
    args = parser.parse_args()
    prepare() if args.action == 'prepare' else apply()
