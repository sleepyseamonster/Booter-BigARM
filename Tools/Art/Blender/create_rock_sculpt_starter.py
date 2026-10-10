"""Create a local Blender 5.2 rock-sculpt source with Unity scale references.

Run Blender --background --factory-startup --python this_file -- output.blend proof.json.
Refuses to overwrite artist files. Does not import/export Unity assets or save preferences.
"""
import json
import sys
from pathlib import Path

import bpy
from mathutils import Vector


PLAYER_HEIGHT = 2.48
SCULPT_EDGE = 2.0


def material(name, color):
    result = bpy.data.materials.new(name)
    result.diffuse_color = (*color, 1)
    result.use_nodes = True
    node = result.node_tree.nodes.get("Principled BSDF")
    node.inputs["Base Color"].default_value = (*color, 1)
    node.inputs["Roughness"].default_value = 0.85
    return result


def move_to(obj, collection):
    for previous in list(obj.users_collection):
        previous.objects.unlink(obj)
    collection.objects.link(obj)


def cube(name, edge, x, collection, surface):
    bpy.ops.mesh.primitive_cube_add(size=edge, location=(x, 0, 0))
    obj = bpy.context.object
    obj.name = name
    obj.data.name = name + " Mesh"
    # Bottom-centre pivot at ground; unit transforms remain suitable for later Unity handoff.
    for vertex in obj.data.vertices:
        vertex.co.z += edge / 2
    obj.data.materials.append(surface)
    move_to(obj, collection)
    return obj


def label(text, x, collection):
    bpy.ops.object.text_add(location=(x, -2.05, 0.018))
    obj = bpy.context.object
    obj.name = text
    obj.data.body = text
    obj.data.align_x = "CENTER"
    obj.data.size = 0.30
    obj.data.extrude = 0
    obj.data.materials.append(material(text + " Ink", (0.64, 0.68, 0.72)))
    move_to(obj, collection)
    obj.hide_select = True


