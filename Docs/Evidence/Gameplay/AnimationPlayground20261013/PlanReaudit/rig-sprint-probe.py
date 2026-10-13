import bpy, pathlib, hashlib, json, sys
root=pathlib.Path(sys.argv[sys.argv.index("--")+1])
paths=[root/'male_basemesh_rigged.fbx',root/'male_basemesh_from_blender_rigged.fbx',root/'male_basemesh_mixamo_rigged.fbx',root/'male_basemesh_mixamo_unrigged@Sprint.fbx',root/'male_basemesh_mixamo_unrigged@Sprint (1).fbx',root/'MixamoLocomotion20261013'/'Sprint - forward travel alternative.fbx']
records=[]
for path in paths:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(path))
    rigs=[o for o in bpy.data.objects if o.type=='ARMATURE']
    meshes=[o for o in bpy.data.objects if o.type=='MESH']
    actions=list(bpy.data.actions)
    finger_bones=[]
    if rigs:
        finger_bones=[b.name for b in rigs[0].data.bones if any(t in b.name.lower() for t in ('thumb','index','middle','ring','pinky','little','finger','f_'))]
    rec={'file':path.name,'source_path':str(path),'bytes':path.stat().st_size,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'rig_count':len(rigs),'bone_count':len(rigs[0].data.bones) if rigs else 0,'mesh_count':len(meshes),'vertices':sum(len(m.data.vertices) for m in meshes),'triangles':sum(sum(len(p.vertices)-2 for p in m.data.polygons) for m in meshes),'material_slots':sum(len(m.material_slots) for m in meshes),'finger_bone_names':finger_bones,'mesh_vertex_group_names':sorted({g.name for m in meshes for g in m.vertex_groups}),'actions':[{'start':float(a.frame_range[0]),'end':float(a.frame_range[1])} for a in actions]}
    if 'Sprint' in path.name and rigs and actions:
        rig=rigs[0]; a=actions[0]; poses=[]; hips=[]
        for frame in range(int(a.frame_range[0]),int(a.frame_range[1])+1):
            bpy.context.scene.frame_set(frame)
            poses.append([[b.name,[round(float(v),6) for row in b.matrix for v in row]] for b in sorted(rig.pose.bones,key=lambda x:x.name)])
            hips.append([float(v) for v in rig.pose.bones['mixamorig:Hips'].matrix.translation])
        rec['pose_sequence_sha256_rounded_6']=hashlib.sha256(json.dumps(poses,separators=(',',':')).encode()).hexdigest()
        rec['hip_start_end_delta_rig_local_units']=[hips[-1][i]-hips[0][i] for i in range(3)]
        rec['skeleton_signature']=sorted((b.name,b.parent.name if b.parent else None) for b in rig.data.bones)
    records.append(rec)
out=pathlib.Path(sys.argv[sys.argv.index("--")+2])
out.write_text(json.dumps(records,indent=2),encoding='utf-8')
print('RIG_AND_SPRINT_AUDIT='+json.dumps([{'file':r['file'],'bones':r['bone_count'],'meshes':r['mesh_count'],'vertices':r['vertices'],'finger_bones':len(r['finger_bone_names']),'pose_signature':r.get('pose_sequence_sha256_rounded_6'),'hip_delta':r.get('hip_start_end_delta_rig_local_units')} for r in records]))
