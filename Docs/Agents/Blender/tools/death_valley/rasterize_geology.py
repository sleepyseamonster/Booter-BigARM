"""Reproject and rasterize the public-domain USGS MF-2381 geologic polygons.

The source shapefile has no .prj. Its accompanying USGS metadata states NAD27 /
UTM zone 11 (EPSG:26711), 1:250,000 scale, and about 65 m horizontal accuracy.
Never assign the study's NAD83 UTM grid CRS to the source shapes directly.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import zipfile
from collections import Counter
from pathlib import Path

import numpy as np
import rasterio
import shapefile
from PIL import Image
from pyproj import Transformer
from rasterio.features import rasterize
from rasterio.transform import from_origin
from rasterio.warp import transform_geom


UNIT_CODES = {
    1: "Quaternary surficial",
    2: "Quaternary igneous",
    3: "Tertiary sedimentary",
    4: "Tertiary volcanic",
    5: "Tertiary intrusive",
    6: "Mesozoic sedimentary",
    7: "Mesozoic igneous",
    8: "Paleozoic sedimentary",
    9: "Paleozoic igneous",
    10: "Precambrian",
}

COLORS = np.array(
    [
        (58, 58, 58),
        (208, 188, 145), (64, 58, 61), (177, 124, 100), (99, 75, 76),
        (91, 84, 78), (145, 128, 111), (67, 75, 82), (182, 162, 132),
        (70, 83, 92), (93, 89, 101),
    ], dtype=np.uint8,
)


def hash_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for block in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--zip", type=Path, required=True, help="USGS MF-2381 no-base-map archive")
    parser.add_argument("--manifest", type=Path, required=True, help="Prepared terrain manifest")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    output = args.output.expanduser().resolve()
    source_dir = output / "raw" / "geology_extracted"
    source_dir.mkdir(parents=True, exist_ok=True)
    members = [
        "dvrfs_shapefiles/dvrfs_geology/geopoly.shp",
        "dvrfs_shapefiles/dvrfs_geology/geopoly.shx",
        "dvrfs_shapefiles/dvrfs_geology/geopoly.dbf",
        "dvrfs_documents/mf-2381-a_metadata.txt",
    ]
    with zipfile.ZipFile(args.zip) as archive:
        archive.extractall(source_dir, members=members)
    shape_path = source_dir / members[0]
    reader = shapefile.Reader(str(shape_path))
    terrain_manifest = json.loads(args.manifest.read_text())
    target_crs = terrain_manifest["working_crs"]
    to_source = Transformer.from_crs(target_crs, "EPSG:26711", always_xy=True)
    records = []
    unit_counts = Counter()
    for shape_record in reader.iterShapeRecords():
        geometry = shape_record.shape.__geo_interface__
        if geometry["type"] not in ("Polygon", "MultiPolygon"):
            continue
        code = int(shape_record.record["UNIT_CODE"])
        if code not in UNIT_CODES:
            continue
        records.append((shape_record.shape.bbox, geometry, code))
        unit_counts[code] += 1
    results = {}
    for name, info in terrain_manifest["regions"].items():
        xmin, ymin, xmax, ymax = info["bounds_m"]
        res = float(info["resolution_m"])
        width = int(info["vertex_shape"][1])
        height = int(info["vertex_shape"][0])
        corners = [to_source.transform(x, y) for x in (xmin - 200, xmax + 200) for y in (ymin - 200, ymax + 200)]
        source_bounds = (min(c[0] for c in corners), min(c[1] for c in corners), max(c[0] for c in corners), max(c[1] for c in corners))
        shapes = []
        for bbox, geometry, code in records:
            if bbox[2] < source_bounds[0] or bbox[0] > source_bounds[2] or bbox[3] < source_bounds[1] or bbox[1] > source_bounds[3]:
                continue
            transformed = transform_geom("EPSG:26711", target_crs, geometry, precision=3)
            shapes.append((transformed, code))
        transform = from_origin(xmin - res / 2, ymax + res / 2, res, res)
        geology = rasterize(shapes, out_shape=(height, width), transform=transform, fill=0, dtype="uint8")
        target = output / "prepared" / name
        target.mkdir(parents=True, exist_ok=True)
        with rasterio.open(
            target / "geology_unit_code.tif", "w", driver="GTiff", height=height, width=width,
            count=1, dtype="uint8", crs=target_crs, transform=transform,
            nodata=0, compress="deflate", tiled=True,
        ) as dataset:
            dataset.write(geology, 1)
        np.savez_compressed(target / "geology.npz", unit_code=geology)
        Image.fromarray(COLORS[geology], mode="RGB").save(target / "geology_preview.png")
        values, counts = np.unique(geology, return_counts=True)
        results[name] = {
            "source_polygons_considered": len(shapes),
            "grid_size": [width, height],
            "codes_in_grid": {str(int(value)): int(count) for value, count in zip(values, counts)},
            "coverage_fraction": float(np.mean(geology > 0)),
            "raster_path": str(target / "geology_unit_code.tif"),
            "preview_path": str(target / "geology_preview.png"),
        }
    output_manifest = {
        "source_archive": str(args.zip),
        "source_sha256": hash_file(args.zip),
        "source_metadata": str(source_dir / members[3]),
        "source_crs_from_metadata": "EPSG:26711 (NAD27 / UTM 11N)",
        "source_scale": "1:250000; about 65 m reported horizontal accuracy",
        "target_crs": target_crs,
        "code_legend": UNIT_CODES,
        "source_polygon_count": len(records),
        "source_code_counts": dict(unit_counts),
        "regions": results,
    }
    (output / "prepared" / "geology_manifest.json").write_text(json.dumps(output_manifest, indent=2) + "\n")
    print(json.dumps(results, indent=2))


if __name__ == "__main__":
    main()
