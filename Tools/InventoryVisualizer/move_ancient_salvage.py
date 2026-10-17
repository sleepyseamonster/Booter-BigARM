"""Move ancient-origin finds out of general Salvage into Ancient salvage."""
import json
import move_reviewed_tools as batch
from workbook_edits import parse_sheets


def configure():
    batch.OUT=batch.migration.ROOT/'Logs/AncientSalvageCorrection20261016'
    batch.AUDIT=batch.migration.ROOT/'Docs/Design/Items/ANCIENT_SALVAGE_CORRECTION_2026-10-16.json'
    rows=parse_sheets(batch.migration.SOURCE.read_bytes())['Items'][5:]
    batch.SELECTED={r[0] for r in rows if r[0] and r[3]=='Salvage' and r[25]=='Ancient technology'}
    batch.SOURCE_CATEGORY='Salvage';batch.TARGET_CATEGORY='Ancient salvage'
    batch.REASON='Author direction October 16: ancient-origin items in general Salvage belong in Ancient salvage. IDs, tiers, rarity, condition, properties, recipe roles and unknown operating requirements remain unchanged.'


if __name__=='__main__':
    configure()
    batch.guard.OUT=batch.OUT;batch.guard.prepare=batch.prepare;batch.guard.apply=batch.apply
    batch.guard.run()
    manifest=json.loads(batch.MANIFEST.read_text(encoding='utf-8'))
    if any(i['id'] in batch.SELECTED for i in manifest['items']):batch.update_manifest()
