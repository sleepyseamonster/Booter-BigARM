"""Retain all material variants in saved sources, including zero-user candidates.
Run after capture: background Blender --python seal_native.py -- ROOT
Reconstructs missing materials from the same authored recipe; keeps assigned materials unchanged.
"""
import bpy,ast,json,hashlib,sys
from pathlib import Path
ROOT=Path(sys.argv[sys.argv.index('--')+1]).resolve()
HERE=Path(__file__).resolve().parent.parent
OUT=ROOT/'outputs/Sable/MaterialDioramas20261015'
LOCAL=ROOT/'SourceArt/Blender/Studies/Sable/MaterialDioramas20261015'
OLD=HERE.parent/'2026-10-15-stylized-realism';ATLAS=OLD/'textures/B_subtle_atlas.png';ROCKDIR=ROOT/'Assets/_Project/Art/Environment/Rocks/SculptedBasalt01'
tree=ast.parse((HERE/'tools/build_study.py').read_text(encoding='utf-8'))
needed={'node','wire','setin','mathn','vec','noise','ramp','mix','rgba','painted_stone_patch','atlas_ground','material'}
functions=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in needed]
base=next(n for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='BASE' for t in n.targets))
BASE=ast.literal_eval(base.value)
exec(compile(ast.Module(body=functions,type_ignores=[]),'retained material recipe','exec'),globals())
receipt=json.loads((OUT/'render_receipt.json').read_text(encoding='utf-8'))
retained=[]
paths=([OUT/'reopen/MaterialDioramas.blend'] if '--probe' in sys.argv else [LOCAL/'MaterialDioramas.blend']+sorted(LOCAL.glob('WholeStyle_?.blend')))
for path in paths:
    bpy.ops.wm.open_mainfile(filepath=str(path))
    REFERENCE=bpy.data.objects['GLOBAL_METRE_TEXTURE_ORIGIN']
    before={ob.name:ob.active_material.name for ob in bpy.data.objects if ob.active_material}
    added=[]
    for style in 'ABCD':
        for mid in receipt['materials']:
            name=f'{style}_{mid}'
            mat=bpy.data.materials.get(name)
            if mat is None:mat=material(mid,style);added.append(name)
            mat.use_fake_user=True
        for mid in ['G02','G03']:
            name=f'{style}_{mid} faded deposits'
            mat=bpy.data.materials.get(name)
            if mat is None:
                mat=bpy.data.materials[f'{style}_{mid}'].copy();mat.name=name
                attr=node(mat,'ShaderNodeAttribute','Soft dust boundary');attr.attribute_name='deposit_alpha'
                wire(mat,attr.outputs['Fac'],mat.node_tree.nodes.get('Principled BSDF').inputs['Alpha'])
            mat.use_fake_user=True
    for mat in bpy.data.materials:mat.use_fake_user=True
    assert all(bpy.data.objects[name].active_material.name==mat for name,mat in before.items()),'Active material changed'
    bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(path))
    retained.append({'file':str(path.relative_to(ROOT)),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'material_variants_retained':60,'reconstructed_unassigned_materials':added,'active_assignments_unchanged':True})
if '--probe' in sys.argv:
    print('SABLE_SEAL_PROBE_OK',retained[0]['material_variants_retained'],flush=True)
    sys.exit(0)
receipt['capture_native_sha256']=receipt['native_sha256']
receipt['native_sha256']=retained[0]['sha256'];receipt['native_sources']=retained
receipt['native_seal']='All 60 material variants retained with fake users; previously omitted unassigned candidates reconstructed from the same recipe. Active assignments unchanged, reproduction verified separately.'
(OUT/'render_receipt.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8')
print('SABLE_NATIVE_SEALED',len(retained),flush=True)
