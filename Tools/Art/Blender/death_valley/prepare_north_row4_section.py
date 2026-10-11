"""Discover every accepted retained model owner without guessing execution dates."""
import argparse,sys
from pathlib import Path
from north_row4_contract import ROOT,contract
from prepare_north_row4 import prepare
sys.path.insert(0,str(ROOT/'Docs/DeathValley'))
from north_row2_coverage import newest as second_row
from north_row3_coverage import newest as third_row
from north_row4_coverage import newest as fourth_row

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--step',type=int,required=True);p.add_argument('--date',required=True)
    a=p.parse_args();c=contract(a.step,a.date)
    models=[ROOT/'SourceData/Terrain/DeathValley/NorthRow2Survey2026-10-09/RetainedModel01/manifest.json']
    for adapter,count,steps in [(second_row,4608,6),(third_row,6144,6),(fourth_row,7680,a.step-1)]:
        for k in range(1,steps+1):
            _,m=adapter(ROOT,count+256*k)
            models.append(ROOT/'SourceData/Terrain/DeathValley'/m['batch']/'AcceptedModel01/manifest.json')
    if not all(path.is_file() for path in models):raise ValueError('Every immutable accepted native model bank is required')
    prepare(a.step,a.date,c['data']/'Baseline01/baseline.json',models,c['data']/'Prepared01')
