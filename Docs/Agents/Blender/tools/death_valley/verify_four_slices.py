"""Verify four Badwater slices against source windows, old terrain and RAW seams."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image

from prepare_region import sha256
from verify_game_slice import bilinear_source_samples


def main() -> None:
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest",type=Path,required=True)
    parser.add_argument("--previous-manifest",type=Path,required=True)
    parser.add_argument("--color",type=Path,required=True)
    parser.add_argument("--output",type=Path,required=True)
    args=parser.parse_args()
    record=json.loads(args.manifest.read_text())
    previous=json.loads(args.previous_manifest.read_text())
    bounds=record["bounds_m"]
    if bounds!=[520400,4006200,524496,4010296] or len(record["tiles"])!=256:
        raise RuntimeError("Unexpected four-slice geography or chunk count")
    if sha256(Path(record["terrain_npz"]))!=record["terrain_sha256"]:
        raise RuntimeError("Prepared terrain hash changed")
    if sha256(Path(record["surface_masks_rgb"]))!=record["surface_masks_sha256"]:
        raise RuntimeError("Prepared surface mask hash changed")
    if sha256(Path(record["naip"]["path"]))!=record["naip"]["sha256"]:
        raise RuntimeError("NAIP source hash changed")
    for source in record["source"]["tiles"]:
        if sha256(Path(source["window_path"]))!=source["window_sha256"]:
            raise RuntimeError(f"Source window changed: {source['id']}")
        if sha256(Path(source["metadata_path"]))!=source["metadata_sha256"]:
            raise RuntimeError(f"USGS metadata changed: {source['id']}")
    color_record=json.loads(args.color.with_suffix(".json").read_text())
    if sha256(args.color)!=color_record["composite_sha256"] or color_record["bounds_epsg26911"]!=list(map(float,bounds)):
        raise RuntimeError("Geographic color hash or geographic extent changed")
    with Image.open(args.color) as image,Image.open(record["surface_masks_rgb"]) as masks:
        if image.size!=(2048,2048) or masks.size!=(2048,2048):
            raise RuntimeError("Full-area color/mask grid dimensions changed")
    with np.load(record["terrain_npz"]) as arrays:
        native,regular=arrays["native_1m"],arrays["regular_2m"]
    with np.load(previous["terrain_npz"]) as arrays:
        old=arrays["native_1m"]
    if native.shape!=(4097,4097) or regular.shape!=(2049,2049):
        raise RuntimeError("Unexpected elevation grid dimensions")
    if not np.isfinite(native).all() or not np.array_equal(native[::2,::2],regular):
        raise RuntimeError("DEM has missing cells or 2 m values differ from the 1 m source")
    old_error=float(np.max(np.abs(native[2048:,:2049]-old)))
    if old_error>0.0001:
        raise RuntimeError(f"Original southwest terrain changed by {old_error} m")
    rng=np.random.default_rng(20261005)
    sample_error=0.0
    for source in record["source"]["tiles"]:
        if source["id"]=="x52y401":
            rows=rng.integers(310,4087,size=256)
        else:
            rows=rng.integers(10,280,size=128)
        cols=rng.integers(10,4087,size=len(rows))
        xs=bounds[0]+cols
        ys=bounds[3]-rows
        expected=bilinear_source_samples(Path(source["window_path"]),xs,ys)
        error=float(np.max(np.abs(native[rows,cols]-expected)))
        sample_error=max(sample_error,error)
    if sample_error>0.0005:
        raise RuntimeError(f"1 m grid differs from recorded source by {sample_error} m")
    low,high=(record["height_encoding"][key] for key in ("min_m","max_m"))
    quantization_limit=(high-low)/65535/2+0.0001
    raw={}
    max_quantization_error=0.0
    for tile in record["tiles"]:
        row,col=tile["row_north_to_south"],tile["column_west_to_east"]
        path=Path(tile["lod2m"]["path"])
        if sha256(path)!=tile["lod2m"]["sha256"]:
            raise RuntimeError(f"Changed 2 m RAW: {tile['id']}")
        encoded=np.fromfile(path,dtype="<u2").reshape(129,129)
        decoded=low+encoded.astype("float64")/65535*(high-low)
        expected=regular[row*128:row*128+129,col*128:col*128+129]
        max_quantization_error=max(max_quantization_error,float(np.max(np.abs(decoded-expected))))
        raw[row,col]=encoded
        if "lod1m" in tile:
            path=Path(tile["lod1m"]["path"])
            if sha256(path)!=tile["lod1m"]["sha256"]:
                raise RuntimeError(f"Changed 1 m RAW: {tile['id']}")
            fine=np.fromfile(path,dtype="<u2").reshape(257,257)
            if not np.array_equal(fine[::2,::2],encoded):
                raise RuntimeError(f"LOD shared samples disagree: {tile['id']}")
            decoded=low+fine.astype("float64")/65535*(high-low)
            expected=native[row*256:row*256+257,col*256:col*256+257]
            max_quantization_error=max(max_quantization_error,float(np.max(np.abs(decoded-expected))))
    if max_quantization_error>quantization_limit:
        raise RuntimeError(f"RAW quantization error {max_quantization_error} m")
    for row in range(16):
        for col in range(16):
            if row<15 and not np.array_equal(raw[row,col][-1],raw[row+1,col][0]):
                raise RuntimeError(f"North/south RAW seam {row},{col}")
            if col<15 and not np.array_equal(raw[row,col][:,-1],raw[row,col+1][:,0]):
                raise RuntimeError(f"East/west RAW seam {row},{col}")
    result={"source_windows":len(record["source"]["tiles"]),"chunks":256,
            "southwest_source_error_m":old_error,"independent_source_sample_max_error_m":sample_error,
            "source_join_max_difference_m":record["source"]["tile_overlap_max_difference_m"],
            "raw_max_quantization_error_m":max_quantization_error,
            "raw_edges_identical":True,"naip_coverage":color_record["naip_coverage"]}
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(result,indent=2)+"\n")
    print(json.dumps(result))


if __name__=="__main__":
    main()
