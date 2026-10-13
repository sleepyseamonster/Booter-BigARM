"""Read-only unpublished-history and LFS-cache gate; never uploads or edits Git."""
import argparse
import hashlib
import json
import pathlib
import re
import subprocess

def git(*args, data=None):
    return subprocess.check_output(['git', *args], input=data)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--base', default='origin/main')
    parser.add_argument('--candidate', default='HEAD')
    parser.add_argument('--output', required=True)
    args = parser.parse_args()
    root = pathlib.Path(git('rev-parse', '--show-toplevel').decode().strip())
    output = pathlib.Path(args.output).resolve()
    if output.suffix != '.json' or not any(output.is_relative_to(root / folder) for folder in ('Logs', 'Docs/Evidence')):
        raise ValueError('JSON receipts must be under Logs or Docs/Evidence; project/Git/source files are not output targets')
    base = git('rev-parse', args.base+'^{commit}').decode().strip()
    candidate = git('rev-parse', args.candidate+'^{commit}').decode().strip()
    initial_head = git('rev-parse', 'HEAD')
    subprocess.run(['git', 'merge-base', '--is-ancestor', base, candidate], check=True)
    excluded = git('log', '--format=%H %s', '--name-status', base+'..'+candidate, '--',
                   ':(icase)SourceData/Terrain/', ':(icase,glob)**/*.blend', ':(icase,glob)**/*.blend[0-9]*').decode()
    objects = git('rev-list', '--objects', base+'..'+candidate)
    rows = git('cat-file', '--batch-check=%(objectname) %(objecttype) %(objectsize) %(rest)', data=objects)
    blobs = []
    for row in rows.decode().splitlines():
        fields = row.split(' ', 3)
        if len(fields) >= 3 and fields[1] == 'blob':
            blobs.append((fields[0], int(fields[2]), fields[3] if len(fields) == 4 else ''))
    small = [b for b in blobs if b[1] <= 1024]
    raw = git('cat-file', '--batch', data=('\n'.join(b[0] for b in small)+'\n').encode()) if small else b''
    position = 0
    allowlist = {}
    errors = ['Excluded source paths touched in unpublished history'] if excluded else []
    media = pathlib.Path(git('lfs', 'env').decode().split('LocalMediaDir=', 1)[1].splitlines()[0])
    for blob, _, path in small:
        end = raw.index(b'\n', position)
        size = int(raw[position:end].split()[2])
        payload = raw[end+1:end+1+size]
        position = end+1+size+1
        if not payload.startswith(b'version https://git-lfs.github.com/spec/v1\n'):
            continue
        # Exempt a literal historical receipt only when the sealed candidate
        # does not classify it as LFS. A filename alone is not an upload gate.
        if path.endswith('.lfs-pointer.txt'):
            attr = git('check-attr', '--source='+candidate, '-z', 'filter', '--', path).split(b'\0')
            if len(attr) < 3 or attr[2] != b'lfs':
                continue
        match = re.fullmatch(rb'version https://git-lfs.github.com/spec/v1\noid sha256:([0-9a-f]{64})\nsize ([0-9]+)\n', payload)
        if not match:
            errors.append('Noncanonical LFS pointer: '+path)
            continue
        oid, expected_size = match[1].decode(), int(match[2])
        if path.startswith('SourceData/Terrain/') or re.search(r'\.blend[0-9]*$', path, re.I) or path.endswith('.meta'):
            errors.append('Forbidden LFS path: '+path)
        item = allowlist.setdefault(oid, {'oid': oid, 'bytes': expected_size, 'paths': [], 'pointer_blobs': []})
        if item['bytes'] != expected_size:
            errors.append('Conflicting sizes: '+oid)
        item['paths'].append(path)
        item['pointer_blobs'].append(blob)
    for oid, item in allowlist.items():
        cache = media / oid[:2] / oid[2:4] / oid
        if not cache.is_file() or cache.stat().st_size != item['bytes']:
            errors.append('Missing/wrong-sized LFS object: '+oid)
            continue
        digest = hashlib.sha256()
        with cache.open('rb') as handle:
            for block in iter(lambda: handle.read(1024*1024), b''):
                digest.update(block)
        if digest.hexdigest() != oid:
            errors.append('Corrupt LFS object: '+oid)
    oversized = [{'oid': o, 'bytes': n, 'path': p} for o, n, p in blobs if n > 100*1024*1024]
    if oversized:
        errors.append('Ordinary Git blobs exceed 100 MiB')
    if git('rev-parse', 'HEAD') != initial_head:
        errors.append('Live HEAD changed during verification')
    report = {'candidate': candidate, 'base': base, 'unpublished_commits': int(git('rev-list', '--count', base+'..'+candidate)),
              'excluded_history': excluded, 'new_blob_count': len(blobs), 'oversized_blobs': oversized,
              'lfs_objects': sorted(allowlist.values(), key=lambda item: item['oid']),
              'lfs_unique_count': len(allowlist), 'lfs_unique_bytes': sum(i['bytes'] for i in allowlist.values()),
              'errors': errors, 'passed': not errors,
              'boundary': 'Cached base, unpublished reachable blobs and local cache only; live remote, billing, dry-run reconciliation, pack size and Unity proof remain separate gates.'}
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_bytes((json.dumps(report, indent=2)+'\n').encode('utf-8'))
    print(json.dumps({k: report[k] for k in ('candidate', 'base', 'unpublished_commits', 'lfs_unique_count', 'lfs_unique_bytes', 'errors', 'passed')}))
    return 0 if report['passed'] else 1

if __name__ == '__main__':
    raise SystemExit(main())
