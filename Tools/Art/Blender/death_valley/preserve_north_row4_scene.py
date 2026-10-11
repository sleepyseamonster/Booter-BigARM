"""Restore retained scene documents exactly after isolated native integration."""
import argparse,re
from pathlib import Path
from north_row4_contract import contract,sha256
from preserve_expansion_scene import split_documents

def preserve(before,current):
    prefix,old=split_documents(before);_,new=split_documents(current)
    parent='1259644692'
    if set(old)-set(new) or parent not in old:raise ValueError('Existing native scene document removed')
    pattern=r'(?m)^  m_Children:\r?\n(?:  - \{fileID: \d+\}\r?\n)*'
    a=re.search(pattern,old[parent]);b=re.search(pattern,new[parent])
    if not a or not b:raise ValueError('Terrain parent child list missing')
    prior=re.findall(r'fileID: (\d+)',a[0]);now=re.findall(r'fileID: (\d+)',b[0])
    added={k:v for k,v in new.items() if k not in old}
    if now[:len(prior)]!=prior or len(now)!=len(prior)+1 or now[-1] not in added or len(added)!=1314:
        raise ValueError('Expected exactly one appended sixteen-sector section and 1314 documents')
    counts={kind:sum(v.startswith(f'--- !u!{kind} ') for v in added.values()) for kind in (218,154)}
    if counts!={218:256,154:256}:raise ValueError('Added Terrain/TerrainCollider count differs')
    old[parent]=old[parent][:a.start()]+b[0]+old[parent][a.end():]
    return prefix+''.join(old.values())+''.join(added.values())

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--step',type=int,required=True);p.add_argument('--date',required=True);p.add_argument('--scene',type=Path,required=True)
    a=p.parse_args();c=contract(a.step,a.date);before=c['data']/'Baseline01/production_before.unity'
    result=preserve(before.read_bytes().decode('utf-8'),a.scene.read_bytes().decode('utf-8'))
    a.scene.write_bytes(result.encode('utf-8'))
    print('Retained scene documents restored byte-exact except terrain-parent child append; scene SHA256 '+sha256(a.scene))
