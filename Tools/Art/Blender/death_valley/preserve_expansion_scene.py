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


def preserve(original,current,parent_id,section_count=3,terrain_count=768):
    if section_count<1 or terrain_count!=section_count*256:raise ValueError('Expected complete 256-terrain sections')
    prefix,old=split_documents(original);_,new=split_documents(current)
    if set(old)-set(new):raise ValueError('Existing scene documents removed')
    if parent_id not in old:raise ValueError('Original terrain parent absent')
    pattern=r'(?m)^  m_Children:\r?\n(?:  - \{fileID: \d+\}\r?\n)*'
    before=re.search(pattern,old[parent_id]);after=re.search(pattern,new[parent_id])
    if not before or not after:raise ValueError('Terrain parent child list missing')
    prior=re.findall(r'fileID: (\d+)',before[0]);now=re.findall(r'fileID: (\d+)',after[0])
    if now[:len(prior)]!=prior or len(now)!=len(prior)+section_count or any(i in old for i in now[len(prior):]):
        raise ValueError('Unexpected appended native section children')
    added={k:v for k,v in new.items() if k not in old}
    if len(added)!=terrain_count*5+section_count*2 or any(i not in added for i in now[len(prior):]):
        raise ValueError('Unexpected native terrain/section document count')
    old[parent_id]=old[parent_id][:before.start()]+after[0]+old[parent_id][before.end():]
    return prefix+''.join(old.values())+''.join(added.values())


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--before',type=Path,required=True);p.add_argument('--scene',type=Path,required=True)
    p.add_argument('--parent-transform-id',required=True)
    p.add_argument('--sections',type=int,default=3)
    p.add_argument('--new-terrains',type=int,default=768)
    a=p.parse_args()
    result=preserve(a.before.read_bytes().decode('utf-8'),a.scene.read_bytes().decode('utf-8'),a.parent_transform_id,a.sections,a.new_terrains)
    a.scene.write_bytes(result.encode('utf-8'))
    print(f'Original scene documents preserved; {a.sections} section children and {a.new_terrains*5+a.sections*2} new native documents retained')
