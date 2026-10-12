"""Apply exact, candidate-only timing overlays; fail before writing on source drift or a live Editor."""
import argparse
import hashlib
import json
import re
from pathlib import Path
import subprocess


def scope(unit, statement):
    return f"using (PopulationWorkTimings.Measure(PopulationWorkTimings.Unit.{unit})) {{ {statement} }}"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--candidate', type=Path, required=True)
    parser.add_argument('--label', default='WorkUnits')
    args = parser.parse_args()
    if not re.fullmatch(r'[A-Za-z][A-Za-z0-9]{0,31}', args.label):
        raise ValueError('Use a bounded alphanumeric build label.')
    root = Path(__file__).resolve().parents[2]
    candidate = args.candidate.resolve()
    if not str(candidate).startswith('D:\\BooterBigArmValidation\\PerformancePlayer') or (candidate / '.git').exists():
        raise ValueError('Owned non-Git PerformancePlayer copy required.')
    command = 'Get-CimInstance Win32_Process -Filter "Name = \'Unity.exe\' OR Name = \'PopulationSectorAudit.exe\'" | Select-Object CommandLine | ConvertTo-Json -Compress'
    processes = subprocess.check_output(['powershell', '-NoProfile', '-Command', command], text=True)
    if str(candidate).replace('\\', '/').lower() in processes.replace('\\\\', '/').lower():
        raise ValueError('Candidate Editor or audit Player is still running.')
    runtime = 'Assets/_Project/Scripts/Runtime/TopDown3D/'
    edits = {
        runtime + 'FixedTerrainPopulationSectorStudy.cs': [
            ('if (now >= nextDemand) UpdateDemand(now);', 'if (now >= nextDemand) ' + scope('Demand', 'UpdateDemand(now);')),
            ('Physics.SyncTransforms(); synchronizedCollisionRevision = presentation.CollisionRevision; PhysicsSynchronizations++;', scope('PhysicsSync', 'Physics.SyncTransforms();') + ' synchronizedCollisionRevision = presentation.CollisionRevision; PhysicsSynchronizations++;')],
        runtime + 'FixedTerrainPopulationScheduler.cs': [
            ('presentation.Release(expired.Cell);', scope('Release', 'presentation.Release(expired.Cell);')),
            ('selected.Build = planner.Begin(selected.Cell);', scope('PlannerBegin', 'selected.Build = planner.Begin(selected.Cell);')),
            ('count += selected.Build.Advance(1, remaining);', scope('PlannerStep', 'count += selected.Build.Advance(1, remaining);')),
            ('if (!selected.Build.TryCommitBaseline(state))', 'bool committed; ' + scope('BaselineCommit', 'committed = selected.Build.TryCommitBaseline(state);') + ' if (!committed)'),
            ('if (presentation.QueueAfterBaselineCommit(selected.Cell, selected.Build, selected.Collision, selected.Visual, state))', 'bool queued; ' + scope('Queue', 'queued = presentation.QueueAfterBaselineCommit(selected.Cell, selected.Build, selected.Collision, selected.Visual, state);') + ' if (queued)')],
        runtime + 'FixedTerrainPopulationPresentation.cs': [
            ('Retire(retiring.Dequeue());', scope('Retire', 'Retire(retiring.Dequeue());')),
            ('var instance = Acquire();', 'Instance instance; ' + scope('Acquire', 'instance = Acquire();')),
            ('Collision(item, request.CollisionRelevant); Visual(item, request.VisualRelevant); item.Object.SetActive(true); request.Activation++; CollisionRevision++;', scope('Activation', 'Collision(item, request.CollisionRelevant); Visual(item, request.VisualRelevant); item.Object.SetActive(true); request.Activation++; CollisionRevision++;'))],
        'Tools/Diagnostics/PopulationSectorPlayerAudit.cs': [
            ('public Sample[] samples;', 'public Sample[] samples; public PopulationWorkTimings.Summary[] workUnits;'),
            ('study.enabled = false; study.Initialize();', 'PopulationWorkTimings.Reset(); study.enabled = false; ' + scope('Initialize', 'study.Initialize();')),
            ('view.enabled = phase != 1;', 'PopulationWorkTimings.SetPhase(sampleAt == 0 ? 0 : phase + 1); view.enabled = phase != 1;'),
            ('report.samples = samples.ToArray();', 'report.samples = samples.ToArray(); report.workUnits = PopulationWorkTimings.Capture();')]
    }
    output = []
    for relative, replacements in edits.items():
        source = (root / relative).read_text(encoding='utf-8-sig')
        for old, new in replacements:
            if source.count(old) != 1:
                raise ValueError(f'Exact source anchor changed: {relative}: {old}')
            source = source.replace(old, new)
        target = candidate / (runtime + 'PopulationSectorPlayerAudit.cs' if relative.startswith('Tools/') else relative)
        output.append((target, source, relative))
    output.append((candidate / (runtime + 'PopulationWorkTimings.cs'), (root / 'Tools/Diagnostics/PopulationWorkTimings.cs').read_text(), 'Tools/Diagnostics/PopulationWorkTimings.cs'))
    for name in ('FixedTerrainPopulationState.cs', 'FixedTerrainPopulationPlanner.cs'):
        output.append((candidate / (runtime + name), (root / (runtime + name)).read_text(), runtime + name))
    output.append((candidate / 'Assets/_Project/Scripts/Editor/IsolatedPopulationAuditBuild.cs',
                   (root / 'Tools/Diagnostics/IsolatedPopulationAuditBuild.cs').read_text().replace('BuildsPopulationLocal/', 'BuildsPopulation' + args.label + '/'),
                   'Tools/Diagnostics/IsolatedPopulationAuditBuild.cs'))
    receipt = candidate / ('Logs/population-work-units-overlay.json' if args.label == 'WorkUnits' else 'Logs/population-' + args.label + '-overlay.json')
    if receipt.exists() or (candidate / ('BuildsPopulation' + args.label)).exists():
        raise ValueError('Fresh overlay receipt/output required.')
    records = []
    for target, source, original in output:
        target.write_text(source, encoding='utf-8', newline='\n')
        records.append({'source': original, 'source_sha256': hashlib.sha256((root / original).read_bytes()).hexdigest(),
                        'candidate_path': str(target), 'overlay_sha256': hashlib.sha256(target.read_bytes()).hexdigest()})
    receipt.write_text(json.dumps({'scope': 'Diagnostic-only work timing; clocks add observer overhead; not production FPS',
                                   'files': records}, indent=2) + '\n', encoding='utf-8', newline='\n')


if __name__ == '__main__':
    main()
