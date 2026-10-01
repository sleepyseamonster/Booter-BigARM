"""Give the primary Formation_00 outcrop black porous volcanic stone.

Run in the existing badlands study. Other formations and smaller rocks retain
their lighter material variation. Save and inspect the live result afterward.
"""

from pathlib import Path

import bpy


STUDY = "BrokenWorldBadlandsStudy.blend"
TEXTURES = Path(__file__).with_name("textures")
MATERIAL = "Geology - black porous volcanic rock"
MARKER = "primary_black_volcanic_rock_2026_09_30"
OBJECTS = (
    "Formation_00_Core",
    "Formation_00_Buttress_0",
    "Formation_00_Buttress_1",
    "Formation_00_Buttress_2",
)


def apply():
    if Path(bpy.data.filepath).name != STUDY:
        raise RuntimeError(f"Open the existing {STUDY}")
    images = {}
    for channel, suffix in (
        ("Albedo", "Albedo.png"),
        ("Roughness", "Roughness.png"),
        ("Height", "Height16.png"),
        ("AO", "AO.png"),
        ("Metallic", "Metallic.png"),
    ):
        path = TEXTURES / f"VolcanicBlackBasalt_{suffix}"
        if not path.is_file():
            raise FileNotFoundError(path)
        images[channel] = path
    targets = [bpy.data.objects.get(name) for name in OBJECTS]
    if any(obj is None or obj.type != "MESH" for obj in targets):
        raise RuntimeError("Primary formation is missing a mesh")
    if all(obj.get(MARKER) for obj in targets):
        print("Black volcanic material already assigned to the primary formation")
        return

    original = targets[0].active_material
    if original is None or original.node_tree is None:
        raise RuntimeError("Expected the existing geological rock shader")
    material = bpy.data.materials.get(MATERIAL)
    if material is None:
        material = original.copy()
        material.name = MATERIAL
        material.diffuse_color = (0.055, 0.057, 0.061, 1.0)
        nodes, links = material.node_tree.nodes, material.node_tree.links
        shader = next(node for node in nodes if node.type == "BSDF_PRINCIPLED")
        for link in tuple(shader.inputs["Base Color"].links):
            links.remove(link)

        coordinates = nodes.new("ShaderNodeTexCoord")
        coordinates.name = "Local rock coordinates"
        coordinates.location = (-1050, 480)

        textures = {}
        for index, (channel, path) in enumerate(images.items()):
            image = bpy.data.images.load(str(path), check_existing=True)
            image.colorspace_settings.name = "sRGB" if channel == "Albedo" else "Non-Color"
            image.pack()
            tex = nodes.new("ShaderNodeTexImage")
            tex.name = f"Volcanic {channel}"
            tex.image = image
            tex.projection = "BOX"
            tex.projection_blend = 0.35
            tex.extension = "EXTEND"
            tex.location = (-790, 600 - index * 240)
            links.new(coordinates.outputs["Generated"], tex.inputs["Vector"])
            textures[channel] = tex

        ao_mix = nodes.new("ShaderNodeMixRGB")
        ao_mix.name = "Cavity shading restrained"
        ao_mix.blend_type = "MULTIPLY"
        ao_mix.inputs[0].default_value = .32
        links.new(textures["Albedo"].outputs["Color"], ao_mix.inputs[1])
        links.new(textures["AO"].outputs["Color"], ao_mix.inputs[2])
        links.new(ao_mix.outputs["Color"], shader.inputs["Base Color"])
        for socket in ("Roughness", "Metallic"):
            for link in tuple(shader.inputs[socket].links):
                links.remove(link)
            links.new(textures[socket].outputs["Color"], shader.inputs[socket])
        shader.inputs["Specular IOR Level"].default_value = .28

        for link in tuple(shader.inputs["Normal"].links):
            links.remove(link)
        bump = nodes.new("ShaderNodeBump")
        bump.name = "Porous basalt from aligned height map"
        bump.inputs["Strength"].default_value = .27
        bump.inputs["Distance"].default_value = .055
        links.new(textures["Height"].outputs["Color"], bump.inputs["Height"])
        links.new(bump.outputs["Normal"], shader.inputs["Normal"])
        material[MARKER] = "Matched seamless PBR maps; height bump in Blender, tangent normal supplied for UV export"

    for obj in targets:
        if obj.data.users > 1:
            obj.data = obj.data.copy()
        if obj.data.materials:
            obj.data.materials[0] = material
        else:
            obj.data.materials.append(material)
        obj[MARKER] = True
    print(f"BLACK VOLCANIC ROCK: {len(targets)} primary formation meshes, other rocks unchanged")


if __name__ == "__main__":
    apply()
