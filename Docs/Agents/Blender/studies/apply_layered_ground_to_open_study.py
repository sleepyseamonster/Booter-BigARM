"""Layer rust sand, exposed shale, swept dust, and local pebble aprons.

Run once in the Python Console of the open BrokenWorldBadlandsStudy.blend.
The pass copies the existing material, retains its original ground masks, and
adds a separate point attribute for texture buildup around the low formations.
"""

from math import hypot
from pathlib import Path

import bpy
from mathutils import Vector


STUDY_FILE = "BrokenWorldBadlandsStudy.blend"
LAYERED_MATERIAL = "StudyGround_LayeredBadlandsReferences"
APRON_ATTRIBUTE = "ReferenceGroundApron"
REVIEW_CAMERA = "Layered ground texture review camera"


def source_image(name):
    image = bpy.data.images.get(name)
    if image is None:
        study = Path(__file__).resolve().parent
        source = study / "textures" / name
        if not source.is_file():
            repo = Path(__file__).resolve().parents[4]
            source = repo / "Assets/_Project/Art/Environment/Ground/SandDirt" / name
        if not source.is_file():
            raise FileNotFoundError(source)
        image = bpy.data.images.load(str(source), check_existing=True)
    image.pack()
    return image


def paint_apron_attribute(terrain, scene):
    mesh = terrain.data
    attribute = mesh.attributes.get(APRON_ATTRIBUTE)
    if attribute is None:
        attribute = mesh.attributes.new(APRON_ATTRIBUTE, "FLOAT", "POINT")
    cores = []
    for index in range(6):
        name = f"Formation_{index:02d}_Core" if index < 5 else "Reference low eroded shelf"
        obj = scene.objects.get(name)
        if obj is None:
            raise RuntimeError(f"Expected low formation: {name}")
        cores.append((obj.location.x, obj.location.y,
                      0.35 * max(obj.dimensions.x, obj.dimensions.y)))
    values = []
    for vertex in mesh.vertices:
        x, y = vertex.co.x + terrain.location.x, vertex.co.y + terrain.location.y
        nearest = min(max(0.0, hypot(x - cx, y - cy) - radius)
                      for cx, cy, radius in cores)
        # A broad dusty transition surrounds the tight dark rubble at each foot.
        values.append(max(0.0, 1.0 - nearest / 15.0) ** 1.7)
    attribute.data.foreach_set("value", values)


def node(nodes, kind, name, x, y):
    result = nodes.new(kind)
    result.name = name
    result.label = name
    result.location = (x, y)
    return result


def ramp(nodes, links, source, name, stops, x, y):
    result = node(nodes, "ShaderNodeValToRGB", name, x, y)
    result.color_ramp.elements[0].position = stops[0]
    result.color_ramp.elements[1].position = stops[1]
    links.new(source, result.inputs["Factor"])
    return result.outputs["Color"]


def image_layer(nodes, links, position, image_name, scale, x, y):
    mapping = node(nodes, "ShaderNodeVectorMath", f"{image_name} world scale", x, y)
    mapping.operation = "SCALE"
    mapping.inputs["Scale"].default_value = scale
    links.new(position, mapping.inputs["Vector"])
    texture = node(nodes, "ShaderNodeTexImage", image_name, x + 190, y)
    texture.image = source_image(image_name)
    texture.extension = "MIRROR" if image_name.startswith("Badlands") else "REPEAT"
    texture.interpolation = "Linear"
    links.new(mapping.outputs["Vector"], texture.inputs["Vector"])
    return texture


