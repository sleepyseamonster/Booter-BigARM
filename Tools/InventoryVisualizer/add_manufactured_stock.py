"""Add eight author-requested stock concepts using the verified preserving appender."""
import json
from add_ancient_salvage import ROOT, SOURCE, build
from workbook_edits import atomic_write

DATA = ROOT / 'Docs/Design/Items/MANUFACTURED_STOCK_PROPOSALS.json'
OUT = ROOT / 'Logs/EightManufacturedStock'


if __name__ == '__main__':
    OUT.mkdir(parents=True, exist_ok=True)
    original = SOURCE.read_bytes()
    (OUT / 'before.xlsx').write_bytes(original)
    candidate, receipt = build(original, json.loads(DATA.read_text(encoding='utf-8')))
    (OUT / 'after.xlsx').write_bytes(candidate)
    atomic_write(SOURCE, candidate, receipt['sourceHash'])
    (OUT / 'receipt.json').write_text(json.dumps(receipt, indent=2), encoding='utf-8')
    print(json.dumps(receipt, indent=2))
