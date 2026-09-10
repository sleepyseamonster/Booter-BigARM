"""One render-only region load/retire/return check. No simulated gameplay input."""
import argparse
import json
from pathlib import Path
import subprocess
import sys
from common import ROOT,inside,write_json
from verify_foundation import png
from verify_animation import compare

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    for name in ['catalog','terrain','rock','out']:parser.add_argument('--'+name,required=True)
    args=parser.parse_args();catalog,terrain,rock,out=map(inside,(args.catalog,args.terrain,args.rock,args.out))
    if out.exists():parser.error('Choose a new evidence directory')
    executable=ROOT/'build/foundation/engine_workbench'
    command=[sys.executable,str(ROOT/'Tools/record.py'),'--out',str(out/'run'),'--timeout','90']
    for path in [executable,catalog,terrain,rock,Path(__file__),*sorted((executable.parent/'Shaders').glob('*.bin'))]:command+=['--input',str(path)]
    command+=['--',str(executable),'--catalog',str(catalog),'--terrain',str(terrain),'--stream-rock',str(rock),'--verify-stream',str(out/'captures')]
    subprocess.run(command,cwd=ROOT,check=True,stdout=subprocess.DEVNULL)
    report=json.loads((out/'captures/stream.json').read_text())['payload']
    origin=png(out/'captures/origin.png');distant=png(out/'captures/distant.png');returned=png(out/'captures/returned.png')
    comparisons={'distant':compare(origin,distant),'returned':compare(origin,returned)}
    passed=report['passed'] and report['real_surfaces'] and comparisons['returned']['changed_over_3']==0 and comparisons['distant']['changed_over_3']>100
    result={'result':'passed' if passed else 'failed','native':report,'comparisons':comparisons,'limits':'Mac Metal load/retire/return with technical anchors; no gameplay, companion, world save, origin rebasing or Windows claim.'}
    write_json(out/'result.json',result);print(json.dumps(result,indent=2))
    if not passed:raise ValueError('Stream image/resource comparison failed')

if __name__=='__main__':main()
