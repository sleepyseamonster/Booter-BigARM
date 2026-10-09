"""Prepare a Blender-first north ridge section against the complete saved Unity baseline."""
import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter
import rasterio
from rasterio.enums import Resampling
from rasterio.transform import from_bounds
from rasterio.warp import reproject

from north_row_contract import contract,color_path,verified_anchor_image,validate_baseline,feather_color,ROOT,sha256,expected_cells
from prepare_expansion import promote, constrain_border
from prepare_region import sha256

C=None;BASE=None;BOUNDS=None;SECTIONS=None;SECTION_BOUNDS=None

def configure(step,batch=None):
    global C,BASE,BOUNDS,SECTIONS,SECTION_BOUNDS
    C=contract(step,batch);BASE=C["data"]/"Baseline01";BOUNDS=C["bounds"];SECTIONS={C["section"]:BOUNDS};SECTION_BOUNDS=C["retained"]


def seal_baseline():
    target = BASE / "baseline.json"
    if target.exists():
        raise ValueError("Baseline already sealed")
    tiles, arrays = [], {}
    for line in (BASE/"tiles.csv").read_text(encoding="utf-8-sig").splitlines():
        values=line.split(",")
        if len(values) != 7:raise ValueError("Unexpected capture identity record")
        ident, east, north, path, guid = values[:5]
        if (sha256(ROOT/path)!=values[5] or sha256(ROOT/(path+".meta"))!=values[6]):
            raise ValueError("Saved terrain bytes changed between isolated capture and sealing")
        e, n = int(east), int(north)
        a = np.fromfile(BASE/(ident+".bytes"), dtype="<f4").reshape(257,257)
        if not np.isfinite(a).all() or a.min()<0 or a.max()>1:
            raise ValueError("Invalid captured height field")
        arrays[ident] = a
        matches = [name for name, b in SECTION_BOUNDS.items() if b[0] <= e < b[2] and b[1] <= n < b[3]]
        if len(matches) != 1 or ident != f"e{e}_n{n}" or (e-499920) % 256 or (n-4006200) % 256:
            raise ValueError("Capture outside accepted twelve-section grid")
        section = matches[0]
        b = SECTION_BOUNDS[section]
        row, col = (b[3]-n)//256-1, (e-b[0])//256
        tiles.append({"id": ident, "section": section, "local_id": f"r{row:02}_c{col:02}",
                      "geographic_key": f"epsg26911/e{e}/n{n}/size256", "bounds_m": [e,n,e+256,n+256],
                      "position": [e-522448,-100,n-4008248], "data_path": path, "data_guid": guid,
                      "data_sha256": sha256(ROOT/path), "meta_sha256": sha256(ROOT/(path+".meta"))})
    if len(tiles)!=C["retained_count"] or len(arrays)!=C["retained_count"]:
        raise ValueError("Incomplete baseline capture")
    edges=0
    for t in tiles:
        e,n=t["bounds_m"][:2];a=arrays[t["id"]]
        for ident, edge, index in [(f"e{e+256}_n{n}",a[:,-1],(slice(None),0)),(f"e{e}_n{n+256}",a[-1,:],(0,slice(None)))]:
            if ident in arrays:
                if not np.array_equal(edge,arrays[ident][index]):
                    raise ValueError("Current baseline seam is not exact")
                edges+=1
    if edges!=C["baseline_edges"]:
        raise ValueError("Baseline footprint is not the accepted grid")
    payload=BASE/"render_heights.npz"
    np.savez_compressed(payload,**arrays)
    protected=[]
    for folder in C["roots"]:
        for file in sorted((ROOT/folder).rglob("*")):
            if file.is_file():
                protected.append({"path":file.relative_to(ROOT).as_posix(),"sha256":sha256(file)})
    for path in ("Packages/manifest.json","Packages/packages-lock.json","ProjectSettings/ProjectVersion.txt","ProjectSettings/EditorBuildSettings.asset"):
        protected.append({"path":path,"sha256":sha256(ROOT/path)})
    scene="Assets/_Project/Scenes/Production/GreaterWasteland.unity"
    (BASE/"production_before.unity").write_bytes((ROOT/scene).read_bytes())
    record={"schema_version":1,"status":"verified_readback_baseline","tiles":tiles,"edges":edges,
            "render_heights_file":payload.name,"render_heights_sha256":sha256(payload),"protected_files":protected,
            "scene":scene,"scene_sha256":sha256(ROOT/scene),"scene_guid":"4de5dd018ee194314a00fd369e2d3eeb"}
    validate_baseline(C,record)
    record["row_step"]=C["step"];record["predecessor_manifest_sha256"]=sha256(ROOT/C["predecessor"])
    target.write_text(json.dumps(record,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"retained":len(tiles),"edges":edges,"protected_files":len(protected)}),flush=True)


