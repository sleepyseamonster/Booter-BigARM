"""Stage JSON catalog proposals and append verified Artifact Tool-authored inputs.

Uses the preserving appender rather than exporting the large production workbook.
"""
import argparse
import json

import openpyxl
from add_ancient_salvage import ROOT, SOURCE, build
from workbook_edits import atomic_write, digest


def main(action, specification, directory):
    spec_path=(ROOT/specification).resolve()
    out=(ROOT/directory).resolve()
    assert spec_path.is_relative_to(ROOT) and out.is_relative_to(ROOT/'Logs')
    out.mkdir(parents=True,exist_ok=True)
    spec=json.loads(spec_path.read_text(encoding='utf-8'))
    if action=='prepare':
        snapshot=SOURCE.read_bytes()
        (out/'before.xlsx').write_bytes(snapshot)
        (out/'plan.json').write_text(json.dumps(dict(sourceHash=digest(snapshot),spec=spec),indent=2),encoding='utf-8')
        print(f'Prepared {len(spec["items"])} catalog proposal(s).')
        return
    plan=json.loads((out/'plan.json').read_text(encoding='utf-8'))
    assert spec==plan['spec'] and digest(SOURCE.read_bytes())==plan['sourceHash'],'Inputs changed; prepare again'
    authored=openpyxl.load_workbook(out/'authored.xlsx',read_only=True,data_only=True)
    rows=list(authored['Catalog inputs'].values)[1:]
    assert len(rows)==len(spec['items'])
    for row,item in zip(rows,spec['items']):
        assert list(row[:6])==[item[k] for k in ['id','name','purpose','icon','material','use']]
        assert json.loads(row[6])==item.get('fields',{})
    authored.close()
    candidate,receipt=build((out/'before.xlsx').read_bytes(),spec)
    (out/'after.xlsx').write_bytes(candidate)
    atomic_write(SOURCE,candidate,plan['sourceHash'])
    (out/'receipt.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8')
    print(json.dumps(receipt,indent=2))


if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('action',choices=['prepare','apply'])
    parser.add_argument('--spec',required=True)
    parser.add_argument('--operation-dir',required=True)
    args=parser.parse_args()
    main(args.action,args.spec,args.operation_dir)
