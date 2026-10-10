"""Current second-row coverage requires fresh saved native proof and every owned source."""
import json
from pathlib import Path
import re
import sys
import numpy as np
from expansion_coverage import digest, owned_file
from north_row_coverage import source_batches as old_source_batches

def occupied_cells(step):
    if type(step) is not int or not 1 <= step <= 6:
        raise ValueError("Unsupported second-row scope")
    return ({(e,n) for e in range(499920,524496,256) for n in range(4006200,4018488,256)} |
            {(e,n) for e in range(499920,499920+step*4096,256) for n in range(4018488,4022584,256)})

def newest(root, terrain_count):
    step=(terrain_count-4608)//256
    if terrain_count!=4608+256*step or not 1<=step<=6:
        raise ValueError("Unsupported second-row native count")
    found=[]
    for path in (Path(root)/"Assets/_Project/Art/Terrain").glob("NorthRow2E*/Source/manifest.json"):
        m=json.loads(path.read_text(encoding="utf-8"))
        if m.get("family")=="north_row2" and m.get("row_step")==step:
            found.append((path,m))
    if len(found)!=1:
        raise ValueError("Missing or ambiguous accepted row owner")
    return found[0]

def append_verified_north_row2(root, scene, text, retained_tiles, terrain_count):
    root=Path(root).resolve();manifest,m=newest(root,terrain_count);step=m["row_step"]
    expected=occupied_cells(step)
    if m["schema_version"]!=2 or m["bounds_m"]!=[499920,4006200,524496,4022584] or m["unity_origin_m"]!=[522448,4008248] or m["tile_count"]!=terrain_count or m["new_tile_count"]!=256:
        raise ValueError("Invalid latest row manifest")
    proof_path=manifest.parent/"unity_production_validation.json"
    proof=json.loads(proof_path.read_text(encoding="utf-8"))
    checks={"family":"north_row2","row_step":step,"status":"validated_full_grid_and_collider_samples","scene":"Assets/_Project/Scenes/Production/GreaterWasteland.unity","scene_sha256":digest(scene),"manifest_sha256":digest(manifest),"terrains":terrain_count,"retained":terrain_count-256,"edges":9568+512*(step-1),"outer_edges":320,"collider_samples":terrain_count*25}
    if any(proof.get(k)!=v for k,v in checks.items()):
        raise ValueError("Missing or stale full native row production proof")
    for f in m["protected_files"]:
        if digest(owned_file(root,root,f["path"]))!=f["sha256"]:
            raise ValueError("Protected source/native owner changed")
    sys.path.insert(0,str(root/"Tools/Art/Blender/death_valley"))
    from north_row2_native_references import read_native_reference
    cells={}
    for t in m["retained_tiles"]+m["new_tiles"]:
        e,n=t["bounds_m"][:2]
        if (e,n) not in expected or (e,n) in cells or t["bounds_m"]!=[e,n,e+256,n+256] or t["geographic_key"]!=f"epsg26911/e{e}/n{n}/size256":
            raise ValueError("Missing, duplicate, shifted or premature source coverage")
        cells[(e,n)]=read_native_reference(root,t["native_reference"],m["protected_roots"]+[m["asset_root"],m["shared_snapshot_owner"]])[::-1]
    if set(cells)!=expected:
        raise ValueError("Incomplete native coverage")
    edges=0
    for (e,n),a in cells.items():
        for cell,edge,index in (((e+256,n),a[:,-1],(slice(None),0)),((e,n+256),a[0],(-1,slice(None)))):
            if cell in cells:
                if not np.array_equal(edge,cells[cell][index]):
                    raise ValueError("Exact native reference edge differs")
                edges+=1
    if edges!=checks["edges"]:
        raise ValueError("Wrong exact join count")
    batches=list(old_source_batches(4608))
    for k in range(1,step+1):
        path,prior=newest(root,4608+256*k)
        batches.append((prior["asset_root"],{prior["section"]},256))
    tiles=list(retained_tiles);keys={t["geographic_key"] for t in tiles};guids=set();paths=[]
    if len(tiles)!=256 or len(keys)!=256:
        raise ValueError("Incomplete original source footprint")
    for owner,allowed,count in batches:
        base=root/owner/"Source";batch=json.loads((base/"manifest.json").read_text(encoding="utf-8"))
        records=batch["new_tiles"]
        if len(records)!=count or batch["new_tile_count"]!=count:
            raise ValueError("Incomplete historical/source batch")
        section_counts={s:0 for s in allowed}
        for t in records:
            if t["section"] not in allowed or t["geographic_key"] in keys or tuple(t["bounds_m"][:2]) not in cells:
                raise ValueError("Unexpected or duplicate terrain section")
            for key,h in (("render_file","render_sha256"),("height_file","height_sha256"),("color_file","color_sha256")):
                if digest(owned_file(root,base,t[key]))!=t[h]:
                    raise ValueError("Authoritative terrain source changed")
            asset=owned_file(root,root,t["data_path"])
            guid=re.search(r"^guid: ([0-9a-f]{32})$",asset.with_name(asset.name+".meta").read_text(encoding="utf-8"),re.M)
            if not asset.is_file() or not asset.is_relative_to(root/owner/"TerrainData") or not guid or guid[1] in guids or guid[1] not in text:
                raise ValueError("Changed native asset ownership/GUID/reference")
            guids.add(guid[1]);keys.add(t["geographic_key"]);paths.append(asset);section_counts[t["section"]]+=1
            ident=t["local_id"]
            if not re.fullmatch(r"r(?:0[0-9]|1[0-5])_c(?:0[0-9]|1[0-5])",ident):
                raise ValueError("Invalid local tile address")
            r,c=int(ident[1:3]),int(ident[5:7])
            tiles.append({"id":t["section"]+"/"+ident,"legacy_id":ident,"geographic_key":t["geographic_key"],"section":t["section"],"quarter":("N" if r<8 else "S")+("W" if c<8 else "E"),"source_version":t["source_version"],"bounds_m":t["bounds_m"],"source_spacing_m":2,"source_samples":129,"render_samples":257,"height_source":(base/t["height_file"]).relative_to(root).as_posix(),"height_sha256":t["height_sha256"],"color_source":(base/t["color_file"]).relative_to(root).as_posix(),"color_sha256":t["color_sha256"],"integrity":"source_hashes_and_current_Unity_production_readback_verified"})
        if any(v!=256 for v in section_counts.values()):
            raise ValueError("Incomplete section source records")
    if len(tiles)!=terrain_count or len(paths)!=terrain_count-256 or len(keys)!=terrain_count:
        raise ValueError("Incomplete coverage lookup")
    return tiles,paths,{"height_files":terrain_count,"color_files":terrain_count,"shared_edges_checked":edges,"max_encoded_edge_difference":0,"unity_readback_proof_sha256":digest(proof_path),"limits":"Full saved native readback and sampled collision; Player performance/traversal acceptance remains user-owned"}
