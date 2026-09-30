"""Render a fixed inspection view without saving changes to the .blend.

Blender --background FILE --python THIS -- --view close|wide|ground-level --output PATH
Optional --path-traced and --samples 48 for a final lighting review.
"""
import argparse
import sys
sys.dont_write_bytecode=True
import importlib.util
from pathlib import Path
import bpy

p=argparse.ArgumentParser()
p.add_argument('--view',choices=['close','wide','ground-level'],default='wide')
p.add_argument('--output',required=True)
p.add_argument('--path-traced',action='store_true')
p.add_argument('--samples',type=int,default=32)
args=p.parse_args(sys.argv[sys.argv.index('--')+1:])
path=Path(__file__).with_name('author_badlands_realism.py')
spec=importlib.util.spec_from_file_location('author_realism',path)
author=importlib.util.module_from_spec(spec); spec.loader.exec_module(author)
author.review_cameras()
s=bpy.context.scene
s.camera=bpy.data.objects['Geology '+args.view+' review']
s.render.engine='CYCLES' if args.path_traced else 'BLENDER_EEVEE'
if args.path_traced:
    s.cycles.samples=args.samples
    s.cycles.use_denoising=True
    prefs=bpy.context.preferences.addons['cycles'].preferences
    prefs.compute_device_type='METAL'; prefs.get_devices()
    for device in prefs.devices: device.use=device.type=='METAL'
    s.cycles.device='GPU'
s.render.resolution_x=1400
s.render.resolution_y=840
s.render.resolution_percentage=100
s.render.image_settings.file_format='PNG'
s.render.filepath=args.output
bpy.ops.render.render(write_still=True)