def main():
    trailing = sys.argv[sys.argv.index("--") + 1:]
    output, proof_path = (Path(p).resolve() for p in trailing[:2])
    if output.exists():
        raise FileExistsError("Preserve the artist source: " + str(output))
    output.parent.mkdir(parents=True, exist_ok=True)
    proof_path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    scene = bpy.context.scene
    scene.name = "Unity Scale Rock Sculpting"
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.unit_settings.length_unit = "METERS"
    scene["unity_scale"] = "1 Blender metre = 1 Unity unit; verify export/import before production use."
    scene["player_height_source"] = "GreaterWasteland Booter CapsuleCollider height 2.48 m, unit root scale; visual body about 2.478405 m."
    references = bpy.data.collections.new("Scale References - protected")
    sculpture = bpy.data.collections.new("Sculpt Rock - export only this collection")
    presentation = bpy.data.collections.new("Preview Camera and Lights - not for export")
    for collection in (references, sculpture, presentation):
        scene.collection.children.link(collection)
    gray = material("Rock Sculpt Neutral", (0.44, 0.40, 0.35))
    unit = cube("Reference - 1 Unity Unit - 1m Cube", 1.0, -6, references, material("Unit Reference Blue", (0.23, 0.40, 0.58)))
    player = cube("Reference - Booter Height - 2.48m Cube", PLAYER_HEIGHT, -2.5, references, material("Player Reference Ochre", (0.58, 0.43, 0.22)))
    rock = cube("Rock_Sculpt_01", SCULPT_EDGE, 5, sculpture, gray)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.subdivide(number_cuts=63, smoothness=0)
    bpy.ops.object.mode_set(mode="OBJECT")
    # Keep the starting cube genuinely planar/sharp. Sculpting can introduce smooth planes later.
    for face in rock.data.polygons:
        face.use_smooth = False
    rock.data.remesh_voxel_size = 0.04
    rock["role"] = "Working sculpture; reference cubes, ground, labels and lighting are excluded from Unity export."
    rock["source_stage"] = "Unsculpted standard cube with dense closed quad topology."
    bpy.ops.mesh.primitive_plane_add(size=2, location=(0, 0, -0.01))
    ground = bpy.context.object
    ground.name = "Flat Ground Reference - 18m x 12m"
    ground.scale = (9, 6, 1)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ground.data.materials.append(material("Ground Neutral", (0.16, 0.17, 0.18)))
    move_to(ground, references)
    label("1 m / Unity unit", -6, references)
    label("2.48 m / Booter height", -2.5, references)
    label("2 m / sculpt this cube", 5, references)
    for obj in references.objects:
        obj.hide_select = True

    # Non-export presentation utility; hidden from viewport so only the working setup is visible.
    bpy.ops.object.camera_add(location=(12, -22, 15))
    camera = bpy.context.object
    camera.name = "Preview Camera"
    camera.rotation_euler = (Vector((0, 0, 0.7)) - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 21
    scene.camera = camera
    move_to(camera, presentation)
    for name, location, energy, size in (("Preview Key", (1, -5, 10), 1800, 9), ("Preview Fill", (-7, 4, 7), 1100, 7)):
        data = bpy.data.lights.new(name, "AREA")
        data.energy = energy
        data.shape = "DISK"
        data.size = size
        obj = bpy.data.objects.new(name, data)
        presentation.objects.link(obj)
        obj.location = location
        obj.rotation_euler = (Vector((0, 0, 0.5)) - obj.location).to_track_quat("-Z", "Y").to_euler()
    for obj in presentation.objects:
        obj.hide_set(True)
        obj.hide_select = True

    bpy.ops.object.select_all(action="DESELECT")
    rock.select_set(True)
    bpy.context.view_layer.objects.active = rock
    bpy.ops.object.mode_set(mode="SCULPT")
    settings = scene.tool_settings.sculpt
    settings.use_symmetry_x = settings.use_symmetry_y = settings.use_symmetry_z = False
    bpy.ops.brush.asset_activate(asset_library_type="ESSENTIALS", relative_asset_identifier="brushes/essentials_brushes-mesh_sculpt.blend/Brush/Clay Strips")
    brush = settings.brush
    if brush is None:
        raise RuntimeError("Clay Strips did not activate.")
    brush.use_pressure_strength = True
    brush.use_pressure_size = False
    unified = settings.unified_paint_settings
    unified.use_unified_size = True
    unified.size = 70
    unified.use_unified_strength = True
    unified.strength = 0.35

    window = bpy.context.window
    window.workspace = bpy.data.workspaces["Sculpting"]
    eye, target = Vector((8, -18, 12)), Vector((0, 0, 0.9))
    for area in window.screen.areas:
        if area.type == "VIEW_3D":
            space = area.spaces.active
            space.show_region_toolbar = True
            space.show_region_ui = False
            space.show_region_asset_shelf = True
            space.show_region_tool_header = True
            space.shading.type = "SOLID"
            space.shading.light = "STUDIO"
            space.shading.color_type = "MATERIAL"
            space.shading.show_shadows = True
            space.shading.show_cavity = True
            space.overlay.show_floor = True
            space.overlay.show_axis_x = space.overlay.show_axis_y = False
            space.region_3d.view_location = target
            space.region_3d.view_distance = 20
            space.region_3d.view_rotation = (target - eye).to_track_quat("-Z", "Y")
            space.region_3d.view_perspective = "PERSP"
            with bpy.context.temp_override(window=window, area=area):
                bpy.ops.wm.tool_set_by_id(name="builtin.brush")
        elif area.type == "PROPERTIES":
            area.spaces.active.context = "TOOL"

    note = bpy.data.texts.new("START HERE - Unity Rock Sculpting")
    note.write("""Rock_Sculpt_01 is selected in Sculpt Mode. Clay Strips is ready.
1 Blender metre = 1 Unity unit. Blue cube: 1m. Ochre cube: Booter height 2.48m.
The 2m working cube has a bottom-centre pivot, unit scale and dense closed quad geometry.
Sculpt asymmetrically: all mirror axes are off. Shift smooths; Ctrl inverts supported brushes.
Try Clay Strips, Grab, Scrape/Fill and Flatten from the brush shelf for broad rocky planes.
Numpad . frames the selected sculpture; the saved starting view shows all three cubes.
Voxel size starts at 0.04m. Remesh deliberately after major stretching; save a version first.
Ctrl+S saves this local .blend. Reference and preview collections are not export content.
Later: make a separate optimized/UV/baked game mesh; export only the sculpture, with
a ground pivot and verified Blender-Z-up to Unity-Y-up orientation. No Unity asset is exported yet.
This file and its backups remain local-only. Do not replace it by rerunning the creator.
""")
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    bpy.ops.wm.save_as_mainfile(filepath=str(output), check_existing=False)
    report = {
        "file": str(output), "blender": bpy.app.version_string, "workspace": window.workspace.name,
        "active_object": rock.name, "mode": rock.mode, "brush": brush.name,
        "units": "METRIC", "metres_per_blender_unit": scene.unit_settings.scale_length,
        "cubes": [{"name": o.name, "dimensions": list(o.dimensions), "scale": list(o.scale), "location": list(o.location)} for o in (unit, player, rock)],
        "sculpt_vertices": len(rock.data.vertices), "sculpt_faces": len(rock.data.polygons),
        "voxel_metres": rock.data.remesh_voxel_size, "symmetry": [settings.use_symmetry_x, settings.use_symmetry_y, settings.use_symmetry_z],
        "ground_dimensions": list(ground.dimensions), "export_performed": False,
    }
    proof_path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
