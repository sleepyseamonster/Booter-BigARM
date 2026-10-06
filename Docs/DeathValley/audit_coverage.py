"""Inventory retained Death Valley terrain; never edits source or Unity assets.

Run with the pinned repository Python. --refresh-boundary downloads only the
official NPS polygon. Outputs are confined to this tool's directory. File paths
are observations; projected bounds and source identities survive file moves.
"""
from __future__ import annotations

import argparse
import csv
import hashlib
import html
import json
import re
import subprocess
import struct
import urllib.parse
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / "ProjectSettings/ProjectVersion.txt").exists())
NPS = "https://services1.arcgis.com/fBc8EJBxQRMcHlei/ArcGIS/rest/services/National_Park_Service_Boundaries/FeatureServer/0/query"
TERRAIN_SCENE_GUID = "4de5dd018ee194314a00fd369e2d3eeb"


def digest(path):
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def relative(path):
    return path.relative_to(ROOT).as_posix()


def find(name, area="Docs"):
    preferred = ROOT / area
    candidates = sorted(preferred.rglob(name))
    candidates = [p for p in candidates if "archives" not in p.parts]
    if len(candidates) != 1:
        raise ValueError(f"Expected one canonical {name} under {area}; found {len(candidates)}. Resolve moves before refresh.")
    return candidates[0]


def read_config(name):
    path = find(name)
    return json.loads(path.read_text()), relative(path)


def terrain_scene():
    matches=[]
    for meta in (ROOT / "Assets").rglob("*.unity.meta"):
        if re.search(r"^guid: " + TERRAIN_SCENE_GUID + r"$", meta.read_text(), re.M):
            asset=meta.with_suffix("")
            if asset.exists(): matches.append(asset)
    if len(matches)!=1:
        raise ValueError("Expected one terrain scene with the preserved GUID. Finish concurrent moves before refreshing.")
    return matches[0]


def write_json(path, value):
    path.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8", newline="\n")


def boundary(refresh):
    path = HERE / "nps_deva_boundary.json"
    if refresh:
        query = urllib.parse.urlencode({"where": "UNIT_CODE='DEVA'", "outFields": "UNIT_CODE,UNIT_NAME,DATE_EDIT", "returnGeometry": "true", "outSR": 26911, "f": "json"})
        url = NPS + "?" + query
        request = urllib.request.Request(url, headers={"User-Agent": "BooterBigARM-DeathValleyInventory/1.0"})
        with urllib.request.urlopen(request, timeout=45) as response:
            raw = response.read()
        data = json.loads(raw)
        if data.get("error") or not data.get("features") or data.get("exceededTransferLimit"):
            raise ValueError(f"Incomplete NPS response: {data.get('error', data.keys())}")
        sr = data.get("spatialReference", {})
        if sr.get("latestWkid", sr.get("wkid")) != 26911:
            raise ValueError(f"Unexpected NPS coordinate reference: {sr}")
        if any(f["attributes"]["UNIT_CODE"] != "DEVA" for f in data["features"]):
            raise ValueError("NPS returned an unexpected park")
        write_json(path, {"request_url": url, "retrieved_utc": datetime.now(timezone.utc).isoformat(), "response_sha256": hashlib.sha256(raw).hexdigest(), "response": data})
    return json.loads(path.read_text()) if path.exists() else None


def asset_record(path):
    with path.open("rb") as stream:
        signature = stream.read(80)
    return {"path": relative(path), "bytes": path.stat().st_size, "sha256": digest(path),
            "storage": "lfs_pointer" if signature.startswith(b"version https://git-lfs.github.com/spec/v1") else "materialized_file",
            "readability": "not_reopened_by_this_audit"}


