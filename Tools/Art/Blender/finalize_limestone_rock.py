"""Keep all optional maps in the delivered blend; create a readme inside it.
Run only on the task-owned LimestoneRock01_GameReady.blend in background.
"""
from pathlib import Path
import bpy
base=Path(bpy.data.filepath).parent
assert base.name=='LimestoneRock01' and Path(bpy.data.filepath).name=='LimestoneRock01_GameReady.blend'
for kind in ('BaseColor','Roughness','Occlusion','Height','Normal','MetallicSmoothness'):
    image=bpy.data.images.get('LimestoneRock01_'+kind)
    if image is None: image=bpy.data.images.load(str(base/'Exports'/f'LimestoneRock01_{kind}.png'))
    image.name='LimestoneRock01_'+kind
    image.colorspace_settings.name='sRGB' if kind=='BaseColor' else 'Non-Color'
    image.use_fake_user=True; image.pack()
text=bpy.data.texts.get('START HERE - Limestone Rock') or bpy.data.texts.new('START HERE - Limestone Rock')
text.clear()
text.write('''LimestoneRock01_LOD0 is the optimized, textured rock: 712 triangles, 358 mesh vertices.
LOD1: 356 triangles. LOD2: 178 triangles. Convex collision proxy: 220 triangles.
The original Icosphere sculpt and high-detail bake source remain hidden and intact.
This is a separate version of your Booter ground scene; the artist source was not overwritten.
Six 2048px maps are packed and also saved under Exports: BaseColor, Roughness,
Occlusion, Height, tangent-space Normal (+Y), and Unity MetallicSmoothness.
BaseColor uses sRGB. All other maps use Non-Color. Metallic RGB is zero;
MetallicSmoothness alpha is 1 minus roughness. The normals include sculpted
detail and restrained relief derived from the dark workbench side surface map.
This light limestone palette retains the source texture's exact surface patterns.
The visible asset is LOD0; other detail levels and collision are hidden.
The FBX contains only the three detail levels and collision mesh at a bottom-centre
pivot. It excludes Booter, ground, camera, lights and sculpt source.
The LODs share the same texture set and are intended for progressively distant views.
No Unity production import, placement, LOD thresholds or runtime validation was performed.
Ctrl+S saves this local-only blend. Unity production integration is a separate handoff.
''')
camera=bpy.context.scene.camera
camera.data.type='PERSP'
low=bpy.data.objects['LimestoneRock01_LOD0']
from mathutils import Vector
target=low.location+Vector((0,0,low.dimensions.z/2))
camera.location=target+Vector((4,-6,3))
camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath,check_existing=False)
print('Saved all six packed maps and in-file editing instructions.')
