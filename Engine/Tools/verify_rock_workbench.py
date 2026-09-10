"""One bounded rock render/edit pass; no gameplay input or repeated stress cycles."""
import argparse
import json
from pathlib import Path
import subprocess
import sys
from common import ROOT, inside, write_json
from verify_foundation import png
from verify_animation import compare


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--catalog',required=True)
    parser.add_argument('--recipe',required=True)
    parser.add_argument('--out',required=True)
    args=parser.parse_args()
    catalog,recipe,out=map(inside,(args.catalog,args.recipe,args.out))
    if out.exists():parser.error('Choose a new evidence directory')
    executable=ROOT/'build/foundation/engine_workbench'
    command=[sys.executable,str(ROOT/'Tools/record.py'),'--out',str(out/'run'),'--timeout','60']
    for path in [executable,catalog,recipe,Path(__file__),*sorted((ROOT/'build/foundation/Shaders').glob('*.bin'))]:command+=['--input',str(path)]
    command+=['--',str(executable),'--catalog',str(catalog),'--rock',str(recipe),'--verify-rock',str(out/'captures')]
    subprocess.run(command,cwd=ROOT,check=True,stdout=subprocess.DEVNULL)
    report=json.loads((out/'captures/rock.json').read_text())['payload']
    images={name:png(out/'captures'/(name+'.png')) for name in ['lod0','lod1','lod2','edited','undo','opposite']}
    comparisons={name:compare(images[a],images[b]) for name,a,b in [('lod1','lod0','lod1'),('lod2','lod1','lod2'),('edit','lod0','edited'),('undo','lod0','undo'),('opposite','lod0','opposite')]}
    passed=report['passed'] and report['real_surfaces'] and comparisons['undo']['changed_over_3']==0 and all(comparisons[name]['changed_over_3']>100 for name in ['lod1','lod2','edit','opposite'])
    result={'result':'passed' if passed else 'failed','native':report,'comparisons':comparisons,'limits':'One Mac Metal rock and edit/undo sequence; no gameplay, Windows, final-art or population-performance claim.'}
    write_json(out/'result.json',result);print(json.dumps(result,indent=2))
    if not passed:raise ValueError('Rock render/edit comparison failed')


if __name__=='__main__':main()
