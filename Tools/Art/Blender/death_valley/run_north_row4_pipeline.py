"""Fresh, hidden isolated Unity stages for the bounded fourth northern row."""
import argparse,json,os,subprocess,time
from pathlib import Path
from north_row4_contract import ROOT,contract

def run(stage,step,date,isolation,version,graphics=False,test_filter=None,seal_native_proof=True):
    c=contract(step,date);isolation=isolation.resolve()
    if isolation==ROOT or not (isolation/'ProjectSettings/ProjectVersion.txt').is_file() or (isolation/'.git').exists():
        raise ValueError('A distinct physical validation project is required')
    target=isolation.as_posix().replace("'","''")
    query=f"@(Get-CimInstance Win32_Process | Where-Object {{$_.Name -eq 'Unity.exe' -and $_.CommandLine.Replace([char]92,[char]47) -like '*{target}*'}}).Count"
    if int(subprocess.check_output(['powershell.exe','-NoProfile','-Command',query],text=True).strip()):
        raise ValueError('Target Editor already running; wait for its terminal result')
    suffix='' if version=='01' else version
    prefix=f'step{step}-{stage}{suffix}'
    log=isolation/'Logs'/(prefix+'.log');log.parent.mkdir(exist_ok=True)
    if log.exists():raise ValueError('Fresh proof version required; never overwrite a prior log')
    editor=Path(os.environ['LOCALAPPDATA'])/'Unity/Editors/6000.4.0f1/Editor/Unity.exe'
    args=[str(editor),'-projectPath',str(isolation),'-batchmode','-northRow4Step',str(step),'-northRow4Date',date,'-logFile',str(log)]
    if not graphics:args+=['-nographics']
    methods={'candidate':'BadwaterNorthRow4Builder.BuildCandidateFromCli','candidate-check':'BadwaterNorthRow4Validator.ValidateFromCli','integrate':'BadwaterNorthRow4Builder.IntegrateSavedSceneFromCli','production-check':'BadwaterNorthRow4Validator.ValidateFromCli','playable':'BadwaterPlayableSceneBuilder.ValidateFromCli','map':'DeathValleyMapWindow.RenderFromCli'}
    if stage in ('tests','current-catalog-tests','contract-tests'):
        xml=isolation/'Logs'/(prefix+'.xml')
        if xml.exists():raise ValueError('Fresh test XML required')
        args+=['-runTests','-testPlatform','EditMode','-testFilter',test_filter or 'BadwaterNorthRow4Tests;DeathValleyMapTests','-testResults',str(xml)]
    else:
        args+=['-quit']
        if stage!='import':args+=['-executeMethod','BooterBigArm.Editor.'+methods[stage]]
        if stage in ('candidate-check','production-check'):
            report=isolation/'Logs'/f'step{step}-{stage.replace("-check","-readback")}{suffix}.json'
            if report.exists():raise ValueError('Fresh native readback path required')
            args+=['-terrainExpansionReport',str(report)]
            if stage=='production-check':args+=['-terrainExpansionScene','Assets/_Project/Scenes/Production/GreaterWasteland.unity','-northRow4SealProof','true' if seal_native_proof else 'false']
    env=dict(os.environ,TEMP='D:/BooterBigArmTools/Temp',TMP='D:/BooterBigArmTools/Temp')
    start=time.monotonic()
    result=subprocess.run(args,env=env,creationflags=subprocess.CREATE_NO_WINDOW)
    if result.returncode:raise RuntimeError(f'Unity {stage} exited {result.returncode}; inspect {log}')
    print(json.dumps({'status':'isolated_Unity_stage_exited_successfully','stage':stage,'step':step,'seconds':time.monotonic()-start,'log':str(log)}),flush=True)

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--stage',choices=['import','candidate','candidate-check','integrate','production-check','tests','current-catalog-tests','contract-tests','playable','map'],required=True)
    p.add_argument('--step',type=int,required=True);p.add_argument('--date',required=True)
    p.add_argument('--isolation',type=Path,required=True);p.add_argument('--proof-version',default='01')
    p.add_argument('--test-filter');p.add_argument('--no-seal-native-proof',action='store_true');a=p.parse_args()
    run(a.stage,a.step,a.date,a.isolation,a.proof_version,a.stage=='map',a.test_filter,not a.no_seal_native_proof)
