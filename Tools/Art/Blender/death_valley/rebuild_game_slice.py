"""Rebuild a Badwater game-scale Blender scene from a prepared source snapshot."""

from __future__ import annotations

import argparse
import shutil
import os
import os
import subprocess
from pathlib import Path


TOOL_DIR = Path(__file__).resolve().parent
STUDY_DIR = next(p for p in Path(__file__).resolve().parents if (p / "Packages/manifest.json").is_file() and (p / "ProjectSettings/ProjectVersion.txt").is_file()) / "SourceArt/Blender/Studies/DeathValley"


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--data-root", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--config", type=Path, default=TOOL_DIR / "badwater_game_slice.json")
    parser.add_argument("--blender", type=Path, default=Path(os.environ.get("BLENDER_EXECUTABLE") or shutil.which("blender") or "blender"))
    args = parser.parse_args()
    root = args.data_root.expanduser().resolve()
    inputs = {
        "--manifest": root / "manifest.json",
        "--config": args.config.expanduser().resolve(),
        "--color": root / "geographic_color.png",
        "--scan-manifest": STUDY_DIR / "scan_materials/rocks_ground_09/manifest.json",
    }
    for path in (args.blender, *inputs.values()):
        if not path.is_file():
            raise FileNotFoundError(path)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    command = [str(args.blender), "-b", "--python", str(TOOL_DIR / "build_game_slice_scene.py"), "--"]
    for key,path in inputs.items():
        command.extend((key,str(path)))
    command.extend(("--output", str(args.output.expanduser().resolve())))
    subprocess.run(command,check=True)


if __name__ == "__main__":
    main()
