"""Bounded sky/display comparisons through the production workbench renderer."""
import argparse
import json
import subprocess
import sys
from common import ROOT, inside, write_json
from verify_foundation import png


def samples(path):
    w, h, channels, rows = png(path)
    # Fixed central scene region excludes the Engine menu button and frame timing.
    pixels = [tuple(rows[y][x*channels:x*channels+3])
              for y in range(h//10, h*9//10, 3)
              for x in range(w//4, w*9//10, 3)]
    return (w, h), pixels


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--executable', default='build/foundation/engine_workbench')
    parser.add_argument('--shaders', default='build/foundation/Shaders')
    parser.add_argument('--catalog', required=True)
    parser.add_argument('--out', required=True)
    args = parser.parse_args()
    exe, shaders, catalog, out = map(inside, (args.executable, args.shaders, args.catalog, args.out))
    if out.exists():
        parser.error('Choose a new evidence directory')
    command = [sys.executable, str(ROOT/'Tools/record.py'), '--out', str(out/'run'), '--timeout', '60']
    for path in [exe, catalog, ROOT/'Tools/verify_environment.py', *sorted(shaders.glob('*.bin'))]:
        command += ['--input', str(path)]
    command += ['--', str(exe), '--shaders', str(shaders), '--catalog', str(catalog), '--verify-environment', str(out/'captures')]
    subprocess.run(command, cwd=ROOT, check=True, stdout=subprocess.DEVNULL)
    report = json.loads((out/'captures/environment.json').read_text())['payload']
    if not report['passed'] or not report['real_surfaces']:
        raise ValueError('Missing native environment/texture evidence')
    names = ['baseline', 'clipped', 'tone-bright', 'low-sun', 'normals', 'normals-exposed',
             'sky-off', 'cool-sky', 'calibration-tone', 'calibration-linear', 'preview-tone', 'preview-linear']
    images = {n: samples(out/'captures'/(n+'.png')) for n in names}
    assert len({image[0] for image in images.values()}) == 1, 'Capture dimensions differ'
    differences = {}
    for label, a, b in [('highlight_operator', 'clipped', 'tone-bright'), ('sun_elevation', 'baseline', 'low-sun'),
                         ('sky_visibility', 'baseline', 'sky-off'), ('environment_colors', 'baseline', 'cool-sky'),
                         ('normal_bypass', 'normals', 'normals-exposed'), ('calibration_bypass', 'calibration-tone', 'calibration-linear'),
                         ('texture_preview_bypass', 'preview-tone', 'preview-linear')]:
        differences[label] = sum(max(abs(x-y) for x, y in zip(p, q)) > 2 for p, q in zip(images[a][1], images[b][1]))
    for label in ['highlight_operator', 'sun_elevation', 'sky_visibility', 'environment_colors']:
        assert differences[label] > 100, (label, differences)
    for label in ['normal_bypass', 'calibration_bypass', 'texture_preview_bypass']:
        assert differences[label] == 0, (label, differences)
    clipped = {n: sum(max(p) >= 254 for p in images[n][1]) for n in ['clipped', 'tone-bright']}
    assert clipped['clipped'] > clipped['tone-bright'] + 100, clipped
    assert len(set(images['calibration-tone'][1])) > 4, 'Empty calibration comparison'
    assert len(set(images['preview-tone'][1])) > 1, 'Empty texture preview comparison'
    result = {'result': 'passed', 'backend': report['backend'], 'drawable': images['baseline'][0],
              'captures': report['captures'], 'changed_sampled_pixels': differences, 'near_clipped_pixels': clipped,
              'limits': 'Technical fixed-scene Metal comparison, no GPU timing, Windows, physical input or final art acceptance.'}
    write_json(out/'result.json', result)
    print(json.dumps(result, indent=2))


if __name__ == '__main__':
    main()
