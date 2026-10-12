"""Root transfer exclusion: fresh live checks or a held exclusive offline lock."""
import contextlib,ctypes,json,os,subprocess
from pathlib import Path
from north_row4_contract import ROOT,contract,sha256

def root_processes():
    query="@((Get-CimInstance Win32_Process | Where-Object {$_.Name -eq 'Unity.exe'}) | Select-Object ProcessId,CommandLine) | ConvertTo-Json -Compress"
    raw=subprocess.check_output(['powershell.exe','-NoProfile','-Command',query],text=True,creationflags=subprocess.CREATE_NO_WINDOW).strip()
    if not raw:return []
    result=json.loads(raw);processes=result if isinstance(result,list) else [result]
    if any(not process.get('CommandLine') for process in processes):
        raise ValueError('Unity project ownership is unreadable; offline transfer is unavailable')
    path=ROOT.as_posix().lower()
    return [process for process in processes if path in process['CommandLine'].replace('\\','/').lower()]

def command(name,*args):
    cli=Path(os.environ['LOCALAPPDATA'])/'Unity/bin/unity.exe'
    args=[str(cli),'command',name,*args,'--json','--project-path',str(ROOT),'--timeout','60']
    data=json.loads(subprocess.check_output(args,text=True,encoding='utf-8',creationflags=subprocess.CREATE_NO_WINDOW))
    if not data.get('success'):raise ValueError('Fresh live Editor guard failed: '+name)
    return data['data']['result']

def saved_guard(step,date):
    c=contract(step,date);bm=json.loads((c['data']/'Baseline01/baseline.json').read_text(encoding='utf-8'))
    actual=sha256(ROOT/bm['scene'])
    if actual!=bm['scene_sha256']:
        if step!=5 or date!='2026-10-11':raise ValueError('Saved committed predecessor changed before transfer')
        receipt=ROOT/'Docs/Evidence/DeathValley/NORTH_ROW4_STEP05_COMMITTED_SCENE_RECONCILIATION_2026-10-12.json'
        if not receipt.is_file():raise ValueError('Exact committed camp change awaits fresh reconciliation proof; preserve root')
        r=json.loads(receipt.read_text(encoding='utf-8'))
        if r['status']!='committed_camp_overlay_freshly_validated' or r['step']!=5 or r['captured_predecessor_scene_sha256']!=bm['scene_sha256'] or r['committed_transfer_predecessor_scene_sha256']!=actual:
            raise ValueError('Exact validated committed camp reconciliation required')
        committed=subprocess.check_output(['git','show',r['source_commit']+':'+bm['scene']],cwd=ROOT)
        if committed!=(ROOT/bm['scene']).read_bytes():raise ValueError('Root differs from specifically reconciled commit')
        subprocess.run(['git','merge-base','--is-ancestor',r['source_commit'],'HEAD'],cwd=ROOT,check=True)
        for f in r['dependencies']:
            if sha256(c['data']/'Reconciliation01/Dependencies'/f['path'])!=f['sha256']:raise ValueError('Frozen committed validation dependency changed')
        bm=dict(bm,captured_predecessor_scene_sha256=bm['scene_sha256'],scene_sha256=actual,reconciliation_receipt_sha256=sha256(receipt))
    if sha256(ROOT/bm['predecessor_manifest'])!=bm['predecessor_manifest_sha256']:
        raise ValueError('Saved committed predecessor or native manifest changed before transfer')
    subprocess.run(['git','diff','--quiet','HEAD','--',bm['scene']],cwd=ROOT,check=True)
    return bm

def classify_transport_errors(truth,entries):
    if truth['compilationFailed'] or truth['compiling']:raise ValueError('Fresh compilation failure or active compilation blocks transfer')
    errors=entries['errors']
    if len(errors)!=truth['consoleErrors']:raise ValueError('Current console severity count is inconsistent')
    for error in errors:
        message=error['message']
        if error['mode']!=8405248 or message.split('\n',1)[0]!='Failed to handle /api/exec request: Main thread operation timed out after 5000ms' or 'Unity.Pipeline.BasePipelineServer/<HandleExecRequest>' not in message:
            raise ValueError('Unclassified current console error blocks transfer')
    return errors

def compiler_guard():
    truth=command('console_status')['groundTruth']
    if truth['compilationFailed'] or truth['compiling']:raise ValueError('Fresh compilation failure or active compilation blocks transfer')
    if truth['consoleErrors']:
        result=command('eval_file',str(ROOT/'Tools/Art/Blender/death_valley/editor_queries/current_console_errors.cs'),'60000')
        if not result['success']:raise ValueError('Current console inspection failed')
        current=command('console_status')['groundTruth']
        classified=classify_transport_errors(current,result['result'])
        return dict(current,classified_transport_errors=classified,console_cleared=False)
    return truth

def live_guard(step,date):
    bm=saved_guard(step,date)
    scenes=command('list_open_scenes')['scenes']
    if any(s['path']==bm['scene'] for s in scenes):raise ValueError('Production must be unloaded for transfer')
    queries=ROOT/'Tools/Art/Blender/death_valley/editor_queries'
    dirty=command('eval_file',str(queries/'dirty_terrain_assets.cs'),'60000')['result']
    if dirty:raise ValueError('Unsaved persistent terrain assets block transfer')
    state=command('eval','return new { playing = UnityEditor.EditorApplication.isPlaying, compiling = UnityEditor.EditorApplication.isCompiling };')['result']
    if state['playing'] or state['compiling']:raise ValueError('Play Mode or compiling blocks transfer')
    truth=compiler_guard()
    fingerprint=command('eval_file',str(queries/'current_scene_fingerprint.cs'),'60000')['result']
    return {'status':'fresh_live_transfer_guard_passed','scene_sha256':bm['scene_sha256'],'scenes':scenes,'editor_state':state,'compiler_truth':truth,'fingerprint':fingerprint}

@contextlib.contextmanager
def offline_lock():
    if root_processes():raise ValueError('Root Editor exists; exclusive offline path is unavailable')
    lock=ROOT/'Temp/UnityLockfile'
    if not lock.is_file():raise ValueError('Offline transfer requires an existing lockfile to hold exclusively; preserve Temp state')
    kernel=ctypes.WinDLL('kernel32',use_last_error=True)
    kernel.CreateFileW.argtypes=[ctypes.c_wchar_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p]
    kernel.CreateFileW.restype=ctypes.c_void_p
    kernel.CloseHandle.argtypes=[ctypes.c_void_p]
    handle=kernel.CreateFileW(str(lock),0x80000000,0,None,3,0x80,None)
    if handle==ctypes.c_void_p(-1).value:raise OSError(ctypes.get_last_error(),'Root lock is owned; preserve Editor state')
    try:
        if root_processes():raise ValueError('Root Editor appeared while taking exclusive lock')
        yield {'status':'offline_root_exclusion_guard_passed','root_processes':[],'exclusive_lock_path':str(lock),'lockfile_modified_or_deleted':False,'loaded_scene_policy':'No root Editor process; saved bytes only, preserve recovery files'}
    finally:kernel.CloseHandle(handle)
