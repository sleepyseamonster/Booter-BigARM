import bpy,json,pathlib,hashlib,math,sys
root=pathlib.Path(sys.argv[sys.argv.index("--")+1])
folders=sorted(root.glob('Mixamo*20261013'))
paths=[p for folder in folders for p in sorted(folder.glob('*.fbx'))]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root/'male_basemesh_mixamo_rigged.fbx'))
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
expected={(b.name,b.parent.name if b.parent else None) for b in rig.data.bones}
model={ 'path':str(root/'male_basemesh_mixamo_rigged.fbx'),'bones':sorted(b.name for b in rig.data.bones),'scene_unit_scale':bpy.context.scene.unit_settings.scale_length,'objects':[{'name':o.name,'type':o.type,'dimensions':list(o.dimensions),'scale':list(o.scale),'vertices':len(o.data.vertices) if o.type=='MESH' else None,'polygons':len(o.data.polygons) if o.type=='MESH' else None,'triangles':sum(len(p.vertices)-2 for p in o.data.polygons) if o.type=='MESH' else None,'material_slots':len(o.material_slots) if o.type=='MESH' else None} for o in bpy.data.objects]}
records=[]
cache={}
for path in paths:
    h=hashlib.sha256(path.read_bytes()).hexdigest()
    if h in cache:
        probe=cache[h]
    else:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=str(path))
        rigs=[o for o in bpy.data.objects if o.type=='ARMATURE']
        actions=list(bpy.data.actions)
        probe={'bone_count':len(rigs[0].data.bones) if rigs else 0,'skeleton_matches':len(rigs)==1 and {(b.name,b.parent.name if b.parent else None) for b in rigs[0].data.bones}==expected,'mesh_count':len([o for o in bpy.data.objects if o.type=='MESH']),'actions':[{'name':a.name,'start':float(a.frame_range[0]),'end':float(a.frame_range[1]),'duration_seconds':float((a.frame_range[1]-a.frame_range[0])/bpy.context.scene.render.fps)} for a in actions],'fps':bpy.context.scene.render.fps}
        if rigs and actions:
            a=actions[0]; samples=[]; hand_samples=[]; allposes=[]
            for f in [a.frame_range[0],(a.frame_range[0]+a.frame_range[1])/2,a.frame_range[1]]:
                bpy.context.scene.frame_set(int(f))
                samples.append([float(v) for v in rigs[0].pose.bones['mixamorig:Hips'].matrix.translation])
                allposes.append(tuple(float(v) for b in rigs[0].pose.bones for row in b.matrix for v in row))
            probe['hip_samples_blender_rig_units']=samples
            probe['hip_start_end_delta_blender_rig_units']=[samples[-1][i]-samples[0][i] for i in range(3)]
            probe['animated_pose_verified']=allposes[0]!=allposes[1] or allposes[0]!=allposes[2]
        cache[h]=probe
    records.append({'group':path.parent.name,'file':path.name,'source_path':str(path),'bytes':path.stat().st_size,'sha256':h,**probe})
out={'model':model,'records':records,'fbx_copies':len(records),'unique_byte_hashes':len(cache),'all_skeletons_match':all(r['skeleton_matches'] for r in records)}
(root/'mixamo-complete-inventory-20261013.json').write_text(json.dumps(out,indent=2))
print('FULL_INVENTORY='+json.dumps({'copies':len(records),'unique_byte_hashes':len(cache),'all_skeletons_match':out['all_skeletons_match'],'nonanimated':[r['file'] for r in records if not r.get('animated_pose_verified')],'model':model}))
