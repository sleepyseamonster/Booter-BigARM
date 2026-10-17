"""Revision-checked material-tier writes with preserved OOXML and linked caches."""
import json
import re
from datetime import datetime, timezone
from io import BytesIO
from zipfile import ZipFile

from openpyxl.utils import get_column_letter
import category_edits
from workbook_edits import EditError, LOCK, atomic_write, digest, master_items, parse_sheets

INGREDIENTS = {'Ingredient candidate', 'Consumed input', 'Contained input', 'Transform input'}


def tier_choices(sheets):
    choices = [r[1] for r in sheets.get('Taxonomy', [])[5:]
               if len(r)>1 and r[0]=='Material tier' and isinstance(r[1], str)]
    return sorted(choices, key=lambda value: -1 if value=='Not applicable' else tier_number(value) if tier_number(value)>=0 else 99)


def tier_number(tier):
    return int(tier[1]) if isinstance(tier, str) and re.fullmatch(r'T[0-5]', tier) else -1


def build_tier(snapshot, item_id, destination, expected_from):
    sheets = parse_sheets(snapshot)
    choices = tier_choices(sheets)
    if not choices or len(choices)!=len(set(choices)) or destination not in choices:
        raise EditError('Choose a registered material tier.', 400)
    master = master_items(sheets)
    if len({r[0] for _,r in master})!=len(master):
        raise EditError('Duplicate item IDs; no changes were made.')
    found = [(n,r) for n,r in master if r[0]==item_id]
    if len(found)!=1:
        raise EditError('This item no longer exists. Reload the spreadsheet.', 404)
    number, item = found[0]
    ih = sheets['Items'][4]
    old = item[ih.index('Tier')]
    if old!=expected_from:
        raise EditError('This item’s tier changed. Reload before saving.')
    receipt = dict(itemId=item_id, itemName=item[1], fromTier=old, toTier=destination, noop=old==destination)
    if old==destination:
        return snapshot, receipt
    changes = {}

    def set_cell(sheet, n, field, value):
        h = sheets[sheet][4]
        if field not in h:
            return
        ci = h.index(field)
        row = sheets[sheet][n-1]
        if row[ci]!=value:
            changes.setdefault(sheet, {})[f'{get_column_letter(ci+1)}{n}'] = value
            updated = list(row); updated[ci] = value; sheets[sheet][n-1] = tuple(updated)

    set_cell('Items', number, 'Tier', destination)
    set_cell('Items', number, 'Tier basis', f'Author-selected material tier {destination} from the item inspector. Previous tier: {old}. Material tier is independent of rarity, condition and item performance.')
    set_cell('Items', number, 'Tier sort order', tier_number(destination) if tier_number(destination)>=0 else 98 if destination=='Unassigned' else 99)
    current = sheets['Items'][number-1]
    fields = ['Item ID','Item name','Category','Subtype','Item type','Processing stage','Origin family','Primary use','Material family','Physical form','Design status','Search aliases','Tier','Finished-item input tier','Crafting role']
    set_cell('Items', number, 'Search terms', '; '.join(str(current[ih.index(k)] or '') for k in fields).lower())
    if 'Listed output routes' in ih:
        outputs = current[ih.index('Listed output routes')] or 0
        set_cell('Items', number, 'Crafting coverage', 'Route listed; review status/completeness' if outputs>0 else 'Raw entry feed; no production route' if destination=='T0' else 'No production route listed')
    linked = {'Tier review':[('Item ID','Current tier'),('Item ID','Tier sort order')],
              'Craft audit':[('Item ID','Tier')], 'Material routes':[('Item ID','Material tier')],
              'Crafting browser':[('Input item ID','Input tier'),('Output item ID','Output tier')]}
    for sheet, mappings in linked.items():
        if sheet not in sheets:
            continue
        h = sheets[sheet][4]
        for key, field in mappings:
            if key not in h or field not in h:
                continue
            for n,r in enumerate(sheets[sheet][5:],6):
                if r[h.index(key)]==item_id:
                    set_cell(sheet,n,field,(tier_number(destination) if tier_number(destination)>=0 else 98 if destination=='Unassigned' else 99) if field=='Tier sort order' else destination)

    recipes = set()
    if 'Material routes' in sheets:
        h = sheets['Material routes'][4]
        for n,r in enumerate(sheets['Material routes'][5:],6):
            if r[h.index('Item ID')]!=item_id:
                continue
            ingredient = r[h.index('Relationship')] in INGREDIENTS
            numeric = tier_number(destination) if ingredient else -1
            set_cell('Material routes',n,'Ingredient tier number',numeric)
            set_cell('Material routes',n,'Unresolved ingredient tier',int(ingredient and numeric==-1))
            if ingredient:
                recipes.add(r[h.index('Recipe ID')])
    listed = {}
    if 'Crafting recipes' in sheets:
        rh = sheets['Material routes'][4]
        h = sheets['Crafting recipes'][4]
        for n,r in enumerate(sheets['Crafting recipes'][5:],6):
            recipe = r[h.index('Recipe ID')]
            if not recipe:
                continue
            if recipe in recipes:
                inputs = [x for x in sheets['Material routes'][5:] if x[rh.index('Recipe ID')]==recipe and x[rh.index('Relationship')] in INGREDIENTS]
                nums = [tier_number(x[rh.index('Material tier')]) for x in inputs]
                value = 'Unassigned' if not nums or -1 in nums else f'T{max(nums)}'
                set_cell('Crafting recipes',n,'Listed ingredient tier',value)
            listed[recipe] = sheets['Crafting recipes'][n-1][h.index('Listed ingredient tier')]
    if 'Crafting browser' in sheets:
        h = sheets['Crafting browser'][4]
        items = {r[0]:dict(zip(ih,r)) for _,r in master_items(sheets)}
        for n,r in enumerate(sheets['Crafting browser'][5:],6):
            get = lambda field:r[h.index(field)]
            if get('Recipe ID') not in recipes and item_id not in (get('Input item ID'),get('Output item ID')):
                continue
            top = listed.get(get('Recipe ID'),'Unassigned')
            output = get('Output tier')
            role = items.get(get('Output item ID'),{}).get('Crafting role')
            interpretation = ('Withheld premise' if get('Route status')=='Withheld' else
                'Reusable prerequisite' if get('Relationship')=='Tool candidate' else
                'Entry-resource preparation' if get('Route kind')=='Raw entry preparation' else
                ('Finished endpoint' if role=='Finished item' else 'Currency / no refinement tier' if role=='Currency' else 'Recovered output / tier not applicable') if output=='Not applicable' else
                'Tier unresolved' if top=='Unassigned' or output=='Unassigned' else
                ('Advances one material tier' if get('Input tier')==top else 'Lower-tier ingredient; route advances one tier') if tier_number(output)>=0 and tier_number(top)>=0 and tier_number(output)-tier_number(top)==1 else
                'Needs tier review')
            set_cell('Crafting browser',n,'Tier interpretation',interpretation)

    result = BytesIO()
    with ZipFile(BytesIO(snapshot)) as archive, ZipFile(result,'w') as output:
        parts = category_edits.parts_for(archive)
        replacement = {parts[s]:category_edits.patch_cells(archive.read(parts[s]).decode(),edits).encode() for s,edits in changes.items()}
        for info in archive.infolist():
            output.writestr(info,replacement.get(info.filename,archive.read(info.filename)))
    candidate = result.getvalue()
    category_edits.verify_move(snapshot,candidate,changes,category_contract=False)
    receipt['changedCells'] = {s:len(edits) for s,edits in changes.items()}
    return candidate, receipt


