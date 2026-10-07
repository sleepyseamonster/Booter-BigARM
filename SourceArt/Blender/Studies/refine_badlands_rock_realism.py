"""Add non-destructive erosion detail to rocks in the open badlands study.

Run once in the Blender Python Console of BrokenWorldBadlandsStudy.blend.
Existing rock positions, materials, debris fans, terrain, and cameras stay put.
Modifiers can be disabled or adjusted independently for visual review.
"""

from pathlib import Path

import bpy


STUDY_FILE = "BrokenWorldBadlandsStudy.blend"
TAG = "badlands_rock_realism_pass"


def cloud_texture(name, scale):
    texture = bpy.data.textures.get(name)
    if texture is None:
        texture = bpy.data.textures.new(name, type="CLOUDS")
    texture.noise_scale = scale
    texture.noise_depth = 3
    texture.noise_type = "SOFT_NOISE"
    return texture


def refine(obj, bevel_width, displacement, cloud_scale, subdivision,
           smooth=True):
    if obj.get(TAG):
        return False
    if obj.type != "MESH" or len(obj.data.polygons) < 6:
        raise RuntimeError(f"Expected rock mesh for {obj.name}")
    if bevel_width:
        bevel = obj.modifiers.new("Eroded fractured edges", "BEVEL")
        bevel.width = bevel_width
        bevel.segments = 3 if bevel_width > 0.08 else 2
        bevel.limit_method = "ANGLE"
        bevel.angle_limit = 0.49
        bevel.loop_slide = True
    if subdivision:
        subdiv = obj.modifiers.new("Dense rock surface", "SUBSURF")
        subdiv.subdivision_type = "SIMPLE"
        subdiv.levels = subdivision
        subdiv.render_levels = subdivision
    if displacement:
        displace = obj.modifiers.new("Weathered micro silhouette", "DISPLACE")
        displace.texture = cloud_texture(f"Rock erosion clouds {cloud_scale:.2f} m", cloud_scale)
        displace.texture_coords = "LOCAL"
        displace.strength = displacement
        displace.mid_level = 0.5
        fine = obj.modifiers.new("Fine chipped stone grain", "DISPLACE")
        fine.texture = cloud_texture(f"Rock fine chips {cloud_scale * 0.27:.2f} m", cloud_scale * 0.27)
        fine.texture_coords = "LOCAL"
        fine.strength = displacement * 0.23
        fine.mid_level = 0.5
    if smooth:
        for face in obj.data.polygons:
            face.use_smooth = True
    obj[TAG] = True
    return True


def main():
    if Path(bpy.data.filepath).name != STUDY_FILE:
        raise RuntimeError(f"Open {STUDY_FILE} first")
    scene = bpy.context.scene
    shelf = scene.objects.get("Reference low eroded shelf")
    if shelf is None:
        raise RuntimeError("Expected the existing low eroded shelf")
    counts = {"shelf": 0, "formations": 0, "stones": 0, "talus": 0, "scree": 0}
    counts["shelf"] += refine(shelf, 0.13, 0.24, 0.55, 3)
    for obj in scene.objects:
        if obj.type != "MESH":
            continue
        if obj.name.startswith("Formation_"):
            counts["formations"] += refine(obj, 0.075, 0.11, 0.38, 2)
        elif obj.name.startswith("Stone_"):
            counts["stones"] += refine(obj, 0.035, 0.065, 0.21, 2)
        elif obj.name.startswith("Reference scree apron"):
            # These six meshes contain 1,920 disconnected small fragments.
            counts["scree"] += refine(obj, 0.011, 0.027, 0.11, 2)
        elif obj.name.startswith("Talus_"):
            counts["talus"] += refine(obj, 0.022, 0.035, 0.16, 2)
    print("Refined badlands rocks", counts)
    print(f"Save the currently open {STUDY_FILE}")


if __name__ == "__main__":
    main()
