"""Export only reopened Blender geometry; pin old borders to exact Unity readback."""
import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image

from acquire_north_ridge import SECTIONS
from prepare_region import sha256

ROOT = Path(__file__).resolve().parents[4]

ASSET_ROOT="Assets/_Project/Art/Terrain/NorthRidge2026-10-09"


def verify_proof(path,manifest,chunks,stride):
    proof=json.loads(path.read_text())
    blend=Path(proof["blend_file"]).resolve();export=(path.parent/proof["export_file"]).resolve()
    if (proof["status"]!="native_reopened_mesh_verified" or proof["chunks"]!=chunks or proof["interior_spacing_m"]!=stride
        or proof["manifest_sha256"]!=sha256(manifest) or not blend.is_relative_to(ROOT/"SourceArt/Blender/Studies/DeathValley/NorthRidge2026-10-09")
        or not export.is_relative_to(path.parent) or sha256(blend)!=proof["blend_sha256"] or sha256(export)!=proof["export_sha256"]
        or proof["max_mesh_edge_difference_m"]!=0 or proof["max_mesh_sample_error_m"]>.00025 or proof["minimum_normal_z"]<=0):
        raise ValueError("Missing, changed or invalid native Blender proof")
    return proof,np.load(export)


def export(manifest,output,current_baseline=None):
    if output.exists():raise ValueError("Fresh Unity export required")
    m=json.loads(manifest.read_text());base=ROOT/m["baseline_manifest"];bm=json.loads(base.read_text())
    if sha256(base)!=m["baseline_manifest_sha256"] or {s["id"] for s in m["sections"]}!=set(SECTIONS):
        raise ValueError("Changed baseline or unsupported section scope")
    combined,_=verify_proof(manifest.parent/"combined_blender_proof.json",manifest,3328,4)
    retained=np.load(base.parent/bm["render_heights_file"])
    if sha256(base.parent/bm["render_heights_file"])!=bm["render_heights_sha256"]:
        raise ValueError("Changed retained readback")
    if len(bm["tiles"]) != 3072 or bm["edges"] != 6016:
        raise ValueError("Unsupported retained baseline")
    if current_baseline and sha256(current_baseline) != sha256(base):
        raise ValueError("Current baseline differs; no alternate baseline accepted")
    for protected in bm["protected_files"]:
        if sha256(ROOT/protected["path"]) != protected["sha256"]:
            raise ValueError("Protected baseline bytes changed before export")
    constraints={};cells={};old=[];new=[];proofs=[]
    output.mkdir(parents=True)
    for t in bm["tiles"]:
        h=retained[t["id"]];e,n=t["bounds_m"][:2];cells[(e,n)]=h[::-1]
        if t["section"] == "northwest_ridge" and n+256 == 4014392:
            for x in range(257):constraints[(e+x,4014392)]=h[-1,x]
        file="Baseline/"+t["id"]+".bytes";(output/file).parent.mkdir(exist_ok=True)
        h[::-1].astype("<f4").tofile(output/file)
        old.append({k:t[k] for k in ("local_id","geographic_key","data_path","data_guid","data_sha256","meta_sha256","position","bounds_m")} |
                   {"render_file":file,"render_sha256":sha256(output/file),"size":[256,1800,256]})
    for section in m["sections"]:
        name=section["id"];proof,arrays=verify_proof(manifest.parent/(name+"_blender_proof.json"),manifest,256,2)
        color=manifest.parent/section["color_file"]
        if sha256(color)!=section["color_sha256"]:raise ValueError("Changed geographic color")
        image=Image.open(color).convert("RGB")
        if image.size!=(2048,2048):raise ValueError("Unexpected image grid")
        proofs.append({"section":name,"blend_sha256":proof["blend_sha256"],"export_sha256":proof["export_sha256"]})
        for t in section["tiles"]:
            ident=t["local_id"];e,n=t["bounds_m"][:2];h=arrays[name+"__"+ident]
            if h.shape!=(257,257) or not np.isfinite(h).all() or h.min()<-100 or h.max()>1700:raise ValueError("Invalid Blender height field")
            normalized=((h+np.float32(100))/np.float32(1800)).astype("<f4")
            if n==4014392:
                for x in range(257):
                    target=constraints[(e+x,n)]
                    if abs(float(h[-1,x])-float(target*np.float32(1800)-np.float32(100)))>.00025:raise ValueError("Blender south border moved")
                    normalized[-1,x]=target
            cells[(e,n)]=normalized
            render=f"Render/{name}/{ident}.bytes";raw=f"Heights/{name}/{ident}.bytes";rgb=f"Colors/{name}/{ident}.png"
            for file in (render,raw,rgb):(output/file).parent.mkdir(parents=True,exist_ok=True)
            normalized.tofile(output/render);np.rint(normalized[::2,::2]*65535).astype("<u2").tofile(output/raw)
            r,c=t["row"],t["col"];image.crop((c*128,r*128,(c+1)*128,(r+1)*128)).save(output/rgb)
            new.append({"section":name,"local_id":ident,"geographic_key":t["geographic_key"],"source_version":t["source_version"],"bounds_m":t["bounds_m"],
                        "position":[e-522448,-100,n-4008248],"size":[256,1800,256],"source_spacing_m":2,"source_samples":129,"render_samples":257,
                        "render_file":render,"render_sha256":sha256(output/render),"height_file":raw,"height_sha256":sha256(output/raw),
                        "color_file":rgb,"color_sha256":sha256(output/rgb),"data_path":f"{ASSET_ROOT}/TerrainData/{name}/{ident}.asset",
                        "layer_path":f"{ASSET_ROOT}/TerrainLayers/{name}/{ident}.terrainlayer"})
    edges=0
    for (e,n),a in cells.items():
        for cell,edge,index in (((e+256,n),a[:,-1],(slice(None),0)),((e,n+256),a[0,:],(-1,slice(None)))):
            if cell in cells:
                if not np.array_equal(edge,cells[cell][index]):raise ValueError("Full normalized export seam changed")
                edges+=1
    if len(new)!=256 or len(old)!=3072 or len(cells)!=3328 or edges!=6512:raise ValueError("Incomplete expansion")
    record={"schema_version":1,"working_crs":"EPSG:26911","bounds_m":[499920,4006200,524496,4018488],"unity_origin_m":[522448,4008248],
            "scene_guid":bm["scene_guid"],"tile_count":3328,"new_tile_count":256,"asset_root":ASSET_ROOT,"minimum_m":-100,"range_m":1800,
            "new_tiles":new,"retained_tiles":old,"protected_files":bm["protected_files"],"prepared_manifest_sha256":sha256(manifest),
            "combined_blend_sha256":combined["blend_sha256"],"blender_proofs":proofs,"normalized_shared_edges":edges,
            "current_baseline_manifest_sha256":sha256(current_baseline) if current_baseline else sha256(base),
            "status":"verified_Blender_export_requires_isolated_Unity_validation"}
    (output/"manifest.json").write_text(json.dumps(record,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"new":len(new),"retained":len(old),"edges":edges}),flush=True)


if __name__=="__main__":
    p=argparse.ArgumentParser(description=__doc__);p.add_argument("--manifest",type=Path,required=True);p.add_argument("--output",type=Path,required=True);p.add_argument("--current-baseline",type=Path)
    a=p.parse_args();export(a.manifest.resolve(),a.output.resolve(),a.current_baseline.resolve() if a.current_baseline else None)
