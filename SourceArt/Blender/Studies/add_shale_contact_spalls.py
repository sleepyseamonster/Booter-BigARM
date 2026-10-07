"""Place thin, shared-material shale spalls along exposed bedrock margins.

This is a bounded source-art pass. Spalls are joined into 20 m cells so the
Blender study stays editable without one object per chip. Existing terrain,
rocks, materials, and object transforms are not changed.
"""
from collections import defaultdict
from pathlib import Path
import hashlib
import math
import random
import runpy

import bpy
from mathutils import Vector


MARKER = 'contact_spalls_2026_09_30'
HERE = Path(__file__).resolve().parent


def apply():
    if Path(bpy.data.filepath).name != 'BrokenWorldBadlandsStudy.blend':
        raise RuntimeError('Open the existing badlands study')
    terrain = bpy.data.objects.get('Badlands terrain - study only')
    if terrain is None or not terrain.get('irregular_shale_rims_2026_09_30'):
        raise RuntimeError('Apply the irregular shale rims first')
    if terrain.get(MARKER):
        print('Shale contact spalls already applied')
        return
    author = runpy.run_path(str(HERE / 'author_badlands_realism.py'))
    ground = author['ground']
    collection = author['collection']('Realism - shale contact spalls')
    material = bpy.data.materials['Geology - fractured shale with dust']
    cells = defaultdict(lambda: ([], []))
    plates = sorted((o for o in bpy.context.scene.objects if o.type == 'MESH'
                     and o.name.startswith('Exposed shale plate')), key=lambda o:o.name)
    count = 0
    for plate in plates:
        seed = int.from_bytes(hashlib.blake2b(plate.name.encode(),digest_size=8).digest(),'little')
        rng = random.Random(seed)
        corners = [Vector(corner) for corner in plate.bound_box]
        center_local = sum(corners, Vector()) / len(corners)
        center = plate.matrix_world @ center_local
        rx, ry = plate.dimensions.x*.5, plate.dimensions.y*.5
        orientation = plate.rotation_euler.z
        for index in range(11):
            theta = rng.uniform(0, math.tau)
            distance = rng.uniform(.72, 1.31)
            u = rx*distance*math.cos(theta)
            v = ry*distance*math.sin(theta)
            x = center.x + u*math.cos(orientation)-v*math.sin(orientation)
            y = center.y + u*math.sin(orientation)+v*math.cos(orientation)
            if max(abs(x),abs(y)) > 175:
                continue
            z = ground(x,y)-.018
            size = (rng.uniform(.07,.19) if index%7 else
                    rng.uniform(.22,.39))
            length = size*rng.uniform(1.05,1.75)
            width = size*rng.uniform(.65,1.00)
            thickness = size*rng.uniform(.10,.23)
            heading = orientation+rng.uniform(-1.4,1.4)
            axis_x, axis_y = math.cos(heading), math.sin(heading)
            cell = (math.floor(x/20), math.floor(y/20))
            vertices, faces = cells[cell]
            start = len(vertices)
            sides = 7
            radii = [rng.uniform(.79,1.10) for _ in range(sides)]
            for layer in range(2):
                for k in range(sides):
                    angle = math.tau*k/sides
                    lx = math.cos(angle)*length*radii[k]
                    ly = math.sin(angle)*width*radii[k]
                    vertices.append((x+axis_x*lx-axis_y*ly,
                                     y+axis_y*lx+axis_x*ly,
                                     z+(thickness if layer else -.018)))
            vertices.append((x,y,z+thickness*rng.uniform(1.02,1.15)))
            for k in range(sides):
                nxt = (k+1)%sides
                faces.append((start+k,start+nxt,start+sides+nxt,start+sides+k))
                faces.append((start+sides+k,start+sides+nxt,start+2*sides))
            faces.append(tuple(start+k for k in reversed(range(sides))))
            count += 1
    for (ix,iy),(vertices,faces) in sorted(cells.items()):
        mesh = bpy.data.meshes.new(f'Shale contact spalls {ix:+03d} {iy:+03d}')
        mesh.from_pydata(vertices,[],faces)
        mesh.materials.append(material)
        mesh.update(calc_edges=True)
        for polygon in mesh.polygons:
            polygon.use_smooth = True
        obj = bpy.data.objects.new(mesh.name,mesh)
        collection.objects.link(obj)
        obj['source_art_contact_spalls'] = True
    terrain[MARKER] = True
    print(f'CONTACT SPALLS: {count} thin fragments in {len(cells)} spatial cells')


if __name__ == '__main__':
    apply()