def make_atlas(catalog, park):
    regions = catalog["regions"]
    frame = regions[1]["bounds_m"]
    def panel(view, width, height, layers, tiles=False):
        xmin, ymin, xmax, ymax = view
        margin = 24
        scale = min((width-2*margin)/(xmax-xmin), (height-2*margin)/(ymax-ymin))
        def xy(x, y):
            return margin+(x-xmin)*scale, height-margin-(y-ymin)*scale
        def rect(bounds, color, label, ident, dashed=False):
            x0,y0,x1,y1=bounds
            x,y=xy(x0,y1)
            return f'<rect tabindex="0" role="button" aria-label="{html.escape(label)}" data-id="{html.escape(ident)}" x="{x:.3f}" y="{y:.3f}" width="{(x1-x0)*scale:.3f}" height="{(y1-y0)*scale:.3f}" fill="{color}" fill-opacity=".14" stroke="{color}" stroke-width="1.2" '+('stroke-dasharray="5 3" ' if dashed else '')+f'><title>{html.escape(label)}</title></rect>'
        parts=[f'<svg viewBox="0 0 {width} {height}" role="img" aria-label="Projected terrain footprints, north up"><rect width="100%" height="100%" fill="#f5f2ea"/>']
        if park:
            paths=[]
            for feature in park["response"]["features"]:
                for ring in feature["geometry"]["rings"]:
                    paths.append("M"+" L".join(f"{xy(x,y)[0]:.2f},{xy(x,y)[1]:.2f}" for x,y in ring)+" Z")
            parts.append(f'<path d="{" ".join(paths)}" fill="#d8dccd" fill-rule="evenodd" stroke="#70795e" stroke-width="1"><title>Official NPS Death Valley park boundary</title></path>')
        for region in layers:
            parts.append(rect(region["bounds_m"],region["color"],region["label"],region["id"]))
        if not tiles:
            for tile in catalog["regional_source_tiles"]:
                x0,y0,x1,y1=tile["bounds_m"];x,y=xy(x0,y1)
                parts.append(f'<rect x="{x:.3f}" y="{y:.3f}" width="{(x1-x0)*scale:.3f}" height="{(y1-y0)*scale:.3f}" fill="none" stroke="#77765e" stroke-opacity=".45" stroke-width=".7" pointer-events="none"/>')
        if tiles:
            for tile in catalog["unity_tiles"]:
                parts.append(rect(tile["bounds_m"],"#c26b19" if tile["source_spacing_m"]==1 else "#266aa0",tile["legacy_id"],tile["legacy_id"]))
            x0,y0,x1,y1=catalog["regions"][6]["bounds_m"]
            cx,cy=xy((x0+x1)/2,(y0+y1)/2)
            left,top=xy(x0,y1);right,bottom=xy(x1,y0)
            parts.append(f'<path d="M{cx:.2f},{top:.2f} V{bottom:.2f} M{left:.2f},{cy:.2f} H{right:.2f}" stroke="#243b50" stroke-width="2" pointer-events="none"/>')
            for label,x,y in (("NW",x0+1024,y1-1024),("NE",x1-1024,y1-1024),("SW",x0+1024,y0+1024),("SE",x1-1024,y0+1024)):
                px,py=xy(x,y)
                parts.append(f'<text x="{px:.2f}" y="{py:.2f}" text-anchor="middle" font-size="28" fill="#243b50" pointer-events="none">{label}</text>')
        for candidate in catalog["expansion_candidates"]:
            if tiles:
                parts.append(rect(candidate["bounds_m"],"#95614f",candidate["label"]+" — proposed, not built",candidate["id"],True))
                b=candidate["bounds_m"];px,py=xy((b[0]+b[2])/2,(b[1]+b[3])/2)
                short_label=candidate["label"].split()[0]
                parts.append(f'<text x="{px:.2f}" y="{py:.2f}" text-anchor="middle" font-size="28" fill="#634335" pointer-events="none">{html.escape(short_label)}</text>')
        parts.append(f'<text x="{width-32}" y="20" font-size="12">N ↑</text>')
        bar_m=20000 if not tiles else 1000
        parts.append(f'<path d="M24 {height-12} h{bar_m*scale:.2f}" stroke="#333" stroke-width="2"/><text x="24" y="{height-19}" font-size="11">{bar_m//1000} km · EPSG:26911</text></svg>')
        return "".join(parts)
    overview=panel(frame,650,740,regions)
    detail=panel([517840,4005688,527056,4012856],900,720,regions[3:7],True)
    (HERE/"coverage_overview.svg").write_text(overview,encoding="utf-8",newline="\n")
    (HERE/"coverage_badwater.svg").write_text(detail,encoding="utf-8",newline="\n")
    cards=[]
    for region in regions:
        w=(region["bounds_m"][2]-region["bounds_m"][0])/1000
        h=(region["bounds_m"][3]-region["bounds_m"][1])/1000
        cards.append(f'<li><button data-id="{region["id"]}">{html.escape(region["label"])}</button> · {w:g} × {h:g} km · {region["spacing_m"]} m</li>')
    lookup={r["id"]:r for r in regions}
    lookup.update({t["legacy_id"]:t for t in catalog["unity_tiles"]})
    lookup.update({c["id"]:c for c in catalog["expansion_candidates"]})
    encoded=json.dumps(lookup).replace("<","\\u003c")
    page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Death Valley coverage atlas</title>
