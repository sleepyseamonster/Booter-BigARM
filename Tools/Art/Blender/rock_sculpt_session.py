"""Prepare the new rock starter's interactive Sculpting workspace without saving preferences.

Run explicitly with Blender --python after opening the starter. Never embedded auto-execution.
Workspace switching is deferred because background-created .blend files can retain Layout as active.
"""
import json
import os
from pathlib import Path

import bpy
from mathutils import Vector

bpy.context.preferences.use_preferences_save = False
bpy.context.preferences.inputs.use_mouse_emulate_3_button = True
bpy.context.preferences.inputs.mouse_emulate_3_button_modifier = "ALT"
bpy.context.preferences.inputs.use_rotate_around_active = True
bpy.context.preferences.inputs.tablet_api = "AUTOMATIC"

stage = 0


def prepare():
    global stage
    window = bpy.context.window
    if window is None:
        return 0.25
    if stage == 0:
        window.workspace = bpy.data.workspaces["Sculpting"]
        stage = 1
        return 0.3
    if window.workspace.name != "Sculpting":
        return 0.3
    rock = bpy.data.objects.get("Rock_Sculpt_01")
    if rock is None:
        raise RuntimeError("The rock starter is not the open document.")
    if bpy.context.object is not None and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    for obj in bpy.context.scene.objects:
        obj.select_set(False)
    rock.select_set(True)
    bpy.context.view_layer.objects.active = rock
    bpy.ops.object.mode_set(mode="SCULPT")
    bpy.ops.brush.asset_activate(asset_library_type="ESSENTIALS", relative_asset_identifier="brushes/essentials_brushes-mesh_sculpt.blend/Brush/Clay Strips")
    target, eye = Vector((0, 0, 0.9)), Vector((8, -18, 12))
    for area in window.screen.areas:
        if area.type == "VIEW_3D":
            space = area.spaces.active
            space.show_region_toolbar = True
            space.show_region_asset_shelf = True
            space.show_region_tool_header = True
            space.show_region_ui = False
            space.shading.type = "SOLID"
            space.shading.light = "STUDIO"
            space.shading.color_type = "MATERIAL"
            space.shading.show_cavity = True
            space.overlay.show_floor = True
            space.region_3d.view_location = target
            space.region_3d.view_distance = 20
            space.region_3d.view_rotation = (target - eye).to_track_quat("-Z", "Y")
            space.region_3d.view_perspective = "PERSP"
            with bpy.context.temp_override(window=window, area=area):
                bpy.ops.wm.tool_set_by_id(name="builtin.brush")
        elif area.type == "PROPERTIES":
            area.spaces.active.context = "TOOL"
    saved_ready = os.environ.get("BOOTER_ROCK_SCULPT_SAVE_READY") == "1"
    if saved_ready:
        bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath, check_existing=False)
    proof = os.environ.get("BOOTER_ROCK_SCULPT_SESSION_PROOF")
    if proof:
        Path(proof).write_text(json.dumps({
            "file": bpy.data.filepath, "workspace": window.workspace.name,
            "active_object": rock.name, "mode": rock.mode,
            "brush": bpy.context.scene.tool_settings.sculpt.brush.name,
            "viewport_count": sum(a.type == "VIEW_3D" for a in window.screen.areas),
            "preferences_saved": False, "source_saved_by_session": saved_ready,
        }, indent=2) + "\n", encoding="utf-8")
    return None


bpy.app.timers.register(prepare, first_interval=0.5)
