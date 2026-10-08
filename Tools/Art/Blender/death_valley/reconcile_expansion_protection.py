"""Account for a separately committed bridge package change without changing terrain."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def reconcile(manifest, reference_root, owner_commit):
    manifest=Path(manifest).resolve();root=Path(reference_root).resolve()
    m=json.loads(manifest.read_text(encoding='utf-8'))
    if 'protection_reconciliation' in m:
        raise ValueError('Protection reconciliation already recorded')
    owner=subprocess.check_output(['git','rev-parse',owner_commit+'^{commit}'],cwd=root,text=True).strip()
    subprocess.run(['git','merge-base','--is-ancestor',owner,'HEAD'],cwd=root,check=True)
    allowed={'Packages/manifest.json','Packages/packages-lock.json'}
    changes=[]
    for entry in m['protected_files']:
        path=root/entry['path'];current=digest(path)
        if current==entry['sha256']:continue
        if entry['path'] not in allowed:
            raise ValueError('Retained protected content changed: '+entry['path'])
        committed=subprocess.check_output(['git','show',owner+':'+entry['path']],cwd=root)
        # Git stores LF while this Windows checkout uses CRLF.
        if path.read_bytes().replace(b'\r\n',b'\n')!=committed.replace(b'\r\n',b'\n'):
            raise ValueError('Package does not match the recorded owner commit')
        changes.append({'path':entry['path'],'baseline_sha256':entry['sha256'],
                        'current_sha256':current,'owner_commit':owner})
        entry['sha256']=current
    if not changes:
        raise ValueError('No separately owned protected-file changes to reconcile')
    m['protection_reconciliation']={'original_export_manifest_sha256':digest(manifest),
        'reason':'Preserve separately committed Unity CLI bridge packages; retained terrain protection unchanged',
        'changes':changes}
    manifest.write_text(json.dumps(m,indent=2)+'\n',encoding='utf-8')
    return m['protection_reconciliation']


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--manifest',type=Path,required=True)
    p.add_argument('--reference-root',type=Path,required=True)
    p.add_argument('--owner-commit',required=True)
    a=p.parse_args()
    print(json.dumps(reconcile(a.manifest,a.reference_root,a.owner_commit),indent=2))
