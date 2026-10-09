"""North-ridge-section adapter for the proven native mesh builder and reopen verifier.

The original expansion entry point and archived sources are left unchanged.
Only the new manifest's verified thirteen-section resource contract is substituted.
"""
import argparse
import json
from pathlib import Path
import sys

import numpy as np
sys.path.insert(0,str(Path(__file__).resolve().parent))
import build_expansion_scene as native

from north_row_contract import contract,expected_cells,validate_occupancy
C=None;NEW=None;ALL=None


def arguments():
    global C,NEW,ALL
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument("--step",type=int,required=True);p.add_argument("--batch")
    p.add_argument("--manifest",type=Path,required=True)
    p.add_argument("--mode",choices=["build","verify","render"],required=True)
    p.add_argument("--section",required=True)
    p.add_argument("--output",type=Path,required=True)
    p.add_argument("--stride",type=int,choices=[2,4],default=2)
    p.add_argument("--view",choices=["overhead","oblique","junction"],default="oblique")
    a=p.parse_args(sys.argv[sys.argv.index("--")+1:]);C=contract(a.step,a.batch);NEW={C["section"]};ALL=NEW|set(C["retained"])
    if a.section not in NEW|{"combined","pilot"}:p.error("Wrong section for row step")
    return a


def load(manifest):
    record=json.loads(manifest.read_text())
    if record["schema_version"]!=1 or record["unity_origin_m"]!=[522448,4008248] or record["bounds_m"]!=[499920,4006200,524496,4018488] or record.get("row_step")!=C["step"] or record.get("batch")!=C["batch"]:
        raise ValueError("Unsupported west-pair footprint")
    if native.sha256(native.ROOT/record["baseline_manifest"])!=record["baseline_manifest_sha256"]:
        raise ValueError("Baseline changed")
    if {s["id"] for s in record["sections"]}!=NEW or {s["id"] for s in record["retained_sections"]}!=ALL-NEW:
        raise ValueError("Incomplete section ownership")
    grids,colors,tiles={},{},[]
    for section in record["sections"]+record["retained_sections"]:
        name=section["id"]
        if len(section["tiles"])!=256:
            raise ValueError("Incomplete section")
        for key,h in (("mesh_grids_file","mesh_grids_sha256"),("color_file","color_sha256")):
            resource=(manifest.parent/section[key]).resolve()
            if not resource.is_relative_to(manifest.parent) or native.sha256(resource)!=section[h]:
                raise ValueError("Changed or escaped Blender resource")
        grids[name]=np.load(manifest.parent/section["mesh_grids_file"])
        colors[name]=manifest.parent/section["color_file"]
        for t in section["tiles"]:
            tiles.append(dict(t,id=name+"/"+t["local_id"],section=name,section_bounds=section["bounds_m"]))
    if len({t["geographic_key"] for t in tiles})!=C["total"] or {tuple(t["bounds_m"][:2]) for t in tiles}!=expected_cells(C,True):
        raise ValueError("Duplicate geographic identity")
    validate_occupancy(C,tiles,True)
    return record,grids,colors,tiles


def selected(tiles,section):
    if section=="combined":return tiles
    if section=="pilot":
        ids={C["section"]+"/r15_c00",C["section"]+"/r14_c00",C["section"]+"/r15_c08",C["south"]+"/r00_c00",C["south"]+"/r00_c08",C["west"]+"/r15_c15",C["west"]+"/r14_c15"}
        southwest=(C["bounds"][0]-256,C["bounds"][1]-256)
        result=[t for t in tiles if t["id"] in ids or tuple(t["bounds_m"][:2])==southwest]
        if len(result)!=8:raise ValueError("Incomplete two-border junction pilot")
        return result
    return [t for t in tiles if t["section"]==section]


original_mesh=native.mesh
original_camera=native.setup_camera


def mesh(*args):
    obj=original_mesh(*args)
    retained=args[0]["section"] not in NEW
    obj["retained_reference"]=retained
    obj.hide_select=retained
    return obj


def setup_camera(view,tiles):
    if view=="junction":
        e,n=C["bounds"][:2]
        corner=[t for t in tiles if t["bounds_m"][0] in (e-256,e) and t["bounds_m"][1] in (n-256,n)]
        if len(corner)!=4:raise ValueError("Missing four-terrain corner review")
        keys={t["geographic_key"] for t in corner}
        excluded=[(obj,obj["geographic_key"]) for obj in native.bpy.data.objects
                  if obj.type=="MESH" and "geographic_key" in obj and obj["geographic_key"] not in keys]
        # The shared camera's final fit uses all geographic objects, so narrow
        # that transient fit without changing or saving any mesh identity.
        for obj,key in excluded:del obj["geographic_key"]
        try:original_camera("oblique",corner)
        finally:
            for obj,key in excluded:obj["geographic_key"]=key
    else:original_camera(view,tiles)


if __name__=="__main__":
    native.arguments=arguments
    native.load=load
    native.selected=selected
    native.mesh=mesh
    native.setup_camera=setup_camera
    native.main()
