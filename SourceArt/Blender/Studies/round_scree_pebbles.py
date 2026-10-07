"""Soften sharp scree chips into irregular pebbles at the same face count."""
from pathlib import Path
import bpy


MARKER = 'rounded_scree_pebbles_2026_09_30'


def apply():
    if Path(bpy.data.filepath).name != 'BrokenWorldBadlandsStudy.blend':
        raise RuntimeError('Open the existing badlands study')
    terrain = bpy.data.objects.get('Badlands terrain - study only')
    if not terrain or not terrain.get('asymmetric_scree_aprons_2026_09_30'):
        raise RuntimeError('Apply asymmetric scree aprons first')
    if terrain.get(MARKER):
        print('Rounded scree pebbles already applied')
        return
    aprons = sorted((o for o in bpy.context.scene.objects
                     if o.type == 'MESH' and o.name.startswith('Reference scree apron ')),
                    key=lambda o: o.name)
    if len(aprons) != 6:
        raise RuntimeError(f'Expected six scree aprons, found {len(aprons)}')
    for apron in aprons:
        mesh = apron.data.copy()
        if len(mesh.vertices) % 12:
            raise RuntimeError(f'Unexpected chip topology in {apron.name}')
        for chip in range(len(mesh.vertices) // 12):
            first = chip * 12
            for ring in range(2):
                start = first + ring * 6
                coords = [mesh.vertices[start + j].co.copy() for j in range(6)]
                cx = sum(p.x for p in coords) / 6
                cy = sum(p.y for p in coords) / 6
                # Relax extreme points toward their neighbors; the six points
                # and eight polygons per pebble remain unchanged.
                for j in range(6):
                    prev, cur, nxt = coords[(j - 1) % 6], coords[j], coords[(j + 1) % 6]
                    point = mesh.vertices[start + j].co
                    blend = .39 if ring else .31
                    point.x = cur.x * (1 - blend) + (prev.x + nxt.x) * blend * .5
                    point.y = cur.y * (1 - blend) + (prev.y + nxt.y) * blend * .5
                    # Keep an uneven, geologically plausible rim while
                    # removing needle-like peaks from the top ring.
                    if ring:
                        point.z = cur.z * .72 + (prev.z + nxt.z) * .14
                # Cap extreme stretch but retain a long and short axis.
                points = [mesh.vertices[start + j].co for j in range(6)]
                span_x = max(p.x for p in points) - min(p.x for p in points)
                span_y = max(p.y for p in points) - min(p.y for p in points)
                if span_x > 1.8 * max(span_y, 1e-5):
                    for p in points:
                        p.y = cy + (p.y - cy) * min(1.35, span_x / (1.8 * span_y))
                elif span_y > 1.8 * max(span_x, 1e-5):
                    for p in points:
                        p.x = cx + (p.x - cx) * min(1.35, span_y / (1.8 * span_x))
        mesh.update()
        apron.data = mesh
        apron[MARKER] = True
    terrain[MARKER] = True
    print('ROUNDED SCREE PEBBLES: 1920 fragments, eight faces each')


if __name__ == '__main__':
    apply()
