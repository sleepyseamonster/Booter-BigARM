"""Build one freely navigable Death Valley terrain viewer.

The regional DEM is required; aligned Badwater detail levels and review guides
are optional. No game camera, rock geometry, or Unity asset is created.
"""

from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Euler

sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_region_study import build_terrain, collection, sample_coarse_grid, stitched_hero


# Approximate central point near 36.6813 N, 117.5627 W; geographic reference only.
RACETRACK_EPSG26911 = (449725.0, 4059666.0)


def arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--overview-color", type=Path, required=True)
    parser.add_argument("--corridor", type=Path, help="Optional georeferenced review outline and markers")
    parser.add_argument("--corridor-manifest", type=Path, help="Prepared 40 m corridor manifest")
    parser.add_argument("--corridor-color", type=Path)
    parser.add_argument("--pilot-manifest", type=Path, help="Prepared 20 m pilot manifest")
    parser.add_argument("--pilot-color", type=Path)
    parser.add_argument("--patch-manifest", type=Path, help="Prepared 10 m canyon patch manifest")
    parser.add_argument("--patch-color", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])


def add_corridor(path: Path, overview: dict, origin: tuple[float, float]) -> tuple[float, float]:
    record = json.loads(path.read_text())
    if record["working_crs"] != "EPSG:26911":
        raise ValueError("Corridor CRS must match the overview")
    x0, y0, x1, y1 = record["bounds_m"]
    bx0, by0, bx1, by1 = overview["bounds_m"]
    if not (bx0 <= x0 < x1 <= bx1 and by0 <= y0 < y1 <= by1):
        raise ValueError("Corridor bounds are outside the overview")
    with np.load(overview["prepared_npz"]) as arrays:
        elevation = arrays["elevation"]

    def height(x: float, y: float) -> float:
        result = sample_coarse_grid(elevation, overview["bounds_m"], overview["resolution_m"],
                                    np.array([x]), np.array([y]))
        return float(result[0])

    review = collection("Badwater_CandidateGuide_NotGameAssets")
    line_points = []
    for ax, ay, bx, by in ((x0, y0, x1, y0), (x1, y0, x1, y1),
                           (x1, y1, x0, y1), (x0, y1, x0, y0)):
        count = max(2, round(max(abs(bx - ax), abs(by - ay)) / 200) + 1)
        for i in range(count - 1):
            t = i / (count - 1)
            x, y = ax * (1 - t) + bx * t, ay * (1 - t) + by * t
            line_points.append((x - origin[0], y - origin[1], height(x, y) + 100))
    line_points.append(line_points[0])
    curve = bpy.data.curves.new("Badwater_CandidateBoundary_Curve", "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = 35
    curve.bevel_resolution = 1
    spline = curve.splines.new("POLY")
    spline.points.add(len(line_points) - 1)
    for point, position in zip(spline.points, line_points):
        point.co = (*position, 1)
    guide = bpy.data.objects.new("Badwater candidate detail boundary", curve)
    review.objects.link(guide)
    material = bpy.data.materials.new("BadwaterBoundary_Yellow")
    material.use_nodes = True
    emission = material.node_tree.nodes.get("Principled BSDF")
    emission.inputs["Base Color"].default_value = (1, 0.82, 0.08, 1)
    emission.inputs["Emission Color"].default_value = (1, 0.65, 0.02, 1)
    emission.inputs["Emission Strength"].default_value = 2
    curve.materials.append(material)
    for marker in record["markers"]:
        x, y = marker["xy_m"]
        item = bpy.data.objects.new(marker["name"], None)
        item.empty_display_type = "SPHERE"
        item.empty_display_size = 250
        item.show_name = True
        item.location = (x - origin[0], y - origin[1], height(x, y) + 350)
        item["source"] = marker["source"]
        review.objects.link(item)
    scene = bpy.context.scene
    scene["badwater_candidate_bounds_epsg26911"] = json.dumps(record["bounds_m"])
    scene["badwater_candidate_status"] = record["status"]
    return (record["markers"][0]["xy_m"][0], record["markers"][0]["xy_m"][1])


def detail_record(manifest_path: Path, key: str, color_path: Path) -> dict:
    if not color_path.exists():
        raise FileNotFoundError(color_path)
    record = dict(json.loads(manifest_path.read_text())["regions"][key])
    record["color_reference_png"] = str(color_path)
    return record


def color_weights(bounds: list[float], spacing: float, shape: tuple[int, int],
                  band_width: float) -> np.ndarray:
    xs = bounds[0] + np.arange(shape[1]) * spacing
    ys = bounds[3] - np.arange(shape[0]) * spacing
    xx, yy = np.meshgrid(xs, ys)
    distance = np.minimum.reduce((xx - bounds[0], bounds[2] - xx,
                                  yy - bounds[1], bounds[3] - yy))
    return np.clip(distance / band_width, 0, 1).astype("float32")


def main() -> None:
    args = arguments()
    manifest = json.loads(args.manifest.read_text())
    overview = dict(manifest["regions"]["overview"])
    overview["color_reference_png"] = str(args.overview_color)
    pairs = ((args.corridor_manifest, args.corridor_color),
             (args.pilot_manifest, args.pilot_color),
             (args.patch_manifest, args.patch_color))
    if any(bool(a) != bool(b) for a, b in pairs):
        raise ValueError("Each detail manifest requires its matching color image")
    if args.pilot_manifest and not args.corridor_manifest:
        raise ValueError("Pilot needs the corridor parent")
    if args.patch_manifest and not args.pilot_manifest:
        raise ValueError("Canyon patch needs the pilot parent")
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
    corridor = (detail_record(args.corridor_manifest, "corridor", args.corridor_color)
                if args.corridor_manifest else None)
    pilot = (detail_record(args.pilot_manifest, "pilot", args.pilot_color)
             if args.pilot_manifest else None)
    patch = (detail_record(args.patch_manifest, "canyon_patch", args.patch_color)
             if args.patch_manifest else None)
    objects, triangles = build_terrain("Overview", overview, origin, terrain, 160,
                                       exclude_bounds=corridor["bounds_m"] if corridor else None)
    empty_overview_objects = [obj for obj in terrain.objects if obj.type == "MESH" and not obj.data.polygons]
    for obj in empty_overview_objects:
        bpy.data.objects.remove(obj, do_unlink=True)
    visible_overview_objects = objects - len(empty_overview_objects)
    shape = overview["vertex_shape"]
    scene["regional_source_triangles"] = 2 * (shape[0] - 1) * (shape[1] - 1)
    detail_counts = {}
    for name, record, parent, excluded, color_band in (
        ("BadwaterCorridor40m", corridor, overview, pilot, 1000),
        ("BadwaterPilot20m", pilot, corridor, patch, 300),
        ("BadwaterCanyonPatch10m", patch, pilot, None, 300),
    ):
        if record is None:
            continue
        stitched, geology, stitched_bounds, _ = stitched_hero(parent, record)
        if list(stitched_bounds) != list(record["bounds_m"]):
            raise ValueError(f"{name} bounds did not align to the parent grid")
        destination = collection(name)
        count, tri = build_terrain(
            name, record, origin, destination, 200,
            elevation_override=stitched, geology_override=geology,
            bounds_override=stitched_bounds,
            exclude_bounds=excluded["bounds_m"] if excluded else None,
            blend_weights=color_weights(stitched_bounds, record["resolution_m"],
                                        stitched.shape, color_band),
            regional_image=parent["color_reference_png"],
            regional_bounds=parent["bounds_m"],
        )
        detail_counts[name] = {"objects": count, "visible_triangles": tri,
                               "resolution_m": record["resolution_m"]}
    marker = bpy.data.objects.new("Racetrack Playa", None)
    marker.empty_display_type = "SPHERE"
    marker.empty_display_size = 250
    marker.show_name = True
    marker.location = (rx - origin[0], ry - origin[1], 1150)
    markers.objects.link(marker)
    view_center = (rx, ry)
    if args.corridor:
        view_center = add_corridor(args.corridor, overview, origin)
    scene["study_bounds_epsg26911"] = json.dumps(bounds)
    scene["study_origin_epsg26911"] = json.dumps(origin)
    scene["overview_resolution_m"] = overview["resolution_m"]
    scene["overview_triangles"] = triangles
    scene["detail_counts"] = json.dumps(detail_counts)
    scene["detail_manifests"] = json.dumps([str(path) for path in
                                             (args.corridor_manifest, args.pilot_manifest,
                                              args.patch_manifest) if path])
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
            space.region_3d.view_location = (view_center[0] - origin[0], view_center[1] - origin[1], 900)
            space.region_3d.view_distance = 22000 if args.corridor else 9500
            space.region_3d.view_rotation = Euler((math.radians(45), 0, math.radians(-25)), "XYZ").to_quaternion()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(args.output))
    print(json.dumps({"regional_grid_tiles": objects, "visible_regional_objects": visible_overview_objects,
                      "visible_regional_triangles": triangles, "detail_counts": detail_counts,
                      "racetrack_local_xy": [rx - origin[0], ry - origin[1]],
                      "viewport_clip_end_m": 500000}))


if __name__ == "__main__":
    main()
