"""One bounded rock render/edit pass; no gameplay input or repeated stress cycles."""
import argparse
import json
from pathlib import Path
import subprocess
import sys
import hashlib
import shutil
from common import ROOT, inside, write_json
from verify_foundation import png
from verify_animation import compare


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--catalog')
    parser.add_argument('--recipe')
    parser.add_argument('--package',help='Verify a fresh package through its launcher from an unrelated working directory')
    parser.add_argument('--out',required=True)
    args=parser.parse_args()
    out=inside(args.out)
    if out.exists():parser.error('Choose a new evidence directory')
    package=inside(args.package) if args.package else None
    if package:
        if (package/'UserData').exists():parser.error('Package verification requires a fresh package without user data')
        executable=package/'bin/engine_workbench'
        catalog=package/'bin/Assets/catalog.json';recipe=package/'bin/Assets/Recipes/rock.json'
        launcher=package/'Launch-Rock-Generator.command'
        inventory=json.loads((package/'package.json').read_text())['files']
        for name,digest in inventory.items():
            path=inside(package/name)
            if package not in path.parents:raise ValueError('Package inventory escapes root')
            if hashlib.sha256(path.read_bytes()).hexdigest()!=digest:raise ValueError('Changed package payload: '+name)
        cwd=out/'unrelated-cwd';cwd.mkdir(parents=True)
    else:
        if not args.catalog or not args.recipe:parser.error('Supply --package or both --catalog and --recipe')
        catalog,recipe=map(inside,(args.catalog,args.recipe))
        executable=ROOT/'build/foundation/engine_workbench';cwd=ROOT
    command=[sys.executable,str(ROOT/'Tools/record.py'),'--out',str(out/'run'),'--cwd',str(cwd),'--timeout','60']
    inputs=[executable,catalog,recipe,Path(__file__),*sorted((executable.parent/'Shaders').glob('*.bin'))]
    if package:inputs += [launcher,package/'package.json']
    for path in inputs:command+=['--input',str(path)]
    if package:command+=['--',str(launcher),'--verify-rock',str(out/'captures')]
    else:command+=['--',str(executable),'--catalog',str(catalog),'--rock',str(recipe),'--verify-rock',str(out/'captures')]
    subprocess.run(command,cwd=ROOT,check=True,stdout=subprocess.DEVNULL)
    report=json.loads((out/'captures/rock.json').read_text())['payload']
    images={name:png(out/'captures'/(name+'.png')) for name in ['lod0','lod1','lod2','edited','undo','opposite']}
    comparisons={name:compare(images[a],images[b]) for name,a,b in [('lod1','lod0','lod1'),('lod2','lod1','lod2'),('edit','lod0','edited'),('undo','lod0','undo'),('opposite','lod0','opposite')]}
    passed=report['passed'] and report['real_surfaces'] and comparisons['undo']['changed_over_3']==0 and all(comparisons[name]['changed_over_3']>100 for name in ['lod1','lod2','edit','opposite'])
    package_checks={}
    if package:
        working=package/'UserData/rock.json'
        if working.read_bytes()!=recipe.read_bytes():raise ValueError('Fresh launcher did not preserve bundled recipe inputs')
        edited=json.loads(working.read_text());edited['payload']['seed']+=1;write_json(working,edited)
        before=working.read_bytes()
        subprocess.run([str(launcher),'--build-info'],cwd=cwd,check=True,stdout=subprocess.DEVNULL)
        if working.read_bytes()!=before:raise ValueError('Launcher replaced existing working recipe')
        package_checks={'inventory_files':len(inventory),'launch_from_unrelated_cwd':True,'existing_recipe_preserved':True,'inspection_saved':(package/'UserData/inspection.json').is_file()}
        if not package_checks['inspection_saved']:raise ValueError('Packaged inspection save missing')
        shutil.copyfile(working,out/'preserved-working-recipe.json')
        # UserData was absent on entry and contains only this verifier's files.
        shutil.rmtree(package/'UserData')
    result={'package_checks':package_checks,'result':'passed' if passed else 'failed','native':report,'comparisons':comparisons,'limits':'One Mac Metal rock and edit/undo sequence; no gameplay, Windows, final-art or population-performance claim.'}
    write_json(out/'result.json',result);print(json.dumps(result,indent=2))
    if not passed:raise ValueError('Rock render/edit comparison failed')


if __name__=='__main__':main()
