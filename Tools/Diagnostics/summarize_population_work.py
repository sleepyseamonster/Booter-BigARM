"""Validate candidate-only local work clocks. Nested scopes are inclusive, never additive frame totals."""
import argparse
import gzip
import hashlib
import json
import math
from pathlib import Path
from summarize_population_audit import summarize

UNITS = ('Demand', 'PlannerBegin', 'PlannerStep', 'BaselineCommit', 'Queue', 'Acquire',
         'Activation', 'Retire', 'PhysicsSync', 'Release', 'Initialize')


def analyze(report):
    base = summarize(report)
    entries = report.get('workUnits', [])
    expected = {(phase, unit) for phase in range(4) for unit in UNITS}
    if len(entries) != len(expected) or {(e['phase'], e['unit']) for e in entries} != expected:
        raise ValueError('Exactly one clock bucket for each phase/unit required.')
    for e in entries:
        if not isinstance(e['count'], int) or e['count'] < 0:
            raise ValueError('Nonnegative integral sample count required.')
        if not all(math.isfinite(e[k]) and e[k] >= 0 for k in ('totalMs', 'maximumMs', 'maximumAtSeconds')):
            raise ValueError('Finite nonnegative work clocks required.')
        if e['maximumMs'] > e['totalMs'] + 1e-6 or e['count'] == 0 and e['totalMs'] != 0:
            raise ValueError('Inconsistent bucket totals.')
    return {'capture': base, 'work_phases': [
        {'phase': p, 'name': name, 'units': sorted([e for e in entries if e['phase'] == p and e['count']],
                                                 key=lambda e: e['maximumMs'], reverse=True)}
        for p, name in enumerate(('startup_and_settle', 'normal', 'camera_absent', 'restored'))],
        'limits': ['Scope clocks include OS preemption, cold initialization and observer overhead',
                   'Nested scopes are inclusive; do not sum to derive a frame time',
                   'Fixed-step synchronization uses the most recently set update phase',
                   'One run identifies measured work units, not a controlled optimization speedup']}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('report', type=Path)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    with (gzip.open(args.report, 'rb') if args.report.suffix == '.gz' else args.report.open('rb')) as stream:
        source = stream.read(20 * 1024 * 1024 + 1)
    if len(source) > 20 * 1024 * 1024:
        raise ValueError('Bounded input required.')
    result = analyze(json.loads(source))
    result['source_sha256'] = hashlib.sha256(source).hexdigest()
    with args.output.open('x', encoding='utf-8', newline='\n') as output:
        output.write(json.dumps(result, indent=2) + '\n')


if __name__ == '__main__':
    main()
