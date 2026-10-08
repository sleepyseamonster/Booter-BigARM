"""Create a separate, pen-friendly practice file with Blender 5.2 defaults.

Run with --background --factory-startup --python this_file -- output.blend.
Does not save global preferences or touch production assets.
"""
import json
import sys
from pathlib import Path

import bpy
from mathutils import Quaternion

output = Path(sys.argv[sys.argv.index('--') + 1]).resolve()
if output.exists():
    raise FileExistsError(output)
output.parent.mkdir(parents=True, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=8, radius=1)
obj = bpy.context.object
obj.name = 'Sculpt Sphere'
obj.data.name = 'Sculpt Sphere Mesh'
for face in obj.data.polygons:
    face.use_smooth = True
obj.data.remesh_voxel_size = 0.025
bpy.ops.object.mode_set(mode='SCULPT')
sculpt = bpy.context.scene.tool_settings.sculpt
sculpt.use_symmetry_x = True
sculpt.use_symmetry_y = False
sculpt.use_symmetry_z = False

# Use Blender's shipped brush asset rather than obsolete brush-tool APIs.
bpy.ops.brush.asset_activate(
    asset_library_type='ESSENTIALS',
    relative_asset_identifier='brushes/essentials_brushes-mesh_sculpt.blend/Brush/Draw',
)
brush = sculpt.brush
assert brush is not None, 'Draw brush failed to activate'
brush.use_pressure_strength = True
brush.use_pressure_size = False
brush.strength = 0.35
unified = sculpt.unified_paint_settings
unified.use_unified_size = True
unified.size = 70

window = bpy.context.window
window.workspace = bpy.data.workspaces['Sculpting']
window.workspace.name = 'Sculpt'
area = next(a for a in window.screen.areas if a.type == 'VIEW_3D')
with bpy.context.temp_override(window=window, area=area):
    bpy.ops.wm.tool_set_by_id(name='builtin.brush')
    bpy.ops.screen.screen_full_area(use_hide_panels=False)

for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type != 'VIEW_3D':
            continue
        space = area.spaces.active
        space.show_region_ui = False
        space.show_region_toolbar = False
        space.show_region_asset_shelf = False
        space.show_region_tool_header = True
        space.overlay.show_overlays = False
        space.shading.type = 'SOLID'
        space.shading.light = 'MATCAP'
        space.shading.studiolight_rotate_z = 0
        space.shading.studio_light = 'clay_studio.exr'
        space.shading.show_shadows = True
        space.region_3d.view_location = (0, 0, 0)
        space.region_3d.view_distance = 4.3
        space.region_3d.view_rotation = Quaternion((1, 0, 0), 1.5707963267948966)
        space.region_3d.view_perspective = 'PERSP'

bpy.ops.wm.save_as_mainfile(filepath=str(output))
print(json.dumps({
    'file': str(output), 'blender': bpy.app.version_string,
    'objects': len(bpy.context.scene.objects), 'mode': obj.mode,
    'vertices': len(obj.data.vertices), 'faces': len(obj.data.polygons),
    'brush': brush.name, 'pressure_strength': brush.use_pressure_strength,
    'pressure_size': brush.use_pressure_size,
    'symmetry_x': sculpt.use_symmetry_x,
    'active_screen': window.screen.name,
    'active_areas': [a.type for a in window.screen.areas],
}, indent=2))
