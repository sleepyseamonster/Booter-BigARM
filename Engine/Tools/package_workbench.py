"""Install a local technical workbench package and bind its payload to source hashes.

No publication. The destination must be new and inside Engine. The caller must
build and test first; this manifest does not replace native validation.
"""
import argparse
import hashlib
import json
import subprocess
import shutil
from pathlib import Path
from common import ROOT, inside, write_json

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--build',required=True)
    parser.add_argument('--out',required=True)
    parser.add_argument('--catalog',help='Optional cooked catalog copied into executable-relative Assets')
    parser.add_argument('--model',help='Optional cooked model manifest for the standalone player')
    parser.add_argument('--rock',help='Native recipe; adds a rock-generator launcher and writable working copy')
    args=parser.parse_args()
    if args.rock and not args.catalog:parser.error('--rock requires the cooked material catalog')
    build,out=inside(args.build),inside(args.out)
    if out.exists() or not (build/'CMakeCache.txt').is_file():
        parser.error('Build must be configured and package destination must be new')
    subprocess.run(['cmake','--install',str(build),'--config','Release','--prefix',str(out)],check=True,cwd=ROOT)
    executable=out/'bin'/('engine_workbench.exe' if (out/'bin/engine_workbench.exe').exists() else 'engine_workbench')
    if not executable.is_file() or not all((out/'bin/Shaders'/(p.stem+'.bin')).is_file() for p in (ROOT/'Shaders').glob('*.sc') if p.name.startswith(('vs_','fs_'))):
        raise ValueError('Incomplete installed workbench')
    if args.catalog:
        catalog=inside(args.catalog)
        document=json.loads(catalog.read_text())
        records=document["payload"]["textures"]
        if args.rock:
            required={"surface/textures/rocks/workbench/layered/rockworkbenchside_"+suffix for suffix in ['albedo','normal','surface']}
            required.add('surface/textures/ground/sanddirt/brokenworldsanddirtalbedo')
            records=[row for row in records if row['id'] in required]
            if {row['id'] for row in records}!=required:raise ValueError('Rock package requires all four current material textures')
            document['payload']['textures']=records
        asset_root=out/'bin/Assets';asset_root.mkdir()
        write_json(asset_root/'catalog.json',document)
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
    if args.rock:
        recipe=inside(args.rock)
        document=json.loads(recipe.read_text())
        if document.get('kind')!='engine.rock-recipe' or document.get('version')!=1:raise ValueError('Unsupported rock recipe')
        destination=out/'bin/Assets/Recipes';destination.mkdir(parents=True)
        shutil.copyfile(recipe,destination/'rock.json')
        windows=executable.suffix=='.exe'
        name='Launch-Rock-Generator.cmd' if windows else 'Launch-Rock-Generator.command'
        shutil.copyfile(ROOT/'Tools/Packaging'/name,out/name)
        if not windows:(out/name).chmod(0o755)
        (out/'START-HERE.md').write_text("""# Native Rock Generator — First Pass

Open **Launch-Rock-Generator.command** on Mac (the .cmd launcher is for a future native Windows package). Keep the package in a writable folder. No Unity installation or source checkout is needed.

1. Change the seed, radii, detail, irregularity or bands, then select **Apply recipe**.
2. Right-drag to orbit; use the wheel to zoom. Lighting/material controls are in the left inspector.
3. **Undo/Redo** restores accepted recipes. **Save recipe** saves the accepted recipe; **Reload** reads the displayed file.
4. Choose a new **Export directory**, then **Export rock** to write the accepted mesh, normals, bounds, triangle surface classes and recipe in the engine format.

The launcher creates UserData/rock.json on first use and never replaces it on later launches. Inspection settings save on normal exit. Defaults remain under bin/Assets. Exports default to UserData/Exports/rock-001; choose a new name for another export. Apply draft edits before saving or exporting. Closing without Save recipe discards unsaved recipe edits.

The model is a technical rock generator, not final geological art. Three basic detail levels and collision support are present. Exports contain the highest-detail static mesh and recipe, not embedded textures or a baked collision file. The workbench uses bundled triplanar textures; other applications need their own material binding. Changing the LOD preview does not change exported geometry.

Shared simulation can enable the third-person proxy to walk around the rock. Disable character mode to edit. Hands-on feel and creative acceptance are separate from technical verification. Live streaming and native Windows verification remain later engine work. This is a local development package, not a signed/notarized distribution release.
""",encoding='utf-8')
    payload={p.relative_to(out).as_posix():sha(p) for p in sorted(out.rglob('*')) if p.is_file()}
    sources={p.relative_to(ROOT).as_posix():sha(p) for folder in ['Source','Apps','Shaders','CMake']
             for p in sorted((ROOT/folder).rglob('*')) if p.is_file()}
    for name in ['CMakeLists.txt','CMakePresets.json','Research/probe-lock.json','Research/runtime-lock.json']:
        sources[name]=sha(ROOT/name)
    for path in [Path(__file__),*sorted((ROOT/'Tools/Packaging').glob('*'))]:sources[path.relative_to(ROOT).as_posix()]=sha(path)
    write_json(out/'package.json',{'schema_version':1,'kind':'native-rock-generator' if args.rock else ('technical-player-and-workbench' if args.model else 'technical-workbench'),'files':payload,'source_inputs':sources,
        'proof_limit':'Install inventory only; compare with build receipts. Not Windows validation, complete third-party shipping clearance or release approval.'})
    print(json.dumps({'package':str(out),'payload_files':len(payload)},indent=2))

if __name__=='__main__':main()
