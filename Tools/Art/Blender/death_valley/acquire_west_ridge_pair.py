"""Acquire a versioned, aligned next west/northwest source candidate without touching Unity.

DEM ownership is explicit at e500000 and n4010000; source samples stay immutable.
The candidate must still pass retained readback joins and Blender gates.
"""
import argparse
from datetime import datetime, timezone
import json
from pathlib import Path
import re
import urllib.parse
import urllib.request

import numpy as np
import rasterio
from rasterio.enums import Resampling
from rasterio.transform import from_origin
from rasterio.warp import reproject
from rasterio.windows import from_bounds, transform as window_transform
from pyproj import Transformer
from prepare_region import sha256, fetch_grid

BOUNDS = [499920, 4006200, 504016, 4014392]
SECTIONS = {"west_ridge": [499920,4006200,504016,4010296],
            "northwest_ridge": [499920,4010296,504016,4014392]}

def download(url):
    req = urllib.request.Request(url, headers={"User-Agent": "BooterBigArm-TerrainExpansion/1.0"})
    with urllib.request.urlopen(req, timeout=90) as response:
        return response.read()

def acquire_dem(out):
    if out.exists():
        raise ValueError("DEM acquisition requires a fresh directory; preserve incomplete attempts")
    out.mkdir(parents=True)
    transform = Transformer.from_crs(26911,4326,always_xy=True)
    xy = [transform.transform(x,y) for x in (BOUNDS[0],BOUNDS[2]) for y in (BOUNDS[1],BOUNDS[3])]
    bbox = [min(v[0] for v in xy),min(v[1] for v in xy),max(v[0] for v in xy),max(v[1] for v in xy)]
    url = "https://tnmaccess.nationalmap.gov/api/v1/products?" + urllib.parse.urlencode({
        "bbox": ",".join(map(str,bbox)), "datasets": "Digital Elevation Model (DEM) 1 meter", "outputFormat": "JSON", "max": 100})
    raw = download(url)
    (out/"catalog.json").write_bytes(raw)
    catalog = json.loads(raw)
    chosen = {}
    for item in catalog.get("items",[]):
        match = re.search(r"x(49|50)y(401|402)_CA_FEMAR9Southeast_D24\.tif$",item.get("downloadURL", ""))
        if match:
            key = f"x{match[1]}y{match[2]}"
            if key in chosen: raise ValueError("Ambiguous DEM product: " + key)
            chosen[key] = item
    if set(chosen) != {"x49y401","x49y402","x50y401","x50y402"}:
        raise ValueError("Expected recorded project products covering all four source quadrants")
    grid = np.full((8193,4097), np.nan, dtype="float32")
    target = from_origin(BOUNDS[0]-.5, BOUNDS[3]+.5, 1, 1)
    norths = BOUNDS[3]-np.arange(8193)
    easts = BOUNDS[0]+np.arange(4097)
    records = []
    for key,item in sorted(chosen.items()):
        source_url = item["downloadURL"]
        with rasterio.Env(GDAL_DISABLE_READDIR_ON_OPEN="EMPTY_DIR", CPL_VSIL_CURL_ALLOWED_EXTENSIONS=".tif", GDAL_HTTP_TIMEOUT="60"):
            with rasterio.open(source_url) as src:
                if src.crs.to_epsg()!=26911 or src.res!=(1.,1.) or src.count!=1:
                    raise ValueError("Unexpected raster contract: " + key)
                clip=[max(BOUNDS[0]-4,src.bounds.left+1),max(BOUNDS[1]-4,src.bounds.bottom+1),
                      min(BOUNDS[2]+4,src.bounds.right-1),min(BOUNDS[3]+4,src.bounds.top-1)]
                window=from_bounds(*clip,src.transform).round_offsets().round_lengths()
                pixels=src.read(1,window=window)
                tr=window_transform(window,src.transform)
                nodata=src.nodata
                bounds=list(src.bounds)
        file=out/(key+"_window.tif")
        with rasterio.open(file,"w",driver="GTiff",width=pixels.shape[1],height=pixels.shape[0],count=1,
                           dtype="float32",crs="EPSG:26911",transform=tr,nodata=nodata,compress="deflate") as dst:
            dst.write(pixels.astype("float32"),1)
        metadata_url="https://thor-f5.er.usgs.gov/ngtoc/metadata/waf/elevation/1_meter/geotiff/CA_FEMAR9Southeast_D24/USGS_1M_11_"+key+"_CA_FEMAR9Southeast_D24.xml"
        metadata=download(metadata_url)
        if b"NAVD88" not in metadata or b"bare-earth" not in metadata:
            raise ValueError("Datum/surface metadata unsupported: " + key)
        meta=out/(key+"_metadata.xml");meta.write_bytes(metadata)
        samples=np.full_like(grid,np.nan)
        reproject(pixels,samples,src_transform=tr,src_crs="EPSG:26911",src_nodata=nodata,
                  dst_transform=target,dst_crs="EPSG:26911",dst_nodata=np.nan,resampling=Resampling.bilinear)
        owns_x=easts<500000 if key.startswith("x49") else easts>=500000
        owns_y=norths<=4010000 if key.endswith("401") else norths>4010000
        mask=owns_y[:,None]&owns_x[None,:]
        if not np.isfinite(samples[mask]).all():raise ValueError("Missing native samples for owner "+key)
        overlap=np.isfinite(grid)&np.isfinite(samples)
        overlap_error=float(np.abs(grid[overlap]-samples[overlap]).max()) if overlap.any() else None
        grid[mask]=samples[mask]
        records.append({"id":key,"url":source_url,"source_bounds_m":bounds,"resolution_m":1,
                        "window_file":file.name,"window_sha256":sha256(file),"window_transform":list(tr)[:6],
                        "window_shape":list(pixels.shape),"metadata_url":metadata_url,"metadata_file":meta.name,
                        "metadata_sha256":sha256(meta),"vertical_datum":"NAVD88 metres","surface":"bare-earth",
                        "overlap_max_difference_m":overlap_error})
        print(json.dumps({"acquired":key,"shape":list(pixels.shape),"overlap_max_difference_m":overlap_error}),flush=True)
        del samples,pixels,mask,overlap
    if not np.isfinite(grid).all():raise ValueError("Union grid has NoData")
    if grid.min() < -100 or grid.max() > 1700:raise ValueError("Candidate exceeds retained encoding range")
    sections=[]
    for name,b in SECTIONS.items():
        row=BOUNDS[3]-b[3];col=b[0]-BOUNDS[0]
        native=grid[row:row+4097,col:col+4097]
        file=out/(name+"_terrain.npz")
        np.savez_compressed(file,native_1m=native,regular_2m=native[::2,::2])
        sections.append({"id":name,"bounds_m":b,"terrain_file":file.name,"terrain_sha256":sha256(file),"chunks":256})
    receipt={"schema_version":1,"status":"source_candidate_not_blender_or_unity_verified","acquired_utc":datetime.now(timezone.utc).isoformat(),
             "working_crs":"EPSG:26911","bounds_m":BOUNDS,"unity_origin_m":[522448,4008248],"catalog_request":url,
             "catalog_sha256":sha256(out/"catalog.json"),"source_owner_rule":"x<500000 west; y<=4010000 south",
             "sampling":"bilinear native 1m vertex preparation; strict 2m decimation","sources":records,"sections":sections,
             "nodata_count":0,"min_height_m":float(grid.min()),"max_height_m":float(grid.max()),
             "fits_retained_height_range": bool(grid.min() >= -100 and grid.max() <= 1700),
             "height_encoding":{"type":"uint16 little-endian north-first","minimum_m":-100,"range_m":1800}}
    (out/"manifest.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"dem_complete":str(out),"min_m":receipt["min_height_m"],"max_m":receipt["max_height_m"]}),flush=True)

def acquire_imagery(out):
    if out.exists():raise ValueError("Imagery acquisition requires a fresh directory")
    out.mkdir(parents=True)
    service="https://imagery.nationalmap.gov/arcgis/rest/services/USGSNAIPImagery/ImageServer"
    metadata=download(service+"?f=pjson");(out/"service.json").write_bytes(metadata)
    params={"f":"json","geometry":json.dumps({"xmin":BOUNDS[0],"ymin":BOUNDS[1],"xmax":BOUNDS[2],"ymax":BOUNDS[3],"spatialReference":{"wkid":26911}}),
            "geometryType":"esriGeometryEnvelope","inSR":26911,"spatialRel":"esriSpatialRelIntersects",
            "outFields":"*","returnGeometry":"true","outSR":26911}
    catalog_url=service+"/query?"+urllib.parse.urlencode(params)
    catalog=download(catalog_url);(out/"imagery_catalog.json").write_bytes(catalog)
    if "error" in json.loads(catalog):raise ValueError("Imagery acquisition metadata query failed")
    records=[]
    for name,bounds in SECTIONS.items():
        record=fetch_grid(service,out/(name+"_naip_2m.tif"),tuple(bounds),2,26911,vertices=False,elevation=False)
        with rasterio.open(record["path"]) as data:
            pixels=data.read(masked=True)
            if np.ma.getmaskarray(pixels).any():raise ValueError("Imagery contains masked pixels")
            if data.count not in (3,4):raise ValueError("Expected RGB or RGBN imagery")
        record.update({"section":name,"bounds_m":bounds,"acquired_utc":datetime.now(timezone.utc).isoformat()})
        for key in ("path","service_response"):record[key]=Path(record[key]).name
        records.append(record);print(json.dumps({"imagery_acquired":name}),flush=True)
    (out/"manifest.json").write_text(json.dumps({"schema_version":1,"service_metadata_sha256":sha256(out/"service.json"),
        "imagery_catalog_request":catalog_url,"imagery_catalog_sha256":sha256(out/"imagery_catalog.json"),"sections":records},indent=2)+"\n")

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument("--output",type=Path,required=True);p.add_argument("--phase",choices=["dem","imagery"],required=True)
    a=p.parse_args();(acquire_dem if a.phase=="dem" else acquire_imagery)(a.output.resolve())

if __name__=="__main__":main()
