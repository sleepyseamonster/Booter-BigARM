"""Break the six-sided scree silhouettes without increasing fragment geometry.

Run in the open badlands study after the irregular stone pass. Each apron is a
batched mesh of 320 independent 12-vertex chips. The old modifier stack spent
geometry on micro bevels while leaving their regular six-sided outline intact.
"""
from pathlib import Path
import hashlib
import random

import bpy


MARKER = 'asymmetric_scree_aprons_2026_09_30'


def apply():
    if Path(bpy.data.filepath).name != 'BrokenWorldBadlandsStudy.blend':
        raise RuntimeError('Open the existing badlands study')
    terrain = bpy.data.objects.get('Badlands terrain - study only')
    if not terrain or not terrain.get('irregular_clutter_stones_2026_09_30'):
        raise RuntimeError('Apply irregular stone pass first')
    if terrain.get(MARKER):
        print('Asymmetric scree aprons already applied')
        return
    aprons = sorted((o for o in bpy.context.scene.objects
                     if o.type == 'MESH' and o.name.startswith('Reference scree apron ')),
                    key=lambda o: o.name)
    if len(aprons) != 6:
        raise RuntimeError(f'Expected six scree aprons, found {len(aprons)}')
    changed = 0
    removed = 0
    for apron in aprons:
        mesh = apron.data.copy()
        if len(mesh.vertices) % 12:
            raise RuntimeError(f'Unexpected chip topology in {apron.name}')
        for chip in range(len(mesh.vertices) // 12):
            first = chip * 12
            seed = int.from_bytes(hashlib.blake2b(
                f'{apron.name}:{chip}'.encode(), digest_size=8).digest(), 'little')
            rng = random.Random(seed)
            ring = [mesh.vertices[first + j] for j in range(12)]
            cx = sum(v.co.x for v in ring[:6]) / 6
            cy = sum(v.co.y for v in ring[:6]) / 6
            z0 = sum(v.co.z for v in ring[:6]) / 6
            z1 = sum(v.co.z for v in ring[6:]) / 6
            height = max(.001, z1 - z0)
            # Make two corners prominent and collapse one into a chipped notch.
            radii = [rng.uniform(.78, 1.24) for _ in range(6)]
            notch = rng.randrange(6)
            radii[notch] *= rng.uniform(.52, .70)
            radii[(notch + 2) % 6] *= rng.uniform(1.12, 1.32)
            radii[(notch + 4) % 6] *= rng.uniform(1.04, 1.23)
            for j in range(6):
                bottom = ring[j].co
                top = ring[j + 6].co
                dx, dy = bottom.x - cx, bottom.y - cy
                # Same corner shift on both rings keeps the chip closed.
                bottom.x = cx + dx * radii[j]
                bottom.y = cy + dy * radii[j]
                top.x = cx + (top.x - cx) * radii[j] + dx * rng.uniform(-.11, .11)
                top.y = cy + (top.y - cy) * radii[j] + dy * rng.uniform(-.11, .11)
                top.z = z1 + height * rng.uniform(-.20, .18)
                if j == notch:
                    top.z -= height * .12
        mesh.update()
        for modifier in list(apron.modifiers):
            apron.modifiers.remove(modifier)
            removed += 1
        apron.data = mesh
        apron[MARKER] = True
        changed += 1
    terrain[MARKER] = True
    print(f'ASYMMETRIC SCREE APRONS: {changed} batches, '
          f'{sum(len(o.data.vertices) // 12 for o in aprons)} chips, '
          f'{removed} dense modifiers removed')


if __name__ == '__main__':
    apply()
