"""Per-section remaining capacity, retaining a reserve on both production and isolation drives."""
import argparse,json,shutil
from pathlib import Path
from north_row4_contract import ROOT,contract

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--step',type=int,required=True);p.add_argument('--date',required=True);p.add_argument('--isolation',type=Path,required=True);p.add_argument('--output',type=Path)
    a=p.parse_args();c=contract(a.step,a.date);remaining=7-a.step
    root_free=shutil.disk_usage(ROOT).free;iso_free=shutil.disk_usage(a.isolation).free
    root_growth=(2*remaining+5)*1024**3;iso_growth=remaining*1024**3;reserve=3*1024**3
    shared_volume=ROOT.resolve().anchor.lower()==a.isolation.resolve().anchor.lower()
    shared_required=root_growth+iso_growth+reserve
    if shared_volume and min(root_free,iso_free)<=shared_required:raise ValueError(f'Shared-volume capacity gate blocked: free {min(root_free,iso_free)}, required {shared_required}')
    if root_free<=root_growth+reserve or iso_free<=iso_growth+reserve:raise ValueError(f'Capacity gate blocked: root free {root_free}, required {root_growth+reserve}; isolation free {iso_free}, required {iso_growth+reserve}')
    result={'status':'remaining_row_capacity_gate_passed','step':a.step,'remaining_sections':remaining,'root_free_bytes':root_free,'root_growth_allowance_bytes':root_growth,'isolation_free_bytes':iso_free,'isolation_growth_allowance_bytes':iso_growth,'reserve_bytes_each_drive':reserve,'shared_volume':shared_volume,'shared_volume_required_bytes':shared_required if shared_volume else None,'policy':'Preserve source, old validation and rollback owners; stop before reserve violation'}
    if a.output:
        if a.output.exists():raise ValueError('Fresh capacity evidence path required')
        a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_bytes((json.dumps(result,indent=2)+'\n').encode())
    print(json.dumps(result,indent=2))
