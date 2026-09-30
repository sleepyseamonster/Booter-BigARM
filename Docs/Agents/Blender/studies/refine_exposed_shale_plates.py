"""Break up exposed shale plate rims without moving authored objects.

Run in the existing badlands study. Shared source meshes stay shared; object
placement, rotation, scale, and material assignments remain untouched.
"""
from pathlib import Path
import hashlib
import math
import random

import bpy


MARKER = 'irregular_shale_rims_2026_09_30'


def apply():
    if Path(bpy.data.filepath).name != 'BrokenWorldBadlandsStudy.blend':
        raise RuntimeError('Open the existing badlands study')
    terrain = bpy.data.objects.get('Badlands terrain - study only')
    if terrain is None or not terrain.get('clean_fracture_topology_2026_09_30'):
        raise RuntimeError('Apply the repaired rock topology first')
    if terrain.get(MARKER):
        print('Irregular shale rims already applied')
        return
    objects = [o for o in bpy.context.scene.objects if o.type == 'MESH'
               and o.name.startswith('Exposed shale plate')]
    source_map = {}
    for obj in objects:
        old = obj.data
        if old not in source_map:
            mesh = old.copy()
            source_map[old] = mesh
            seed = int.from_bytes(hashlib.blake2b(old.name.encode(), digest_size=8).digest(), 'little')
            rng = random.Random(seed)
            lo = [min(v.co[i] for v in mesh.vertices) for i in range(3)]
            hi = [max(v.co[i] for v in mesh.vertices) for i in range(3)]
            cx, cy = (lo[0]+hi[0])*.5, (lo[1]+hi[1])*.5
            hx, hy = (hi[0]-lo[0])*.5, (hi[1]-lo[1])*.5
            phase = rng.uniform(0, math.tau)
            notches = [(rng.uniform(-math.pi, math.pi),
                        rng.uniform(.20, .36), rng.uniform(.15, .29))
                       for _ in range(3)]
            for vert in mesh.vertices:
                nx, ny = (vert.co.x-cx)/hx, (vert.co.y-cy)/hy
                radius = math.hypot(nx, ny)
                angle = math.atan2(ny, nx)
                irregular = (1 + .065*math.sin(3*angle+phase)
                             + .035*math.sin(7*angle-phase*.7))
                for center, width, depth in notches:
                    distance = math.atan2(math.sin(angle-center), math.cos(angle-center))
                    irregular -= depth*math.exp(-(distance/width)**2)
                # Fade the outline change into the interior so facets cannot
                # fold into a narrow pinched fan at the center.
                amount = min(1, max(0, radius)**2)
                factor = 1 + (irregular-1)*amount
                vert.co.x = cx + (vert.co.x-cx)*factor
                vert.co.y = cy + (vert.co.y-cy)*factor
                edge = min(1, max(0, (radius-.55)/.55))
                vert.co.z = lo[2] + (vert.co.z-lo[2])*(.65-.30*edge)
            mesh.update()
        obj.data = source_map[old]
        obj[MARKER] = True
    terrain[MARKER] = True
    print(f'IRREGULAR SHALE RIMS: {len(objects)} plates, {len(source_map)} shared sources')


if __name__ == '__main__':
    apply()