def prepare(acquisition, output):
    if output.exists():
        raise ValueError("Fresh preparation output required")
    bm=json.loads((BASE/"baseline.json").read_text());dm=json.loads((acquisition/"DEM/manifest.json").read_text())
    im=json.loads((acquisition/"Imagery/manifest.json").read_text())
    if dm["bounds_m"]!=BOUNDS or not dm["fits_retained_height_range"] or sha256(BASE/bm["render_heights_file"])!=bm["render_heights_sha256"]:
        raise ValueError("Footprint, encoding or baseline changed")
    if dm.get("row_step")!=C["step"] or dm.get("batch")!=C["batch"] or set(s["id"] for s in dm["sections"])!=set(SECTIONS):raise ValueError("Wrong acquisition step")
    if sha256(acquisition/"DEM/catalog.json")!=dm["catalog_sha256"]:raise ValueError("Source catalog changed")
    for file,key in (("service.json","service_metadata_sha256"),("imagery_catalog.json","imagery_catalog_sha256")):
        if sha256(acquisition/"Imagery"/file)!=im[key]:raise ValueError("Imagery provenance changed")
    for source in dm["sources"]:
        for file,h in (("window_file","window_sha256"),("metadata_file","metadata_sha256")):
            if sha256(acquisition/"DEM"/source[file])!=source[h]:
                raise ValueError("Acquired source changed")
    validate_baseline(C,bm)
    if bm["row_step"]!=C["step"] or bm["predecessor_manifest_sha256"]!=sha256(ROOT/C["predecessor"]):raise ValueError("Predecessor manifest changed")
    output.mkdir(parents=True)
    retained=np.load(BASE/bm["render_heights_file"])
    constraints={};old_sections=[]
    for name,b in SECTION_BOUNDS.items():
        tiles=[t for t in bm["tiles"] if t["section"]==name]
        arrays={}
        for t in tiles:
            h=retained[t["id"]];arrays[t["local_id"]]=h[::-1]*np.float32(1800)-np.float32(100)
            if name==C["south"] and t["bounds_m"][3]==4014392:
                e=t["bounds_m"][0]
                for x in range(257):
                    xy=(e+x,4014392);v=h[-1,x]*np.float32(1800)-np.float32(100)
                    if xy in constraints and constraints[xy]!=v:raise ValueError("South/west height corner disagreement")
                    constraints[xy]=v
            if name==C["west"] and t["bounds_m"][2]==BOUNDS[0]:
                n=t["bounds_m"][1]
                for z in range(257):
                    xy=(BOUNDS[0],n+z);v=h[z,-1]*np.float32(1800)-np.float32(100)
                    if xy in constraints and constraints[xy]!=v:raise ValueError("South/west height corner disagreement")
                    constraints[xy]=v
        file=output/("retained_"+name+"_mesh_grids.npz");np.savez_compressed(file,**arrays)
        color=color_path(name)
        dst=output/("retained_"+name+"_color.png");dst.write_bytes(color.read_bytes())
        old_sections.append({"id":name,"bounds_m":b,"mesh_grids_file":file.name,"mesh_grids_sha256":sha256(file),
                             "color_file":dst.name,"color_sha256":sha256(dst),"tiles":tiles})
    sections=[];edges={};max_adjustment=0
    # Match the single north addition against the original macro-color source.
    canvas=np.empty((2048,2048,3),dtype="uint8")
    for name,b in SECTIONS.items():
        rec=next(s for s in im["sections"] if s["section"]==name)
        file=acquisition/"Imagery"/rec["path"]
        if rec["bounds_m"]!=b or sha256(file)!=rec["sha256"]:
            raise ValueError("Imagery hash changed")
        with rasterio.open(file) as src:
            rgb=src.read([1,2,3]).transpose(1,2,0)
        canvas[:]=rgb
    parent=ROOT/"SourceData/Terrain/DeathValley/MacSnapshot2026-10-07/expanded_v2/corridor_40m/prepared/corridor/naip_landsat_composite.png"
    meta=json.loads(parent.with_suffix(".json").read_text());pixels=np.asarray(Image.open(parent).convert("RGB"));sampled=np.zeros_like(canvas)
    for c in range(3):
        reproject(pixels[:,:,c],sampled[:,:,c],src_transform=from_bounds(*meta["bounds_epsg26911"],pixels.shape[1],pixels.shape[0]),src_crs="EPSG:26911",
                  dst_transform=from_bounds(*BOUNDS,2048,2048),dst_crs="EPSG:26911",resampling=Resampling.bilinear)
    matched=canvas.astype("float32")+np.asarray(Image.fromarray(sampled).filter(ImageFilter.GaussianBlur(50)),dtype="float32")-np.asarray(Image.fromarray(canvas).filter(ImageFilter.GaussianBlur(50)),dtype="float32")
    south=verified_anchor_image(C["south"])[0,:,:]
    west_image=verified_anchor_image(C["west"])
    west=west_image[:,-1,:]
    if C["step"]>1:
        prev_south=verified_anchor_image(contract(C["step"]-1)["south"])[0,-1,:]
        if not np.array_equal(west[-1],prev_south):raise ValueError("Predecessor southeast pixel changed")
    result,color_proof=feather_color(matched,south,west,C["step"])
    for name,b in SECTIONS.items():
        source=next(s for s in dm["sections"] if s["id"]==name);file=acquisition/"DEM"/source["terrain_file"]
        if sha256(file)!=source["terrain_sha256"] or source["bounds_m"]!=b:
            raise ValueError("DEM section changed")
        raw=np.load(file);coarse=raw["regular_2m"]
        if coarse.shape!=(2049,2049) or not np.array_equal(raw["native_1m"][::2,::2],coarse):
            raise ValueError("DEM sampling changed")
        arrays={};tiles=[]
        for row in range(16):
            for col in range(16):
                e=b[0]+col*256;n=b[3]-(row+1)*256;ident=f"r{row:02}_c{col:02}"
                h=promote(coarse[row*128:row*128+129,col*128:col*128+129])
                delta,count=constrain_border(h,e,n,constraints);max_adjustment=max(max_adjustment,delta)
                h=promote(h[::2,::2].copy());constrain_border(h,e,n,constraints)
                for r,x in [(r,c) for r in range(257) for c in (0,256)]+[(r,c) for c in range(1,256) for r in (0,256)]:
                    xy=(e+x,n+256-r)
                    if xy in edges and edges[xy]!=h[r,x]:
                        raise ValueError("Prepared new seam disagreement")
                    edges[xy]=h[r,x]
                arrays[ident]=h
                tiles.append({"id":name+"/"+ident,"local_id":ident,"geographic_key":f"epsg26911/e{e}/n{n}/size256",
                              "bounds_m":[e,n,e+256,n+256],"row":row,"col":col,"source_version":source["terrain_sha256"],"retained_boundary_samples":count})
        file=output/(name+"_mesh_grids.npz");np.savez_compressed(file,**arrays)
        color=output/(name+"_geographic_color.png");Image.fromarray(result).save(color)
        sections.append({"id":name,"bounds_m":b,"mesh_grids_file":file.name,"mesh_grids_sha256":sha256(file),"color_file":color.name,"color_sha256":sha256(color),"tiles":tiles})
    budget=float(1.5*1800/65535+4*np.finfo(np.float32).eps*1800)
    if max_adjustment>budget:
        raise ValueError("Border adjustment exceeds encoding precision budget")
    record={"schema_version":1,"working_crs":"EPSG:26911","bounds_m":[499920,4006200,524496,4018488],"unity_origin_m":[522448,4008248],
            "baseline_manifest":(BASE/"baseline.json").relative_to(ROOT).as_posix(),"baseline_manifest_sha256":sha256(BASE/"baseline.json"),
            "source_manifest":(acquisition/"DEM/manifest.json").relative_to(ROOT).as_posix(),"source_manifest_sha256":sha256(acquisition/"DEM/manifest.json"),
            "sections":sections,"retained_sections":old_sections,"status":"prepared_candidate_requires_native_Blender_and_Unity",
            "proof":{"new_chunks":256,"retained_chunks":C["retained_count"],"max_retained_adjustment_m":max_adjustment,"quantization_budget_m":budget,"new_edge_error_m":0,
                     "retained_rgb_edge_error":0,"color_corner":color_proof}}
    record.update(row_step=C["step"],batch=C["batch"],predecessor_scene_sha256=bm["scene_sha256"],predecessor_manifest_sha256=bm["predecessor_manifest_sha256"])
    (output/"manifest.json").write_text(json.dumps(record,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(record["proof"]),flush=True)


def main():
    global BASE
    p=argparse.ArgumentParser(description=__doc__);p.add_argument("--step",type=int,required=True);p.add_argument("--batch");p.add_argument("--seal-baseline",action="store_true");p.add_argument("--acquisition",type=Path);p.add_argument("--output",type=Path);p.add_argument("--baseline",type=Path)
    a=p.parse_args();configure(a.step,a.batch)
    if a.baseline:BASE=a.baseline.resolve()
    if a.seal_baseline:
        seal_baseline()
    else:
        prepare(a.acquisition.resolve() if a.acquisition else C["data"]/"Acquisition01",a.output.resolve() if a.output else C["data"]/"Prepared01")


if __name__=="__main__":main()
