"""Read one retained authored sector without opening the full production scene in Unity."""
import argparse
import hashlib
import json
from pathlib import Path
import re


def main():
    root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', required=True)
    parser.add_argument('--row-start', type=int, default=12)
    parser.add_argument('--column-start', type=int, default=0)
    args = parser.parse_args()
    if args.row_start not in range(0, 13, 4) or args.column_start not in range(0, 13, 4):
        raise SystemExit('Choose an aligned 4x4 patch in the retained 16x16 Badwater source.')
    output = Path(args.output).resolve()
    if output.parent != root / 'Logs' or output.suffix != '.json' or output.exists():
        raise SystemExit('Use a new JSON receipt directly under this checkout Logs directory.')
    scene = root / 'Assets/_Project/Scenes/Production/GreaterWasteland.unity'
    blocks = re.split(r'^--- !u!', scene.read_text(encoding='utf-8'), flags=re.M)[1:]
    transforms, objects, terrains, colliders = {}, {}, {}, {}
    for block in blocks:
        match = re.match(r'(\d+) &(-?\d+)', block)
        if not match:
            continue
        kind, identifier = map(int, match.groups())
        owner = re.search(r'm_GameObject: \{fileID: (-?\d+)\}', block)
        if kind == 4 and owner:
            def vector(field, dimensions):
                m = re.search(field + r': \{([^}]+)\}', block)
                assert m, field
                values = dict(re.findall(r'(\w): ([^,}]+)', m[1]))
                return {axis: float(values[axis]) for axis in dimensions}
            transforms[identifier] = (int(owner[1]), int(re.search(r'm_Father: \{fileID: (-?\d+)\}', block)[1]),
                vector('m_LocalPosition', 'xyz'), vector('m_LocalRotation', 'xyzw'), vector('m_LocalScale', 'xyz'))
            objects[int(owner[1])] = identifier
        if kind in (154, 218) and owner:
            guid = re.search(r'm_TerrainData: \{fileID: \d+, guid: ([a-f0-9]+)', block)
            if guid:
                (terrains if kind == 218 else colliders)[guid[1]] = (int(owner[1]), block)

    def world_position(identifier):
        owner, parent, local, rotation, scale = transforms[identifier]
        assert all(abs(rotation[a]) < 1e-6 for a in 'xyz') and abs(abs(rotation['w']) - 1) < 1e-6
        assert all(abs(scale[a] - 1) < 1e-6 for a in 'xyz')
        upstream = world_position(parent) if parent else {a: 0 for a in 'xyz'}
        return {a: upstream[a] + local[a] for a in 'xyz'}

    tiles = []
    for row in range(args.row_start, args.row_start + 4):
        for column in range(args.column_start, args.column_start + 4):
            relative = f'Assets/_Project/Art/Terrain/BadwaterFourSlices/TerrainData/r{row:02d}_c{column:02d}.asset'
            path = root / relative
            guid = re.search(r'^guid: (\w+)$', Path(str(path) + '.meta').read_text(), re.M)[1]
            owner, block = terrains[guid]
            collider_owner, collider_block = colliders[guid]
            assert owner == collider_owner and 'm_Enabled: 1' in block and 'm_Enabled: 1' in collider_block
            position = world_position(objects[owner])
            assert all(abs(position[a] / 256 - round(position[a] / 256)) < 1e-6 for a in 'xz')
            tiles.append({'path': relative, 'guid': guid, 'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
                'position': position, 'materialGuid': re.search(r'm_MaterialTemplate: \{fileID: \d+, guid: (\w+)', block)[1],
                'pixelError': float(re.search(r'm_HeightmapPixelError: ([^\n]+)', block)[1]),
                'instanced': 'm_DrawInstanced: 1' in block,
                'shadowCastingMode': int(re.search(r'm_ShadowCastingMode: (\d+)', block)[1])})
    cells = {(int(t['position']['x'] / 256), int(t['position']['z'] / 256)) for t in tiles}
    min_x = min(c[0] for c in cells); min_z = min(c[1] for c in cells)
    assert cells == {(x, z) for x in range(min_x, min_x + 4) for z in range(min_z, min_z + 4)}
    receipt = {'sourceScene': str(scene.relative_to(root)).replace('\\', '/'),
        'sourceSceneSha256': hashlib.sha256(scene.read_bytes()).hexdigest(),
        'scope': 'One 4x4 authored native patch, original data and world poses; study constraints are not production POI migration.',
        'row_start': args.row_start, 'column_start': args.column_start,
        'tiles': tiles}
    output.write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'output': str(output), 'tiles': len(tiles), 'minimum_cell': [min_x, min_z]}))


if __name__ == '__main__':
    main()
