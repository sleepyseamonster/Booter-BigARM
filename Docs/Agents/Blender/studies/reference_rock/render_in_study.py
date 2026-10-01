"""Render the rock in an already loaded temporary or saved study; never save."""
from pathlib import Path
import argparse
import sys
import bpy
from mathutils import Vector

p=argparse.ArgumentParser();p.add_argument('--output',required=True)
p.add_argument('--add',action='store_true')
a=p.parse_args(sys.argv[sys.argv.index('--')+1:])
if a.add:
    import runpy
    runpy.run_path(str(Path(__file__).with_name('add_reference_rock_to_study.py')),run_name='__main__')
rock=bpy.data.objects['ReferenceRock_FracturedSlate_01']
target=rock.location+Vector((0,0,1.5))
data=bpy.data.cameras.new('Temporary reference-rock review')
cam=bpy.data.objects.new('Temporary reference-rock review',data)
bpy.context.scene.collection.objects.link(cam)
cam.location=target+Vector((4,-7,3.2))
cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
data.lens=52
scene=bpy.context.scene;scene.camera=cam
scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=1300;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.filepath=a.output
bpy.ops.render.render(write_still=True)
