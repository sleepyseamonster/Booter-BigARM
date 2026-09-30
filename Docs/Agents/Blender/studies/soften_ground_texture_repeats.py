"""Break visible mirrored ground repeats in the open badlands study.

Keep the previous layered material for comparison. Each active ground image is
sampled twice at different rotations, offsets, and scales. A shared continuous
coordinate warp bends the repeat boundaries; a broad noise mask blends the two
samples without introducing square-shaped transition masks.
"""

from math import radians
from pathlib import Path

import bpy


STUDY_FILE = "BrokenWorldBadlandsStudy.blend"
OLD_MATERIAL = "StudyGround_LayeredBadlandsReferences"
NEW_MATERIAL = "StudyGround_SoftStochasticBadlands"


def make_node(nodes, kind, name, x, y):
    result = nodes.new(kind)
    result.name = name
    result.label = name
    result.location = (x, y)
    return result


def main():
    if Path(bpy.data.filepath).name != STUDY_FILE:
        raise RuntimeError(f"Open {STUDY_FILE} first")
    terrain = bpy.context.scene.objects.get("Badlands terrain - study only")
    if terrain is None or terrain.active_material is None:
        raise RuntimeError("Expected the existing badlands terrain and ground material")
    if terrain.active_material.name == NEW_MATERIAL:
        print("Soft stochastic ground pass is already active")
        return
    if terrain.active_material.name != OLD_MATERIAL:
        raise RuntimeError(f"Expected {OLD_MATERIAL} as the active terrain material")

    # Blender removes unreferenced materials on save unless they have a fake user.
    terrain.active_material.use_fake_user = True
    material = terrain.active_material.copy()
    material.name = NEW_MATERIAL
    terrain.active_material = material
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    geometry = nodes.get("Layered ground world position")
    if geometry is None:
        raise RuntimeError("Expected the prior layered ground world coordinates")
    position = geometry.outputs["Position"]

    warp = make_node(nodes, "ShaderNodeTexNoise", "Low-frequency ground coordinate warp", -2100, -3000)
    warp.inputs["Scale"].default_value = 0.095
    warp.inputs["Detail"].default_value = 2.0
    warp.inputs["Roughness"].default_value = 0.56
    links.new(position, warp.inputs["Vector"])
    center = make_node(nodes, "ShaderNodeVectorMath", "Center coordinate warp", -1870, -3000)
    center.operation = "SUBTRACT"
    center.inputs[1].default_value = (0.5, 0.5, 0.5)
    links.new(warp.outputs["Color"], center.inputs[0])
    strength = make_node(nodes, "ShaderNodeVectorMath", "Warp repeat boundaries in meters", -1650, -3000)
    strength.operation = "SCALE"
    strength.inputs["Scale"].default_value = 2.1
    links.new(center.outputs["Vector"], strength.inputs[0])
    warped = make_node(nodes, "ShaderNodeVectorMath", "Distorted world position", -1420, -3000)
    warped.operation = "ADD"
    links.new(position, warped.inputs[0])
    links.new(strength.outputs["Vector"], warped.inputs[1])

    originals = [node for node in nodes if node.type == "TEX_IMAGE" and node.image is not None]
    if len(originals) != 7:
        raise RuntimeError(f"Expected seven active image samples, found {len(originals)}")
    for index, texture in enumerate(originals):
        vector_links = list(texture.inputs["Vector"].links)
        if len(vector_links) != 1 or vector_links[0].from_node.type != "VECT_MATH":
            raise RuntimeError(f"Expected one existing world-scale input for {texture.name}")
        mapping = vector_links[0].from_node
        scale = mapping.inputs["Scale"].default_value
        downstream = [(link.to_node, link.to_socket) for link in list(texture.outputs["Color"].links)]
        if not downstream:
            continue

        links.new(warped.outputs["Vector"], mapping.inputs["Vector"])
        texture.extension = "MIRROR"

        y = -3350 - index * 500
        rotation = make_node(nodes, "ShaderNodeVectorRotate", f"{texture.name} alternate rotation", -1210, y)
        rotation.rotation_type = "Z_AXIS"
        rotation.inputs["Angle"].default_value = radians(28 + index * 11)
        links.new(warped.outputs["Vector"], rotation.inputs["Vector"])
        alternate_scale = make_node(nodes, "ShaderNodeVectorMath", f"{texture.name} alternate scale", -990, y)
        alternate_scale.operation = "SCALE"
        alternate_scale.inputs["Scale"].default_value = scale * (1.17 + (index % 3) * 0.07)
        links.new(rotation.outputs["Vector"], alternate_scale.inputs["Vector"])
        offset = make_node(nodes, "ShaderNodeVectorMath", f"{texture.name} offset alternate tiles", -770, y)
        offset.operation = "ADD"
        offset.inputs[1].default_value = (0.37 + index * 0.19, 0.63 + index * 0.23, 0.0)
        links.new(alternate_scale.outputs["Vector"], offset.inputs[0])
        alternate = make_node(nodes, "ShaderNodeTexImage", f"{texture.name} rotated overlap", -550, y)
        alternate.image = texture.image
        alternate.extension = "MIRROR"
        alternate.interpolation = "Linear"
        links.new(offset.outputs["Vector"], alternate.inputs["Vector"])

        coverage = make_node(nodes, "ShaderNodeTexNoise", f"{texture.name} soft overlap field", -1210, y - 220)
        coverage.inputs["Scale"].default_value = 0.047 + index * 0.008
        coverage.inputs["Detail"].default_value = 2.0
        links.new(position, coverage.inputs["Vector"])
        mix_range = make_node(nodes, "ShaderNodeMapRange", f"{texture.name} balanced overlap", -770, y - 220)
        mix_range.inputs["From Min"].default_value = 0.25
        mix_range.inputs["From Max"].default_value = 0.75
        mix_range.inputs["To Min"].default_value = 0.36
        mix_range.inputs["To Max"].default_value = 0.64
        links.new(coverage.outputs["Fac"], mix_range.inputs["Value"])
        blend = make_node(nodes, "ShaderNodeMixRGB", f"{texture.name} soft overlapping samples", -310, y)
        blend.blend_type = "MIX"
        links.new(mix_range.outputs["Result"], blend.inputs[0])
        links.new(texture.outputs["Color"], blend.inputs[1])
        links.new(alternate.outputs["Color"], blend.inputs[2])
        for to_node, to_socket in downstream:
            links.new(blend.outputs["Color"], to_socket)

    material["texture_repeat_solution"] = (
        "Warped and crossfaded differently rotated image samples; previous layered material retained"
    )
    print(f"Ground repeats softened in the open {STUDY_FILE}; save this file")


if __name__ == "__main__":
    main()
