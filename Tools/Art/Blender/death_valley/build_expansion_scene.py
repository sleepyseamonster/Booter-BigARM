"""Native Blender construction, reopen verification, height export and fixed-view review.

Uses existing study material conventions. Source 2 m control meshes have complete
1 m border vertices; Unity interiors reconstruct the recorded bilinear render grid.
"""
import argparse
import json
from pathlib import Path
import sys
import time
import hashlib
import bpy
import numpy as np
from mathutils import Vector

sys.path.insert(0,str(Path(__file__).resolve().parent))
from build_region_study import collection, material

ROOT=Path(__file__).resolve().parents[4]

def sha256(path):
    h=hashlib.sha256()
    with Path(path).open('rb') as handle:
        for block in iter(lambda:handle.read(1024*1024),b''):h.update(block)
    return h.hexdigest()

def arguments():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--manifest',type=Path,required=True);p.add_argument('--mode',choices=['build','verify','render'],required=True)
    p.add_argument('--section',choices=['west','north','northwest','combined','pilot'],required=True)
    p.add_argument('--output',type=Path,required=True);p.add_argument('--stride',type=int,choices=[2,4,8],default=2)
    p.add_argument('--view',choices=['overhead','oblique','junction','west_seam','north_seam','northwest_west','northwest_north'],default='oblique')
    return p.parse_args(sys.argv[sys.argv.index('--')+1:])

def load(manifest):
    record=json.loads(manifest.read_text())
    if record['schema_version']!=1 or record['unity_origin_m']!=[522448,4008248]:raise ValueError('Unexpected manifest/origin')
    if sha256(ROOT/record['baseline_manifest'])!=record['baseline_manifest_sha256']:raise ValueError('Retained proof changed')
    grids={};colors={};tiles=[]
    for s in record['sections']:
        for key,hashkey in [('mesh_grids_file','mesh_grids_sha256'),('color_file','color_sha256')]:
            if sha256(manifest.parent/s[key])!=s[hashkey]:raise ValueError('Prepared resource changed')
        grids[s['id']]=np.load(manifest.parent/s['mesh_grids_file'])
        colors[s['id']]=manifest.parent/s['color_file']
        for t in s['tiles']:tiles.append(dict(t,section=s['id'],section_bounds=s['bounds_m']))
    baseline=json.loads((ROOT/record['baseline_manifest']).read_text())
    baseline_grids=np.load((ROOT/record['baseline_manifest']).parent/baseline['render_heights_file'])
    grids['existing']={key:(baseline_grids[key][::-1]*np.float32(1800)-np.float32(100)) for key in baseline_grids.files}
    colors['existing']=ROOT/'SourceData/Terrain/DeathValley/MacSnapshot2026-10-07/four_slices_v1/geographic_color.png'
    for t in baseline['tiles']:
        row,col=int(t['id'][1:3]),int(t['id'][5:7]);east=520400+256*col;north=4010296-256*(row+1)
        tiles.append({'id':'existing/'+t['id'],'local_id':t['id'],'geographic_key':t['geographic_key'],'row':row,'col':col,
                      'bounds_m':[east,north,east+256,north+256],'section_bounds':[520400,4006200,524496,4010296],'section':'existing'})
    return record,grids,colors,tiles

def selected(tiles,section):
    if section=='combined':return tiles
    if section=='pilot':return [t for t in tiles if t['id'] in ['west/r00_c15','north/r15_c00','northwest/r15_c15','existing/r00_c00']]
    return [t for t in tiles if t['section']==section]

def mesh(t,data,stride,dest,mat):
    # Dense boundary loops prevent the reduced review from hiding edge samples.
    vertices=[];indices={};faces=[]
    def vertex(r,c):
        key=(r,c)
        if key not in indices:
            indices[key]=len(vertices);vertices.append((float(c),float(256-r),float(data[r,c])))
        return indices[key]
    for r in range(0,256,stride):
        for c in range(0,256,stride):
            polygon=[]
            # Clockwise in north-first raster coordinates, upward in Blender XY.
            for rr in range(r,r+stride,1 if c==0 else stride):polygon.append(vertex(rr,c))
            for cc in range(c,c+stride,1 if r+stride==256 else stride):polygon.append(vertex(r+stride,cc))
            for rr in range(r+stride,r,-1 if c+stride==256 else -stride):polygon.append(vertex(rr,c+stride))
            for cc in range(c+stride,c,-1 if r==0 else -stride):polygon.append(vertex(r,cc))
            faces.append(polygon)
    data_mesh=bpy.data.meshes.new(t['id'].replace('/','_')+'_Mesh');data_mesh.from_pydata(vertices,[],faces);data_mesh.update()
    obj=bpy.data.objects.new(t['id'].replace('/','_'),data_mesh);dest.objects.link(obj)
    obj.location=(t['bounds_m'][0]-522448,t['bounds_m'][1]-4008248,0)
    obj['section']=t['section'];obj['local_id']=t['local_id'];obj['geographic_key']=t['geographic_key'];obj['interior_spacing_m']=stride
    obj['retained_reference']=t['section']=='existing';obj.hide_select=t['section']=='existing'
    data_mesh.materials.append(mat)
    uv=data_mesh.uv_layers.new(name='GeographicUV')
    bounds=t['section_bounds']
    for poly in data_mesh.polygons:
        poly.use_smooth=True
        for li in poly.loop_indices:
            v=data_mesh.vertices[data_mesh.loops[li].vertex_index].co
            uv.data[li].uv=((v.x+t['bounds_m'][0]-bounds[0])/4096,(v.y+t['bounds_m'][1]-bounds[1])/4096)
    return obj

