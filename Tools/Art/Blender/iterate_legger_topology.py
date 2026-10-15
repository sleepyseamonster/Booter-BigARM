"""Conservative, versioned Legger topology trial. Run in background Blender on a saved source."""
import argparse
import hashlib
import json
import math
import sys
from pathlib import Path
import bmesh
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree


def log(message):
    print(message, flush=True)


def stats(obj):
    mesh = obj.data
    mesh.calc_loop_triangles()
    return {'vertices': len(mesh.vertices), 'faces': len(mesh.polygons),
            'triangles': len(mesh.loop_triangles),
            'quads': sum(len(p.vertices) == 4 for p in mesh.polygons),
            'dimensions': list(obj.dimensions), 'matrix_world': [list(r) for r in obj.matrix_world],
            'uv_layers': [l.name for l in mesh.uv_layers]}


def sample_points(mesh, count=16000):
    step = max(1, len(mesh.vertices) // count)
    return [mesh.vertices[i].co.copy() for i in range(0, len(mesh.vertices), step)]


def distances(tree, points):
    values = sorted(tree.find_nearest(p)[3] for p in points)
    return {'samples': len(values), 'mean_local_units': sum(values)/len(values),
            'p99_local_units': values[int((len(values)-1)*.99)], 'max_local_units': values[-1]}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', required=True)
    parser.add_argument('--evidence', required=True)
    parser.add_argument('--ratio', type=float, default=.9)
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:])
    if not .5 <= args.ratio <= 1:
        raise ValueError('This conservative workflow accepts ratios .5 through 1 only')
    source = Path(bpy.data.filepath)
    target = Path(args.output)
    evidence = Path(args.evidence)
    if target.exists() or source.resolve() == target.resolve():
        raise FileExistsError('Output must be a new file, separate from the saved source')
    evidence.mkdir(parents=True, exist_ok=True)
    target.parent.mkdir(parents=True, exist_ok=True)
    source_hash = hashlib.sha256(source.read_bytes()).hexdigest()
    obj = bpy.data.objects.get('Legger_Body')
    if obj is None or obj.type != 'MESH':
        raise ValueError('Expected the named Legger_Body mesh; do not guess another object')
    if obj.modifiers:
        raise ValueError('Review existing modifiers before running this trial')
    if bpy.context.object is not None and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    report = {'source': str(source), 'source_sha256': source_hash,
              'output': str(target), 'blender_version': bpy.app.version_string,
              'ratio_requested': args.ratio, 'before': stats(obj)}
    log('Baseline: '+json.dumps(report['before']))
    original = obj.data.copy()
    baseline_samples = sample_points(original)
    scene = bpy.context.scene
    review_bounds = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    # Both review passes use the identical source framing.
    def renders(label):
        render_keys = ['engine', 'resolution_x', 'resolution_y', 'resolution_percentage', 'filepath', 'film_transparent']
        render_settings = {k: getattr(scene.render, k) for k in render_keys}
        shading = scene.display.shading
        shading_keys = ['light', 'show_shadows', 'show_cavity', 'color_type', 'single_color']
        shading_settings = {k: tuple(getattr(shading, k)) if k == 'single_color' else getattr(shading, k) for k in shading_keys}
        image_format = scene.render.image_settings.file_format
        old = [(o, o.hide_render) for o in scene.objects if o.type == 'MESH' and o != obj]
        for o, _ in old: o.hide_render = True
        pts = review_bounds
        lo = Vector([min(p[i] for p in pts) for i in range(3)])
        hi = Vector([max(p[i] for p in pts) for i in range(3)])
        center = (lo+hi)/2
        span = max(hi-lo)
        data = bpy.data.cameras.new('TemporaryTopologyReview')
        camera = bpy.data.objects.new('TemporaryTopologyReview', data)
        scene.collection.objects.link(camera)
        previous_camera = scene.camera
        scene.camera = camera
        scene.render.engine = 'BLENDER_WORKBENCH'
        scene.render.resolution_x = scene.render.resolution_y = 1024
        scene.render.resolution_percentage = 100
        scene.render.image_settings.file_format = 'PNG'
        scene.render.film_transparent = True
        shading = scene.display.shading
        shading.light = 'STUDIO'
        shading.show_shadows = True
        shading.show_cavity = True
        data.type = 'ORTHO'
        data.ortho_scale = span*1.12
        for name, direction in [('front',(0,-4,0)),('side',(4,0,0)),('elevated',(2,-3,1.6)),('underside',(2,-3,-2))]:
            camera.location = center+Vector(direction)*span
            camera.rotation_euler = (center-camera.location).to_track_quat('-Z','Y').to_euler()
            for mode in ['TEXTURE','SINGLE']:
                shading.color_type = mode
                shading.single_color = (.55,.55,.55)
                scene.render.filepath = str(evidence/f'{label}_{name}_{mode.lower()}.png')
                bpy.ops.render.render(write_still=True)
        scene.camera = previous_camera
        bpy.data.objects.remove(camera, do_unlink=True)
        bpy.data.cameras.remove(data)
        for o, hidden in old: o.hide_render = hidden
        for k, v in render_settings.items(): setattr(scene.render, k, v)
        for k, v in shading_settings.items(): setattr(shading, k, v)
        scene.render.image_settings.file_format = image_format
    renders('before')
    log('Applying conservative collapse')
    bpy.ops.object.select_all(action='DESELECT')
    obj.hide_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    modifier = obj.modifiers.new('ConservativeCollapse', 'DECIMATE')
    modifier.decimate_type = 'COLLAPSE'
    modifier.ratio = args.ratio
    modifier.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    report['after_collapse'] = stats(obj)
    log('Joining compatible triangle pairs')
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.join_triangles(bm, faces=list(bm.faces), cmp_seam=True, cmp_sharp=True,
                            cmp_uvs=True, cmp_vcols=True, cmp_materials=True,
                            angle_face_threshold=math.radians(3),
                            angle_shape_threshold=math.radians(40))
    report['mesh_health'] = {'boundary_edges': sum(e.is_boundary for e in bm.edges),
                             'non_manifold_edges': sum(not e.is_manifold for e in bm.edges),
                             'loose_vertices': sum(not v.link_edges for v in bm.verts)}
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    bpy.context.view_layer.update()
    report['after'] = stats(obj)
    log('Measuring bidirectional sampled surface deviation')
    ref_bm = bmesh.new()
    ref_bm.from_mesh(original)
    tree = BVHTree.FromBMesh(ref_bm)
    ref_bm.free()
    report['candidate_to_source'] = distances(tree, sample_points(obj.data))
    del tree
    low_bm = bmesh.new()
    low_bm.from_mesh(obj.data)
    tree = BVHTree.FromBMesh(low_bm)
    low_bm.free()
    report['source_to_candidate'] = distances(tree, baseline_samples)
    del tree
    # Preserve authored scale, positions, UV names, materials and packed images.
    assert report['before']['matrix_world'] == report['after']['matrix_world']
    assert report['before']['uv_layers'] == report['after']['uv_layers']
    report['packed_images'] = sum(bool(i.packed_file) for i in bpy.data.images)
    report['triangle_reduction_percent'] = 100*(1-report['after']['triangles']/report['before']['triangles'])
    report['limitations'] = ['Sampled distances are not a global error bound.',
                            'Quad pairing is not animation-ready edge flow.',
                            'No rig, bake, collision, LOD or Unity import is established.']
    obj['topology_iteration'] = f'Collapse ratio {args.ratio}; UV-aware conservative quad pairing; review candidate'
    obj['topology_source_sha256'] = source_hash
    bpy.data.meshes.remove(original)
    bpy.ops.wm.save_as_mainfile(filepath=str(target))
    if hashlib.sha256(source.read_bytes()).hexdigest() != source_hash:
        raise RuntimeError('Source changed concurrently; candidate requires rebase/review')
    (evidence/'report.json').write_text(json.dumps(report, indent=2)+'\n')
    renders('after')
    log('Completed '+str(target))


if __name__ == '__main__':
    main()
