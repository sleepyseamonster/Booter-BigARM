"""Build the single expanded Death Valley Blender viewer from prepared snapshots."""

from __future__ import annotations

import argparse
import shutil
import os
import os
import subprocess
from pathlib import Path


TOOL_DIR = Path(__file__).resolve().parent


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--data-root", type=Path, required=True,
                        help="Directory containing expanded_v2-style prepared data")
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--blender", type=Path,
                        default=Path(os.environ.get("BLENDER_EXECUTABLE") or shutil.which("blender") or "blender"))
    args = parser.parse_args()
    root = args.data_root.expanduser().resolve()
    paths = {
        "--manifest": root / "manifest.json",
        "--overview-color": root / "prepared/overview/landsat_natural_color.png",
        "--corridor": TOOL_DIR / "badwater_corridor.json",
        "--corridor-manifest": root / "corridor_40m/manifest.json",
        "--corridor-color": root / "corridor_40m/prepared/corridor/naip_landsat_composite.png",
        "--pilot-manifest": root / "pilot_20m/manifest.json",
        "--pilot-color": root / "pilot_20m/prepared/pilot/naip_landsat_composite.png",
        "--patch-manifest": root / "canyon_patch_10m/manifest.json",
        "--patch-color": root / "canyon_patch_10m/prepared/canyon_patch/matched_color.png",
    }
    for path in (args.blender, *paths.values()):
        if not path.is_file():
            raise FileNotFoundError(path)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    command = [str(args.blender), "-b", "--python", str(TOOL_DIR / "build_explore_scene.py"), "--"]
    for option, path in paths.items():
        command.extend((option, str(path)))
    command.extend(("--output", str(args.output.expanduser().resolve())))
    subprocess.run(command, check=True)


if __name__ == "__main__":
    main()
