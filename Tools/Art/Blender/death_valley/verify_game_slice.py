"""Verify source-height fidelity, RAW encoding and every Badwater tile seam."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
import rasterio
from PIL import Image

from prepare_region import sha256


def bilinear_source_samples(path: Path, xs: np.ndarray, ys: np.ndarray) -> np.ndarray:
    with rasterio.open(path) as dataset:
        source = dataset.read(1)
        left, top = dataset.transform.c, dataset.transform.f
        col = (xs - left) - 0.5
        row = (top - ys) - 0.5
        c0 = np.floor(col).astype(int)
        r0 = np.floor(row).astype(int)
        if np.any(c0 < 0) or np.any(r0 < 0) or np.any(c0+1 >= source.shape[1]) or np.any(r0+1 >= source.shape[0]):
            raise RuntimeError("Independent sample fell outside recorded source clip")
        dc, dr = col-c0, row-r0
        return ((1-dr)*((1-dc)*source[r0,c0]+dc*source[r0,c0+1])
                +dr*((1-dc)*source[r0+1,c0]+dc*source[r0+1,c0+1]))


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--color", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    record = json.loads(args.manifest.read_text())
    bounds = record["bounds_m"]
    source_path = Path(record["source"]["source_window_path"])
    if sha256(source_path) != record["source"]["source_window_sha256"]:
        raise RuntimeError("Recorded source clip hash changed")
    if sha256(Path(record["source"]["metadata_path"])) != record["source"]["metadata_sha256"]:
        raise RuntimeError("Recorded USGS product metadata changed")
    if sha256(Path(record["terrain_npz"])) != record["terrain_sha256"]:
        raise RuntimeError("Prepared terrain hash changed")
    if sha256(Path(record["surface_masks_rgb"])) != record["surface_masks_sha256"]:
        raise RuntimeError("Surface mask hash changed")
    if sha256(Path(record["naip"]["path"])) != record["naip"]["sha256"]:
        raise RuntimeError("NAIP source hash changed")
    with np.load(record["terrain_npz"]) as arrays:
        native, regular = arrays["native_1m"], arrays["regular_2m"]
    if native.shape != (2049,2049) or regular.shape != (1025,1025):
        raise RuntimeError("Unexpected terrain array shape")
    if not np.isfinite(native).all() or not np.array_equal(native[::2,::2],regular):
        raise RuntimeError("Native/regular terrain mismatch or NoData")
    with Image.open(args.color) as image, Image.open(record["surface_masks_rgb"]) as masks:
        if image.size != (1024,1024) or masks.size != (1024,1024):
            raise RuntimeError("Surface image dimensions do not match slice grid")
    # This check samples the recorded native GeoTIFF independently from the
    # warp used during preparation. It verifies the 1 m grid was regridded,
    # rather than upsampled from the old 10 m patch.
    rng = np.random.default_rng(20261005)
    cols = rng.integers(8,2041,size=256)
    rows = rng.integers(8,2041,size=256)
    expected = bilinear_source_samples(source_path, bounds[0]+cols, bounds[3]-rows)
    difference = np.abs(native[rows,cols]-expected)
    if float(difference.max()) > 0.0005:
        raise RuntimeError(f"1 m grid differs from source clip by {difference.max()} m")
    enc = record["height_encoding"]
    low, high = enc["min_m"],enc["max_m"]
    max_quantization_error = (high-low)/65535/2+0.0001
    tiles = {}
    for tile in record["tiles"]:
        row,col=tile["row_north_to_south"],tile["column_west_to_east"]
        coarse=np.fromfile(tile["lod2m"]["path"],dtype="<u2").reshape(129,129)
        if sha256(Path(tile["lod2m"]["path"])) != tile["lod2m"]["sha256"]:
            raise RuntimeError(f"Changed RAW: {tile['id']}")
        decoded=low+coarse.astype("float64")/65535*(high-low)
        error=float(np.max(np.abs(decoded-regular[row*128:row*128+129,col*128:col*128+129])))
        if error>max_quantization_error:
            raise RuntimeError(f"2 m RAW height error {tile['id']}: {error}")
        tiles[(row,col)]=coarse
        if "lod1m" in tile:
            fine=np.fromfile(tile["lod1m"]["path"],dtype="<u2").reshape(257,257)
            if sha256(Path(tile["lod1m"]["path"])) != tile["lod1m"]["sha256"]:
                raise RuntimeError(f"Changed fine RAW: {tile['id']}")
            if not np.array_equal(fine[::2,::2],coarse):
                raise RuntimeError(f"Shared LOD samples mismatch: {tile['id']}")
            decoded=low+fine.astype("float64")/65535*(high-low)
            error=float(np.max(np.abs(decoded-native[row*256:row*256+257,col*256:col*256+257])))
            if error>max_quantization_error:
                raise RuntimeError(f"1 m RAW height error {tile['id']}: {error}")
    for row in range(8):
        for col in range(8):
            if row<7 and not np.array_equal(tiles[(row,col)][-1],tiles[(row+1,col)][0]):
                raise RuntimeError(f"North/south RAW seam at {row},{col}")
            if col<7 and not np.array_equal(tiles[(row,col)][:,-1],tiles[(row,col+1)][:,0]):
                raise RuntimeError(f"East/west RAW seam at {row},{col}")
    report={
        "source":record["source"]["url"],
        "source_clip_sha256":record["source"]["source_window_sha256"],
        "native_grid_shape":list(native.shape),
        "no_data_vertices":0,
        "elevation_range_m":[float(native.min()),float(native.max())],
        "independent_bilinear_samples":len(cols),
        "maximum_source_regrid_difference_m":float(difference.max()),
        "tile_count":len(tiles),
        "high_detail_tile_count":sum("lod1m" in tile for tile in record["tiles"]),
        "raw_shared_edges_equal":True,
        "maximum_allowed_raw_quantization_error_m":max_quantization_error,
        "note":"Source DEM is bare-earth geographic data. Visual shader bump and color do not alter this height grid.",
    }
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(report,indent=2)+"\n")
    print(json.dumps(report))


if __name__=="__main__":
    main()
