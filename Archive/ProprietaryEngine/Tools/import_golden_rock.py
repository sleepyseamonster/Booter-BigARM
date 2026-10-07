"""Translate the saved Unity ApprovedBoulderFamilyRecipe into native authoring inputs.
Reads only the explicitly supplied source; output stays under Engine. No Unity runtime.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

def scalar(text, name):
    matches = re.findall(r'^\s*' + re.escape(name) + r':\s*([-+\d.eE]+)\s*$', text, re.M)
    if len(matches) != 1:
        raise ValueError(f'Expected one {name}')
    return float(matches[0])

def vector(text, name, axes):
    match = re.search(r'^\s*' + re.escape(name) + r':\s*\{([^}]+)\}', text, re.M)
    if not match:
        raise ValueError(f'Missing {name}')
    values = dict(re.findall(r'([xyzwrgba]):\s*([-+\d.eE]+)', match[1]))
    if set(values) != set(axes):
        raise ValueError(f'Invalid {name}')
    return [float(values[a]) for a in axes]

def linear(value):
    return value / 12.92 if value <= .04045 else ((value + .055) / 1.055) ** 2.4

def translate(source):
    text = source.read_text()
    header, *variants = re.split(r'^  - stableId: ', text, flags=re.M)
    if len(variants) != 3 or scalar(header, 'recipeVersion') != 1:
        raise ValueError('Expected the captured three-variant approved family')
    result = []
    for item in variants:
        metadata, *volumes = re.split(r'^    - name: ', item, flags=re.M)
        if len(volumes) != 2:
            raise ValueError('Expected the approved two-mass composition')
        seed = int(scalar(metadata, 'generationSeed')) & 0xffffffff
        native = []
        for index, volume in enumerate(volumes):
            shape = int(scalar(volume, 'sourceShape'))
            if shape != 0 or scalar(volume, 'operation') != 0:
                raise ValueError('Source changed from the approved additive weathered blocks')
            native.append(dict(id=index, center=vector(volume, 'localPosition', 'xyz'),
                half_size=[v / 2 for v in vector(volume, 'localScale', 'xyz')], yaw=0., subtractive=False,
                primitive=0, pitch=0., roll=0., taper=0., orientation=vector(volume, 'localRotation', 'xyzw'),
                shape_seed=int(scalar(volume, 'shapeSeed')) & 0xffffffff))
        controls = dict(family='wasteland-layered-v1')
        for target, original in [('grit','sideGrit'),('shale','undersideShale'),('cracks','crackAmount'),('variation','surfaceVariation'),('worn','wornShine'),('side_shale','sideShalePatches'),('top_shale','topShalePatches')]:
            controls[target] = round(scalar(header, original) * 1000)
        controls['dust'] = 60
        controls['geology_mm'] = round(scalar(header, 'geologyScale') * 1000)
        controls['dust_color_linear'] = [linear(v) for v in vector(header, 'environmentDustColor', 'rgba')[:3]]
        payload = dict(generator_version=6, seed=seed, subdivisions=2, radii_mm=[500,375,500],
            distortion_permille=0, band_permille=0, bands=1,
            shape=dict(masses=2, compaction=0, asymmetry=0, fractures=0, edge_damage=1000),
            formation=dict(kind=0, members=1, spacing_mm=1500), material=controls, volumes=native,
            profile=0, member_edits=[], calibration=dict(source_seed=seed,
                fusion=scalar(header,'fusionSmoothness'), relaxation=scalar(header,'surfaceRelaxation'),
                scale=1., sampling_mm=round(scalar(header,'previewVoxelSize') * 1000)))
        result.append(dict(kind='engine.rock-recipe', version=1, payload=payload))
    return result

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    out = args.output.resolve()
    if ROOT not in out.parents or out.exists():
        parser.error('Output must be a new directory inside Engine')
    recipes = translate(args.source)
    out.mkdir(parents=True)
    names = ['01-Golden-Rock', '02-Approved-Variation-A', '03-Approved-Variation-B']
    for name, recipe in zip(names, recipes):
        (out/(name+'.json')).write_text(json.dumps(recipe,indent=2)+'\n')
    record = dict(source=str(args.source), source_sha256=hashlib.sha256(args.source.read_bytes()).hexdigest(),
        variants=3, captured_mass_transforms=6, material_source='Saved ApprovedBoulderFamilyRecipe, not superseded prose values',
        native_adaptations=['Shared native mesher triangulation', 'Native BRDF and lighting', 'Dust amount uses preserved material default 0.06'],
        not_ported=['Full Unity random source-layout planner', 'Terrain and sand/clutter scene'])
    (out/'source-receipt.json.txt').write_text(json.dumps(record,indent=2)+'\n')
    print('Translated three approved recipes and six physical source transforms')

if __name__ == '__main__':
    main()