def main():
    if Path(bpy.data.filepath).name != STUDY_FILE:
        raise RuntimeError(f"Open {STUDY_FILE} first")
    scene = bpy.context.scene
    terrain = scene.objects.get("Badlands terrain - study only")
    if terrain is None or terrain.get("expanded_authoring_area_m") != 360:
        raise RuntimeError("Expected the existing 360 m badlands terrain")
    if terrain.active_material.name == LAYERED_MATERIAL:
        print("Layered ground pass is already active")
        return
    original = terrain.active_material
    if original is None or original.name != "StudyGround_SandGravelRock":
        raise RuntimeError("Expected the original study ground material")
    paint_apron_attribute(terrain, scene)
    material = original.copy()
    material.name = LAYERED_MATERIAL
    terrain.active_material = material
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    principal = nodes.get("Principled BSDF")
    balance = nodes.get("Dry earth color balance")
    swept = nodes.get("Image Texture.003")
    if not all((principal, balance, swept)):
        raise RuntimeError("Expected original ground shader nodes")
    base = nodes.get("Image Texture")
    if base is None:
        raise RuntimeError("Expected original rust soil texture node")
    base.image = source_image("BadlandsCompactedRustSoil.png")
    base.extension = "MIRROR"
    nodes["Vector Math"].inputs["Scale"].default_value = 0.14

    position_node = node(nodes, "ShaderNodeNewGeometry", "Layered ground world position", -1150, -850)
    position = position_node.outputs["Position"]
    apron = node(nodes, "ShaderNodeAttribute", "Dark debris near low formations", -1130, -1200)
    apron.attribute_name = APRON_ATTRIBUTE

    broad = node(nodes, "ShaderNodeTexNoise", "Broad exposed-rock islands", -900, -950)
    broad.inputs["Scale"].default_value = 0.055
    broad.inputs["Detail"].default_value = 3.1
    broad.inputs["Roughness"].default_value = 0.58
    links.new(position, broad.inputs["Vector"])
    broad_mask = ramp(nodes, links, broad.outputs["Fac"], "Soft rock island edge",
                      (0.40, 0.63), -680, -920)

    rocky = image_layer(nodes, links, position, "BadlandsEmbeddedShaleGravel.png",
                        0.19, -700, -1330)
    rocky_weight = node(nodes, "ShaderNodeMath", "Exposed rock coverage", -390, -920)
    rocky_weight.operation = "MULTIPLY"
    rocky_weight.inputs[1].default_value = 0.72
    links.new(broad_mask, rocky_weight.inputs[0])
    apron_weight = node(nodes, "ShaderNodeMath", "Rock at formation feet", -390, -1100)
    apron_weight.operation = "MULTIPLY"
    apron_weight.inputs[1].default_value = 0.36
    links.new(apron.outputs["Fac"], apron_weight.inputs[0])
    rocky_total = node(nodes, "ShaderNodeMath", "Overlapping rock coverage", -180, -990)
    rocky_total.operation = "ADD"
    rocky_total.use_clamp = True
    links.new(rocky_weight.outputs[0], rocky_total.inputs[0])
    links.new(apron_weight.outputs[0], rocky_total.inputs[1])
    rocky_mix = node(nodes, "ShaderNodeMixRGB", "Rust soil over rocky transition", 50, -900)
    rocky_mix.blend_type = "MIX"
    links.new(rocky_total.outputs[0], rocky_mix.inputs[0])
    links.new(balance.outputs["Color"], rocky_mix.inputs[1])
    links.new(rocky.outputs["Color"], rocky_mix.inputs[2])

    pebbles = image_layer(nodes, links, position, "BadlandsDarkScree.png",
                          0.23, -700, -1720)
    pebble_noise = node(nodes, "ShaderNodeTexNoise", "Broken pebble coverage", -900, -1900)
    pebble_noise.inputs["Scale"].default_value = 0.25
    pebble_noise.inputs["Detail"].default_value = 2.2
    links.new(position, pebble_noise.inputs["Vector"])
    pebble_edge = ramp(nodes, links, pebble_noise.outputs["Fac"],
                       "Irregular dark gravel edge", (0.32, 0.63), -670, -1900)
    pebble_weight = node(nodes, "ShaderNodeMath", "Pebble apron strength", -380, -1760)
    pebble_weight.operation = "MULTIPLY"
    pebble_weight.inputs[1].default_value = 0.78
    links.new(apron.outputs["Fac"], pebble_weight.inputs[0])
    pebble_total = node(nodes, "ShaderNodeMath", "Patchy dark debris mask", -150, -1760)
    pebble_total.operation = "MULTIPLY"
    links.new(pebble_weight.outputs[0], pebble_total.inputs[0])
    links.new(pebble_edge, pebble_total.inputs[1])
    pebble_mix = node(nodes, "ShaderNodeMixRGB", "Dark pebbles in rust dust", 330, -900)
    pebble_mix.blend_type = "MIX"
    links.new(pebble_total.outputs[0], pebble_mix.inputs[0])
    links.new(rocky_mix.outputs["Color"], pebble_mix.inputs[1])
    links.new(pebbles.outputs["Color"], pebble_mix.inputs[2])

    sweep_noise = node(nodes, "ShaderNodeTexNoise", "Swept dust ribbons", -920, -2280)
    sweep_noise.inputs["Scale"].default_value = 0.082
    sweep_noise.inputs["Detail"].default_value = 2.0
    links.new(position, sweep_noise.inputs["Vector"])
    sweep_edge = ramp(nodes, links, sweep_noise.outputs["Fac"],
                      "Feathered dust edge", (0.50, 0.72), -680, -2260)
    sweep_weight = node(nodes, "ShaderNodeMath", "Thin dust coverage", -390, -2220)
    sweep_weight.operation = "MULTIPLY"
    sweep_weight.inputs[1].default_value = 0.25
    links.new(sweep_edge, sweep_weight.inputs[0])
    dust_tint = node(nodes, "ShaderNodeMixRGB", "Warm the pale swept sand", -130, -2260)
    dust_tint.blend_type = "MULTIPLY"
    dust_tint.inputs[0].default_value = 0.75
    dust_tint.inputs[2].default_value = (0.58, 0.33, 0.20, 1.0)
    links.new(swept.outputs["Color"], dust_tint.inputs[1])
    final_color = node(nodes, "ShaderNodeMixRGB", "Fine rust dust over rubble", 620, -900)
    final_color.blend_type = "MIX"
    links.new(sweep_weight.outputs[0], final_color.inputs[0])
    links.new(pebble_mix.outputs["Color"], final_color.inputs[1])
    links.new(dust_tint.outputs["Color"], final_color.inputs[2])
    links.new(final_color.outputs["Color"], principal.inputs["Base Color"])

    height = image_layer(nodes, links, position, "BrokenWorldRockyMidTransitionHeight.png",
                         0.165, -700, -2680)
    height.image.colorspace_settings.name = "Non-Color"
    bump = node(nodes, "ShaderNodeBump", "Subtle embedded stone relief", 610, -1270)
    bump.inputs["Strength"].default_value = 0.20
    bump.inputs["Distance"].default_value = 0.052
    links.new(height.outputs["Color"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], principal.inputs["Normal"])

    roughness = node(nodes, "ShaderNodeMapRange", "Dust-to-stone roughness", 350, -1510)
    roughness.inputs["From Min"].default_value = 0.0
    roughness.inputs["From Max"].default_value = 1.0
    roughness.inputs["To Min"].default_value = 0.95
    roughness.inputs["To Max"].default_value = 0.83
    links.new(rocky_total.outputs[0], roughness.inputs["Value"])
    links.new(roughness.outputs["Result"], principal.inputs["Roughness"])
    material["reference_note"] = (
        "Layered rust sand, rocky islands, dark rubble aprons, and swept dust; provisional Blender study"
    )
    camera = scene.objects.get(REVIEW_CAMERA)
    if camera is None:
        camera = bpy.data.objects.new(REVIEW_CAMERA, bpy.data.cameras.new(REVIEW_CAMERA))
        scene.collection.objects.link(camera)
    camera.location = (50.0, -40.0, 18.0)
    camera.rotation_euler = (Vector((64.0, -9.0, 0.0)) - camera.location).to_track_quat(
        "-Z", "Y"
    ).to_euler()
    camera.data.lens = 38
    camera.data.clip_end = 500
    scene.camera = camera
    print("Layered ground active: broad rock islands, local pebble aprons, fine dust, and relief")
    print(f"Save the currently open {STUDY_FILE}")


if __name__ == "__main__":
    main()