def operation_status(workbook, backups, operation_id):
    return category_edits.operation_status(workbook, backups/'Tier', operation_id)


def update(workbook, backups, body):
    op = category_edits.operation_id(body.get('operationId'))
    with LOCK:
        existing = operation_status(workbook,backups,op)
        if existing['status']!='not-found':
            if any(existing.get(k)!=body.get(k) for k in ['itemId','fromTier','toTier','sourceHash']):
                raise EditError('Operation ID already belongs to another tier edit.')
            if existing['status']=='committed':
                return existing
            if existing['status']!='not-applied':
                raise EditError('Previous save is uncertain. Reload and inspect the tier.')
        snapshot = workbook.read_bytes()
        if digest(snapshot)!=body.get('sourceHash'):
            raise EditError('Spreadsheet changed. Reload before saving this tier.')
        candidate,receipt = build_tier(snapshot,body.get('itemId'),body.get('toTier'),body.get('fromTier'))
        receipt.update(operationId=op,sourceHash=digest(snapshot),resultHash=digest(candidate),
                       changedAt=datetime.now(timezone.utc).isoformat(),status='prepared')
        directory = backups/'Tier'; directory.mkdir(parents=True,exist_ok=True)
        backup = directory/(digest(snapshot)+'.xlsx')
        if not backup.exists():
            backup.write_bytes(snapshot)
        prepared = directory/f'category-{op}.prepared.json'
        if prepared.exists():
            prior = json.loads(prepared.read_text())
            if prior['resultHash']!=receipt['resultHash']:
                raise EditError('Prepared tier edit no longer matches the workbook.')
            receipt = prior
        else:
            atomic_write(prepared,json.dumps(receipt,indent=2).encode())
        if candidate!=snapshot:
            atomic_write(workbook,candidate,receipt['sourceHash'])
        receipt['status']='committed'
        try:
            atomic_write(directory/f'category-{op}.committed.json',json.dumps(receipt,indent=2).encode())
        except OSError:
            pass  # Prepared result hash can reconcile a lost completion journal.
        return receipt