def setup_camera(view,tiles):
    xmin=min(t['bounds_m'][0] for t in tiles)-522448;ymin=min(t['bounds_m'][1] for t in tiles)-4008248
    xmax=max(t['bounds_m'][2] for t in tiles)-522448;ymax=max(t['bounds_m'][3] for t in tiles)-4008248
    center=Vector(((xmin+xmax)/2,(ymin+ymax)/2,200));span=max(xmax-xmin,ymax-ymin)
    if view=='junction':center=Vector((-2048,2048,-84));span=700
    elif view=='west_seam':center=Vector((-2048,0,0));span=4600
    elif view=='north_seam':center=Vector((0,2048,100));span=4600
    elif view=='northwest_west':center=Vector((-4096,2048,400));span=4600
    elif view=='northwest_north':center=Vector((-2048,4096,500));span=4600
    for o in list(bpy.data.objects):
        if o.type=='CAMERA':bpy.data.objects.remove(o,do_unlink=True)
    camera=bpy.data.cameras.new('TerrainReviewCamera');obj=bpy.data.objects.new('TerrainReviewCamera',camera);bpy.context.scene.collection.objects.link(obj)
    obj.location=center+Vector((0,0,span*2)) if view=='overhead' else center+Vector((-span*.75,-span*.75,span*1.05))
    obj.rotation_euler=(center-obj.location).to_track_quat('-Z','Y').to_euler();camera.type='ORTHO';camera.ortho_scale=span*1.15;camera.clip_end=50000
    bpy.context.scene.camera=obj
    if view in ('overhead','oblique'):
        bpy.context.view_layer.update()
        inv=obj.matrix_world.inverted()
        points=[inv@(o.matrix_world@Vector(corner)) for o in bpy.data.objects if o.type=='MESH' and 'geographic_key' in o for corner in o.bound_box]
        x0=min(p.x for p in points);x1=max(p.x for p in points);y0=min(p.y for p in points);y1=max(p.y for p in points)
        aspect=bpy.context.scene.render.resolution_x/bpy.context.scene.render.resolution_y
        camera.ortho_scale=max(x1-x0,(y1-y0)*aspect)*1.08
        obj.location+=obj.rotation_euler.to_matrix()@Vector(((x0+x1)*.5,(y0+y1)*.5,0))

