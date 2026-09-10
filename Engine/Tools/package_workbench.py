"""Install a local technical workbench package and bind its payload to source hashes.

No publication. The destination must be new and inside Engine. The caller must
build and test first; this manifest does not replace native validation.
"""
import argparse
import hashlib
import json
import subprocess
from common import ROOT, inside, write_json

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--build',required=True)
    parser.add_argument('--out',required=True)
    args=parser.parse_args()
    build,out=inside(args.build),inside(args.out)
    if out.exists() or not (build/'CMakeCache.txt').is_file():
        parser.error('Build must be configured and package destination must be new')
    subprocess.run(['cmake','--install',str(build),'--config','Release','--prefix',str(out)],check=True,cwd=ROOT)
    executable=out/'bin'/('engine_workbench.exe' if (out/'bin/engine_workbench.exe').exists() else 'engine_workbench')
    if not executable.is_file() or len(list((out/'bin/Shaders').glob('*.bin')))<7:
        raise ValueError('Incomplete installed workbench')
    payload={p.relative_to(out).as_posix():sha(p) for p in sorted(out.rglob('*')) if p.is_file()}
    sources={p.relative_to(ROOT).as_posix():sha(p) for folder in ['Source','Apps','Shaders','CMake']
             for p in sorted((ROOT/folder).rglob('*')) if p.is_file()}
    for name in ['CMakeLists.txt','CMakePresets.json','Research/probe-lock.json','Research/runtime-lock.json']:
        sources[name]=sha(ROOT/name)
    write_json(out/'package.json',{'schema_version':1,'kind':'technical-workbench','files':payload,'source_inputs':sources,
        'proof_limit':'Install inventory only; compare with build receipts. Not Windows validation, complete third-party shipping clearance or release approval.'})
    print(json.dumps({'package':str(out),'payload_files':len(payload)},indent=2))

if __name__=='__main__':main()