<style>body{font:16px/1.5 system-ui,sans-serif;background:#f5f2ea;color:#242722;margin:0;padding:24px;max-width:1250px;margin:auto}h1{font-size:28px}h2{font-size:20px}button{font:inherit;background:transparent;color:#20537a;border:0;text-decoration:underline;cursor:pointer;text-align:left}svg{width:100%;height:auto;border:1px solid #ccc}svg rect[data-id]{cursor:pointer}svg rect[data-id]:focus{stroke:#111;stroke-width:3}main{display:grid;grid-template-columns:1fr 1.35fr;gap:24px}pre{white-space:pre-wrap;overflow-wrap:anywhere;background:#fff;padding:16px;border:1px solid #ccc;max-height:380px;overflow:auto;font-size:13px}@media(max-width:800px){main{grid-template-columns:1fr}body{padding:12px}}</style>
<h1>Death Valley coverage atlas</h1><p>North up. Measured geographic footprints, not a terrain render. Park polygon: official NPS snapshot. Click an area or a Badwater tile for its record.</p>
<main><section><h2>Regional coverage</h2>OVERVIEW<ul>CARDS</ul></section><section><h2>Badwater terrain chunks</h2>DETAIL<p>Blue: retained Unity terrain. Orange: four 1 m source chunks. Brown dashed outlines: possible next batches, not built. Colored study outlines are Blender footprints.</p><pre id="record" aria-live="polite">Select a footprint or tile.</pre></section></main>
<p>Source spacing describes retained measurements. Unity renders all 256 chunks on 257 × 257 grids; interpolated vertices do not add surveyed detail. Blender presence is file-level evidence, not a fresh scene-readability check.</p>
<script>const records=DATA;function select(id){document.getElementById('record').textContent=JSON.stringify(records[id],null,2)}document.querySelectorAll('[data-id]').forEach(e=>{e.addEventListener('click',()=>select(e.dataset.id));e.addEventListener('keydown',event=>{if(event.key==='Enter'||event.key===' '){event.preventDefault();select(e.dataset.id)}})});</script></html>'''
    page=page.replace("OVERVIEW",overview).replace("DETAIL",detail).replace("CARDS","".join(cards)).replace("DATA",encoded)
    (HERE/"coverage_atlas.html").write_text(page,encoding="utf-8",newline="\n")


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--refresh-boundary",action="store_true")
    args=parser.parse_args()
    park=boundary(args.refresh_boundary)
    definitions=[
        ("original_region","Original regional overview","region.json","overview",200,"DeathValleyExplore.blend","#8b7c51"),
        ("expanded_region","Expanded regional overview","region_full_tiles.json","overview",200,"DeathValleyExploreExpanded.blend","#6a6e41"),
        ("mosaic","Mosaic Canyon study","region.json","hero",10,"DeathValleyMosaicDetail.blend","#8661a2"),
        ("corridor","Badwater corridor","badwater_corridor.json",None,40,"DeathValleyExploreExpanded.blend","#678f78"),
        ("pilot","Basin-edge pilot","badwater_transition_pilot.json",None,20,"DeathValleyExploreExpanded.blend","#d38d28"),
        ("canyon","Eastern canyon patch","badwater_canyon_patch.json",None,10,"DeathValleyExploreExpanded.blend","#b77952"),
        ("badwater","Unity Badwater four slices","badwater_four_slices.json",None,2,"BadwaterGameSlice.blend","#266aa0")]
    regions=[]
    for ident,label,name,key,spacing,blend,color in definitions:
        cfg,path=read_config(name)
        section=cfg[key] if key else cfg
        regions.append({"id":ident,"label":label,"bounds_m":section["bounds_m"],"spacing_m":spacing,"config":path,"blend":relative(find(blend)),"color":color,"footprint_evidence":"versioned_configuration","geometry_evidence":"retained_build_record_not_reopened"})
    regions.append({"id":"mosaic_focus","label":"Mosaic 1 m technique patch","bounds_m":[486650,4046900,487150,4047400],"spacing_m":1,"config":relative(find("prepare_detail.py")),"blend":relative(find("DeathValleyTerrainMaster.blend")),"color":"#8661a2","footprint_evidence":"recorded_build_and_script_default","geometry_evidence":"retained_build_record_not_reopened"})
    source=find("tiles.csv","Assets").parent
    provenance=json.loads((source/"provenance.json").read_text())
    bounds=provenance["bounds_m"]
    xmin,ymin,xmax,ymax=bounds
    if xmax-xmin!=4096 or ymax-ymin!=4096: raise ValueError("Unexpected Badwater bounds")
    rows=list(csv.DictReader((source/"tiles.csv").open(newline="")))
    if len(rows)!=256: raise ValueError("Expected 256 tiles")
    tiles=[]
    addresses=set()
    raw_arrays={}
    for row in rows:
        r,c,spacing,samples=map(int,(row["row"],row["col"],row["resolution_m"],row["samples"]))
        ident=row["id"]
        if not (0<=r<16 and 0<=c<16) or (r,c) in addresses or ident!=f"r{r:02}_c{c:02}" or spacing not in (1,2) or samples!=256//spacing+1:
            raise ValueError(f"Invalid tile address or grid: {ident}")
        addresses.add((r,c))
        height=source/"Heights"/(ident+".bytes")
        color=source/"Colors"/(ident+".png")
        if digest(height)!=row["raw_sha256"] or digest(color)!=row["color_sha256"]: raise ValueError(f"Hash mismatch: {ident}")
        raw=height.read_bytes()
        if len(raw)!=samples*samples*2: raise ValueError(f"Wrong byte count: {ident}")
        with color.open("rb") as stream: png_header=stream.read(24)
        if png_header[:8]!=b"\x89PNG\r\n\x1a\n" or png_header[12:16]!=b"IHDR" or struct.unpack(">II",png_header[16:24])!=(128,128):
            raise ValueError(f"Invalid geographic color tile: {ident}")
        import numpy as np
        raw_arrays[(r,c)]=np.frombuffer(raw,dtype="<u2").reshape(samples,samples)
        x0,y0=xmin+c*256,ymax-(r+1)*256
        quarter=("N" if r<8 else "S")+("W" if c<8 else "E")
        tiles.append({"legacy_id":ident,"geographic_key":f"epsg26911/e{x0}/n{y0}/size256","source_version":provenance["terrain_manifest_sha256"],"bounds_m":[x0,y0,x0+256,y0+256],"quarter":quarter,"source_spacing_m":spacing,"source_samples":samples,"render_samples":257,"height_source":relative(height),"height_sha256":row["raw_sha256"],"color_source":relative(color),"color_sha256":row["color_sha256"],"integrity":"hash_verified"})
    import numpy as np
    max_edge=0
    edges=0
    for (r,c),grid in raw_arrays.items():
        for neighbor,a,b in (((r,c+1),grid[:,-1],lambda g:g[:,0]),((r+1,c),grid[-1,:],lambda g:g[0,:])):
            if neighbor not in raw_arrays: continue
            other=b(raw_arrays[neighbor]); step=(len(a)-1)//(len(other)-1) if len(a)>=len(other) else 1
            if len(a)>len(other): a=a[::step]
            elif len(other)>len(a): other=other[::((len(other)-1)//(len(a)-1))]
            max_edge=max(max_edge,int(np.max(np.abs(a.astype(int)-other.astype(int)))))
            edges+=1
    if edges!=480 or max_edge!=0: raise ValueError(f"Source borders fail: {edges} edges, maximum {max_edge}")
    scene=terrain_scene()
    text=scene.read_text()
    counts={"terrains":len(re.findall(r"^--- !u!218 ",text,re.M)),"colliders":len(re.findall(r"^--- !u!154 ",text,re.M))}
    if counts!={"terrains":256,"colliders":256}: raise ValueError(f"Scene count mismatch: {counts}")
    terrain_data=list((source.parent/"TerrainData").glob("*.asset"))
    if len(terrain_data)!=256: raise ValueError("TerrainData count mismatch")
    for asset in terrain_data:
        meta=asset.with_name(asset.name+".meta")
        guid=re.search(r"^guid: ([0-9a-f]{32})$",meta.read_text(),re.M)
        if not guid or guid[1] not in text: raise ValueError(f"Unlinked TerrainData: {asset}")
    candidates=[{"id":"candidate_west","label":"West basin candidate","bounds_m":[xmin-2048,ymin,xmin,ymin+2048]}, {"id":"candidate_north","label":"North basin-edge candidate","bounds_m":[xmin,ymax,xmin+2048,ymax+2048]}, {"id":"candidate_east","label":"East mountain candidate","bounds_m":[xmax,ymin,xmax+2048,ymin+2048]}]
    for candidate in candidates: candidate.update({"status":"proposed_not_selected_not_acquired","size_km":2.048})
    assessment=HERE/"west_source_assessment.json"
    if assessment.exists():
        candidates[0].update({"status":"source_verified_locally_not_selected_not_imported","source_assessment":relative(assessment)})
    assets=[asset_record(find(name)) for name in ("DeathValleyExplore.blend","DeathValleyExploreExpanded.blend","DeathValleyRegionalStudy.blend","DeathValleyMosaicDetail.blend","DeathValleyTerrainMaster.blend","BadwaterGameSlice.blend")]
    regional=[]
    rx0,ry0,rx1,ry1=regions[1]["bounds_m"]
    if rx1-rx0!=192000 or ry1-ry0!=224000: raise ValueError("Expanded source-tile footprint changed; update the grid contract.")
    for r in range(7):
        for c in range(6):
            x0,y0=rx0+c*32000,ry1-(r+1)*32000
            regional.append({"id":f"overview_v2_r{r:02d}_c{c:02d}","expected_builder_object":f"Overview_r{r*160:04d}_c{c*160:04d}","bounds_m":[x0,y0,x0+32000,y0+32000],"source_spacing_m":200,"status":"configured_source_footprint_not_imported_to_unity","geometry_note":"Detail cutouts may omit or replace geometry; individual Blender object presence is not verified."})
    gis=[]
    for area in (ROOT/"Docs",ROOT/"Assets",ROOT/"wetransfer_blender_2026-10-06_2121"):
        if area.exists(): gis.extend(relative(p) for p in area.rglob("*") if p.is_file() and p.suffix.lower() in (".tif",".tiff",".npz",".npy",".laz",".las"))
    base_cfg,_=read_config("region.json")
    catalog={"schema_version":1,"audited_utc":datetime.now(timezone.utc).isoformat(),"working_crs":"EPSG:26911","git_head":subprocess.check_output(["git","rev-parse","HEAD"],cwd=ROOT,text=True).strip(),"regions":regions,"blender_files":assets,"unity_scene":{"path":relative(scene),"guid":TERRAIN_SCENE_GUID,"sha256":digest(scene),**counts,"terrain_data_assets":len(terrain_data),"terrain_data_guid_links_verified":True},"source_audit":{"height_files":256,"color_files":256,"hash_mismatches":0,"shared_edges_checked":edges,"max_encoded_edge_difference":max_edge,"limits":"Source export and serialized reference checks only; no Editor readback, Player performance or visual acceptance."},"unity_tiles":tiles,"expansion_candidates":candidates,"gis_files_in_scanned_roots":gis,"gis_scan_roots":["Docs","Assets","wetransfer_blender_2026-10-06_2121"],"recovery":{"original_external_path":"/Users/worldbuilder/Desktop/Death Valley Terrain Data","external_data_restored":"not_confirmed","badwater_export_rebuild":"retained_height_and_color_exports_present","original_full_precision_grid":"not_found_in_scanned_roots","sources":base_cfg["source_references"]},"park_boundary":{"path":"nps_deva_boundary.json","response_sha256":park["response_sha256"],"retrieved_utc":park["retrieved_utc"]} if park else None}
    catalog["regional_source_tiles"]=regional
    write_json(HERE/"coverage_catalog.json",catalog)
    with (HERE/"unity_tiles.csv").open("w",newline="",encoding="utf-8") as stream:
        writer=csv.DictWriter(stream,fieldnames=["legacy_id","geographic_key","quarter","xmin","ymin","xmax","ymax","source_spacing_m","source_samples","render_samples","height_source","color_source","integrity"],lineterminator="\n")
        writer.writeheader()
        for t in tiles:
            writer.writerow({k:t[k] for k in writer.fieldnames if k not in ("xmin","ymin","xmax","ymax")}|dict(zip(("xmin","ymin","xmax","ymax"),t["bounds_m"])))
    with (HERE/"regional_source_tiles.csv").open("w",newline="",encoding="utf-8") as stream:
        writer=csv.DictWriter(stream,fieldnames=["id","expected_builder_object","xmin","ymin","xmax","ymax","source_spacing_m","status","geometry_note"],lineterminator="\n")
        writer.writeheader()
        for t in regional:
            writer.writerow({k:t[k] for k in writer.fieldnames if k not in ("xmin","ymin","xmax","ymax")}|dict(zip(("xmin","ymin","xmax","ymax"),t["bounds_m"])))
    make_atlas(catalog,park)
    print(json.dumps({"regions":len(regions),"blender_files":len(assets),"unity_tiles":len(tiles),"source_audit":catalog["source_audit"],"gis_files":len(gis),"nps_boundary":park is not None,"atlas":str(HERE/"coverage_atlas.html")}))


if __name__ == "__main__":
    main()
