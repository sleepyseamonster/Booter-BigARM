"""Read-only top-surface comparison against the preserved artist stones.
Run on the applied original Blender scene with -- ROOT.
"""
import json,sys
from pathlib import Path
import bpy
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
root=Path(sys.argv[sys.argv.index('--')+1]).resolve(); asset='FlatStoneGroundPatch01'
proof=root/'Logs'/asset; docs=root/'SourceArt/Blender/Studies/BooterGroundSetup'/asset
mode=sys.argv[-1] if sys.argv[-1] in ['prepared','applied'] else 'applied'
report=json.loads(((proof/'preparation.json') if mode=='prepared' else (docs/'application.json')).read_text())
if mode=='prepared':
    with bpy.data.libraries.load(str(proof/'prepared_patch.blend'),link=False) as (src,dst): dst.objects=[asset]
    bpy.context.scene.collection.objects.link(dst.objects[0])
bpy.context.view_layer.update(); patch=bpy.data.objects[asset]
vertices=[]; faces=[]
for name in report['source_selection']:
    obj=bpy.data.objects[name]; start=len(vertices)
    vertices.extend(patch.matrix_world.inverted()@obj.matrix_world@v.co for v in obj.data.vertices)
    faces.extend(tuple(start+i for i in p.vertices) for p in obj.data.polygons)
high=BVHTree.FromPolygons(vertices,faces)
low=BVHTree.FromPolygons([v.co for v in patch.data.vertices],[p.vertices[:] for p in patch.data.polygons])
rows=[]
for name in report['source_selection']:
    stone=bpy.data.objects[name]
    if len(stone.data.vertices)==4: continue
    coords=[patch.matrix_world.inverted()@stone.matrix_world@v.co for v in stone.data.vertices]
    minx,miny=min(v.x for v in coords)-.02,min(v.y for v in coords)-.02
    maxx,maxy=max(v.x for v in coords)+.02,max(v.y for v in coords)+.02
    old=[]; new=[]; distances=[]; surface_distances=[]
    for y in range(128):
        for x in range(128):
            p=Vector((minx+(x+.5)/128*(maxx-minx),miny+(y+.5)/128*(maxy-miny),1))
            a=high.ray_cast(p,Vector((0,0,-1)),2)[0]; b=low.ray_cast(p,Vector((0,0,-1)),2)[0]
            assert a is not None and b is not None, 'Base coverage changed'
            old.append(a.z>.003); new.append(b.z>.003); distances.append(abs(a.z-b.z))
            if a.z>.003: surface_distances.append(low.find_nearest(a)[3])
            if b.z>.003: surface_distances.append(high.find_nearest(b)[3])
    a=np.array(old); b=np.array(new); union=np.count_nonzero(a|b)
    rows.append(dict(stone=name,surface_footprint_iou=float(np.count_nonzero(a&b)/union),
                     p95_vertical_error_metres=float(np.percentile(distances,95)),max_vertical_error_metres=max(distances),
                     silhouette_transition_samples=int(np.count_nonzero(a!=b)),
                     p95_nearest_surface_error_metres=float(np.percentile(surface_distances,95)),
                     max_nearest_surface_error_metres=max(surface_distances)))
result=dict(sample_grid_per_stone=128,threshold_above_base_metres=.003,rows=rows,
            minimum_footprint_iou=min(r['surface_footprint_iou'] for r in rows),
            maximum_vertical_error_metres=max(r['max_vertical_error_metres'] for r in rows),
            maximum_nearest_surface_error_metres=max(r['max_nearest_surface_error_metres'] for r in rows),
            vertical_metric_note='A vertical ray may switch from a steep stone edge to the base after a small horizontal outline shift; nearest-surface distance measures actual geometric displacement')
assert result['minimum_footprint_iou']>=.993
(proof/'surface-comparison.json').write_text(json.dumps(result,indent=2)+'\n',newline='\n')
print(json.dumps(result),flush=True)
