"""Remove repeated geometric outlines and excess faces from stone clutter.

The repaired stone and talus meshes have far more polygons than their small
silhouettes need. This pass reshapes and reduces them without moving objects.
"""
from pathlib import Path
import hashlib
import math
import random

import bpy
import bmesh


MARKER = 'irregular_clutter_stones_2026_09_30'


def apply():
    if Path(bpy.data.filepath).name != 'BrokenWorldBadlandsStudy.blend':
        raise RuntimeError('Open the existing badlands study')
    terrain = bpy.data.objects.get('Badlands terrain - study only')
    if terrain is None or not terrain.get('clean_fracture_topology_2026_09_30'):
        raise RuntimeError('Apply repaired rock topology first')
    if terrain.get(MARKER):
        print('Irregular clutter stones already applied')
        return
    objects = sorted((o for o in bpy.context.scene.objects
                      if o.type == 'MESH' and o.name.startswith(('Stone_', 'Talus_'))),
                     key=lambda o: o.name)
    changed = 0
    before_faces = after_faces = 0
    for obj in objects:
        original = obj.data
        before_faces += len(original.polygons)
        mesh = original.copy()
        seed = int.from_bytes(hashlib.blake2b(obj.name.encode(), digest_size=8).digest(), 'little')
        rng = random.Random(seed)
        lo = [min(v.co[i] for v in mesh.vertices) for i in range(3)]
        hi = [max(v.co[i] for v in mesh.vertices) for i in range(3)]
        cx, cy = (lo[0] + hi[0]) * .5, (lo[1] + hi[1]) * .5
        hx, hy = max((hi[0] - lo[0]) * .5, .001), max((hi[1] - lo[1]) * .5, .001)
        height = max(hi[2] - lo[2], .001)
        phases = [rng.uniform(-math.pi, math.pi) for _ in range(5)]
        nicks = [(rng.uniform(-math.pi, math.pi), rng.uniform(.13, .30),
                  rng.uniform(.10, .22)) for _ in range(3)]
        shear_x, shear_y = rng.uniform(-.045, .045), rng.uniform(-.045, .045)
        for vert in mesh.vertices:
            nx, ny = (vert.co.x - cx) / hx, (vert.co.y - cy) / hy
            angle = math.atan2(ny, nx)
            radius = math.hypot(nx, ny)
            outline = (1 + .075 * math.sin(3 * angle + phases[0])
                       + .050 * math.sin(5 * angle + phases[1])
                       + .025 * math.sin(9 * angle + phases[2]))
            nick_strength = 0.0
            for center, width, depth in nicks:
                delta = math.atan2(math.sin(angle - center), math.cos(angle - center))
                notch = depth * math.exp(-(delta / width) ** 2)
                outline -= notch
                nick_strength = max(nick_strength, notch)
            edge = min(1.0, max(0.0, radius) ** 2)
            zfrac = min(1.0, max(0.0, (vert.co.z - lo[2]) / height))
            factor = 1 + (outline - 1) * edge
            vert.co.x = cx + (vert.co.x - cx) * factor + hx * shear_x * zfrac
            vert.co.y = cy + (vert.co.y - cy) * factor + hy * shear_y * zfrac
            relief = (.045 * math.sin(2.6 * nx + 1.7 * ny + phases[3])
                      + .030 * math.sin(4.1 * ny - 2.2 * nx + phases[4])
                      + .018 * math.sin(7.7 * nx + 6.1 * ny + phases[1]))
            vert.co.z += height * (relief - .22 * nick_strength * edge) * zfrac
        mesh.update()
        obj.data = mesh
        target_faces = 180 if obj.name.startswith('Stone_') else 100
        modifier = obj.modifiers.new('Source art face budget', 'DECIMATE')
        modifier.ratio = min(1.0, target_faces / len(mesh.polygons))
        modifier.use_collapse_triangulate = True
        depsgraph = bpy.context.evaluated_depsgraph_get()
        depsgraph.update()
        reduced = bpy.data.meshes.new_from_object(obj.evaluated_get(depsgraph),
                                                  preserve_all_data_layers=True,
                                                  depsgraph=depsgraph)
        obj.modifiers.remove(modifier)
        obj.data = reduced
        # The collapse modifier can leave an isolated triangle hinged on one
        # edge at aggressive ratios. Remove only that flap; it has two open
        # edges and does not contribute to the silhouette.
        bm = bmesh.new()
        bm.from_mesh(reduced)
        for edge in list(bm.edges):
            if len(edge.link_faces) == 3:
                for face in list(edge.link_faces):
                    if sum(other.is_boundary for other in face.edges) >= 2:
                        bm.faces.remove(face)
                        break
        for edge in list(bm.edges):
            if not edge.link_faces:
                bm.edges.remove(edge)
        bm.to_mesh(reduced)
        bm.free()
        reduced.update()
        after_faces += len(reduced.polygons)
        obj[MARKER] = True
        changed += 1
    terrain[MARKER] = True
    print(f'IRREGULAR CLUTTER STONES: {changed} placed rocks, '
          f'{before_faces} to {after_faces} source faces')


if __name__ == '__main__':
    apply()
