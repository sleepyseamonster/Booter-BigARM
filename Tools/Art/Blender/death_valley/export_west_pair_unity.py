"""Export only reopened Blender geometry; pin old borders to exact Unity readback."""
import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image

from acquire_west_pair import ROOT, SECTIONS
from prepare_region import sha256

ASSET_ROOT="Assets/_Project/Art/Terrain/WestPair2026-10-08"


def verify_proof(path,manifest,chunks,stride):
    proof=json.loads(path.read_text())
    blend=Path(proof["blend_file"]).resolve();export=(path.parent/proof["export_file"]).resolve()
    if (proof["status"]!="native_reopened_mesh_verified" or proof["chunks"]!=chunks or proof["interior_spacing_m"]!=stride
        or proof["manifest_sha256"]!=sha256(manifest) or not blend.is_relative_to(ROOT/"SourceArt/Blender/Studies/DeathValley/WestPair2026-10-08")
        or not export.is_relative_to(path.parent) or sha256(blend)!=proof["blend_sha256"] or sha256(export)!=proof["export_sha256"]
        or proof["max_mesh_edge_difference_m"]!=0 or proof["max_mesh_sample_error_m"]>.00025 or proof["minimum_normal_z"]<=0):
        raise ValueError("Missing, changed or invalid native Blender proof")
    return proof,np.load(export)


def export(manifest,output,current_baseline=None):
    if output.exists():raise ValueError("Fresh Unity export required")
    m=json.loads(manifest.read_text());base=ROOT/m["baseline_manifest"];bm=json.loads(base.read_text())
    if sha256(base)!=m["baseline_manifest_sha256"] or {s["id"] for s in m["sections"]}!=set(SECTIONS):
        raise ValueError("Changed baseline or unsupported section scope")
    combined,_=verify_proof(manifest.parent/"combined_blender_proof.json",manifest,1536,4)
    retained=np.load(base.parent/bm["render_heights_file"])
    if sha256(base.parent/bm["render_heights_file"])!=bm["render_heights_sha256"]:
        raise ValueError("Changed retained readback")
    reconciled=[]
    if current_baseline:
        current=json.loads(current_baseline.read_text())
        file=current_baseline.parent/current["render_heights_file"]
        if sha256(file)!=current["render_heights_sha256"]:raise ValueError("Changed fresh readback")
        latest=np.load(file)
        if set(latest.files)!=set(retained.files) or any(not np.array_equal(latest[k],retained[k]) for k in retained.files):
            raise ValueError("Current retained geometry differs from the Blender reference; build a fresh authoring candidate")
        old_files={x["path"]:x["sha256"] for x in bm["protected_files"]}
        new_files={x["path"]:x["sha256"] for x in current["protected_files"]}
        data_paths={t["data_path"] for t in bm["tiles"]}
        if set(old_files)!=set(new_files):raise ValueError("Protection ownership changed")
        for path in old_files:
            if old_files[path]!=new_files[path]:
                if path not in data_paths:raise ValueError("Non-terrain protected source changed")
                reconciled.append({"path":path,"before_sha256":old_files[path],"current_sha256":new_files[path],"in_memory_capture_matches_later_saved_readback":True,
                                   "limits":"Initial in-memory capture does not prove heights in the earlier serialized asset; no hash variant is accepted"})
        old_tiles={t["id"]:t for t in bm["tiles"]}
        for t in current["tiles"]:
            if any(t[k]!=old_tiles[t["id"]][k] for k in ("data_guid","meta_sha256","position","bounds_m","geographic_key","data_path")):
                raise ValueError("Retained identity or bounds changed")
        bm=current
    constraints={};cells={};old=[];new=[];proofs=[]
    output.mkdir(parents=True)
    fixture_root=ROOT/"SourceData/Terrain/DeathValley/WestPair2026-10-08"
    diagnostic_fixtures=[]
    for name,file in (("before",fixture_root/"Baseline01/north_r12_c05_serialization_before.bin"),
                      ("current",fixture_root/"Baseline02/north_r12_c05_serialization_sculpt.bin")):
        if file.exists():
            target=f"SerializationVariants/north_r12_c05_{name}.bytes"
            (output/target).parent.mkdir(exist_ok=True);(output/target).write_bytes(file.read_bytes())
            diagnostic_fixtures.append({"file":target,"sha256":sha256(file),"purpose":"Distinct original and discarded sculpt states for strict preservation tests; no alternative production hash accepted"})
    for variant in reconciled:
        # These are diagnostic fixtures for distinct saved terrain states.
        # Only the selected baseline payload is accepted by production validation.
        before=base.parent/"north_r12_c05_serialization_before.bin"
        if variant["path"]!="Assets/_Project/Art/Terrain/WestNorthNorthwest2026-10-07/TerrainData/north/r12_c05.asset" or sha256(before)!=variant["before_sha256"]:
            raise ValueError("Missing native serialization variant proof")
        for name,file,key in (("before",before,"before_sha256"),("current",ROOT/variant["path"],"current_sha256")):
            target=f"SerializationVariants/north_r12_c05_{name}.bytes"
            if sha256(file)!=variant[key]:raise ValueError("Serialization variant changed during export")
            (output/target).parent.mkdir(exist_ok=True);(output/target).write_bytes(file.read_bytes())
            variant[name+"_file"]=target
    for t in bm["tiles"]:
        h=retained[t["id"]];e,n=t["bounds_m"][:2];cells[(e,n)]=h[::-1]
        if e==516304:
            for z in range(257):constraints[(e,n+z)]=h[z,0]
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
            if e+256==516304:
                for r in range(257):
                    target=constraints[(e+256,n+256-r)]
                    if abs(float(h[r,256])-float(target*np.float32(1800)-np.float32(100)))>.00025:raise ValueError("Blender border moved")
                    normalized[r,256]=target
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
    if len(new)!=512 or len(old)!=1024 or len(cells)!=1536 or edges!=2992:raise ValueError("Incomplete expansion")
    record={"schema_version":1,"working_crs":"EPSG:26911","bounds_m":[512208,4006200,524496,4014392],"unity_origin_m":[522448,4008248],
            "scene_guid":bm["scene_guid"],"tile_count":1536,"new_tile_count":512,"asset_root":ASSET_ROOT,"minimum_m":-100,"range_m":1800,
            "new_tiles":new,"retained_tiles":old,"protected_files":bm["protected_files"],"prepared_manifest_sha256":sha256(manifest),
            "combined_blend_sha256":combined["blend_sha256"],"blender_proofs":proofs,"normalized_shared_edges":edges,
            "current_baseline_manifest_sha256":sha256(current_baseline) if current_baseline else sha256(base),
            "in_memory_capture_and_saved_payload_reconciliation":reconciled,
            "diagnostic_terrain_fixtures":diagnostic_fixtures,
            "status":"verified_Blender_export_requires_isolated_Unity_validation"}
    (output/"manifest.json").write_text(json.dumps(record,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"new":len(new),"retained":len(old),"edges":edges}),flush=True)


if __name__=="__main__":
    p=argparse.ArgumentParser(description=__doc__);p.add_argument("--manifest",type=Path,required=True);p.add_argument("--output",type=Path,required=True);p.add_argument("--current-baseline",type=Path)
    a=p.parse_args();export(a.manifest.resolve(),a.output.resolve(),a.current_baseline.resolve() if a.current_baseline else None)
