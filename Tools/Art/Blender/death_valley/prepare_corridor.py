"""Prepare one aligned Badwater terrain batch with the existing GIS pipeline."""

from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path

from prepare_region import run_region


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--config", type=Path, required=True, help="Overview config supplying CRS and services")
    parser.add_argument("--bounds", type=Path, required=True, help="Corridor or subarea JSON with bounds_m")
    parser.add_argument("--name", required=True)
    parser.add_argument("--resolution", type=int, required=True)
    parser.add_argument("--imagery-resolution", type=int, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    config = json.loads(args.config.read_text())
    bounds_record = json.loads(args.bounds.read_text())
    bounds = bounds_record["bounds_m"]
    if bounds_record["working_crs"] != config["working_crs"]:
        raise ValueError("Detail CRS does not match overview")
    if args.resolution % args.imagery_resolution:
        raise ValueError("Terrain resolution must be an integer multiple of imagery resolution")
    for coordinate in bounds:
        if coordinate % args.resolution:
            raise ValueError("Bounds must align to the terrain resolution")
    region = {
        "bounds_m": bounds,
        "resolution_m": args.resolution,
        "imagery_resolution_m": args.imagery_resolution,
    }
    output = args.output.expanduser().resolve()
    output.mkdir(parents=True, exist_ok=True)
    info = run_region(args.name, region, config, output)
    manifest = {
        "name": f"death-valley-{args.name}-{args.resolution}m",
        "generated_utc": datetime.now(timezone.utc).isoformat(),
        "working_crs": config["working_crs"],
        "vertical_units": config["vertical_units"],
        "vertical_datum_note": config["vertical_datum"],
        "source_references": config["source_references"],
        "regions": {args.name: info},
    }
    (output / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    print(json.dumps({"name": args.name, "bounds_m": bounds,
                      "vertex_shape": info["vertex_shape"],
                      "imagery_valid_fraction": info["imagery_valid_fraction"]}))


if __name__ == "__main__":
    main()
