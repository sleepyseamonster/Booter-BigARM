"""Freeze current tracked Unity content for an isolated Player performance capture."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess


def main():
    root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser()
    parser.add_argument('--destination', required=True)
    args = parser.parse_args()
    destination = Path(args.destination).resolve()
    if destination.parent != Path('D:/BooterBigArmValidation').resolve() or not destination.name.startswith('PerformancePlayer') or destination.exists():
        raise SystemExit('Use a new PerformancePlayer child of D:/BooterBigArmValidation; retained copies are preserved.')
    names = subprocess.check_output(['git', 'ls-files', '-z', 'Assets', 'Packages', 'ProjectSettings'], cwd=root).decode().split('\0')
    dirty = subprocess.check_output(['git', 'diff', '--name-only', 'HEAD'], cwd=root).decode().splitlines()
    selected = [n for n in names if n and not n.startswith('Assets/_Project/Legacy2D/') and '.blend' not in n.lower()]
    total = sum((root/n).stat().st_size for n in selected if (root/n).is_file())
    if shutil.disk_usage(destination.parent).free < total + 30 * 1024**3:
        raise SystemExit('Preserve at least 30 GiB for import/build/cache headroom in addition to source copies.')
    destination.mkdir(); records = []
    for name in selected:
        source = root/name
        if not source.is_file():
            continue
        target = destination/name; target.parent.mkdir(parents=True, exist_ok=True)
        if name.endswith('.meta') and source.with_suffix('').is_dir():
            target.with_suffix('').mkdir(exist_ok=True)
        before = hashlib.sha256(source.read_bytes()).hexdigest()
        shutil.copyfile(source, target)
        assert hashlib.sha256(target.read_bytes()).hexdigest() == before
        assert hashlib.sha256(source.read_bytes()).hexdigest() == before, f'Source changed during copy: {name}'
        records.append({'path': name, 'sha256': before, 'bytes': target.stat().st_size})
    receipt = {'source': str(root), 'destination': str(destination),
        'head': subprocess.check_output(['git','rev-parse','HEAD'],cwd=root).decode().strip(),
        'scope':'Current tracked active Unity working files, including the existing dirty material; no original files written. Blender, legacy and unrelated untracked art excluded.',
        'dirty_at_capture':dirty,'files':records,'total_bytes':sum(r['bytes'] for r in records)}
    (destination/'Logs').mkdir()
    (destination/'Logs/player-audit-source-snapshot.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({'destination':str(destination),'files':len(records),'bytes':receipt['total_bytes']}))


if __name__ == '__main__':
    main()
