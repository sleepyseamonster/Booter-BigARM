"""Render read-only wide and close inspections of a saved Badwater slice."""

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


def render(name: str, center: tuple[float, float, float], offset: tuple[float, float, float],
           scale: float, output: Path) -> None:
    scene = bpy.context.scene
    data = bpy.data.cameras.new(name)
    data.type = "ORTHO"
    data.ortho_scale = scale
    data.clip_end = 10000
    camera = bpy.data.objects.new(name, data)
    scene.collection.objects.link(camera)
    camera.location = Vector(center) + Vector(offset)
    camera.rotation_euler = (Vector(center) - camera.location).to_track_quat("-Z", "Y").to_euler()
    scene.camera = camera
    scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(camera, do_unlink=True)


def render_perspective(name: str, location: tuple[float, float, float],
                       target: tuple[float, float, float], output: Path) -> None:
    scene = bpy.context.scene
    data = bpy.data.cameras.new(name)
    data.type = "PERSP"
    data.lens = 35
    data.clip_end = 10000
    camera = bpy.data.objects.new(name, data)
    scene.collection.objects.link(camera)
    camera.location = location
    camera.rotation_euler = (Vector(target) - camera.location).to_track_quat("-Z", "Y").to_euler()
    scene.camera = camera
    scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(camera, do_unlink=True)


def main() -> None:
    args = arguments()
    args.output_dir.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "Standard"
    scene.world.use_nodes = True
    scene.world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.18, 0.19, 0.22, 1)
    scene.world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.65
    data = bpy.data.lights.new("TemporarySliceSun", type="SUN")
    data.energy = 1.6
    data.angle = math.radians(8)
    light = bpy.data.objects.new("TemporarySliceSun", data)
    scene.collection.objects.link(light)
    light.rotation_euler = (math.radians(25), math.radians(-20), math.radians(-45))
    out = {}
    for name, center, offset, scale in (
        ("badwater_slice_wide", (0, 0, 180), (1200, -1250, 1700), 2850),
        ("badwater_slice_transition", (-450, -270, 70), (500, -620, 570), 1120),
        ("badwater_slice_ground_detail", (-524, -256, -12), (145, -185, 145), 310),
    ):
        path = args.output_dir / f"{name}.png"
        render(name, center, offset, scale, path)
        out[name] = str(path)
    path = args.output_dir / "badwater_slice_game_view.png"
    render_perspective("badwater_slice_game_view", (-724, -424, -10),
                       (-374, -174, 45), path)
    out["badwater_slice_game_view"] = str(path)
    path = args.output_dir / "badwater_slice_surface_close.png"
    render_perspective("badwater_slice_surface_close", (-724, -424, -32),
                       (-724, -424, -38), path)
    out["badwater_slice_surface_close"] = str(path)
    bpy.data.objects.remove(light, do_unlink=True)
    print(json.dumps(out))


if __name__ == "__main__":
    main()
