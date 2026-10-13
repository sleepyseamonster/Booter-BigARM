"""Observe one task-owned Windows process with bounded time and memory; never restart it."""
import argparse
import ctypes
from ctypes import wintypes
import json
from pathlib import Path
import subprocess
import time


class Memory(ctypes.Structure):
    _fields_ = [('length', wintypes.DWORD), ('load', wintypes.DWORD)] + [(n, ctypes.c_ulonglong) for n in
        ['total_phys','avail_phys','total_page','avail_page','total_virtual','avail_virtual','avail_extended']]


class Counters(ctypes.Structure):
    _fields_ = [('cb',wintypes.DWORD),('faults',wintypes.DWORD)] + [(n,ctypes.c_size_t) for n in
        ['peak_working','working','peak_paged','paged','peak_nonpaged','nonpaged','pagefile','peak_pagefile','private']]


def main():
    parser=argparse.ArgumentParser(); parser.add_argument('--receipt',required=True); parser.add_argument('--timeout',type=int,default=900)
    args=parser.parse_args(); path=Path(args.receipt).resolve()
    if path.parent.name != 'Logs' or not path.parent.parent.name.startswith('InputInteraction') or path.parent.parent.parent not in [Path('C:/BooterBigArmValidation'), Path('D:/BooterBigArmValidation')]:
        raise SystemExit('A task-owned InputInteraction launch receipt is required.')
    r=json.loads(path.read_text(encoding='utf-8-sig')); pid=int(r['pid']); candidate=r['candidate']
    command=f"Get-CimInstance Win32_Process -Filter 'ProcessId = {pid}' | Select-Object ProcessId,CommandLine | ConvertTo-Json -Compress"
    value=subprocess.check_output(['powershell','-NoProfile','-Command',command],text=True).strip()
    if not value: raise SystemExit('The recorded process has already exited; inspect its output rather than restart it.')
    current=json.loads(value); normalized=current['CommandLine'].replace('\\','/').lower()
    if candidate.replace('\\','/').lower() not in normalized: raise SystemExit('Recorded PID no longer belongs to the candidate.')
    kernel=ctypes.WinDLL('kernel32',use_last_error=True); psapi=ctypes.WinDLL('psapi',use_last_error=True)
    kernel.OpenProcess.argtypes=[wintypes.DWORD,wintypes.BOOL,wintypes.DWORD]; kernel.OpenProcess.restype=wintypes.HANDLE
    kernel.WaitForSingleObject.argtypes=[wintypes.HANDLE,wintypes.DWORD]; kernel.GetExitCodeProcess.argtypes=[wintypes.HANDLE,ctypes.POINTER(wintypes.DWORD)]
    kernel.TerminateProcess.argtypes=[wintypes.HANDLE,wintypes.UINT]; kernel.CloseHandle.argtypes=[wintypes.HANDLE]
    psapi.GetProcessMemoryInfo.argtypes=[wintypes.HANDLE,ctypes.POINTER(Counters),wintypes.DWORD]
    handle=kernel.OpenProcess(0x100000|0x400|0x10|0x1,False,pid)
    if not handle: raise ctypes.WinError(ctypes.get_last_error())
    start=time.monotonic(); peak_private=peak_working=0; minimum_free=float('inf'); stopped=None; code=wintypes.DWORD()
    try:
        while kernel.WaitForSingleObject(handle,1000)==258:
            memory=Memory(); memory.length=ctypes.sizeof(memory)
            if not kernel.GlobalMemoryStatusEx(ctypes.byref(memory)): raise ctypes.WinError(ctypes.get_last_error())
            counters=Counters(); counters.cb=ctypes.sizeof(counters)
            if not psapi.GetProcessMemoryInfo(handle,ctypes.byref(counters),ctypes.sizeof(counters)): continue
            peak_private=max(peak_private,counters.private); peak_working=max(peak_working,counters.working); minimum_free=min(minimum_free,memory.avail_phys)
            if memory.avail_phys < 12*1024**3: stopped='free system memory below 12 GiB'
            elif counters.private > 20*1024**3 or counters.working > 20*1024**3: stopped='owned process memory above 20 GiB'
            elif time.monotonic()-start>args.timeout: stopped='owned process deadline exceeded'
            if stopped:
                kernel.TerminateProcess(handle,3); kernel.WaitForSingleObject(handle,10000); break
        kernel.GetExitCodeProcess(handle,ctypes.byref(code))
    finally: kernel.CloseHandle(handle)
    result={'pid':pid,'exit_code':code.value,'stopped_for_budget':stopped,'observed_seconds':time.monotonic()-start,
        'peak_private_bytes':peak_private,'peak_working_bytes':peak_working,'minimum_free_bytes':None if minimum_free==float('inf') else minimum_free,
        'limits':{'minimum_free_GiB':12,'maximum_owned_process_GiB':20,'deadline_seconds':args.timeout}}
    output=path.with_name(path.stem+'-watch.json'); output.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8'); print(json.dumps(result),flush=True)
    if stopped or code.value: raise SystemExit(1)


if __name__=='__main__': main()
