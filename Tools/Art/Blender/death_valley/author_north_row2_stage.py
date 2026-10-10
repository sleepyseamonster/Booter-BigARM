"""Run one bounded native authoring stage; visual approval remains a separate gate."""
import argparse
from pathlib import Path
import subprocess
import sys
from north_row2_contract import contract

if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--step',type=int,required=True)
    parser.add_argument('--date',required=True)
    parser.add_argument('--stage',choices=['pilot','section','combined'],required=True)
    parser.add_argument('--blender',type=Path,required=True)
    args=parser.parse_args()
    c=contract(args.step,args.date)
    directory=c['data']/'Prepared01'
    manifest=directory/'manifest.json'
    section=c['section'] if args.stage=='section' else args.stage
    stem='Pilot01' if args.stage=='pilot' else section+'_01' if args.stage=='section' else 'combined01'
    blend=c['art']/(stem+'.blend')
    proof=directory/(section+'_blender_proof.json')
    views=['junction'] if args.stage=='pilot' else ['overhead','oblique']
    stride=4 if args.stage=='combined' else 2
    launcher=Path(__file__).with_name('run_north_row2_blender.py')
    common=[sys.executable,str(launcher),'--blender',str(args.blender.resolve()),'--step',str(args.step),
            '--date',args.date,'--manifest',str(manifest),'--section',section,'--stride',str(stride)]
    stages=[('build',blend,None),('verify',proof,None)]+[('render',directory/(args.stage+'_'+view+'.png'),view) for view in views]
    if not manifest.is_file() or any(output.exists() for _,output,_ in stages) or proof.with_suffix('.npz').exists():
        raise ValueError('Fresh stage outputs and accepted preparation required')
    for mode,output,view in stages:
        log=c['data']/'NativeLogs01'/(args.stage+'_'+mode+('_'+view if view else '')+'.log')
        command=common+['--mode',mode,'--output',str(output),'--log',str(log)]
        if mode!='build':command+=['--blend',str(blend)]
        if view:command+=['--view',view]
        print('Native stage',args.stage,mode,view or '',flush=True)
        subprocess.run(command,check=True)
    print('Saved/reopened stage and views ready for inspection:',args.stage,flush=True)
