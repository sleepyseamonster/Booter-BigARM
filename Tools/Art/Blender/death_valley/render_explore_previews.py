"""Render read-only map evidence from a saved Blender viewer; never save a camera."""

from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-dir", type=Path, required=True)
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])


def render(name: str, center: tuple[float, float], scene_origin: tuple[float, float],
           scale: float, output: Path, oblique: bool = False) -> float:
    scene = bpy.context.scene
    camera_data = bpy.data.cameras.new(name)
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = scale
    camera_data.clip_end = 500000
    camera = bpy.data.objects.new(name, camera_data)
    scene.collection.objects.link(camera)
    cx, cy = center[0] - scene_origin[0], center[1] - scene_origin[1]
    target = Vector((cx, cy, 0))
    camera.location = (cx + (scale * 0.28 if oblique else 0),
                       cy - (scale * 0.35 if oblique else 0),
                       scale * 0.55 if oblique else 250000)
    direction = target - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    scene.camera = camera
    scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(camera, do_unlink=True)
    return scale


def main() -> None:
    args = arguments()
    args.output_dir.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    bounds = json.loads(scene["study_bounds_epsg26911"])
    corridor = json.loads(scene["badwater_candidate_bounds_epsg26911"])
    origin = tuple(json.loads(scene["study_origin_epsg26911"]))
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.world.use_nodes = True
    background = scene.world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = (0.4, 0.43, 0.48, 1)
    background.inputs["Strength"].default_value = 0.8
    sun_data = bpy.data.lights.new("TemporaryPreviewSun", type="SUN")
    sun_data.energy = 2
    sun_data.angle = math.radians(10)
    sun = bpy.data.objects.new("TemporaryPreviewSun", sun_data)
    scene.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(28), math.radians(-25), math.radians(-30))
    results = {}
    for name, target, scale, oblique in (
        ("expanded_region", ((bounds[0] + bounds[2]) / 2, (bounds[1] + bounds[3]) / 2), 280000, False),
        ("badwater_corridor", ((corridor[0] + corridor[2]) / 2, (corridor[1] + corridor[3]) / 2), 60000, False),
        ("badwater_oblique", ((corridor[0] + corridor[2]) / 2, (corridor[1] + corridor[3]) / 2), 65000, True),
    ):
        output = args.output_dir / f"{name}.png"
        render(name, target, origin, scale, output, oblique)
        results[name] = str(output)
    manifests = json.loads(scene.get("detail_manifests", "[]"))
    if manifests:
        patch = json.loads(Path(manifests[-1]).read_text())["regions"].get("canyon_patch")
        if patch:
            patch_bounds = patch["bounds_m"]
            target = ((patch_bounds[0] + patch_bounds[2]) / 2,
                      (patch_bounds[1] + patch_bounds[3]) / 2)
            output = args.output_dir / "canyon_patch_oblique.png"
            render("canyon_patch_oblique", target, origin, 4800, output, True)
            results["canyon_patch_oblique"] = str(output)
    bpy.data.objects.remove(sun, do_unlink=True)
    print(json.dumps(results))


if __name__ == "__main__":
    main()
