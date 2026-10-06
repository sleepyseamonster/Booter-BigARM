"""Build a free-navigation Blender viewport of the full Death Valley region.

This scene has only the regional DEM mesh, a geographic color reference, and a
Racetrack Playa navigation marker. It contains no game cameras or close tiles.
"""

from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Euler

sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_region_study import build_terrain, collection


# Approximate central point near 36.6813 N, 117.5627 W; geographic reference only.
RACETRACK_EPSG26911 = (449725.0, 4059666.0)


def arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--overview-color", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])


def main() -> None:
    args = arguments()
    manifest = json.loads(args.manifest.read_text())
    overview = dict(manifest["regions"]["overview"])
    overview["color_reference_png"] = str(args.overview_color)
    bounds = overview["bounds_m"]
    origin = ((bounds[0] + bounds[2]) / 2, (bounds[1] + bounds[3]) / 2)
    rx, ry = RACETRACK_EPSG26911
    if not (bounds[0] < rx < bounds[2] and bounds[1] < ry < bounds[3]):
        raise ValueError("Racetrack Playa lies outside the prepared regional bounds")
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1
    terrain = collection("DeathValley_RegionalTerrain_200m")
    markers = collection("NavigationMarkers_NotGameAssets")
    objects, triangles = build_terrain("Overview", overview, origin, terrain, 160)
    marker = bpy.data.objects.new("Racetrack Playa", None)
    marker.empty_display_type = "SPHERE"
    marker.empty_display_size = 250
    marker.show_name = True
    marker.location = (rx - origin[0], ry - origin[1], 1150)
    markers.objects.link(marker)
    scene["study_bounds_epsg26911"] = json.dumps(bounds)
    scene["study_origin_epsg26911"] = json.dumps(origin)
    scene["overview_resolution_m"] = 200
    scene["overview_triangles"] = triangles
    scene["racetrack_epsg26911"] = json.dumps(RACETRACK_EPSG26911)
    scene["navigation"] = "Free viewport: orbit middle mouse, pan Shift+middle mouse, zoom wheel, Home frames all"
    scene["source_manifest"] = str(args.manifest)
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type != "VIEW_3D":
                continue
            space = area.spaces.active
            space.clip_start = 1
            space.clip_end = 500000
            space.shading.type = "MATERIAL"
            space.overlay.show_floor = False
            space.overlay.show_axis_x = False
            space.overlay.show_axis_y = False
            space.region_3d.view_perspective = "PERSP"
            space.region_3d.view_location = (rx - origin[0], ry - origin[1], 1100)
            space.region_3d.view_distance = 9500
            space.region_3d.view_rotation = Euler((math.radians(45), 0, math.radians(-25)), "XYZ").to_quaternion()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(args.output))
    print(json.dumps({"terrain_objects": objects, "triangles": triangles,
                      "racetrack_local_xy": [rx - origin[0], ry - origin[1]],
                      "viewport_clip_end_m": 500000}))


if __name__ == "__main__":
    main()
