"""Publish six-section coverage only against fresh full production readback."""
import json
from pathlib import Path
import re

import numpy as np
from expansion_coverage import digest, owned_file

ASSET_ROOT="Assets/_Project/Art/Terrain/WestPair2026-10-08"
PRIOR_ROOT="Assets/_Project/Art/Terrain/WestNorthNorthwest2026-10-07"


def append_verified_west_pair(root,scene,text,retained_tiles):
    root=Path(root).resolve();source=root/ASSET_ROOT/"Source";manifest=source/"manifest.json"
    m=json.loads(manifest.read_text());proof=json.loads((source/"unity_production_validation.json").read_text())
    if (m["schema_version"]!=1 or m["bounds_m"]!=[512208,4006200,524496,4014392] or m["unity_origin_m"]!=[522448,4008248] or
        m["new_tile_count"]!=512 or proof["status"]!="validated_full_grid_and_collider_samples" or
        proof["scene"]!="Assets/_Project/Scenes/Production/GreaterWasteland.unity" or proof["scene_sha256"]!=digest(scene) or
        proof["manifest_sha256"]!=digest(manifest) or proof["terrains"]!=1536 or proof["retained"]!=1024 or
        proof["edges"]!=2992 or proof["outer_edges"]!=160 or proof["collider_samples"]!=38400):
        raise ValueError("Missing or stale six-section production proof")
    for file in m["protected_files"]:
        actual=digest(owned_file(root,root,file["path"]))
        if actual!=file["sha256"]:
            raise ValueError("Retained source changed after validation")
    cells={};tiles=list(retained_tiles);paths=[];guids=set();keys={t["geographic_key"] for t in tiles}
    for t in m["new_tiles"]+m["retained_tiles"]:
        render=owned_file(root,source,t["render_file"])
        if digest(render)!=t["render_sha256"]:raise ValueError("Readback source changed")
        a=np.fromfile(render,dtype="<f4").reshape(257,257);e,n=int(t["position"][0]+522448),int(t["position"][2]+4008248)
        if not np.isfinite(a).all() or a.min()<0 or a.max()>1 or (e,n) in cells or t["geographic_key"]!=f"epsg26911/e{e}/n{n}/size256":
            raise ValueError("Invalid or duplicate geographic source")
        cells[(e,n)]=a
    prior_source=root/PRIOR_ROOT/"Source";prior=json.loads((prior_source/"manifest.json").read_text())
    for base,records,allowed,owner in ((prior_source,prior["new_tiles"],{"west","north","northwest"},PRIOR_ROOT),
                                       (source,m["new_tiles"],{"west_outer","northwest_outer"},ASSET_ROOT)):
        for t in records:
            if t["section"] not in allowed or t["geographic_key"] in keys:raise ValueError("Duplicate or unexpected section")
            keys.add(t["geographic_key"])
            for file,h in (("height_file","height_sha256"),("color_file","color_sha256"),("render_file","render_sha256")):
                if digest(owned_file(root,base,t[file]))!=t[h]:raise ValueError("Section source changed")
            asset=owned_file(root,root,t["data_path"])
            if not asset.is_relative_to(root/owner/"TerrainData"):raise ValueError("Terrain ownership escaped")
            guid=re.search(r"^guid: ([0-9a-f]{32})$",asset.with_name(asset.name+".meta").read_text(),re.M)
            if not guid or guid[1] in guids or guid[1] not in text:raise ValueError("Missing or duplicated TerrainData reference")
            guids.add(guid[1]);paths.append(asset)
            r,c=int(t["local_id"][1:3]),int(t["local_id"][5:7])
            tiles.append({"id":t["section"]+"/"+t["local_id"],"legacy_id":t["local_id"],"geographic_key":t["geographic_key"],
                          "section":t["section"],"quarter":("N" if r<8 else "S")+("W" if c<8 else "E"),"source_version":t["source_version"],
                          "bounds_m":t["bounds_m"],"source_spacing_m":2,"source_samples":129,"render_samples":257,
                          "height_source":(base/t["height_file"]).relative_to(root).as_posix(),"height_sha256":t["height_sha256"],
                          "color_source":(base/t["color_file"]).relative_to(root).as_posix(),"color_sha256":t["color_sha256"],
                          "integrity":"source_hashes_and_current_Unity_production_readback_verified"})
    edges=0
    for (e,n),a in cells.items():
        for cell,edge,index in (((e+256,n),a[:,-1],(slice(None),0)),((e,n+256),a[0,:],(-1,slice(None)))):
            if cell in cells:
                if not np.array_equal(edge,cells[cell][index]):raise ValueError("Normalized seam changed")
                edges+=1
    if len(tiles)!=1536 or len(paths)!=1280 or len(cells)!=1536 or edges!=2992:raise ValueError("Incomplete six-section coverage")
    return tiles,paths,{"height_files":1536,"color_files":1536,"shared_edges_checked":2992,"max_encoded_edge_difference":0,
                        "unity_readback_proof_sha256":digest(source/"unity_production_validation.json"),
                        "limits":"Current full native readback and sampled terrain collision; Player performance and actor traversal remain user-owned"}
