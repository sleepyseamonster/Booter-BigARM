"""Prepare and apply six Artifact Tool-reviewed raw fauna proposals."""
import argparse
import json

import openpyxl
from add_ancient_salvage import ROOT, SOURCE, build
from workbook_edits import atomic_write, digest

DATA = ROOT / 'Docs/Design/Items/SIX_BIOLOGICAL_RESOURCE_PROPOSALS_2026-10-15.json'
OUT = ROOT / 'Logs/SixBiologicalResources20261015'


def main(action):
    OUT.mkdir(parents=True, exist_ok=True)
    if action == 'prepare':
        original = SOURCE.read_bytes()
        spec = json.loads(DATA.read_text(encoding='utf-8'))
        assert len(spec['items']) == 6
        (OUT / 'before.xlsx').write_bytes(original)
        (OUT / 'plan.json').write_text(json.dumps(dict(sourceHash=digest(original), spec=spec), indent=2), encoding='utf-8')
        print('Prepared six new raw biological resources.')
        return
    plan = json.loads((OUT / 'plan.json').read_text(encoding='utf-8'))
    assert digest(SOURCE.read_bytes()) == plan['sourceHash'], 'Workbook changed; prepare again'
    spec = plan['spec']
    assert json.loads(DATA.read_text(encoding='utf-8')) == spec
    authored = openpyxl.load_workbook(OUT / 'authored.xlsx', read_only=True, data_only=True)
    rows = list(authored['Resource inputs'].values)[1:]
    assert len(rows) == 6
    for row, item in zip(rows, spec['items']):
        assert list(row[:6]) == [item[k] for k in ['id','name','purpose','icon','material','use']]
        assert json.loads(row[6]) == item['fields']
    authored.close()
    candidate, receipt = build((OUT / 'before.xlsx').read_bytes(), spec)
    (OUT / 'after.xlsx').write_bytes(candidate)
    atomic_write(SOURCE, candidate, plan['sourceHash'])
    (OUT / 'receipt.json').write_text(json.dumps(receipt, indent=2), encoding='utf-8')
    print(json.dumps(receipt, indent=2))


if __name__ == '__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('action', choices=['prepare','apply'])
    main(parser.parse_args().action)
