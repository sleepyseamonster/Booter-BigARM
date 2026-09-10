"""Install a local technical workbench package and bind its payload to source hashes.

No publication. The destination must be new and inside Engine. The caller must
build and test first; this manifest does not replace native validation.
"""
import argparse
import hashlib
import json
import subprocess
import shutil
from common import ROOT, inside, write_json

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--build',required=True)
    parser.add_argument('--out',required=True)
    parser.add_argument('--catalog',help='Optional cooked catalog copied into executable-relative Assets')
    parser.add_argument('--model',help='Optional cooked model manifest for the standalone player')
    args=parser.parse_args()
    build,out=inside(args.build),inside(args.out)
    if out.exists() or not (build/'CMakeCache.txt').is_file():
        parser.error('Build must be configured and package destination must be new')
    subprocess.run(['cmake','--install',str(build),'--config','Release','--prefix',str(out)],check=True,cwd=ROOT)
    executable=out/'bin'/('engine_workbench.exe' if (out/'bin/engine_workbench.exe').exists() else 'engine_workbench')
    if not executable.is_file() or not all((out/'bin/Shaders'/(p.stem+'.bin')).is_file() for p in (ROOT/'Shaders').glob('*.sc') if p.name.startswith(('vs_','fs_'))):
        raise ValueError('Incomplete installed workbench')
    if args.catalog:
        catalog=inside(args.catalog)
        records=json.loads(catalog.read_text())["payload"]["textures"]
        asset_root=out/'bin/Assets';asset_root.mkdir()
        shutil.copyfile(catalog,asset_root/'catalog.json')
        for row in records:
            source=inside(catalog.parent/row['file'])
            destination=asset_root/row['file']
            if asset_root.resolve() not in destination.resolve().parents:raise ValueError('Catalog path escapes package assets')
            destination.parent.mkdir(parents=True,exist_ok=True)
            if sha(source)!=row['sha256']:raise ValueError('Cooked catalog hash mismatch')
            shutil.copyfile(source,destination)
    if args.model:
        model=inside(args.model)
        document=json.loads(model.read_text())
        if document.get('kind')!='engine.model' or document.get('version')!=1:raise ValueError('Unsupported model manifest')
        relative=document['payload']['mesh']
        from pathlib import PurePosixPath
        part=PurePosixPath(relative)
        if part.is_absolute() or '..' in part.parts or '.' in part.parts or '\\' in relative or ':' in relative:raise ValueError('Invalid model mesh reference')
        mesh=inside(model.parent/relative)
        if model.parent.resolve() not in mesh.resolve().parents:raise ValueError('Model mesh escapes asset root')
        destination=out/'bin/Assets/Models/Calibration';destination.mkdir(parents=True)
        shutil.copyfile(model,destination/'model.json')
        (destination/relative).parent.mkdir(parents=True,exist_ok=True)
        shutil.copyfile(mesh,destination/relative)
        if not any((out/'bin'/name).is_file() for name in ['engine_player','engine_player.exe']):raise ValueError('Missing player executable')
    payload={p.relative_to(out).as_posix():sha(p) for p in sorted(out.rglob('*')) if p.is_file()}
    sources={p.relative_to(ROOT).as_posix():sha(p) for folder in ['Source','Apps','Shaders','CMake']
             for p in sorted((ROOT/folder).rglob('*')) if p.is_file()}
    for name in ['CMakeLists.txt','CMakePresets.json','Research/probe-lock.json','Research/runtime-lock.json']:
        sources[name]=sha(ROOT/name)
    write_json(out/'package.json',{'schema_version':1,'kind':'technical-player-and-workbench' if args.model else 'technical-workbench','files':payload,'source_inputs':sources,
        'proof_limit':'Install inventory only; compare with build receipts. Not Windows validation, complete third-party shipping clearance or release approval.'})
    print(json.dumps({'package':str(out),'payload_files':len(payload)},indent=2))

if __name__=='__main__':main()
