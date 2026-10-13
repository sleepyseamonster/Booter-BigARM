"""Compare named warm CPU/allocation probes; never convert them to FPS."""
import argparse
import json
import math
from pathlib import Path
import statistics


def load(path):
    report = json.loads(Path(path).read_text())
    if report['residentTerrains'] != 9216 or report['terrainColliders'] != 9216:
        raise ValueError('Complete production terrain required')
    if report['positiveControlEvents'] < 64 or report['negativeControlEvents'] != 0:
        raise ValueError('Allocation controls failed')
    if not report['cargoProxyClear'] or not report['cargoProxyBlocked'] or report['ignoredChildRayHits'] < 128:
        raise ValueError('LOS fixture validation failed')
    if report['denseOverlapHitCount'] < 64:
        raise ValueError('Dense overlap fixture did not saturate the query')
    if not report['headless'] or report['ironstoneNodes'] != 12:
        raise ValueError('Expected headless production capture with twelve authored nodes')
    rows = {}
    for row in report['rows']:
        if not row['allocationObserverValid'] or row['samples'] < 32:
            raise ValueError('Invalid or insufficient measurements')
        if row['operationsPerSample'] < 1 or any(not math.isfinite(row[key]) or row[key] < 0
                for key in ('medianUs', 'p95Us', 'allocationEventsPerOperation')):
            raise ValueError('Invalid measurement values')
        rows.setdefault(row['name'], []).append(row)
    if not rows or any(len(values) != 2 or {row['repeat'] for row in values} != {0, 1} for values in rows.values()):
        raise ValueError('Two independent warm probe repetitions required')
    return report, rows


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--before', required=True)
    parser.add_argument('--after', required=True)
    parser.add_argument('--output', required=True)
    args = parser.parse_args()
    before, old = load(args.before)
    after, new = load(args.after)
    if (before['scene'], before['unityVersion'], before['inventoryCargoAccessible'], before.get('actionCargoAccessible')) != (
            after['scene'], after['unityVersion'], after['inventoryCargoAccessible'], after.get('actionCargoAccessible')):
        raise ValueError('Capture scene/version/inventory/action cargo access posture differs')
    comparisons = []
    for name in sorted(old.keys() & new.keys()):
        b = old[name]; a = new[name]
        if len({(row['samples'], row['operationsPerSample']) for row in b + a}) != 1:
            raise ValueError('Capture sampling configuration differs: ' + name)
        bm = statistics.mean(row['medianUs'] for row in b)
        am = statistics.mean(row['medianUs'] for row in a)
        comparisons.append({'name': name, 'beforeMedianUsMean': bm, 'afterMedianUsMean': am,
            'medianPercentChange': (am / bm - 1) * 100 if bm else None,
            'beforeP95UsMean': statistics.mean(row['p95Us'] for row in b),
            'afterP95UsMean': statistics.mean(row['p95Us'] for row in a),
            'beforeAllocationEventsPerOp': statistics.mean(row['allocationEventsPerOperation'] for row in b),
            'afterAllocationEventsPerOp': statistics.mean(row['allocationEventsPerOperation'] for row in a),
            'medianRepeatRangesSeparated': max(row['medianUs'] for row in a) < min(row['medianUs'] for row in b)})
    result = {'before': str(Path(args.before).resolve()), 'after': str(Path(args.after).resolve()),
        'evidenceKind': 'Warm batched CPU microseconds per operation and calibrated allocation events; not FPS or physical-device latency.',
        'sameInventoryAccessPosture': before['inventoryCargoAccessible'] == after['inventoryCargoAccessible'],
        'unmatchedBefore': sorted(old.keys() - new.keys()), 'unmatchedAfter': sorted(new.keys() - old.keys()), 'rows': comparisons}
    output = Path(args.output); output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(result, indent=2))


if __name__ == '__main__':
    main()