def main():
    a=arguments();record,grids,colors,tiles=load(a.manifest.resolve());tiles=selected(tiles,a.section);out=a.output.resolve()
    if out.exists():raise ValueError('Refuse to overwrite build/proof/render output')
    out.parent.mkdir(parents=True,exist_ok=True);start=time.monotonic()
    if a.mode=='build':
        bpy.ops.wm.read_factory_settings(use_empty=True)
        mats={};collections={}
        for section in sorted({t['section'] for t in tiles}):
            mats[section]=material('Geographic_'+section,str(colors[section]));collections[section]=collection(('Protected_' if section=='existing' else 'New_')+section)
        for i,t in enumerate(tiles):
            mesh(t,grids[t['section']][t['local_id']],a.stride,collections[t['section']],mats[t['section']])
            if (i+1)%32==0:print('BUILT_CHUNKS',i+1,flush=True)
        scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
        scene['expansion_manifest_sha256']=sha256(a.manifest);scene['local_origin_epsg26911']=json.dumps(record['unity_origin_m']);scene['build_section']=a.section
        scene['interior_spacing_m']=a.stride;scene['perimeter_spacing_m']=1
        scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=1400;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
        scene.world=bpy.data.worlds.new('TerrainReviewWorld');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.12,.15,.18,1)
        sun=bpy.data.lights.new('TerrainReviewSun','SUN');sun.energy=2.5;sun.angle=.08
        sun_obj=bpy.data.objects.new('TerrainReviewSun',sun);scene.collection.objects.link(sun_obj);sun_obj.rotation_euler=(.5,-.6,-.7)
        setup_camera('oblique',tiles)
        for image in bpy.data.images:
            if image.source=='FILE':image.pack()
        bpy.ops.wm.save_as_mainfile(filepath=str(out),compress=True)
        print(json.dumps({'built':a.section,'chunks':len(tiles),'seconds':time.monotonic()-start,'blend':str(out)}),flush=True)
    elif a.mode=='verify':
        if bpy.context.scene.get('expansion_manifest_sha256')!=sha256(a.manifest) or bpy.context.scene.get('build_section')!=a.section:raise ValueError('Saved build proof differs from manifest')
        objects={o['geographic_key']:o for o in bpy.data.objects if o.type=='MESH' and 'geographic_key' in o}
        if len(objects)!=len(tiles) or sum(o.type=='MESH' for o in bpy.data.objects)!=len(tiles):raise ValueError('Wrong mesh count or missing/duplicate identities')
        maximum=0.;edges={};edge_error=0.;exports={};normals=1.
        for t in tiles:
            obj=objects[t['geographic_key']]
            if obj.modifiers or any(abs(v-1)>1e-7 for v in obj.scale) or any(abs(v)>1e-7 for v in obj.rotation_euler):raise ValueError('Unexpected mesh transform/modifier')
            if obj['section']!=t['section'] or obj['local_id']!=t['local_id'] or tuple(obj.location)!=(t['bounds_m'][0]-522448,t['bounds_m'][1]-4008248,0):raise ValueError('Section identity or placement changed')
            expected=grids[t['section']][t['local_id']];coarse={};border={}
            for v in obj.data.vertices:
                world=obj.matrix_world@v.co
                x=int(round(world.x+522448-t['bounds_m'][0]));nr=256-int(round(world.y+4008248-t['bounds_m'][1]))
                if not 0<=nr<=256 or not 0<=x<=256:raise ValueError('Off-grid mesh coordinate')
                if abs(world.x-(t['bounds_m'][0]+x-522448))>1e-5 or abs(world.y-(t['bounds_m'][3]-nr-4008248))>1e-5:raise ValueError('Fractional or shifted mesh coordinate')
                maximum=max(maximum,abs(float(world.z)-float(expected[nr,x])))
                coarse[(nr,x)]=np.float32(world.z)
                if nr in (0,256) or x in (0,256):
                    xy=(t['bounds_m'][0]+x,t['bounds_m'][3]-nr)
                    if xy in edges:edge_error=max(edge_error,abs(float(edges[xy])-float(world.z)))
                    else:edges[xy]=np.float32(world.z)
                    border[(nr,x)]=np.float32(world.z)
            stride=int(obj['interior_spacing_m']);control=np.array([[coarse[(r,c)] for c in range(0,257,stride)] for r in range(0,257,stride)],dtype='float32')
            # Explicit bilinear reconstruction of the recorded control lattice.
            yy,xx=np.indices((257,257));r0=yy//stride;c0=xx//stride;r1=np.minimum(r0+1,control.shape[0]-1);c1=np.minimum(c0+1,control.shape[1]-1)
            fx=(xx%stride)/stride;fy=(yy%stride)/stride
            render=((control[r0,c0]*(1-fx)+control[r0,c1]*fx)*(1-fy)+(control[r1,c0]*(1-fx)+control[r1,c1]*fx)*fy).astype('float32')
            for (nr,x),height in border.items():render[nr,x]=height
            if a.section!='combined' and not obj.get('retained_reference',t['section']=='existing'):
                if float(abs(render-expected).max())>.00025:raise ValueError('Blender reconstruction differs from prepared render samples')
            exports[t['id'].replace('/','__')]=render
            normals=min(normals,min(p.normal.z for p in obj.data.polygons))
        if maximum>.00025 or edge_error!=0 or normals<=0:raise ValueError(f'Mesh/source, seam or normal verification failed: sample={maximum}, edge={edge_error}, normal_z={normals}')
        if any(i.source=='FILE' and not i.packed_file for i in bpy.data.images):raise ValueError('Unpacked image dependency')
        export=out.with_suffix('.npz');np.savez_compressed(export,**exports)
        proof={'schema_version':1,'section':a.section,'chunks':len(tiles),'blender_version':bpy.app.version_string,
               'manifest_sha256':sha256(a.manifest),'blend_file':bpy.data.filepath,'blend_sha256':sha256(Path(bpy.data.filepath)),
               'export_file':export.name,'export_sha256':sha256(export),'max_mesh_sample_error_m':maximum,'max_mesh_edge_difference_m':edge_error,
               'minimum_normal_z':normals,'interior_spacing_m':int(bpy.context.scene['interior_spacing_m']),'perimeter_spacing_m':1,
               'status':'native_reopened_mesh_verified','seconds':time.monotonic()-start}
        out.write_text(json.dumps(proof,indent=2)+'\n');print(json.dumps(proof),flush=True)
    else:
        setup_camera(a.view,tiles);bpy.context.scene.render.filepath=str(out);bpy.ops.render.render(write_still=True)
        print(json.dumps({'rendered':a.view,'file':str(out)}),flush=True)

if __name__=='__main__':main()
