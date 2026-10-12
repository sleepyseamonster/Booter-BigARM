"""Recover missing local canonical logs using exact accepted scene snapshots in isolation."""
import argparse, json
from pathlib import Path
from north_row4_contract import ROOT, contract, sha256
from run_north_row4_pipeline import run

def recover(isolation, output):
    isolation = isolation.resolve()
    if isolation == ROOT or (isolation / '.git').exists() or output.exists():
        raise ValueError('Fresh receipt and independent physical validation project required')
    relative = 'Assets/_Project/Scenes/Production/GreaterWasteland.unity'
    target = isolation / relative
    saved = target.read_bytes()
    original = sha256(target)
    if original != sha256(ROOT / relative):
        raise ValueError('Root and isolation accepted checkpoints differ')
    records = []
    try:
        for step in range(1, 4):
            c = contract(step, '2026-10-11')
            close_path = ROOT / 'Docs/Evidence/DeathValley' / c['batch'] / 'closeout.json'
            close = json.loads(close_path.read_text(encoding='utf-8'))
            old = close['canonical_playable_validator']
            if Path(old['local_log']).exists():
                raise ValueError('Recovery applies only to absent original local logs')
            snapshot = contract(step + 1, '2026-10-11')['data'] / 'Baseline01/production_before.unity'
            if sha256(snapshot) != close['production_scene_sha256']:
                raise ValueError('Exact accepted scene snapshot required')
            # run() checks process ownership before launching. Check before mutation too.
            import subprocess
            normalized = isolation.as_posix().replace("'", "''")
            query = f"@(Get-CimInstance Win32_Process | Where-Object {{$_.Name -eq 'Unity.exe' -and $_.CommandLine.Replace([char]92,[char]47) -like '*{normalized}*'}}).Count"
            if int(subprocess.check_output(['powershell.exe','-NoProfile','-Command',query],text=True).strip()):
                raise ValueError('Isolation must be offline before snapshot replacement')
            target.write_bytes(snapshot.read_bytes())
            run('playable', step, '2026-10-11', isolation, 'recovery01')
            log = isolation / f'Logs/step{step}-playablerecovery01.log'
            marker = f'Playable Badwater scene validated: {c["total"]} terrains, Booter, Legger, input, camera, UI, twilight and dust.'
            if marker not in log.read_text(encoding='utf-8') or sha256(target) != close['production_scene_sha256']:
                raise ValueError('Canonical marker or immutable snapshot verification failed')
            records.append(dict(step=step, closeout_sha256=sha256(close_path),
                original_missing_log=old['local_log'], original_log_sha256=old['sha256'],
                production_scene_sha256=close['production_scene_sha256'], manifest_sha256=close['manifest_sha256'],
                exact_snapshot_path=snapshot.relative_to(ROOT).as_posix(),
                local_log=str(log), sha256=sha256(log), exit_code=0))
    finally:
        target.write_bytes(saved)
        assert sha256(target) == original
    assert sha256(ROOT / relative) == original
    receipt = dict(schema_version=1, status='exact_accepted_snapshots_canonically_reverified',
        isolation=str(isolation), restored_isolation_scene_sha256=original,
        root_scene_unchanged=True, original_public_closeouts_unchanged=True, sections=records,
        limits='Fresh canonical verification replaces unavailable local logs; original log contents are not reconstructed.')
    output.write_text(json.dumps(receipt,indent=2)+'\n', encoding='utf-8')
    print(json.dumps(receipt,indent=2),flush=True)

if __name__ == '__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--isolation',type=Path,required=True)
    p.add_argument('--output',type=Path,required=True)
    a=p.parse_args();recover(a.isolation,a.output)
