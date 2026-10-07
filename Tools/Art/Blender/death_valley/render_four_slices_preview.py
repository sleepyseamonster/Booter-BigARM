"""Render read-only full-map and basin-eye previews of a saved four-slice scene."""

from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def main() -> None:
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-dir",type=Path,required=True)
    args=parser.parse_args(sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else [])
    args.output_dir.mkdir(parents=True,exist_ok=True)
    scene=bpy.context.scene
    scene.render.engine="BLENDER_EEVEE"
    scene.render.resolution_x=1200
    scene.render.resolution_y=900
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format="PNG"
    scene.view_settings.view_transform="Standard"
    scene.world.use_nodes=True
    scene.world.node_tree.nodes["Background"].inputs["Color"].default_value=(0.18,0.19,0.22,1)
    scene.world.node_tree.nodes["Background"].inputs["Strength"].default_value=0.65
    light_data=bpy.data.lights.new("TemporarySliceSun",type="SUN")
    light_data.energy=1.6
    light_data.angle=math.radians(8)
    light=bpy.data.objects.new("TemporarySliceSun",light_data)
    scene.collection.objects.link(light)
    light.rotation_euler=(math.radians(25),math.radians(-20),math.radians(-45))
    data=bpy.data.cameras.new("TemporaryFourSliceMap")
    data.type="ORTHO"
    data.ortho_scale=6100
    data.clip_end=10000
    camera=bpy.data.objects.new("TemporaryFourSliceMap",data)
    scene.collection.objects.link(camera)
    camera.location=(0,0,5500)
    camera.rotation_euler=(Vector((0,0,0))-camera.location).to_track_quat("-Z","Y").to_euler()
    scene.camera=camera
    map_path=args.output_dir/"badwater_four_slices_map.png"
    scene.render.filepath=str(map_path)
    bpy.ops.render.render(write_still=True)
    scene.camera=bpy.data.objects["BasinEyeView_1p7m"]
    eye_path=args.output_dir/"badwater_four_slices_eye.png"
    scene.render.filepath=str(eye_path)
    bpy.ops.render.render(write_still=True)
    print(json.dumps({"map":str(map_path),"eye":str(eye_path)}))


if __name__=="__main__":
    main()
