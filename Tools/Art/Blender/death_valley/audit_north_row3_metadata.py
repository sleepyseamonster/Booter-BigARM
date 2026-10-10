"""Verify task-owned Unity metadata and GUID uniqueness without editing assets."""
import argparse
import json
from pathlib import Path
import re
from north_row3_contract import ROOT, contract, sha256

def audit(step, date):
    c = contract(step, date)
    owners = [ROOT/c['asset']]
    assets = []
    for owner in owners:
        assets.extend([owner]+list(owner.rglob('*')))
    assets = [p for p in assets if p.suffix != '.meta']
    assets += [ROOT/'Assets/_Project/Scenes/Reference'/f"{c['batch']}Candidate.unity"]
    owned_guids = {}
    for p in assets:
        meta = Path(str(p)+'.meta')
        if not meta.is_file():
            raise ValueError('Missing native-generated metadata: '+str(p))
        match = re.search(r'^guid: ([0-9a-f]{32})$', meta.read_text(encoding='utf-8'), re.M)
        if not match or match[1] in owned_guids:
            raise ValueError('Invalid or duplicate owned GUID: '+str(meta))
        owned_guids[match[1]] = meta
    occurrences = {g: [] for g in owned_guids}
    for meta in (ROOT/'Assets').rglob('*.meta'):
        match = re.search(r'^guid: ([0-9a-f]{32})$', meta.read_text(encoding='utf-8'), re.M)
        if match and match[1] in occurrences:
            occurrences[match[1]].append(meta)
    if any(len(paths) != 1 for paths in occurrences.values()):
        raise ValueError('Owned GUID collides with another asset')
    manifest = ROOT/c['asset']/'Source/manifest.json'
    m = json.loads(manifest.read_text(encoding='utf-8'))
    for t in m['new_tiles']:
        path = ROOT/t['data_path']
        if owned_guids.get(t['data_guid']) != Path(str(path)+'.meta') or sha256(path) != t['data_sha256'] or sha256(Path(str(path)+'.meta')) != t['meta_sha256']:
            raise ValueError('Native-sealed TerrainData ownership changed')
    result = dict(status='owned_metadata_and_global_GUID_uniqueness_verified', step=step,
                  owned_assets_and_folders=len(assets), native_terrains=len(m['new_tiles']),
                  manifest_sha256=sha256(manifest))
    print(json.dumps(result, indent=2))
    return result

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--step', type=int, required=True)
    parser.add_argument('--date', default='2026-10-10')
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    result = audit(args.step, args.date)
    if args.output:
        args.output.write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
