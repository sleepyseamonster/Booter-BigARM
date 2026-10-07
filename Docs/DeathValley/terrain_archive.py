"""Resolve immutable Mac terrain-source paths through a verified transfer receipt.

Creates local manifest views for retained validation tools without rewriting
the original acquisition metadata. No terrain or source file is modified.
"""
import argparse
import hashlib
import json
from pathlib import Path

HERE=Path(__file__).resolve().parent
ROOT=next(p for p in HERE.parents if (p/'ProjectSettings/ProjectVersion.txt').exists())
RECEIPT=ROOT/'SourceData/Terrain/DeathValley/MacSnapshot2026-10-07/transfer_receipt.json'
MAC_PREFIX='/Users/worldbuilder/Desktop/Death Valley Terrain Data/'


class TerrainArchive:
    def __init__(self,root,receipt):
        self.root=Path(root).resolve()
        self.dataset=(self.root/receipt['data_root']).resolve()
        if not self.dataset.is_relative_to(self.root):raise ValueError('Dataset escapes repository')
        self.checked={}
        self.records={r['source_relative']:r for r in receipt['records'] if r['status']!='excluded_mac_virtual_environment'}
        if len(self.records)!=sum(r['status']!='excluded_mac_virtual_environment' for r in receipt['records']):
            raise ValueError('Duplicate archived source identity')

    def resolve(self,value):
        if value==MAC_PREFIX.rstrip('/'):return str(self.dataset)
        if not isinstance(value,str) or not value.startswith(MAC_PREFIX):return value
        relative=value[len(MAC_PREFIX):]
        if relative not in self.records:
            directory=(self.dataset/relative).resolve()
            if directory.is_relative_to(self.dataset) and directory.is_dir():return str(directory)
            raise ValueError('Mac source path missing from transfer receipt: '+relative)
        destination=self.records[relative]['destination']
        if Path(destination).is_absolute():raise ValueError('Archive destination must be relative')
        path=(self.root/destination).resolve()
        if not path.is_relative_to(self.root):raise ValueError('Archive destination escapes repository')
        if not path.is_file():raise FileNotFoundError(path)
        expected=self.records[relative]['sha256']
        if path not in self.checked:
            digest=hashlib.sha256()
            with path.open('rb') as stream:
                for block in iter(lambda:stream.read(4*1024*1024),b''):digest.update(block)
            self.checked[path]=digest.hexdigest()
        if self.checked[path]!=expected:raise ValueError('Archived source bytes changed: '+relative)
        return str(path)

    def rewrite(self,value):
        if isinstance(value,dict):return {k:self.rewrite(v) for k,v in value.items()}
        if isinstance(value,list):return [self.rewrite(v) for v in value]
        return self.resolve(value)


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--manifest',type=Path,required=True)
    parser.add_argument('--output',type=Path,required=True)
    args=parser.parse_args()
    output=args.output.resolve();allowed=(ROOT/'Logs/DeathValleyTransfer').resolve()
    if not output.is_relative_to(allowed) or output.exists():raise ValueError('Use a new output file under Logs/DeathValleyTransfer')
    archive=TerrainArchive(ROOT,json.loads(RECEIPT.read_text(encoding='utf-8')))
    converted=archive.rewrite(json.loads(args.manifest.read_text(encoding='utf-8')))
    output.parent.mkdir(parents=True,exist_ok=True)
    with output.open('x',encoding='utf-8',newline='\n') as stream:stream.write(json.dumps(converted,indent=2)+'\n')
    print(output)


if __name__=='__main__':main()
