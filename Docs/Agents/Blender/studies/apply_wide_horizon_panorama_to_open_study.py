"""Use the user's exact wide landscape PNG as this study's World environment.

Run once in the Python Console of the open BrokenWorldBadlandsStudy.blend.
The 8-bit PNG is an LDR panorama, not a true high-dynamic-range HDRI. The
existing sun and area lights remain responsible for strong directional light.
"""

from pathlib import Path

import bpy
from mathutils import Vector


STUDY_FILE = "BrokenWorldBadlandsStudy.blend"
PANORAMA_NAME = "BadlandsWideHorizonPanorama.png"
WORLD_NAME = "Badlands wide horizon panorama - LDR study"
CAMERA_NAME = "Wide horizon terrain review camera"


def main():
    if Path(bpy.data.filepath).name != STUDY_FILE:
        raise RuntimeError(f"Open {STUDY_FILE} first")

    scene = bpy.context.scene
    terrain = scene.objects.get("Badlands terrain - study only")
    if terrain is None or terrain.get("expanded_authoring_area_m") != 360:
        raise RuntimeError("Expected the existing 360 m badlands study")

    panorama_path = Path(bpy.data.filepath).parent / "references" / PANORAMA_NAME
    if not panorama_path.is_file():
        raise FileNotFoundError(panorama_path)
    image = bpy.data.images.get(PANORAMA_NAME)
    if image is None:
        image = bpy.data.images.load(str(panorama_path), check_existing=True)
    if tuple(image.size) != (1774, 887):
        raise RuntimeError("Unexpected panorama dimensions; do not replace the reference")
    image.name = PANORAMA_NAME
    image.filepath = str(panorama_path)
    image.pack()

    world = bpy.data.worlds.get(WORLD_NAME)
    if world is None:
        world = bpy.data.worlds.new(WORLD_NAME)
    world.use_nodes = True
    nodes = world.node_tree.nodes
    nodes.clear()
    environment = nodes.new("ShaderNodeTexEnvironment")
    environment.name = "Exact user panorama - equirectangular LDR"
    environment.image = image
    environment.projection = "EQUIRECTANGULAR"
    environment.location = (-360, 0)
    background = nodes.new("ShaderNodeBackground")
    background.name = "Panorama environment fill"
    background.inputs["Strength"].default_value = 0.65
    background.location = (-80, 0)
    output = nodes.new("ShaderNodeOutputWorld")
    output.location = (150, 0)
    world.node_tree.links.new(environment.outputs["Color"], background.inputs["Color"])
    world.node_tree.links.new(background.outputs["Background"], output.inputs["Surface"])
    world["reference_note"] = (
        "Exact user 8-bit PNG mapped equirectangularly; an LDR environment, not HDR radiance"
    )
    scene.world = world

    camera = scene.objects.get(CAMERA_NAME)
    if camera is None:
        camera = bpy.data.objects.new(CAMERA_NAME, bpy.data.cameras.new(CAMERA_NAME))
        scene.collection.objects.link(camera)
    camera.location = (5.0, 75.0, 3.4)
    target = Vector((3.0, -45.0, 37.0))
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = 18
    camera.data.clip_end = 500
    camera["reference_note"] = "Low eye-level review of broad terrain and panoramic horizon"
    scene.camera = camera
    print("Applied exact 1774 x 887 PNG as an LDR equirectangular World environment")
    print("Added wide horizon camera; existing geometry, materials, and lights retained")
    print(f"Save the currently open {STUDY_FILE}")


if __name__ == "__main__":
    main()
