"""Use seamless dirt, sparse shale, and dense shale tiles in the open study.

The three surfaces occupy separate macro areas. Only the short borders between
areas crossfade. Each image is sampled once with ordinary repeat mapping; no
mirroring, coordinate warp, rotated duplicate, or per-image overlap is used.
"""

from pathlib import Path

import bpy


STUDY = "BrokenWorldBadlandsStudy.blend"
MATERIAL = "StudyGround_DistinctTileableShaleAndDirt"
TILES = Path(__file__).resolve().parent / "textures"


def node(nodes, kind, name, x, y):
    n = nodes.new(kind)
    n.name = n.label = name
    n.location = (x, y)
    return n


def mask_ramp(nodes, links, source, name, low, high, x, y):
    n = node(nodes, "ShaderNodeValToRGB", name, x, y)
    n.color_ramp.elements[0].position = low
    n.color_ramp.elements[1].position = high
    links.new(source, n.inputs["Factor"])
    return n.outputs["Color"]


def tile(nodes, links, vector, filename, x, y):
    path = TILES / filename
    if not path.is_file():
        raise FileNotFoundError(path)
    texture = node(nodes, "ShaderNodeTexImage", filename, x, y)
    image = bpy.data.images.load(str(path), check_existing=True)
    image.pack()
    texture.image = image
    texture.extension = "REPEAT"
    texture.interpolation = "Linear"
    links.new(vector, texture.inputs["Vector"])
    return texture.outputs["Color"]


def main():
    if Path(bpy.data.filepath).name != STUDY:
        raise RuntimeError(f"Open {STUDY} first")
    terrain = bpy.context.scene.objects.get("Badlands terrain - study only")
    if terrain is None or terrain.data.attributes.get("ReferenceGroundApron") is None:
        raise RuntimeError("Expected the existing terrain and formation apron mask")
    if terrain.active_material and terrain.active_material.name == MATERIAL:
        print("Distinct tileable ground is already active")
        return
    if terrain.active_material is None:
        raise RuntimeError("Expected an existing ground material for comparison")
    terrain.active_material.use_fake_user = True

    material = bpy.data.materials.new(MATERIAL)
    material.use_nodes = True
    material.node_tree.nodes.clear()
    nodes, links = material.node_tree.nodes, material.node_tree.links
    output = node(nodes, "ShaderNodeOutputMaterial", "Material Output", 1340, 100)
    surface = node(nodes, "ShaderNodeBsdfPrincipled", "Dry badlands ground", 1080, 100)
    surface.inputs["Roughness"].default_value = 0.91
    links.new(surface.outputs["BSDF"], output.inputs["Surface"])

    geometry = node(nodes, "ShaderNodeNewGeometry", "World-position mapping", -1500, 380)
    position = geometry.outputs["Position"]
    scale = node(nodes, "ShaderNodeVectorMath", "Five-meter texture tiles", -1250, 380)
    scale.operation = "SCALE"
    scale.inputs["Scale"].default_value = 0.20
    links.new(position, scale.inputs["Vector"])
    dirt = tile(nodes, links, scale.outputs["Vector"], "BadlandsRustDirtTile.png", -980, 520)
    sparse = tile(nodes, links, scale.outputs["Vector"], "BadlandsSparseShaleTile.png", -980, 240)
    dense = tile(nodes, links, scale.outputs["Vector"], "BadlandsDenseShaleTile.png", -980, -40)

    macro = node(nodes, "ShaderNodeTexNoise", "Broad shale exposure", -1260, -440)
    macro.inputs["Scale"].default_value = 0.042
    macro.inputs["Detail"].default_value = 3.0
    macro.inputs["Roughness"].default_value = 0.55
    links.new(position, macro.inputs["Vector"])
    apron = node(nodes, "ShaderNodeAttribute", "Shale near formation feet", -1260, -750)
    apron.attribute_name = "ReferenceGroundApron"
    apron_boost = node(nodes, "ShaderNodeMath", "Formation shale boost", -980, -670)
    apron_boost.operation = "MULTIPLY"
    apron_boost.inputs[1].default_value = 0.22
    links.new(apron.outputs["Fac"], apron_boost.inputs[0])
    coverage = node(nodes, "ShaderNodeMath", "Distinct surface coverage", -740, -440)
    coverage.operation = "ADD"
    links.new(macro.outputs["Fac"], coverage.inputs[0])
    links.new(apron_boost.outputs[0], coverage.inputs[1])

    sparse_edge = mask_ramp(nodes, links, coverage.outputs[0],
                            "Dirt to sparse shale edge", 0.47, 0.50, -480, -430)
    dense_edge = mask_ramp(nodes, links, coverage.outputs[0],
                           "Sparse to dense shale edge", 0.53, 0.56, -480, -680)
    first = node(nodes, "ShaderNodeMixRGB", "Dirt plus sparse shale", 20, 260)
    first.blend_type = "MIX"
    links.new(sparse_edge, first.inputs[0])
    links.new(dirt, first.inputs[1])
    links.new(sparse, first.inputs[2])
    second = node(nodes, "ShaderNodeMixRGB", "Sparse shale plus dense shale", 350, 260)
    second.blend_type = "MIX"
    links.new(dense_edge, second.inputs[0])
    links.new(first.outputs["Color"], second.inputs[1])
    links.new(dense, second.inputs[2])
    links.new(second.outputs["Color"], surface.inputs["Base Color"])

    grain = node(nodes, "ShaderNodeTexNoise", "Continuous fine ground grain", 350, -350)
    grain.inputs["Scale"].default_value = 3.6
    grain.inputs["Detail"].default_value = 3.0
    links.new(position, grain.inputs["Vector"])
    bump = node(nodes, "ShaderNodeBump", "Fine dirt and chip relief", 710, -300)
    bump.inputs["Strength"].default_value = 0.18
    bump.inputs["Distance"].default_value = 0.045
    links.new(grain.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], surface.inputs["Normal"])

    material["ground_texture_pass"] = "single-sample seamless dirt/sparse-shale/dense-shale tiles"
    terrain.active_material = material
    print(f"Distinct tileable shale and red dirt active in the open {STUDY}; save this file")


if __name__ == "__main__":
    main()
