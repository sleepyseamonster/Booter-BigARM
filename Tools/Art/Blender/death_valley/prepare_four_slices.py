"""Prepare one measured 1 m grid, imagery and chunk exports for four Badwater slices.

Source products are read as bounded remote windows. The original southwest
slice must agree with its previous prepared grid before any new scene is built.
"""

from __future__ import annotations

import argparse
import json
import math
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
import rasterio
from PIL import Image
from rasterio.enums import Resampling
from rasterio.transform import from_origin
from rasterio.warp import reproject
from rasterio.windows import from_bounds, transform as window_transform

from prepare_game_slice import save_raw
from prepare_region import fetch_grid, landform_masks, sha256, terrain_signals


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--config", type=Path, required=True)
    parser.add_argument("--region-config", type=Path, required=True)
    parser.add_argument("--previous-manifest", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    config = json.loads(args.config.read_text())
    region_config = json.loads(args.region_config.read_text())
    old = json.loads(args.previous_manifest.read_text())
    bounds = config["bounds_m"]
    xmin,ymin,xmax,ymax = bounds
    side = xmax-xmin
    if side != ymax-ymin or side != 4096 or old["bounds_m"] != [xmin,ymin,xmin+2048,ymin+2048]:
        raise ValueError("Four-slice extent does not align with the previous southwest slice")
    out = args.output.expanduser().resolve()
    out.mkdir(parents=True, exist_ok=True)
    target_transform = from_origin(xmin-0.5, ymax+0.5, 1, 1)
    grids = {}
    sources = []
    for tile in config["source_tiles"]:
        ident = tile["id"]
        with rasterio.Env(GDAL_DISABLE_READDIR_ON_OPEN="EMPTY_DIR", CPL_VSIL_CURL_ALLOWED_EXTENSIONS=".tif"):
            with rasterio.open(tile["url"]) as source:
                if source.crs.to_epsg() != 26911 or source.res != (1.0,1.0):
                    raise RuntimeError(f"Unexpected native source grid for {ident}")
                x0=max(xmin-3,source.bounds.left+1)
                y0=max(ymin-3,source.bounds.bottom+1)
                x1=min(xmax+3,source.bounds.right-1)
                y1=min(ymax+3,source.bounds.top-1)
                window=from_bounds(x0,y0,x1,y1,source.transform).round_offsets().round_lengths()
                pixels=source.read(1,window=window)
                transform=window_transform(window,source.transform)
                profile={"driver":"GTiff","width":pixels.shape[1],"height":pixels.shape[0],
                         "count":1,"dtype":"float32","crs":"EPSG:26911",
                         "transform":transform,"nodata":source.nodata,"compress":"deflate"}
                source_bounds=list(source.bounds)
                nodata=source.nodata
        clip=out/f"source_{ident}_window.tif"
        with rasterio.open(clip,"w",**profile) as target:
            target.write(pixels.astype("float32"),1)
        metadata_path=out/f"usgs_{ident}_metadata.xml"
        if not metadata_path.exists():
            request=urllib.request.Request(tile["metadata_url"],headers={"User-Agent":"BooterBigARM-DeathValleyStudy/1.0"})
            with urllib.request.urlopen(request,timeout=90) as response:
                metadata_path.write_bytes(response.read())
        metadata=metadata_path.read_text()
        if "NAVD88" not in metadata or "bare-earth" not in metadata:
            raise RuntimeError(f"USGS metadata does not establish expected vertical reference for {ident}")
        grid=np.full((side+1,side+1),np.nan,dtype="float32")
        reproject(pixels,grid,src_transform=transform,src_crs="EPSG:26911",
                  src_nodata=nodata,dst_transform=target_transform,dst_crs="EPSG:26911",
                  dst_nodata=np.nan,resampling=Resampling.bilinear)
        grids[ident]=grid
        sources.append({"id":ident,"url":tile["url"],"source_bounds_m":source_bounds,
                        "window_path":str(clip),"window_sha256":sha256(clip),
                        "window_transform":list(transform)[:6],"window_shape":list(pixels.shape),
                        "metadata_url":tile["metadata_url"],"metadata_path":str(metadata_path),
                        "metadata_sha256":sha256(metadata_path)})
    south,north=grids["x52y401"],grids["x52y402"]
    overlap=np.isfinite(south)&np.isfinite(north)
    if not overlap.any():
        raise RuntimeError("USGS source tiles do not overlap")
    overlap_difference=np.abs(south[overlap]-north[overlap])
    yy=ymax-np.arange(side+1)
    # The outermost source-image row is GDAL's resampling edge, not the chosen
    # join. Check the actual shared interior around the 4,010,000 m boundary.
    join_band=(yy>=4009996)&(yy<=4010004)
    join_difference=np.abs(south[join_band]-north[join_band])
    if not np.isfinite(join_difference).all():
        raise RuntimeError("The chosen source join contains NoData")
    max_difference=float(join_difference.max())
    if max_difference>0.001:
        raise RuntimeError(f"USGS 1 m source join disagrees by {max_difference:.3f} m")
    native=np.where(yy[:,None]<=4010000,south,north)
    if not np.isfinite(native).all():
        raise RuntimeError("Four-slice native grid has missing source elevations")
    with np.load(old["terrain_npz"]) as arrays:
        previous=arrays["native_1m"]
    old_area=native[2048:, :2049]
    old_error=float(np.max(np.abs(old_area-previous)))
    if old_error>0.0001:
        raise RuntimeError(f"Existing southwest terrain changed by {old_error:.6f} m")
    regular=native[::2,::2].copy()
    terrain=out/"terrain.npz"
    np.savez_compressed(terrain,native_1m=native,regular_2m=regular)
    imagery=fetch_grid(region_config["services"]["imagery"],out/"naip_2m.tif",
                       tuple(bounds),2,26911,vertices=False,elevation=False)
    slope,_,ruggedness,_=terrain_signals(regular,2)
    rough=landform_masks(regular,slope,ruggedness,2)
    bedrock=np.clip((slope-18)/24,0,1)
    talus=np.clip(rough["talus"]*0.8+np.clip((slope-8)/20,0,0.5),0,1)
    playa=np.clip((5-slope)/4,0,1)*np.clip((-35-regular)/35,0,1)
    mask_image=np.stack((bedrock[:-1,:-1],talus[:-1,:-1],playa[:-1,:-1]),axis=-1)
    mask_path=out/"surface_masks_rgb.png"
    Image.fromarray(np.rint(mask_image*255).astype("uint8"),"RGB").save(mask_path)
    count=side//config["chunk_size_m"]
    core_rows=set(config["high_detail_rows"])
    core_cols=set(config["high_detail_columns"])
    low=math.floor(float(native.min())/100)*100
    high=math.ceil(float(native.max())/100)*100
    raw_dir=out/"height_raw"
    raw_dir.mkdir(exist_ok=True)
    tiles=[]
    for row in range(count):
        for col in range(count):
            r,c=row*256,col*256
            name=f"r{row:02d}_c{col:02d}"
            coarse=regular[r//2:r//2+129,c//2:c//2+129]
            tile={"id":name,"row_north_to_south":row,"column_west_to_east":col,
                  "bounds_m":[xmin+c,ymax-r-256,xmin+c+256,ymax-r],
                  "lod2m":save_raw(coarse,low,high,raw_dir/f"{name}_2m_129.raw")}
            if row in core_rows and col in core_cols:
                fine=native[r:r+257,c:c+257]
                if not np.array_equal(fine[::2,::2],coarse):
                    raise RuntimeError(f"LOD sample mismatch at {name}")
                tile["lod1m"]=save_raw(fine,low,high,raw_dir/f"{name}_1m_257.raw")
            tiles.append(tile)
    record={"name":config["name"],"generated_utc":datetime.now(timezone.utc).isoformat(),
            "working_crs":"EPSG:26911","bounds_m":bounds,"chunk_size_m":256,
            "grid_shape_1m":list(native.shape),"grid_shape_2m":list(regular.shape),
            "source":{"tiles":sources,
                      "tile_overlap_max_difference_m":max_difference,
                      "outer_overlap_edge_max_difference_m":float(overlap_difference.max()),
                      "previous_southwest_max_difference_m":old_error},
            "naip":imagery,"terrain_npz":str(terrain),"terrain_sha256":sha256(terrain),
            "surface_masks_rgb":str(mask_path),"surface_masks_sha256":sha256(mask_path),
            "height_encoding":{"dtype":"little-endian uint16","row_order":"north to south",
                               "column_order":"west to east","min_m":low,"max_m":high},
            "tiles":tiles,"status":"Measured Blender terrain; Unity import and streaming unverified"}
    (out/"manifest.json").write_text(json.dumps(record,indent=2)+"\n")
    print(json.dumps({"source_overlap_max_m":max_difference,"old_area_max_error_m":old_error,
                      "height_min_m":float(native.min()),"height_max_m":float(native.max()),
                      "tiles":len(tiles),"naip":imagery["path"]}))


if __name__=="__main__":
    main()
