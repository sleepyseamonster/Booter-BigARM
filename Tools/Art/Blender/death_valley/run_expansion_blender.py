"""Launch the verified Blender runtime without foreground windows, with strict exit/output gates."""
import argparse
import json
from pathlib import Path
import subprocess
import time

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--blender',type=Path,required=True);p.add_argument('--manifest',type=Path,required=True)
    p.add_argument('--mode',choices=['build','verify','render'],required=True);p.add_argument('--section',required=True)
    p.add_argument('--output',type=Path,required=True);p.add_argument('--log',type=Path,required=True)
    p.add_argument('--blend',type=Path);p.add_argument('--stride',type=int,default=2);p.add_argument('--view',default='oblique')
    a=p.parse_args();output=a.output.resolve();log=a.log.resolve()
    if output.exists() or log.exists():raise ValueError('Use fresh output and log paths')
    cmd=[str(a.blender.resolve()),'--background','--factory-startup','--disable-autoexec','--python-exit-code','1']
    if a.mode!='build':
        if not a.blend or not a.blend.is_file():raise ValueError('Verification/render requires a saved source')
        cmd.append(str(a.blend.resolve()))
    cmd+=['--python',str(Path(__file__).with_name('build_expansion_scene.py')),'--','--manifest',str(a.manifest.resolve()),
          '--mode',a.mode,'--section',a.section,'--output',str(output),'--stride',str(a.stride),'--view',a.view]
    log.parent.mkdir(parents=True,exist_ok=True);start=time.monotonic()
    with log.open('x',encoding='utf-8') as handle:
        result=subprocess.run(cmd,stdout=handle,stderr=subprocess.STDOUT,creationflags=subprocess.CREATE_NO_WINDOW,timeout=3600)
    if result.returncode!=0 or not output.is_file():raise RuntimeError('Blender failed; inspect '+str(log))
    print(json.dumps({'mode':a.mode,'section':a.section,'seconds':time.monotonic()-start,'output':str(output),'bytes':output.stat().st_size,'exit_code':result.returncode}))

if __name__=='__main__':main()
