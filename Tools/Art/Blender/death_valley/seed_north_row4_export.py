"""Seed a fresh isolated source owner only against an identical saved predecessor."""
import argparse,json,shutil,subprocess
from pathlib import Path
from north_row4_contract import ROOT,contract,sha256

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--step',type=int,required=True);p.add_argument('--date',required=True);p.add_argument('--isolation',type=Path,required=True)
    a=p.parse_args();c=contract(a.step,a.date);iso=a.isolation.resolve()
    if iso==ROOT or (iso/'.git').exists():raise ValueError('Physical isolation required')
    target=str(iso).replace("'","''")
    query=f"@(Get-CimInstance Win32_Process | Where-Object {{$_.Name -eq 'Unity.exe' -and $_.CommandLine -like '*{target}*'}}).Count"
    if int(subprocess.check_output(['powershell.exe','-NoProfile','-Command',query],text=True).strip()):raise ValueError('Isolated Editor must be stopped before seeding')
    bm=json.loads((c['data']/'Baseline01/baseline.json').read_text(encoding='utf-8'))
    if sha256(ROOT/bm['scene'])!=bm['scene_sha256'] or sha256(iso/bm['scene'])!=bm['scene_sha256']:
        raise ValueError('Root and isolated committed predecessors differ')
    source=c['data']/'RuntimeExport01';dest=iso/c['asset']/'Source'
    if not (source/'manifest.json').is_file() or dest.parent.exists():raise ValueError('Fresh complete export and absent native owner required')
    shutil.copytree(source,dest)
    for file in source.rglob('*'):
        if file.is_file() and sha256(file)!=sha256(dest/file.relative_to(source)):raise ValueError('Seeded export bytes differ')
    print('Seeded exact fresh runtime source owner:',dest)
