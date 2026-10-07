"""Check prepared Death Valley grids and exports without launching Unity."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image

from prepare_region import sha256


def verify_region(record: dict) -> dict:
    path = Path(record["prepared_npz"])
    assert sha256(path) == record["prepared_sha256"], path
    for source_name in ("elevation", "imagery"):
        source = record[source_name]
        source_path = Path(source["path"])
        assert sha256(source_path) == source["sha256"], source_path
    with np.load(path) as arrays:
        elevation = arrays["elevation"]
        assert list(elevation.shape) == record["vertex_shape"]
        assert np.isfinite(elevation).all()
        for name in ("slope", "aspect", "ruggedness", "wash", "fan", "cliff", "talus"):
            assert arrays[name].shape == elevation.shape, name
        for name in ("wash", "fan", "cliff", "talus"):
            assert np.isfinite(arrays[name]).all() and np.all((arrays[name] >= 0) & (arrays[name] <= 1)), name
        xmin, ymin, xmax, ymax = record["bounds_m"]
        spacing = record["resolution_m"]
        assert abs((elevation.shape[1] - 1) * spacing - (xmax - xmin)) < 0.01
        assert abs((elevation.shape[0] - 1) * spacing - (ymax - ymin)) < 0.01
    geology_path = path.parent / "geology.npz"
    with np.load(geology_path) as arrays:
        geology = arrays["unit_code"]
    assert geology.shape == elevation.shape
    assert np.issubdtype(geology.dtype, np.integer)
    assert set(np.unique(geology)).issubset(set(range(11)))
    return {"shape": list(elevation.shape), "geology_coverage": float(np.mean(geology != 0))}


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--overview-only", action="store_true")
    parser.add_argument("--overview-color", type=Path)
    parser.add_argument("--terrain-only", action="store_true", help="Verify one regional/detail DEM and its source color")
    parser.add_argument("--color", type=Path)
    parser.add_argument("--detail-manifest", type=Path)
    parser.add_argument("--rock-maps", type=Path)
    parser.add_argument("--rock-exports", type=Path)
    args = parser.parse_args()
    manifest = json.loads(args.manifest.read_text())
    results = {name: verify_region(record) for name, record in manifest["regions"].items()}
    if args.overview_only or args.terrain_only:
        if len(results) != 1 or (args.overview_only and set(results) != {"overview"}):
            parser.error("Terrain-only verification requires a one-region manifest")
        color = args.overview_color if args.overview_only else args.color
        if color is None:
            parser.error("Supply --overview-color or --color for terrain-only verification")
        region = next(iter(manifest["regions"].values()))
        bounds = region["bounds_m"]
        spacing = region["imagery_resolution_m"]
        expected = (round((bounds[2] - bounds[0]) / spacing),
                    round((bounds[3] - bounds[1]) / spacing))
        with Image.open(color) as image:
            assert image.size == expected
        color_record = json.loads(color.with_suffix(".json").read_text())
        expected_hash = color_record.get("sha256") or color_record.get("composite_sha256")
        assert sha256(color) == expected_hash
        print(json.dumps({"regions": results, "color": str(color)}))
        return
    if not (args.detail_manifest and args.rock_maps and args.rock_exports):
        parser.error("Full verification requires --detail-manifest, --rock-maps, and --rock-exports")
    detail = json.loads(args.detail_manifest.read_text())
    assert sha256(Path(detail["prepared_npz"])) == detail["prepared_sha256"]
    with np.load(detail["prepared_npz"]) as arrays:
        assert arrays["elevation"].shape == tuple(detail["vertex_shape"])
        assert np.isfinite(arrays["elevation"]).all()
    maps = json.loads(args.rock_maps.read_text())
    for family, variants in maps["families"].items():
        assert (args.rock_exports / f"{family}.fbx").stat().st_size > 1000
        for variant in variants.values():
            assert sha256(Path(variant["path"])) == variant["sha256"]
    print(json.dumps({"regions": results, "detail_shape": detail["vertex_shape"], "rock_families": len(maps["families"])}))


if __name__ == "__main__":
    main()
