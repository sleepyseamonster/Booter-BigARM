"""Check an engine_outdoor_check capture set; visual acceptance remains separate."""
import argparse
import json
from common import inside, write_json
from verify_foundation import png


def changes(a, b):
    if a[:2] != b[:2]:
        raise ValueError('Capture size mismatch')
    w, h, ca, ra = a
    _, _, cb, rb = b
    bands = [0, 0, 0]
    brighter = 0
    for y in range(h // 3, h, 3):
        for x in range(0, w, 3):
            before = sum(ra[y][x * ca:x * ca + 3])
            after = sum(rb[y][x * cb:x * cb + 3])
            if before - after > 9:
                bands[min(2, (y - h // 3) * 3 // (h - h // 3))] += 1
            brighter += after - before > 9
    return {'darker_by_height_band': bands, 'brighter': brighter}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('captures')
    args = parser.parse_args()
    root = inside(args.captures)
    report = json.loads((root / 'result.json').read_text())['payload']
    if not report['passed'] or not report['offscreen_caster_confirmed']:
        raise ValueError('Native check did not pass')
    images = {name: png(root / (name + '.png')) for name in
              ('unshadowed', 'shadowed', 'without-offscreen-caster', 'camera-step', 'resized')}
    shadow = changes(images['unshadowed'], images['shadowed'])
    offscreen = changes(images['without-offscreen-caster'], images['shadowed'])
    if sum(shadow['darker_by_height_band']) < 100 or shadow['brighter'] > 10:
        raise ValueError('Expected selective shadow darkening')
    if sum(offscreen['darker_by_height_band']) < 100 or offscreen['brighter'] > 10:
        raise ValueError('Off-camera caster must affect visible receivers')
    if images['resized'][:2] == images['shadowed'][:2]:
        raise ValueError('Missing target replacement')
    result = {'result': 'passed', 'shadow': shadow, 'offscreen_caster': offscreen,
              'dimensions': list(images['shadowed'][:2]), 'resized': list(images['resized'][:2]),
              'limits': 'Pixel checks prove selective darkening and off-camera influence; contact, acne, transition and camera quality require image inspection and CPU fit contracts.'}
    write_json(root / 'comparison.json', result)
    print(json.dumps(result, indent=2))


if __name__ == '__main__':
    main()
