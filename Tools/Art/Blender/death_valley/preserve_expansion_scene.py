"""Preserve original scene documents after Unity's additive terrain save."""
import argparse
from pathlib import Path
import re


def split_documents(text):
    headers=list(re.finditer(r'(?m)^--- !u!\d+ &(\d+)[^\r\n]*\r?\n',text))
    if not headers:raise ValueError('Missing native Unity scene documents')
    documents={}
    for i,match in enumerate(headers):
        ident=match[1]
        if ident in documents:raise ValueError('Duplicate scene document identity')
        documents[ident]=text[match.start():headers[i+1].start() if i+1<len(headers) else len(text)]
    return text[:headers[0].start()],documents


def preserve(original,current,parent_id):
    prefix,old=split_documents(original);_,new=split_documents(current)
    if set(old)-set(new):raise ValueError('Existing scene documents removed')
    if parent_id not in old:raise ValueError('Original terrain parent absent')
    pattern=r'(?m)^  m_Children:\r?\n(?:  - \{fileID: \d+\}\r?\n)*'
    before=re.search(pattern,old[parent_id]);after=re.search(pattern,new[parent_id])
    if not before or not after:raise ValueError('Terrain parent child list missing')
    prior=re.findall(r'fileID: (\d+)',before[0]);now=re.findall(r'fileID: (\d+)',after[0])
    if now[:len(prior)]!=prior or len(now)!=len(prior)+3 or any(i in old for i in now[len(prior):]):
        raise ValueError('Expected exactly three appended native section children')
    added={k:v for k,v in new.items() if k not in old}
    if len(added)!=3846 or any(i not in added for i in now[len(prior):]):
        raise ValueError('Expected 768 native terrain objects and three section parents')
    old[parent_id]=old[parent_id][:before.start()]+after[0]+old[parent_id][before.end():]
    return prefix+''.join(old.values())+''.join(added.values())


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--before',type=Path,required=True);p.add_argument('--scene',type=Path,required=True)
    p.add_argument('--parent-transform-id',required=True)
    a=p.parse_args()
    result=preserve(a.before.read_bytes().decode('utf-8'),a.scene.read_bytes().decode('utf-8'),a.parent_transform_id)
    a.scene.write_bytes(result.encode('utf-8'))
    print('Original scene documents preserved; three section children and 3846 new native documents retained')
